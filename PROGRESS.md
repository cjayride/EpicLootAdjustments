# EpicLootAdjustments Progress

**Repository:** https://github.com/cjayride/EpicLootAdjustments

## Goal
Stop creatures from dropping gear whose recipe uses materials from a later biome than the creature's own zone. EpicLoot magic drops are included. A swamp creature must not drop an item made with Ashlands materials.

## Acceptance
- Every creature and boss in the supplied lists maps to one zone.
- Biome order is Meadows, Black Forest, Swamp, Mountain/Ocean (same tier), Plains, Mistlands, Ashlands, Deep North.
- When a mapped creature drops gear above that tier, the drop becomes the same kind of gear from the creature's tier.
- Trophies, hides, ore, and food are left as they are.
- Release build succeeds.

## Status
- 1.2.0 also filters food and trophies, not only weapons.
- `AxeIron`, `TrophyDraugr`, and `Sausages` now count as swamp (tier 3). Fish stays unzoned so Meadows can drop it.
- Release build succeeded. Not tested on a live Eikthyr kill yet.

## Files
- `src/LootMappings.cs` — zone tiers, creature map, material map.
- `src/ItemTierIndex.cs` — walks crafting recipes and picks a same-slot replacement.
- `src/Plugin.cs` — patches `CharacterDrop.GenerateDropList` and EpicLoot's item choice.
- `Cjayride.CLCCLootPatch.csproj` — version 1.1.0.

## Decisions
- Ocean shares Mountain's tier, so serpent materials are tier 4, not swamp.
- Deep North is the highest tier.
- FrostwingFae is Mountain, from the miniboss line. The Deep North creature list also named it.
- Only weapons, armor, shields, tools, and ammo are replaced. A seal can still drop seal hide.
- The replacement keeps the item kind (a sword stays a sword) and uses the highest tier that is still legal for that creature.
- EpicLoot still applies the magic roll after the base item is swapped.

## Build
`dotnet build Cjayride.CLCCLootPatch.csproj -c Release` succeeded.
DLL: `D:\AI-Agent-Projects\cjaycraft\Cjayride.CLCCLootPatch\bin\Release\net481\Cjayride.CLCCLootPatch.dll`
File version 1.1.0.0.

## Next
1. Load the DLL in the test profile and kill a swamp creature after Ashlands gear is unlocked.
2. Confirm the log line `Draugr (Swamp) SwordFlametal -> SwordIron` (or another swamp sword).
3. Confirm trophies and hides are unchanged.
