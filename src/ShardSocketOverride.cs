using System;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;

namespace Cjayride.EpicLootAdjustments
{
    public static class ShardSocketOverride
    {
        const string SocketManagerTypeName = "EpicLoot.ShardStones.ShardSocketManager";
        const string ConflictReasonPrefix = "$mod_epicloot_socket_conflict";

        public static ConfigEntry<bool> AllowAnyShardInSlot;

        public static void Bind(ConfigFile config)
        {
            AllowAnyShardInSlot = config.Bind(
                "Shards",
                "AllowAnyShardInSlot",
                true,
                "Allow socketing a shard even when an existing enchant is in the same exclusive effect family (for example Imbue Lightning on a weapon that already has Physical Damage Dealt). Does not allow two copies of the same shardstone. Unique/boss exclusivity, item-type rules, and empty-slot checks are unchanged.");
        }

        public static void TryPatch(Harmony harmony)
        {
            Type manager = AccessTools.TypeByName(SocketManagerTypeName);
            if (manager == null)
            {
                Plugin.Log.LogInfo("EpicLoot ShardSocketManager was not found. Shard slot override is skipped.");
                return;
            }

            PatchAllowMatching(harmony, manager);
            PatchSocketResult(harmony, manager, "CanSocket");
            PatchSocketResult(harmony, manager, "CanCoexist");

            Plugin.Log.LogInfo(AllowAnyShardInSlot.Value
                ? "EpicLoot shard slots ignore exclusive-effect conflicts with existing enchants."
                : "EpicLoot shard exclusive-effect rules are unchanged.");
        }

        public static bool ForceAllowMatching(ref bool __result)
        {
            if (!Enabled())
            {
                return true;
            }

            __result = true;
            return false;
        }

        public static void AfterSocketCheck(ref bool __result, ref string reason)
        {
            if (__result || !Enabled() || string.IsNullOrEmpty(reason))
            {
                return;
            }

            if (reason.StartsWith(ConflictReasonPrefix, StringComparison.Ordinal))
            {
                __result = true;
                reason = null;
            }
        }

        static bool Enabled()
        {
            return AllowAnyShardInSlot != null && AllowAnyShardInSlot.Value;
        }

        static void PatchAllowMatching(Harmony harmony, Type manager)
        {
            MethodInfo method = AccessTools.Method(manager, "AllowMatchingItemEffect");
            if (method == null || method.ReturnType != typeof(bool))
            {
                Plugin.Log.LogWarning("EpicLoot AllowMatchingItemEffect was not found.");
                return;
            }

            try
            {
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(ShardSocketOverride), nameof(ForceAllowMatching)));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Could not patch EpicLoot AllowMatchingItemEffect: " + ex.Message);
            }
        }

        static void PatchSocketResult(Harmony harmony, Type manager, string methodName)
        {
            MethodInfo method = AccessTools.Method(manager, methodName);
            if (method == null || method.ReturnType != typeof(bool))
            {
                Plugin.Log.LogWarning("EpicLoot " + methodName + " was not found for shard slot override.");
                return;
            }

            try
            {
                harmony.Patch(method, postfix: new HarmonyMethod(typeof(ShardSocketOverride), nameof(AfterSocketCheck)));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Could not patch EpicLoot " + methodName + ": " + ex.Message);
            }
        }
    }
}
