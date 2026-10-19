using System;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace YunJian.Riichi
{
    public partial class RiichiClient
    {
        [Serializable] sealed class SavedSession
        {
            public string endpoint,token,pin,room,category,phase;
            public int round,n;
            public bool host;
        }
        SavedSession savedSession;
        public bool HasOngoingSession=>savedSession!=null&&new[]{"play","reaction","result"}.Contains(savedSession.phase);
        const string SessionKey="riichi.currentSession";
        string savedSessionJson="";
        bool resumingConnection;
        public bool AutoPlaying=>State!=null&&State.you>=0&&State.you<State.players.Length&&State.players[State.you].auto;
        public string PendingSessionDescription {
            get {
                var s=savedSession;if(s==null)return "当前无进行中的牌局";
                string category=s.category=="ranked"?"段位场":s.category=="competition"?"比赛场":"友人场";
                string round=s.phase=="lobby"?"准备中":"东南西北"[Mathf.Clamp(s.round/Mathf.Max(3,s.n),0,3)]+(s.round%Mathf.Max(3,s.n)+1).ToString()+"局"+(s.phase=="result"?"·结算中":"");
                return "当前有未结的对局\n（"+category+"-"+round+" 房间号"+s.pin+"）\n是否继续牌局？";
            }
        }
        void LoadSession()
        {
            if(validationSession)return;
            try{savedSessionJson=PlayerPrefs.GetString(SessionKey,"");savedSession=JsonUtility.FromJson<SavedSession>(savedSessionJson);}catch{savedSession=null;}
            if(savedSession!=null&&(!ValidEndpoint(savedSession.endpoint)||string.IsNullOrEmpty(savedSession.token)))savedSession=null;
            // Older releases already stored the endpoint and seat token, but
            // had no currentSession record. Preserve that connection on upgrade;
            // peek will supply the actual PIN/round before showing the dialog.
            if(savedSession==null)savedSession=LegacyConnection(endpoint,token);
        }
        static SavedSession LegacyConnection(string address,string credential)
        {return ValidEndpoint(address)&&!string.IsNullOrEmpty(credential)?new SavedSession{endpoint=address,token=credential,pin="",room="",phase="unknown",host=new Uri(address).IsLoopback}:null;}
        void CacheSession(RoomView view,string address,string credential)
        {
            if(view==null||view.phase=="empty"||view.phase=="join"||string.IsNullOrEmpty(credential))return;
            savedSession=new SavedSession{endpoint=address,token=credential,pin=view.pin,room=view.audioRoom,category=view.lobby?.category??"friend",phase=view.phase,round=view.round,n=view.n,host=view.host};
            string json=JsonUtility.ToJson(savedSession);
            if(!validationSession&&json!=savedSessionJson){savedSessionJson=json;PlayerPrefs.SetString(SessionKey,json);PlayerPrefs.Save();}
        }
        void ForgetSession()
        {
            if(savedSession!=null&&!validationSession)PlayerPrefs.DeleteKey("riichi.token:"+savedSession.endpoint);
            savedSession=null;savedSessionJson="";token="";resumingConnection=false;if(!validationSession){PlayerPrefs.DeleteKey(SessionKey);PlayerPrefs.Save();}
        }
        void ApplyRoomState(RoomView view,bool returning=false)
        {
            // JsonUtility can materialize JSON null classes as empty objects.
            if(view.resourceSetup!=null&&(view.resourceSetup.revision<=0||view.resourceSetup.choices==null||view.resourceSetup.players==null))view.resourceSetup=null;
            view.resourcePeers=view.resourcePeers??new ResourcePeer[0];foreach(var peer in view.resourcePeers)peer.hashes=peer.hashes??new string[0];
            bool handChanged=State!=null&&(State.audioRoom!=view.audioRoom||State.matchSeq!=view.matchSeq||State.handSeq!=view.handSeq);
            // Release references to the old table before Render replaces its tiles.
            if(handChanged){effects.Reset();voice.Reset();settlementAudio.Reset();gameAudio.ClearHandEffects();resultDisplay.Reset();ClearSelection();RiichiMode=false;choosingKind=null;}
            bool changed=handChanged||State==null||State.version!=view.version||State.phase!=view.phase;
            State=view;SettlementAudio.BindCharacters(view);ObserveWaitState();yakuman.Observe(State);
            introduction.Observe(State,validationSession);voice.Observe(State);gameAudio.SetScene(State);allLast.Observe(State);
            if(State.phase=="empty"||State.phase=="join"){active=false;page="home";table.Clear();return;}
            active=true;if(returning)page="home";
            if(!State.options.Any(x=>x.kind=="riichi"))RiichiMode=false;
            RebindSelection();if(changed)table.Render(State);effects.Accept(State);
            if(choosingKind!=null&&!State.options.Any(x=>x.kind==choosingKind))choosingKind=null;
            CacheSession(State,endpoint,token);
            if(returning&&State.phase!="lobby")introduction.Reenter(State);
        }
        // Session requests inspect credentials without activating the table.
        // Busy is owned by the surrounding operation, excluding background polls.
        IEnumerator SessionRequest(string address,RequestBody request,Action<RoomReply> done)
        {
            if(!ValidEndpoint(address)){done(new RoomReply{networkError=true,error="无效的局域网地址"});yield break;}
            request.id=Guid.NewGuid().ToString("N");request.version=State?.version??-1;
            using(var web=new UnityWebRequest(address+"/api","POST")){
                web.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(request)));web.downloadHandler=new DownloadHandlerBuffer();
                web.SetRequestHeader("Content-Type","application/json");web.timeout=5;web.redirectLimit=0;
                yield return web.SendWebRequest();
                if(web.result!=UnityWebRequest.Result.Success){done(new RoomReply{networkError=true,error=web.error});yield break;}
                RoomReply reply;try{reply=JsonUtility.FromJson<RoomReply>(web.downloadHandler.text);}catch{reply=null;}
                done(reply??new RoomReply{error="无效响应"});
            }
        }
        IEnumerator ContactSaved(RequestBody request,Action<RoomReply> done)
        {
            var saved=savedSession;if(saved==null){done(new RoomReply());yield break;}
            if(saved.host){saved.endpoint="http://127.0.0.1:8765";
#if UNITY_ANDROID && !UNITY_EDITOR
                if(bridge!=null)yield return WaitForHost();
#endif
            }
            request.token=saved.token;request.room=saved.room;RoomReply reply=null;
            yield return SessionRequest(saved.endpoint,request,r=>reply=r);
            if(reply.networkError&&!saved.host){
                var find=lobby.FindRoomEndpoint(saved.pin);while(!find.IsCompleted)yield return null;
                if(!find.IsFaulted&&!find.IsCanceled&&!string.IsNullOrEmpty(find.Result)&&find.Result!=saved.endpoint){saved.endpoint=find.Result;yield return SessionRequest(saved.endpoint,request,r=>reply=r);}
            }
            done(reply);
        }
        public void InspectSession(string category=null){if(!Busy)StartCoroutine(InspectSessionRoutine(category));}
        IEnumerator InspectSessionRoutine(string category)
        {
            Busy=true;generation++;
            try{
                RoomReply reply=null;bool hadSession=savedSession!=null;
                if(hadSession)yield return ContactSaved(new RequestBody{op="peek"},r=>reply=r);
                else if(!string.IsNullOrEmpty(secret))yield return SessionRequest("http://127.0.0.1:8765",new RequestBody{op="hostSession",host=secret},r=>reply=r);
                if(reply?.ok==true&&reply.state!=null&&reply.state.phase!="empty"&&reply.state.phase!="join"){
                    CacheSession(reply.state,savedSession?.endpoint??"http://127.0.0.1:8765",string.IsNullOrEmpty(reply.token)?savedSession?.token:reply.token);lobby.ShowPendingSession();
                }else if(hadSession&&savedSession.host&&reply?.networkError==true)lobby.ShowMediumNotice("牌局恢复","本机牌局服务暂未就绪，请稍后重试。");
                else if(hadSession){ForgetSession();if(category!=null)lobby.OpenCategoryDirect(category);lobby.ShowSessionCancelled();}
                else if(category!=null)lobby.OpenCategoryDirect(category);
                else lobby.ShowMediumNotice("牌局","当前无进行中的牌局");
            }finally{Busy=false;}
        }
        public void ResumeSession(){if(!Busy)StartCoroutine(ResumeSessionRoutine());}
        IEnumerator ResumeSessionRoutine()
        {
            Busy=true;generation++;
            try{
                RoomReply reply=null;yield return ContactSaved(new RequestBody{op="resume"},r=>reply=r);
                if(reply?.ok!=true||reply.state==null||reply.state.phase=="empty"||reply.state.phase=="join"){
                    if(savedSession?.host==true&&reply?.networkError==true){lobby.ShowMediumNotice("牌局恢复","本机牌局服务暂未就绪，请稍后重试。");yield break;}
                    ForgetSession();lobby.ShowSessionCancelled();yield break;
                }
                endpoint=savedSession.endpoint;token=savedSession.token;
                lobby.CloseModal();yield return new WaitForSecondsRealtime(.15f);
                effects.Reset();resultDisplay.Reset();ApplyRoomState(reply.state,true);nextPoll=Time.unscaledTime+.65f;
            }finally{Busy=false;}
        }
        public void EndCurrentMatch(string continueCategory=null){if(!Busy)StartCoroutine(EndMatchRoutine(continueCategory));}
        IEnumerator EndMatchRoutine(string continueCategory)
        {
            Busy=true;generation++;
            try{
                if(savedSession==null&&State!=null)CacheSession(State,endpoint,token);
                RoomReply reply=null;yield return ContactSaved(new RequestBody{op="leaveMatch"},r=>reply=r);
                if(reply?.networkError==true&&active){lobby.ShowMediumNotice("退出牌局","暂时无法连接房主，请稍后重试。");yield break;}
                ForgetSession();EnterHome();lobby.Home();if(continueCategory!=null)lobby.OpenCategoryDirect(continueCategory);
                if(reply?.ok!=true)lobby.ShowSessionCancelled();
            }finally{Busy=false;}
        }
        void SessionPause(bool paused)
        {
            if(!active||State==null||State.phase=="lobby")return;
            if(!paused){resumingConnection=true;return;}
            if(!Busy)StartCoroutine(Send(new RequestBody{op="presence",present=false}));
        }
        void DrawAutoPlay(float width,float height)
        {
            if(State.phase!="play"&&State.phase!="reaction")return;
            var bounds=new Rect(23,height-79,64,64);
            var icon=new Rect(bounds.x+6,bounds.y,52,48);GUI.DrawTexture(icon,Skin(AutoPlaying?"ob_ting_on_btn":"ob_ting_off_btn"),ScaleMode.ScaleToFit);
            NativeUiChrome.CaptionBar(new Rect(bounds.x,bounds.y+44,64,16));
            var caption=new GUIStyle(centered){font=Resources.Load<Font>("Majsoul/Lobby/Fonts/advanced"),fontSize=13,fontStyle=FontStyle.Normal};caption.normal.textColor=new Color(.69804f,.73333f,.80784f);GUI.Label(new Rect(bounds.x,bounds.y+41,64,22),"AI接管",caption);
            var a=GUI.matrix.MultiplyPoint3x4(bounds.position);var b=GUI.matrix.MultiplyPoint3x4(new Vector3(bounds.xMax,bounds.yMax,0));uiButtons.Add(Rect.MinMaxRect(a.x,a.y,b.x,b.y));
            bool enabled=GUI.enabled;GUI.enabled&=!Busy;
            if(GUI.Button(bounds,new GUIContent("",AutoPlaying?"点击收回操作":"点击启用AI接管"),GUIStyle.none)){ClearSelection();RiichiMode=false;StartCoroutine(Send(new RequestBody{op="autoPlay",enabled=!AutoPlaying}));}
            GUI.enabled=enabled;
        }
#if UNITY_EDITOR
        public void DesignPreviewPendingSession(RoomView fixture)
        {
            validationSession=true;SessionPreviewOnly=true;CacheSession(fixture,"http://127.0.0.1:8765","preview-seat-only");
            EnterHome();lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Home);
        }
        public void DesignPreviewSessionTable(RoomView fixture)
        {
            validationSession=true;SessionPreviewOnly=true;lobby.DesignPreviewClose();ShowValidationFixture(fixture);
        }
        public void DesignPreviewReentry(){introduction.Reenter(State);introduction.Freeze=true;}
#endif
    }
}
