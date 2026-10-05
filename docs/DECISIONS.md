# Decisions and tradeoffs

> Branding note: this game is **Duatborn**; historical titles "Rootbound: Fractured Realms" and "Dawnkeepers: Gates of Duat" are superseded. Internal identifiers, namespaces, assemblies, and `Assets/Rootbound` keep the old name (`docs/CREATIVE_DIRECTION.md`).

## D1 - Unity 6.0 LTS `6000.0.84f1`, not the newest LTS

The official release API lists both `6000.0.84f1` and `6000.3.25f1` in the LTS
stream. `6000.0` was chosen for ecosystem maturity and broad package/tooling
compatibility. `6000.3.25f1` was published 2026-09-24 and is very new.
Tradeoff: we forgo newer engine fixes; switching later is a project upgrade and
must be intentional, not silent.

## D2 - Pure gameplay core with no engine references

`Rootbound.Core` compiles without `UnityEngine`. Tradeoff: conversions at the
boundary (`Vec2` <-> `Vector3`, specs <-> ScriptableObjects) and a small
hand-rolled `Vec2`. Benefit: the authoritative rules are unit-testable without
the editor, which is the only executable validation available in this
environment, and they stay portable.

## D3 - Command-based, tick-simulated, host-authoritative combat

Unity submits `PlayerCommand` values; the simulation validates and resolves
everything. Tradeoff: presentation is one step behind and requires interpolation
for smooth motion. Benefit: no gameplay logic duplicated between solo and
networked paths, and authority boundaries match the host-authoritative model
required for Fusion.

## D4 - Fixed 60 Hz simulation with an accumulator

`LocalGameRunner` accumulates `Time.deltaTime` and runs whole fixed steps
(capped at 5 per frame). Tradeoff: a small input latency at very low frame rates.
Benefit: frame-rate-independent movement and cooldowns rather than
`Update`-scaled logic.

## D5 - Root Cage ignition is capped by total burn time

`TryIgnite` clamps `IgnitedTimeRemaining` to `MaxIgnitionDuration`. Re-triggering
at the cap is a no-op (returns false), which prevents accidental damage stacking.
Tradeoff: a second ignition near the cap gives diminishing benefit. Chosen over a
stack counter because it directly bounds total damage and is simple to test.

## D6 - Photon Fusion 2 deferred; offline session shipped

> **Superseded in Milestone 2:** Fusion 2.1.3 is now imported, an App ID is
> configured locally, and online Host Mode is implemented (this record is kept for
> history).

Fusion is not present and no Photon App ID or credentials exist. Rather than add
an unverifiable package reference or a fake networking framework, the project
ships `INetworkSession` + `OfflineNetworkSession`, reports the exact blocker, and
keeps the simulation host-authoritative so a real Fusion runner can be dropped
in. The manifest deliberately omits Fusion so the project resolves cleanly.
Tradeoff: no online play this milestone. This is the single largest gap.

## D7 - IMGUI placeholder HUD

`CombatHud` and `RootboundMenu` use `OnGUI` instead of UGUI/TextMeshPro. Tradeoff:
not production UI. Benefit: no canvas/font/TMP asset dependencies, so the code
is reproducible and compiles without authored UI assets. Replace with uGUI/TMP
in the next milestone.

## D8 - Procedurally generated placeholder visuals

`CombatView` builds primitives via `GameObject.CreatePrimitive`; materials detect
the active render pipeline. Tradeoff: no authored art or VFX, and runtime object
churn on bind. Benefit: the milestone does not spend effort on final art and
avoids hand-authored prefab/scene YAML that cannot be validated without Unity.

## D9 - Reproducible content via an editor generator

`Rootbound > Build Milestone 1 Content` and `Create Arena Scene` generate assets
and the scene from code rather than committing unverifiable YAML. Tradeoff: one
extra manual step; URP asset assignment remains manual because creating a URP
pipeline asset relies on editor APIs that could not be verified here. Benefit:
generation is deterministic and reviewable in source.

## D10 - Two creatures, two specials, no universal ability language

`AttackSpec` and `SpecialSpec` have a small `Kind` enum and typed branches.
Tradeoff: adding a third distinct ability kind touches the simulation. Benefit:
avoids an over-general ability DSL that the brief explicitly warns against.

## Open uncertainties

- Exact URP/Input System/Test Framework patch resolution on first editor open
  (lines are pinned; patches may be adjusted by Package Manager).
- All Unity API usage is syntax-validated only, never compiled.
- Fusion package name, version, and App ID must be confirmed from Photon at
  integration time; no value is invented here.
