# Milestone 3 plan (proposal - not implemented)

> Branding note: this game is **Duatborn**; historical titles "Rootbound: Fractured Realms" and "Dawnkeepers: Gates of Duat" are superseded. Namespaces, assemblies, and the `Assets/Duatborn` path use the Duatborn name; the migration is recorded in `docs/RENAME_PLAN.md`.

Goal: turn the single arena into a small run loop, preserving the existing
`CombatSimulation`, solo mode and Fusion Host Mode replication. No content pass
beyond what is listed.

## Scope

1. **Three authored rooms** - data-driven `ArenaDefinition` assets (enemy
   count/types, spawn layout, bounds). No procedural generation.
2. **Upgrade selection between rooms** - after a room clears, choose 1 of 3
   authored upgrades; applies for the rest of the run.
3. **One additional enemy type** - a second `EnemySpec` (e.g. a ranged or
   charging Hollow Sentinel variant) with its own authored tuning.
4. **One finale encounter** - a heavier authored room after room 3.
5. **Victory / defeat / replay** - run ends on finale clear (victory) or all
   players defeated (defeat); return to menu and start a fresh run.
6. **Solo and online co-op** - the same run loop in both modes.

## Host authority in online mode

The host owns the single `CombatSimulation` and all run decisions:

- **Upgrade selection**: choices are presented by the host; the host records the
  selection into the run state and replicates the resulting modifiers. Clients
  send a choice index as input; the host validates eligibility (one pick, offered
  set) and applies it authoritatively. (Design choice: offer the same three
  options to everyone and let the host decide; or, later, each player picks a
  personal upgrade - decide before implementing.)
- **Room transitions**: the host decides when a room is cleared and which
  `ArenaDefinition` loads next, then replicates the room index/state. Clients do
  not load or advance rooms independently.
- **Encounter decisions**: enemy spawning, completion and rewards resolve on the
  host; clients mirror state (as today).

Offline solo runs the same loop with the local runner as the authority.

## Boundaries (out of scope)

Permanent progression/metaprogression, procedural worlds, Steam invitations,
accounts, cloud saves, matchmaking, host migration, new infrastructure.
Determinism across machines is not assumed; upgrades must be represented as
host-authored state, not recomputed client-side.

## Smallest next implementation step

Add an authored `ArenaDefinition` ScriptableObject and a `RunState` on the host
that selects one of two authored rooms and advances after clear, replicating the
room index. Prove it with a Multi-Peer PlayMode test (room advances on both
peers) before adding upgrades or the finale. Keep damage/enemy replication
unchanged.
