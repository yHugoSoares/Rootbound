using System;
using System.Globalization;

namespace Duatborn.Core
{
    public static class CooldownDisplay
    {
        public static string Label(Cooldown cooldown)
        {
            if (cooldown.IsReady) return "ready";
            float tenths = (float)Math.Ceiling(cooldown.Remaining * 10f);
            if (tenths < 1f) tenths = 1f;
            return (tenths / 10f).ToString("0.0", CultureInfo.InvariantCulture);
        }

        public static float NormalizedRemaining(Cooldown cooldown)
        {
            if (cooldown.Duration <= 0f) return 0f;
            float normalized = cooldown.Remaining / cooldown.Duration;
            if (normalized < 0f) normalized = 0f;
            if (normalized > 1f) normalized = 1f;
            return normalized;
        }

        public static string NormalizedLabel(Cooldown cooldown)
        {
            return NormalizedRemaining(cooldown).ToString("0.00", CultureInfo.InvariantCulture);
        }

        public static string Seconds(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            return seconds.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
