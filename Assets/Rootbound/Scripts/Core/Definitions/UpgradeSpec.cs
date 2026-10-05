using System;

namespace Rootbound.Core
{
    public enum UpgradeKind
    {
        MaxHealth = 0,
        PrimaryDamage = 1,
        MoveSpeed = 2,
        CooldownReduction = 3
    }

    [System.Serializable]
    public sealed class UpgradeSpec
    {
        public string Id;
        public string DisplayName;
        public UpgradeKind Kind;
        public float Amount;
    }

    public static class UpgradeRules
    {
        public static void Apply(UpgradeSpec upgrade, CreatureSpec spec)
        {
            if (upgrade == null || spec == null) return;
            switch (upgrade.Kind)
            {
                case UpgradeKind.MaxHealth:
                    spec.MaxHealth += upgrade.Amount;
                    break;
                case UpgradeKind.PrimaryDamage:
                    spec.Primary.Damage += upgrade.Amount;
                    break;
                case UpgradeKind.MoveSpeed:
                    spec.MoveSpeed += upgrade.Amount;
                    break;
                case UpgradeKind.CooldownReduction:
                    spec.Primary.Cooldown = Math.Max(0.05f, spec.Primary.Cooldown - upgrade.Amount);
                    break;
            }
        }
    }
}
