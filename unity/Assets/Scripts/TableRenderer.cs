using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace YunJian.Riichi
{
    public class TableRenderer : MonoBehaviour
    {
        public RiichiClient Client;
        public Camera Camera { get; private set; }
        public Camera HandCamera { get; private set; }
        OriginalTiles original;
        readonly ReferenceScoreHub scoreHub = new ReferenceScoreHub();
        // Outside OnGUI this also re-renders a shining preview; inside it only reads.
        public Texture DoraTexture(string tile,bool match=false) => original.DoraTexture(tile,match,state!=null&&IsDora(tile),!OriginalTiles.InGui);
        // Render the indicator and settlement dora/ura previews before the GUI pass reads them.
        void RefreshDoraPreviews()
        {
            if(state==null || (state.phase!="play" && state.phase!="reaction" && state.phase!="result"))return;
            original.DoraTexture(null,false,false,true);
            foreach(string indicator in state.indicators??new string[0])original.DoraTexture(indicator,IsMatchingTile(indicator),IsDora(indicator),true);
            foreach(var detail in state.result?.details??new WinDetail[0])foreach(string tile in detail.ura??new string[0])original.DoraTexture(tile,false,IsDora(tile),true);
        }
        static readonly string[] AllFaces="m0 m1 m2 m3 m4 m5 m6 m7 m8 m9 p0 p1 p2 p3 p4 p5 p6 p7 p8 p9 s0 s1 s2 s3 s4 s5 s6 s7 s8 s9 z1 z2 z3 z4 z5 z6 z7".Split(' ');
        public bool IsDora(string tile){
            if(string.IsNullOrEmpty(tile))return false;if(tile.Length>1&&tile[1]=='0')return true;
            foreach(string indicator in state.indicators??new string[0]){
                int value=indicator[1]-'0';if(value==0)value=5;int next=indicator[0]=='z'?(value<=4?value%4+1:(value-4)%3+5):state.n==3&&indicator[0]=='m'?(value==1?9:1):value%9+1;
                if(tile==indicator[0].ToString()+next)return true;
            }return false;
        }
        public Texture HandTexture(string tile) => original.HandTexture(tile);
        Transform content;
        RoomView state;
        readonly Dictionary<string, Material> faces = new Dictionary<string, Material>();
        readonly Dictionary<string, Texture2D> portraits = new Dictionary<string, Texture2D>();
        readonly List<Transform> handTiles = new List<Transform>();
        readonly List<Vector3> handBase = new List<Vector3>();
        readonly HandOpening opening = new HandOpening();
        public bool ArrangingHand => opening.Active;
        public HandOpening Opening => opening;
        Material ivory, frame, felt, red, gold, contactShadow;
        Mesh shadowQuad;
        Vector2 shadowFootprint;
        Font font;
        Transform cameraHand;
        Transform responseTarget;
        readonly List<Tuple<Transform,string>> exposedTiles=new List<Tuple<Transform,string>>();
        string selectedTile;
        public bool IsMatchingTile(string tile) => selectedTile!=null && TileKind(tile)==selectedTile;
        static string TileKind(string tile) => string.IsNullOrEmpty(tile)?null:tile.Replace("0","5");
        string drawKey;float drawBegan;
        readonly List<Tuple<Transform,Vector3>> drawTiles=new List<Tuple<Transform,Vector3>>();
        public Rect HandScreenBounds()
        {
            if(handTiles.Count==0)return new Rect();
            Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);
            foreach(var hand in handTiles){var b=hand.GetComponent<Renderer>().bounds;
                foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1}){
                    var p=HandCamera.WorldToScreenPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,0)));
                    min=Vector2.Min(min,p);max=Vector2.Max(max,p);
                }
            }
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        void UpdateDrawReveal()
        {
            if(drawTiles.Count==0)return;
            var fx=GetComponent<TableEffects>();
            if(drawBegan<0){if(fx.HasPendingActions && !fx.WinActionStarted)return;drawBegan=Time.unscaledTime;GetComponent<GameAudio>()?.DrawReveal(drawKey);}
            float age=Time.unscaledTime-drawBegan,t=HandRevealMotion.Progress(age);
            if(cameraHand!=null && state?.phase=="result" && (state.result?.details??new WinDetail[0]).Any(d=>d.seat==state.you)){
                foreach(var hand in handTiles){var renderer=hand.GetComponent<MeshRenderer>();var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetFloat("_Opacity",Mathf.Clamp01(1-age/.14f));renderer.SetPropertyBlock(block);}
            }
            foreach(var tile in drawTiles){
                tile.Item1.localRotation=Quaternion.Euler(-90*(1-t),0,0);
                tile.Item1.localPosition=tile.Item2+Vector3.up*original.TableLength*.29f*.5f*Mathf.Max(0,1-t);
            }
        }
        Transform selfMelds;
        readonly List<Transform> exposedGroups=new List<Transform>();
        readonly Dictionary<Mesh,Vector3[]> tileVertices=new Dictionary<Mesh,Vector3[]>();
        readonly Dictionary<int, List<Transform>> riverTiles = new Dictionary<int, List<Transform>>();
        readonly Dictionary<int, Transform> stickAnchors = new Dictionary<int, Transform>();
        readonly Dictionary<int, Transform> meldAnchors = new Dictionary<int, Transform>();
        public sealed class MeldVisual {public Transform group,anchor,extra;public string value;public Vector3 extraRest;}
        readonly Dictionary<int,List<MeldVisual>> meldGroups=new Dictionary<int,List<MeldVisual>>();
        public MeldVisual EventMeld(PublicSound e){
            if(!meldGroups.TryGetValue(e.seat,out var list)||list.Count==0)return null;
            if(!string.IsNullOrEmpty(e.meld)){var exact=list.FirstOrDefault(m=>m.value==e.meld);if(exact!=null)return exact;}
            return list[list.Count-1];
        }
        readonly Dictionary<int, List<Transform>> northTiles = new Dictionary<int, List<Transform>>();
        readonly Dictionary<int, Transform> winningTiles = new Dictionary<int, Transform>();

        void Awake()
        {
            var cameraObject = new GameObject("TableCamera"); Camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            const float sourceUnits = .33f / .02947767f;
            Camera.transform.position = new Vector3(0, .846f, -.882f) * sourceUnits;
            Camera.transform.rotation = new Quaternion(.398749113f, 0, 0, .917060137f);
            Camera.fieldOfView = 28; Camera.nearClipPlane = .1f; Camera.farClipPlane = 50;
            Camera.clearFlags = CameraClearFlags.SolidColor; Camera.backgroundColor = Color.black;
            Camera.allowMSAA = true; QualitySettings.antiAliasing = 4;
            QualitySettings.anisotropicFiltering=AnisotropicFiltering.ForceEnable;
            Camera.cullingMask &= ~((1 << 12) | (1 << 13) | (1 << 14) | (1 << 15));
            HandCamera = new GameObject("Original orthographic hand camera").AddComponent<Camera>();
            HandCamera.transform.position = new Vector3(0, 0, -30);
            HandCamera.orthographic = true; HandCamera.orthographicSize = 9;
            HandCamera.clearFlags = CameraClearFlags.Depth; HandCamera.depth = Camera.depth + 1;
            HandCamera.cullingMask = 1 << 12; HandCamera.allowMSAA = true;
            // The Catfood tile shaders only read the direction of scene_mj/light.
            Shader.SetGlobalVector("_MjLightDir", TableLook.LightDirection);
            original = new OriginalTiles();
            // GUI panels (chi choices, waits, settlement) read these; render them up front.
            original.HandTexture(null); foreach (var face in AllFaces) original.HandTexture(face);
            new GameObject("Original room sun_shine").AddComponent<SunShine>();
            var lamp = new GameObject("SoftLight").AddComponent<Light>(); lamp.type = LightType.Directional;
            lamp.transform.rotation = Quaternion.Euler(65, -22, 0); lamp.intensity = .8f;
            lamp.shadows = LightShadows.Soft; lamp.shadowStrength = .6f; lamp.shadowBias = .015f;
            QualitySettings.shadowDistance = 25;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.42f, .40f, .38f);
            ivory = Material("Ivory", new Color(.90f, .89f, .85f));
            var tileShadow = TableLook.TileShadow; shadowFootprint = TableLook.ShadowFootprint;
            contactShadow = new Material(Shader.Find("YunJian/TileShadow")) { mainTexture = Resources.Load<Texture2D>("Majsoul/TableLook/" + tileShadow.texture) };
            contactShadow.SetColor("_Tint", TableLook.ColorOf(tileShadow.tint));
            shadowQuad = new Mesh { name = "Original effect_shadow plane",
                vertices = new[] { new Vector3(-.5f,0,-.5f), new Vector3(-.5f,0,.5f), new Vector3(.5f,0,.5f), new Vector3(.5f,0,-.5f) },
                uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right }, triangles = new[] { 0,1,2,0,2,3 } };
            shadowQuad.RecalculateBounds();
            frame = Material("Frame", new Color(.13f, .08f, .12f));
            red = Material("RiichiDot", new Color(.76f, .09f, .12f)); gold = Material("Selection", new Color(1, .73f, .2f));
            felt = new Material(Shader.Find("YunJian/TableCloth"));
            var texture = Resources.Load<Texture2D>("Majsoul/Table/cloth-chigua");
            if (texture != null) { felt.mainTexture = texture; felt.mainTextureScale = new Vector2(-1, -1); felt.mainTextureOffset = Vector2.one; }
            var baseRoot = new GameObject("Table").transform;
            // Extend the surface beyond the camera, like a seated player's view.
            // The original texture was forced onto a rectangle; keep its source aspect.
            Cube("Wood frame", baseRoot, new Vector3(0, -.22f, 0), new Vector3(15.8f, .3f, 15.8f), frame);
            float clothAspect = texture != null ? texture.width / (float)texture.height : 1;
            Cube("Cloth", baseRoot, new Vector3(0, -.045f, 0), new Vector3(15.8f, .09f, 15.8f / clothAspect), felt);
            font = Resources.Load<Font>("Majsoul/Font/game") ?? Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "sans-serif" }, 32);
            content = new GameObject("PublicTableState").transform;
        }
        Material Material(string name, Color color)
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader) { name = name, color = color };
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", .18f);
            return mat;
        }
        GameObject Cube(string name, Transform parent, Vector3 position, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
        public Texture2D TileTexture(string tile) => Resources.Load<Texture2D>("Majsoul/TableTiles/" + tile) ?? Resources.Load<Texture2D>("Majsoul/Tiles/" + tile);
        public Texture2D Portrait(PlayerView p)
        {
            if (!string.IsNullOrEmpty(p.avatar))
            {
                if (portraits.TryGetValue(p.avatar, out var cached)) return cached;
                try
                {
                    if (!p.avatar.StartsWith("data:image/jpeg;base64,") || p.avatar.Length > 12288) throw new Exception();
                    var t = new Texture2D(2, 2); if (!t.LoadImage(Convert.FromBase64String(p.avatar.Substring(p.avatar.IndexOf(',') + 1)))) throw new Exception();
                    portraits[p.avatar] = t; return t;
                }
                catch { /* Keep default avatar for malformed saved data. */ }
            }
            return Resources.Load<Texture2D>("Majsoul/Portraits/" + (p.bot ? "yiji" : "erjietang"));
        }
        public void Clear(bool resetOpening=true,bool keepHand=false)
        {
            if(resetOpening)opening.Reset();
            foreach (Transform t in content) { if(keepHand && t==cameraHand)continue;t.gameObject.SetActive(false); Destroy(t.gameObject); }
            exposedTiles.Clear();exposedGroups.Clear();selectedTile=null;if(!keepHand){handTiles.Clear();handBase.Clear();cameraHand=null;}drawTiles.Clear(); riverTiles.Clear(); stickAnchors.Clear(); meldAnchors.Clear(); northTiles.Clear(); winningTiles.Clear(); responseTarget = null; selfMelds = null; state = null;
        }
        public int RelativeSeat(int i) => (i - state.you + state.n) % state.n;
        float Angle(int i)
        {
            int d = RelativeSeat(i);
            return d == 0 ? 0 : d == 1 ? (state.n == 3 ? 180 : -90) : d == state.n - 1 ? 90 : 180;
        }
        public float SeatAngle(int i) => Angle(i);
        public Transform CreateEffectTile(string tile) => original.CreateTable(null,tile,Vector3.zero,.33f,false,false,1);
        // Original effect_chipeng ('ci' particle, effect_fulu/bang) is an additive flash;
        // the shader's _Tint scales it to nothing for the fade-out. See
        // Actions/meld-effect-provenance.json.
        Mesh impactMesh;
        public Transform CreateMeldImpact()
        {
            if (impactMesh == null)
            {
                impactMesh = new Mesh { name = "Meld impact quad",
                    vertices = new[] { new Vector3(-.5f,-.5f,0), new Vector3(-.5f,.5f,0), new Vector3(.5f,.5f,0), new Vector3(.5f,-.5f,0) },
                    uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right },
                    triangles = new[] { 0,1,2,0,2,3 } };
                impactMesh.RecalculateBounds();
            }
            var root = new GameObject("Meld impact").transform;
            root.gameObject.AddComponent<MeshFilter>().sharedMesh = impactMesh;
            var renderer = root.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(Shader.Find("YunJian/ResponseGlow")) { mainTexture = Resources.Load<Texture2D>("Majsoul/Actions/meld-bang") };
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return root;
        }
        // The carried stick mirrors the deposited one so the hand-off is seamless.
        public Transform CreateEffectStick()
        {
            var root = new GameObject("Carried riichi stick").transform;
            Cube("Riichi 1000", root, Vector3.zero, new Vector3(.74f, .045f, .075f), ivory);
            Cube("Red dot", root, new Vector3(0, .024f, 0), new Vector3(.045f, .004f, .045f), red);
            return root;
        }
        public Transform FindDiscard(int seat, string tile, int riverIndex=0)
        {
            // New hosts identify the exact committed river slot (one-based), including
            // multiple discards queued between two polls. Called tiles never qualify.
            if(state==null || seat<0 || seat>=state.players.Length)return null;
            if(riverIndex>0){
                var history=state.players[seat].river;int raw=riverIndex-1;
                if(raw>=history.Length || history[raw].called || history[raw].tile!=tile)return null;
                int visible=history.Take(raw).Count(t=>!t.called);
                return riverTiles.TryGetValue(seat,out var rendered) && visible<rendered.Count?rendered[visible]:null;
            }
            // Legacy hosts can only identify the current last discard.
            if(state.last==null || state.last.from!=seat || state.last.tile!=tile) return null;
            var river=state.players[seat].river;
            if(river.Length==0 || river[river.Length-1].called || river[river.Length-1].tile!=tile) return null;
            if (!riverTiles.TryGetValue(seat,out var tiles)) return null;
            return tiles.LastOrDefault(t=>t!=null && t.name.EndsWith(" "+tile,StringComparison.Ordinal));
        }
        public Vector3 DiscardPoint(int seat,string tile,int riverIndex=0)
        {
            Transform target=riverIndex>0?FindDiscard(seat,tile,riverIndex):null;
            if(riverIndex==0 && riverTiles.TryGetValue(seat,out var tiles))target=tiles.LastOrDefault(t=>t!=null && t.name.EndsWith(" "+tile,StringComparison.Ordinal));
            return target!=null?target.position:Quaternion.Euler(0,Angle(seat),0)*new Vector3(-.895f,0,-1.48f);
        }
        public Transform ResponseTarget => responseTarget;
#if UNITY_EDITOR
        public bool ValidationLegacyMeldFit;
        // 0 = playable, 1 = dimmed by the riichi declaration filter.
        public float ValidationHandDim(int index)
        {
            if (index < 0 || index >= handTiles.Count) return -1f;
            var renderer = handTiles[index].GetComponentInChildren<MeshRenderer>();
            if (renderer == null) return -1f;
            var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
            return block.GetFloat("_Dim");
        }
        public Vector3 ValidationHandPosition(int index)=>handTiles[index].localPosition;
        public Vector3 ValidationHandBase(int index)=>handBase[index];
        public float ValidationHandOpacity(int index){var b=new MaterialPropertyBlock();handTiles[index].GetComponent<MeshRenderer>().GetPropertyBlock(b);return b.GetFloat("_Opacity");}
        public int ValidationHandCount => handTiles.Count;
        public int ValidationMatchCount => exposedTiles.Count(x=>OriginalTiles.IsMatchedModel(x.Item1));
        public bool ValidationMatchesOnlyFaces => exposedTiles.Where(x=>OriginalTiles.IsMatchedModel(x.Item1)).All(x=>x.Item1.Find("Original tile").localRotation==Quaternion.identity);
#endif
        // Room.view() returns options for this client alone, and the engine creates
        // them only when a call is legal; a lone 'pass' is not a call.
        bool SelfCanCall => state.options != null && state.options.Any(o => o.kind != "pass");
        // The deposited riichi stick and the newest meld are the anchors that the hand
        // animations carry their object to.
        public Transform StickTarget(int seat) => stickAnchors.TryGetValue(seat, out var stick) ? stick : null;
        public Vector3 StickPoint(int seat) { var stick = StickTarget(seat); return stick != null ? stick.position : Quaternion.Euler(0, Angle(seat), 0) * new Vector3(0, .105f, -.833f); }
        // A committed nuki event carries a one-based extraction count. Older hosts
        // omit it, in which case their newest North remains a compatible target.
        public Transform NorthTarget(int seat, int extraction = 0)
        {
            if (!northTiles.TryGetValue(seat, out var tiles) || tiles.Count == 0) return null;
            int index = extraction > 0 ? extraction - 1 : tiles.Count - 1;
            return index >= 0 && index < tiles.Count ? tiles[index] : null;
        }
        public Vector3 NorthPoint(int seat, int extraction = 0)
        {
            var tile = NorthTarget(seat, extraction);
            return tile != null ? tile.position : Quaternion.Euler(0, Angle(seat), 0) * new Vector3(-2.95f, .02f, -3.10f);
        }
        public Transform MeldTarget(int seat) => meldAnchors.TryGetValue(seat, out var meld) ? meld : null;
        public Vector3 MeldPoint(int seat) { var meld = MeldTarget(seat); return meld != null ? meld.position : Quaternion.Euler(0, Angle(seat), 0) * new Vector3(0, .02f, -4.15f); }
        public static RiverTile[] VisibleRiver(PlayerView player) => player.river.Where(r=>!r.called).ToArray();
        Transform Seat(int i)
        {
            var t = new GameObject("Seat " + i).transform; t.SetParent(content, false); t.localRotation = Quaternion.Euler(0, Angle(i), 0); return t;
        }
        public Vector3 SeatAnchor(int i)
        {
            float angle = Angle(i);
            var local = i == state.you ? new Vector3(-3.8f, .15f, -2.8f) : new Vector3(-2.9f, .15f, -3.7f);
            return Quaternion.Euler(0, angle, 0) * local;
        }
        void LateUpdate() { opening.Apply(handTiles,handBase,GetComponent<GameAudio>());UpdateDrawReveal();ApplyDeferredDraw();FitCamera(Screen.width / (float)Screen.height);RefreshDoraPreviews(); }
        public bool DrawDeferred {get;private set;}
        void ApplyDeferredDraw(){
            if(state?.phase=="result")return;
            DrawDeferred=state!=null && state.phase=="play" && state.turn==state.you && !string.IsNullOrEmpty(state.drawn) && !opening.Active && GetComponent<TableEffects>().HasPendingActions;
            if(opening.Active || state==null || string.IsNullOrEmpty(state.drawn) || handTiles.Count==0)return;
            var tile=handTiles[handTiles.Count-1];var renderer=tile.GetComponent<MeshRenderer>();var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetFloat("_Opacity",DrawDeferred?0:1);renderer.SetPropertyBlock(block);
            foreach(var collider in tile.GetComponentsInChildren<Collider>())collider.enabled=!DrawDeferred && tile.GetComponent<TileTarget>()!=null;
        }
        public void FitCamera(float aspect)
        {
            // Preserve vertical composition on phones; fit the side hands on 16:9.
            Camera.fieldOfView = 2 * Mathf.Atan(Mathf.Tan(28 * Mathf.Deg2Rad / 2) * Mathf.Max(1, 1.9f / aspect)) * Mathf.Rad2Deg;
            // Without a foreground hand no group is moved, so the native positions apply.
            if (cameraHand == null) { ScaleNorthToMelds(); return; }
            Rect safe = Screen.safeArea;
            float safeWidth = safe.width / Screen.width, safeHeight = safe.height / Screen.height;
            float handSpan = Mathf.Max(1, handTiles.Count) * OriginalTiles.HandWidth + .4f;
            // Source hand camera: orthographic size 9, 2.54-high quad, x scale .62.
            HandCamera.orthographicSize = 9;
            float scale = Mathf.Min(safeHeight, 18 * aspect * safeWidth * .9f / handSpan);
            float centerX = safe.center.x;
            // Keep the native quad height. Public edge groups yield space when
            // their silhouettes would otherwise force the foreground hand to shrink.
            var restored=new List<Tuple<Transform,Vector3>>();
            if(meldGroups.TryGetValue(state.you,out var ownGroups))foreach(var meld in ownGroups){restored.Add(Tuple.Create(meld.group,meld.group.localPosition));meld.group.localPosition=Vector3.zero;if(meld.extra!=null){restored.Add(Tuple.Create(meld.extra,meld.extra.localPosition));meld.extra.localPosition=meld.extraRest;}}
            foreach(var group in exposedGroups){group.localPosition=Vector3.zero;group.localScale=Vector3.one;}
            float meldBottom=.85f;
#if UNITY_EDITOR
            if(ValidationLegacyMeldFit)meldBottom=.32f;
#endif
            AlignSelfMelds(safe.yMin+meldBottom*scale/18*Screen.height,safe.xMax-64*Screen.height/600f);
            ScaleNorthToMelds();
            float left=safe.xMin+safe.width*.05f,right=safe.xMax-safe.width*.05f;
            float handTop=safe.yMin+(OriginalTiles.HandHeight+.42f)*scale/18*Screen.height;
            float margin=10*Screen.height/780f;
            float half=handSpan*scale/(18*aspect)*Screen.width*.5f;
            foreach(var group in exposedGroups)
            {
                if(!ProjectedBounds(group,out var bounds) || bounds.yMin>handTop || bounds.yMax<safe.yMin)continue;
                if(group==selfMelds || bounds.center.x>=safe.center.x)right=Mathf.Min(right,bounds.xMin-margin);
                else left=Mathf.Max(left,bounds.xMax+margin);
            }
            bool handFits=right-left>=half*2;
            if(handFits)centerX=Mathf.Clamp(centerX,left+half,right-half);
            else {centerX=Mathf.Max(safe.xMin+half+margin,right-half);}
            var handBand=Rect.MinMaxRect(centerX-half,safe.yMin,centerX+half,handTop);
            var hud=TableViewport.Fit(Screen.width,Screen.height,safe,true);
            float avatarLeft=hud.Origin.x+(hud.Width*.26f-3)*hud.Scale,avatarRight=avatarLeft+68*hud.Scale;
            float avatarTop=Screen.height-(hud.Origin.y+(hud.Height*.83f-107)*hud.Scale);
            foreach(var group in exposedGroups){
                if((group!=selfMelds || !handFits) && ProjectedBounds(group,out var bounds) && handBand.Overlaps(bounds)){
                    float clearance=handTop+margin;
                    if(group==selfMelds && bounds.xMin<avatarRight && bounds.xMax>avatarLeft)clearance=Mathf.Max(clearance,avatarTop+margin);
                    MoveAboveHand(group,clearance);
                }
            }
            float x = ((centerX / Screen.width) - .5f) * 18 * aspect;
            float bottom = -9 + safe.y / Screen.height * 18;
            cameraHand.position = new Vector3(x, bottom + (OriginalTiles.HandHeight / 2 + .12f) * scale, 0);
            cameraHand.rotation = Quaternion.identity;
            cameraHand.localScale = Vector3.one * scale;
            foreach(var saved in restored)if(saved.Item1!=null)saved.Item1.localPosition=saved.Item2;
        }
        public const float MeldWidth=.35f;
#if UNITY_EDITOR
        public float ValidationMeldFaceWidth(int seat)=>Mathf.Min(MeldFaceWidth(state.you),MeldFaceWidth(seat));        public float ValidationFaceWidth(Transform tile)=>FaceWidth(tile);
        public Rect ValidationTileOutline(Transform tile)=>ProjectedTileOutline(tile);
#endif
        // Native meld corner (DrawMelds' first group) in seat space, before any screen fitting.
        Vector3 NativeMeldPoint(int seat){float angle=Angle(seat);float cursor=angle<-45?3.50f:4.20f;float z=Mathf.Abs(angle)>135?-3.50f:seat==state.you?-4.10f:-4.20f;return Quaternion.Euler(0,angle,0)*new Vector3(cursor-.55f,.02f,z);}
        // Apparent size of a tile's top face: square root of its projected area, so screen
        // position, perspective skew and the seat's rotation do not bias the comparison.
        float FaceWidth(Vector3 foot,Quaternion rotation,float width)
        {
            float top=original.TableDepth*width,length=original.TableLength*width;
            var p=new Vector2[4];int i=0;foreach(var c in new[]{new Vector2(-1,-1),new Vector2(1,-1),new Vector2(1,1),new Vector2(-1,1)})p[i++]=Camera.WorldToScreenPoint(foot+rotation*new Vector3(c.x*width/2,top,c.y*length/2));
            float area=0;for(int k=0;k<4;k++){var a=p[k];var b=p[(k+1)%4];area+=a.x*b.y-b.x*a.y;}
            return Mathf.Sqrt(Mathf.Abs(area)/2);
        }
        float FaceWidth(Transform tile)=>FaceWidth(tile.position,tile.rotation,MeldWidth*tile.lossyScale.x);
        // That seat's displayed upright meld tile, or the native meld corner without melds.
        float MeldFaceWidth(int seat)
        {
            if(meldGroups.TryGetValue(seat,out var groups)){
                var tile=groups.SelectMany(g=>g.group.GetComponentsInChildren<BoxCollider>(true)).Select(c=>c.transform).Where(t=>t.gameObject.activeInHierarchy)
                    .OrderBy(t=>Mathf.Abs(Mathf.DeltaAngle(t.localRotation.eulerAngles.y,0))).FirstOrDefault();
                if(tile!=null)return FaceWidth(tile);
            }
            return FaceWidth(NativeMeldPoint(seat),Quaternion.Euler(0,Angle(seat),0),MeldWidth);
        }
        // 1012-2 review: North tiles read larger than the player's melds (most of all the
        // left seat's, right in front of the camera). Shrink every North so its face is no
        // larger than the player's own meld faces, or than that seat's melds if smaller.
        void ScaleNorthToMelds()
        {
            if(state==null)return;float player=MeldFaceWidth(state.you);
            foreach(var entry in northTiles){
                if(entry.Value.Count==0)continue;float reference=Mathf.Min(player,MeldFaceWidth(entry.Key));
                foreach(var tile in entry.Value){if(tile==null)continue;tile.localScale=Vector3.one;float own=FaceWidth(tile);tile.localScale=Vector3.one*(own>1?Mathf.Clamp(reference/own,.6f,1f):1);}
            }
        }
        void AlignSelfMelds(float bottom,float right){
            if(selfMelds==null || selfMelds.GetComponentsInChildren<BoxCollider>(true).Length==0)return;
            var first=selfMelds.GetComponentsInChildren<BoxCollider>(true).OrderBy(c=>Mathf.Abs(c.transform.localRotation.eulerAngles.y)).First();
            // The collider's rectangular footprint overestimates the visible rounded
            // tile under perspective. Fit the actual APK mesh/outline vertices instead.
            // 1012-2 reference, normalized to the game viewport: an upright meld tile
            // white tile is 55-58 px wide in a 1280x720 viewport. Including its
            // model rim/outline, use .98 of the original foreground quad width.
            float width=OriginalTiles.HandWidth/18*Screen.height*.98f;
#if UNITY_EDITOR
            if(ValidationLegacyMeldFit)width=OriginalTiles.HandWidth/18*Screen.height*.76f;
#endif
            for(int pass=0;pass<6;pass++){
                var tile=ProjectedTileOutline(first.transform);if(tile.width<1)return;
#if UNITY_EDITOR
                if(ValidationLegacyMeldFit)ProjectedBounds(first.transform,out tile);
#endif
                float factor=Mathf.Clamp(width/tile.width,.65f,1.4f);selfMelds.localScale*=factor;
                if(!ProjectedBounds(selfMelds,out var bounds))return;
                TranslateOnCloth(selfMelds,new Vector2(right-bounds.xMax,bottom-bounds.yMin));
            }
        }
        Rect ProjectedTileOutline(Transform tile){
            Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);bool any=false;
            var model=tile.Find("Original tile");if(model==null)return new Rect();
            var projection=Camera.projectionMatrix*Camera.worldToCameraMatrix;var viewport=Camera.pixelRect;
            foreach(var filter in model.GetComponentsInChildren<MeshFilter>(true)){
                if(filter.name!="Original tile" && filter.name!="Original tile outline")continue;
                var mesh=filter.sharedMesh;if(mesh==null)continue;
                if(!tileVertices.TryGetValue(mesh,out var vertices))tileVertices[mesh]=vertices=mesh.vertices;
                var matrix=projection*filter.transform.localToWorldMatrix;
                foreach(var vertex in vertices){var clip=matrix*new Vector4(vertex.x,vertex.y,vertex.z,1);if(clip.w<=0)continue;var p=new Vector2(viewport.x+(clip.x/clip.w*.5f+.5f)*viewport.width,viewport.y+(clip.y/clip.w*.5f+.5f)*viewport.height);min=Vector2.Min(min,p);max=Vector2.Max(max,p);any=true;}
            }
            return any?Rect.MinMaxRect(min.x,min.y,max.x,max.y):new Rect();
        }
        void TranslateOnCloth(Transform group,Vector2 pixels){
            float lowest=float.MaxValue;Vector3 point=Vector3.zero,screen=Vector3.zero;
            foreach(var box in group.GetComponentsInChildren<BoxCollider>(true))for(int corner=0;corner<8;corner++){
                var offset=Vector3.Scale(box.size*.5f,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));var world=box.transform.TransformPoint(box.center+offset);var p=Camera.WorldToScreenPoint(world);if(p.z>0&&p.y<lowest){lowest=p.y;point=world;screen=p;}
            }
            if(lowest==float.MaxValue)return;var ray=Camera.ScreenPointToRay(new Vector3(screen.x+pixels.x,screen.y+pixels.y,0));if(new Plane(Vector3.up,point).Raycast(ray,out float distance)){var delta=ray.GetPoint(distance)-point;delta.y=0;group.position+=delta;}
        }
        void MoveAboveHand(Transform group,float bottom)
        {
            // Translate only along the cloth plane; no rescaling or orientation change.
            // The lowest projected corner gives an exact ray/plane target at each size.
            for(int pass=0;pass<4;pass++)
            {
                float lowest=float.MaxValue;Vector3 point=Vector3.zero,screen=Vector3.zero;
                foreach(var box in group.GetComponentsInChildren<BoxCollider>(true))for(int corner=0;corner<8;corner++)
                {
                    var offset=Vector3.Scale(box.size*.5f,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                    var world=box.transform.TransformPoint(box.center+offset);var p=Camera.WorldToScreenPoint(world);
                    if(p.z>0 && p.y<lowest){lowest=p.y;point=world;screen=p;}
                }
                if(lowest>=bottom || lowest==float.MaxValue)return;
                var ray=Camera.ScreenPointToRay(new Vector3(screen.x,bottom,0));
                if(!new Plane(Vector3.up,point).Raycast(ray,out float distance))return;
                var delta=ray.GetPoint(distance)-point;delta.y=0;group.position+=delta;
            }
        }
        bool ProjectedBounds(Transform group,out Rect bounds)
        {
            Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);bool any=false;
            foreach(var box in group.GetComponentsInChildren<BoxCollider>(true))
            for(int corner=0;corner<8;corner++)
            {
                var offset=Vector3.Scale(box.size*.5f,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                var p=Camera.WorldToScreenPoint(box.transform.TransformPoint(box.center+offset));
                if(p.z<=0)continue;any=true;min=Vector2.Min(min,p);max=Vector2.Max(max,p);
            }
            bounds=any?Rect.MinMaxRect(min.x,min.y,max.x,max.y):new Rect();return any;
        }
        Transform Tile(Transform parent, string tile, Vector3 position, float w = .28f, float h = .42f, bool hidden = false, bool sideways = false, float opacity = 1)
        {
            var root = original.CreateTable(parent, tile, position, w, hidden, sideways, opacity);
            if(!hidden){exposedTiles.Add(Tuple.Create(root,tile));if(IsDora(tile))OriginalTiles.AddShine(root,false);}
            ContactShadow(root, .002f - position.y, w, w * original.TableLength);
            return root;
        }
        // Original mjp_new/effect_shadow: soft shadow.png centred under the tile footprint.
        void ContactShadow(Transform parent, float y, float width, float length)
        {
            var shadow = new GameObject("Contact shadow"); shadow.transform.SetParent(parent, false);
            shadow.transform.localPosition = new Vector3(0, y, 0);
            shadow.transform.localScale = new Vector3(width * shadowFootprint.x, 1, length * shadowFootprint.y);
            shadow.AddComponent<MeshFilter>().sharedMesh = shadowQuad;
            var sr = shadow.AddComponent<MeshRenderer>(); sr.sharedMaterial = contactShadow; sr.shadowCastingMode = ShadowCastingMode.Off; sr.receiveShadows = false;
        }
        void Text(Transform parent, string text, Vector3 position, float size, Color color, float angle = 0)
        {
            var go = new GameObject(text); go.transform.SetParent(parent, false); go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(90, angle, 0);
            var tm = go.AddComponent<TextMesh>(); tm.text = text; tm.font = font; tm.fontSize = 64; tm.characterSize = size / 400;
            tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = color;
            go.GetComponent<Renderer>().sharedMaterial = font.material;
        }
        public void Render(RoomView view)
        {
            bool keepHand=view.phase=="result" && view.result!=null && !view.result.draw && cameraHand!=null && handTiles.Count>0;
            Clear(false,keepHand);state=view;opening.Observe(view,GetComponent<RiichiClient>()?.IntroRemaining??0);
            bool exhausted=state.phase=="result" && state.result!=null && state.result.draw && !state.result.abort && (state.result.drawDetails?.Length??0)>0;
            bool terminals=state.result!=null && state.result.abort && (state.result.abortHand?.tiles?.Length??0)>0;
            if(exhausted || terminals || state.phase=="result" && (state.result?.details?.Length??0)>0){string next=state.audioRoom+":"+state.round+":"+state.honba+":"+state.result.title;
                if(next!=drawKey){drawKey=next;drawBegan=-1;}
            }else drawKey=null;
            if (state.phase != "play" && state.phase != "reaction" && state.phase != "result") return;
            var hub = scoreHub.Create(content, state);
            for (int i = 0; i < state.players.Length; i++)
            {
                var p = state.players[i]; var seat = Seat(i);
                scoreHub.Seat(hub, p, Angle(i), state.turn == i);
                DrawRiver(seat, p, i);
                bool deposited = p.riichi > 0 && !(state.pending != null && state.pending.riichi && state.pending.from == i);
                if (deposited)
                {
                    // Grouped per seat so a placement animation can hide and reveal it.
                    var stick = new GameObject("Riichi stick seat " + i).transform; stick.SetParent(seat, false);
                    // The plate's lower recessed slot holds the deposited stick; it has to sit
                    // above the plate surface (hub root y = .092) instead of on the table.
                    stick.localPosition = new Vector3(0, .105f, -.833f);
                    Cube("Riichi 1000", stick, Vector3.zero, new Vector3(.74f, .045f, .075f), ivory);
                    Cube("Red dot", stick, new Vector3(0, .024f, 0), new Vector3(.045f, .004f, .045f), red);
                    stickAnchors[i] = stick;
                }
                DrawMelds(seat, p);
                var shown=state.phase=="result"?state.result?.details?.FirstOrDefault(d=>d.seat==i && d.tiles!=null):null;
                if(exhausted){
                    var d=state.result.drawDetails.FirstOrDefault(x=>x.seat==i);
                    var hand=new GameObject("Draw hand seat "+i).transform;hand.SetParent(seat,false);
                    int count=d!=null && d.tenpai?d.tiles.Length:p.count;float pitch=.31f;
                    float edge=i==state.you?3.08f:Mathf.Abs(Mathf.Sin(Angle(i)*Mathf.Deg2Rad))>.5f?4f:3.35f;
                    for(int j=0;j<count;j++){
                        var tile=Tile(hand,d!=null&&d.tenpai?d.tiles[j]:"z5",new Vector3((j-(count-1)*.5f)*pitch,.02f,-edge),.29f,.42f,d==null||!d.tenpai);
                        drawTiles.Add(Tuple.Create(tile,tile.localPosition));
                    }
                }
                else if(state.phase=="result" && terminals && state.result.abortHand.seat==i){
                    var reveal=new GameObject("Nine terminals hand seat "+i).transform;reveal.SetParent(seat,false);
                    var tiles=state.result.abortHand.tiles;bool self=i==state.you;float pitch=self?.37f:.31f,width=self?.35f:.29f;
                    float edge=self?4.10f:Mathf.Abs(Mathf.Sin(Angle(i)*Mathf.Deg2Rad))>.5f?4f:3.35f;
                    for(int j=0;j<tiles.Length;j++){
                        var tile=Tile(reveal,tiles[j],new Vector3((j-(tiles.Length-1)*.5f)*pitch,.02f,-edge),width);
                        drawTiles.Add(Tuple.Create(tile,tile.localPosition));
                    }
                }
                else if(shown!=null){
                    var revealed=new GameObject("Revealed winning hand").transform;revealed.SetParent(seat,false);
                    float pitch=.31f;float edge=Mathf.Abs(Mathf.Sin(Angle(i)*Mathf.Deg2Rad))>.5f?4.0f:2.95f;
                    if(i==state.you){pitch=.37f;edge=4.05f;}
                    for(int j=0;j<shown.tiles.Length;j++){
                        var tile=Tile(revealed,shown.tiles[j],new Vector3((j-(shown.tiles.Length-1)*.5f)*pitch,.02f,-edge),i==state.you?.35f:.29f);
                        drawTiles.Add(Tuple.Create(tile,tile.localPosition));
                    }
                }
                else if (i == state.you) {if(!keepHand)DrawHand();}
                else for (int j = 0; j < p.count - (SelfDrawWinner(i) != null ? 1 : 0); j++)
                {
                    float edge = Mathf.Abs(Mathf.Sin(Angle(i)*Mathf.Deg2Rad))>.5f ? 4.34f : 3.35f;
                    var tile = original.CreateStandingBack(seat, new Vector3(-1.86f + j*.31f, 0, -edge), .29f);
                    ContactShadow(tile, .002f, .29f, original.TableDepth * .29f);
                }
            }
            DrawWinningTiles();
            SelectHand(Client.SelectedIndex);
        }
        WinDetail SelfDrawWinner(int seat) => state.phase=="result" ? state.result?.details?.FirstOrDefault(d=>d.seat==seat && !d.ron) : null;
        public Transform WinningTarget(PublicSound item)
        {
            if(item.kind=="ron" && (string.IsNullOrEmpty(item.winSource) || item.winSource=="discard"))return FindDiscard(item.from,item.tile);
            return winningTiles.TryGetValue(item.seat,out var tile)?tile:null;
        }
        public Vector3 WinningPoint(PublicSound item)
        {
            var tile=WinningTarget(item);if(tile!=null)return tile.position;
            int seat=item.kind=="ron"?item.from:item.seat;
            return item.kind=="ron"?DiscardPoint(seat,item.tile):SelfDrawPoint(seat);
        }
        Vector3 SelfDrawPoint(int seat){
            var detail=SelfDrawWinner(seat);int count=detail?.tiles?.Length??13;
            float pitch=seat==state.you?.37f:.31f,width=seat==state.you?.35f:.29f;
            float edge=seat==state.you?4.05f:Mathf.Abs(Mathf.Sin(Angle(seat)*Mathf.Deg2Rad))>.5f?4f:2.95f;
            // One tile past the right end of the exposed hand, on the same seat axis.
            return Quaternion.Euler(0,Angle(seat),0)*new Vector3((count-1)*pitch/2+width+.16f,.02f,-edge);
        }
        void DrawWinningTiles()
        {
            if(state.phase!="result" || state.result?.details==null)return;
            var robbed=new Dictionary<string,Transform>();
            foreach(var d in state.result.details)
            {
                if(string.IsNullOrEmpty(d.tile) || d.seat<0 || d.seat>=state.n)continue;
                if(d.ron && (string.IsNullOrEmpty(d.winSource) || d.winSource=="discard"))continue;
                string key=d.from+":"+d.winSource+":"+d.tile;
                if(d.ron && robbed.TryGetValue(key,out var shared)){winningTiles[d.seat]=shared;continue;}
                int seat=d.ron?d.from:d.seat;
                Vector3 point=d.ron ? (d.winSource=="nuki"?NorthPoint(seat)+Quaternion.Euler(0,Angle(seat),0)*new Vector3(0,0,.4f):MeldPoint(seat)+Vector3.up*.16f) : SelfDrawPoint(seat);
                var tile=original.CreateTable(content,d.tile,point,d.ron?.33f:seat==state.you?.35f:.29f,false,false,1);
                tile.name="Winning tile "+d.seat+" "+d.tile;tile.rotation=Quaternion.Euler(0,Angle(seat),0);winningTiles[d.seat]=tile;
                if(d.ron)robbed[key]=tile;
            }
        }
        // Stable per-discard offset (x, z) and yaw in degrees. 0.5.26 (.005/.012/1.2) read as
        // too orderly and 0.5.28 (.010/.014/2.0) as too messy; this is ~70% of the latter.
        public static Vector3 RiverScatter(int seat,int rawIndex,int round,int honba){
            uint seed=unchecked((uint)((seat+1)*1973+(rawIndex+1)*9277+round*7919+honba*313));
            float next(){seed=1664525u*seed+1013904223u;return (seed>>8)/16777215f*2-1;}
            return new Vector3(next()*.007f,next()*.010f,next()*1.4f);
        }
        void DrawRiver(Transform parent, PlayerView p, int seatIndex)
        {
            var visible = VisibleRiver(p); var rendered = new List<Transform>();riverTiles[seatIndex]=rendered;
            for (int start = 0; start < visible.Length; start += 6)
            {
                var row = visible.Skip(start).Take(6).ToArray();
                float cursor = -1.06f;
                for (int j = 0; j < row.Length; j++)
                {
                    var r = row[j]; float width = r.riichi ? .44f : .33f;
                    int raw=Array.IndexOf(p.river,r);var scatter=RiverScatter(seatIndex,raw,state.round,state.honba);
                    var tile=Tile(parent, r.tile, new Vector3(cursor + width / 2+scatter.x, 0, -1.48f - start / 6 * .46f+scatter.y), .33f, .44f, false, r.riichi);
                    tile.localRotation=Quaternion.Euler(0,(r.riichi?90:0)+scatter.z,0);
                    tile.name="River "+(start+j)+" "+r.tile;rendered.Add(tile);
                    // The marker belongs to the seat that can actually call, not to
                    // every discard while any seat still has a pending decision.
                    if(SelfCanCall && state.phase=="reaction" && state.pending!=null && state.pending.kind=="discard" && state.pending.from==seatIndex && start+j==visible.Length-1)
                    {
                        responseTarget=tile;
                        original.CreateResponseGlow(tile, .33f);
                    }
                    cursor += width + .015f;
                }
            }
        }
        public static System.Collections.Generic.List<string> DisplayHand(RoomView view)
        {
            var hand=view.hand.ToList();
            if(!string.IsNullOrEmpty(view.drawn)){int at=hand.LastIndexOf(view.drawn);if(at>=0){hand.RemoveAt(at);hand.Add(view.drawn);}}
            return hand;
        }
        public string HandFace(int index)=>index>=0 && index<handTiles.Count?handTiles[index].name.Substring(5):null;
        void DrawHand()
        {
            cameraHand = new GameObject("Camera-facing hand").transform; cameraHand.SetParent(content, false);
            var hand = state.hand.ToList();
            var winner=SelfDrawWinner(state.you);if(winner!=null){int winAt=hand.LastIndexOf(winner.tile);if(winAt>=0)hand.RemoveAt(winAt);}
            if (!string.IsNullOrEmpty(state.drawn)) { int at = hand.LastIndexOf(state.drawn); if (at >= 0) { hand.RemoveAt(at); hand.Add(state.drawn); } }
            const float pitch = OriginalTiles.HandWidth;
            float width = (hand.Count - 1) * pitch + (!string.IsNullOrEmpty(state.drawn) ? .4f : 0);
            var choices = state.options.Where(x => x.kind == (Client.RiichiMode ? "riichi" : "discard")).ToArray();
            // Only a seat that actually has a discard decision dims the tiles it cannot
            // play; while another seat is acting every tile keeps its normal look.
            bool choosing = choices.Length > 0;
            for (int i = 0; i < hand.Count; i++)
            {
                bool drawn = winner==null && !string.IsNullOrEmpty(state.drawn) && i == hand.Count - 1;
                var choice = choices.FirstOrDefault(a => a.tile == hand[i] && a.id.EndsWith("_", StringComparison.Ordinal) == drawn);
                Vector3 pos = new Vector3(-width / 2 + i * pitch + (drawn ? .4f : 0), 0, 0);
                var tile = original.CreateHand(cameraHand, hand[i], pos);
                if(IsDora(hand[i]))OriginalTiles.AddShine(tile,true);
                // A presentation hand must not cast a giant floating shadow onto the table.
                foreach (var renderer in tile.GetComponentsInChildren<Renderer>()) { renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; }
                handTiles.Add(tile); handBase.Add(pos);
                if (choice != null || !choosing && (state.phase=="play" || state.phase=="reaction")) { var target = tile.gameObject.AddComponent<TileTarget>(); target.Index = i; target.Action = choice?.id; }
                else
                {
                    // A tile that cannot be discarded right now is dimmed and unpickable,
                    // so a riichi declaration only offers the tiles that keep tenpai.
                    foreach (var collider in tile.GetComponentsInChildren<Collider>()) collider.enabled = false;
                    bool special=state.players[state.you].riichi>0 && (drawn || state.options.Any(o=>(o.kind=="kan" && (o.meld??"").Skip(1).Any(d=>char.IsDigit(d)&&TileKind(o.meld[0].ToString()+d)==TileKind(hand[i]))) || (o.kind=="nuki" && hand[i]=="z4")));
                    if (choosing && !special)
                    {
                        var dim = new MaterialPropertyBlock(); dim.SetFloat("_Dim", 1f);
                        foreach (var handRenderer in tile.GetComponentsInChildren<MeshRenderer>()) handRenderer.SetPropertyBlock(dim);
                    }
                }
            }
            SelectHand(Client.SelectedIndex);
            FitCamera(Camera.aspect);
        }
        public void SelectHand(int index)
        {
            if(opening.Active){opening.Apply(handTiles,handBase,GetComponent<GameAudio>());return;}
            selectedTile=index>=0 && index<handTiles.Count?TileKind(handTiles[index].name.Substring(5)):null;
            for (int i = 0; i < handTiles.Count; i++){
                handTiles[i].localPosition = handBase[i] + (i == index ? new Vector3(0, .30f, 0) : Vector3.zero);
            }
            foreach(var entry in exposedTiles){
                OriginalTiles.MatchModel(entry.Item1,IsMatchingTile(entry.Item2));
            }
        }
        void DrawMelds(Transform parent, PlayerView p)
        {
            // Source scene_mj ming anchors: .375/.377 metres, scaled to tile width.
            int index=Array.IndexOf(state.players,p);float angle=Angle(index);
            float cursor = angle < -45 ? 3.50f : 4.20f;
            float meldZ = Mathf.Abs(angle)>135 ? -3.50f : p == state.players[state.you] ? -4.10f : -4.20f;
            Transform anchor=null;
            var root=new GameObject("Exposed melds").transform;root.SetParent(parent,false);parent=root;
            var groups=new List<MeldVisual>();meldGroups[index]=groups;
            if (index == state.you) selfMelds = root;
            exposedGroups.Add(root);
            // Read backwards so the first meld remains nearest the player's right edge.
            foreach (string meld in p.melds)
            {
                var group=new GameObject("Meld "+groups.Count).transform;group.SetParent(root,false);parent=group;
                var visual=new MeldVisual{group=group,value=meld};groups.Add(visual);
                var digits = meld.Skip(1).Where(char.IsDigit).ToList(); int marker = meld.IndexOfAny(new[] { '-', '=', '+' });
                char direction = marker >= 0 ? meld[marker] : '\0';
                int called = marker >= 0 ? meld.Substring(1, marker - 1).Count(char.IsDigit) - 1 : -1;
                bool added = marker >= 0 && marker < meld.Length - 1 && digits.Count == 4;
                var pieces = new List<Tuple<char, bool, bool>>();
                if (marker < 0)
                {
                    for (int j = 0; j < digits.Count; j++) pieces.Add(Tuple.Create(digits[j], false, j == 0 || j == 3));
                }
                else
                {
                    char claimed = digits[called]; digits.RemoveAt(called); char extra = added ? digits[digits.Count - 1] : '\0'; if (added) digits.RemoveAt(digits.Count - 1);
                    pieces.AddRange(digits.Select(d => Tuple.Create(d, false, false)));
                    int slot = direction == '-' ? 0 : direction == '=' ? 1 : pieces.Count;
                    pieces.Insert(slot, Tuple.Create(claimed, true, false));
                    float start = cursor - pieces.Sum(x => x.Item2 ? .53f : .37f);
                    float x = start;
                    foreach (var piece in pieces)
                    {
                        float w = piece.Item2 ? .53f : .37f;
                        var calledTile = Tile(parent, meld[0].ToString() + piece.Item1, new Vector3(x + w / 2, .02f, meldZ), .35f, .51f, false, piece.Item2);
                        if(visual.anchor==null || piece.Item2)visual.anchor=calledTile;
                        if (piece.Item2) anchor = calledTile;
                        if (added && piece.Item2){visual.extra=Tile(parent, meld[0].ToString() + extra, new Vector3(x + w / 2, .17f, meldZ), .35f, .51f, false, true);visual.extraRest=visual.extra.localPosition;}
                        x += w;
                    }
                    cursor = start - .16f; continue;
                }
                cursor -= pieces.Count * .37f;
                for (int j = 0; j < pieces.Count; j++)
                {
                    var concealed = Tile(parent, meld[0].ToString() + pieces[j].Item1, new Vector3(cursor + j * .37f, .02f, meldZ), .35f, .51f, pieces[j].Item3);
                    if (anchor == null) anchor = concealed;
                    if(visual.anchor==null)visual.anchor=concealed;
                }
                cursor -= .16f;
            }
            if (anchor != null) meldAnchors[index] = anchor;
            // Source babei anchor lies inward of, and separate from, the meld corner.
            var north=new GameObject("Extracted North").transform;north.SetParent(root.parent,false);
            exposedGroups.Add(north);
            bool self=index==state.you;
            var extracted = new List<Transform>(); northTiles[index] = extracted;
            for (int i = 0; i < p.nuki; i++)
            {
                var tile = Tile(north, "z4", NativeReviewLayout.NorthLocal(NativeReviewLayout.Physical(RelativeSeat(index),state.n),i,angle), MeldWidth, .51f);
                tile.localRotation = Quaternion.identity;
                extracted.Add(tile);
            }
        }
        void OnDestroy()
        {
            original?.Dispose(); scoreHub.Dispose();
            foreach (var mat in faces.Values) Destroy(mat);
            foreach (var texture in portraits.Values) Destroy(texture);
            foreach (var mat in new[] { ivory, frame, felt, red, gold, contactShadow }) if (mat != null) Destroy(mat);
            if (shadowQuad != null) Destroy(shadowQuad);
        }
    }
}
