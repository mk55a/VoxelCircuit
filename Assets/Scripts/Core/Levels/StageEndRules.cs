using System;

namespace CylinderShooter.Core
{
    /// <summary>Decides when a stage ends by itself. One class per rule; rules hold no state, so they are shared.</summary>
    public interface IStageEndRule
    {
        /// <summary>True when the rule waits for the arena to be empty.</summary>
        bool RequiresClear { get; }

        bool IsMet(float elapsed, float duration, int aliveEnemies, bool sawEnemies);
    }

    public sealed class TimeUpRule : IStageEndRule
    {
        public bool RequiresClear => false;

        public bool IsMet(float elapsed, float duration, int aliveEnemies, bool sawEnemies) => elapsed >= duration;
    }

    /// <summary>Needs at least one enemy to have appeared; otherwise the stage would end before anything spawned.</summary>
    public sealed class AllClearedRule : IStageEndRule
    {
        public bool RequiresClear => true;

        public bool IsMet(float elapsed, float duration, int aliveEnemies, bool sawEnemies) =>
            sawEnemies && aliveEnemies == 0;
    }

    public sealed class TimeUpThenClearedRule : IStageEndRule
    {
        public bool RequiresClear => true;

        public bool IsMet(float elapsed, float duration, int aliveEnemies, bool sawEnemies) =>
            elapsed >= duration && aliveEnemies == 0;
    }

    public static class StageEndRules
    {
        private static readonly IStageEndRule TimeUp = new TimeUpRule();
        private static readonly IStageEndRule AllCleared = new AllClearedRule();
        private static readonly IStageEndRule TimeUpThenCleared = new TimeUpThenClearedRule();

        public static IStageEndRule For(StageEndMode mode)
        {
            switch (mode)
            {
                case StageEndMode.TimeUp: return TimeUp;
                case StageEndMode.AllCleared: return AllCleared;
                case StageEndMode.TimeUpThenCleared: return TimeUpThenCleared;
                default: throw new ArgumentOutOfRangeException(nameof(mode), mode, "No end rule for this mode.");
            }
        }
    }
}
