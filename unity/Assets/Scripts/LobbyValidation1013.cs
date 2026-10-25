#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
namespace YunJian.Riichi
{
    // Editor-only render-callback automation. This is not an Android touch or
    // multi-phone hotspot test. Uses its own disposable desktop test room.
    public sealed class LobbyValidation1013 : MonoBehaviour
    {
        RiichiClient client;LobbyController lobby;RoomReply reply;int checks;string folder,hostSecret;
        bool holdingPoll;FieldInfo nextPoll,pressField,goingHomeField;
        void Update(){if(holdingPoll)nextPoll.SetValue(client,float.PositiveInfinity);}
        void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
        IEnumerator Start()
        {
            folder=Path.GetFullPath("../build/lobby-reference-1013/rendered");Directory.CreateDirectory(folder);
            foreach(var name in new[]{"failed.txt","passed.txt"}){var p=Path.Combine(folder,name);if(File.Exists(p))File.Delete(p);}
            client=GetComponent<RiichiClient>();lobby=client.Lobby;
            hostSecret=Environment.GetEnvironmentVariable("RIICHI_HOST_SECRET")??"lobby-validation-1013";
            client.BeginLobbyValidation(hostSecret);
            // Test identities stay in memory and must never inspect an operator's
            // cached phone session when asynchronous category entry runs.
            typeof(RiichiClient).GetField("savedSession",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(client,null);
            nextPoll=typeof(RiichiClient).GetField("nextPoll",BindingFlags.Instance|BindingFlags.NonPublic);
            pressField=typeof(LobbyController).GetField("validationPress",BindingFlags.Instance|BindingFlags.NonPublic);
            goingHomeField=typeof(LobbyController).GetField("goingHome",BindingFlags.Instance|BindingFlags.NonPublic);
            var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0){object next=null;try{var top=stack.Peek();if(!top.MoveNext()){stack.Pop();continue;}next=top.Current;if(next is IEnumerator nested){stack.Push(nested);continue;}}catch(Exception e){File.WriteAllText(Path.Combine(folder,"failed.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
            File.WriteAllText(Path.Combine(folder,"passed.txt"),checks+" actual Unity lobby checks: rendered pages, render-callback buttons, live room API, discovery loopback, bot/guest management, readiness/start, table handoff and clean return. Android touch/hotspot unverified.");
            Debug.Log("RIICHI LOBBY 1013 PASS: "+checks);EditorApplication.Exit(0);
        }
        static void Size(int w,int h){var win=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));win.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(win,new object[]{new Vector2(w,h),"Lobby 1013"});win.Repaint();}
        IEnumerator Shot(string name){if(lobby.Modal!="")yield return new WaitForSecondsRealtime(.2f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return new WaitForSecondsRealtime(.1f);}
        IEnumerator Press(string target){
            // Slow batch rendering can otherwise start a state poll in the same
            // frame as a button and make an enabled test press silently vanish.
            holdingPoll=true;nextPoll.SetValue(client,float.PositiveInfinity);
            try{
                yield return Idle();yield return WaitFor(()=>ControlReady(target),"Button never rendered inside screen: "+target);
                var r=lobby.ValidationTarget(target);Check(r.width>0,"Missing button "+target);Check(r.xMin>=-.1f&&r.xMax<=Screen.width+.1f&&r.yMin>=-.1f&&r.yMax<=Screen.height+.1f,"Button outside safe screen "+target+" "+r);
                var beforePage=lobby.Page;string beforeModal=lobby.Modal;lobby.ValidationPress(target);
                yield return WaitFor(()=>pressField.GetValue(lobby)==null,"Button never accepted a press: "+target);yield return Idle();
                if(target=="back"||target=="modeBack"){
                    if(beforeModal!="")yield return WaitFor(()=>lobby.Modal=="","Back did not finish closing modal");
                    else if(beforePage==LobbyController.ScreenPage.Room)yield return WaitFor(()=>lobby.Modal=="leave","Room back did not open its confirmation");
                    else yield return WaitFor(HomeComplete,"Back did not finish returning home");
                }else if((target=="session-primary"&&beforeModal=="match-exit")||(target=="confirm"&&beforeModal=="leave")){
                    yield return WaitFor(()=>!client.LobbyActive&&HomeComplete(),"Confirmed exit did not finish returning home");
                }else if(target=="session-secondary"&&beforeModal=="session"){
                    yield return WaitFor(()=>!client.LobbyActive&&lobby.Page==LobbyController.ScreenPage.Modes&&lobby.Modal=="","Pending-match exit did not reach room choices");
                }else if(target=="session-primary"&&beforeModal=="session"){
                    yield return WaitFor(()=>client.LobbyActive&&lobby.Modal=="","Continue match did not finish resuming");
                }else if(target=="closeModal"||target=="confirm"||target.EndsWith("-confirm")||target=="session-secondary"){
                    yield return WaitFor(()=>lobby.Modal=="","Modal did not finish closing after "+target);
                }
            }
            finally{holdingPoll=false;nextPoll.SetValue(client,Time.unscaledTime+.65f);}
        }
        bool ControlReady(string target){var r=lobby.ValidationTarget(target);return r.width>0&&r.xMin>=-.1f&&r.xMax<=Screen.width+.1f&&r.yMin>=-.1f&&r.yMax<=Screen.height+.1f;}
        bool HomeComplete()=>lobby.Visible&&lobby.Page==LobbyController.ScreenPage.Home&&lobby.Modal==""&&!(bool)goingHomeField.GetValue(lobby);
        IEnumerator Home(){lobby.Home();yield return WaitFor(HomeComplete,"Direct Home transition did not finish");}
        IEnumerator RecoverHost(){
            holdingPoll=true;nextPoll.SetValue(client,float.PositiveInfinity);
            try{yield return Idle();client.LobbyRecover();yield return Idle();yield return WaitFor(()=>client.State?.host==true&&client.State.phase=="lobby","Host recovery did not reach the waiting room");}
            finally{holdingPoll=false;nextPoll.SetValue(client,Time.unscaledTime+.65f);}
        }
        IEnumerator Idle(){float deadline=Time.realtimeSinceStartup+18;while(client.Busy&&Time.realtimeSinceStartup<deadline)yield return null;Check(!client.Busy,"Network call timed out");yield return null;}
        IEnumerator WaitFor(Func<bool> predicate,string message){float deadline=Time.realtimeSinceStartup+18;while(!predicate()&&Time.realtimeSinceStartup<deadline)yield return null;Check(predicate(),message+" [page="+lobby.Page+", modal="+lobby.Modal+", phase="+client.State?.phase+", busy="+client.Busy+"]");}
        IEnumerator Api(RequestBody body,bool success=true){using(var web=new UnityWebRequest("http://127.0.0.1:8765/api","POST")){web.uploadHandler=new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));web.downloadHandler=new DownloadHandlerBuffer();web.SetRequestHeader("Content-Type","application/json");web.timeout=10;yield return web.SendWebRequest();Check(web.result==UnityWebRequest.Result.Success,"Test host HTTP failed");reply=JsonUtility.FromJson<RoomReply>(web.downloadHandler.text);if(success)Check(reply.ok,reply.error);}}
        IEnumerator ResumeSeat(RoomView view,string credential){
            yield return Idle();
            // Simulate the saved original connection, without a recovery-code
            // input or fake UI state. The live resume API owns the transition.
            typeof(RiichiClient).GetMethod("EnterHome",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(client,null);yield return Home();
            typeof(RiichiClient).GetMethod("CacheSession",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(client,new object[]{view,"http://127.0.0.1:8765",credential});
            yield return Idle();client.ResumeSession();yield return Idle();yield return WaitFor(()=>client.LobbyActive,"Original-token resume did not activate the seat");
        }
        IEnumerator Run()
        {
            yield return Api(new RequestBody{op="recoverHost",host=hostSecret},false);
            if(reply.ok){Check(reply.state.players[0].name=="大厅验证","Refusing to reset a non-test room");yield return Api(new RequestBody{op="close",token=reply.token,version=reply.state.version,id=Guid.NewGuid().ToString("N")});}
            Check(Resources.Load<Font>("Majsoul/Lobby/Fonts/brush")!=null&&Resources.Load<Font>("Majsoul/Font/game")!=null,"Native fonts missing");
            foreach(var size in new[]{new Vector2(1920,1080),new Vector2(1688,780),new Vector2(1280,720),new Vector2(2400,1080)}){
                Size((int)size.x,(int)size.y);for(int i=0;i<8;i++)yield return null;yield return Home();
                Check(lobby.Visible&&lobby.Page==LobbyController.ScreenPage.Home,"Home not visible");yield return Shot("home-"+(int)size.x);
                yield return Press("friend");yield return Idle();yield return WaitFor(()=>lobby.Page==LobbyController.ScreenPage.Modes&&lobby.Modal=="","Category inspection did not reach modes");yield return new WaitForSecondsRealtime(.5f);Check(lobby.Category=="friend","Category did not change");yield return Shot("friend-"+(int)size.x);
                yield return Press("mode0");Check(lobby.Page==LobbyController.ScreenPage.Create,"Create route failed");yield return Shot("create-"+(int)size.x);
                yield return Press("advancedOn");Check(lobby.Advanced,"Advanced settings not expanded");yield return Shot("advanced-"+(int)size.x);
                yield return Press("back");Check(lobby.Page==LobbyController.ScreenPage.Home,"Create back did not return home");
            }
            Size(1688,780);for(int i=0;i<8;i++)yield return null;
            foreach(string category in new[]{"ranked","competition"}){yield return Home();yield return Press(category);yield return Idle();yield return WaitFor(()=>lobby.Page==LobbyController.ScreenPage.Modes&&lobby.Modal=="","Named category inspection failed");yield return new WaitForSecondsRealtime(.5f);Check(lobby.Category==category,"Wrong named category");yield return Shot(category);yield return Press("help0");Check(lobby.Modal=="friend-help","Help popup failed");yield return Shot(category+"-help");yield return Press("closeModal");}
            lobby.OpenCategory("friend");yield return Idle();yield return WaitFor(()=>lobby.Page==LobbyController.ScreenPage.Modes&&lobby.Modal=="","Friend inspection failed");yield return new WaitForSecondsRealtime(.5f);yield return Press("mode1");Check(lobby.Modal=="join","Join dialog missing");
            foreach(var digit in new[]{0,1,2,3,4,5})yield return Press("digit"+digit);yield return Shot("join-keypad");yield return Press("digit11");yield return Press("digit9");yield return Press("closeModal");
            lobby.OpenCreate();yield return Press("three");yield return Press("east");yield return Press("advancedOn");lobby.DesignPreviewAdvancedLower();yield return null;yield return Press("advanced-red-0");
            Check(lobby.Players==3&&lobby.Rounds==1&&!lobby.Red,"Configuration not retained");yield return Press("rules");yield return Shot("rules-four");yield return Press("ruleMode1");yield return Shot("rules-three");yield return Press("ruleTab1");yield return Shot("rules-detail");yield return Press("closeModal");
            yield return Press("create");yield return Idle();yield return WaitFor(()=>client.State?.phase=="lobby","Create API failed");
            Check(client.State.n==3&&client.State.rounds==1&&client.State.lobby.red==false,"Server settings differ from form");Check(!lobby.CanStart,"Empty room allows start");yield return Shot("room-empty-three");
            using(var locator=new LanRoomDiscovery()){var task=locator.Find(client.State.pin,System.Threading.CancellationToken.None);while(!task.IsCompleted)yield return null;Debug.Log("Lobby UDP result: "+(task.IsFaulted?task.Exception.ToString():task.Result??"null"));Check(!task.IsFaulted&&RiichiClient.ValidEndpoint(task.Result),"UDP room-number discovery failed");}
            yield return Press("bot1");yield return Idle();Check(client.State.players.Length==2,"Bot not added");yield return Shot("room-bot");yield return Press("kick1");yield return Idle();Check(client.State.players.Length==1&&lobby.Modal=="","Bot not removed directly");yield return Shot("room-bot-removed");
            yield return Api(new RequestBody{op="join",name="真实测试牌友",pin=client.State.pin});string guest=reply.token;var guestView=reply.state;
            yield return WaitFor(()=>client.State.players.Length==2,"Host did not poll guest join");Check(!client.State.players[1].ready,"Guest was silently ready");
            yield return ResumeSeat(guestView,guest);Check(!client.State.host&&client.State.you==1,"Guest original-token resume did not adopt guest credentials");yield return Shot("room-guest-unready");
            yield return Press("ready");yield return Idle();Check(client.State.players[1].ready,"Guest prepare button failed");
            yield return Press("ready");yield return Idle();Check(!client.State.players[1].ready,"Guest cancel prepare button failed");
            yield return Press("ready");yield return Idle();yield return RecoverHost();Check(client.State.host&&client.State.players[1].ready,"Ready did not reach recovered host");
            yield return Shot("room-guest-ready");yield return Press("kick1");yield return Idle();Check(client.State.players.Length==1&&lobby.Modal=="","Human not kicked directly");
            yield return Api(new RequestBody{op="state",token=guest});Check(reply.state.phase=="join","Kicked token remains seated");
            yield return Press("bot1");yield return Idle();yield return Press("bot2");yield return Idle();Check(lobby.CanStart,"Full ready room cannot start");yield return Shot("room-ready-three");
            yield return Press("start");yield return Idle();yield return WaitFor(()=>client.State?.phase=="play"&&!lobby.Visible,"Start did not hand off to existing table after resource preparation");yield return new WaitForSecondsRealtime(6.5f);yield return Shot("existing-table-handoff");
            // The table exit now uses the same medium dialog as session prompts.
            lobby.ShowMatchExit();yield return null;Check(lobby.Modal=="match-exit","Table exit medium dialog missing");yield return Shot("table-exit-confirm");
            yield return Press("session-secondary");Check(client.LobbyActive&&lobby.Modal=="","Cancel table exit did not retain the match");
            lobby.ShowMatchExit();yield return Press("session-primary");yield return Idle();yield return WaitFor(()=>!client.LobbyActive,"Host confirmed table exit did not return home");
            yield return Api(new RequestBody{op="hostSession",host=hostSecret});Check(reply.state.phase=="empty","Host exit did not clear the table");
            lobby.OpenCreate();yield return Press("four");yield return Press("create");yield return Idle();Check(client.State.n==4,"Next room did not apply new player count");yield return Shot("room-empty-four");
            yield return Api(new RequestBody{op="join",name="退房测试",pin=client.State.pin});yield return ResumeSeat(reply.state,reply.token);yield return Press("back");yield return Shot("guest-leave-confirm");yield return Press("confirm");yield return Idle();Check(!client.LobbyActive,"Guest exit did not return home");
            yield return RecoverHost();Check(client.State.players.Length==1,"Guest exit failed to release seat");
            yield return Press("back");yield return Shot("room-leave-confirm");yield return Press("confirm");yield return Idle();Check(!client.LobbyActive&&lobby.Page==LobbyController.ScreenPage.Home,"Host exit left waiting room behind");yield return Shot("returned-home");
        }
    }
}
#endif
