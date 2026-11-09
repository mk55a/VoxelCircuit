#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using CylinderShooter.Cam;
using CylinderShooter.Enemies;
using CylinderShooter.Ship;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CylinderShooter.Formations
{
    [DefaultExecutionOrder(-50)]
    public sealed class FormationTestSpawner : MonoBehaviour
    {
        [SerializeField] private ShipController _ship;
        [SerializeField] private Camera _camera;
        [SerializeField] private FovBoostEffect _fovBoost;
        [SerializeField] private CylinderConfig _cylinder;
        [SerializeField] private Chaser _prefab;
        [SerializeField] private MeshFilter _arena;
        [SerializeField] private FormationDefinition _formation;
        [SerializeField] private FormationEntrySide _entrySide = FormationEntrySide.Random;
        [SerializeField, Min(1)] private int _maxEnemies = 150;
        [SerializeField, Min(0)] private int _prewarm = 25;
        [SerializeField, Min(0f)] private float _viewportMargin = 0.02f;

        private readonly Stack<Chaser> _available = new Stack<Chaser>();
        private readonly HashSet<Chaser> _active = new HashSet<Chaser>();
        private readonly List<FormationMotion> _motions = new List<FormationMotion>();
        private Transform _inactiveRoot;
        private InputAction _spawnAction;
        private InputAction _clearAction;
        private float _visualRadius;
        private MeshCollider _occlusionProbe;

        public int ActiveCount => _active.Count;

        private void Awake()
        {
            if (_ship == null || _camera == null || _cylinder == null || _prefab == null ||
                _cylinder.Radius <= 0f || _cylinder.MinHeight >= _cylinder.MaxHeight ||
                _maxEnemies < 1 || _prewarm < 0 || !_prefab.enabled || !_prefab.HasValidTuning)
            {
                Debug.LogError("Formation test spawner requires a ship, camera, valid cylinder, Chaser prefab and pool settings.", this);
                enabled = false;
                return;
            }

            var root = new GameObject("Inactive Chaser Pool");
            root.transform.SetParent(transform, false);
            root.SetActive(false);
            _inactiveRoot = root.transform;
            var probe = new GameObject("Formation Occlusion Probe");
            probe.transform.SetParent(transform, false);
            _occlusionProbe = probe.AddComponent<MeshCollider>();
            _occlusionProbe.enabled = false;
            _visualRadius = CalculateVisualRadius(_prefab);
            for (int i = 0; i < Mathf.Min(_prewarm, _maxEnemies); i++)
                _available.Push(CreateChaser());

            _spawnAction = new InputAction("Spawn formation", InputActionType.Button, "<Keyboard>/f");
            _clearAction = new InputAction("Clear formations", InputActionType.Button, "<Keyboard>/r");
        }

        private void OnEnable()
        {
            foreach (var chaser in _available)
                if (chaser != null && chaser.gameObject.activeSelf) chaser.gameObject.SetActive(false);
            _spawnAction?.Enable();
            _clearAction?.Enable();
        }

        private void OnDisable()
        {
            _spawnAction?.Disable();
            _clearAction?.Disable();
            if (gameObject.activeInHierarchy) Clear();
        }

        private void OnDestroy()
        {
            _spawnAction?.Dispose();
            _clearAction?.Dispose();
        }

        private void Update()
        {
            for (int i = _motions.Count - 1; i >= 0; i--)
            {
                if (_motions[i].MembersRemaining == 0) _motions.RemoveAt(i);
                else _motions[i].Advance(Time.deltaTime);
            }
            if (_spawnAction.WasPressedThisFrame()) Spawn();
            if (_clearAction.WasPressedThisFrame()) Clear();
        }

        public bool Spawn()
        {
            if (!enabled || _formation == null)
                return Reject("Assign a formation definition to the test spawner.");
            if (!_formation.Validate(_cylinder.MinHeight, _cylinder.MaxHeight, _maxEnemies, out string reason))
                return Reject(reason);
            if (_active.Count + _formation.Count > _maxEnemies)
                return Reject("The formation would exceed the sandbox enemy cap.");

            int side = _entrySide == FormationEntrySide.Random ? (Random.value < 0.5f ? -1 : 1)
                : _entrySide == FormationEntrySide.Left ? -1 : 1;
            if (!TryFindSpawn(side, out var motion))
                return Reject("Formation cannot fit off-screen or behind an opaque arena on the chosen side.");

            _motions.Add(motion);
            for (int i = 0; i < _formation.Count; i++)
            {
                var chaser = _available.Count > 0 ? _available.Pop() : CreateChaser();
                chaser.PrepareSpawn(_ship, motion, i, Return);
                _active.Add(chaser);
                chaser.transform.SetParent(transform, false);
                chaser.gameObject.SetActive(true);
            }
            return true;
        }

        public void Clear()
        {
            while (_active.Count > 0)
            {
                Chaser member = null;
                foreach (var active in _active) { member = active; break; }
                if (member == null)
                {
                    Debug.LogError("A sandbox Chaser was destroyed outside its pool.", this);
                    _active.Clear();
                    break;
                }
                member.gameObject.SetActive(false);
            }
            _motions.Clear();
        }

        private Chaser CreateChaser()
        {
            var instance = Instantiate(_prefab, _inactiveRoot);
            instance.gameObject.SetActive(false);
            return instance;
        }

        private void Return(Chaser chaser)
        {
            if (!_active.Remove(chaser))
            {
                Debug.LogError("A Chaser was returned to the formation pool more than once.", this);
                return;
            }
            _available.Push(chaser);
            if (_active.Count == 0) _motions.Clear();
        }

        private bool TryFindSpawn(int side, out FormationMotion result)
        {
            float radius = _cylinder.Radius;
            Vector3 cameraOffset = _camera.transform.position;
            float cameraAngle = Mathf.Atan2(cameraOffset.x, cameraOffset.z);
            float extent = Mathf.Max(_prefab.Radius, _visualRadius);
            bool canOcclude = PrepareOcclusionProbe();
            float widestFov = _fovBoost == null ? _camera.fieldOfView
                : Mathf.Max(_camera.fieldOfView, _fovBoost.MaximumFieldOfView);
            float viewportScale = _camera.orthographic ? 1f
                : Mathf.Tan(_camera.fieldOfView * Mathf.Deg2Rad * 0.5f) /
                  Mathf.Tan(Mathf.Min(widestFov, 179f) * Mathf.Deg2Rad * 0.5f);
            var motion = new FormationMotion(_formation,
                new Core.CylinderPosition(cameraAngle, _formation.CenterHeight), radius, -side);
            for (int step = 1; step < 1024; step++)
            {
                float angle = cameraAngle - side * (Mathf.PI * step / 1024f);
                motion.SetCenter(new Core.CylinderPosition(angle, _formation.CenterHeight));
                bool fits = true;
                for (int i = 0; i < _formation.Count && fits; i++)
                {
                    var position = motion.PositionAt(i);
                    var world = position.ToWorld(radius);
                    float sideAngle = -side * Core.CylinderSpace.ShortestDelta(cameraAngle, position.Angle);
                    if (sideAngle <= 0f || sideAngle >= Mathf.PI)
                    {
                        fits = false;
                        break;
                    }
                    float envelope = extent + _formation.WaveAmplitude;
                    bool outside = true;
                    bool hidden = canOcclude;
                    for (int corner = 0; corner < 8; corner++)
                    {
                        Vector3 point = world + new Vector3((corner & 1) == 0 ? -extent : extent,
                            (corner & 2) == 0 ? -envelope : envelope, (corner & 4) == 0 ? -extent : extent);
                        var viewport = _camera.WorldToViewportPoint(point);
                        viewport.x = 0.5f + (viewport.x - 0.5f) * viewportScale;
                        if (viewport.z <= _camera.nearClipPlane ||
                            (side < 0 ? viewport.x > -_viewportMargin : viewport.x < 1f + _viewportMargin))
                            outside = false;
                        Vector3 ray = point - _camera.transform.position;
                        if (hidden && !_occlusionProbe.Raycast(new Ray(_camera.transform.position, ray.normalized),
                            out _, ray.magnitude - 0.01f))
                            hidden = false;
                    }
                    fits = outside || hidden;
                }
                if (fits)
                {
                    if (canOcclude) _occlusionProbe.enabled = false;
                    result = motion;
                    return true;
                }
            }
            if (canOcclude) _occlusionProbe.enabled = false;
            result = null;
            return false;
        }

        private bool PrepareOcclusionProbe()
        {
            if (_arena == null || _arena.sharedMesh == null) return false;
            var renderer = _arena.GetComponent<Renderer>();
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                (_camera.cullingMask & (1 << renderer.gameObject.layer)) == 0) return false;
            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null || material.renderQueue >= 2501 ||
                    (material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0f) ||
                    (material.HasProperty("_AlphaClip") && material.GetFloat("_AlphaClip") > 0f))
                    return false;
            }
            _occlusionProbe.transform.SetPositionAndRotation(_arena.transform.position, _arena.transform.rotation);
            _occlusionProbe.transform.localScale = _arena.transform.lossyScale;
            _occlusionProbe.sharedMesh = _arena.sharedMesh;
            _occlusionProbe.enabled = true;
            return true;
        }

        private static float CalculateVisualRadius(Chaser prefab)
        {
            float radius = 0f;
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                Bounds bounds = renderer.localBounds;
                for (int i = 0; i < 8; i++)
                {
                    var point = bounds.center + Vector3.Scale(bounds.extents,
                        new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f,
                            (i & 4) == 0 ? -1f : 1f));
                    radius = Mathf.Max(radius, (renderer.transform.TransformPoint(point) - prefab.transform.position).magnitude);
                }
            }
            return radius;
        }

        private bool Reject(string reason)
        {
            Debug.LogWarning("Formation spawn cancelled: " + reason, this);
            return false;
        }
    }
}
#endif
