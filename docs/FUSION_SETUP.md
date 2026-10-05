# Photon Fusion 2 setup (Milestone 2 dependency)

> Branding note: this game is **Duatborn**; historical titles "Rootbound: Fractured Realms" and "Dawnkeepers: Gates of Duat" are superseded. Internal identifiers, namespaces, assemblies, and `Assets/Rootbound` keep the old name (`docs/CREATIVE_DIRECTION.md`).

## Current state (verified on this machine, 2026-10-02)

- **Fusion 2.1.3 is imported** at `Assets/Photon/Fusion`; loaded assemblies are
  `Fusion.Runtime 2.1.3.0` (the `package.json` "version 1.1.0" is the UPM wrapper
  version, not the SDK).
- The project **compiles** with Fusion (`Tundra build success`, no `error CS`).
- The import added `com.unity.nuget.mono-cecil: 1.10.2` to
  `Packages/manifest.json` and added Fusion scripting defines + enabled unsafe
  code in `ProjectSettings/ProjectSettings.asset`. These are Fusion-required.
- **The Fusion App ID is NOT set** (`AppIdFusion` is empty in
  `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset`). Until it is set,
  host/join cannot connect and **no online behavior can be tested**.
- `PhotonAppSettings.asset` is now git-ignored so the App ID is never committed.

Previously Fusion was not installed; the sections below are the install steps and
the integration plan. Nothing online is implemented or validated yet.

## Verified facts (official Photon docs/download page, 2026-10-02)

- Latest stable SDK: **Fusion 2.1.3 Stable, Build 2390** (`photon-fusion-2.1.3-stable-2390.unitypackage`).
  Previous stable line: **2.0.13 Stable, Build 2379**.
- **Unity support: `2021.3.45`, `2022.3.45`, `6.0.x`, `6.3.x`.** This project's
  editor is `6000.0.84f1` (Unity 6.0 LTS), which is **officially supported**.
- Distribution is a **`.unitypackage`** from `downloads.photonengine.com`
  (sign-in required). Fusion is **not** a UPM registry package, so
  `Packages/manifest.json` does **not** change.
- Required config: a **Fusion App ID** created in the Photon dashboard.
- Required editor setting: **Asset Serialization = Force Text**. This project
  already has it (`ProjectSettings/EditorSettings.asset` -> `m_SerializationMode: 2`).

Docs: <https://doc.photonengine.com/fusion/v2/fusion-intro>.
Download table: <https://doc.photonengine.com/fusion/v2/getting-started/sdk-download>.

## Steps

1. Create a Photon account and a Fusion app:
   - <https://dashboard.photonengine.com> -> `YOUR > APPS > Development` ->
     `CREATE A NEW APP` -> type **Fusion**.
   - Copy the **App ID** that is shown. Treat it as local configuration; never
     commit it.
2. Download the SDK (sign in first):
   - <https://downloads.photonengine.com/download/fusion/photon-fusion-2.1.3-stable-2390.unitypackage>
   - The `?pre=sp` links on the download page require an authenticated session.
3. In Unity `6000.0.84f1`, open the project and import the package:
   - `Assets > Import Package > Custom Package...` -> select the `.unitypackage`
     -> **Import all**.
   - This creates `Assets/Photon/Fusion/...` and
     `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset`.
4. Set the App ID (either works):
   - **Fusion Hub** window field, or
   - select `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset` and paste it
     into `AppSettings > App Id Fusion`.
5. Confirm `Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion` exists.
6. Re-run the local tests to confirm the package did not change the offline arena:
   - `dotnet test Tools/CoreTests/CoreTests.csproj`
   - Unity Test Runner (EditMode/PlayMode), or the batchmode commands in
     `docs/TESTING.md`.

## Rootbound patch: Fusion installer vs MPPM virtual projects

Fusion 2.1.3's `[InitializeOnLoad] FusionInstaller` reads
`Packages/manifest.json` unconditionally, but MPPM virtual players have no
manifest (launched with `-noUpm`, empty `Packages` dir, redirected `Library`),
so activation threw `FileNotFoundException` in the static constructor. A narrow
patch adds an MPPM guard using Fusion's supported `Fusion.FusionMppm.Status`.
See `docs/patches/fusion-installer-mppm.patch` for the exact before/after,
the reason, and reapplication steps. A Fusion SDK update overwrites the file, so
reapply the patch after updates and verify with
`Rootbound > Verify Fusion MPPM Patch` (read-only) or the
`FusionInstallerPatchPresenceTests` PlayMode test.

## Keeping credentials out of version control

`.gitignore` already excludes `*.fusionappid`, `secrets/`, and `.env`. In
addition:

- Do **not** commit a populated `PhotonAppSettings.asset` with a real App ID if
  the team treats it as sensitive. Keep it local and share a template instead.
- Do not paste the App ID into docs, commit messages, or code.
- No purchase is required to create a development app; do not buy a plan without
  approval.

## What will be built once Fusion is present

Per `docs/ARCHITECTURE.md` ("Milestone 2 plan"):

- `FusionNetworkSession : INetworkSession` for host/join, session name, status,
  and disconnect handling.
- A Fusion `NetworkBehaviour` that owns the existing `CombatSimulation` and calls
  `Step(dt)` from `FixedUpdateNetwork`. **No second combat implementation.**
- Client `PlayerCommand` input (including the aimed `TargetPoint` and latched
  dodge) sent to the host; players control only their own creature.
- Replicated player/enemy/cage/projectile state; authoritative damage resolved
  once on the host.
- Local camera follows the client's own player via
  `IsometricCameraRig.SetLocalPlayer(id)` (seam added; offline still uses the
  centroid).
- Menu shows connection status and failure messages; returning to the menu when
  the host disconnects.

## Verification checklist for the dependency

- [ ] `Assets/Photon/Fusion` exists and the project compiles.
- [ ] `PhotonAppSettings.asset` has the App ID.
- [ ] A Fusion sample/bootstrap can start a host and a client on one machine.
- [ ] Offline arena tests still pass unchanged.
