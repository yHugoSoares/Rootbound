using NUnit.Framework;
using Rootbound.Core;

namespace Rootbound.Tests
{
    public class PlayerSnapshotTests
    {
        [Test]
        public void CodecRoundTripsPlayerValues()
        {
            PlayerSnapshot[] players = new PlayerSnapshot[2];
            players[0].Id = 0;
            players[0].Position = new Vec2(1.5f, -2.25f);
            players[0].Facing = new Vec2(0f, 1f);
            players[0].Health = 120f;
            players[0].MaxHealth = 160f;
            players[0].PrimaryCooldown = 0.25f;
            players[0].SpecialCooldown = 3.5f;
            players[0].DodgeCooldown = 0.9f;
            players[0].DodgeActive = true;
            players[0].DodgeDirection = new Vec2(1f, 0f);
            players[1].Id = 1;
            players[1].Position = new Vec2(-3f, 4f);
            players[1].Health = 95f;
            players[1].MaxHealth = 95f;
            players[1].Defeated = true;

            byte[] buffer = new byte[256];
            int length = PlayerSnapshotCodec.Write(players, buffer);
            PlayerSnapshot[] read = PlayerSnapshotCodec.Read(buffer, length);

            Assert.That(read.Length, Is.EqualTo(2));
            Assert.That(read[0].Position.X, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(read[0].Position.Y, Is.EqualTo(-2.25f).Within(0.0001f));
            Assert.That(read[0].Health, Is.EqualTo(120f).Within(0.0001f));
            Assert.That(read[0].DodgeActive, Is.True);
            Assert.That(read[1].Defeated, Is.True);
            Assert.That(read[1].MaxHealth, Is.EqualTo(95f).Within(0.0001f));
        }

        [Test]
        public void CaptureApplyRestoresSimulationMovement()
        {
            CombatSimulation host = new CombatSimulation(CombatSetup.DefaultSolo(CreatureKind.RootGuardian));
            PlayerCommand move = default(PlayerCommand);
            move.Move = new Vec2(0f, 1f);
            for (int i = 0; i < 30; i++)
            {
                host.SubmitCommand(0, move);
                host.Step();
            }

            PlayerSnapshot[] captured = host.CapturePlayers();
            byte[] buffer = new byte[256];
            int length = PlayerSnapshotCodec.Write(captured, buffer);

            CombatSimulation client = new CombatSimulation(CombatSetup.DefaultSolo(CreatureKind.RootGuardian));
            client.ApplyPlayerSnapshots(PlayerSnapshotCodec.Read(buffer, length));

            Assert.That(client.GetPlayer(0).Position.Y, Is.EqualTo(host.GetPlayer(0).Position.Y).Within(0.0001f));
            Assert.That(client.GetPlayer(0).Facing.Y, Is.EqualTo(host.GetPlayer(0).Facing.Y).Within(0.0001f));
            Assert.That(client.GetPlayer(0).Health.Current, Is.EqualTo(host.GetPlayer(0).Health.Current).Within(0.0001f));
        }
    }
}
