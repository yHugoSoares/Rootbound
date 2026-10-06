using NUnit.Framework;
using Duatborn.Core;

namespace Duatborn.Tests
{
    public class CombatSimulationTests
    {
        private static EnemySpec StationaryDummy()
        {
            EnemySpec e = DefaultContent.HollowSentinel();
            e.MoveSpeed = 0f;
            return e;
        }

        private static EnemySpec ImmediateAttacker()
        {
            EnemySpec e = DefaultContent.HollowSentinel();
            e.MoveSpeed = 0f;
            e.AttackRange = 5f;
            e.AttackWindup = 0f;
            e.AttackActive = 0.05f;
            e.AttackRecovery = 0.1f;
            e.AttackCooldown = 5f;
            e.AttackDamage = 10f;
            return e;
        }

        private static CombatSetup Setup(CreatureSpec[] players, EnemySpec enemy, int enemyCount, Vec2[] playerSpawns, Vec2[] enemySpawns)
        {
            CombatSetup s = new CombatSetup();
            s.PlayerSpecs = players;
            s.EnemySpec = enemy;
            s.EnemyCount = enemyCount;
            s.PlayerSpawns = playerSpawns;
            s.EnemySpawns = enemySpawns;
            return s;
        }

        [Test]
        public void MeleeAttackDamagesEnemyInFront()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.DuneWarden() },
                StationaryDummy(), 1,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 1.2f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand cmd = default(PlayerCommand);
            cmd.Aim = new Vec2(0f, 1f);
            cmd.Primary = true;

            for (int i = 0; i < 12; i++)
            {
                sim.SubmitCommand(0, cmd);
                sim.Step();
            }

            Assert.That(sim.GetEnemy(1).Health.Current, Is.LessThan(40f));
        }

        [Test]
        public void MeleeAttackIgnoresEnemyBehind()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.DuneWarden() },
                StationaryDummy(), 1,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, -1.2f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand cmd = default(PlayerCommand);
            cmd.Aim = new Vec2(0f, 1f);
            cmd.Primary = true;

            for (int i = 0; i < 12; i++)
            {
                sim.SubmitCommand(0, cmd);
                sim.Step();
            }

            Assert.That(sim.GetEnemy(1).Health.Current, Is.EqualTo(40f).Within(0.0001f));
        }

        [Test]
        public void PlayerCooldownsAreIndependent()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.Sunwing(), DefaultContent.Sunwing() },
                StationaryDummy(), 1,
                new[] { new Vec2(-1f, 0f), new Vec2(1f, 0f) },
                new[] { new Vec2(0f, 4f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand idle = default(PlayerCommand);
            PlayerCommand fire = default(PlayerCommand);
            fire.Aim = new Vec2(0f, 1f);
            fire.Primary = true;

            sim.SubmitCommand(0, fire);
            sim.SubmitCommand(1, idle);
            sim.Step();
            sim.Step();

            Assert.That(sim.GetPlayer(0).PrimaryCooldown.IsReady, Is.False);
            Assert.That(sim.GetPlayer(1).PrimaryCooldown.IsReady, Is.True);
        }

        [Test]
        public void EnemyDefeatRaisesSingleEvent()
        {
            EnemySpec enemy = StationaryDummy();
            enemy.MaxHealth = 20f;
            CombatSetup s = Setup(
                new[] { DefaultContent.DuneWarden() },
                enemy, 1,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 1.2f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand cmd = default(PlayerCommand);
            cmd.Aim = new Vec2(0f, 1f);
            cmd.Primary = true;

            int defeatEvents = 0;
            for (int i = 0; i < 60; i++)
            {
                sim.SubmitCommand(0, cmd);
                sim.Step();
                for (int e = 0; e < sim.Events.Count; e++)
                {
                    if (sim.Events[e].Kind == SimEventKind.EntityDefeated && sim.Events[e].TargetId == 1)
                        defeatEvents++;
                }
            }

            Assert.That(sim.GetEnemy(1).Health.IsDefeated, Is.True);
            Assert.That(defeatEvents, Is.EqualTo(1));
        }

        [Test]
        public void EncounterClearedAfterAllEnemiesDefeated()
        {
            EnemySpec enemy = StationaryDummy();
            enemy.MaxHealth = 10f;
            CombatSetup s = Setup(
                new[] { DefaultContent.DuneWarden() },
                enemy, 2,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 1.2f), new Vec2(0.5f, 1.0f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand cmd = default(PlayerCommand);
            cmd.Aim = new Vec2(0f, 1f);
            cmd.Primary = true;

            for (int i = 0; i < 120 && !sim.EncounterCleared; i++)
            {
                sim.SubmitCommand(0, cmd);
                sim.Step();
            }

            Assert.That(sim.EncounterCleared, Is.True);
        }

        [Test]
        public void BindingSealRestrainsEnemy()
        {
            EnemySpec enemy = DefaultContent.HollowSentinel();
            CombatSetup s = Setup(
                new[] { DefaultContent.DuneWarden() },
                enemy, 1,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 6f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand cast = default(PlayerCommand);
            cast.Aim = new Vec2(0f, 1f);
            cast.Special = true;

            for (int i = 0; i < 15; i++)
            {
                sim.SubmitCommand(0, cast);
                sim.Step();
            }

            Assert.That(sim.Cages.Count, Is.EqualTo(1));
            EnemyState e = sim.GetEnemy(1);
            Assert.That(e.RestrainAmount, Is.GreaterThanOrEqualTo(0.99f));

            float yAfterCage = e.Position.Y;
            PlayerCommand idle = default(PlayerCommand);
            for (int i = 0; i < 30; i++)
            {
                sim.SubmitCommand(0, idle);
                sim.Step();
            }

            Assert.That(sim.GetEnemy(1).Position.Y, Is.EqualTo(yAfterCage).Within(0.05f));
        }

        [Test]
        public void SolarConsecrationIgnitesCageAndFireDealsDamage()
        {
            EnemySpec enemy = StationaryDummy();
            CombatSetup s = Setup(
                new[] { DefaultContent.DuneWarden(), DefaultContent.Sunwing() },
                enemy, 1,
                new[] { Vec2.Zero, new Vec2(0f, -3f) },
                new[] { new Vec2(0f, 6f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand cage = default(PlayerCommand);
            cage.Aim = new Vec2(0f, 1f);
            cage.Special = true;
            PlayerCommand idle = default(PlayerCommand);
            for (int i = 0; i < 15; i++)
            {
                sim.SubmitCommand(0, cage);
                sim.SubmitCommand(1, idle);
                sim.Step();
            }

            Assert.That(sim.Cages.Count, Is.EqualTo(1));
            Assert.That(sim.Cages[0].IsIgnited, Is.False);

            PlayerCommand ignite = default(PlayerCommand);
            ignite.Aim = new Vec2(0f, 1f);
            ignite.Special = true;
            for (int i = 0; i < 15; i++)
            {
                sim.SubmitCommand(0, idle);
                sim.SubmitCommand(1, ignite);
                sim.Step();
            }

            Assert.That(sim.Cages[0].IsIgnited, Is.True);
            Assert.That(sim.Cages[0].IgnitedTimeRemaining, Is.GreaterThan(0f));

            float before = sim.GetEnemy(1).Health.Current;
            for (int i = 0; i < 60; i++)
            {
                sim.SubmitCommand(0, idle);
                sim.SubmitCommand(1, idle);
                sim.Step();
            }

            Assert.That(sim.GetEnemy(1).Health.Current, Is.LessThan(before));
        }

        [Test]
        public void DodgeBlocksIncomingDamage()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.Sunwing() },
                ImmediateAttacker(), 1,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 1f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand cmd = default(PlayerCommand);
            cmd.Dodge = true;

            for (int i = 0; i < 10; i++)
            {
                sim.SubmitCommand(0, cmd);
                sim.Step();
            }

            Assert.That(sim.GetPlayer(0).Health.Current, Is.EqualTo(95f).Within(0.0001f));
        }

        [Test]
        public void DamageLandsWhenNotDodging()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.Sunwing() },
                ImmediateAttacker(), 1,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 1f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand idle = default(PlayerCommand);
            for (int i = 0; i < 10; i++)
            {
                sim.SubmitCommand(0, idle);
                sim.Step();
            }

            Assert.That(sim.GetPlayer(0).Health.Current, Is.LessThan(95f));
        }

        [Test]
        public void DefeatedPlayerCommandsAreIgnored()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.DuneWarden() },
                StationaryDummy(), 1,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 6f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerState p = sim.GetPlayer(0);
            p.Health.ApplyDamage(9999f);

            PlayerCommand cmd = default(PlayerCommand);
            cmd.Move = new Vec2(1f, 0f);
            cmd.Primary = true;
            for (int i = 0; i < 5; i++)
            {
                sim.SubmitCommand(0, cmd);
                sim.Step();
            }

            Assert.That(p.Position.X, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(p.Primary.Phase, Is.EqualTo(AbilityPhase.Ready));
        }

        [Test]
        public void SolarConsecrationDoesNotDamageAllies()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.DuneWarden(), DefaultContent.Sunwing() },
                StationaryDummy(), 1,
                new[] { Vec2.Zero, new Vec2(0f, 1f) },
                new[] { new Vec2(0f, 10f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerState guardian = sim.GetPlayer(0);
            PlayerCommand burst = default(PlayerCommand);
            burst.Aim = new Vec2(0f, -1f);
            burst.Special = true;
            PlayerCommand idle = default(PlayerCommand);
            for (int i = 0; i < 15; i++)
            {
                sim.SubmitCommand(0, idle);
                sim.SubmitCommand(1, burst);
                sim.Step();
            }

            Assert.That(guardian.Health.Current, Is.EqualTo(160f).Within(0.0001f));
        }

        [Test]
        public void RestartRestoresInitialState()
        {
            EnemySpec enemy = DefaultContent.HollowSentinel();
            CombatSetup s = Setup(
                new[] { DefaultContent.DuneWarden() },
                enemy, 2,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 1f), new Vec2(1f, 1f) });
            CombatSimulation sim = new CombatSimulation(s);

            sim.GetEnemy(1).Health.ApplyDamage(5f);
            sim.GetPlayer(0).Health.ApplyDamage(10f);
            sim.Reset();

            Assert.That(sim.Tick, Is.EqualTo(0));
            Assert.That(sim.GetEnemy(1).Health.Current, Is.EqualTo(40f).Within(0.0001f));
            Assert.That(sim.GetPlayer(0).Health.Current, Is.EqualTo(160f).Within(0.0001f));
            Assert.That(sim.Cages.Count, Is.EqualTo(0));
            Assert.That(sim.Enemies.Count, Is.EqualTo(2));
        }

        private static int CountEvents(CombatSimulation sim, SimEventKind kind, int targetId)
        {
            int count = 0;
            for (int i = 0; i < sim.Events.Count; i++)
            {
                SimEvent e = sim.Events[i];
                if (e.Kind != kind) continue;
                if (targetId >= 0 && e.TargetId != targetId) continue;
                count++;
            }
            return count;
        }

        private static int CountActivations(CombatSimulation sim, AbilitySlot slot)
        {
            int count = 0;
            for (int i = 0; i < sim.Events.Count; i++)
            {
                SimEvent e = sim.Events[i];
                if (e.Kind == SimEventKind.AbilityActivated && e.ExtraId == (int)slot) count++;
            }
            return count;
        }

        [Test]
        public void CooldownsAdvanceWithoutNewInput()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.Sunwing() },
                StationaryDummy(), 1,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 10f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand fire = default(PlayerCommand);
            fire.Aim = new Vec2(0f, 1f);
            fire.Primary = true;
            sim.SubmitCommand(0, fire);
            sim.Step();

            float started = sim.GetPlayer(0).PrimaryCooldown.Remaining;
            Assert.That(started, Is.GreaterThan(0f));

            PlayerCommand idle = default(PlayerCommand);
            for (int i = 0; i < 60; i++)
            {
                sim.SubmitCommand(0, idle);
                sim.Step();
            }

            Assert.That(sim.GetPlayer(0).PrimaryCooldown.Remaining, Is.LessThan(started));
            Assert.That(sim.GetPlayer(0).PrimaryCooldown.IsReady, Is.True);
        }

        [Test]
        public void SpecialCanReactivateAfterCooldown()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.DuneWarden() },
                StationaryDummy(), 1,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 12f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand cast = default(PlayerCommand);
            cast.Aim = new Vec2(0f, 1f);
            cast.Special = true;
            PlayerCommand idle = default(PlayerCommand);

            int created = 0;
            for (int i = 0; i < 20; i++)
            {
                sim.SubmitCommand(0, cast);
                sim.Step();
                created += CountEvents(sim, SimEventKind.CageCreated, -1);
            }
            Assert.That(created, Is.EqualTo(1));
            Assert.That(sim.GetPlayer(0).SpecialCooldown.IsReady, Is.False);

            for (int i = 0; i < 400; i++)
            {
                sim.SubmitCommand(0, idle);
                sim.Step();
                created += CountEvents(sim, SimEventKind.CageCreated, -1);
            }
            Assert.That(sim.GetPlayer(0).SpecialCooldown.IsReady, Is.True);

            for (int i = 0; i < 20; i++)
            {
                sim.SubmitCommand(0, cast);
                sim.Step();
                created += CountEvents(sim, SimEventKind.CageCreated, -1);
            }
            Assert.That(created, Is.EqualTo(2));
        }

        [Test]
        public void DodgeEndsAndCanReactivateAfterCooldown()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.Sunwing() },
                StationaryDummy(), 1,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 10f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand dodge = default(PlayerCommand);
            dodge.Dodge = true;
            PlayerCommand idle = default(PlayerCommand);

            int activations = 0;
            sim.SubmitCommand(0, dodge);
            sim.Step();
            activations += CountActivations(sim, AbilitySlot.Dodge);
            Assert.That(sim.GetPlayer(0).Dodge.IsActive, Is.True);
            Assert.That(sim.GetPlayer(0).Dodge.IsInvulnerable, Is.True);

            for (int i = 0; i < 20; i++)
            {
                sim.SubmitCommand(0, idle);
                sim.Step();
            }
            Assert.That(sim.GetPlayer(0).Dodge.IsActive, Is.False);

            for (int i = 0; i < 80; i++)
            {
                sim.SubmitCommand(0, idle);
                sim.Step();
            }
            Assert.That(sim.GetPlayer(0).DodgeCooldown.IsReady, Is.True);

            sim.SubmitCommand(0, dodge);
            sim.Step();
            activations += CountActivations(sim, AbilitySlot.Dodge);
            Assert.That(sim.GetPlayer(0).Dodge.IsActive, Is.True);
            Assert.That(activations, Is.EqualTo(2));
        }

        private static EnemySpec RapidAttacker()
        {
            EnemySpec e = DefaultContent.HollowSentinel();
            e.MoveSpeed = 0f;
            e.AttackRange = 5f;
            e.AttackWindup = 0f;
            e.AttackActive = 0.05f;
            e.AttackRecovery = 0.1f;
            e.AttackCooldown = 0.3f;
            e.AttackDamage = 5f;
            return e;
        }

        [Test]
        public void EnemyDamagesSameTargetOnMultipleAttackCycles()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.DuneWarden() },
                RapidAttacker(), 1,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 1f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand idle = default(PlayerCommand);
            int hits = 0;
            for (int i = 0; i < 300; i++)
            {
                sim.SubmitCommand(0, idle);
                sim.Step();
                hits += CountEvents(sim, SimEventKind.DamageDealt, 0);
            }

            Assert.That(sim.GetPlayer(0).Health.IsDefeated, Is.False);
            Assert.That(hits, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void DodgeBlocksDamageDuringWindowThenTakesDamageAfter()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.Sunwing() },
                RapidAttacker(), 1,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 1f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand dodge = default(PlayerCommand);
            dodge.Dodge = true;
            sim.SubmitCommand(0, dodge);
            sim.Step();

            Assert.That(sim.GetPlayer(0).Health.Current, Is.EqualTo(95f).Within(0.0001f));

            PlayerCommand idle = default(PlayerCommand);
            int hits = 0;
            for (int i = 0; i < 300; i++)
            {
                sim.SubmitCommand(0, idle);
                sim.Step();
                hits += CountEvents(sim, SimEventKind.DamageDealt, 0);
            }

            Assert.That(hits, Is.GreaterThanOrEqualTo(1));
            Assert.That(sim.GetPlayer(0).Health.Current, Is.LessThan(95f));
            Assert.That(sim.GetPlayer(0).Health.IsDefeated, Is.False);
        }

        [Test]
        public void StatePersistsAcrossSuccessiveTicks()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.DuneWarden() },
                StationaryDummy(), 1,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 1.2f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand strike = default(PlayerCommand);
            strike.Aim = new Vec2(0f, 1f);
            strike.Primary = true;
            for (int i = 0; i < 20; i++)
            {
                sim.SubmitCommand(0, strike);
                sim.Step();
            }

            float enemyHealth = sim.GetEnemy(1).Health.Current;
            float remaining = sim.GetPlayer(0).PrimaryCooldown.Remaining;
            Assert.That(enemyHealth, Is.LessThan(40f));
            Assert.That(remaining, Is.GreaterThan(0f));

            PlayerCommand idle = default(PlayerCommand);
            for (int i = 0; i < 10; i++)
            {
                sim.SubmitCommand(0, idle);
                sim.Step();
            }

            Assert.That(sim.GetEnemy(1).Health.Current, Is.EqualTo(enemyHealth).Within(0.0001f));
            Assert.That(sim.GetPlayer(0).PrimaryCooldown.Remaining, Is.LessThan(remaining));
        }

        [Test]
        public void SpecialUsesRequestedTargetPointWhenInRange()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.DuneWarden() },
                StationaryDummy(), 1,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 12f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand cast = default(PlayerCommand);
            cast.Special = true;
            cast.HasTargetPoint = true;
            cast.TargetPoint = new Vec2(0f, 4f);
            for (int i = 0; i < 20; i++)
            {
                sim.SubmitCommand(0, cast);
                sim.Step();
            }

            Assert.That(sim.Cages.Count, Is.EqualTo(1));
            Assert.That(Vec2.Distance(sim.Cages[0].Position, new Vec2(0f, 4f)), Is.LessThan(0.05f));
            Assert.That(sim.GetPlayer(0).AimTargetClamped, Is.False);
        }

        [Test]
        public void SpecialTargetIsClampedToCastRange()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.DuneWarden() },
                StationaryDummy(), 1,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 12f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand cast = default(PlayerCommand);
            cast.Special = true;
            cast.HasTargetPoint = true;
            cast.TargetPoint = new Vec2(0f, 20f);
            for (int i = 0; i < 20; i++)
            {
                sim.SubmitCommand(0, cast);
                sim.Step();
            }

            Assert.That(sim.Cages.Count, Is.EqualTo(1));
            Assert.That(Vec2.Distance(sim.Cages[0].Position, new Vec2(0f, 6f)), Is.LessThan(0.05f));
            Assert.That(sim.GetPlayer(0).AimTargetClamped, Is.True);
            Assert.That(sim.GetPlayer(0).AimTarget.Y, Is.GreaterThan(0f));
        }

        [Test]
        public void CageExpiresThenNewCastCreatesSecondCage()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.DuneWarden() },
                StationaryDummy(), 1,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 12f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand cast = default(PlayerCommand);
            cast.Special = true;
            cast.HasTargetPoint = true;
            cast.TargetPoint = new Vec2(0f, 4f);
            PlayerCommand idle = default(PlayerCommand);

            int created = 0;
            int expired = 0;
            for (int i = 0; i < 20; i++)
            {
                sim.SubmitCommand(0, cast);
                sim.Step();
                created += CountEvents(sim, SimEventKind.CageCreated, -1);
                expired += CountEvents(sim, SimEventKind.CageExpired, -1);
            }
            Assert.That(created, Is.EqualTo(1));

            for (int i = 0; i < 400; i++)
            {
                sim.SubmitCommand(0, idle);
                sim.Step();
                created += CountEvents(sim, SimEventKind.CageCreated, -1);
                expired += CountEvents(sim, SimEventKind.CageExpired, -1);
            }

            Assert.That(expired, Is.GreaterThanOrEqualTo(1));
            Assert.That(sim.GetPlayer(0).SpecialCooldown.IsReady, Is.True);

            for (int i = 0; i < 20; i++)
            {
                sim.SubmitCommand(0, cast);
                sim.Step();
                created += CountEvents(sim, SimEventKind.CageCreated, -1);
                expired += CountEvents(sim, SimEventKind.CageExpired, -1);
            }

            Assert.That(created, Is.EqualTo(2));
            Assert.That(sim.Cages.Count, Is.EqualTo(1));
            Assert.That(Vec2.Distance(sim.Cages[0].Position, new Vec2(0f, 4f)), Is.LessThan(0.05f));
        }

        [Test]
        public void DiscreteSpecialPressCastsOnce()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.DuneWarden() },
                StationaryDummy(), 1,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 12f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand cast = default(PlayerCommand);
            cast.Special = true;
            PlayerCommand idle = default(PlayerCommand);

            int activations = 0;
            sim.SubmitCommand(0, cast);
            sim.Step();
            activations += CountActivations(sim, AbilitySlot.Special);

            for (int i = 0; i < 5; i++)
            {
                sim.SubmitCommand(0, idle);
                sim.Step();
                activations += CountActivations(sim, AbilitySlot.Special);
            }

            for (int i = 0; i < 5; i++)
            {
                sim.SubmitCommand(0, cast);
                sim.Step();
                activations += CountActivations(sim, AbilitySlot.Special);
            }

            Assert.That(activations, Is.EqualTo(1));
            Assert.That(sim.GetPlayer(0).Special.Phase, Is.Not.EqualTo(AbilityPhase.Ready));
        }

        [Test]
        public void DodgeDirectionUsesMoveInputWhenMoving()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.Sunwing() },
                StationaryDummy(), 1,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 12f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand cmd = default(PlayerCommand);
            cmd.Move = new Vec2(1f, 0f);
            cmd.Dodge = true;
            sim.SubmitCommand(0, cmd);
            sim.Step();

            Vec2 dir = sim.GetPlayer(0).Dodge.Direction;
            Assert.That(Vec2.Distance(dir, new Vec2(1f, 0f)), Is.LessThan(0.001f));
        }

        [Test]
        public void DodgeDirectionUsesFacingWhenStationary()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.Sunwing() },
                StationaryDummy(), 1,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 12f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand cmd = default(PlayerCommand);
            cmd.Aim = new Vec2(0f, 1f);
            cmd.Dodge = true;
            sim.SubmitCommand(0, cmd);
            sim.Step();

            Vec2 dir = sim.GetPlayer(0).Dodge.Direction;
            Assert.That(Vec2.Distance(dir, new Vec2(0f, 1f)), Is.LessThan(0.001f));
        }

        [Test]
        public void DodgeRejectedDuringCooldownThenRecovers()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.Sunwing() },
                StationaryDummy(), 1,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 12f) });
            CombatSimulation sim = new CombatSimulation(s);

            PlayerCommand dodge = default(PlayerCommand);
            dodge.Dodge = true;
            PlayerCommand idle = default(PlayerCommand);

            int activations = 0;
            sim.SubmitCommand(0, dodge);
            sim.Step();
            activations += CountActivations(sim, AbilitySlot.Dodge);
            Assert.That(sim.GetPlayer(0).LastRequestAccepted, Is.True);

            for (int i = 0; i < 20; i++)
            {
                sim.SubmitCommand(0, idle);
                sim.Step();
                activations += CountActivations(sim, AbilitySlot.Dodge);
            }
            Assert.That(sim.GetPlayer(0).Dodge.IsActive, Is.False);

            sim.SubmitCommand(0, dodge);
            sim.Step();
            activations += CountActivations(sim, AbilitySlot.Dodge);
            Assert.That(sim.GetPlayer(0).LastRequestAccepted, Is.False);
            Assert.That(sim.GetPlayer(0).LastRequestReason, Does.Contain("cooldown"));

            for (int i = 0; i < 80; i++)
            {
                sim.SubmitCommand(0, idle);
                sim.Step();
                activations += CountActivations(sim, AbilitySlot.Dodge);
            }

            sim.SubmitCommand(0, dodge);
            sim.Step();
            activations += CountActivations(sim, AbilitySlot.Dodge);
            Assert.That(sim.GetPlayer(0).LastRequestAccepted, Is.True);
            Assert.That(activations, Is.EqualTo(2));
        }

        [Test]
        public void DodgeCooldownDisplayTracksSimulationState()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.Sunwing() },
                StationaryDummy(), 1,
                new[] { Vec2.Zero },
                new[] { new Vec2(0f, 12f) });
            CombatSimulation sim = new CombatSimulation(s);
            PlayerState p = sim.GetPlayer(0);

            Assert.That(CooldownDisplay.Label(p.DodgeCooldown), Is.EqualTo("ready"));

            PlayerCommand dodge = default(PlayerCommand);
            dodge.Dodge = true;
            sim.SubmitCommand(0, dodge);
            sim.Step();

            Assert.That(p.DodgeCooldown.IsReady, Is.False);
            Assert.That(CooldownDisplay.Label(p.DodgeCooldown), Is.Not.EqualTo("ready"));

            PlayerCommand idle = default(PlayerCommand);
            for (int i = 0; i < 20; i++)
            {
                sim.SubmitCommand(0, idle);
                sim.Step();
            }
            Assert.That(p.Dodge.IsActive, Is.False);
            Assert.That(p.DodgeCooldown.IsReady, Is.False);

            float beforeReject = p.DodgeCooldown.Remaining;
            sim.SubmitCommand(0, dodge);
            sim.Step();

            Assert.That(p.LastRequestAccepted, Is.False);
            Assert.That(p.DodgeCooldown.Remaining, Is.GreaterThan(0f));
            Assert.That(p.DodgeCooldown.Remaining, Is.LessThanOrEqualTo(beforeReject));

            for (int i = 0; i < 80; i++)
            {
                sim.SubmitCommand(0, idle);
                sim.Step();
            }
            Assert.That(CooldownDisplay.Label(p.DodgeCooldown), Is.EqualTo("ready"));
        }
    }
}
