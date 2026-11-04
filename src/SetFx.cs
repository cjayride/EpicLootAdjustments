using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Cjayride.EpicLootAdjustments
{
    public static class SetFx
    {
        // Prefabs/asset objects and spawned instances share one "already scaled" set,
        // so an effect is never scaled twice. Permanent objects are kept; when the cap
        // is hit the instance cache is cleared (instance ids recycle, asset ids do not).
        static readonly HashSet<int> Touched = new HashSet<int>();
        const int TouchedCap = 8192;

        // ZSFX.Play fires for every sound in the game, so each emitter is resolved once:
        // matched ancestor transform, or "not a set effect" (then the prefix does nothing).
        static readonly Dictionary<int, Transform> ZsfxRoots = new Dictionary<int, Transform>();
        static readonly HashSet<int> ZsfxIgnored = new HashSet<int>();
        const int ZsfxCacheCap = 4096;

        static readonly Dictionary<string, bool> NameMatches = new Dictionary<string, bool>(StringComparer.Ordinal);
        const int NameCacheCap = 2048;

        static readonly HashSet<string> AssetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        static readonly string[] NameHints =
        {
            "IceSpikes", "Berserker", "Bulwark", "Undying", "MagicShield", "DodgeBuff",
            "sfx_berserker", "sfx_bulwark", "sfx_undying", "sfx_offset",
            "fenring_icenova", "FrostNova", "FrostAOE",
            "Earthshaker", "Firebrand", "Artillery", "Assassin", "Hexen", "Huldu",
            "SpiritAnimal", "ShieldThrow", "Kinetic", "Overwhelming", "Jotunn",
            "Gullveig", "EitrInfusion", "Quarry", "HelHarvest", "PiercingBolt",
            "ConjuredArrow", "Frostwalker", "LifeSiphon", "NineSteps"
        };

        static readonly string[] SmokeHints = { "smoke", "mist", "fog", "dust", "steam", "puff", "cloud" };

        // Reused so each applied effect allocates no component arrays.
        static readonly List<ZSFX> ZsfxBuffer = new List<ZSFX>();
        static readonly List<AudioSource> SourceBuffer = new List<AudioSource>();
        static readonly List<ParticleSystem> ParticleBuffer = new List<ParticleSystem>();
        static readonly List<TrailRenderer> TrailBuffer = new List<TrailRenderer>();
        static readonly List<Light> LightBuffer = new List<Light>();

        public static ConfigEntry<float> Volume;
        public static ConfigEntry<bool> DisableSmoke;
        public static ConfigEntry<bool> ReduceParticles;
        public static ConfigEntry<bool> DisableVisuals;

        public static void Bind(ConfigFile config)
        {
            Volume = config.Bind(
                "Set Abilities",
                "Volume",
                0.35f,
                new ConfigDescription(
                    "Volume scale for EpicLoot legendary set abilities (Ragnar frost cone, Berserker, Bulwark, Undying, and newer set abilities). 1 is the original volume.",
                    new AcceptableValueRange<float>(0f, 1f)));
            DisableSmoke = config.Bind(
                "Set Abilities",
                "DisableSmokeEffects",
                true,
                "Disable smoke, mist, fog, dust, and steam particle systems on set-ability effects.");
            ReduceParticles = config.Bind(
                "Set Abilities",
                "ReduceParticleEffects",
                true,
                "Lower particle counts on remaining set-ability visuals so they cost less FPS.");
            DisableVisuals = config.Bind(
                "Set Abilities",
                "DisableVisualEffects",
                false,
                "Turn off all particle, trail, and light visuals on set-ability effects. Sound is still scaled by Volume.");
        }

        public static void TryPatch(Harmony harmony)
        {
            PatchIfPresent(harmony, "EpicLoot.EpicLoot", "LoadAssets", nameof(OnEpicPrefabsReady));
            PatchIfPresent(harmony, "EpicLoot.EpicLoot", "ResolveAudioMixerMocks", nameof(OnEpicPrefabsReady));
            PatchIfPresent(harmony, "EpicLoot.MagicItemEffects.FrostAOE", "HookUpIceSpikesAudio", nameof(OnEpicPrefabsReady));
            PatchIfPresent(harmony, "EpicLoot.src.Magic.MagicItemEffects.Helpers.FrostNovaFx", "GetOrCreateTemplate", nameof(OnNovaTemplate));
            PatchIfPresent(harmony, "EpicLoot.src.Magic.MagicItemEffects.Helpers.FrostNovaFx", "GetOrBuildTemplate", nameof(OnNovaTemplate));
            Plugin.Log.LogInfo("EpicLoot set-ability volume default is " + Volume.Value + ".");
        }

        static void PatchIfPresent(Harmony harmony, string typeName, string methodName, string postfix)
        {
            Type type = AccessTools.TypeByName(typeName);
            MethodInfo method = FindMethod(type, methodName);
            if (method == null)
            {
                return;
            }

            harmony.Patch(method, postfix: new HarmonyMethod(typeof(SetFx), postfix));
        }

        static MethodInfo FindMethod(Type type, string methodName)
        {
            if (type == null)
            {
                return null;
            }

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (method.Name == methodName)
                {
                    return method;
                }
            }

            return null;
        }

        public static void OnEpicPrefabsReady()
        {
            Type assets = AccessTools.TypeByName("EpicLoot.EpicAssets");
            if (assets == null)
            {
                return;
            }

            foreach (FieldInfo field in assets.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType != typeof(GameObject))
                {
                    continue;
                }

                var prefab = field.GetValue(null) as GameObject;
                if (prefab != null && IsSetEffect(prefab.name))
                {
                    Apply(prefab, true);
                }
            }
        }

        public static void OnNovaTemplate(GameObject __result)
        {
            Apply(__result, true);
        }

        public static void Apply(GameObject root, bool isPrefab)
        {
            if (root == null || !IsSetEffect(root.name))
            {
                return;
            }

            float volume = Volume != null ? Mathf.Clamp01(Volume.Value) : 0.35f;
            bool visuals = DisableVisuals != null && DisableVisuals.Value;
            bool smoke = DisableSmoke != null && DisableSmoke.Value;
            bool reduce = ReduceParticles != null && ReduceParticles.Value;
            bool scaleAudio = volume < 1f;

            int id = root.GetInstanceID();

            // Each object is scaled exactly once (original behaviour): multiplying
            // volumes twice would halve the volume a second time.
            if (!MarkTouched(id))
            {
                return;
            }

            // Nothing to change: skip every component scan on this effect.
            if (!scaleAudio && !visuals && !smoke && !reduce)
            {
                return;
            }

            if (scaleAudio)
            {
                ZsfxBuffer.Clear();
                root.GetComponentsInChildren(true, ZsfxBuffer);
                for (int i = 0; i < ZsfxBuffer.Count; i++)
                {
                    ScaleSound(ZsfxBuffer[i], volume);
                }

                SourceBuffer.Clear();
                root.GetComponentsInChildren(true, SourceBuffer);
                for (int i = 0; i < SourceBuffer.Count; i++)
                {
                    if (SourceBuffer[i] != null)
                    {
                        SourceBuffer[i].volume *= volume;
                    }
                }
            }

            if (smoke || reduce || visuals)
            {
                ParticleBuffer.Clear();
                root.GetComponentsInChildren(true, ParticleBuffer);
                for (int i = 0; i < ParticleBuffer.Count; i++)
                {
                    TrimParticles(ParticleBuffer[i], smoke, reduce, visuals);
                }
            }

            if (visuals)
            {
                TrailBuffer.Clear();
                root.GetComponentsInChildren(true, TrailBuffer);
                for (int i = 0; i < TrailBuffer.Count; i++)
                {
                    if (TrailBuffer[i] != null)
                    {
                        TrailBuffer[i].enabled = false;
                    }
                }

                LightBuffer.Clear();
                root.GetComponentsInChildren(true, LightBuffer);
                for (int i = 0; i < LightBuffer.Count; i++)
                {
                    if (LightBuffer[i] != null)
                    {
                        LightBuffer[i].enabled = false;
                    }
                }
            }
        }

        static bool MarkTouched(int id)
        {
            if (Touched.Contains(id))
            {
                return false;
            }

            if (Touched.Count >= TouchedCap)
            {
                Touched.Clear();
            }

            Touched.Add(id);
            return true;
        }

        // Called by the ZSFX.Play prefix: resolves an emitter to its set-effect root once.
        public static void ResolveAndApply(ZSFX sound)
        {
            int id = sound.GetInstanceID();
            if (ZsfxIgnored.Contains(id))
            {
                return;
            }

            if (ZsfxRoots.TryGetValue(id, out Transform cached))
            {
                if (cached == null)
                {
                    // Unity instance ids recycle; the cached root was destroyed. Resolve fresh.
                    ZsfxRoots.Remove(id);
                }
                else
                {
                    Apply(cached.gameObject, false);
                    return;
                }
            }

            Transform match = null;
            Transform t = sound.transform;
            while (t != null)
            {
                if (IsSetEffect(t.name))
                {
                    match = t;
                    break;
                }

                t = t.parent;
            }

            if (ZsfxRoots.Count + ZsfxIgnored.Count >= ZsfxCacheCap)
            {
                ZsfxRoots.Clear();
                ZsfxIgnored.Clear();
            }

            if (match != null)
            {
                ZsfxRoots[id] = match;
                Apply(match.gameObject, false);
            }
            else
            {
                ZsfxIgnored.Add(id);
            }
        }

        static void ScaleSound(ZSFX sound, float volume)
        {
            if (sound == null)
            {
                return;
            }

            sound.m_minVol *= volume;
            sound.m_maxVol *= volume;
            AudioSource source = sound.GetComponent<AudioSource>();
            if (source != null)
            {
                source.volume *= volume;
            }
        }

        static void TrimParticles(ParticleSystem system, bool smoke, bool reduce, bool visuals)
        {
            if (system == null)
            {
                return;
            }

            string name = system.gameObject.name ?? "";
            bool isSmoke = ContainsAny(name, SmokeHints);
            bool kill = visuals || (isSmoke && smoke);
            if (kill)
            {
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var emission = system.emission;
                emission.enabled = false;
                system.gameObject.SetActive(false);
                return;
            }

            if (!reduce)
            {
                return;
            }

            var main = system.main;
            if (main.maxParticles > 24)
            {
                main.maxParticles = 24;
            }

            var emit = system.emission;
            if (emit.rateOverTime.constant > 8f)
            {
                emit.rateOverTime = 8f;
            }
        }

        public static bool IsSetEffect(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            name = LootMappings.Clean(name);
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (NameMatches.TryGetValue(name, out bool known))
            {
                return known;
            }

            bool match = AssetNames.Contains(name) || MatchesHint(name);
            if (NameMatches.Count < NameCacheCap)
            {
                NameMatches[name] = match;
            }

            return match;
        }

        static bool MatchesHint(string name)
        {
            for (int i = 0; i < NameHints.Length; i++)
            {
                if (name.IndexOf(NameHints[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        static bool ContainsAny(string name, string[] parts)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (name.IndexOf(parts[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(EffectList), nameof(EffectList.Create))]
    static class SetFxEffectCreatePatch
    {
        static void Postfix(GameObject[] __result)
        {
            if (__result == null || SetFx.Volume == null)
            {
                return;
            }

            for (int i = 0; i < __result.Length; i++)
            {
                GameObject go = __result[i];
                if (go != null && SetFx.IsSetEffect(go.name))
                {
                    SetFx.Apply(go, false);
                }
            }
        }
    }

    [HarmonyPatch(typeof(ZSFX), "Play")]
    static class SetFxZsfxPatch
    {
        static void Prefix(ZSFX __instance)
        {
            if (__instance == null || SetFx.Volume == null)
            {
                return;
            }

            SetFx.ResolveAndApply(__instance);
        }
    }
}
