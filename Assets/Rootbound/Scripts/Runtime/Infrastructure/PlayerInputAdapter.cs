using UnityEngine;
using UnityEngine.InputSystem;
using Rootbound.Core;

namespace Rootbound.Unity
{
    public sealed class PlayerInputAdapter
    {
        private readonly RootboundInputActions _actions;
        private readonly Camera _camera;
        private readonly bool _pointerAim;
        private readonly Plane _ground = new Plane(Vector3.up, Vector3.zero);
        private bool _dodgeQueued;

        public PlayerInputAdapter(RootboundInputActions actions, Camera camera)
        {
            _actions = actions;
            _camera = camera;
            _pointerAim = actions.UsesPointerAim && Mouse.current != null;
        }

        // Must run once per rendered frame, including frames with no fixed step,
        // so a discrete press is not lost when the simulation does not tick.
        public void CaptureFrame(bool dodgePressedThisFrame)
        {
            if (dodgePressedThisFrame) _dodgeQueued = true;
        }

        public PlayerCommand Build(Vector3 worldPosition)
        {
            PlayerCommand command = default(PlayerCommand);

            Vector2 move = _actions.Move.ReadValue<Vector2>();
            command.Move = ArenaSpace.ToPlanar(CameraRelative(move));

            Vec2 direction;
            Vec2 targetPoint;
            bool hasTargetPoint;
            BuildAim(worldPosition, out direction, out targetPoint, out hasTargetPoint);
            command.Aim = direction;
            command.TargetPoint = targetPoint;
            command.HasTargetPoint = hasTargetPoint;

            command.Primary = _actions.Primary.IsPressed();
            command.Special = _actions.Special.IsPressed();
            command.Dodge = _dodgeQueued;
            command.Interact = _actions.Interact.IsPressed();
            _dodgeQueued = false;
            return command;
        }

        private Vector3 CameraRelative(Vector2 input)
        {
            if (_camera == null) return new Vector3(input.x, 0f, input.y);
            Vector3 forward = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(_camera.transform.right, Vector3.up).normalized;
            return right * input.x + forward * input.y;
        }

        private void BuildAim(Vector3 worldPosition, out Vec2 direction, out Vec2 targetPoint, out bool hasTargetPoint)
        {
            if (_pointerAim && _camera != null)
            {
                Vector2 screen = _actions.Aim.ReadValue<Vector2>();
                Ray ray = _camera.ScreenPointToRay(screen);
                float distance;
                if (_ground.Raycast(ray, out distance))
                {
                    Vector3 point = ray.GetPoint(distance);
                    targetPoint = ArenaSpace.ToPlanar(point);
                    Vector3 dir = point - worldPosition;
                    dir.y = 0f;
                    direction = ArenaSpace.ToPlanar(dir);
                    hasTargetPoint = true;
                    return;
                }
            }

            Vector2 stick = _actions.Aim.ReadValue<Vector2>();
            direction = ArenaSpace.ToPlanar(CameraRelative(stick));
            targetPoint = ArenaSpace.ToPlanar(worldPosition);
            hasTargetPoint = false;
        }
    }
}
