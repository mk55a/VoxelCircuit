using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CylinderShooter.Rendering.Outline
{
    /// <summary>Inspector settings for <see cref="GameplayOutlineFeature"/>, stored on the renderer asset.</summary>
    [Serializable]
    public sealed class GameplayOutlineSettings
    {
        [Tooltip("Layers that get an outline. Their materials need a GameplayOutlineMask pass " +
                 "(VoxelCircuit/Gameplay Toon has one); the outline uses each material's Outline Colour.")]
        [SerializeField] private LayerMask _layers;

        [Tooltip("Material using VoxelCircuit/Internal/Gameplay Outline Composite. Its Thickness sets the width in pixels.")]
        [SerializeField] private Material _compositeMaterial;

        [Tooltip("Before post-processing, so the outline is tonemapped and graded like the rest of the frame.")]
        [SerializeField] private RenderPassEvent _passEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        public LayerMask Layers => _layers;
        public Material CompositeMaterial => _compositeMaterial;
        public RenderPassEvent PassEvent => _passEvent;
        public bool IsValid => _compositeMaterial != null && _layers.value != 0;
    }
}
