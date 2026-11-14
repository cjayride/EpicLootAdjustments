using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;

namespace Cjayride.EpicLootAdjustments
{
    // BetterArchery puts the quiver in a custom equipment slot ("quiver").
    // EpicLoot only sums magic effects from Inventory.GetEquippedItems() (m_equipped
    // items still in the bag). Custom-slot items are stored off that list, so the
    // quiver can be enchanted and socketed while its effects never reach character
    // stats. EpicLoot already exposes RegisterEquipmentProvider for this case.
    public static class QuiverEnchants
    {
        const string ProviderId = "cjayride.betterarchery.quiver";

        static MethodInfo _getSlotItem;
        static MethodInfo _isMagic;
        static MethodInfo _invalidateCache;
        static FieldInfo _slotNameField;
        static FieldInfo _slotDataField;
        static bool _registered;

        public static ConfigEntry<bool> EnableQuiverEnchants;

        public static void Bind(ConfigFile config)
        {
            EnableQuiverEnchants = config.Bind(
                "Compatibility",
                "EnableQuiverEnchants",
                true,
                "Count EpicLoot enchant effects on an equipped BetterArchery quiver toward character stats. The quiver can already be enchanted and take shards; without this, those effects never apply because EpicLoot only reads vanilla equipped slots.");
        }

        public static void TryPatch(Harmony harmony)
        {
            if (AccessTools.TypeByName("BetterArchery.Patches") == null
                && AccessTools.TypeByName("Common.CustomSlotCreator") == null)
            {
                Plugin.Log.LogInfo("BetterArchery was not found. Quiver enchant compatibility is skipped.");
                return;
            }

            ResolveLookups();
            if (!RegisterProvider() && !PatchGetMagicEquipment(harmony))
            {
                Plugin.Log.LogWarning("Could not hook EpicLoot equipment for BetterArchery quivers.");
                return;
            }

            PatchBetterArcheryEquip(harmony, "EquipItemPostfix");
            PatchBetterArcheryEquip(harmony, "UnequipItemPostfix");
            PatchBetterArcheryEquip(harmony, "SyncQuiverFromZdo");
            PatchBetterArcheryEquip(harmony, "InventoryLoadPostfix");

            Plugin.Log.LogInfo(EnableQuiverEnchants.Value
                ? "BetterArchery quiver enchants count toward character stats."
                : "BetterArchery quiver enchants are left as EpicLoot ships them (not applied).");
        }

        public static List<ItemDrop.ItemData> GetExtraEquipped(Player player)
        {
            var list = new List<ItemDrop.ItemData>();
            if (!Enabled() || player == null)
            {
                return list;
            }

            ItemDrop.ItemData quiver = ReadQuiver(player);
            if (quiver != null && IsMagicItem(quiver))
            {
                list.Add(quiver);
            }

            return list;
        }

        public static void AfterGetMagicEquipment(Player player, List<ItemDrop.ItemData> __result)
        {
            if (!Enabled() || player == null || __result == null)
            {
                return;
            }

            ItemDrop.ItemData quiver = ReadQuiver(player);
            if (quiver == null || !IsMagicItem(quiver) || __result.Contains(quiver))
            {
                return;
            }

            __result.Add(quiver);
        }

        public static void AfterQuiverEquipChanged()
        {
            if (_invalidateCache == null || Player.m_localPlayer == null)
            {
                return;
            }

            try
            {
                _invalidateCache.Invoke(null, new object[] { Player.m_localPlayer });
            }
            catch
            {
            }
        }

        static bool Enabled()
        {
            return EnableQuiverEnchants != null && EnableQuiverEnchants.Value;
        }

        static void ResolveLookups()
        {
            Type creator = AccessTools.TypeByName("Common.CustomSlotCreator");
            if (creator != null)
            {
                _getSlotItem = AccessTools.Method(creator, "GetSlotItem", new[] { typeof(Humanoid), typeof(string) })
                    ?? AccessTools.Method(creator, "GetSlotItem", new[] { typeof(Player), typeof(string) });
                _slotDataField = AccessTools.Field(creator, "customSlotItemData");
            }

            Type patches = AccessTools.TypeByName("BetterArchery.Patches");
            if (patches != null)
            {
                _slotNameField = AccessTools.Field(patches, "QuiverSlotName");
            }

            Type itemExt = AccessTools.TypeByName("EpicLoot.ItemDataExtensions");
            if (itemExt != null)
            {
                _isMagic = AccessTools.Method(itemExt, "IsMagic", new[] { typeof(ItemDrop.ItemData) });
            }

            Type api = AccessTools.TypeByName("EpicLoot.API");
            if (api != null)
            {
                _invalidateCache = AccessTools.Method(api, "InvalidatePlayerEffectCache", new[] { typeof(Player) });
            }
        }

        static bool RegisterProvider()
        {
            Type api = AccessTools.TypeByName("EpicLoot.API");
            MethodInfo register = api == null
                ? null
                : AccessTools.Method(api, "RegisterEquipmentProvider");
            if (register == null)
            {
                return false;
            }

            try
            {
                object ok = register.Invoke(null, new object[]
                {
                    ProviderId,
                    new Func<Player, List<ItemDrop.ItemData>>(GetExtraEquipped)
                });
                _registered = ok is bool flag && flag;
                return _registered;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("EpicLoot RegisterEquipmentProvider failed: " + ex.Message);
                return false;
            }
        }

        static bool PatchGetMagicEquipment(Harmony harmony)
        {
            Type ext = AccessTools.TypeByName("EpicLoot.PlayerExtensions");
            MethodInfo method = ext == null ? null : AccessTools.Method(ext, "GetMagicEquipment");
            if (method == null)
            {
                return false;
            }

            try
            {
                harmony.Patch(method, postfix: new HarmonyMethod(typeof(QuiverEnchants), nameof(AfterGetMagicEquipment)));
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Could not patch EpicLoot GetMagicEquipment: " + ex.Message);
                return false;
            }
        }

        static void PatchBetterArcheryEquip(Harmony harmony, string methodName)
        {
            Type patches = AccessTools.TypeByName("BetterArchery.Patches");
            MethodInfo method = patches == null ? null : AccessTools.Method(patches, methodName);
            if (method == null)
            {
                return;
            }

            try
            {
                harmony.Patch(method, postfix: new HarmonyMethod(typeof(QuiverEnchants), nameof(AfterQuiverEquipChanged)));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Could not patch BetterArchery " + methodName + ": " + ex.Message);
            }
        }

        static ItemDrop.ItemData ReadQuiver(Player player)
        {
            string slot = SlotName();
            if (_getSlotItem != null)
            {
                try
                {
                    object found = _getSlotItem.Invoke(null, new object[] { player, slot });
                    if (found is ItemDrop.ItemData item)
                    {
                        return item;
                    }
                }
                catch
                {
                }
            }

            return ReadQuiverFromSlotMap(player, slot);
        }

        static ItemDrop.ItemData ReadQuiverFromSlotMap(Player player, string slot)
        {
            if (_slotDataField == null)
            {
                return null;
            }

            object map;
            try
            {
                map = _slotDataField.GetValue(null);
            }
            catch
            {
                return null;
            }

            if (!(map is IDictionary dict))
            {
                return null;
            }

            object slots = null;
            if (dict.Contains(player))
            {
                slots = dict[player];
            }
            else if (dict.Contains((Humanoid)player))
            {
                slots = dict[(Humanoid)player];
            }

            if (slots is IDictionary byName)
            {
                if (byName.Contains(slot) && byName[slot] is ItemDrop.ItemData named)
                {
                    return named;
                }

                foreach (DictionaryEntry entry in byName)
                {
                    if (entry.Value is ItemDrop.ItemData item && IsQuiverItem(item))
                    {
                        return item;
                    }
                }
            }

            return null;
        }

        static bool IsQuiverItem(ItemDrop.ItemData item)
        {
            if (item == null)
            {
                return false;
            }

            string prefab = item.m_dropPrefab == null ? null : item.m_dropPrefab.name;
            if (!string.IsNullOrEmpty(prefab) && prefab.IndexOf("quiver", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            string name = item.m_shared == null ? null : item.m_shared.m_name;
            return !string.IsNullOrEmpty(name) && name.IndexOf("quiver", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static string SlotName()
        {
            if (_slotNameField != null)
            {
                try
                {
                    object value = _slotNameField.GetValue(null);
                    if (value is string name && !string.IsNullOrEmpty(name))
                    {
                        return name;
                    }
                }
                catch
                {
                }
            }

            return "quiver";
        }

        static bool IsMagicItem(ItemDrop.ItemData item)
        {
            if (item == null || _isMagic == null)
            {
                return false;
            }

            try
            {
                return _isMagic.Invoke(null, new object[] { item }) is bool magic && magic;
            }
            catch
            {
                return false;
            }
        }
    }
}
