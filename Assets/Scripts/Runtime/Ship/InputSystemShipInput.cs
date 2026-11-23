using CylinderShooter.Combat;
using CylinderShooter.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CylinderShooter.Ship
{
    /// <summary>
    /// Keyboard, mouse and gamepad input. Firing is two-sided: left mouse / J / left trigger fires screen-left,
    /// right mouse / L / right trigger fires screen-right, one side at a time. Shockwave: K / E / middle mouse /
    /// gamepad Y. Runs before the ship and weapons.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class InputSystemShipInput : MonoBehaviour, IShipInput, IFireInput, IShockwaveInput
    {
        readonly FireSideSelector _fireSide = new FireSideSelector();
        InputAction _move;
        InputAction _boost;
        InputAction _fireLeft;
        InputAction _fireRight;
        InputAction _shockwave;
        bool _boostQueued;
        bool _shockwaveQueued;

        public int FireDirection => _fireSide.Current;

        public Vector2 Move => Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f);

        public bool ConsumeBoost()
        {
            bool pressed = _boostQueued;
            _boostQueued = false;
            return pressed;
        }

        public bool ConsumeShockwave()
        {
            bool pressed = _shockwaveQueued;
            _shockwaveQueued = false;
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

            _fireLeft = new InputAction("Fire Left", InputActionType.Button);
            _fireLeft.AddBinding("<Mouse>/leftButton");
            _fireLeft.AddBinding("<Keyboard>/j");
            _fireLeft.AddBinding("<Gamepad>/leftTrigger");

            _fireRight = new InputAction("Fire Right", InputActionType.Button);
            _fireRight.AddBinding("<Mouse>/rightButton");
            _fireRight.AddBinding("<Keyboard>/l");
            _fireRight.AddBinding("<Gamepad>/rightTrigger");

            _shockwave = new InputAction("Shockwave", InputActionType.Button);
            _shockwave.AddBinding("<Keyboard>/k");
            _shockwave.AddBinding("<Keyboard>/e");
            _shockwave.AddBinding("<Mouse>/middleButton");
            _shockwave.AddBinding("<Gamepad>/buttonNorth");
            _shockwave.performed += _ => _shockwaveQueued = true;
        }

        void Update() => _fireSide.Update(_fireLeft.IsPressed(), _fireRight.IsPressed());

        void OnEnable() { _move.Enable(); _boost.Enable(); _fireLeft.Enable(); _fireRight.Enable(); _shockwave.Enable(); }
        void OnDisable()
        {
            _move.Disable(); _boost.Disable(); _fireLeft.Disable(); _fireRight.Disable(); _shockwave.Disable();
            _fireSide.Reset();
        }
        void OnDestroy() { _move.Dispose(); _boost.Dispose(); _fireLeft.Dispose(); _fireRight.Dispose(); _shockwave.Dispose(); }
    }
}
