using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Effects
{
    /// <summary>
    /// Scene-wide pulse service: three shared particle systems (flash, ring, sparks) that every kill emits
    /// into, so no GameObjects are created per kill. The look of each layer (material, colour/size over
    /// lifetime, render mode) is set up in the Inspector; this script only enforces the settings emitting
    /// depends on and places the particles. Registered as IPulseEmitter by GameInstaller.
    /// </summary>
    public sealed class PulseEmitter : MonoBehaviour, IPulseEmitter
    {
        [Tooltip("Billboard flash: soft round glow, additive.")]
        [SerializeField] private ParticleSystem _flash;
        [Tooltip("Billboard ring: the pulse ring material (Shader Graph), additive.")]
        [SerializeField] private ParticleSystem _ring;
        [Tooltip("Stretched Billboard sparks: spark streak texture, additive.")]
        [SerializeField] private ParticleSystem _sparks;

        private void Awake()
        {
            if (_flash == null || _ring == null || _sparks == null)
            {
                Debug.LogError("PulseEmitter requires flash, ring and spark particle systems.", this);
                enabled = false;
                return;
            }

            PrepareForEmitting(_flash);
            PrepareForEmitting(_ring);
            PrepareForEmitting(_sparks);
        }

        public void Emit(Vector3 position, float radius, PulseProfile profile)
        {
            if (!enabled || profile == null || radius <= 0f) return;

            float diameter = radius * 2f;
            Color32 tint = profile.Color;

            if (profile.FlashSize > 0f)
            {
                _flash.Emit(new ParticleSystem.EmitParams
                {
                    position = position,
                    velocity = Vector3.zero,
                    startSize = diameter * profile.FlashSize,
                    startLifetime = profile.FlashLifetime,
                    startColor = tint,
                    rotation = Random.Range(0f, 360f),
                    applyShapeToPosition = false
                }, 1);
            }

            if (profile.RingSize > 0f)
            {
                _ring.Emit(new ParticleSystem.EmitParams
                {
                    position = position,
                    velocity = Vector3.zero,
                    startSize = diameter * profile.RingSize,
                    startLifetime = profile.RingLifetime,
                    startColor = tint,
                    applyShapeToPosition = false
                }, 1);
            }

            float surfaceAngle = Mathf.Atan2(position.x, position.z);
            var spark = new ParticleSystem.EmitParams { startColor = tint, startSize = profile.SparkSize, applyShapeToPosition = false };
            for (int i = 0; i < profile.SparkCount; i++)
            {
                Vector3 direction = PulseMath.ScreenPlaneDirection(surfaceAngle, Random.Range(0f, Mathf.PI * 2f));
                spark.position = position + direction * (radius * 0.3f);
                spark.velocity = direction * Random.Range(profile.SparkSpeed.x, profile.SparkSpeed.y);
                spark.startLifetime = Random.Range(profile.SparkLifetime.x, profile.SparkLifetime.y);
                _sparks.Emit(spark, 1);
            }
        }

        /// <summary>
        /// Settings emitting relies on, enforced so a misconfigured system can't silently break kills:
        /// world space (particles stay where the enemy died), always playing, and no automatic emission.
        /// </summary>
        private static void PrepareForEmitting(ParticleSystem system)
        {
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.playOnAwake = true;
            var emission = system.emission;
            emission.enabled = false;
            var shape = system.shape;
            shape.enabled = false;
            system.Play();
        }
    }
}
