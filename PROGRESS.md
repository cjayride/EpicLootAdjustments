# EpicLootAdjustments Progress

**Repository:** https://github.com/cjayride/EpicLootAdjustments

## Goal
Fix the FPS collapse (90 → ~10) caused by per-frame `AccessTools.TypeByName("Common.QuickConfigBroker")` on `Menu.Update`. Keep zone loot filtering, set FX, and optional config-button hide.

## Acceptance
- No `TypeByName` / field lookup on every `Menu.Update` frame.
- If Quick Config is missing (Epic Loot 0.15.4), skip the hide path and log once.
- Hide still works when the broker type exists (`HideEpicLootConfigButton = true`).
- Release 1.5.1 builds; Thunderstore zip in `/dist`.

## Status
Completed (code + package). In-game FPS check still needs a client launch with 1.5.1 installed.

## Files
- `src/ConfigButtonHide.cs` — cache broker type/fields; no `Menu.Update` patch; hide on `Menu.Show` only; silent type resolve.
- `src/Plugin.cs` — version 1.5.1; Epic Loot is a hard dependency.
- `Cjayride.CLCCLootPatch.csproj` — 1.5.1
- `CHANGELOG.md`, `Package/CHANGELOG.md`, `Package/manifest.json`
- `beta_cjaycraft` + `beta_cjaycraft-server` dependency `cjayride-EpicLootAdjustments-1.5.1`

## Decisions
- Epic Loot 0.15.4 DLL has no `QuickConfigBroker` string; hide is skipped on that version (no FPS cost).
- Type lookup uses `Assembly.GetType` so Harmony does not log missing types even at Awake.

## Build
`dotnet build Cjayride.CLCCLootPatch.csproj -c Release` succeeded.
DLL: `D:\AI-Agent-Projects\cjaycraft\Cjayride.CLCCLootPatch\bin\Release\net481\Cjayride.EpicLootAdjustments.dll`
File version 1.5.1.0
ZIP: `D:\AI-Agent-Projects\cjaycraft\Cjayride.CLCCLootPatch\dist\cjayride-EpicLootAdjustments-1.5.1.zip`

## Next
1. Drop the 1.5.1 DLL into the Gale/CJAYCRAFT client (re-enable the mod).
2. Confirm main menu + Meadows stay at the ~80 FPS baseline with no repeating `QuickConfigBroker` log lines.
