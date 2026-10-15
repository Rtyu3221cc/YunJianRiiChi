using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;
namespace YunJian.Riichi.Editor
{
    // Lobby-only release smoke checks. Never invokes start, a game action,
    // ProjectValidation or PlayValidation; Android phone testing is separate.
    [InitializeOnLoad] public static class LobbyReleaseValidation
    {
        const string Key="Riichi.LobbyReleaseValidation";
        static LobbyReleaseValidation(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        static void Changed(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){
                SessionState.SetBool(Key,false);UnityEngine.Object.FindFirstObjectByType<RiichiClient>().gameObject.AddComponent<LobbyReleaseChecks>();
            }
        }
    }
    public sealed class LobbyReleaseChecks : MonoBehaviour
    {
        string Host=>Environment.GetEnvironmentVariable("RIICHI_HOST_SECRET")??"lobby-release-0541";
        RiichiClient client;LobbyController lobby;RoomReply reply;int checks;string output;
        bool holdingPoll;FieldInfo nextPoll,pressField,goingHomeField;
        void Update(){if(holdingPoll)nextPoll.SetValue(client,float.PositiveInfinity);}
        void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
        IEnumerator Start()
        {
            output=Path.GetFullPath("../build/lobby-release-0.5.41");Directory.CreateDirectory(output);
            foreach(string name in new[]{"passed.txt","failed.txt"}){var file=Path.Combine(output,name);if(File.Exists(file))File.Delete(file);}
            client=GetComponent<RiichiClient>();lobby=client.Lobby;client.BeginLobbyValidation(Host);
            typeof(RiichiClient).GetField("savedSession",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(client,null);
            nextPoll=typeof(RiichiClient).GetField("nextPoll",BindingFlags.Instance|BindingFlags.NonPublic);
            pressField=typeof(LobbyController).GetField("validationPress",BindingFlags.Instance|BindingFlags.NonPublic);
            goingHomeField=typeof(LobbyController).GetField("goingHome",BindingFlags.Instance|BindingFlags.NonPublic);
            var stack=new Stack<IEnumerator>();stack.Push(RunChecks());
            while(stack.Count>0){object value=null;try{
                var iterator=stack.Peek();if(!iterator.MoveNext()){stack.Pop();continue;}value=iterator.Current;
                if(value is IEnumerator child){stack.Push(child);continue;}
            }catch(Exception error){File.WriteAllText(Path.Combine(output,"failed.txt"),error.ToString());Debug.LogException(error);EditorApplication.Exit(1);yield break;}yield return value;}
            string report=checks+" lobby-only checks passed. Scope: asynchronous entry routes, create options, keypad, help/rules/yaku, live waiting-room create/join/original-token resume/readiness/bots/kick/leave/close, loopback discovery. No start/gameplay/table regression or physical Android/hotspot testing.";
            File.WriteAllText(Path.Combine(output,"passed.txt"),report);Debug.Log("LOBBY RELEASE ONLY PASS: "+report);EditorApplication.Exit(0);
        }
        IEnumerator Press(string id)
        {
            // Keep the real polling path between steps, but serialize each UI
            // press so a slow batch frame cannot let a state poll consume it.
            holdingPoll=true;nextPoll.SetValue(client,float.PositiveInfinity);
            try{
                yield return Idle();yield return Until(()=>ControlReady(id),"Lobby control never rendered inside screen: "+id);var r=lobby.ValidationTarget(id);Check(r.width>0,"Missing lobby control: "+id);
                Check(r.xMin>=-.1f&&r.yMin>=-.1f&&r.xMax<=Screen.width+.1f&&r.yMax<=Screen.height+.1f,"Lobby control outside screen: "+id+" "+r);
                var beforePage=lobby.Page;string beforeModal=lobby.Modal;lobby.ValidationPress(id);
                yield return Until(()=>pressField.GetValue(lobby)==null,"Lobby control never accepted a press: "+id);yield return Idle();
                if(id=="back"||id=="modeBack"){
                    if(beforeModal!="")yield return Until(()=>lobby.Modal=="","Back did not finish closing modal");
                    else if(beforePage==LobbyController.ScreenPage.Room)yield return Until(()=>lobby.Modal=="leave","Room back did not open its confirmation");
                    else yield return Until(HomeComplete,"Back did not finish returning home");
                }else if((id=="session-primary"&&beforeModal=="match-exit")||(id=="confirm"&&beforeModal=="leave")){
                    yield return Until(()=>!client.LobbyActive&&HomeComplete(),"Confirmed exit did not finish returning home");
                }else if(id=="session-secondary"&&beforeModal=="session"){
                    yield return Until(()=>!client.LobbyActive&&lobby.Page==LobbyController.ScreenPage.Modes&&lobby.Modal=="","Pending-match exit did not reach room choices");
                }else if(id=="session-primary"&&beforeModal=="session"){
                    yield return Until(()=>client.LobbyActive&&lobby.Modal=="","Continue match did not finish resuming");
                }else if(id=="closeModal"||id=="confirm"||id.EndsWith("-confirm")||id=="session-secondary"){
                    yield return Until(()=>lobby.Modal=="","Modal did not finish closing after "+id);
                }
            }finally{holdingPoll=false;nextPoll.SetValue(client,Time.unscaledTime+.65f);}
        }
        bool ControlReady(string id){var r=lobby.ValidationTarget(id);return r.width>0&&r.xMin>=-.1f&&r.yMin>=-.1f&&r.xMax<=Screen.width+.1f&&r.yMax<=Screen.height+.1f;}
        bool HomeComplete()=>lobby.Visible&&lobby.Page==LobbyController.ScreenPage.Home&&lobby.Modal==""&&!(bool)goingHomeField.GetValue(lobby);
        IEnumerator Home(){lobby.Home();yield return Until(HomeComplete,"Direct Home transition did not finish");}
        IEnumerator RecoverHost(){
            holdingPoll=true;nextPoll.SetValue(client,float.PositiveInfinity);
            try{yield return Idle();client.LobbyRecover();yield return Idle();yield return Until(()=>client.State?.host==true&&client.State.phase=="lobby","Host recovery did not reach the waiting room");}
            finally{holdingPoll=false;nextPoll.SetValue(client,Time.unscaledTime+.65f);}
        }
        IEnumerator Idle(){yield return Until(()=>!client.Busy,"Lobby API timed out");yield return null;}
        IEnumerator Until(Func<bool> condition,string message){float deadline=Time.realtimeSinceStartup+18;while(!condition()&&Time.realtimeSinceStartup<deadline)yield return null;Check(condition(),message+" [page="+lobby.Page+", modal="+lobby.Modal+", phase="+client.State?.phase+", busy="+client.Busy+"]");}
        IEnumerator Shot(string name){yield return new WaitForSecondsRealtime(.22f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.12f);}
        IEnumerator Api(RequestBody body,bool expected=true)
        {
            using(var web=new UnityWebRequest("http://127.0.0.1:8765/api","POST")){
                web.uploadHandler=new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));web.downloadHandler=new DownloadHandlerBuffer();web.SetRequestHeader("Content-Type","application/json");web.timeout=10;
                yield return web.SendWebRequest();Check(web.result==UnityWebRequest.Result.Success,"Dedicated lobby host HTTP error");reply=JsonUtility.FromJson<RoomReply>(web.downloadHandler.text);if(expected)Check(reply.ok,reply.error);
            }
        }
        IEnumerator ResumeSeat(RoomView view,string credential)
        {
            yield return Idle();
            typeof(RiichiClient).GetMethod("EnterHome",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(client,null);yield return Home();
            // This is the original connection record, not a recovery-code form.
            typeof(RiichiClient).GetMethod("CacheSession",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(client,new object[]{view,"http://127.0.0.1:8765",credential});
            yield return Idle();client.ResumeSession();yield return Idle();yield return Until(()=>client.LobbyActive,"Original-token seat resume failed");
        }
        IEnumerator RunChecks()
        {
            yield return Api(new RequestBody{op="recoverHost",host=Host},false);Check(!reply.ok,"Dedicated release host must start with an empty state");
            foreach(var size in new[]{new Vector2(1688,780),new Vector2(1280,720)}){
                var win=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));win.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(win,new object[]{size,"Lobby-only release checks"});win.Repaint();for(int i=0;i<8;i++)yield return null;
                yield return Home();yield return Press("friend");yield return Idle();yield return Until(()=>lobby.Page==LobbyController.ScreenPage.Modes&&lobby.Modal=="","Friend session inspection failed");yield return new WaitForSecondsRealtime(.5f);
                yield return Press("help0");Check(lobby.Modal=="friend-help","Create explanation missing");yield return Press("friend-help-confirm");
                yield return Press("help1");Check(lobby.Modal=="friend-help","Join explanation missing");yield return Press("closeModal");
                yield return Press("mode1");Check(lobby.Modal=="join","Join keypad missing");
                foreach(int n in new[]{0,1,2,3,4,5})yield return Press("digit"+n);
                var number=typeof(LobbyController).GetField("number",BindingFlags.Instance|BindingFlags.NonPublic);Check((string)number.GetValue(lobby)=="123456","Keypad digits incorrect");
                yield return Press("digit11");Check((string)number.GetValue(lobby)=="12345","Keypad backspace failed");yield return Press("digit9");Check((string)number.GetValue(lobby)=="","Keypad clear failed");yield return Shot("join-"+(int)size.x);yield return Press("closeModal");
                yield return Press("mode0");Check(lobby.Page==LobbyController.ScreenPage.Create&&lobby.Advanced,"Create must expand advanced settings by default");
                yield return Press("unlimited");yield return Press("unlimited");Check(lobby.Modal==""&&lobby.Advanced,"Unlimited time must remain selected");
                yield return Press("advancedOff");Check(!lobby.Advanced,"Advanced hide failed");yield return Press("advancedOn");Check(lobby.Advanced,"Advanced expand failed");
                yield return Press("rules");Check(lobby.Modal=="rules","Create rules missing");yield return Press("ruleMode1");yield return Press("ruleTab1");yield return Shot("rules-"+(int)size.x);yield return Press("closeModal");
                yield return Press("back");Check(lobby.Page==LobbyController.ScreenPage.Home,"Create back route failed");
            }
            foreach(string category in new[]{"ranked","competition"}){
                yield return Home();yield return Press(category);yield return Idle();yield return Until(()=>lobby.Page==LobbyController.ScreenPage.Modes&&lobby.Modal=="","Named category session inspection failed");yield return new WaitForSecondsRealtime(.5f);Check(lobby.Category==category,"Wrong lobby category");yield return Press("modeBack");
            }
            lobby.OpenCreate();yield return Press("three");yield return Press("east");
            lobby.DesignPreviewAdvancedTop();yield return null;yield return Press("number-initial");Check(lobby.Modal=="advanced-help","Fixed standard initial points must explain the restriction");yield return Press("advanced-help-confirm");
            lobby.DesignPreviewAdvancedLower();yield return null;yield return Press("advanced-red-0");Check(!lobby.Red,"No-red option failed");
            yield return Press("help-kuitan");Check(lobby.Modal=="advanced-help","Kuitan explanation missing");yield return Shot("kuitan-help");yield return Press("advanced-help-confirm");
            yield return Press("create");yield return Idle();yield return Until(()=>client.State?.phase=="lobby","Create did not reach waiting room");
            Check(client.State.n==3&&client.State.rounds==1&&client.State.lobby.red==false,"Room settings differ from selected form");Check(!lobby.CanStart,"Empty room permits start");yield return Shot("room-three");
            using(var locator=new LanRoomDiscovery()){
                var task=locator.Find(client.State.pin,System.Threading.CancellationToken.None);while(!task.IsCompleted)yield return null;
                Check(!task.IsFaulted&&RiichiClient.ValidEndpoint(task.Result),"Loopback discovery failed");
            }
            yield return Press("rules");Check(lobby.Modal=="rules","Waiting-room rules route failed");yield return Shot("room-rules");yield return Press("closeModal");
            yield return Press("yaku");Check(lobby.Modal=="yaku","Yaku button missing");yield return Press("yaku-confirm");
            yield return Press("bot1");yield return Idle();Check(client.State.players.Length==2,"Bot was not added");yield return Press("kick1");yield return Idle();Check(client.State.players.Length==1&&lobby.Modal=="","Bot removal must act directly");
            yield return Api(new RequestBody{op="join",name="大厅专项牌友",pin=client.State.pin});string guest=reply.token;var guestView=reply.state;
            yield return Until(()=>client.State.players.Length==2,"Host did not see guest join");Check(!client.State.players[1].ready,"Guest incorrectly auto-ready");
            yield return ResumeSeat(guestView,guest);Check(!client.State.host&&client.State.you==1,"Guest original-token resume failed");
            yield return Press("ready");yield return Idle();Check(client.State.players[1].ready,"Guest ready failed");yield return Press("ready");yield return Idle();Check(!client.State.players[1].ready,"Guest unready failed");
            yield return Press("ready");yield return Idle();yield return RecoverHost();Check(client.State.host&&client.State.players[1].ready,"Host recover lost readiness");
            yield return Press("kick1");yield return Idle();Check(client.State.players.Length==1,"Human kick failed");yield return Api(new RequestBody{op="state",token=guest});Check(reply.state.phase=="join","Kicked guest retained seat");
            yield return Press("bot1");yield return Idle();yield return Press("bot2");yield return Idle();Check(lobby.CanStart&&client.State.phase=="lobby","Full ready room state incorrect");yield return Shot("room-ready-no-start");
            yield return Press("back");Check(lobby.Modal=="leave","Host dissolve confirmation missing");yield return Shot("host-close");yield return Press("confirm");yield return Idle();Check(!client.LobbyActive,"Host did not dissolve waiting room");
            lobby.OpenCreate();yield return Press("four");yield return Press("south");lobby.DesignPreviewAdvancedLower();yield return null;yield return Press("advanced-red-1");yield return Press("create");yield return Idle();
            Check(client.State.n==4&&client.State.rounds==2&&client.State.lobby.red,"Four-player form settings incorrect");yield return Shot("room-four");
            yield return Api(new RequestBody{op="join",name="退房专项牌友",pin=client.State.pin});yield return ResumeSeat(reply.state,reply.token);yield return Press("back");Check(lobby.Modal=="leave","Guest leave confirmation missing");yield return Shot("guest-leave");yield return Press("confirm");yield return Idle();Check(!client.LobbyActive,"Guest did not return home");
            yield return RecoverHost();Check(client.State.players.Length==1,"Guest leave did not release seat");yield return Press("back");yield return Press("confirm");yield return Idle();Check(!client.LobbyActive&&lobby.Page==LobbyController.ScreenPage.Home,"Final close did not return home");
            yield return Shot("returned-home");
        }
    }
}
