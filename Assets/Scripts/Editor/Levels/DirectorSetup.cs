using CylinderShooter.Combat;
using CylinderShooter.Core;
using CylinderShooter.Levels;
using UnityEditor;
using UnityEngine;

namespace CylinderShooter.Editor
{
    /// <summary>
    /// One-click setup for the level system: a small sample level to start tuning from, and the scene object that
    /// plays it. Assets that already exist are never changed.
    /// </summary>
    public static class DirectorSetup
    {
        public const string SampleLevelPath = ProjectPaths.DirectorFolder + "/Level1.asset";

        [MenuItem("Cylinder Shooter/Director/Create Sample Level")]
        public static void CreateSampleLevel()
        {
            if (AssetDatabase.LoadAssetAtPath<LevelDefinition>(SampleLevelPath) != null)
            {
                Debug.Log($"{SampleLevelPath} already exists and was left unchanged.");
                return;
            }
            // No enemies: this level only shows stages and endings. The spawn test level has the director's enemies.
            var warmUp = Stage("Stage_WarmUp", StageEndMode.TimeUp, 15f, 15f);
            var buildUp = Stage("Stage_BuildUp", StageEndMode.TimeUp, 30f, 30f);
            var peak = Stage("Stage_Peak", StageEndMode.TimeUpThenCleared, 25f, 60f);
            var rest = Stage("Stage_Rest", StageEndMode.TimeUp, 10f, 10f);
            var first = Wave("Wave1_FirstContact", warmUp, buildUp, peak, rest);
            var second = Wave("Wave2_Minefield", buildUp, peak, rest);

            var level = LoadOrCreate<LevelDefinition>(SampleLevelPath, out bool created);
            if (created) SetList(level, "_waves", first, second);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created {SampleLevelPath} with its waves and stages in {ProjectPaths.DirectorFolder}.");
        }

        [MenuItem("Cylinder Shooter/Director/Add Level Manager To Scene")]
        public static void AddLevelManagerToScene()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Add the Level Manager outside Play mode.");
                return;
            }
            var installer = Object.FindFirstObjectByType<GameInstaller>();
            if (installer == null)
            {
                Debug.LogError("This scene has no GameInstaller, so a Level Manager can't be hooked up.");
                return;
            }
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(SampleLevelPath);
            if (level == null)
            {
                Debug.LogError("Run Cylinder Shooter > Director > Create Sample Level first.");
                return;
            }

            Undo.SetCurrentGroupName("Add Level Manager");
            int group = Undo.GetCurrentGroup();
            var manager = Object.FindFirstObjectByType<LevelManager>();
            if (manager == null)
            {
                var go = new GameObject("Level Manager");
                Undo.RegisterCreatedObjectUndo(go, "Add Level Manager");
                manager = Undo.AddComponent<LevelManager>(go);
                SetReference(manager, "_level", level);
            }
            SetReference(installer, "_levelManager", manager);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = manager.gameObject;
            Debug.Log("Level Manager added and hooked into the GameInstaller. Save the scene to keep it.", manager);
        }

        internal static StageDefinition Stage(string name, StageEndMode endMode, float duration, float maxDuration)
        {
            var stage = LoadOrCreate<StageDefinition>($"{ProjectPaths.DirectorFolder}/{name}.asset", out bool created);
            if (!created) return stage;
            var so = new SerializedObject(stage);
            so.FindProperty("_endMode").enumValueIndex = (int)endMode;
            so.FindProperty("_duration").floatValue = duration;
            so.FindProperty("_maxDuration").floatValue = maxDuration;
            so.ApplyModifiedPropertiesWithoutUndo();
            return stage;
        }

        private static StandardWaveDefinition Wave(string name, params StageDefinition[] stages)
        {
            var wave = LoadOrCreate<StandardWaveDefinition>($"{ProjectPaths.DirectorFolder}/{name}.asset", out bool created);
            if (created) SetList(wave, "_stages", stages);
            return wave;
        }

        internal static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            created = existing == null;
            if (!created) return existing;
            ProjectPaths.EnsureFolder(ProjectPaths.FolderOf(path));
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        internal static void SetList(Object target, string field, params Object[] values)
        {
            var so = new SerializedObject(target);
            var list = so.FindProperty(field);
            list.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetReference(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }
    }
}
