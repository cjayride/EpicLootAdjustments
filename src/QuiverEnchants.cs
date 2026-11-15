using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;

namespace Cjayride.EpicLootAdjustments
{
    // BetterArchery keeps the quiver in custom slot "quiver" as ItemType.Misc.
    // EpicLoot can still enchant Misc items (rarity + sockets) but magiceffects.json
    // has no Misc rows, so the roll finishes Ancient with 0 effects. BetterArchery
    // also loads after this plugin unless we depend on it, so the slot hook must
    // retry after ObjectDB. This feeds the slot into EpicLoot equipment, treats
    // the quiver as Utility/Shoulder for effect rolls, and persists EpicLoot JSON.
    public static class QuiverEnchants
    {
        const string ProviderId = "cjayride.betterarchery.quiver";
        const string MagicZdoKey = "cjayride.QuiverEpicLoot";
        const string EpicLootMagicKey = "randyknapp.mods.epicloot#EpicLoot.MagicItemComponent";

        static MethodInfo _getSlotItem;
        static MethodInfo _isMagic;
        static MethodInfo _invalidateCache;
        static MethodInfo _applyMagicJson;
        static MethodInfo _initCustomData;
        static FieldInfo _slotNameField;
        static FieldInfo _slotDataField;
        static FieldInfo _allowedItemTypes;
        static Harmony _harmony;
        static bool _applied;
        static bool _loggedMissing;
        static bool _loggedRestore;

        public static ConfigEntry<bool> EnableQuiverEnchants;

        public static void Bind(ConfigFile config)
        {
            EnableQuiverEnchants = config.Bind(
                "Compatibility",
                "EnableQuiverEnchants",
                true,
                "Count EpicLoot enchant effects on an equipped BetterArchery quiver toward character stats, and keep those enchants when you log out. The quiver can already be enchanted and take shards; without this, EpicLoot only reads vanilla equipped slots and BetterArchery does not save EpicLoot's effect list with the quiver.");
        }

        public static void TryPatch(Harmony harmony)
        {
            _harmony = harmony;
            ApplyIfReady();
        }

        public static void RetryIfNeeded()
        {
            ApplyIfReady();
        }

        static void ApplyIfReady()
        {
            if (_applied)
            {
                return;
            }

            if (AccessTools.TypeByName("BetterArchery.Patches") == null
                && AccessTools.TypeByName("Common.CustomSlotCreator") == null)
            {
                if (!_loggedMissing)
                {
                    _loggedMissing = true;
                    Plugin.Log.LogInfo("BetterArchery is not loaded yet. Quiver enchant compatibility will retry when ObjectDB loads.");
                }
                return;
            }

            if (_harmony == null)
            {
                return;
            }

            ResolveLookups();
            PatchEffectItemTypes(_harmony);
            if (!RegisterProvider() && !PatchGetMagicEquipment(_harmony))
            {
                Plugin.Log.LogWarning("Could not hook EpicLoot equipment for BetterArchery quivers.");
                return;
            }

            RegisterInventorySave();
            ListenForMagicChanges();
            PatchBetterArcheryEquip(_harmony, "EquipItemPostfix");
            PatchBetterArcheryEquip(_harmony, "UnequipItemPostfix");
            PatchBetterArcheryEquip(_harmony, "SyncQuiverFromZdo");
            PatchBetterArcheryEquip(_harmony, "InventoryLoadPostfix");
            PatchInventorySave(_harmony);
            PatchPlayerSave(_harmony);
            _applied = true;

            Plugin.Log.LogInfo(EnableQuiverEnchants.Value
                ? "BetterArchery quiver enchants count toward character stats, roll Utility/Shoulder effects, and are saved with the character."
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
            RestoreQuiverMagic(Player.m_localPlayer);
            PersistQuiverMagic(Player.m_localPlayer);
            InvalidateCache();
        }

        public static void BeforeInventorySave(Inventory __instance)
        {
            Player player = Player.m_localPlayer;
            if (player == null || __instance == null || player.GetInventory() != __instance)
            {
                return;
            }

            PersistQuiverMagic(player);
        }

        public static void BeforePlayerSave(Player __instance)
        {
            PersistQuiverMagic(__instance);
        }

        public static void OnMagicItemChanged(ItemDrop.ItemData item, string reason)
        {
            if (!Enabled() || item == null || !IsQuiverItem(item))
            {
                return;
            }

            PersistQuiverMagic(Player.m_localPlayer);
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
                _initCustomData = AccessTools.Method(itemExt, "InitializeCustomData", new[] { typeof(ItemDrop.ItemData) });
            }

            Type api = AccessTools.TypeByName("EpicLoot.API");
            if (api != null)
            {
                _invalidateCache = AccessTools.Method(api, "InvalidatePlayerEffectCache", new[] { typeof(Player) });
                _applyMagicJson = AccessTools.Method(api, "ApplyMagicItemJson", new[] { typeof(ItemDrop.ItemData), typeof(string) });
            }

            Type requirements = AccessTools.TypeByName("EpicLoot.MagicItemEffectRequirements");
            if (requirements != null)
            {
                _allowedItemTypes = AccessTools.Field(requirements, "AllowedItemTypes");
            }
        }

        static bool RegisterProvider()
        {
            Type api = AccessTools.TypeByName("EpicLoot.API");
            MethodInfo register = api == null ? null : AccessTools.Method(api, "RegisterEquipmentProvider");
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
                return ok is bool flag && flag;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("EpicLoot RegisterEquipmentProvider failed: " + ex.Message);
                return false;
            }
        }

        static void RegisterInventorySave()
        {
            Type api = AccessTools.TypeByName("EpicLoot.API");
            if (api == null)
            {
                return;
            }

            MethodInfo register = AccessTools.Method(api, "RegisterInventoryProvider");
            MethodInfo save = AccessTools.Method(api, "RegisterInventoryProviderSaveHandler");
            if (register == null || save == null)
            {
                return;
            }

            try
            {
                register.Invoke(null, new object[]
                {
                    ProviderId,
                    new Func<List<ItemDrop.ItemData>>(GetProviderItems),
                    new Func<string, int>(CountProviderItem),
                    new Func<string, int, int>((name, amount) => 0),
                    new Func<ItemDrop.ItemData, int, int>((item, amount) => 0)
                });
                save.Invoke(null, new object[]
                {
                    ProviderId,
                    new Func<ItemDrop.ItemData, bool>(SaveProviderItem)
                });
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("EpicLoot quiver inventory provider failed: " + ex.Message);
            }
        }

        static List<ItemDrop.ItemData> GetProviderItems()
        {
            var list = new List<ItemDrop.ItemData>();
            Player player = Player.m_localPlayer;
            if (!Enabled() || player == null)
            {
                return list;
            }

            ItemDrop.ItemData quiver = ReadQuiver(player);
            Inventory bag = player.GetInventory();
            if (quiver != null && (bag == null || !bag.ContainsItem(quiver)))
            {
                list.Add(quiver);
            }

            return list;
        }

        static int CountProviderItem(string sharedName)
        {
            Player player = Player.m_localPlayer;
            ItemDrop.ItemData quiver = ReadQuiver(player);
            if (quiver?.m_shared == null || quiver.m_shared.m_name != sharedName)
            {
                return 0;
            }

            Inventory bag = player == null ? null : player.GetInventory();
            if (bag != null && bag.ContainsItem(quiver))
            {
                return 0;
            }

            return quiver.m_stack;
        }

        static bool SaveProviderItem(ItemDrop.ItemData item)
        {
            if (item == null || !IsQuiverItem(item))
            {
                return false;
            }

            PersistQuiverMagic(Player.m_localPlayer);
            return true;
        }

        static void ListenForMagicChanges()
        {
            Type api = AccessTools.TypeByName("EpicLoot.API");
            MethodInfo add = api == null ? null : AccessTools.Method(api, "AddMagicItemChangedListener");
            if (add == null)
            {
                return;
            }

            try
            {
                add.Invoke(null, new object[] { new Action<ItemDrop.ItemData, string>(OnMagicItemChanged) });
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Could not listen for EpicLoot magic-item changes: " + ex.Message);
            }
        }

        static void PatchEffectItemTypes(Harmony harmony)
        {
            Type requirements = AccessTools.TypeByName("EpicLoot.MagicItemEffectRequirements");
            MethodInfo method = requirements == null
                ? null
                : AccessTools.Method(requirements, "AllowByItemType", new[] { typeof(ItemDrop.ItemData) });
            if (method == null)
            {
                Plugin.Log.LogWarning("Could not hook EpicLoot effect item types for quivers.");
                return;
            }

            try
            {
                harmony.Patch(method, postfix: new HarmonyMethod(typeof(QuiverEnchants), nameof(AfterAllowByItemType)));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Could not patch EpicLoot AllowByItemType for quivers: " + ex.Message);
            }
        }

        public static void AfterAllowByItemType(object __instance, ItemDrop.ItemData itemData, ref bool __result)
        {
            if (__result || !Enabled() || !IsQuiverItem(itemData) || _allowedItemTypes == null)
            {
                return;
            }

            try
            {
                object value = _allowedItemTypes.GetValue(__instance);
                if (!(value is IList list) || list.Count == 0)
                {
                    return;
                }

                for (int i = 0; i < list.Count; i++)
                {
                    string type = list[i] as string;
                    if (type == "Utility" || type == "Shoulder")
                    {
                        __result = true;
                        return;
                    }
                }
            }
            catch
            {
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

        static void PatchInventorySave(Harmony harmony)
        {
            MethodInfo method = AccessTools.Method(typeof(Inventory), "Save");
            if (method == null)
            {
                return;
            }

            try
            {
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(QuiverEnchants), nameof(BeforeInventorySave)));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Could not patch Inventory.Save for quiver enchants: " + ex.Message);
            }
        }

        static void PatchPlayerSave(Harmony harmony)
        {
            MethodInfo method = AccessTools.Method(typeof(Player), "Save");
            if (method == null)
            {
                return;
            }

            try
            {
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(QuiverEnchants), nameof(BeforePlayerSave)));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Could not patch Player.Save for quiver enchants: " + ex.Message);
            }
        }

        static void PersistQuiverMagic(Player player)
        {
            if (!Enabled() || player == null)
            {
                return;
            }

            ItemDrop.ItemData quiver = ReadQuiver(player) ?? FindEquippedQuiver(player);
            string json = ReadMagicJson(quiver);
            if (string.IsNullOrEmpty(json) || !HasEffectList(json))
            {
                return;
            }

            ZDO zdo = GetZdo(player);
            if (zdo == null || !zdo.IsOwner())
            {
                return;
            }

            string existing = zdo.GetString(MagicZdoKey, "");
            if (existing == json)
            {
                return;
            }

            zdo.Set(MagicZdoKey, json);
        }

        static void RestoreQuiverMagic(Player player)
        {
            if (!Enabled() || player == null)
            {
                return;
            }

            ItemDrop.ItemData quiver = ReadQuiver(player) ?? FindEquippedQuiver(player);
            if (quiver == null)
            {
                return;
            }

            if (_initCustomData != null)
            {
                try
                {
                    _initCustomData.Invoke(null, new object[] { quiver });
                }
                catch
                {
                }
            }

            string current = ReadMagicJson(quiver);
            if (HasEffectList(current))
            {
                PersistQuiverMagic(player);
                return;
            }

            ZDO zdo = GetZdo(player);
            string backup = zdo == null ? null : zdo.GetString(MagicZdoKey, "");
            if (!HasEffectList(backup) || _applyMagicJson == null)
            {
                return;
            }

            try
            {
                _applyMagicJson.Invoke(null, new object[] { quiver, backup });
                if (!_loggedRestore)
                {
                    _loggedRestore = true;
                    Plugin.Log.LogInfo("Restored EpicLoot quiver enchant data after load.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Could not restore quiver enchant data: " + ex.Message);
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

        static ItemDrop.ItemData FindEquippedQuiver(Player player)
        {
            Inventory bag = player == null ? null : player.GetInventory();
            if (bag == null)
            {
                return null;
            }

            List<ItemDrop.ItemData> equipped = bag.GetEquippedItems();
            if (equipped == null)
            {
                return null;
            }

            for (int i = 0; i < equipped.Count; i++)
            {
                if (IsQuiverItem(equipped[i]))
                {
                    return equipped[i];
                }
            }

            return null;
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

        static string ReadMagicJson(ItemDrop.ItemData item)
        {
            if (item?.m_customData == null)
            {
                return null;
            }

            string json;
            if (!item.m_customData.TryGetValue(EpicLootMagicKey, out json))
            {
                return null;
            }

            return json;
        }

        static bool HasEffectList(string json)
        {
            return !string.IsNullOrEmpty(json) && json.IndexOf("EffectType", StringComparison.Ordinal) >= 0;
        }

        static ZDO GetZdo(Player player)
        {
            if (player == null || player.m_nview == null)
            {
                return null;
            }

            return player.m_nview.GetZDO();
        }

        static void InvalidateCache()
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
    }
}
