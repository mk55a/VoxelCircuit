using System.Collections.Generic;
using CylinderShooter.Core;
using CylinderShooter.Formations;
using UnityEngine;

namespace CylinderShooter.Enemies
{
    /// <summary>
    /// The scene's one store of enemies: one pool per enemy type, filled before play so spawning reuses objects instead
    /// of creating them. Also counts who is alive, per type, for the spawn gate. Enemies come back by themselves when
    /// they deactivate (killed, or removed by a skip).
    /// </summary>
    public sealed class EnemyPool : MonoBehaviour, IEnemyCensus, IKillFeed
    {
        [Tooltip("An inactive child object that holds unused enemies (inactive, so they don't wake up).")]
        [SerializeField] private Transform _inactiveRoot;
        [Tooltip("The object active enemies are placed under.")]
        [SerializeField] private Transform _activeRoot;

        private readonly Dictionary<EnemyDefinition, Stack<IFormationMember>> _free =
            new Dictionary<EnemyDefinition, Stack<IFormationMember>>();
        private readonly Dictionary<IFormationMember, EnemyDefinition> _home = new Dictionary<IFormationMember, EnemyDefinition>();
        private readonly Dictionary<IEnemyType, int> _alive = new Dictionary<IEnemyType, int>();
        private readonly HashSet<EnemyDefinition> _warnedGrowth = new HashSet<EnemyDefinition>();
        private System.Action<IFormationMember> _returnHandler;
        private System.Action<DestructionEvent> _destroyedHandler;

        /// <summary>An enemy from this pool was destroyed in play (not removed by a skip or a clear).</summary>
        public event System.Action EnemyDestroyed;

        public int TotalAlive { get; private set; }
        public bool IsReady { get; private set; }

        public int AliveOf(IEnemyType type) => type != null && _alive.TryGetValue(type, out int count) ? count : 0;

        /// <summary>Called by GameInstaller with every enemy the level can spawn. Returns false (with an error) if unusable.</summary>
        public bool Initialize(IEnumerable<EnemyDefinition> enemies)
        {
            if (_inactiveRoot == null || _activeRoot == null || _inactiveRoot.gameObject.activeSelf ||
                _inactiveRoot == _activeRoot)
            {
                Debug.LogError("EnemyPool needs 'Inactive Root' (an inactive child object) and 'Active Root' (an active " +
                               "object). Cylinder Shooter > Director > Add Spawning To Scene sets them up.", this);
                enabled = false;
                return false;
            }
            _returnHandler = Return;
            _destroyedHandler = HandleDestroyed;
            foreach (var enemy in enemies)
            {
                if (enemy == null || _free.ContainsKey(enemy)) continue;
                if (enemy.Prefab == null)
                {
                    Debug.LogError($"Enemy '{enemy.DisplayName}' has no usable 'Prefab' (it needs an enemy that can fly " +
                                   "in formations, e.g. Chaser or Bomber).", enemy);
                    continue;
                }
                var stack = new Stack<IFormationMember>(enemy.Prewarm);
                _free.Add(enemy, stack);
                for (int i = 0; i < enemy.Prewarm; i++) stack.Push(Create(enemy));
            }
            IsReady = true;
            return true;
        }

        /// <summary>
        /// An inactive enemy of this type, ready for <see cref="IFormationMember.PrepareSpawn"/>. Pass
        /// <see cref="ReturnHandler"/> as its owner. Creates one if the pool has run out (with a warning to raise Prewarm).
        /// </summary>
        public IFormationMember Take(EnemyDefinition enemy)
        {
            if (!IsReady || enemy == null || enemy.Prefab == null) return null;
            if (!_free.TryGetValue(enemy, out var stack))
            {
                stack = new Stack<IFormationMember>();
                _free.Add(enemy, stack);
            }
            if (stack.Count > 0) return stack.Pop();
            if (_warnedGrowth.Add(enemy))
                Debug.LogWarning($"The pool ran out of '{enemy.DisplayName}' and made more during play. Raise its " +
                                 "'Prewarm' to avoid this.", enemy);
            return Create(enemy);
        }

        /// <summary>Activates an enemy taken from the pool. Call after PrepareSpawn.</summary>
        public void Activate(IFormationMember member)
        {
            if (!_home.TryGetValue(member, out var enemy)) return;
            _alive[enemy] = AliveOf(enemy) + 1;
            TotalAlive++;
            member.gameObject.transform.SetParent(_activeRoot, false);
            member.gameObject.SetActive(true);
        }

        /// <summary>Gives an enemy that was taken but not activated back to the pool.</summary>
        public void Release(IFormationMember member)
        {
            if (member != null && _home.TryGetValue(member, out var enemy)) _free[enemy].Push(member);
        }

        /// <summary>The owner callback enemies call when they deactivate.</summary>
        public System.Action<IFormationMember> ReturnHandler => _returnHandler;

        private IFormationMember Create(EnemyDefinition enemy)
        {
            var instance = (IFormationMember)Instantiate((MonoBehaviour)enemy.Prefab, _inactiveRoot);
            instance.gameObject.SetActive(false);
            _home.Add(instance, enemy);
            // Subscribed once for the instance's whole life, so kills are counted without allocating per kill.
            if (instance is IDestructible destructible) destructible.Destroyed += _destroyedHandler;
            return instance;
        }

        private void HandleDestroyed(DestructionEvent _) => EnemyDestroyed?.Invoke();

        private void Return(IFormationMember member)
        {
            if (member == null || !_home.TryGetValue(member, out var enemy)) return;
            int alive = AliveOf(enemy);
            if (alive > 0)
            {
                _alive[enemy] = alive - 1;
                TotalAlive--;
            }
            // Not moved back under the inactive root: changing parents while an object is being deactivated fails when
            // its parent is the one deactivating (e.g. when the scene unloads). An inactive enemy can stay where it is.
            _free[enemy].Push(member);
        }
    }
}
