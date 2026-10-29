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
    [InitializeOnLoad] public static class TsumoHandR20Preview {
        const string Key="TsumoHandR20Preview";
        static TsumoHandR20Preview(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<TsumoHandR20Capture>();}};}
        public static void Run(){AssetDatabase.Refresh();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class TsumoHandR20Capture:MonoBehaviour {
        [Serializable] public class Cue {public string cue;public float time;}
        [Serializable] public class Report {public int frames,fps=30,checks;public float oldScale=.36f,newScale=TableEffects.TsumoHandScale;public List<Cue> cues=new List<Cue>();}
        RiichiClient client;TableRenderer table;TableEffects fx;string repo,output,caption;bool recording,oldDynamic;float oldInset;int frame;
        readonly List<string> checks=new List<string>();readonly Report report=new Report();readonly HashSet<WinTileEffect> sounded=new HashSet<WinTileEffect>();
        System.Diagnostics.Process encoder;Stream pipe;byte[] rgba;readonly object logLock=new object();
        IEnumerator Start(){
            repo=Environment.GetEnvironmentVariable("YUNJIAN_R20_REPO")??Path.GetFullPath("..");output=repo+"/build/tsumo-r20/capture";Directory.CreateDirectory(output);
            client=GetComponent<RiichiClient>();table=GetComponent<TableRenderer>();fx=GetComponent<TableEffects>();oldDynamic=AppPreferences.DynamicOutfits;oldInset=AppPreferences.ScreenInset;
            CharacterRoster.BeginPreview();WardrobeSelection.BeginPreview();AppPreferences.SetDynamicOutfits(false);AppPreferences.SetInset(0);AudioListener.pause=true;client.BeginLobbyValidation("tsumo-r20");client.SessionPreviewOnly=true;GetComponent<ResourceRoomCoordinator>().enabled=false;
            var tasks=new Stack<IEnumerator>();tasks.Push(Run());while(tasks.Count>0){object next;try{var task=tasks.Peek();if(!task.MoveNext()){tasks.Pop();continue;}next=task.Current;if(next is IEnumerator child){tasks.Push(child);continue;}}catch(Exception e){File.WriteAllText(output+"/error.txt",e.ToString());Debug.LogException(e);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            CloseEncoder();report.frames=frame;report.checks=checks.Count;File.WriteAllLines(output+"/checks.txt",checks);File.WriteAllText(output+"/capture.json",JsonUtility.ToJson(report,true));Cleanup();Debug.Log("TSUMO R20 PASS "+checks.Count+" frames="+frame);EditorApplication.Exit(0);
        }
        void Cleanup(){recording=false;if(encoder!=null){try{pipe?.Close();encoder.WaitForExit(3000);}catch{}encoder.Dispose();encoder=null;}fx.Reset();fx.ValidationTsumoHandScale=-1;fx.ValidationFreeze=false;fx.ValidationSequentialClock=false;client.ValidationResult.Reset();Time.captureFramerate=0;AppPreferences.SetDynamicOutfits(oldDynamic);AppPreferences.SetInset(oldInset);WardrobeSelection.EndPreview();CharacterRoster.EndPreview();AudioListener.pause=false;}
        void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add("PASS "+message);}
        IEnumerator Wait(int n=3){for(int i=0;i<n;i++)yield return null;}
        RoomView Copy(RoomView v)=>JsonUtility.FromJson<RoomView>(JsonUtility.ToJson(v));
        RoomView View(int viewer=0,string style="female"){
            var v=JsonUtility.FromJson<RoomView>(File.ReadAllText(repo+"/build/unity-smoke/win-4-tsumo-0-0.json"));v.you=viewer;v.resourceSetup=null;v.dealtAt=0;v.openingHand=new string[0];v.audioRoom="tsumo-r20-"+viewer+style;
            foreach(var p in v.players){p.auto=false;p.character=style=="male"?"mingzhiyingshu":"yiji";p.skin=style=="male"?401401:400101;p.wardrobe=new WardrobeLoadout{hand=style=="cat"?"hand-cat-orange":"hand-human",headFrame="headframe_default",cloth="cloth-default",tileFace="mjpface_default",tileBack="mjp_default",winEffect="ron-fate-hf",riichiEffect="riichi-blue",stick="stick-default"};}return v;
        }
        IEnumerator Use(RoomView v,bool tableOnly=true){client.ShowValidationFixture(v);client.ValidationTableOnly=tableOnly;client.ValidationIntro.Reset();fx.Reset();fx.ValidationFreeze=true;fx.ValidationSequentialClock=false;fx.ValidationTime=0;fx.Accept(v);yield return Wait();}
        void Hand(RoomView v,float scale){fx.ValidationTsumoHandScale=scale;fx.ValidationPlay(0,"tsumo",v.result.details[0].tile);typeof(TableEffects).GetMethod("FinishCall",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(fx,null);typeof(TableEffects).GetField("cutInWin",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(fx,null);}
        Renderer Palm()=>UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).First(r=>r.gameObject.name=="Source hand mesh"&&r.gameObject.activeInHierarchy);
        IEnumerator Still(string name){yield return new WaitForEndOfFrame();var im=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+name+".png",im.EncodeToPNG());Destroy(im);}
        IEnumerator Run(){
            var w=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));w.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,new object[]{new Vector2(1746,810),"Tsumo hand scale review"});yield return Wait(8);
            foreach(var sample in new[]{(0,"female"),(1,"female"),(2,"female"),(3,"female"),(0,"male"),(0,"cat")}){
                var v=View(sample.Item1,sample.Item2);yield return Use(v);Hand(v,.36f);fx.ValidationTime=.34f;yield return Wait(1);var previous=Palm().bounds.size;float width=TableTileGeometry.WorldWidth(fx.ValidationAnimatedTile);
                if(sample.Item1==0&&sample.Item2=="female")yield return Still("before");
                yield return Use(v);Hand(v,-1);fx.ValidationTime=.34f;yield return Wait(1);var palm=Palm();Check(Mathf.Abs(palm.transform.parent.localScale.x/TableTileGeometry.SourceUnits-.48f)<.0001f,"Moderately enlarged hand scale "+sample);Check(Vector3.Distance(palm.bounds.size,previous*(.48f/.36f))<.001f,"Exact one-third linear enlargement "+sample);Check(Mathf.Abs(TableTileGeometry.WorldWidth(fx.ValidationAnimatedTile)-width)<.00001f,"Carried tile size unchanged "+sample);
                if(sample.Item1==0&&sample.Item2=="female")yield return Still("after");
                fx.ValidationTime=fx.ValidationHandoff;yield return Wait(1);Check(Vector3.Distance(fx.ValidationAnimatedTile.position,fx.ValidationTarget.position)<.025f,"Tile still reaches its reserved landing point "+sample);Check(fx.ValidationTarget.gameObject.activeSelf,"Real winning tile restored at handoff "+sample);fx.Reset();
            }
            BeginEncoder();
            foreach(float scale in new[]{.36f,-1f}){
                var v=View();yield return Use(v);Hand(v,scale);caption=scale>0?"调整前 · 原缩放 0.36 · 动作慢放":"调整后 · 放大约33% · 动作慢放";
                for(int i=0;i<90;i++){fx.ValidationTime=i/90f;yield return Frame();}
            }
            fx.ValidationTsumoHandScale=-1;var after=View();var before=Copy(after);before.phase="reaction";before.result=null;before.audioSeq=0;before.audioEvents=new PublicSound[0];yield return Use(before,false);fx.ValidationSequentialClock=true;fx.ValidationTime=0;caption="调整后 · 自摸完整流程与 Fate 特效";
            for(int i=0;i<18;i++)yield return Frame();client.ValidationApplyState(after);fx.Accept(after);bool hand=false,effect=false,settled=false;float resultStart=-1;
            for(int i=0;i<390;i++){float time=i/30f;fx.ValidationTime=time;hand|=fx.ValidationKind=="tsumo";effect|=fx.ValidationWin!=null&&fx.ValidationWin.UsesNewHuleBinding;if(effect&&!fx.ShowWinPresentation){settled=true;if(resultStart<0)resultStart=time;client.ValidationResult.Freeze=true;client.ValidationResult.FrozenAge=time-resultStart;}yield return Frame();}
            Check(hand&&effect&&settled,"Complete tsumo, Fate playback and yaku settlement remain connected");
        }
        void BeginEncoder(){
            var info=new System.Diagnostics.ProcessStartInfo{FileName="C:/Users/Rtyu3221/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe",Arguments="-X utf8 \""+repo+"/scripts/encode-game-capture.py\" --output \""+output+"/tsumo-r20-silent.mp4\" --width "+Screen.width+" --height "+Screen.height+" --fps 30",UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};encoder=new System.Diagnostics.Process{StartInfo=info};encoder.OutputDataReceived+=(s,e)=>{if(e.Data!=null)lock(logLock)File.AppendAllText(output+"/encoder.log",e.Data+"\n");};encoder.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)lock(logLock)File.AppendAllText(output+"/encoder.log",e.Data+"\n");};encoder.Start();encoder.BeginOutputReadLine();encoder.BeginErrorReadLine();pipe=encoder.StandardInput.BaseStream;recording=true;Time.captureFramerate=30;
        }
        void CloseEncoder(){recording=false;pipe.Close();encoder.WaitForExit();Check(encoder.ExitCode==0,"Comparison video encoder completes");encoder.Dispose();encoder=null;pipe=null;}
        IEnumerator Frame(){yield return new WaitForEndOfFrame();var effect=fx.ValidationWin;if(effect!=null&&effect.SoundTriggered&&sounded.Add(effect))report.cues.Add(new Cue{cue=effect.SoundCue,time=frame/30f});var im=ScreenCapture.CaptureScreenshotAsTexture();var pixels=im.GetPixels32();if(rgba==null||rgba.Length!=pixels.Length*4)rgba=new byte[pixels.Length*4];for(int i=0;i<pixels.Length;i++){int j=i*4;rgba[j]=pixels[i].r;rgba[j+1]=pixels[i].g;rgba[j+2]=pixels[i].b;rgba[j+3]=pixels[i].a;}pipe.Write(rgba,0,rgba.Length);Destroy(im);frame++;}
        void OnGUI(){if(!recording)return;int depth=GUI.depth;var matrix=GUI.matrix;var color=GUI.color;GUI.depth=-10000;GUI.matrix=Matrix4x4.identity;GUI.color=new Color(0,0,0,.65f);GUI.DrawTexture(new Rect(Screen.width*.2f,8,Screen.width*.6f,45),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(Screen.width*.2f,8,Screen.width*.6f,45),caption,new GUIStyle(GUI.skin.label){font=Resources.Load<Font>("Majsoul/Font/game"),fontSize=23,alignment=TextAnchor.MiddleCenter});GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;}
    }
}
