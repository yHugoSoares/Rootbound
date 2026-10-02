# Testing

## What was actually executed

Environment: macOS arm64, .NET SDK 10.0.300. Unity was **not installed**, so no
Unity test run or play-mode run occurred.

```
dotnet test Tools/CoreTests/CoreTests.csproj
```

Result (last run, during Windows handoff prep): **Passed - Failed: 0, Passed: 28,
Skipped: 0, Total: 28.**

This compiles `Assets/Rootbound/Scripts/Core/**` and
`Assets/Rootbound/Tests/EditMode/**` together. It is the same core code Unity
compiles; the Unity-only adapters are not part of this test run.

A Roslyn parse of all 29 C# files (C# 9) reported **0 syntax errors**. This is a
syntax check only, not a Unity semantic compile.

## Automated coverage map

| Required check | Test(s) | Status |
| --- | --- | --- |
| Damage cannot reduce health below zero | `HealthTests.DamageCannotReduceHealthBelowZero` | pass |
| Defeat triggers only once | `HealthTests.DefeatIsReportedOnceAndFurtherDamageIsIgnored`, `CombatSimulationTests.EnemyDefeatRaisesSingleEvent` | pass |
| Abilities cannot activate during cooldown | `CooldownTests.AbilityCannotActivateDuringCooldown`, `PlayerCooldownsAreIndependent` | pass |
| Dodge invulnerability timing | `DodgeTests.InvulnerabilityOnlyWithinWindow`, `CombatSimulationTests.DodgeBlocksIncomingDamage`, `DamageLandsWhenNotDodging` | pass |
| Invalid / friendly targets rejected | `TargetRulesTests.*`, `CombatSimulationTests.IgnitionBurstDoesNotDamageAllies` | pass |
| Root Cage ignition stacking and duration | `RootCageTests.*`, `CombatSimulationTests.IgnitionBurstIgnitesCageAndFireDealsDamage` | pass |
| Different players do not share cooldown state | `CombatSimulationTests.PlayerCooldownsAreIndependent` | pass |
| Cage restrains enemies | `CombatSimulationTests.RootCageRestrainsEnemy` | pass |
| Encounter completion | `CombatSimulationTests.EncounterClearedAfterAllEnemiesDefeated` | pass |
| Restart resets state | `CombatSimulationTests.RestartRestoresInitialState` | pass |
| Defeated players ignore commands | `CombatSimulationTests.DefeatedPlayerCommandsAreIgnored` | pass |

## Running the Unity EditMode tests

1. Open the project in Unity `6000.0.84f1`.
2. `Window > General > Test Runner`.
3. Select the **EditMode** tab and `Run All`.
4. `Rootbound.Tests.EditMode` must reference `Rootbound.Core`; the tests use only
   NUnit and the core, so they should mirror the `dotnet test` result.

These tests have **not** been run in Unity here.

## Manual checklist (not executed - requires Unity and a second input device)

Each item below is unverified in this environment and must be performed after
opening the project.

- [ ] Movement is consistent at 30, 60 and 144 FPS (fixed-step accumulator).
- [ ] A remote player can move, attack and dodge. (Blocked: Fusion not installed;
      for now verify local player 2 via gamepad only.)
- [ ] Damage is resolved once (watch a single hit subtract once).
- [ ] Ownership restrictions hold (defeated player cannot act).
- [ ] Enemy defeat and encounter completion agree across clients. (Blocked: Fusion.)
- [ ] Disconnecting does not leave a broken menu. (Blocked: online session absent;
      verify `RootboundMenu.ReturnToMenu` locally.)
- [ ] Controller input and mouse aim both work.
- [ ] Combat stays readable with both players active.
- [ ] Root Cage restrains, ignites, shows remaining duration, and stops burning.
- [ ] Restart after clear and after all players are defeated.

## Latency and packet-loss testing

Not performed. No networking stack is present. When Fusion is integrated, use the
Fusion Simulation (latency + packet loss) settings and record concrete
conditions and observed problems. Do not claim results that were not measured.

## Known test gaps

- No test asserts Unity presentation (camera framing, HUD layout, primitive syncing).
- No test asserts Fusion prediction/reconciliation or resimulation guards.
- `PlayerInputAdapter` (mouse raycast, camera-relative mapping) is untested.
- The syntax check cannot catch missing or misnamed Unity APIs.
