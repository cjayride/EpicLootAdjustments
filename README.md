# ⚔️ EpicLootAdjustments

**Customize Epic Loot for progression in Valheim.**

EpicLootAdjustments is a BepInEx plugin that balances loot progression, reduces heavy legendary set ability effects, and cleans up the Epic Loot interface.

## ✨ Features

### 🛡️ Zone-Based Loot Progression

Prevents creatures from dropping gear that exceeds the material tier of their home biome.

- Supports vanilla drops and Epic Loot magic item rolls.
- Removes over-tier weapons, armor, shields, tools, and ammunition. Drops are never rewritten into a different item.
- Materials, shardstones, food, and trophies are left exactly as Drop That / Epic Loot rolled them.
- Supports optional exceptions from Drop That configuration files, allowing specific creature/item combinations to bypass the zone restrictions.

**Biome progression:** Meadows → Black Forest → Swamp → Mountain/Ocean → Plains → Mistlands → Ashlands → Deep North.

### 🔊 Legendary Set Ability Effects

Reduce the volume and visual intensity of legendary set abilities.

- **Audio volume:** Configurable, default `0.35`.
- **Visual effects:** Optional smoke, particle, and visual reduction.
- Includes effects associated with Ragnar, Berserker, Bulwark, Undying, and related set abilities.
- Uses compatibility-safe optional hooks to accommodate Epic Loot API differences.

### 🖥️ Interface Customization

Hide Epic Loot's pause-menu **Mod Config** button.

- Enabled by default.
- BepInEx configuration files remain available for adjusting settings.

## ⚙️ Configuration

Configuration file:

`BepInEx/config/com.cjayride.epiclootadjustments.cfg`

Available settings include:

| Section | Setting | Default / Behavior |
|---|---|---|
| Exceptions | `DropThatAllowFiles` | `drop_that.character_drop.BossBonusTools.cfg` |
| Set Abilities | Audio volume | `0.35` |
| Set Abilities | Visual effect reduction | Optional |
| Interface | `HideEpicLootConfigButton` | `true` |
| Interface | `HideEpicLootOutdatedConfigPrompt` | `true` |

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

