using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Formations
{
    public sealed class FormationMotion
    {
        private readonly FormationShape _shape;
        private readonly int _count;
        private readonly int _columns;
        private readonly int _rows;
        private readonly float _horizontalSpacing;
        private readonly float _verticalSpacing;
        private readonly float _radius;
        private readonly float _speed;
        private readonly float _detectionRange;
        private readonly float _amplitude;
        private readonly float _frequency;
        private readonly float _phase;
        private readonly int _direction;
        private CylinderPosition _center;
        private float _elapsed;
        public int MembersRemaining { get; private set; }

        public FormationMotion(FormationDefinition definition, CylinderPosition center, float radius, int direction)
        {
            _shape = definition.Shape;
            _count = definition.Count;
            MembersRemaining = _count;
            _columns = definition.Columns;
            _rows = definition.Rows;
            _horizontalSpacing = definition.HorizontalSpacing;
            _verticalSpacing = definition.VerticalSpacing;
            _speed = definition.Speed;
            _detectionRange = definition.DetectionRange;
            _amplitude = definition.WaveAmplitude;
            _frequency = definition.WaveFrequency;
            _phase = definition.WavePhaseDegrees * Mathf.Deg2Rad;
            _radius = radius;
            _direction = direction;
            _center = center;
        }

        public void Advance(float deltaTime)
        {
            _elapsed += deltaTime;
            _center = _center.Moved(-_direction * _speed * deltaTime / _radius, 0f);
        }

        public void SetCenter(CylinderPosition center)
        {
            _center = center;
        }

        public void RemoveMember()
        {
            if (MembersRemaining <= 0)
                throw new System.InvalidOperationException("Formation member removed more than once.");
            MembersRemaining--;
        }

        public CylinderPosition PositionAt(int slot)
        {
            float x = 0f;
            float y = 0f;
            if (_shape == FormationShape.HorizontalLine || _shape == FormationShape.WaveQueue)
                x = (slot - (_count - 1) * 0.5f) * _horizontalSpacing;
            else if (_shape == FormationShape.VerticalLine)
                y = (slot - (_count - 1) * 0.5f) * _verticalSpacing;
            else
            {
                x = (slot % _columns - (_columns - 1) * 0.5f) * _horizontalSpacing;
                y = (slot / _columns - (_rows - 1) * 0.5f) * _verticalSpacing;
            }

            if (_shape == FormationShape.WaveQueue)
                y += _amplitude * Mathf.Sin(_elapsed * _frequency * Mathf.PI * 2f - slot * _phase);
            return _center.Moved(-x / _radius, y);
        }

        public float HeadingAt(int slot)
        {
            float verticalSpeed = _shape == FormationShape.WaveQueue
                ? _amplitude * _frequency * Mathf.PI * 2f *
                  Mathf.Cos(_elapsed * _frequency * Mathf.PI * 2f - slot * _phase)
                : 0f;
            return Mathf.Atan2(verticalSpeed, _direction * _speed) * Mathf.Rad2Deg;
        }

        public bool IsWithinDetectionRange(CylinderPosition position, CylinderPosition target)
        {
            float horizontal = CylinderSpace.ShortestDelta(position.Angle, target.Angle) * _radius;
            float vertical = position.Height - target.Height;
            return horizontal * horizontal + vertical * vertical <= _detectionRange * _detectionRange;
        }
    }
}
