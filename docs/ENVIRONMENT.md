# Development environment policy

> Branding note: this game is **Duatborn**; historical titles "Rootbound: Fractured Realms" and "Dawnkeepers: Gates of Duat" are superseded. Internal identifiers, namespaces, assemblies, and `Assets/Rootbound` keep the old name (`docs/CREATIVE_DIRECTION.md`).

Recorded so the team agrees on where Duatborn is built, tested, and shipped.
The project has been imported, compiled, tested, and built headlessly on the
secondary Mac M1 environment (Unity `6000.0.84f1`); see `docs/HANDOFF.md`.

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
- Keep the clone at a short path (for example `C:\dev\duatborn`) to avoid
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

Photon Fusion 2.1.3 is imported at `Assets/Photon/Fusion`, and online Host Mode
is implemented. A Photon App ID is required at runtime; it is held in the
git-ignored `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset` (and injected
in CI from the `PHOTON_APP_ID` secret). WSL and the core test project are
unaffected. Do not commit the App ID, `*.fusionappid`, `secrets/`, or `.env`.
