using System;

namespace Rootbound.Core
{
    public sealed class RootCageState
    {
        public int Id;
        public int OwnerPlayerId;
        public int IgnitedByPlayerId = -1;
        public Vec2 Position;
        public float Radius;
        public float Duration;
        public float Elapsed;
        public float RestrainFactor;

        public bool IsIgnited;
        public float FireTickInterval;
        public float FireDamagePerTick;
        public float IgnitedTimeRemaining;
        public float MaxIgnitionDuration;
        public float FireTickAccumulator;

        public float Remaining { get { return Duration - Elapsed; } }

        public bool IsActive { get { return Elapsed < Duration; } }

        public void Tick(float dt)
        {
            Elapsed += dt;
            if (!IsIgnited) return;
            IgnitedTimeRemaining -= dt;
            FireTickAccumulator += dt;
            if (IgnitedTimeRemaining <= 0f) IsIgnited = false;
        }

        public bool TryIgnite(float extension)
        {
            if (!IsActive) return false;
            if (extension <= 0f) return false;
            float before = IgnitedTimeRemaining;
            float next = IgnitedTimeRemaining + extension;
            if (next > MaxIgnitionDuration) next = MaxIgnitionDuration;
            IgnitedTimeRemaining = next;
            IsIgnited = IgnitedTimeRemaining > 0f;
            return IgnitedTimeRemaining > before;
        }

        public bool ConsumeFireTick()
        {
            if (!IsActive || !IsIgnited) return false;
            if (FireTickInterval <= 0f) return false;
            if (FireTickAccumulator < FireTickInterval) return false;
            FireTickAccumulator -= FireTickInterval;
            if (FireTickAccumulator > FireTickInterval) FireTickAccumulator = 0f;
            return true;
        }
    }
}
