using System;
using System.Collections.Generic;

namespace CylinderShooter.Core
{
    public enum RunState
    {
        NotStarted,
        Running,
        Ended
    }

    /// <summary>
    /// Plays a level: its waves in order, and the events other systems react to. Can be skipped, finished, jumped and
    /// failed at any point. Commands sent from inside one of its own events are refused, so a listener can't change the
    /// run half-way through a change.
    /// </summary>
    public sealed class LevelRunner : IRunEvents
    {
        private readonly IWaveSettings[] _waves;
        private readonly IBattlefield _battlefield;
        private bool _busy;

        public LevelRunner(ILevelSettings settings, IBattlefield battlefield)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _battlefield = battlefield ?? throw new ArgumentNullException(nameof(battlefield));
            if (settings.Waves.Count == 0)
                throw new ArgumentException($"Level '{settings.DisplayName}' has no waves.", nameof(settings));
            Name = settings.DisplayName;
            _waves = new IWaveSettings[settings.Waves.Count];
            for (int i = 0; i < _waves.Length; i++) _waves[i] = settings.Waves[i];
        }

        public event Action RunStarted;
        public event Action<WaveInfo> WaveStarted;
        public event Action<StageInfo> StageStarted;
        public event Action<StageInfo, EndReason> StageEnded;
        public event Action<WaveInfo, EndReason> WaveEnded;
        public event Action<EndReason> RunEnded;

        public string Name { get; }
        public RunState State { get; private set; }

        /// <summary>Why the run ended. Only meaningful once <see cref="State"/> is Ended.</summary>
        public EndReason EndReason { get; private set; }

        /// <summary>True once any part of this run was skipped or jumped, so its result isn't a real playthrough.</summary>
        public bool WasSkipped { get; private set; }

        /// <summary>Seconds played since the run started (jumping doesn't add the time jumped over).</summary>
        public float RunTime { get; private set; }

        public IReadOnlyList<IWaveSettings> Waves => _waves;
        public int WaveCount => _waves.Length;
        public int WaveIndex { get; private set; } = -1;
        public string WaveName => WaveIndex >= 0 ? _waves[WaveIndex].DisplayName : string.Empty;
        public IWaveRunner CurrentWave { get; private set; }

        /// <summary>Settings of the wave being played, or null when none is.</summary>
        public IWaveSettings CurrentWaveSettings => CurrentWave != null ? _waves[WaveIndex] : null;
        public StageRunner CurrentStage => CurrentWave?.CurrentStage;

        /// <summary>Starts (or restarts) the level from the first wave.</summary>
        public bool Begin()
        {
            if (_busy) return false;
            _busy = true;
            try
            {
                if (State == RunState.Running)
                {
                    _battlefield.RemoveAllEnemies();
                    EndRun(EndReason.Skipped);
                }
                StartRun();
                StartWave(0, 0);
                return true;
            }
            finally { _busy = false; }
        }

        public void Tick(float deltaTime)
        {
            if (!CanCommand || deltaTime <= 0f) return;
            _busy = true;
            try
            {
                RunTime += deltaTime;
                float remaining = deltaTime;
                while (State == RunState.Running)
                {
                    remaining = CurrentWave.Tick(remaining);
                    if (!CurrentWave.IsComplete) return;
                    AdvanceWave();
                    if (remaining <= 0f) return;
                }
            }
            finally { _busy = false; }
        }

        /// <summary>
        /// Ends the current stage and moves on. Skipping removes the enemies. Completing ends it as if the player had
        /// done it, so enemies are removed only when the stage's rule needs them gone.
        /// </summary>
        public bool EndStage(EndReason reason)
        {
            if (!CanCommand) return false;
            _busy = true;
            try
            {
                NoteSkip(reason);
                if (reason != EndReason.Completed || CurrentStage == null || CurrentStage.RequiresClear)
                    _battlefield.RemoveAllEnemies();
                if (CurrentStage == null) CurrentWave.End(reason);
                else CurrentWave.EndStage(reason);
                if (CurrentWave.IsComplete) AdvanceWave();
                return true;
            }
            finally { _busy = false; }
        }

        /// <summary>Ends the current wave and moves to the next. Enemies are removed either way.</summary>
        public bool EndWave(EndReason reason)
        {
            if (!CanCommand) return false;
            _busy = true;
            try
            {
                NoteSkip(reason);
                _battlefield.RemoveAllEnemies();
                CurrentWave.End(reason);
                AdvanceWave();
                return true;
            }
            finally { _busy = false; }
        }

        /// <summary>Goes straight to a stage. What was being played ends as skipped. Restarts a run that has ended.</summary>
        public bool JumpTo(int waveIndex, int stageIndex)
        {
            if (_busy || waveIndex < 0 || waveIndex >= WaveCount || stageIndex < 0) return false;
            _busy = true;
            try
            {
                _battlefield.RemoveAllEnemies();
                if (State == RunState.Running && waveIndex == WaveIndex && stageIndex < CurrentWave.StageCount)
                {
                    WasSkipped = true;
                    CurrentWave.JumpToStage(stageIndex);
                    return true;
                }
                if (State == RunState.Running) LeaveWave(EndReason.Skipped);
                else StartRun();
                WasSkipped = true;
                StartWave(waveIndex, stageIndex);
                return true;
            }
            finally { _busy = false; }
        }

        /// <summary>Ends the run early, for example because the player lost.</summary>
        public bool EndEarly(EndReason reason)
        {
            if (!CanCommand) return false;
            _busy = true;
            try
            {
                NoteSkip(reason);
                EndRun(reason);
                return true;
            }
            finally { _busy = false; }
        }

        private bool CanCommand => State == RunState.Running && !_busy && CurrentWave != null;

        private void NoteSkip(EndReason reason)
        {
            if (reason == EndReason.Skipped) WasSkipped = true;
        }

        private void StartRun()
        {
            State = RunState.Running;
            RunTime = 0f;
            WasSkipped = false;
            RunStarted?.Invoke();
        }

        private void StartWave(int index, int stageIndex)
        {
            WaveIndex = index;
            CurrentWave = _waves[index].CreateRunner(_battlefield);
            CurrentWave.StageStarted += HandleStageStarted;
            CurrentWave.StageEnded += HandleStageEnded;
            WaveStarted?.Invoke(new WaveInfo(index, _waves[index].DisplayName));
            int last = Math.Max(0, CurrentWave.StageCount - 1);
            CurrentWave.Begin(Math.Min(stageIndex, last));
        }

        private void AdvanceWave()
        {
            var reason = CurrentWave.EndReason;
            int next = WaveIndex + 1;
            LeaveWave(reason);
            if (next < WaveCount) StartWave(next, 0);
            else FinishRun(reason);
        }

        private void LeaveWave(EndReason reason)
        {
            var wave = CurrentWave;
            if (wave == null) return;
            wave.End(reason);
            wave.StageStarted -= HandleStageStarted;
            wave.StageEnded -= HandleStageEnded;
            CurrentWave = null;
            WaveEnded?.Invoke(new WaveInfo(WaveIndex, _waves[WaveIndex].DisplayName), wave.EndReason);
        }

        private void EndRun(EndReason reason)
        {
            LeaveWave(reason);
            FinishRun(reason);
        }

        private void FinishRun(EndReason reason)
        {
            State = RunState.Ended;
            EndReason = reason;
            RunEnded?.Invoke(reason);
        }

        private void HandleStageStarted(int index, IStageSettings stage) =>
            StageStarted?.Invoke(new StageInfo(WaveIndex, index, stage.DisplayName));

        private void HandleStageEnded(int index, IStageSettings stage, EndReason reason) =>
            StageEnded?.Invoke(new StageInfo(WaveIndex, index, stage.DisplayName), reason);
    }
}
