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
        public Texture DoraTexture(string tile) => original.DoraTexture(tile);
        public Texture HandTexture(string tile) => original.HandTexture(tile);
        Transform content;
        RoomView state;
        readonly Dictionary<string, Material> faces = new Dictionary<string, Material>();
        readonly Dictionary<string, Texture2D> portraits = new Dictionary<string, Texture2D>();
        readonly List<Transform> handTiles = new List<Transform>();
        readonly List<Vector3> handBase = new List<Vector3>();
        Material ivory, frame, felt, red, gold, contactShadow;
        Texture2D shadowTexture;
        Font font;
        Transform cameraHand;
        Transform responseTarget;
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
            float t=Mathf.SmoothStep(0,1,(Time.unscaledTime-drawBegan)/.55f);
            foreach(var tile in drawTiles){
                tile.Item1.localRotation=Quaternion.Euler(-90*(1-t),0,0);
                tile.Item1.localPosition=tile.Item2+Vector3.up*original.TableLength*.29f*.5f*(1-t);
            }
        }
        Transform selfMelds;
        readonly Dictionary<int, List<Transform>> riverTiles = new Dictionary<int, List<Transform>>();
        readonly Dictionary<int, Transform> stickAnchors = new Dictionary<int, Transform>();
        readonly Dictionary<int, Transform> meldAnchors = new Dictionary<int, Transform>();
        readonly Dictionary<int, List<Transform>> northTiles = new Dictionary<int, List<Transform>>();
        readonly Dictionary<int, Transform> winningTiles = new Dictionary<int, Transform>();
        readonly TileGeometry geometry = new TileGeometry();

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
            Camera.cullingMask &= ~((1 << 12) | (1 << 13) | (1 << 14));
            HandCamera = new GameObject("Original orthographic hand camera").AddComponent<Camera>();
            HandCamera.transform.position = new Vector3(0, 0, -30);
            HandCamera.orthographic = true; HandCamera.orthographicSize = 9;
            HandCamera.clearFlags = CameraClearFlags.Depth; HandCamera.depth = Camera.depth + 1;
            HandCamera.cullingMask = 1 << 12; HandCamera.allowMSAA = true;
            original = new OriginalTiles();
            var lamp = new GameObject("SoftLight").AddComponent<Light>(); lamp.type = LightType.Directional;
            lamp.transform.rotation = Quaternion.Euler(65, -22, 0); lamp.intensity = .8f;
            lamp.shadows = LightShadows.Soft; lamp.shadowStrength = .6f; lamp.shadowBias = .015f;
            QualitySettings.shadowDistance = 25;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.42f, .40f, .38f);
            ivory = Material("Ivory", new Color(.90f, .89f, .85f));
            shadowTexture = new Texture2D(1, 1); shadowTexture.SetPixel(0, 0, new Color(0, 0, 0, .24f)); shadowTexture.Apply();
            contactShadow = new Material(Shader.Find("Unlit/Transparent")) { mainTexture = shadowTexture, renderQueue = 2400 };
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
        public void Clear()
        {
            foreach (Transform t in content) { t.gameObject.SetActive(false); Destroy(t.gameObject); }
            handTiles.Clear(); handBase.Clear(); drawTiles.Clear(); riverTiles.Clear(); stickAnchors.Clear(); meldAnchors.Clear(); northTiles.Clear(); winningTiles.Clear(); responseTarget = null; selfMelds = null; cameraHand = null; state = null;
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
        public Transform FindDiscard(int seat, string tile)
        {
            // A delayed event must never hide an older identical discard after a call.
            if(state.last==null || state.last.from!=seat || state.last.tile!=tile) return null;
            var river=state.players[seat].river;
            if(river.Length==0 || river[river.Length-1].called || river[river.Length-1].tile!=tile) return null;
            if (!riverTiles.TryGetValue(seat,out var tiles)) return null;
            return tiles.LastOrDefault(t=>t!=null && t.name.EndsWith(" "+tile,StringComparison.Ordinal));
        }
        public Vector3 DiscardPoint(int seat,string tile)
        {
            Transform target=null;
            if(riverTiles.TryGetValue(seat,out var tiles))target=tiles.LastOrDefault(t=>t!=null && t.name.EndsWith(" "+tile,StringComparison.Ordinal));
            return target!=null?target.position:Quaternion.Euler(0,Angle(seat),0)*new Vector3(-.895f,0,-1.48f);
        }
        public Transform ResponseTarget => responseTarget;
#if UNITY_EDITOR
        // 0 = playable, 1 = dimmed by the riichi declaration filter.
        public float ValidationHandDim(int index)
        {
            if (index < 0 || index >= handTiles.Count) return -1f;
            var renderer = handTiles[index].GetComponentInChildren<MeshRenderer>();
            if (renderer == null) return -1f;
            var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
            return block.GetFloat("_Dim");
        }
        public int ValidationHandCount => handTiles.Count;
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
            return tile != null ? tile.position : Quaternion.Euler(0, Angle(seat), 0) * new Vector3(seat == state.you ? -3.35f : 2, .02f, seat == state.you ? -3.08f : -2.70f);
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
        void LateUpdate() { UpdateDrawReveal();FitCamera(Screen.width / (float)Screen.height); }
        public void FitCamera(float aspect)
        {
            // Preserve vertical composition on phones; fit the side hands on 16:9.
            Camera.fieldOfView = 2 * Mathf.Atan(Mathf.Tan(28 * Mathf.Deg2Rad / 2) * Mathf.Max(1, 1.9f / aspect)) * Mathf.Rad2Deg;
            if (cameraHand == null) return;
            Rect safe = Screen.safeArea;
            float safeWidth = safe.width / Screen.width, safeHeight = safe.height / Screen.height;
            float handSpan = Mathf.Max(1, handTiles.Count) * OriginalTiles.HandWidth + .4f;
            // Source hand camera: orthographic size 9, 2.54-high quad, x scale .62.
            HandCamera.orthographicSize = 9;
            float scale = Mathf.Min(safeHeight, 18 * aspect * safeWidth * .9f / handSpan);
            float centerX = safe.center.x;
            if (selfMelds != null && selfMelds.childCount > 0)
            {
                // The concealed hand uses an overlay camera. Reserve the actual
                // projected meld footprint so it cannot cover exposed tiles.
                float right = safe.xMax;
                foreach (var box in selfMelds.GetComponentsInChildren<BoxCollider>())
                for (int corner = 0; corner < 8; corner++)
                {
                    var offset = Vector3.Scale(box.size * .5f, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    float px = Camera.WorldToScreenPoint(box.transform.TransformPoint(box.center + offset)).x;
                    right = Mathf.Min(right, px - 18 * Screen.height / 780f);
                }
                float left = safe.xMin + safe.width * .08f;
                scale = Mathf.Min(scale, Mathf.Max(1, right - left) * 18 * aspect / Screen.width / handSpan);
                float half = handSpan * scale / (18 * aspect) * Screen.width * .5f;
                centerX = Mathf.Clamp(safe.center.x - safe.width * .025f * Mathf.Min(2, state.players[state.you].melds.Length), left + half, right - half);
            }
            float x = ((centerX / Screen.width) - .5f) * 18 * aspect;
            float bottom = -9 + safe.y / Screen.height * 18;
            cameraHand.position = new Vector3(x, bottom + (OriginalTiles.HandHeight / 2 + .12f) * scale, 0);
            cameraHand.rotation = Quaternion.identity;
            cameraHand.localScale = Vector3.one * scale;
        }
        Transform Tile(Transform parent, string tile, Vector3 position, float w = .28f, float h = .42f, bool hidden = false, bool sideways = false, float opacity = 1)
        {
            var root = original.CreateTable(parent, tile, position, w, hidden, sideways, opacity);
            var shadow = new GameObject("Contact shadow"); shadow.transform.SetParent(root, false);
            shadow.transform.localPosition = new Vector3(.018f, .002f - position.y, -.022f);
            shadow.AddComponent<MeshFilter>().sharedMesh = geometry.Get(w + .02f, w * original.TableLength + .02f, true);
            var sr = shadow.AddComponent<MeshRenderer>(); sr.sharedMaterial = contactShadow; sr.shadowCastingMode = ShadowCastingMode.Off;
            return root;
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
            Clear(); state = view;
            bool exhausted=state.phase=="result" && state.result!=null && state.result.draw && !state.result.abort && (state.result.drawDetails?.Length??0)>0;
            if(exhausted){string next=state.audioRoom+":"+state.round+":"+state.honba+":"+state.result.title;
                if(next!=drawKey){drawKey=next;drawBegan=Time.unscaledTime;}
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
                else if(shown!=null){
                    var revealed=new GameObject("Revealed winning hand").transform;revealed.SetParent(seat,false);
                    float pitch=.31f;float edge=Mathf.Abs(Mathf.Sin(Angle(i)*Mathf.Deg2Rad))>.5f?4.0f:2.95f;
                    for(int j=0;j<shown.tiles.Length;j++)Tile(revealed,shown.tiles[j],new Vector3(-2.2f+j*pitch,.02f,-edge),.29f);
                }
                else if (i == state.you) DrawHand();
                else for (int j = 0; j < p.count - (SelfDrawWinner(i) != null ? 1 : 0); j++)
                {
                    float edge = Mathf.Abs(Mathf.Sin(Angle(i)*Mathf.Deg2Rad))>.5f ? 4.34f : 3.35f;
                    var tile = original.CreateStandingBack(seat, new Vector3(-1.86f + j*.31f, 0, -edge), .29f);
                    var shadow = new GameObject("Standing hand contact shadow"); shadow.transform.SetParent(tile, false);
                    shadow.transform.localPosition = new Vector3(.018f, .002f, -.022f);
                    shadow.AddComponent<MeshFilter>().sharedMesh = geometry.Get(.31f, original.TableDepth * .29f + .04f, true);
                    var sr = shadow.AddComponent<MeshRenderer>(); sr.sharedMaterial = contactShadow; sr.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            DrawWinningTiles();
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
            return item.kind=="ron"?DiscardPoint(seat,item.tile):Quaternion.Euler(0,Angle(seat),0)*new Vector3(1.85f,.02f,-2.95f);
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
                Vector3 point=d.ron ? (d.winSource=="nuki"?NorthPoint(seat)+Quaternion.Euler(0,Angle(seat),0)*new Vector3(0,0,.4f):MeldPoint(seat)+Vector3.up*.16f) : Quaternion.Euler(0,Angle(seat),0)*new Vector3(1.85f,.02f,-2.95f);
                var tile=original.CreateTable(content,d.tile,point,.33f,false,false,1);
                tile.name="Winning tile "+d.seat+" "+d.tile;tile.rotation=Quaternion.Euler(0,Angle(seat),0);winningTiles[d.seat]=tile;
                if(d.ron)robbed[key]=tile;
            }
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
                    var tile=Tile(parent, r.tile, new Vector3(cursor + width / 2, 0, -1.48f - start / 6 * .45f), .33f, .44f, false, r.riichi);
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
                // A presentation hand must not cast a giant floating shadow onto the table.
                foreach (var renderer in tile.GetComponentsInChildren<Renderer>()) { renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; }
                handTiles.Add(tile); handBase.Add(pos);
                if (choice != null) { var target = tile.gameObject.AddComponent<TileTarget>(); target.Index = i; target.Action = choice.id; }
                else
                {
                    // A tile that cannot be discarded right now is dimmed and unpickable,
                    // so a riichi declaration only offers the tiles that keep tenpai.
                    foreach (var collider in tile.GetComponentsInChildren<Collider>()) collider.enabled = false;
                    if (choosing)
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
            for (int i = 0; i < handTiles.Count; i++)
                handTiles[i].localPosition = handBase[i] + (i == index ? new Vector3(0, .30f, 0) : Vector3.zero);
        }
        void DrawMelds(Transform parent, PlayerView p)
        {
            // Source scene_mj ming anchors: .375/.377 metres, scaled to tile width.
            int index=Array.IndexOf(state.players,p);float angle=Angle(index);
            float cursor = angle < -45 ? 3.50f : 4.20f;
            float meldZ = Mathf.Abs(angle)>135 ? -3.50f : p == state.players[state.you] ? -4.10f : -4.20f;
            Transform anchor=null;
            var root=new GameObject("Exposed melds").transform;root.SetParent(parent,false);parent=root;
            if (index == state.you) selfMelds = root;
            // Read backwards so the first meld remains nearest the player's right edge.
            foreach (string meld in p.melds)
            {
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
                        if (piece.Item2) anchor = calledTile;
                        if (added && piece.Item2) Tile(parent, meld[0].ToString() + extra, new Vector3(x + w / 2, .17f, meldZ), .35f, .51f, false, true);
                        x += w;
                    }
                    cursor = start - .16f; continue;
                }
                cursor -= pieces.Count * .37f;
                for (int j = 0; j < pieces.Count; j++)
                {
                    var concealed = Tile(parent, meld[0].ToString() + pieces[j].Item1, new Vector3(cursor + j * .37f, .02f, meldZ), .35f, .51f, pieces[j].Item3);
                    if (anchor == null) anchor = concealed;
                }
                cursor -= .16f;
            }
            if (anchor != null) meldAnchors[index] = anchor;
            // Source babei anchor lies inward of, and separate from, the meld corner.
            var north=new GameObject("Extracted North").transform;north.SetParent(parent.parent,false);
            bool self=index==state.you;
            var extracted = new List<Transform>(); northTiles[index] = extracted;
            for (int i = 0; i < p.nuki; i++)
            {
                var tile = Tile(north, "z4", new Vector3(self ? -3.35f : 2.0f, .02f, self ? -3.08f+i*.37f : -2.70f-i*.37f), .35f, .51f);
                // Rotate the glyph toward the table, with North tiles stacked
                // along the side of each seat rather than across the corner.
                tile.localRotation = Quaternion.Euler(0, self ? 90 : -90, 0);
                extracted.Add(tile);
            }
        }
        void OnDestroy()
        {
            geometry.Dispose(); original?.Dispose(); scoreHub.Dispose();
            foreach (var mat in faces.Values) Destroy(mat);
            foreach (var texture in portraits.Values) Destroy(texture);
            foreach (var mat in new[] { ivory, frame, felt, red, gold, contactShadow }) if (mat != null) Destroy(mat);
            if (shadowTexture != null) Destroy(shadowTexture);
        }
    }
}
