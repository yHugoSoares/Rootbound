using System;

namespace Duatborn.Core
{
    public struct PlayerSnapshot
    {
        public int Id;
        public Vec2 Position;
        public Vec2 Facing;
        public float Health;
        public float MaxHealth;
        public bool Defeated;
        public float PrimaryCooldown;
        public float SpecialCooldown;
        public float DodgeCooldown;
        public float DodgeDuration;
        public bool DodgeActive;
        public Vec2 DodgeDirection;
    }

    public struct CageSnapshot
    {
        public int Id;
        public Vec2 Position;
        public float Radius;
        public float Remaining;
        public bool Ignited;
        public float IgnitedTimeRemaining;
    }

    public static class PlayerSnapshotCodec
    {
        public const int BytesPerPlayer = 4 + 8 + 8 + 4 + 4 + 1 + 4 + 4 + 4 + 4 + 1 + 8;

        public static int Write(PlayerSnapshot[] players, byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            int offset = 0;
            int count = players == null ? 0 : players.Length;
            WriteInt(buffer, ref offset, count);
            for (int i = 0; i < count; i++)
                WritePlayer(buffer, ref offset, players[i]);
            return offset;
        }

        public static PlayerSnapshot[] Read(byte[] buffer, int length)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            int offset = 0;
            int count = ReadInt(buffer, ref offset);
            if (count < 0) count = 0;
            PlayerSnapshot[] players = new PlayerSnapshot[count];
            for (int i = 0; i < count; i++)
                players[i] = ReadPlayer(buffer, ref offset);
            return players;
        }

        private static void WritePlayer(byte[] b, ref int o, PlayerSnapshot p)
        {
            WriteInt(b, ref o, p.Id);
            WriteFloat(b, ref o, p.Position.X);
            WriteFloat(b, ref o, p.Position.Y);
            WriteFloat(b, ref o, p.Facing.X);
            WriteFloat(b, ref o, p.Facing.Y);
            WriteFloat(b, ref o, p.Health);
            WriteFloat(b, ref o, p.MaxHealth);
            b[o++] = (byte)(p.Defeated ? 1 : 0);
            WriteFloat(b, ref o, p.PrimaryCooldown);
            WriteFloat(b, ref o, p.SpecialCooldown);
            WriteFloat(b, ref o, p.DodgeCooldown);
            WriteFloat(b, ref o, p.DodgeDuration);
            b[o++] = (byte)(p.DodgeActive ? 1 : 0);
            WriteFloat(b, ref o, p.DodgeDirection.X);
            WriteFloat(b, ref o, p.DodgeDirection.Y);
        }

        private static PlayerSnapshot ReadPlayer(byte[] b, ref int o)
        {
            PlayerSnapshot p = default(PlayerSnapshot);
            p.Id = ReadInt(b, ref o);
            p.Position = new Vec2(ReadFloat(b, ref o), ReadFloat(b, ref o));
            p.Facing = new Vec2(ReadFloat(b, ref o), ReadFloat(b, ref o));
            p.Health = ReadFloat(b, ref o);
            p.MaxHealth = ReadFloat(b, ref o);
            p.Defeated = b[o++] != 0;
            p.PrimaryCooldown = ReadFloat(b, ref o);
            p.SpecialCooldown = ReadFloat(b, ref o);
            p.DodgeCooldown = ReadFloat(b, ref o);
            p.DodgeDuration = ReadFloat(b, ref o);
            p.DodgeActive = b[o++] != 0;
            p.DodgeDirection = new Vec2(ReadFloat(b, ref o), ReadFloat(b, ref o));
            return p;
        }

        private static void WriteInt(byte[] b, ref int o, int v)
        {
            BitConverter.TryWriteBytes(new Span<byte>(b, o, 4), v);
            o += 4;
        }

        private static int ReadInt(byte[] b, ref int o)
        {
            int v = BitConverter.ToInt32(b, o);
            o += 4;
            return v;
        }

        private static void WriteFloat(byte[] b, ref int o, float v)
        {
            BitConverter.TryWriteBytes(new Span<byte>(b, o, 4), v);
            o += 4;
        }

        private static float ReadFloat(byte[] b, ref int o)
        {
            float v = BitConverter.ToSingle(b, o);
            o += 4;
            return v;
        }
    }
}
