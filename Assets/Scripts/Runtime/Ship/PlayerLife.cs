using System;
using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Ship
{
    public sealed class PlayerLife : MonoBehaviour
    {
        [SerializeField] private PlayerLifeTuning _tuning;
        [SerializeField] private ShipController _ship;
        [SerializeField] private GameObject _visual;

        private PlayerLifeState _state;

        public event Action<int> OnLivesChanged;
        public event Action<PlayerStatus> OnStatusChanged;

        public int Lives => _state.Lives;
        public PlayerStatus Status => _state.Status;
        public bool CanControl => Status != PlayerStatus.Respawning;
        public float ContactRadius => _tuning.ContactRadius;

        private void Awake()
        {
            if (_tuning == null || _ship == null || _visual == null || _visual == gameObject
                || !_visual.transform.IsChildOf(transform) || _tuning.ContactRadius <= 0f)
            {
                Debug.LogError("PlayerLife requires tuning, ship, a child visual and a positive contact radius.", this);
                enabled = false;
                return;
            }
            _state = new PlayerLifeState(_tuning.StartingLives, _tuning.RespawnDelay, _tuning.InvulnerabilityDuration);
            _state.OnLivesChanged += lives => OnLivesChanged?.Invoke(lives);
            _state.OnStatusChanged += HandleStatusChanged;
        }

        private void Update()
        {
            _state.Tick(Time.deltaTime);
        }

        public void AddLives(int count) => _state.AddLives(count);
        public void RemoveLives(int count) => _state.RemoveLives(count);
        public bool TryDestroy() => _state.TryDestroy();

        private void HandleStatusChanged(PlayerStatus status)
        {
            if (status == PlayerStatus.Respawning)
            {
                _ship.StopAtCurrentPosition();
                _visual.SetActive(false);
            }
            else if (!_visual.activeSelf)
            {
                _ship.ResetAfterRespawn();
                _visual.SetActive(true);
            }
            OnStatusChanged?.Invoke(status);
        }
    }
}
