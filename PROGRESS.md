# EpicLootAdjustments Progress

## Goal
Fix 1.5.9 blocking all shard socketing (SeneaLHudLayout and Epic Loot UI). Keep allowing a shard whose effect already exists as an item enchant.

## Acceptance
- Socketing works again for shards that do not collide with an existing enchant.
- `AllowAnyShardInSlot` still lets a +damage shard go on an item that already rolled +damage.
- Unique/boss exclusivity and empty-slot checks still apply.

## Status
Complete — v1.5.10 built and packaged. Not launched in-game this session.

## Cause
`ShardSocketManager.CheckDuplicateEffect` returns true when the check **passes**. 1.5.9 Harmony-forced it to false, so `CanSocket` always failed. SeneaLHudLayout hosts Epic Loot's own drop/add path, so it broke too.

The intended gate is `AllowMatchingItemEffect` (Epic Loot config `AllowShardstoneDuplicateItemEffect`), used only when `CheckRequirements` fails with ExclusiveSelf against the same effect type.

## Files changed
- `src/ShardSocketOverride.cs` — patch only `AllowMatchingItemEffect`.
- Version 1.5.10 in plugin, csproj, manifest, changelogs.

## Next steps
1. Build and package 1.5.10.
2. Confirm shard window drop works in-game.
