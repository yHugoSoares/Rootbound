using NUnit.Framework;
using Rootbound.Core;

namespace Rootbound.Tests
{
    public class CooldownDisplayTests
    {
        [Test]
        public void ReadyWhenNoCooldownRemaining()
        {
            Cooldown cooldown = default(Cooldown);

            Assert.That(CooldownDisplay.Label(cooldown), Is.EqualTo("ready"));
            Assert.That(CooldownDisplay.NormalizedRemaining(cooldown), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ShowsRemainingRoundedUpToTenths()
        {
            Cooldown cooldown = default(Cooldown);
            cooldown.TryStart(0.9f);
            Assert.That(CooldownDisplay.Label(cooldown), Is.EqualTo("0.9"));

            cooldown.Tick(0.31f);
            Assert.That(cooldown.Remaining, Is.EqualTo(0.59f).Within(0.001f));
            Assert.That(CooldownDisplay.Label(cooldown), Is.EqualTo("0.6"));
        }

        [Test]
        public void NeverShowsZeroWhileStillOnCooldown()
        {
            Cooldown cooldown = default(Cooldown);
            cooldown.TryStart(0.9f);
            cooldown.Tick(0.88f);

            Assert.That(cooldown.IsReady, Is.False);
            Assert.That(CooldownDisplay.Label(cooldown), Is.EqualTo("0.1"));
        }

        [Test]
        public void RejectedStartDoesNotChangeDisplayedValue()
        {
            Cooldown cooldown = default(Cooldown);
            cooldown.TryStart(0.9f);
            cooldown.Tick(0.3f);
            string before = CooldownDisplay.Label(cooldown);

            bool started = cooldown.TryStart(0.5f);

            Assert.That(started, Is.False);
            Assert.That(CooldownDisplay.Label(cooldown), Is.EqualTo(before));
        }

        [Test]
        public void NormalizedRemainingMapsAndClamps()
        {
            Cooldown cooldown = default(Cooldown);
            cooldown.TryStart(1.0f);
            Assert.That(CooldownDisplay.NormalizedRemaining(cooldown), Is.EqualTo(1f).Within(0.0001f));

            cooldown.Tick(0.5f);
            Assert.That(CooldownDisplay.NormalizedRemaining(cooldown), Is.EqualTo(0.5f).Within(0.0001f));

            cooldown.Tick(10f);
            Assert.That(CooldownDisplay.NormalizedRemaining(cooldown), Is.EqualTo(0f).Within(0.0001f));
        }
    }
}
