using CylinderShooter.Combat;
using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Ship
{
    /// <summary>
    /// The player's special move: on input, releases a shockwave from the ship's position.
    /// The emitter is the scene's IShockwaveEmitter, registered by GameInstaller. The ship is untouchable while the
    /// wave builds up, so pressing it just before a collision always saves the player.
    /// For now it is always available (test mode); the build-up meter will gate it here later.
    /// </summary>
    public sealed class PlayerShockwave : MonoBehaviour
    {
        [SerializeField] private ShipController _ship;
        [Tooltip("Component that implements IShockwaveInput, e.g. InputSystemShipInput.")]
        [SerializeField] private MonoBehaviour _input;
        [SerializeField] private PlayerLife _life;
        [Tooltip("Seconds the ship cannot be hit after triggering. Cover at least the profile's build-up time.")]
        [SerializeField, Min(0f)] private float _invulnerableSeconds = 0.6f;

        private IShockwaveInput _shockwaveInput;
        private IShockwaveEmitter _emitter;

        private void Awake()
        {
            _shockwaveInput = _input as IShockwaveInput;
            if (_ship == null || _shockwaveInput == null)
            {
                Debug.LogError("PlayerShockwave requires a ship and an IShockwaveInput.", this);
                enabled = false;
                return;
            }
            if (!ServiceLocator.TryGet(out _emitter))
            {
                Debug.LogError("PlayerShockwave found no IShockwaveEmitter. Assign the Shockwave object on the GameInstaller.", this);
                enabled = false;
            }
        }

        // Dragging a GameObject into a MonoBehaviour slot picks its first script; swap in the one that fits.
        private void OnValidate()
        {
            if (_input != null && !(_input is IShockwaveInput) && _input.TryGetComponent(out IShockwaveInput input))
                _input = input as MonoBehaviour;
        }

        private void Update()
        {
            if (!_shockwaveInput.ConsumeShockwave()) return;
            if (_life != null && !_life.CanControl) return;
            if (_emitter.TryTrigger(_ship.Position) && _life != null) _life.GrantInvulnerability(_invulnerableSeconds);
        }
    }
}
