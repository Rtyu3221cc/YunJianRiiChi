using System.Collections.Generic;
using UnityEngine;

namespace YunJian.Riichi
{
    // Shared chamfered tile meshes: rounded ivory shell, narrow crimson base.
    public sealed class TileGeometry
    {
        readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        public Mesh Get(float width, float length, bool face)
        {
            string key = width + ":" + length + ":" + face;
            if (meshes.TryGetValue(key, out var cached)) return cached;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            const int steps = 5, count = steps * 4;
            float radius = width * .09f;
            float[] levels = face ? new[] { 0f } : new[] { .027f, .043f, .118f, .141f };
            float[] insets = face ? new[] { 0f } : new[] { .010f, 0f, 0f, .017f };
            for (int ring = 0; ring < levels.Length; ring++)
            for (int corner = 0; corner < 4; corner++)
            for (int step = 0; step < steps; step++)
            {
                float angle = (corner * 90 + step * 90f / (steps - 1)) * Mathf.Deg2Rad;
                float x = (corner == 0 || corner == 3 ? 1 : -1) * (width / 2 - radius - insets[ring]) + Mathf.Cos(angle) * radius;
                float z = (corner < 2 ? 1 : -1) * (length / 2 - radius - insets[ring]) + Mathf.Sin(angle) * radius;
                vertices.Add(new Vector3(x, levels[ring], z)); uv.Add(new Vector2(x / width + .5f, z / length + .5f));
            }
            for (int ring = 0; ring < levels.Length - 1; ring++)
            for (int i = 0; i < count; i++)
            {
                int a = ring * count + i, b = ring * count + (i + 1) % count, c = a + count, d = b + count;
                triangles.AddRange(new[] { a, c, b, b, c, d });
            }
            int top = vertices.Count;
            vertices.Add(new Vector3(0, levels[levels.Length - 1], 0)); uv.Add(new Vector2(.5f, .5f));
            int start = (levels.Length - 1) * count;
            for (int i = 0; i < count; i++) triangles.AddRange(new[] { top, start + (i + 1) % count, start + i });
            var mesh = new Mesh { name = "Rounded tile " + key };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            meshes[key] = mesh; return mesh;
        }
        public void Dispose() { foreach (var mesh in meshes.Values) Object.Destroy(mesh); meshes.Clear(); }
    }
}
