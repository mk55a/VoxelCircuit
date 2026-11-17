using CylinderShooter.Arena;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CylinderShooter.Editor
{
    /// <summary>
    /// One-click setup for the arena visual pass: configures the arena textures, creates the wall and floor
    /// materials (only if they don't exist yet, so tuning is never overwritten), and adds a test section to the
    /// open scene. Existing scene objects, including the old arena the formation spawner hides behind, are untouched.
    /// </summary>
    public static class ArenaSectionBuilder
    {
        private const string CylinderPath = "Assets/Data/CylinderConfig.asset";
        private const string ArtFolder = "Assets/Art/Arena";
        private const string SectionName = "Arena Section";

        [MenuItem("Cylinder Shooter/Arena/Create Test Section")]
        public static void CreateTestSection()
        {
            var existing = GameObject.Find(SectionName);
            if (existing != null)
            {
                Selection.activeGameObject = existing;
                Debug.Log("An Arena Section already exists in this scene; selected it instead of creating another.");
                return;
            }

            var cylinder = AssetDatabase.LoadAssetAtPath<CylinderConfig>(CylinderPath);
            if (cylinder == null)
            {
                Debug.LogError($"Arena section requires a cylinder config at {CylinderPath}.");
                return;
            }

            var wallMaterial = LoadOrCreateMaterial("Arena_Wall", new Color(1f, 1f, 1f), 0.9f);
            var floorMaterial = LoadOrCreateMaterial("Arena_Floor", new Color(0.85f, 0.85f, 0.9f), 0.9f);
            if (wallMaterial == null || floorMaterial == null) return;

            var root = new GameObject(SectionName);
            Undo.RegisterCreatedObjectUndo(root, "Create Arena Section");

            var wall = CreateBand(root.transform, "Wall", cylinder, wallMaterial);
            Configure(wall, CylinderBandShape.Wall, -0.4f, cylinder.MinHeight - 1f);
            var floor = CreateBand(root.transform, "Floor", cylinder, floorMaterial);
            Configure(floor, CylinderBandShape.Ring, -0.4f, cylinder.MinHeight - 1f);

            Selection.activeGameObject = root;
        }

        private static CylinderBand CreateBand(Transform parent, string name, CylinderConfig cylinder, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            var band = go.AddComponent<CylinderBand>();
            var so = new SerializedObject(band);
            so.FindProperty("_cylinder").objectReferenceValue = cylinder;
            so.ApplyModifiedPropertiesWithoutUndo();
            return band;
        }

        private static void Configure(CylinderBand band, CylinderBandShape shape, float radiusOffset, float bottom)
        {
            var so = new SerializedObject(band);
            so.FindProperty("_shape").enumValueIndex = (int)shape;
            so.FindProperty("_radiusOffset").floatValue = radiusOffset;
            so.FindProperty("_bottom").floatValue = bottom;
            so.ApplyModifiedPropertiesWithoutUndo();
            band.Rebuild();
        }

        /// <summary>Creates a URP/Lit material from the three arena textures, or returns the existing one untouched.</summary>
        private static Material LoadOrCreateMaterial(string name, Color baseColor, float emissionStrength)
        {
            string path = $"{ArtFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var albedo = PrepareTexture($"{ArtFolder}/{name}_Albedo.png", false);
            var normal = PrepareTexture($"{ArtFolder}/{name}_Normal.png", true);
            var emission = PrepareTexture($"{ArtFolder}/{name}_Emission.png", false);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (albedo == null || normal == null || emission == null || shader == null)
            {
                Debug.LogError($"Arena material {name} requires its three textures in {ArtFolder} and the URP Lit shader.");
                return null;
            }

            var material = new Material(shader) { name = name };
            material.SetTexture("_BaseMap", albedo);
            material.SetColor("_BaseColor", baseColor);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 1f);
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_EmissionMap", emission);
            // Kept below 1 so the accents read as lit panels rather than blooming neon.
            material.SetColor("_EmissionColor", Color.white * emissionStrength);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            material.SetFloat("_Metallic", 0.45f);
            material.SetFloat("_Smoothness", 0.35f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>Repeat wrapping, mipmaps and anisotropic filtering for surfaces seen at grazing angles.</summary>
        private static Texture2D PrepareTexture(string path, bool isNormalMap)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return null;

            bool changed = false;
            var type = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType != type) { importer.textureType = type; changed = true; }
            if (importer.wrapMode != TextureWrapMode.Repeat) { importer.wrapMode = TextureWrapMode.Repeat; changed = true; }
            if (!importer.mipmapEnabled) { importer.mipmapEnabled = true; changed = true; }
            if (importer.anisoLevel < 4) { importer.anisoLevel = 4; changed = true; }
            if (changed) importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
