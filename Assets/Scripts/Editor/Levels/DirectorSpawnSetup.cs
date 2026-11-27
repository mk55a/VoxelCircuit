using System;
using CylinderShooter.Cam;
using CylinderShooter.Combat;
using CylinderShooter.Core;
using CylinderShooter.Enemies;
using CylinderShooter.Formations;
using CylinderShooter.Levels;
using CylinderShooter.Ship;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CylinderShooter.Editor
{
    /// <summary>
    /// One-click setup for spawning: the scene's Enemy Pool and Enemy Spawner, and a test level for the spawn director
    /// with Chasers and Bombers, their formations, a cost table and four stages of different pressure.
    /// </summary>
    public static class DirectorSpawnSetup
    {
        private const string Folder = ProjectPaths.DirectorFolder + "/SpawnTest";
        public const string PlacementPath = ProjectPaths.DirectorFolder + "/SpawnPlacement.asset";
        public const string SpawnTestLevelPath = Folder + "/Level_SpawnTest.asset";
        public const string SpawnTestCostsPath = Folder + "/Costs_SpawnTest.asset";

        [MenuItem("Cylinder Shooter/Director/Add Spawning To Scene")]
        public static void AddSpawningToScene()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Add spawning outside Play mode.");
                return;
            }
            var installer = Object.FindFirstObjectByType<GameInstaller>();
            var ship = Object.FindFirstObjectByType<ShipController>();
            var camera = Camera.main;
            if (installer == null || ship == null || camera == null)
            {
                Debug.LogError("Spawning needs a GameInstaller, the player ship and a camera tagged MainCamera in the scene.");
                return;
            }

            var placement = DirectorSetup.LoadOrCreate<SpawnPlacementProfile>(PlacementPath, out _);
            Undo.SetCurrentGroupName("Add Spawning");
            int group = Undo.GetCurrentGroup();

            var pool = Object.FindFirstObjectByType<EnemyPool>(FindObjectsInactive.Include);
            if (pool == null)
            {
                var poolObject = new GameObject("Enemy Pool");
                Undo.RegisterCreatedObjectUndo(poolObject, "Add Spawning");
                var inactive = new GameObject("Inactive Enemies");
                Undo.RegisterCreatedObjectUndo(inactive, "Add Spawning");
                inactive.transform.SetParent(poolObject.transform, false);
                inactive.SetActive(false);
                var active = new GameObject("Active Enemies");
                Undo.RegisterCreatedObjectUndo(active, "Add Spawning");
                active.transform.SetParent(poolObject.transform, false);
                pool = Undo.AddComponent<EnemyPool>(poolObject);
                DirectorSetup.SetReference(pool, "_inactiveRoot", inactive.transform);
                DirectorSetup.SetReference(pool, "_activeRoot", active.transform);
            }

            var spawner = Object.FindFirstObjectByType<EnemySpawner>(FindObjectsInactive.Include);
            if (spawner == null)
            {
                var spawnerObject = new GameObject("Enemy Spawner");
                Undo.RegisterCreatedObjectUndo(spawnerObject, "Add Spawning");
                spawner = Undo.AddComponent<EnemySpawner>(spawnerObject);
            }
            DirectorSetup.SetReference(spawner, "_ship", ship);
            DirectorSetup.SetReference(spawner, "_camera", camera);
            DirectorSetup.SetReference(spawner, "_fovBoost", camera.GetComponent<FovBoostEffect>());
            DirectorSetup.SetReference(spawner, "_pool", pool);
            DirectorSetup.SetReference(spawner, "_placement", placement);
            DirectorSetup.SetReference(installer, "_enemyPool", pool);
            DirectorSetup.SetReference(installer, "_enemySpawner", spawner);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = spawner.gameObject;
            Debug.Log("Enemy Pool and Enemy Spawner added and hooked into the GameInstaller. Save the scene to keep them.", spawner);
        }

        /// <summary>
        /// Creates the spawn test level, or resets it to these starting values if it exists. The assets keep their
        /// identity, so the Level Manager's 'Level' field stays set. Starting values are guesses to tune by playing.
        /// </summary>
        [MenuItem("Cylinder Shooter/Director/Create Or Reset Spawn Test Level")]
        public static void CreateSpawnTestLevel()
        {
            var chaserPrefab = AssetDatabase.LoadAssetAtPath<Chaser>(ProjectPaths.ChaserPrefab);
            var bomberPrefab = AssetDatabase.LoadAssetAtPath<Bomber>(ProjectPaths.BomberPrefab);
            var waveQueue = Formation("WaveQueue");
            var line = Formation("HorizontalLine");
            var column = Formation("VerticalLine");
            var grid = Formation("Grid");
            if (chaserPrefab == null || bomberPrefab == null || waveQueue == null || line == null || column == null ||
                grid == null)
            {
                Debug.LogError("The spawn test level needs the Chaser and Bomber prefabs and the WaveQueue, HorizontalLine, " +
                               "VerticalLine and Grid formations in " + ProjectPaths.FormationsFolder + ".");
                return;
            }

            var chaser = Enemy("Enemy_Chaser", chaserPrefab, 120,
                new EnemyFormationOption(waveQueue, 6, 20, 2f),
                new EnemyFormationOption(line, 5, 12),
                new EnemyFormationOption(grid, 10, 20));
            var bomber = Enemy("Enemy_Bomber", bomberPrefab, 50,
                new EnemyFormationOption(line, 4, 10),
                new EnemyFormationOption(column, 3, 6));

            var costs = DirectorSetup.LoadOrCreate<EnemyCostTable>(SpawnTestCostsPath, out _);
            Edit(costs, so =>
            {
                var list = so.FindProperty("_costs");
                list.arraySize = 2;
                SetCost(list.GetArrayElementAtIndex(0), chaser, 1f);
                SetCost(list.GetArrayElementAtIndex(1), bomber, 2f);
            });

            var warmUp = Stage("ST_WarmUp", StageEndMode.TimeUp, 25f, 25f,
                start: 6f, rate: 1.5f, cap: 15f, groupSize: 0.3f, patience: 6f, cooldown: 2f, maxAlive: 25,
                SpawnSides.Ahead, laneFocus: 0.3f, pincer: false,
                Row(chaser, 1f, 0), Row(bomber, 1f, 0, enabled: false));
            var mines = Stage("ST_Mines", StageEndMode.TimeUp, 30f, 30f,
                start: 8f, rate: 3f, cap: 24f, groupSize: 0.5f, patience: 6f, cooldown: 2f, maxAlive: 40,
                SpawnSides.Either, laneFocus: 0.6f, pincer: false,
                Row(chaser, 1f, 20), Row(bomber, 3f, 0));
            var peak = Stage("ST_Peak", StageEndMode.TimeUpThenCleared, 35f, 70f,
                start: 20f, rate: 8f, cap: 50f, groupSize: 0.8f, patience: 5f, cooldown: 1f, maxAlive: 120,
                SpawnSides.Either, laneFocus: 0.7f, pincer: true,
                Row(chaser, 3f, 0), Row(bomber, 2f, 40));
            var rest = Stage("ST_Rest", StageEndMode.TimeUp, 12f, 12f,
                start: 0f, rate: 0.8f, cap: 8f, groupSize: 0.1f, patience: 8f, cooldown: 3f, maxAlive: 10,
                SpawnSides.Behind, laneFocus: 0f, pincer: false,
                Row(chaser, 1f, 0));

            var wave = DirectorSetup.LoadOrCreate<StandardWaveDefinition>(Folder + "/Wave_SpawnTest.asset", out _);
            DirectorSetup.SetList(wave, "_stages", warmUp, mines, peak, rest);
            var level = DirectorSetup.LoadOrCreate<LevelDefinition>(SpawnTestLevelPath, out _);
            DirectorSetup.SetList(level, "_waves", wave);
            DirectorSetup.SetReference(level, "_costs", costs);
            AssetDatabase.SaveAssets();
            Debug.Log($"{SpawnTestLevelPath} is ready. Put it in the Level Manager's 'Level' field to play it.", level);
        }

        private static FormationDefinition Formation(string name) =>
            AssetDatabase.LoadAssetAtPath<FormationDefinition>($"{ProjectPaths.FormationsFolder}/{name}.asset");

        private static EnemyDefinition Enemy(string name, MonoBehaviour prefab, int prewarm,
            params EnemyFormationOption[] formations)
        {
            var enemy = DirectorSetup.LoadOrCreate<EnemyDefinition>($"{Folder}/{name}.asset", out _);
            Edit(enemy, so =>
            {
                so.FindProperty("_prefab").objectReferenceValue = prefab;
                so.FindProperty("_prewarm").intValue = prewarm;
                var list = so.FindProperty("_formations");
                list.arraySize = formations.Length;
                for (int i = 0; i < formations.Length; i++)
                {
                    var element = list.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("_formation").objectReferenceValue = formations[i].Formation;
                    element.FindPropertyRelative("_minCount").intValue = formations[i].MinCount;
                    element.FindPropertyRelative("_maxCount").intValue = formations[i].MaxCount;
                    element.FindPropertyRelative("_weight").floatValue = formations[i].Weight;
                }
                so.FindProperty("_entries").arraySize = 0;
            });
            return enemy;
        }

        private static StageEnemyEntry Row(EnemyDefinition enemy, float weight, int maxAlive, bool enabled = true) =>
            new StageEnemyEntry(enemy, weight, maxAlive, enabled);

        private static StageDefinition Stage(string name, StageEndMode endMode, float duration, float maxDuration,
            float start, float rate, float cap, float groupSize, float patience, float cooldown, int maxAlive,
            SpawnSides sides, float laneFocus, bool pincer, params StageEnemyEntry[] enemies)
        {
            var stage = DirectorSetup.LoadOrCreate<StageDefinition>($"{Folder}/{name}.asset", out _);
            Edit(stage, so =>
            {
                so.FindProperty("_endMode").enumValueIndex = (int)endMode;
                so.FindProperty("_duration").floatValue = duration;
                so.FindProperty("_maxDuration").floatValue = maxDuration;
                so.FindProperty("_startingCredits").floatValue = start;
                so.FindProperty("_creditsPerSecond").floatValue = rate;
                so.FindProperty("_creditCap").floatValue = cap;
                so.FindProperty("_groupSize").floatValue = groupSize;
                so.FindProperty("_patience").floatValue = patience;
                so.FindProperty("_cooldown").floatValue = cooldown;
                so.FindProperty("_maxAlive").intValue = maxAlive;
                so.FindProperty("_sides").enumValueIndex = (int)sides;
                so.FindProperty("_laneFocus").floatValue = laneFocus;
                so.FindProperty("_allowPincer").boolValue = pincer;
                var list = so.FindProperty("_enemies");
                list.arraySize = enemies.Length;
                for (int i = 0; i < enemies.Length; i++)
                {
                    var element = list.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("_enemy").objectReferenceValue = enemies[i].Definition;
                    element.FindPropertyRelative("_enabled").boolValue = enemies[i].Enabled;
                    element.FindPropertyRelative("_weight").floatValue = enemies[i].Weight;
                    element.FindPropertyRelative("_maxAlive").intValue = enemies[i].MaxAlive;
                }
            });
            return stage;
        }

        private static void SetCost(SerializedProperty element, EnemyDefinition enemy, float cost)
        {
            element.FindPropertyRelative("_enemy").objectReferenceValue = enemy;
            element.FindPropertyRelative("_cost").floatValue = cost;
        }

        private static void Edit(Object target, Action<SerializedObject> change)
        {
            var so = new SerializedObject(target);
            change(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
