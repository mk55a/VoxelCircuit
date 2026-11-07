using CylinderShooter.Editor;
using CylinderShooter.Enemies;
using CylinderShooter.Ship;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CylinderShooter.Tests
{
    public class ChaserSceneBuilderTests
    {
        [Test]
        public void AddChasers_WiresPlayerAndPrefabInstances()
        {
            var shipObject = new GameObject("Test ship");
            GameObject root = null;
            try
            {
                var ship = shipObject.AddComponent<ShipController>();
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Chaser.prefab");
                var cylinder = AssetDatabase.LoadAssetAtPath<CylinderConfig>("Assets/Data/CylinderConfig.asset");
                root = ChaserSceneBuilder.CreateChasers(prefab, ship, cylinder);
                var chasers = root.GetComponentsInChildren<Chaser>();

                Assert.AreEqual(2, chasers.Length);
                foreach (var chaser in chasers)
                {
                    Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(chaser));
                    Assert.AreSame(ship, new SerializedObject(chaser).FindProperty("_target").objectReferenceValue);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(shipObject);
            }
        }
    }
}
