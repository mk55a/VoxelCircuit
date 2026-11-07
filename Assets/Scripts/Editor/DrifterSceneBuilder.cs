using CylinderShooter.Combat;
using CylinderShooter.Core;
using CylinderShooter.Enemies;
using UnityEditor;
using UnityEngine;

namespace CylinderShooter.Editor
{
    public static class DrifterSceneBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Drifter.prefab";
        private const string CylinderPath = "Assets/Data/CylinderConfig.asset";

        [MenuItem("Cylinder Shooter/Add Drifters To Scene")]
        public static void AddDrifters()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Add drifters while the editor is not in Play mode.");
                return;
            }

            var installer = Object.FindFirstObjectByType<GameInstaller>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var cylinder = AssetDatabase.LoadAssetAtPath<CylinderConfig>(CylinderPath);
            if (installer == null || prefab == null || cylinder == null)
            {
                Debug.LogError("Drifters require the Stage 2 scene, Drifter prefab and cylinder config.");
                return;
            }

            var root = new GameObject("Drifters");
            Undo.RegisterCreatedObjectUndo(root, "Add Drifters");
            CreateDrifter(prefab, root.transform, cylinder, -25f, 0f, false);
            CreateDrifter(prefab, root.transform, cylinder, -75f, 3f, true);
            CreateDrifter(prefab, root.transform, cylinder, 55f, -3f, false);
            Selection.activeGameObject = root;
        }

        private static void CreateDrifter(GameObject prefab, Transform parent, CylinderConfig cylinder,
            float angleDegrees, float height, bool moveRight)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            var serialized = new SerializedObject(instance.GetComponent<Drifter>());
            serialized.FindProperty("_startAngleDegrees").floatValue = angleDegrees;
            serialized.FindProperty("_startHeight").floatValue = height;
            serialized.FindProperty("_moveRight").boolValue = moveRight;
            serialized.ApplyModifiedProperties();

            float angle = angleDegrees * Mathf.Deg2Rad;
            instance.transform.position = CylinderSpace.ToWorld(angle, height, cylinder.radius);
            var direction = CylinderSpace.Tangent(angle) * (moveRight ? -1f : 1f);
            instance.transform.rotation = Quaternion.LookRotation(direction, CylinderSpace.Outward(angle));
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        }
    }
}
