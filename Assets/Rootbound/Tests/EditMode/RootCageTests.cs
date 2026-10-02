using NUnit.Framework;
using Rootbound.Core;

namespace Rootbound.Tests
{
    public class RootCageTests
    {
        private static RootCageState MakeCage()
        {
            RootCageState cage = new RootCageState();
            cage.Id = 1;
            cage.Duration = 10f;
            cage.Radius = 2.2f;
            cage.RestrainFactor = 1f;
            cage.FireTickInterval = 0.5f;
            cage.FireDamagePerTick = 8f;
            cage.MaxIgnitionDuration = 3f;
            return cage;
        }

        [Test]
        public void IgnitionExtendsBurnThenCaps()
        {
            RootCageState cage = MakeCage();

            Assert.That(cage.TryIgnite(2.5f), Is.True);
            Assert.That(cage.IgnitedTimeRemaining, Is.EqualTo(2.5f).Within(0.0001f));

            Assert.That(cage.TryIgnite(2.5f), Is.True);
            Assert.That(cage.IgnitedTimeRemaining, Is.EqualTo(3f).Within(0.0001f));

            Assert.That(cage.TryIgnite(2.5f), Is.False);
            Assert.That(cage.IgnitedTimeRemaining, Is.EqualTo(3f).Within(0.0001f));
        }

        [Test]
        public void ExpiredCageCannotBeIgnited()
        {
            RootCageState cage = MakeCage();
            cage.Elapsed = cage.Duration;

            Assert.That(cage.TryIgnite(2.5f), Is.False);
            Assert.That(cage.IsIgnited, Is.False);
        }

        [Test]
        public void FireTicksAtIntervalOnlyWhileIgnited()
        {
            RootCageState cage = MakeCage();
            cage.TryIgnite(3f);

            cage.Tick(0.4f);
            Assert.That(cage.ConsumeFireTick(), Is.False);

            cage.Tick(0.2f);
            Assert.That(cage.ConsumeFireTick(), Is.True);
        }

        [Test]
        public void BurnStopsWhenIgnitionTimeExpires()
        {
            RootCageState cage = MakeCage();
            cage.TryIgnite(0.3f);
            cage.Tick(0.4f);

            Assert.That(cage.IsIgnited, Is.False);
            Assert.That(cage.ConsumeFireTick(), Is.False);
        }
    }
}
