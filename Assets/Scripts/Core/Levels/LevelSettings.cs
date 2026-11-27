using System.Collections.Generic;

namespace CylinderShooter.Core
{
    /// <summary>What has to happen for a stage to end by itself.</summary>
    public enum StageEndMode
    {
        /// <summary>The duration has passed, whatever is still alive.</summary>
        TimeUp,
        /// <summary>At least one enemy appeared, and none are left. The director stops buying after the duration.</summary>
        AllCleared,
        /// <summary>The duration has passed, and no enemies are left. The director stops buying after the duration.</summary>
        TimeUpThenCleared
    }

    /// <summary>Why a stage, wave or run ended, so rewards and statistics never count a skipped stage as beaten.</summary>
    public enum EndReason
    {
        /// <summary>Ended by its own rule, as designed.</summary>
        Completed,
        /// <summary>Reached its maximum duration while still waiting for enemies to be cleared.</summary>
        TimedOut,
        /// <summary>Skipped, jumped out of or restarted (debugging).</summary>
        Skipped,
        /// <summary>The player lost.</summary>
        Failed
    }

    /// <summary>Which side of the player groups may come from, relative to where the ship is heading.</summary>
    public enum SpawnSides
    {
        Either,
        Ahead,
        Behind
    }

    /// <summary>
    /// One stage as the designer tunes it: how long it lasts, how much pressure the director may put on the player
    /// (credits), how it spends them, and which enemies it may use. Read every tick, so changes apply while playing.
    /// </summary>
    public interface IStageSettings
    {
        string DisplayName { get; }
        StageEndMode EndMode { get; }

        /// <summary>Seconds of spawning. Time Up ends here; the cleared modes stop buying here and wait.</summary>
        float Duration { get; }

        /// <summary>Safety limit in seconds for stages that wait for a clear, so they can never hang the level.</summary>
        float MaxDuration { get; }

        /// <summary>Credits the stage opens with.</summary>
        float StartingCredits { get; }

        /// <summary>Credits earned per second: how fast the director saves.</summary>
        float CreditsPerSecond { get; }

        /// <summary>The most credits the director can hold. 0 = no limit.</summary>
        float CreditCap { get; }

        /// <summary>0 = small groups bought as soon as possible (a trickle), 1 = big groups saved up for (calm, then a burst).</summary>
        float GroupSize { get; }

        /// <summary>Seconds the director saves for one plan before it buys a smaller group or picks again.</summary>
        float Patience { get; }

        /// <summary>Minimum seconds between two groups.</summary>
        float Cooldown { get; }

        /// <summary>Most enemies alive at once during this stage, all types together. 0 = only the level's limit.</summary>
        int MaxAlive { get; }

        /// <summary>The enemies this stage may use. An enemy missing here, or switched off, can't spawn.</summary>
        IReadOnlyList<IStageEnemy> Enemies { get; }

        SpawnSides Sides { get; }

        /// <summary>0 to 1: chance that a group is sent into the height lane the player uses most.</summary>
        float LaneFocus { get; }

        /// <summary>Allow groups to come at the player's lane from both sides at once.</summary>
        bool AllowPincer { get; }
    }

    /// <summary>
    /// One wave. Each wave type creates its own runner, so a new type (for example a boss) is added without changing
    /// the level or the existing types.
    /// </summary>
    public interface IWaveSettings
    {
        string DisplayName { get; }
        IWaveRunner CreateRunner(IBattlefield battlefield);
    }

    /// <summary>A wave made of stages played in order.</summary>
    public interface IStagedWaveSettings
    {
        string DisplayName { get; }
        IReadOnlyList<IStageSettings> Stages { get; }
    }

    /// <summary>A whole level: waves played in order.</summary>
    public interface ILevelSettings
    {
        string DisplayName { get; }
        IReadOnlyList<IWaveSettings> Waves { get; }
    }

    /// <summary>
    /// The level-wide rules of the spawn director: costs, variety, how player behaviour is measured, and the hard
    /// limit on enemies alive. Stages say how much and what; these say how the director thinks.
    /// </summary>
    public interface IDirectorSettings
    {
        /// <summary>Most enemies alive at once in the level. A performance limit; never ignored.</summary>
        int MaxEnemiesAlive { get; }

        /// <summary>What one enemy of each type costs in this level.</summary>
        ICostTable Costs { get; }

        /// <summary>0 to 1: an enemy's or formation's chance is multiplied by this right after it is picked.</summary>
        float VarietyDip { get; }

        /// <summary>Seconds for a picked enemy's or formation's chance to recover fully.</summary>
        float VarietyRecovery { get; }

        /// <summary>0 to 1: how far a group's size may stray from the stage's Group Size, at random.</summary>
        float GroupSizeSpread { get; }

        /// <summary>Seconds of the player's lane history that count (older time fades).</summary>
        float LaneMemory { get; }

        /// <summary>Seconds a lane keeps the side its last group came from, when pincers aren't allowed.</summary>
        float PincerWindow { get; }

        /// <summary>Recent kills that count as a hot streak. 0 = streaks are off.</summary>
        float StreakKills { get; }

        /// <summary>Seconds over which kills fade from the streak.</summary>
        float StreakWindow { get; }

        /// <summary>Credits per second are multiplied by this while the player is on a streak.</summary>
        float StreakBonus { get; }
    }

    /// <summary>What the runners need to know about the fight, without knowing enemies or GameObjects.</summary>
    public interface IBattlefield
    {
        int AliveEnemies { get; }

        /// <summary>Removes every enemy without counting as kills (no debris, score or rewards).</summary>
        void RemoveAllEnemies();
    }
}
