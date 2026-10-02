namespace Rootbound.Core
{
    public enum AbilitySlot
    {
        Primary = 0,
        Special = 1,
        Dodge = 2
    }

    public struct PlayerCommand
    {
        public Vec2 Move;
        public Vec2 Aim;
        public bool Primary;
        public bool Special;
        public bool Dodge;
    }

    public enum SimEventKind
    {
        DamageDealt = 0,
        EntityDefeated = 1,
        AbilityActivated = 2,
        ProjectileSpawned = 3,
        CageCreated = 4,
        CageIgnited = 5,
        CageExpired = 6,
        FireTick = 7,
        EncounterCleared = 8,
        AllPlayersDefeated = 9
    }

    public struct SimEvent
    {
        public SimEventKind Kind;
        public int SourceId;
        public int TargetId;
        public Vec2 Position;
        public float Amount;
        public int ExtraId;
    }
}
