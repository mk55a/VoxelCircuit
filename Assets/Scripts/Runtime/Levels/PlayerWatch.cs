using System;
using CylinderShooter.Core;
using CylinderShooter.Ship;

namespace CylinderShooter.Levels
{
    /// <summary>
    /// Lets the director watch the player (height, heading, getting hit) without knowing the ship's components. Plain
    /// C#, created once by GameInstaller.
    /// </summary>
    public sealed class PlayerWatch : IPlayerWatch, IDisposable
    {
        private readonly ShipController _ship;
        private readonly PlayerLife _life;
        private readonly Action<DestructionEvent> _onDestroyed;

        public PlayerWatch(ShipController ship, PlayerLife life)
        {
            _ship = ship ?? throw new ArgumentNullException(nameof(ship));
            _life = life;
            _onDestroyed = _ => Hit?.Invoke();
            if (_life != null) _life.Destroyed += _onDestroyed;
        }

        public event Action Hit;

        public float Height => _ship != null ? _ship.Height : 0f;
        public float FacingSign => _ship != null ? _ship.FacingSign : 1f;

        public void Dispose()
        {
            if (_life != null) _life.Destroyed -= _onDestroyed;
        }
    }
}
