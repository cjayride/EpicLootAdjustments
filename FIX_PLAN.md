# EpicLootAdjustments FPS fix — handoff plan

## Problem (confirmed 2026-10-30)

- With **EpicLootAdjustments on**, client FPS drops to ~5–14; BepInEx log spams:
  `AccessTools.TypeByName: Could not find type named Common.QuickConfigBroker`
- With **EpicLootAdjustments off** (Epic Loot 0.15.4 on, rest of CJAYCRAFT on): **80–86 FPS**.
- Disabling Epic Loot without disabling VNEI / SeneaL / this mod caused additional Harmony spam; that was bisect noise, not the root fix.

## Root cause (code)

**Project:** `D:\AI-Agent-Projects\cjaycraft\Cjayride.CLCCLootPatch`  
**GUID:** `com.cjayride.epiclootadjustments` v1.5.0

`src/ConfigButtonHide.cs`:

1. `[HarmonyPatch(typeof(Menu), nameof(Menu.Update))]` → **`HideBrokerButtons()` every frame** on main menu and in game.
2. `HideBrokerButtons()` calls **`AccessTools.TypeByName("Common.QuickConfigBroker")` every frame** instead of caching the `Type`.
3. Epic Loot **0.15.4** may have moved, renamed, or inlined Quick Config; if the type is missing, Harmony logs **every lookup** → main-thread death spiral.
4. `TryPatch()` only runs once at Awake; the **Menu.Update postfix is unconditional** (always patched via `PatchAll` on the patch class).

## Goal

- Restore **EpicLootAdjustments** in the modpack with **no per-frame TypeByName** and **no work when Quick Config is absent**.
- Keep behavior: hide Epic Loot config button when `HideEpicLootConfigButton = true` (modpack default).

## Phase 1 — Scope (30 min)

1. Read `ConfigButtonHide.cs`, `Plugin.cs`, `SetFx.cs`, `EpicLootPatches` (if any).
2. Inspect **Epic Loot 0.15.4** DLL on disk:
   - Path: Gale profile `RandyKnapp-EpicLoot/EpicLoot.dll` or `D:\AI-Agent-Projects\Mods\libs`.
   - Confirm whether `Common.QuickConfigBroker` still exists (ILSpy, `strings`, or reflection one-liner).
3. Document: type name / namespace in 0.15.4 vs what we patch (`TryBuildButtons`, `EnsurePauseButton`, `OnMenuStart`, `RefreshVisibility`).

## Phase 2 — Scan for same pattern (15 min)

- Ripgrep `TypeByName`, `Menu.Update`, `AccessTools` in `src/`.
- Ensure no other **per-frame** Epic Loot reflection.

## Phase 3 — Implement fix

**Minimum fix (likely enough):**

1. **Cache** at successful `TryPatch` time:
   - `static Type _brokerType`
   - `static FieldInfo _instanceField`, `_pauseButton`, `_mainMenuButton`, `_listPanel`
   - `static bool _quickConfigAvailable`
2. If broker type **null** at Awake: **do not register** `MenuConfigButtonPatch` (or postfix returns immediately with `_quickConfigAvailable == false`). Log **once**.
3. **`HideBrokerButtons()`**: use cache only; **never** `TypeByName` in Update.
4. **Throttle** UI hide: run on menu open / pause open (patch those entry points) or at most **once per second** while menu visible — not every `Menu.Update` frame.
5. Change `[BepInDependency("randyknapp.mods.epicloot")]` from **Soft** to **Hard** so load order is sane (optional but recommended).

**If Quick Config API changed in 0.15.4:**

- Update type/method names to match 0.15.4, **or**
- If button hide is no longer possible via broker: gate feature behind config; default off hide path; document in CHANGELOG.

**Version bump:** 1.5.1 (patch).

## Phase 4 — Verify

1. Build Release → print full path to `Cjayride.EpicLootAdjustments.dll`.
2. Gale test profile: Epic Loot **on**, Adjustments **on**, full CJAYCRAFT client.
3. **Acceptance:**
   - Main menu + Meadows: **FPS ≥ prior baseline** (~80+ on reporter’s machine).
   - Log: **zero** repeating `QuickConfigBroker` / `TypeByName` warnings over 2 minutes.
   - With `HideEpicLootConfigButton = true`: config button still hidden (visual check).
   - With `false`: button visible.
4. Regression: loot tier downgrade still works (kill one known creature); set FX toggles still apply.

## Phase 5 — Modpack release

1. Thunderstore zip → `Cjayride.CLCCLootPatch/dist/`.
2. Update `beta_cjaycraft` + `beta_cjaycraft-server` manifest dependency version.
3. CHANGELOG: FPS fix, Quick Config cache, Epic Loot 0.15.4 compatibility.
4. Re-enable EpicLootAdjustments in modpack (currently validated off in production).

## Files expected to touch

- `src/ConfigButtonHide.cs` (primary)
- `src/Plugin.cs` (dependency / conditional patch registration)
- `CHANGELOG.md`, `Package/CHANGELOG.md`, `Package/manifest.json`
- `PROGRESS.md` (session log)
- Optional: `beta_cjaycraft/config/com.cjayride.epiclootadjustments.cfg` (comments only)

## Out of scope

- Graphics / VPO / Drop That debug dumps (separate cleanup).
- Epic Loot config merge (unaffected).
