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
            public Skills.SkillType Skill;
        }

        static readonly Dictionary<string, ItemRecord> ByName = new Dictionary<string, ItemRecord>();
        static readonly Dictionary<(ItemDrop.ItemData.ItemType, Skills.SkillType), List<ItemRecord>> ByKind =
            new Dictionary<(ItemDrop.ItemData.ItemType, Skills.SkillType), List<ItemRecord>>();
        static readonly Dictionary<ItemDrop.ItemData.ItemType, List<ItemRecord>> ByType =
            new Dictionary<ItemDrop.ItemData.ItemType, List<ItemRecord>>();
        static readonly Dictionary<string, List<Recipe>> Recipes = new Dictionary<string, List<Recipe>>();
        static readonly Dictionary<string, int> TierCache = new Dictionary<string, int>();
        static readonly HashSet<string> Visiting = new HashSet<string>();
        static bool _ready;

        public static void Invalidate()
        {
            _ready = false;
        }

        public static bool NeedsReplacement(string itemName, int maxTier, out string replacement)
        {
            replacement = null;
            itemName = LootMappings.Clean(itemName);
            if (string.IsNullOrEmpty(itemName))
            {
                return false;
            }

            EnsureBuilt();
            ItemRecord record = Find(itemName);
            int tier = record != null ? record.Tier : LootMappings.MaterialTier(itemName);
            if (tier <= maxTier)
            {
                return false;
            }

            replacement = record != null
                ? Pick(record, maxTier) ?? FallbackFor(record.Type, maxTier)
                : FallbackMaterial(maxTier);
            if (replacement == itemName)
            {
                replacement = null;
            }

            return true;
        }

        static ItemRecord Find(string itemName)
        {
            if (ByName.TryGetValue(itemName, out ItemRecord record))
            {
                return record;
            }

            GameObject prefab = ObjectDB.instance?.GetItemPrefab(itemName);
            if (prefab == null)
            {
                return null;
            }

            return AddPrefab(prefab);
        }

        static bool EnsureBuilt()
        {
            if (_ready)
            {
                return true;
            }

            ObjectDB db = ObjectDB.instance;
            if (db == null || db.m_items == null || db.m_items.Count == 0)
            {
                return false;
            }

            try
            {
                Build(db);
                _ready = true;
                Plugin.Log.LogInfo("Indexed " + ByName.Count + " items for zone checks.");
                return true;
            }
            catch (Exception ex)
            {
                _ready = true;
                Plugin.Log.LogError("Failed to index item tiers: " + ex);
                return false;
            }
        }

        static void Build(ObjectDB db)
        {
            ByName.Clear();
            ByKind.Clear();
            ByType.Clear();
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
                Skill = shared.m_skillType,
                Tier = ComputeTier(name)
            };
            ByName.Add(name, record);
            if (record.Tier <= 0)
            {
                return record;
            }

            var kind = (record.Type, record.Skill);
            if (!ByKind.TryGetValue(kind, out List<ItemRecord> kindList))
            {
                kindList = new List<ItemRecord>();
                ByKind.Add(kind, kindList);
            }

            kindList.Add(record);
            if (!ByType.TryGetValue(record.Type, out List<ItemRecord> typeList))
            {
                typeList = new List<ItemRecord>();
                ByType.Add(record.Type, typeList);
            }

            typeList.Add(record);
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

        static string Pick(ItemRecord source, int maxTier)
        {
            string best = PickFrom(ByKind.TryGetValue((source.Type, source.Skill), out List<ItemRecord> kind) ? kind : null, source.Name, maxTier);
            if (best != null)
            {
                return best;
            }

            return PickFrom(ByType.TryGetValue(source.Type, out List<ItemRecord> type) ? type : null, source.Name, maxTier);
        }

        static string PickFrom(List<ItemRecord> candidates, string original, int maxTier)
        {
            if (candidates == null)
            {
                return null;
            }

            int bestTier = 0;
            int count = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                ItemRecord candidate = candidates[i];
                if (candidate.Name == original || candidate.Tier <= 0 || candidate.Tier > maxTier)
                {
                    continue;
                }

                if (candidate.Tier > bestTier)
                {
                    bestTier = candidate.Tier;
                    count = 1;
                }
                else if (candidate.Tier == bestTier)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return null;
            }

            int choice = UnityEngine.Random.Range(0, count);
            for (int i = 0; i < candidates.Count; i++)
            {
                ItemRecord candidate = candidates[i];
                if (candidate.Name == original || candidate.Tier != bestTier || candidate.Tier > maxTier)
                {
                    continue;
                }

                if (choice == 0)
                {
                    return candidate.Name;
                }

                choice--;
            }

            return null;
        }

        static string FallbackFor(ItemDrop.ItemData.ItemType type, int maxTier)
        {
            if (type == ItemDrop.ItemData.ItemType.Consumable)
            {
                return FirstExisting(LootMappings.FallbackFood, maxTier) ?? FallbackMaterial(maxTier);
            }

            if (type == ItemDrop.ItemData.ItemType.Trophy)
            {
                return FirstExisting(LootMappings.FallbackTrophies, maxTier);
            }

            return FallbackMaterial(maxTier);
        }

        static string FallbackMaterial(int maxTier)
        {
            return FirstExisting(LootMappings.FallbackMaterials, maxTier);
        }

        static string FirstExisting(Func<int, string[]> listForTier, int maxTier)
        {
            for (int tier = maxTier; tier >= LootMappings.MeadowsTier; tier--)
            {
                string[] names = listForTier(tier);
                for (int i = 0; i < names.Length; i++)
                {
                    if (ObjectDB.instance?.GetItemPrefab(names[i]) != null)
                    {
                        return names[i];
                    }
                }
            }

            return null;
        }
    }
}
