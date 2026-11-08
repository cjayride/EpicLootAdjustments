# EpicLootAdjustments Progress

## Goal
Let a Light Blue Imbue Lightning shard socket into a weapon that already rolled Physical Damage Dealt. Do not allow two of the same shardstone.

## Acceptance
- Legendary Bronze Sledge with Physical Damage Dealt accepts Epic Light Blue Shardstone (Imbue Lightning 4%).
- Duplicate same shardstones still blocked by Epic Loot.
- Unique/boss exclusivity unchanged.

## Status
Complete — v1.5.11 built and packaged. Not launched in-game this session.

## Cause
`AddLightningDamage` lists `ModifyPhysicalDamage` in `ExclusiveEffectTypes`. `CanSocket` fails with `RequirementFailure.ConflictingEffect`. `AllowMatchingItemEffect` only bypasses when the conflicting type **equals** the shard's own type (ExclusiveSelf). Lightning vs physical never hits that bypass.

## Files changed
- `src/ShardSocketOverride.cs` — postfix `CanSocket` / `CanCoexist` to allow `$mod_epicloot_socket_conflict`.
- Version 1.5.11.

## Next steps
1. Build and package.
2. In-game: drop Light Blue shard onto the sledge empty slot.
