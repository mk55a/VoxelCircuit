using UnityEngine;

namespace CylinderShooter.Core
{
    public enum FuseState
    {
        /// <summary>Waiting for the player to come within trigger range.</summary>
        Idle,
        /// <summary>Counting down; the lamp blinks faster and faster.</summary>
        Armed,
        /// <summary>Exploded (or cleared). Stays here until <see cref="ProximityFuse.Reset"/>.</summary>
        Detonated
    }

    /// <summary>
    /// The timing half of an exploding enemy: arms when the player comes close, counts down, then reports the
    /// explosion. Once armed it always goes off, even if the player moves away, so arming is a commitment the
    /// player can read and react to. It knows nothing about damage or effects; the component decides what the
    /// explosion does.
    /// </summary>
    public sealed class ProximityFuse
    {
        private readonly IFuseSettings _settings;
        private float _armedTime;
        private float _blinkPhase;

        public ProximityFuse(IFuseSettings settings)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
        }

        public FuseState State { get; private set; }
        public bool IsArmed => State == FuseState.Armed;

        /// <summary>0 when the fuse arms, 1 at the explosion. 0 while idle.</summary>
        public float Progress => State == FuseState.Armed && _settings.FuseDuration > 0f
            ? Mathf.Clamp01(_armedTime / _settings.FuseDuration)
            : State == FuseState.Detonated ? 1f : 0f;

        /// <summary>1 while the lamp is lit in its blink, 0 while dark. Always 0 when not armed.</summary>
        public float BlinkLevel => State == FuseState.Armed && Mathf.Repeat(_blinkPhase, 1f) < 0.5f ? 1f : 0f;

        /// <summary>Back to idle, ready for the next spawn.</summary>
        public void Reset()
        {
            State = FuseState.Idle;
            _armedTime = 0f;
            _blinkPhase = 0f;
        }

        /// <summary>Starts the countdown now, whatever the distance (for example when shot while idle, if a design wants that).</summary>
        public void Arm()
        {
            if (State != FuseState.Idle) return;
            State = FuseState.Armed;
            _armedTime = 0f;
            _blinkPhase = 0f;
        }

        /// <summary>Marks the fuse as spent without counting down (it exploded early, or was cleared).</summary>
        public void MarkDetonated() => State = FuseState.Detonated;

        /// <summary>
        /// Advances the fuse. Returns true on the one call where the countdown finishes and the mine should explode.
        /// </summary>
        /// <param name="distanceToPlayer">Surface distance to the player, in metres.</param>
        /// <param name="playerTargetable">False while the player is respawning: an idle fuse doesn't arm then.</param>
        public bool Tick(float deltaTime, float distanceToPlayer, bool playerTargetable)
        {
            if (State == FuseState.Idle)
            {
                if (!playerTargetable || distanceToPlayer > _settings.TriggerRange) return false;
                Arm();
            }
            if (State != FuseState.Armed) return false;

            _armedTime += Mathf.Max(0f, deltaTime);
            float rate = Mathf.Lerp(_settings.BlinkRateStart, _settings.BlinkRateEnd, Progress);
            _blinkPhase += rate * Mathf.Max(0f, deltaTime);
            if (_armedTime < _settings.FuseDuration) return false;
            State = FuseState.Detonated;
            return true;
        }
    }
}
