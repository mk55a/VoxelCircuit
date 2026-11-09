using System;
using System.Collections.Generic;
using UnityEngine;

namespace CylinderShooter.Effects
{
    /// <summary>
    /// The cubes a model is made of, read from its *_Voxels.json (positions in the model's local space,
    /// metres, plus a colour per cube). Parsed once per file and cached.
    /// </summary>
    public sealed class VoxelShape
    {
        [Serializable] private class PaletteEntry { public string name; public string hex; public bool emissive; }
        [Serializable] private class VoxelEntry { public float[] p; public int c; }
        [Serializable] private class Data { public string name; public float voxelSize; public PaletteEntry[] palette; public VoxelEntry[] voxels; }

        private static readonly Dictionary<int, VoxelShape> s_cache = new Dictionary<int, VoxelShape>();

        public string Name { get; }
        public float VoxelSize { get; }
        public Vector3[] Positions { get; }
        public Color32[] Colors { get; }
        public int Count => Positions.Length;

        private VoxelShape(string name, float voxelSize, Vector3[] positions, Color32[] colors)
        {
            Name = name;
            VoxelSize = voxelSize;
            Positions = positions;
            Colors = colors;
        }

        public static VoxelShape Load(TextAsset source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            int key = source.GetInstanceID();
            if (!s_cache.TryGetValue(key, out var shape))
            {
                shape = Parse(source.text);
                s_cache[key] = shape;
            }
            return shape;
        }

        public static VoxelShape Parse(string json)
        {
            var data = JsonUtility.FromJson<Data>(json);
            if (data == null || data.voxels == null || data.palette == null || data.voxelSize <= 0f)
                throw new FormatException("Voxel data is missing voxels, palette or a positive voxelSize.");

            var palette = new Color32[data.palette.Length];
            for (int i = 0; i < palette.Length; i++)
            {
                if (!ColorUtility.TryParseHtmlString(data.palette[i].hex, out Color c))
                    throw new FormatException($"Bad palette colour '{data.palette[i].hex}'.");
                palette[i] = c;
            }

            var positions = new Vector3[data.voxels.Length];
            var colors = new Color32[data.voxels.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                var v = data.voxels[i];
                if (v.p == null || v.p.Length != 3 || v.c < 0 || v.c >= palette.Length)
                    throw new FormatException($"Voxel {i} has a bad position or colour index.");
                positions[i] = new Vector3(v.p[0], v.p[1], v.p[2]);
                colors[i] = palette[v.c];
            }
            return new VoxelShape(data.name, data.voxelSize, positions, colors);
        }
    }
}
