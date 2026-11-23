using System;
using CylinderShooter.Combat;
using CylinderShooter.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CylinderShooter.Rendering.Shockwave
{
    /// <summary>
    /// Screen side of the shockwave: blends authored post-processing Volumes in and out (the build-up dim, the release
    /// flash and lens thump) and asks the camera for a copy of the scene while the wave runs, which the heat-haze
    /// material bends. Nothing costs anything while no wave is running.
    /// </summary>
    public sealed class ShockwaveScreenEffects : MonoBehaviour
    {
        private enum Moment
        {
            Trigger,
            Release
        }

        [Serializable]
        private sealed class VolumeCue
        {
            [Tooltip("A Volume with its own profile (e.g. dim + chromatic aberration, or flash + lens distortion). Keep its Weight at 0.")]
            public Volume Volume;
            [Tooltip("Which moment the curve's time 0 is: when K is pressed, or when the wave is thrown.")]
            public Moment StartsAt = Moment.Trigger;
            [Tooltip("Volume weight (0-1) over seconds since that moment. End the curve at 0.")]
            public AnimationCurve Weight = AnimationCurve.EaseInOut(0f, 0f, 0.5f, 1f);
        }

        [Tooltip("Camera that renders the game. It provides the scene copy for the heat haze while a wave runs.")]
        [SerializeField] private UniversalAdditionalCameraData _camera;
        [SerializeField] private VolumeCue[] _cues = Array.Empty<VolumeCue>();

        private IShockwaveEmitter _emitter;
        private CameraOverrideOption _cameraColorDefault;
        private float _triggerTime = float.NegativeInfinity;
        private float _releaseTime = float.NegativeInfinity;
        private bool _playing;

        private void Start()
        {
            if (!ServiceLocator.TryGet(out _emitter))
            {
                Debug.LogError("ShockwaveScreenEffects found no IShockwaveEmitter. Assign the Shockwave object on the GameInstaller.", this);
                enabled = false;
                return;
            }
            if (_camera != null) _cameraColorDefault = _camera.requiresColorOption;
            _emitter.Triggered += HandleTriggered;
            _emitter.Released += HandleReleased;
            SetAllWeights(0f);
        }

        private void OnDestroy()
        {
            if (_emitter == null) return;
            _emitter.Triggered -= HandleTriggered;
            _emitter.Released -= HandleReleased;
        }

        private void HandleTriggered()
        {
            _triggerTime = Time.time;
            _releaseTime = float.PositiveInfinity;
            _playing = true;
            if (_camera != null) _camera.requiresColorOption = CameraOverrideOption.On;
        }

        private void HandleReleased() => _releaseTime = Time.time;

        private void Update()
        {
            if (!_playing) return;

            bool anyRunning = false;
            for (int i = 0; i < _cues.Length; i++)
            {
                var cue = _cues[i];
                if (cue.Volume == null || cue.Weight == null || cue.Weight.length == 0) continue;

                float start = cue.StartsAt == Moment.Trigger ? _triggerTime : _releaseTime;
                float t = Time.time - start;
                float end = cue.Weight[cue.Weight.length - 1].time;
                cue.Volume.weight = t < 0f ? 0f : Mathf.Clamp01(cue.Weight.Evaluate(t));
                if (t < end) anyRunning = true;
            }

            if (anyRunning || _emitter.IsActive) return;
            _playing = false;
            SetAllWeights(0f);
            if (_camera != null) _camera.requiresColorOption = _cameraColorDefault;
        }

        private void SetAllWeights(float weight)
        {
            for (int i = 0; i < _cues.Length; i++)
            {
                if (_cues[i].Volume != null) _cues[i].Volume.weight = weight;
            }
        }
    }
}
