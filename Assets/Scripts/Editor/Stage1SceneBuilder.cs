using CylinderShooter.Cam;
using CylinderShooter.Ship;
using UnityEditor;
using UnityEngine;

namespace CylinderShooter.Editor
{
    public static class Stage1SceneBuilder
    {
        [MenuItem("Cylinder Shooter/Build Stage 1 Test Scene")]
        public static void Build()
        {
            var cyl = LoadOrCreate<CylinderConfig>("Assets/Data/CylinderConfig.asset");
            var tuning = LoadOrCreate<ShipTuning>("Assets/Data/ShipTuning.asset");

            var root = new GameObject("Stage1");

            var arena = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arena.name = "Arena";
            arena.transform.SetParent(root.transform);
            float h = cyl.MaxHeight - cyl.MinHeight + 4f;
            arena.transform.localScale = new Vector3((cyl.Radius - 0.5f) * 2f, h / 2f, (cyl.Radius - 0.5f) * 2f);
            arena.transform.position = new Vector3(0f, (cyl.MaxHeight + cyl.MinHeight) / 2f, 0f);
            Object.DestroyImmediate(arena.GetComponent<Collider>());

            // Pillars every 30 degrees; the first is red so the seam and a completed loop are obvious.
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = "Marker" + i;
                pillar.transform.SetParent(root.transform);
                pillar.transform.position = Core.CylinderSpace.ToWorld(a, cyl.MinHeight - 1f, cyl.Radius);
                pillar.transform.rotation = Quaternion.LookRotation(Core.CylinderSpace.Tangent(a), Vector3.up);
                pillar.transform.localScale = new Vector3(1f, 1f, 1f);
                Object.DestroyImmediate(pillar.GetComponent<Collider>());
                Tint(pillar, i == 0 ? Color.red : Color.gray);
            }

            var ship = new GameObject("Ship");
            ship.transform.SetParent(root.transform);
            var input = ship.AddComponent<InputSystemShipInput>();
            var controller = ship.AddComponent<ShipController>();
            var model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.name = "Model";
            model.transform.SetParent(ship.transform, false);
            model.transform.localScale = new Vector3(0.8f, 0.4f, 2f);
            Object.DestroyImmediate(model.GetComponent<Collider>());
            Tint(model, Color.cyan);
            var banking = ship.AddComponent<ShipBanking>();

            Set(controller, "_cylinder", cyl);
            Set(controller, "_tuning", tuning);
            Set(controller, "_inputSource", input);
            Set(banking, "_ship", controller);
            Set(banking, "_model", model.transform);

            var cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
                cam.tag = "MainCamera";
            }
            var follow = GetOrAdd<CylinderFollowCamera>(cam.gameObject);
            var fov = GetOrAdd<FovBoostEffect>(cam.gameObject);
            Set(follow, "_targetSource", controller);
            Set(follow, "_cylinder", cyl);
            Set(fov, "_targetSource", controller);

            Selection.activeGameObject = ship;
            Undo.RegisterCreatedObjectUndo(root, "Build Stage 1");
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        static void Tint(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            var m = new Material(r.sharedMaterial) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            r.sharedMaterial = m;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
