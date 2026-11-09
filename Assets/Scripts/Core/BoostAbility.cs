using System;

namespace CylinderShooter.Core
{
    /// <summary>Boost state machine: Ready -> Active -> Cooldown -> Ready.</summary>
    public sealed class BoostAbility
    {
        readonly IShipTuning _tuning;
        float _activeLeft;
        float _activeDuration;
        float _cooldownLeft;

        public BoostAbility(IShipTuning tuning) => _tuning = tuning;

        public event Action<bool> OnActiveChanged;

        public bool IsActive => _activeLeft > 0f;
        public bool IsReady => !IsActive && _cooldownLeft <= 0f;
        public float CooldownRemaining => _cooldownLeft;
        public float ActiveProgress => IsActive && _activeDuration > 0f
            ? Math.Max(0f, Math.Min(1f, 1f - _activeLeft / _activeDuration))
            : 0f;

        public bool TryActivate()
        {
            if (!IsReady) return false;
            _activeDuration = _tuning.BoostDuration;
            _activeLeft = _activeDuration;
            OnActiveChanged?.Invoke(true);
            return true;
        }

        public void Interrupt()
        {
            if (!IsActive) return;
            _activeLeft = 0f;
            _cooldownLeft = _tuning.BoostCooldown;
            OnActiveChanged?.Invoke(false);
        }

        public void Tick(float dt)
        {
            if (IsActive)
            {
                _activeLeft -= dt;
                if (_activeLeft <= 0f)
                {
                    _activeLeft = 0f;
                    _cooldownLeft = _tuning.BoostCooldown;
                    OnActiveChanged?.Invoke(false);
                }
            }
            else if (_cooldownLeft > 0f)
            {
                _cooldownLeft = Math.Max(0f, _cooldownLeft - dt);
            }
        }
    }
}
