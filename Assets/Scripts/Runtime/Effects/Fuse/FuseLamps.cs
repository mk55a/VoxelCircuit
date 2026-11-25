using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Effects
{
    /// <summary>
    /// Blinks an enemy's lamps while its fuse counts down, by setting the material's emission colour through a
    /// MaterialPropertyBlock (no material copies). Works with anything that implements <see cref="IFuseSource"/>.
    /// Only writes to the renderer when the colour actually changes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FuseLamps : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Tooltip("The component with the fuse (for example the Bomber on this object). Drag the object in; the right component is picked.")]
        [SerializeField] private MonoBehaviour _fuseSource;
        [Tooltip("The renderer whose material has the lamp emission map (the model child).")]
        [SerializeField] private Renderer _renderer;
        [SerializeField] private FuseLampProfile _profile;

        private IFuseSource _fuse;
        private MaterialPropertyBlock _block;
        private Color _applied;
        private bool _hasApplied;

        private void OnValidate()
        {
            if (_fuseSource != null && !(_fuseSource is IFuseSource))
                _fuseSource = _fuseSource.GetComponent<IFuseSource>() as MonoBehaviour;
        }

        private void Awake()
        {
            _fuse = _fuseSource as IFuseSource;
            if (_fuse == null || _renderer == null || _profile == null)
            {
                Debug.LogError("FuseLamps needs 'Fuse Source' (the Bomber), 'Renderer' (the model's MeshRenderer) and " +
                               "'Profile' (a FuseLampProfile, e.g. Assets/Tuning/Enemies/Bomber_Lamps).", this);
                enabled = false;
                return;
            }
            _block = new MaterialPropertyBlock();
        }

        private void OnEnable() => _hasApplied = false;

        private void LateUpdate()
        {
            Color colour = !_fuse.IsArmed ? _profile.Idle
                : _fuse.BlinkLevel > 0.5f ? _profile.ArmedOn : _profile.ArmedOff;
            if (_hasApplied && colour == _applied) return;
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(EmissionColorId, colour);
            _renderer.SetPropertyBlock(_block);
            _applied = colour;
            _hasApplied = true;
        }
    }
}
