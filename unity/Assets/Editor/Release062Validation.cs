using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YunJian.Riichi.Editor
{
    [InitializeOnLoad] public static class Release062Validation
    {
        const string Key="Riichi.Release062";
        static Release062Validation(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<Release062Capture>();}};}
        public static void Run(){ProjectSetup.Configure();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class Release062Capture:MonoBehaviour
    {
        static readonly BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        readonly List<string> checks=new List<string>(),errors=new List<string>();
        string output,fixture;RiichiClient client;LobbyController lobby;TableRenderer table;TableEffects hands;RiichiDiscardVisual visual;GameAudio audio;
        void Check(bool condition,string message){if(!condition)throw new Exception(message);checks.Add(message);}
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception)errors.Add(message);}
        IEnumerator Start(){
            output=Path.GetFullPath("../build/release-062/render");Directory.CreateDirectory(output);Application.logMessageReceived+=Log;
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0){object next;try{var top=stack.Peek();if(!top.MoveNext()){stack.Pop();continue;}next=top.Current;if(next is IEnumerator child){stack.Push(child);continue;}}catch(Exception e){File.WriteAllText(Path.Combine(output,"failed.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
            File.WriteAllText(Path.Combine(output,"validation.txt"),string.Join("\n",checks));Debug.Log("RELEASE 0.6.2 PASS: "+checks.Count+" focused checks; phone testing pending");EditorApplication.Exit(0);
        }
        IEnumerator Size(int w,int h){var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",Private).Invoke(window,new object[]{new Vector2(w,h),"0.6.2 review"});for(int i=0;i<7;i++)yield return null;}
        IEnumerator Shot(string name){yield return null;yield return new WaitForEndOfFrame();var pic=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),pic.EncodeToPNG());Destroy(pic);}
        RoomView Fixture(int n){var v=JsonUtility.FromJson<RoomView>(fixture);v.n=n;v.you=0;v.players=v.players.Take(n).ToArray();v.phase="play";v.audioRoom="release062-"+n;v.matchSeq=1;v.handSeq=1;v.audioSeq=0;v.audioEvents=new PublicSound[0];foreach(var p in v.players){p.riichi=0;p.river=new[]{new RiverTile{tile="m1",riichi=true}};}return v;}
        PublicSound Action(int seat,bool riichi=true)=>new PublicSound{id=100+seat,seat=seat,kind="discard",tile="m1",riichi=riichi,riverIndex=1};
        IEnumerator Run(){
            client=GetComponent<RiichiClient>();lobby=client.Lobby;table=GetComponent<TableRenderer>();hands=GetComponent<TableEffects>();visual=GetComponent<RiichiDiscardVisual>();audio=GetComponent<GameAudio>();
            client.BeginLobbyValidation("release062-local");lobby.DesignPreviewClock=100;lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Home);AudioListener.pause=true;
            yield return Size(1688,780);client.LobbyOpenSettings();yield return new WaitForSecondsRealtime(.3f);yield return Shot("settings-1688");client.LobbyCloseSettings();lobby.DesignPreviewGardenHelp();
            var yaku=(NativeYakuUi)typeof(LobbyController).GetField("nativeYaku",Private).GetValue(lobby);yaku.Section=1;
            foreach(int page in new[]{0,1,8}){yaku.TutorialPage=page;yield return Shot("tutorial-"+(page+1)+"-1688");}
            lobby.DesignPreviewClock=101;lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Home);
            fixture=File.ReadAllText("../build/unity-smoke/preview-4.json");
            Check(Resources.Load<AudioClip>("Majsoul/Audio/effect_rich_fire")?.length>.9f,"Original fire-riichi audio is present");
            foreach(int n in new[]{3,4}){
                var view=Fixture(n);client.DesignPreviewSessionTable(view);visual.Observe(view,false);audio.Configure(false,0,true,.5f);int before=visual.PlayedCount,cues=audio.PlayedCues.Count(x=>x=="effect_rich_fire");
                Check(!visual.TryPlay(Action(0,false),table.FindDiscard(0,"m1",1),0),n+"p ordinary discard has no burst");
                for(int seat=0;seat<n;seat++){
                    var action=Action(seat);Check(visual.TryPlay(action,table.FindDiscard(seat,"m1",1),0),n+"p seat "+seat+" first declaration plays");
                    Check(!visual.TryPlay(Action(seat),table.FindDiscard(seat,"m1",1),0),n+"p seat "+seat+" duplicate is suppressed");
                }
                visual.Tick(.2f);Check(visual.Latest.LayerCount==9&&visual.Latest.MeshLayerCount==5&&visual.Latest.ParticleCount>0,n+"p renders all 9 original emitters and 5 source meshes");
                Check(visual.PlayedCount-before==n&&audio.PlayedCues.Count(x=>x=="effect_rich_fire")-cues==n,n+"p cue and visual fire exactly once per player");
                visual.StopVisuals();visual.Observe(view,false);Check(!visual.TryPlay(Action(0),table.FindDiscard(0,"m1",1),1),n+"p presentation reset/poll does not replay");
                view.handSeq++;visual.Observe(view,false);Check(visual.TryPlay(Action(0),table.FindDiscard(0,"m1",1),1),n+"p new hand permits a new burst");
                view.handSeq++;view.players[0].riichi=1;visual.Observe(view,true);Check(!visual.TryPlay(Action(0),table.FindDiscard(0,"m1",1),1),n+"p resume snapshot does not replay a past declaration");
                view.handSeq++;visual.Observe(view,false);audio.Configure(false,0,false,0);AudioListener.pause=false;Check(visual.TryPlay(Action(0),table.FindDiscard(0,"m1",1),1)&&!audio.EffectsPlaying,n+"p muted effects retain visuals without sound");AudioListener.pause=true;visual.StopVisuals();
            }
            yield return Size(1280,720);var demo=Fixture(4);demo.audioRoom="release062-animation";client.DesignPreviewSessionTable(demo);visual.Observe(demo,false);audio.Configure(false,0,true,.5f);hands.ValidationPlay(0,"discard","m1",true,1);
            Directory.CreateDirectory(Path.Combine(output,"riichi-motion"));int initial=visual.PlayedCount;int soundAt=-1;
            for(int frame=0;frame<150;frame++){hands.ValidationTime=frame/60f;yield return Shot("riichi-motion/frame-"+frame.ToString("D4"));if(visual.PlayedCount>initial&&soundAt<0)soundAt=frame;}
            Check(visual.PlayedCount==initial+1&&soundAt>=0,"Actual declaration hand animation triggers the new effect once");
            File.WriteAllText(Path.Combine(output,"timeline.json"),"{\"fps\":60,\"frames\":150,\"soundFrame\":"+soundAt+"}");
            Check(Application.version=="0.6.2","Runtime version is 0.6.2");Check(errors.Count==0,"No Unity runtime or GUI errors");AudioListener.pause=false;
        }
        void OnDestroy(){Application.logMessageReceived-=Log;AudioListener.pause=false;}
    }
}
