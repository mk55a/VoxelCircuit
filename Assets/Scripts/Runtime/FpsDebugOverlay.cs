#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace CylinderShooter.Debugging
{
    public sealed class FpsDebugOverlay : MonoBehaviour
    {
        private static FpsDebugOverlay s_instance;

        private FpsSampler _sampler = new FpsSampler();
        private GUIContent _content = new GUIContent("FPS: --");
        private GUIStyle _style;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (s_instance != null)
            {
                s_instance.ResetDisplay();
                return;
            }

            var overlay = new GameObject("FPS Debug Overlay");
            DontDestroyOnLoad(overlay);
            overlay.AddComponent<FpsDebugOverlay>();
        }

        private void OnEnable()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
        }

        private void OnDestroy()
        {
            if (s_instance == this) s_instance = null;
        }

        private void ResetDisplay()
        {
            _sampler = new FpsSampler();
            _content.text = "FPS: --";
            _style = null;
        }

        private void Update()
        {
            if (_sampler.RecordFrame(Time.unscaledDeltaTime, out float fps))
                _content.text = $"FPS: {fps:F0}";
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold
                };
            }

            _style.normal.textColor = Color.black;
            GUI.Label(new Rect(11f, 11f, 200f, 30f), _content, _style);
            _style.normal.textColor = Color.white;
            GUI.Label(new Rect(10f, 10f, 200f, 30f), _content, _style);
        }
    }
}
#endif
