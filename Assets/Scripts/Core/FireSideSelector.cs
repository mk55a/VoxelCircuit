namespace CylinderShooter.Core
{
    /// <summary>
    /// Turns two "fire" buttons (left side, right side) into one firing direction, so only one side fires at a time.
    /// While both are held, the button pressed most recently wins; releasing it hands back to the other.
    /// </summary>
    public sealed class FireSideSelector
    {
        public const int None = 0;
        public const int Left = -1;
        public const int Right = 1;

        private bool _leftWasHeld;
        private bool _rightWasHeld;
        private int _lastSide = Right;

        /// <summary>-1 left, +1 right, 0 not firing.</summary>
        public int Current { get; private set; }

        /// <summary>Call once per frame with the buttons' held state.</summary>
        public int Update(bool leftHeld, bool rightHeld)
        {
            bool leftPressed = leftHeld && !_leftWasHeld;
            bool rightPressed = rightHeld && !_rightWasHeld;
            _leftWasHeld = leftHeld;
            _rightWasHeld = rightHeld;

            if (leftPressed != rightPressed) Current = leftPressed ? Left : Right;
            else if (leftHeld != rightHeld) Current = leftHeld ? Left : Right;
            else if (!leftHeld) Current = None;
            else if (Current == None) Current = _lastSide;   // both pressed on the same frame

            if (Current != None) _lastSide = Current;
            return Current;
        }

        public void Reset()
        {
            _leftWasHeld = false;
            _rightWasHeld = false;
            Current = None;
        }
    }
}
