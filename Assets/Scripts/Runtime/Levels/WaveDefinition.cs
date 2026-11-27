using System.Collections.Generic;
using CylinderShooter.Core;
using CylinderShooter.Enemies;
using UnityEngine;

namespace CylinderShooter.Levels
{
    /// <summary>
    /// Base for every wave type. A new type (for example a boss wave) is a new subclass that creates its own runner;
    /// levels and the other wave types don't change.
    /// </summary>
    public abstract class WaveDefinition : ScriptableObject, IWaveSettings
    {
        [Tooltip("Shown in the debugger and in events. Empty = the asset's name.")]
        [SerializeField] private string _displayName;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;

        public abstract IWaveRunner CreateRunner(IBattlefield battlefield);

        /// <summary>False, with a reason to show the designer, when the wave can't be played.</summary>
        public abstract bool Validate(out string reason);

        /// <summary>Every enemy this wave can spawn, each once, so the pool can prepare them before play.</summary>
        public abstract void CollectEnemies(List<EnemyDefinition> into);

        /// <summary>The wave's stages, for checks that need the level's settings. Empty for wave types without stages.</summary>
        public virtual IReadOnlyList<StageDefinition> StageDefinitions => System.Array.Empty<StageDefinition>();
    }
}
