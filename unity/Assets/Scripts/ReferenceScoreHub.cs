using System.Collections.Generic;
using UnityEngine;

namespace YunJian.Riichi
{
    // The original hub uses separate atlas glyphs. Positions are expressed in
    // pixels of the 256 x 256 source plate, so every glyph stays in its recess.
    public sealed class ReferenceScoreHub
    {
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        void Glyph(Transform parent, string atlas, Rect uv, float x, float y, float w, float h)
        {
            string key = atlas + uv;
            if (!materials.TryGetValue(key, out var m))
            {
                m = new Material(Shader.Find("Unlit/Transparent")) { mainTexture = Resources.Load<Texture2D>("Majsoul/Hud/" + atlas), mainTextureScale = uv.size, mainTextureOffset = uv.position, renderQueue = 2460 };
                materials[key] = m;
            }
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Hub glyph " + atlas;
            q.transform.SetParent(parent, false); q.transform.localPosition = new Vector3((x / 256 - .5f) * 2.05f, .102f, (.5f - y / 256) * 2.05f);
            q.transform.localRotation = Quaternion.Euler(90,0,0); q.transform.localScale = new Vector3(w / 256 * 2.05f, h / 256 * 2.05f, 1);
            q.GetComponent<Renderer>().sharedMaterial = m; Object.Destroy(q.GetComponent<Collider>());
        }
        public void Center(Transform parent, RoomView state)
        {
            // The upper and lower windows are split by the plate's horizontal divider.
            float height = 23, charWidth = height * 29 / 35, digitWidth = height * 17 / 31;
            Glyph(parent, "chang", new Rect((1 + Mathf.Clamp(state.round / state.n,0,3)) / 6f,0,1f/6,1), 128 - (charWidth + digitWidth)/2,110,charWidth,height);
            // Original ju atlas is ordered 1..9,0 (unlike the other number atlases).
            Glyph(parent, "ju", new Rect((state.round % state.n) / 10f,0,.1f,1),128,110,digitWidth,height);
            Glyph(parent, "chang",new Rect(5f/6,0,1f/6,1),128 + (charWidth + digitWidth)/2,110,charWidth,height);
            Glyph(parent,"word_left",new Rect(0,0,1,1),109,150,18.8f,18);
            Digits(parent,"number_left",state.remaining.ToString("00"),140,150,14.4f,18,false);
        }
        public void Seat(Transform parent, PlayerView player)
        {
            Digits(parent,"number_score",player.score.ToString(),128,210,9.74f,16,true);
            Glyph(parent,"dir",new Rect(player.wind / 4f,0,.245f,1),32,224,31,31);
        }
        void Digits(Transform parent,string atlas,string text,float x,float y,float w,float h,bool score)
        {
            for (int i=0;i<text.Length;i++)
            {
                int slot = text[i]=='-' ? 0 : text[i]-'0'+(score?2:0);
                Glyph(parent,atlas,new Rect(slot/(score?12f:10f),0,1f/(score?12:10),score?1f/3:1),x+(i-(text.Length-1)/2f)*w,y,w,h);
            }
        }
        public void Dispose() { foreach (var m in materials.Values) Object.Destroy(m); }
    }
}
