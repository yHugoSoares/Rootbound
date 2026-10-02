using UnityEngine;
using Rootbound.Core;

namespace Rootbound.Unity
{
    [CreateAssetMenu(menuName = "Rootbound/Enemy Definition", fileName = "EnemyDefinition")]
    public sealed class EnemyDefinitionAsset : ScriptableObject
    {
        public EnemySpec spec = new EnemySpec();
    }
}
