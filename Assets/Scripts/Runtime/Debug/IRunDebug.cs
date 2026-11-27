#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CylinderShooter.Core;

namespace CylinderShooter.Debugging
{
    /// <summary>
    /// Everything the Level Debugger may do to a run. The window only knows this, never the runners, and it doesn't
    /// exist in release builds. Commands return false when they can't be carried out right now.
    /// </summary>
    public interface IRunDebug
    {
        /// <summary>The run itself, for reading its state and events.</summary>
        LevelRunner Run { get; }

        int AliveEnemies { get; }

        /// <summary>Stops the level's clock without stopping the game.</summary>
        bool IsPaused { get; set; }

        bool SkipStage();
        bool FinishStage();
        bool SkipWave();
        bool FinishWave();
        bool FailRun();
        bool RestartStage();
        bool RestartRun();
        bool JumpTo(int waveIndex, int stageIndex);
        void ClearEnemies();

        /// <summary>The spawn director, for reading credits, the plan and the player readings. Null without a spawner.</summary>
        SpawnDirector Director { get; }

        /// <summary>Spawns a chosen group now through the normal gate. Returns null on success, otherwise why not.</summary>
        string SpawnGroup(IEnemyType enemy, IFormationOption formation, int count, bool ignoreStageList);
    }
}
#endif
