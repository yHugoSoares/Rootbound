# Testing

> Branding note: this game is **Duatborn**; historical titles "Rootbound: Fractured Realms" and "Dawnkeepers: Gates of Duat" are superseded. Internal identifiers, namespaces, assemblies, and `Assets/Rootbound` keep the old name (`docs/CREATIVE_DIRECTION.md`).

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

Result: **Passed: 75, Failed: 0, Skipped: 0, Total: 75.**

### 2. Unity import and compile

`-batchmode -nographics` import/compile returned EXIT=0 with no `error CS`.
The EditMode and PlayMode runs below recompiled the full project, so the current
code (including the cooldown fix) compiles in Unity.

### 3. Unity EditMode tests

```
Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode \
  -testResults /tmp/rb-edit.xml
```

Result: **total=75, passed=75, failed=0, skipped=0.**

### 4. Unity PlayMode tests

```
Unity -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode \
  -testResults /tmp/rb-play.xml
```

Result: **total=11, passed=11, failed=0, skipped=0.**

Arena / runner (`ArenaPlayModeTests`), 7:

- `ArenaSceneRunsLocalSession` - loads the generated scene and asserts wiring,
  creature spawning from ScriptableObjects, simulation stepping, Blightling
  movement, Blightling damage to a player, and no exceptions.
- `RunnerAdvancesCooldownsWithoutInput` - starts a session through the real
  `LocalGameRunner`, forces the player's primary/special/dodge cooldowns to
  non-zero, waits 1 second with no input, and asserts all three recover and
  `ElapsedTime` advances.
- `DodgePressIsLatchedAcrossFramesWithoutSimulationSteps` - drives the real
  `PlayerInputAdapter` (no devices) across a frame with no fixed step and asserts
  the discrete dodge press survives and is consumed exactly once.
- `CameraFocusesConfiguredLocalPlayer` - local player selection focuses the
  camera on that player.
- `SoloRunnerSpawnsOneSelectedCreatureAndCycles` - solo spawns exactly the
  selected creature and completes a cycle.
- `SoloPauseStopsLocalSimulation` - `Esc` pauses the offline simulation.
- `OnlineHostModeIgnoresPause` - pause does not affect an online host session.

Fusion multi-peer, 4 (`FusionConnectionGateTests` x2,
`FusionMovementReplicationTests`, `FusionInstallerPatchPresenceTests`):

- `HostCreatesCodeClientJoinsAndBothLeaveCleanly` - the connection gate.
- `FreshHostJoinFailedJoinAndSoloFallback` - reconnect, failed join, solo fallback.
- `AllCombatStateReplicatesToClient` - combined replication (movement, attack,
  health, enemies, cage, ignition).
- `FusionInstallerHasMppmPatchMarker` - confirms the MPPM patch is present.

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

### Multiplayer (developer-confirmed)

The developer manually validated multiplayer in a two-peer session and reports it
working as expected: host/join by code, per-player ownership, client movement,
attack, special, dodge, damage resolved once, Root Cage placement and Ember Moth
ignition, consistent health/encounter outcome, ground-pickup upgrade collection,
and returning to the menu. Recorded as confirmed on the developer's report. No
specific machine, build, or network conditions were captured, and no latency or
packet-loss measurement was taken. The automated Multi-Peer tests
(`FusionConnectionGateTests`, `FusionMovementReplicationTests`) remain as
regression coverage.

Not validated - do not describe these as working:

- [ ] Real gamepad input (Player 2).
- [ ] Same-machine local two-player session (Player 2 requires a gamepad).
- [ ] No repeated exceptions across a long manual session.
- [ ] Movement consistency at 30/60/144 FPS.
- [x] macOS (arm64) and Windows (x64) standalone builds **produced** (`v0.1.0`).
- [ ] Launched/played either standalone build (runtime untested).
- [ ] Self-hosted GitHub Actions build (workflow added, never run).
- [ ] URP visual quality / no missing shaders under scrutiny.

## Solo mode (Milestone 2 addition)

Genuine offline singleplayer uses the existing offline runner
(`LocalGameRunner` + `OfflineNetworkSession`) with **one** player spec — no Fusion
runner, no Photon connection, no App ID required. Same `Rootbound.Core` combat
rules as co-op; no separate rule set. Player-hosted co-op (Fusion Host Mode) is
unchanged and extended, not replaced.

Architecture decision: reuse the existing offline runner rather than Fusion
`GameMode.Single`. `GameMode.Single` would still spin up a `NetworkRunner` and
require weaving/config for no lifecycle saving, whereas the offline runner
already exists and needs no connection. Both paths share `CombatSimulation`.

New automated tests:

- Core (also runs via `dotnet`): `SoloModeTests` — exactly one player, selected
  creature, encounter completion with one player, defeat + restart, solo primary
  damage.
- Unity PlayMode: `SoloRunnerSpawnsOneSelectedCreatureAndCycles`,
  `SoloPauseStopsLocalSimulation`, `OnlineHostModeIgnoresPause`.

Actual results (all executed with the Editor closed):

- `dotnet test` -> **75 passed, 0 failed** (run loop, upgrades, pickups, snapshots).
- Unity EditMode -> **75 passed, 0 failed, 0 skipped**.
- Unity PlayMode -> **11 passed, 0 failed, 0 skipped** (7 arena/solo + 2 connection gate + 1 combined replication + 1 MPPM patch).

## Connection gate (Fusion Multi-Peer)

Method: **Fusion Multi-Peer** — a host and a client `NetworkRunner` in one
process, over the real **Photon Cloud, GameMode.Host** transport (no macOS build,
no MPPM). Chosen over MPPM because the session layer is a thin `NetworkRunner`
wrapper and Multi-Peer is headless and automatable. Tests:
`FusionConnectionGateTests`.

Observed in a passing run (Automated, `/tmp/rb11-play.xml`):

| Gate | Evidence | Result |
| --- | --- | --- |
| 1. Host creates explicit session code | `host.SessionCode` non-empty after connect | pass |
| 2. Second peer joins that code | client reaches `Connected` via `Join(code)` | pass |
| 3. Host sees second connection | `host.PlayerCount >= 2` | pass |
| 4. Client leaves cleanly | client `Leave()`; host `PlayerCount <= 1` | pass |
| 5. Host ends cleanly | host `Leave()` -> `Offline` | pass |
| 6. Fresh host/join after shutdown | new host/client connect after full shutdown + settle delay | pass |
| 7. Failed join returns cleanly | `Join("RB-ZZZZ")` -> `Error` (`GameNotFound`, ErrorCode 32758) | pass |
| 8. Solo available after failures/exits | `OfflineNetworkSession.StartHost()` -> `Hosting` | pass |

The failed join logs `[Fusion] StartGame Failed ... GameNotFound`; the test
expects that error log. This is not a substitute for two-machine testing.

## Increment 1: ownership + movement replication

Host-authoritative: the host owns the single `CombatSimulation` and steps it in
`FixedUpdateNetwork`; it maps `PlayerRef -> player index` by join order and reads
each client's `RootboundInput`. Movement/facing are replicated as `[Networked]`
vectors; clients apply them to a local mirror and focus their camera on their own
player. No prediction. Match object: `Assets/Rootbound/Prefabs/FusionMatch.prefab`
(`NetworkObject` + `FusionCombatHost`, labeled `FusionPrefab`).

Test `FusionMovementReplicationTests.AllCombatStateReplicatesToClient` (**pass**,
`/tmp/rb28-play.xml`) uses a **single** host/client connection and checks, in
order: host movement reaches the client proxy; a host primary attack damages an
adjacent enemy once and the health replicates; player health replicates; a host
Root Cage replicates (position); the Ember Moth ignition replicates
(`IsIgnited`). One connection avoids repeated Multi-Peer shutdown/reconnect
flakiness.

Online lobby/controls:
- The match starts as a **lobby with only the connected players** (no enemies);
  the **host presses R** to start (and to restart after clear/defeat). Any player
  can also request a restart; the host executes it.
- **Dynamic roster**: the lobby shows one creature until Player 2 joins, then a
  second slot is added using **that player's own selected creature** (not a fixed
  second creature). Creature selection is sent in `RootboundInput.Creature`.
- The `EncounterStarted` flag, player count, creatures and enemy count replicate,
  so peers rebuild their mirror on each transition.
- The result overlay ("Press R to run it back") is shown per local player: a
  defeated player does not see it when a teammate cleared the encounter.
- Online **dodge** is captured in `FusionNetworkSession.Update` via
  `PlayerInputAdapter.CaptureFrame` (previously only `LocalGameRunner` did this,
  so online dodges were never latched).
- The combined replication test asserts the lobby has zero enemies, that
  `StartEncounter()` replicates, and that ignition replicates with Player 2 as
  Ember Moth.

Player/enemy/cage state uses `[Networked] NetworkArray<...>`; clients apply it in
`Render` (the proxy's `FixedUpdateNetwork` is not reliably invoked in Multi-Peer).

Disconnect handling lives in `FusionNetworkSession` (`OnPlayerLeft`,
`OnDisconnectedFromServer`) and the menu returns to the menu; combat object
despawn is Fusion-managed. Not separately asserted in a GUI. A client-originated
attack path is plumbed but not separately asserted.

### Solo manual checklist (run with internet disconnected)

- [ ] Menu shows **Play Solo**, **Host Co-op**, **Join Co-op**, and **Local Co-op** separately.
- [ ] Select Root Guardian, then Play Solo: exactly one creature spawns, no idle second player.
- [ ] Repeat selecting Ember Moth; the solo creature is Ember Moth.
- [ ] Movement, primary, Q targeting/clamping, dodge, cooldowns, defeat, `R` restart all work solo.
- [ ] Camera and HUD show only the solo player.
- [ ] Clearing all Blightlings completes the encounter with no teammate present.
- [ ] The cage + Ember ignition is not required to clear.
- [ ] `Esc` pauses the solo simulation; `Esc` resumes; HUD shows PAUSED.
- [ ] Disconnect Wi-Fi and confirm Play Solo still starts.
- [ ] Mode transitions: Solo -> menu -> Solo; Solo -> menu -> Host; failed Join -> menu -> Solo; online disconnect -> menu -> Solo.

## Build status

macOS (arm64, unsigned) and Windows (x64) players were built locally with
`6000.0.84f1` (which now has **MacStandaloneSupport** installed) and published as
`v0.1.0`. The builds have **not been launched/played**, so runtime is unverified.
See `docs/HANDOFF.md` ("Standalone validation").

## Known test gaps

- No visual/GUI assertion (rendering, camera framing, HUD layout).
- Real device input (keyboard, mouse, gamepad) is not exercised; PlayMode tests run
  without physical devices.
- No Fusion prediction/reconciliation tests.
