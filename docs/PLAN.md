# Rootbound: Fractured Realms — Implementation Plan and Assumptions

Status: living document. Section 1 below records the original authoring
environment (Unity was not installed then). Unity is now installed and the project
compiles and runs headlessly; for current status see `docs/HANDOFF.md`.

## 1. Environment inventory (verified)

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

Consequence: **Unity cannot be executed in this environment.** All Unity-side
code is authored and inspected but not compiled by the Unity editor here. To keep
validation honest and executable, the gameplay domain is written as a pure C#
assembly with no `UnityEngine` dependency and is exercised by a `dotnet test`
project that compiles the *same* source files Unity uses.

## 2. Version decisions (recorded)

- Editor: **Unity 6.0 LTS `6000.0.84f1`** (pinned in `ProjectSettings/ProjectVersion.txt`).
  - Alternative supported LTS observed in the official release API: `6000.3.25f1` (Unity 6.3 LTS).
  - Rationale: 6.0 LTS has the broadest package/tooling compatibility and is
    mature; 6.3.25f1 was published 2026-09-24 and is very new.
  - Source: Unity release API `https://services.api.unity.com/unity/editor/release/v1/releases?stream=LTS`.
- Render pipeline: **URP 17.0.3** (Unity 6.0 URP line).
- Input: **Unity Input System 1.11.2** (confirmed via Unity package docs).
- Tests: **Unity Test Framework 1.4.6** (confirmed via Unity package docs).
- Networking: **Photon Fusion 2 in Host Mode** — intended, **not installed** (see Section 5).

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

## 4. Milestone 1 scope

Combat arena: Root Guardian + Ember Moth, one enemy (Blightling), movement,
fixed isometric camera, directional primary attack, dodge with i-frames and
cooldown, one special each, health/damage/defeat/restart, combat HUD, and the
Root Cage ignition interaction. Local two-player; Fusion deferred.

Explicitly out of scope for this milestone: procedural generation, sanctuary,
dialogue, bosses, inventory/crafting, matchmaking, host migration, upgrades,
final art.

## 5. Blockers and required next steps

1. **Unity editor missing (blocks executable Unity validation).**
   Next step: install Unity Hub + Unity `6000.0.84f1` (or 6.3 LTS) with macOS
   build support, open the project, resolve packages, run the editor menu
   `Rootbound ▸ Build Milestone 1 Content`, then `Rootbound ▸ Create Arena Scene`.
2. **Photon Fusion unavailable (blocks online multiplayer).**
   Fusion requires obtaining the package from Photon and a Photon App ID.
   `Packages/manifest.json` intentionally does **not** reference Fusion so the
   project resolves cleanly without it. The game runs offline/local. See
   `docs/DECISIONS.md` for the intended integration boundary.
3. **Exact URP/package patch versions unverified** without the editor; documented above.
