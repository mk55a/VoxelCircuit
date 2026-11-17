using System;
using System.Linq;
using CylinderShooter;
using CylinderShooter.City;
using CylinderShooter.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CylinderShooter.Tests
{
    public class CityGenerationTests
    {
        private string _assetPath;
        private GameObject _city;
        private CityGenerator _generator;
        private CylinderConfig _cylinder;
        private GameObject _prefab;

        [SetUp]
        public void SetUp()
        {
            _assetPath = "Assets/CityTest_" + Guid.NewGuid().ToString("N") + ".prefab";
            var source = new GameObject("Offset building");
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(source.transform, false);
            visual.transform.localPosition = new Vector3(15f, 4f, -12f);
            visual.transform.localScale = new Vector3(4f, 8f, 4f);
            _prefab = PrefabUtility.SaveAsPrefabAsset(source, _assetPath);
            Object.DestroyImmediate(source);

            _city = new GameObject("City");
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.transform.SetParent(_city.transform);
            plane.transform.position = new Vector3(0f, -20f, 0f);
            plane.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
            plane.transform.localScale = new Vector3(20f, 1f, 20f);
            _cylinder = ScriptableObject.CreateInstance<CylinderConfig>();
            var cylinderSettings = new SerializedObject(_cylinder);
            cylinderSettings.FindProperty("_radius").floatValue = 60f;
            cylinderSettings.ApplyModifiedPropertiesWithoutUndo();
            _generator = plane.AddComponent<CityGenerator>();
            var so = new SerializedObject(_generator);
            so.FindProperty("_plane").objectReferenceValue = plane.GetComponent<MeshFilter>();
            so.FindProperty("_cylinder").objectReferenceValue = _cylinder;
            var prefabs = so.FindProperty("_buildingPrefabs");
            prefabs.arraySize = 1;
            prefabs.GetArrayElementAtIndex(0).objectReferenceValue = _prefab;
            so.FindProperty("_minimumBuildings").intValue = 3;
            so.FindProperty("_maximumBuildings").intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_city);
            Object.DestroyImmediate(_cylinder);
            AssetDatabase.DeleteAsset(_assetPath);
        }

        [TestCase(CityLayout.GridStreets)]
        [TestCase(CityLayout.NoiseClusters)]
        public void Layout_IsRepeatableAndKeepsFootprintsApart(CityLayout layout)
        {
            SetInt("_layout", (int)layout);
            SetFloat("_noiseThreshold", 0f);
            Assert.IsTrue(CityGeneration.TryPlan(_generator, out var first, out var reason), reason);
            Assert.IsTrue(CityGeneration.TryPlan(_generator, out var second, out reason), reason);
            Assert.AreEqual(3, first.Buildings.Count);
            for (int i = 0; i < first.Buildings.Count; i++)
            {
                Assert.AreEqual(first.Buildings[i].Position, second.Buildings[i].Position);
                for (int j = 0; j < i; j++)
                    Assert.Greater(Vector3.Distance(first.Buildings[i].Position, first.Buildings[j].Position), 4f);
            }
        }

        [Test]
        public void Generate_AlignsOffsetPrefabAndUndoRestoresManualBuildings()
        {
            var old = (GameObject)PrefabUtility.InstantiatePrefab(_prefab);
            old.transform.SetParent(_city.transform);
            string oldName = old.name;
            var kept = new GameObject("Not a prefab");
            kept.transform.SetParent(_city.transform);
            Assert.IsTrue(CityGeneration.TryPlan(_generator, out var plan, out var reason), reason);
            CollectionAssert.Contains(plan.Replaced, old);
            CityGeneration.Apply(_generator, plan);
            Assert.IsTrue(old == null);
            Assert.IsNotNull(kept);
            var root = _generator.GeneratedRoot;
            Assert.AreEqual(3, root.transform.childCount);
            var instance = root.transform.GetChild(0);
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(instance));
            var renderer = instance.GetComponentInChildren<MeshRenderer>();
            Assert.That(renderer.bounds.min.y, Is.EqualTo(_generator.transform.position.y).Within(0.001f));
            Assert.That(renderer.bounds.size.y, Is.EqualTo(8f).Within(0.001f));
            var before = instance.position;
            _generator.transform.position += Vector3.up * 5f;
            Assert.That(instance.position.y, Is.EqualTo(before.y + 5f).Within(0.001f));
            _generator.transform.position -= Vector3.up * 5f;

            Undo.PerformUndo();
            Assert.IsTrue(_generator.GeneratedRoot == null);
            Assert.IsTrue(_city.transform.Cast<Transform>().Any(t => t.name == oldName));
            Undo.PerformRedo();
            Assert.IsNotNull(_generator.GeneratedRoot);
            Assert.AreEqual(3, _generator.GeneratedRoot.transform.childCount);
        }

        [Test]
        public void Failure_DoesNotRemoveExistingCity()
        {
            Assert.IsTrue(CityGeneration.TryPlan(_generator, out var plan, out var reason), reason);
            CityGeneration.Apply(_generator, plan);
            var original = _generator.GeneratedRoot;
            SetInt("_minimumBuildings", 1999);
            SetInt("_maximumBuildings", 1999);
            Assert.IsFalse(CityGeneration.TryPlan(_generator, out _, out reason));
            Assert.IsNotEmpty(reason);
            Assert.AreSame(original, _generator.GeneratedRoot);
            Assert.AreEqual(3, original.transform.childCount);
        }

        [Test]
        public void Clear_RemovesOnlyGeneratedBuildingsAndCanBeUndone()
        {
            var old = (GameObject)PrefabUtility.InstantiatePrefab(_prefab);
            old.transform.SetParent(_city.transform);
            Assert.IsTrue(CityGeneration.TryPlan(_generator, out var plan, out var reason), reason);
            CityGeneration.Apply(_generator, plan);
            var addedLater = (GameObject)PrefabUtility.InstantiatePrefab(_prefab);
            addedLater.transform.SetParent(_city.transform);
            CityGeneration.Clear(_generator);
            Assert.IsTrue(_generator.GeneratedRoot == null);
            Assert.IsNotNull(addedLater);
            Undo.PerformUndo();
            Assert.IsNotNull(_generator.GeneratedRoot);
            Assert.AreEqual(3, _generator.GeneratedRoot.transform.childCount);
            Assert.IsNotNull(addedLater);
        }

        [Test]
        public void UnsupportedPlaneTransform_IsRejectedWithoutChanges()
        {
            _generator.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
            Assert.IsFalse(CityGeneration.TryPlan(_generator, out _, out var reason));
            Assert.IsNotEmpty(reason);
            Assert.IsTrue(_generator.GeneratedRoot == null);
        }

        [Test]
        public void GenerateAgain_ReplacesOnlyPreviousGeneratedCity()
        {
            Assert.IsTrue(CityGeneration.TryPlan(_generator, out var first, out var reason), reason);
            CityGeneration.Apply(_generator, first);
            var oldRoot = _generator.GeneratedRoot;
            SetInt("_seed", 54321);
            Assert.IsTrue(CityGeneration.TryPlan(_generator, out var second, out reason), reason);
            CityGeneration.Apply(_generator, second);
            Assert.IsTrue(oldRoot == null);
            Assert.AreEqual(3, _generator.GeneratedRoot.transform.childCount);
            Undo.PerformUndo();
            Assert.IsNotNull(_generator.GeneratedRoot);
            Assert.AreEqual(3, _generator.GeneratedRoot.transform.childCount);
        }

        [Test]
        public void Planning_DoesNotChangeUnityRandomState()
        {
            var original = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(42);
                var before = UnityEngine.Random.state;
                Assert.IsTrue(CityGeneration.TryPlan(_generator, out _, out var reason), reason);
                float actual = UnityEngine.Random.value;
                UnityEngine.Random.state = before;
                Assert.AreEqual(UnityEngine.Random.value, actual);
            }
            finally { UnityEngine.Random.state = original; }
        }

        [Test]
        public void RealCityPrefab_HasUsableBounds()
        {
            var real = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/City/Prefabs/towerlarge.prefab");
            Assert.IsNotNull(real);
            var so = new SerializedObject(_generator);
            so.FindProperty("_buildingPrefabs").GetArrayElementAtIndex(0).objectReferenceValue = real;
            so.FindProperty("_minimumBuildings").intValue = 1;
            so.FindProperty("_maximumBuildings").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(CityGeneration.TryPlan(_generator, out var plan, out var reason), reason);
            Assert.AreEqual(1, plan.Buildings.Count);
        }

        [Test]
        public void RealCityPrefabs_PlanTogetherOnLookTestPlane()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/LookTest.unity", OpenSceneMode.Additive);
            try
            {
                var stage = scene.GetRootGameObjects().Single(root => root.name == "Stage1");
                var plane = stage.transform.Find("City/Plane");
                Assert.IsNotNull(plane);
                var generator = plane.gameObject.AddComponent<CityGenerator>();
                var so = new SerializedObject(generator);
                so.FindProperty("_plane").objectReferenceValue = plane.GetComponent<MeshFilter>();
                so.FindProperty("_cylinder").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<CylinderConfig>("Assets/Data/CylinderConfig.asset");
                var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Art/City/Prefabs" });
                Assert.Greater(guids.Length, 0);
                var list = so.FindProperty("_buildingPrefabs");
                list.arraySize = guids.Length;
                for (int i = 0; i < guids.Length; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[i]));
                so.ApplyModifiedPropertiesWithoutUndo();
                Assert.IsTrue(CityGeneration.TryPlan(generator, out var plan, out var reason), reason);
                Assert.That(plan.Buildings.Count, Is.InRange(15, 25));
                CityGeneration.Apply(generator, plan);
                Assert.AreEqual(plan.Buildings.Count, generator.GeneratedRoot.transform.childCount);
                var placed = new System.Collections.Generic.List<Bounds>();
                foreach (Transform building in generator.GeneratedRoot.transform)
                {
                    var renderers = building.GetComponentsInChildren<MeshRenderer>();
                    Assert.IsNotEmpty(renderers);
                    float bottom = renderers.Min(renderer => renderer.bounds.min.y);
                    Assert.That(bottom, Is.EqualTo(plane.position.y).Within(0.01f));
                    var bounds = renderers[0].bounds;
                    foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                    float farX = Mathf.Max(Mathf.Abs(bounds.min.x), Mathf.Abs(bounds.max.x));
                    float farZ = Mathf.Max(Mathf.Abs(bounds.min.z), Mathf.Abs(bounds.max.z));
                    Assert.LessOrEqual(farX * farX + farZ * farZ,
                        Mathf.Pow(generator.Cylinder.Radius - generator.BoundaryMargin, 2f) + 0.1f);
                    foreach (var other in placed)
                        Assert.IsFalse(bounds.min.x < other.max.x && bounds.max.x > other.min.x &&
                            bounds.min.z < other.max.z && bounds.max.z > other.min.z);
                    placed.Add(bounds);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private void SetInt(string name, int value)
        {
            var so = new SerializedObject(_generator);
            so.FindProperty(name).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void SetFloat(string name, float value)
        {
            var so = new SerializedObject(_generator);
            so.FindProperty(name).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
