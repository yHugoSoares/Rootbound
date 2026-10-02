namespace Rootbound.Core
{
    public sealed class PlayerState
    {
        public int Id;
        public CreatureSpec Spec;
        public Vec2 Position;
        public Vec2 Facing = new Vec2(0f, 1f);
        public Vec2 MoveInput;

        public Health Health;

        public Cooldown PrimaryCooldown;
        public Cooldown SpecialCooldown;
        public Cooldown DodgeCooldown;

        public AbilityExecution Primary;
        public AbilityExecution Special;
        public DodgeState Dodge;

        public bool IsDefeated { get { return Health.IsDefeated; } }
    }

    public sealed class EnemyState
    {
        public int Id;
        public EnemySpec Spec;
        public Vec2 Position;
        public Vec2 Facing = new Vec2(0f, -1f);

        public Health Health;

        public Cooldown AttackCooldown;
        public AbilityExecution Attack;

        public float RestrainAmount;
        public bool IsRestrained;

        public bool IsDefeated { get { return Health.IsDefeated; } }
    }

    public sealed class ProjectileState
    {
        public int Id;
        public int OwnerPlayerId;
        public Vec2 Position;
        public Vec2 Velocity;
        public float Damage;
        public float Radius;
        public float RemainingLife;
        public bool IsAlive = true;
    }
}
