using UnityEngine;
using Duatborn.Core;

namespace Duatborn.Unity
{
    [CreateAssetMenu(menuName = "Duatborn/Creature Definition", fileName = "CreatureDefinition")]
    public sealed class CreatureDefinitionAsset : ScriptableObject
    {
        public CreatureSpec spec = new CreatureSpec();
    }
}
