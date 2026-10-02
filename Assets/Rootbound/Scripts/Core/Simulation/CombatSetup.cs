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
            CombatSetup s = new CombatSetup();
            s.PlayerSpecs = new[] { DefaultContent.RootGuardian() };
            s.EnemySpec = DefaultContent.Blightling();
            s.EnemyCount = 6;
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
