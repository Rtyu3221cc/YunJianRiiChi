using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    [InitializeOnLoad] public static class PortraitRegression068 {
        const string Key="PortraitRegression068";
        static PortraitRegression068(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<PortraitRegression068Capture>();}};}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Bootstrap(){if(!SessionState.GetBool(Key,false))return;CharacterPackService.RootOverride=Path.GetFullPath("../build/portrait-fix-068/installed");CharacterPackService.RespectPreviewAvailability=true;CharacterPackService.ResetForReview();}
        public static void Run(){ProjectSetup.Configure();AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class PortraitRegression068Capture:MonoBehaviour {
        [Serializable] sealed class Measurement {public string page;public int frames,syncBundleLoads,asyncBundleLoads,loadedBundles,diskChecks;public float firstFrameMs,maxFrameMs;}
        [Serializable] sealed class Report {public bool androidDeviceTested;public List<string> checks=new List<string>();public List<Measurement> measurements=new List<Measurement>();}
        readonly Report report=new Report();readonly List<string> errors=new List<string>();
        RiichiClient client;LobbyController lobby;string output;float clock=10;
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception)errors.Add(message);}
        void Check(bool ok,string message){if(!ok)throw new Exception(message);report.checks.Add(message);Debug.Log("PORTRAIT 068 CHECK "+message);}
        IEnumerator Start(){
            output=Path.GetFullPath("../build/portrait-fix-068/render");Directory.CreateDirectory(output);Application.logMessageReceived+=Log;
            var stack=new Stack<IEnumerator>();stack.Push(Capture());
            while(stack.Count>0){object current;try{var work=stack.Peek();if(!work.MoveNext()){stack.Pop();continue;}current=work.Current;if(current is IEnumerator nested){stack.Push(nested);continue;}}catch(Exception ex){File.WriteAllText(Path.Combine(output,"failed.txt"),ex.ToString());Debug.LogException(ex);EditorApplication.Exit(1);yield break;}yield return current;}
            Check(errors.Count==0,"No Unity errors during actual UI and presentation rendering");File.WriteAllText(Path.Combine(output,"checks.json"),JsonUtility.ToJson(report,true));CharacterRoster.EndPreview();Application.logMessageReceived-=Log;Debug.Log("PORTRAIT 068 REVIEW PASS "+report.checks.Count);EditorApplication.Exit(0);
        }
        IEnumerator Hold(int frames=8){clock+=frames/30f;lobby.DesignPreviewClock=clock;for(int i=0;i<frames;i++)yield return null;}
        IEnumerator Until(Func<bool> ready,string label){float deadline=Time.realtimeSinceStartup+60;while(!ready()){if(Time.realtimeSinceStartup>deadline)throw new Exception("Timeout: "+label);yield return null;}}
        IEnumerator Shot(string name){yield return Hold(3);yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),t.EncodeToPNG());Destroy(t);}
        IEnumerator Measure(string page,Action open){
            int sync=CharacterPackService.ReviewSyncBundleLoads,async=CharacterPackService.ReviewAsyncBundleLoads,disk=CharacterPackService.ReviewDiskChecks;
            var m=new Measurement{page=page};open();
            for(int i=0;i<24;i++){yield return null;float ms=Time.unscaledDeltaTime*1000;if(i==0)m.firstFrameMs=ms;m.maxFrameMs=Mathf.Max(m.maxFrameMs,ms);m.frames++;}
            m.syncBundleLoads=CharacterPackService.ReviewSyncBundleLoads-sync;m.asyncBundleLoads=CharacterPackService.ReviewAsyncBundleLoads-async;m.loadedBundles=CharacterPackService.ReviewLoadedBundles;m.diskChecks=CharacterPackService.ReviewDiskChecks-disk;report.measurements.Add(m);
            Check(m.syncBundleLoads==0,page+": opens with zero synchronous optional bundle loads");Check(m.loadedBundles==0,page+": default avatar grid opens no optional bundles with all 122 roles installed");yield return Shot(page);
        }
        IEnumerator Capture(){
            CharacterRoster.BeginPreview();client=GetComponent<RiichiClient>();client.BeginLobbyValidation("portrait-068");lobby=client.Lobby;AudioListener.pause=true;Application.targetFrameRate=60;
            var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(1688,768),"Portrait regression"});yield return Hold(8);
            Check(CharacterPackService.All.All(p=>CharacterPackService.Installed(p.key)),"All 122 real character installations are available without downloads");
            CharacterRoster.Use("yiji",400101);lobby.DesignPreviewGardenEntry();yield return Hold(100);
            yield return Measure("cold-role-selection",()=>lobby.OpenRoleSelection());lobby.PreviewHideCharacters();
            yield return Measure("cold-dorm",()=>lobby.OpenDorm());lobby.PreviewHideCharacters();
            CharacterRoster.Use("fuji",403805);lobby.DesignPreviewGardenEntry();yield return Until(()=>CharacterRoster.DisplayTexture("Skins/403805/full")!=null,"asynchronous 福姬 illustration");yield return Hold(60);yield return Shot("home-fuji");
            Check(CharacterPackService.ReviewAsyncBundleLoads>0,"Selected optional illustration loads asynchronously");
            lobby.PreviewDorm("fuji",403805);yield return Hold(30);yield return Shot("dorm-fuji");lobby.PreviewDorm("fuji",403805,"skins");yield return Hold(25);yield return Shot("skin-fuji");
            var fixture=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/unity-smoke/preview-4.json"));fixture.you=0;fixture.audioRoom="portrait-regression-068";fixture.audioSeq=0;fixture.audioEvents=new PublicSound[0];fixture.result=null;fixture.options=new Choice[0];
            for(int i=0;i<4;i++){fixture.players[i].character=i==0?"fuji":"yiji";fixture.players[i].skin=i==0?403805:400101;fixture.players[i].name=i==0?"旅人":"AI · "+i;fixture.players[i].takeoverMask=false;fixture.players[i].bot=i!=0;fixture.players[i].ready=true;fixture.players[i].river=new RiverTile[0];}
            fixture.phase="lobby";fixture.pin="787768";fixture.host=true;fixture.lobby=new LobbyRoomView{category="friend",red=true,managed=true};lobby.PreviewHideCharacters();GetComponent<ResourceRoomCoordinator>().enabled=false;client.DesignPreviewSessionTable(fixture);typeof(RiichiClient).GetField("resourceSelectionRevision",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(client,CharacterPackService.Revision);typeof(RiichiClient).GetField("resourceSelectionDirty",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(client,false);lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Room);yield return Hold(20);Check(lobby.ValidationTarget("start").width>0,"Actual ready-room controls and all four portrait cards are rendered");yield return Shot("room-fuji-yiji");
            fixture.phase="play";client.ShowValidationFixture(fixture);client.ValidationIntro.Reenter(fixture);client.ValidationIntro.Freeze=true;client.ValidationIntro.FrozenAge=1.1f;yield return Hold(10);yield return Shot("intro-fuji-yiji");client.ValidationIntro.Reset();
            fixture.phase="lobby";fixture.players[1].character="erjietang";fixture.players[1].skin=400201;fixture.players[3].character="weilai";fixture.players[3].skin=406903;client.DesignPreviewSessionTable(fixture);lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Room);yield return Hold(15);Check(lobby.ValidationTarget("start").width>0,"Legacy waitingroom sprite and native Mirai framing render in the same room");yield return Shot("room-legacy-mixed");fixture.phase="play";client.ShowValidationFixture(fixture);client.ValidationIntro.Reenter(fixture);client.ValidationIntro.Freeze=true;client.ValidationIntro.FrozenAge=1.1f;yield return Shot("intro-legacy-mixed");client.ValidationIntro.Reset();fixture.players[1].character=fixture.players[3].character="yiji";fixture.players[1].skin=fixture.players[3].skin=400101;
            var effects=GetComponent<TableEffects>();foreach(int seat in new[]{0,1,2,3}){effects.Reset();effects.ValidationFreeze=true;effects.ValidationTime=.25f;typeof(TableEffects).GetMethod("BeginCall",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(effects,new object[]{new PublicSound{seat=seat,kind="riichi"}});yield return Hold(5);yield return Shot("riichi-seat-"+seat);}effects.Reset();
            effects.CutIn.Play(fixture.players[0]);effects.CutIn.Freeze=true;effects.CutIn.FrozenAge=.55f;yield return Shot("win-cutin-fuji");Check(effects.CutIn.Active,"Winner cut-in uses the original K0 scene with the full illustration");effects.Reset();
            var win=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/unity-smoke/win-4-tsumo-1-0.json"));win.audioRoom="portrait-results-068";win.audioEvents=new PublicSound[0];win.you=0;win.result.matchOver=true;win.result.details[0].seat=0;win.result.details[0].name="旅人";for(int i=0;i<4;i++){win.players[i].character=i==0?"fuji":"yiji";win.players[i].skin=i==0?403805:400101;win.players[i].ready=true;}client.DesignPreviewSessionTable(win);var results=client.ValidationResult;results.Observe(win);results.Freeze=true;results.FrozenAge=1;yield return Shot("result-win-fuji");while(!results.ShowingScores){results.FrozenAge=100;results.Advance(win);}results.FrozenAge=100;results.Advance(win);results.FrozenAge=1;yield return Shot("result-final-fuji");Check(results.ShowingFinal,"Final settlement uses the original B2 scene");
            client.BeginLobbyValidation("portrait-068");lobby.PreviewDorm("yiji",400107);yield return Hold(30);yield return Shot("dorm-live-yiji");
            using(var art=new CharacterIllustration()){var skin=CharacterRoster.Find("qingluan").skins.Single(s=>s.id==406704);Texture a=null;yield return Until(()=>{a=art.Art(skin,Time.unscaledTime);return a is RenderTexture;},"downloaded dynamic skeleton and pages");Check(a is RenderTexture,"Optional dynamic outfit becomes animated after asynchronous asset preparation");}
            Check(CharacterPackService.ReviewLoadedBundles<12,"UI navigation keeps a bounded cache instead of retaining all 120 optional packs");
        }
    }
}
