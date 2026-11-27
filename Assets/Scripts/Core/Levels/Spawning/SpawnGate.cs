using System;

namespace CylinderShooter.Core
{
    /// <summary>
    /// The one check every spawn request passes, whoever asks (the director, the debugger): is this enemy switched on
    /// in the running stage, and is there room under its stage limit, its own limit, the stage's limit and the level's
    /// total? Nothing spawns without it. Ignoring the stage list is possible only when asked for explicitly; the
    /// level's total can never be ignored.
    /// </summary>
    public sealed class SpawnGate : ISpawnApproval
    {
        private readonly IEnemyCensus _census;
        private IStageSettings _stage;
        private bool _ignoreStageList;

        public SpawnGate(IEnemyCensus census, int maxTotalAlive)
        {
            _census = census ?? throw new ArgumentNullException(nameof(census));
            if (maxTotalAlive < 1) throw new ArgumentOutOfRangeException(nameof(maxTotalAlive));
            MaxTotalAlive = maxTotalAlive;
        }

        public int MaxTotalAlive { get; }

        /// <summary>Sets the stage the next approvals are checked against.</summary>
        public void SetContext(IStageSettings stage, bool ignoreStageList)
        {
            _stage = stage;
            _ignoreStageList = ignoreStageList;
        }

        /// <summary>No stage is running: every request is refused.</summary>
        public void ClearContext()
        {
            _stage = null;
            _ignoreStageList = false;
        }

        public SpawnVerdict Approve(IEnemyType type, int count)
        {
            if (type == null || count < 1) return SpawnVerdict.NoEnemyType;
            if (_stage == null) return SpawnVerdict.NoRunningStage;
            int aliveOfType = _census.AliveOf(type);
            if (!_ignoreStageList)
            {
                var row = StageEnemies.Find(_stage, type);
                if (row == null || !row.Enabled) return SpawnVerdict.NotInStage;
                if (row.MaxAlive > 0 && aliveOfType + count > row.MaxAlive) return SpawnVerdict.StageEnemyCapReached;
                if (_stage.MaxAlive > 0 && _census.TotalAlive + count > _stage.MaxAlive) return SpawnVerdict.StageCapReached;
            }
            if (type.MaxAlive > 0 && aliveOfType + count > type.MaxAlive) return SpawnVerdict.TypeCapReached;
            if (_census.TotalAlive + count > MaxTotalAlive) return SpawnVerdict.TotalCapReached;
            return SpawnVerdict.Approved;
        }

        public static string Describe(SpawnVerdict verdict, IEnemyType type, int count)
        {
            string name = type?.DisplayName ?? "(no enemy)";
            switch (verdict)
            {
                case SpawnVerdict.NotInStage: return $"{name} is not switched on in this stage";
                case SpawnVerdict.StageEnemyCapReached: return $"{count} more {name} would pass its limit in this stage";
                case SpawnVerdict.TypeCapReached: return $"{count} more {name} would pass its own limit of {type?.MaxAlive}";
                case SpawnVerdict.StageCapReached: return $"{count} more would pass the stage's enemy limit";
                case SpawnVerdict.TotalCapReached: return $"{count} more would pass the level's enemy limit";
                case SpawnVerdict.NoRunningStage: return "no stage is running";
                case SpawnVerdict.NoEnemyType: return "the group has no enemy";
                default: return "approved";
            }
        }
    }
}
