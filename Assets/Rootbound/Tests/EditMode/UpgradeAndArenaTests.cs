using NUnit.Framework;
using Rootbound.Core;

namespace Rootbound.Tests
{
    public class UpgradeAndArenaTests
    {
        [Test]
        public void CatalogHasAtLeastThreeUpgrades()
        {
            Assert.That(DefaultContent.UpgradeCatalog().Length, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void MaxHealthUpgradeRaisesMaxHealth()
        {
            CreatureSpec spec = DefaultContent.RootGuardian();
            float before = spec.MaxHealth;
            UpgradeSpec upgrade = new UpgradeSpec { Kind = UpgradeKind.MaxHealth, Amount = 30f };

            UpgradeRules.Apply(upgrade, spec);

            Assert.That(spec.MaxHealth, Is.EqualTo(before + 30f).Within(0.0001f));
        }

        [Test]
        public void PrimaryDamageUpgradeRaisesPrimaryDamage()
        {
            CreatureSpec spec = DefaultContent.EmberMoth();
            float before = spec.Primary.Damage;
            UpgradeSpec upgrade = new UpgradeSpec { Kind = UpgradeKind.PrimaryDamage, Amount = 6f };

            UpgradeRules.Apply(upgrade, spec);

            Assert.That(spec.Primary.Damage, Is.EqualTo(before + 6f).Within(0.0001f));
        }

        [Test]
        public void MoveSpeedUpgradeRaisesMoveSpeed()
        {
            CreatureSpec spec = DefaultContent.RootGuardian();
            float before = spec.MoveSpeed;
            UpgradeRules.Apply(new UpgradeSpec { Kind = UpgradeKind.MoveSpeed, Amount = 0.6f }, spec);
            Assert.That(spec.MoveSpeed, Is.EqualTo(before + 0.6f).Within(0.0001f));
        }

        [Test]
        public void MixedArenaSpawnsBothEnemyTypes()
        {
            ArenaSpec[] rooms = DefaultContent.DefaultRun();
            ArenaSpec room = rooms[1];
            CombatSimulation sim = new CombatSimulation(CombatSetup.FromArena(CombatSetup.DefaultTwoPlayer().PlayerSpecs, room, true));

            int blight = 0;
            int spore = 0;
            for (int i = 0; i < sim.Enemies.Count; i++)
            {
                if (sim.Enemies[i].Spec.Id == "sporeling") spore++;
                else blight++;
            }

            Assert.That(spore, Is.EqualTo(3));
            Assert.That(blight, Is.EqualTo(4));
            Assert.That(sim.Enemies.Count, Is.EqualTo(room.EnemyCount));
        }

        [Test]
        public void LobbyArenaSpawnsNoEnemies()
        {
            ArenaSpec room = DefaultContent.DefaultRun()[0];
            CombatSimulation sim = new CombatSimulation(CombatSetup.FromArena(CombatSetup.DefaultTwoPlayer().PlayerSpecs, room, false));
            Assert.That(sim.Enemies.Count, Is.EqualTo(0));
        }
    }
}
