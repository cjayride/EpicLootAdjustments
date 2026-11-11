using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Cjayride.EpicLootAdjustments
{
    // The CJAYCRAFT jackpot rule. Gear above a creature's biome is normally removed.
    // A creature that is a boss, or that has enough stars, keeps the over-tier item
    // on a rare roll, up to a bounded number of biome tiers ahead.
    //
    // Stars are read from the game's own creature level data (the game's
    // GetStarLevel when present, otherwise level above base tier, which is how
    // Creature Level & Control tracks stars in this pack). No mod DLL is referenced.
    static class LuckyDrops
    {
        static ConfigEntry<bool> Enabled;
        static ConfigEntry<float> Chance;
        static ConfigEntry<int> MinStars;
        static ConfigEntry<int> TierStep;
        static ConfigEntry<bool> RequireZoneBoss;

        // Global-key boss progression per gear tier, the same boss list and
        // "defeated_" global keys the game uses (ZoneSystem.GetGlobalKey), as
        // tracked by Creature Level & Control: eikthyr, gdking, bonemass, dragon,
        // goblinking, queen, fader, frozenking.
        static readonly Dictionary<int, string> TierBossKeys = new Dictionary<int, string>
        {
            { LootMappings.MeadowsTier, "defeated_eikthyr" },
            { LootMappings.BlackForestTier, "defeated_gdking" },
            { LootMappings.SwampTier, "defeated_bonemass" },
            { LootMappings.MountainTier, "defeated_dragon" },
            { LootMappings.PlainsTier, "defeated_goblinking" },
            { LootMappings.MistlandsTier, "defeated_queen" },
            { LootMappings.AshlandsTier, "defeated_fader" },
            { LootMappings.DeepNorthTier, "defeated_frozenking" }
        };

        static MethodInfo _getStarLevel;
        static MethodInfo _getTier;
        static bool _starsResolved;

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind(
                "Lucky Drops",
                "Enabled",
                true,
                "Rare jackpot drops. A boss, or a creature with enough stars, keeps an over-tier gear drop on a rare roll instead of the zone cap removing it.");
            Chance = config.Bind(
                "Lucky Drops",
                "Chance",
                2f,
                "Percent chance that an over-tier gear drop survives the cap on an eligible creature. 2 makes finding one feel like luck. Applies per dropped gear item.");
            MinStars = config.Bind(
                "Lucky Drops",
                "MinStars",
                5,
                "Stars a non-boss creature must have to be eligible (stars = creature level above its base tier, as Creature Level & Control sets them). 5 keeps every plain Meadows and Black Forest animal capped; bosses are always eligible. Set 0 to allow every creature.");
            TierStep = config.Bind(
                "Lucky Drops",
                "TierStep",
                1,
                "How many biome tiers above its own zone the jackpot item may be. 1 means a Black Forest creature can jackpot Swamp iron, never Mountain silver.");
            RequireZoneBoss = config.Bind(
                "Lucky Drops",
                "RequireZoneBoss",
                true,
                "The jackpot item's own zone boss must be defeated first: jackpot iron needs Bonemass dead, jackpot silver needs Moder dead. Progression is checked with the game's world-global boss keys (the same ones raids and Creature Level & Control read).");
        }

        // Keeps (returns true for) an over-tier gear drop when the jackpot rules pass.
        // Called only for items the zone cap would otherwise remove.
        public static bool TryAllow(Character creature, string creatureName, int itemTier, int zoneTier, string itemName)
        {
            if (Enabled == null || !Enabled.Value || creature == null || zoneTier <= 0)
            {
                return false;
            }

            int step = TierStep.Value;
            if (itemTier > zoneTier + step)
            {
                return false;
            }

            bool boss = false;
            try
            {
                boss = creature.IsBoss();
            }
            catch (Exception)
            {
                boss = false;
            }

            int stars = StarsOf(creature);
            if (!boss && stars < MinStars.Value)
            {
                return false;
            }

            // Progression gate: the item's own zone boss must be defeated, so the
            // jackpot can never hand out gear the world has not unlocked yet.
            if (RequireZoneBoss.Value && !BossDefeated(itemTier))
            {
                return false;
            }

            float chance = Chance.Value;
            if (chance <= 0f || UnityEngine.Random.value * 100f >= chance)
            {
                return false;
            }

            if (Plugin.LogBlockedDrops == null || Plugin.LogBlockedDrops.Value)
            {
                Plugin.Log.LogInfo("LUCKY: " + creatureName + " (tier " + zoneTier
                    + (boss ? ", boss" : ", " + stars + " star" + (stars == 1 ? "" : "s")) + ") kept "
                    + itemName + " (tier " + itemTier + ", cap " + (zoneTier + step) + ") at " + chance + "%.");
            }

            return true;
        }

        // World-global boss progression, read the way the game stores it:
        // ZoneSystem.GetGlobalKey("defeated_bonemass") and friends.
        static bool BossDefeated(int itemTier)
        {
            if (itemTier <= LootMappings.MeadowsTier)
            {
                return true;
            }

            if (!TierBossKeys.TryGetValue(itemTier, out string key))
            {
                return false;
            }

            try
            {
                return ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(key);
            }
            catch (Exception)
            {
                return false;
            }
        }

        static int StarsOf(Character creature)
        {
            ResolveStarAccess();
            try
            {
                if (_getStarLevel != null)
                {
                    object stars = _getStarLevel.Invoke(creature, null);
                    if (stars is int value)
                    {
                        return value;
                    }
                }

                int level = creature.GetLevel();
                if (_getTier != null)
                {
                    object tier = _getTier.Invoke(creature, null);
                    if (tier is int baseTier)
                    {
                        return Math.Max(0, level - baseTier);
                    }
                }

                // Without the game's tier data, level - 1 is not a safe star count for
                // high-base-tier creatures (a level-6 Troll would read as 5 stars), so
                // report 0 and let only bosses win.
                return 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        static void ResolveStarAccess()
        {
            if (_starsResolved)
            {
                return;
            }

            _starsResolved = true;
            _getStarLevel = AccessTools.Method(typeof(Character), "GetStarLevel");
            _getTier = AccessTools.Method(typeof(Character), "GetTier");
            if (_getStarLevel == null && _getTier == null)
            {
                Plugin.Log.LogInfo("No star-level lookup is available in this game build; only bosses can win jackpot drops.");
            }
        }
    }
}
