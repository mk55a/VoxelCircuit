using System;
using System.Collections.Generic;
using CylinderShooter.Core;
using NUnit.Framework;

namespace CylinderShooter.Tests
{
    public sealed class LevelRunnerTests
    {
        private sealed class Field : IBattlefield
        {
            public int AliveEnemies { get; set; }
            public int Removals { get; private set; }

            public void RemoveAllEnemies()
            {
                Removals++;
                AliveEnemies = 0;
            }
        }

        private sealed class Stage : IStageSettings
        {
            public string DisplayName { get; set; } = "Stage";
            public StageEndMode EndMode { get; set; } = StageEndMode.TimeUp;
            public float Duration { get; set; } = 10f;
            public float MaxDuration { get; set; } = 20f;
            public float StartingCredits => 0f;
            public float CreditsPerSecond => 0f;
            public float CreditCap => 0f;
            public float GroupSize => 0.5f;
            public float Patience => 5f;
            public float Cooldown => 0f;
            public int MaxAlive => 0;
            public IReadOnlyList<IStageEnemy> Enemies => Array.Empty<IStageEnemy>();
            public SpawnSides Sides => SpawnSides.Either;
            public float LaneFocus => 0f;
            public bool AllowPincer => false;
        }

        private sealed class Wave : IWaveSettings, IStagedWaveSettings
        {
            public Wave(params IStageSettings[] stages) => Stages = stages;
            public string DisplayName { get; set; } = "Wave";
            public IReadOnlyList<IStageSettings> Stages { get; }
            public IWaveRunner CreateRunner(IBattlefield battlefield) => new StagedWaveRunner(this, battlefield);
        }

        private sealed class Level : ILevelSettings
        {
            public Level(params IWaveSettings[] waves) => Waves = waves;
            public string DisplayName => "Level";
            public IReadOnlyList<IWaveSettings> Waves { get; }
        }

        /// <summary>A wave type without stages, as a boss wave would be: proves the level needs no changes for it.</summary>
        private sealed class StagelessWave : IWaveSettings, IWaveRunner
        {
            public string DisplayName => "Boss";
            public IWaveRunner CreateRunner(IBattlefield battlefield) => this;
#pragma warning disable 67
            public event Action<int, IStageSettings> StageStarted;
            public event Action<int, IStageSettings, EndReason> StageEnded;
#pragma warning restore 67
            public bool Defeated { get; set; }
            public bool IsComplete { get; private set; }
            public EndReason EndReason { get; private set; }
            public int StageCount => 0;
            public int StageIndex => -1;
            public StageRunner CurrentStage => null;
            public void Begin(int stageIndex) => IsComplete = false;

            public float Tick(float deltaTime)
            {
                if (Defeated) End(EndReason.Completed);
                return 0f;
            }

            public void EndStage(EndReason reason) => End(reason);
            public void JumpToStage(int stageIndex) { }

            public void End(EndReason reason)
            {
                if (IsComplete) return;
                IsComplete = true;
                EndReason = reason;
            }
        }

        private Field _field;
        private List<string> _events;

        [SetUp]
        public void SetUp()
        {
            _field = new Field();
            _events = new List<string>();
        }

        [Test]
        public void TimeUp_EndsExactlyAtItsDuration()
        {
            var stage = new StageRunner(new Stage(), _field);
            stage.Tick(9.9f);
            Assert.IsFalse(stage.IsComplete);
            stage.Tick(0.2f);
            Assert.IsTrue(stage.IsComplete);
            Assert.AreEqual(EndReason.Completed, stage.EndReason);
        }

        [Test]
        public void TimeUp_EndsEvenWithEnemiesAlive()
        {
            _field.AliveEnemies = 5;
            var stage = new StageRunner(new Stage(), _field);
            stage.Tick(10f);
            Assert.IsTrue(stage.IsComplete);
        }

        [Test]
        public void AllCleared_WaitsUntilEnemiesHaveAppeared()
        {
            var stage = new StageRunner(new Stage { EndMode = StageEndMode.AllCleared }, _field);
            stage.Tick(1f);
            Assert.IsFalse(stage.IsComplete, "Nothing has spawned yet.");
            _field.AliveEnemies = 2;
            stage.Tick(1f);
            _field.AliveEnemies = 0;
            stage.Tick(0.1f);
            Assert.IsTrue(stage.IsComplete);
            Assert.AreEqual(EndReason.Completed, stage.EndReason);
        }

        [Test]
        public void TimeUpThenCleared_WaitsForEnemiesAfterTheDuration()
        {
            _field.AliveEnemies = 1;
            var stage = new StageRunner(new Stage { EndMode = StageEndMode.TimeUpThenCleared }, _field);
            stage.Tick(12f);
            Assert.IsFalse(stage.IsComplete);
            Assert.AreEqual(1f, stage.Progress);
            _field.AliveEnemies = 0;
            stage.Tick(0.1f);
            Assert.AreEqual(EndReason.Completed, stage.EndReason);
            Assert.IsTrue(stage.IsComplete);
        }

        [TestCase(StageEndMode.AllCleared)]
        [TestCase(StageEndMode.TimeUpThenCleared)]
        public void WaitingForAClear_TimesOutSoTheLevelCannotHang(StageEndMode mode)
        {
            _field.AliveEnemies = 1;
            var stage = new StageRunner(new Stage { EndMode = mode }, _field);
            stage.Tick(19.9f);
            Assert.IsFalse(stage.IsComplete);
            stage.Tick(0.2f);
            Assert.IsTrue(stage.IsComplete);
            Assert.AreEqual(EndReason.TimedOut, stage.EndReason);
        }

        [Test]
        public void Level_PlaysEverythingInOrder()
        {
            var run = Run(new Level(new Wave(Named("A"), Named("B")) { DisplayName = "W1" },
                new Wave(Named("C")) { DisplayName = "W2" }));
            run.Begin();
            for (int i = 0; i < 400; i++) run.Tick(0.1f);
            CollectionAssert.AreEqual(new[]
            {
                "run", "+W1", "+A", "-A Completed", "+B", "-B Completed", "-W1 Completed",
                "+W2", "+C", "-C Completed", "-W2 Completed", "end Completed"
            }, _events);
            Assert.AreEqual(RunState.Ended, run.State);
            Assert.IsFalse(run.WasSkipped);
        }

        [Test]
        public void OneLongTick_CarriesLeftOverTimeIntoTheNextStage()
        {
            var run = Run(new Level(new Wave(Named("A"), Named("B")), new Wave(Named("C"))));
            run.Begin();
            run.Tick(23f);   // A 10 s, B 10 s, then 3 s into C
            Assert.AreEqual("C", run.CurrentStage.Settings.DisplayName);
            Assert.AreEqual(3f, run.CurrentStage.Elapsed, 1e-3f);
            Assert.AreEqual(23f, run.RunTime, 1e-3f);
        }

        [Test]
        public void SkipStage_RemovesEnemies_EndsAsSkipped_AndMarksTheRun()
        {
            var run = Run(new Level(new Wave(Named("A"), Named("B"))));
            run.Begin();
            _field.AliveEnemies = 4;
            Assert.IsTrue(run.EndStage(EndReason.Skipped));
            Assert.AreEqual(0, _field.AliveEnemies);
            Assert.AreEqual("B", run.CurrentStage.Settings.DisplayName);
            CollectionAssert.Contains(_events, "-A Skipped");
            Assert.IsTrue(run.WasSkipped);
        }

        [Test]
        public void FinishStage_EndsAsCompleted_AndOnlyClearsWhenTheRuleNeedsIt()
        {
            var run = Run(new Level(new Wave(Named("A"), Named("B", StageEndMode.TimeUpThenCleared), Named("C"))));
            run.Begin();
            _field.AliveEnemies = 4;
            run.EndStage(EndReason.Completed);
            Assert.AreEqual(4, _field.AliveEnemies, "A time-up stage ends with enemies still alive.");
            run.EndStage(EndReason.Completed);
            Assert.AreEqual(0, _field.AliveEnemies, "A stage that waits for a clear ends cleared.");
            CollectionAssert.Contains(_events, "-A Completed");
            CollectionAssert.Contains(_events, "-B Completed");
            Assert.IsFalse(run.WasSkipped);
        }

        [Test]
        public void SkipWave_GoesToTheNextWave_AndLastWaveEndsTheRun()
        {
            var run = Run(new Level(new Wave(Named("A"), Named("B")), new Wave(Named("C"))));
            run.Begin();
            run.EndWave(EndReason.Skipped);
            Assert.AreEqual(1, run.WaveIndex);
            run.EndWave(EndReason.Skipped);
            Assert.AreEqual(RunState.Ended, run.State);
            Assert.AreEqual(EndReason.Skipped, run.EndReason);
            Assert.IsNull(run.CurrentStage);
        }

        [Test]
        public void JumpTo_WithinAndAcrossWaves()
        {
            var run = Run(new Level(new Wave(Named("A"), Named("B")), new Wave(Named("C"), Named("D"))));
            run.Begin();
            Assert.IsTrue(run.JumpTo(0, 1));
            Assert.AreEqual("B", run.CurrentStage.Settings.DisplayName);
            Assert.IsTrue(run.JumpTo(1, 1));
            Assert.AreEqual("D", run.CurrentStage.Settings.DisplayName);
            Assert.IsTrue(run.JumpTo(0, 0));
            Assert.AreEqual("A", run.CurrentStage.Settings.DisplayName);
            Assert.IsFalse(run.JumpTo(5, 0));
            Assert.IsTrue(run.WasSkipped);
        }

        [Test]
        public void JumpTo_RestartsAnEndedRun()
        {
            var run = Run(new Level(new Wave(Named("A")), new Wave(Named("B"))));
            run.Begin();
            run.EndEarly(EndReason.Failed);
            Assert.IsTrue(run.JumpTo(1, 0));
            Assert.AreEqual(RunState.Running, run.State);
            Assert.AreEqual("B", run.CurrentStage.Settings.DisplayName);
        }

        [Test]
        public void Fail_EndsStageWaveAndRunAsFailed()
        {
            var run = Run(new Level(new Wave(Named("A")) { DisplayName = "W" }));
            run.Begin();
            run.EndEarly(EndReason.Failed);
            CollectionAssert.AreEqual(new[] { "run", "+W", "+A", "-A Failed", "-W Failed", "end Failed" }, _events);
            run.Tick(100f);
            Assert.IsFalse(run.EndStage(EndReason.Skipped), "Nothing to skip once the run has ended.");
        }

        [Test]
        public void Restart_StartsCleanAgain()
        {
            var run = Run(new Level(new Wave(Named("A"), Named("B"))));
            run.Begin();
            run.Tick(12f);
            run.EndStage(EndReason.Skipped);
            Assert.IsTrue(run.Begin());
            Assert.AreEqual(0f, run.RunTime);
            Assert.AreEqual("A", run.CurrentStage.Settings.DisplayName);
            Assert.IsFalse(run.WasSkipped);
        }

        [Test]
        public void CommandsFromInsideAnEvent_AreRefused()
        {
            var run = Run(new Level(new Wave(Named("A"), Named("B"), Named("C"))));
            bool? accepted = null;
            run.StageEnded += (_, __) => accepted = run.EndStage(EndReason.Skipped);
            run.Begin();
            run.EndStage(EndReason.Completed);
            Assert.AreEqual(false, accepted);
            Assert.AreEqual("B", run.CurrentStage.Settings.DisplayName);
        }

        [Test]
        public void AWaveTypeWithoutStages_NeedsNoChangesToTheLevel()
        {
            var boss = new StagelessWave();
            var run = Run(new Level(new Wave(Named("A")), boss));
            run.Begin();
            run.Tick(10f);
            Assert.AreSame(boss, run.CurrentWave);
            run.Tick(60f);
            Assert.AreEqual(RunState.Running, run.State);
            boss.Defeated = true;
            run.Tick(0.1f);
            Assert.AreEqual(RunState.Ended, run.State);
            Assert.AreEqual(EndReason.Completed, run.EndReason);
        }

        [Test]
        public void EmptyLevelOrWave_IsRefused()
        {
            Assert.Throws<ArgumentException>(() => new LevelRunner(new Level(), _field));
            Assert.Throws<ArgumentException>(() => new StagedWaveRunner(new Wave(), _field));
        }

        [Test]
        public void EveryEndMode_HasARule()
        {
            foreach (StageEndMode mode in Enum.GetValues(typeof(StageEndMode)))
                Assert.IsNotNull(StageEndRules.For(mode));
        }

        private static Stage Named(string name, StageEndMode mode = StageEndMode.TimeUp) =>
            new Stage { DisplayName = name, EndMode = mode };

        private LevelRunner Run(ILevelSettings level)
        {
            var run = new LevelRunner(level, _field);
            run.RunStarted += () => _events.Add("run");
            run.WaveStarted += w => _events.Add("+" + w.Name);
            run.StageStarted += s => _events.Add("+" + s.Name);
            run.StageEnded += (s, r) => _events.Add($"-{s.Name} {r}");
            run.WaveEnded += (w, r) => _events.Add($"-{w.Name} {r}");
            run.RunEnded += r => _events.Add("end " + r);
            return run;
        }
    }
}
