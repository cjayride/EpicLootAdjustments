using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Cjayride.EpicLootAdjustments
{
    [BepInPlugin("com.cjayride.epiclootadjustments", "EpicLootAdjustments", "1.5.15")]
    [BepInDependency("randyknapp.mods.epicloot")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        public static bool IsInitialized;
        public static ConfigEntry<bool> LogBlockedDrops;
        public static ConfigEntry<bool> CapUnknownCreatures;

        public void Awake()
        {
            Log = Logger;
            LogBlockedDrops = Config.Bind(
                "Logging",
                "LogBlockedDrops",
                true,
                "Log each blocked over-tier drop. Turn off during large raids to avoid writing one log line per blocked item.");
            CapUnknownCreatures = Config.Bind(
                "Zones",
                "CapUnknownCreatures",
                true,
                "Cap gear dropped by creatures the mod does not have in its biome map (sleeping variants, added-mod creatures, treasure chests) at the tier of the biome the drop happened in. Without this, any creature missing from the map drops Epic Loot's full boss-gated gear list, including Swamp iron in the Meadows.");
            SetFx.Bind(Config);
            DropThatAllowList.Bind(Config);
            LuckyDrops.Bind(Config);
            ConfigButtonHide.Bind(Config);
            OutdatedConfigHide.Bind(Config);
            ShardSocketOverride.Bind(Config);
            BulwarkTuning.Bind(Config);
            var harmony = new Harmony("com.cjayride.epiclootadjustments");
            harmony.PatchAll();
            EpicLootPatches.TryPatch(harmony);
            SetFx.TryPatch(harmony);
            ConfigButtonHide.TryPatch(harmony);
            OutdatedConfigHide.TryPatch(harmony);
            ShardSocketOverride.TryPatch(harmony);
            BulwarkTuning.TryPatch(harmony);
            IsInitialized = true;
            Log.LogInfo("Ready. Mapped " + LootMappings.CreatureZones.Count + " creatures.");
        }
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    static class ObjectDBAwakePatch
    {
        static void Postfix()
        {
            ItemTierIndex.Invalidate();
            ItemTierIndex.Warm();
            ZoneContext.Invalidate();
            SetFx.OnEpicPrefabsReady();
            BulwarkTuning.Apply();
        }
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
    static class ObjectDBCopyPatch
    {
        static void Postfix()
        {
            ItemTierIndex.Invalidate();
            ItemTierIndex.Warm();
            ZoneContext.Invalidate();
            SetFx.OnEpicPrefabsReady();
            BulwarkTuning.Apply();
        }
    }

    // Shared scope resolution. A mapped creature caps at its home biome; an unmapped
    // prefab (Bjorn_sleeping, Skeleton_Meadows, a treasure chest, an added-mod creature)
    // caps at the biome the drop physically happened in, which is the CJAYCRAFT rule:
    // gear never outruns the ground you are standing on.
    static class DropScope
    {
        public static bool Resolve(string objectName, Vector3 position, out string creature, out string zone, out int maxTier)
        {
            creature = LootMappings.Clean(objectName);
            zone = null;
            maxTier = 0;

            if (LootMappings.TryGetCreature(objectName, out creature, out zone, out maxTier))
            {
                return true;
            }

            if (Plugin.CapUnknownCreatures == null || !Plugin.CapUnknownCreatures.Value)
            {
                return false;
            }

            if (!ZoneContext.TryZone(position, out maxTier, out string biome))
            {
                creature = string.IsNullOrEmpty(creature) ? objectName : creature;
                return false;
            }

            zone = "biome:" + biome;
            return true;
        }
    }

    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    public static class CharacterDropPatch
    {
        // The creature behind the drop, for the star-level jackpot rule.
        [HarmonyPrefix]
        public static void CaptureCreature(CharacterDrop __instance)
        {
            DropContext.Creature = __instance != null ? __instance.m_character : null;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(CharacterDrop __instance, ref List<KeyValuePair<GameObject, int>> __result)
        {
            if (__result == null || __result.Count == 0 || __instance == null)
            {
                return;
            }

            Transform transform = __instance.transform;
            Vector3 position = transform == null ? Vector3.zero : transform.position;
            if (!DropScope.Resolve(__instance.gameObject.name, position, out string creature, out string zone, out int maxTier))
            {
                return;
            }

            Character owner = __instance.m_character;
            if (owner == null)
            {
                owner = DropContext.Creature;
            }

            for (int i = 0; i < __result.Count; i++)
            {
                GameObject prefab = __result[i].Key;
                if (prefab == null)
                {
                    continue;
                }

                if (!ItemTierIndex.ShouldRemove(prefab.name, maxTier))
                {
                    continue;
                }

                int itemTier = ItemTierIndex.ItemTier(prefab.name);
                if (DropThatAllowList.IsAllowed(creature, prefab.name, maxTier, itemTier))
                {
                    continue;
                }

                if (LuckyDrops.TryAllow(owner, creature, itemTier, maxTier, prefab.name))
                {
                    continue;
                }

                if (Plugin.LogBlockedDrops == null || Plugin.LogBlockedDrops.Value)
                {
                    Plugin.Log.LogInfo(creature + " (" + zone + ") blocked " + LootMappings.Clean(prefab.name));
                }

                __result[i] = new KeyValuePair<GameObject, int>(prefab, 0);
            }

            // Runs after Epic Loot's roll (Priority.Last): the live creature is no longer needed.
            DropContext.Creature = null;
        }
    }

    // The creature that is currently generating drops, for rules that need the
    // live Character (star level) rather than a prefab name.
    static class DropContext
    {
        public static Character Creature;
    }

    static class EpicLootPatches
    {
        static string _creature;
        static string _zone;
        static int _maxTier;
        static int _rollDepth;

        public static void TryPatch(Harmony harmony)
        {
            Type roller = AccessTools.TypeByName("EpicLoot.LootRoller");
            Type helper = AccessTools.TypeByName("EpicLoot.GatedItemType.GatedItemTypeHelper");
            if (roller == null || helper == null)
            {
                Plugin.Log.LogInfo("EpicLoot is not loaded. Ordinary creature drops are still filtered.");
                return;
            }

            MethodInfo spawn = AccessTools.Method(roller, "SpawnNormalItem");
            MethodInfo gated = null;
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(helper))
            {
                if (method.Name != "GetGatedItemNameFromItemOrType" || method.ReturnType != typeof(string))
                {
                    continue;
                }

                if (method.GetParameters().Length == 2)
                {
                    gated = method;
                    break;
                }
            }

            if (spawn == null || gated == null)
            {
                Plugin.Log.LogWarning("EpicLoot is loaded, but its loot methods were not found. Magic drops will not be filtered.");
                return;
            }

            var begin = new HarmonyMethod(typeof(EpicLootPatches), nameof(BeginDrop));
            var end = new HarmonyMethod(typeof(EpicLootPatches), nameof(EndDrop));
            harmony.Patch(spawn, prefix: begin, finalizer: end);

            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(roller))
            {
                if (method.Name != "RollLootTableInternal")
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                bool hasObjectName = false;
                bool hasDropPoint = false;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].Name == "objectName")
                    {
                        hasObjectName = true;
                    }

                    if (parameters[i].Name == "dropPoint" && parameters[i].ParameterType == typeof(Vector3))
                    {
                        hasDropPoint = true;
                    }
                }

                if (hasObjectName)
                {
                    harmony.Patch(method,
                        prefix: hasDropPoint ? Begin : BeginNoPosition,
                        finalizer: End);
                }
            }

            harmony.Patch(gated, postfix: new HarmonyMethod(typeof(EpicLootPatches), nameof(FilterGatedItem)));
            Plugin.Log.LogInfo("EpicLoot over-tier gear will be removed. Drops are not rewritten.");
        }

        // RollLootTableInternal exposes the drop point, so an unmapped creature can be
        // capped by the biome it died in. The overload without one caps by creature map.
        static readonly HarmonyMethod Begin = new HarmonyMethod(typeof(EpicLootPatches), nameof(BeginDrop));
        static readonly HarmonyMethod BeginNoPosition = new HarmonyMethod(typeof(EpicLootPatches), nameof(BeginDropNoPosition));
        static readonly HarmonyMethod End = new HarmonyMethod(typeof(EpicLootPatches), nameof(EndDrop));

        public static void BeginDrop(string objectName, Vector3 dropPoint)
        {
            if (_rollDepth++ > 0)
            {
                return;
            }

            DropScope.Resolve(objectName, dropPoint, out _creature, out _zone, out _maxTier);
        }

        public static void BeginDropNoPosition(string objectName)
        {
            if (_rollDepth++ > 0)
            {
                return;
            }

            DropScope.Resolve(objectName, Vector3.zero, out _creature, out _zone, out _maxTier);
        }

        public static Exception EndDrop()
        {
            if (--_rollDepth > 0)
            {
                return null;
            }

            _rollDepth = 0;
            _creature = null;
            return null;
        }

        public static void FilterGatedItem(ref string __result)
        {
            if (string.IsNullOrEmpty(_creature) || string.IsNullOrEmpty(__result))
            {
                return;
            }

            try
            {
                if (!ItemTierIndex.ShouldRemove(__result, _maxTier))
                {
                    return;
                }

                int gatedTier = ItemTierIndex.ItemTier(__result);
                if (DropThatAllowList.IsAllowed(_creature, __result, _maxTier, gatedTier))
                {
                    return;
                }

                if (LuckyDrops.TryAllow(DropContext.Creature, _creature, gatedTier, _maxTier, __result))
                {
                    return;
                }

                if (Plugin.LogBlockedDrops == null || Plugin.LogBlockedDrops.Value)
                {
                    Plugin.Log.LogInfo(_creature + " (" + _zone + ") blocked " + __result);
                }

                __result = "";
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Failed to filter " + __result + " from " + _creature + ": " + ex.Message);
            }
        }
    }
}
