using System;
using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Levels
{
    /// <summary>
    /// Counts and removes enemies through the contact hazard registry, which every enemy registers in. Works with
    /// whatever spawned them. The enemy pool's own registry will replace this.
    /// </summary>
    public sealed class HazardBattlefield : IBattlefield
    {
        private readonly ContactHazardRegistry _hazards;

        public HazardBattlefield(ContactHazardRegistry hazards)
        {
            _hazards = hazards ?? throw new ArgumentNullException(nameof(hazards));
        }

        public int AliveEnemies => _hazards.Hazards.Count;

        public void RemoveAllEnemies()
        {
            // Backwards: deactivating an enemy removes it from the registry.
            for (int i = _hazards.Hazards.Count - 1; i >= 0; i--)
            {
                if (i < _hazards.Hazards.Count && _hazards.Hazards[i] is Component enemy && enemy != null)
                    enemy.gameObject.SetActive(false);
            }
        }
    }
}
