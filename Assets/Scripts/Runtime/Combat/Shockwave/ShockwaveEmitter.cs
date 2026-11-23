using System;
using System.Collections.Generic;
using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Combat
{
    /// <summary>
    /// Runs one shockwave at a time: forms it around the origin, throws it outward, destroys every active target it
    /// sweeps over, and places the two wave objects (one per direction). The wave objects are authored in the prefab;
    /// every renderer under them (glow, heat haze) gets the same per-frame values. Screen effects listen to the events.
    /// </summary>
    public sealed class ShockwaveEmitter : MonoBehaviour, IShockwaveEmitter
    {
        private static readonly int FadeId = Shader.PropertyToID("_Fade");
        private static readonly int CurveDepthId = Shader.PropertyToID("_CurveDepth");
        private static readonly int FrontDistanceId = Shader.PropertyToID("_FrontDistance");
        private static readonly int TrailLengthId = Shader.PropertyToID("_TrailLength");
        private static readonly int ClipBottomId = Shader.PropertyToID("_ClipBottom");
        private static readonly int ClipTopId = Shader.PropertyToID("_ClipTop");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        [SerializeField] private ShockwaveProfile _profile;
        [Tooltip("Wave that travels screen-right (Shockwave_Band mesh). The shader bends it into the ring shape and " +
                 "trail; child renderers (e.g. heat haze) follow it.")]
        [SerializeField] private Renderer _rightWave;
        [Tooltip("Wave that travels screen-left. Same setup as the right wave.")]
        [SerializeField] private Renderer _leftWave;

        private readonly ShockwaveFront _front = new ShockwaveFront();
        private readonly HashSet<IDamageable> _alreadyHit = new HashSet<IDamageable>();
        private CylinderConfig _cylinder;
        private ITargetRegistry _targets;
        private MaterialPropertyBlock _properties;
        private Renderer[] _rightRenderers;
        private Renderer[] _leftRenderers;
        private bool _released;

        public event Action Triggered;
        public event Action Released;
        public event Action Finished;

        public bool IsActive => _front.IsActive;

        // The wave covers the whole cylinder wall: the play area plus padding above and below.
        private float Bottom => _cylinder.MinHeight - _profile.HeightPadding;
        private float Top => _cylinder.MaxHeight + _profile.HeightPadding;

        private void Awake()
        {
            if (_profile == null || _rightWave == null || _leftWave == null)
            {
                Debug.LogError("ShockwaveEmitter requires a profile and both wave renderers.", this);
                enabled = false;
                return;
            }

            _cylinder = ServiceLocator.Get<CylinderConfig>();
            _targets = ServiceLocator.Get<ITargetRegistry>();
            _properties = new MaterialPropertyBlock();
            _rightRenderers = _rightWave.GetComponentsInChildren<Renderer>(true);
            _leftRenderers = _leftWave.GetComponentsInChildren<Renderer>(true);
            ShowWaves(false);
        }

        public bool TryTrigger(CylinderPosition origin)
        {
            if (!enabled || _front.IsActive) return false;

            _front.Start(_profile, origin.Angle, origin.Height, _cylinder.Radius, Bottom, Top);
            _alreadyHit.Clear();
            _released = false;

            // Hit straight away: an enemy touching the ship on this frame is already inside the ring.
            HitTargets();
            PlaceWaves();
            ShowWaves(true);
            Triggered?.Invoke();
            return true;
        }

        private void Update()
        {
            if (!_front.IsActive) return;

            _front.Advance(Time.deltaTime);
            if (!_front.IsActive)
            {
                ShowWaves(false);
                Finished?.Invoke();
                return;
            }
            if (!_released && _front.IsReleased)
            {
                _released = true;
                Released?.Invoke();
            }

            HitTargets();
            PlaceWaves();
        }

        private void HitTargets()
        {
            var targets = _targets.Targets;
            // Backwards: a destroyed target unregisters itself, removing its own entry from this list.
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                if (i >= targets.Count) continue;
                var target = targets[i];
                if (!target.IsAlive || _alreadyHit.Contains(target)) continue;

                Vector3 position = target.Position;
                float angle = Mathf.Atan2(position.x, position.z);
                if (!_front.Hits(angle, position.y, target.Radius)) continue;

                _alreadyHit.Add(target);
                target.TakeHit(_profile.Damage);
            }
        }

        private void PlaceWaves()
        {
            float fadeStart = 1f - _profile.FadeOut;
            float fade = _profile.FadeOut > 0f ? 1f - Mathf.Clamp01((_front.ReleaseProgress - fadeStart) / _profile.FadeOut) : 1f;
            _properties.Clear();
            _properties.SetFloat(FadeId, fade);
            _properties.SetFloat(CurveDepthId, _front.CurveDepth);
            _properties.SetFloat(FrontDistanceId, _front.Distance);
            _properties.SetFloat(TrailLengthId, _profile.VisualLength);
            _properties.SetFloat(ClipBottomId, _front.Bottom);
            _properties.SetFloat(ClipTopId, _front.Top);
            _properties.SetFloat(IntensityId, _profile.GlowAt(_front.Elapsed));

            PlaceWave(_rightWave, _rightRenderers, ShockwaveFront.Right);
            PlaceWave(_leftWave, _leftRenderers, ShockwaveFront.Left);
        }

        private void PlaceWave(Renderer wave, Renderer[] renderers, int side)
        {
            float angle = _front.FrontAngle(side);
            Vector3 outward = CylinderSpace.Outward(angle);

            // Aim local +Z inward (right wave) or outward (left wave) so local +X points in the travel direction:
            // screen-right is -tangent. +Y stays world up.
            Vector3 facing = side == ShockwaveFront.Right ? -outward : outward;
            wave.transform.SetPositionAndRotation(
                CylinderSpace.ToWorld(angle, _front.CenterHeight, _cylinder.Radius + _profile.RadiusOffset),
                Quaternion.LookRotation(facing, Vector3.up));

            // Height only: the shader shapes the ring and the trail in metres, and cuts it at the cylinder's top and bottom.
            wave.transform.localScale = new Vector3(1f, _front.HalfHeight * 2f, 1f);

            for (int i = 0; i < renderers.Length; i++) renderers[i].SetPropertyBlock(_properties);
        }

        private void ShowWaves(bool visible)
        {
            for (int i = 0; i < _rightRenderers.Length; i++) _rightRenderers[i].enabled = visible;
            for (int i = 0; i < _leftRenderers.Length; i++) _leftRenderers[i].enabled = visible;
        }
    }
}
