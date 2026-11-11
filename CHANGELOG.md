## 1.5.15
- **NEW:** `Lucky Drops / RequireZoneBoss` (default **true**) — a jackpot item's own zone boss must be defeated first. Jackpot iron needs Bonemass dead, silver needs Moder, blackmetal needs Yagluth, and so on, checked against the game's world-global boss keys (the same `defeated_*` progression raids and Creature Level & Control read). This closes the last gap: a 5-star creature on a direct loot-table entry (like the pack's `Tier3Tools` → `PickaxeIron`) can no longer hand out gear the world has not unlocked yet. Set `false` for star-level-only gating.

## 1.5.14
- **NEW:** **Lucky Drops** — the jackpot rule. A **boss**, or a creature at **5+ stars**, keeps an over-tier gear drop **2%** of the time, capped at **one biome tier** above its zone. A 5-star Troll (Mountain tier 4) in a raid can luck into a silver sword; a plain 1–2 star Greydwarf, Deer, or Bjorn never can, and no creature can ever jackpot more than one biome ahead (Black Forest tops out at Swamp iron). The star level is the same one Creature Level & Control sets (level above base tier), read from the game's own data — no mod DLL is referenced. Settings: `Lucky Drops / Enabled`, `Chance` (%), `MinStars`, `TierStep`.

## 1.5.13
- **FIX (leak):** Creatures the mod has no biome entry for were **not filtered at all**. Sleeping variants (`Bjorn_sleeping`, `Troll_sleeping`), Hildir "nochest" variants, `Skeleton_Meadows`, treasure chests and added-mod creatures fell through an early return, so Epic Loot handed them its full boss-gated gear list — this is how an **Iron pickaxe** (Swamp, tier 3) came out of a plain creature in the Meadows / Black Forest. Any such drop is now capped at the tier of the **biome the drop physically happened in**, read from the world's ground data. `Zones / CapUnknownCreatures` (default `true`) controls it; set `false` to restore the old unfiltered behaviour for unknown names.
- **FIX (leak):** `_sleeping`, `_nochest`, `_NonSleeping`, `_hatchling`, `_calf`, `_cave` and similar spawn suffixes now inherit the base creature's biome, so a sleeping Black Forest bear is capped as Black Forest gear instead of being treated as an unknown object.
- **FIX (leak):** The Drop That exceptions file had **no ceiling**. `drop_that.character_drop.BossBonusTools.cfg` lists Eikthyr (Meadows) → `PickaxeIron` / `AxeIron` at 100%, and The Elder (Black Forest) → `PickaxeSilver_TW` / `AxeSilver_TW` at ~33%, and the mod let every one through. `Exceptions / BonusTierStep` (default **1**) now bounds the file to its own stated design — *"bonus tool drops one tier above the boss zone"* — so Bonemass (Swamp) → Silver and Moder (Mountain) → Blackmetal stay, while the 2-tier entries are blocked. The step applies to **tools only** (prefab names starting with `Axe` / `Pickaxe`); weapons, armour, shields and ammo are always capped at the creature's own biome, at any setting. Set `BonusTierStep = 0` for an absolute zone cap where the exceptions file grants nothing over-tier.

## 1.5.12
- **NEW:** `Set Abilities / BulwarkDuration` (default **3**) — sets how many seconds the Heimdall 3-piece ability **Bulwark** blocks *all* damage. Epic Loot ships this number inside its own asset bundle (the prefab's `m_ttl`, 5s) and applies the status effect with duration 0, so the per-rarity values in `magiceffects.json` are never read by the ability and no patch file can reach it. Set to `5` for the stock duration; the accepted range is 1–10.

## 1.5.11
- **FIX:** Socketing was still blocked when the shard's effect is in the same exclusive family as an existing enchant, not the same type. Example: Epic Light Blue Shardstone (`AddLightningDamage`) on a sledge that already rolled Physical Damage Dealt (`ModifyPhysicalDamage`). `AllowMatchingItemEffect` only bypasses ExclusiveSelf (same effect type). The override now also ignores `$mod_epicloot_socket_conflict` from `CanSocket` / `CanCoexist`. Duplicate same-color shards, unique/boss exclusivity, and item-type rules are unchanged.

## 1.5.10
- **FIX:** 1.5.9 forced Epic Loot's `CheckDuplicateEffect` to false. That method returns **true when socketing is allowed**, so every shard drop failed (including SeneaLHudLayout's shard window). The override now only turns on `AllowMatchingItemEffect`, which is the real "item already has this enchant" gate.

## 1.5.9
- **NEW:** `Shards / AllowAnyShardInSlot` (default **true**) — socket a shard even when the item already has a matching magic effect (for example +damage). Enchant rolls already allow multiple damage types on one item; this override stops Epic Loot from blocking the same combination via shards. Unique/boss exclusivity is unchanged. Turn off to restore Epic Loot's duplicate-effect socket rules.

## 1.5.8
- **FIX:** Rebuild the item-material index when ObjectDB recipes are added after world load (WackyDatabase / Chaos Armor). `T0ChaosPlateArmor` ("Fell Suit" as an Epic Loot name) is silver + obsidian mountain plate; it was indexed with no recipe yet, treated as biome-legal, and could drop from The Elder.
- **FIX:** Keep the killing creature in scope for nested Epic Loot rolls (`RollLootTableInternal` around `SpawnNormalItem`) so gated magic chests are still blocked.

## 1.5.7
- **PERF:** Verified against the latest Valheim build (Unity 6000.0.75f1, Steam build 26249040) and Epic Loot 0.15.4 – 0.15.6. All patch targets confirmed present in the installed game and mod assemblies.
- **PERF:** `ZSFX.Play` no longer walks the transform hierarchy and runs 30+ case-insensitive name searches on **every sound in the game**. Each emitter is resolved once and cached, so non-set sounds are a single dictionary lookup.
- **PERF:** Set-ability effects are applied without allocating component arrays (reused `List<T>` buffers), and all scans are skipped entirely when volume is 1 and no visual options are enabled.
- **PERF:** Item/drop name parsing (`(Clone)` strip, trim, material-tier matching) is memoized, and the drop filter now caches items with no indexed data instead of re-scanning the whole `ObjectDB` item list on every drop.
- **PERF:** The item tier index is built when the world's `ObjectDB` loads, removing the frame spike on the first creature kill.
- **NEW:** `Logging / LogBlockedDrops` (default `true`) — turns off the one-log-line-per-blocked-item output during large raids.

## 1.5.6
- **NEW:** **Interface / HideEpicLootOutdatedConfigPrompt** (default **true**) — suppresses Epic Loot’s main-menu “Outdated Configs” prompt and auto-records a decline so modpack JSON is not replaced.

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
