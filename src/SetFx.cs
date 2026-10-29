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
        static readonly HashSet<int> Touched = new HashSet<int>();
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

            int id = root.GetInstanceID();
            if (!Touched.Add(id))
            {
                return;
            }

            float volume = Volume != null ? Mathf.Clamp01(Volume.Value) : 0.35f;
            ZSFX[] sounds = root.GetComponentsInChildren<ZSFX>(true);
            for (int i = 0; i < sounds.Length; i++)
            {
                ScaleSound(sounds[i], volume);
            }

            AudioSource[] sources = root.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null)
                {
                    sources[i].volume *= volume;
                }
            }

            ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                TrimParticles(particles[i]);
            }

            if (DisableVisuals != null && DisableVisuals.Value)
            {
                TrailRenderer[] trails = root.GetComponentsInChildren<TrailRenderer>(true);
                for (int i = 0; i < trails.Length; i++)
                {
                    if (trails[i] != null)
                    {
                        trails[i].enabled = false;
                    }
                }

                Light[] lights = root.GetComponentsInChildren<Light>(true);
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i] != null)
                    {
                        lights[i].enabled = false;
                    }
                }
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

        static void TrimParticles(ParticleSystem system)
        {
            if (system == null)
            {
                return;
            }

            string name = system.gameObject.name ?? "";
            bool smoke = ContainsAny(name, "smoke", "mist", "fog", "dust", "steam", "puff", "cloud");
            bool kill = (DisableVisuals != null && DisableVisuals.Value)
                || (smoke && DisableSmoke != null && DisableSmoke.Value);
            if (kill)
            {
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var emission = system.emission;
                emission.enabled = false;
                system.gameObject.SetActive(false);
                return;
            }

            if (ReduceParticles == null || !ReduceParticles.Value)
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
            name = LootMappings.Clean(name);
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (AssetNames.Contains(name))
            {
                return true;
            }

            return MatchesHint(name);
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

        static bool ContainsAny(string name, params string[] parts)
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

            Transform t = __instance.transform;
            while (t != null)
            {
                if (SetFx.IsSetEffect(t.name))
                {
                    SetFx.Apply(t.gameObject, false);
                    return;
                }

                t = t.parent;
            }
        }
    }
}
