using System;
using System.Collections.Generic;

namespace Cjayride.EpicLootAdjustments
{
    public static class LootMappings
    {
        // Meadows → Black Forest → Swamp → Mountain/Ocean → Plains → Mistlands → Ashlands → Deep North.
        // Ocean shares Mountain's tier. Deep North is the last biome, so it is the highest.
        public const int MeadowsTier = 1;
        public const int BlackForestTier = 2;
        public const int SwampTier = 3;
        public const int MountainTier = 4;
        public const int PlainsTier = 5;
        public const int MistlandsTier = 6;
        public const int AshlandsTier = 7;
        public const int DeepNorthTier = 8;

        public static readonly Dictionary<string, int> ZoneTiers = new Dictionary<string, int>
        {
            { "Meadows", MeadowsTier },
            { "Black Forest", BlackForestTier },
            { "Swamp", SwampTier },
            { "Mountain", MountainTier },
            { "Ocean", MountainTier },
            { "Plains", PlainsTier },
            { "Mistlands", MistlandsTier },
            { "Ashlands", AshlandsTier },
            { "Deep North", DeepNorthTier }
        };

        public static readonly Dictionary<string, string> CreatureZones = new Dictionary<string, string>();
        public static readonly Dictionary<string, int> Materials = new Dictionary<string, int>();

        static readonly List<KeyValuePair<string, int>> Tokens = new List<KeyValuePair<string, int>>();

        static LootMappings()
        {
            AddMaterials(MeadowsTier, "Wood", "Stone", "Flint", "Resin", "LeatherScraps", "DeerHide", "HardAntler", "Coal", "Feathers");
            AddMaterials(BlackForestTier,
                "Copper", "CopperOre", "Tin", "TinOre", "Bronze", "BronzeNails", "RoundLog", "FineWood", "CoreWood",
                "TrollHide", "BjornHide", "BjornPaw", "GreydwarfEye", "BoneFragments", "SurtlingCore");
            AddMaterials(SwampTier,
                "Iron", "IronScrap", "IronOre", "IronNails", "Chain", "ElderBark", "Guck", "Ooze", "Root",
                "Bloodbag", "Entrails", "WitheredBone", "Turnip", "WrithanRoots",
                "Sausages", "Sausage", "TurnipStew", "BlackSoup");
            // Ocean is the same step as Mountain, so serpent materials are not swamp loot.
            AddMaterials(MountainTier,
                "Silver", "SilverOre", "Obsidian", "WolfHairBundle", "WolfPelt", "WolfClaw", "WolfFang", "FreezeGland",
                "Chitin", "SerpentScale", "SerpentMeat");
            AddMaterials(PlainsTier,
                "Needle", "BlackMetal", "BlackMetalScrap", "LinenThread", "Linen", "LoxPelt", "Barley", "BarleyFlour",
                "Flax", "Tar", "Cloudberry", "UndeadBjornRibcage");
            AddMaterials(MistlandsTier,
                "YggdrasilWood", "BlackMarble", "Eitr", "RefinedEitr", "BlackCore", "Mandible", "Carapace", "ScaleHide",
                "Sap", "Softtissue", "Magecap", "RoyalJelly", "Bilebag", "JuteRed", "Wisp", "YagluthDrop");
            AddMaterials(AshlandsTier,
                "Flametal", "FlametalNew", "FlametalOre", "FlametalOreNew", "Blackwood", "CharredBone", "MoltenCore",
                "GemstoneBlue", "GemstoneGreen", "GemstoneRed", "CelestialFeather", "AskHide", "AsksvinHide",
                "AsksvinBladder", "Grausten", "CeramicPlate", "ProustitePowder", "MorgenHeart", "MorgenSinew",
                "Fiddlehead", "VoltureEgg", "Sulfur");
            AddMaterials(DeepNorthTier,
                "Gold", "GoldOre", "Frostwood", "FrostCore", "Ice", "FrozenFuel", "MooseHide", "MooseSinew", "MooseMeat",
                "SealHide", "SealBlubber", "ElakingHairBundle", "NornThread", "Leatherstraps", "MoleClaws", "BarkaBranch",
                "OozeMork", "OrbFrostFire", "OrbThunderBlood", "AncientCoin", "AncientGemstoneBlack", "AncientGemstoneGreen",
                "AncientGemstoneOrange", "AncientGemstonePurple", "MemorialCoal", "HatefulBlood", "CrownJewel", "FrozenKingDrop",
                "Lingonberry", "GlowWorm", "Kale", "KaleSeeds", "Oat", "OatSeeds", "Poteitr", "PoteitrSeeds", "FirConeFrost");

            AddCreatures("Meadows",
                "Boar", "Boar_piggy", "Chicken", "Deer", "Greyling", "Hen", "MeadowsSpider", "Neck", "Rainbow_Butterfly",
                "Eikthyr", "Golden_Greydwarf_Miniboss", "GoldenEikthyr");
            AddCreatures("Black Forest",
                "Bjorn", "BlackForestSpider", "ForestSpider", "Green_Butterfly", "Greydwarf", "Greydwarf_Elite",
                "Greydwarf_Purple", "Greydwarf_Purple_Shroom", "Greydwarf_Shaman", "JumpingSpider", "LandMolluscan",
                "Molluscan", "NormalSkeletonWarrior", "Skeleton", "SkeletonWarrior", "Skeleton_NoArcher", "TreeSpider", "Troll",
                "gd_king", "Skeleton_Hildir", "Golden_Troll_Miniboss", "GoldenTheElder");
            AddCreatures("Swamp",
                "Abomination", "Blob", "BlobElite", "BogWitchKvastur", "Draugr", "DraugrShip", "Draugr_Elite", "Draugr_Ranged",
                "Ghost", "GreenSpider", "Leech", "Leech_Calf", "Leech_cave", "LurkingSpiderSwamp", "ML_DraugrShip",
                "RancidRemains", "Skeleton_Poison", "Surtling", "Wraith", "Writhan",
                "Bonemass", "Golden_Wraith_Miniboss", "DraugrMariner", "GoldenBonemass");
            AddCreatures("Mountain",
                "Bat", "Drake", "Fenring", "Fenring_Cultist", "GhostWarrior", "Hatchling", "SnowSpider", "StoneGolem",
                "Ulv", "Wolf", "Wolf_cub",
                "Dragon", "Ulv_Hildir", "FrostwingFae", "GoldenModer");
            AddCreatures("Plains",
                "BlobTar", "BrownSpider", "Deathsquito", "DwarfGoblin", "DwarfGoblinLoot", "DwarfGoblinShaman", "FulingShip",
                "Goblin", "GoblinArcher", "GoblinBase", "GoblinBrute", "GoblinBruteBros", "GoblinBruteBros_nochest",
                "GoblinLoot", "GoblinLox", "GoblinShaman", "GoblinShamanNew", "Goblin_Gem", "Lox", "LoxCalf", "Unbjorn",
                "GoblinKing", "GoblinBrute_Hildir", "GoldenYagluth");
            AddCreatures("Mistlands",
                "Dverger", "DvergerMage", "DvergerMageFire", "DvergerMageIce", "DvergerMageSupport", "Hare",
                "PulpHexahedric", "PulpShapeless", "PulpTiny", "Seeker", "SeekerBrood", "SeekerBrute", "Silkworm_Butterfly",
                "Spiderling", "Tick",
                "SeekerQueen", "SvartalfarQueen");
            AddCreatures("Ashlands",
                "AshHatchling", "AshHuldra", "AshHuldraQueen", "Asksvin", "Asksvin_hatchling", "BlobLava", "Bonemaw",
                "BonemawSerpent", "ChaosSkeletonWarrior", "CharredMelee_Reto", "Charred_Archer", "Charred_Archer_Fader",
                "Charred_Mage", "Charred_Melee", "Charred_Melee_Dyrnwyn", "Charred_Melee_Fader", "Charred_Twitcher",
                "Charred_Twitcher_Summoned", "DamnedOne_TW", "DvergerAshlands", "FallenValkyrie", "FireGolem",
                "FireSkeletonWarrior", "InfernalMinotaur", "ML_AshHatchling", "ML_AshHuldra", "MagmaSkarab", "Morgen",
                "Morgen_NonSleeping", "ObsidianGolem", "SkeletonWarriorFire", "Volture",
                "Fader", "HelDemon");
            AddCreatures("Deep North",
                "Barka", "BlobFrost", "CrystalhideUrsar", "CrystalhideUrsar_Cub", "Elaking", "EyelessOne", "FallenWarrior",
                "FrigidSpider", "FrostSpider", "Frostling", "Gammeltroll", "Hexen", "IceGolem", "IceSkeletonWarrior",
                "IceSkeletonWarriorT6", "Krigen", "Moose", "SkeletonWarriorIce",
                "KallFimbulbringer", "Jotunn");
            AddCreatures("Ocean",
                "CommonJellyfish", "DeepSeaMolluscan", "DeepSeaSerpent", "DeepSea_Serpent", "Kraken", "KrakenLD",
                "ML_DeepSeaMolluscan", "ML_JellyFish0", "Seal", "Serpent");

            AddTokens(DeepNorthTier, "DeepNorth", "Frostwood", "FrostCore", "NornThread");
            AddTokens(AshlandsTier, "Flametal", "Blackwood", "CharredBone", "MoltenCore", "Asksvin", "Grausten", "Dyrnwyn", "Niedhogg", "Berzerkr");
            AddTokens(MistlandsTier, "Yggdrasil", "BlackMarble", "Carapace", "ScaleHide", "Mistwalker", "JotunBane", "Himminafl", "HimminAfl");
            AddTokens(PlainsTier, "BlackMetal", "Blackmetal", "LinenThread");
            AddTokens(MountainTier, "SerpentScale", "FreezeGland", "WolfPelt", "Obsidian");
            AddTokens(SwampTier, "ElderBark", "WitheredBone", "IronScrap", "Sausage");
            AddTokens(BlackForestTier, "FineWood", "CoreWood", "TrollHide", "Bronze");
            AddTokens(MountainTier, "Silver");
            AddTokens(PlainsTier, "BlackMetal", "Blackmetal");
            AddTokens(AshlandsTier, "Flametal");
            AddTokens(DeepNorthTier, "Gold");

            foreach (KeyValuePair<string, string> creature in CreatureZones)
            {
                if (ZoneTiers.TryGetValue(creature.Value, out int creatureTier))
                {
                    Materials[creature.Key] = creatureTier;
                    Materials["Trophy" + creature.Key] = creatureTier;
                }
            }
        }

        public static bool TryGetCreature(string prefabName, out string creature, out string zone, out int tier)
        {
            creature = Clean(prefabName);
            zone = null;
            tier = 0;
            if (string.IsNullOrEmpty(creature))
            {
                return false;
            }

            if (!CreatureZones.TryGetValue(creature, out zone))
            {
                return false;
            }

            return ZoneTiers.TryGetValue(zone, out tier);
        }

        public static bool IsShardStone(string prefabName)
        {
            return !string.IsNullOrEmpty(prefabName)
                && prefabName.IndexOf("ShardStone", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static int MaterialTier(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                return 0;
            }

            int shard = ShardStoneTier(prefabName);
            if (shard > 0)
            {
                return shard;
            }

            int tier = 0;
            if (Materials.TryGetValue(prefabName, out int exact) && exact > tier)
            {
                tier = exact;
            }

            int segment = SegmentTier(prefabName);
            if (segment > tier)
            {
                tier = segment;
            }

            int token = TokenTier(prefabName);
            if (token > tier)
            {
                tier = token;
            }

            return tier;
        }

        public static string[] FallbackMaterials(int tier)
        {
            switch (tier)
            {
                case MeadowsTier: return new[] { "Wood", "Flint" };
                case BlackForestTier: return new[] { "Bronze", "FineWood" };
                case SwampTier: return new[] { "Iron", "ElderBark" };
                case MountainTier: return new[] { "Silver", "Obsidian" };
                case PlainsTier: return new[] { "BlackMetal", "Needle" };
                case MistlandsTier: return new[] { "Eitr", "Carapace", "YggdrasilWood" };
                case AshlandsTier: return new[] { "FlametalNew", "Flametal", "Blackwood" };
                case DeepNorthTier: return new[] { "Gold", "GoldOre", "Frostwood" };
                default: return new[] { "Wood" };
            }
        }

        public static string[] FallbackFood(int tier)
        {
            switch (tier)
            {
                case MeadowsTier: return new[] { "CookedMeat", "GrilledNeckTail", "Honey", "Raspberry" };
                case BlackForestTier: return new[] { "CarrotSoup", "MincedMeatSauce", "CookedDeerMeat" };
                case SwampTier: return new[] { "Sausages", "TurnipStew", "BlackSoup" };
                case MountainTier: return new[] { "WolfMeat", "OnionSoup", "CookedWolfMeat" };
                case PlainsTier: return new[] { "LoxPie", "BloodPudding", "Bread" };
                case MistlandsTier: return new[] { "MisthareSupreme", "MushroomOmelette", "Salad" };
                case AshlandsTier: return new[] { "CookedAsksvinMeat", "FiddleheadFern", "PiquantPie" };
                case DeepNorthTier: return new[] { "CookedMooseMeat", "Lingonberry" };
                default: return new[] { "CookedMeat" };
            }
        }

        public static string[] FallbackTrophies(int tier)
        {
            switch (tier)
            {
                case MeadowsTier: return new[] { "TrophyBoar", "TrophyDeer", "TrophyNeck", "TrophyEikthyr" };
                case BlackForestTier: return new[] { "TrophyGreydwarf", "TrophySkeleton", "TrophyTroll", "TrophyTheElder", "Trophygd_king" };
                case SwampTier: return new[] { "TrophyDraugr", "TrophyLeech", "TrophyBlob", "TrophyBonemass" };
                case MountainTier: return new[] { "TrophyWolf", "TrophyHatchling", "TrophySGolem", "TrophyDragonQueen" };
                case PlainsTier: return new[] { "TrophyGoblin", "TrophyLox", "TrophyGrowth", "TrophyGoblinKing" };
                case MistlandsTier: return new[] { "TrophySeeker", "TrophySeekerBrute", "TrophyDvergr", "TrophySeekerQueen" };
                case AshlandsTier: return new[] { "TrophyCharredMelee", "TrophyAsksvin", "TrophyFader" };
                case DeepNorthTier: return new[] { "TrophyMoose", "TrophySeal", "TrophyElaking" };
                default: return new[] { "TrophyBoar" };
            }
        }

        public static string Clean(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            int clone = name.IndexOf("(Clone)", StringComparison.Ordinal);
            if (clone >= 0)
            {
                name = name.Remove(clone);
            }

            return name.Trim();
        }

        static void AddMaterials(int tier, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Materials[names[i]] = tier;
            }
        }

        static void AddCreatures(string zone, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (CreatureZones.ContainsKey(names[i]))
                {
                    throw new InvalidOperationException("Duplicate creature mapping: " + names[i]);
                }

                CreatureZones.Add(names[i], zone);
            }
        }

        static int ShardStoneTier(string name)
        {
            if (!IsShardStone(name))
            {
                return 0;
            }

            if (name.IndexOf("_Ancient_", StringComparison.OrdinalIgnoreCase) >= 0
                || name.EndsWith("_Ancient_ShardStone", StringComparison.OrdinalIgnoreCase)
                || name.IndexOf("Ancient_ShardStone", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return DeepNorthTier;
            }

            if (name.IndexOf("_Mythic_", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Mythic_ShardStone", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return MistlandsTier;
            }

            if (name.IndexOf("_Legendary_", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Legendary_ShardStone", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return MountainTier;
            }

            if (name.IndexOf("_Epic_", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Epic_ShardStone", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return SwampTier;
            }

            if (name.IndexOf("_Rare_", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Rare_ShardStone", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return BlackForestTier;
            }

            if (name.IndexOf("_Magic_", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Magic_ShardStone", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return MeadowsTier;
            }

            return DeepNorthTier;
        }

        static int SegmentTier(string name)
        {
            int tier = 0;
            int start = 0;
            for (int i = 0; i <= name.Length; i++)
            {
                bool end = i == name.Length;
                bool splitCase = !end && i > start && char.IsUpper(name[i]) && char.IsLower(name[i - 1]);
                bool splitOther = end || !char.IsLetter(name[i]);
                if (!splitCase && !splitOther)
                {
                    continue;
                }

                if (i > start && Materials.TryGetValue(name.Substring(start, i - start), out int part) && part > tier)
                {
                    tier = part;
                }

                start = splitCase ? i : i + 1;
            }

            return tier;
        }

        static void AddTokens(int tier, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Tokens.Add(new KeyValuePair<string, int>(names[i], tier));
            }
        }

        static int TokenTier(string name)
        {
            int tier = 0;
            for (int i = 0; i < Tokens.Count; i++)
            {
                if (Tokens[i].Value > tier && name.IndexOf(Tokens[i].Key, StringComparison.Ordinal) >= 0)
                {
                    tier = Tokens[i].Value;
                }
            }

            return tier;
        }
    }
}
