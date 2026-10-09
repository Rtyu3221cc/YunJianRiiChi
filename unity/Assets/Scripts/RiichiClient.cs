using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace YunJian.Riichi
{
    public class RiichiClient : MonoBehaviour
    {
        public RoomView State { get; private set; }
        public string SelectedAction { get; private set; }
        public int SelectedIndex { get; private set; } = -1;
        public bool RiichiMode { get; private set; }
        public bool Busy { get; private set; }
        string endpoint = "http://127.0.0.1:8765", token = "", secret = "", nickname = "旅人", pin = "", recovery = "";
        string message = "", page = "home", addresses = "", hostError = "";
        int players = 3, rounds = 1, generation;
        bool active, red = true, showRoom, muted;
        float nextPoll, volume = .8f;
        TableRenderer table;
        RoleVoice voice;
        TableEffects effects;
        string choosingKind;
        GUIStyle label, title, button, field, small;
        GUIStyle caption, centered, tableButton;
        TableViewport viewport;
        readonly Dictionary<string, Texture2D> skin = new Dictionary<string, Texture2D>();
        bool showHints;
        readonly List<Rect> uiButtons = new List<Rect>();
        Font font;
        AndroidJavaClass bridge;
        bool validationSession;

#if UNITY_EDITOR
        public void BeginValidationPractice(string hostSecret)
        {
            validationSession = true; endpoint = "http://127.0.0.1:8765";
            secret = hostSecret; token = ""; nickname = "Unity运行验证";
            players = 3; generation++;
            StartCoroutine(CreateValidationPractice());
        }
        IEnumerator CreateValidationPractice()
        {
            // This endpoint is the dedicated local test host, never a phone save.
            yield return Send(new RequestBody {op="recoverHost",host=secret});
            if(State!=null && State.host)yield return Send(new RequestBody {op="close"});
            yield return CreateOrRecover(true);
        }
        public void ShowValidationFixture(RoomView fixture)
        {
            StopAllCoroutines(); Busy = false; State = fixture; active = true; page = "home";
            nextPoll = float.PositiveInfinity; SelectedIndex = -1; SelectedAction = null;
            table.Render(fixture);
            effects.Reset();choosingKind=null;
        }
#endif

        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            table = gameObject.AddComponent<TableRenderer>(); table.Client = this;
            effects = gameObject.AddComponent<TableEffects>();
            voice = gameObject.AddComponent<RoleVoice>();
            nickname = PlayerPrefs.GetString("riichi.name", "旅人");
            endpoint = PlayerPrefs.GetString("riichi.server", endpoint);
            secret = PlayerPrefs.GetString("riichi.editorHost", "");
            volume = PlayerPrefs.GetFloat("riichi.volume", .8f);
            muted = PlayerPrefs.GetInt("riichi.muted", 0) != 0;
            token = PlayerPrefs.GetString("riichi.token:" + endpoint, "");
            voice.Configure(!muted, volume);
            font = Resources.Load<Font>("Majsoul/Font/game") ?? Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "sans-serif" }, 24);
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                bridge = new AndroidJavaClass("com.yunjian.riichi.UnityHostBridge");
                using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    bridge.CallStatic("start", unity.GetStatic<AndroidJavaObject>("currentActivity"));
                StartCoroutine(WaitForHost());
            }
            catch (Exception e) { message = "手机房主服务启动失败：" + e.Message; }
#endif
        }
        IEnumerator WaitForHost()
        {
            for (int i = 0; i < 60; i++)
            {
                var config = JsonUtility.FromJson<HostConfig>(bridge.CallStatic<string>("config"));
                secret = config.host; addresses = string.Join("\n", config.addresses ?? new string[0]);
                hostError = config.error ?? "";
                if (config.ready) yield break;
                yield return new WaitForSecondsRealtime(.25f);
            }
            message = "房主服务尚未就绪。" + hostError;
        }
        public static bool ValidEndpoint(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "http" || uri.Port != 8765 || uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo)) return false;
            if (!IPAddress.TryParse(uri.Host, out var ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return false;
            var a = ip.GetAddressBytes();
            return a[0] == 127 || a[0] == 10 || (a[0] == 192 && a[1] == 168) || (a[0] == 172 && a[1] >= 16 && a[1] <= 31);
        }
        public void SelectTile(int index, string action)
        {
            if (Busy || State == null || !State.options.Any(x => x.id == action)) return;
            if (SelectedIndex == index && SelectedAction == action) { SelectedAction = null; SelectedIndex = -1; Act(action); }
            else { SelectedIndex = index; SelectedAction = action; table.SelectHand(index); }
        }
        public void Act(string id) { choosingKind=null;StartCoroutine(Send(new RequestBody { op = "action", action = id })); }
        IEnumerator Send(RequestBody req, Action success = null, bool poll = false)
        {
            if (Busy) yield break;
            if (!ValidEndpoint(endpoint)) { message = "请输入局域网 IPv4 地址，端口固定为 8765"; yield break; }
            Busy = true;
            int gen = generation;
            req.token = token; req.version = State != null ? State.version : -1;
            req.id = Guid.NewGuid().ToString("N");
            using (var web = new UnityWebRequest(endpoint + "/api", "POST"))
            {
                web.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(req)));
                web.downloadHandler = new DownloadHandlerBuffer();
                web.SetRequestHeader("Content-Type", "application/json");
                web.timeout = 16; web.redirectLimit = 0;
                yield return web.SendWebRequest();
                Busy = false;
                if (gen != generation) yield break;
                if (web.result != UnityWebRequest.Result.Success) { message = "连接中断，正在重连：" + web.error; yield break; }
                RoomReply reply;
                try { reply = JsonUtility.FromJson<RoomReply>(web.downloadHandler.text); }
                catch (Exception e) { message = "房主响应无法解析：" + e.Message; yield break; }
                if (reply == null || !reply.ok) { message = reply?.error ?? "无效响应"; yield break; }
                message = "";
                if (!string.IsNullOrEmpty(reply.token))
                {
                    token = reply.token;
                    if (!validationSession)
                    {
                        PlayerPrefs.SetString("riichi.token:" + endpoint, token);
                        PlayerPrefs.SetString("riichi.server", endpoint); PlayerPrefs.Save();
                    }
                }
                if (reply.state != null)
                {
                    bool changed = State == null || State.version != reply.state.version || State.phase != reply.state.phase;
                    State = reply.state;
                    voice.Accept(State);
                    if (State.phase == "empty" || State.phase == "join") { active = false; page = "home"; table.Clear(); }
                    else
                    {
                        active = true;
                        if (SelectedAction != null && !State.options.Any(x => x.id == SelectedAction)) { SelectedAction = null; SelectedIndex = -1; }
                        if (!State.options.Any(x => x.kind == "riichi")) RiichiMode = false;
                        if (changed) table.Render(State);
                        effects.Accept(State);
                        if(choosingKind!=null && !State.options.Any(x=>x.kind==choosingKind)) choosingKind=null;
                    }
                }
                success?.Invoke();
            }
        }
        void Update()
        {
            if (active && !Busy && Time.unscaledTime >= nextPoll) { nextPoll = Time.unscaledTime + .65f; StartCoroutine(Send(new RequestBody { op = "state" }, null, true)); }
            if (active && !Busy && Input.GetMouseButtonUp(0) && !InUI(Input.mousePosition))
            {
                Ray ray = table.HandCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, 100f))
                {
                    var tile = hit.collider.GetComponentInParent<TileTarget>();
                    if (tile != null) SelectTile(tile.Index, tile.Action);
                }
            }
        }
        bool InUI(Vector3 mouse)
        {
            float x = mouse.x, y = Screen.height - mouse.y;
            return showRoom || page == "settings" || page == "confirm-close" || uiButtons.Any(r => r.Contains(new Vector2(x, y)));
        }
        void EnterHome()
        {
            generation++; active = false; State = null; SelectedAction = null; SelectedIndex = -1; RiichiMode = false;
            page = "home"; table.Clear(); voice.Reset();effects.Reset();choosingKind=null;
        }
        void StartRoom(bool practice)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            endpoint = "http://127.0.0.1:8765";
#endif
            generation++; token = PlayerPrefs.GetString("riichi.token:" + endpoint, "");
            PlayerPrefs.SetString("riichi.name", nickname); PlayerPrefs.SetString("riichi.editorHost", secret); PlayerPrefs.Save();
            StartCoroutine(CreateOrRecover(practice));
        }
        IEnumerator CreateOrRecover(bool practice)
        {
            bool recovered = false;
            // Reuse a phone's existing room without discarding its save.
            yield return Send(new RequestBody { op = "recoverHost", host = secret }, () => recovered = true);
            if (recovered) yield break;
            yield return Send(new RequestBody { op = "create", host = secret, name = nickname, n = players, rounds = rounds, red = red, bots = practice },
                () => { if (practice) StartCoroutine(Send(new RequestBody { op = "start", fillBots = true })); });
        }
        void JoinRoom()
        {
            string address = endpoint.Trim().TrimEnd('/');
            if (!address.StartsWith("http://")) address = "http://" + address;
            if (Uri.TryCreate(address, UriKind.Absolute, out var uri) && uri.IsDefaultPort) address += ":8765";
            if (!ValidEndpoint(address)) { message = "请输入同一热点内的房主 IPv4 地址"; return; }
            endpoint = address; generation++; token = PlayerPrefs.GetString("riichi.token:" + endpoint, "");
            StartCoroutine(Send(new RequestBody { op = "join", name = nickname, pin = pin, recovery = recovery }));
        }
        void Styles()
        {
            if (label != null) return;
            label = new GUIStyle(GUI.skin.label) { font = font, fontSize = 22, wordWrap = true };
            label.normal.textColor = new Color(.96f, .93f, .84f);
            small = new GUIStyle(label) { fontSize = 17 };
            title = new GUIStyle(label) { fontSize = 35, fontStyle = FontStyle.Bold };
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 23, padding = new RectOffset(10, 10, 7, 7) };
            field = new GUIStyle(GUI.skin.textField) { font = font, fontSize = 23 };
            caption = new GUIStyle(small) { fontSize = 12, wordWrap = false, clipping = TextClipping.Clip };
            centered = new GUIStyle(caption) { alignment = TextAnchor.MiddleCenter };
            tableButton = new GUIStyle(centered) { fontSize = 18 };
        }
        static void Fill(Rect r, Color color) { var old = GUI.color; GUI.color = color; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
        bool Button(Rect r, string text)
        {
            Vector3 a = GUI.matrix.MultiplyPoint3x4(new Vector3(r.x, r.y, 0)), b = GUI.matrix.MultiplyPoint3x4(new Vector3(r.xMax, r.yMax, 0));
            uiButtons.Add(Rect.MinMaxRect(a.x, a.y, b.x, b.y));
            bool old = GUI.enabled; GUI.enabled = old && !Busy;
            Fill(r, new Color(.48f, .4f, .27f, .95f));
            Fill(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), new Color(.065f, .105f, .17f, .96f));
            bool pressed = GUI.Button(r, text, tableButton); GUI.enabled = old; return pressed;
        }
        void Panel(Rect r)
        {
            Fill(r, new Color(.48f, .4f, .27f, .95f));
            Fill(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), new Color(.045f, .065f, .10f, .95f));
        }
        Texture2D Skin(string name)
        {
            if (!skin.TryGetValue(name, out var texture)) { texture = Resources.Load<Texture2D>("Majsoul/UI/" + name); skin[name] = texture; }
            return texture;
        }
        bool SkinButton(Rect r, string sprite, string tooltip)
        {
            Vector3 a = GUI.matrix.MultiplyPoint3x4(new Vector3(r.x, r.y, 0)), b = GUI.matrix.MultiplyPoint3x4(new Vector3(r.xMax, r.yMax, 0));
            uiButtons.Add(Rect.MinMaxRect(a.x, a.y, b.x, b.y));
            var texture = Skin(sprite); if (texture != null) GUI.DrawTexture(r, texture, ScaleMode.ScaleToFit);
            bool enabled = GUI.enabled; GUI.enabled &= !Busy;
            bool clicked = GUI.Button(r, new GUIContent("", tooltip), GUIStyle.none); GUI.enabled = enabled; return clicked;
        }
        void ShadowLabel(Rect r, string text, GUIStyle style, Color color)
        {
            Color old = GUI.color; GUI.color = new Color(0, 0, 0, .85f); GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), text, style);
            GUI.color = color; GUI.Label(r, text, style); GUI.color = old;
        }
        void PortraitFrame(Rect r, bool current)
        {
            var texture = Skin("headframe-default"); if (texture == null) return;
            Color old = GUI.color; GUI.color = current ? new Color(1, .70f, .60f) : Color.white;
            float[] x = { r.x, r.x + 4, r.xMax - 4, r.xMax }, y = { r.y, r.y + 4, r.yMax - 4, r.yMax };
            float edge = 18f / texture.width; float[] uv = { 0, edge, 1 - edge, 1 };
            for (int iy = 0; iy < 3; iy++) for (int ix = 0; ix < 3; ix++)
            {
                if (ix == 1 && iy == 1) continue;
                GUI.DrawTextureWithTexCoords(Rect.MinMaxRect(x[ix], y[iy], x[ix + 1], y[iy + 1]), texture, new Rect(uv[ix], 1 - uv[iy + 1], uv[ix + 1] - uv[ix], uv[iy + 1] - uv[iy]));
            }
            GUI.color = old;
        }
        void OnGUI()
        {
            Styles(); uiButtons.Clear(); var previous = GUI.matrix;
            bool playing = active && State != null && (State.phase == "play" || State.phase == "reaction");
            viewport = TableViewport.Fit(Screen.width, Screen.height, Screen.safeArea, playing);
            GUI.matrix = viewport.Matrix;
            if (State == null || !active) DrawHome();
            else if (State.phase == "lobby") DrawLobby();
            else if (State.phase == "result") DrawResult();
            else DrawTableUI();
            // Dialogs retain their own centered, uniformly scaled canvas.
            GUI.matrix = TableViewport.Fit(Screen.width, Screen.height, Screen.safeArea, false).Matrix;
            if (!string.IsNullOrEmpty(message)) { Panel(new Rect(190, 658, 900, 52)); GUI.Label(new Rect(205, 670, 870, 32), message, small); }
            if (showRoom) DrawRoom();
            if (page == "settings") DrawSettings();
            if (page == "confirm-close")
            {
                Panel(new Rect(350, 200, 580, 250)); GUI.Label(new Rect(385, 235, 500, 70), "结束牌桌会清除当前对局。", label);
                if (Button(new Rect(385, 340, 230, 55), "确认结束")) { page = "home"; StartCoroutine(Send(new RequestBody { op = "close" })); }
                if (Button(new Rect(645, 340, 230, 55), "继续打牌")) page = "home";
            }
            GUI.matrix = previous;
        }
        void HudNumber(int value, float x, float y, float scale)
        {
            string text = value.ToString();
            for (int i=0;i<text.Length;i++)
            {
                var digit = Resources.Load<Texture2D>("Majsoul/Hud/w_" + text[i]);
                if (digit != null) GUI.DrawTexture(new Rect(x+i*25*scale,y,24*scale,36*scale),digit,ScaleMode.ScaleToFit);
            }
        }
        void DrawHome()
        {
            Panel(new Rect(345, 65, 590, 575)); GUI.Label(new Rect(380, 90, 520, 56), "云间日麻 · 三维牌桌", title);
            GUI.Label(new Rect(380, 151, 500, 30), "三维牌桌 · 手机开房 · 离线局域网", small);
            GUI.Label(new Rect(380, 199, 100, 35), "昵称", label); nickname = GUI.TextField(new Rect(490, 195, 400, 40), nickname, 12, field);
            GUI.Label(new Rect(380, 248, 100, 35), "房主地址", small); endpoint = GUI.TextField(new Rect(490, 244, 400, 40), endpoint, field);
#if UNITY_EDITOR || UNITY_STANDALONE
            GUI.Label(new Rect(380, 294, 110, 35), "测试房主密钥", small); secret = GUI.TextField(new Rect(490, 290, 400, 40), secret, field);
#endif
            if (Button(new Rect(380, 342, 160, 42), players == 3 ? "三人麻将" : "四人麻将")) players = players == 3 ? 4 : 3;
            if (Button(new Rect(550, 342, 160, 42), rounds == 1 ? "东风战" : "半庄战")) rounds = rounds == 1 ? 2 : 1;
            if (Button(new Rect(720, 342, 170, 42), red ? "有赤宝牌" : "无赤宝牌")) red = !red;
            if (Button(new Rect(380, 395, 245, 52), "单人练习")) StartRoom(true);
            if (Button(new Rect(645, 395, 245, 52), "创建房间")) StartRoom(false);
            GUI.Label(new Rect(380, 462, 100, 30), "房间码", small); pin = GUI.TextField(new Rect(480, 457, 150, 40), pin, 6, field);
            recovery = GUI.TextField(new Rect(645, 457, 245, 40), recovery, 12, field);
            GUI.Label(new Rect(645, 500, 245, 26), "恢复码（首次加入留空）", small);
            if (Button(new Rect(380, 531, 245, 48), "加入房间")) JoinRoom();
            if (Button(new Rect(645, 531, 245, 48), "继续上次牌桌")) StartCoroutine(Send(new RequestBody { op = "state" }));
            if (Button(new Rect(1040, 24, 210, 44), "声音设置")) page = "settings";
        }
        void DrawLobby()
        {
            Panel(new Rect(170, 110, 940, 475)); GUI.Label(new Rect(205, 135, 800, 50), "好友入座 · 房间 " + State.pin, title);
            for (int i = 0; i < State.n; i++)
            {
                Rect r = new Rect(205 + i * 220, 215, 200, 210); Panel(r);
                if (i < State.players.Length)
                {
                    var p = State.players[i]; GUI.DrawTexture(new Rect(r.x + 53, r.y + 15, 94, 94), table.Portrait(p), ScaleMode.ScaleToFit);
                    GUI.Label(new Rect(r.x + 15, r.y + 118, 175, 35), p.name, label);
                    if (State.host && p.bot && Button(new Rect(r.x + 20, r.y + 160, 160, 38), "移除 AI")) StartCoroutine(Send(new RequestBody { op = "removeBot", seat = i }));
                }
                else if (State.host && Button(new Rect(r.x + 20, r.y + 70, 160, 50), "添加 AI")) StartCoroutine(Send(new RequestBody { op = "addBot" }));
            }
            if (State.host && Button(new Rect(730, 480, 320, 60), "补齐 AI 并开局")) StartCoroutine(Send(new RequestBody { op = "start", fillBots = true }));
            if (Button(new Rect(205, 480, 180, 60), "房间信息")) showRoom = true;
            if (Button(new Rect(410, 480, 180, 60), "返回大厅")) EnterHome();
        }
        void DrawTableUI()
        {
            float w = viewport.Width, h = viewport.Height;
            if (SkinButton(new Rect(w - 175, 20, 40, 40), "replay_collect", "房间信息")) showRoom = true;
            if (SkinButton(new Rect(w - 117, 20, 40, 40), "btn_settings", "声音设置")) page = "settings";
            if (SkinButton(new Rect(w - 59, 20, 40, 40), "btn_leave", "返回大厅")) { EnterHome(); return; }
            // Original UI_DesktopInfo top-left node coordinates, uniformly scaled.
            const float k = .55f, ox = 34, oy = .6f;
            GUI.DrawTexture(new Rect(ox + 11*k, oy + 28*k, 313*k, 158*k), Skin("left_top"), ScaleMode.ScaleToFit);
            for (int i = 0; i < 5; i++)
            {
                Rect tile = new Rect(ox + (28+i*56)*k, oy + 44*k, 54*k, 81*k);
                GUI.DrawTexture(tile, table.DoraTexture(i < State.indicators.Length ? State.indicators[i] : null), ScaleMode.ScaleToFit);
            }
            HudNumber(State.sticks, ox + 117*k, oy + 139*k, k);
            HudNumber(State.honba, ox + 256*k, oy + 139*k, k);
            ShadowLabel(new Rect(ox + 23*k, oy + 186*k, 290*k, 51*k), "云间 · " + (State.n == 3 ? "三人" : "四人") + (State.rounds == 1 ? "东" : "半庄"), caption, new Color(.94f, .78f, .49f));
            foreach (var p in State.players.Select((v, i) => new { v, i }))
            {
                int seat = table.RelativeSeat(p.i);
                Vector2 anchor = seat == 0 ? new Vector2(w * .26f, h * .83f - 104) : seat == State.n - 1 ? new Vector2(64, h * .265f) : seat == 1 && State.n == 4 ? new Vector2(w - 132, h * .52f) : new Vector2(w * .676f, 26);
                bool turn = State.turn == p.i;
                GUI.DrawTexture(new Rect(anchor.x, anchor.y, 62, 62), table.Portrait(p.v), ScaleMode.ScaleAndCrop);
                PortraitFrame(new Rect(anchor.x - 3, anchor.y - 3, 68, 68), turn);
                ShadowLabel(new Rect(anchor.x - 35, anchor.y + 64, 132, 19), p.v.name, centered, Color.white);
                ShadowLabel(new Rect(anchor.x - 35, anchor.y + 82, 132, 19), "东南西北"[p.v.wind] + "家 · " + p.v.score, centered, new Color(.69f, .77f, .94f));
            }
            string status = State.phase == "reaction" ? (State.options.Length > 0 ? "请选择操作" : "等待其他玩家响应") : State.turn == State.you ? "轮到你出牌" : State.players[State.turn].name + " 的回合";
            if (showHints || State.phase == "reaction") ShadowLabel(new Rect(w * .5f - 210, h - 128, 420, 26), status, centered, Color.white);
            if (SkinButton(new Rect(24, h - 66, 36, 36), showHints ? "ob_ting_on_btn" : "ob_ting_off_btn", "切换出牌提示")) showHints = !showHints;
            ShadowLabel(new Rect(4, h - 31, 76, 18), "出牌提示", centered, new Color(.66f, .75f, .92f));
            if (RiichiMode) GUI.Label(new Rect(w * .5f - 210, h - 105, 420, 22), "选牌后再点一次 · 立直打出", centered);
            if (State.furiten) GUI.Label(new Rect(22, h - 58, 175, 28), "振听 · 暂不可荣和", caption);
            DrawResponseMarker();
            DrawActionButtons(w,h);
            effects.DrawCall(w,h,State.n);
        }
        bool ActionButton(Rect r,string sprite,string tooltip)
        {
            var a=GUI.matrix.MultiplyPoint3x4(new Vector3(r.x,r.y,0));var b=GUI.matrix.MultiplyPoint3x4(new Vector3(r.xMax,r.yMax,0));
            uiButtons.Add(Rect.MinMaxRect(a.x,a.y,b.x,b.y));
            var image=effects.Art(sprite);if(image!=null)GUI.DrawTexture(r,image,ScaleMode.ScaleToFit);
            bool old=GUI.enabled;GUI.enabled&=!Busy;bool clicked=GUI.Button(r,new GUIContent("",tooltip),GUIStyle.none);GUI.enabled=old;return clicked;
        }
        public Rect[] ActionRects(float width,float height)
        {
            int count=State.options.Where(x=>x.kind!="discard" && x.kind!="riichi").Select(x=>x.kind).Distinct().Count()+(State.options.Any(x=>x.kind=="riichi")?1:0);
            float buttonWidth=Mathf.Min(156,(width*.62f-20)/Mathf.Max(1,count));
            return Enumerable.Range(0,count).Select(i=>new Rect(width-100-(count-i)*buttonWidth,height-168,buttonWidth-5,72)).ToArray();
        }
        void ChooseActionKind(string kind)
        {
            if(Busy)return;
            var choices=State.options.Where(x=>x.kind==kind).ToArray();
            if(choices.Length==1)Act(choices[0].id);
            else if(choices.Length>1)choosingKind=choosingKind==kind?null:kind;
        }
#if UNITY_EDITOR
        public string ValidationChoosingKind => choosingKind;
        public void ValidationChooseActionKind(string kind) => ChooseActionKind(kind);
#endif
        void DrawActionButtons(float width,float height)
        {
            var groups=State.options.Where(x=>x.kind!="discard" && x.kind!="riichi").GroupBy(x=>x.kind).OrderBy(g=>g.Key=="pass"?1:0).ToArray();
            var rects=ActionRects(width,height);int index=0;
            if(State.options.Any(x=>x.kind=="riichi"))
            {
                if(ActionButton(rects[index++],"op_lizhi",RiichiMode?"取消立直":"立直")) {RiichiMode=!RiichiMode;SelectedAction=null;SelectedIndex=-1;table.Render(State);}
            }
            foreach(var group in groups)
            {
                string sprite=group.Key=="chi"?"op_chi":group.Key=="pon"?"op_peng":group.Key=="kan"||group.Key=="minkan"?"op_gang":group.Key=="ron"?"op_hu":group.Key=="tsumo"?"op_zimo":group.Key=="nuki"?"op_babei":group.Key=="abort9"?"op_liuju":"op_x";
                if(ActionButton(rects[index++],sprite,ActionName(group.Key)))
                {
                    ChooseActionKind(group.Key);
                }
            }
            if(choosingKind==null)return;
            var choices=State.options.Where(x=>x.kind==choosingKind).ToArray();float choiceWidth=126;float total=Mathf.Min(width*.82f,choices.Length*choiceWidth);
            float x=width-100-total,y=height-258;
            var bg=effects.Art("bg_chipengchoose");if(bg!=null)GUI.DrawTexture(new Rect(x-8,y-8,total+16,84),bg,ScaleMode.StretchToFill);
            for(int i=0;i<choices.Length;i++)
            {
                Rect r=new Rect(x+i*total/choices.Length,y,total/choices.Length-4,66);
                var a=GUI.matrix.MultiplyPoint3x4(new Vector3(r.x,r.y,0));var b=GUI.matrix.MultiplyPoint3x4(new Vector3(r.xMax,r.yMax,0));uiButtons.Add(Rect.MinMaxRect(a.x,a.y,b.x,b.y));
                var meld=choices[i].meld;var digits=meld.Skip(1).Where(char.IsDigit).ToArray();float tw=Mathf.Min(38,(r.width-8)/Mathf.Max(1,digits.Length));
                for(int j=0;j<digits.Length;j++)GUI.DrawTexture(new Rect(r.center.x-digits.Length*tw/2+j*tw,r.y+4,tw-1,53),table.TileTexture(meld[0].ToString()+digits[j]),ScaleMode.ScaleToFit);
                bool old=GUI.enabled;GUI.enabled&=!Busy;if(GUI.Button(r,"",GUIStyle.none))Act(choices[i].id);GUI.enabled=old;
            }
        }
        void DrawResponseMarker()
        {
            var target=table.ResponseTarget;if(target==null)return;
            var box=target.GetComponent<BoxCollider>();Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
            {
                var point=table.Camera.WorldToScreenPoint(box.transform.TransformPoint(box.center+Vector3.Scale(box.size/2,new Vector3(x,y,z))));
                var gui=viewport.FromScreen(point,Screen.height);min=Vector2.Min(min,gui);max=Vector2.Max(max,gui);
            }
            Rect r=Rect.MinMaxRect(min.x-3,min.y-3,max.x+3,max.y+3);
            var arrow=effects.Art("response-arrow");if(arrow!=null) {var old=GUI.color;GUI.color=new Color(1,.9f,.2f);GUI.DrawTexture(new Rect(r.center.x-13,r.y-22+Mathf.Sin(Time.unscaledTime*5)*2,26,20),arrow,ScaleMode.ScaleToFit);GUI.color=old;}
        }
        void DrawResult()
        {
            Panel(new Rect(190, 80, 900, 550)); var result = State.result;
            GUI.Label(new Rect(225, 110, 820, 50), result?.title ?? "本局结束", title);
            int y = 185;
            if (result?.details != null) foreach (var d in result.details)
            {
                GUI.Label(new Rect(225, y, 800, 32), d.name + "  " + (d.score.damanguan > 0 ? d.score.damanguan + "倍役满" : d.score.fanshu + "番 " + d.score.fu + "符") + "  " + d.score.defen + "点", label); y += 34;
                GUI.Label(new Rect(225, y, 800, 30), d.hand, small); y += 40;
            }
            for (int i = 0; i < State.players.Length; i++)
            {
                int delta = result?.delta != null && i < result.delta.Length ? result.delta[i] : 0;
                GUI.Label(new Rect(225, y, 800, 32), State.players[i].name + "   " + (delta >= 0 ? "+" : "") + delta + " → " + State.players[i].score, label); y += 35;
            }
            if (!State.players[State.you].ready && Button(new Rect(225, 545, 240, 55), "确认结算")) StartCoroutine(Send(new RequestBody { op = "ready" }));
            if (State.host && State.players.All(x => x.ready) && Button(new Rect(485, 545, 240, 55), result != null && result.matchOver ? "再来一场" : "下一局")) StartCoroutine(Send(new RequestBody { op = "next" }));
            if (Button(new Rect(805, 545, 240, 55), "返回大厅")) EnterHome();
        }
        void DrawRoom()
        {
            Panel(new Rect(270, 120, 740, 450)); GUI.Label(new Rect(305, 145, 650, 50), "房间信息", title);
            GUI.Label(new Rect(305, 210, 650, 60), "房间码：" + State?.pin + "\n个人恢复码：" + State?.recovery, label);
            GUI.Label(new Rect(305, 295, 650, 100), string.IsNullOrEmpty(addresses) ? endpoint : addresses, label);
            if (Button(new Rect(305, 455, 210, 55), "关闭")) showRoom = false;
            if (State != null && State.host && Button(new Rect(535, 455, 210, 55), "结束当前牌桌")) { showRoom = false; page = "confirm-close"; }
        }
        void DrawSettings()
        {
            Panel(new Rect(350, 160, 580, 350)); GUI.Label(new Rect(385, 190, 490, 50), voice.HasYiji ? "一姬角色语音" : "离线中文语音", title);
            if (Button(new Rect(385, 255, 490, 48), muted ? "语音：关闭" : "语音：开启")) { muted = !muted; SaveSound(); }
            volume = GUI.HorizontalSlider(new Rect(385, 330, 490, 25), volume, 0, 1); voice.Configure(!muted, volume);
            if (Button(new Rect(385, 395, 230, 55), "试听荣和")) voice.Preview();
            if (Button(new Rect(645, 395, 230, 55), "保存并关闭")) { SaveSound(); page = "home"; }
        }
        void SaveSound() { PlayerPrefs.SetInt("riichi.muted", muted ? 1 : 0); PlayerPrefs.SetFloat("riichi.volume", volume); PlayerPrefs.Save(); voice.Configure(!muted, volume); }
        public static string TileName(string p)
        {
            if (string.IsNullOrEmpty(p) || p.Length < 2) return "";
            if (p[0] == 'z') return "东南西北白发中"[p[1] - '1'].ToString();
            return (p[1] == '0' ? "赤5" : p[1].ToString()) + (p[0] == 'm' ? "万" : p[0] == 'p' ? "筒" : "索");
        }
        static string MeldName(string m) { return string.Join("", m.Skip(1).Where(char.IsDigit).Select(n => TileName(m[0].ToString() + n))); }
        static string ActionName(string kind)
        {
            switch (kind) { case "ron": return "荣和"; case "tsumo": return "自摸"; case "pon": return "碰"; case "chi": return "吃"; case "kan": case "minkan": return "杠"; case "nuki": return "拔北"; case "abort9": return "九种九牌"; case "pass": return "过"; default: return kind; }
        }
        void OnApplicationPause(bool pause) { voice.Suspend(pause);effects.Reset(); }
        void OnDestroy() { bridge?.Dispose(); }
    }
}
