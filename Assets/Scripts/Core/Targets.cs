using System.Collections.Generic;
using UnityEngine;

namespace CylinderShooter.Core
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        Vector3 Position { get; }
        float Radius { get; }
        void TakeHit(int damage);
    }

    /// <summary>Everything a bullet can hit. Targets add/remove themselves; bullets only read.</summary>
    public interface ITargetRegistry
    {
        IReadOnlyList<IDamageable> Targets { get; }
        void Register(IDamageable target);
        void Unregister(IDamageable target);
    }

    public sealed class TargetRegistry : ITargetRegistry
    {
        readonly List<IDamageable> _targets = new List<IDamageable>();

        public IReadOnlyList<IDamageable> Targets => _targets;

        public void Register(IDamageable target)
        {
            if (!_targets.Contains(target)) _targets.Add(target);
        }

        public void Unregister(IDamageable target) => _targets.Remove(target);
    }
}
