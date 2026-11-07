using CylinderShooter.Combat;
using CylinderShooter.Ship;
using UnityEditor;
using UnityEngine;

namespace CylinderShooter.Editor
{
    public static class PlayerLifeSceneBuilder
    {
        private const string TuningPath = "Assets/Data/PlayerLifeTuning.asset";

        [MenuItem("Cylinder Shooter/Add Player Lives To Scene")]
        public static void AddPlayerLives()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Configure player lives outside Play mode.");
                return;
            }

            var ship = Object.FindFirstObjectByType<ShipController>();
            var installer = Object.FindFirstObjectByType<GameInstaller>();
            if (ship == null || installer == null)
            {
                Debug.LogError("Player lives require a Stage 2 scene with a ship and GameInstaller.");
                return;
            }
            Configure(ship);
        }

        public static void Configure(ShipController ship)
        {
            var tuning = AssetDatabase.LoadAssetAtPath<PlayerLifeTuning>(TuningPath);
            var banking = ship.GetComponent<ShipBanking>();
            var cannon = ship.GetComponent<MainCannon>();
            var shipProperties = new SerializedObject(ship);
            var cylinder = shipProperties.FindProperty("cylinder").objectReferenceValue;
            var model = banking != null
                ? new SerializedObject(banking).FindProperty("model").objectReferenceValue as Transform
                : null;
            if (tuning == null || cannon == null || cylinder == null || model == null
                || !model.IsChildOf(ship.transform))
            {
                Debug.LogError("Player lives require PlayerLifeTuning, a cannon, cylinder and assigned ship model.", ship);
                return;
            }

            var life = ship.GetComponent<PlayerLife>();
            if (life == null)
            {
                life = Undo.AddComponent<PlayerLife>(ship.gameObject);
            }
            var contact = ship.GetComponent<PlayerContact>();
            if (contact == null)
            {
                contact = Undo.AddComponent<PlayerContact>(ship.gameObject);
            }
            if (new SerializedObject(life).FindProperty("_tuning").objectReferenceValue == null)
            {
                Set(life, "_tuning", tuning);
            }
            Set(life, "_ship", ship);
            Set(life, "_visual", model.gameObject);
            Set(contact, "_ship", ship);
            Set(contact, "_life", life);
            Set(contact, "_cylinder", cylinder);
            Set(ship, "_life", life);
            Set(cannon, "_life", life);
        }

        private static void Set(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }
    }
}
