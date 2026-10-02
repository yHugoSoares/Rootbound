using System;

namespace Rootbound.Core
{
    public struct Vec2 : IEquatable<Vec2>
    {
        public float X;
        public float Y;

        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static readonly Vec2 Zero = new Vec2(0f, 0f);
        public static readonly Vec2 Right = new Vec2(1f, 0f);
        public static readonly Vec2 Up = new Vec2(0f, 1f);

        public float SqrMagnitude { get { return X * X + Y * Y; } }

        public float Magnitude { get { return (float)Math.Sqrt(X * X + Y * Y); } }

        public Vec2 Normalized
        {
            get
            {
                float m = Magnitude;
                return m > 1e-6f ? new Vec2(X / m, Y / m) : Zero;
            }
        }

        public static Vec2 operator +(Vec2 a, Vec2 b) { return new Vec2(a.X + b.X, a.Y + b.Y); }
        public static Vec2 operator -(Vec2 a, Vec2 b) { return new Vec2(a.X - b.X, a.Y - b.Y); }
        public static Vec2 operator -(Vec2 a) { return new Vec2(-a.X, -a.Y); }
        public static Vec2 operator *(Vec2 a, float s) { return new Vec2(a.X * s, a.Y * s); }
        public static Vec2 operator *(float s, Vec2 a) { return new Vec2(a.X * s, a.Y * s); }
        public static Vec2 operator /(Vec2 a, float s) { return new Vec2(a.X / s, a.Y / s); }

        public static float Dot(Vec2 a, Vec2 b) { return a.X * b.X + a.Y * b.Y; }

        public static float Distance(Vec2 a, Vec2 b) { return (a - b).Magnitude; }

        public static float SqrDistance(Vec2 a, Vec2 b) { return (a - b).SqrMagnitude; }

        public static Vec2 Lerp(Vec2 a, Vec2 b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return new Vec2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
        }

        public static Vec2 ClampMagnitude(Vec2 v, float max)
        {
            float sq = v.SqrMagnitude;
            if (sq <= max * max || sq <= 1e-12f) return v;
            float scale = max / (float)Math.Sqrt(sq);
            return new Vec2(v.X * scale, v.Y * scale);
        }

        public bool Equals(Vec2 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is Vec2 && Equals((Vec2)obj);
        }

        public override int GetHashCode()
        {
            unchecked { return (X.GetHashCode() * 397) ^ Y.GetHashCode(); }
        }

        public override string ToString()
        {
            return "(" + X.ToString("0.###") + ", " + Y.ToString("0.###") + ")";
        }
    }
}
