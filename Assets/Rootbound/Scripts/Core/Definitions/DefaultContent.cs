namespace Rootbound.Core
{
    public static class DefaultContent
    {
        public const float DegreesToRadians = 0.0174532924f;

        public static CreatureSpec RootGuardian()
        {
            CreatureSpec c = new CreatureSpec();
            c.Kind = CreatureKind.RootGuardian;
            c.Id = "root_guardian";
            c.DisplayName = "Dune Warden";
            c.MaxHealth = 160f;
            c.MoveSpeed = 4.2f;
            c.BodyRadius = 0.45f;
            c.Dodge.Distance = 3.0f;
            c.Dodge.Duration = 0.28f;
            c.Dodge.InvulnStart = 0.02f;
            c.Dodge.InvulnEnd = 0.26f;
            c.Dodge.Cooldown = 1.1f;

            c.Primary.Kind = AttackKind.MeleeSweep;
            c.Primary.Cooldown = 0.55f;
            c.Primary.Damage = 22f;
            c.Primary.Range = 1.9f;
            c.Primary.ArcDegrees = 110f;
            c.Primary.Windup = 0.06f;
            c.Primary.Active = 0.10f;
            c.Primary.Recovery = 0.18f;
            c.Primary.HitRadius = 0.4f;

            c.Special.Kind = SpecialKind.RootCage;
            c.Special.Cooldown = 5.5f;
            c.Special.Damage = 0f;
            c.Special.CastRange = 6.0f;
            c.Special.Radius = 2.2f;
            c.Special.Duration = 3.5f;
            c.Special.RestrainFactor = 1.0f;
            c.Special.Windup = 0.15f;
            c.Special.Active = 0.05f;
            c.Special.Recovery = 0.35f;
            c.Special.FireTickInterval = 0.5f;
            c.Special.FireDamagePerTick = 8f;
            c.Special.MaxIgnitionDuration = 3.0f;
            c.Special.IgnitionExtension = 2.5f;
            return c;
        }

        public static CreatureSpec EmberMoth()
        {
            CreatureSpec c = new CreatureSpec();
            c.Kind = CreatureKind.EmberMoth;
            c.Id = "ember_moth";
            c.DisplayName = "Sunwing";
            c.MaxHealth = 95f;
            c.MoveSpeed = 5.8f;
            c.BodyRadius = 0.35f;
            c.Dodge.Distance = 4.0f;
            c.Dodge.Duration = 0.18f;
            c.Dodge.InvulnStart = 0.0f;
            c.Dodge.InvulnEnd = 0.16f;
            c.Dodge.Cooldown = 0.9f;

            c.Primary.Kind = AttackKind.EmberProjectile;
            c.Primary.Cooldown = 0.42f;
            c.Primary.Damage = 16f;
            c.Primary.Range = 10f;
            c.Primary.ArcDegrees = 0f;
            c.Primary.Windup = 0.05f;
            c.Primary.Active = 0.02f;
            c.Primary.Recovery = 0.12f;
            c.Primary.ProjectileSpeed = 14f;
            c.Primary.ProjectileRadius = 0.22f;
            c.Primary.ProjectileLifetime = 1.6f;

            c.Special.Kind = SpecialKind.IgnitionBurst;
            c.Special.Cooldown = 6.0f;
            c.Special.Damage = 12f;
            c.Special.CastRange = 9.0f;
            c.Special.Radius = 2.5f;
            c.Special.Duration = 0f;
            c.Special.RestrainFactor = 0f;
            c.Special.Windup = 0.12f;
            c.Special.Active = 0.05f;
            c.Special.Recovery = 0.25f;
            c.Special.FireTickInterval = 0.5f;
            c.Special.FireDamagePerTick = 8f;
            c.Special.MaxIgnitionDuration = 3.0f;
            c.Special.IgnitionExtension = 2.5f;
            return c;
        }

        public static ArenaSpec[] DefaultRun()
        {
            return new[]
            {
                MakeArena("room_1", "Sun Gate", false, Group(Blightling(), 6)),
                MakeArena("room_2", "Shadow Gate", false, Group(Blightling(), 4), Group(Sporeling(), 3)),
                MakeArena("room_3", "Horizon Gate", true, Group(Blightling(), 5), Group(Sporeling(), 3))
            };
        }

        public static Vec2[] UpgradePositions()
        {
            return new[] { new Vec2(-3f, 0f), new Vec2(0f, 3f), new Vec2(3f, 0f) };
        }

        public static UpgradeSpec[] UpgradeCatalog()
        {
            UpgradeSpec vigor = new UpgradeSpec();
            vigor.Id = "up_vigor";
            vigor.DisplayName = "Sun's Vitality (+30 HP)";
            vigor.Kind = UpgradeKind.MaxHealth;
            vigor.Amount = 30f;

            UpgradeSpec edge = new UpgradeSpec();
            edge.Id = "up_edge";
            edge.DisplayName = "Khopesh Edge (+6 damage)";
            edge.Kind = UpgradeKind.PrimaryDamage;
            edge.Amount = 6f;

            UpgradeSpec fleet = new UpgradeSpec();
            fleet.Id = "up_fleet";
            fleet.DisplayName = "Swift Sandals (+0.6 speed)";
            fleet.Kind = UpgradeKind.MoveSpeed;
            fleet.Amount = 0.6f;

            UpgradeSpec overgrowth = new UpgradeSpec();
            overgrowth.Id = "up_overgrowth";
            overgrowth.DisplayName = "Widened Rite (+1.0 special radius)";
            overgrowth.Kind = UpgradeKind.SpecialRadius;
            overgrowth.Amount = 1.0f;

            UpgradeSpec lingering = new UpgradeSpec();
            lingering.Id = "up_lingering";
            lingering.DisplayName = "Enduring Seal (+1.5 seal duration)";
            lingering.Kind = UpgradeKind.SpecialDuration;
            lingering.Amount = 1.5f;

            UpgradeSpec siphoning = new UpgradeSpec();
            siphoning.Id = "up_siphoning";
            siphoning.DisplayName = "Funerary Offering (heal 6 on kill)";
            siphoning.Kind = UpgradeKind.LifeOnKill;
            siphoning.Amount = 6f;

            return new[] { vigor, edge, fleet, overgrowth, lingering, siphoning };
        }

        public static int[] PickUpgradeIndices(int count, CreatureKind[] creatures, System.Random random)
        {
            UpgradeSpec[] catalog = UpgradeCatalog();
            System.Collections.Generic.List<int> pool = new System.Collections.Generic.List<int>();
            for (int i = 0; i < catalog.Length; i++)
                if (UpgradeRules.AppliesToAny(catalog[i], creatures)) pool.Add(i);
            if (count > pool.Count) count = pool.Count;
            if (count < 0) count = 0;

            int[] result = new int[count];
            for (int i = 0; i < count; i++)
            {
                int pick = random.Next(pool.Count);
                result[i] = pool[pick];
                pool.RemoveAt(pick);
            }
            return result;
        }

        private static ArenaEnemyGroup Group(EnemySpec enemy, int count)
        {
            ArenaEnemyGroup group = new ArenaEnemyGroup();
            group.Enemy = enemy;
            group.Count = count;
            return group;
        }

        private static ArenaSpec MakeArena(string id, string name, bool finale, params ArenaEnemyGroup[] groups)
        {
            ArenaSpec arena = new ArenaSpec();
            arena.Id = id;
            arena.DisplayName = name;
            arena.Groups = groups;
            int total = 0;
            for (int i = 0; i < groups.Length; i++) total += groups[i].Count;
            arena.EnemyCount = total;
            arena.Enemy = groups.Length > 0 ? groups[0].Enemy : Blightling();
            arena.IsFinale = finale;
            return arena;
        }

        public static EnemySpec Sporeling()
        {
            EnemySpec e = new EnemySpec();
            e.Id = "sporeling";
            e.DisplayName = "Fractured Vessel";
            e.MaxHealth = 22f;
            e.MoveSpeed = 4.2f;
            e.BodyRadius = 0.3f;
            e.AttackRange = 1.0f;
            e.AttackArcDegrees = 90f;
            e.AttackDamage = 6f;
            e.AttackCooldown = 0.9f;
            e.AttackWindup = 0.25f;
            e.AttackActive = 0.08f;
            e.AttackRecovery = 0.2f;
            e.DeathBurstRadius = 2.0f;
            e.DeathBurstDamage = 8f;
            return e;
        }

        public static EnemySpec Blightling()
        {
            EnemySpec e = new EnemySpec();
            e.Id = "blightling";
            e.DisplayName = "Hollow Sentinel";
            e.MaxHealth = 40f;
            e.MoveSpeed = 2.6f;
            e.BodyRadius = 0.4f;
            e.AttackRange = 1.2f;
            e.AttackArcDegrees = 90f;
            e.AttackDamage = 10f;
            e.AttackCooldown = 1.3f;
            e.AttackWindup = 0.35f;
            e.AttackActive = 0.10f;
            e.AttackRecovery = 0.25f;
            return e;
        }
    }
}
