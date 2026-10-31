using System;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;

namespace Cjayride.EpicLootAdjustments
{
    public static class OutdatedConfigHide
    {
        const string VersionManagerTypeName = "EpicLoot.Config.ConfigVersionManager";
        const string UpdatePromptTypeName = "EpicLoot.ConfigUpdatePrompt";

        static MethodInfo _declineOutdated;
        static PropertyInfo _hasOutdatedConfigs;
        static bool _declinedOnce;

        public static ConfigEntry<bool> HideOutdatedConfigPrompt;

        public static void Bind(ConfigFile config)
        {
            HideOutdatedConfigPrompt = config.Bind(
                "Interface",
                "HideEpicLootOutdatedConfigPrompt",
                true,
                "Hide Epic Loot's main-menu \"Outdated Configs\" update prompt. Modpack JSON is left unchanged; declining is recorded so Epic Loot does not ask again until those files change in a future Epic Loot update.");
        }

        public static void TryPatch(Harmony harmony)
        {
            Type promptType = AccessTools.TypeByName(UpdatePromptTypeName);
            if (promptType == null)
            {
                Plugin.Log.LogInfo("EpicLoot ConfigUpdatePrompt was not found. Outdated config prompt hide is skipped.");
                return;
            }

            Type versionManager = AccessTools.TypeByName(VersionManagerTypeName);
            if (versionManager != null)
            {
                _declineOutdated = AccessTools.Method(versionManager, "DeclineOutdatedConfigs");
                _hasOutdatedConfigs = AccessTools.Property(versionManager, "HasOutdatedConfigs");
                MethodInfo refresh = AccessTools.Method(versionManager, "RefreshUnmodifiedConfigs");
                if (refresh != null)
                {
                    harmony.Patch(
                        refresh,
                        postfix: new HarmonyMethod(typeof(OutdatedConfigHide), nameof(AfterConfigVersionRefresh)));
                }
            }

            PatchPrefixReturnFalse(harmony, promptType, "ShouldPrompt", nameof(BlockPrompt));
            PatchPrefixReturnFalse(harmony, promptType, "TryOpen", nameof(BlockPrompt));
            PatchPrefixReturnFalse(harmony, promptType, "ShowConfigMessage", nameof(BlockShowMessage));

            Plugin.Log.LogInfo(HideOutdatedConfigPrompt.Value
                ? "EpicLoot outdated config prompt is hidden."
                : "EpicLoot outdated config prompt is visible.");
        }

        public static void AfterConfigVersionRefresh()
        {
            TryDeclineOutdatedConfigs();
        }

        public static bool BlockPrompt(ref bool __result)
        {
            if (HideOutdatedConfigPrompt == null || !HideOutdatedConfigPrompt.Value)
            {
                return true;
            }

            TryDeclineOutdatedConfigs();
            __result = false;
            return false;
        }

        public static bool BlockShowMessage()
        {
            if (HideOutdatedConfigPrompt == null || !HideOutdatedConfigPrompt.Value)
            {
                return true;
            }

            TryDeclineOutdatedConfigs();
            return false;
        }

        static void TryDeclineOutdatedConfigs()
        {
            if (_declinedOnce || HideOutdatedConfigPrompt == null || !HideOutdatedConfigPrompt.Value)
            {
                return;
            }

            if (_declineOutdated == null)
            {
                return;
            }

            try
            {
                if (_hasOutdatedConfigs != null)
                {
                    object hasOutdated = _hasOutdatedConfigs.GetValue(null, null);
                    if (hasOutdated is bool flagged && !flagged)
                    {
                        return;
                    }
                }

                _declineOutdated.Invoke(null, null);
                _declinedOnce = true;
                Plugin.Log.LogInfo("EpicLoot outdated config files were auto-declined (modpack configs unchanged).");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Could not auto-decline EpicLoot outdated configs: " + ex.Message);
            }
        }

        static void PatchPrefixReturnFalse(Harmony harmony, Type type, string methodName, string prefixName)
        {
            MethodInfo method = AccessTools.Method(type, methodName);
            if (method == null)
            {
                Plugin.Log.LogWarning("EpicLoot method " + methodName + " was not found for outdated config hide.");
                return;
            }

            harmony.Patch(method, prefix: new HarmonyMethod(typeof(OutdatedConfigHide), prefixName));
        }
    }
}
