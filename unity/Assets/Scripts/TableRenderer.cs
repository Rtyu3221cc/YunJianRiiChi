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
            Camera.cullingMask &= ~((1 << 12) | (1 << 13));
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
            handTiles.Clear(); handBase.Clear(); cameraHand = null; state = null;
        }
        public int RelativeSeat(int i) => (i - state.you + state.n) % state.n;
        float Angle(int i)
        {
            int d = RelativeSeat(i);
            return d == 0 ? 0 : d == 1 ? (state.n == 3 ? 180 : -90) : d == state.n - 1 ? 90 : 180;
        }
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
        void LateUpdate() { FitCamera(Screen.width / (float)Screen.height); }
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
            float x = ((safe.center.x / Screen.width) - .5f) * 18 * aspect;
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
            if (state.phase != "play" && state.phase != "reaction") return;
            var hub = scoreHub.Create(content, state);
            for (int i = 0; i < state.players.Length; i++)
            {
                var p = state.players[i]; var seat = Seat(i);
                scoreHub.Seat(hub, p, Angle(i), state.turn == i);
                DrawRiver(seat, p);
                bool deposited = p.riichi > 0 && !(state.pending != null && state.pending.riichi && state.pending.from == i);
                if (deposited)
                {
                    Cube("Riichi 1000", seat, new Vector3(0, .06f, -1.13f), new Vector3(.74f, .045f, .075f), ivory);
                    Cube("Red dot", seat, new Vector3(0, .084f, -1.13f), new Vector3(.045f, .004f, .045f), red);
                }
                DrawMelds(seat, p);
                if (i == state.you) DrawHand();
                else for (int j = 0; j < p.count; j++)
                {
                    var tile = original.CreateStandingBack(seat, new Vector3((j - (p.count - 1) / 2f) * .31f, 0, -3.35f), .29f);
                    var shadow = new GameObject("Standing hand contact shadow"); shadow.transform.SetParent(tile, false);
                    shadow.transform.localPosition = new Vector3(.018f, .002f, -.022f);
                    shadow.AddComponent<MeshFilter>().sharedMesh = geometry.Get(.31f, original.TableDepth * .29f + .04f, true);
                    var sr = shadow.AddComponent<MeshRenderer>(); sr.sharedMaterial = contactShadow; sr.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
        }
        void DrawRiver(Transform parent, PlayerView p)
        {
            for (int start = 0; start < p.river.Length; start += 6)
            {
                var row = p.river.Skip(start).Take(6).ToArray();
                float cursor = -1.06f;
                for (int j = 0; j < row.Length; j++)
                {
                    var r = row[j]; float width = r.riichi ? .44f : .33f;
                    Tile(parent, r.tile, new Vector3(cursor + width / 2, 0, -1.48f - start / 6 * .45f), .33f, .44f, false, r.riichi, r.called ? .3f : 1);
                    cursor += width + .015f;
                }
            }
        }
        void DrawHand()
        {
            cameraHand = new GameObject("Camera-facing hand").transform; cameraHand.SetParent(content, false);
            var hand = state.hand.ToList();
            if (!string.IsNullOrEmpty(state.drawn)) { int at = hand.LastIndexOf(state.drawn); if (at >= 0) { hand.RemoveAt(at); hand.Add(state.drawn); } }
            const float pitch = OriginalTiles.HandWidth;
            float width = (hand.Count - 1) * pitch + (!string.IsNullOrEmpty(state.drawn) ? .4f : 0);
            var choices = state.options.Where(x => x.kind == (Client.RiichiMode ? "riichi" : "discard")).ToArray();
            for (int i = 0; i < hand.Count; i++)
            {
                bool drawn = !string.IsNullOrEmpty(state.drawn) && i == hand.Count - 1;
                var choice = choices.FirstOrDefault(a => a.tile == hand[i] && a.id.EndsWith("_", StringComparison.Ordinal) == drawn);
                Vector3 pos = new Vector3(-width / 2 + i * pitch + (drawn ? .4f : 0), 0, 0);
                var tile = original.CreateHand(cameraHand, hand[i], pos);
                // A presentation hand must not cast a giant floating shadow onto the table.
                foreach (var renderer in tile.GetComponentsInChildren<Renderer>()) { renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; }
                handTiles.Add(tile); handBase.Add(pos);
                if (choice != null) { var target = tile.gameObject.AddComponent<TileTarget>(); target.Index = i; target.Action = choice.id; }
                else foreach (var collider in tile.GetComponentsInChildren<Collider>()) collider.enabled = false;
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
            float cursor = 3.6f;
            float meldZ = p == state.players[state.you] ? -2.93f : -2.75f;
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
                        Tile(parent, meld[0].ToString() + piece.Item1, new Vector3(x + w / 2, .02f, meldZ), .35f, .51f, false, piece.Item2);
                        if (added && piece.Item2) Tile(parent, meld[0].ToString() + extra, new Vector3(x + w / 2, .17f, meldZ), .35f, .51f, false, true);
                        x += w;
                    }
                    cursor = start - .16f; continue;
                }
                cursor -= pieces.Count * .37f;
                for (int j = 0; j < pieces.Count; j++) Tile(parent, meld[0].ToString() + pieces[j].Item1, new Vector3(cursor + j * .37f, .02f, meldZ), .35f, .51f, pieces[j].Item3);
                cursor -= .16f;
            }
            for (int i = 0; i < p.nuki; i++) Tile(parent, "z4", new Vector3(1.3f + i * .37f, .02f, -2.1f), .35f, .51f);
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
