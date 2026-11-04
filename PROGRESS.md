# EpicLootAdjustments Progress

## Goal
1. Double-check the mod works against the latest installed Valheim + Epic Loot.
2. Improve code speed, load times, reduce lag, increase FPS (no behavior change).

## Status
Complete — v1.5.7 built, verified, packaged, installed to a test profile.

## Compatibility verification
Method: Mono.Cecil inspection of the real installed assemblies
(`D:\AI-Agent-Projects\cjaycraft\_tmp_scan\ela_target_check.ps1`).

- Valheim install `D:\SteamLibrary\steamapps\common\Valheim`
  - `valheim.exe` FileVersion 6000.0.75.2503836, ProductVersion 6000.0.75f1
  - Steam buildid 26249040 (`appmanifest_892970.acf`)
  - `assembly_valheim.dll` 2,577,920 bytes, 2026-10-28
- Game patch targets: ALL PRESENT
  - `ObjectDB.Awake()`, `ObjectDB.CopyOtherDB(ObjectDB)`, `ObjectDB.GetItemPrefab(string)`
  - `CharacterDrop.GenerateDropList()`, `EffectList.Create(...)`, `ZSFX.Play()`, `Menu.Show()`
- Epic Loot client 0.15.6 (`beta_cjaycraft_client-4.4.19`) and server 0.15.5
  (`beta_cjaycraft_server-4.4.19`): ALL patch targets present
  - `LootRoller.SpawnNormalItem`, `GatedItemTypeHelper.GetGatedItemNameFromItemOrType`
  - `ConfigUpdatePrompt.ShouldPrompt/TryOpen/ShowConfigMessage`
  - `ConfigVersionManager.DeclineOutdatedConfigs/RefreshUnmodifiedConfigs/HasOutdatedConfigs`
  - `EpicLoot.LoadAssets`, `ResolveAudioMixerMocks`, `EpicAssets` (14 static GameObject fields)
  - `Common.QuickConfigBroker` + Instance/pauseButton/mainMenuButton/listPanel
    + TryBuildButtons/EnsurePauseButton/OnMenuStart/RefreshVisibility
- Two optional targets no longer exist in 0.15.5/0.15.6 (gracefully skipped by `PatchIfPresent`):
  - `EpicLoot.MagicItemEffects.FrostAOE::HookUpIceSpikesAudio`
  - `FrostNovaFx::GetOrCreateTemplate` (renamed to `GetOrBuildTemplate`, which IS present,
    so FrostNova coverage is intact)
- BepInEx pack in use: 5.4.2360 — matches manifest dependency.
- Built DLL IL check (`_tmp_scan\ela_dll_inspect2.ps1`, `ela_harmony_attrs.ps1`):
  references BepInEx 5.4.23.5, 0Harmony 2.9.0.0; TypeRefs include ObjectDB, CharacterDrop,
  ZSFX, EffectList, Menu; all 5 HarmonyPatch classes present.

## Performance changes
1. `ZSFX.Play` prefix was the hot path: a transform walk + 30 case-insensitive `IndexOf`
   per ancestor on **every sound in the game**. Now each emitter resolves once into
   `ZsfxRoots`/`ZsfxIgnored` caches; a non-set sound is one dictionary lookup.
2. `SetFx.Apply`: component arrays replaced with reused `List<T>` buffers (the installed
   Unity exposes the non-alloc `GetComponentsInChildren(bool, List<T>)` overload — verified
   in `UnityEngine.CoreModule`). Audio/particle/trail/light scans skipped entirely when
   volume = 1 and no visual options are on.
3. `Touched` set is capped (8192) — no unbounded memory growth over a session.
4. `LootMappings.Clean` and `MaterialTier` are memoized (capped at 4096 entries, falls back
   to the original path past the cap).
5. `ItemTierIndex`: names with no indexed item are recorded in a `Missing` set; previously
   every drop of an unindexed item repeated `GetItemPrefab`'s linear scan + a GetComponent.
6. Index now built eagerly in the `ObjectDB.Awake`/`CopyOtherDB` postfix (`Warm()`) —
   removes the first-kill-in-combat frame spike.
7. Blocked-drop logging gated behind `Logging / LogBlockedDrops` (default `true`).

## Files changed
- `src/SetFx.cs` — caches, non-alloc component scans, no-op short-circuit, capped Touched.
- `src/LootMappings.cs` — `CleanCache`, `MaterialTierCache`, `ComputeClean`/`ComputeMaterialTier`.
- `src/ItemTierIndex.cs` — `Missing` negative cache, `Warm()`, `Invalidate()` clears caches.
- `src/Plugin.cs` — `LogBlockedDrops` config, `Warm()` calls, version 1.5.7.
- `Cjayride.CLCCLootPatch.csproj` — Version 1.5.7.
- `CHANGELOG.md`, `README.md` — 1.5.7 entries + Performance section.
- `dist/package/manifest.json` — 1.5.7. Staged DLL + docs in `dist/package`.
- `PROGRESS.md` — this file.

## Build
- Command: `dotnet build ...Cjayride.CLCCLootPatch.csproj -c Release`
- Result: `Build succeeded. 0 Warning(s) 0 Error(s)`
- First attempt failed with 4x `CS0103: The name 'LogBlockedDrops' does not exist`
  (referenced from sibling static patch classes); fixed by qualifying `Plugin.LogBlockedDrops`.
- DLL: `D:\AI-Agent-Projects\cjaycraft\Cjayride.CLCCLootPatch\bin\Release\net481\Cjayride.EpicLootAdjustments.dll`
  (FileVersion 1.5.7.0, 45,568 bytes)

## Testing
- Static: patch-target + IL reference verification above (all targets resolve).
- Installed 1.5.7 to `shardstonetest` profile
  (`...\profiles\shardstonetest\BepInEx\plugins\cjayride-EpicLootAdjustments\`),
  a profile with EpicLoot 0.15.6 and no prior copy of this mod.
- Backed up beta client + server DLLs as `*.dll.bak-1.5.6`. Beta packs NOT modified.
- The `default` Gale profile does not exist (`Test-Path` = False) — AGENTS.md calls it the
  primary testing profile, but it is not present on this machine, so `shardstonetest` was used.
- NOT yet launched in-game — runtime FPS/behaviour confirmation needs a game session.

## Release
- ZIP: `D:\AI-Agent-Projects\cjaycraft\Cjayride.CLCCLootPatch\dist\cjayride-EpicLootAdjustments-1.5.7.zip`
  (150,149 bytes; entries: DLL, manifest.json, icon.png, README.md, CHANGELOG.md)
- Tag: not created (git not on PATH in this session).

## Next steps
1. Launch `shardstonetest`, kill a few mobs, check BepInEx log for the startup lines
   ("Indexed N items", "EpicLoot set-ability volume default", no Harmony warnings).
2. Confirm set-ability volume/particles behave as before; then promote to beta packs.
3. Optional: bump manifest dependency to `RandyKnapp-EpicLoot-0.15.6` if the packs standardise on it.
