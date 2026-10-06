namespace Duatborn.Core
{
    public sealed class RunState
    {
        public int RoomCount { get; private set; }
        public int RoomIndex { get; private set; }
        public bool RunComplete { get; private set; }
        public bool RunFailed { get; private set; }
        public bool IsFinalRoom { get { return RoomCount > 0 && RoomIndex >= RoomCount - 1; } }

        public void Reset(int roomCount)
        {
            RoomCount = roomCount < 1 ? 1 : roomCount;
            RoomIndex = 0;
            RunComplete = false;
            RunFailed = false;
        }

        public bool Update(bool encounterCleared, bool allPlayersDefeated)
        {
            if (RunComplete || RunFailed) return false;
            if (allPlayersDefeated)
            {
                RunFailed = true;
                return false;
            }
            if (!encounterCleared) return false;
            if (IsFinalRoom)
            {
                RunComplete = true;
                return false;
            }
            RoomIndex++;
            return true;
        }
    }
}
