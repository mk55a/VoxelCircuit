using System;
using System.Collections.Generic;
using CylinderShooter.Core;
using NUnit.Framework;

namespace CylinderShooter.Tests
{
    /// <summary>The spawn gate, the director's parts (wallet, variety, lanes, streak, planner) and the director itself.</summary>
    public sealed class SpawningTests
    {
        private sealed class Formation : IFormationOption
        {
            public Formation(int min, int max, float weight = 1f)
            {
                MinCount = min;
                MaxCount = max;
                Weight = weight;
            }

            public string DisplayName => "Formation";
            public int MinCount { get; }
            public int MaxCount { get; }
            public float Weight { get; set; }
        }

        private sealed class Enemy : IEnemyType
        {
            public Enemy(string name, int maxAlive = 0, params IFormationOption[] formations)
            {
                DisplayName = name;
                MaxAlive = maxAlive;
                Formations = formations.Length > 0 ? formations : new IFormationOption[] { new Formation(5, 5) };
            }

            public string DisplayName { get; }
            public int MaxAlive { get; }
            public IReadOnlyList<IFormationOption> Formations { get; }
            public IReadOnlyList<IEntryOption> Entries => Array.Empty<IEntryOption>();
        }

        private sealed class Row : IStageEnemy
        {
            public Row(IEnemyType enemy, float weight = 1f, int maxAlive = 0, bool enabled = true)
            {
                Enemy = enemy;
                Weight = weight;
                MaxAlive = maxAlive;
                Enabled = enabled;
            }

            public IEnemyType Enemy { get; }
            public bool Enabled { get; set; }
            public float Weight { get; set; }
            public int MaxAlive { get; set; }
        }

        private sealed class Stage : IStageSettings
        {
            public string DisplayName => "Stage";
            public StageEndMode EndMode { get; set; } = StageEndMode.TimeUp;
            public float Duration { get; set; } = 100f;
            public float MaxDuration { get; set; } = 200f;
            public float StartingCredits { get; set; }
            public float CreditsPerSecond { get; set; } = 2f;
            public float CreditCap { get; set; } = 100f;
            public float GroupSize { get; set; } = 0.5f;
            public float Patience { get; set; } = 100f;
            public float Cooldown { get; set; }
            public int MaxAlive { get; set; }
            public List<IStageEnemy> Rows { get; } = new List<IStageEnemy>();
            public IReadOnlyList<IStageEnemy> Enemies => Rows;
            public SpawnSides Sides { get; set; } = SpawnSides.Either;
            public float LaneFocus { get; set; }
            public bool AllowPincer { get; set; }
        }

        private sealed class Wave : IWaveSettings, IStagedWaveSettings
        {
            public Wave(params IStageSettings[] stages) => Stages = stages;
            public string DisplayName => "Wave";
            public IReadOnlyList<IStageSettings> Stages { get; }
            public IWaveRunner CreateRunner(IBattlefield battlefield) => new StagedWaveRunner(this, battlefield);
        }

        private sealed class Level : ILevelSettings, IDirectorSettings
        {
            public Level(params IWaveSettings[] waves) => Waves = waves;
            public string DisplayName => "Level";
            public IReadOnlyList<IWaveSettings> Waves { get; }
            public int MaxEnemiesAlive { get; set; } = 150;
            public Costs Table { get; } = new Costs();
            public ICostTable Costs => Table;
            public float VarietyDip { get; set; } = 1f;
            public float VarietyRecovery { get; set; } = 8f;
            public float GroupSizeSpread { get; set; }
            public float LaneMemory { get; set; } = 8f;
            public float PincerWindow { get; set; } = 6f;
            public float StreakKills { get; set; }
            public float StreakWindow { get; set; } = 8f;
            public float StreakBonus { get; set; } = 2f;
        }

        private sealed class Costs : ICostTable
        {
            public readonly Dictionary<IEnemyType, float> Values = new Dictionary<IEnemyType, float>();
            public bool TryGetCost(IEnemyType type, out float cost) =>
                Values.TryGetValue(type, out cost) && cost > 0f;
        }

        private sealed class Census : IEnemyCensus
        {
            public readonly Dictionary<IEnemyType, int> Alive = new Dictionary<IEnemyType, int>();
            public int TotalAlive { get; set; }
            public int AliveOf(IEnemyType type) => type != null && Alive.TryGetValue(type, out int n) ? n : 0;

            public void Add(IEnemyType type, int count)
            {
                Alive[type] = AliveOf(type) + count;
                TotalAlive += count;
            }

            public void Clear()
            {
                Alive.Clear();
                TotalAlive = 0;
            }
        }

        private sealed class Field : IBattlefield
        {
            private readonly Census _census;
            public Field(Census census) => _census = census;
            public int AliveEnemies => _census.TotalAlive;
            public void RemoveAllEnemies() => _census.Clear();
        }

        /// <summary>Spawns what the gate approves and records it, or reports no room when told to.</summary>
        private sealed class FakeSpawner : IEnemySpawner
        {
            private readonly Census _census;
            public FakeSpawner(Census census) => _census = census;
            public bool NoRoom { get; set; }
            public List<SpawnRequest> Spawned { get; } = new List<SpawnRequest>();

            public SpawnOutcome TrySpawn(in SpawnRequest request, ISpawnApproval approval)
            {
                var verdict = approval.Approve(request.Enemy, request.Count);
                if (verdict != SpawnVerdict.Approved) return SpawnOutcome.Refused(verdict, verdict.ToString());
                if (NoRoom) return SpawnOutcome.NoRoom("no room");
                _census.Add(request.Enemy, request.Count);
                Spawned.Add(request);
                return SpawnOutcome.Spawned(request.Count);
            }
        }

        private sealed class FixedRandom : IRandomSource
        {
            public float Next { get; set; } = 0.5f;
            public float Value() => Next;
        }

        private sealed class Player : IPlayerWatch
        {
            public float Height { get; set; }
            public float FacingSign { get; set; } = 1f;
            public event Action Hit;
            public void GetHit() => Hit?.Invoke();
        }

        private sealed class Kills : IKillFeed
        {
            public event Action EnemyDestroyed;
            public void Kill() => EnemyDestroyed?.Invoke();
        }

        private Census _census;
        private FakeSpawner _spawner;
        private FixedRandom _random;
        private Player _player;
        private Kills _kills;
        private Stage _stage;
        private Level _level;
        private LevelRunner _run;
        private SpawnDirector _director;
        private Enemy _chaser;

        [SetUp]
        public void SetUp()
        {
            _census = new Census();
            _spawner = new FakeSpawner(_census);
            _random = new FixedRandom();
            _player = new Player();
            _kills = new Kills();
            _chaser = new Enemy("Chaser");
            _stage = new Stage();
            _stage.Rows.Add(new Row(_chaser));
            _level = new Level(new Wave(_stage));
            _level.Table.Values[_chaser] = 1f;
        }

        [TearDown]
        public void TearDown() => _director?.Dispose();

        private void Start()
        {
            _run = new LevelRunner(_level, new Field(_census));
            _director = new SpawnDirector(_run, _spawner, new SpawnGate(_census, _level.MaxEnemiesAlive), _census, _level,
                _random, new LaneTracker(-6f, 6f), _player, _kills);
            _run.Begin();
        }

        private void Step(float seconds, float dt = 0.1f)
        {
            for (float t = 0f; t < seconds - 1e-4f; t += dt)
            {
                _run.Tick(dt);
                _director.Tick(dt);
            }
        }

        // ---- Gate ----

        [Test]
        public void Gate_RefusesEnemiesNotOnInTheStage()
        {
            var gate = new SpawnGate(_census, 150);
            var bomber = new Enemy("Bomber");
            gate.SetContext(_stage, false);
            Assert.AreEqual(SpawnVerdict.Approved, gate.Approve(_chaser, 5));
            Assert.AreEqual(SpawnVerdict.NotInStage, gate.Approve(bomber, 5));
            ((Row)_stage.Rows[0]).Enabled = false;
            Assert.AreEqual(SpawnVerdict.NotInStage, gate.Approve(_chaser, 5));
            gate.ClearContext();
            Assert.AreEqual(SpawnVerdict.NoRunningStage, gate.Approve(_chaser, 5));
        }

        [Test]
        public void Gate_KeepsEveryLimit()
        {
            var gate = new SpawnGate(_census, 20);
            var row = (Row)_stage.Rows[0];
            gate.SetContext(_stage, false);
            row.MaxAlive = 4;
            Assert.AreEqual(SpawnVerdict.StageEnemyCapReached, gate.Approve(_chaser, 5));
            row.MaxAlive = 0;
            _stage.MaxAlive = 4;
            Assert.AreEqual(SpawnVerdict.StageCapReached, gate.Approve(_chaser, 5));
            _stage.MaxAlive = 0;
            _census.Add(_chaser, 18);
            Assert.AreEqual(SpawnVerdict.TotalCapReached, gate.Approve(_chaser, 5));
            var capped = new Enemy("Capped", 3);
            gate.SetContext(_stage, true);
            Assert.AreEqual(SpawnVerdict.TypeCapReached, gate.Approve(capped, 4), "ignoring the list never ignores an enemy's own limit");
            Assert.AreEqual(SpawnVerdict.Approved, gate.Approve(capped, 2), "ignoring the list allows an enemy the stage doesn't have");
            Assert.AreEqual(SpawnVerdict.TotalCapReached, gate.Approve(_chaser, 5), "the level's limit is never ignored");
        }

        // ---- Parts ----

        [Test]
        public void Wallet_EarnsUpToTheCapAndSpends()
        {
            var wallet = new CreditWallet();
            wallet.Reset(5f);
            wallet.Earn(10f, 12f);
            Assert.AreEqual(12f, wallet.Credits, 1e-4f);
            Assert.IsTrue(wallet.CanAfford(12f));
            wallet.Spend(7f);
            Assert.AreEqual(5f, wallet.Credits, 1e-4f);
            wallet.Earn(100f, 0f);
            Assert.AreEqual(105f, wallet.Credits, 1e-4f, "cap 0 = no limit");
        }

        [Test]
        public void Variety_DipsAfterAPickAndRecovers()
        {
            var memory = new VarietyMemory();
            var key = new object();
            Assert.AreEqual(1f, memory.Factor(key, 0f, 0.3f, 10f));
            memory.Mark(key, 0f);
            Assert.AreEqual(0.3f, memory.Factor(key, 0f, 0.3f, 10f), 1e-4f);
            Assert.AreEqual(0.65f, memory.Factor(key, 5f, 0.3f, 10f), 1e-4f);
            Assert.AreEqual(1f, memory.Factor(key, 10f, 0.3f, 10f), 1e-4f);
        }

        [Test]
        public void WeightedPick_FollowsTheWeights()
        {
            var weights = new List<float> { 3f, 0f, 1f };
            Assert.AreEqual(0, WeightedPick.Choose(weights, 0f));
            Assert.AreEqual(0, WeightedPick.Choose(weights, 0.74f));
            Assert.AreEqual(2, WeightedPick.Choose(weights, 0.76f));
            Assert.AreEqual(2, WeightedPick.Choose(weights, 1f));
            Assert.AreEqual(-1, WeightedPick.Choose(new List<float> { 0f, 0f }, 0.5f));
        }

        [Test]
        public void Lanes_FindTheFavouriteAndForgetOldTime()
        {
            var lanes = new LaneTracker(-6f, 6f);
            Assert.AreEqual(0, lanes.LaneOf(-5f));
            Assert.AreEqual(1, lanes.LaneOf(0f));
            Assert.AreEqual(2, lanes.LaneOf(5.9f));
            Assert.AreEqual(4f, lanes.CenterOf(2), 1e-4f);
            for (int i = 0; i < 100; i++) lanes.Tick(0.1f, 5f, 8f);
            Assert.AreEqual(2, lanes.Favourite);
            for (int i = 0; i < 300; i++) lanes.Tick(0.1f, -5f, 8f);
            Assert.AreEqual(0, lanes.Favourite);
            Assert.Greater(lanes.Share(0), 0.9f);
        }

        [Test]
        public void Streak_HeatsUpWithKillsAndEndsOnAHit()
        {
            var streak = new StreakMeter();
            for (int i = 0; i < 10; i++) streak.Kill();
            Assert.IsTrue(streak.IsHot(10f));
            streak.Tick(8f, 8f);
            Assert.IsFalse(streak.IsHot(10f), "kills fade over the window");
            for (int i = 0; i < 20; i++) streak.Kill();
            streak.Hit();
            Assert.IsFalse(streak.IsHot(10f));
            Assert.IsFalse(streak.IsHot(0f), "threshold 0 = streaks off");
        }

        // ---- Director ----

        [Test]
        public void Director_SavesThenSpawns()
        {
            Start();
            Step(2.4f);
            Assert.AreEqual(0, _spawner.Spawned.Count);
            Assert.AreEqual(DirectorState.Saving, _director.State);
            Step(0.2f);
            Assert.AreEqual(1, _spawner.Spawned.Count);
            Assert.AreEqual(5, _spawner.Spawned[0].Count);
            Assert.Less(_director.Credits, 0.5f);
        }

        [Test]
        public void Director_GroupSizeSetsHowBigGroupsAre()
        {
            _chaser = new Enemy("Chaser", 0, new Formation(4, 20));
            _stage.Rows[0] = new Row(_chaser);
            _level.Table.Values[_chaser] = 1f;
            _stage.StartingCredits = 100f;
            _stage.GroupSize = 0f;
            Start();
            Step(0.1f);
            Assert.AreEqual(4, _spawner.Spawned[0].Count);

            _stage.GroupSize = 1f;
            _stage.CreditCap = 12f;
            _director.NewPlan();
            Assert.AreEqual(12, _director.Plan.Count, "big, but never more than the credit cap can buy");
        }

        [Test]
        public void Director_PatienceBuysASmallerGroup()
        {
            _chaser = new Enemy("Chaser", 0, new Formation(2, 10));
            _stage.Rows[0] = new Row(_chaser);
            _level.Table.Values[_chaser] = 1f;
            _stage.GroupSize = 1f;
            _stage.StartingCredits = 3f;
            _stage.CreditsPerSecond = 0f;
            _stage.Patience = 2f;
            Start();
            Step(1.9f);
            Assert.AreEqual(0, _spawner.Spawned.Count);
            Assert.AreEqual(10, _director.Plan.Count);
            Step(0.3f);
            Assert.AreEqual(1, _spawner.Spawned.Count);
            Assert.AreEqual(3, _spawner.Spawned[0].Count);
        }

        [Test]
        public void Director_WaitsForRoomWhenTheStageIsFull()
        {
            _stage.MaxAlive = 6;
            _stage.StartingCredits = 50f;
            Start();
            Step(0.1f);
            Assert.AreEqual(1, _spawner.Spawned.Count);
            Step(1f);
            Assert.AreEqual(1, _spawner.Spawned.Count);
            Assert.AreEqual(DirectorState.StageFull, _director.State);
            _census.Clear();
            Step(0.1f);
            Assert.AreEqual(2, _spawner.Spawned.Count);
        }

        [Test]
        public void Director_StopsBuyingAfterTheDurationWhenWaitingForAClear()
        {
            _stage.EndMode = StageEndMode.TimeUpThenCleared;
            _stage.Duration = 1f;
            _stage.CreditsPerSecond = 100f;
            _stage.Cooldown = 0f;
            Start();
            Step(0.95f);
            int spawned = _spawner.Spawned.Count;
            Assert.Greater(spawned, 0);
            Step(2f);
            Assert.AreEqual(spawned, _spawner.Spawned.Count);
            Assert.AreEqual(DirectorState.StoppedBuying, _director.State);
        }

        [Test]
        public void Director_CooldownSeparatesGroups()
        {
            _stage.StartingCredits = 100f;
            _stage.Cooldown = 1f;
            Start();
            Step(0.1f);
            Assert.AreEqual(1, _spawner.Spawned.Count);
            Step(0.8f);
            Assert.AreEqual(1, _spawner.Spawned.Count);
            Assert.AreEqual(DirectorState.CoolingDown, _director.State);
            Step(0.3f);
            Assert.AreEqual(2, _spawner.Spawned.Count);
        }

        [Test]
        public void Director_EarnsFasterOnAHotStreakUntilHit()
        {
            _level.StreakKills = 5f;
            _stage.Rows.Clear();
            Start();
            Assert.AreEqual(2f, _director.EarnRate, 1e-4f);
            for (int i = 0; i < 6; i++) _kills.Kill();
            Assert.IsTrue(_director.IsHot);
            Assert.AreEqual(4f, _director.EarnRate, 1e-4f);
            _player.GetHit();
            Assert.AreEqual(2f, _director.EarnRate, 1e-4f);
        }

        [Test]
        public void Director_ReportsWhenNothingCanBeBought()
        {
            _stage.CreditCap = 3f;
            Start();
            Step(0.1f);
            Assert.AreEqual(DirectorState.TooExpensive, _director.State, "a group of 5 at cost 1 never fits a cap of 3");
            _stage.Rows.Clear();
            _director.NewPlan();
            Step(0.1f);
            Assert.AreEqual(DirectorState.NothingEnabled, _director.State);
        }

        [Test]
        public void Director_StartsEachStageWithItsOwnCredits()
        {
            var second = new Stage { StartingCredits = 7f, CreditsPerSecond = 0f };
            _level = new Level(new Wave(_stage, second));
            _level.Table.Values[_chaser] = 1f;
            _stage.Duration = 1f;
            _stage.CreditsPerSecond = 0f;
            _stage.StartingCredits = 3f;
            Start();
            Step(1.05f);
            Assert.AreSame(second, _run.CurrentStage.Settings);
            Assert.AreEqual(7f, _director.Credits, 1e-4f);
        }

        [Test]
        public void Planner_TargetsTheFavouriteLane()
        {
            _stage.LaneFocus = 1f;
            _player.Height = 5f;
            Start();
            _stage.StartingCredits = 0f;
            _stage.CreditsPerSecond = 0f;
            Step(5f);
            _director.NewPlan();
            Assert.AreEqual(2, _director.Plan.Lane);
        }

        [Test]
        public void Planner_LeavesAWayOutUnlessPincersAreAllowed()
        {
            var lanes = new LaneTracker(-6f, 6f);
            var planner = new GroupPlanner();
            var plan = new GroupPlan();
            _random.Next = 0.9f;   // Either side: picks the left (-1); lanes at random: the top lane
            Assert.AreEqual(PlanFailure.None, planner.TryPlan(plan, _stage, _level, _census, lanes, 1f, _random, 0f));
            Assert.AreEqual(-1, plan.ScreenSide);
            planner.Remember(plan, 0f);

            _random.Next = 0.1f;   // would pick the right (+1), and the bottom lane
            planner.TryPlan(plan, _stage, _level, _census, lanes, 1f, _random, 1f);
            Assert.AreEqual(0, plan.Lane);
            Assert.AreEqual(1, plan.ScreenSide, "another lane: free to choose");

            _random.Next = 0.9f;
            planner.TryPlan(plan, _stage, _level, _census, lanes, 1f, _random, 1f);
            _random.Next = 0.1f;
            _stage.Sides = SpawnSides.Ahead;   // ahead = right, but the top lane was entered from the left
            planner.TryPlan(plan, _stage, _level, _census, lanes, 1f, _random, 2f);
            Assert.AreEqual(0, plan.Lane);
            var top = new GroupPlan();
            _random.Next = 0.9f;
            planner.TryPlan(top, _stage, _level, _census, lanes, 1f, _random, 2f);
            Assert.AreEqual(2, top.Lane);
            Assert.AreEqual(-1, top.ScreenSide, "the top lane keeps its side");

            _stage.AllowPincer = true;
            planner.TryPlan(top, _stage, _level, _census, lanes, 1f, _random, 2f);
            Assert.AreEqual(1, top.ScreenSide, "pincers allowed: ahead");
            planner.TryPlan(top, _stage, _level, _census, lanes, 1f, _random, 10f);
            _stage.AllowPincer = false;
            planner.TryPlan(top, _stage, _level, _census, lanes, 1f, _random, 10f);
            Assert.AreEqual(1, top.ScreenSide, "after the pincer window the lane is free again");
        }

        [Test]
        public void Planner_PrefersEnemiesThatWereNotJustPicked()
        {
            var bomber = new Enemy("Bomber");
            _stage.Rows.Add(new Row(bomber));
            _level.Table.Values[bomber] = 1f;
            _level.VarietyDip = 0.1f;
            var planner = new GroupPlanner();
            var plan = new GroupPlan();
            var lanes = new LaneTracker(-6f, 6f);
            _random.Next = 0.4f;   // equal weights: the first half picks the Chaser
            planner.TryPlan(plan, _stage, _level, _census, lanes, 1f, _random, 0f);
            Assert.AreSame(_chaser, plan.Enemy);
            planner.Remember(plan, 0f);
            planner.TryPlan(plan, _stage, _level, _census, lanes, 1f, _random, 0f);
            Assert.AreSame(bomber, plan.Enemy, "the Chaser's chance dipped to 0.1 against the Bomber's 1");
        }
    }
}
