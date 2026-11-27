using CylinderShooter.Core;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CylinderShooter.Debugging;
#endif

namespace CylinderShooter.Levels
{
    /// <summary>
    /// The only Unity component of the level system: builds the run and the spawn director from the level asset and
    /// gives them the time each frame. The rules live in LevelRunner, the wave runners, StageRunner and SpawnDirector,
    /// which are plain C#. Set up by GameInstaller, so its services exist before anything else wakes up.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelManager : MonoBehaviour
    {
        [Tooltip("The level to play (e.g. Assets/Tuning/Director/SpawnTest/Level_SpawnTest).")]
        [SerializeField] private LevelDefinition _level;
        [Tooltip("Start the level as soon as the scene starts. Off = something else starts it.")]
        [SerializeField] private bool _startAutomatically = true;

        public LevelRunner Run { get; private set; }

        /// <summary>Decides what to spawn, how many, where and when. Null when the scene has no spawner.</summary>
        public SpawnDirector Director { get; private set; }

        public LevelDefinition Level => _level;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public IRunDebug RunDebug { get; private set; }
#endif

        /// <summary>
        /// Called by GameInstaller. Without a spawner and census the level still runs, but nothing spawns. The player
        /// watch and kill feed are optional: without them the director doesn't follow lanes or streaks. Returns false
        /// (with an error) when the level can't be played.
        /// </summary>
        public bool Initialize(IBattlefield battlefield, IEnemySpawner spawner = null, IEnemyCensus census = null,
            IPlayerWatch player = null, IKillFeed kills = null)
        {
            if (_level == null)
            {
                Debug.LogError("LevelManager needs a level asset in 'Level' (e.g. Assets/Tuning/Director/SpawnTest/" +
                               "Level_SpawnTest).", this);
                enabled = false;
                return false;
            }
            if (!_level.Validate(out string reason))
            {
                Debug.LogError(reason, _level);
                enabled = false;
                return false;
            }
            var warnings = new System.Collections.Generic.List<string>();
            _level.CollectWarnings(warnings);
            foreach (string warning in warnings) Debug.LogWarning(warning, _level);
            Run = new LevelRunner(_level, battlefield);
            if (spawner != null && census != null)
            {
                var cylinder = ServiceLocator.Get<CylinderConfig>();
                var lanes = new LaneTracker(cylinder.MinHeight, cylinder.MaxHeight);
                Director = new SpawnDirector(Run, spawner, new SpawnGate(census, _level.MaxEnemiesAlive), census, _level,
                    new SeededRandom(System.Environment.TickCount), lanes, player, kills);
                Director.GroupDropped += problem => Debug.LogWarning($"The director gave up a group: {problem}.", this);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RunDebug = new RunDebug(Run, battlefield, Director);
#endif
            return true;
        }

        private void Start()
        {
            if (Run == null)
            {
                Debug.LogError("LevelManager was not set up. Drag this object into 'Level Manager' on the scene's " +
                               "GameInstaller.", this);
                enabled = false;
                return;
            }
            // Start, not Awake: listeners have subscribed by now and hear the first events.
            if (_startAutomatically && Run.State == RunState.NotStarted) Run.Begin();
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (RunDebug != null && RunDebug.IsPaused) return;
#endif
            Run.Tick(Time.deltaTime);
            Director?.Tick(Time.deltaTime);
        }

        private void OnDestroy() => Director?.Dispose();
    }
}
