using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YunJian.Riichi
{
    // Geometry, UVs and hand dimensions are taken from the supplied mjp_new and
    // scene_mjhandpai prefabs. See OriginalTile/provenance.json.
    public sealed class OriginalTiles : IDisposable
    {
        [Serializable] class MeshData { public float[] vertices, normals, uv; public int[] body, face; }
        [Serializable] class OutlineData { public float[] vertices,color; public int[] triangles;public float scale; }
        public const float HandHeight = 2.54f, HandWidth = HandHeight * .62f;
        public readonly float TableLength, TableDepth;
        readonly Mesh tableMesh, handMesh, outlineMesh, glowMesh;
        readonly Material shell, outline, glow;
        readonly float outlineScale;
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        readonly Dictionary<string, RenderTexture> doraTextures = new Dictionary<string, RenderTexture>();
        readonly Dictionary<string, RenderTexture> handTextures = new Dictionary<string, RenderTexture>();
        Camera doraCamera, handPreviewCamera;
        public OriginalTiles()
        {
            var d = JsonUtility.FromJson<MeshData>(Resources.Load<TextAsset>("Majsoul/OriginalTile/mesh").text);
            const float width = .02947767f, depth = .01913448f;
            var vertices = new Vector3[d.vertices.Length / 3]; var normals = new Vector3[vertices.Length]; var uv = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vector3(-d.vertices[i * 3], d.vertices[i * 3 + 2] + depth / 2, d.vertices[i * 3 + 1]) / width;
                normals[i] = new Vector3(-d.normals[i * 3], d.normals[i * 3 + 2], d.normals[i * 3 + 1]);
                uv[i] = new Vector2(d.uv[i * 2], d.uv[i * 2 + 1]);
            }
            tableMesh = new Mesh { name = "Original mjp_new 001", vertices = vertices, normals = normals, uv = uv, subMeshCount = 2 };
            tableMesh.SetTriangles(d.body, 0); tableMesh.SetTriangles(d.face, 1); tableMesh.RecalculateBounds();
            TableLength = tableMesh.bounds.size.z; TableDepth = tableMesh.bounds.size.y;
            shell = new Material(Shader.Find("YunJian/OriginalTileBody")) { mainTexture = Texture("table-back") };
            shell.SetFloat("_Depth", TableDepth);
            var edge=JsonUtility.FromJson<OutlineData>(Resources.Load<TextAsset>("Majsoul/OriginalTile/outline").text);
            var edgeVertices=new Vector3[edge.vertices.Length/3];
            for(int i=0;i<edgeVertices.Length;i++)edgeVertices[i]=new Vector3(-edge.vertices[i*3],edge.vertices[i*3+2]+depth/2,edge.vertices[i*3+1])/width;
            outlineMesh=new Mesh {name="Original maque_outline 031",vertices=edgeVertices,triangles=edge.triangles};outlineMesh.RecalculateBounds();
            outlineScale=edge.scale;outline=new Material(Shader.Find("YunJian/OriginalTileOutline")) {color=new Color(edge.color[0],edge.color[1],edge.color[2],edge.color[3])};
            glowMesh=new Mesh {name="Ground particle quad",vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(.5f,0,-.5f)},uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)},triangles=new[]{0,1,2,0,2,3}};
            glowMesh.RecalculateBounds();glow=new Material(Shader.Find("YunJian/ResponseGlow")) {mainTexture=Resources.Load<Texture2D>("Majsoul/Actions/response-glow")};
            handMesh = new Mesh { name = "Original orthographic hand quad",
                vertices = new[] { new Vector3(-HandWidth/2,-HandHeight/2,0), new Vector3(-HandWidth/2,HandHeight/2,0), new Vector3(HandWidth/2,HandHeight/2,0), new Vector3(HandWidth/2,-HandHeight/2,0) },
                uv = new[] { new Vector2(0,0), new Vector2(0,1), new Vector2(1,1), new Vector2(1,0) }, triangles = new[] { 0,1,2,0,2,3 } };
            handMesh.RecalculateBounds(); handMesh.RecalculateNormals();
        }
        static Texture2D Texture(string name) => Resources.Load<Texture2D>("Majsoul/OriginalTile/" + name);
        Material Face(string tile, bool hand)
        {
            string key = (hand ? "hand/" : "table/") + tile;
            if (materials.TryGetValue(key, out var cached)) return cached;
            Material m;
            if (hand)
            {
                m = new Material(Shader.Find("YunJian/OriginalHand")) { mainTexture = Texture("hand-face") };
                if (string.IsNullOrEmpty(tile)) m.SetFloat("_FaceDown", 1);
                else {
                    int row = "smpz".IndexOf(tile[0]), column = tile[1] - '0' - (tile[0] == 'z' ? 1 : 0);
                    m.mainTextureScale = new Vector2(.1f, .25f); m.mainTextureOffset = new Vector2(column * .1f, .75f - row * .25f);
                }
                m.SetTexture("_BackTex", Texture("hand-back"));
            }
            else
            {
                // Table face is the 50 x 70 interior of the existing 52 x 72 crop.
                // The original mesh's face submesh supplies the UV mapping.
                m = new Material(Shader.Find("Unlit/Texture")) { mainTexture = Resources.Load<Texture2D>("Majsoul/TableTiles/" + tile) };
                m.mainTextureScale = new Vector2(50f / 52, 70f / 72); m.mainTextureOffset = new Vector2(1f / 52, 1f / 72);
            }
            materials[key] = m; return m;
        }
        public Transform CreateHand(Transform parent, string tile, Vector3 position)
        {
            var t = new GameObject("Hand " + tile).transform; t.SetParent(parent, false); t.localPosition = position; t.gameObject.layer = 12;
            t.gameObject.AddComponent<MeshFilter>().sharedMesh = handMesh;
            var r = t.gameObject.AddComponent<MeshRenderer>(); r.sharedMaterial = Face(tile, true); r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            t.gameObject.AddComponent<BoxCollider>().size = new Vector3(HandWidth, HandHeight, .04f);
            return t;
        }
        public Transform CreateTable(Transform parent, string tile, Vector3 position, float width, bool hidden, bool sideways, float opacity)
        {
            var root = new GameObject(hidden ? "Concealed" : tile).transform; root.SetParent(parent, false); root.localPosition = position;
            if (sideways) root.localRotation = Quaternion.Euler(0,90,0);
            var model = new GameObject("Original tile").transform; model.SetParent(root, false); model.localScale = Vector3.one * width;
            if (hidden) { model.localRotation = Quaternion.Euler(0,0,180); model.localPosition = Vector3.up * TableDepth * width; }
            model.gameObject.AddComponent<MeshFilter>().sharedMesh = tableMesh;
            model.gameObject.AddComponent<MeshRenderer>().sharedMaterials = new[] { shell, Face(hidden ? "z5" : tile, false) };
            var edge=new GameObject("Original tile outline").transform;edge.SetParent(model,false);edge.localScale=Vector3.one*outlineScale;
            edge.gameObject.AddComponent<MeshFilter>().sharedMesh=outlineMesh;
            var edgeRenderer=edge.gameObject.AddComponent<MeshRenderer>();edgeRenderer.sharedMaterial=outline;edgeRenderer.shadowCastingMode=ShadowCastingMode.Off;
            var box = root.gameObject.AddComponent<BoxCollider>(); box.center = Vector3.up * TableDepth * width / 2; box.size = new Vector3(width, TableDepth * width, TableLength * width);
            if (opacity < 1) { root.localScale = Vector3.one * .8f; root.localPosition -= Vector3.up * .04f; }
            return root;
        }
        public Transform CreateResponseGlow(Transform tile,float width,string name="Original response ground glow")
        {
            var light=new GameObject(name).transform;light.SetParent(tile,false);
            light.localPosition=Vector3.up*.006f;
            // The source image includes its bloom margins; align its inner ring to the foot.
            light.localScale=new Vector3(width*2.67f,1,width*TableLength*1.80f);
            light.gameObject.AddComponent<MeshFilter>().sharedMesh=glowMesh;
            var renderer=light.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=glow;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            return light;
        }
        public Transform CreateStandingBack(Transform parent, Vector3 position, float width)
        {
            // A player's concealed hand stands on its short edge, with the back
            // facing the table centre (+Z in seat space). Face-down kans still
            // use CreateTable and must keep their horizontal pose.
            var root = CreateTable(parent, "", position, width, true, false, 1);
            root.name = "Standing concealed hand";
            var model = root.Find("Original tile");
            model.localRotation = Quaternion.Euler(-90, 0, 0);
            model.localPosition = new Vector3(0, TableLength * width / 2, TableDepth * width / 2);
            var box = root.GetComponent<BoxCollider>();
            box.center = Vector3.up * TableLength * width / 2;
            box.size = new Vector3(width, TableLength * width, TableDepth * width);
            return root;
        }
        // UI_MJPai_Hand uses the selected gameplay hand face/back. Render that
        // same material into a fixed-aspect target instead of importing old crops.
        public Texture HandTexture(string tile)
        {
            string key = string.IsNullOrEmpty(tile) ? "back" : tile;
            if (handTextures.TryGetValue(key, out var cached)) return cached;
            if (handPreviewCamera == null) {
                handPreviewCamera = new GameObject("Result hand preview camera").AddComponent<Camera>();
                handPreviewCamera.enabled = false; handPreviewCamera.orthographic = true;
                handPreviewCamera.orthographicSize = HandHeight / 2;
                handPreviewCamera.clearFlags = CameraClearFlags.SolidColor; handPreviewCamera.backgroundColor = Color.clear;
                handPreviewCamera.cullingMask = 1 << 14; handPreviewCamera.nearClipPlane = .1f; handPreviewCamera.farClipPlane = 10;
            }
            Vector3 origin = new Vector3(60,60,60);
            var root = CreateHand(null, key == "back" ? null : tile, origin); root.gameObject.layer = 14;
            handPreviewCamera.transform.position = origin + Vector3.back * 3;
            var target = new RenderTexture(124, 200, 24, RenderTextureFormat.ARGB32) {
                name = "Result hand " + key, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };
            target.Create(); var previous = RenderTexture.active;
            handPreviewCamera.targetTexture = target; handPreviewCamera.Render(); handPreviewCamera.targetTexture = null;
            RenderTexture.active = previous;
            root.gameObject.SetActive(false); UnityEngine.Object.Destroy(root.gameObject);
            handTextures[key] = target; return target;
        }
        public Texture DoraTexture(string tile)
        {
            string key = string.IsNullOrEmpty(tile) ? "back" : tile;
            if (doraTextures.TryGetValue(key, out var cached)) return cached;
            if (doraCamera == null)
            {
                doraCamera = new GameObject("Dora tile preview camera").AddComponent<Camera>();
                doraCamera.enabled = false; doraCamera.orthographic = true; doraCamera.orthographicSize = .78f;
                doraCamera.clearFlags = CameraClearFlags.SolidColor; doraCamera.backgroundColor = Color.clear;
                doraCamera.cullingMask = 1 << 13; doraCamera.nearClipPlane = .1f; doraCamera.farClipPlane = 10;
            }
            Vector3 origin = new Vector3(50,50,50);
            var root = CreateTable(null, tile, origin, 1, key == "back", false, 1);
            foreach (var t in root.GetComponentsInChildren<Transform>()) t.gameObject.layer = 13;
            doraCamera.transform.position = origin + new Vector3(0,4,-.65f);
            doraCamera.transform.LookAt(origin + Vector3.up * TableDepth / 2, Vector3.forward);
            var target = new RenderTexture(108,162,24,RenderTextureFormat.ARGB32) { antiAliasing = 4, name = "Dora " + key };
            target.Create(); doraCamera.targetTexture = target; doraCamera.Render(); doraCamera.targetTexture = null;
            root.gameObject.SetActive(false); UnityEngine.Object.Destroy(root.gameObject);
            doraTextures[key] = target; return target;
        }
        public void Dispose() { UnityEngine.Object.Destroy(tableMesh); UnityEngine.Object.Destroy(handMesh);UnityEngine.Object.Destroy(outlineMesh);UnityEngine.Object.Destroy(glowMesh); UnityEngine.Object.Destroy(shell);UnityEngine.Object.Destroy(outline);UnityEngine.Object.Destroy(glow); foreach (var m in materials.Values) UnityEngine.Object.Destroy(m); foreach (var t in doraTextures.Values) { t.Release(); UnityEngine.Object.Destroy(t); } foreach (var t in handTextures.Values) { t.Release(); UnityEngine.Object.Destroy(t); } if (doraCamera != null) UnityEngine.Object.Destroy(doraCamera.gameObject); if (handPreviewCamera != null) UnityEngine.Object.Destroy(handPreviewCamera.gameObject); }
    }
}
