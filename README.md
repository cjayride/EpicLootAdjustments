# EpicLootAdjustments

Valheim BepInEx plugin that tunes Epic Loot for CJAYCRAFT progression:

1. **Zone loot filter** — Stops creatures from dropping gear (including Epic Loot rolls) whose recipe uses materials from a higher biome than the creature’s home zone. Optional exceptions from Drop That files (e.g. boss bonus tools).
2. **Set ability audio & VFX** — Lowers legendary set ability volume and trims heavy particles (Ragnar frost cone, Berserker, etc.).
3. **Hide config button** — Hides Epic Loot’s pause-menu “Mod Config” button by default.

## Build

```powershell
dotnet build Cjayride.CLCCLootPatch.csproj -c Release
```

Output DLL:

`bin\Release\net481\Cjayride.EpicLootAdjustments.dll`

## Config

`BepInEx\config\com.cjayride.epiclootadjustments.cfg`

## Dependencies

- BepInEx
- Epic Loot (soft dependency — loot filter and UI hooks need it)

## Thunderstore

Package folder: `Package/` (`icon.png`, `manifest.json`, built DLL under `plugins/` in the zip).
