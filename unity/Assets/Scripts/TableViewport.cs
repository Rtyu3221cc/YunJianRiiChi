using UnityEngine;

namespace YunJian.Riichi
{
    // One scale for both axes. UI drawing and pointer hit tests share this transform.
    public struct TableViewport
    {
        public float Scale, Width, Height;
        public Vector2 Origin;
        public Matrix4x4 Matrix => Matrix4x4.TRS(new Vector3(Origin.x, Origin.y, 0), Quaternion.identity, new Vector3(Scale, Scale, 1));
        public Vector2 FromScreen(Vector2 point, float screenHeight) => (new Vector2(point.x, screenHeight - point.y) - Origin) / Scale;

        public static TableViewport Fit(float width, float height, Rect safeArea, bool table)
        {
            float scale = Mathf.Min(safeArea.width / 1280f, safeArea.height / (table ? 600f : 720f));
            var layout = new TableViewport { Scale = scale, Width = safeArea.width / scale, Height = safeArea.height / scale };
            layout.Origin = new Vector2(safeArea.x, height - safeArea.yMax);
            if (!table)
            {
                layout.Origin += new Vector2((safeArea.width - 1280 * scale) / 2, (safeArea.height - 720 * scale) / 2);
                layout.Width = 1280; layout.Height = 720;
            }
            return layout;
        }
    }
}
