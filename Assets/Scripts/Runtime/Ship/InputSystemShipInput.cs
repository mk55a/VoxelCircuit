using CylinderShooter.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CylinderShooter.Ship
{
    public sealed class InputSystemShipInput : MonoBehaviour, IShipInput, IFireInput
    {
        InputAction _move;
        InputAction _boost;
        InputAction _fire;
        bool _boostQueued;

        public bool FireHeld => _fire.IsPressed();

        public Vector2 Move => Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f);

        public bool ConsumeBoost()
        {
            bool pressed = _boostQueued;
            _boostQueued = false;
            return pressed;
        }

        void Awake()
        {
            _move = new InputAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            _move.AddBinding("<Gamepad>/leftStick");

            _boost = new InputAction("Boost", InputActionType.Button);
            _boost.AddBinding("<Keyboard>/space");
            _boost.AddBinding("<Gamepad>/buttonSouth");
            _boost.performed += _ => _boostQueued = true;

            _fire = new InputAction("Fire", InputActionType.Button);
            _fire.AddBinding("<Keyboard>/j");
            _fire.AddBinding("<Mouse>/leftButton");
            _fire.AddBinding("<Gamepad>/rightTrigger");
            _fire.AddBinding("<Gamepad>/buttonWest");
        }

        void OnEnable() { _move.Enable(); _boost.Enable(); _fire.Enable(); }
        void OnDisable() { _move.Disable(); _boost.Disable(); _fire.Disable(); }
        void OnDestroy() { _move.Dispose(); _boost.Dispose(); _fire.Dispose(); }
    }
}
