using System.Collections;
using System.Reflection;
using CylinderShooter.Debugging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CylinderShooter.Tests
{
    public class FpsDebugOverlayTests
    {
        [UnityTest]
        public IEnumerator Overlay_IsUniqueAcrossSceneChangesAndUpdatesWhenPaused()
        {
            var overlays = Object.FindObjectsByType<FpsDebugOverlay>(FindObjectsSortMode.None);
            Assert.AreEqual(1, overlays.Length);
            var overlay = overlays[0];
            var initialize = typeof(FpsDebugOverlay).GetMethod("Initialize", BindingFlags.Static | BindingFlags.NonPublic);
            initialize.Invoke(null, null);
            Assert.AreEqual(1, Object.FindObjectsByType<FpsDebugOverlay>(FindObjectsSortMode.None).Length);

            var scene = SceneManager.CreateScene("FPS overlay test");
            yield return SceneManager.UnloadSceneAsync(scene);
            overlays = Object.FindObjectsByType<FpsDebugOverlay>(FindObjectsSortMode.None);
            Assert.AreEqual(1, overlays.Length);
            Assert.AreSame(overlay, overlays[0]);

            var content = (GUIContent)typeof(FpsDebugOverlay)
                .GetField("_content", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(overlay);
            content.text = "FPS: --";
            float previousTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 0f;
                yield return new WaitForSecondsRealtime(0.7f);
                Assert.AreNotEqual("FPS: --", content.text);
            }
            finally
            {
                Time.timeScale = previousTimeScale;
            }
        }
    }
}
