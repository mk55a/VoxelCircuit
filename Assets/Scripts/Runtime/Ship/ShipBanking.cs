using UnityEngine;

namespace CylinderShooter.Ship
{
    public sealed class ShipBanking : MonoBehaviour
    {
        [SerializeField] private ShipController _ship;
        [SerializeField] private Transform _model;

        float _bank;

        void LateUpdate()
        {
            var t = _ship.Tuning;
            float target = -_ship.NormalizedVelocity * t.MaxBankAngle * _ship.Facing;
            _bank = Mathf.Lerp(_bank, target, 1f - Mathf.Exp(-t.BankSmoothing * Time.deltaTime));
            _model.localRotation = Quaternion.Euler(0f, 0f, _bank);
        }
    }
}
