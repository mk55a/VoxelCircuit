using System.Collections.Generic;
using System.Text;
using CylinderShooter.Core;
using CylinderShooter.Debugging;
using CylinderShooter.Enemies;
using CylinderShooter.Levels;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CylinderShooter.Editor
{
    /// <summary>
    /// Play mode control of the running level: where it is, what the spawn director is doing (credits, plan, the
    /// player's lane and streak), and skip, finish, jump, restart, pause and spawn. Talks only to <see cref="IRunDebug"/>.
    /// Outside Play mode it offers to keep or revert director assets changed while playing.
    /// </summary>
    public sealed class LevelDebuggerWindow : EditorWindow
    {
        private const long RefreshMilliseconds = 100;
        private const int LogLines = 30;

        private readonly List<string> _log = new List<string>();
        private readonly List<Button> _jumpButtons = new List<Button>();
        private readonly List<Vector2Int> _jumpTargets = new List<Vector2Int>();
        private IRunDebug _debug;
        private VisualElement _playing;
        private VisualElement _changes;
        private VisualElement _jumpList;
        private DirectorGraph _graph;
        private Label _director;
        private Toggle _freezeCredits;
        private int _graphStage = -1;
        private int _graphWave = -1;
        private Label _notice;
        private Label _changesLabel;
        private Label _headline;
        private Label _status;
        private Label _logLabel;
        private Toggle _pause;
        private PopupField<EnemyDefinition> _enemyPicker;
        private VisualElement _formationHolder;
        private PopupField<EnemyFormationOption> _formationPicker;
        private IntegerField _count;
        private Toggle _ignoreStageList;
        private Label _spawnMessage;
        private Slider _timeScale;

        [MenuItem("Cylinder Shooter/Debug/Level Debugger")]
        public static void Open() => GetWindow<LevelDebuggerWindow>("Level Debugger");

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>(ProjectPaths.LevelDebuggerStyle);
            if (style != null) rootVisualElement.styleSheets.Add(style);

            var scroll = new ScrollView();
            scroll.AddToClassList("debugger");
            rootVisualElement.Add(scroll);

            _notice = AddLabel(scroll, "notice");
            BuildChanges(scroll);
            _playing = new VisualElement();
            scroll.Add(_playing);
            BuildStatus();
            BuildDirector();
            BuildControls();
            BuildSpawn();
            _playing.Add(Title("Jump to"));
            _jumpList = new VisualElement();
            _playing.Add(_jumpList);
            _playing.Add(Title("Log (newest first)"));
            _logLabel = AddLabel(_playing, "log");

            rootVisualElement.schedule.Execute(Refresh).Every(RefreshMilliseconds);
            Refresh();
        }

        private void OnEnable() => EditorApplication.playModeStateChanged += HandlePlayModeChanged;

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
            Bind(null);
        }

        private void HandlePlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                Time.timeScale = 1f;
                Bind(null);
            }
            if (_notice != null) Refresh();
        }

        private void BuildChanges(VisualElement parent)
        {
            _changes = new VisualElement();
            parent.Add(_changes);
            _changesLabel = AddLabel(_changes, "notice");
            var row = Row(_changes);
            row.Add(new Button(() => { DirectorTuningSnapshot.Keep(); Refresh(); }) { text = "Keep changes" });
            row.Add(new Button(() => { DirectorTuningSnapshot.Revert(); Refresh(); }) { text = "Revert changes" });
        }

        private void BuildStatus()
        {
            _headline = AddLabel(_playing, "headline");
            _status = AddLabel(_playing, "status");
            _playing.Add(Title("Director (orange: credits / cap, cyan: alive / limit, ticks: groups)"));
            _graph = new DirectorGraph();
            _playing.Add(_graph);
            _director = AddLabel(_playing, "status");
        }

        private void BuildDirector()
        {
            var row = Row(_playing);
            row.Add(Director("Give 10 credits", d => { d.GiveCredits(10f); return true; }));
            row.Add(Director("Spawn plan now", d => d.SpawnPlanNow()));
            row.Add(Director("New plan", d => { d.NewPlan(); return true; }));
            _freezeCredits = new Toggle("Freeze credits") { tooltip = "Stop earning credits, to watch one moment." };
            _freezeCredits.RegisterValueChangedCallback(e =>
            {
                if (_debug?.Director != null) _debug.Director.FreezeCredits = e.newValue;
            });
            _playing.Add(_freezeCredits);
        }

        private Button Director(string text, System.Func<SpawnDirector, bool> command)
        {
            return new Button(() =>
            {
                var director = _debug?.Director;
                if (director == null) Log("This scene has no Enemy Spawner, so there is no director");
                else if (!command(director)) Log($"'{text}' isn't possible right now");
                Refresh();
            }) { text = text };
        }

        private void BuildControls()
        {
            _playing.Add(Title("Skip: move on, no rewards"));
            var skip = Row(_playing);
            skip.Add(Command("Skip stage", d => d.SkipStage()));
            skip.Add(Command("Skip wave", d => d.SkipWave()));

            _playing.Add(Title("Finish: end as if the player did it"));
            var finish = Row(_playing);
            finish.Add(Command("Finish stage", d => d.FinishStage()));
            finish.Add(Command("Finish wave", d => d.FinishWave()));

            _playing.Add(Title("Run"));
            var run = Row(_playing);
            run.Add(Command("Restart stage", d => d.RestartStage()));
            run.Add(Command("Restart run", d => d.RestartRun()));
            run.Add(Command("Fail run", d => d.FailRun()));
            run.Add(Command("Clear enemies", d => { d.ClearEnemies(); return true; }));

            _pause = new Toggle("Pause level clock")
            {
                tooltip = "Stops the level's time. The game itself keeps running."
            };
            _pause.RegisterValueChangedCallback(e => { if (_debug != null) _debug.IsPaused = e.newValue; });
            _playing.Add(_pause);

            _timeScale = new Slider("Game speed", 0f, 4f) { value = 1f, showInputField = true };
            _timeScale.RegisterValueChangedCallback(e =>
            {
                if (EditorApplication.isPlaying) Time.timeScale = Mathf.Max(0f, e.newValue);
            });
            _playing.Add(_timeScale);
        }

        private void BuildSpawn()
        {
            _playing.Add(Title("Spawn a group now (free, just off-screen, in the player's lane)"));
            var enemies = new List<EnemyDefinition>();
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition"))
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (enemy != null) enemies.Add(enemy);
            }
            if (enemies.Count == 0)
            {
                AddLabel(_playing, "status").text = "No enemy assets yet (Cylinder Shooter > Director > Create Spawn Test Level).";
                return;
            }
            _enemyPicker = new PopupField<EnemyDefinition>("Enemy", enemies, 0, e => e.DisplayName, e => e.DisplayName);
            _enemyPicker.RegisterValueChangedCallback(_ => BuildFormationPicker());
            _playing.Add(_enemyPicker);
            _formationHolder = new VisualElement();
            _playing.Add(_formationHolder);
            BuildFormationPicker();
            _count = new IntegerField("Count") { value = 8 };
            _playing.Add(_count);
            _ignoreStageList = new Toggle("Ignore the stage's enemy list")
            {
                tooltip = "Spawn it even where the stage doesn't switch this enemy on. The enemy's own limit and the " +
                          "level's limit still apply."
            };
            _playing.Add(_ignoreStageList);
            var row = Row(_playing);
            row.Add(new Button(SpawnSelected) { text = "Spawn" });
            _spawnMessage = AddLabel(_playing, "status");
        }

        private void BuildFormationPicker()
        {
            _formationHolder.Clear();
            _formationPicker = null;
            var options = new List<EnemyFormationOption>();
            var enemy = _enemyPicker?.value;
            if (enemy != null)
                foreach (var option in enemy.FormationOptions)
                    if (option?.Formation != null) options.Add(option);
            if (options.Count == 0)
            {
                AddLabel(_formationHolder, "status").text = "This enemy has no formations (add some to its 'Formations').";
                return;
            }
            _formationPicker = new PopupField<EnemyFormationOption>("Formation", options, 0,
                o => o.DisplayName, o => o.DisplayName);
            _formationHolder.Add(_formationPicker);
        }

        private void SpawnSelected()
        {
            if (_debug == null || _enemyPicker?.value == null || _formationPicker?.value == null) return;
            var enemy = _enemyPicker.value;
            string problem = _debug.SpawnGroup(enemy, _formationPicker.value, Mathf.Max(1, _count.value),
                _ignoreStageList.value);
            string result = problem == null
                ? $"Spawned {_count.value} '{enemy.DisplayName}' in '{_formationPicker.value.DisplayName}'"
                : $"Not spawned: {problem}";
            _spawnMessage.text = result;
            Log(result);
        }

        private void Refresh()
        {
            IRunDebug current = null;
            if (EditorApplication.isPlaying) ServiceLocator.TryGet(out current);
            if (!ReferenceEquals(current, _debug)) Bind(current);

            SetVisible(_playing, _debug != null);
            SetVisible(_notice, _debug == null);
            RefreshChanges();
            if (_debug == null)
            {
                _notice.text = EditorApplication.isPlaying
                    ? "No level is running. The scene needs a Level Manager: stop Play, then use " +
                      "Cylinder Shooter > Director > Add Level Manager To Scene."
                    : "Press Play in a scene with a Level Manager.";
                return;
            }
            RefreshStatus();
        }

        private void RefreshChanges()
        {
            var changed = EditorApplication.isPlayingOrWillChangePlaymode ? null : DirectorTuningSnapshot.FindChanges();
            bool any = changed != null && changed.Count > 0;
            SetVisible(_changes, any);
            if (!any) return;
            var text = new StringBuilder("Director assets changed during Play:");
            foreach (string path in changed) text.Append("\n  ").Append(System.IO.Path.GetFileNameWithoutExtension(path));
            _changesLabel.text = text.ToString();
        }

        private void RefreshStatus()
        {
            var run = _debug.Run;
            var stage = run.CurrentStage;
            _headline.text = run.State == RunState.Running
                ? $"{run.Name}: wave {run.WaveIndex + 1}/{run.WaveCount} '{run.WaveName}'"
                : run.State == RunState.Ended ? $"{run.Name}: ended ({run.EndReason})" : $"{run.Name}: not started";

            var text = new StringBuilder();
            if (stage != null)
            {
                var settings = stage.Settings;
                text.Append($"Stage {run.CurrentWave.StageIndex + 1}/{run.CurrentWave.StageCount} '{settings.DisplayName}'\n");
                text.Append($"{stage.Elapsed:0.0} / {settings.Duration:0.0} s, ends: {Describe(stage)}\n");
            }
            text.Append($"Enemies alive {_debug.AliveEnemies}\n");
            text.Append($"Run time {run.RunTime:0.0} s");
            if (run.WasSkipped) text.Append(" (parts skipped, not a real playthrough)");
            _status.text = text.ToString();

            _pause.SetValueWithoutNotify(_debug.IsPaused);
            _timeScale.SetValueWithoutNotify(Time.timeScale);
            RefreshDirector(run, stage);
            int stageIndex = run.CurrentWave?.StageIndex ?? -1;
            for (int i = 0; i < _jumpButtons.Count; i++)
                _jumpButtons[i].EnableInClassList("jump-button--current", run.State == RunState.Running &&
                    _jumpTargets[i].x == run.WaveIndex && _jumpTargets[i].y == stageIndex);
        }

        private void RefreshDirector(LevelRunner run, StageRunner stage)
        {
            var director = _debug.Director;
            if (director == null)
            {
                _director.text = "No director: this scene has no Enemy Spawner (Cylinder Shooter > Director > Add Spawning To Scene).";
                return;
            }
            _freezeCredits.SetValueWithoutNotify(director.FreezeCredits);
            int waveIndex = run.WaveIndex;
            int stageIndex = run.CurrentWave?.StageIndex ?? -1;
            if (waveIndex != _graphWave || stageIndex != _graphStage)
            {
                _graph.Clear();
                _graphWave = waveIndex;
                _graphStage = stageIndex;
            }
            var settings = stage?.Settings;
            if (settings != null && run.State == RunState.Running)
            {
                float cap = settings.CreditCap > 0f ? settings.CreditCap : Mathf.Max(1f, director.Credits);
                int limit = settings.MaxAlive > 0 ? settings.MaxAlive : director.MaxEnemiesAlive;
                _graph.Add(run.RunTime, director.Credits / cap, _debug.AliveEnemies / (float)Mathf.Max(1, limit));
            }

            var text = new StringBuilder();
            text.Append($"Credits {director.Credits:0.0}");
            if (settings != null && settings.CreditCap > 0f) text.Append($" / {settings.CreditCap:0}");
            text.Append($", earning {director.EarnRate:0.0}/s");
            if (director.IsHot) text.Append(" (HOT STREAK bonus)");
            text.Append($"\nDoing: {Describe(director)}");
            var plan = director.Plan;
            if (plan.IsSet)
            {
                text.Append($"\nPlan: {plan.Count} {plan.Enemy.DisplayName} in {plan.Formation.DisplayName}, ");
                text.Append($"{plan.Cost:0.#} credits, {LaneName(plan.Lane)} lane, from the ");
                text.Append(plan.ScreenSide > 0 ? "right" : "left");
                if (plan.Entry != null && plan.Entry.Style == EntryStyle.AtDistance)
                    text.Append($", {plan.Entry.Distance:0} m away");
                text.Append($", waited {plan.Age:0.0} s");
            }
            var lanes = director.Lanes;
            int favourite = lanes.Favourite;
            text.Append($"\nPlayer: favourite lane {LaneName(favourite)} ({lanes.Share(favourite) * 100f:0}% of recent time), ");
            text.Append($"streak {director.Streak.Score:0.0} recent kills");
            if (director.LastProblem != null) text.Append($"\nLast problem: {director.LastProblem}");
            _director.text = text.ToString();
        }

        private static string Describe(SpawnDirector director)
        {
            switch (director.State)
            {
                case DirectorState.Idle: return "nothing (no stage running)";
                case DirectorState.StoppedBuying: return "stopped buying; waiting for the stage to be cleared";
                case DirectorState.NothingEnabled: return "nothing to buy: no enemy is on with a weight, formations and a cost";
                case DirectorState.StageFull: return "waiting for room (a limit is reached)";
                case DirectorState.TooExpensive: return "nothing affordable: the smallest group costs more than the credit cap";
                case DirectorState.Saving: return "saving for the plan";
                case DirectorState.CoolingDown: return $"cooling down ({director.CooldownLeft:0.0} s)";
                case DirectorState.NoPlace: return "can afford the plan, looking for a free place";
                default: return "spawned a group";
            }
        }

        private static string LaneName(int lane) => lane <= 0 ? "bottom" : lane == 1 ? "middle" : "top";

        private static string Describe(StageRunner stage)
        {
            switch (stage.Settings.EndMode)
            {
                case StageEndMode.AllCleared: return $"all cleared (limit {Limit(stage):0} s)";
                case StageEndMode.TimeUpThenCleared: return $"time up, then cleared (limit {Limit(stage):0} s)";
                default: return "time up";
            }
        }

        private static float Limit(StageRunner stage) => Mathf.Max(stage.Settings.MaxDuration, stage.Settings.Duration);

        private void Bind(IRunDebug debug)
        {
            if (_debug != null)
            {
                var old = _debug.Run;
                old.RunStarted -= LogRunStarted;
                old.WaveStarted -= LogWaveStarted;
                old.StageStarted -= LogStageStarted;
                old.StageEnded -= LogStageEnded;
                old.WaveEnded -= LogWaveEnded;
                old.RunEnded -= LogRunEnded;
                if (_debug.Director != null) _debug.Director.GroupSpawned -= LogGroupSpawned;
            }
            _debug = debug;
            _log.Clear();
            if (_logLabel != null) _logLabel.text = string.Empty;
            BuildJumpList();
            if (_debug == null) return;
            var run = _debug.Run;
            run.RunStarted += LogRunStarted;
            run.WaveStarted += LogWaveStarted;
            run.StageStarted += LogStageStarted;
            run.StageEnded += LogStageEnded;
            run.WaveEnded += LogWaveEnded;
            run.RunEnded += LogRunEnded;
            if (_debug.Director != null) _debug.Director.GroupSpawned += LogGroupSpawned;
            _graph?.Clear();
            _graphWave = -1;
            _graphStage = -1;
        }

        private void BuildJumpList()
        {
            if (_jumpList == null) return;
            _jumpList.Clear();
            _jumpButtons.Clear();
            _jumpTargets.Clear();
            if (_debug == null) return;
            var waves = _debug.Run.Waves;
            for (int w = 0; w < waves.Count; w++)
            {
                AddLabel(_jumpList, "status").text = $"{w + 1}. {waves[w].DisplayName}";
                if (waves[w] is IStagedWaveSettings staged)
                {
                    for (int s = 0; s < staged.Stages.Count; s++)
                        AddJump(w, s, $"{w + 1}.{s + 1}  {staged.Stages[s].DisplayName}");
                }
                else
                {
                    AddJump(w, 0, $"{w + 1}  Start wave");
                }
            }
        }

        private void AddJump(int wave, int stage, string text)
        {
            var button = Command(text, d => d.JumpTo(wave, stage));
            button.AddToClassList("jump-button");
            _jumpList.Add(button);
            _jumpButtons.Add(button);
            _jumpTargets.Add(new Vector2Int(wave, stage));
        }

        private Button Command(string text, System.Func<IRunDebug, bool> command)
        {
            return new Button(() =>
            {
                if (_debug == null) return;
                if (!command(_debug)) Log($"'{text}' isn't possible right now");
                Refresh();
            }) { text = text };
        }

        private void LogRunStarted() => Log("Run started");
        private void LogWaveStarted(WaveInfo wave) => Log($"Wave '{wave.Name}' started");
        private void LogStageStarted(StageInfo stage) => Log($"  Stage '{stage.Name}' started");

        private void LogGroupSpawned(GroupPlan plan)
        {
            _graph?.MarkSpawn(_debug != null ? _debug.Run.RunTime : 0f);
            Log($"    {plan.Count} {plan.Enemy.DisplayName} in {plan.Formation.DisplayName} ({plan.Cost:0.#} credits), " +
                $"{LaneName(plan.Lane)} lane, {(plan.ScreenSide > 0 ? "right" : "left")}");
        }
        private void LogStageEnded(StageInfo stage, EndReason reason) => Log($"  Stage '{stage.Name}' ended: {reason}");
        private void LogWaveEnded(WaveInfo wave, EndReason reason) => Log($"Wave '{wave.Name}' ended: {reason}");
        private void LogRunEnded(EndReason reason) => Log($"Run ended: {reason}");

        private void Log(string line)
        {
            _log.Insert(0, $"{(_debug != null ? _debug.Run.RunTime : 0f):0.0}s  {line}");
            if (_log.Count > LogLines) _log.RemoveAt(_log.Count - 1);
            if (_logLabel != null) _logLabel.text = string.Join("\n", _log);
        }

        private static Label Title(string text)
        {
            var label = new Label(text);
            label.AddToClassList("section-title");
            return label;
        }

        private static Label AddLabel(VisualElement parent, string className)
        {
            var label = new Label();
            label.AddToClassList(className);
            parent.Add(label);
            return label;
        }

        private static VisualElement Row(VisualElement parent)
        {
            var row = new VisualElement();
            row.AddToClassList("row");
            parent.Add(row);
            return row;
        }

        private static void SetVisible(VisualElement element, bool visible) =>
            element.EnableInClassList("hidden", !visible);
    }
}
