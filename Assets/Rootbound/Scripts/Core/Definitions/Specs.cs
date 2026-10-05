namespace Rootbound.Core
{
    public enum Team
    {
        Players = 0,
        Enemies = 1
    }

    public enum CreatureKind
    {
        RootGuardian = 0,
        EmberMoth = 1
    }

    public enum AttackKind
    {
        MeleeSweep = 0,
        EmberProjectile = 1
    }

    public enum SpecialKind
    {
        RootCage = 0,
        IgnitionBurst = 1
    }

    [System.Serializable]
    public sealed class DodgeSpec
    {
        public float Distance;
        public float Duration;
        public float InvulnStart;
        public float InvulnEnd;
        public float Cooldown;
    }

    [System.Serializable]
    public sealed class AttackSpec
    {
        public AttackKind Kind;
        public float Cooldown;
        public float Damage;
        public float Range;
        public float ArcDegrees;
        public float Windup;
        public float Active;
        public float Recovery;
        public float HitRadius;
        public float ProjectileSpeed;
        public float ProjectileRadius;
        public float ProjectileLifetime;
    }

    [System.Serializable]
    public sealed class SpecialSpec
    {
        public SpecialKind Kind;
        public float Cooldown;
        public float Damage;
        public float CastRange;
        public float Radius;
        public float Duration;
        public float RestrainFactor;
        public float Windup;
        public float Active;
        public float Recovery;
        public float FireTickInterval;
        public float FireDamagePerTick;
        public float MaxIgnitionDuration;
        public float IgnitionExtension;
    }

    [System.Serializable]
    public sealed class CreatureSpec
    {
        public CreatureKind Kind;
        public string Id;
        public string DisplayName;
        public float MaxHealth;
        public float MoveSpeed;
        public float BodyRadius;
        public DodgeSpec Dodge = new DodgeSpec();
        public AttackSpec Primary = new AttackSpec();
        public SpecialSpec Special = new SpecialSpec();
    }

    [System.Serializable]
    public sealed class EnemySpec
    {
        public string Id;
        public string DisplayName;
        public float MaxHealth;
        public float MoveSpeed;
        public float BodyRadius;
        public float AttackRange;
        public float AttackArcDegrees;
        public float AttackDamage;
        public float AttackCooldown;
        public float AttackWindup;
        public float AttackActive;
        public float AttackRecovery;
        public float DeathBurstRadius;
        public float DeathBurstDamage;
    }
}
