# Milestone 4 plan (proposal - not implemented)

> Branding note: this game is **Duatborn**; historical titles "Rootbound: Fractured Realms" and "Dawnkeepers: Gates of Duat" are superseded. Namespaces, assemblies, and the `Assets/Duatborn` path use the Duatborn name; the migration is recorded in `docs/RENAME_PLAN.md`.

> Status: **proposal.** The direction and the open decisions below must be agreed
> before substantial code. This document does not claim anything is implemented.

Goal: turn Milestone 3's three-gate run into a complete short run with a proper
finale and a light metagame, while preserving the engine-agnostic
`CombatSimulation`, offline solo/local play, and Fusion Host Mode. Keep the
`ArenaSpec` / `RunState` / `UpgradeSpec` shapes; no rewrite.

## Scope (proposed)

1. **Boss gate.** Replace the third gate's finale flag with an authored boss
   encounter: one heavy enemy archetype with 2-3 telegraphed phases (e.g. a
   charging Sentinel construct and a ranged Fractured Vessel phase). Authored as
   data (`EnemySpec` + `ArenaSpec`), not procedural.
2. **Offering (meta currency).** Award Offerings at run end (victory > defeat),
   scaled by gates cleared. Persisted locally.
3. **Sanctuary.** A between-runs screen to spend Offerings on permanent unlocks
   (a starting upgrade, an alternate rite, or cosmetic gate inscriptions).
4. **Run summary.** End-of-run overlay: outcome, gates cleared, Offering award,
   and the next unlock cost.
5. **Save v1.** Versioned local JSON under `Application.persistentDataPath`.
   Corrupt/missing save falls back to defaults; no cloud, no accounts.

## Host authority and online

- The run loop stays host-authoritative (`RunState` advances on the host only).
- The boss is an enemy: spawning, damage, phases, and defeat resolve on the host
  and replicate like existing enemies. No client-side boss logic.
- **Meta progression is local per player and is not replicated.** Offerings,
  unlocks, and the Sanctuary are not part of the shared session. (Open decision
  3 below.)
- No prediction/reconciliation is added; this milestone does not change the
  network model.

## Boundaries (out of scope)

Procedural generation, new biomes, Steam invitations, accounts, cloud saves,
matchmaking, host migration, new infrastructure, final art, localization. No
change to `CombatSimulation` rules beyond what a new `EnemySpec` needs.

## Smallest next implementation step

Add an authored boss `EnemySpec` and a single `ArenaSpec` boss gate, gated behind
the existing `RunState`/`ArenaSpec` run loop, and prove it with an EditMode test
(the run reaches the boss gate and a boss defeat completes the run). Do **not**
add Offerings, the Sanctuary, or saving yet; those follow once the boss gate is
stable. Keep enemy replication unchanged.

## Open decisions (agree before coding)

1. **Direction.** Metagame (Offerings + Sanctuary) **or** pure content widening
   (a fourth normal gate + boss) first? The scope above assumes both, metagame
   second.
2. **Boss archetype and phases.** Which existing behavior (charge, ranged burst,
   area denial) is the core, and how many phases?
3. **Offerings scope.** Per-run only (resets each session) vs persisted per
   profile; offline-only vs also accumulated in online runs.
4. **Unlock surface.** Starting upgrades, alternate rites, or cosmetic gate
   inscriptions. Cosmetic-only is the lowest-risk first cut.
5. **Save ownership.** Local file only (proposed) vs future cloud; confirm no
   account work is implied.
