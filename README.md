# Rootbound: Fractured Realms

An original cooperative action roguelite prototype (working title). Strange
creatures enter a fractured world-tree and combine abilities to reclaim
corrupted regions. Isometric action combat, solo or 2-4 players, PC-first.

This repository currently contains **Milestone 1**: one local combat arena where
Root Guardian and Ember Moth fight Blightlings, with the Root Cage ignition
co-op interaction.

> Honesty note: this project was authored in an environment **without the Unity
> editor installed**. The pure gameplay core is compiled and tested with the
> .NET SDK (`dotnet test`, 28 tests passing). All Unity-side code has been
> syntax-validated but **not compiled or run by Unity**, and Photon Fusion is
> **not installed**. See `docs/HANDOFF.md`.

**Native Windows is the primary development environment and the initial release
target; Mac M1 is secondary. WSL is optional tooling, not the Unity environment.**
See `docs/ENVIRONMENT.md`.

## Requirements

- Unity 6.0 LTS **`6000.0.84f1`** (pinned in `ProjectSettings/ProjectVersion.txt`).
  Unity 6.3 LTS `6000.3.25f1` is also a supported alternative.
- .NET SDK (for the pure-core test project only; verified with 10.0.300).
- Git. Keep the clone at a short path (e.g. `C:\dev\Rootbound`) on Windows.

## Open the project

1. Install Unity Hub and Unity `6000.0.84f1` with Windows build support (primary),
   or macOS build support (secondary).
2. In Unity Hub, **Add** this repository folder and open it. Unity resolves
   `Packages/manifest.json` (URP 17.0.3, Input System 1.11.2, Test Framework 1.4.6).
3. **Set up URP (manual, required once).** Because the project was not created
   through the Unity template, no render pipeline asset is committed:
   - `Assets > Create > Rendering > URP Asset (with Universal Renderer)`.
   - `Edit > Project Settings > Graphics` -> assign the URP Asset to
     **Scriptable Render Pipeline Settings**.
   - `Project Settings > Quality` -> assign the URP Asset to each quality level's
     **Render Pipeline**.
   - Placeholder materials detect the active pipeline automatically.

## Build and run the arena

From the Unity menu bar:

1. `Rootbound > Build Milestone 1 Content`
   Creates `Assets/Rootbound/Data/*.asset` definition ScriptableObjects.
2. `Rootbound > Create Arena Scene`
   Creates `Assets/Rootbound/Scenes/CombatArena.unity` with camera, light, ground,
   arena controller, HUD and menu, and adds it to Build Settings.
3. Open `CombatArena.unity` and press **Play**.
4. Click **Host Local Session (2 players)**.

The menu reports `Session: Hosting LOCAL (offline/local)`. Online join is
intentionally unavailable until Fusion is integrated.

## Controls

| Action | Player 1 (keyboard/mouse) | Player 2 (gamepad) |
| --- | --- | --- |
| Move | WASD | Left stick |
| Aim | Mouse position | Right stick |
| Primary | Left mouse | Right trigger |
| Special | Q / Right mouse | Left trigger |
| Dodge | Space | East / B |
| Restart | R (after clear/defeat) | R |

## Tests

Pure gameplay core (runs anywhere, no Unity required):

```
dotnet test Tools/CoreTests/CoreTests.csproj
```

Inside Unity: `Window > General > Test Runner > EditMode > Run All`.

The same NUnit test files under
`Assets/Rootbound/Tests/EditMode/` are compiled by both paths.

## Repository layout

```
Assets/Rootbound/
  Scripts/Core/         Pure C# gameplay domain (no UnityEngine)
  Scripts/Runtime/      Unity adapters: input, runner, presentation, UI, networking
  Scripts/Editor/       Reproducible content/scene generator
  Tests/EditMode/       NUnit tests (also run via dotnet)
  Data/                 Generated ScriptableObject definitions (not committed)
  Scenes/               Generated arena scene (not committed)
Packages/               Unity package manifest
ProjectSettings/        Pinned editor version
Tools/CoreTests/        dotnet test project for the pure core
docs/                   ARCHITECTURE, DECISIONS, TESTING, HANDOFF, PLAN
```

## Non-goals for this milestone

Procedural generation, sanctuary, dialogue, bosses, inventory/crafting, public
matchmaking, host migration, cross-platform release work, and a large upgrade
catalogue are explicitly out of scope.

Rootbound is an original work. It is inspired by Norse imagery but does not
present invented lore as authentic mythology and copies no assets, characters,
UI, dialogue, or implementation from existing games. The title is provisional.
