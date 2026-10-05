using System;

namespace Rootbound.Core
{
    public sealed class SimConfig
    {
        public int TickRate = 60;
        public float ArenaRadius = 18f;
    }

    public sealed class CombatSetup
    {
        public CreatureSpec[] PlayerSpecs = new CreatureSpec[0];
        public EnemySpec EnemySpec;
        public int EnemyCount = 1;
        public int TickRate = 60;
        public float ArenaRadius = 18f;
        public Vec2[] PlayerSpawns;
        public Vec2[] EnemySpawns;

        public SimConfig SimConfig
        {
            get
            {
                SimConfig c = new SimConfig();
                c.TickRate = TickRate;
                c.ArenaRadius = ArenaRadius;
                return c;
            }
        }

        public static CombatSetup DefaultSolo()
        {
            return DefaultSolo(CreatureKind.RootGuardian);
        }

        public static CombatSetup DefaultSolo(CreatureKind creature)
        {
            CombatSetup s = new CombatSetup();
            CreatureSpec spec = creature == CreatureKind.EmberMoth
                ? DefaultContent.EmberMoth()
                : DefaultContent.RootGuardian();
            s.PlayerSpecs = new[] { spec };
            s.EnemySpec = DefaultContent.Blightling();
            s.EnemyCount = 6;
            return s;
        }

        public static CombatSetup Coop(CreatureKind[] creatures, int enemyCount)
        {
            CombatSetup s = new CombatSetup();
            int count = creatures == null || creatures.Length == 0 ? 1 : creatures.Length;
            s.PlayerSpecs = new CreatureSpec[count];
            for (int i = 0; i < count; i++)
            {
                CreatureKind kind = creatures != null && i < creatures.Length ? creatures[i] : CreatureKind.RootGuardian;
                s.PlayerSpecs[i] = kind == CreatureKind.EmberMoth ? DefaultContent.EmberMoth() : DefaultContent.RootGuardian();
            }
            s.EnemySpec = DefaultContent.Blightling();
            s.EnemyCount = enemyCount;
            return s;
        }

        public static CombatSetup DefaultTwoPlayer()
        {
            CombatSetup s = new CombatSetup();
            s.PlayerSpecs = new[] { DefaultContent.RootGuardian(), DefaultContent.EmberMoth() };
            s.EnemySpec = DefaultContent.Blightling();
            s.EnemyCount = 8;
            return s;
        }
    }
}
