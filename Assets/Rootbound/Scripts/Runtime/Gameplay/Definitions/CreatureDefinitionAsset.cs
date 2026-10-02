using UnityEngine;
using Rootbound.Core;

namespace Rootbound.Unity
{
    [CreateAssetMenu(menuName = "Rootbound/Creature Definition", fileName = "CreatureDefinition")]
    public sealed class CreatureDefinitionAsset : ScriptableObject
    {
        public CreatureSpec spec = new CreatureSpec();
    }
}
