using System;
using System.Collections.Generic;
using System.Text;
using CylinderShooter.City;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CylinderShooter.Editor
{
    [CustomEditor(typeof(CityGenerator))]
    public sealed class CityGeneratorEditor : UnityEditor.Editor
    {
        [MenuItem("Cylinder Shooter/City/Add Generator To Selected Plane")]
        private static void AddGenerator()
        {
            var selected = Selection.activeGameObject;
            if (Application.isPlaying || selected == null || !selected.scene.IsValid() ||
                selected.GetComponent<MeshFilter>() == null)
            {
                Debug.LogError("Select a scene plane with a MeshFilter outside Play mode.");
                return;
            }
            if (selected.GetComponent<CityGenerator>() != null)
            {
                Debug.LogWarning("The selected plane already has a city generator.", selected);
                return;
            }
            var generator = Undo.AddComponent<CityGenerator>(selected);
            var so = new SerializedObject(generator);
            so.FindProperty("_plane").objectReferenceValue = selected.GetComponent<MeshFilter>();
            so.FindProperty("_cylinder").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<CylinderConfig>(ProjectPaths.CylinderConfig);
            so.ApplyModifiedProperties();
            Selection.activeGameObject = selected;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var generator = (CityGenerator)target;
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button("Randomize Seed"))
                {
                    var seed = serializedObject.FindProperty("_seed");
                    seed.intValue = Guid.NewGuid().GetHashCode();
                    serializedObject.ApplyModifiedProperties();
                }
                if (GUILayout.Button("Generate City"))
                {
                    if (!CityGeneration.TryPlan(generator, out var plan, out var reason))
                    {
                        Debug.LogWarning("City generation cancelled: " + reason, generator);
                        EditorUtility.DisplayDialog("City generation cancelled", reason, "OK");
                    }
                    else
                    {
                        var text = new StringBuilder();
                        text.AppendLine($"Generate {plan.Buildings.Count} buildings?");
                        text.AppendLine($"This will remove {plan.Replaced.Count} prefab instance(s) directly under {generator.Plane.transform.parent.name}, regardless of prefab type:");
                        foreach (var item in plan.Replaced) text.AppendLine("- " + item.name);
                        text.AppendLine("The previous generated city will also be replaced. Undo restores the previous scene.");
                        if (EditorUtility.DisplayDialog("Replace city", text.ToString(), "Generate", "Cancel"))
                            CityGeneration.Apply(generator, plan);
                    }
                }
                if (GUILayout.Button("Clear Generated City"))
                    CityGeneration.Clear(generator);
            }
        }
    }

    public static class CityGeneration
    {
        private const int MaxCandidates = 40000;
        private const int MaxBuildings = 2000;

        public sealed class Building
        {
            public GameObject Prefab;
            public Vector3 Position;
        }

        public sealed class Plan
        {
            public readonly List<Building> Buildings = new List<Building>();
            public readonly List<GameObject> Replaced = new List<GameObject>();
        }

        private sealed class PrefabInfo
        {
            public GameObject Prefab;
            public Bounds Bounds;
        }

        public static bool TryPlan(CityGenerator generator, out Plan plan, out string reason)
        {
            plan = null;
            if (generator == null || generator.Plane == null || generator.Plane.sharedMesh == null ||
                generator.Cylinder == null || generator.Plane.transform != generator.transform ||
                generator.transform.parent == null || !generator.gameObject.scene.IsValid())
                return Fail("Assign the plane on the generator's own scene object and a cylinder config; the plane needs a parent and mesh.", out reason);
            if (generator.GeneratedRoot != null && generator.GeneratedRoot.transform.parent != generator.transform)
                return Fail("The generated root is no longer a direct child of the plane; restore it before generating.", out reason);
            if (generator.MinimumBuildings < 0 || generator.MaximumBuildings < generator.MinimumBuildings ||
                generator.MaximumBuildings > MaxBuildings || generator.BuildingPrefabs == null ||
                generator.BuildingPrefabs.Length == 0 || generator.BlockSize < 1 ||
                !Enum.IsDefined(typeof(CityLayout), generator.Layout))
                return Fail("Check the building list, count range (maximum 2000), layout and block size.", out reason);
            if (!Finite(generator.Cylinder.Radius) || !Finite(generator.BoundaryMargin) ||
                !Finite(generator.Clearance) || !Finite(generator.CellSpacing) ||
                !Finite(generator.StreetWidth) || !Finite(generator.NoiseScale) ||
                !Finite(generator.NoiseThreshold) || generator.BoundaryMargin < 0f ||
                generator.Clearance < 0f || generator.CellSpacing < 0.1f || generator.StreetWidth < 0f ||
                generator.NoiseScale < 0.1f || generator.NoiseThreshold < 0f || generator.NoiseThreshold > 1f)
                return Fail("Spacing, radius, margin and noise settings must be finite and within their Inspector ranges.", out reason);

            var plane = generator.Plane.transform;
            for (var current = plane; current != null; current = current.parent)
                if (current.localScale.x <= 0f || current.localScale.y <= 0f || current.localScale.z <= 0f)
                    return Fail("The plane and its parents must have positive scales.", out reason);
            var matrix = plane.localToWorldMatrix;
            Vector3 x = matrix.MultiplyVector(Vector3.right);
            Vector3 y = matrix.MultiplyVector(Vector3.up);
            Vector3 z = matrix.MultiplyVector(Vector3.forward);
            if (x.sqrMagnitude < 0.000001f || y.sqrMagnitude < 0.000001f || z.sqrMagnitude < 0.000001f ||
                Vector3.Dot(Vector3.Cross(z, x), y) <= 0f ||
                Mathf.Abs(Vector3.Dot(x.normalized, z.normalized)) > 0.0001f ||
                Vector3.Dot(y.normalized, Vector3.up) < 0.99999f ||
                Mathf.Abs(x.y) > 0.0001f || Mathf.Abs(z.y) > 0.0001f ||
                Mathf.Abs(x.magnitude - z.magnitude) > 0.0001f * Mathf.Max(x.magnitude, z.magnitude))
                return Fail("Use a horizontal, unmirrored plane with equal X/Z world scale and no shear.", out reason);

            float radius = generator.Cylinder.Radius - generator.BoundaryMargin;
            var planeBounds = generator.Plane.sharedMesh.bounds;
            Vector3 originLocal = plane.InverseTransformPoint(new Vector3(0f, plane.position.y, 0f));
            float localRadius = radius / x.magnitude;
            if (radius <= 0f || Mathf.Abs(originLocal.x - planeBounds.center.x) + localRadius > planeBounds.extents.x ||
                Mathf.Abs(originLocal.z - planeBounds.center.z) + localRadius > planeBounds.extents.z)
                return Fail("The plane must cover the entire city disk centered on the cylinder; check its size, position and boundary margin.", out reason);

            var prefabs = new List<PrefabInfo>();
            foreach (var prefab in generator.BuildingPrefabs)
            {
                if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab) ||
                    !TryMeasure(prefab, plane.rotation, out var bounds))
                    return Fail("Every building entry must be a prefab asset with enabled, visible mesh renderers and valid bounds.", out reason);
                prefabs.Add(new PrefabInfo { Prefab = prefab, Bounds = bounds });
            }

            var random = new System.Random(generator.Seed);
            int count = random.Next(generator.MinimumBuildings, generator.MaximumBuildings + 1);
            int steps = Mathf.CeilToInt(radius / generator.CellSpacing);
            if (steps > 99)
                return Fail("Cell spacing produces too many candidates; increase it.", out reason);
            var candidates = new List<Vector2>();
            float noiseX = (float)random.NextDouble() * 10000f;
            float noiseZ = (float)random.NextDouble() * 10000f;
            for (int ix = -steps; ix <= steps; ix++)
            for (int iz = -steps; iz <= steps; iz++)
            {
                float px = ix * generator.CellSpacing;
                float pz = iz * generator.CellSpacing;
                if (generator.Layout == CityLayout.GridStreets)
                {
                    px += Mathf.Floor((float)ix / generator.BlockSize) * generator.StreetWidth;
                    pz += Mathf.Floor((float)iz / generator.BlockSize) * generator.StreetWidth;
                }
                else if (Mathf.PerlinNoise(noiseX + px / generator.NoiseScale, noiseZ + pz / generator.NoiseScale)
                         < generator.NoiseThreshold) continue;
                if (px * px + pz * pz <= radius * radius) candidates.Add(new Vector2(px, pz));
            }
            if (candidates.Count > MaxCandidates)
                return Fail("Too many placement candidates; increase cell spacing.", out reason);
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                var swap = candidates[i]; candidates[i] = candidates[j]; candidates[j] = swap;
            }

            var result = new Plan();
            var occupied = new List<Rect>();
            float surfaceY = plane.TransformPoint(new Vector3(planeBounds.center.x, planeBounds.max.y, planeBounds.center.z)).y;
            foreach (var candidate in candidates)
            {
                if (result.Buildings.Count == count) break;
                int first = random.Next(prefabs.Count);
                for (int j = 0; j < prefabs.Count; j++)
                {
                    var info = prefabs[(first + j) % prefabs.Count];
                    float hx = info.Bounds.extents.x + generator.Clearance * 0.5f;
                    float hz = info.Bounds.extents.z + generator.Clearance * 0.5f;
                    if (generator.Layout == CityLayout.GridStreets &&
                        (hx > generator.CellSpacing * 0.5f || hz > generator.CellSpacing * 0.5f))
                        continue;
                    float farX = Mathf.Abs(candidate.x) + hx;
                    float farZ = Mathf.Abs(candidate.y) + hz;
                    if (farX * farX + farZ * farZ > radius * radius) continue;
                    var rect = new Rect(candidate.x - hx, candidate.y - hz, 2f * hx, 2f * hz);
                    bool overlap = false;
                    foreach (var other in occupied)
                        if (rect.Overlaps(other)) { overlap = true; break; }
                    if (overlap) continue;
                    occupied.Add(rect);
                    result.Buildings.Add(new Building
                    {
                        Prefab = info.Prefab,
                        Position = new Vector3(candidate.x - info.Bounds.center.x,
                            surfaceY - info.Bounds.min.y, candidate.y - info.Bounds.center.z)
                    });
                    break;
                }
            }
            if (result.Buildings.Count != count)
                return Fail($"Only {result.Buildings.Count} of {count} buildings fit. Reduce the count or building size, or adjust spacing, margin or noise threshold.", out reason);
            foreach (Transform child in plane.parent)
                if (child != plane && PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject))
                    result.Replaced.Add(child.gameObject);
            plan = result;
            reason = null;
            return true;
        }

        private static bool TryMeasure(GameObject prefab, Quaternion planeRotation, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            var root = prefab.transform;
            var rootMatrix = Matrix4x4.TRS(Vector3.zero, planeRotation * root.localRotation, root.localScale);
            foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!renderer.enabled || !IsActiveInPrefab(renderer.transform, root) ||
                    renderer.GetComponent<MeshFilter>() == null || renderer.GetComponent<MeshFilter>().sharedMesh == null)
                    continue;
                var localBounds = renderer.localBounds;
                var matrix = rootMatrix * root.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var point = matrix.MultiplyPoint3x4(localBounds.center + Vector3.Scale(localBounds.extents,
                        new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f)));
                    if (!Finite(point.x) || !Finite(point.y) || !Finite(point.z)) return false;
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                    else bounds.Encapsulate(point);
                }
            }
            return found && bounds.size.x > 0f && bounds.size.y > 0f && bounds.size.z > 0f;
        }

        private static bool IsActiveInPrefab(Transform child, Transform root)
        {
            for (var current = child; current != null; current = current.parent)
            {
                if (!current.gameObject.activeSelf) return false;
                if (current == root) return true;
            }
            return false;
        }

        public static void Apply(CityGenerator generator, Plan plan)
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Generate city");
            try
            {
                foreach (var item in plan.Replaced) Undo.DestroyObjectImmediate(item);
                if (generator.GeneratedRoot != null) Undo.DestroyObjectImmediate(generator.GeneratedRoot);
                var root = new GameObject("Generated City");
                Undo.RegisterCreatedObjectUndo(root, "Generate city");
                root.transform.SetParent(generator.transform, false);
                var scale = generator.transform.lossyScale;
                root.transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
                var serialized = new SerializedObject(generator);
                serialized.FindProperty("_generatedRoot").objectReferenceValue = root;
                serialized.ApplyModifiedProperties();
                foreach (var building in plan.Buildings)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(building.Prefab, generator.gameObject.scene);
                    if (instance == null) throw new InvalidOperationException("Could not instantiate " + building.Prefab.name);
                    Undo.RegisterCreatedObjectUndo(instance, "Generate city");
                    instance.transform.SetParent(root.transform, false);
                    instance.transform.position = building.Position;
                }
                EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
                Undo.CollapseUndoOperations(group);
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(group);
                Debug.LogException(exception, generator);
            }
        }

        public static void Clear(CityGenerator generator)
        {
            if (generator.GeneratedRoot == null) return;
            if (generator.GeneratedRoot.transform.parent != generator.transform)
            {
                Debug.LogError("Cannot clear: generated root is not under this plane.", generator);
                return;
            }
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Clear generated city");
            Undo.DestroyObjectImmediate(generator.GeneratedRoot);
            var so = new SerializedObject(generator);
            so.FindProperty("_generatedRoot").objectReferenceValue = null;
            so.ApplyModifiedProperties();
            Undo.CollapseUndoOperations(group);
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Fail(string message, out string reason) { reason = message; return false; }
    }
}
