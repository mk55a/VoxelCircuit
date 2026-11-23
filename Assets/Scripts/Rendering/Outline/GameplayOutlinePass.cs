using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace CylinderShooter.Rendering.Outline
{
    /// <summary>
    /// Two render graph passes:
    /// 1. Mask: draws gameplay objects with their GameplayOutlineMask pass (outline colour, alpha 1) into a temporary
    ///    texture, depth-tested against the scene, so parts hidden behind other geometry are left out.
    /// 2. Composite: a full-screen pass paints that colour around each object's visible silhouette onto the camera colour.
    /// </summary>
    internal sealed class GameplayOutlinePass : ScriptableRenderPass
    {
        private const string MaskTextureName = "_GameplayOutlineMask";
        private static readonly ShaderTagId MaskShaderTag = new ShaderTagId("GameplayOutlineMask");
        private static readonly Vector4 FullScreenScaleBias = new Vector4(1f, 1f, 0f, 0f);

        private readonly GameplayOutlineSettings _settings;

        private sealed class MaskPassData
        {
            public RendererListHandle Renderers;
        }

        private sealed class CompositePassData
        {
            public TextureHandle Mask;
            public Material Material;
        }

        public GameplayOutlinePass(GameplayOutlineSettings settings)
        {
            _settings = settings;
            renderPassEvent = settings.PassEvent;
            profilingSampler = new ProfilingSampler("Gameplay Outline");
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (resources.isActiveTargetBackBuffer) return;

            TextureHandle mask = RecordMask(renderGraph, frameData, resources.activeDepthTexture);
            RecordComposite(renderGraph, mask, resources.activeColorTexture);
        }

        private TextureHandle RecordMask(RenderGraph renderGraph, ContextContainer frameData, TextureHandle sceneDepth)
        {
            var renderingData = frameData.Get<UniversalRenderingData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var lightData = frameData.Get<UniversalLightData>();

            RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
            descriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
            descriptor.depthStencilFormat = GraphicsFormat.None;
            // Keeps the camera's MSAA sample count so the mask matches the scene depth it is tested against.
            TextureHandle mask = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, MaskTextureName, true);

            DrawingSettings drawing = RenderingUtils.CreateDrawingSettings(
                MaskShaderTag, renderingData, cameraData, lightData, SortingCriteria.CommonOpaque);
            var filtering = new FilteringSettings(RenderQueueRange.opaque, _settings.Layers);
            var rendererList = new RendererListParams(renderingData.cullResults, drawing, filtering);

            using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>("Gameplay Outline Mask", out var data, profilingSampler))
            {
                data.Renderers = renderGraph.CreateRendererList(rendererList);
                builder.UseRendererList(data.Renderers);
                builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                // Read-only: the mask is depth-tested against the scene but never changes the scene's depth.
                builder.SetRenderAttachmentDepth(sceneDepth, AccessFlags.Read);
                builder.SetRenderFunc(static (MaskPassData pass, RasterGraphContext context) =>
                    context.cmd.DrawRendererList(pass.Renderers));
            }
            return mask;
        }

        private void RecordComposite(RenderGraph renderGraph, TextureHandle mask, TextureHandle cameraColor)
        {
            using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>("Gameplay Outline Composite", out var data, profilingSampler))
            {
                data.Mask = mask;
                data.Material = _settings.CompositeMaterial;
                builder.UseTexture(mask, AccessFlags.Read);
                // ReadWrite: the outline is blended over the existing frame, so its contents must be kept.
                builder.SetRenderAttachment(cameraColor, 0, AccessFlags.ReadWrite);
                builder.SetRenderFunc(static (CompositePassData pass, RasterGraphContext context) =>
                    Blitter.BlitTexture(context.cmd, pass.Mask, FullScreenScaleBias, pass.Material, 0));
            }
        }
    }
}
