## 1.5.5
- **FIX:** This patch no longer rewrites drops into other items (no wood/flint/shardstone substitutions). Materials, shardstones, food, and trophies are left as rolled. Only over-tier weapons, armor, shields, tools, and ammo are removed.

## 1.5.4
- **FIX:** Over-tier shardstones are removed instead of turned into Magic stones. This patch never adds a shardstone that the creature did not already roll at a legal rarity.

## 1.5.3
- **FIX:** Shardstones are no longer treated as Meadows `Stone`. `ShardStone` names were split into `Stone`, so Ancient and boss-colored stones counted as tier 1 and could be swapped onto Greylings and Bjorn raids. Rarity now maps Magic → Meadows through Ancient → Deep North, and shardstones are never used as generic material replacements.

## 1.5.2
- Version bump.

## 1.5.1
- **FIX:** Stopped a severe FPS drop caused by looking up Epic Loot's Quick Config type every `Menu.Update` frame and Harmony-logging when that type is missing in Epic Loot **0.15.4**.
- Config-button hide now caches types once at startup, skips all menu work when Quick Config is absent, and only re-hides on pause-menu `Show`.

## 1.5.0
- **RENAME:** Mod is now **EpicLootAdjustments** (`com.cjayride.epiclootadjustments`, `Cjayride.EpicLootAdjustments.dll`). Remove any old `Cjayride.CLCCLootPatch.dll` from `BepInEx/plugins`.
- **NEW:** GitHub repo, README, Thunderstore `manifest.json`, and package icon.
- Config file is now `BepInEx/config/com.cjayride.epiclootadjustments.cfg` (old `com.cjayride.clcllootpatch.cfg` is not read).

## 1.4.1
- **FIX:** Set-ability audio/VFX patches tolerate Epic Loot **0.15** API renames; optional hooks use reflection so missing methods no longer spam Harmony warnings on older builds.

## 1.4.0
- **NEW:** **Set Abilities** config — lower legendary set ability volume (default **0.35**), optional smoke/particle/visual reduction (Ragnar frost cone, Berserker, Bulwark, Undying, and related set FX).
- **NEW:** **Interface / HideEpicLootConfigButton** (default **true**) — hides Epic Loot’s pause/main-menu Mod Config button; BepInEx cfg files still apply.

## 1.3.0
- **NEW:** **Exceptions / DropThatAllowFiles** — comma-separated Drop That character-drop configs (default `drop_that.character_drop.BossBonusTools.cfg`). Creature/item pairs listed there bypass zone downgrades.

## 1.2.0
- **FIX:** Food and trophies are tier-checked like gear (e.g. early bosses no longer drop iron-tier food, draugr trophies, or iron axes when those imply a higher biome).
- **FIX:** CamelCase prefab names infer material tier when not in the recipe index (e.g. `AxeIron` → swamp tier). Fish stays unzoned so Meadows can still drop it.

## 1.1.0
- **NEW:** Epic Loot integration — patches `LootRoller.SpawnNormalItem` and `GatedItemTypeHelper.GetGatedItemNameFromItemOrType` so magic rolls respect the same zone cap as vanilla drops.

## 1.0.0
- **NEW:** Zone loot filter for CJAYCRAFT progression — mapped creatures (~178 entries) cannot drop weapons, armor, shields, tools, or ammo whose recipe materials exceed the creature’s home biome tier. Over-tier drops are swapped to the same item kind at the highest legal tier for that creature.
- Biome order: Meadows → Black Forest → Swamp → Mountain/Ocean (same tier) → Plains → Mistlands → Ashlands → Deep North.
- Ore, hides, and similar non-gear materials are not replaced by slot logic; Epic Loot still applies enchant rolls after the base item is chosen.
- Requires BepInEx; Epic Loot is a soft dependency (loot filter and UI hooks need it installed).
