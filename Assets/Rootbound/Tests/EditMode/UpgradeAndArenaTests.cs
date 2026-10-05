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
        public void SealDurationUpgradeIsNotOfferedToSunwingOnly()
        {
            int[] indices = DefaultContent.PickUpgradeIndices(6, new[] { CreatureKind.EmberMoth }, new System.Random(7));
            UpgradeSpec[] catalog = DefaultContent.UpgradeCatalog();
            for (int i = 0; i < indices.Length; i++)
                Assert.That(catalog[indices[i]].Kind, Is.Not.EqualTo(UpgradeKind.SpecialDuration));
        }

        [Test]
        public void SealDurationUpgradeIsOfferedToDuneWarden()
        {
            int[] indices = DefaultContent.PickUpgradeIndices(6, new[] { CreatureKind.RootGuardian }, new System.Random(0));
            UpgradeSpec[] catalog = DefaultContent.UpgradeCatalog();
            bool found = false;
            for (int i = 0; i < indices.Length; i++)
                if (catalog[indices[i]].Kind == UpgradeKind.SpecialDuration) found = true;
            Assert.That(found, Is.True);
        }

        [Test]
        public void PickUpgradeIndicesReturnsDistinctIndicesInRange()
        {
            int[] indices = DefaultContent.PickUpgradeIndices(3, new[] { CreatureKind.RootGuardian }, new System.Random(1234));
            Assert.That(indices.Length, Is.EqualTo(3));

            System.Collections.Generic.HashSet<int> unique = new System.Collections.Generic.HashSet<int>(indices);
            Assert.That(unique.Count, Is.EqualTo(3));
            for (int i = 0; i < indices.Length; i++)
                Assert.That(indices[i], Is.InRange(0, DefaultContent.UpgradeCatalog().Length - 1));
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
        public void SpecialRadiusUpgradeRaisesSpecialRadius()
        {
            CreatureSpec spec = DefaultContent.RootGuardian();
            float before = spec.Special.Radius;
            UpgradeRules.Apply(new UpgradeSpec { Kind = UpgradeKind.SpecialRadius, Amount = 1.0f }, spec);
            Assert.That(spec.Special.Radius, Is.EqualTo(before + 1.0f).Within(0.0001f));
        }

        [Test]
        public void SpecialDurationUpgradeRaisesSpecialDuration()
        {
            CreatureSpec spec = DefaultContent.RootGuardian();
            float before = spec.Special.Duration;
            UpgradeRules.Apply(new UpgradeSpec { Kind = UpgradeKind.SpecialDuration, Amount = 1.5f }, spec);
            Assert.That(spec.Special.Duration, Is.EqualTo(before + 1.5f).Within(0.0001f));
        }

        [Test]
        public void LifeOnKillHealsTheKiller()
        {
            EnemySpec blight = DefaultContent.Blightling();
            blight.MaxHealth = 10f;
            blight.AttackDamage = 0f;
            CombatSetup setup = new CombatSetup();
            setup.PlayerSpecs = new[] { DefaultContent.RootGuardian() };
            setup.EnemySpec = blight;
            setup.EnemyCount = 1;
            setup.PlayerSpawns = new[] { Vec2.Zero };
            setup.EnemySpawns = new[] { new Vec2(0f, 1f) };
            CombatSimulation sim = new CombatSimulation(setup);
            sim.GetPlayer(0).Spec.LifeOnKill = 12f;
            sim.GetPlayer(0).Health.ApplyDamage(30f);
            float damaged = sim.GetPlayer(0).Health.Current;

            PlayerCommand strike = default(PlayerCommand);
            strike.Aim = new Vec2(0f, 1f);
            strike.Primary = true;
            for (int i = 0; i < 12; i++)
            {
                sim.SubmitCommand(0, strike);
                sim.Step();
            }

            Assert.That(sim.Enemies[0].Health.IsDefeated, Is.True);
            Assert.That(sim.GetPlayer(0).Health.Current, Is.EqualTo(damaged + 12f).Within(0.0001f));
        }

        [Test]
        public void SporelingDeathBurstDamagesNearbyPlayer()
        {
            EnemySpec spore = DefaultContent.Sporeling();
            spore.AttackDamage = 0f;

            CombatSetup setup = new CombatSetup();
            setup.PlayerSpecs = new[] { DefaultContent.RootGuardian() };
            setup.EnemySpec = spore;
            setup.EnemySpecs = new[] { spore };
            setup.EnemyCount = 1;
            setup.PlayerSpawns = new[] { Vec2.Zero };
            setup.EnemySpawns = new[] { new Vec2(0f, 1f) };

            CombatSimulation sim = new CombatSimulation(setup);
            float before = sim.GetPlayer(0).Health.Current;

            PlayerCommand strike = default(PlayerCommand);
            strike.Aim = new Vec2(0f, 1f);
            strike.Primary = true;
            for (int i = 0; i < 30; i++)
            {
                sim.SubmitCommand(0, strike);
                sim.Step();
            }

            Assert.That(sim.Enemies[0].Health.IsDefeated, Is.True);
            Assert.That(sim.GetPlayer(0).Health.Current, Is.LessThan(before),
                "Sporeling death burst did not damage the nearby player.");
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
