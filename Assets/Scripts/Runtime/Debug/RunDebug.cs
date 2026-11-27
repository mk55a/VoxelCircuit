#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using CylinderShooter.Core;

namespace CylinderShooter.Debugging
{
    /// <summary>
    /// Turns debug commands into run commands. Skip leaves with no rewards (ends as Skipped); Finish ends as if the
    /// player had done it (ends as Completed), so what happens at the end can be tested.
    /// </summary>
    public sealed class RunDebug : IRunDebug
    {
        private readonly IBattlefield _battlefield;
        private const string NoSpawner = "this scene has no Enemy Spawner (Cylinder Shooter > Director > Add Spawning To Scene)";

        public RunDebug(LevelRunner run, IBattlefield battlefield, SpawnDirector director = null)
        {
            Director = director;
            Run = run ?? throw new ArgumentNullException(nameof(run));
            _battlefield = battlefield ?? throw new ArgumentNullException(nameof(battlefield));
        }

        public LevelRunner Run { get; }
        public SpawnDirector Director { get; }
        public int AliveEnemies => _battlefield.AliveEnemies;
        public bool IsPaused { get; set; }

        public bool SkipStage() => Run.EndStage(EndReason.Skipped);
        public bool FinishStage() => Run.EndStage(EndReason.Completed);
        public bool SkipWave() => Run.EndWave(EndReason.Skipped);
        public bool FinishWave() => Run.EndWave(EndReason.Completed);
        public bool FailRun() => Run.EndEarly(EndReason.Failed);
        public bool RestartRun() => Run.Begin();
        public bool JumpTo(int waveIndex, int stageIndex) => Run.JumpTo(waveIndex, stageIndex);
        public void ClearEnemies() => _battlefield.RemoveAllEnemies();
        public string SpawnGroup(IEnemyType enemy, IFormationOption formation, int count, bool ignoreStageList) =>
            Director == null ? NoSpawner : Director.SpawnGroup(enemy, formation, count, ignoreStageList);

        public bool RestartStage() =>
            Run.State == RunState.Running && Run.CurrentWave != null &&
            Run.JumpTo(Run.WaveIndex, Math.Max(0, Run.CurrentWave.StageIndex));
    }
}
#endif
