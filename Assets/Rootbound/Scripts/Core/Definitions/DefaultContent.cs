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
            c.DisplayName = "Root Guardian";
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
            c.DisplayName = "Ember Moth";
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

        public static EnemySpec Blightling()
        {
            EnemySpec e = new EnemySpec();
            e.Id = "blightling";
            e.DisplayName = "Blightling";
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
