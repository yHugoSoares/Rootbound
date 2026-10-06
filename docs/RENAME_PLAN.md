# Duatborn technical rename plan (Phase 2 — proposal, not applied)

> Status: **applied and validated (Phases 1-4).** The mapping below is kept as
> the migration record. See "Phase 4 results" at the end.

## Goal

Replace the temporary internal `Rootbound` naming with `Duatborn` throughout
first-party code, assemblies, assets, editor tooling, tests, and references —
**without changing gameplay, stable content IDs, GUIDs, or vendor Photon code.**

## Explicit non-goals / do not touch

- `Assets/Photon/**` namespaces, SDK code, and packages (one narrow config change
  is called out separately: `AssembliesToWeave`).
- Photon App ID / `PhotonAppSettings.asset`, credentials, Steam App ID.
- Repository name/history/visibility, CI runner configuration, local checkout.
- Unity version (`6000.0.84f1`) and the networking architecture.
- Numeric enum values, ScriptableObject `.meta` GUIDs, and stable string IDs.

## 1. Namespace mapping

| Current | New |
| --- | --- |
| `Rootbound.Core` | `Duatborn.Core` |
| `Rootbound.Unity` | `Duatborn.Unity` |
| `Rootbound.Fusion` | `Duatborn.Fusion` |
| `Rootbound.EditorTools` | `Duatborn.EditorTools` |
| `Rootbound.Tests` | `Duatborn.Tests` |

## 2. Assembly definitions (name + filename)

| Current file / name | New file / name |
| --- | --- |
| `Rootbound.Core.asmdef` / `Rootbound.Core` | `Duatborn.Core.asmdef` / `Duatborn.Core` |
| `Rootbound.Unity.asmdef` / `Rootbound.Unity` | `Duatborn.Unity.asmdef` / `Duatborn.Unity` |
| `Rootbound.Fusion.asmdef` / `Rootbound.Fusion` | `Duatborn.Fusion.asmdef` / `Duatborn.Fusion` |
| `Rootbound.Editor.asmdef` / `Rootbound.Editor` (rootNamespace `Rootbound.EditorTools`) | `Duatborn.Editor.asmdef` / `Duatborn.Editor` (rootNamespace `Duatborn.EditorTools`) |
| `Rootbound.Tests.EditMode.asmdef` | `Duatborn.Tests.EditMode.asmdef` |
| `Rootbound.Tests.PlayMode.asmdef` | `Duatborn.Tests.PlayMode.asmdef` |

Also update every `"references"` entry inside the asmdefs.

## 3. First-party types and filenames

| Current | New | Notes |
| --- | --- | --- |
| `RootboundBuild.cs` / `RootboundBuild` | `DuatbornBuild.cs` / `DuatbornBuild` | host of `PerformBuild` |
| `RootboundFusionSetup.cs` / `RootboundFusionSetup` | `DuatbornFusionSetup.cs` / `DuatbornFusionSetup` | |
| `RootboundProjectConfigurator.cs` / `RootboundProjectConfigurator` | `DuatbornProjectConfigurator.cs` / `DuatbornProjectConfigurator` | |
| `RootboundSceneBuilder.cs` / `RootboundSceneBuilder` | `DuatbornSceneBuilder.cs` / `DuatbornSceneBuilder` | |
| `RootboundInput.cs` / `RootboundInput` (Fusion `INetworkInput` struct) | `DuatbornInput.cs` / `DuatbornInput` | Fusion-woven; see §8 |
| `RootboundInputActions.cs` / `RootboundInputActions` | `DuatbornInputActions.cs` / `DuatbornInputActions` | generated input wrapper |
| `RootboundMenu.cs` / `RootboundMenu` | `DuatbornMenu.cs` / `DuatbornMenu` | |
| `RootCageState.cs` / `RootCageState` | `BindingSealState.cs` / `BindingSealState` | internal mechanic type |

## 4. Enums (keep numeric values; do not renumber)

| Current member | New member | Value |
| --- | --- | --- |
| `CreatureKind.RootGuardian` | `CreatureKind.DuneWarden` | 0 |
| `CreatureKind.EmberMoth` | `CreatureKind.Sunwing` | 1 |
| `AttackKind.EmberProjectile` | `AttackKind.SolarBolt` | 1 |
| `SpecialKind.RootCage` | `SpecialKind.BindingSeal` | 0 |
| `SpecialKind.IgnitionBurst` | `SpecialKind.SolarConsecration` | 1 |

Unity serializes these as integers (`Kind: 0` in the `.asset` YAML), so the
rename is serialization-safe.

## 5. First-party factory methods (pure C#, not serialized)

`DefaultContent`: `RootGuardian()` → `DuneWarden()`, `EmberMoth()` → `Sunwing()`,
`Blightling()` → `HollowSentinel()`, `Sporeling()` → `FracturedVessel()`.
Callers: `CombatSetup`, editor generators, tests.

## 6. ScriptableObject assets and folders

Rename via **Unity asset moves** (or external move of `.cs`/`.asset` **with their
`.meta` counterparts**) so GUIDs are preserved:

| Current | New |
| --- | --- |
| `Assets/Rootbound/` | `Assets/Duatborn/` (entire tree, all `.meta`) |
| `Data/RootGuardian.asset` | `Data/DuneWarden.asset` |
| `Data/EmberMoth.asset` | `Data/Sunwing.asset` |
| `Data/Blightling.asset` | `Data/HollowSentinel.asset` |
| `Settings/RootboundUrpAsset.asset` | `Settings/DuatbornUrpAsset.asset` |
| `Settings/RootboundUniversalRenderer.asset` | `Settings/DuatbornUniversalRenderer.asset` |

Areas unaffected by folder move because they reference by GUID: scene/prefab
`m_Script` entries, `GraphicsSettings`/`QualitySettings` URP references, and the
scene's references to the data assets.

**Must be updated by path string:**
- `ProjectSettings/EditorBuildSettings.asset` →
  `Assets/Duatborn/Scenes/CombatArena.unity`.
- `RootboundSceneBuilder` folder/scene-path constants and `LoadCreature`/`LoadEnemy`
  asset names.
- `RootboundProjectConfigurator` URP asset paths.
- `Tools/CoreTests/CoreTests.csproj` `<Compile Include>` globs.

## 7. Editor menus, log tags, build method, CI

- `[MenuItem("Rootbound/…")]` → `[MenuItem("Duatborn/…")]` (5 items).
- `[CreateAssetMenu(menuName = "Rootbound/…")]` → `"Duatborn/…"` (2 items).
- `Debug.Log("[Rootbound] …")` → `"[Duatborn] …"` (15 occurrences).
- Build execute method: `Rootbound.EditorTools.RootboundBuild.PerformBuild` →
  `Duatborn.EditorTools.DuatbornBuild.PerformBuild` in
  `.github/workflows/build.yml`, `.github/workflows/build-selfhosted.yml`, and
  `docs/HANDOFF.md`.
- CI artifact names are already `Duatborn-macOS` / `Duatborn-Windows` — no change.

## 8. Fusion weaving (narrow vendor-config change)

`Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion` has:

```json
"AssembliesToWeave": [ "Assembly-CSharp", "Assembly-CSharp-firstpass", "Rootbound.Fusion" ]
```

`Rootbound.Fusion` must become `Duatborn.Fusion` or the weaver stops processing
the renamed assembly. This is project **config**, not SDK source, and is the only
`Assets/Photon/**` change proposed.

**Leave as-is (compatibility):** the patch marker string
`ROOTBOUND_PATCH(FusionInstaller-MPPM)` embedded in the vendor file
`Assets/Photon/Fusion/Editor/Fusion.Unity.Editor.cs`, and the matching assertion
in `FusionInstallerPatchPresenceTests`. Renaming it would edit vendor code and
re-apply a patch; keep it and document the exception.

## 9. Tests

- Namespace `Rootbound.Tests` → `Duatborn.Tests` (all 16 test files).
- `RootCageTests.cs` / `RootCageTests` → `BindingSealTests.cs` / `BindingSealTests`.
- Update all references to renamed enum members, factory methods, and types.
- `Tools/CoreTests/CoreTests.csproj`: `<RootNamespace>Rootbound.Tests</RootNamespace>`
  → `Duatborn.Tests`; `<AssemblyName>Rootbound.CoreTests</AssemblyName>` →
  `Duatborn.CoreTests`; compile globs → `Assets/Duatborn/…`.

## 10. Documentation

Update name references in `README.md`, `docs/ARCHITECTURE.md`, `docs/DECISIONS.md`,
`docs/ENVIRONMENT.md`, `docs/FUSION_SETUP.md`, `docs/HANDOFF.md`, `docs/PLAN.md`,
`docs/TESTING.md`, `docs/MILESTONE3_PLAN.md`, `docs/MILESTONE4_PLAN.md`, and the
menu-path prose in `docs/patches/fusion-installer-mppm.patch`.
Keep the historical-title notes (classified Historical) and the
`docs/CREATIVE_DIRECTION.md` internal→display mapping table.

## Stable IDs — explicitly preserved

Content IDs `root_guardian`, `ember_moth`, `blightling`, `sporeling`,
`room_1..room_3`, and `up_*` are **not** changed. Room display names (`Sun Gate`,
`Shadow Gate`, `Horizon Gate`) are already correct.

---

# Risk analysis

| Risk | Why | Mitigation |
| --- | --- | --- |
| Broken scene/prefab script refs | `m_Script` is a GUID; renaming a class file changes the class, not the GUID | Keep the `.cs.meta` GUID; rename file + class together; verify no "missing script" in the scene |
| Lost SO asset refs | scene → asset refs are GUID | Move `.asset` + `.meta` as pairs; verify `m_Script` and cross-refs |
| Enum serialization | assets store enums as ints | Keep values; rename members only |
| Assembly reference breakage | asmdef `references` and test asmdefs | Update refs atomically; recompile |
| Fusion weaving stops | `AssembliesToWeave` lists old assembly | Update the single config entry; run a PlayMode Multi-Peer test |
| URP asset loss | `GraphicsSettings`/`QualitySettings` refs are GUID | Move URP assets with meta; verify graphics settings |
| Build/CI breakage | execute-method string in workflows | Update both workflows + docs in the same change; local build via new method |
| Editor generator drift | generator creates/loads assets by path/name | Update constants and names; do not re-run the generator unless needed (assets already exist) |
| `Assets/Photon` unintended edits | vendor | Only `AssembliesToWeave` config; leave the patch marker |
| Root-level `.csproj`/`.sln` | untracked generated files | Not committed; Unity regenerates after rename |

## Suggested order of operations (Phase 3, after approval)

1. Work on a branch; keep the tree clean to allow rollback.
2. Rename namespaces/types/files (`git mv` + edit), including `RootCageState`.
3. Rename asmdefs + update `references`.
4. Rename enum members, factory methods, menu paths, log tags.
5. Move `Assets/Rootbound` → `Assets/Duatborn` with all `.meta`; rename data/URP assets.
6. Update `EditorBuildSettings`, `CoreTests.csproj`, CI workflows, docs.
7. Update `AssembliesToWeave`.
8. Recompile, open the scene, confirm no missing scripts. Do not regenerate assets.

## Validation (Phase 4)

- `dotnet test Tools/CoreTests/CoreTests.csproj`.
- Unity headless EditMode + PlayMode (expect 75 / 11).
- Unity compile with no `error CS`; scene opens with 0 missing scripts.
- Manual: solo + creature selection; local/online co-op; ability labels match
  effects; upgrade descriptions + eligibility; gate progression and replay;
  Fusion weaving.
- Local build via the renamed execute method; CI `buildMethod` path inspection.

## Remaining old-name inventory (expected after Phase 3)

- **Historical:** superseded-title notes, `docs/CREATIVE_DIRECTION.md` mapping table.
- **Compatibility/stable IDs:** `root_guardian`, `ember_moth`, `blightling`,
  `sporeling`, `up_*`, room IDs; `ROOTBOUND_PATCH(FusionInstaller-MPPM)` marker.
- **Third-party:** `Assets/Photon/**` (except the one config entry).
- **Unresolved:** none intended.

## Rollback

No commits are made for this migration until approved. On a branch, rollback is
`git checkout -- .` / `git clean -fd` or deleting the branch.

## Phase 4 results

- Core tests (`dotnet test`, assembly `Duatborn.CoreTests`): **75/75 pass**.
- Unity EditMode (headless, `6000.0.84f1`): **75/75 pass**, no `error CS`.
- Unity PlayMode (headless): **11/11 pass**, suite name `Duatborn`; Fusion
  Multi-Peer replication (weaving) exercised.
- Scene/prefab `m_Script` GUIDs all resolve to the renamed scripts — no missing
  scripts. `.meta` GUIDs preserved across every rename (spot-checked, including
  `DuatbornMenu`, `DuatbornInput`, `BindingSealState`, `DuneWarden.asset`,
  `DuatbornUrpAsset.asset`, `Duatborn.Core.asmdef`).
- `EditorBuildSettings` scene path, URP asset GUID references, and the scene's
  data-asset reference verified.
- Local macOS build via `Duatborn.EditorTools.DuatbornBuild.PerformBuild`:
  **succeeded** (`[Duatborn] Build succeeded`); the player launch smoke-test
  initialized the engine/assemblies with no early crash.
- CI workflows re-parsed as valid YAML and reference the renamed method.
- No file or directory named `rootbound` remains (excluding historical mapping
  and the vendor patch marker).

### Remaining old-name inventory (justified)

- **Historical:** superseded-title notes and `docs/CREATIVE_DIRECTION.md` mapping.
- **Compatibility/stable IDs:** `root_guardian`, `ember_moth`, `blightling`,
  `sporeling`, `up_*`, `room_*`; the `ROOTBOUND_PATCH(FusionInstaller-MPPM)`
  marker embedded in vendor code.
- **Third-party:** `Assets/Photon/**` (one project-config entry changed:
  `AssembliesToWeave`).
- **Unresolved:** none.

### Manual checks still outstanding (not runnable headlessly)

- Play a build: offline solo + creature selection, local co-op, online co-op.
- Confirm ability labels match effects and upgrade eligibility in the GUI.
- Two-machine online test.
