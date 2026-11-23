using System.Collections.Generic;

namespace CylinderShooter.Core
{
    public sealed class ContactHazardRegistry
    {
        private readonly List<IContactHazard> _hazards = new List<IContactHazard>();

        public IReadOnlyList<IContactHazard> Hazards => _hazards;

        public void Register(IContactHazard hazard)
        {
            if (!_hazards.Contains(hazard))
            {
                _hazards.Add(hazard);
            }
        }

        public void Unregister(IContactHazard hazard)
        {
            _hazards.Remove(hazard);
        }
    }
}
