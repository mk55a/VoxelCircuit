using CylinderShooter.Arena;
using CylinderShooter.Cam;
using CylinderShooter.Formations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CylinderShooter.Editor
{
    /// <summary>
    /// Turns the arena into see-through glass: adds a smooth 360-degree glass wall (a CylinderBand), hides the old
    /// opaque arena's renderer (the object is kept), hides the opaque test-section wall if present (its floor stays),
    /// and tells the formation spawner that far-away points count as hidden, since glass no longer blocks the view.
    /// Every change is undoable. The glass material is only created if it doesn't exist, so tuning is never lost.
    /// </summary>
    public static class ArenaGlassBuilder
    {
        private const string CylinderPath = "Assets/Data/CylinderConfig.asset";
        private const string ArtFolder = "Assets/Art/Arena";
        private const string GlassName = "Arena Glass";

        [MenuItem("Cylinder Shooter/Arena/Make Arena Glass")]
        public static void MakeArenaGlass()
        {
            var cylinder = AssetDatabase.LoadAssetAtPath<CylinderConfig>(CylinderPath);
            var material = LoadOrCreateGlassMaterial();
            if (cylinder == null || material == null)
            {
                Debug.LogError($"Arena glass requires a cylinder config at {CylinderPath} and the glass textures in {ArtFolder}.");
                return;
            }

            Undo.SetCurrentGroupName("Make Arena Glass");
            int undoGroup = Undo.GetCurrentGroup();

            var glass = GameObject.Find(GlassName);
            if (glass == null) glass = CreateGlassWall(cylinder, material);

            HideRenderer("Arena", "the old opaque arena");
            var sectionWall = GameObject.Find("Arena Section/Wall");
            if (sectionWall != null && sectionWall.activeSelf)
            {
                Undo.RecordObject(sectionWall, "Hide opaque section wall");
                sectionWall.SetActive(false);
                Debug.Log("Arena glass: hid the opaque test-section wall (its floor is kept).");
            }

            ConfigureSpawner(cylinder);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = glass;
        }

        private static GameObject CreateGlassWall(CylinderConfig cylinder, Material material)
        {
            var go = new GameObject(GlassName, typeof(MeshFilter), typeof(MeshRenderer));
            Undo.RegisterCreatedObjectUndo(go, "Create arena glass");
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var band = go.AddComponent<CylinderBand>();
            var so = new SerializedObject(band);
            so.FindProperty("_cylinder").objectReferenceValue = cylinder;
            so.FindProperty("_shape").enumValueIndex = (int)CylinderBandShape.Wall;
            so.FindProperty("_radiusOffset").floatValue = -0.4f;          // just behind the ships
            so.FindProperty("_bottom").floatValue = cylinder.MinHeight - 1f; // street level / debris floor
            so.FindProperty("_top").floatValue = cylinder.MaxHeight + 4f;
            so.FindProperty("_startAngleDegrees").floatValue = 0f;
            so.FindProperty("_arcDegrees").floatValue = 360f;
            so.FindProperty("_tileWidth").floatValue = 16f;
            so.FindProperty("_tileHeight").floatValue = 0f;               // one texture over the full height
            so.ApplyModifiedPropertiesWithoutUndo();
            band.Rebuild();
            return go;
        }

        private static void HideRenderer(string objectName, string description)
        {
            var go = GameObject.Find(objectName);
            var renderer = go != null ? go.GetComponent<Renderer>() : null;
            if (renderer == null || !renderer.enabled) return;
            Undo.RecordObject(renderer, $"Hide {description}");
            renderer.enabled = false;
            Debug.Log($"Arena glass: hid the renderer of {description} ('{objectName}'). The object is kept.");
        }

        /// <summary>
        /// With glass, the far side of the ring is no longer blocked from view, so the spawner treats anything at least
        /// as far as a point a quarter-turn round the ring as hidden (fog will make that literally true).
        /// Only set if the value is still 0, so a tuned value is kept.
        /// </summary>
        private static void ConfigureSpawner(CylinderConfig cylinder)
        {
            var spawner = Object.FindFirstObjectByType<FormationTestSpawner>();
            if (spawner == null) return;

            var so = new SerializedObject(spawner);
            var hiddenBeyond = so.FindProperty("_hiddenBeyondDistance");
            if (hiddenBeyond == null || hiddenBeyond.floatValue > 0f) return;

            float cameraDistance = 22f;
            var follow = Object.FindFirstObjectByType<CylinderFollowCamera>();
            if (follow != null)
            {
                var distance = new SerializedObject(follow).FindProperty("_distance");
                if (distance != null) cameraDistance = distance.floatValue;
            }
            float cameraRadius = cylinder.Radius + cameraDistance;
            float quarterTurn = Mathf.Sqrt(cameraRadius * cameraRadius + cylinder.Radius * cylinder.Radius);

            hiddenBeyond.floatValue = Mathf.Round(quarterTurn);
            so.ApplyModifiedProperties();
            Debug.Log($"Arena glass: formation spawner now treats points beyond {hiddenBeyond.floatValue} m as hidden.");
        }

        /// <summary>URP/Lit set to transparent alpha blending: faint dark tint, glowing frame lines, sharp highlights.</summary>
        private static Material LoadOrCreateGlassMaterial()
        {
            string path = $"{ArtFolder}/Arena_Glass.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var baseMap = PrepareTexture($"{ArtFolder}/Arena_Glass.png", true);
            var emissionMap = PrepareTexture($"{ArtFolder}/Arena_Glass_Emission.png", false);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (baseMap == null || emissionMap == null || shader == null) return null;

            var m = new Material(shader) { name = "Arena_Glass" };
            m.SetTexture("_BaseMap", baseMap);
            m.SetColor("_BaseColor", new Color(0.03f, 0.04f, 0.10f, 1f));   // alpha comes from the texture
            m.SetTexture("_EmissionMap", emissionMap);
            m.SetColor("_EmissionColor", new Color(0.23f, 0.29f, 1f) * 0.35f); // faint blue-violet frame lines
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", 0.92f);
            m.SetFloat("_EnvironmentReflections", 0f);    // no sky reflections in a dark scene
            m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");

            // Transparent, alpha-blended (the same values URP's material Inspector sets for Surface Type = Transparent).
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", (float)CullMode.Back);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("DepthOnly", false);
            m.SetShaderPassEnabled("ShadowCaster", false);

            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static Texture2D PrepareTexture(string path, bool hasAlpha)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return null;
            bool changed = false;
            if (importer.textureType != TextureImporterType.Default) { importer.textureType = TextureImporterType.Default; changed = true; }
            if (importer.wrapMode != TextureWrapMode.Repeat) { importer.wrapMode = TextureWrapMode.Repeat; changed = true; }
            if (hasAlpha && !importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
            if (importer.anisoLevel < 4) { importer.anisoLevel = 4; changed = true; }
            if (changed) importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
