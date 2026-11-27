using CylinderShooter.Core;
using CylinderShooter.Editor;
using CylinderShooter.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CylinderShooter.Tests
{
    public sealed class LevelAssetTests
    {
        private const string Folder = "Assets/LevelAssetTests_Temp";

        private sealed class EmptyField : IBattlefield
        {
            public int AliveEnemies => 0;
            public void RemoveAllEnemies() { }
        }

        [TearDown]
        public void TearDown()
        {
            DirectorTuningSnapshot.Keep();
            AssetDatabase.DeleteAsset(Folder);
        }

        [Test]
        public void SampleLevel_IsValidAndPlaysToTheEnd()
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(DirectorSetup.SampleLevelPath);
            Assert.IsNotNull(level, "Run Cylinder Shooter > Director > Create Sample Level.");
            Assert.IsTrue(level.Validate(out string reason), reason);
            var run = new LevelRunner(level, new EmptyField());
            run.Begin();
            for (int i = 0; i < 200000 && run.State == RunState.Running; i++) run.Tick(0.1f);
            Assert.AreEqual(RunState.Ended, run.State);
            Assert.AreEqual(EndReason.Completed, run.EndReason);
        }

        [Test]
        public void SpawnTestLevel_IsValidWithoutWarnings()
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(DirectorSpawnSetup.SpawnTestLevelPath);
            Assert.IsNotNull(level, "Run Cylinder Shooter > Director > Create Spawn Test Level.");
            Assert.IsTrue(level.Validate(out string reason), reason);
            var warnings = new System.Collections.Generic.List<string>();
            level.CollectWarnings(warnings);
            CollectionAssert.IsEmpty(warnings);
            var enemies = new System.Collections.Generic.List<CylinderShooter.Enemies.EnemyDefinition>();
            level.CollectEnemies(enemies);
            Assert.AreEqual(2, enemies.Count);
            foreach (var enemy in enemies) Assert.IsNotNull(enemy.Prefab, enemy.name);
        }

        [Test]
        public void Validate_NamesTheProblem()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            var wave = ScriptableObject.CreateInstance<StandardWaveDefinition>();
            Assert.IsFalse(level.Validate(out string reason));
            StringAssert.Contains("no waves", reason);
            AddTo(level, "_waves", wave);
            Assert.IsFalse(level.Validate(out reason));
            StringAssert.Contains("no stages", reason);
            AddTo(wave, "_stages", null);
            Assert.IsFalse(level.Validate(out reason));
            StringAssert.Contains("empty slot", reason);
            Object.DestroyImmediate(level);
            Object.DestroyImmediate(wave);
        }

        [Test]
        public void Snapshot_RevertPutsValuesAndReferencesBack()
        {
            AssetDatabase.CreateFolder("Assets", "LevelAssetTests_Temp");
            var stage = ScriptableObject.CreateInstance<StageDefinition>();
            var wave = ScriptableObject.CreateInstance<StandardWaveDefinition>();
            AssetDatabase.CreateAsset(stage, Folder + "/Stage.asset");
            AssetDatabase.CreateAsset(wave, Folder + "/Wave.asset");
            AddTo(wave, "_stages", stage);
            AssetDatabase.SaveAssets();

            DirectorTuningSnapshot.Capture(Folder);
            Assert.IsEmpty(DirectorTuningSnapshot.FindChanges());

            SetDuration(stage, 99f);
            var so = new SerializedObject(wave);
            so.FindProperty("_stages").ClearArray();
            so.ApplyModifiedPropertiesWithoutUndo();
            Assert.AreEqual(2, DirectorTuningSnapshot.FindChanges().Count);

            DirectorTuningSnapshot.Revert();
            Assert.AreEqual(30f, stage.Duration);
            Assert.AreEqual(1, wave.Stages.Count);
            Assert.AreSame(stage, wave.Stages[0]);
            Assert.IsEmpty(DirectorTuningSnapshot.FindChanges());
        }

        [Test]
        public void Snapshot_KeepLeavesTheChanges()
        {
            AssetDatabase.CreateFolder("Assets", "LevelAssetTests_Temp");
            var stage = ScriptableObject.CreateInstance<StageDefinition>();
            AssetDatabase.CreateAsset(stage, Folder + "/Stage.asset");
            DirectorTuningSnapshot.Capture(Folder);
            SetDuration(stage, 99f);
            DirectorTuningSnapshot.Keep();
            Assert.AreEqual(99f, stage.Duration);
            Assert.IsEmpty(DirectorTuningSnapshot.FindChanges());
        }

        private static void SetDuration(StageDefinition stage, float value)
        {
            var so = new SerializedObject(stage);
            so.FindProperty("_duration").floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddTo(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var list = so.FindProperty(field);
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
