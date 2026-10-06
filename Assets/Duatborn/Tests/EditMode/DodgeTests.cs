using NUnit.Framework;
using Duatborn.Core;

namespace Duatborn.Tests
{
    public class DodgeTests
    {
        [Test]
        public void InvulnerabilityOnlyWithinWindow()
        {
            DodgeState d = default(DodgeState);
            d.Begin(new Vec2(1f, 0f), 0.28f, 0.02f, 0.26f);

            Assert.That(d.IsInvulnerable, Is.False);

            d.Tick(0.03f, 3f);
            Assert.That(d.IsInvulnerable, Is.True);

            d.Tick(0.20f, 3f);
            Assert.That(d.IsInvulnerable, Is.True);

            d.Tick(0.06f, 3f);
            Assert.That(d.IsInvulnerable, Is.False);
        }

        [Test]
        public void DodgeMovesFixedDistanceAndEnds()
        {
            DodgeState d = default(DodgeState);
            d.Begin(new Vec2(1f, 0f), 0.2f, 0f, 0.2f);

            Vec2 total = Vec2.Zero;
            for (int i = 0; i < 20; i++)
                total = total + d.Tick(0.01f, 3f);

            Assert.That(total.X, Is.EqualTo(3f).Within(0.01f));
            Assert.That(d.IsActive, Is.False);
        }
    }
}
