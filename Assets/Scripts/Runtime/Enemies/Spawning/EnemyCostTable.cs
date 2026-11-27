using System;
using System.Collections.Generic;
using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Enemies
{
    /// <summary>
    /// What one enemy of each type costs the director. A level points at one table: levels that should feel alike share
    /// a table, a level that should differ gets its own. An enemy missing from the table can't be bought.
    /// </summary>
    [CreateAssetMenu(menuName = "Cylinder Shooter/Director/Enemy Cost Table")]
    public sealed class EnemyCostTable : ScriptableObject, ICostTable
    {
        [Tooltip("Credits for one enemy. A group costs count x cost. Think of it as how dangerous one of them is.")]
        [SerializeField] private List<EnemyCost> _costs = new List<EnemyCost>();

        public IReadOnlyList<EnemyCost> Costs => _costs;

        public bool TryGetCost(IEnemyType type, out float cost)
        {
            for (int i = 0; i < _costs.Count; i++)
            {
                var entry = _costs[i];
                if (entry == null || entry.Enemy == null || !ReferenceEquals(entry.Enemy, type)) continue;
                cost = entry.Cost;
                return cost > 0f;
            }
            cost = 0f;
            return false;
        }
    }

    [Serializable]
    public sealed class EnemyCost
    {
        [SerializeField] private EnemyDefinition _enemy;
        [Tooltip("Credits for one of this enemy. Must be above 0.")]
        [SerializeField, Min(0f)] private float _cost = 1f;

        public EnemyCost() { }

        public EnemyCost(EnemyDefinition enemy, float cost)
        {
            _enemy = enemy;
            _cost = cost;
        }

        public EnemyDefinition Enemy => _enemy;
        public float Cost => _cost;
    }
}
