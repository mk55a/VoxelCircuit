using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Enemies
{
    /// <summary>
    /// Tuning for the Bomber (sea mine): how tough it is, when its fuse arms, how long the player has to react,
    /// and how big the explosion is. Movement comes from the formation it flies in.
    /// </summary>
    [CreateAssetMenu(menuName = "Cylinder Shooter/Enemies/Bomber Tuning")]
    public sealed class BomberTuning : ScriptableObject, IFuseSettings
    {
        [Header("Body")]
        [Tooltip("Radius of the body for bullet hits and crashing into the player, in metres.")]
        [SerializeField, Min(0.01f)] private float _hitRadius = 0.75f;
        [Tooltip("Hits needed to pop it while it is NOT armed. Any hit while armed sets it off instead.")]
        [SerializeField, Min(1)] private int _health = 1;

        [Header("Fuse")]
        [Tooltip("The fuse arms when the player comes this close, in metres along the cylinder surface.")]
        [SerializeField, Min(0f)] private float _triggerRange = 4.5f;
        [Tooltip("Seconds from arming to the explosion: the player's window to boost away or shoot it.")]
        [SerializeField, Min(0.01f)] private float _fuseDuration = 0.6f;
        [Tooltip("Lamp blinks per second just after arming.")]
        [SerializeField, Min(0f)] private float _blinkRateStart = 5f;
        [Tooltip("Lamp blinks per second just before the explosion.")]
        [SerializeField, Min(0f)] private float _blinkRateEnd = 16f;

        [Header("Blast")]
        [Tooltip("Explosion radius in metres along the cylinder surface. The blast ring effect is drawn at this size.")]
        [SerializeField, Min(0.1f)] private float _blastRadius = 3.5f;
        [Tooltip("Damage dealt to every enemy of the same formation inside the blast. Large = always kills.")]
        [SerializeField, Min(1)] private int _blastDamage = 100;
        [Tooltip("If on, a blast set off by the player's shot can also destroy the player when they are inside it. " +
                 "Off: shooting an armed mine is a safe chain reaction, only the fuse running out hurts the player.")]
        [SerializeField] private bool _shotBlastHurtsPlayer;

        public float HitRadius => _hitRadius;
        public int Health => _health;
        public float TriggerRange => _triggerRange;
        public float FuseDuration => _fuseDuration;
        public float BlinkRateStart => _blinkRateStart;
        public float BlinkRateEnd => _blinkRateEnd;
        public float BlastRadius => _blastRadius;
        public int BlastDamage => _blastDamage;
        public bool ShotBlastHurtsPlayer => _shotBlastHurtsPlayer;
    }
}
