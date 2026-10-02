using NUnit.Framework;
using Rootbound.Core;

namespace Rootbound.Tests
{
    public class CombatSimulationTests
    {
        private static EnemySpec StationaryDummy()
        {
            EnemySpec e = DefaultContent.Blightling();
            e.MoveSpeed = 0f;
            return e;
        }

        private static EnemySpec ImmediateAttacker()
        {
            EnemySpec e = DefaultContent.Blightling();
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
                new[] { DefaultContent.RootGuardian() },
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
                new[] { DefaultContent.RootGuardian() },
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
                new[] { DefaultContent.EmberMoth(), DefaultContent.EmberMoth() },
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
                new[] { DefaultContent.RootGuardian() },
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
                new[] { DefaultContent.RootGuardian() },
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
        public void RootCageRestrainsEnemy()
        {
            EnemySpec enemy = DefaultContent.Blightling();
            CombatSetup s = Setup(
                new[] { DefaultContent.RootGuardian() },
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
        public void IgnitionBurstIgnitesCageAndFireDealsDamage()
        {
            EnemySpec enemy = StationaryDummy();
            CombatSetup s = Setup(
                new[] { DefaultContent.RootGuardian(), DefaultContent.EmberMoth() },
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
                new[] { DefaultContent.EmberMoth() },
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
                new[] { DefaultContent.EmberMoth() },
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
                new[] { DefaultContent.RootGuardian() },
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
        public void IgnitionBurstDoesNotDamageAllies()
        {
            CombatSetup s = Setup(
                new[] { DefaultContent.RootGuardian(), DefaultContent.EmberMoth() },
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
            EnemySpec enemy = DefaultContent.Blightling();
            CombatSetup s = Setup(
                new[] { DefaultContent.RootGuardian() },
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
    }
}
