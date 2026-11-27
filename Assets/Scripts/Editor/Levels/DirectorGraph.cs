using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CylinderShooter.Editor
{
    /// <summary>
    /// The pace of the current stage as it actually played: credits (orange, against the credit cap) and enemies alive
    /// (cyan, against the stage's limit) over time, with a white tick at each group that spawned. This is where the
    /// tuned numbers become visible, instead of being typed in as a curve.
    /// </summary>
    public sealed class DirectorGraph : VisualElement
    {
        private const float WindowSeconds = 60f;
        private static readonly Color CreditColor = new Color(1f, 0.5f, 0.16f);
        private static readonly Color AliveColor = new Color(0.49f, 0.98f, 1f);
        private static readonly Color SpawnColor = new Color(1f, 1f, 1f, 0.7f);

        private readonly List<Vector3> _samples = new List<Vector3>();   // time, credits 0-1, alive 0-1
        private readonly List<float> _spawns = new List<float>();

        public DirectorGraph()
        {
            AddToClassList("graph");
            generateVisualContent += Draw;
        }

        public void Clear()
        {
            _samples.Clear();
            _spawns.Clear();
            MarkDirtyRepaint();
        }

        public void Add(float time, float credits01, float alive01)
        {
            _samples.Add(new Vector3(time, Mathf.Clamp01(credits01), Mathf.Clamp01(alive01)));
            while (_samples.Count > 0 && time - _samples[0].x > WindowSeconds) _samples.RemoveAt(0);
            while (_spawns.Count > 0 && time - _spawns[0] > WindowSeconds) _spawns.RemoveAt(0);
            MarkDirtyRepaint();
        }

        public void MarkSpawn(float time) => _spawns.Add(time);

        private void Draw(MeshGenerationContext context)
        {
            float width = contentRect.width;
            float height = contentRect.height;
            if (_samples.Count < 2 || width < 1f || height < 1f) return;
            float end = _samples[_samples.Count - 1].x;
            float start = end - WindowSeconds;
            var painter = context.painter2D;

            painter.lineWidth = 1f;
            painter.strokeColor = SpawnColor;
            for (int i = 0; i < _spawns.Count; i++)
            {
                float x = (_spawns[i] - start) / WindowSeconds * width;
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, height));
                painter.LineTo(new Vector2(x, height * 0.75f));
                painter.Stroke();
            }
            Line(painter, start, width, height, CreditColor, true);
            Line(painter, start, width, height, AliveColor, false);
        }

        private void Line(Painter2D painter, float start, float width, float height, Color color, bool credits)
        {
            painter.lineWidth = 2f;
            painter.strokeColor = color;
            painter.BeginPath();
            for (int i = 0; i < _samples.Count; i++)
            {
                var s = _samples[i];
                var point = new Vector2((s.x - start) / WindowSeconds * width, height * (1f - (credits ? s.y : s.z)));
                if (i == 0) painter.MoveTo(point);
                else painter.LineTo(point);
            }
            painter.Stroke();
        }
    }
}
