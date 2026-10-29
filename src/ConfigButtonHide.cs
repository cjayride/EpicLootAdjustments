using System;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Cjayride.EpicLootAdjustments
{
    public static class ConfigButtonHide
    {
        static readonly string[] BrokerTypeNames =
        {
            "Common.QuickConfigBroker",
            "EpicLoot.QuickConfigBroker",
            "EpicLoot.Common.QuickConfigBroker"
        };

        static Type _brokerType;
        static FieldInfo _instanceField;
        static FieldInfo _pauseButtonField;
        static FieldInfo _mainMenuButtonField;
        static FieldInfo _listPanelField;
        static bool _quickConfigAvailable;

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
            _brokerType = ResolveBrokerType();
            if (_brokerType == null)
            {
                Plugin.Log.LogInfo("EpicLoot Quick Config button was not found. Config button hide is skipped (no Menu.Update work).");
                return;
            }

            const BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            _instanceField = _brokerType.GetField("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            _pauseButtonField = _brokerType.GetField("pauseButton", instanceFlags);
            _mainMenuButtonField = _brokerType.GetField("mainMenuButton", instanceFlags);
            _listPanelField = _brokerType.GetField("listPanel", instanceFlags);
            _quickConfigAvailable = true;

            PatchPrefix(harmony, _brokerType, "TryBuildButtons", nameof(SkipIfHidden));
            PatchPrefix(harmony, _brokerType, "EnsurePauseButton", nameof(SkipIfHidden));
            PatchPrefix(harmony, _brokerType, "OnMenuStart", nameof(SkipIfHidden));
            MethodInfo refresh = FindMethod(_brokerType, "RefreshVisibility");
            if (refresh != null)
            {
                harmony.Patch(refresh, postfix: new HarmonyMethod(typeof(ConfigButtonHide), nameof(HideBrokerButtons)));
            }

            MethodInfo menuShow = AccessTools.Method(typeof(Menu), "Show");
            if (menuShow != null)
            {
                harmony.Patch(menuShow, postfix: new HarmonyMethod(typeof(ConfigButtonHide), nameof(HideBrokerButtons)));
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
            if (!_quickConfigAvailable || HideButton == null || !HideButton.Value)
            {
                return;
            }

            object instance = _instanceField?.GetValue(null);
            if (instance == null)
            {
                return;
            }

            HideField(_pauseButtonField, instance);
            HideField(_mainMenuButtonField, instance);
            HideField(_listPanelField, instance);
        }

        static Type ResolveBrokerType()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (string name in BrokerTypeNames)
            {
                foreach (Assembly assembly in assemblies)
                {
                    Type type = assembly.GetType(name, false);
                    if (type != null)
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        static void HideField(FieldInfo field, object instance)
        {
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
}
