using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CylinderShooter.Editor
{
    /// <summary>
    /// Asset edits made in Play mode are kept, so an experiment could overwrite a tuned level. This remembers the
    /// director assets when Play starts, so the changes can be kept or put back when it stops. It covers changes to
    /// existing assets, not assets created or deleted during Play.
    /// </summary>
    [InitializeOnLoad]
    public static class DirectorTuningSnapshot
    {
        private const string SessionKey = "VoxelCircuit.DirectorTuningSnapshot";

        [Serializable]
        private sealed class Store
        {
            public List<string> Paths = new List<string>();
            public List<string> Contents = new List<string>();
        }

        static DirectorTuningSnapshot()
        {
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        }

        /// <summary>Remembers every director asset as it is now.</summary>
        public static void Capture() => Capture(ProjectPaths.DirectorFolder);

        public static void Capture(string folder)
        {
            var store = new Store();
            if (AssetDatabase.IsValidFolder(folder))
            {
                foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    if (asset == null) continue;
                    store.Paths.Add(path);
                    store.Contents.Add(EditorJsonUtility.ToJson(asset));
                }
            }
            SessionState.SetString(SessionKey, JsonUtility.ToJson(store));
        }

        /// <summary>Paths of assets that differ from the snapshot. Empty when there is no snapshot.</summary>
        public static List<string> FindChanges()
        {
            var changed = new List<string>();
            var store = Load();
            for (int i = 0; i < store.Paths.Count; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(store.Paths[i]);
                if (asset != null && EditorJsonUtility.ToJson(asset) != store.Contents[i]) changed.Add(store.Paths[i]);
            }
            return changed;
        }

        /// <summary>Keeps the changes: forgets the snapshot.</summary>
        public static void Keep() => SessionState.EraseString(SessionKey);

        /// <summary>Puts the changed assets back to how they were when the snapshot was taken.</summary>
        public static void Revert()
        {
            var store = Load();
            for (int i = 0; i < store.Paths.Count; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(store.Paths[i]);
                if (asset == null || EditorJsonUtility.ToJson(asset) == store.Contents[i]) continue;
                EditorJsonUtility.FromJsonOverwrite(store.Contents[i], asset);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
            }
            Keep();
        }

        private static Store Load()
        {
            string json = SessionState.GetString(SessionKey, string.Empty);
            return string.IsNullOrEmpty(json) ? new Store() : JsonUtility.FromJson<Store>(json);
        }

        private static void HandlePlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode) Capture();
            else if (change == PlayModeStateChange.EnteredEditMode && FindChanges().Count == 0) Keep();
        }
    }
}
