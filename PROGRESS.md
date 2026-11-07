# EpicLootAdjustments Progress

## Goal
Stop The Elder (Black Forest) from dropping Chaos Armor `T0ChaosPlateArmor` (Epic Loot display name "Fell Suit"), which recycles into silver and obsidian.

## Acceptance
- Gear tier is taken from ObjectDB recipes, including recipes added after `ObjectDB.Awake`.
- `T0ChaosPlateArmor` (Silver + Obsidian + WolfPelt) is mountain tier and is blocked on `gd_king` / Black Forest.
- Nested Epic Loot rolls keep the killing creature in scope.

## Status
Complete — v1.5.8 built and packaged. Not launched in-game this session.

## Cause
1. `T0ChaosPlateArmor` is Chaos plate chest. Wacky recipe is `Silver:20`, `Obsidian:20`, `WolfPelt:10`, `Crystal`, `TrophySGolem`. Epic Loot names an Epic roll "Fell Suit" (`mod_epicloot_epic_fell` + `mod_epicloot_epic_suit`).
2. Prefab name has no silver/obsidian tokens, so name matching alone gives tier 0.
3. v1.5.7 built the index in `ObjectDB.Awake` before WackyDatabase recipes exist, then never rebuilt. Recipe walk never saw silver.
4. Elder Epic Loot table uses `Tier2Everything`. Chaos plate can land there if Epic Loot treats forge-level-1 plate as Black Forest.
5. `SpawnNormalItem` finalizer cleared creature context while `RollLootTableInternal` was still resolving later drops.

## Files changed
- `src/ItemTierIndex.cs` — rebuild when recipe or item counts change.
- `src/Plugin.cs` — 1.5.8; nested roll depth; patch `RollLootTableInternal`.
- `Cjayride.CLCCLootPatch.csproj`, `CHANGELOG.md`, `Package/CHANGELOG.md`, `Package/manifest.json`.

## Build
- `dotnet build ...Cjayride.CLCCLootPatch.csproj -c Release`
- Succeeded, 0 warnings, 0 errors
- DLL: `D:\AI-Agent-Projects\cjaycraft\Cjayride.CLCCLootPatch\bin\Release\net481\Cjayride.EpicLootAdjustments.dll`

## Testing
- Static: recipe file confirms T0 chest is silver/obsidian; Elder is `gd_king` Black Forest tier 2.
- Not in-game this session.

## Release
- ZIP: `D:\AI-Agent-Projects\cjaycraft\Cjayride.CLCCLootPatch\dist\cjayride-EpicLootAdjustments-1.5.8.zip`

## Next steps
1. Install 1.5.8, kill Elder, confirm no silver/obsidian plate and a blocked-drop log line for `T0ChaosPlateArmor` if it rolls.
2. Confirm log shows a second "Indexed N items from M recipes" after Wacky recipes load.
