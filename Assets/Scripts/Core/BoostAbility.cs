using System;

namespace CylinderShooter.Core
{
    /// <summary>Boost state machine: Ready -> Active -> Cooldown -> Ready.</summary>
    public sealed class BoostAbility
    {
        readonly IShipTuning _tuning;
        float _activeLeft;
        float _cooldownLeft;

        public BoostAbility(IShipTuning tuning) => _tuning = tuning;

        public event Action<bool> ActiveChanged;

        public bool IsActive => _activeLeft > 0f;
        public bool IsReady => !IsActive && _cooldownLeft <= 0f;
        public float CooldownRemaining => _cooldownLeft;

        public bool TryActivate()
        {
            if (!IsReady) return false;
            _activeLeft = _tuning.BoostDuration;
            ActiveChanged?.Invoke(true);
            return true;
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
                    ActiveChanged?.Invoke(false);
                }
            }
            else if (_cooldownLeft > 0f)
            {
                _cooldownLeft = Math.Max(0f, _cooldownLeft - dt);
            }
        }
    }
}
