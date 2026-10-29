using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;

namespace Cjayride.EpicLootAdjustments
{
    public static class DropThatAllowList
    {
        static readonly Dictionary<string, HashSet<string>> Allowed =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        static ConfigEntry<string> FileNames;
        static bool _loaded;

        public static void Bind(ConfigFile config)
        {
            FileNames = config.Bind(
                "Exceptions",
                "DropThatAllowFiles",
                "drop_that.character_drop.BossBonusTools.cfg",
                "Comma-separated Drop That character-drop files in BepInEx/config. Items listed on a creature in these files are not downgraded, even if they are above that creature's zone.");
        }

        public static bool IsAllowed(string creature, string item)
        {
            EnsureLoaded();
            creature = LootMappings.Clean(creature);
            item = LootMappings.Clean(item);
            if (string.IsNullOrEmpty(creature) || string.IsNullOrEmpty(item))
            {
                return false;
            }

            return Allowed.TryGetValue(creature, out HashSet<string> items) && items.Contains(item);
        }

        static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            Allowed.Clear();
            string configDir = Paths.ConfigPath;
            if (string.IsNullOrEmpty(configDir) || !Directory.Exists(configDir))
            {
                return;
            }

            string list = FileNames != null ? FileNames.Value : "drop_that.character_drop.BossBonusTools.cfg";
            string[] names = list.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            int pairs = 0;
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i].Trim();
                if (name.Length == 0)
                {
                    continue;
                }

                string path = Path.Combine(configDir, name);
                if (!File.Exists(path))
                {
                    Plugin.Log.LogInfo("Drop That allow-list file not found: " + path);
                    continue;
                }

                pairs += LoadFile(path);
            }

            Plugin.Log.LogInfo("Drop That allow-list loaded " + pairs + " creature/item pairs.");
        }

        static int LoadFile(string path)
        {
            int added = 0;
            string creature = null;
            string prefab = null;
            bool enabled = true;
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                if (line[0] == '[')
                {
                    added += Commit(creature, prefab, enabled);
                    creature = ParseCreature(line);
                    prefab = null;
                    enabled = true;
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();
                if (key.Equals("PrefabName", StringComparison.OrdinalIgnoreCase))
                {
                    prefab = value;
                }
                else if (key.Equals("EnableConfig", StringComparison.OrdinalIgnoreCase))
                {
                    enabled = !value.Equals("false", StringComparison.OrdinalIgnoreCase);
                }
            }

            added += Commit(creature, prefab, enabled);
            return added;
        }

        static int Commit(string creature, string prefab, bool enabled)
        {
            if (!enabled || string.IsNullOrEmpty(creature) || string.IsNullOrEmpty(prefab))
            {
                return 0;
            }

            if (!Allowed.TryGetValue(creature, out HashSet<string> items))
            {
                items = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                Allowed.Add(creature, items);
            }

            return items.Add(prefab) ? 1 : 0;
        }

        static string ParseCreature(string section)
        {
            int start = section.IndexOf('[') + 1;
            int end = section.IndexOf(']');
            if (start <= 0 || end <= start)
            {
                return null;
            }

            string name = section.Substring(start, end - start);
            int dot = name.IndexOf('.');
            if (dot > 0)
            {
                name = name.Substring(0, dot);
            }

            return name.Trim();
        }
    }
}
