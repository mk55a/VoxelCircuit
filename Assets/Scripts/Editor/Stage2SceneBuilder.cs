using CylinderShooter.Combat;
using CylinderShooter.Ship;
using UnityEditor;
using UnityEngine;

namespace CylinderShooter.Editor
{
    public static class Stage2SceneBuilder
    {
        const string PrefabFolder = "Assets/Prefabs";

        [MenuItem("Cylinder Shooter/Add Stage 2 (Shooting) To Scene")]
        public static void Build()
        {
            var ship = Object.FindFirstObjectByType<ShipController>();
            if (ship == null)
            {
                Debug.LogError("Run 'Build Stage 1 Test Scene' first.");
                return;
            }
            if (Object.FindFirstObjectByType<GameInstaller>() != null)
            {
                Debug.LogWarning("Stage 2 is already in this scene.");
                return;
            }

            var cylinder = AssetDatabase.LoadAssetAtPath<CylinderConfig>("Assets/Data/CylinderConfig.asset");
            if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder("Assets", "Prefabs");
            var weapon = LoadOrCreate<WeaponTuning>("Assets/Data/WeaponTuning.asset");
            var bulletPrefab = CreateBulletPrefab();
            var dummyPrefab = CreateDummyPrefab();

            var game = new GameObject("Game");
            var pool = new GameObject("BulletPool").AddComponent<BulletPool>();
            pool.transform.SetParent(game.transform);
            Set(pool, "_prefab", bulletPrefab);

            var installer = game.AddComponent<GameInstaller>();
            Set(installer, "_cylinder", cylinder);
            Set(installer, "_bulletPool", pool);

            var cannon = ship.gameObject.AddComponent<MainCannon>();
            Set(cannon, "_tuning", weapon);
            Set(cannon, "_aimSource", ship);
            Set(cannon, "_fireInput", ship.GetComponent<InputSystemShipInput>());
            PlayerLifeSceneBuilder.Configure(ship);

            var targets = new GameObject("Targets");
            float[] angles = { -25f, -50f, -75f, -100f, -125f, -150f };
            for (int i = 0; i < angles.Length; i++)
            {
                var dummy = (GameObject)PrefabUtility.InstantiatePrefab(dummyPrefab, targets.transform);
                var td = dummy.GetComponent<TargetDummy>();
                SetFloat(td, "_angleDegrees", angles[i]);
                SetFloat(td, "_height", (i % 3 - 1) * 3f);
            }

            Undo.RegisterCreatedObjectUndo(game, "Add Stage 2");
            Undo.RegisterCreatedObjectUndo(targets, "Add Stage 2");
            Selection.activeGameObject = ship.gameObject;
        }

        static Bullet CreateBulletPrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Bullet";
            go.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            Tint(go, Color.yellow);
            // Capsule's long axis is Y; wrap it so the bullet's forward (Z) is the long axis.
            var visual = go.transform;
            var root = new GameObject("Bullet");
            visual.SetParent(root.transform, false);
            visual.localRotation = Quaternion.Euler(90f, 0f, 0f);
            visual.localScale = new Vector3(0.25f, 0.8f, 0.25f);
            root.AddComponent<Bullet>();
            return SavePrefab(root, "Bullet").GetComponent<Bullet>();
        }

        static GameObject CreateDummyPrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "TargetDummy";
            go.transform.localScale = Vector3.one * 1.6f;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            Tint(go, new Color(0.9f, 0.3f, 0.2f));
            var dummy = go.AddComponent<TargetDummy>();
            Set(dummy, "_body", go.GetComponent<Renderer>());
            return SavePrefab(go, "TargetDummy");
        }

        static GameObject SavePrefab(GameObject go, string name)
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder("Assets", "Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabFolder}/{name}.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static void Tint(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            var m = new Material(r.sharedMaterial) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            AssetDatabase.CreateAsset(m, $"{PrefabFolder}/{go.name}_{System.Guid.NewGuid():N}.mat");
            r.sharedMaterial = m;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
