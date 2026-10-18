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
    public partial class RiichiClient : MonoBehaviour
    {
        public RoomView State { get; private set; }
        public string SelectedAction { get; private set; }
        public int SelectedIndex { get; private set; } = -1;
        public bool RiichiMode { get; private set; }
        public bool Busy { get; private set; }
        string endpoint = "http://127.0.0.1:8765", token = "", secret = "", nickname = "旅人", pin = "", recovery = "";
        string message = "", page = "home", addresses = "", hostError = "";
        int players = 3, rounds = 1, generation;
        bool active, red = true, showRoom, muted,pendingUserActivity;
        float nextPoll, volume = .8f;
        TableRenderer table;
        RoleVoice voice;
        GameAudio gameAudio;
        SettlementAudio settlementAudio;
        PlaylistAudio playlistAudio;
        NativeSettingsUi nativeSettings;
        int audioSettingsRevision = -1;
        bool musicMuted, effectsMuted;
        float musicVolume=.35f, effectsVolume=.8f;
        TableEffects effects;
        ScreenTapEffect tap;
        LobbyController lobby;
        public ScreenTapEffect Tap => tap;
        readonly ResultPresentation resultDisplay=new ResultPresentation();
        readonly MatchIntroduction introduction=new MatchIntroduction();
        readonly AllLastAnnouncement allLast=new AllLastAnnouncement();
        readonly YakumanAnnouncement yakuman=new YakumanAnnouncement();
        public float IntroRemaining=>introduction.Remaining;
#if UNITY_EDITOR
        public MatchIntroduction ValidationIntro=>introduction;
        public AllLastAnnouncement ValidationAllLast=>allLast;
        public YakumanAnnouncement ValidationYakuman=>yakuman;
#endif
#if UNITY_EDITOR
        public ResultPresentation ValidationResult => resultDisplay;
#endif
        string choosingKind;
        GUIStyle label, title, button, field, small;
        GUIStyle caption, centered, tableButton;
        TableViewport viewport;
        readonly Dictionary<string, Texture2D> skin = new Dictionary<string, Texture2D>();
        GUIStyle namePlateText,roomCaption;
        static readonly WaitDetail[] NoWaits=new WaitDetail[0];
        bool waitPressCaptured,waitPressCancelled,waitHoldVisible;
        string waitDecisionKey;
        Rect waitButtonScreenRect;
#if UNITY_EDITOR
        bool validationWaitInput;
#endif
        public bool OptionsReady => State!=null && !AutoPlaying && !lobby.OverlayActive && !introduction.Active && !allLast.Active && !table.ArrangingHand && !table.RevealingDraw && !effects.HasPendingActions;
        bool CanInspectWaits => active && !AutoPlaying && State!=null && (State.phase=="play" || State.phase=="reaction") && !showRoom && page!="settings" && !lobby.OverlayActive;
        public bool WaitIndicatorVisible => CanInspectWaits && ((State.waitDetails?.Length??0)>0 || State.phase=="play"&&State.turn==State.you&&(State.discardHints??new DiscardHint[0]).Any(h=>(h.waits?.Length??0)>0));
        DiscardHint ActiveWaitHint => State?.discardHints?.FirstOrDefault(h=>h.id==SelectedAction);
        bool PreviewingDiscard => CanInspectWaits && State.phase=="play" && State.turn==State.you && SelectedAction!=null && State.options.Any(o=>o.id==SelectedAction && (o.kind=="discard" || o.kind=="riichi"));
        // Keep the original wait-only popup. A prospective discard opens it
        // after that tile is selected; submitting the discard closes it.
        public WaitDetail[] CurrentWaitDetails => !CanInspectWaits || !waitHoldVisible ? NoWaits : PreviewingDiscard ? ActiveWaitHint?.waits??NoWaits : State.waitDetails??NoWaits;
        public DiscardHint[] CurrentDiscardWaits=>CanInspectWaits && waitHoldVisible && !PreviewingDiscard && CurrentWaitDetails.Length==0?(State.discardHints??new DiscardHint[0]).Where(h=>h.waits?.Length>0 && State.options.Any(o=>o.id==h.id && o.kind==(RiichiMode?"riichi":"discard"))).GroupBy(h=>State.options.First(o=>o.id==h.id).tile).Select(g=>g.First()).ToArray():new DiscardHint[0];
        public Rect CurrentWaitButton { get; private set; }
        public bool CurrentFuriten => ActiveWaitHint?.furiten??(State?.furiten??false);
        public Rect CurrentWaitPanel { get; private set; }
        public Rect CurrentFuritenPanel { get; private set; }
        readonly List<Rect> uiButtons = new List<Rect>();
        Font font;
        AndroidJavaClass bridge;
        bool validationSession;

#if UNITY_EDITOR
        public bool ValidationTableOnly;
        public bool SessionPreviewOnly;
        public void BeginLobbyValidation(string hostSecret)
        {
            EnterHome();validationSession=true;secret=hostSecret;token="";endpoint="http://127.0.0.1:8765";nickname="大厅验证";lobby.Home();
        }
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
            introduction.Reset();allLast.Reset();ResetWaitPress();validationWaitInput=false;
            nextPoll = float.PositiveInfinity; ClearSelection();
            table.Render(fixture);gameAudio.SetScene(fixture);
            effects.Reset();resultDisplay.Reset();choosingKind=null;waitDecisionKey=null;ObserveWaitState();yakuman.Reset();yakuman.Observe(State);
        }
        public void ValidationApplyState(RoomView fixture){State=fixture;ObserveWaitState();yakuman.Observe(fixture);introduction.Observe(fixture,true);allLast.Observe(fixture);RebindSelection();table.Render(fixture);gameAudio.SetScene(fixture);}
#endif

        void Awake()
        {
            AppPreferences.Load();
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            table = gameObject.AddComponent<TableRenderer>(); table.Client = this;
            effects = gameObject.AddComponent<TableEffects>();
            voice = gameObject.AddComponent<RoleVoice>();
            gameAudio = gameObject.AddComponent<GameAudio>();settlementAudio=gameObject.AddComponent<SettlementAudio>();
            playlistAudio = gameObject.AddComponent<PlaylistAudio>(); playlistAudio.Initialize(this);
            nativeSettings = new NativeSettingsUi(playlistAudio);
            tap = gameObject.AddComponent<ScreenTapEffect>();
            lobby=gameObject.AddComponent<LobbyController>();lobby.Initialize(this);
            musicMuted=PlayerPrefs.GetInt("riichi.musicMuted",0)!=0;effectsMuted=PlayerPrefs.GetInt("riichi.effectsMuted",0)!=0;
            musicVolume=PlayerPrefs.GetFloat("riichi.musicVolume",.35f);effectsVolume=PlayerPrefs.GetFloat("riichi.effectsVolume",.8f);
            gameAudio.Configure(!musicMuted,musicVolume,!effectsMuted,effectsVolume);
            nickname = PlayerPrefs.GetString("riichi.name", "旅人");
            endpoint = PlayerPrefs.GetString("riichi.server", endpoint);
            secret = PlayerPrefs.GetString("riichi.editorHost", "");
            volume = PlayerPrefs.GetFloat("riichi.volume", .8f);
            muted = PlayerPrefs.GetInt("riichi.muted", 0) != 0;
            token = PlayerPrefs.GetString("riichi.token:" + endpoint, "");
            LoadSession();
            voice.Configure(!muted, volume);settlementAudio.Configure(!muted,volume);
            ApplyAudioSettings();
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
        string selectedFace;int selectedCopy;
        void ClearSelection(){SelectedAction=null;SelectedIndex=-1;selectedFace=null;selectedCopy=0;}
        void RebindSelection()
        {
            if(selectedFace==null || State==null || (State.phase!="play" && State.phase!="reaction")){ClearSelection();return;}
            var hand=TableRenderer.DisplayHand(State);int copy=0,index=-1;
            for(int i=0;i<hand.Count;i++)if(hand[i]==selectedFace && copy++==selectedCopy){index=i;break;}
            if(index<0){ClearSelection();return;}
            SelectedIndex=index;bool drawn=!string.IsNullOrEmpty(State.drawn) && index==hand.Count-1;
            var choices=State.options.Where(o=>o.kind==(RiichiMode?"riichi":"discard")).ToArray();
            SelectedAction=TableRenderer.HandChoice(State,selectedFace,drawn,RiichiMode)?.id;
            if(choices.Length>0 && SelectedAction==null)ClearSelection();
        }
        public void SelectTile(int index, string action)
        {
            if(table.ArrangingHand || AutoPlaying || lobby.OverlayActive || State==null || (State.phase!="play" && State.phase!="reaction"))return;
            var hand=TableRenderer.DisplayHand(State);if(index<0 || index>=hand.Count)return;
            bool legal=action!=null && State.options.Any(o=>o.id==action && (o.kind=="discard" || o.kind=="riichi"));
            bool choosing=State.options.Any(o=>o.kind==(RiichiMode?"riichi":"discard"));
            if(choosing && !legal)return;
            if(legal && OptionsReady && !Busy && SelectedIndex==index && SelectedAction==action){ClearSelection();table.SelectHand(-1);Act(action);return;}
            SelectedIndex=index;SelectedAction=legal?action:null;selectedFace=hand[index];selectedCopy=hand.Take(index).Count(t=>t==selectedFace);table.SelectHand(index);
            if(legal&&(ActiveWaitHint?.waits?.Length??0)>0)waitHoldVisible=true;
        }
        public void Act(string id) { if(!OptionsReady)return;choosingKind=null;if(id.StartsWith("discard:")||id.StartsWith("riichi:"))ResetWaitPress();StartCoroutine(Send(new RequestBody { op = "action", action = id })); }
        IEnumerator Send(RequestBody req, Action success = null, bool poll = false)
        {
            if (Busy) yield break;
            if (!ValidEndpoint(endpoint)) { message = "请输入局域网 IPv4 地址，端口固定为 8765"; yield break; }
            if(active&&pendingUserActivity){req.userActive=true;pendingUserActivity=false;}
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
                if (web.result != UnityWebRequest.Result.Success) { if(poll)resumingConnection=true;message = "连接中断，正在重连：" + web.error; yield break; }
                RoomReply reply;
                try { reply = JsonUtility.FromJson<RoomReply>(web.downloadHandler.text); }
                catch (Exception e) { message = "房主响应无法解析：" + e.Message; yield break; }
                if (reply == null || !reply.ok) {
                    if(reply?.code=="ROOM_ACTIVE"&&reply.state!=null){CacheSession(reply.state,endpoint,reply.token);message="";lobby.ShowPendingSession();}
                    else {message="";lobby.ShowMediumNotice("连接提示",reply?.error??"无效响应");}
                    yield break;
                }
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
                    bool gone=active&&(reply.state.phase=="empty"||reply.state.phase=="join");
                    if(gone){ForgetSession();EnterHome();lobby.Home();lobby.ShowSessionCancelled();}
                    else ApplyRoomState(reply.state,poll&&req.op=="resume");
                }
                if(req.op=="resume")resumingConnection=false;
                success?.Invoke();
            }
        }
        void ResetWaitPress()
        {waitPressCaptured=false;waitPressCancelled=false;waitHoldVisible=false;}
        void ObserveWaitState()
        {
            if(State==null||State.phase!="play"||State.turn!=State.you||!State.options.Any(o=>o.kind=="discard"||o.kind=="riichi")){
                waitDecisionKey=null;ResetWaitPress();return;
            }
            string next=State.audioRoom+":"+State.matchSeq+":"+State.handSeq+":"+State.players[State.you].river.Length+":"+string.Join(",",State.hand)+":"+State.drawn+":"+string.Join(",",State.players[State.you].melds);
            if(next==waitDecisionKey)return;
            waitDecisionKey=next;waitPressCaptured=false;waitPressCancelled=false;
            waitHoldVisible=(State.waitDetails?.Length??0)>0;
        }
        // Capture the whole gesture, including release outside the button, so
        // dragging off this read-only control cannot accidentally discard a tile.
        bool UpdateWaitPointer(Vector2 point,bool down,bool held,bool up,float now)
        {
            bool consumed=waitPressCaptured;
            if(!WaitIndicatorVisible){ResetWaitPress();return consumed;}
            if(down && waitButtonScreenRect.Contains(point)){
                waitPressCaptured=true;waitPressCancelled=false;consumed=true;
            }
            if(!waitPressCaptured)return consumed;
            if(up || !held){bool toggle=!waitPressCancelled&&waitButtonScreenRect.Contains(point);waitPressCaptured=false;waitPressCancelled=false;if(toggle)waitHoldVisible=!waitHoldVisible;return true;}
            if(!new Rect(waitButtonScreenRect.x-10,waitButtonScreenRect.y-10,waitButtonScreenRect.width+20,waitButtonScreenRect.height+20).Contains(point))waitPressCancelled=true;
            return true;
        }
        void OnApplicationFocus(bool focused){if(!focused)ResetWaitPress();}
        void OnDisable(){ResetWaitPress();}
        void Update()
        {
            ApplyAudioSettings();
            introduction.Tick(gameAudio);
            if(active&&(Input.anyKeyDown||Input.GetMouseButton(0)||Input.mouseScrollDelta.sqrMagnitude>0))pendingUserActivity=true;
            yakuman.Tick(active&&State!=null&&(State.phase=="play"||State.phase=="reaction")&&!introduction.Active&&!lobby.OverlayActive&&!showRoom&&page!="settings"&&!AutoPlaying,gameAudio);
            // Every press, on the table or the HUD, gets Majsoul's sakura tap burst.
            if(Input.GetMouseButtonDown(0))tap.Play(Input.mousePosition);
            if(State?.phase=="result" && (State.result.draw || !effects.ShowWinPresentation)){
                resultDisplay.Observe(State);settlementAudio.Tick(State,resultDisplay,true,gameAudio);
#if UNITY_EDITOR
                bool livePresentation=!SessionPreviewOnly;
#else
                bool livePresentation=true;
#endif
                if(livePresentation&&active&&!Busy&&!introduction.Active&&!effects.HasPendingActions&&!lobby.OverlayActive&&page!="settings"&&resultDisplay.Tick(State,AutoPlaying)==ResultPresentation.Action.Ready)
                    StartCoroutine(Send(new RequestBody{op="ready",handSeq=State.handSeq}));
            }
            else if(State?.phase!="result")settlementAudio.Reset();
            bool waitInputConsumed=waitPressCaptured;
#if UNITY_EDITOR
            if(!validationWaitInput)
#endif
            waitInputConsumed=UpdateWaitPointer(new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y),Input.GetMouseButtonDown(0),Input.GetMouseButton(0),Input.GetMouseButtonUp(0),Time.unscaledTime);
            if (active && !Busy && Time.unscaledTime >= nextPoll) { nextPoll = Time.unscaledTime + .65f;
#if UNITY_EDITOR
                if(!SessionPreviewOnly)
#endif
                StartCoroutine(Send(new RequestBody { op = resumingConnection?"resume":"state",room=State?.audioRoom }, null, true)); }
            if(active&&State?.phase!="lobby"&&Input.GetKeyDown(KeyCode.Escape)){if(page=="settings")LobbyCloseSettings();else if(lobby.OverlayActive)lobby.CloseModal();else lobby.ShowMatchExit();}
            if (active && !introduction.Active && Input.GetMouseButtonUp(0) && !waitInputConsumed && !InUI(Input.mousePosition))
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
            return showRoom || page == "settings" || lobby.OverlayActive || uiButtons.Any(r => r.Contains(new Vector2(x, y)));
        }
        void EnterHome()
        {
            generation++; active = false; State = null; ClearSelection(); RiichiMode = false;showRoom=false;
            page = "home";introduction.Reset();allLast.Reset();ResetWaitPress();waitDecisionKey=null;table.Clear(); voice.Reset();gameAudio.Reset();settlementAudio.Reset();effects.Reset();resultDisplay.Reset();choosingKind=null;
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
            if (!ValidEndpoint(address)) { lobby.ShowMediumNotice("加入房间","请输入同一局域网内的房主 IPv4 地址"); return; }
            endpoint = address; generation++; token = PlayerPrefs.GetString("riichi.token:" + endpoint, "");
            StartCoroutine(Send(new RequestBody { op = "join", name = nickname, pin = pin },()=>{if(State!=null&&State.phase!="lobby")introduction.Reenter(State);}));
        }
        // Narrow lobby adapter. The existing action/selection/rendering path
        // remains owned by this client and is not called by the new screens.
        public LobbyController Lobby => lobby;
        public bool LobbyActive => active;
        public bool LobbyPlaying => active && State!=null && State.phase!="lobby";
        public bool LobbyOverlayVisible => showRoom || page=="settings";
        public bool LobbyCloseSettings(){if(page!="settings")return false;if(nativeSettings.Back())page="home";return true;}
        public string LobbyNickname { get=>nickname;set{nickname=value;PlayerPrefs.SetString("riichi.name",value);PlayerPrefs.Save();} }
        public string LobbyEndpoint => endpoint;
        public string InvitationPayload {
            get {
                if(string.IsNullOrEmpty(State?.pin))return "";
                var hosts=State.host&&!string.IsNullOrWhiteSpace(addresses)?addresses.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries):new[]{endpoint};
                return RoomQr.EncodeInvite(State.pin,hosts.Where(ValidEndpoint).Where(host=>Application.isEditor||!new Uri(host).IsLoopback));
            }
        }
        public string LobbyInviteText => "房间号："+(State?.pin??"")+"\n房主地址：\n"+(string.IsNullOrEmpty(addresses)?endpoint:addresses)+"\n连接同一局域网后输入房间号加入，也可接替电脑座位。";
        public void LobbyOpenSettings(){nativeSettings.Open();page="settings";}
        void UseLocalHost()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            endpoint="http://127.0.0.1:8765";
#endif
            generation++;token=PlayerPrefs.GetString("riichi.token:"+endpoint,"");
        }
        public void LobbyCreate(int count,int length,bool useRed,string category,int idleSeconds=120)
        {
            if(Busy)return;
            if(string.IsNullOrWhiteSpace(nickname)){message="请先在个人设置中填写昵称";return;}
            UseLocalHost();players=count;rounds=length;red=useRed;
            StartCoroutine(Send(new RequestBody{op="create",host=secret,name=nickname,n=count,rounds=length,idleSeconds=idleSeconds,red=useRed,category=category,lobbyFlow=true}));
        }
        public void LobbyRecover(){if(Busy)return;UseLocalHost();StartCoroutine(Send(new RequestBody{op="recoverHost",host=secret}));}
        public void LobbyJoin(string address,string code,string restore)
        {
            if(Busy)return;endpoint=address;pin=code;recovery=restore;JoinRoom();
        }
        public void LobbyCommand(string operation,int seat=0)
        {
            if(Busy||State?.phase!="lobby")return;
            StartCoroutine(Send(new RequestBody{op=operation,seat=seat}));
        }
        public void LobbyReady(bool ready){if(!Busy&&State?.phase=="lobby")StartCoroutine(Send(new RequestBody{op="lobbyReady",ready=ready}));}
        public void LobbyLeave()
        {
            if(Busy||State?.phase!="lobby")return;string oldEndpoint=endpoint;
            StartCoroutine(Send(new RequestBody{op=State.host?"closeLobby":"leaveLobby"},()=>{
                ForgetSession();EnterHome();lobby.Home();
            }));
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
        bool ResultButton(Rect r,string value)
        {
            var a=GUI.matrix.MultiplyPoint3x4(new Vector3(r.x,r.y,0));var b=GUI.matrix.MultiplyPoint3x4(new Vector3(r.xMax,r.yMax,0));
            uiButtons.Add(Rect.MinMaxRect(a.x,a.y,b.x,b.y));
            bool enabled=GUI.enabled;GUI.enabled&=!Busy;
            bool pressed=GUI.Button(r,"",GUIStyle.none);GUI.enabled=enabled;return pressed;
        }
        void Panel(Rect r)
        {
            Fill(r, new Color(.48f, .4f, .27f, .95f));
            Fill(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), new Color(.045f, .065f, .10f, .95f));
        }
        Texture2D Skin(string name)
        {
            if (!skin.TryGetValue(name, out var texture)) { texture = Resources.Load<Texture2D>("Majsoul/UI/" + name)??Resources.Load<Texture2D>("Majsoul/TablePolish/"+name)??Resources.Load<Texture2D>("Majsoul/TableLook/Sprites/"+name); skin[name] = texture; }
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
            OriginalTiles.InGui = true;
            try { DrawGui(); } finally { OriginalTiles.InGui = false; }
            if(active&&State?.phase!="lobby"&&!introduction.Active)lobby.DrawTableOverlay();
            tap.Render();
        }
        void DrawGui()
        {
            if(introduction.Active){introduction.Draw();return;}
            Styles(); uiButtons.Clear(); var previous = GUI.matrix;
            CurrentWaitPanel=new Rect();CurrentWaitButton=new Rect();waitButtonScreenRect=new Rect();
            if(State?.phase!="result")resultDisplay.Reset();
            bool playing = active && State != null && (State.phase == "play" || State.phase == "reaction" || (State.phase == "result" && effects.ShowWinPresentation));
            viewport = TableViewport.Fit(Screen.width, Screen.height, Screen.safeArea, playing);
            GUI.matrix = viewport.Matrix;
            bool beforeSettings = GUI.enabled; GUI.enabled &= page != "settings";
            if (State == null || !active) DrawHome();
            else if (State.phase == "lobby") DrawLobby();
            else if (State.phase == "result" && !effects.ShowWinPresentation) {bool enabled=GUI.enabled;GUI.enabled&=!lobby.OverlayActive;DrawResult();GUI.enabled=enabled;}
            else {bool enabled=GUI.enabled;GUI.enabled&=!lobby.OverlayActive;DrawTableUI();GUI.enabled=enabled;}
            GUI.enabled = beforeSettings;
            // Dialogs retain their own centered, uniformly scaled canvas.
            GUI.matrix = TableViewport.Fit(Screen.width, Screen.height, Screen.safeArea, false).Matrix;
            if (!string.IsNullOrEmpty(message)) { Panel(new Rect(190, 658, 900, 52)); GUI.Label(new Rect(205, 670, 870, 32), message, small); }
            if (showRoom) DrawRoom();
            if (page == "settings") DrawSettings();
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
            lobby.Draw();
        }
        void DrawLobby()
        {
            lobby.Draw();
        }
        void DrawTableUI(bool passive=false)
        {
            float w = viewport.Width, h = viewport.Height;
            if (SkinButton(new Rect(w - 175, 20, 40, 40), "replay_collect", "邀请好友")) lobby.OpenInvitation(true);
            if (SkinButton(new Rect(w - 117, 20, 40, 40), "btn_settings", "设置")) LobbyOpenSettings();
            if (SkinButton(new Rect(w - 59, 20, 40, 40), "btn_leave", "退出牌局")) { lobby.ShowMatchExit(); return; }
            // Original UI_DesktopInfo top-left node coordinates, uniformly scaled.
            const float k = .55f, ox = 34, oy = .6f;
            GUI.DrawTexture(new Rect(ox + 11*k, oy + 28*k, 313*k, 158*k), Skin("left_top"), ScaleMode.ScaleToFit);
            for (int i = 0; i < 5; i++)
            {
                Rect tile = new Rect(ox + (28+i*56)*k, oy + 44*k, 54*k, 81*k);
                string indicator=i<State.indicators.Length?State.indicators[i]:null;
                var preview=table.DoraTexture(indicator,table.IsMatchingTile(indicator));if(preview!=null)GUI.DrawTexture(tile,preview,ScaleMode.ScaleToFit);
            }
            HudNumber(State.sticks, ox + 117*k, oy + 139*k, k);
            HudNumber(State.honba, ox + 256*k, oy + 139*k, k);
            // The brush font's ascenders exceed its line box; never clip the caption.
            if(roomCaption==null){roomCaption=new GUIStyle(centered){font=Resources.Load<Font>("Majsoul/Settlement/round-font-chs"),fontSize=24,clipping=TextClipping.Overflow};namePlateText=new GUIStyle(centered){font=Resources.Load<Font>("Majsoul/TablePolish/name-font"),fontSize=11};}
            // container_lefttop/gamemode: bg_type band (23,186,290,51); lb_mode 309 wide, colour (.91,.686,.443).
            var band=new Rect(ox + 23*k, oy + 186*k, 290*k, 51*k);GUI.DrawTexture(band, Skin("bg_type"), ScaleMode.StretchToFill);
            string mode=State.lobby?.category=="ranked"?"段位场":State.lobby?.category=="competition"?"比赛场":"友人场";
            ShadowLabel(new Rect(band.center.x - 309*k/2, band.y, 309*k, band.height), mode+" · " + (State.n == 3 ? "三人" : "四人") + (State.rounds == 1 ? "东" : State.rounds==4?"整庄":"南"), roomCaption, new Color(.91f, .686f, .443f));
            foreach (var p in State.players.Select((v, i) => new { v, i }))
            {
                int seat = table.RelativeSeat(p.i);
                Vector2 anchor = seat == 0 ? new Vector2(w * .26f, h * .83f - 104) : seat == State.n - 1 ? new Vector2(64, h * .265f) : seat == 1 && State.n == 4 ? new Vector2(w - 132, h * .52f) : new Vector2(w * .676f, 26);
                bool turn = State.turn == p.i;
                GUI.DrawTexture(new Rect(anchor.x, anchor.y, 62, 62), table.Portrait(p.v), ScaleMode.ScaleAndCrop);
                PortraitFrame(new Rect(anchor.x - 3, anchor.y - 3, 68, 68), turn);
                if(p.v.takeoverMask)NativeTakeoverBadge.Draw(new Rect(anchor.x,anchor.y,62,62));
                // UI_DesktopInfo container_name: bg_name_1 180x30 x.9, label 32pt x.7 x.9;
                // title 208x40 x.9 directly below. Same .55 canvas factor as the dora panel.
                float cx=anchor.x+31;var nameRect=new Rect(cx-162*k/2,anchor.y+66,162*k,27*k);GUI.DrawTexture(nameRect,Skin("bg_name_1"),ScaleMode.StretchToFill);
                namePlateText.fontSize=p.v.name.Length>11?9:p.v.name.Length>8?10:11;ShadowLabel(nameRect,p.v.name,namePlateText,Color.white);
                // Players without a title show the official extendRes/title/notitle image.
                GUI.DrawTexture(new Rect(cx-187*k/2,nameRect.yMax,187*k,36*k),Skin("notitle"),ScaleMode.ScaleToFit);
            }
            if(passive)return;
            DrawAutoPlay(w,h);
            string status = State.phase == "result" ? State.result.title : State.phase == "reaction" ? (State.options.Length > 0 ? "请选择操作" : "等待其他玩家响应") : State.turn == State.you ? "轮到你出牌" : State.players[State.turn].name + " 的回合";
            // With choices the centred button row speaks for itself; it occupies this band.
            // No response-waiting overlay on the table.
            if (RiichiMode) GUI.Label(new Rect(w * .5f - 210, h - 105, 420, 22), "选牌后再点一次 · 立直打出", centered);
            var bounds=table.HandScreenBounds();
            var top=viewport.FromScreen(new Vector2(bounds.xMin,bounds.yMax),Screen.height);
            var bottom=viewport.FromScreen(new Vector2(bounds.xMax,bounds.yMin),Screen.height);
            CurrentFuritenPanel=new Rect();
            if(CurrentFuriten && bounds.width>0){
                var badge=TableIndicators.Art("pic_zhengting");float bh=Mathf.Clamp(bottom.y-top.y,55,76),bw=bh*badge.width/badge.height;
                CurrentFuritenPanel=new Rect(top.x-bw-4,(top.y+bottom.y-bh)/2,bw,bh);
                TableIndicators.Image("pic_zhengting",CurrentFuritenPanel);
            }
            CurrentWaitButton=new Rect();waitButtonScreenRect=new Rect();
            if(WaitIndicatorVisible)DrawWaitButton(new Rect(23,h-157,64,64));
            CurrentWaitPanel=new Rect();
            if(CurrentWaitDetails.Length>0 && bounds.width>0){
                CurrentWaitPanel=TableIndicators.WaitRect(CurrentWaitDetails.Length,new Vector2((top.x+bottom.x)/2,0),top.y-10,w*.70f);
                TableIndicators.Waits(table,CurrentWaitDetails,CurrentWaitPanel);
            }
            DrawResponseMarker();
            if(!AutoPlaying)DrawActionButtons(w,h);
            effects.DrawCall(w,h,State.n);
            allLast.Draw(w,h);
        }
        void DrawWaitButton(Rect r)
        {
            CurrentWaitButton=r;
            var a=GUI.matrix.MultiplyPoint3x4(new Vector3(r.x,r.y,0));var b=GUI.matrix.MultiplyPoint3x4(new Vector3(r.xMax,r.yMax,0));
            waitButtonScreenRect=Rect.MinMaxRect(a.x,a.y,b.x,b.y);uiButtons.Add(waitButtonScreenRect);
            var icon=new Rect(r.x+6,r.y,52,48);
            GUI.DrawTexture(icon,Skin(waitHoldVisible?"ob_ting_on_btn":"ob_ting_off_btn"),ScaleMode.ScaleToFit);
            NativeUiChrome.CaptionBar(new Rect(r.x,r.y+44,64,16));
            var style=new GUIStyle(centered){font=Resources.Load<Font>("Majsoul/Lobby/Fonts/advanced"),fontSize=13,fontStyle=FontStyle.Normal};style.normal.textColor=new Color(.69804f,.73333f,.80784f);
            GUI.Label(new Rect(r.x,r.y+41,64,22),"听牌",style);
            GUI.Label(r,new GUIContent("",waitHoldVisible?"点击收起听牌":"点击查看听牌"),GUIStyle.none);
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
            // UI_ChiPengHu / UI_LiqiZimo container_btns: (-317,-230) from the centre, scale .7;
            // 384x186 buttons, pivot (.5,.2), filled from the container's right edge.
            float u=Mathf.Min(width/1920f,height/1080f),bw=384*.7f*u,bh=186*.7f*u,py=height/2+(230+(75+55.8f)*.7f)*u;
            return Enumerable.Range(0,count).Select(i=>{int slot=count-1-i;float px=width/2+(-317+(1152-192-384*slot)*.7f)*u;return new Rect(px-bw/2,py-bh*.8f,bw,bh);}).ToArray();
        }
        void ChooseActionKind(string kind)
        {
            if(Busy || !OptionsReady)return;
            var choices=State.options.Where(x=>x.kind==kind).ToArray();
            if(choices.Length==1)Act(choices[0].id);
            else if(choices.Length>1)choosingKind=choosingKind==kind?null:kind;
        }
#if UNITY_EDITOR
        public bool ValidationWaitPointer(Vector2 canvasPoint,bool down,bool held,bool up,float now)
        {
            validationWaitInput=true;var point=viewport.Matrix.MultiplyPoint3x4(canvasPoint);
            return UpdateWaitPointer(new Vector2(point.x,point.y),down,held,up,now);
        }
        public void ValidationEndWaitPointer(){validationWaitInput=false;ResetWaitPress();}
        public string ValidationChoosingKind => choosingKind;
        public void ValidationChooseActionKind(string kind) => ChooseActionKind(kind);
        public void ValidationToggleRiichi(bool on) { RiichiMode = on; SelectedIndex = -1; SelectedAction = null; table.Render(State); }
#endif
        void DrawActionButtons(float width,float height)
        {
            if(!OptionsReady)return;
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
            string previewKind=choosingKind??(State.options.Any(o=>o.kind=="chi")?"chi":null);
            if(previewKind==null)return;
            var choices=State.options.Where(x=>x.kind==previewKind).ToArray();if(choices.Length==0)return;
            float choiceWidth=134;float total=Mathf.Min(width*.82f,choices.Length*choiceWidth);
            // Keep the combination panel above the button row, right-aligned with it.
            float x=Mathf.Max(28,rects[rects.Length-1].xMax-total),y=rects[0].y-90;
            TableIndicators.Panel(new Rect(x-28,y-24,total+56,112),containTiles:true);
            for(int i=0;i<choices.Length;i++)
            {
                Rect r=new Rect(x+i*total/choices.Length,y,total/choices.Length-4,66);
                var a=GUI.matrix.MultiplyPoint3x4(new Vector3(r.x,r.y,0));var b=GUI.matrix.MultiplyPoint3x4(new Vector3(r.xMax,r.yMax,0));uiButtons.Add(Rect.MinMaxRect(a.x,a.y,b.x,b.y));
                var meld=choices[i].meld;var digits=meld.Skip(1).Where(char.IsDigit).ToArray();float tw=Mathf.Min(38,(r.width-8)/Mathf.Max(1,digits.Length));
                for(int j=0;j<digits.Length;j++)GUI.DrawTexture(new Rect(r.center.x-digits.Length*tw/2+j*tw,r.y+5,tw-1,tw*1.5f),table.HandTexture(meld[0].ToString()+digits[j]),ScaleMode.ScaleToFit);
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
            var resultMatrix=GUI.matrix;var resultColor=GUI.color;
            {
                viewport=TableViewport.Fit(Screen.width,Screen.height,Screen.safeArea,true);GUI.matrix=viewport.Matrix;
                bool enabled=GUI.enabled;GUI.enabled=false;DrawTableUI(true);GUI.enabled=enabled;
                viewport=TableViewport.Fit(Screen.width,Screen.height,Screen.safeArea,false);GUI.matrix=resultMatrix;
            }
#if UNITY_EDITOR
            if(ValidationTableOnly)return;
#endif
            GUI.matrix=Matrix4x4.identity;GUI.color=new Color(0,0,0,resultDisplay.ShowingFinal ? .72f : State.result.draw ? .38f : .68f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=resultColor;GUI.matrix=resultMatrix;
            var action=resultDisplay.Draw(State,table,effects,font,ResultButton);
#if UNITY_EDITOR
            if(SessionPreviewOnly)return;
#endif
            if(action==ResultPresentation.Action.Ready)StartCoroutine(Send(new RequestBody{op="ready",handSeq=State.handSeq}));
            else if(action==ResultPresentation.Action.Next)StartCoroutine(Send(new RequestBody{op="next"}));
            else if(action==ResultPresentation.Action.Home)EndCurrentMatch();
        }
        void DrawRoom()
        {
            Panel(new Rect(270, 120, 740, 450)); GUI.Label(new Rect(305, 145, 650, 50), "房间信息", title);
            GUI.Label(new Rect(305, 210, 650, 60), "房间号：" + State?.pin + "\n可在同一局域网下通过房间号加入。", label);
            GUI.Label(new Rect(305, 295, 650, 100), string.IsNullOrEmpty(addresses) ? endpoint : addresses, label);
            if (Button(new Rect(305, 455, 210, 55), "关闭")) showRoom = false;
            if (State != null && Button(new Rect(535, 455, 210, 55), "退出牌局")) { showRoom = false; lobby.ShowMatchExit(); }
        }
        void DrawSettings()
        {
            nativeSettings.Draw(()=>page="home");
        }
        void ApplyAudioSettings() {
            if(audioSettingsRevision==AppPreferences.Revision)return;audioSettingsRevision=AppPreferences.Revision;
            float speech=AppPreferences.Gain(4),sfx=AppPreferences.Gain(2);
            voice.Configure(speech>0,speech);settlementAudio.Configure(speech>0,speech);
            gameAudio.Configure(false,0,sfx>0,sfx);
        }
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
        void OnApplicationPause(bool pause) { if(pause){introduction.Reset();resultDisplay.ClearEffects();ResetWaitPress();tap.Clear();}voice.Suspend(pause);settlementAudio.Suspend(pause);gameAudio.Suspend(pause);if(pause)table.Opening.Cancel();effects.Reset();SessionPause(pause); }
        void OnDestroy() { introduction.Reset();resultDisplay.Reset();yakuman.Reset();bridge?.Dispose(); }
    }
}
