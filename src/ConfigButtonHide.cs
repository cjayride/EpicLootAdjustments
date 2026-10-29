using System;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Cjayride.EpicLootAdjustments
{
    public static class ConfigButtonHide
    {
        public static ConfigEntry<bool> HideButton;

        public static void Bind(ConfigFile config)
        {
            HideButton = config.Bind(
                "Interface",
                "HideEpicLootConfigButton",
                true,
                "Hide the Epic Loot / Mod Config button on the main menu and pause screen. The BepInEx config files still work.");
        }

        public static void TryPatch(Harmony harmony)
        {
            Type broker = AccessTools.TypeByName("Common.QuickConfigBroker");
            if (broker == null)
            {
                Plugin.Log.LogInfo("EpicLoot Quick Config button was not found (older EpicLoot).");
                return;
            }

            PatchPrefix(harmony, broker, "TryBuildButtons", nameof(SkipIfHidden));
            PatchPrefix(harmony, broker, "EnsurePauseButton", nameof(SkipIfHidden));
            PatchPrefix(harmony, broker, "OnMenuStart", nameof(SkipIfHidden));
            MethodInfo refresh = FindMethod(broker, "RefreshVisibility");
            if (refresh != null)
            {
                harmony.Patch(refresh, postfix: new HarmonyMethod(typeof(ConfigButtonHide), nameof(HideBrokerButtons)));
            }

            Plugin.Log.LogInfo(HideButton.Value
                ? "EpicLoot pause-screen config button is hidden."
                : "EpicLoot pause-screen config button is visible.");
        }

        public static bool SkipIfHidden()
        {
            return HideButton == null || !HideButton.Value;
        }

        public static void HideBrokerButtons()
        {
            if (HideButton == null || !HideButton.Value)
            {
                return;
            }

            Type broker = AccessTools.TypeByName("Common.QuickConfigBroker");
            object instance = broker?.GetField("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if (instance == null)
            {
                return;
            }

            HideField(broker, instance, "pauseButton");
            HideField(broker, instance, "mainMenuButton");
            HideField(broker, instance, "listPanel");
        }

        static void HideField(Type type, object instance, string fieldName)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var go = field?.GetValue(instance) as GameObject;
            if (go != null && go.activeSelf)
            {
                go.SetActive(false);
            }
        }

        static void PatchPrefix(Harmony harmony, Type type, string methodName, string prefix)
        {
            MethodInfo method = FindMethod(type, methodName);
            if (method == null)
            {
                return;
            }

            harmony.Patch(method, prefix: new HarmonyMethod(typeof(ConfigButtonHide), prefix));
        }

        static MethodInfo FindMethod(Type type, string methodName)
        {
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
    }

    [HarmonyPatch(typeof(Menu), nameof(Menu.Update))]
    static class MenuConfigButtonPatch
    {
        static void Postfix()
        {
            ConfigButtonHide.HideBrokerButtons();
        }
    }
}
