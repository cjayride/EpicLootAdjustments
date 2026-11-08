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
                "Allow socketing a shard even when the item already has that same magic effect from an enchant (for example +damage). Unique/boss exclusivity and empty-slot checks are unchanged.");
        }

        public static void TryPatch(Harmony harmony)
        {
            Type manager = AccessTools.TypeByName(SocketManagerTypeName);
            if (manager == null)
            {
                Plugin.Log.LogInfo("EpicLoot ShardSocketManager was not found. Shard slot override is skipped.");
                return;
            }

            MethodInfo method = AccessTools.Method(manager, "AllowMatchingItemEffect");
            if (method == null || method.ReturnType != typeof(bool))
            {
                Plugin.Log.LogWarning("EpicLoot AllowMatchingItemEffect was not found. Shard slot override is skipped.");
                return;
            }

            try
            {
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(ShardSocketOverride), nameof(ForceAllowMatching)));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Could not patch EpicLoot AllowMatchingItemEffect: " + ex.Message);
                return;
            }

            Plugin.Log.LogInfo(AllowAnyShardInSlot.Value
                ? "EpicLoot shard slots accept shards that match an existing item effect."
                : "EpicLoot shard duplicate-effect rules are unchanged.");
        }

        public static bool ForceAllowMatching(ref bool __result)
        {
            if (AllowAnyShardInSlot == null || !AllowAnyShardInSlot.Value)
            {
                return true;
            }

            __result = true;
            return false;
        }
    }
}
