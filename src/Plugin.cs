using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Cjayride.EpicLootAdjustments
{
    [BepInPlugin("com.cjayride.epiclootadjustments", "EpicLootAdjustments", "1.5.6")]
    [BepInDependency("randyknapp.mods.epicloot")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        public static bool IsInitialized;

        public void Awake()
        {
            Log = Logger;
            SetFx.Bind(Config);
            DropThatAllowList.Bind(Config);
            ConfigButtonHide.Bind(Config);
            OutdatedConfigHide.Bind(Config);
            var harmony = new Harmony("com.cjayride.epiclootadjustments");
            harmony.PatchAll();
            EpicLootPatches.TryPatch(harmony);
            SetFx.TryPatch(harmony);
            ConfigButtonHide.TryPatch(harmony);
            OutdatedConfigHide.TryPatch(harmony);
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
            SetFx.OnEpicPrefabsReady();
        }
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
    static class ObjectDBCopyPatch
    {
        static void Postfix()
        {
            ItemTierIndex.Invalidate();
            SetFx.OnEpicPrefabsReady();
        }
    }

    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    public static class CharacterDropPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(CharacterDrop __instance, ref List<KeyValuePair<GameObject, int>> __result)
        {
            if (__result == null || __result.Count == 0 || __instance == null)
            {
                return;
            }

            if (!LootMappings.TryGetCreature(__instance.gameObject.name, out string creature, out string zone, out int maxTier))
            {
                return;
            }

            for (int i = 0; i < __result.Count; i++)
            {
                GameObject prefab = __result[i].Key;
                if (prefab == null)
                {
                    continue;
                }

                if (DropThatAllowList.IsAllowed(creature, prefab.name))
                {
                    continue;
                }

                if (!ItemTierIndex.ShouldRemove(prefab.name, maxTier))
                {
                    continue;
                }

                Plugin.Log.LogInfo(creature + " (" + zone + ") blocked " + LootMappings.Clean(prefab.name));
                __result[i] = new KeyValuePair<GameObject, int>(prefab, 0);
            }
        }
    }

    static class EpicLootPatches
    {
        static string _creature;
        static string _zone;
        static int _maxTier;

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

            harmony.Patch(spawn,
                prefix: new HarmonyMethod(typeof(EpicLootPatches), nameof(BeginDrop)),
                finalizer: new HarmonyMethod(typeof(EpicLootPatches), nameof(EndDrop)));
            harmony.Patch(gated, postfix: new HarmonyMethod(typeof(EpicLootPatches), nameof(FilterGatedItem)));
            Plugin.Log.LogInfo("EpicLoot over-tier gear will be removed. Drops are not rewritten.");
        }

        public static void BeginDrop(string objectName)
        {
            _creature = null;
            if (!LootMappings.TryGetCreature(objectName, out _creature, out _zone, out _maxTier))
            {
                _creature = null;
            }
        }

        public static Exception EndDrop()
        {
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

                Plugin.Log.LogInfo(_creature + " (" + _zone + ") blocked " + __result);
                __result = "";
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Failed to filter " + __result + " from " + _creature + ": " + ex.Message);
            }
        }
    }
}
