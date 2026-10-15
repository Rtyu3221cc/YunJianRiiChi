#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
namespace YunJian.Riichi
{
    // Editor-only render-callback automation. This is not an Android touch or
    // multi-phone hotspot test. Uses its own disposable desktop test room.
    public sealed class LobbyValidation1013 : MonoBehaviour
    {
        RiichiClient client;LobbyController lobby;RoomReply reply;int checks;string folder;
        void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
        IEnumerator Start()
        {
            folder=Path.GetFullPath("../build/lobby-reference-1013/rendered");Directory.CreateDirectory(folder);
            foreach(var name in new[]{"failed.txt","passed.txt"}){var p=Path.Combine(folder,name);if(File.Exists(p))File.Delete(p);}
            client=GetComponent<RiichiClient>();lobby=client.Lobby;
            client.BeginLobbyValidation(Environment.GetEnvironmentVariable("RIICHI_HOST_SECRET")??"lobby-validation-1013");
            var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0){object next=null;try{var top=stack.Peek();if(!top.MoveNext()){stack.Pop();continue;}next=top.Current;if(next is IEnumerator nested){stack.Push(nested);continue;}}catch(Exception e){File.WriteAllText(Path.Combine(folder,"failed.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
            File.WriteAllText(Path.Combine(folder,"passed.txt"),checks+" actual Unity lobby checks: rendered pages, render-callback buttons, live room API, discovery loopback, bot/guest management, readiness/start, table handoff and clean return. Android touch/hotspot unverified.");
            Debug.Log("RIICHI LOBBY 1013 PASS: "+checks);EditorApplication.Exit(0);
        }
        static void Size(int w,int h){var win=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));win.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(win,new object[]{new Vector2(w,h),"Lobby 1013"});win.Repaint();}
        IEnumerator Shot(string name){if(lobby.Modal!="")yield return new WaitForSecondsRealtime(.2f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return new WaitForSecondsRealtime(.1f);}
        IEnumerator Press(string target){yield return null;var r=lobby.ValidationTarget(target);Check(r.width>0,"Missing button "+target);Check(r.xMin>=-.1f&&r.xMax<=Screen.width+.1f&&r.yMin>=-.1f&&r.yMax<=Screen.height+.1f,"Button outside safe screen "+target+" "+r);lobby.ValidationPress(target);yield return null;yield return null;if(target=="closeModal"||target=="confirm")yield return new WaitForSecondsRealtime(.18f);}
        IEnumerator Idle(){float deadline=Time.realtimeSinceStartup+18;while(client.Busy&&Time.realtimeSinceStartup<deadline)yield return null;Check(!client.Busy,"Network call timed out");yield return null;}
        IEnumerator WaitFor(Func<bool> predicate,string message){float deadline=Time.realtimeSinceStartup+18;while(!predicate()&&Time.realtimeSinceStartup<deadline)yield return null;Check(predicate(),message);}
        IEnumerator Api(RequestBody body,bool success=true){using(var web=new UnityWebRequest("http://127.0.0.1:8765/api","POST")){web.uploadHandler=new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));web.downloadHandler=new DownloadHandlerBuffer();web.SetRequestHeader("Content-Type","application/json");web.timeout=10;yield return web.SendWebRequest();Check(web.result==UnityWebRequest.Result.Success,"Test host HTTP failed");reply=JsonUtility.FromJson<RoomReply>(web.downloadHandler.text);if(success)Check(reply.ok,reply.error);}}
        IEnumerator Run()
        {
            yield return Api(new RequestBody{op="recoverHost",host="lobby-validation-1013"},false);
            if(reply.ok){Check(reply.state.players[0].name=="大厅验证","Refusing to reset a non-test room");yield return Api(new RequestBody{op="close",token=reply.token,version=reply.state.version,id=Guid.NewGuid().ToString("N")});}
            Check(Resources.Load<Font>("Majsoul/Lobby/Fonts/brush")!=null&&Resources.Load<Font>("Majsoul/Font/game")!=null,"Native fonts missing");
            foreach(var size in new[]{new Vector2(1920,1080),new Vector2(1688,780),new Vector2(1280,720),new Vector2(2400,1080)}){
                Size((int)size.x,(int)size.y);for(int i=0;i<8;i++)yield return null;lobby.Home();yield return new WaitForSecondsRealtime(.65f);
                Check(lobby.Visible&&lobby.Page==LobbyController.ScreenPage.Home,"Home not visible");yield return Shot("home-"+(int)size.x);
                yield return Press("friend");yield return new WaitForSecondsRealtime(.5f);Check(lobby.Category=="friend","Category did not change");yield return Shot("friend-"+(int)size.x);
                yield return Press("mode0");Check(lobby.Page==LobbyController.ScreenPage.Create,"Create route failed");yield return Shot("create-"+(int)size.x);
                yield return Press("advancedOn");Check(lobby.Advanced,"Advanced settings not expanded");yield return Shot("advanced-"+(int)size.x);
                yield return Press("back");Check(lobby.Page==LobbyController.ScreenPage.Home,"Create back did not return home");
            }
            Size(1688,780);for(int i=0;i<8;i++)yield return null;
            foreach(string category in new[]{"ranked","competition"}){lobby.Home();yield return new WaitForSecondsRealtime(.65f);yield return Press(category);yield return new WaitForSecondsRealtime(.5f);Check(lobby.Category==category,"Wrong named category");yield return Shot(category);yield return Press("help0");Check(lobby.Modal=="help","Help popup failed");yield return Shot(category+"-help");yield return Press("closeModal");}
            lobby.OpenCategory("friend");yield return new WaitForSecondsRealtime(.5f);yield return Press("mode1");Check(lobby.Modal=="join","Join dialog missing");
            foreach(var digit in new[]{0,1,2,3,4,5})yield return Press("digit"+digit);yield return Shot("join-keypad");yield return Press("digit11");yield return Press("digit9");yield return Press("closeModal");
            lobby.OpenCreate();yield return Press("three");yield return Press("east");yield return Press("advancedOn");yield return Press("red");
            Check(lobby.Players==3&&lobby.Rounds==1&&!lobby.Red,"Configuration not retained");yield return Press("rules");yield return Shot("rules-four");yield return Press("ruleMode1");yield return Shot("rules-three");yield return Press("ruleTab1");yield return Shot("rules-detail");yield return Press("closeModal");
            yield return Press("create");yield return Idle();yield return WaitFor(()=>client.State?.phase=="lobby","Create API failed");
            Check(client.State.n==3&&client.State.rounds==1&&client.State.lobby.red==false,"Server settings differ from form");Check(!lobby.CanStart,"Empty room allows start");yield return Shot("room-empty-three");
            using(var locator=new LanRoomDiscovery()){var task=locator.Find(client.State.pin,System.Threading.CancellationToken.None);while(!task.IsCompleted)yield return null;Debug.Log("Lobby UDP result: "+(task.IsFaulted?task.Exception.ToString():task.Result??"null"));Check(!task.IsFaulted&&RiichiClient.ValidEndpoint(task.Result),"UDP room-number discovery failed");}
            yield return Press("bot1");yield return Idle();Check(client.State.players.Length==2,"Bot not added");yield return Shot("room-bot");yield return Press("kick1");yield return Shot("room-kick-confirm");yield return Press("confirm");yield return Idle();Check(client.State.players.Length==1,"Bot not removed");
            yield return Api(new RequestBody{op="join",name="真实测试牌友",pin=client.State.pin});string guest=reply.token,guestRecovery=reply.state.recovery;
            yield return WaitFor(()=>client.State.players.Length==2,"Host did not poll guest join");Check(!client.State.players[1].ready,"Guest was silently ready");
            string roomPin=client.State.pin;client.LobbyJoin("http://127.0.0.1:8765",roomPin,guestRecovery);yield return Idle();Check(!client.State.host&&client.State.you==1,"Guest recovery UI did not adopt guest credentials");yield return Shot("room-guest-unready");
            yield return Press("ready");yield return Idle();Check(client.State.players[1].ready,"Guest prepare button failed");
            yield return Press("ready");yield return Idle();Check(!client.State.players[1].ready,"Guest cancel prepare button failed");
            yield return Press("ready");yield return Idle();client.LobbyRecover();yield return Idle();Check(client.State.host&&client.State.players[1].ready,"Ready did not reach recovered host");
            yield return Shot("room-guest-ready");yield return Press("kick1");yield return Press("confirm");yield return Idle();Check(client.State.players.Length==1,"Human not kicked");
            yield return Api(new RequestBody{op="state",token=guest});Check(reply.state.phase=="join","Kicked token remains seated");
            yield return Press("bot1");yield return Idle();yield return Press("bot2");yield return Idle();Check(lobby.CanStart,"Full ready room cannot start");yield return Shot("room-ready-three");
            yield return Press("start");yield return Idle();Check(client.State.phase=="play"&&!lobby.Visible,"Start did not hand off to existing table");yield return new WaitForSecondsRealtime(6.5f);yield return Shot("existing-table-handoff");
            yield return Api(new RequestBody{op="recoverHost",host="lobby-validation-1013"});string host=reply.token;int version=reply.state.version;
            yield return Api(new RequestBody{op="close",token=host,version=version,id=Guid.NewGuid().ToString("N")});yield return WaitFor(()=>!client.LobbyActive,"Closed test match did not return home");
            lobby.OpenCreate();yield return Press("four");yield return Press("create");yield return Idle();Check(client.State.n==4,"Next room did not apply new player count");yield return Shot("room-empty-four");
            yield return Api(new RequestBody{op="join",name="退房测试",pin=client.State.pin});guestRecovery=reply.state.recovery;roomPin=client.State.pin;
            client.LobbyJoin("http://127.0.0.1:8765",roomPin,guestRecovery);yield return Idle();yield return Press("back");yield return Shot("guest-leave-confirm");yield return Press("confirm");yield return Idle();Check(!client.LobbyActive,"Guest exit did not return home");
            client.LobbyRecover();yield return Idle();Check(client.State.players.Length==1,"Guest exit failed to release seat");
            yield return Press("back");yield return Shot("room-leave-confirm");yield return Press("confirm");yield return Idle();Check(!client.LobbyActive&&lobby.Page==LobbyController.ScreenPage.Home,"Host exit left waiting room behind");yield return Shot("returned-home");
        }
    }
}
#endif
