using System.Collections.Generic;
using CylinderShooter.Core;
using CylinderShooter.Effects;
using CylinderShooter.Enemies;
using CylinderShooter.Levels;
using CylinderShooter.Ship;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CylinderShooter.Debugging;
#endif

namespace CylinderShooter.Combat
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameInstaller : MonoBehaviour
    {
        [SerializeField] private CylinderConfig _cylinder;
        [SerializeField] private BulletPool _bulletPool;
        [Tooltip("Optional: shared voxel debris for destruction effects.")]
        [SerializeField] private VoxelDebris _debris;
        [Tooltip("Optional: shared energy pulses for destruction effects.")]
        [SerializeField] private PulseEmitter _pulses;
        [Tooltip("Optional: the scene's shockwave (the player's special move).")]
        [SerializeField] private ShockwaveEmitter _shockwave;
        [Tooltip("Optional: runs the scene's level (waves and stages).")]
        [SerializeField] private LevelManager _levelManager;
        [Tooltip("Optional: the scene's enemy pool, prepared with every enemy the level can spawn.")]
        [SerializeField] private EnemyPool _enemyPool;
        [Tooltip("Optional: brings the director's groups into the arena. Needs the Enemy Pool.")]
        [SerializeField] private EnemySpawner _enemySpawner;

        TargetRegistry _targets;
        ContactHazardRegistry _hazards;
        // Kept separately: LevelManager may already be destroyed when this is.
        IRunEvents _runEvents;
        PlayerWatch _playerWatch;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        IRunDebug _runDebug;
#endif

        void Awake()
        {
            _targets = new TargetRegistry();
            _hazards = new ContactHazardRegistry();
            _bulletPool.Initialize(_cylinder.Radius, _targets);

            ServiceLocator.Register(_cylinder);
            ServiceLocator.Register<ITargetRegistry>(_targets);
            ServiceLocator.Register<IBulletPool>(_bulletPool);
            ServiceLocator.Register(_hazards);

            if (_debris != null)
            {
                _debris.Initialize(_cylinder);
                ServiceLocator.Register<IDebrisEmitter>(_debris);
            }
            if (_pulses != null) ServiceLocator.Register<IPulseEmitter>(_pulses);
            if (_shockwave != null) ServiceLocator.Register<IShockwaveEmitter>(_shockwave);

            bool canSpawn = false;
            if (_levelManager != null && _enemyPool != null && _enemySpawner != null && _levelManager.Level != null)
            {
                var enemies = new List<EnemyDefinition>();
                _levelManager.Level.CollectEnemies(enemies);
                canSpawn = _enemyPool.Initialize(enemies);
            }
            if (canSpawn && _enemySpawner.Ship != null)
                _playerWatch = new PlayerWatch(_enemySpawner.Ship, _enemySpawner.Ship.GetComponent<PlayerLife>());
            if (_levelManager != null && _levelManager.Initialize(new HazardBattlefield(_hazards),
                    canSpawn ? _enemySpawner : null, canSpawn ? _enemyPool : null, _playerWatch,
                    canSpawn ? _enemyPool : null))
            {
                _runEvents = _levelManager.Run;
                ServiceLocator.Register(_runEvents);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                _runDebug = _levelManager.RunDebug;
                ServiceLocator.Register(_runDebug);
#endif
            }
        }

        void OnDestroy()
        {
            ServiceLocator.Unregister(_cylinder);
            ServiceLocator.Unregister<ITargetRegistry>(_targets);
            ServiceLocator.Unregister<IBulletPool>(_bulletPool);
            ServiceLocator.Unregister(_hazards);
            if (_debris != null) ServiceLocator.Unregister<IDebrisEmitter>(_debris);
            if (_pulses != null) ServiceLocator.Unregister<IPulseEmitter>(_pulses);
            if (_shockwave != null) ServiceLocator.Unregister<IShockwaveEmitter>(_shockwave);
            if (_runEvents != null) ServiceLocator.Unregister(_runEvents);
            _playerWatch?.Dispose();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_runDebug != null) ServiceLocator.Unregister(_runDebug);
#endif
        }
    }
}
