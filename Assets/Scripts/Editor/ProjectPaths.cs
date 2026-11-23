using UnityEditor;

namespace CylinderShooter.Editor
{
    /// <summary>
    /// Every asset path the editor tools and tests load or create, in one place. When a folder moves, only this
    /// file changes. Layout: materials in <c>Materials/</c>, tunable ScriptableObjects in <c>Tuning/</c>, prefabs in
    /// <c>Prefabs/</c>, raw art (meshes, textures, shaders) in <c>Art/</c>.
    /// </summary>
    public static class ProjectPaths
    {
        public const string TuningFolder = "Assets/Tuning";
        public const string PlayerTuningFolder = TuningFolder + "/Player";
        public const string FormationsFolder = TuningFolder + "/Formations";

        public const string CylinderConfig = TuningFolder + "/World/CylinderConfig.asset";
        public const string ShipTuning = PlayerTuningFolder + "/ShipTuning.asset";
        public const string WeaponTuning = PlayerTuningFolder + "/WeaponTuning.asset";
        public const string PlayerLifeTuning = PlayerTuningFolder + "/PlayerLifeTuning.asset";

        public const string MaterialsFolder = "Assets/Materials";
        public const string CombatMaterialsFolder = MaterialsFolder + "/Combat";
        public const string ArenaMaterialsFolder = MaterialsFolder + "/Arena";
        public const string EnvironmentMaterialsFolder = MaterialsFolder + "/Environment";

        public const string PrefabsFolder = "Assets/Prefabs";
        public const string CombatPrefabsFolder = PrefabsFolder + "/Combat";
        public const string ChaserPrefab = PrefabsFolder + "/Enemies/Chaser.prefab";
        public const string DrifterPrefab = PrefabsFolder + "/Enemies/Drifter.prefab";

        public const string ArenaArtFolder = "Assets/Art/Arena";
        public const string EnvironmentArtFolder = "Assets/Art/Environment";
        public const string CityPrefabsFolder = "Assets/Art/City/Prefabs";

        public const string LevelTestScene = "Assets/Scenes/LevelTest.unity";
        public const string LookTestScene = "Assets/Scenes/LookTest.unity";
        public const string PlayArenaScene = "Assets/Scenes/PlayArena.unity";

        /// <summary>Creates a folder and any missing parents, e.g. "Assets/Tuning/Player".</summary>
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        /// <summary>The folder part of an asset path ("Assets/A/b.asset" gives "Assets/A").</summary>
        public static string FolderOf(string assetPath) => assetPath.Substring(0, assetPath.LastIndexOf('/'));
    }
}
