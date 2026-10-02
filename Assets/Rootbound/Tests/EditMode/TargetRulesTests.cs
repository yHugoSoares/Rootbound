using NUnit.Framework;
using Rootbound.Core;

namespace Rootbound.Tests
{
    public class TargetRulesTests
    {
        [Test]
        public void FriendlyTargetsAreRejected()
        {
            Assert.That(TargetRules.CanDamage(Team.Players, Team.Players), Is.False);
            Assert.That(TargetRules.CanDamage(Team.Enemies, Team.Enemies), Is.False);
            Assert.That(TargetRules.CanDamage(Team.Players, Team.Enemies), Is.True);
        }

        [Test]
        public void WithinArcRejectsTargetBehind()
        {
            Vec2 facing = new Vec2(0f, 1f);
            Vec2 behind = new Vec2(0f, -1f);
            Vec2 inFront = new Vec2(0.1f, 1f);

            Assert.That(TargetRules.WithinArc(facing, behind, 90f), Is.False);
            Assert.That(TargetRules.WithinArc(facing, inFront, 90f), Is.True);
        }

        [Test]
        public void FullCircleArcAcceptsAnything()
        {
            Assert.That(TargetRules.WithinArc(new Vec2(0f, 1f), new Vec2(0f, -1f), 360f), Is.True);
        }
    }
}
