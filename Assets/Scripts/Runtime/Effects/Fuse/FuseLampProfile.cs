using UnityEngine;

namespace CylinderShooter.Effects
{
    /// <summary>How an enemy's warning lamps look while idle and while its fuse counts down.</summary>
    [CreateAssetMenu(menuName = "Cylinder Shooter/Effects/Fuse Lamp Profile")]
    public sealed class FuseLampProfile : ScriptableObject
    {
        [Tooltip("Lamp glow while the fuse is idle. Multiplies the material's emission map (only the lamp voxels glow). " +
                 "Keep the brightness low so idle mines don't bloom.")]
        [SerializeField, ColorUsage(false, true)] private Color _idle = new Color(0.6f, 0.45f, 0.25f);
        [Tooltip("Lamp glow during the 'on' part of each blink once armed. Bright (HDR above about 1.3) so it blooms.")]
        [SerializeField, ColorUsage(false, true)] private Color _armedOn = new Color(4f, 2.4f, 0.8f);
        [Tooltip("Lamp glow during the 'off' part of each blink once armed.")]
        [SerializeField, ColorUsage(false, true)] private Color _armedOff = new Color(0.15f, 0.08f, 0.03f);

        public Color Idle => _idle;
        public Color ArmedOn => _armedOn;
        public Color ArmedOff => _armedOff;
    }
}
