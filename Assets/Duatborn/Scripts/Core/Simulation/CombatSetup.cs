using System;

namespace Duatborn.Core
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

        public EnemySpec[] EnemySpecs;

        public static CombatSetup FromArena(CreatureKind[] creatures, ArenaSpec arena, bool active)
        {
            int count = creatures == null || creatures.Length == 0 ? 1 : creatures.Length;
            CreatureSpec[] specs = new CreatureSpec[count];
            for (int i = 0; i < count; i++)
            {
                CreatureKind kind = creatures != null && i < creatures.Length ? creatures[i] : CreatureKind.DuneWarden;
                specs[i] = kind == CreatureKind.Sunwing ? DefaultContent.Sunwing() : DefaultContent.DuneWarden();
            }
            return FromArena(specs, arena, active);
        }

        public static CombatSetup FromArena(CreatureSpec[] players, ArenaSpec arena, bool active)
        {
            CombatSetup s = new CombatSetup();
            s.PlayerSpecs = players != null && players.Length > 0 ? players : new[] { DefaultContent.DuneWarden() };
            ApplyArena(s, arena, active);
            return s;
        }

        private static void ApplyArena(CombatSetup s, ArenaSpec arena, bool active)
        {
            if (arena == null)
            {
                s.EnemySpec = DefaultContent.HollowSentinel();
                s.EnemyCount = 0;
                return;
            }

            if (arena.Groups != null && arena.Groups.Length > 0)
            {
                System.Collections.Generic.List<EnemySpec> list = new System.Collections.Generic.List<EnemySpec>();
                for (int g = 0; g < arena.Groups.Length; g++)
                {
                    ArenaEnemyGroup group = arena.Groups[g];
                    if (group == null || group.Enemy == null) continue;
                    for (int i = 0; i < group.Count; i++) list.Add(group.Enemy);
                }
                s.EnemySpecs = list.ToArray();
                s.EnemyCount = active ? list.Count : 0;
                s.EnemySpec = arena.Groups[0].Enemy != null ? arena.Groups[0].Enemy : DefaultContent.HollowSentinel();
            }
            else
            {
                s.EnemySpec = arena.Enemy != null ? arena.Enemy : DefaultContent.HollowSentinel();
                s.EnemyCount = active ? arena.EnemyCount : 0;
            }
        }

        public static CombatSetup DefaultSolo()
        {
            return DefaultSolo(CreatureKind.DuneWarden);
        }

        public static CombatSetup DefaultSolo(CreatureKind creature)
        {
            CombatSetup s = new CombatSetup();
            CreatureSpec spec = creature == CreatureKind.Sunwing
                ? DefaultContent.Sunwing()
                : DefaultContent.DuneWarden();
            s.PlayerSpecs = new[] { spec };
            s.EnemySpec = DefaultContent.HollowSentinel();
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
                CreatureKind kind = creatures != null && i < creatures.Length ? creatures[i] : CreatureKind.DuneWarden;
                s.PlayerSpecs[i] = kind == CreatureKind.Sunwing ? DefaultContent.Sunwing() : DefaultContent.DuneWarden();
            }
            s.EnemySpec = DefaultContent.HollowSentinel();
            s.EnemyCount = enemyCount;
            return s;
        }

        public static CombatSetup DefaultTwoPlayer()
        {
            CombatSetup s = new CombatSetup();
            s.PlayerSpecs = new[] { DefaultContent.DuneWarden(), DefaultContent.Sunwing() };
            s.EnemySpec = DefaultContent.HollowSentinel();
            s.EnemyCount = 8;
            return s;
        }
    }
}
