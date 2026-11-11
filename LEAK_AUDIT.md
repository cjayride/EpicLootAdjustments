# Zone-Tier Loot Leak Audit

Mod: `Cjayride.EpicLootAdjustments` (project `Cjayride.CLCCLootPatch`), fixed in **1.5.13**.
Reported symptom: an **Iron pickaxe** in the player's inventory while fighting plain creatures in
the **Meadows** and **Dark Forest**, with no raid and no boss kill. Iron is **tier 3 (Swamp)**.

## Tier ladder used

| Zone | Tier | | Zone | Tier |
|---|---|---|---|---|
| Meadows | 1 | | Plains | 5 |
| Black Forest | 2 | | Mistlands | 6 |
| Swamp | 3 | | Ashlands | 7 |
| Mountain / Ocean | 4 | | Deep North | 8 |

Iron = 3, Silver = 4, Blackmetal = 5, Chitin = 4 (gated at Mountain), Flametal = 7.

## Root causes found

### 1. Unmapped prefab names were filtered **not at all** (primary — matches the report)

All three filter entry points started with the same guard:

- `CharacterDrop.GenerateDropList` postfix → `return` when `LootMappings.TryGetCreature` failed
- Epic Loot `LootRoller.SpawnNormalItem` / `RollLootTableInternal` prefix → same scope, unset
- Epic Loot `GatedItemTypeHelper.GetGatedItemNameFromItemOrType` postfix → nothing to compare against

`LootMappings.CreatureZones` holds **178** names. Verified unmapped in the pack's Epic Loot config:

| Prefab | Mapped? | Epic Loot `RefObject` → table |
|---|---|---|
| `Bjorn` | yes (Black Forest) | `Tier3EliteMob` |
| `Bjorn_sleeping` | **no** | `Tier3EliteMob` |
| `Troll` | yes (Black Forest) | `Tier3EliteMob` |
| `Troll_sleeping` | **no** | `Tier3EliteMob` |
| `Skeleton_Hildir_nochest` | **no** | `Tier3EliteMob` |
| `Skeleton_Meadows` / `_noarcher` | **no** | `Tier1Mob` |
| `Bat_Swamp`, `Growth`, `Hitni` | **no** | various |

Chain for the reported drop, in the pack's own files:

1. `config\EpicLoot\baseconfig\loottables.json` maps `Bjorn` / `Bjorn_sleeping` / `Troll` /
   `Troll_sleeping` → `Tier3EliteMob` → `Tier3Mob` → `Tier3Everything` / **`Tier3Tools`** →
   `{ "Item": "PickaxeIron", "Weight": 1 }` (line ~2369). A plain **Swamp-tier** table on a
   **Black Forest** creature.
2. `Bjorn_sleeping` / `Troll_sleeping` are **not** in the 178-name map, so the mod's early return
   left the roll completely unfiltered.
3. Epic Loot gates items by **defeated bosses, not biome** (`iteminfo.json`, `"Type": "Pickaxes"`
   → `defeated_bonemass: ["PickaxeIron"]`, `defeated_gdking: ["PickaxeBronze"]`). With
   `Item Drop Limits = BossKillUnlocksCurrentBiomeItems`, any `Pickaxes` type roll can resolve to
   Iron once Bonemass is dead — anywhere in the world.

Meadows loot tables were cleared separately: `Tier1Mob` resolves only to Tier0/Tier1Everything
(max tier 1), and Meadows chest tables are Tier0/Tier1. The Black Forest creature path above is
the leak.

### 2. The Drop That exceptions file had no ceiling (boss-only, secondary)

`Exceptions/DropThatAllowFiles` defaults to `drop_that.character_drop.BossBonusTools.cfg`; every
pair in it bypassed the cap with **no limit**. Full audit of that file (client + server packs,
identical copies):

| Creature | Home tier | Grants | Item tier | Delta |
|---|---|---|---|---|
| `Eikthyr` | 1 Meadows | `AxeIron`, `PickaxeIron` @ 100% | 3 | **+2** |
| `gd_king` | 2 Black Forest | `AxeSilver_TW`, `PickaxeSilver_TW` @ ~33% | 4 | **+2** |
| `Bonemass` | 3 Swamp | `AxeSilver_TW`, `PickaxeSilver_TW` | 4 | +1 |
| `Dragon` (Moder) | 4 Mountain | `AxeBlackMetal`, `PickaxeBlackmetal` | 5 | +1 |
| `Kraken` / `KrakenLD` | 4 Ocean | `AxeBlackMetal`, `PickaxeBlackmetal` | 5 | +1 |
| `BogWitchKvastur` | 3 Swamp | `AxeSilver_TW`, `PickaxeSilver_TW` | 4 | +1 |

The file's own header states the intent: *"bonus tool drops **one tier above** the boss zone"*.
The **+2** rows violate it and are what made Iron reachable from a Meadows kill.

### 3. Non-boss grants in the main Drop That file (blocked by the existing cap)

`drop_that.character_drop.cfg` (5069 lines) grants `gd_king` (tier 2) `SwordIron`, `AxeIron`,
`PickaxeIron`, `MaceIron`, `SledgeIron`, `AtgeirIron`, `ShieldIronBuckler`, `ShieldIronTower`
(lines ~2289–2445) and `Eikthyr` (tier 1) `SwordBronze`, `AxeBronze`, `PickaxeBronze`.
These are **not** in the allow-list, so the 1.5.12 cap already removed them; they are not a leak.
`Bonemass.24 PickaxeIron` is legal (Swamp = tier 3).

## Audit result (`_tmp_scan\leakaudit.ps1`)

Scanned **290** gear grants across `drop_that*.cfg` in `beta_cjaycraft\config` and
`beta_cjaycraft-server\config`:

- **66** over-tier rows on mapped creatures — all **bosses** (`Eikthyr`, `gd_king`, `Bonemass`,
  `Dragon`, `Kraken`, `KrakenLD`, `BogWitchKvastur`, `Fader`); the 1.5.12 cap covered these.
- **0** gear grants on unmapped creatures in the Drop That files. The plain-creature leak came
  through Epic Loot's loot tables (cause 1), not Drop That — which is why the first audit pass,
  restricted to mapped creatures, could not see it.

Also verified as **not** leaks: unidentified drops (`Items Unidentified Drop Ratio = 0`),
sacrifice and enchantment products (materials only), Meadows `TreasureMapChest` tables
(untouched by `patches\cjayride\treasure_balance_loottables.json`).

## Fixes in 1.5.13

1. **`src\ZoneContext.cs` (new).** Resolves the biome of a drop position through
   `ZoneSystem.GetGroundData` (reflection, resolved once; the `Biome` enum is used only through
   its printed name, so no enum identity is hard-coded). One-entry position cache; biome names
   normalised (`BlackForest` / `Black Forest` / `black_forest` all resolve).
2. **`DropScope.Resolve` in `src\Plugin.cs`.** Mapped creature → home biome; **unmapped → biome of
   the drop position**. Applies to the `CharacterDrop.GenerateDropList` postfix and to the Epic
   Loot prefix that feeds `FilterGatedItem`, since `RollLootTableInternal` exposes `dropPoint`.
   `Zones / CapUnknownCreatures` (default `true`).
3. **`LootMappings.TryGetCreature` suffix inheritance.** `_sleeping`, `_NonSleeping`, `_nochest`,
   `_hatchling`, `_calf`, `_cave`, `_loose_stone`, `_p3`, `_Fader`, `_Reto`, `_Altar` inherit the
   base creature's zone, so sleeping/nochest variants are capped by their real biome and not by a
   fallback.
4. **`DropThatAllowList.IsAllowed` bound.** Over-tier allow-list entries need
   `itemTier <= creatureTier + BonusTierStep` **and** must be a tool (`Axe*` / `Pickaxe*`);
   non-tools are always capped. Default `BonusTierStep = 1` enforces the pack's own one-tier
   design, so `Eikthyr → PickaxeIron` and `gd_king → PickaxeSilver_TW` are now refused with a log
   line, while Bonemass/Kraken/Moder/Dvergr-era +1 tools stay legal. `BonusTierStep = 0` gives an
   absolute cap.
5. Item-tier comparison for the allow-list uses `ItemTierIndex.ItemTier`, added alongside the
   existing recipe-material tier computation.

## Verification

- `dotnet build -c Release` → **exit 0, 0 code warnings**; DLL `FileVersion 1.5.13.0`.
- Flat-root Thunderstore zip verified: 5 entries, **no `/` or `\`** in any entry name.
- Leak audit re-run against 1.5.13 semantics: the cause-1 path is capped by biome tier at the
  drop position; the cause-2 **+2** entries are refused at `BonusTierStep = 1`.
- **Not runtime-tested.** In-game confirmation needs a run in the Gale profile: kill
  `Bjorn_sleeping` / `Troll_sleeping` in the Black Forest with Bonemass already defeated, and
  check that `LogBlockedDrops` reports the blocked `PickaxeIron` and no Iron pickaxe lands.

## Remaining / accepted

- `BonusTierStep = 1` deliberately keeps the boss bonus-tool feature. For a strictly absolute cap
  set it to `0`, and the allow-list grants nothing over-tier.
- Epic Loot's own **boss-gated item pool** is untouched: a `Pickaxes` roll can still name
  `PickaxeIron` in the Meadows. The mod now removes it at the drop, which is the same behaviour as
  every other over-tier item, rather than rewriting the roll.
- Chest loot (`TreasureChest_forestcrypt` → `Tier2Everything` → `KnifeChitin`, `SpearChitin`,
  `TrinketChitinSwim`) is Chitin, gated at Mountain in this pack. Chests are now capped by the
  biome they sit in, so these are removed in the Black Forest; a deliberate Chitin-in-forest design
  needs a `BonusTierStep` of 2 and is not enabled.
- `Frometal` has no tier token in `LootMappings`, so Frometal gear is not tiered and is not capped.
  Not part of this report; noted for a future pass.
- Pack config files were **not** modified. `BossBonusTools.cfg` is unchanged in both packs (and in
  both live Gale profiles); the 1.5.13 code enforces the bound at runtime. Backups of the previous
  mod files are in `backup-1.5.12\`.
