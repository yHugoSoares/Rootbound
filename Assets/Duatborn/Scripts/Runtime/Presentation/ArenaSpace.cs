using UnityEngine;
using Duatborn.Core;

namespace Duatborn.Unity
{
    public static class ArenaSpace
    {
        public static Vector3 ToWorld(Vec2 p)
        {
            return new Vector3(p.X, 0f, p.Y);
        }

        public static Vec2 ToPlanar(Vector3 p)
        {
            return new Vec2(p.x, p.z);
        }

        public static float ToYaw(Vec2 facing)
        {
            return Mathf.Atan2(facing.X, facing.Y) * Mathf.Rad2Deg;
        }
    }
}
