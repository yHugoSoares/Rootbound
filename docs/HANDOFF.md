# Handoff

Milestone 1 (local combat arena) has now been **imported, compiled, and run
headlessly** on the Mac with Unity `6000.0.84f1`. This document records exactly
what was verified, what was fixed, and what still requires the GUI or hardware.
Platform policy is in `docs/ENVIRONMENT.md`.

## Current status

- Unity compile: **succeeded** (`-batchmode`, EXIT=0, no `error CS`).
- Unity EditMode tests: **47/47 passed**.
- Unity PlayMode integration tests: **3/3 passed**.
- Pure core tests: **47/47 passed**.
- Arena scene: **generated** (`Assets/Rootbound/Scenes/CombatArena.unity`) and it
  runs in Play Mode headlessly.
- Local GUI play (keyboard/mouse, single instance): **manually validated** (see
  "Manual validation"). Gamepad, two-player, standalone builds, and online play:
  **not validated**.

## Exact engine and packages (resolved on this machine)

- Editor used: **`6000.0.84f1`**, arm64, `/Applications/Unity/Hub/Editor/6000.0.84f1`.
  A second editor `6000.6.4f1` is installed but was **not** used, to avoid a
  silent editor migration.
- `Packages/packages-lock.json` resolved:
  - `com.unity.render-pipelines.universal` **17.0.4** (editor-bundled; manifest pins 17.0.3)
  - `com.unity.render-pipelines.core` 17.0.4
  - `com.unity.inputsystem` **1.11.2** (registry)
  - `com.unity.test-framework` **1.6.0** (editor-bundled; manifest pins 1.4.6)
  - `com.unity.ugui` 2.0.0
- Active Input Handling was `0` (Input Manager only). It is now **Both**, written
  to `ProjectSettings/ProjectSettings.asset`, so the Input System backend is active.

## Setup performed (reproducible)

Run from the project root with the pinned editor:

```
"/Applications/Unity/Hub/Editor/6000.0.84f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -quit -projectPath "$(pwd)" \
  -executeMethod Rootbound.EditorTools.RootboundSceneBuilder.SetupAndCreateArenaScene
```

That command:
1. Creates and assigns the URP asset
   (`Assets/Rootbound/Settings/RootboundUrpAsset.asset` +
   `RootboundUniversalRenderer.asset`) in Graphics and Quality settings.
2. Sets Active Input Handling to Both.
3. Writes the definition assets (`Assets/Rootbound/Data/*.asset`).
4. Creates `Assets/Rootbound/Scenes/CombatArena.unity` and adds it to Build Settings.

The same steps are available in the menu as
`Rootbound > Setup Project and Create Arena Scene`, or as separate
`Rootbound > Configure URP and Input`, `Build Milestone 1 Content`,
`Create Arena Scene`. All are idempotent: existing assets are reused, not
overwritten.

## Errors found and fixed during import

1. `RootboundMenu.cs`: `Repaint()` does not exist on `MonoBehaviour`. Removed the
   unused `INetworkSession.Changed` handler; the IMGUI menu redraws each frame.
2. `RootboundInputActions.cs`: `InputActionMap.AddAction` has no parameter named
   `expectedControlType`. The correct name in Input System 1.11.2 is
   `expectedControlLayout`; both actions updated.
3. `Rootbound.Editor.asmdef`: the configurator needs URP types. Added
   `Unity.RenderPipelines.Core.Runtime` and `Unity.RenderPipelines.Universal.Runtime`
   references (and `RootboundProjectConfigurator.cs` for URP/input automation).

No gameplay logic was changed during import.

## Manual review round 2: cooldowns never advanced (root cause)

Manual findings (WASD worked; primary had no effect; special and dodge worked once
then never; HUD was frozen; enemies stopped damaging after the first hit) were all
caused by one defect:

- `CombatSimulation.Step` advanced `AbilityExecution`, `DodgeState`, cages and
  projectiles, but **never called `Cooldown.Tick`**. Player primary/special/dodge
  cooldowns and `EnemyState.AttackCooldown` therefore froze at their configured
  duration after first use.
- Mutable structs were **not** the problem. `PlayerState`/`EnemyState` are classes
  and `Health`/`Cooldown`/`DodgeState` are public fields, so mutation happens in
  place; `IReadOnlyList<PlayerState>` and `foreach` yield references, not copies.
  The defect was purely the missing update call.

Fix (minimal, no architecture change, no cooldown bypass, invulnerability kept):

- `CombatSimulation.UpdateCooldowns(dt)` runs at the start of every `Step` and
  ticks primary/special/dodge for all players and attack for all enemies.
- `CombatSimulation.ElapsedTime` now advances per step.
- Dev diagnostics added: `CombatHud` F1 overlay (release-hidden) and
  `PlayerState.LastRequestedSlot/LastRequestAccepted/LastRequestReason/LastRequestTick`.

Regression proof: with the fix removed the suite was **6 failed / 28 passed /
34 total** (exactly the six new tests); with the fix restored it is **34/34**.
A PlayMode test (`RunnerAdvancesCooldownsWithoutInput`) verifies recovery through
the real `LocalGameRunner` path. See `docs/TESTING.md`.

## Manual review round 3: targeting, special repeat, dodge latch

Three separate issues were found and fixed (no architecture change, no Photon):

1. **Special ignored the cursor.** `PlayerCommand` carried only a direction and
   `ResolveSpecial` used `Position + Facing * CastRange` (always max range).
   Added an optional `TargetPoint` to `PlayerCommand`; the simulation snapshots
   the target on the accepted cast, clamps it to `CastRange` in the same
   direction, and spawns the cage there. Dev-only aim markers show the actual
   landing point (orange when clamped).
2. **"Q only once".** There is no single-cage restriction. The cage expires at
   3.5s but the special cooldown is 5.5s, and rejections were invisible, so a
   press inside that gap silently did nothing. Repeat casts are now covered by
   tests, and rejections show the exact reason (`Special: cooldown 2.31s`).
3. **Intermittent dodge.** `WasPressedThisFrame()` was sampled only inside
   `PlayerInputAdapter.Build`, which only runs on fixed steps, so frames with no
   step dropped the press. `LocalGameRunner` now calls `CaptureFrame` every
   rendered frame; the adapter latches the press until a `Build` consumes it once.
   Direction rule: move direction if moving, otherwise facing.

The F1 diagnostics overlay default was also corrected to hidden.

## Manual review round 4: dodge counter display

The counter was already reading the authoritative `PlayerState.DodgeCooldown`
(live, correct player, correct unit) and `OnGUI` only reads; only
`CombatSimulation.Step` advances timers. The visible defects were presentation:

1. **Locale formatting bug:** `ToString("0.0")` is culture-sensitive and rendered
   `0,9` (comma) on this machine. All cooldown values now go through
   `CooldownDisplay` with `CultureInfo.InvariantCulture`.
2. **`0.0` shown before ready:** rounding displayed `0.0` while a press was still
   rejected. `CooldownDisplay.Label` rounds up to the next tenth and only shows
   `ready` at `IsReady`.
3. **Overlapping text:** the rejection line overlapped the cooldown line and the
   next player's block; spacing was increased.
4. **Paused when unfocused:** `runInBackground` was `0`, so Editor play paused
   when the window lost focus, matching "only counts down while interacting".
   Set to `1`.

No change to dodge timing, cooldown duration, or invulnerability. The simulation
remains the single source of truth; no UI timer was added.

## Manual validation (keyboard/mouse, local)

Confirmed by the developer in the Unity Editor on this Mac:

- WASD movement and mouse aiming.
- Primary attack deals damage; cooldown counts down; can attack again.
- Cursor targeting and range clamping (marker at cursor; clamped landing point).
- Repeated Q casts after cooldown; cage spawns at the marker.
- Moving and stationary dodge; executes and recovers.
- Cooldown display counts down and returns to ready; rejection feedback shown.
- F1 diagnostics toggle.
- Repeated enemy damage over time and live HUD updates.

Explicitly **not** validated: gamepad input, local two-player, Root Cage ignition
in the GUI, encounter/defeat overlays and `R` restart in the GUI, long-session
exception soak, frame-rate sweep, standalone builds, and online play.

## Tests actually executed

| Suite | Command | Result |
| --- | --- | --- |
| Pure core | `dotnet test Tools/CoreTests/CoreTests.csproj` | 47 passed, 0 failed |
| Unity import/compile | `-batchmode -nographics -quit` | EXIT=0, no compiler errors |
| Unity EditMode | `-runTests -testPlatform EditMode` | 47 passed, 0 failed |
| Unity PlayMode | `-runTests -testPlatform PlayMode` | 3 passed, 0 failed |

The PlayMode tests load the generated scene and assert scene wiring, creature
spawning from ScriptableObjects, simulation stepping, Blightling movement/damage,
no exceptions, cooldown recovery through `LocalGameRunner` with no input, and
that a discrete dodge press is latched across a frame with no fixed step. They
run with `-nographics`; this is **not** a visual or GUI check. Full detail in
`docs/TESTING.md`.

A Unity batchmode run requires the Editor to be closed for this project. If the
Editor is open, use `Window > General > Test Runner` instead of launching a second
instance.

## Controls and two-player assignment (verified by code inspection)

`RootboundMenu.StartLocal()` starts `LocalGameRunner.BeginSession()` with
`playerCount = 2`. `BeginSession` builds one input map per player index:

- **Player 1 (index 0) - keyboard/mouse:**
  - Move `WASD` (also left stick), Aim mouse position (also right stick),
    Primary left mouse (also gamepad South), Special `Q` / right mouse (also
    gamepad West), Dodge `Space` (also gamepad East).
  - Aim uses a mouse raycast to the ground plane whenever a mouse is present.
- **Player 2 (index 1) - gamepad only:**
  - Move left stick, Aim right stick, Primary right trigger, Special left
    trigger, Dodge gamepad East.
  - **There are no keyboard bindings for player 2.**

Consequence: **local two-player requires one gamepad for player 2.** Player 1 can
be played alone with keyboard and mouse. Player 2 cannot be played without a
gamepad. Both maps bind `<Gamepad>` generically, so two simultaneous gamepads are
not distinctly assigned; this is a known limitation. This has **not** been tested
with physical hardware here.

Local two-player simulation is not online multiplayer. Fusion is not installed
and was intentionally not integrated.

## Still unverified (requires hardware, builds, or Fusion)

- Real gamepad input (Player 2); local two-player session.
- Root Cage restrain + Ember Moth ignition in the GUI (automated tests only).
- Encounter-cleared / all-players-defeated overlays and `R` restart in the GUI.
- Long-session exception soak; frame-rate consistency at 30/60/144 FPS.
- URP visual quality under scrutiny.
- macOS and Windows standalone builds.
- Online multiplayer (Photon Fusion 2 is not installed).

## Build status

No build was produced. The pinned editor `6000.0.84f1` has only
**WindowsStandaloneSupport**; **Mac Build Support (IL2CPP) is not installed**, so
a macOS player cannot be built with it. The `6000.6.4f1` editor *does* include
`MacStandaloneSupport`, but building with it would migrate the project off the
pinned version. To build a macOS player without migrating, add **Mac Build
Support (IL2CPP)** to `6000.0.84f1` via Unity Hub. Windows build/runtime
validation remains pending.

## Blockers

1. **Mac build module missing** for `6000.0.84f1` (manual Hub install required).
2. **No gamepad connected** in this environment, so player 2 input and
   two-player local play are untested.
3. **Photon Fusion 2 not installed, no App ID.** Online play is not implemented.
4. Local prototype checkpoint committed; generated assets, `.meta`, and
   `ProjectSettings` are now tracked.

## Next smallest milestone

Milestone 2: two separate instances create/join a Photon Fusion **Host Mode**
session, each controlling its own creature with consistent replicated combat
state. The integration plan, authority model, and prediction approach (documented
when the milestone starts in `docs/ARCHITECTURE.md`) must be agreed before
substantial code. Local two-player and gamepad should be validated first if
hardware is available.
