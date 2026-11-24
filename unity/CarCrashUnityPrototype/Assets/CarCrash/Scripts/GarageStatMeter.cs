using UnityEngine;
using UnityEngine.UI;

namespace CarCrash
{
    internal sealed class GarageStatMeter : MaskableGraphic
    {
        private const int Segments = 12;
        private float _progress;

        public void SetValue(float value)
        {
            var progress = Mathf.Clamp01(value);
            if (Mathf.Approximately(progress, _progress)) return;
            _progress = progress;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            var stride = rect.width / Segments;
            var width = Mathf.Max(0f, stride - 2f);
            var track = new Color32(65, 91, 98, 255);
            for (var i = 0; i < Segments; i++)
            {
                var left = rect.xMin + i * stride;
                AddSegment(mesh, left, width, rect, track);
                var fill = Mathf.Clamp01(_progress * Segments - i);
                if (fill > 0f) AddSegment(mesh, left, width * fill, rect, color);
            }
        }

        private static void AddSegment(VertexHelper mesh, float x, float width, Rect rect, Color32 tint)
        {
            var start = mesh.currentVertCount;
            var angle = Mathf.Min(1.8f, width * .4f);
            mesh.AddVert(new Vector2(x, rect.yMin), tint, Vector2.zero);
            mesh.AddVert(new Vector2(x + width - angle, rect.yMin), tint, Vector2.zero);
            mesh.AddVert(new Vector2(x + width, rect.yMax), tint, Vector2.zero);
            mesh.AddVert(new Vector2(x + angle, rect.yMax), tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start + 2, start + 3, start);
        }
    }
}
