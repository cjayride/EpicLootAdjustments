# ⚔️ EpicLootAdjustments

**Customize Epic Loot for progression in Valheim.**

EpicLootAdjustments is a BepInEx plugin that balances loot progression, reduces heavy legendary set ability effects, and cleans up the Epic Loot interface.

## ✨ Features

### 🛡️ Zone-Based Loot Progression

Prevents creatures from dropping gear that exceeds the material tier of their home biome.

- Supports vanilla drops and Epic Loot magic item rolls.
- Removes over-tier weapons, armor, shields, tools, and ammunition. Drops are never rewritten into a different item.
- Materials, shardstones, food, and trophies are left exactly as Drop That / Epic Loot rolled them.
- **Every object is capped.** A creature in the biome map is capped at its home biome. Anything not in the map — sleeping variants, Hildir "nochest" prefabs, treasure chests, added-mod creatures — is capped at the biome the drop happened in, so gear never outruns the ground you are standing on. Controlled by `Zones / CapUnknownCreatures` (default `true`).
- Supports optional exceptions from Drop That configuration files, allowing specific creature/item combinations to exceed the creature's biome **by up to `Exceptions / BonusTierStep` tiers, tools only**. Default `1` keeps the CJAYCRAFT bonus-tool design (Bonemass → Silver, Moder → Blackmetal) while blocking 2-tier entries such as Eikthyr → Iron. `0` makes the zone cap absolute.

**Biome progression:** Meadows → Black Forest → Swamp → Mountain/Ocean → Plains → Mistlands → Ashlands → Deep North.

### 🎰 Lucky Drops (Jackpots)

A **boss**, or a creature at **5+ stars**, can keep an over-tier gear drop **2%** of the time — up to **one biome tier** above its zone.

- A 5-star raid Troll (Mountain) can luck into silver; a plain 1–2 star Greydwarf, Deer, or Bjorn never can.
- **The item's zone boss must already be defeated**: jackpot iron requires Bonemass dead, silver requires Moder, blackmetal requires Yagluth.
- The one-tier ceiling is absolute: Black Forest creatures top out at Swamp iron, never Mountain silver.
- Stars are the same level above base tier that Creature Level & Control assigns.
- Settings: `Lucky Drops / Enabled`, `Chance` (percent), `MinStars`, `TierStep`, `RequireZoneBoss`.

### 🔊 Legendary Set Ability Effects

Reduce the volume and visual intensity of legendary set abilities.

- **Audio volume:** Configurable, default `0.35`.
- **Visual effects:** Optional smoke, particle, and visual reduction.
- Includes effects associated with Ragnar, Berserker, Bulwark, Undying, and related set abilities.
- **`Set Abilities / BulwarkDuration`** (default `3`) — seconds of total damage immunity granted by the Heimdall 3-piece **Bulwark** ability. Epic Loot bakes this number into its own asset bundle, where no config or patch file can reach it; `5` is the stock duration.
- Uses compatibility-safe optional hooks to accommodate Epic Loot API differences.

### 🖥️ Interface Customization

Hide Epic Loot's pause-menu **Mod Config** button.

- Enabled by default.
- BepInEx configuration files remain available for adjusting settings.

### 💎 Shard Socket Override

Epic Loot treats damage-style effects as one exclusive family. A rolled **Physical Damage Dealt** enchant blocks a Light Blue **Imbue Lightning** shard even though those are different damage types.

- **`Shards / AllowAnyShardInSlot`** (default **true**) ignores that enchant-vs-shard exclusive-family conflict so the shard can go in an empty slot.
- It does **not** allow two of the same shardstone, and unique/boss exclusivity is unchanged.
- Set the option to `false` to restore Epic Loot's exclusive-effect socket rules.

## ⚙️ Configuration

Configuration file:

`BepInEx/config/com.cjayride.epiclootadjustments.cfg`

Available settings include:

| Section | Setting | Default / Behavior |
|---|---|---|
| Zones | `CapUnknownCreatures` | `true` — cap unlisted creatures/chests at the biome the drop happened in |
| Exceptions | `DropThatAllowFiles` | `drop_that.character_drop.BossBonusTools.cfg` |
| Exceptions | `BonusTierStep` | `1` — how far above its biome a listed tool may drop (`0` = absolute cap) |
| Lucky Drops | `Enabled` | `true` — bosses and 5+ star creatures keep over-tier gear on a rare roll |
| Lucky Drops | `Chance` | `2` percent per over-tier gear drop |
| Lucky Drops | `MinStars` | `5` stars for non-boss eligibility (`0` = every creature) |
| Lucky Drops | `TierStep` | `1` biome tier above the zone, maximum |
| Lucky Drops | `RequireZoneBoss` | `true` — the jackpot item's zone boss must be defeated first |
| Set Abilities | Audio volume | `0.35` |
| Set Abilities | Visual effect reduction | Optional |
| Interface | `HideEpicLootConfigButton` | `true` |
| Interface | `HideEpicLootOutdatedConfigPrompt` | `true` |
| Shards | `AllowAnyShardInSlot` | `true` |
| Logging | `LogBlockedDrops` | `true` |

Settings can be changed through the configuration file or a compatible BepInEx configuration manager.

## 📦 Requirements

- **BepInEx** — Required.
- **Epic Loot** — Soft dependency; required for Epic Loot loot-roll integration and interface hooks.

## 📥 Installation

1. Install BepInEx.
2. Install Epic Loot to enable its integrations.
3. Place `Cjayride.EpicLootAdjustments.dll` in your `BepInEx/plugins` folder.
4. Launch Valheim and configure the plugin as needed.

### Upgrading from CLCCLootPatch

This plugin replaces the previous `Cjayride.CLCCLootPatch` name.

- Remove the old `Cjayride.CLCCLootPatch.dll` from `BepInEx/plugins`.
- The new plugin uses `com.cjayride.epiclootadjustments.cfg`.
- The old `com.cjayride.clcllootpatch.cfg` configuration file is not loaded automatically.

## 🧰 Build Information

**Output DLL:**

`bin/Release/net481/Cjayride.EpicLootAdjustments.dll`

**Thunderstore package:** The `Package/` directory contains the package icon, manifest, and plugin files used to assemble the release.

## ⚡ Performance

The plugin is designed to stay off the game's hot paths:

- Sound effects (`ZSFX.Play`) resolve each emitter once and cache the result, so ordinary sounds cost a single lookup.
- Set-ability effects are processed with reused buffers, so applying them allocates no component arrays.
- Item and creature name lookups are memoized, and the item tier index is built when the world loads instead of during combat.
- Drop filtering writes no log lines by default during raids beyond the configured `LogBlockedDrops` setting.

