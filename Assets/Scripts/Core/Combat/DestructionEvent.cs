namespace CylinderShooter.Core
{
    /// <summary>Where something was destroyed, and optionally what destroyed it (for direction-based effects).</summary>
    public readonly struct DestructionEvent
    {
        /// <summary>Destroyed with no known instigator: effects burst evenly.</summary>
        public DestructionEvent(CylinderPosition position) : this(position, false, 0f) { }

        /// <summary>Destroyed by something at <paramref name="instigatorAngle"/>: effects are pushed away from it.</summary>
        public DestructionEvent(CylinderPosition position, float instigatorAngle) : this(position, true, instigatorAngle) { }

        private DestructionEvent(CylinderPosition position, bool hasInstigator, float instigatorAngle)
        {
            Position = position;
            HasInstigator = hasInstigator;
            InstigatorAngle = instigatorAngle;
        }

        public CylinderPosition Position { get; }
        public bool HasInstigator { get; }
        public float InstigatorAngle { get; }
    }
}
