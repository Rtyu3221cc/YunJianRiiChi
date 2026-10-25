using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;

namespace YunJian.Riichi.Editor {
    [InitializeOnLoad] public static class Interaction070Review {
        const string Key="Interaction070Review";
        static Interaction070Review(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<Interaction070Capture>();}};}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Bootstrap(){if(!SessionState.GetBool(Key,false))return;CharacterPackService.RootOverride=Path.GetFullPath("../build/portrait-fix-068/installed");CharacterPackService.RespectPreviewAvailability=true;CharacterPackService.ResetForReview();}
        public static void Run(){ProjectSetup.Configure();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class Interaction070Capture:MonoBehaviour {
        [Serializable] sealed class Report {public bool androidDeviceTested,serverRandomFixedForPreview=true;public int syncLoads,asyncLoads;public float maxFrameMs,warmupMs;public long[] botReplyMs;public List<string> checks=new List<string>();}
        readonly Report report=new Report();readonly List<string> errors=new List<string>();string output;RiichiClient client;LobbyController lobby;RoomReply reply;bool oldBots,oldLive,watch;float oldInset;
        void Update(){if(watch)report.maxFrameMs=Mathf.Max(report.maxFrameMs,Time.unscaledDeltaTime*1000);}
        void Log(string message,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error)errors.Add(message);}
        void Check(bool ok,string message){if(!ok)throw new Exception(message);report.checks.Add(message);Debug.Log("INTERACTION 070 CHECK "+message);}
        IEnumerator Hold(int frames=5){lobby.DesignPreviewClock=30;for(int i=0;i<frames;i++)yield return null;}
        IEnumerator Until(Func<bool> ready,string message){float end=Time.realtimeSinceStartup+90;while(!ready()){if(Time.realtimeSinceStartup>end)throw new Exception("Timeout: "+message);yield return null;}}
        IEnumerator Shot(string name,bool immediate=false){if(!immediate)yield return Hold(3);yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());Destroy(image);}
        void Cleanup(){AppPreferences.SetInset(oldInset);AppPreferences.SetDynamicOutfits(oldLive);AppPreferences.SetRandomBotAppearance(oldBots);CharacterRoster.EndPreview();Application.logMessageReceived-=Log;AudioListener.pause=false;}
        IEnumerator Start(){
            output=Path.GetFullPath("../build/interaction-070/render");Directory.CreateDirectory(output);Application.logMessageReceived+=Log;
            oldBots=AppPreferences.RandomBotAppearance;oldLive=AppPreferences.DynamicOutfits;oldInset=AppPreferences.ScreenInset;CharacterRoster.BeginPreview();AppPreferences.SetInset(0);AppPreferences.SetDynamicOutfits(false);AudioListener.pause=true;Application.targetFrameRate=30;
            client=GetComponent<RiichiClient>();client.BeginLobbyValidation("interaction-070");lobby=client.Lobby;GetComponent<ResourceRoomCoordinator>().enabled=false;
            var stack=new Stack<IEnumerator>();stack.Push(Capture());while(stack.Count>0){object next;try{var top=stack.Peek();if(!top.MoveNext()){stack.Pop();continue;}next=top.Current;if(next is IEnumerator child){stack.Push(child);continue;}}catch(Exception ex){File.WriteAllText(Path.Combine(output,"failed.txt"),ex.ToString());Debug.LogException(ex);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            Check(errors.Count==0,"No Unity errors during cold appearance loading and actual HTTP emoji interaction");File.WriteAllText(Path.Combine(output,"checks.json"),JsonUtility.ToJson(report,true));Cleanup();Debug.Log("INTERACTION 070 PREVIEW PASS "+report.checks.Count);EditorApplication.Exit(0);
        }
        IEnumerator Raw(RequestBody command){command.id=Guid.NewGuid().ToString("N");using(var web=new UnityWebRequest("http://127.0.0.1:8765/api","POST")){web.uploadHandler=new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(command)));web.downloadHandler=new DownloadHandlerBuffer();web.SetRequestHeader("Content-Type","application/json");web.timeout=10;yield return web.SendWebRequest();if(web.result!=UnityWebRequest.Result.Success)throw new Exception(web.error);reply=RoomReply.Parse(web.downloadHandler.text);if(reply?.ok!=true)throw new Exception(reply?.error??"No reply");}}
        void State(RoomView state,string token=null){client.DesignPreviewSessionTable(state);client.ValidationIntro.Reset();if(token!=null)typeof(RiichiClient).GetField("token",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(client,token);typeof(RiichiClient).GetField("resourceSelectionRevision",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(client,CharacterPackService.Revision);typeof(RiichiClient).GetField("resourceSelectionDirty",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(client,false);}
        IEnumerator Capture(){
            var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(1688,768),"Interaction fixes 0.6.10"});yield return Hold(8);
            Check(AppPreferences.RandomBotAppearance,"Random computer appearance is ON after the former default upgrade");
            var fixture=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/unity-smoke/preview-4.json"));fixture.audioEvents=new PublicSound[0];fixture.audioRoom="cold-bot-070";fixture.phase="lobby";fixture.resourceSetup=null;
            string[] keys={"yiji","fuji","weilai","kawei"};int[] skins={400101,403805,406903,401004};for(int i=0;i<4;i++){fixture.players[i].character=keys[i];fixture.players[i].skin=skins[i];fixture.players[i].bot=i>0;fixture.players[i].ready=true;}
            int syncBefore=CharacterPackService.ReviewSyncBundleLoads,asyncBefore=CharacterPackService.ReviewAsyncBundleLoads;watch=true;State(fixture);lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Room);yield return Hold(1);yield return Shot("room-loading",true);
            float began=Time.realtimeSinceStartup;var setup=new ResourceSetupView{choices=keys.Select((key,i)=>new CharacterChoice(key,skins[i])).ToArray()};yield return (IEnumerator)typeof(ResourceRoomCoordinator).GetMethod("Preload",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(GetComponent<ResourceRoomCoordinator>(),new object[]{setup});report.warmupMs=(Time.realtimeSinceStartup-began)*1000;
            yield return Until(()=>lobby.ReviewRoomCardReady.Count==4&&lobby.ReviewRoomCardReady.Values.All(v=>v),"cold room portraits");watch=false;report.syncLoads=CharacterPackService.ReviewSyncBundleLoads-syncBefore;report.asyncLoads=CharacterPackService.ReviewAsyncBundleLoads-asyncBefore;
            Check(report.syncLoads==0,"Cold random room cards and match preload perform zero synchronous optional bundle loads");Check(report.asyncLoads>=3,"Three previously cold optional roles load asynchronously");yield return Hold(8);yield return Shot("room-loaded");
            client.BeginLobbyValidation("interaction-070");client.LobbyOpenSettings();yield return Hold();var settings=(NativeSettingsUi)typeof(RiichiClient).GetField("nativeSettings",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(client);settings.ReviewPress="settings-tab-3";yield return Hold();yield return Shot("preferences-random-default");client.LobbyCloseSettings();
            CharacterResourceUi.Current.Ask("jiutiao",true);yield return Shot("download-confirmation");Check(CharacterResourceUi.Current.Targets.ContainsKey("resource-prompt-download"),"Enlarged native resource confirmation buttons are rendered");CharacterResourceUi.Current.Close();
            yield return Raw(new RequestBody{op="create",host="interaction-070",name="旅人",n=4,rounds=1,character="fuji",skin=403805,bots=true,randomBotAppearance=true,botCharacters=new[]{new CharacterChoice("yiji",400101),new CharacterChoice("weilai",406903),new CharacterChoice("kawei",401004)},resourceProtocol=0});string token=reply.token;
            yield return Raw(new RequestBody{op="start",token=token,version=reply.state.version,resourceProtocol=0});State(reply.state,token);yield return Hold(25);yield return Shot("emote-entry-20-percent");
            var emotes=client.ValidationEmotes;emotes.ReviewPress="emote-open";yield return Hold();yield return Until(()=>Enumerable.Range(0,9).All(i=>CharacterRoster.DisplayTexture("Emoji/fuji/"+i)!=null),"own emojis");emotes.ReviewPress="emote-send-0";yield return Until(()=>!client.Busy&&client.State.emoteSeq>0,"actual picker send");
            float limit=Time.realtimeSinceStartup+8;while(true){yield return Raw(new RequestBody{op="state",token=token});State(reply.state,token);if(reply.state.emotes.Count(e=>e.seat!=reply.state.you)==3)break;if(Time.realtimeSinceStartup>limit)throw new Exception("Delayed AI replies missing");yield return Hold(3);}
            long sent=reply.state.emotes.First(e=>e.seat==reply.state.you).at;report.botReplyMs=reply.state.emotes.Where(e=>e.seat!=reply.state.you).Select(e=>e.at-sent).ToArray();Check(report.botReplyMs.All(ms=>ms>=1200&&ms<=1750),"Actual HTTP bot replies arrive after the requested randomized delay, within one 50ms host tick");
            Check(reply.state.emotes.All(e=>e.character==reply.state.players[e.seat].character),"Bot emoji identities match their own frozen characters");yield return Until(()=>client.State.emotes.All(e=>CharacterRoster.DisplayTexture("Emoji/"+e.character+"/"+e.emoji)!=null),"bot emoji art");yield return Shot("bot-emote-interaction");
        }
    }
}
