using CylinderShooter;
using CylinderShooter.Combat;
using CylinderShooter.Editor;
using CylinderShooter.Ship;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CylinderShooter.Tests
{
    public class PlayerLifeSceneBuilderTests
    {
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
                Set(ship, "cylinder", cylinder);
                Set(banking, "model", model.transform);

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

        private static void Set(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
