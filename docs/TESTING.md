# Testing

## Environment used for these runs

- macOS 26.7.1, Apple Silicon (arm64).
- Unity Editor **`6000.0.84f1`** (arm64). A second editor `6000.6.4f1` is
  installed but was **not** used (it would migrate the project).
- .NET SDK 10.0.300 (for the pure-core test project).
- Unity runs are headless: `-batchmode -nographics`. No GUI or visual inspection.

## What was actually executed

### 1. Pure core tests (`dotnet`)

```
dotnet test Tools/CoreTests/CoreTests.csproj
```

Result: **Passed: 47, Failed: 0, Skipped: 0, Total: 47.**

### 2. Unity import and compile

`-batchmode -nographics` import/compile returned EXIT=0 with no `error CS`.
The EditMode and PlayMode runs below recompiled the full project, so the current
code (including the cooldown fix) compiles in Unity.

### 3. Unity EditMode tests

```
Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode \
  -testResults /tmp/rb-edit.xml
```

Result: **total=47, passed=47, failed=0, skipped=0.**

### 4. Unity PlayMode tests

```
Unity -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode \
  -testResults /tmp/rb-play.xml
```

Result: **total=3, passed=3, failed=0.**

- `ArenaSceneRunsLocalSession` - loads the generated scene and asserts wiring,
  creature spawning from ScriptableObjects, simulation stepping, Blightling
  movement, Blightling damage to a player, and no exceptions.
- `RunnerAdvancesCooldownsWithoutInput` - starts a session through the real
  `LocalGameRunner`, forces the player's primary/special/dodge cooldowns to
  non-zero, waits 1 second with no input, and asserts all three recover and
  `ElapsedTime` advances. This is the integration test through the
  `LocalGameRunner` path.
- `DodgePressIsLatchedAcrossFramesWithoutSimulationSteps` - drives the real
  `PlayerInputAdapter` (no devices) across a frame with no fixed step and asserts
  the discrete dodge press survives and is consumed exactly once.

These runs execute the real scene at runtime but are not visual/GUI checks.

## Root cause and regression evidence

**Root cause:** `CombatSimulation.Step` advanced ability phases (`AbilityExecution.Tick`),
dodge (`DodgeState.Tick`), cages and projectiles, but **never called `Cooldown.Tick`
on any cooldown**. Every cooldown (player primary/special/dodge and enemy attack)
therefore froze at its configured duration after first use. Movement was
unaffected, which is why WASD worked while combat appeared broken.

**Evidence the regression tests catch it:** with the single `UpdateCooldowns(dt)`
call temporarily removed, the suite reported **Failed: 6, Passed: 28, Total: 34** -
exactly the six new tests below. With the fix restored: **34/34**.

New tests:

- `CooldownsAdvanceWithoutNewInput`
- `SpecialCanReactivateAfterCooldown`
- `DodgeEndsAndCanReactivateAfterCooldown`
- `EnemyDamagesSameTargetOnMultipleAttackCycles`
- `DodgeBlocksDamageDuringWindowThenTakesDamageAfter`
- `StatePersistsAcrossSuccessiveTicks`

## Second review round: targeting, special repeat, dodge latch

- **Special targeting (fixed):** `PlayerCommand` carried only a direction, and
  `ResolveSpecial` placed the cage at `Position + Facing * CastRange` (always max
  range). The command now carries an optional `TargetPoint`; the simulation
  snapshots it on the accepted cast, clamps it to `CastRange` preserving
  direction, and places the cage there. `PlayerState.AimTarget/AimTargetClamped`
  expose the marker position. Dev-only aim markers show the actual (clamped)
  landing point.
- **"Special only once" (explained):** there was no single-cage rule; the cage
  expires at 3.5s while the special cooldown is 5.5s, and rejections were
  invisible, so a press in that gap silently did nothing. Repeat casts are proven
  to work (`SpecialCanReactivateAfterCooldown`, `CageExpiresThenNewCastCreatesSecondCage`,
  `DiscreteSpecialPressCastsOnce`), and rejections now show the exact reason
  (e.g. `Special: cooldown 2.31s`).
- **Intermittent dodge (fixed):** `WasPressedThisFrame()` was sampled only inside
  `PlayerInputAdapter.Build`, which runs only on fixed steps, so any frame with no
  step dropped the press. `LocalGameRunner` now calls `CaptureFrame` once per
  rendered frame and the adapter latches the press until a `Build` consumes it.
  Direction rule: move input direction if moving, otherwise facing.

## Third review round: dodge counter display

The counter reads `PlayerState.DodgeCooldown.Remaining` (live authoritative state)
and is rendered by `CooldownDisplay.Label`. `OnGUI` only reads; only
`CombatSimulation.Step` advances timers. Defects fixed:

- **Culture-sensitive formatting:** `ToString("0.0")` rendered `0,9` on a
  comma-decimal locale. All cooldown formatting now goes through
  `CooldownDisplay` with `CultureInfo.InvariantCulture`.
- **`0.0` shown before ready:** one-decimal rounding displayed `0.0` for up to
  ~0.05 s while a press was still rejected. `CooldownDisplay.Label` rounds
  **up** to the next tenth and never shows `0.0` before `IsReady`.
- **Overlap:** the rejection line sat on top of the cooldown line and the next
  player's name; player block spacing was increased.
- **Input-gated updates:** `ProjectSettings` had `runInBackground: 0`, so Editor
  play paused when the window lost focus and the counter appeared to only update
  while interacting. Set to `1`.

New tests: `CooldownDisplayTests` (ready, ceiling mapping, no `0.0` before ready,
rejected `TryStart` does not change the value, normalized clamp) and
`DodgeCooldownDisplayTracksSimulationState`.

## Automated coverage map

| Required check | Test(s) | Status |
| --- | --- | --- |
| Damage cannot reduce health below zero | `HealthTests.DamageCannotReduceHealthBelowZero` | pass |
| Defeat triggers only once | `HealthTests.DefeatIsReportedOnceAndFurtherDamageIsIgnored`, `CombatSimulationTests.EnemyDefeatRaisesSingleEvent` | pass |
| Abilities cannot activate during cooldown | `CooldownTests.AbilityCannotActivateDuringCooldown`, `PlayerCooldownsAreIndependent` | pass |
| Cooldowns advance without input | `CooldownsAdvanceWithoutNewInput` | pass |
| Special re-activates after cooldown | `SpecialCanReactivateAfterCooldown` | pass |
| Dodge ends and re-activates after cooldown | `DodgeEndsAndCanReactivateAfterCooldown` | pass |
| Dodge i-frames, then damage after | `DodgeTests.InvulnerabilityOnlyWithinWindow`, `DodgeBlocksDamageDuringWindowThenTakesDamageAfter` | pass |
| Enemy damages same target across cycles | `EnemyDamagesSameTargetOnMultipleAttackCycles` | pass |
| State persists across ticks | `StatePersistsAcrossSuccessiveTicks` | pass |
| Invalid / friendly targets rejected | `TargetRulesTests.*`, `IgnitionBurstDoesNotDamageAllies` | pass |
| Root Cage ignition stacking and duration | `RootCageTests.*`, `IgnitionBurstIgnitesCageAndFireDealsDamage` | pass |
| Cage restrains enemies | `RootCageRestrainsEnemy` | pass |
| Encounter completion / restart | `EncounterClearedAfterAllEnemiesDefeated`, `RestartRestoresInitialState` | pass |
| Defeated players ignore commands | `DefeatedPlayerCommandsAreIgnored` | pass |
| Runner-path cooldown recovery | `RunnerAdvancesCooldownsWithoutInput` (PlayMode) | pass |
| Special uses requested target point in range | `SpecialUsesRequestedTargetPointWhenInRange` | pass |
| Target clamped to cast range keeping direction | `SpecialTargetIsClampedToCastRange` | pass |
| Cage expiry then a new cast | `CageExpiresThenNewCastCreatesSecondCage` | pass |
| Discrete press casts once | `DiscreteSpecialPressCastsOnce` | pass |
| Dodge direction moving vs stationary | `DodgeDirectionUsesMoveInputWhenMoving`, `DodgeDirectionUsesFacingWhenStationary` | pass |
| Dodge rejected with reason, then recovers | `DodgeRejectedDuringCooldownThenRecovers` | pass |
| Discrete dodge press latched across frames | `DodgePressIsLatchedAcrossFramesWithoutSimulationSteps` (PlayMode) | pass |
| Cooldown label: ready / ceiling / no 0.0 before ready | `CooldownDisplayTests.*` | pass |
| Rejected start does not change displayed value | `CooldownDisplayTests.RejectedStartDoesNotChangeDisplayedValue` | pass |
| Dodge label tracks simulation and returns to ready | `DodgeCooldownDisplayTracksSimulationState` | pass |

## Diagnostics overlay

`CombatHud` draws a development-only panel (hidden by default; toggle **F1** once
per press; hidden in release builds) showing: tick, elapsed simulation time, timestep, `Time.timeScale`, each
player's health/defeated state, primary/special/dodge remaining cooldowns, dodge
active/invulnerable, enemy attack cooldown, and the last requested action with its
acceptance/rejection reason. The simulation records that reason in
`PlayerState.LastRequestedSlot/LastRequestAccepted/LastRequestReason/LastRequestTick`.
No per-frame Console output is produced.

## Running the tests

```
dotnet test Tools/CoreTests/CoreTests.csproj
```

```
"/Applications/Unity/Hub/Editor/6000.0.84f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -projectPath "$(pwd)" \
  -runTests -testPlatform EditMode -testResults /tmp/rb-edit.xml
```

GUI: `Window > General > Test Runner`, run the **EditMode** and **PlayMode** tabs.
Do not run batchmode while the Editor has the project open.

## Manual validation

Performed by the developer in the Unity Editor on this Mac. `[x]` = confirmed by
manual play; `[ ]` = **not** validated.

Confirmed:

- [x] Keyboard/mouse: WASD movement and mouse aiming.
- [x] Primary attack deals damage; cooldown counts down; can attack again.
- [x] Cursor targeting and range clamping (marker at cursor; clamped landing point).
- [x] Repeated Q casts after cooldown; cage spawns at the marker.
- [x] Moving and stationary dodge; executes and recovers.
- [x] Cooldown display counts down and returns to ready; rejection feedback shown.
- [x] F1 diagnostics toggle (hidden by default; toggles once per press).
- [x] Repeated enemy damage over time and live HUD updates.

Not validated - do not describe these as working:

- [ ] Real gamepad input (Player 2).
- [ ] Local two-player session (Player 2 requires a gamepad).
- [ ] Root Cage restrain + Ember Moth ignition interaction in the GUI
      (covered by automated tests only).
- [ ] Encounter-cleared / all-players-defeated overlays and `R` restart in the GUI.
- [ ] No repeated exceptions across a long manual session.
- [ ] Movement consistency at 30/60/144 FPS.
- [ ] macOS or Windows standalone builds.
- [ ] Online multiplayer (Photon Fusion is not installed).
- [ ] URP visual quality / no missing shaders under scrutiny.

## Build status

No build was produced. The pinned editor `6000.0.84f1` has only
**WindowsStandaloneSupport**; **Mac Build Support (IL2CPP) is missing**, so a
macOS player cannot be built with it. `6000.6.4f1` has Mac support but would
migrate the project. See `docs/HANDOFF.md`.

## Known test gaps

- No visual/GUI assertion (rendering, camera framing, HUD layout).
- Real device input (keyboard, mouse, gamepad) is not exercised; PlayMode tests run
  without physical devices.
- No Fusion prediction/reconciliation tests.
