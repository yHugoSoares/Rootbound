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

        public PlayerInputAdapter(RootboundInputActions actions, Camera camera)
        {
            _actions = actions;
            _camera = camera;
            _pointerAim = actions.UsesPointerAim && Mouse.current != null;
        }

        public PlayerCommand Build(Vector3 worldPosition)
        {
            PlayerCommand command = default(PlayerCommand);

            Vector2 move = _actions.Move.ReadValue<Vector2>();
            command.Move = ArenaSpace.ToPlanar(CameraRelative(move));

            command.Aim = BuildAim(worldPosition);
            command.Primary = _actions.Primary.IsPressed();
            command.Special = _actions.Special.IsPressed();
            command.Dodge = _actions.Dodge.WasPressedThisFrame();
            return command;
        }

        private Vector3 CameraRelative(Vector2 input)
        {
            if (_camera == null) return new Vector3(input.x, 0f, input.y);
            Vector3 forward = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(_camera.transform.right, Vector3.up).normalized;
            return right * input.x + forward * input.y;
        }

        private Vec2 BuildAim(Vector3 worldPosition)
        {
            if (_pointerAim && _camera != null)
            {
                Vector2 screen = _actions.Aim.ReadValue<Vector2>();
                Ray ray = _camera.ScreenPointToRay(screen);
                float distance;
                if (_ground.Raycast(ray, out distance))
                {
                    Vector3 point = ray.GetPoint(distance);
                    Vector3 dir = point - worldPosition;
                    dir.y = 0f;
                    return ArenaSpace.ToPlanar(dir);
                }
                return Vec2.Zero;
            }

            Vector2 stick = _actions.Aim.ReadValue<Vector2>();
            return ArenaSpace.ToPlanar(CameraRelative(stick));
        }
    }
}
