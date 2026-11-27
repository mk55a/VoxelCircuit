using CylinderShooter.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CylinderShooter.Tests
{
    public sealed class LevelDebuggerWindowTests
    {
        [Test]
        public void Window_BuildsAndHidesControlsOutsidePlayMode()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<StyleSheet>(ProjectPaths.LevelDebuggerStyle));
            var window = ScriptableObject.CreateInstance<LevelDebuggerWindow>();
            try
            {
                window.CreateGUI();
                var root = window.rootVisualElement;
                Assert.IsNotNull(root.Q<DirectorGraph>());
                Button skip = null;
                root.Query<Button>().ForEach(b => { if (b.text == "Skip stage") skip = b; });
                Assert.IsNotNull(skip);
                Assert.IsTrue(IsInsideHidden(skip), "No level is running, so the controls must be hidden.");
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
        }

        private static bool IsInsideHidden(VisualElement element)
        {
            for (var e = element; e != null; e = e.parent)
                if (e.ClassListContains("hidden")) return true;
            return false;
        }
    }
}
