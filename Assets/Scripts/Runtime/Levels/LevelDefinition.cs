using System.Collections.Generic;
using CylinderShooter.Core;
using CylinderShooter.Enemies;
using UnityEngine;

namespace CylinderShooter.Levels
{
    /// <summary>
    /// A whole level: waves played from top to bottom, plus the level-wide rules of the spawn director (costs, variety,
    /// how the player is read, the hard enemy limit).
    /// </summary>
    [CreateAssetMenu(menuName = "Cylinder Shooter/Director/Level")]
    public sealed class LevelDefinition : ScriptableObject, ILevelSettings, IDirectorSettings
    {
        [Tooltip("Shown in the debugger. Empty = the asset's name.")]
        [SerializeField] private string _displayName;
        [Tooltip("Played from top to bottom.")]
        [SerializeField] private List<WaveDefinition> _waves = new List<WaveDefinition>();
        [Tooltip("Most enemies alive at once in this level, of all types together. A performance limit: the game is " +
                 "built for about 150.")]
        [SerializeField, Min(1)] private int _maxEnemiesAlive = 150;

        [Header("Director")]
        [Tooltip("What one enemy of each type costs in this level. Share a table between levels to keep costs the " +
                 "same, or give a level its own.")]
        [SerializeField] private EnemyCostTable _costs;
        [Tooltip("0 to 1: right after an enemy or formation is picked, its chance is multiplied by this, so the same " +
                 "one isn't picked again and again. 1 = no effect.")]
        [SerializeField, Range(0f, 1f)] private float _varietyDip = 0.3f;
        [Tooltip("Seconds for a picked enemy's or formation's chance to recover fully.")]
        [SerializeField, Min(0f)] private float _varietyRecovery = 8f;
        [Tooltip("0 to 1: how far a group's size may stray, at random, from the stage's Group Size.")]
        [SerializeField, Range(0f, 1f)] private float _groupSizeSpread = 0.2f;

        [Header("Reading the player")]
        [Tooltip("Seconds of the player's lane history that count when finding their favourite lane. Older time fades.")]
        [SerializeField, Min(0.1f)] private float _laneMemory = 8f;
        [Tooltip("Seconds a lane keeps the side its last group came from, when a stage doesn't allow pincers.")]
        [SerializeField, Min(0f)] private float _pincerWindow = 6f;
        [Tooltip("Recent kills (fading over Streak Window) that count as a hot streak. A hit ends the streak. 0 = off.")]
        [SerializeField, Min(0f)] private float _streakKills = 20f;
        [Tooltip("Seconds over which kills fade out of the streak.")]
        [SerializeField, Min(0.1f)] private float _streakWindow = 8f;
        [Tooltip("Credits per second are multiplied by this during a hot streak, so a player doing well gets more to " +
                 "destroy. 1 = no bonus.")]
        [SerializeField, Min(0f)] private float _streakBonus = 1.5f;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public IReadOnlyList<IWaveSettings> Waves => _waves;
        public int MaxEnemiesAlive => _maxEnemiesAlive;
        public ICostTable Costs => _costs != null ? _costs : null;
        public EnemyCostTable CostTable => _costs;
        public float VarietyDip => _varietyDip;
        public float VarietyRecovery => _varietyRecovery;
        public float GroupSizeSpread => _groupSizeSpread;
        public float LaneMemory => _laneMemory;
        public float PincerWindow => _pincerWindow;
        public float StreakKills => _streakKills;
        public float StreakWindow => _streakWindow;
        public float StreakBonus => _streakBonus;

        /// <summary>Every enemy any stage lists, each once. The pool prepares these before play.</summary>
        public void CollectEnemies(List<EnemyDefinition> into)
        {
            for (int w = 0; w < _waves.Count; w++)
                if (_waves[w] != null) _waves[w].CollectEnemies(into);
        }

        /// <summary>Design mistakes that don't stop the level from playing, worded for the designer.</summary>
        public void CollectWarnings(List<string> into)
        {
            var checkedStages = new HashSet<StageDefinition>();
            for (int w = 0; w < _waves.Count; w++)
            {
                if (_waves[w] == null) continue;
                var stages = _waves[w].StageDefinitions;
                for (int s = 0; s < stages.Count; s++)
                    if (stages[s] != null && checkedStages.Add(stages[s])) StageWarnings(stages[s], into);
            }
        }

        private void StageWarnings(StageDefinition stage, List<string> into)
        {
            string name = $"Stage '{stage.DisplayName}'";
            if (stage.StartingCredits <= 0f && stage.CreditsPerSecond <= 0f)
                into.Add($"{name} has no Starting Credits and no Credits Per Second, so nothing will spawn.");
            bool anyOn = false;
            var rows = stage.EnemyEntries;
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null || !row.Enabled || row.Weight <= 0f) continue;
                var enemy = row.Definition;
                if (enemy == null)
                {
                    into.Add($"{name} has an enemy row with no enemy (element {i}).");
                    continue;
                }
                anyOn = true;
                int smallest = int.MaxValue;
                foreach (var option in enemy.FormationOptions)
                    if (option != null && option.Weight > 0f) smallest = Mathf.Min(smallest, option.MinCount);
                if (smallest == int.MaxValue)
                {
                    into.Add($"Enemy '{enemy.DisplayName}' is on in {name} but has no formations (add some to its 'Formations').");
                    continue;
                }
                if (_costs == null || !_costs.TryGetCost(enemy, out float cost))
                {
                    into.Add($"Enemy '{enemy.DisplayName}' is on in {name} but has no cost in this level's 'Costs' table, " +
                             "so it can't be bought.");
                    continue;
                }
                if (stage.CreditCap > 0f && smallest * cost > stage.CreditCap + 1e-4f)
                    into.Add($"In {name}, the smallest '{enemy.DisplayName}' group ({smallest} x {cost} = {smallest * cost} " +
                             $"credits) costs more than the Credit Cap ({stage.CreditCap}), so it can never be bought.");
                int limit = row.MaxAlive > 0 ? row.MaxAlive : enemy.MaxAlive;
                if (stage.MaxAlive > 0) limit = limit > 0 ? Mathf.Min(limit, stage.MaxAlive) : stage.MaxAlive;
                if (limit > 0 && limit < smallest)
                    into.Add($"In {name}, '{enemy.DisplayName}' may have {limit} alive but its smallest group is {smallest}, " +
                             "so it can never spawn.");
            }
            if (!anyOn) into.Add($"{name} has no enemy switched on with a weight above 0, so nothing will spawn.");
        }

        /// <summary>False, with a reason to show the designer, when the level can't be played.</summary>
        public bool Validate(out string reason)
        {
            if (_waves.Count == 0)
            {
                reason = $"Level '{DisplayName}' has no waves. Add at least one to its 'Waves' list.";
                return false;
            }
            for (int i = 0; i < _waves.Count; i++)
            {
                if (_waves[i] == null)
                {
                    reason = $"Level '{DisplayName}' has an empty slot in 'Waves' (element {i}). Assign a wave or remove it.";
                    return false;
                }
                if (!_waves[i].Validate(out reason)) return false;
            }
            reason = null;
            return true;
        }
    }
}
