using Fusion;
using UnityEngine;

namespace Rootbound.Fusion
{
    public struct RootboundInput : INetworkInput
    {
        public Vector2 Move;
        public Vector2 Aim;
        public Vector2 TargetPoint;
        public NetworkBool HasTargetPoint;
        public NetworkBool Primary;
        public NetworkBool Special;
        public NetworkBool Dodge;
        public NetworkBool Restart;
        public byte Creature;
    }
}
