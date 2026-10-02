using System;

namespace Rootbound.Core
{
    public struct Health
    {
        public float Max;
        public float Current;
        public bool IsDefeated;

        public float Normalized
        {
            get { return Max <= 0f ? 0f : Current / Max; }
        }

        public static Health Create(float max)
        {
            Health h = default(Health);
            h.Max = max;
            h.Current = max;
            h.IsDefeated = false;
            return h;
        }

        public float ApplyDamage(float amount)
        {
            if (amount <= 0f || IsDefeated) return 0f;
            float before = Current;
            Current -= amount;
            if (Current <= 0f)
            {
                Current = 0f;
                IsDefeated = true;
            }
            return before - Current;
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || IsDefeated) return;
            Current += amount;
            if (Current > Max) Current = Max;
        }

        public void Reset()
        {
            Current = Max;
            IsDefeated = false;
        }
    }

    public struct Cooldown
    {
        public float Duration;
        public float Remaining;

        public bool IsReady { get { return Remaining <= 0f; } }

        public float Normalized
        {
            get { return Duration <= 0f ? 0f : Remaining / Duration; }
        }

        public bool TryStart(float duration)
        {
            if (Remaining > 0f) return false;
            Duration = duration;
            Remaining = duration;
            return true;
        }

        public void Tick(float dt)
        {
            if (Remaining <= 0f) return;
            Remaining -= dt;
            if (Remaining < 0f) Remaining = 0f;
        }

        public void Reset()
        {
            Remaining = 0f;
        }
    }

    public enum AbilityPhase
    {
        Ready = 0,
        Windup = 1,
        Active = 2,
        Recovery = 3
    }

    public struct AbilityExecution
    {
        public AbilityPhase Phase;
        public float Elapsed;
        public float Windup;
        public float Active;
        public float Recovery;

        public bool IsBusy { get { return Phase != AbilityPhase.Ready; } }

        public bool IsMovementLocked
        {
            get { return Phase == AbilityPhase.Windup || Phase == AbilityPhase.Active; }
        }

        public void Begin(float windup, float active, float recovery)
        {
            Windup = windup;
            Active = active;
            Recovery = recovery;
            Elapsed = 0f;
            Phase = AbilityPhase.Windup;
        }

        public void Cancel()
        {
            Phase = AbilityPhase.Ready;
            Elapsed = 0f;
        }

        public bool Tick(float dt)
        {
            if (Phase == AbilityPhase.Ready) return false;
            Elapsed += dt;
            if (Phase == AbilityPhase.Windup)
            {
                if (Elapsed >= Windup)
                {
                    Phase = AbilityPhase.Active;
                    return true;
                }
            }
            else if (Phase == AbilityPhase.Active)
            {
                if (Elapsed >= Windup + Active) Phase = AbilityPhase.Recovery;
            }
            else if (Phase == AbilityPhase.Recovery)
            {
                if (Elapsed >= Windup + Active + Recovery) Phase = AbilityPhase.Ready;
            }
            return false;
        }
    }

    public struct DodgeState
    {
        public bool IsActive;
        public float Elapsed;
        public float Duration;
        public Vec2 Direction;
        public float InvulnStart;
        public float InvulnEnd;

        public bool IsInvulnerable
        {
            get { return IsActive && Elapsed >= InvulnStart && Elapsed <= InvulnEnd; }
        }

        public float NormalizedTime
        {
            get { return Duration <= 0f ? 1f : Elapsed / Duration; }
        }

        public void Begin(Vec2 direction, float duration, float invulnStart, float invulnEnd)
        {
            IsActive = true;
            Elapsed = 0f;
            Duration = duration;
            Direction = direction.Normalized;
            InvulnStart = invulnStart;
            InvulnEnd = invulnEnd;
        }

        public Vec2 Tick(float dt, float distance)
        {
            if (!IsActive) return Vec2.Zero;
            float speed = Duration > 0f ? distance / Duration : 0f;
            Vec2 delta = Direction * (speed * dt);
            Elapsed += dt;
            if (Elapsed >= Duration) IsActive = false;
            return delta;
        }

        public void Reset()
        {
            IsActive = false;
            Elapsed = 0f;
        }
    }

    public static class TargetRules
    {
        public static bool CanDamage(Team source, Team target)
        {
            return source != target;
        }

        public static bool WithinArc(Vec2 facing, Vec2 toTarget, float arcDegrees)
        {
            if (arcDegrees >= 360f) return true;
            Vec2 f = facing.Normalized;
            Vec2 t = toTarget.Normalized;
            if (f.SqrMagnitude <= 1e-8f || t.SqrMagnitude <= 1e-8f) return true;
            float half = arcDegrees * 0.5f * DefaultContent.DegreesToRadians;
            float cosHalf = (float)Math.Cos(half);
            return Vec2.Dot(f, t) >= cosHalf;
        }
    }
}
