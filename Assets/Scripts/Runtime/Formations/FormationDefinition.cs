using UnityEngine;

namespace CylinderShooter.Formations
{
    [CreateAssetMenu(menuName = "Cylinder Shooter/Formations/Chaser Formation")]
    public sealed class FormationDefinition : ScriptableObject
    {
        [SerializeField] private FormationShape _shape = FormationShape.WaveQueue;
        [SerializeField, Min(1)] private int _count = 25;
        [SerializeField, Min(1)] private int _columns = 5;
        [SerializeField, Min(1)] private int _rows = 5;
        [SerializeField, Min(0.01f)] private float _horizontalSpacing = 1.8f;
        [SerializeField, Min(0.01f)] private float _verticalSpacing = 1.8f;
        [SerializeField] private float _centerHeight;
        [SerializeField, Min(0f)] private float _speed = 7f;
        [SerializeField, Min(0f)] private float _detectionRange = 18f;
        [SerializeField, Min(0f)] private float _waveAmplitude = 2f;
        [SerializeField, Min(0f)] private float _waveFrequency = 0.5f;
        [SerializeField] private float _wavePhaseDegrees = 30f;

        public FormationShape Shape => _shape;
        public int Count => _shape == FormationShape.Grid ? _columns * _rows : _count;
        public int Columns => _columns;
        public int Rows => _rows;
        public float HorizontalSpacing => _horizontalSpacing;
        public float VerticalSpacing => _verticalSpacing;
        public float CenterHeight => _centerHeight;
        public float Speed => _speed;
        public float DetectionRange => _detectionRange;
        public float WaveAmplitude => _shape == FormationShape.WaveQueue ? _waveAmplitude : 0f;
        public float WaveFrequency => _waveFrequency;
        public float WavePhaseDegrees => _wavePhaseDegrees;

        public bool Validate(float minHeight, float maxHeight, int maxCount, out string reason)
        {
            long count = _shape == FormationShape.Grid ? (long)_columns * _rows : _count;
            if (!System.Enum.IsDefined(typeof(FormationShape), _shape) || count < 1 || count > maxCount ||
                (_shape == FormationShape.Grid && (_columns < 1 || _rows < 1)))
            {
                reason = "Formation count or shape is invalid, or exceeds the enemy cap.";
                return false;
            }
            if (!IsFinite(_horizontalSpacing) || !IsFinite(_verticalSpacing) ||
                _horizontalSpacing <= 0f || _verticalSpacing <= 0f ||
                !IsFinite(_centerHeight) || !IsFinite(_speed) || _speed < 0f ||
                !IsFinite(_detectionRange) || _detectionRange < 0f ||
                !IsFinite(_waveAmplitude) || _waveAmplitude < 0f ||
                !IsFinite(_waveFrequency) || _waveFrequency < 0f || !IsFinite(_wavePhaseDegrees))
            {
                reason = "Formation settings must be finite, with positive spacing and non-negative speed, range and wave settings.";
                return false;
            }
            float height = _shape == FormationShape.VerticalLine ? (_count - 1) * _verticalSpacing :
                _shape == FormationShape.Grid ? (_rows - 1) * _verticalSpacing : 0f;
            float halfHeight = height * 0.5f + WaveAmplitude;
            if (_centerHeight - halfHeight < minHeight || _centerHeight + halfHeight > maxHeight)
            {
                reason = "Formation layout and wave exceed the cylinder height bounds.";
                return false;
            }
            reason = null;
            return true;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
