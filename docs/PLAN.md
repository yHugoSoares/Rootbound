# Duatborn (formerly Rootbound: Fractured Realms) — Implementation Plan and Assumptions

> Branding note: this game is **Duatborn**; historical titles "Rootbound: Fractured Realms" and "Dawnkeepers: Gates of Duat" are superseded. Internal identifiers, namespaces, assemblies, and `Assets/Rootbound` keep the old name (`docs/CREATIVE_DIRECTION.md`).

Status: living document. Section 1 below records the original authoring
environment (Unity was not installed then). Unity is now installed and the project
compiles and runs headlessly; for current status see `docs/HANDOFF.md`.

## 1. Environment inventory (historical — original authoring environment)

| Item | Finding |
| --- | --- |
| Repository | Empty git repo at `Rootbound/`, branch `master`, no commits. Preserved. |
| Unity Editor | **Not installed.** No `/Applications/Unity/Hub/Editor`, no `Unity.app`, no Unity Hub, no `~/Library/Application Support/Unity`. |
| Unity Hub | Not installed. |
| .NET SDK | `10.0.300`; runtime `10.0.8`. |
| Mono | Not installed. |
| OS / arch | macOS 26.7.1, arm64. |
| Network | Available (Unity docs + release API reachable). |
| Photon Fusion | Not present. No Photon account or App ID available; credentials must not be invented. |

At authoring time Unity was not installed, so the project could not be compiled
in-editor. To keep validation executable meanwhile, the gameplay domain was
written as a pure C# assembly with no `UnityEngine` dependency and is exercised by
a `dotnet test` project that compiles the *same* source files Unity uses.

**Superseded:** Unity `6000.0.84f1` and Photon Fusion 2.1.3 are now installed, the
project compiles and runs headlessly, and Milestones 1–3 are implemented. Current
status, test counts, and builds live in `docs/HANDOFF.md`.

## 2. Version decisions (recorded)

- Editor: **Unity 6.0 LTS `6000.0.84f1`** (pinned in `ProjectSettings/ProjectVersion.txt`).
  - Alternative supported LTS observed in the official release API: `6000.3.25f1` (Unity 6.3 LTS).
  - Rationale: 6.0 LTS has the broadest package/tooling compatibility and is
    mature; 6.3.25f1 was published 2026-09-24 and is very new.
  - Source: Unity release API `https://services.api.unity.com/unity/editor/release/v1/releases?stream=LTS`.
- Render pipeline: **URP 17.0.3** (Unity 6.0 URP line).
- Input: **Unity Input System 1.11.2** (confirmed via Unity package docs).
- Tests: **Unity Test Framework 1.4.6** (confirmed via Unity package docs).
- Networking: **Photon Fusion 2.1.3 in Host Mode** — installed; online host/join implemented (see `docs/HANDOFF.md`).

All package patch versions must be confirmed by the Package Manager on first
editor open; the manifest pins the lines recorded above and Unity may resolve a
compatible patch. This is the only version uncertainty and is called out in
`docs/DECISIONS.md`.

## 3. Architecture summary

Two assemblies and a hard layer boundary:

1. `Rootbound.Core` — pure C#, **no engine references** (`noEngineReferences: true`).
   Authored `*Spec` definitions (plain C#), runtime rules (health, cooldowns,
   dodge, target filtering, Root Cage ignition), and the authoritative
   tick-based `CombatSimulation`.
2. `Rootbound.Unity` — MonoBehaviours only. Input capture, fixed-step hosting of
   the simulation, presentation (transforms, VFX hooks, camera), HUD, and a
   `INetworkSession` abstraction.

The simulation owns all mutable gameplay state. Unity never mutates simulation
state directly; it submits `PlayerCommand` values and reads state/events after
each `Step`. This is the same shape a host-authoritative Fusion integration
needs, so networking can be added without rewriting combat.

Authored definitions live in ScriptableObjects in the Unity layer and are
projected into immutable `*Spec` objects for the core. ScriptableObjects hold
no per-player runtime state.

## 4. Milestones (implemented)

- **Milestone 1 — local combat arena.** Root Guardian + Ember Moth, one enemy
  (Blightling), movement, fixed isometric camera, directional primary attack,
  dodge with i-frames and cooldown, one special each, health/damage/defeat/
  restart, combat HUD, and the Root Cage ignition interaction. Local two-player.
- **Milestone 2 — offline solo + Photon Fusion online.** Solo via the offline
  runner; Fusion Host Mode host/join, per-player ownership, host-authoritative
  `CombatSimulation`, replicated movement/combat/enemies/cage/ignition.
- **Milestone 3 — run loop.** Three authored rooms, Sporeling enemy, between-room
  upgrade pickups, victory/defeat/replay. Plan: `docs/MILESTONE3_PLAN.md`.

Explicitly out of scope so far: procedural generation, sanctuary, dialogue,
bosses, inventory/crafting, public matchmaking, host migration, metaprogression,
final art, prediction/reconciliation.

## 5. Current blockers and next steps

Current test counts, build/release state, and the validation checklist live in
`docs/HANDOFF.md`. In summary:

1. **Standalone runtime untested** — macOS (arm64, unsigned) and Windows builds
   exist (`v0.1.0`) but have not been launched/played.
2. **No second machine** for the cross-machine online test.
3. **No gamepad** in the verification environment, so Player 2 is untested.
4. **Network feel unmeasured** — no numbers under Fusion `NetworkSimulationConfiguration`.
5. **CI releases manual** — no Unity license secrets / self-hosted runner configured.
6. **Milestone 4 scope undefined** — agree it in a `docs/MILESTONE4_PLAN.md`.
