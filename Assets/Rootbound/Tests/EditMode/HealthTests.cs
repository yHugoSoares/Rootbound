using NUnit.Framework;
using Rootbound.Core;

namespace Rootbound.Tests
{
    public class HealthTests
    {
        [Test]
        public void DamageCannotReduceHealthBelowZero()
        {
            Health h = Health.Create(30f);
            float applied = h.ApplyDamage(1000f);

            Assert.That(applied, Is.EqualTo(30f).Within(0.0001f));
            Assert.That(h.Current, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void DefeatIsReportedOnceAndFurtherDamageIsIgnored()
        {
            Health h = Health.Create(10f);
            float first = h.ApplyDamage(10f);
            float second = h.ApplyDamage(10f);

            Assert.That(first, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(second, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(h.IsDefeated, Is.True);
            Assert.That(h.Current, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void HealClampsToMaximum()
        {
            Health h = Health.Create(20f);
            h.ApplyDamage(5f);
            h.Heal(100f);

            Assert.That(h.Current, Is.EqualTo(20f).Within(0.0001f));
        }

        [Test]
        public void NonPositiveDamageDoesNothing()
        {
            Health h = Health.Create(20f);
            float applied = h.ApplyDamage(0f);

            Assert.That(applied, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(h.Current, Is.EqualTo(20f).Within(0.0001f));
        }
    }
}
