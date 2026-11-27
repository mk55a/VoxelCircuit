using System;

namespace CylinderShooter.Core
{
    /// <summary>
    /// What every wave type can do. The level only knows this, so a new wave type never changes the level.
    /// A wave without stages (a boss) reports zero stages and no current stage.
    /// </summary>
    public interface IWaveRunner
    {
        event Action<int, IStageSettings> StageStarted;
        event Action<int, IStageSettings, EndReason> StageEnded;

        bool IsComplete { get; }
        EndReason EndReason { get; }
        int StageCount { get; }
        int StageIndex { get; }

        /// <summary>The stage being played, or null (not started, ended, or a wave without stages).</summary>
        StageRunner CurrentStage { get; }

        void Begin(int stageIndex);

        /// <summary>Advances the wave and returns the time left over if it ended part-way through.</summary>
        float Tick(float deltaTime);

        /// <summary>Ends the current stage and moves to the next one (or ends the wave after the last).</summary>
        void EndStage(EndReason reason);

        /// <summary>Leaves the current stage (as skipped) and starts another one of this wave.</summary>
        void JumpToStage(int stageIndex);

        /// <summary>Ends the whole wave now, including its current stage.</summary>
        void End(EndReason reason);
    }
}
