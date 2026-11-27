using System;

namespace CylinderShooter.Core
{
    /// <summary>The standard wave: plays its stages in order and ends after the last one.</summary>
    public sealed class StagedWaveRunner : IWaveRunner
    {
        private readonly IStageSettings[] _stages;
        private readonly IBattlefield _battlefield;

        public StagedWaveRunner(IStagedWaveSettings settings, IBattlefield battlefield)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _battlefield = battlefield ?? throw new ArgumentNullException(nameof(battlefield));
            if (settings.Stages.Count == 0)
                throw new ArgumentException($"Wave '{settings.DisplayName}' has no stages.", nameof(settings));
            // Copied, so adding or reordering stages while playing takes effect the next time the wave starts.
            _stages = new IStageSettings[settings.Stages.Count];
            for (int i = 0; i < _stages.Length; i++) _stages[i] = settings.Stages[i];
        }

        public event Action<int, IStageSettings> StageStarted;
        public event Action<int, IStageSettings, EndReason> StageEnded;

        public bool IsComplete { get; private set; }
        public EndReason EndReason { get; private set; }
        public int StageCount => _stages.Length;
        public int StageIndex { get; private set; } = -1;
        public StageRunner CurrentStage { get; private set; }

        public void Begin(int stageIndex)
        {
            if (stageIndex < 0 || stageIndex >= _stages.Length)
                throw new ArgumentOutOfRangeException(nameof(stageIndex));
            IsComplete = false;
            StartStage(stageIndex);
        }

        public float Tick(float deltaTime)
        {
            float remaining = deltaTime;
            while (!IsComplete && CurrentStage != null)
            {
                remaining = CurrentStage.Tick(remaining);
                if (!CurrentStage.IsComplete) return 0f;
                CompleteCurrentStage();
                if (remaining <= 0f) return 0f;
            }
            return remaining;
        }

        public void EndStage(EndReason reason)
        {
            if (IsComplete || CurrentStage == null) return;
            CurrentStage.End(reason);
            CompleteCurrentStage();
        }

        public void JumpToStage(int stageIndex)
        {
            if (stageIndex < 0 || stageIndex >= _stages.Length)
                throw new ArgumentOutOfRangeException(nameof(stageIndex));
            LeaveCurrentStage(EndReason.Skipped);
            IsComplete = false;
            StartStage(stageIndex);
        }

        public void End(EndReason reason)
        {
            if (IsComplete) return;
            LeaveCurrentStage(reason);
            IsComplete = true;
            EndReason = reason;
        }

        private void StartStage(int index)
        {
            StageIndex = index;
            CurrentStage = new StageRunner(_stages[index], _battlefield);
            StageStarted?.Invoke(index, _stages[index]);
        }

        private void CompleteCurrentStage()
        {
            var reason = CurrentStage.EndReason;
            int index = StageIndex;
            LeaveCurrentStage(reason);
            if (index + 1 < _stages.Length)
            {
                StartStage(index + 1);
                return;
            }
            IsComplete = true;
            // Timing out of the last stage still finishes the wave; being skipped out of it doesn't count as beating it.
            EndReason = reason == EndReason.TimedOut ? EndReason.Completed : reason;
        }

        private void LeaveCurrentStage(EndReason reason)
        {
            var stage = CurrentStage;
            if (stage == null) return;
            stage.End(reason);
            CurrentStage = null;
            StageEnded?.Invoke(StageIndex, stage.Settings, stage.EndReason);
        }
    }
}
