using NUnit.Framework;
using Duatborn.Core;

namespace Duatborn.Tests
{
    public class RunStateTests
    {
        [Test]
        public void AdvancesOnlyWhenEncounterCleared()
        {
            RunState run = new RunState();
            run.Reset(3);

            Assert.That(run.Update(false, false), Is.False);
            Assert.That(run.RoomIndex, Is.EqualTo(0));

            Assert.That(run.Update(true, false), Is.True);
            Assert.That(run.RoomIndex, Is.EqualTo(1));
        }

        [Test]
        public void FinalRoomClearCompletesTheRun()
        {
            RunState run = new RunState();
            run.Reset(3);
            run.Update(true, false);
            run.Update(true, false);

            Assert.That(run.IsFinalRoom, Is.True);
            Assert.That(run.Update(true, false), Is.False);
            Assert.That(run.RunComplete, Is.True);
            Assert.That(run.RoomIndex, Is.EqualTo(2));
        }

        [Test]
        public void AllPlayersDefeatedFailsTheRun()
        {
            RunState run = new RunState();
            run.Reset(3);

            run.Update(false, true);

            Assert.That(run.RunFailed, Is.True);
            Assert.That(run.RoomIndex, Is.EqualTo(0));
        }

        [Test]
        public void ResetClearsRunState()
        {
            RunState run = new RunState();
            run.Reset(2);
            run.Update(true, false);
            run.Update(true, false);
            Assert.That(run.RunComplete, Is.True);

            run.Reset(3);
            Assert.That(run.RoomIndex, Is.EqualTo(0));
            Assert.That(run.RunComplete, Is.False);
            Assert.That(run.RunFailed, Is.False);
            Assert.That(run.RoomCount, Is.EqualTo(3));
        }

        [Test]
        public void DefaultRunHasThreeRoomsWithFinale()
        {
            ArenaSpec[] rooms = DefaultContent.DefaultRun();
            Assert.That(rooms.Length, Is.EqualTo(3));
            Assert.That(rooms[2].IsFinale, Is.True);
            Assert.That(rooms[0].EnemyCount, Is.LessThan(rooms[2].EnemyCount));
        }
    }
}
