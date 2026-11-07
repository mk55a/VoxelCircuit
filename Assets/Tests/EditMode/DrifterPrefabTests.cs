using CylinderShooter.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CylinderShooter.Tests
{
    public class DrifterPrefabTests
    {
        [Test]
        public void Prefab_HasTuningAndVisibleModel()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Drifter.prefab");
            Assert.IsNotNull(prefab);
            var drifter = prefab.GetComponent<Drifter>();
            Assert.IsNotNull(drifter);
            var serialized = new SerializedObject(drifter);
            Assert.IsInstanceOf<DrifterTuning>(serialized.FindProperty("_tuning").objectReferenceValue);
            Assert.IsNotNull(prefab.GetComponentInChildren<MeshRenderer>());
            Assert.Greater(drifter.Radius, 0f);
        }
    }
}
