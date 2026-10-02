using NUnit.Framework;
using Rootbound.Core;

namespace Rootbound.Tests
{
    public class CooldownTests
    {
        [Test]
        public void AbilityCannotActivateDuringCooldown()
        {
            Cooldown c = default(Cooldown);
            bool first = c.TryStart(1.0f);
            bool second = c.TryStart(1.0f);

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(c.IsReady, Is.False);
        }

        [Test]
        public void CooldownBecomesReadyAfterEnoughTime()
        {
            Cooldown c = default(Cooldown);
            c.TryStart(1.0f);
            c.Tick(0.5f);

            Assert.That(c.IsReady, Is.False);
            Assert.That(c.Remaining, Is.EqualTo(0.5f).Within(0.0001f));

            c.Tick(0.5f);
            Assert.That(c.IsReady, Is.True);
        }

        [Test]
        public void CooldownDoesNotGoNegative()
        {
            Cooldown c = default(Cooldown);
            c.TryStart(0.2f);
            c.Tick(5f);

            Assert.That(c.Remaining, Is.EqualTo(0f).Within(0.0001f));
        }
    }
}
