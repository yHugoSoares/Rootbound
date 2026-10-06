using UnityEngine;
using Duatborn.Core;

namespace Duatborn.Unity
{
    [CreateAssetMenu(menuName = "Duatborn/Enemy Definition", fileName = "EnemyDefinition")]
    public sealed class EnemyDefinitionAsset : ScriptableObject
    {
        public EnemySpec spec = new EnemySpec();
    }
}
