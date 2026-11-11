using System;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Cjayride.EpicLootAdjustments
{
    // Bulwark's damage-immunity window lives in EpicLoot's bundled prefab
    // ("BulwarkStatusEffect"), not in any JSON config: the ability applies the
    // status effect with duration 0, which makes Valheim fall back to the
    // prefab's own m_ttl. The per-rarity values in magiceffects.json are not
    // read by the ability, so this sets the prefab duration once, as soon as
    // EpicLoot has loaded its assets. Assigning a fixed value is idempotent,
    // so every late hook (ObjectDB copy, asset reload) is safe.
    public static class BulwarkTuning
    {
        public static ConfigEntry<float> Duration;

        public static void Bind(ConfigFile config)
        {
            Duration = config.Bind(
                "Set Abilities",
                "BulwarkDuration",
                3f,
                new ConfigDescription(
                    "Seconds of damage immunity granted by the Heimdall 3-piece ability Bulwark. EpicLoot ships 5s inside its own asset bundle, where no config file can reach it; the rarity values in magiceffects.json are not used by the ability. Set to 5 to restore the stock duration.",
                    new AcceptableValueRange<float>(1f, 10f)));
        }

        public static void TryPatch(Harmony harmony)
        {
            PatchHook(harmony, "EpicLoot.EpicLoot", "LoadAssets");
            PatchHook(harmony, "EpicLoot.EpicLoot", "ResolveAudioMixerMocks");
            Plugin.Log.LogInfo("Bulwark duration is set to " + Duration.Value + "s when EpicLoot assets load.");
        }

        public static void Apply()
        {
            if (Duration == null)
            {
                return;
            }

            float seconds = Duration.Value;
            if (seconds <= 0f)
            {
                return;
            }

            Type assets = AccessTools.TypeByName("EpicLoot.EpicAssets");
            if (assets == null)
            {
                return;
            }

            FieldInfo field = AccessTools.Field(assets, "BulwarkStatusEffect");
            if (field == null)
            {
                return;
            }

            SE_Stats stats = field.GetValue(null) as SE_Stats;
            if (stats == null)
            {
                return;
            }

            stats.m_ttl = seconds;
        }

        static void PatchHook(Harmony harmony, string typeName, string methodName)
        {
            Type type = AccessTools.TypeByName(typeName);
            if (type == null)
            {
                return;
            }

            MethodInfo method = null;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            foreach (MethodInfo candidate in type.GetMethods(flags))
            {
                if (candidate.Name == methodName)
                {
                    method = candidate;
                    break;
                }
            }

            if (method == null)
            {
                Plugin.Log.LogWarning("EpicLoot " + methodName + " was not found; Bulwark duration stays at its built-in value.");
                return;
            }

            try
            {
                harmony.Patch(method, postfix: new HarmonyMethod(typeof(BulwarkTuning), nameof(Apply)));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Could not patch EpicLoot " + methodName + ": " + ex.Message);
            }
        }
    }
}
