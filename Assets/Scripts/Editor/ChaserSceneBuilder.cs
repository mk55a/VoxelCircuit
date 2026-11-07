using CylinderShooter.Combat;
using CylinderShooter.Core;
using CylinderShooter.Enemies;
using CylinderShooter.Ship;
using UnityEditor;
using UnityEngine;

namespace CylinderShooter.Editor
{
    public static class ChaserSceneBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Chaser.prefab";
        private const string CylinderPath = "Assets/Data/CylinderConfig.asset";

        [MenuItem("Cylinder Shooter/Add Chasers To Scene")]
        public static void AddChasers()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Add chasers while the editor is not in Play mode.");
                return;
            }

            var installer = Object.FindFirstObjectByType<GameInstaller>();
            var ship = Object.FindFirstObjectByType<ShipController>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var cylinder = AssetDatabase.LoadAssetAtPath<CylinderConfig>(CylinderPath);
            if (installer == null || ship == null || prefab == null || cylinder == null)
            {
                Debug.LogError("Chasers require a Stage 2 scene, ship, Chaser prefab and cylinder config.");
                return;
            }

            var root = CreateChasers(prefab, ship, cylinder);
            Selection.activeGameObject = root;
        }

        public static GameObject CreateChasers(GameObject prefab, ShipController ship, CylinderConfig cylinder)
        {
            var root = new GameObject("Chasers");
            Undo.RegisterCreatedObjectUndo(root, "Add Chasers");
            CreateChaser(prefab, root.transform, ship, cylinder, -35f, 0f, 180f);
            CreateChaser(prefab, root.transform, ship, cylinder, 55f, 3f, 0f);
            return root;
        }

        private static void CreateChaser(GameObject prefab, Transform parent, ShipController ship,
            CylinderConfig cylinder, float angleDegrees, float height, float headingDegrees)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            var serialized = new SerializedObject(instance.GetComponent<Chaser>());
            serialized.FindProperty("_target").objectReferenceValue = ship;
            serialized.FindProperty("_startAngleDegrees").floatValue = angleDegrees;
            serialized.FindProperty("_startHeight").floatValue = height;
            serialized.FindProperty("_startHeadingDegrees").floatValue = headingDegrees;
            serialized.ApplyModifiedProperties();

            float angle = angleDegrees * Mathf.Deg2Rad;
            instance.transform.position = CylinderSpace.ToWorld(angle, height, cylinder.radius);
            Vector3 direction = -CylinderSpace.Tangent(angle) * Mathf.Cos(headingDegrees * Mathf.Deg2Rad)
                + Vector3.up * Mathf.Sin(headingDegrees * Mathf.Deg2Rad);
            instance.transform.rotation = Quaternion.LookRotation(direction, CylinderSpace.Outward(angle));
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        }
    }
}
