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
        // Camera.Render inside OnGUI leaves the GUI pass showing an unrendered target.
        // Event.current is not reliably null outside OnGUI, so RiichiClient.OnGUI sets this.
        public static bool InGui;
        public readonly float TableLength, TableDepth;
        public readonly TableTileGeometry Geometry;
        readonly Mesh tableMesh, handMesh, outlineMesh, glowMesh;
        readonly Material shell, outline, glow;
        readonly float outlineScale;
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        readonly Dictionary<string, RenderTexture> doraTextures = new Dictionary<string, RenderTexture>();
        readonly Dictionary<string,Transform> liveDora=new Dictionary<string,Transform>();
        readonly Dictionary<string, RenderTexture> handTextures = new Dictionary<string, RenderTexture>();
        Camera doraCamera, handPreviewCamera;
        public OriginalTiles()
        {
            var d = JsonUtility.FromJson<MeshData>(Resources.Load<TextAsset>("Majsoul/OriginalTile/mesh").text);
            const float width = TableTileGeometry.SourceWidth, depth = TableTileGeometry.SourceDepth;
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
            // mjp_mid: back skin as _MainTex, face atlas body column as _TileTex.
            shell = new Material(Shader.Find("YunJian/OriginalTileBody")) { mainTexture = Texture("table-back") };
            var body = TableLook.Body; shell.SetTexture("_TileTex", Texture("table-face"));
            shell.SetTextureScale("_TileTex", new Vector2(body.tileTexScale[0], body.tileTexScale[1])); shell.SetTextureOffset("_TileTex", new Vector2(body.tileTexOffset[0], body.tileTexOffset[1]));
            TableLook.Apply(shell, body);
            var edge=JsonUtility.FromJson<OutlineData>(Resources.Load<TextAsset>("Majsoul/OriginalTile/outline").text);
            var edgeVertices=new Vector3[edge.vertices.Length/3];
            for(int i=0;i<edgeVertices.Length;i++)edgeVertices[i]=new Vector3(-edge.vertices[i*3],edge.vertices[i*3+2]+depth/2,edge.vertices[i*3+1])/width;
            outlineMesh=new Mesh {name="Original maque_outline 031",vertices=edgeVertices,triangles=edge.triangles};outlineMesh.RecalculateBounds();
            // Catfood/Outline outputs the material's _Tint; _OutlineColor in outline.json is unused by it.
            outlineScale=edge.scale;outline=new Material(Shader.Find("YunJian/OriginalTileOutline")) {color=TableLook.OutlineColor};
            Geometry = new TableTileGeometry(tableMesh.bounds, outlineMesh.bounds, outlineScale);
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
                // The APK atlas uses 52x72 cells, with the face starting at (3,3).
                // The old per-tile crop sampled (1,1), pushing every suit right/down.
                // Sample the atlas directly: the 50x70 face extends past that old crop.
                int number=tile[1]-'0',index=tile[0]=='m'?number:tile[0]=='p'?10+number:tile[0]=='s'?20+number:29+number;
                m = new Material(Shader.Find("YunJian/OriginalTileFace")) { mainTexture = Texture("table-face") };
                if(m.mainTexture!=null){m.mainTexture.anisoLevel=16;m.mainTexture.filterMode=FilterMode.Trilinear;m.mainTexture.mipMapBias=-.35f;}
                m.mainTextureScale = new Vector2(50f/512,70f/512);
                m.mainTextureOffset = new Vector2((3+index%7*52)/512f,1-(3+index/7*72+70)/512f);
                TableLook.Apply(m, TableLook.Face);
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
        public static void AddShine(Transform tile,bool hand){
            var parent=hand?tile:tile.Find("Original tile");if(parent.Find("APK dora shine")!=null)return;
            var shine=new GameObject("APK dora shine");shine.layer=parent.gameObject.layer;shine.transform.SetParent(parent,false);shine.AddComponent<NativeDoraShine>().Initialize(hand);
        }
        public Transform CreateTable(Transform parent, string tile, Vector3 position, bool hidden=false, bool sideways=false, float opacity=1)
            => CreateModel(parent,tile,position,TableTileGeometry.Width,hidden,sideways,opacity);
        // Fixed-size GUI previews are separate from gameplay geometry.
        Transform CreateModel(Transform parent, string tile, Vector3 position, float width, bool hidden, bool sideways, float opacity)
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
        public static void MatchModel(Transform tile,bool match)
        {
            var renderer=tile.Find("Original tile").GetComponent<MeshRenderer>();
            var block=new MaterialPropertyBlock();block.SetFloat("_Match",match?1:0);
            // One block on the renderer applies the source tint to face and shell.
            renderer.SetPropertyBlock(block);
            tile.Find("Original tile/Original tile outline").GetComponent<MeshRenderer>().SetPropertyBlock(block);
        }
        public static bool IsMatchedModel(Transform tile)
        {
            var block=new MaterialPropertyBlock();tile.Find("Original tile").GetComponent<MeshRenderer>().GetPropertyBlock(block);
            return block.GetFloat("_Match")>.5f;
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
        public Transform CreateStandingBack(Transform parent, Vector3 position)
        {
            // A player's concealed hand stands on its short edge, with the back
            // facing the table centre (+Z in seat space). Face-down kans still
            // use CreateTable and must keep their horizontal pose.
            const float width=TableTileGeometry.Width;
            var root = CreateTable(parent, "", position, true, false, 1);
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
            // Never render from inside OnGUI; TableRenderer prewarms every face on Awake.
            if (OriginalTiles.InGui) return null;
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
        // Camera.Render must not run inside OnGUI (the GUI pass then shows an unrendered
        // target). TableRenderer.LateUpdate calls this with refresh=true; OnGUI only reads.
        public Texture DoraTexture(string tile,bool match=false,bool shine=false,bool refresh=false)
        {
            bool hidden=string.IsNullOrEmpty(tile);match&=!hidden;shine&=!hidden;
            string key = (hidden ? "back" : tile)+(match?"/blue":"")+(shine?"/shine":"");
            if (doraTextures.TryGetValue(key, out var cached)) {
                if(shine&&refresh&&!InGui){var model=liveDora[key];model.gameObject.SetActive(true);model.GetComponentInChildren<NativeDoraShine>().Apply();doraCamera.targetTexture=cached;doraCamera.Render();doraCamera.targetTexture=null;model.gameObject.SetActive(false);}return cached;
            }
            if(InGui)return null;
            if (doraCamera == null)
            {
                doraCamera = new GameObject("Dora tile preview camera").AddComponent<Camera>();
                doraCamera.enabled = false; doraCamera.orthographic = true; doraCamera.orthographicSize = .78f;
                doraCamera.clearFlags = CameraClearFlags.SolidColor; doraCamera.backgroundColor = Color.clear;
                doraCamera.cullingMask = 1 << 13; doraCamera.nearClipPlane = .1f; doraCamera.farClipPlane = 10;
            }
            Vector3 origin = new Vector3(50,50,50);
            var root = CreateModel(null, tile, origin, 1, hidden, false, 1);
            if(shine)AddShine(root,false);
            MatchModel(root,match);
            foreach (var t in root.GetComponentsInChildren<Transform>()) t.gameObject.layer = 13;
            doraCamera.transform.position = origin + new Vector3(0,4,-.65f);
            doraCamera.transform.LookAt(origin + Vector3.up * TableDepth / 2, Vector3.forward);
            var target = new RenderTexture(108,162,24,RenderTextureFormat.ARGB32) { antiAliasing = 4, name = "Dora " + key };
            target.Create(); doraCamera.targetTexture = target; doraCamera.Render(); doraCamera.targetTexture = null;
            root.gameObject.SetActive(false);if(shine)liveDora[key]=root;else UnityEngine.Object.Destroy(root.gameObject);
            doraTextures[key] = target; return target;
        }
        public void Dispose() {foreach(var model in liveDora.Values)if(model!=null)UnityEngine.Object.Destroy(model.gameObject);liveDora.Clear(); UnityEngine.Object.Destroy(tableMesh); UnityEngine.Object.Destroy(handMesh);UnityEngine.Object.Destroy(outlineMesh);UnityEngine.Object.Destroy(glowMesh); UnityEngine.Object.Destroy(shell);UnityEngine.Object.Destroy(outline);UnityEngine.Object.Destroy(glow); foreach (var m in materials.Values) UnityEngine.Object.Destroy(m); foreach (var t in doraTextures.Values) { t.Release(); UnityEngine.Object.Destroy(t); } foreach (var t in handTextures.Values) { t.Release(); UnityEngine.Object.Destroy(t); } if (doraCamera != null) UnityEngine.Object.Destroy(doraCamera.gameObject); if (handPreviewCamera != null) UnityEngine.Object.Destroy(handPreviewCamera.gameObject); }
    }
}
