using System;
using UnityEngine.InputSystem;

namespace Rootbound.Unity
{
    public sealed class RootboundInputActions : IDisposable
    {
        public readonly InputAction Move;
        public readonly InputAction Aim;
        public readonly InputAction Primary;
        public readonly InputAction Special;
        public readonly InputAction Dodge;
        public readonly InputAction Interact;

        private readonly InputActionMap _map;

        public bool UsesPointerAim { get; private set; }

        public RootboundInputActions(int playerIndex)
        {
            _map = new InputActionMap(playerIndex == 0 ? "KeyboardMouse" : "Gamepad");
            Move = _map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            Aim = _map.AddAction("Aim", InputActionType.Value, expectedControlLayout: "Vector2");
            Primary = _map.AddAction("Primary", InputActionType.Button);
            Special = _map.AddAction("Special", InputActionType.Button);
            Dodge = _map.AddAction("Dodge", InputActionType.Button);
            Interact = _map.AddAction("Interact", InputActionType.Button);

            if (playerIndex == 0) ConfigureKeyboardMouse();
            else ConfigureGamepad();
        }

        private void ConfigureKeyboardMouse()
        {
            UsesPointerAim = true;
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            Move.AddBinding("<Gamepad>/leftStick");
            Aim.AddBinding("<Mouse>/position");
            Aim.AddBinding("<Gamepad>/rightStick");
            Primary.AddBinding("<Mouse>/leftButton");
            Primary.AddBinding("<Gamepad>/buttonSouth");
            Special.AddBinding("<Keyboard>/q");
            Special.AddBinding("<Mouse>/rightButton");
            Special.AddBinding("<Gamepad>/buttonWest");
            Dodge.AddBinding("<Keyboard>/space");
            Dodge.AddBinding("<Gamepad>/buttonEast");
            Interact.AddBinding("<Keyboard>/e");
            Interact.AddBinding("<Gamepad>/buttonNorth");
        }

        private void ConfigureGamepad()
        {
            UsesPointerAim = false;
            Move.AddBinding("<Gamepad>/leftStick");
            Aim.AddBinding("<Gamepad>/rightStick");
            Primary.AddBinding("<Gamepad>/rightTrigger");
            Special.AddBinding("<Gamepad>/leftTrigger");
            Dodge.AddBinding("<Gamepad>/buttonEast");
            Interact.AddBinding("<Gamepad>/buttonNorth");
        }

        public void Enable()
        {
            _map.Enable();
        }

        public void Disable()
        {
            _map.Disable();
        }

        public void Dispose()
        {
            _map.Disable();
            _map.Dispose();
        }
    }
}
