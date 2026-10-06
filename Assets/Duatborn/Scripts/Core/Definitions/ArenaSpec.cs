namespace Duatborn.Core
{
    [System.Serializable]
    public sealed class ArenaEnemyGroup
    {
        public EnemySpec Enemy = new EnemySpec();
        public int Count = 3;
    }

    [System.Serializable]
    public sealed class ArenaSpec
    {
        public string Id;
        public string DisplayName;
        public EnemySpec Enemy = new EnemySpec();
        public int EnemyCount = 4;
        public ArenaEnemyGroup[] Groups;
        public bool IsFinale;
    }
}
