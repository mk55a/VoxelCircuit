using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Ship
{
    [DefaultExecutionOrder(100)]
    public sealed class PlayerContact : MonoBehaviour
    {
        [SerializeField] private ShipController _ship;
        [SerializeField] private PlayerLife _life;
        [SerializeField] private CylinderConfig _cylinder;

        private ContactHazardRegistry _hazards;
        private CylinderPosition _previous;
        private bool _hasPrevious;

        private void Awake()
        {
            if (_ship == null || _life == null || _cylinder == null || _cylinder.radius <= 0f)
            {
                Debug.LogError("PlayerContact requires the ship, life component and a valid cylinder.", this);
                enabled = false;
                return;
            }
            _hazards = ServiceLocator.Get<ContactHazardRegistry>();
        }

        private void LateUpdate()
        {
            var current = _ship.Position;
            if (!_life.CanControl)
            {
                _hasPrevious = false;
                return;
            }

            var start = _hasPrevious ? _previous : current;
            bool destroyed = false;
            for (int i = _hazards.Hazards.Count - 1; i >= 0; i--)
            {
                var hazard = _hazards.Hazards[i];
                if (!hazard.IsAlive || !CylinderContact.SweptSpheres(start, current, _life.ContactRadius,
                    hazard.PreviousContactPosition, hazard.ContactPosition, hazard.Radius, _cylinder.radius))
                {
                    continue;
                }
                hazard.DestroyOnContact();
                if (!destroyed && _life.Status == PlayerStatus.Alive && _life.TryDestroy())
                {
                    destroyed = true;
                }
            }
            if (destroyed)
            {
                _hasPrevious = false;
                return;
            }
            _previous = current;
            _hasPrevious = true;
        }
    }
}
