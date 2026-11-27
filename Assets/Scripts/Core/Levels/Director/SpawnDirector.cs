using System;
using UnityEngine;

namespace CylinderShooter.Core
{
    /// <summary>What the director is doing right now. An enum, not a sentence, so nothing is built every frame.</summary>
    public enum DirectorState
    {
        /// <summary>No stage is running.</summary>
        Idle,
        /// <summary>The stage's time is up and it waits for a clear: no more buying.</summary>
        StoppedBuying,
        /// <summary>No enemy is switched on with a weight, formations and a cost in this level.</summary>
        NothingEnabled,
        /// <summary>Every enemy is at a limit; waiting for some to die.</summary>
        StageFull,
        /// <summary>Even the smallest group costs more than the credit cap. A setup mistake.</summary>
        TooExpensive,
        /// <summary>Has a plan and is earning credits for it.</summary>
        Saving,
        /// <summary>Can afford the plan, waiting for the gap between groups.</summary>
        CoolingDown,
        /// <summary>Can afford the plan, but the spawner found no free place this frame.</summary>
        NoPlace,
        /// <summary>A group just spawned.</summary>
        Spawned
    }

    /// <summary>
    /// The spawn director. Each frame of a stage it earns credits (faster on a hot streak), keeps one plan for the next
    /// group, saves until it can afford it, waits out the cooldown, and asks the gate and the spawner to bring it in. A
    /// plan that takes longer than the stage's patience is shrunk to what is affordable, or replaced. The designer sets
    /// the pressure (credits) and the enemies; this decides what, how many, where and when.
    /// </summary>
    public sealed class SpawnDirector : IDisposable
    {
        private readonly LevelRunner _run;
        private readonly IEnemySpawner _spawner;
        private readonly SpawnGate _gate;
        private readonly IEnemyCensus _census;
        private readonly IDirectorSettings _level;
        private readonly IRandomSource _random;
        private readonly IPlayerWatch _player;
        private readonly IKillFeed _kills;
        private readonly LaneTracker _lanes;
        private readonly GroupPlanner _planner = new GroupPlanner();
        private readonly CreditWallet _wallet = new CreditWallet();
        private float _now;
        private float _cooldownLeft;

        public SpawnDirector(LevelRunner run, IEnemySpawner spawner, SpawnGate gate, IEnemyCensus census,
            IDirectorSettings level, IRandomSource random, LaneTracker lanes, IPlayerWatch player = null,
            IKillFeed kills = null)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _spawner = spawner ?? throw new ArgumentNullException(nameof(spawner));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _census = census ?? throw new ArgumentNullException(nameof(census));
            _level = level ?? throw new ArgumentNullException(nameof(level));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _lanes = lanes ?? throw new ArgumentNullException(nameof(lanes));
            _player = player;
            _kills = kills;
            _run.StageStarted += HandleStageStarted;
            _run.StageEnded += HandleStageEnded;
            _run.RunEnded += HandleRunEnded;
            if (_player != null) _player.Hit += HandleHit;
            if (_kills != null) _kills.EnemyDestroyed += HandleKill;
        }

        /// <summary>A group came in: enemy, formation, count, cost.</summary>
        public event Action<GroupPlan> GroupSpawned;

        /// <summary>A plan was given up because it can never spawn, with the reason.</summary>
        public event Action<string> GroupDropped;

        public DirectorState State { get; private set; }
        public float Credits => _wallet.Credits;
        public GroupPlan Plan { get; } = new GroupPlan();
        public LaneTracker Lanes => _lanes;
        public StreakMeter Streak { get; } = new StreakMeter();
        public bool IsHot => Streak.IsHot(_level.StreakKills);
        public float CooldownLeft => Mathf.Max(0f, _cooldownLeft);
        public int MaxEnemiesAlive => _level.MaxEnemiesAlive;

        /// <summary>Credits per second right now, with the streak bonus.</summary>
        public float EarnRate
        {
            get
            {
                var stage = _run.CurrentStage?.Settings;
                if (stage == null) return 0f;
                return Mathf.Max(0f, stage.CreditsPerSecond) * (IsHot ? Mathf.Max(0f, _level.StreakBonus) : 1f);
            }
        }

        /// <summary>The last spawn refusal or failure (null after a success). Built only when something goes wrong.</summary>
        public string LastProblem { get; private set; }

        /// <summary>Debugging: stop earning credits.</summary>
        public bool FreezeCredits { get; set; }

        public void Tick(float deltaTime)
        {
            deltaTime = Mathf.Max(0f, deltaTime);
            _now += deltaTime;
            Streak.Tick(deltaTime, _level.StreakWindow);
            if (_player != null) _lanes.Tick(deltaTime, _player.Height, _level.LaneMemory);

            var stageRunner = _run.CurrentStage;
            if (stageRunner == null || stageRunner.IsComplete)
            {
                State = DirectorState.Idle;
                return;
            }
            var stage = stageRunner.Settings;
            _cooldownLeft -= deltaTime;
            if (!FreezeCredits) _wallet.Earn(EarnRate * deltaTime, stage.CreditCap);

            if (stage.EndMode != StageEndMode.TimeUp && stageRunner.Elapsed >= stage.Duration)
            {
                State = DirectorState.StoppedBuying;
                Plan.Clear();
                return;
            }

            if (!Plan.IsSet && !MakePlan(stage)) return;
            Plan.Age += deltaTime;
            if (Plan.Age > stage.Patience && !TryBeQuicker(stage)) return;

            if (!_wallet.CanAfford(Plan.Cost))
            {
                State = DirectorState.Saving;
                return;
            }
            if (_cooldownLeft > 0f)
            {
                State = DirectorState.CoolingDown;
                return;
            }
            Spawn(stage, false);
        }

        /// <summary>Debugging: spawn the current plan now, ignoring credits and the cooldown (limits still apply).</summary>
        public bool SpawnPlanNow()
        {
            var stage = _run.CurrentStage?.Settings;
            if (stage == null) return false;
            if (!Plan.IsSet && !MakePlan(stage)) return false;
            return Spawn(stage, true);
        }

        /// <summary>Debugging: forget the plan and pick again.</summary>
        public void NewPlan()
        {
            var stage = _run.CurrentStage?.Settings;
            Plan.Clear();
            if (stage != null) MakePlan(stage);
        }

        /// <summary>Debugging: add credits (never over the cap).</summary>
        public void GiveCredits(float amount)
        {
            var stage = _run.CurrentStage?.Settings;
            _wallet.Earn(amount, stage?.CreditCap ?? 0f);
        }

        /// <summary>
        /// Debugging: spawn a chosen group now, free, just off-screen in the player's lane. Returns null on success,
        /// otherwise why not.
        /// </summary>
        public string SpawnGroup(IEnemyType enemy, IFormationOption formation, int count, bool ignoreStageList)
        {
            var stage = _run.CurrentStage?.Settings;
            if (stage == null) return "no stage is running";
            if (enemy == null || formation == null || count < 1) return "choose an enemy, a formation and a count";
            int lane = _player != null ? _lanes.LaneOf(_player.Height) : 1;
            int side = _random.Value() < 0.5f ? 1 : -1;
            var request = new SpawnRequest(enemy, formation, count, null, _lanes.CenterOf(lane), side);
            _gate.SetContext(stage, false);
            var outcome = _spawner.TrySpawn(request, _gate);
            _gate.ClearContext();
            LastProblem = outcome.Status == SpawnStatus.Spawned ? null : $"{enemy.DisplayName}: {outcome.Reason}";
            return LastProblem;
        }

        public void Dispose()
        {
            _run.StageStarted -= HandleStageStarted;
            _run.StageEnded -= HandleStageEnded;
            _run.RunEnded -= HandleRunEnded;
            if (_player != null) _player.Hit -= HandleHit;
            if (_kills != null) _kills.EnemyDestroyed -= HandleKill;
        }

        private bool MakePlan(IStageSettings stage)
        {
            var failure = _planner.TryPlan(Plan, stage, _level, _census, _lanes, _player?.FacingSign ?? 1f, _random, _now);
            switch (failure)
            {
                case PlanFailure.None: return true;
                case PlanFailure.NothingEnabled: State = DirectorState.NothingEnabled; return false;
                case PlanFailure.TooExpensive: State = DirectorState.TooExpensive; return false;
                default: State = DirectorState.StageFull; return false;
            }
        }

        /// <summary>
        /// Patience ran out: buy a smaller group now if one is affordable and fits, otherwise pick again (maybe
        /// something cheaper). False when there is nothing to buy this frame.
        /// </summary>
        private bool TryBeQuicker(IStageSettings stage)
        {
            int affordable = Plan.CostEach > 0f ? Mathf.FloorToInt(_wallet.Credits / Plan.CostEach + 1e-4f) : 0;
            var row = StageEnemies.Find(stage, Plan.Enemy);
            int room = row != null ? GroupPlanner.Room(row, stage, _level, _census) : 0;
            int count = Mathf.Min(Plan.Count, Mathf.Min(affordable, room));
            if (count >= Plan.MinCount)
            {
                Plan.Shrink(count);
                return true;
            }
            Plan.Clear();
            return MakePlan(stage);
        }

        private bool Spawn(IStageSettings stage, bool free)
        {
            // Ask the gate first: a refusal here costs no string building, which matters when it repeats every frame.
            _gate.SetContext(stage, false);
            var verdict = _gate.Approve(Plan.Enemy, Plan.Count);
            if (verdict != SpawnVerdict.Approved)
            {
                _gate.ClearContext();
                if (verdict == SpawnVerdict.NotInStage || verdict == SpawnVerdict.NoEnemyType)
                {
                    Drop(SpawnGate.Describe(verdict, Plan.Enemy, Plan.Count));
                    return false;
                }
                State = DirectorState.StageFull;
                return false;
            }

            var request = new SpawnRequest(Plan.Enemy, Plan.Formation, Plan.Count, Plan.Entry,
                _lanes.CenterOf(Plan.Lane), Plan.ScreenSide);
            var outcome = _spawner.TrySpawn(request, _gate);
            _gate.ClearContext();
            switch (outcome.Status)
            {
                case SpawnStatus.Spawned:
                    if (!free) _wallet.Spend(Plan.Cost);
                    _planner.Remember(Plan, _now);
                    _cooldownLeft = stage.Cooldown;
                    State = DirectorState.Spawned;
                    LastProblem = null;
                    GroupSpawned?.Invoke(Plan);
                    Plan.Clear();
                    return true;
                case SpawnStatus.NoRoom:
                    State = DirectorState.NoPlace;
                    return false;
                default:
                    if (outcome.IsFull)
                    {
                        State = DirectorState.StageFull;
                        return false;
                    }
                    Drop($"{Plan.Enemy.DisplayName}: {outcome.Reason}");
                    return false;
            }
        }

        private void Drop(string reason)
        {
            LastProblem = reason;
            Plan.Clear();
            GroupDropped?.Invoke(reason);
        }

        private void HandleStageStarted(StageInfo info)
        {
            var stage = _run.CurrentStage?.Settings;
            _wallet.Reset(stage?.StartingCredits ?? 0f);
            Plan.Clear();
            _cooldownLeft = 0f;
            LastProblem = null;
        }

        private void HandleStageEnded(StageInfo info, EndReason reason)
        {
            Plan.Clear();
            State = DirectorState.Idle;
        }

        private void HandleRunEnded(EndReason reason)
        {
            Plan.Clear();
            State = DirectorState.Idle;
            _wallet.Reset(0f);
        }

        private void HandleKill() => Streak.Kill();

        private void HandleHit() => Streak.Hit();
    }
}
