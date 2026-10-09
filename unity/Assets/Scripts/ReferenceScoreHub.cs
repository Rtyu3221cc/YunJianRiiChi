using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YunJian.Riichi
{
    // Plate, slots and light share one 256px coordinate system and one plane.
    public sealed class ReferenceScoreHub
    {
        public const float Size = 2.05f;
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        Mesh plateMesh, lightMesh;
        Texture2D lightTexture;
        static Vector3 Point(float x, float y, float lift = 0) => new Vector3((x / 256 - .5f) * Size, lift, (.5f - y / 256) * Size);
        Material Material(string path, Rect uv, int queue)
        {
            string key = path + uv;
            if (!materials.TryGetValue(key, out var m))
            {
                m = new Material(Shader.Find("Unlit/Transparent")) { mainTexture = Resources.Load<Texture2D>("Majsoul/" + path), mainTextureScale = uv.size, mainTextureOffset = uv.position, renderQueue = queue };
                materials[key] = m;
            }
            return m;
        }
        static void MeshObject(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        }
        Mesh Plate()
        {
            if (plateMesh != null) return plateMesh;
            // Use the complete original plate UVs. Cutting and rotating pieces
            // of the frame changes its bevels and creates visible seams.
            plateMesh = new Mesh { name = "Original score plate",
                vertices = new[] { Point(0,0), Point(0,256), Point(256,256), Point(256,0) },
                uv = new[] { Vector2.up, Vector2.zero, Vector2.right, Vector2.one },
                triangles = new[] { 0,2,1,0,3,2 } };
            plateMesh.RecalculateBounds(); plateMesh.RecalculateNormals(); return plateMesh;
        }
        void Glyph(Transform parent, string atlas, Rect uv, float x, float y, float w, float h)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Hub glyph " + atlas;
            q.transform.SetParent(parent, false); q.transform.localPosition = Point(x, y, .002f);
            q.transform.localRotation = Quaternion.Euler(90,0,0); q.transform.localScale = new Vector3(w / 256 * Size, h / 256 * Size, 1);
            var r = q.GetComponent<Renderer>(); r.sharedMaterial = Material("Hud/" + atlas, uv, 2460);
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; Object.Destroy(q.GetComponent<Collider>());
        }
        public Transform Create(Transform parent, RoomView state)
        {
            var root = new GameObject("Score hub").transform; root.SetParent(parent, false); root.localPosition = new Vector3(0, .092f, 0);
            MeshObject(root, "Score plate", Plate(), Material("Table/score-hub", new Rect(0,0,1,1), 2450));
            float height = 23, charWidth = height * 29 / 35, digitWidth = height * 17 / 31;
            Glyph(root, "chang", new Rect((1 + Mathf.Clamp(state.round / state.n,0,3)) / 6f,0,1f/6,1), 128 - (charWidth + digitWidth)/2,110,charWidth,height);
            Glyph(root, "ju", new Rect((state.round % state.n) / 10f,0,.1f,1),128,110,digitWidth,height);
            Glyph(root, "chang",new Rect(5f/6,0,1f/6,1),128 + (charWidth + digitWidth)/2,110,charWidth,height);
            Glyph(root,"word_left",new Rect(0,0,1,1),109,150,18.8f,18);
            Digits(root,"number_left",state.remaining.ToString("00"),140,150,14.4f,18,false);
            return root;
        }
        public void Seat(Transform hub, PlayerView player, float angle, bool current)
        {
            var root = new GameObject("Hub seat " + player.wind).transform; root.SetParent(hub, false); root.localRotation = Quaternion.Euler(0, angle, 0);
            Digits(root,"number_score",player.score.ToString(),128,192,11.69f,19.2f,true);
            Glyph(root,"dir",new Rect(player.wind / 4f,0,.245f,1),32,224,31,31);
            if (current)
            {
                if (lightMesh == null)
                {
                    lightMesh = new Mesh { name = "Inset turn light",
                        vertices = new[] { Point(88,222,.003f), Point(91,230,.003f), Point(165,230,.003f), Point(168,222,.003f) },
                        uv = new[] { Vector2.up, Vector2.zero, Vector2.right, Vector2.one }, triangles = new[] { 0,2,1,0,3,2 } };
                    lightMesh.RecalculateBounds();
                    lightTexture = new Texture2D(1,16,TextureFormat.RGBA32,false) { name = "Amber turn indicator", wrapMode = TextureWrapMode.Clamp };
                    for (int y = 0; y < 16; y++)
                        lightTexture.SetPixel(0,y,Color.Lerp(new Color(1,.58f,.06f),new Color(1,.96f,.60f),y/15f));
                    lightTexture.Apply();
                }
                var m = Material("Hud/turn-light", new Rect(0,0,1,1), 2460);
                m.mainTexture = lightTexture;
                MeshObject(root, "Turn light", lightMesh, m);
            }
        }
        void Digits(Transform parent,string atlas,string text,float x,float y,float w,float h,bool score)
        {
            for (int i=0;i<text.Length;i++)
            {
                int slot = text[i]=='-' ? 0 : text[i]-'0'+(score?2:0);
                Glyph(parent,atlas,new Rect(slot/(score?12f:10f),0,1f/(score?12:10),score?1f/3:1),x+(i-(text.Length-1)/2f)*w,y,w,h);
            }
        }
        public void Dispose()
        {
            foreach (var m in materials.Values) Object.Destroy(m);
            Object.Destroy(plateMesh); Object.Destroy(lightMesh);
            Object.Destroy(lightTexture);
        }
    }
}
