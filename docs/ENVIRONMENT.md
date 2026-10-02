# Development environment policy

Recorded so the team agrees on where Rootbound is built, tested, and shipped.
This is policy, not a claim that any environment has run the Unity project.

## Platforms

- **Native Windows is the primary development environment.** Day-to-day work,
  Unity editing, and first validation happen here.
- **Mac M1 is a secondary environment.** Useful for code and core-logic work and
  as an alternate checkout; it is not the lead platform.
- **Windows is the initial release target.** PC-first, Windows before other
  platforms.
- **WSL is optional tooling, not the Unity Editor environment.** WSL may be used
  for Git, CLI, dotnet, and scripts. Unity and the Unity Editor must run on
  native Windows, not inside WSL.

## Toolchain

| Tool | Version | Purpose |
| --- | --- | --- |
| Unity Editor | `6000.0.84f1` (Unity 6.0 LTS) | Primary engine. Windows primary, Mac secondary. |
| Unity Hub | current | Install and manage editors. |
| .NET SDK | 10.0.x (verified 10.0.300) | `Tools/CoreTests` pure-core tests only. |
| Git | current | Version control. |
| VS 2022 or Rider | current | C# editing on Windows. |

Unity 6.3 LTS `6000.3.25f1` is a supported alternative to `6000.0.84f1`, but the
project is pinned to `6000.0.84f1`. Do not upgrade the editor silently.

## Cross-platform conventions

- The repository normalizes line endings via `.gitattributes` (`* text=auto`).
- Windows and default macOS filesystems are case-insensitive. Do not create
  files or folders whose names differ only by case.
- Keep the clone at a short path (for example `C:\dev\Rootbound`) to avoid
  Windows `MAX_PATH` issues with Unity and generated `Library/` content.
- Generated caches (`Library/`, `Temp/`, `Logs/`, `obj/`, `bin/`, `Builds/`,
  `UserSettings/`) are ignored and must never be committed.
- Authored/generated project content is committed so a checkpoint reproduces the
  scene: `Assets/Rootbound/Data/` (definitions), `Assets/Rootbound/Scenes/`
  (`CombatArena.unity`), `Assets/Rootbound/Settings/` (URP assets), the URP global
  settings, and `ProjectSettings/`. The editor menu generator is idempotent and
  reuses existing assets rather than overwriting them.
- `.meta` files are committed (Unity generated 68 on first import). Do not
  hand-author GUIDs.

## Networking

Photon Fusion 2 is intended but is **not installed**, and no App ID is
configured. WSL and the core test project do not change this. Do not commit
`*.fusionappid`, `secrets/`, or `.env`.
