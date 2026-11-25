using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Effects
{
    /// <summary>
    /// Add next to any <see cref="IBlastSource"/>. When it explodes, plays a pulse sized to the blast radius, so the
    /// ring the player sees is the zone that hurt them. Reuses the shared pulse emitter (no extra particle systems).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PulseOnDetonated : MonoBehaviour
    {
        [Tooltip("How the blast looks (e.g. Assets/Tuning/Effects/Pulse_Blast). Ring Size 1 = the ring reaches the blast radius.")]
        [SerializeField] private PulseProfile _profile;

        private IBlastSource _source;

        private void Awake()
        {
            _source = GetComponent<IBlastSource>();
            if (_source == null || _profile == null)
            {
                Debug.LogError("PulseOnDetonated needs an exploding component (e.g. Bomber) on the same object and a " +
                               "pulse profile in 'Profile' (e.g. Assets/Tuning/Effects/Pulse_Blast).", this);
                enabled = false;
                return;
            }
            _source.Detonated += HandleDetonated;
        }

        private void OnDestroy()
        {
            if (_source != null) _source.Detonated -= HandleDetonated;
        }

        private void HandleDetonated(BlastEvent blast)
        {
            if (!ServiceLocator.TryGet<IPulseEmitter>(out var pulses)) return;
            pulses.Emit(blast.Position, blast.Radius, _profile);
        }
    }
}
