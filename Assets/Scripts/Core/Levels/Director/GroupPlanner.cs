using System.Collections.Generic;
using UnityEngine;

namespace CylinderShooter.Core
{
    /// <summary>The group the director has decided to buy next, and is saving for. Reused, so planning makes no garbage.</summary>
    public sealed class GroupPlan
    {
        public bool IsSet { get; private set; }
        public IEnemyType Enemy { get; private set; }
        public IFormationOption Formation { get; private set; }
        public IEntryOption Entry { get; private set; }
        public int Count { get; private set; }
        public int MinCount { get; private set; }
        public float CostEach { get; private set; }
        public int Lane { get; private set; }
        public int ScreenSide { get; private set; }

        /// <summary>Seconds since the plan was made (or last shrunk).</summary>
        public float Age { get; set; }

        public float Cost => Count * CostEach;

        public void Set(IEnemyType enemy, IFormationOption formation, IEntryOption entry, int count, int minCount,
            float costEach, int lane, int screenSide)
        {
            IsSet = true;
            Enemy = enemy;
            Formation = formation;
            Entry = entry;
            Count = count;
            MinCount = minCount;
            CostEach = costEach;
            Lane = lane;
            ScreenSide = screenSide;
            Age = 0f;
        }

        /// <summary>Makes the group smaller (never below its minimum) and starts its patience again.</summary>
        public void Shrink(int count)
        {
            Count = Mathf.Clamp(count, MinCount, Count);
            Age = 0f;
        }

        public void Clear()
        {
            IsSet = false;
            Enemy = null;
            Formation = null;
            Entry = null;
            Count = 0;
            Age = 0f;
        }
    }

    public enum PlanFailure
    {
        None,
        /// <summary>No enemy is switched on with a weight, formations and a cost.</summary>
        NothingEnabled,
        /// <summary>Enemies are on, but every one is at a limit or its smallest group doesn't fit.</summary>
        NoRoom,
        /// <summary>Even the smallest group costs more than the credit cap.</summary>
        TooExpensive
    }

    /// <summary>
    /// Decides what to buy next: an enemy (by the stage's weights, lowered for recent picks), one of its formations,
    /// a group size (pushed by the stage's Group Size, kept under every limit and the credit cap), how it enters, which
    /// lane (often the one the player favours) and which side (keeping one side of that lane open unless pincers are
    /// allowed). Plain C#, so every choice can be tested.
    /// </summary>
    public sealed class GroupPlanner
    {
        private readonly List<float> _weights = new List<float>();
        private readonly int[] _pincerSide = new int[LaneTracker.LaneCount];
        private readonly float[] _pincerTime = new float[LaneTracker.LaneCount];

        public GroupPlanner()
        {
            ClearPincers();
        }

        public VarietyMemory Variety { get; } = new VarietyMemory();

        public void ClearPincers()
        {
            for (int i = 0; i < LaneTracker.LaneCount; i++) _pincerTime[i] = float.NegativeInfinity;
        }

        /// <summary>Call when a group has been sent into a lane from a side.</summary>
        public void Remember(GroupPlan plan, float now)
        {
            Variety.Mark(plan.Enemy, now);
            Variety.Mark(plan.Formation, now);
            if (plan.Lane < 0 || plan.Lane >= LaneTracker.LaneCount) return;
            _pincerSide[plan.Lane] = plan.ScreenSide;
            _pincerTime[plan.Lane] = now;
        }

        public PlanFailure TryPlan(GroupPlan plan, IStageSettings stage, IDirectorSettings level, IEnemyCensus census,
            LaneTracker lanes, float facingSign, IRandomSource random, float now)
        {
            plan.Clear();
            var rows = stage.Enemies;
            if (rows == null || rows.Count == 0) return PlanFailure.NothingEnabled;

            bool anyEnabled = false;
            bool anyTooExpensive = false;
            _weights.Clear();
            for (int i = 0; i < rows.Count; i++)
            {
                float weight = 0f;
                var row = rows[i];
                if (IsUsable(row, level, out float cost))
                {
                    anyEnabled = true;
                    int room = Room(row, stage, level, census);
                    int capCount = CapCount(stage, cost);
                    int smallest = SmallestGroup(row.Enemy);
                    if (smallest > capCount) anyTooExpensive = true;
                    else if (smallest <= room)
                        weight = row.Weight * Variety.Factor(row.Enemy, now, level.VarietyDip, level.VarietyRecovery);
                }
                _weights.Add(weight);
            }
            int pick = WeightedPick.Choose(_weights, random.Value());
            if (pick < 0)
                return !anyEnabled ? PlanFailure.NothingEnabled : anyTooExpensive ? PlanFailure.TooExpensive : PlanFailure.NoRoom;

            var chosen = rows[pick];
            var enemy = chosen.Enemy;
            level.Costs.TryGetCost(enemy, out float costEach);
            int roomFor = Room(chosen, stage, level, census);
            int maxByCredits = CapCount(stage, costEach);

            _weights.Clear();
            var formations = enemy.Formations;
            for (int i = 0; i < formations.Count; i++)
            {
                var option = formations[i];
                float weight = 0f;
                if (option != null && option.Weight > 0f && MinOf(option) <= roomFor && MinOf(option) <= maxByCredits)
                    weight = option.Weight * Variety.Factor(option, now, level.VarietyDip, level.VarietyRecovery);
                _weights.Add(weight);
            }
            int formationIndex = WeightedPick.Choose(_weights, random.Value());
            if (formationIndex < 0) return PlanFailure.NoRoom;
            var formation = formations[formationIndex];

            int lo = MinOf(formation);
            int hi = Mathf.Max(lo, Mathf.Min(MaxOf(formation), Mathf.Min(roomFor, maxByCredits)));
            float spread = Mathf.Clamp01(level.GroupSizeSpread);
            float t = Mathf.Clamp01(stage.GroupSize + (random.Value() * 2f - 1f) * spread);
            int count = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(lo, hi, t)), lo, hi);

            var entry = PickEntry(enemy, random);
            int lane = random.Value() < Mathf.Clamp01(stage.LaneFocus)
                ? lanes.Favourite
                : Mathf.Min(LaneTracker.LaneCount - 1, (int)(random.Value() * LaneTracker.LaneCount));
            int side = PickSide(stage, level, lane, facingSign, random, now);

            plan.Set(enemy, formation, entry, count, lo, costEach, lane, side);
            return PlanFailure.None;
        }

        /// <summary>How many more of this row's enemy may be alive now, under every limit.</summary>
        public static int Room(IStageEnemy row, IStageSettings stage, IDirectorSettings level, IEnemyCensus census)
        {
            int total = census.TotalAlive;
            int alive = census.AliveOf(row.Enemy);
            int room = level.MaxEnemiesAlive - total;
            if (stage.MaxAlive > 0) room = Mathf.Min(room, stage.MaxAlive - total);
            if (row.MaxAlive > 0) room = Mathf.Min(room, row.MaxAlive - alive);
            if (row.Enemy.MaxAlive > 0) room = Mathf.Min(room, row.Enemy.MaxAlive - alive);
            return Mathf.Max(0, room);
        }

        private static bool IsUsable(IStageEnemy row, IDirectorSettings level, out float cost)
        {
            cost = 0f;
            return row != null && row.Enabled && row.Weight > 0f && row.Enemy != null &&
                   row.Enemy.Formations != null && row.Enemy.Formations.Count > 0 &&
                   level.Costs != null && level.Costs.TryGetCost(row.Enemy, out cost) && cost > 0f;
        }

        private static int CapCount(IStageSettings stage, float cost) =>
            stage.CreditCap > 0f ? Mathf.FloorToInt(stage.CreditCap / cost + 1e-4f) : int.MaxValue;

        private static int SmallestGroup(IEnemyType enemy)
        {
            int smallest = int.MaxValue;
            var formations = enemy.Formations;
            for (int i = 0; i < formations.Count; i++)
                if (formations[i] != null && formations[i].Weight > 0f) smallest = Mathf.Min(smallest, MinOf(formations[i]));
            return smallest;
        }

        private static int MinOf(IFormationOption option) => Mathf.Max(1, option.MinCount);
        private static int MaxOf(IFormationOption option) => Mathf.Max(MinOf(option), option.MaxCount);

        private IEntryOption PickEntry(IEnemyType enemy, IRandomSource random)
        {
            var entries = enemy.Entries;
            if (entries == null || entries.Count == 0) return null;
            _weights.Clear();
            for (int i = 0; i < entries.Count; i++) _weights.Add(entries[i] != null ? entries[i].Weight : 0f);
            int index = WeightedPick.Choose(_weights, random.Value());
            return index < 0 ? null : entries[index];
        }

        private int PickSide(IStageSettings stage, IDirectorSettings level, int lane, float facingSign,
            IRandomSource random, float now)
        {
            int ahead = facingSign >= 0f ? 1 : -1;
            int side;
            switch (stage.Sides)
            {
                case SpawnSides.Ahead: side = ahead; break;
                case SpawnSides.Behind: side = -ahead; break;
                default: side = random.Value() < 0.5f ? 1 : -1; break;
            }
            // Leave the player a way out: a lane that was just entered from one side keeps that side for a while.
            if (!stage.AllowPincer && now - _pincerTime[lane] < level.PincerWindow) side = _pincerSide[lane];
            return side;
        }
    }
}
