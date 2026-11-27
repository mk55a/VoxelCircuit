using System;
using System.Collections.Generic;
using CylinderShooter.Core;
using CylinderShooter.Enemies;
using UnityEngine;

namespace CylinderShooter.Levels
{
    /// <summary>
    /// One stage: the smallest part of a level that is designed. It sets how much pressure the director may put on the
    /// player (credits), how it spends them, and which enemies it may use; the director decides what, how many, where
    /// and when. Its own asset, so a good Rest or Peak can be used in several waves (changing it changes it everywhere).
    /// </summary>
    [CreateAssetMenu(menuName = "Cylinder Shooter/Director/Stage")]
    public sealed class StageDefinition : ScriptableObject, IStageSettings
    {
        [Tooltip("Shown in the debugger and in events. Empty = the asset's name.")]
        [SerializeField] private string _displayName;

        [Header("Ending")]
        [Tooltip("Time Up: ends after Duration. Time Up Then Cleared: buys for Duration, then waits until no enemies " +
                 "are left. All Cleared: buys for Duration, but ends as soon as no enemies are left once one appeared.")]
        [SerializeField] private StageEndMode _endMode = StageEndMode.TimeUp;
        [Tooltip("Seconds the director buys groups for.")]
        [SerializeField, Min(0.1f)] private float _duration = 30f;
        [Tooltip("Give up after this many seconds, for stages that wait for a clear, so the level can never get stuck. " +
                 "Ignored by Time Up.")]
        [SerializeField, Min(0.1f)] private float _maxDuration = 60f;

        [Header("Credits (how much pressure)")]
        [Tooltip("Credits the stage opens with, so the first group can come at once.")]
        [SerializeField, Min(0f)] private float _startingCredits = 5f;
        [Tooltip("Credits earned per second: how fast the director saves. A Chaser costing 1 at 2 per second = up to " +
                 "two Chasers a second.")]
        [SerializeField, Min(0f)] private float _creditsPerSecond = 2f;
        [Tooltip("The most credits the director can save. Limits the biggest group and the burst after a clear. " +
                 "0 = no limit.")]
        [SerializeField, Min(0f)] private float _creditCap = 30f;

        [Header("Spending (how it arrives)")]
        [Tooltip("0 = small groups, bought as soon as they are affordable (a steady trickle). 1 = big groups, saved up " +
                 "for (calm, then a burst).")]
        [SerializeField, Range(0f, 1f)] private float _groupSize = 0.5f;
        [Tooltip("Seconds the director saves for one group. After that it buys a smaller group it can afford, or picks " +
                 "something else, so the stage never goes quiet by accident.")]
        [SerializeField, Min(0.1f)] private float _patience = 6f;
        [Tooltip("Minimum seconds between two groups, so they don't all arrive in the same moment.")]
        [SerializeField, Min(0f)] private float _cooldown = 1.5f;
        [Tooltip("Most enemies alive at once in this stage, all types together. 0 = only the level's limit.")]
        [SerializeField, Min(0)] private int _maxAlive = 60;

        [Header("Enemies")]
        [Tooltip("The enemies this stage may use. On = may spawn here. Weight = chance compared with the others. Max " +
                 "Alive = its limit in this stage (0 = its own limit only). Missing or off = can't spawn.")]
        [SerializeField] private List<StageEnemyEntry> _enemies = new List<StageEnemyEntry>();

        [Header("Where")]
        [Tooltip("Which side of the player groups come from, relative to where the ship is heading.")]
        [SerializeField] private SpawnSides _sides = SpawnSides.Either;
        [Tooltip("0 to 1: chance a group is sent into the height lane (bottom, middle, top) the player uses most, to " +
                 "make them move. 0 = lanes at random.")]
        [SerializeField, Range(0f, 1f)] private float _laneFocus = 0.5f;
        [Tooltip("Allow groups to close the player's lane from both sides at once. Off = a lane just entered from one " +
                 "side keeps that side for a while (the level's Pincer Window), so there is always a way out.")]
        [SerializeField] private bool _allowPincer;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public StageEndMode EndMode => _endMode;
        public float Duration => _duration;
        public float MaxDuration => _maxDuration;
        public float StartingCredits => _startingCredits;
        public float CreditsPerSecond => _creditsPerSecond;
        public float CreditCap => _creditCap;
        public float GroupSize => _groupSize;
        public float Patience => _patience;
        public float Cooldown => _cooldown;
        public int MaxAlive => _maxAlive;
        public IReadOnlyList<IStageEnemy> Enemies => _enemies;
        public IReadOnlyList<StageEnemyEntry> EnemyEntries => _enemies;
        public SpawnSides Sides => _sides;
        public float LaneFocus => _laneFocus;
        public bool AllowPincer => _allowPincer;
    }

    /// <summary>One row of a stage's enemy list.</summary>
    [Serializable]
    public sealed class StageEnemyEntry : IStageEnemy
    {
        [SerializeField] private EnemyDefinition _enemy;
        [Tooltip("The unlock toggle: off = this enemy can't spawn in this stage.")]
        [SerializeField] private bool _enabled = true;
        [Tooltip("Chance of being picked, compared with the other rows. 3 and 1 = picked about 75% and 25% of the time.")]
        [SerializeField, Min(0f)] private float _weight = 1f;
        [Tooltip("Most of this enemy alive at once in this stage. 0 = only its own limit.")]
        [SerializeField, Min(0)] private int _maxAlive;

        public StageEnemyEntry() { }

        public StageEnemyEntry(EnemyDefinition enemy, float weight, int maxAlive, bool enabled = true)
        {
            _enemy = enemy;
            _weight = weight;
            _maxAlive = maxAlive;
            _enabled = enabled;
        }

        // Unity's "missing" reference isn't C# null, so turn it into one for code that only knows the interface.
        public IEnemyType Enemy => _enemy != null ? _enemy : null;
        public EnemyDefinition Definition => _enemy;
        public bool Enabled => _enabled;
        public float Weight => _weight;
        public int MaxAlive => _maxAlive;
    }
}
