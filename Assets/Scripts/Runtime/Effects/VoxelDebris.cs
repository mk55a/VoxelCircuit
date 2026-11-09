using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Effects
{
    /// <summary>
    /// Scene-wide debris service: one pooled particle system that every destructible shares. Owns only what
    /// is global to all debris (rendering, gravity, drag, floor); per-destruction feel comes from a DebrisProfile.
    /// Initialised and registered as IDebrisEmitter by GameInstaller.
    /// </summary>
    public sealed class VoxelDebris : MonoBehaviour, IDebrisEmitter
    {
        [Header("Rendering")]
        [Tooltip("URP Particles/Lit, Opaque. Leave empty to create a metallic one at runtime.")]
        [SerializeField] private Material _material;
        [Tooltip("Leave empty to use Unity's built-in cube.")]
        [SerializeField] private Mesh _mesh;
        [SerializeField, Min(100)] private int _maxParticles = 4000;

        [Header("Physics (shared by all debris)")]
        [SerializeField, Min(0f)] private float _gravityMultiplier = 1.4f;
        [SerializeField, Range(0f, 5f)] private float _drag = 0.6f;
        [SerializeField] private bool _collideWithFloor = true;
        [Tooltip("Floor height = the play area's minimum height minus this.")]
        [SerializeField] private float _floorBelowPlayArea = 1f;
        [SerializeField, Range(0f, 1f)] private float _bounce = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _bounceDamping = 0.4f;

        private ParticleSystem _system;
        private ParticleSystem.EmitParams _emit;

        public void Initialize(CylinderConfig cylinder)
        {
            if (_system != null) return;
            if (cylinder == null)
            {
                Debug.LogError("VoxelDebris requires a cylinder config.", this);
                enabled = false;
                return;
            }

            var go = new GameObject("Voxel Debris Particles");
            go.transform.SetParent(transform, false);
            _system = go.AddComponent<ParticleSystem>();
            _system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ConfigureMain();
            ConfigureMotion();
            if (_collideWithFloor) ConfigureFloor(cylinder.MinHeight - _floorBelowPlayArea);
            ConfigureRenderer(go.GetComponent<ParticleSystemRenderer>());

            _emit = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            _system.Play();
        }

        public void Emit(VoxelShape shape, Transform model, Vector3 push, DebrisProfile profile)
        {
            if (_system == null || shape == null || shape.Count == 0 || model == null || profile == null) return;

            int stride = DebrisMath.Stride(shape.Count, profile.MaxPieces);
            float size = shape.VoxelSize * DebrisMath.SizeForStride(stride) * profile.SizeMultiplier * model.lossyScale.x;
            Vector3 center = model.position;
            Vector3 towardCamera = CylinderSpace.Outward(Mathf.Atan2(center.x, center.z));
            Vector3 startRotation = model.rotation.eulerAngles;
            Color32 uniform = profile.UniformColor;
            bool useUniform = profile.ColorMode == DebrisColorMode.Uniform;

            // Random starting offset so a capped burst doesn't always pick the same cubes.
            for (int i = stride > 1 ? Random.Range(0, stride) : 0; i < shape.Count; i += stride)
            {
                Vector3 world = model.TransformPoint(shape.Positions[i]);
                Vector3 fromCenter = world - center;
                Vector3 outward = fromCenter.sqrMagnitude > 1e-6f ? fromCenter.normalized : Random.onUnitSphere;

                _emit.position = world;
                _emit.velocity = outward * Range(profile.BurstSpeed)
                    + push * Range(profile.PushSpeed)
                    + Vector3.up * Range(profile.UpSpeed)
                    + towardCamera * Range(profile.TowardCameraSpeed)
                    + Random.insideUnitSphere * 0.5f;
                _emit.startColor = useUniform ? uniform : shape.Colors[i];
                _emit.startSize = size;
                _emit.startLifetime = Range(profile.Lifetime);
                _emit.rotation3D = startRotation;
                _emit.angularVelocity3D = Random.insideUnitSphere * profile.MaxSpinDegreesPerSecond;
                _system.Emit(_emit, 1);
            }
        }

        private void ConfigureMain()
        {
            var main = _system.main;
            main.playOnAwake = false;
            main.loop = true;                       // stays "playing" so emitted pieces keep simulating
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.gravityModifier = _gravityMultiplier;
            main.maxParticles = _maxParticles;
            main.startRotation3D = true;

            var emission = _system.emission;
            emission.enabled = false;               // pieces only come from Emit()
            var shape = _system.shape;
            shape.enabled = false;
        }

        private void ConfigureMotion()
        {
            var limit = _system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 1000f;                    // no speed cap, only air drag
            limit.dampen = 0f;
            limit.drag = _drag;

            var size = _system.sizeOverLifetime;
            size.enabled = true;                    // full size, then shrink away at the end
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0f)));
        }

        private void ConfigureFloor(float height)
        {
            var floor = new GameObject("Debris Floor").transform;
            floor.SetParent(transform, false);
            floor.SetPositionAndRotation(new Vector3(0f, height, 0f), Quaternion.identity);   // plane normal = up

            var collision = _system.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.Planes;
            collision.SetPlane(0, floor);
            collision.bounce = _bounce;
            collision.dampen = _bounceDamping;
            collision.lifetimeLoss = 0f;
        }

        private void ConfigureRenderer(ParticleSystemRenderer renderer)
        {
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = _mesh != null ? _mesh : Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            renderer.alignment = ParticleSystemRenderSpace.World;   // each cube keeps its own world rotation
            renderer.enableGPUInstancing = true;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = _material != null ? _material : CreateFallbackMaterial();
        }

        private static float Range(Vector2 range) => Random.Range(range.x, range.y);

        private Material CreateFallbackMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Lit");
            if (shader == null)
            {
                Debug.LogError("VoxelDebris requires a material (URP Particles/Lit was not found).", this);
                return null;
            }
            var material = new Material(shader) { name = "Voxel Debris (runtime)", enableInstancing = true };
            material.SetFloat("_Metallic", 0.6f);
            material.SetFloat("_Smoothness", 0.5f);
            return material;
        }
    }
}
