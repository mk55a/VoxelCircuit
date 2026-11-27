using System;
using UnityEngine;

namespace CylinderShooter.Core
{
    /// <summary>
    /// Plays one stage: its time and when it ends. Never reads Unity's clock; it is given the time,
    /// so the same code runs in the game, in tests and in a simulator.
    /// </summary>
    public sealed class StageRunner
    {
        private readonly IBattlefield _battlefield;
        private bool _sawEnemies;

        public StageRunner(IStageSettings settings, IBattlefield battlefield)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _battlefield = battlefield ?? throw new ArgumentNullException(nameof(battlefield));
        }

        public IStageSettings Settings { get; }
        public float Elapsed { get; private set; }
        public bool IsComplete { get; private set; }
        public EndReason EndReason { get; private set; }

        /// <summary>0 at the start, 1 at the designed end. Stays at 1 while waiting for a clear.</summary>
        public float Progress => Settings.Duration > 0f ? Mathf.Clamp01(Elapsed / Settings.Duration) : 1f;

        public bool RequiresClear => StageEndRules.For(Settings.EndMode).RequiresClear;

        /// <summary>
        /// Advances the stage. Returns the time left over when the stage ended part-way through this tick, so the
        /// next stage starts at the right moment even when a tick is long.
        /// </summary>
        public float Tick(float deltaTime)
        {
            if (IsComplete) return deltaTime;
            Elapsed += Mathf.Max(0f, deltaTime);
            int alive = _battlefield.AliveEnemies;
            if (alive > 0) _sawEnemies = true;

            var rule = StageEndRules.For(Settings.EndMode);
            if (rule.IsMet(Elapsed, Settings.Duration, alive, _sawEnemies))
            {
                End(EndReason.Completed);
                return rule.RequiresClear ? 0f : Mathf.Clamp(Elapsed - Settings.Duration, 0f, Mathf.Max(0f, deltaTime));
            }
            if (rule.RequiresClear && Elapsed >= Mathf.Max(Settings.MaxDuration, Settings.Duration))
                End(EndReason.TimedOut);
            return 0f;
        }

        /// <summary>Ends the stage now. Does nothing if it has already ended.</summary>
        public void End(EndReason reason)
        {
            if (IsComplete) return;
            IsComplete = true;
            EndReason = reason;
        }
    }
}
