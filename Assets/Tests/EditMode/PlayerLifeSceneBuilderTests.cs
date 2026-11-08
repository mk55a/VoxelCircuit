using CylinderShooter;
using System.Linq;
using System.Reflection;
using CylinderShooter.Cam;
using CylinderShooter.Combat;
using CylinderShooter.Editor;
using CylinderShooter.Enemies;
using CylinderShooter.Ship;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CylinderShooter.Tests
{
    public class PlayerLifeSceneBuilderTests
    {
        [Test]
        public void ShipTargets_AcceptShipControllerComponents()
        {
            Assert.AreEqual(typeof(ShipController), typeof(CylinderFollowCamera)
                .GetField("_targetSource", BindingFlags.Instance | BindingFlags.NonPublic).FieldType);
            Assert.AreEqual(typeof(ShipController), typeof(FovBoostEffect)
                .GetField("_targetSource", BindingFlags.Instance | BindingFlags.NonPublic).FieldType);
            Assert.AreEqual(typeof(ShipController), typeof(Chaser)
                .GetField("_target", BindingFlags.Instance | BindingFlags.NonPublic).FieldType);
            Assert.AreEqual(typeof(ShipController), typeof(MainCannon)
                .GetField("_aimSource", BindingFlags.Instance | BindingFlags.NonPublic).FieldType);
        }

        [Test]
        public void Configure_WiresExistingShipIdempotently()
        {
            var root = new GameObject("Test ship");
            var cylinder = ScriptableObject.CreateInstance<CylinderConfig>();
            try
            {
                var ship = root.AddComponent<ShipController>();
                var cannon = root.AddComponent<MainCannon>();
                var banking = root.AddComponent<ShipBanking>();
                var model = new GameObject("Model");
                model.transform.SetParent(root.transform);
                Set(ship, "_cylinder", cylinder);
                Set(banking, "_model", model.transform);

                PlayerLifeSceneBuilder.Configure(ship);
                PlayerLifeSceneBuilder.Configure(ship);

                Assert.AreEqual(1, root.GetComponents<PlayerLife>().Length);
                Assert.AreEqual(1, root.GetComponents<PlayerContact>().Length);
                var life = root.GetComponent<PlayerLife>();
                Assert.AreSame(life, new SerializedObject(ship).FindProperty("_life").objectReferenceValue);
                Assert.AreSame(life, new SerializedObject(cannon).FindProperty("_life").objectReferenceValue);
                Assert.AreSame(model, new SerializedObject(life).FindProperty("_visual").objectReferenceValue);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cylinder);
            }
        }

        [Test]
        public void SavedSceneAndAssets_PreserveRenamedFields()
        {
            var cylinder = AssetDatabase.LoadAssetAtPath<CylinderConfig>("Assets/Data/CylinderConfig.asset");
            var shipTuning = AssetDatabase.LoadAssetAtPath<ShipTuning>("Assets/Data/ShipTuning.asset");
            var weaponTuning = AssetDatabase.LoadAssetAtPath<WeaponTuning>("Assets/Data/WeaponTuning.asset");
            Assert.AreEqual(60f, cylinder.Radius);
            Assert.AreEqual(-6f, cylinder.MinHeight);
            Assert.AreEqual(6f, cylinder.MaxHeight);
            Assert.AreEqual(70f, shipTuning.Thrust);
            Assert.AreEqual(60f, weaponTuning.BulletSpeed);

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Additive);
            try
            {
                var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
                var ship = components.OfType<ShipController>().Single();
                var shipProperties = new SerializedObject(ship);
                Assert.AreSame(cylinder, shipProperties.FindProperty("_cylinder").objectReferenceValue);
                Assert.AreSame(shipTuning, shipProperties.FindProperty("_tuning").objectReferenceValue);
                Assert.NotNull(shipProperties.FindProperty("_inputSource").objectReferenceValue);

                var camera = new SerializedObject(components.OfType<CylinderFollowCamera>().Single());
                Assert.AreSame(ship, camera.FindProperty("_targetSource").objectReferenceValue);
                Assert.AreSame(cylinder, camera.FindProperty("_cylinder").objectReferenceValue);
                var fov = new SerializedObject(components.OfType<FovBoostEffect>().Single());
                Assert.AreSame(ship, fov.FindProperty("_targetSource").objectReferenceValue);
                var cannon = new SerializedObject(components.OfType<MainCannon>().Single());
                Assert.AreSame(weaponTuning, cannon.FindProperty("_tuning").objectReferenceValue);
                Assert.AreSame(ship, cannon.FindProperty("_aimSource").objectReferenceValue);
                Assert.NotNull(cannon.FindProperty("_fireInput").objectReferenceValue);
                foreach (var chaser in components.OfType<Chaser>())
                    Assert.AreSame(ship, new SerializedObject(chaser).FindProperty("_target").objectReferenceValue);
                var pool = new SerializedObject(components.OfType<BulletPool>().Single());
                Assert.NotNull(pool.FindProperty("_prefab").objectReferenceValue);
                var installer = new SerializedObject(components.OfType<GameInstaller>().Single());
                Assert.AreSame(cylinder, installer.FindProperty("_cylinder").objectReferenceValue);
                Assert.NotNull(installer.FindProperty("_bulletPool").objectReferenceValue);

                var dummies = components.OfType<TargetDummy>().Select(dummy => new SerializedObject(dummy)).ToArray();
                Assert.AreEqual(6, dummies.Length);
                CollectionAssert.AreEquivalent(new[] { -25f, -50f, -75f, -100f, -125f, -150f },
                    dummies.Select(dummy => dummy.FindProperty("_angleDegrees").floatValue));
                foreach (var dummy in dummies)
                    Assert.NotNull(dummy.FindProperty("_body").objectReferenceValue);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void Set(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
