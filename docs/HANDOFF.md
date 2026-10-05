# Handoff

Milestone 1 (local combat arena) has now been **imported, compiled, and run
headlessly** on the Mac with Unity `6000.0.84f1`. This document records exactly
what was verified, what was fixed, and what still requires the GUI or hardware.
Platform policy is in `docs/ENVIRONMENT.md`.

> Naming: the game is branded **Duatborn** (no subtitle). Historical titles
> "Rootbound: Fractured Realms" and "Dawnkeepers: Gates of Duat" are superseded.
> Internal identifiers, namespaces, assemblies, build method names, and the
> `Assets/Rootbound` path keep the "Rootbound" name; see
> `docs/CREATIVE_DIRECTION.md`.

## Current status (Milestone 2 closeout)

- Unity compile: **succeeded** with Photon Fusion imported (no `error CS`).
- Latest full run (Editor closed, Milestone 3): core **73/73**, Unity EditMode
  **73/73**, Unity PlayMode **11/11**.
- **Multiplayer manually validated** by the developer in a two-peer session
  (host/join, ownership, client combat, cage/ignition, ground pickups, return to
  menu). No machine/build/network-condition measurements were captured.
- PlayMode includes a **connection gate** (host code, join, host sees peer, clean
  leave/shutdown, fresh host/join, failed join, solo fallback) and one combined
  **combat replication** test (movement, attack, health, enemies, cage,
  ignition), over Photon Cloud Host Mode via Fusion Multi-Peer.
- Offline: solo (Root Guardian/Ember Moth) and local two-player co-op remain.
- Photon Fusion 2.1.3 imported; **App ID is set locally** (git-ignored; not
  printed). Host/join connects.
- MPPM 1.6.3 installed; the Fusion installer patch lets virtual players start
  (confirmed in `Library/VP/mppm*/Logs/Editor.log`).

### What actually passed (recorded separately)

| Item | Status | Evidence |
| --- | --- | --- |
| MPPM activation | **Passed** (manual) | User ran two MPPM windows; VP log shows the patched `Fusion: no manifest.json (MPPM virtual project)` skip, no `FileNotFoundException`. |
| Automated connection/replication | **Passed** | EditMode 54/54, PlayMode 11/11 (Multi-Peer, Photon Cloud Host Mode). |
| Manual two-peer GUI checks | **Partially observed** | User confirmed peers connect, Player 2 moves, dodge works. Lobby/roster/per-player-creature/aim-marker fixes came **after** that run and are not yet re-confirmed on two windows. |
| Standalone builds | **Not produced** | Pinned `6000.0.84f1` has only `WindowsStandaloneSupport`; Mac Build Support (IL2CPP) is not installed. See "Standalone validation". |
| Two-machine tests | **Not tested** | Requires a second machine; steps provided. |

Do not read "the virtual player starts" as "multiplayer passed": activation is a
prerequisite; the combat replication evidence above is separate, and two-window
combat is only automated-proven (one-process Multi-Peer), not manually re-run.

### Multiplayer checklist (B)

| Item | Status | How |
| --- | --- | --- |
| Host/join by code | Passed | `FusionConnectionGateTests` (Multi-Peer, Photon Cloud). |
| Player ownership (each controls own) | Passed | Host maps `PlayerRef -> index`; roster test. |
| Client-originated movement | Partial | Input path plumbed; automated test drives the host override, client input not separately asserted. Manual: move Player 2. |
| Client-originated attack / special | Partial | Same; host resolves. Manual: press LMB/Q on the client. |
| Client-originated dodge | Passed (manual) | User confirmed dodge works online after the latch fix. |
| Damage resolved once | Passed | Attack test: one activation damages once; health replicates. |
| Cage placement + client ignition | Passed | Combined test: cage replicates, ignition replicates. |
| Consistent health/cooldowns/outcome | Partial | Health replicated; cooldowns are host-only (not displayed remotely yet); encounter outcome via replicated enemy/defeat state. |
| Client leave / host leave / fresh session | Passed | Connection gate. |
| Return to offline solo after shutdown | Passed | Gate asserts `OfflineNetworkSession` still hosts. |

### Standalone validation (D)

**No build exists.** The pinned editor `6000.0.84f1` has only
`WindowsStandaloneSupport`; **Mac Build Support (IL2CPP)** is not installed.

Exact steps (do not upgrade Unity):
1. Unity Hub -> Installs -> `6000.0.84f1` -> gear -> *Add modules* ->
   **Mac Build Support (IL2CPP)** (and *Mac Build Support (Mono)* if offered).
2. `File > Build Settings` -> platform **macOS** -> enable **Development Build**
   -> *Build* to `Builds/DuatbornDev.app`.
3. The App ID is compiled in from `PhotonAppSettings.asset` (client-side); do not
   print it. Ensure `Assets/Rootbound/Prefabs/FusionMatch.prefab` is included
   (it is referenced by `Runner.Spawn("FusionMatch", ...)`).
4. Test Editor host -> standalone client, then reverse roles.
5. Two-machine test: copy the build (or build on the second machine) and join the
   same session code over the internet.

The `6000.6.4f1` editor has `MacStandaloneSupport`, but using it would migrate the
project off the pinned version — not done.

### Network feel (E)

Fusion's supported simulator is `NetworkSimulationConfiguration`, exposed via
`NetworkProjectConfig.NetworkConditions` and `StartGameArgs.Config` (fields:
`Enabled`, `DelayMin/Max`, `LossChanceMin/Max`, `AdditionalJitter`,
`AdditionalLoss`). To use it: set `NetworkConditions` in
`Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion` (or pass a cloned
config in `StartGameArgs.Config`), then host/join.

Status: **not yet collected.** No configured conditions were run, so there are no
observed numbers for correctness, remote-state smoothness, or local input
responsiveness. Remote state is applied as-is (host-confirmed); **this is not
prediction** and no prediction is implemented. Do not claim interpolation.

### CI (GitHub Actions)

`.github/workflows/build.yml` uses `game-ci/unity-builder@v4` to build
**StandaloneOSX** and **StandaloneWindows64** on `master` (and manual dispatch),
when Unity license secrets are configured. It uploads per-platform artifacts
`Duatborn-macOS` / `Duatborn-Windows`, and on `v*` tags creates a GitHub Release
with versioned archives. If no license is configured, the build is skipped with a
warning (the `secrets` context is checked in a step, not a job-level `if`, which
GitHub rejects). Builds output `build/<target>/Duatborn.app` (macOS) and
`build/<target>/Duatborn.exe` (Windows); `Unity Product Name` is `Duatborn`.

License (game-ci v4; there is **no activation workflow** any more): activate a
Personal license **locally** with Unity Hub (`Preferences > Licenses > Add > Get
a free personal license`), then use the `.ulf` file:
`/Library/Application Support/Unity/Unity_lic.ulf` (Mac),
`C:\ProgramData\Unity\Unity_lic.ulf` (Windows),
`~/.local/share/unity3d/Unity/Unity_lic.ulf` (Linux). Licenses are not tied to a
Unity version or platform. Repository secrets: `UNITY_LICENSE` (the `.ulf`
contents) + `UNITY_EMAIL` + `UNITY_PASSWORD` for Personal; `UNITY_SERIAL` +
email/password for Pro. Never commit these.

Alternative (recommended on a licensed machine): `.github/workflows/build-selfhosted.yml`
(the build target is passed via the `DUATBORN_BUILD_TARGET` environment variable)
runs on a **self-hosted macOS runner** using its locally installed and licensed
Unity editor, so **no license secrets are needed**. Setup: register the machine as
a GitHub Actions runner with the labels `self-hosted` and `macOS`; install Unity
`6000.0.84f1` with **Mac + Windows build support**; optionally add the
`PHOTON_APP_ID` secret for online builds, and a `UNITY_PATH` repository variable
if the editor isn't at the default Hub path. It invokes
`Rootbound.EditorTools.RootboundBuild.PerformBuild` (which also injects
`PHOTON_APP_ID` into `PhotonAppSettings` when set).

Known limitation: CI builds have **no Fusion App ID** because
`PhotonAppSettings.asset` is git-ignored, so they run offline/local co-op. To
enable online in CI, add a `PHOTON_APP_ID` secret and a custom `buildMethod` that
writes it into `PhotonAppSettings.Global.AppIdFusion` before building (not wired
yet, to avoid guessing game-ci's custom-method contract). Not verified: no
workflow run has been executed here; Unity builds have not been produced.

### Milestone 3 (implemented)

- Three authored rooms (Rootway 6 Blightlings, Blight Hollow 4+3 Sporelings,
  Heartwood finale 5+3), a host-authoritative run loop (advance on clear,
  victory/defeat/R to replay), and one additional enemy type (Sporeling) in
  mixed groups.
- Sporeling death burst has a world-space **expanding ring** sized to
  `EnemySpec.DeathBurstRadius` (derived on every peer from the defeated state, so
  no extra sync).
- Upgrade pool expanded to six, mixing flat stats (HP/damage/speed) with
  meaningful choices (special radius, cage duration, heal-on-kill); the host (or
  offline run) offers **three distinct random picks** per room via
  `DefaultContent.PickUpgradeIndices`.
- Upgrades between rooms are **ground pickups**: coloured cylinders with a
  world-space effect label shown when a local player is within ~3 m; collect with
  **E** (gamepad North/Y). Host-authoritative online (pickups replicated;
  picking player receives the upgrade). Offline co-op supported.
- Tests: core 68/68, EditMode 68/68, PlayMode 11/11. Online pickup collection is
  code-complete but not yet manually re-confirmed on two windows.
- Original plan: `docs/MILESTONE3_PLAN.md`.

## Milestone 2 dependency verification (Fusion import)

- **SDK version:** loaded assemblies report `Fusion.Runtime 2.1.3.0` (imported via
  `.unitypackage` at `Assets/Photon/Fusion`). The SDK's `package.json` says
  "1.1.0" (wrapper version); trust the assembly version.
- **Compilation:** the project compiles with Fusion. Import added
  `com.unity.nuget.mono-cecil: 1.10.2` to `Packages/manifest.json` and Fusion
  scripting defines plus `allowUnsafeCode: 1` to `ProjectSettings.asset`; these are
  required and were kept.
- **Transient import warnings:** `Fusion.CodeGen.ILWeaverBindings: Failed to
  locate a valid config` appeared during the import only; the latest build
  succeeds and the warning no longer appears. Not fixed (import ordering), no
  action taken.
- **Console error `ScriptableSingleton already exists. Did you query the singleton
  in a constructor?`:** full stack trace is entirely inside Unity's own Package
  Manager UI:
  `UnityEditor.ScriptableSingleton<T>:.ctor` ->
  `UnityEditor.PackageManager.UI.Internal.ServicesContainer:.ctor` and
  `...PackageManagerProjectSettings:.ctor`. It recurs once per domain reload, is
  **not caused by Rootbound or Fusion**, is benign, and has no project-side fix.
  No assets were deleted.
- **Repository handling:** `Assets/Photon` is untracked (imported after the last
  commit). `PhotonAppSettings.asset` is now git-ignored (with its `.meta`) so a
  populated App ID is never committed; each developer sets it locally. The
  Fusion-required `manifest.json` and `ProjectSettings.asset` changes are kept.
- **Blocker:** the App ID is empty, so no online test is possible yet.

## Solo mode and menu (Milestone 2 addition)

- Menu now separates **Play Solo** (with Root Guardian / Ember Moth selection),
  **Host Co-op**, **Join Co-op**, and **Local Co-op (2 players)**. A two-player
  arena is never labelled singleplayer.
- **Solo uses the existing offline runner** (`LocalGameRunner` +
  `OfflineNetworkSession`) with one player spec. No `NetworkRunner`, no Photon
  Cloud connection, and no App ID are required for Play Solo.
- Architecture decision: did **not** use Fusion `GameMode.Single`. It would add a
  Fusion runner/weave lifecycle without reducing duplication, since the offline
  runner already exists and the shared `CombatSimulation` rules are reused
  unchanged. Both solo and co-op call the same Core combat code.
- `LocalGameRunner` gained `selectedCreature` (solo), single-player camera focus
  (`SetLocalPlayer(0)`), and a `pauseAllowed`/`IsPaused` pause that only applies
  to offline modes — an online host's simulation is never paused.
- Register only if genuinely verified: no Unity suite was re-run this session
  (Editor open); `dotnet` is 52/52.

## Milestone 2 increment 1: ownership + movement (implemented, automated)

- `FusionCombatHost` (`NetworkBehaviour`): host owns the single `CombatSimulation`,
  maps `PlayerRef -> index` by join order, reads each client's `RootboundInput`,
  steps, and replicates player position/facing via `[Networked]` vectors; clients
  apply to a mirror and focus their camera on their own player.
- Match prefab `Assets/Rootbound/Prefabs/FusionMatch.prefab` (`NetworkObject` +
  `FusionCombatHost`), labeled `FusionPrefab` so Fusion registers it (editor menu
  `Rootbound > Build Fusion Match Prefab`).
- `FusionNetworkSession` spawns the match on the host and provides local input.
- Menu: online modes no longer run `LocalGameRunner`; the Fusion host binds
  presentation.
- MPPM **1.6.3** added to `Packages/manifest.json` (latest for Unity 6000.0) for
  a real second-Editor peer.
- Increment 2 adds player health and enemy position/health/defeated replication
  (`[Networked]` arrays applied on clients in `Render`).
- Increment 3 covers primary attacks: the host resolves ability damage once and
  replicates the result.
- Increment 4 covers the Root Cage and Ember Moth ignition replication.
- Online polish: a **lobby with no enemies** that the host starts with **R**
  (replicated `EncounterStarted`), and **online dodge input** (latched in
  `FusionNetworkSession.Update`, since online never runs `LocalGameRunner`).
- MPPM unblocked by a narrow Fusion `FusionInstaller` patch (see
  `docs/patches/fusion-installer-mppm.patch`).
- Verified by `FusionMovementReplicationTests.AllCombatStateReplicatesToClient`
  over Photon Cloud Host Mode (Multi-Peer) in one connection. **No prediction.**
  Disconnect handling is in `FusionNetworkSession`/menu (not GUI-asserted). Not
  manually tested on two machines.

## Milestone 2 integration issues found and fixed

- **`NetworkRunner should not be reused`** on `StartGame`: the IMGUI `OnGUI`
  multi-pass invoked the `Host` button handler more than once per click, and the
  session reused a runner. Fixed by moving session actions to `Update`
  (single call via a request flag), adding a `_busy`/`CanStart` guard, and
  disposing the runner on `Leave`/failure (`FusionNetworkSession.EnsureRunner`).
- **`RootboundInput has no attribute Fusion.NetworkInputWeavedAttribute`**:
  Fusion's IL weaver only processes assemblies listed in
  `Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion` ->
  `AssembliesToWeave` (defaults to `Assembly-CSharp`, `Assembly-CSharp-firstpass`).
  Added **`Rootbound.Fusion`**. Verified the rebuilt
  `Library/ScriptAssemblies/Rootbound.Fusion.dll` now contains
  `NetworkInputWeavedAttribute`/`NetworkAssemblyWeavedAttribute`.
- The new `Rootbound.Fusion` assembly compiles (no `error CS`); the offline arena
  assemblies remain Fusion-free via `NetworkSessionFactory`.

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
| Pure core | `dotnet test Tools/CoreTests/CoreTests.csproj` | 54 passed, 0 failed |
| Unity import/compile | `-batchmode -nographics -quit` | EXIT=0, no compiler errors |
| Unity EditMode | `-runTests -testPlatform EditMode` | 54 passed, 0 failed |
| Unity PlayMode | `-runTests -testPlatform PlayMode` | 10 passed, 0 failed |
| Connection gate | `FusionConnectionGateTests` (Photon Cloud, Host Mode, Multi-Peer) | 1 host + 1 client; gates 1-8 pass |
| Combat replication | `FusionMovementReplicationTests` | movement, attack damage, player/enemy health, cage and ignition reach the client mirror |

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
   Install Fusion 2.1.3 (Build 2390) and configure an App ID per
   `docs/FUSION_SETUP.md`; Unity 6.0.x is officially supported. `Assets/Photon`
   must not be added to version control with a populated App ID.
4. Local prototype checkpoint committed; generated assets, `.meta`, and
   `ProjectSettings` are now tracked.

## Next smallest milestone

Milestone 2: two separate instances create/join a Photon Fusion **Host Mode**
session, each controlling its own creature with consistent replicated combat
state. The integration plan, authority model, and prediction approach (documented
when the milestone starts in `docs/ARCHITECTURE.md`) must be agreed before
substantial code. Local two-player and gamepad should be validated first if
hardware is available.
