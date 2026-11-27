using System;
using System.Collections.Generic;

namespace CylinderShooter.Core
{
    /// <summary>
    /// A kind of enemy as the level system sees it: its own limit, the formations it may fly in and how it may enter.
    /// Facts about the enemy that hold in every stage, so they are set once.
    /// </summary>
    public interface IEnemyType
    {
        string DisplayName { get; }

        /// <summary>Most of this type alive at once. 0 = no limit of its own.</summary>
        int MaxAlive { get; }

        IReadOnlyList<IFormationOption> Formations { get; }

        /// <summary>How groups of this enemy may enter. Empty = just off-screen.</summary>
        IReadOnlyList<IEntryOption> Entries { get; }
    }

    /// <summary>A formation an enemy may fly in, and how many may be in it.</summary>
    public interface IFormationOption
    {
        string DisplayName { get; }
        int MinCount { get; }
        int MaxCount { get; }
        float Weight { get; }
    }

    public enum EntryStyle
    {
        /// <summary>Just past the edge of the screen, then flies in. Reaches the player quickly.</summary>
        JustOffScreen,
        /// <summary>A set distance along the ring, for enemies the player should see coming (for example a boss).</summary>
        AtDistance
    }

    /// <summary>One way a group of an enemy may enter.</summary>
    public interface IEntryOption
    {
        EntryStyle Style { get; }

        /// <summary>Metres along the ring from the player. Only for <see cref="EntryStyle.AtDistance"/>.</summary>
        float Distance { get; }

        float Weight { get; }
    }

    /// <summary>One row of a stage's enemy list.</summary>
    public interface IStageEnemy
    {
        IEnemyType Enemy { get; }

        /// <summary>The unlock toggle: off = this enemy can't spawn in the stage.</summary>
        bool Enabled { get; }

        /// <summary>Chance of being picked, compared with the other rows.</summary>
        float Weight { get; }

        /// <summary>Most of this enemy alive at once in the stage. 0 = only the enemy's own limit.</summary>
        int MaxAlive { get; }
    }

    /// <summary>What one enemy costs. Per level, so levels can share a table or each have their own.</summary>
    public interface ICostTable
    {
        bool TryGetCost(IEnemyType type, out float cost);
    }

    /// <summary>How many enemies are alive, in total and per type.</summary>
    public interface IEnemyCensus
    {
        int TotalAlive { get; }
        int AliveOf(IEnemyType type);
    }

    /// <summary>Raised when an enemy is destroyed in play (not when it is removed by a skip).</summary>
    public interface IKillFeed
    {
        event Action EnemyDestroyed;
    }

    /// <summary>What the director watches about the player.</summary>
    public interface IPlayerWatch
    {
        float Height { get; }

        /// <summary>+1 heading screen-right, -1 heading screen-left.</summary>
        float FacingSign { get; }

        /// <summary>The player lost a life.</summary>
        event Action Hit;
    }

    /// <summary>Asked by a spawner once it knows how many enemies a group will bring.</summary>
    public interface ISpawnApproval
    {
        SpawnVerdict Approve(IEnemyType type, int count);
    }

    /// <summary>
    /// One group the director wants: what, how many, at which height, from which side and how it enters. The spawner
    /// finds the exact place; the director never deals with GameObjects.
    /// </summary>
    public readonly struct SpawnRequest
    {
        public SpawnRequest(IEnemyType enemy, IFormationOption formation, int count, IEntryOption entry, float height,
            int screenSide)
        {
            Enemy = enemy;
            Formation = formation;
            Count = count;
            Entry = entry;
            Height = height;
            ScreenSide = screenSide;
        }

        public IEnemyType Enemy { get; }
        public IFormationOption Formation { get; }
        public int Count { get; }

        /// <summary>Null = just off-screen.</summary>
        public IEntryOption Entry { get; }

        /// <summary>Where the formation's centre should be, in metres (the spawner keeps it inside the arena).</summary>
        public float Height { get; }

        /// <summary>+1 = screen-right of the player, -1 = screen-left.</summary>
        public int ScreenSide { get; }
    }

    /// <summary>Brings groups into the arena. The director decides what and when; the spawner decides the exact place.</summary>
    public interface IEnemySpawner
    {
        SpawnOutcome TrySpawn(in SpawnRequest request, ISpawnApproval approval);
    }

    public enum SpawnVerdict
    {
        Approved,
        NoEnemyType,
        NotInStage,
        StageEnemyCapReached,
        TypeCapReached,
        StageCapReached,
        TotalCapReached,
        NoRunningStage
    }

    public enum SpawnStatus
    {
        Spawned,
        /// <summary>The gate said no. See the verdict.</summary>
        Refused,
        /// <summary>No place was found this time. Worth trying again.</summary>
        NoRoom,
        /// <summary>The group or its enemy can't be spawned at all (missing prefab or formation).</summary>
        Invalid
    }

    /// <summary>What happened to a spawn request, with a reason the debugger can show.</summary>
    public readonly struct SpawnOutcome
    {
        public SpawnOutcome(SpawnStatus status, SpawnVerdict verdict, int count, string reason)
        {
            Status = status;
            Verdict = verdict;
            Count = count;
            Reason = reason;
        }

        public SpawnStatus Status { get; }
        public SpawnVerdict Verdict { get; }
        public int Count { get; }
        public string Reason { get; }

        /// <summary>A cap was reached: waiting may help.</summary>
        public bool IsFull => Status == SpawnStatus.Refused &&
                              (Verdict == SpawnVerdict.StageEnemyCapReached || Verdict == SpawnVerdict.TypeCapReached ||
                               Verdict == SpawnVerdict.StageCapReached || Verdict == SpawnVerdict.TotalCapReached);

        public static SpawnOutcome Spawned(int count) => new SpawnOutcome(SpawnStatus.Spawned, SpawnVerdict.Approved, count, null);
        public static SpawnOutcome Refused(SpawnVerdict verdict, string reason) => new SpawnOutcome(SpawnStatus.Refused, verdict, 0, reason);
        public static SpawnOutcome NoRoom(string reason) => new SpawnOutcome(SpawnStatus.NoRoom, SpawnVerdict.Approved, 0, reason);
        public static SpawnOutcome Invalid(string reason) => new SpawnOutcome(SpawnStatus.Invalid, SpawnVerdict.Approved, 0, reason);
    }

    public static class StageEnemies
    {
        /// <summary>The stage's row for this enemy, or null when the stage doesn't list it.</summary>
        public static IStageEnemy Find(IStageSettings stage, IEnemyType type)
        {
            if (stage?.Enemies == null || type == null) return null;
            var rows = stage.Enemies;
            for (int i = 0; i < rows.Count; i++)
                if (rows[i] != null && ReferenceEquals(rows[i].Enemy, type)) return rows[i];
            return null;
        }
    }
}
