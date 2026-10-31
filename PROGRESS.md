# EpicLootAdjustments Progress

## Goal
Hide Epic Loot "Outdated Configs" main-menu prompt by default so modpack players are never asked to replace JSON.

## Status
Completed — v1.5.6 built and installed to Gale test profile.

## Changes
- `src/OutdatedConfigHide.cs` — Harmony patches on `EpicLoot.ConfigUpdatePrompt` (`ShouldPrompt`, `TryOpen`, `ShowConfigMessage`) and auto-`DeclineOutdatedConfigs` when enabled.
- `src/Plugin.cs` — wire bind/patch; version 1.5.6.
- Modpack cfgs: `beta_cjaycraft/config/com.cjayride.epiclootadjustments.cfg`, server copy.
- Docs/manifest bumped to 1.5.6.

## Build
- Command: `dotnet build ... -c Release` — success.
- DLL: `D:\AI-Agent-Projects\cjaycraft\Cjayride.CLCCLootPatch\bin\Release\net481\Cjayride.EpicLootAdjustments.dll` (1.5.6.0)

## Test profile
- Installed to `...\beta_cjaycraft_client_4.4.14\BepInEx\plugins\cjayride-EpicLootAdjustments\` (backup `.bak-1.5.5`).
- Updated `BepInEx/config/com.cjayride.epiclootadjustments.cfg` with `HideEpicLootOutdatedConfigPrompt = true`.

## Verify in game
1. Launch profile, open main menu — no "Epic Loot: Outdated Configs" dialog.
2. BepInEx log should include: `EpicLoot outdated config prompt is hidden.` and possibly `auto-declined`.

## Release
- ZIP: `D:\AI-Agent-Projects\cjaycraft\Cjayride.CLCCLootPatch\dist\cjayride-EpicLootAdjustments-1.5.6.zip`
- Tag: `v1.5.6`

## Next steps
- Ship modpack update with new DLL + cfg when ready.
