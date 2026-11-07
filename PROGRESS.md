# EpicLootAdjustments Progress

## Goal
Allow socketing shards into an item that already has a matching magic effect (for example +damage), because enchant rolls already allow stacking different damage modifiers. Config default on.

## Acceptance
- New `Shards / AllowAnyShardInSlot` (default `true`).
- Epic Loot duplicate-effect socket checks are skipped when the option is on.
- Unique/boss shard exclusivity is not removed.
- Items still need an empty shard slot.
- v1.5.9 builds and packages.

## Status
Complete — v1.5.9 built and packaged. Not launched in-game this session.

## Cause
Epic Loot `ShardSocketManager` refuses a shard when `CheckDuplicateEffect` / `AllowMatchingItemEffect` / `CanCoexist` say the item already has that effect type. Enchant tables can still roll multiple damage types on one item, so the shard rule is stricter than enchants.

## Files changed
- `src/ShardSocketOverride.cs` — new Harmony override of those three bool checks.
- `src/Plugin.cs` — 1.5.9; bind/patch the override.
- `Cjayride.CLCCLootPatch.csproj`, `CHANGELOG.md`, `Package/CHANGELOG.md`, `Package/manifest.json`, `README.md`, `Package/README.md`.

## Build
- `dotnet build ...Cjayride.CLCCLootPatch.csproj -c Release`
- Succeeded, 0 warnings, 0 errors
- DLL: `D:\AI-Agent-Projects\cjaycraft\Cjayride.CLCCLootPatch\bin\Release\net481\Cjayride.EpicLootAdjustments.dll`

## Testing
- Compile only. Not in-game this session.

## Release
- ZIP: `D:\AI-Agent-Projects\cjaycraft\Cjayride.CLCCLootPatch\dist\cjayride-EpicLootAdjustments-1.5.9.zip`

## Next steps
1. Install 1.5.9, confirm a +damage shard sockets into a weapon that already has a +damage enchant.
2. Confirm unique/boss exclusivity still blocks a second unique/boss shard.
3. Set `AllowAnyShardInSlot = false` and confirm Epic Loot's original refusal returns.
