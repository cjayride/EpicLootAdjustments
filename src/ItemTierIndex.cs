using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cjayride.EpicLootAdjustments
{
    public static class ItemTierIndex
    {
        class ItemRecord
        {
            public string Name;
            public int Tier;
            public ItemDrop.ItemData.ItemType Type;
        }

        static readonly Dictionary<string, ItemRecord> ByName = new Dictionary<string, ItemRecord>();
        static readonly Dictionary<string, List<Recipe>> Recipes = new Dictionary<string, List<Recipe>>();
        static readonly Dictionary<string, int> TierCache = new Dictionary<string, int>();
        static readonly HashSet<string> Visiting = new HashSet<string>();
        // Names confirmed to have no indexed item data. Without this, every drop of an
        // unindexed item repeats ObjectDB.GetItemPrefab's linear scan plus a GetComponent.
        static readonly HashSet<string> Missing = new HashSet<string>(System.StringComparer.Ordinal);
        const int MissingCap = 4096;
        static bool _ready;
        static int _indexedRecipeCount;
        static int _indexedItemCount;

        public static void Invalidate()
        {
            _ready = false;
            _indexedRecipeCount = -1;
            _indexedItemCount = -1;
            Missing.Clear();
        }

        // Builds the index up front (ObjectDB Awake) so the first creature kill in a
        // fight does not pay for the whole item/recipe scan.
        public static void Warm()
        {
            EnsureBuilt();
        }

        // True only for over-tier weapons, armor, shields, tools, and ammo.
        // Never rewrites a drop into a different item. Materials, shardstones,
        // food, and trophies are left exactly as rolled.
        public static bool ShouldRemove(string itemName, int maxTier)
        {
            itemName = LootMappings.Clean(itemName);
            if (string.IsNullOrEmpty(itemName) || LootMappings.IsShardStone(itemName))
            {
                return false;
            }

            EnsureBuilt();
            ItemRecord record = Find(itemName);
            if (record == null || !IsGear(record.Type))
            {
                return false;
            }

            return record.Tier > maxTier;
        }

        static bool IsGear(ItemDrop.ItemData.ItemType type)
        {
            switch (type)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.Shield:
                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Shoulder:
                case ItemDrop.ItemData.ItemType.Utility:
                case ItemDrop.ItemData.ItemType.Tool:
                case ItemDrop.ItemData.ItemType.Torch:
                case ItemDrop.ItemData.ItemType.Attach_Atgeir:
                case ItemDrop.ItemData.ItemType.Ammo:
                case ItemDrop.ItemData.ItemType.AmmoNonEquipable:
                    return true;
                default:
                    return false;
            }
        }

        static ItemRecord Find(string itemName)
        {
            if (ByName.TryGetValue(itemName, out ItemRecord record))
            {
                return record;
            }

            if (Missing.Contains(itemName))
            {
                return null;
            }

            GameObject prefab = ObjectDB.instance?.GetItemPrefab(itemName);
            if (prefab == null)
            {
                if (Missing.Count < MissingCap)
                {
                    Missing.Add(itemName);
                }

                return null;
            }

            ItemRecord added = AddPrefab(prefab);
            if (added == null && Missing.Count < MissingCap)
            {
                Missing.Add(itemName);
            }

            return added;
        }

        static bool EnsureBuilt()
        {
            ObjectDB db = ObjectDB.instance;
            if (db == null || db.m_items == null || db.m_items.Count == 0)
            {
                return false;
            }

            int recipeCount = db.m_recipes == null ? 0 : db.m_recipes.Count;
            int itemCount = db.m_items.Count;
            if (_ready && recipeCount == _indexedRecipeCount && itemCount == _indexedItemCount)
            {
                return true;
            }

            try
            {
                Build(db);
                _ready = true;
                _indexedRecipeCount = recipeCount;
                _indexedItemCount = itemCount;
                Plugin.Log.LogInfo("Indexed " + ByName.Count + " items from " + recipeCount + " recipes for zone checks.");
                return true;
            }
            catch (Exception ex)
            {
                _ready = true;
                _indexedRecipeCount = recipeCount;
                _indexedItemCount = itemCount;
                Plugin.Log.LogError("Failed to index item tiers: " + ex);
                return false;
            }
        }

        static void Build(ObjectDB db)
        {
            ByName.Clear();
            Recipes.Clear();
            TierCache.Clear();
            Visiting.Clear();

            if (db.m_recipes != null)
            {
                for (int i = 0; i < db.m_recipes.Count; i++)
                {
                    Recipe recipe = db.m_recipes[i];
                    if (recipe?.m_item == null)
                    {
                        continue;
                    }

                    string result = LootMappings.Clean(recipe.m_item.name);
                    if (string.IsNullOrEmpty(result))
                    {
                        continue;
                    }

                    if (!Recipes.TryGetValue(result, out List<Recipe> list))
                    {
                        list = new List<Recipe>();
                        Recipes.Add(result, list);
                    }

                    list.Add(recipe);
                }
            }

            for (int i = 0; i < db.m_items.Count; i++)
            {
                AddPrefab(db.m_items[i]);
            }
        }

        static ItemRecord AddPrefab(GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            ItemDrop drop = prefab.GetComponent<ItemDrop>();
            if (drop?.m_itemData?.m_shared == null)
            {
                return null;
            }

            string name = LootMappings.Clean(prefab.name);
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (ByName.TryGetValue(name, out ItemRecord existing))
            {
                return existing;
            }

            var shared = drop.m_itemData.m_shared;
            var record = new ItemRecord
            {
                Name = name,
                Type = shared.m_itemType,
                Tier = ComputeTier(name)
            };
            ByName.Add(name, record);
            return record;
        }

        static int ComputeTier(string name)
        {
            if (TierCache.TryGetValue(name, out int cached))
            {
                return cached;
            }

            if (!Visiting.Add(name))
            {
                return LootMappings.MaterialTier(name);
            }

            int tier = LootMappings.MaterialTier(name);
            if (Recipes.TryGetValue(name, out List<Recipe> recipes))
            {
                for (int i = 0; i < recipes.Count; i++)
                {
                    Piece.Requirement[] resources = recipes[i].m_resources;
                    if (resources == null)
                    {
                        continue;
                    }

                    for (int r = 0; r < resources.Length; r++)
                    {
                        ItemDrop resource = resources[r]?.m_resItem;
                        if (resource == null)
                        {
                            continue;
                        }

                        string resourceName = LootMappings.Clean(resource.name);
                        if (string.IsNullOrEmpty(resourceName) || resourceName == name)
                        {
                            continue;
                        }

                        int resourceTier = ComputeTier(resourceName);
                        if (resourceTier > tier)
                        {
                            tier = resourceTier;
                        }
                    }
                }
            }

            Visiting.Remove(name);
            TierCache[name] = tier;
            return tier;
        }
    }
}
