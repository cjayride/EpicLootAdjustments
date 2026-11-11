using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Cjayride.EpicLootAdjustments
{
    // Resolves the biome a drop happened in, so over-tier gear is capped even when
    // the dropping object is not in the creature map: chests, sleeping variants,
    // Hildir "nochest" variants, added-mod creatures, and any future prefab.
    //
    // ZoneSystem.GetGroundData is resolved by reflection once. Its five parameters
    // are all by-ref (Vector3, Vector3, Biome, BiomeArea, Heightmap) and the biome
    // enum's identity is not needed: only its printed name, which is stable.
    static class ZoneContext
    {
        // Keyed by the biome enum name, lowercased with spaces/underscores removed,
        // so "Black Forest", "BlackForest" and "black_forest" all resolve.
        static readonly Dictionary<string, int> BiomeTiers = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            { "meadows", LootMappings.MeadowsTier },
            { "blackforest", LootMappings.BlackForestTier },
            { "forest", LootMappings.BlackForestTier },
            { "swamp", LootMappings.SwampTier },
            { "mountain", LootMappings.MountainTier },
            { "ocean", LootMappings.MountainTier },
            { "plains", LootMappings.PlainsTier },
            { "heath", LootMappings.PlainsTier },
            { "mistlands", LootMappings.MistlandsTier },
            { "ashlands", LootMappings.AshlandsTier },
            { "deepnorth", LootMappings.DeepNorthTier }
        };

        static readonly HashSet<string> WarnedBiomes = new HashSet<string>(StringComparer.Ordinal);
        static readonly object[] Args = new object[5];

        static MethodInfo _getGroundData;
        static bool _resolved;
        // Boxed defaults for the by-ref Biome / BiomeArea / Heightmap out parameters.
        static object[] _defaults = Array.Empty<object>();

        // One-entry cache: every item from a single kill shares the drop point, and
        // the biome query walks the heightmap.
        static Vector3 _lastPosition;
        static int _lastTier = -1;
        static string _lastBiome;
        static bool _hasLast;

        public static void Resolve()
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;
            MethodInfo method = null;
            foreach (MethodInfo candidate in AccessTools.GetDeclaredMethods(typeof(ZoneSystem)))
            {
                if (candidate.Name != "GetGroundData")
                {
                    continue;
                }

                ParameterInfo[] parameters = candidate.GetParameters();
                if (parameters.Length == 5 && parameters[2].ParameterType.IsByRef && parameters[3].ParameterType.IsByRef)
                {
                    method = candidate;
                    break;
                }
            }

            if (method == null)
            {
                Plugin.Log.LogWarning("ZoneSystem.GetGroundData was not found; unknown creatures are not capped by biome.");
                return;
            }

            _getGroundData = method;
            ParameterInfo[] methodParameters = method.GetParameters();
            var defaults = new object[methodParameters.Length];
            for (int i = 0; i < methodParameters.Length; i++)
            {
                Type type = methodParameters[i].ParameterType;
                if (type.IsByRef)
                {
                    type = type.GetElementType();
                }

                // A by-ref value type (the Biome and BiomeArea enums) needs a boxed
                // zero value; reference types (Heightmap) accept null.
                if (type != null && type.IsValueType)
                {
                    defaults[i] = Activator.CreateInstance(type);
                }
            }

            _defaults = defaults;
        }

        // Biome tier at the drop position. Tier is 0 when the biome is unknown, in
        // which case the caller must not cap anything.
        public static bool TryZone(Vector3 position, out int tier, out string biome)
        {
            if (_hasLast && position == _lastPosition)
            {
                tier = _lastTier;
                biome = _lastBiome;
                return tier > 0;
            }

            biome = ReadBiome(position);
            tier = 0;
            if (!string.IsNullOrEmpty(biome))
            {
                string key = Normalize(biome);
                if (!BiomeTiers.TryGetValue(key, out tier))
                {
                    tier = 0;
                    if (WarnedBiomes.Add(biome) && WarnedBiomes.Count <= 64)
                    {
                        Plugin.Log.LogInfo("Biome '" + biome + "' has no CJAYCRAFT zone tier; gear is not capped there.");
                    }
                }
            }

            _lastPosition = position;
            _lastTier = tier;
            _lastBiome = biome;
            _hasLast = true;
            return tier > 0;
        }

        static string ReadBiome(Vector3 position)
        {
            Resolve();
            ZoneSystem zone = ZoneSystem.instance;
            if (_getGroundData == null || zone == null)
            {
                return "";
            }

            try
            {
                Args[0] = position;
                Args[1] = Vector3.zero;
                Args[2] = null;
                Args[3] = null;
                Args[4] = null;
                for (int i = 2; i < Args.Length && i < _defaults.Length; i++)
                {
                    Args[i] = _defaults[i];
                }

                _getGroundData.Invoke(zone, Args);
                object biome = Args[2];
                return biome == null ? "" : biome.ToString();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Biome lookup failed: " + ex.Message);
                return "";
            }
        }

        static string Normalize(string name)
        {
            char[] buffer = new char[name.Length];
            int count = 0;
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (c == ' ' || c == '_' || c == '-' || c == '.')
                {
                    continue;
                }

                buffer[count++] = char.ToLowerInvariant(c);
            }

            return new string(buffer, 0, count);
        }

        public static void Invalidate()
        {
            _hasLast = false;
            _lastTier = -1;
            _lastBiome = null;
        }
    }
}
