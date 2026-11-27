using System;
using System.Collections.Generic;
using CylinderShooter.Core;
using CylinderShooter.Formations;
using UnityEngine;

namespace CylinderShooter.Enemies
{
    /// <summary>
    /// One kind of enemy for the level system: which prefab, how many may be alive at once, how many the pool makes
    /// before play, and the facts the director needs in every stage: which formations it flies in (and how many) and
    /// how its groups enter. Its cost is set per level, in an Enemy Cost Table.
    /// </summary>
    [CreateAssetMenu(menuName = "Cylinder Shooter/Director/Enemy")]
    public sealed class EnemyDefinition : ScriptableObject, IEnemyType
    {
        [Tooltip("Shown in the debugger. Empty = the asset's name.")]
        [SerializeField] private string _displayName;
        [Tooltip("The enemy prefab (e.g. Assets/Prefabs/Enemies/Chaser). Drag the prefab in; the right component is picked.")]
        [SerializeField] private MonoBehaviour _prefab;
        [Tooltip("Most of this enemy alive at once. 0 = no limit of its own (the level's total limit still applies).")]
        [SerializeField, Min(0)] private int _maxAlive;
        [Tooltip("How many the pool creates when the scene starts. Set it to the most you expect alive at once.")]
        [SerializeField, Min(0)] private int _prewarm = 25;

        [Header("Director")]
        [Tooltip("Formations this enemy may fly in. The director picks one by weight, then a count between Min and Max.")]
        [SerializeField] private List<EnemyFormationOption> _formations = new List<EnemyFormationOption>();
        [Tooltip("How groups of this enemy may enter. The director picks one by weight. Empty = just off-screen.")]
        [SerializeField] private List<EnemyEntryOption> _entries = new List<EnemyEntryOption>();

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public int MaxAlive => _maxAlive;
        public int Prewarm => _prewarm;
        public IReadOnlyList<IFormationOption> Formations => _formations;
        public IReadOnlyList<IEntryOption> Entries => _entries;
        public IReadOnlyList<EnemyFormationOption> FormationOptions => _formations;

        /// <summary>The prefab's formation component, or null when the prefab is missing or can't fly in formations.</summary>
        public IFormationMember Prefab => _prefab != null ? _prefab as IFormationMember : null;

        private void OnValidate()
        {
            if (_prefab != null && !(_prefab is IFormationMember))
                _prefab = _prefab.GetComponent<IFormationMember>() as MonoBehaviour;
        }
    }

    /// <summary>A formation an enemy may fly in, with how many may be in it.</summary>
    [Serializable]
    public sealed class EnemyFormationOption : IFormationOption
    {
        [Tooltip("The formation's shape, spacing and speed (Assets/Tuning/Formations). Its own Count is ignored here.")]
        [SerializeField] private FormationDefinition _formation;
        [Tooltip("Smallest group in this formation.")]
        [SerializeField, Min(1)] private int _minCount = 5;
        [Tooltip("Largest group in this formation. The director keeps it under every limit and the credit cap.")]
        [SerializeField, Min(1)] private int _maxCount = 15;
        [Tooltip("Chance of this formation compared with the enemy's others. 0 = never.")]
        [SerializeField, Min(0f)] private float _weight = 1f;

        public EnemyFormationOption() { }

        public EnemyFormationOption(FormationDefinition formation, int minCount, int maxCount, float weight = 1f)
        {
            _formation = formation;
            _minCount = minCount;
            _maxCount = maxCount;
            _weight = weight;
        }

        public FormationDefinition Formation => _formation;
        public string DisplayName => _formation != null ? _formation.name : "(no formation)";
        public int MinCount => Mathf.Max(1, _minCount);
        public int MaxCount => Mathf.Max(MinCount, _maxCount);

        // A missing formation can't be flown, so it is never picked.
        public float Weight => _formation != null ? _weight : 0f;
    }

    /// <summary>One way a group of an enemy may enter.</summary>
    [Serializable]
    public sealed class EnemyEntryOption : IEntryOption
    {
        [Tooltip("Just Off Screen: right past the screen edge, then flies in. At Distance: a set distance along the " +
                 "ring, so the player sees it coming (for big or expensive enemies).")]
        [SerializeField] private EntryStyle _style = EntryStyle.JustOffScreen;
        [Tooltip("Metres along the ring from the player, for At Distance. The ring is about 377 m around, so 188 m is " +
                 "the far side.")]
        [SerializeField, Min(0f)] private float _distance = 120f;
        [Tooltip("Chance of this entry compared with the enemy's others.")]
        [SerializeField, Min(0f)] private float _weight = 1f;

        public EnemyEntryOption() { }

        public EnemyEntryOption(EntryStyle style, float distance, float weight = 1f)
        {
            _style = style;
            _distance = distance;
            _weight = weight;
        }

        public EntryStyle Style => _style;
        public float Distance => _distance;
        public float Weight => _weight;
    }
}
