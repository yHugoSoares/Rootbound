using System;

namespace Duatborn.Core
{
    public enum UpgradeKind
    {
        MaxHealth = 0,
        PrimaryDamage = 1,
        MoveSpeed = 2,
        CooldownReduction = 3,
        SpecialRadius = 4,
        SpecialDuration = 5,
        LifeOnKill = 6
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
        public static bool AppliesTo(UpgradeSpec upgrade, CreatureKind kind)
        {
            if (upgrade == null) return false;
            switch (upgrade.Kind)
            {
                case UpgradeKind.SpecialDuration:
                    return kind == CreatureKind.DuneWarden;
                default:
                    return true;
            }
        }

        public static bool AppliesToAny(UpgradeSpec upgrade, CreatureKind[] creatures)
        {
            if (upgrade == null) return false;
            if (creatures == null || creatures.Length == 0) return true;
            for (int i = 0; i < creatures.Length; i++)
                if (AppliesTo(upgrade, creatures[i])) return true;
            return false;
        }

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
                case UpgradeKind.SpecialRadius:
                    spec.Special.Radius += upgrade.Amount;
                    break;
                case UpgradeKind.SpecialDuration:
                    spec.Special.Duration += upgrade.Amount;
                    break;
                case UpgradeKind.LifeOnKill:
                    spec.LifeOnKill += upgrade.Amount;
                    break;
            }
        }
    }
}
