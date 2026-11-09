using System.Linq;
using CylinderShooter.Cam;
using CylinderShooter.Combat;
using CylinderShooter.Enemies;
using CylinderShooter.Formations;
using CylinderShooter.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CylinderShooter.Editor
{
    public static class FormationTestSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/LevelTest.unity";
        private const string Folder = "Assets/Data/Formations";

        [MenuItem("Cylinder Shooter/Setup Formation Test Sandbox")]
        public static void Setup()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Set up the formation sandbox outside Play mode.");
                return;
            }
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                Debug.LogError("Open LevelTest as the active scene before setting up the formation sandbox.");
                return;
            }

            var components = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
            var ships = components.OfType<ShipController>().ToArray();
            var cameras = components.OfType<Camera>().Where(camera => camera.CompareTag("MainCamera")).ToArray();
            var installers = components.OfType<GameInstaller>().ToArray();
            var spawners = components.OfType<FormationTestSpawner>().ToArray();
            var arenas = components.OfType<MeshFilter>().Where(filter => filter.name == "Arena").ToArray();
            var prefab = AssetDatabase.LoadAssetAtPath<Chaser>("Assets/Prefabs/Chaser.prefab");
            if (ships.Length != 1 || cameras.Length != 1 || installers.Length != 1 ||
                spawners.Length > 1 || prefab == null || arenas.Length != 1)
            {
                Debug.LogError("LevelTest requires exactly one ship, main camera, Arena mesh and GameInstaller, at most one formation spawner, and a Chaser prefab.");
                return;
            }
            var cylinder = new SerializedObject(installers[0]).FindProperty("_cylinder").objectReferenceValue as CylinderConfig;
            if (cylinder == null)
            {
                Debug.LogError("Assign the cylinder on LevelTest's GameInstaller before setting up the formation sandbox.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Data", "Formations");
            var wave = GetOrCreate("WaveQueue", FormationShape.WaveQueue);
            GetOrCreate("HorizontalLine", FormationShape.HorizontalLine);
            GetOrCreate("VerticalLine", FormationShape.VerticalLine);
            GetOrCreate("Grid", FormationShape.Grid);
            AssetDatabase.SaveAssets();

            FormationTestSpawner spawner;
            if (spawners.Length == 0)
            {
                var root = new GameObject("Formation Test Sandbox");
                Undo.RegisterCreatedObjectUndo(root, "Setup formation sandbox");
                spawner = Undo.AddComponent<FormationTestSpawner>(root);
            }
            else spawner = spawners[0];
            var serialized = new SerializedObject(spawner);
            serialized.FindProperty("_ship").objectReferenceValue = ships[0];
            serialized.FindProperty("_camera").objectReferenceValue = cameras[0];
            serialized.FindProperty("_fovBoost").objectReferenceValue = cameras[0].GetComponent<FovBoostEffect>();
            serialized.FindProperty("_cylinder").objectReferenceValue = cylinder;
            serialized.FindProperty("_prefab").objectReferenceValue = prefab;
            serialized.FindProperty("_arena").objectReferenceValue = arenas[0];
            if (serialized.FindProperty("_formation").objectReferenceValue == null)
                serialized.FindProperty("_formation").objectReferenceValue = wave;
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = spawner.gameObject;
        }

        private static FormationDefinition GetOrCreate(string name, FormationShape shape)
        {
            string path = $"{Folder}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<FormationDefinition>(path);
            if (existing != null) return existing;
            var definition = ScriptableObject.CreateInstance<FormationDefinition>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("_shape").enumValueIndex = (int)shape;
            if (shape == FormationShape.VerticalLine)
                serialized.FindProperty("_count").intValue = 5;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(definition, path);
            return definition;
        }
    }
}
