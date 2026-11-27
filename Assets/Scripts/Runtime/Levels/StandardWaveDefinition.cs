using System.Collections.Generic;
using CylinderShooter.Core;
using CylinderShooter.Enemies;
using UnityEngine;

namespace CylinderShooter.Levels
{
    /// <summary>The normal wave: stages played from top to bottom.</summary>
    [CreateAssetMenu(menuName = "Cylinder Shooter/Director/Standard Wave")]
    public sealed class StandardWaveDefinition : WaveDefinition, IStagedWaveSettings
    {
        [Tooltip("Played from top to bottom. The same stage can be used more than once.")]
        [SerializeField] private List<StageDefinition> _stages = new List<StageDefinition>();

        public IReadOnlyList<IStageSettings> Stages => _stages;
        public override IReadOnlyList<StageDefinition> StageDefinitions => _stages;

        public override IWaveRunner CreateRunner(IBattlefield battlefield) => new StagedWaveRunner(this, battlefield);

        public override void CollectEnemies(List<EnemyDefinition> into)
        {
            for (int s = 0; s < _stages.Count; s++)
            {
                if (_stages[s] == null) continue;
                var rows = _stages[s].EnemyEntries;
                for (int r = 0; r < rows.Count; r++)
                {
                    var enemy = rows[r]?.Definition;
                    if (enemy != null && !into.Contains(enemy)) into.Add(enemy);
                }
            }
        }

        public override bool Validate(out string reason)
        {
            if (_stages.Count == 0)
            {
                reason = $"Wave '{DisplayName}' has no stages. Add at least one to its 'Stages' list.";
                return false;
            }
            for (int i = 0; i < _stages.Count; i++)
            {
                if (_stages[i] != null) continue;
                reason = $"Wave '{DisplayName}' has an empty slot in 'Stages' (element {i}). Assign a stage or remove it.";
                return false;
            }
            reason = null;
            return true;
        }
    }
}
