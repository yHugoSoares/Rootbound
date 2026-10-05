using NUnit.Framework;
using Rootbound.Core;

namespace Rootbound.Tests
{
    public class UpgradeFieldTests
    {
        [Test]
        public void SpawnsOnePickupPerPosition()
        {
            UpgradeField field = new UpgradeField();
            field.Spawn(DefaultContent.UpgradePositions(), new[] { 0, 1, 2 });

            Assert.That(field.Count, Is.EqualTo(3));
        }

        [Test]
        public void CollectsOnlyWhenCloseEnough()
        {
            UpgradeField field = new UpgradeField();
            field.Spawn(new[] { new Vec2(0f, 3f) }, new[] { 1 });

            int upgradeIndex;
            int pickupId;
            Assert.That(field.TryCollectAt(new Vec2(0f, 0f), 2f, out upgradeIndex, out pickupId), Is.False);

            Assert.That(field.TryCollectAt(new Vec2(0f, 2.5f), 2f, out upgradeIndex, out pickupId), Is.True);
            Assert.That(upgradeIndex, Is.EqualTo(1));
            Assert.That(pickupId, Is.GreaterThan(0));
        }

        [Test]
        public void CollectedPickupCannotBeTakenTwice()
        {
            UpgradeField field = new UpgradeField();
            field.Spawn(new[] { new Vec2(0f, 1f) }, new[] { 0 });

            int upgradeIndex;
            int pickupId;
            Assert.That(field.TryCollectAt(new Vec2(0f, 1f), 2f, out upgradeIndex, out pickupId), Is.True);
            Assert.That(field.TryCollectAt(new Vec2(0f, 1f), 2f, out upgradeIndex, out pickupId), Is.False);
            Assert.That(field.AllCollected, Is.True);
        }
    }
}
