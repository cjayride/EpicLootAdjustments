using System;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;

namespace Cjayride.EpicLootAdjustments
{
    public static class ShardSocketOverride
    {
        const string SocketManagerTypeName = "EpicLoot.ShardStones.ShardSocketManager";

        public static ConfigEntry<bool> AllowAnyShardInSlot;

        public static void Bind(ConfigFile config)
        {
            AllowAnyShardInSlot = config.Bind(
                "Shards",
                "AllowAnyShardInSlot",
                true,
                "Allow socketing a shard even when the item already has a matching magic effect (for example +damage). Epic Loot normally blocks that even though enchant rolls can already stack different damage types. Unique/boss shard exclusivity is left unchanged. Items still need an empty shard slot.");
        }

        public static void TryPatch(Harmony harmony)
        {
            Type manager = AccessTools.TypeByName(SocketManagerTypeName);
            if (manager == null)
            {
                Plugin.Log.LogInfo("EpicLoot ShardSocketManager was not found. Shard slot override is skipped.");
                return;
            }

            PatchBoolResult(harmony, manager, "AllowMatchingItemEffect", true);
            PatchBoolResult(harmony, manager, "CheckDuplicateEffect", false);
            PatchBoolResult(harmony, manager, "CanCoexist", true);

            Plugin.Log.LogInfo(AllowAnyShardInSlot.Value
                ? "EpicLoot shard slots accept shards that match an existing item effect."
                : "EpicLoot shard duplicate-effect rules are unchanged.");
        }

        public static bool Enabled()
        {
            return AllowAnyShardInSlot != null && AllowAnyShardInSlot.Value;
        }

        public static bool ForceTrue(ref bool __result)
        {
            if (!Enabled())
            {
                return true;
            }

            __result = true;
            return false;
        }

        public static bool ForceFalse(ref bool __result)
        {
            if (!Enabled())
            {
                return true;
            }

            __result = false;
            return false;
        }

        static void PatchBoolResult(Harmony harmony, Type type, string methodName, bool forcedValue)
        {
            MethodInfo method = AccessTools.Method(type, methodName);
            if (method == null)
            {
                Plugin.Log.LogWarning("EpicLoot method " + methodName + " was not found for shard slot override.");
                return;
            }

            if (method.ReturnType != typeof(bool))
            {
                Plugin.Log.LogWarning("EpicLoot method " + methodName + " does not return bool; shard slot override skipped for it.");
                return;
            }

            try
            {
                string prefix = forcedValue ? nameof(ForceTrue) : nameof(ForceFalse);
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(ShardSocketOverride), prefix));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Could not patch EpicLoot " + methodName + ": " + ex.Message);
            }
        }
    }
}
