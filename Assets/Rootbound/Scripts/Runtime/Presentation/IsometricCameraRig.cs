using UnityEngine;
using Rootbound.Core;

namespace Rootbound.Unity
{
    public sealed class IsometricCameraRig : MonoBehaviour
    {
        public Camera targetCamera;
        public float yaw = 45f;
        public float pitch = 35f;
        public float distance = 24f;
        public float orthographicSize = 11f;
        public float followLerp = 8f;
        public int localPlayerId = -1;

        public Vector3 FocusCenter { get; private set; }

        private CombatSimulation _sim;
        private Vector3 _currentCenter;
        private bool _initialized;

        public void Bind(CombatSimulation sim)
        {
            _sim = sim;
            if (targetCamera == null) targetCamera = GetComponent<Camera>();
            if (targetCamera == null) targetCamera = Camera.main;
            _currentCenter = ComputeCenter();
            _initialized = false;
            Apply(1f);
        }

        public void SetLocalPlayer(int playerId)
        {
            localPlayerId = playerId;
            if (_sim == null) return;
            Apply(1f);
        }

        private void LateUpdate()
        {
            if (_sim == null) return;
            Apply(1f - Mathf.Exp(-followLerp * Time.deltaTime));
        }

        private void Apply(float t)
        {
            Vector3 center = ComputeCenter();
            _currentCenter = Vector3.Lerp(_currentCenter, center, t);
            FocusCenter = _currentCenter;

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 position = _currentCenter - (rotation * Vector3.forward) * distance;

            if (targetCamera != null)
            {
                targetCamera.orthographic = true;
                targetCamera.orthographicSize = orthographicSize;
                targetCamera.transform.position = position;
                targetCamera.transform.rotation = rotation;
            }
            _initialized = true;
        }

        private Vector3 ComputeCenter()
        {
            if (_sim == null) return Vector3.zero;

            if (localPlayerId >= 0)
            {
                PlayerState local = _sim.GetPlayer(localPlayerId);
                if (local != null) return ArenaSpace.ToWorld(local.Position);
            }

            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < _sim.Players.Count; i++)
            {
                PlayerState p = _sim.Players[i];
                if (p.IsDefeated) continue;
                sum += ArenaSpace.ToWorld(p.Position);
                count++;
            }
            if (count == 0) return _initialized ? _currentCenter : Vector3.zero;
            return sum / count;
        }
    }
}
