using System;
using UnityEngine;

namespace YunJian.Riichi
{
    // scene_mj light, Catfood toon/outline and effect_shadow values exported by
    // scripts/import-table-look.py. See TableLook/source.json for provenance.
    public static class TableLook
    {
        [Serializable] public class Light { public float[] rotation; }
        [Serializable] public class Toon { public float splitThreshold; public float[] colorLight, colorUnlight, tileTexScale, tileTexOffset; }
        [Serializable] public class Toons { public Toon body, face; }
        [Serializable] public class Outline { public float[] tint; }
        [Serializable] public class Shadow { public string texture; public float[] tint, meshMin, meshMax, tileBounds; }
        [Serializable] class Data { public Light light; public Toons toon; public Outline outline; public Shadow shadow; }
        static Data data;
        static Data Source => data ?? (data = JsonUtility.FromJson<Data>(Resources.Load<TextAsset>("Majsoul/TableLook/lighting").text));
        public const float SourceUnits = .33f / .02947767f;
        public static Color ColorOf(float[] c) => new Color(c[0], c[1], c[2], c[3]);
        public static Toon Body => Source.toon.body;
        public static Toon Face => Source.toon.face;
        public static Color OutlineColor { get { var c = ColorOf(Source.outline.tint); c.a = 1; return c; } }
        public static Shadow TileShadow => Source.shadow;
        // _WorldSpaceLightPos0 of a directional light points towards the light. Source
        // scene axes map to ours by a half turn about Y: (-x, y, -z).
        public static Vector3 LightDirection
        {
            get
            {
                var r = Source.light.rotation; var toward = -(new Quaternion(r[0], r[1], r[2], r[3]) * Vector3.forward);
                return new Vector3(-toward.x, toward.y, -toward.z).normalized;
            }
        }
        // code.js sizes the 2.54-unit shadow plane by .53 x PAIMODEL_WIDTH/HEIGHT
        // (.032775*.94 by .043225*.94) at the tile's base, centred under it.
        public static Vector2 ShadowFootprint
        {
            get
            {
                var s = Source.shadow; float side = s.meshMax[0] - s.meshMin[0];
                return new Vector2(side * .53f * .032775f * .94f / s.tileBounds[0], side * .53f * .043225f * .94f / s.tileBounds[1]);
            }
        }
        public static void Apply(Material material, Toon toon)
        {
            material.SetFloat("_SplitThreshold", toon.splitThreshold);
            material.SetColor("_ColorLight", ColorOf(toon.colorLight));
            material.SetColor("_ColorUnlight", ColorOf(toon.colorUnlight));
        }
    }
}
