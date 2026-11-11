# EpicLootAdjustments Progress

## Goal
Ship the CJAYCRAFT legendary-set nerfs: every balance value that EpicLoot hard-codes must be reachable, with the feature ON by default, in both the client and server packs.

## Acceptance
- Bulwark's damage-immunity window (asset-bundle prefab value, not JSON) is tunable from config.
- v1.5.12 builds clean, DLL + Thunderstore zip produced under `dist\`, full paths printed.
- `com.cjayride.epiclootadjustments.cfg` updated in both packs.

## Status
Complete — v1.5.12 built (exit 0, 0 warnings) and packaged. Not launched in-game this session.

## Cause
`Ability.ActivateStatusEffectAction` calls `GetSEMan().AddStatusEffect(statusEffect, false, 0, 0f, (short)(-1))`, so the duration passed is 0 and Valheim falls back to the prefab's own `m_ttl`. `BulwarkStatusEffect` is loaded from the embedded asset bundle (`EpicAssets.BulwarkStatusEffect`), and `RegisterBulwark` adds it straight to `ObjectDB.m_StatusEffects`. Nothing in `magiceffects.json` feeds the ability: grep over the full decompile finds `MagicEffectType.Bulwark` referenced only by the Compendium display code, and `SetBonusEvaluator.GetBonusValue` reads `ValuesPerRarity[...].MinValue` for the tooltip/compendium value only. So the 5 s immunity is unreachable by any patch file — it needs a Harmony hook.

## Files changed
- `src/BulwarkTuning.cs` (new) — `Set Abilities / BulwarkDuration` (default **3**, range 1–10). Postfixes `EpicLoot.LoadAssets` and `EpicLoot.ResolveAudioMixerMocks`, plus the existing `ObjectDB.Awake` / `CopyOtherDB` posts, and assigns `SE_Stats.m_ttl` on the `EpicAssets.BulwarkStatusEffect` field. Fixed assignment, so repeated hooks are idempotent; every EpicLoot type/field lookup is null-checked and logs and skips when absent.
- `src/Plugin.cs` — bind + `TryPatch` for `BulwarkTuning`, `Apply()` in both ObjectDB postfixes, plugin version 1.5.12.
- `Cjayride.CLCCLootPatch.csproj` — `<Version>1.5.12</Version>`.
- `Package/manifest.json` — `version_number` 1.5.12.
- `CHANGELOG.md`, `README.md` — 1.5.12 entry, `BulwarkDuration` documented.

## Commands run
- `dotnet build -c Release` → `Build succeeded. 0 Warning(s) 0 Error(s)`, EXITCODE=0.
- `FileVersionInfo` on the new DLL → `FileVersion 1.5.12.0`, `ProductVersion 1.5.12+ec436c8…`.
- Zip built with `Compress-Archive` from a staged folder (ConstrainedLanguage rejects `[System.IO.Compression.ZipArchiveMode]`); entries verified: `CHANGELOG.md`, `Cjayride.EpicLootAdjustments.dll`, `icon.png`, `manifest.json`, `README.md` at zip root, matching the 1.5.11 package layout.

## Output
- DLL: `D:\AI-Agent-Projects\cjaycraft\Cjayride.CLCCLootPatch\bin\Release\net481\Cjayride.EpicLootAdjustments.dll`
- ZIP: `D:\AI-Agent-Projects\cjaycraft\Cjayride.CLCCLootPatch\dist\cjayride-EpicLootAdjustments-1.5.12.zip`

## Important decisions
- Only Bulwark's duration needs the plugin. Undying's cooldown lives in the patchable `abilities.json` (`Cooldown: 1200` → patched to 2700 by the pack), so no code was added for it.
- Bulwark ability cooldown (60 s, `abilities.json`) and Berserker were **not** touched — they were not in the approved change list.
- Default 3 s matches the agreed nerf (5/6/7 → 3/4/4). The prefab is one shared object, so the per-rarity split is not reproducible; one duration applies to all rarities. Documented in the config description.

## Next steps
1. Publish the zip, bump the pack manifests to `cjayride-EpicLootAdjustments-1.5.12`.
2. In-game: cast Bulwark with the Heimdall 3-piece and time the immunity window (expect 3 s).
3. In-game: confirm the set-bonus / affix-count / loottable patches apply (EpicLoot BepInEx log for `Required Patch ... failed to select any tokens`).

---

# 1.5.13 — zone-tier loot leak fix

## Goal
No creature may drop gear above its biome tier. Reported case: an Iron pickaxe (tier 3 / Swamp) from a **plain** creature in the Meadows / Dark Forest, no raid, no boss.

## Acceptance
- Plain, non-boss creatures in Meadows / Black Forest cannot yield Iron gear.
- The fix must not be boss-specific; the non-boss vector has to be closed.
- 1.5.13 builds clean; DLL + flat-root Thunderstore zip under `dist\`, full paths printed.
- Pack configs updated in client and server packs (backups first).

## Status
Complete (code, build, packaging, docs, pack configs). **Not runtime-tested in-game.**

## Root cause (see `LEAK_AUDIT.md`)
1. All filter entry points **returned early** when a prefab name was absent from `LootMappings.CreatureZones` (178 entries). `Bjorn_sleeping`, `Troll_sleeping`, `Skeleton_Hildir_nochest`, `Skeleton_Meadows`, `Bat_Swamp`, chests and mod creatures were therefore **completely unfiltered**. The pack's own `loottables.json` maps `Bjorn`/`Troll` (+ `_sleeping`) -> `Tier3EliteMob` -> `Tier3Mob` -> `Tier3Tools` -> `PickaxeIron`; Epic Loot gates `Type: Pickaxes` by defeated bosses (`defeated_bonemass -> PickaxeIron`), so the roll resolves to Iron anywhere in the world.
2. `Exceptions/DropThatAllowFiles` bypassed the cap with no ceiling: `Eikthyr -> PickaxeIron/AxeIron` @ 100 %, `gd_king -> AxeSilver_TW/PickaxeSilver_TW` @ ~33 %, i.e. **+2** tiers over their zones.

## Files changed
- `src/ZoneContext.cs` **(new)** — biome tier from a drop position via `ZoneSystem.GetGroundData` (reflection once; `Biome` used by printed name only). One-entry position cache, normalised biome keys, warn-once for unknown biomes, boxed defaults for the by-ref enum args.
- `src/Plugin.cs` — new `DropScope.Resolve` (mapped -> home biome, unmapped -> biome of the drop point) used by the `CharacterDrop.GenerateDropList` postfix and the EpicLoot prefixes; new `Zones / CapUnknownCreatures` (default true); version 1.5.13.
- `src/LootMappings.cs` — `TryGetCreature` inherits the base creature's zone for spawn-suffix variants (`_sleeping`, `_NonSleeping`, `_nochest`, `_hatchling`, `_calf`, `_cave`, `_loose_stone`, `_p3`, `_Fader`, `_Reto`, `_Altar`); public `StripSpawnSuffix`.
- `src/DropThatAllowList.cs` — `IsAllowed(creature, item, creatureTier, itemTier)` bounded by new `Exceptions / BonusTierStep` (default **1**), tools only (`Axe*`/`Pickaxe*`); over-step and non-tool entries are refused with a log line. Unbounded 2-arg overload removed (no callers).
- `src/ItemTierIndex.cs` — added `ItemTier(name)` and `IsTool(name)`.
- `Cjayride.CLCCLootPatch.csproj` / `Package/manifest.json` — 1.5.13.
- `CHANGELOG.md`, `README.md` (+ synced copies in `Package\`) — 1.5.13 entries and config table.
- `LEAK_AUDIT.md` **(new)** — full leak audit, verified chains, ruled-out vectors.
- `beta_cjaycraft\config\com.cjayride.epiclootadjustments.cfg` and `beta_cjaycraft-server\config\...` — added `BonusTierStep = 1` and `CapUnknownCreatures = true`; kept byte-identical; `.bak-1.5.13` backups.
- Pack `BossBonusTools.cfg` **not** modified — the runtime bound enforces the file's own one-tier design.

## Commands run
- `dotnet build -c Release` -> `Build succeeded. 0 Warning(s) 0 Error(s)` (only NU1900 offline NuGet warnings), EXITCODE=0.
- `FileVersionInfo` -> `FileVersion 1.5.13.0`.
- Zip built flat-root, entries verified: 5 entries, `WITH-SEPARATOR=0`.
- `_tmp_scan\leakaudit.ps1` -> 290 gear grants scanned; 66 over-tier rows, all on **bosses**; 0 gear grants on unmapped creatures in Drop That files.

## Output
- DLL: `D:\AI-Agent-Projects\cjaycraft\Cjayride.CLCCLootPatch\bin\Release\net481\Cjayride.EpicLootAdjustments.dll`
- ZIP: `D:\AI-Agent-Projects\cjaycraft\Cjayride.CLCCLootPatch\dist\cjayride-EpicLootAdjustments-1.5.13.zip`

## Important decisions
- Unmapped objects are capped by **the biome they dropped in** — gear never outruns the ground you stand on. `CapUnknownCreatures = false` restores the old behaviour.
- `BonusTierStep = 1` keeps the boss bonus-tool feature and matches the header text of `BossBonusTools.cfg`; `0` gives an absolute cap. Chitin-in-Black-Forest designs would need 2 and are off.
- Drops are **removed**, never rewritten, per the 1.5.5 rule.

## Next steps
1. Install 1.5.13 in both Gale profiles (back up the 1.5.12 DLLs first; profile writes need escalated permission) and run in-game.
2. Test: kill `Bjorn_sleeping` / `Troll_sleeping` in Black Forest with Bonemass dead; expect `LogBlockedDrops` lines naming `PickaxeIron` and no Iron pickaxe.
3. Test: kill Eikthyr; expect `Allow-list refused PickaxeIron (tier 3) for Eikthyr (tier 1)` and an Antler/Flint tool drop only.
4. Bump both pack manifests to `cjayride-EpicLootAdjustments-1.5.13`.
5. Optional future pass: tier tokens for `Frometal`; consider a pack-side re-tier of `BossBonusTools.cfg`.

## Backups
`backup-1.5.12\` holds the 1.5.12 `Plugin.cs`, `LootMappings.cs`, `DropThatAllowList.cs`, `csproj`, `manifest.json`, `CHANGELOG.md`, `README.md` and the 1.5.12 DLL.

---

# 1.5.14 — Lucky Drops (jackpot feature)

## Goal
User reaction to the leak report: turning "any creature not in the 178-entry map can get an Iron pickaxe once Bonemass is dead" into a deliberate rare drop, gated by rarity and star level.

## Accepted parameters (asked and answered)
Scope: bosses and high-star creatures only. Chance: 2%. Star floor: 5 stars. Ceiling: one biome tier ahead.

## Status
Built and packaged; pack configs, manifests, changelogs updated. Profile DLLs **still 1.5.13** (Valheim running, file mapped — copy rejected). Install 1.5.14 after closing the game.

## Files changed
- `src/LuckyDrops.cs` **(new)** — `TryAllow(character, name, itemTier, zoneTier, item)`: tier ceiling, boss check via `Character.IsBoss()`, stars via the game's `Character.GetStarLevel()` (reflection; fallback `GetLevel()-GetTier()`; returns 0 = bosses-only when neither resolves — no mod DLL referenced). Config: `Lucky Drops / Enabled (true), Chance (2%), MinStars (5), TierStep (1)`. Logs `LUCKY:` lines through `LogBlockedDrops`.
- `src/Plugin.cs` — `DropContext.Creature` captured in a `GenerateDropList` prefix from `__instance.m_character` (the same access EpicLoot's own patch uses), cleared after the postfix; `LuckyDrops.TryAllow` consulted in both the vanilla postfix and `FilterGatedItem` after the allow-list check; `LuckyDrops.Bind`.
- `csproj` / `manifest.json` → 1.5.14; `CHANGELOG.md` / `README.md` (+ `Package\` copies).
- Pack `com.cjayride.epiclootadjustments.cfg` gets the `[Lucky Drops]` keys (client edited, server copied byte-identical, hash verified).
- Pack manifests → dependency 1.5.14, pack version 4.4.49; both pack CHANGELOGs get the 4.4.49 entry.

## Commands run
- `dotnet build -c Release` → `Build succeeded. 0 Warning(s) 0 Error(s)`, EXITCODE=0, DLL `1.5.14.0`.
- Zip entries: 5, `WITH-SEPARATOR=0`.
- Profile DLL copy → `Copy-Item : The requested operation cannot be performed on a file with a user-mapped section open.` (Valheim PID 60948, started 04:57, has 1.5.13 loaded: `Loading [EpicLootAdjustments 1.5.13]` + `Ready. Mapped 178 creatures.`, no biome warnings). **Deferred, not silently skipped.**

## Output
- DLL: `D:\AI-Agent-Projects\cjaycraft\Cjayride.CLCCLootPatch\bin\Release\net481\Cjayride.EpicLootAdjustments.dll` (1.5.14.0)
- ZIP: `D:\AI-Agent-Projects\cjaycraft\Cjayride.CLCCLootPatch\dist\cjayride-EpicLootAdjustments-1.5.14.zip`

## Important decisions
- Stars = vanilla `GetStarLevel()` (`GetLevel() - GetTier()`), exactly how Creature Level & Control computes them (`SetLevel(base + stars)`). CLCC is **not** linked; reflection + safe fallback. The workspace `assembly_valheim` decompiles are filtered (no `GetTier`/`m_tier` lines), so runtime reflection is the verified path and the bosses-only fallback covers a missing method.
- The 5-star floor + 1-tier ceiling keep the original rule intact: no plain Meadows/Black Forest creature can exceed its biome; Trolls/Bjorns (3 stars) stay capped; a jackpot needs a 5+ star creature (raids, high world tier) or a boss.
- `BonusTierStep` (allow-list) and `Lucky Drops / TierStep` are separate knobs, both default 1.

## Next steps
1. Close Valheim, then copy the 1.5.14 DLL into both Gale profiles (1.5.12 backups already present).
2. In-game: raid kill at 5+ stars → about 1 in 50 over-tier drops survives, with a `LUCKY:` log line; plain forest creatures never produce one.
3. Keep watching the 1.5.13 session log for `GetGroundData was not found` / biome warnings (none so far).


---

# 1.5.15 — Lucky Drops boss gate

## Goal
User question: "this 2% lucky is only if the next zone boss was killed, right?" It was **not** (loot tables name items directly; `Tier3Tools` → `PickaxeIron` needs no boss). User chose: **yes — require that zone's boss killed** (recommended option).

## Status
Complete: built, packaged, installed to both Gale profiles, pack configs/manifests/changelogs synced. Runtime test pending in-game.

## Files changed
- `src/LuckyDrops.cs` — `RequireZoneBoss` (default true) config + `BossDefeated(itemTier)` using `ZoneSystem.instance.GetGlobalKey("defeated_<boss>")`. Tier→boss map: 1 eikthyr, 2 gdking, 3 bonemass, 4 dragon, 5 goblinking, 6 queen, 7 fader, 8 frozenking (the same 8 keys CLCC line 217 lists). Tier ≤1 always allowed; unknown tier / null ZoneSystem / exception → not allowed. Gate runs before the 2% roll. Added `using System.Collections.Generic;`.
- `csproj`/`Package/manifest.json` → 1.5.15; `CHANGELOG.md`, `README.md` (+ `Package\` copies).
- Pack `com.cjayride.epiclootadjustments.cfg` (client edited, server copy byte-identical, 4683 bytes, no BOM) gains `RequireZoneBoss = true` + the `[Lucky Drops]` block.
- Pack manifests: dependency `cjayride-EpicLootAdjustments-1.5.15`; **pack version stays 4.4.47** (user consolidated 4.4.48/4.4.49 into 4.4.47 at ~05:23–05:45; my 4.4.49 replacement no-oped and I aligned to their flow).
- Both pack `CHANGELOG.md`s: added the 1.5.15 Lucky Drops + `Lucky Drops` config lines under the user's 4.4.47 section (files were externally modified; re-read before editing).

## Commands run
- First build after edit → `error CS0246: Dictionary<,>` (missing using) → fixed → `Build succeeded. 0 Warning(s) 0 Error(s)`, EXITCODE=0, DLL `1.5.15.0`.
- Zip: 5 entries, `WITH-SEPARATOR=0`, 159,570 bytes.
- Profile install: `.bak-1.5.14` backups created; both profiles `v1.5.15.0`, SHA256 match source `3948D889…`.

## Output
- DLL: `D:\AI-Agent-Projects\cjaycraft\Cjayride.CLCCLootPatch\bin\Release\net481\Cjayride.EpicLootAdjustments.dll` (1.5.15.0)
- ZIP: `D:\AI-Agent-Projects\cjaycraft\Cjayride.CLCCLootPatch\dist\cjayride-EpicLootAdjustments-1.5.15.zip`

## Important decisions
- Jackpot now requires **boss + stars + tier ceiling + boss progression**: a 5★ forest creature can jackpot iron only after Bonemass; before that, the roll is removed as before (no LUCKY line, no drop).
- Gate reads the game's world-global keys (verified against CLCC's key list); matches EpicLoot's own `defeated_*` gates in `iteminfo.json`.
- `RequireZoneBoss = false` restores pure star-gating.

## Next steps
1. In-game: kill Bonemass, then farm 5★ raids in Black Forest — occasional `LUCKY:` iron lines; before Bonemass, never.
2. Plain forest mobs at any stars: no over-tier gear, ever (5★ floor + 1-tier ceiling + boss gate).
3. Optional: pack-side re-tier of `BossBonusTools.cfg`; tier token for `Frometal`.


## 1.5.15 release addendum (06:09 session)
- `[BepInPlugin]` version string was hardcoded `1.5.13` -- the Gale log line `Loading [EpicLootAdjustments 1.5.13]` was the *attribute*, not a stale DLL (profile DLL file versions verified 1.5.15.0, SHA match). Fixed to `1.5.15`, rebuilt, reinstalled to both profiles (sha `EF300AE591A9...`).
- Zip rebuilt with the corrected DLL: 5 entries, 0 separators, inner DLL SHA256 == build (`EF300AE591A9...`, 61,440 bytes). First hash attempt returned `E3B0C442...` (SHA of an empty stream) because the MemoryStream position was left at EOF after `CopyTo`; re-verified with `Position=0`.
- Git (git at `C:\Program Files\Git\cmd\git.exe`, not on PATH): commit `5aa747d` "Zone loot leak fix + Lucky Drops jackpots (v1.5.12-v1.5.15)" (18 files, +1224/-31; `backup-*/` added to `.gitignore`, no binaries committed), annotated tag `v1.5.15`, pushed `main` + tag to `github.com/cjayride/EpicLootAdjustments`.
- 1.5.12-1.5.15 all shipped in this one commit (HEAD was v1.5.11).
- Encoding incident: `Add-Content -Encoding UTF8` + `Get-Content -Raw` appended sections double-encoded non-ASCII through CP1252 (31 mojibake lines); a repair script (`_tmp_scan\fix-mojibake.ps1`, MatchEvaluator re-decode) restored them; first repair attempt truncated the file to 0 bytes on a type error and was restored via `git checkout -- PROGRESS.md`.
