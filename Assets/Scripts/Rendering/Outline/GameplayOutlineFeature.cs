using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CylinderShooter.Rendering.Outline
{
    /// <summary>
    /// Renderer feature that outlines every object on the configured gameplay layers.
    /// Add it to the URP renderer asset; all settings live there, nothing is created in the scene.
    /// </summary>
    public sealed class GameplayOutlineFeature : ScriptableRendererFeature
    {
        [SerializeField] private GameplayOutlineSettings _settings = new GameplayOutlineSettings();

        private GameplayOutlinePass _pass;

        public override void Create()
        {
            _pass = new GameplayOutlinePass(_settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!_settings.IsValid || !IsGameCamera(renderingData.cameraData.cameraType)) return;
            renderer.EnqueuePass(_pass);
        }

        private static bool IsGameCamera(CameraType type)
        {
            return type == CameraType.Game || type == CameraType.SceneView;
        }
    }
}
