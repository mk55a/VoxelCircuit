using CylinderShooter.Arena;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CylinderShooter.Editor
{
    /// <summary>
    /// One-click setup for the "low cloud, city glow" sky: two drifting cloud rings around the arena (far and near,
    /// for parallax), using the Cloud Band shader. Materials are only created if missing, so tuning is never lost.
    /// Existing rings are selected instead of duplicated. Undoable.
    /// </summary>
    public static class EnvironmentBuilder
    {
        private const string CylinderPath = ProjectPaths.CylinderConfig;
        private const string Folder = ProjectPaths.EnvironmentArtFolder;
        private const string MaterialFolder = ProjectPaths.EnvironmentMaterialsFolder;
        private const string CloudTexturePath = Folder + "/Clouds_LowBand.png";
        private const string ShaderName = "VoxelCircuit/Cloud Band";

        private readonly struct RingSettings
        {
            public RingSettings(string name, float radiusOffset, float bottom, float top, float tileWidth,
                float opacity, float drift, float textureOffset)
            {
                Name = name; RadiusOffset = radiusOffset; Bottom = bottom; Top = top; TileWidth = tileWidth;
                Opacity = opacity; Drift = drift; TextureOffset = textureOffset;
            }

            public string Name { get; }
            public float RadiusOffset { get; }
            public float Bottom { get; }
            public float Top { get; }
            public float TileWidth { get; }
            public float Opacity { get; }
            public float Drift { get; }
            public float TextureOffset { get; }
        }

        // Radii are offsets from the play surface (radius 60): far ring at 280 m, near ring at 180 m.
        private static readonly RingSettings Far = new RingSettings("Clouds Far", 220f, 10f, 170f, 300f, 1f, 0.0015f, 0f);
        private static readonly RingSettings Near = new RingSettings("Clouds Near", 120f, 5f, 110f, 220f, 0.55f, 0.004f, 0.37f);

        [MenuItem("Cylinder Shooter/Environment/Create Cloud Rings")]
        public static void CreateCloudRings()
        {
            var cylinder = AssetDatabase.LoadAssetAtPath<CylinderConfig>(CylinderPath);
            var texture = PrepareCloudTexture();
            var shader = Shader.Find(ShaderName);
            if (cylinder == null || texture == null || shader == null)
            {
                Debug.LogError($"Cloud rings require {CylinderPath}, {CloudTexturePath} and the '{ShaderName}' shader.");
                return;
            }

            Undo.SetCurrentGroupName("Create Cloud Rings");
            int group = Undo.GetCurrentGroup();
            var root = GameObject.Find("Environment");
            if (root == null)
            {
                root = new GameObject("Environment");
                Undo.RegisterCreatedObjectUndo(root, "Create Environment root");
            }

            GameObject last = null;
            foreach (var ring in new[] { Far, Near })
                last = FindOrCreateRing(root.transform, ring, cylinder, LoadOrCreateMaterial(ring, shader, texture));

            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = last != null ? last : root;
        }

        private static GameObject FindOrCreateRing(Transform parent, RingSettings ring, CylinderConfig cylinder, Material material)
        {
            var existing = parent.Find(ring.Name);
            if (existing != null) return existing.gameObject;

            var go = new GameObject(ring.Name, typeof(MeshFilter), typeof(MeshRenderer));
            Undo.RegisterCreatedObjectUndo(go, $"Create {ring.Name}");
            go.transform.SetParent(parent, false);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var band = go.AddComponent<CylinderBand>();
            var so = new SerializedObject(band);
            so.FindProperty("_cylinder").objectReferenceValue = cylinder;
            so.FindProperty("_shape").enumValueIndex = (int)CylinderBandShape.Wall;
            so.FindProperty("_radiusOffset").floatValue = ring.RadiusOffset;
            so.FindProperty("_bottom").floatValue = ring.Bottom;
            so.FindProperty("_top").floatValue = ring.Top;
            so.FindProperty("_startAngleDegrees").floatValue = 0f;
            so.FindProperty("_arcDegrees").floatValue = 360f;
            so.FindProperty("_tileWidth").floatValue = ring.TileWidth;
            so.FindProperty("_tileHeight").floatValue = 0f;
            so.FindProperty("_maxSegmentLength").floatValue = 4f;     // smooth enough at these radii
            so.FindProperty("_faceInward").boolValue = true;           // seen from inside the ring
            so.ApplyModifiedPropertiesWithoutUndo();
            band.Rebuild();
            return go;
        }

        private static Material LoadOrCreateMaterial(RingSettings ring, Shader shader, Texture2D texture)
        {
            string path = $"{MaterialFolder}/{ring.Name.Replace(' ', '_')}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            material = new Material(shader) { name = ring.Name };
            material.SetTexture("_BaseMap", texture);
            material.SetTextureOffset("_BaseMap", new Vector2(ring.TextureOffset, 0f));   // different clouds per ring
            material.SetFloat("_Opacity", ring.Opacity);
            material.SetFloat("_ScrollSpeed", ring.Drift);
            ProjectPaths.EnsureFolder(MaterialFolder);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Texture2D PrepareCloudTexture()
        {
            if (AssetImporter.GetAtPath(CloudTexturePath) is not TextureImporter importer) return null;
            bool changed = false;
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
            if (importer.wrapModeU != TextureWrapMode.Repeat) { importer.wrapModeU = TextureWrapMode.Repeat; changed = true; }
            if (importer.wrapModeV != TextureWrapMode.Clamp) { importer.wrapModeV = TextureWrapMode.Clamp; changed = true; }
            if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; changed = true; }   // keep blocky edges
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }
            if (changed) importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(CloudTexturePath);
        }
    }
}
