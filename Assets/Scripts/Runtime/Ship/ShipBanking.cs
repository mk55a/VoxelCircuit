using UnityEngine;

namespace CylinderShooter.Ship
{
    /// <summary>Visual-only roll on the model child, driven by horizontal speed.</summary>
    public sealed class ShipBanking : MonoBehaviour
    {
        [SerializeField] ShipController ship;
        [SerializeField] Transform model;

        float _bank;

        void LateUpdate()
        {
            var t = ship.Tuning;
            float target = -ship.NormalizedVelocity * t.MaxBankAngle * ship.Facing;
            _bank = Mathf.Lerp(_bank, target, 1f - Mathf.Exp(-t.BankSmoothing * Time.deltaTime));
            model.localRotation = Quaternion.Euler(0f, 0f, _bank);
        }
    }
}
