using System;
using System.Collections.Generic;
using CylinderShooter.Core;
using CylinderShooter.Formations;
using CylinderShooter.Ship;
using UnityEngine;

namespace CylinderShooter.Enemies
{
    /// <summary>
    /// A floating sea mine. It never chases: it stays in its formation slot and drifts across the arena with it.
    /// When the player comes within trigger range its fuse arms, its lamps blink faster and faster, and it explodes.
    ///
    /// - Shot while idle: pops like any enemy (debris and pulse, no blast).
    /// - Shot while armed: explodes at once, destroying members of its own formation inside the blast
    ///   (a chain reaction the player can set up), and by default not hurting the player.
    /// - Fuse runs out: explodes, destroying its formation mates in range and the player if inside the blast.
    /// - Rammed by the player: explodes (the crash itself costs the player a life through PlayerContact).
    /// </summary>
    public sealed class Bomber : MonoBehaviour, IDamageable, IContactHazard, IDestructible, IBlastSource,
        IFuseSource, IFormationMember
    {
        [Header("Configuration")]
        [SerializeField] private BomberTuning _tuning;
        [Tooltip("The player ship. Set automatically when spawned by a formation spawner; only needed for mines placed in a scene by hand.")]
        [SerializeField] private ShipController _target;
        [Tooltip("Where a hand-placed mine sits (it doesn't move without a formation).")]
        [SerializeField] private float _startAngleDegrees;
        [SerializeField] private float _startHeight;

        private readonly List<IDamageable> _blastHits = new List<IDamageable>();
        private ITargetRegistry _targets;
        private ContactHazardRegistry _hazards;
        private CylinderConfig _cylinder;
        private PlayerLife _playerLife;
        private ProximityFuse _fuse;
        private CylinderPosition _position;
        private int _health;
        private bool _exploding;
        private FormationMotion _formation;
        private int _formationSlot;
        private Action<IFormationMember> _onReturned;
        private CylinderPosition _spawnPosition;
        private bool _hasSpawn;

        public bool IsAlive => _health > 0 && !_exploding;
        public bool HasValidTuning => _tuning != null && _tuning.HitRadius > 0f && _tuning.Health > 0 &&
                                      _tuning.BlastRadius > 0f && _tuning.FuseDuration > 0f;
        public Vector3 Position => transform.position;
        public float Radius => _tuning.HitRadius;
        public CylinderPosition PreviousContactPosition { get; private set; }
        public CylinderPosition ContactPosition => _position;
        public FormationMotion Formation => _formation;
        public bool IsArmed => _fuse != null && _fuse.IsArmed;
        public float FuseProgress => _fuse?.Progress ?? 0f;
        public float BlinkLevel => _fuse?.BlinkLevel ?? 0f;

        /// <summary>Popped, or exploded. Debris and pulse effects listen here.</summary>
        public event Action<DestructionEvent> Destroyed;

        /// <summary>Exploded (not raised when it just pops). Blast effects listen here.</summary>
        public event Action<BlastEvent> Detonated;

        public void PrepareSpawn(ShipController target, FormationMotion formation, int slot,
            Action<IFormationMember> onReturned)
        {
            if (gameObject.activeSelf || target == null || formation == null || onReturned == null)
                throw new InvalidOperationException("A pooled Bomber must be inactive and have a target, formation and owner.");
            _target = target;
            _playerLife = target.GetComponent<PlayerLife>();
            _formation = formation;
            _formationSlot = slot;
            _onReturned = onReturned;
            _spawnPosition = formation.PositionAt(slot);
            _hasSpawn = true;
        }

        private void Awake()
        {
            if (!HasValidTuning)
            {
                Debug.LogError("Bomber needs a BomberTuning asset in 'Tuning' (Assets/Tuning/Enemies/BomberTuning) " +
                               "with positive hit radius, health, fuse duration and blast radius.", this);
                enabled = false;
                return;
            }
            if (_target == null)
            {
                Debug.LogError("Bomber needs the player ship in 'Target' (set automatically when a formation spawns it).", this);
                enabled = false;
                return;
            }
            if (_playerLife == null) _playerLife = _target.GetComponent<PlayerLife>();
            if (_playerLife == null)
            {
                Debug.LogError("Bomber's target ship has no PlayerLife component, so the blast can't hurt the player. " +
                               "Add PlayerLife to the ship (Cylinder Shooter > Add Player Lives To Scene).", this);
                enabled = false;
                return;
            }

            _targets = ServiceLocator.Get<ITargetRegistry>();
            _hazards = ServiceLocator.Get<ContactHazardRegistry>();
            _cylinder = ServiceLocator.Get<CylinderConfig>();
            _fuse = new ProximityFuse(_tuning);
        }

        private void OnEnable()
        {
            if (!enabled || _targets == null) return;

            _position = _hasSpawn ? _spawnPosition : new CylinderPosition(_startAngleDegrees * Mathf.Deg2Rad,
                Mathf.Clamp(_startHeight, _cylinder.MinHeight, _cylinder.MaxHeight));
            _hasSpawn = false;
            PreviousContactPosition = _position;
            _health = _tuning.Health;
            _exploding = false;
            _fuse.Reset();
            ApplyPosition();
            _targets.Register(this);
            _hazards.Register(this);
        }

        private void Update()
        {
            PreviousContactPosition = _position;
            if (_formation != null) _position = _formation.PositionAt(_formationSlot);
            ApplyPosition();

            var player = new CylinderPosition(_target.Angle, _target.Height);
            float distance = CylinderSpace.SurfaceDistance(_position, player, _cylinder.Radius);
            if (_fuse.Tick(Time.deltaTime, distance, _playerLife.CanControl))
                Explode(causedByShot: false);
        }

        private void OnDisable()
        {
            _health = 0;
            _targets?.Unregister(this);
            _hazards?.Unregister(this);
            if (_formation != null) _formation.RemoveMember();
            _formation = null;
            _hasSpawn = false;
            _fuse?.MarkDetonated();
            var onReturned = _onReturned;
            _onReturned = null;
            onReturned?.Invoke(this);
        }

        public void TakeHit(int damage)
        {
            if (!IsAlive || damage <= 0) return;
            if (_fuse.IsArmed)
            {
                Explode(causedByShot: true);
                return;
            }
            _health -= damage;
            if (_health <= 0) Pop();
        }

        public void DestroyOnContact()
        {
            if (IsAlive) Explode(causedByShot: false);
        }

        private void Pop()
        {
            _fuse.MarkDetonated();
            Destroyed?.Invoke(new DestructionEvent(_position, _target.Angle));
            gameObject.SetActive(false);
        }

        private void Explode(bool causedByShot)
        {
            // Mark first, so a chain reaction that reaches back to this mine can't set it off twice.
            _exploding = true;
            _fuse.MarkDetonated();
            Detonated?.Invoke(new BlastEvent(transform.position, _tuning.BlastRadius, causedByShot));
            Destroyed?.Invoke(new DestructionEvent(_position));

            if ((!causedByShot || _tuning.ShotBlastHurtsPlayer) && _playerLife.CanControl)
            {
                var player = new CylinderPosition(_target.Angle, _target.Height);
                if (CylinderSpace.SurfaceDistance(_position, player, _cylinder.Radius) <=
                    _tuning.BlastRadius + _playerLife.ContactRadius)
                    _playerLife.TryDestroy(_position.Angle);
            }

            DamageFormationMates();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Hits every member of this mine's own formation inside the blast. Targets are collected first and hit
        /// afterwards, because a hit mine that is armed explodes too and changes the registry while we loop.
        /// </summary>
        private void DamageFormationMates()
        {
            if (_formation == null) return;
            _blastHits.Clear();
            var targets = _targets.Targets;
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                var target = targets[i];
                if (ReferenceEquals(target, this) || !target.IsAlive ||
                    !(target is IFormationMember member) || member.Formation != _formation ||
                    !(target is IContactHazard placed))
                    continue;
                if (CylinderSpace.SurfaceDistance(_position, placed.ContactPosition, _cylinder.Radius) <=
                    _tuning.BlastRadius + target.Radius)
                    _blastHits.Add(target);
            }
            for (int i = 0; i < _blastHits.Count; i++) _blastHits[i].TakeHit(_tuning.BlastDamage);
            _blastHits.Clear();
        }

        private void ApplyPosition()
        {
            transform.position = _position.ToWorld(_cylinder.Radius);
            // A mine floats upright, side-on to the camera: its belt stays level whatever the formation does.
            transform.rotation = Quaternion.LookRotation(-CylinderSpace.Tangent(_position.Angle), Vector3.up);
        }
    }
}
