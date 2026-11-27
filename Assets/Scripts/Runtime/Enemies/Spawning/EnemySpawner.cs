using System.Collections.Generic;
using CylinderShooter.Cam;
using CylinderShooter.Core;
using CylinderShooter.Formations;
using CylinderShooter.Ship;
using UnityEngine;

namespace CylinderShooter.Enemies
{
    /// <summary>
    /// Brings the director's groups into the arena: asks the spawn gate, finds the exact place (just off-screen on the
    /// requested side, or a set distance along the ring), keeps groups apart, takes enemies from the pool and moves the
    /// formations. Decides only *where exactly*; what, how many, which lane and which side come from the director.
    /// </summary>
    public sealed class EnemySpawner : MonoBehaviour, IEnemySpawner
    {
        private const string NoPlaceReason = "no free place on that side right now";

        [SerializeField] private ShipController _ship;
        [SerializeField] private Camera _camera;
        [Tooltip("Optional: the camera's boost zoom, so spawns stay hidden even at the widest view.")]
        [SerializeField] private FovBoostEffect _fovBoost;
        [SerializeField] private EnemyPool _pool;
        [Tooltip("Where groups may appear (e.g. Assets/Tuning/Director/SpawnPlacement).")]
        [SerializeField] private SpawnPlacementProfile _placement;

        private readonly List<FormationMotion> _motions = new List<FormationMotion>();
        private CylinderConfig _cylinder;

        public int ActiveFormations => _motions.Count;
        public ShipController Ship => _ship;

        private void Awake()
        {
            if (_ship == null || _camera == null || _pool == null || _placement == null)
            {
                Debug.LogError("EnemySpawner needs 'Ship' (the player), 'Camera' (Main Camera), 'Pool' (the Enemy Pool) " +
                               "and 'Placement' (Assets/Tuning/Director/SpawnPlacement).", this);
                enabled = false;
                return;
            }
            _cylinder = ServiceLocator.Get<CylinderConfig>();
        }

        private void Update()
        {
            for (int i = _motions.Count - 1; i >= 0; i--)
            {
                if (_motions[i].MembersRemaining == 0) _motions.RemoveAt(i);
                else _motions[i].Advance(Time.deltaTime);
            }
        }

        public SpawnOutcome TrySpawn(in SpawnRequest request, ISpawnApproval approval)
        {
            if (!enabled || _cylinder == null) return SpawnOutcome.Invalid("the spawner isn't set up");
            if (!(request.Enemy is EnemyDefinition enemy))
                return SpawnOutcome.Invalid("the group's enemy isn't an Enemy asset");
            if (!(request.Formation is EnemyFormationOption option) || option.Formation == null)
                return SpawnOutcome.Invalid($"'{enemy.DisplayName}' has a formation row with no formation");
            var prefab = enemy.Prefab;
            if (prefab == null || !prefab.HasValidTuning)
                return SpawnOutcome.Invalid($"'{enemy.DisplayName}' has no usable prefab");

            var formation = option.Formation;
            int count = request.Count;
            float halfHeight = formation.HalfHeightFor(count);
            float minHeight = _cylinder.MinHeight + halfHeight;
            float maxHeight = _cylinder.MaxHeight - halfHeight;
            if (minHeight > maxHeight)
                return SpawnOutcome.Invalid($"{count} in '{formation.name}' is taller than the arena");
            float height = Mathf.Clamp(request.Height, minHeight, maxHeight);

            var verdict = approval.Approve(enemy, count);
            if (verdict != SpawnVerdict.Approved)
                return SpawnOutcome.Refused(verdict, SpawnGate.Describe(verdict, enemy, count));

            int side = request.ScreenSide >= 0 ? 1 : -1;
            bool placed = request.Entry != null && request.Entry.Style == EntryStyle.AtDistance
                ? TryPlaceAtDistance(formation, count, height, side, request.Entry.Distance, out var motion)
                : TryFindHiddenPlace(formation, count, height, prefab.Radius, side, out motion);
            if (!placed) return SpawnOutcome.NoRoom(NoPlaceReason);

            _motions.Add(motion);
            for (int i = 0; i < count; i++)
            {
                var member = _pool.Take(enemy);
                member.PrepareSpawn(_ship, motion, i, _pool.ReturnHandler);
                _pool.Activate(member);
            }
            return SpawnOutcome.Spawned(count);
        }

        /// <summary>A set distance along the ring on one side, moved further out if another group is in the way.</summary>
        private bool TryPlaceAtDistance(FormationDefinition formation, int count, float height, int side, float distance,
            out FormationMotion result)
        {
            float radius = _cylinder.Radius;
            float maxDistance = Mathf.PI * radius;
            float step = Mathf.Max(1f, _placement.GroupSpacing);
            var motion = new FormationMotion(formation, _ship.Position, radius, -side, count);
            for (int attempt = 0; attempt < 8; attempt++)
            {
                float along = Mathf.Min(maxDistance, distance + attempt * step);
                var center = new CylinderPosition(_ship.Position.Angle - side * along / radius, height);
                if (!IsCrowded(center))
                {
                    motion.SetCenter(center);
                    result = motion;
                    return true;
                }
            }
            result = null;
            return false;
        }

        /// <summary>
        /// Searches outward from the camera on one side for the first centre where every member is outside the (widest)
        /// view or far enough away to be lost in the fog, and no other group is too close.
        /// </summary>
        private bool TryFindHiddenPlace(FormationDefinition formation, int count, float height, float memberRadius,
            int side, out FormationMotion result)
        {
            float radius = _cylinder.Radius;
            Vector3 cameraPosition = _camera.transform.position;
            float cameraAngle = Mathf.Atan2(cameraPosition.x, cameraPosition.z);
            float widestFov = _fovBoost == null ? _camera.fieldOfView
                : Mathf.Max(_camera.fieldOfView, _fovBoost.MaximumFieldOfView);
            float viewportScale = _camera.orthographic ? 1f
                : Mathf.Tan(_camera.fieldOfView * Mathf.Deg2Rad * 0.5f) /
                  Mathf.Tan(Mathf.Min(widestFov, 179f) * Mathf.Deg2Rad * 0.5f);
            float margin = _placement.ViewportMargin;
            float hiddenBeyond = _placement.HiddenBeyondDistance;
            int steps = _placement.SearchSteps;
            float extent = memberRadius;
            float envelope = extent + formation.WaveAmplitude;

            var motion = new FormationMotion(formation, new CylinderPosition(cameraAngle, height), radius, -side, count);
            for (int step = 1; step < steps; step++)
            {
                float angle = cameraAngle - side * (Mathf.PI * step / steps);
                var center = new CylinderPosition(angle, height);
                motion.SetCenter(center);
                bool fits = true;
                for (int i = 0; i < count && fits; i++)
                {
                    var position = motion.PositionAt(i);
                    float sideAngle = -side * CylinderSpace.ShortestDelta(cameraAngle, position.Angle);
                    if (sideAngle <= 0f || sideAngle >= Mathf.PI)
                    {
                        fits = false;
                        break;
                    }
                    var world = position.ToWorld(radius);
                    bool outside = true;
                    bool hidden = hiddenBeyond > 0f;
                    for (int corner = 0; corner < 8; corner++)
                    {
                        Vector3 point = world + new Vector3((corner & 1) == 0 ? -extent : extent,
                            (corner & 2) == 0 ? -envelope : envelope, (corner & 4) == 0 ? -extent : extent);
                        var viewport = _camera.WorldToViewportPoint(point);
                        viewport.x = 0.5f + (viewport.x - 0.5f) * viewportScale;
                        if (viewport.z <= _camera.nearClipPlane ||
                            (side < 0 ? viewport.x > -margin : viewport.x < 1f + margin))
                            outside = false;
                        if (hidden && (point - cameraPosition).magnitude < hiddenBeyond) hidden = false;
                    }
                    fits = outside || hidden;
                }
                if (fits && !IsCrowded(center))
                {
                    result = motion;
                    return true;
                }
            }
            result = null;
            return false;
        }

        /// <summary>True when another group's centre is closer than the placement's Group Spacing.</summary>
        private bool IsCrowded(CylinderPosition center)
        {
            float spacing = _placement.GroupSpacing;
            if (spacing <= 0f) return false;
            float radius = _cylinder.Radius;
            for (int i = 0; i < _motions.Count; i++)
            {
                var other = _motions[i].Center;
                float along = CylinderSpace.ShortestDelta(center.Angle, other.Angle) * radius;
                float up = center.Height - other.Height;
                if (along * along + up * up < spacing * spacing) return true;
            }
            return false;
        }
    }
}
