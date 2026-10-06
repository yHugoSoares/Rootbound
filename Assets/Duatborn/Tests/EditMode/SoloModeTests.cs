using NUnit.Framework;
using Duatborn.Core;

namespace Duatborn.Tests
{
    public class SoloModeTests
    {
        [Test]
        public void SoloSetupHasExactlyOnePlayer()
        {
            Assert.That(CombatSetup.DefaultSolo(CreatureKind.DuneWarden).PlayerSpecs.Length, Is.EqualTo(1));
            Assert.That(CombatSetup.DefaultSolo(CreatureKind.Sunwing).PlayerSpecs.Length, Is.EqualTo(1));
        }

        [Test]
        public void SoloSetupUsesSelectedCreature()
        {
            Assert.That(CombatSetup.DefaultSolo(CreatureKind.DuneWarden).PlayerSpecs[0].Kind,
                Is.EqualTo(CreatureKind.DuneWarden));
            Assert.That(CombatSetup.DefaultSolo(CreatureKind.Sunwing).PlayerSpecs[0].Kind,
                Is.EqualTo(CreatureKind.Sunwing));
        }

        [Test]
        public void SoloEncounterClearsWithOnePlayer()
        {
            CombatSimulation sim = new CombatSimulation(CombatSetup.DefaultSolo(CreatureKind.DuneWarden));

            Assert.That(sim.Players.Count, Is.EqualTo(1));

            for (int i = 0; i < sim.Enemies.Count; i++)
                sim.Enemies[i].Health.ApplyDamage(1000f);
            sim.Step();

            Assert.That(sim.EncounterCleared, Is.True);
            Assert.That(sim.AllPlayersDefeated, Is.False);
        }

        [Test]
        public void SoloDefeatTriggersAndRestartClearsState()
        {
            CombatSimulation sim = new CombatSimulation(CombatSetup.DefaultSolo(CreatureKind.Sunwing));

            sim.GetPlayer(0).Health.ApplyDamage(1000f);
            sim.Step();

            Assert.That(sim.AllPlayersDefeated, Is.True);

            sim.Reset();

            Assert.That(sim.Players.Count, Is.EqualTo(1));
            Assert.That(sim.AllPlayersDefeated, Is.False);
            Assert.That(sim.EncounterCleared, Is.False);
            Assert.That(sim.GetPlayer(0).Health.IsDefeated, Is.False);
            Assert.That(sim.GetPlayer(0).Health.Current, Is.EqualTo(sim.GetPlayer(0).Health.Max).Within(0.0001f));
        }

        [Test]
        public void SoloPrimaryAttackDamagesEnemy()
        {
            CombatSetup setup = CombatSetup.DefaultSolo(CreatureKind.DuneWarden);
            setup.EnemyCount = 1;
            setup.PlayerSpawns = new[] { Vec2.Zero };
            setup.EnemySpawns = new[] { new Vec2(0f, 1.2f) };
            CombatSimulation sim = new CombatSimulation(setup);

            PlayerCommand cmd = default(PlayerCommand);
            cmd.Aim = new Vec2(0f, 1f);
            cmd.Primary = true;
            for (int i = 0; i < 12; i++)
            {
                sim.SubmitCommand(0, cmd);
                sim.Step();
            }

            Assert.That(sim.Enemies[0].Health.Current, Is.LessThan(sim.Enemies[0].Health.Max));
        }
    }
}
