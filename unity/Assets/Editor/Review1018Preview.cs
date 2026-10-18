using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    // Visual review only: deterministic public table fixture and real Unity frames.
    [InitializeOnLoad] public static class Review1018Preview {
        const string Key="Riichi.Review1018";
        static Review1018Preview(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<Review1018Capture>();}};}
        public static void Run(){NativeCalloutAssetImport.Configure();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class Review1018Capture:MonoBehaviour {
        static readonly BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        string output;RiichiClient client;TableEffects effects;bool baseline;
        readonly List<string> errors=new List<string>();
        [Serializable] class WordMetric {public string image,word;public int slot,screenWidth,screenHeight;public float x,y,width,height,inkX,inkY,inkWidth,inkHeight;}
        [Serializable] class WordMetrics {public List<WordMetric> entries;}
        readonly List<WordMetric> wordMetrics=new List<WordMetric>();
        void Log(string m,string t,LogType k){if(k==LogType.Error||k==LogType.Exception)errors.Add(m);}
        IEnumerator Start(){
            baseline=Array.IndexOf(Environment.GetCommandLineArgs(),"-review1018Baseline")>=0;
            output=Path.GetFullPath("../build/review-1018/"+(baseline?"before":"render"));Directory.CreateDirectory(output);Application.logMessageReceived+=Log;
            var stack=new Stack<IEnumerator>();stack.Push(Capture());
            while(stack.Count>0){object next;try{var top=stack.Peek();if(!top.MoveNext()){stack.Pop();continue;}next=top.Current;if(next is IEnumerator child){stack.Push(child);continue;}}catch(Exception e){File.WriteAllText(Path.Combine(output,"failed.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
            File.WriteAllText(Path.Combine(output,"word-metrics.json"),JsonUtility.ToJson(new WordMetrics{entries=wordMetrics},true));
            File.WriteAllText(Path.Combine(output,"render-notes.txt"),"Actual Unity rendering; synthetic public table fixture; no APK or test runner.\n"+string.Join("\n",errors));Debug.Log("REVIEW 1018 RENDER COMPLETE errors="+errors.Count);EditorApplication.Exit(errors.Count==0?0:1);
        }
        IEnumerator Size(int w,int h){var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",Private).Invoke(window,new object[]{new Vector2(w,h),"1018 visual review"});for(int i=0;i<7;i++)yield return null;}
        IEnumerator Shot(string name){
            yield return null;yield return new WaitForEndOfFrame();var p=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),p.EncodeToPNG());Destroy(p);
            if(name.IndexOf('/')<0&&(name.StartsWith("riichi-")||name.StartsWith("chi-")||name.StartsWith("pon-")||name.StartsWith("kan-")||name.StartsWith("nuki-")||name.StartsWith("sanma-nuki-"))){
                var native=(NativeCallouts)typeof(TableEffects).GetField("nativeCallouts",Private).GetValue(effects);var r=native.LastWordScreenRect;
                wordMetrics.Add(new WordMetric{image=name+".png",word=native.LastWord,slot=native.LastWordSlot,screenWidth=Screen.width,screenHeight=Screen.height,x=r.x,y=r.y,width=r.width,height=r.height,inkX=native.LastWordInkScreenRect.x,inkY=native.LastWordInkScreenRect.y,inkWidth=native.LastWordInkScreenRect.width,inkHeight=native.LastWordInkScreenRect.height});
            }
        }
        void Call(int seat,string kind){effects.Reset();effects.ValidationFreeze=true;effects.ValidationTime=.25f;typeof(TableEffects).GetMethod("BeginCall",Private).Invoke(effects,new object[]{new PublicSound{seat=seat,kind=kind}});}
        RoomView Fixture(int n){
            var v=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/unity-smoke/preview-4.json"));v.n=n;v.you=0;v.phase="play";v.turn=0;v.audioRoom="review1018-local";v.audioSeq=0;v.audioEvents=new PublicSound[0];v.options=new Choice[0];v.waits=new string[0];v.waitDetails=new WaitDetail[0];v.discardHints=new DiscardHint[0];v.result=null;v.lobby=new LobbyRoomView{category="ranked"};
            if(n==3)Array.Resize(ref v.players,3);
            string[] tiles={"p3","p9","p5","p6","m4","m9","s1","s4","s9","z1","z2","z3"};
            for(int seat=0;seat<n;seat++){var p=v.players[seat];p.score=25000;p.riichi=0;p.takeoverMask=seat==1;p.bot=seat!=0;p.name=seat==0?"你你你旅人":seat==1?"旅人":"AI · "+seat;p.river=new RiverTile[12];for(int i=0;i<12;i++)p.river[i]=new RiverTile{tile=tiles[(i+seat*3)%tiles.Length]};}
            v.hand=new[]{"m3","m6","m8","p4","p5","p6","s4","s4","s5","s5","s6","s6","s8"};v.drawn=null;v.indicators=new[]{"z5"};return v;
        }
        IEnumerator Capture(){
            client=GetComponent<RiichiClient>();effects=GetComponent<TableEffects>();client.BeginLobbyValidation("visual-only");AudioListener.pause=true;
            yield return Size(1688,780);client.DesignPreviewSessionTable(Fixture(4));for(int i=0;i<8;i++)yield return null;
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-review1018AuditOnly")>=0){
                foreach(string kind in new[]{"ron","tsumo"})foreach(int seat in new[]{0,1,2,3}){Call(seat,kind);yield return Shot(kind+"-seat-"+seat);}
                effects.Reset();var all=Fixture(4);var faces=new List<string>();foreach(char suit in "mps")for(int i=0;i<=9;i++)faces.Add(suit.ToString()+i);for(int i=1;i<=7;i++)faces.Add("z"+i);
                for(int seat=0;seat<4;seat++)for(int i=0;i<12;i++)all.players[seat].river[i].tile=faces[(seat*12+i)%faces.Count];client.DesignPreviewSessionTable(all);yield return Shot("all-37-faces");yield break;
            }
            yield return Shot("table-1688");
            foreach(int seat in new[]{0,1,2,3}){Call(seat,"riichi");yield return Shot("riichi-seat-"+seat);}
            if(baseline)yield break;
            foreach(string kind in new[]{"chi","pon","kan","nuki"})foreach(int seat in new[]{0,1,2,3}){Call(seat,kind);yield return Shot(kind+"-seat-"+seat);}
            yield return Size(1280,592);client.DesignPreviewSessionTable(Fixture(4));
            foreach(int seat in new[]{0,1,2,3}){
                string dir="seat-"+seat;Directory.CreateDirectory(Path.Combine(output,dir));int frame=0;
                foreach(string kind in new[]{"riichi","chi","pon","kan","nuki"}){Call(seat,kind);for(int i=0;i<27;i++){effects.ValidationTime=i/30f;yield return Shot(dir+"/frame-"+(frame++).ToString("D4"));}}
            }
            effects.Reset();yield return Size(1280,720);client.DesignPreviewSessionTable(Fixture(4));yield return Shot("table-16x9");Call(2,"pon");yield return Shot("north-16x9");
            foreach(int seat in new[]{0,1,2,3}){Call(seat,"riichi");yield return Shot("riichi-16x9-seat-"+seat);}
            effects.Reset();yield return Size(1280,592);client.DesignPreviewSessionTable(Fixture(3));foreach(int seat in new[]{0,1,2}){Call(seat,"nuki");yield return Shot("sanma-nuki-seat-"+seat);}
        }
        void OnDestroy(){Application.logMessageReceived-=Log;AudioListener.pause=false;}
    }
}
