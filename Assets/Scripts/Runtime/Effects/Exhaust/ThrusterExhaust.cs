using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Effects
{
    /// <summary>
    /// Drives an engine exhaust from any <see cref="IThrusterSource"/>: flame length, width and brightness,
    /// and an optional light. All parts are authored in the prefab; this only sets their values each frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThrusterExhaust : MonoBehaviour
    {
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        [Tooltip("Component that implements IThrusterSource, e.g. the ShipController.")]
        [SerializeField] private MonoBehaviour _thrusterSource;
        [SerializeField] private ExhaustProfile _profile;

        [Header("Parts")]
        [Tooltip("Flame mesh with its pivot at the nozzle, pointing back along local -Z. Its X/Y scale is the nozzle size.")]
        [SerializeField] private Transform _flame;
        [SerializeField] private Renderer _flameRenderer;
        [Tooltip("Optional: light that brightens with the exhaust.")]
        [SerializeField] private Light _light;

        private readonly ThrusterLevel _level = new ThrusterLevel();
        private IThrusterSource _source;
        private MaterialPropertyBlock _flameProperties;
        private Vector2 _nozzleSize;

        private void Awake()
        {
            _source = _thrusterSource as IThrusterSource;
            string missing = FindMissingReference();
            if (missing != null)
            {
                Debug.LogError($"ThrusterExhaust on '{name}': {missing}", this);
                enabled = false;
                return;
            }

            _flameProperties = new MaterialPropertyBlock();
            _nozzleSize = new Vector2(_flame.localScale.x, _flame.localScale.y);
            _level.Reset(ThrusterMath.Idle);
            Apply(_profile.Evaluate(_level.Value));
        }

        // Dragging a GameObject into the source slot picks its first script, which may not be the thruster.
        // Swap it for the component on that object that actually implements IThrusterSource.
        private void OnValidate()
        {
            if (_thrusterSource == null || _thrusterSource is IThrusterSource) return;
            if (_thrusterSource.TryGetComponent(out IThrusterSource source)) _thrusterSource = source as MonoBehaviour;
        }

        private string FindMissingReference()
        {
            if (_thrusterSource == null) return "Thruster Source is empty. Assign the ShipController.";
            if (_source == null) return $"Thruster Source '{_thrusterSource.GetType().Name}' does not implement IThrusterSource. Assign the ShipController.";
            if (_profile == null) return "Profile is empty. Assign an Exhaust Profile asset.";
            if (_flame == null) return "Flame is empty. Assign the flame object that gets scaled.";
            if (_flameRenderer == null) return "Flame Renderer is empty. Assign the object with the flame's Mesh Renderer.";
            return null;
        }

        private void LateUpdate()
        {
            float target = ThrusterMath.TargetLevel(_source.Speed, _source.CruiseSpeed, _source.BoostSpeed, _source.IsBoosting);
            _level.Tick(target, _profile.RiseRate, _profile.FallRate, Time.deltaTime);
            Apply(_profile.Evaluate(_level.Value));
        }

        private void Apply(ExhaustState state)
        {
            _flame.localScale = new Vector3(_nozzleSize.x * state.Width, _nozzleSize.y * state.Width, state.Length);

            _flameRenderer.GetPropertyBlock(_flameProperties);
            _flameProperties.SetFloat(IntensityId, state.Brightness);
            _flameRenderer.SetPropertyBlock(_flameProperties);

            if (_light != null) _light.intensity = state.LightIntensity;
        }
    }
}
