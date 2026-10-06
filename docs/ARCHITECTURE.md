# Architecture

> Branding note: this game is **Duatborn**; historical titles "Rootbound: Fractured Realms" and "Dawnkeepers: Gates of Duat" are superseded. Namespaces, assemblies, and the `Assets/Duatborn` path use the Duatborn name; the migration is recorded in `docs/RENAME_PLAN.md`.

Describes the boundaries actually implemented in Milestone 1.

## Assembly boundaries

| Assembly | Location | Engine refs | Responsibility |
| --- | --- | --- | --- |
| `Duatborn.Core` | `Assets/Duatborn/Scripts/Core` | **No** (`noEngineReferences: true`) | Authored definitions, runtime rules, authoritative simulation. |
| `Duatborn.Unity` | `Assets/Duatborn/Scripts/Runtime` | Yes | Input capture, fixed-step hosting, presentation, HUD, session abstraction. |
| `Duatborn.Editor` | `Assets/Duatborn/Scripts/Editor` | Editor only | Reproducible definition-asset and scene generation. |
| `Duatborn.Tests.EditMode` | `Assets/Duatborn/Tests/EditMode` | Editor only | NUnit tests over `Duatborn.Core`. |

`Duatborn.Core` is a plain C# library. It is compiled unmodified by both Unity
and `Tools/CoreTests/CoreTests.csproj`, so the automated tests exercise the same
domain code Unity runs.

## Layers and flow

```
Input (Unity Input System)
      -> PlayerInputAdapter  (world-space PlayerCommand)
      -> CombatSimulation.SubmitCommand(playerId, command)
      -> CombatSimulation.Step()            <-- authoritative gameplay
      -> Simulation events + read-only state
      -> CombatView / CombatHud / IsometricCameraRig   (presentation only)
```

The simulation owns all mutable gameplay state: health, cooldowns, ability
phases, dodge state, projectiles, cages, enemy AI and end conditions. Unity
never writes simulation state directly.

### Authored vs runtime

- **Authored definitions** (`CreatureSpec`, `AttackSpec`, `SpecialSpec`,
  `DodgeSpec`, `EnemySpec`) are immutable data. In Unity they live in
  `CreatureDefinitionAsset` / `EnemyDefinitionAsset` ScriptableObjects and are
  projected into the core unchanged. Defaults live in `DefaultContent`.
- **Runtime state** (`PlayerState`, `EnemyState`, `ProjectileState`,
  `RootCageState`, `Health`, `Cooldown`, `DodgeState`) lives only in the
  simulation instance. Cooldowns, targets and ownership are per-entity.
- ScriptableObjects contain **no** per-player mutable state.

### Simulation coordinates and units

- Gameplay is a 2D plane. `Vec2 (X, Y)` maps to Unity `(x, 0, z)`.
- Units: metres, seconds, metres/second, degrees. Field names state their unit
  context (e.g. `MoveSpeed`, `Distance`, `Cooldown`).
- Fixed tick rate (default 60 Hz). `CombatSimulation.DeltaTime = 1 / TickRate`.

### Tick order (`CombatSimulation.Step`)

1. `ApplyPlayerCommands` - validate ownership/alive, update facing, begin dodge/primary/special.
2. `UpdatePlayerMovement` - kinematic movement unless dodging or ability-locked.
3. `UpdateDodges` - locked-direction displacement, i-frame window.
4. `UpdateAbilities` - advance windup/active/recovery; resolve effects on the active transition.
5. `UpdateCages` - advance duration, consume fire ticks, expire.
6. `UpdateEnemies` - nearest living player, restrain scaling, attack telegraph, movement.
7. `UpdateProjectiles` - travel, lifetime, hit detection.
8. `ClampEntities` - arena bounds.
9. `EvaluateEndStates` - encounter cleared / all players defeated, raised once.

## Combat rules implemented

- **Damage**: `Health.ApplyDamage` clamps at zero and reports defeat exactly once.
  Further damage on a defeated entity returns 0.
- **Defeat**: a single `EntityDefeated` event on the zero crossing.
- **Cooldowns**: `Cooldown.TryStart` fails while `Remaining > 0`; ticks clamp at zero.
- **Abilities**: windup -> active (effect fires once) -> recovery. Dodging cancels windup/recovery.
- **Target filtering**: `TargetRules.CanDamage` forbids friendly fire; melee uses an arc test.
- **Dodge i-frames**: `DodgeState.IsInvulnerable` is only true inside `[InvulnStart, InvulnEnd]`.
- **Binding Seal**: aimed circle; enemies inside move at `speed * (1 - RestrainFactor)` and
  cannot attack at full restrain. `TryIgnite` adds burn time but clamps total burn to
  `MaxIgnitionDuration`, so repeated ignitions cannot stack without a cap. Fire damage
  ticks on `FireTickInterval` while the cage is active and ignited.

## The co-op interaction

Sunwing's `IgnitionBurst` is resolved by the simulation:

1. It deals its direct damage to enemies inside the burst radius.
2. For each active Binding Seal whose circle overlaps the burst, it calls `TryIgnite`.
3. `TryIgnite` returns true only when burn time was actually added. At the cap it
   returns false, so the `CageIgnited` event and the visual state change fire once
   and repeated casts cannot inflate damage.
4. `CombatHud` shows the cage's remaining duration and remaining burn time;
   `CombatView` recolours the cage when ignited.

This resolves on the host-authoritative timeline, not from a client-side effect.

## Authority and the Fusion seam

The simulation is deliberately shaped for host authority:

- Clients only produce `PlayerCommand` values.
- The host (currently `LocalGameRunner`) owns the simulation and resolves all
  gameplay-affecting interactions.
- Presentation reads confirmed state after `Step()`.

`INetworkSession` (`Duatborn.Unity`) is the intended seam for a Fusion runner.
`OfflineNetworkSession` is the only implementation today; it reports that online
join requires Fusion. A Fusion integration would submit commands into the same
`CombatSimulation` from `FixedUpdateNetwork` and replicate state; it must not
move combat into `Update` timers, and presentation must skip effects while
`Runner.IsResimulating`. See `docs/DECISIONS.md`.

## Milestone 2: Photon Fusion 2 (Host Mode)

Status: **implemented.** Fusion 2.1.3 is imported at `Assets/Photon/Fusion`, an
App ID is configured locally (git-ignored), and host/join, ownership, and combat
replication are implemented and covered by Multi-Peer PlayMode tests. See
`docs/HANDOFF.md` for the verified checklist. The research and mapping notes below
record the design that was followed.

### Version research (from Photon's official download page)

- Fusion **2.0** line: latest stable **2.0.13, build 2379**.
- Fusion **2.1** line: latest stable **2.1.3, build 2390**.
- Distributed as a `.unitypackage` from `downloads.photonengine.com` (sign-in
  required), not a UPM registry package; an App ID comes from the Photon
  dashboard. Credentials must never be committed (`*.fusionappid`, `secrets/`,
  `.env` are ignored).
- Unity support (official requirements): **`2021.3.45`, `2022.3.45`, `6.0.x`,
  `6.3.x`**. The pinned editor `6000.0.84f1` is Unity 6.0 LTS, so it is
  officially supported.
- Asset Serialization must be **Force Text**; this project already is.
- Step-by-step install and App ID configuration: `docs/FUSION_SETUP.md`.

### Mapping onto the existing architecture

- The host runs one `CombatSimulation`; it stays engine-agnostic.
- `FusionNetworkSession : INetworkSession` replaces `OfflineNetworkSession` when
  online; `DuatbornMenu` is unchanged.
- A `NetworkBehaviour` (for example `FusionCombatRunner`) owns the simulation and
  drives it from `FixedUpdateNetwork`, one `Step` per Fusion tick.
- The local camera focuses the controlling player through
  `IsometricCameraRig.SetLocalPlayer(id)`; offline keeps the centroid (`-1`).

### Authority

- Host authoritative for spawning, enemy AI, damage, cooldowns, cage ignition,
  and encounter completion.
- Clients never mutate `CombatSimulation`; they produce `PlayerCommand` only.
- Validation stays in the simulation; the existing accepted/rejected reason is
  the host's decision and can be surfaced on clients.

### Client input

- Each client reads its local `PlayerInputAdapter`. Continuous values
  (`Move`, `Aim`) travel through Fusion input; discrete presses (`Dodge`) keep the
  existing latch so a press is not lost between render frames and ticks.
- The adapter already carries an optional world `TargetPoint`; it is sent with the
  command so the host places aimed abilities at the intended location.

### Replicated state

- Players: position, facing, health/defeated, cooldown `Remaining`/`Duration`,
  ability phase, dodge state.
- Enemies: position, facing, health/defeated, attack cooldown.
- Cages: position, radius, remaining duration, ignited/burn.
- Projectiles: id, position, lifetime.
- Spawn configuration is replicated once. Decorative effects are derived locally
  from replicated state and never sent.

### Prediction and resimulation (limitations)

- Host authority with Fusion `[Networked]` state; the owning client may predict
  its own creature's movement/dodge for feel.
- Prediction is limited to local movement/dodge. Damage, defeat, and cage
  ignition stay host-confirmed to prevent duplicated resolution.
- Do not assume cross-machine floating-point determinism. The simulation is
  plain kinematic math, but correctness comes from host state replication, not
  from identical client results.
- Presentation must skip effects while `Runner.IsResimulating` so particles and
  audio do not replay during correction.
- No host migration (explicitly out of scope for this milestone).

### Explicitly not doing

- No second networking framework, no custom transport, no fake/stub networking
  layer, and no per-frame RPC of all state.

## Presentation

- `IsometricCameraRig`: fixed 35 deg pitch / 45 deg yaw, orthographic, follows the
  living-player centroid. Cameras are local; transforms are never networked.
- `CombatView`: procedurally created placeholder primitives, synced from
  simulation state each frame. Decorative effects are derived from replicated
  state, not sent over the network.
- `CombatHud`: IMGUI placeholder health/cooldown/encounter readout.
- `DuatbornMenu`: minimal host/join/status using `INetworkSession`.

## Deliberate omissions

- No ECS/DOTS, no custom engine, no second networking framework, no ability
  scripting language, no global service locator. Components self-wire via
  `GetComponent` at Awake; there is no `Update`-time scene-wide search.
