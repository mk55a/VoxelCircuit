using CylinderShooter.Effects;
using CylinderShooter.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CylinderShooter.Tests
{
    public sealed class BomberPrefabTests
    {
        [Test]
        public void Prefab_IsFullyWired()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Bomber.prefab");
            Assert.IsNotNull(prefab, "Run Cylinder Shooter > Enemies > Build Bomber Prefab.");
            var bomber = prefab.GetComponent<Bomber>();
            Assert.IsNotNull(bomber);
            Assert.IsTrue(bomber.HasValidTuning);
            Assert.AreSame(bomber, prefab.GetComponents<MonoBehaviour>()[0], "Bomber must be the first script.");
            Assert.IsNotNull(prefab.GetComponent<ShatterOnDestroyed>());
            Assert.IsNotNull(prefab.GetComponent<PulseOnDestroyed>());
            Assert.IsNotNull(prefab.GetComponent<PulseOnDetonated>());
            Assert.IsNotNull(prefab.GetComponent<FuseLamps>());
            var renderer = prefab.GetComponentInChildren<MeshRenderer>();
            Assert.IsNotNull(renderer);
            Assert.AreNotSame(prefab, renderer.gameObject);
            Assert.IsNotNull(renderer.GetComponent<MeshFilter>().sharedMesh);
            Assert.AreEqual("VoxelCircuit/Gameplay Toon", renderer.sharedMaterial.shader.name);
            Assert.AreEqual(LayerMask.NameToLayer("Gameplay"), renderer.gameObject.layer, "The outline only draws the Gameplay layer.");
        }
    }
}
