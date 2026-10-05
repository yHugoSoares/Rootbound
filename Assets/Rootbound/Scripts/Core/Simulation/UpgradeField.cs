using System.Collections.Generic;

namespace Rootbound.Core
{
    public sealed class UpgradePickupState
    {
        public int Id;
        public Vec2 Position;
        public int UpgradeIndex;
        public bool Collected;
    }

    public sealed class UpgradeField
    {
        private readonly List<UpgradePickupState> _pickups = new List<UpgradePickupState>();
        private int _nextId = 1;

        public IReadOnlyList<UpgradePickupState> Pickups { get { return _pickups; } }
        public int Count { get { return _pickups.Count; } }

        public bool AllCollected
        {
            get
            {
                if (_pickups.Count == 0) return false;
                for (int i = 0; i < _pickups.Count; i++)
                    if (!_pickups[i].Collected) return false;
                return true;
            }
        }

        public void Clear()
        {
            _pickups.Clear();
        }

        public void Spawn(Vec2[] positions, int[] upgradeIndices)
        {
            Clear();
            if (positions == null) return;
            for (int i = 0; i < positions.Length; i++)
            {
                UpgradePickupState pickup = new UpgradePickupState();
                pickup.Id = _nextId++;
                pickup.Position = positions[i];
                pickup.UpgradeIndex = upgradeIndices != null && i < upgradeIndices.Length ? upgradeIndices[i] : 0;
                pickup.Collected = false;
                _pickups.Add(pickup);
            }
        }

        public bool TryCollectAt(Vec2 playerPosition, float radius, out int upgradeIndex, out int pickupId)
        {
            upgradeIndex = -1;
            pickupId = -1;
            float sqr = radius * radius;
            for (int i = 0; i < _pickups.Count; i++)
            {
                UpgradePickupState pickup = _pickups[i];
                if (pickup.Collected) continue;
                if (Vec2.SqrDistance(playerPosition, pickup.Position) > sqr) continue;
                pickup.Collected = true;
                upgradeIndex = pickup.UpgradeIndex;
                pickupId = pickup.Id;
                return true;
            }
            return false;
        }
    }
}
