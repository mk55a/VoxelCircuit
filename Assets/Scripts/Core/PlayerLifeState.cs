using System;

namespace CylinderShooter.Core
{
    public enum PlayerStatus
    {
        Alive,
        Respawning,
        Invulnerable
    }

    public sealed class PlayerLifeState
    {
        private readonly float _respawnDelay;
        private readonly float _invulnerabilityDuration;
        private float _remaining;

        public PlayerLifeState(int startingLives, float respawnDelay, float invulnerabilityDuration)
        {
            if (startingLives < 0 || respawnDelay < 0f || invulnerabilityDuration < 0f
                || float.IsNaN(respawnDelay) || float.IsInfinity(respawnDelay)
                || float.IsNaN(invulnerabilityDuration) || float.IsInfinity(invulnerabilityDuration))
            {
                throw new ArgumentOutOfRangeException(nameof(startingLives), "Lives and durations must be finite and non-negative.");
            }

            Lives = startingLives;
            _respawnDelay = respawnDelay;
            _invulnerabilityDuration = invulnerabilityDuration;
        }

        public event Action<int> OnLivesChanged;
        public event Action<PlayerStatus> OnStatusChanged;

        public int Lives { get; private set; }
        public PlayerStatus Status { get; private set; }

        public void AddLives(int count)
        {
            if (count < 0 || count > int.MaxValue - Lives)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            SetLives(Lives + count);
        }

        public void RemoveLives(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            SetLives(Math.Max(0, Lives - count));
        }

        public bool TryDestroy()
        {
            if (Status != PlayerStatus.Alive)
            {
                return false;
            }

            RemoveLives(1);
            _remaining = _respawnDelay;
            SetStatus(PlayerStatus.Respawning);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }
            if (Status == PlayerStatus.Alive)
            {
                return;
            }

            _remaining -= deltaTime;
            if (_remaining > 0f)
            {
                return;
            }
            if (Status == PlayerStatus.Respawning)
            {
                _remaining = _invulnerabilityDuration;
                SetStatus(_remaining > 0f ? PlayerStatus.Invulnerable : PlayerStatus.Alive);
                return;
            }
            SetStatus(PlayerStatus.Alive);
        }

        private void SetLives(int value)
        {
            if (Lives == value)
            {
                return;
            }
            Lives = value;
            OnLivesChanged?.Invoke(value);
        }

        private void SetStatus(PlayerStatus value)
        {
            Status = value;
            OnStatusChanged?.Invoke(value);
        }
    }
}
