using UnityEngine;

namespace CylinderShooter.Core
{
    /// <summary>
    /// One shockwave as pure state. It is an expanding ring centred on where it was triggered, measured on the
    /// unrolled cylinder surface (x = distance along the surface, y = height):
    ///   o  - a small ring around the player (build-up; it already destroys what it touches),
    ///   O  - the ring grows; once it is taller than the cylinder its top and bottom are cut off,
    ///  ( ) - as it is thrown outward the round shape relaxes into a flatter bend with a fixed depth.
    /// Each half is half of an ellipse: front(y) = D - c + c * sqrt(1 - (y / H)^2), where D is the distance of the
    /// middle of the front, H the half height and c the bend depth (c = D = H gives a circle).
    /// Only the band just behind the front hits (swept between steps), so anything that appears where the wave has
    /// already passed is safe. The two halves meet on the far side of the cylinder.
    /// </summary>
    public sealed class ShockwaveFront
    {
        public const int Right = 1;
        public const int Left = -1;

        private IShockwaveSettings _settings;
        private float _radius;

        public bool IsActive { get; private set; }
        public float OriginAngle { get; private set; }

        /// <summary>Height the ring is centred on: where the player was when it was triggered.</summary>
        public float CenterHeight { get; private set; }

        /// <summary>Lowest and highest point the wave covers (the cylinder wall).</summary>
        public float Bottom { get; private set; }
        public float Top { get; private set; }

        /// <summary>Seconds since the wave was triggered.</summary>
        public float Elapsed { get; private set; }

        /// <summary>Distance the middle of the front has travelled from the origin along the surface, in metres.</summary>
        public float Distance { get; private set; }

        /// <summary>Distance at the previous step: the band between the two is swept this step.</summary>
        public float PreviousDistance { get; private set; }

        /// <summary>0 when triggered, 1 at the end of the build-up (eased).</summary>
        public float Formation { get; private set; }

        /// <summary>0 at release, 1 when the halves meet on the far side.</summary>
        public float ReleaseProgress { get; private set; }

        public bool IsReleased => Elapsed >= _settings.FormDuration;

        /// <summary>Half height a ring needs to reach both the top and the bottom of the cylinder from its centre.</summary>
        public float FullHalfHeight => Mathf.Max(Top - CenterHeight, CenterHeight - Bottom);

        /// <summary>Half height of the ring right now: its radius, until it is tall enough to fill the cylinder.</summary>
        public float HalfHeight => Mathf.Max(0.01f, Mathf.Min(Distance, FullHalfHeight));

        /// <summary>Bend depth right now: a circle (equal to the radius) while growing, relaxing to the profile's depth.</summary>
        public float CurveDepth
        {
            get
            {
                float round = Mathf.Min(Distance, FullHalfHeight);
                if (Distance <= FullHalfHeight) return round;
                float relaxed = Mathf.Min(_settings.CurveDepth, round);
                if (_settings.FlattenDistance <= 0f) return relaxed;
                float t = Mathf.Clamp01((Distance - FullHalfHeight) / _settings.FlattenDistance);
                return Mathf.Lerp(round, relaxed, t * t * (3f - 2f * t));
            }
        }

        /// <summary>Half the circumference plus the bend, so the lagging ends also reach the far side.</summary>
        public float TravelDistance => Mathf.PI * _radius + _settings.CurveDepth;

        public void Start(IShockwaveSettings settings, float originAngle, float originHeight,
            float radius, float bottom, float top)
        {
            _settings = settings;
            _radius = radius;
            Bottom = bottom;
            Top = top;
            CenterHeight = Mathf.Clamp(originHeight, bottom, top);
            OriginAngle = CylinderSpace.WrapAngle(originAngle);
            Elapsed = 0f;
            IsActive = true;
            // The ring exists from the first frame, so whatever touches the player right now is already inside it.
            PreviousDistance = 0f;
            UpdateDistance();
        }

        /// <summary>Moves the front. The step that reaches the far side still counts; the one after ends the wave.</summary>
        public void Advance(float deltaTime)
        {
            if (!IsActive) return;
            if (Elapsed >= _settings.FormDuration + _settings.Duration)
            {
                IsActive = false;
                return;
            }

            Elapsed += Mathf.Max(0f, deltaTime);
            PreviousDistance = Distance;
            UpdateDistance();
        }

        /// <summary>
        /// How far the front lags behind its middle at this height (0 in the middle). Infinity above or below the ring.
        /// </summary>
        public float LagAt(float height)
        {
            float normalized = (height - CenterHeight) / HalfHeight;
            if (normalized * normalized > 1f) return float.PositiveInfinity;
            return CurveDepth * (1f - Mathf.Sqrt(1f - normalized * normalized));
        }

        /// <summary>True if a target of this size touches the band the front swept during the last step.</summary>
        public bool Hits(float targetAngle, float targetHeight, float targetRadius)
        {
            if (!IsActive) return false;

            // Test the target's nearest point to the ring's centre line, so a big target at the ring's tip still counts.
            float nearestHeight = Mathf.MoveTowards(targetHeight, CenterHeight, targetRadius);
            float lag = LagAt(nearestHeight);
            if (float.IsPositiveInfinity(lag)) return false;

            float distance = Mathf.Abs(CylinderSpace.ShortestDelta(OriginAngle, targetAngle)) * _radius;
            float leadingEdge = Distance - lag;
            float trailingEdge = PreviousDistance - lag - _settings.Thickness;
            return distance - targetRadius <= leadingEdge && distance + targetRadius >= trailingEdge;
        }

        /// <summary>Angle of the middle of the front for one half. Screen-right is decreasing angle.</summary>
        public float FrontAngle(int side)
        {
            float travelled = CylinderSpace.ArcToAngle(Distance, _radius);
            return CylinderSpace.WrapAngle(side == Right ? OriginAngle - travelled : OriginAngle + travelled);
        }

        private void UpdateDistance()
        {
            float form = _settings.FormDuration > 0f ? Mathf.Clamp01(Elapsed / _settings.FormDuration) : 1f;
            Formation = 1f - (1f - form) * (1f - form);

            float release = _settings.Duration > 0f
                ? Mathf.Clamp01((Elapsed - _settings.FormDuration) / _settings.Duration)
                : 1f;
            ReleaseProgress = IsReleased ? release : 0f;

            if (!IsReleased)
            {
                Distance = Mathf.Lerp(_settings.FormRadius, _settings.FormDistance, Formation);
                return;
            }
            float start = _settings.FormDuration > 0f ? _settings.FormDistance : _settings.FormRadius;
            float thrown = 1f - Mathf.Pow(1f - ReleaseProgress, Mathf.Max(1f, _settings.EaseOut));
            Distance = Mathf.Lerp(start, TravelDistance, thrown);
        }
    }
}
