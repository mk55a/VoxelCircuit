using CylinderShooter.Effects;
using CylinderShooter.Enemies;
using UnityEditor;
using UnityEngine;

namespace CylinderShooter.Editor
{
    /// <summary>
    /// One-click setup for the Bomber (sea mine): import settings for its generated model and textures, its toon
    /// material, tuning assets and the prefab. Assets that already exist are kept as they are, so re-running it never
    /// overwrites tuning. To rebuild the prefab from scratch, delete it first.
    /// The model files come from Tools/VoxelModels/bomber.py.
    /// </summary>
    public static class BomberPrefabBuilder
    {
        private const string ArtFolder = "Assets/Art/Enemies/Bomber";
        private const string ModelPath = ArtFolder + "/Bomber.obj";
        private const string PalettePath = ArtFolder + "/Bomber_Palette.png";
        private const string EmissionPath = ArtFolder + "/Bomber_Emission.png";
        private const string VoxelDataPath = ArtFolder + "/Bomber_Voxels.json";
        private const string ChaserMaterialPath = ProjectPaths.MaterialsFolder + "/Enemies/Chaser_Toon.mat";
        private const string MaterialPath = ProjectPaths.MaterialsFolder + "/Enemies/Bomber_Toon.mat";
        private const string TuningPath = ProjectPaths.TuningFolder + "/Enemies/BomberTuning.asset";
        private const string LampProfilePath = ProjectPaths.TuningFolder + "/Enemies/Bomber_Lamps.asset";
        private const string DebrisProfilePath = ProjectPaths.TuningFolder + "/Effects/Debris_Chaser.asset";
        private const string PopPulsePath = ProjectPaths.TuningFolder + "/Effects/Pulse_Vaporize.asset";
        private const string BlastPulsePath = ProjectPaths.TuningFolder + "/Effects/Pulse_Blast.asset";

        [MenuItem("Cylinder Shooter/Enemies/Build Bomber Prefab")]
        public static void Build()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Build the Bomber prefab outside Play mode.");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null)
            {
                Debug.LogError($"Bomber model not found at {ModelPath}. Run Tools/VoxelModels/bomber.py first.");
                return;
            }

            ConfigurePixelTexture(PalettePath);
            ConfigurePixelTexture(EmissionPath);
            var material = LoadOrCreateMaterial();
            if (material == null) return;
            ConfigureModel(material);

            var tuning = LoadOrCreate<BomberTuning>(TuningPath);
            var lamps = LoadOrCreate<FuseLampProfile>(LampProfilePath);
            var blastPulse = LoadOrCreateBlastPulse();
            var debris = AssetDatabase.LoadAssetAtPath<DebrisProfile>(DebrisProfilePath);
            var popPulse = AssetDatabase.LoadAssetAtPath<PulseProfile>(PopPulsePath);
            var voxels = AssetDatabase.LoadAssetAtPath<TextAsset>(VoxelDataPath);
            if (debris == null || popPulse == null || voxels == null || blastPulse == null)
            {
                Debug.LogError($"Bomber prefab needs {DebrisProfilePath}, {PopPulsePath}, {BlastPulsePath} and {VoxelDataPath}.");
                return;
            }

            string prefabPath = ProjectPaths.BomberPrefab;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"{prefabPath} already exists and was left unchanged (delete it to rebuild). " +
                          "Material, import settings and tuning assets were checked.");
                return;
            }
            BuildPrefab(prefabPath, tuning, lamps, blastPulse, debris, popPulse, voxels);
        }

        private static void BuildPrefab(string path, BomberTuning tuning, FuseLampProfile lamps, PulseProfile blastPulse,
            DebrisProfile debris, PulseProfile popPulse, TextAsset voxels)
        {
            int layer = LayerMask.NameToLayer("Gameplay");
            if (layer < 0) layer = 6;

            var root = new GameObject("Bomber") { layer = layer };
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, root.transform);
            model.name = "Bomber";
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            var renderer = model.GetComponentInChildren<MeshRenderer>();

            // Bomber first, so dragging the prefab into an object field picks it (dragging picks the first script).
            var bomber = root.AddComponent<Bomber>();
            Set(bomber, "_tuning", tuning);

            var shatter = root.AddComponent<ShatterOnDestroyed>();
            Set(shatter, "_voxelData", voxels);
            Set(shatter, "_model", model.transform);
            Set(shatter, "_profile", debris);

            var pop = root.AddComponent<PulseOnDestroyed>();
            Set(pop, "_profile", popPulse);
            Set(pop, "_model", model.transform);

            var blast = root.AddComponent<PulseOnDetonated>();
            Set(blast, "_profile", blastPulse);

            var fuseLamps = root.AddComponent<FuseLamps>();
            Set(fuseLamps, "_fuseSource", bomber);
            Set(fuseLamps, "_renderer", renderer);
            Set(fuseLamps, "_profile", lamps);

            var spin = root.AddComponent<ChaserVisualSpin>();
            var so = new SerializedObject(spin);
            so.FindProperty("_model").objectReferenceValue = model.transform;
            so.FindProperty("_degreesPerSecond").floatValue = 40f;
            so.FindProperty("_speedVariation").floatValue = 15f;
            so.FindProperty("_axis").vector3Value = Vector3.up;
            so.ApplyModifiedPropertiesWithoutUndo();

            ProjectPaths.EnsureFolder(ProjectPaths.FolderOf(path));
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"Built {path}.");
        }

        private static void ConfigurePixelTexture(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;
            bool changed = false;
            if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; changed = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }
            if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; changed = true; }
            if (changed) importer.SaveAndReimport();
        }

        /// <summary>A copy of the Chaser's toon material (same rim, outline and bands) with the Bomber's textures.</summary>
        private static Material LoadOrCreateMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null) return existing;
            var chaser = AssetDatabase.LoadAssetAtPath<Material>(ChaserMaterialPath);
            if (chaser == null)
            {
                Debug.LogError($"Bomber material is copied from {ChaserMaterialPath}, which is missing.");
                return null;
            }
            var material = new Material(chaser) { name = "Bomber_Toon" };
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(PalettePath));
            material.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(EmissionPath));
            material.SetColor("_EmissionColor", new Color(0.6f, 0.45f, 0.25f));
            ProjectPaths.EnsureFolder(ProjectPaths.FolderOf(MaterialPath));
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static void ConfigureModel(Material material)
        {
            if (AssetImporter.GetAtPath(ModelPath) is not ModelImporter importer) return;
            var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), "Bomber");
            importer.GetExternalObjectMap().TryGetValue(id, out var current);
            bool changed = current != material || importer.importCameras || importer.importLights ||
                           importer.addCollider;
            if (!changed) return;
            importer.AddRemap(id, material);
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.SaveAndReimport();
        }

        private static PulseProfile LoadOrCreateBlastPulse()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PulseProfile>(BlastPulsePath);
            if (existing != null) return existing;
            var pulse = ScriptableObject.CreateInstance<PulseProfile>();
            var so = new SerializedObject(pulse);
            so.FindProperty("_color").colorValue = new Color(1f, 0.5f, 0.16f);   // enemy orange, not player cyan
            so.FindProperty("_flashSize").floatValue = 0.6f;
            so.FindProperty("_flashLifetime").floatValue = 0.08f;
            so.FindProperty("_ringSize").floatValue = 1f;                         // ring reaches the blast radius
            so.FindProperty("_ringLifetime").floatValue = 0.3f;
            so.FindProperty("_sparkCount").intValue = 18;
            so.FindProperty("_sparkSpeed").vector2Value = new Vector2(10f, 22f);
            so.FindProperty("_sparkLifetime").vector2Value = new Vector2(0.2f, 0.4f);
            so.FindProperty("_sparkSize").floatValue = 0.1f;
            so.ApplyModifiedPropertiesWithoutUndo();
            ProjectPaths.EnsureFolder(ProjectPaths.FolderOf(BlastPulsePath));
            AssetDatabase.CreateAsset(pulse, BlastPulsePath);
            return pulse;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            ProjectPaths.EnsureFolder(ProjectPaths.FolderOf(path));
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Entry point for building from the command line (batch mode).</summary>
        public static void BuildAndSave()
        {
            Build();
            AssetDatabase.SaveAssets();
        }
    }
}
