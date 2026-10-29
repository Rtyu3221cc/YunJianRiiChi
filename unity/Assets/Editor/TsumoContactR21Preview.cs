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
    [InitializeOnLoad] public static class TsumoContactR21Preview {
        const string Key="TsumoContactR21Preview";
        static TsumoContactR21Preview(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<TsumoContactR21Capture>();}};}
        public static void Run(){AssetDatabase.Refresh();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class TsumoContactR21Capture:MonoBehaviour {
        [Serializable] public class Cue {public string cue;public float time;}
        [Serializable] public class Timing {public string style;public int viewer,n;public float contact,sound,release,lifetime;}
        [Serializable] public class Report {public int frames,fps=30,checks;public List<Cue> cues=new List<Cue>();public List<Timing> timings=new List<Timing>();}
        RiichiClient client;TableRenderer table;TableEffects fx;GameAudio audio;string repo,output,caption;bool recording,oldDynamic,recordLanding;float oldInset;int frame,lastLanding;
        readonly List<string> checks=new List<string>();readonly Report report=new Report();readonly HashSet<WinTileEffect> sounded=new HashSet<WinTileEffect>();
        System.Diagnostics.Process encoder;Stream pipe;byte[] rgba;readonly object logLock=new object();
        IEnumerator Start(){
            repo=Environment.GetEnvironmentVariable("YUNJIAN_R21_REPO")??Path.GetFullPath("..");output=repo+"/build/tsumo-r21/capture";Directory.CreateDirectory(output);File.Delete(output+"/error.txt");
            client=GetComponent<RiichiClient>();table=GetComponent<TableRenderer>();fx=GetComponent<TableEffects>();audio=GetComponent<GameAudio>();oldDynamic=AppPreferences.DynamicOutfits;oldInset=AppPreferences.ScreenInset;
            CharacterRoster.BeginPreview();WardrobeSelection.BeginPreview();AppPreferences.SetDynamicOutfits(false);AppPreferences.SetInset(0);AudioListener.pause=true;client.BeginLobbyValidation("tsumo-r21");client.SessionPreviewOnly=true;GetComponent<ResourceRoomCoordinator>().enabled=false;
            var work=new Stack<IEnumerator>();work.Push(Run());while(work.Count>0){object next;try{var task=work.Peek();if(!task.MoveNext()){work.Pop();continue;}next=task.Current;if(next is IEnumerator child){work.Push(child);continue;}}catch(Exception e){File.WriteAllText(output+"/error.txt",e.ToString());Debug.LogException(e);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            CloseEncoder();report.frames=frame;report.checks=checks.Count;File.WriteAllLines(output+"/checks.txt",checks);File.WriteAllText(output+"/capture.json",JsonUtility.ToJson(report,true));Cleanup();Debug.Log("TSUMO R21 PASS "+checks.Count+" frames="+frame);EditorApplication.Exit(0);
        }
        void Cleanup(){recording=false;if(encoder!=null){try{pipe?.Close();encoder.WaitForExit(3000);}catch{}encoder.Dispose();encoder=null;}fx.Reset();fx.ValidationTsumoHandScale=-1;fx.ValidationFreeze=false;fx.ValidationSequentialClock=false;client.ValidationResult.Reset();Time.captureFramerate=0;AppPreferences.SetDynamicOutfits(oldDynamic);AppPreferences.SetInset(oldInset);WardrobeSelection.EndPreview();CharacterRoster.EndPreview();AudioListener.pause=false;}
        void Check(bool ok,string text){if(!ok)throw new Exception(text);checks.Add("PASS "+text);}
        IEnumerator Wait(int n=3){for(int i=0;i<n;i++)yield return null;}
        RoomView Copy(RoomView v)=>JsonUtility.FromJson<RoomView>(JsonUtility.ToJson(v));
        RoomView View(int viewer=0,string style="female",int count=4){
            var v=JsonUtility.FromJson<RoomView>(File.ReadAllText(repo+"/build/unity-smoke/win-"+count+"-tsumo-0-0.json"));v.you=viewer;v.resourceSetup=null;v.dealtAt=0;v.openingHand=new string[0];v.audioRoom="tsumo-r21-"+viewer+style+count;
            foreach(var p in v.players){p.auto=false;p.character=style=="male"?"mingzhiyingshu":"yiji";p.skin=style=="male"?401401:400101;p.wardrobe=new WardrobeLoadout{hand=style=="cat"?"hand-cat-orange":"hand-human",headFrame="headframe_default",cloth="cloth-default",tileFace="mjpface_default",tileBack="mjp_default",winEffect="ron-fate-hf",riichiEffect="riichi-blue",stick="stick-default"};}return v;
        }
        IEnumerator Use(RoomView v,bool tableOnly=true){client.ShowValidationFixture(v);client.ValidationTableOnly=tableOnly;client.ValidationIntro.Reset();fx.Reset();fx.ValidationFreeze=true;fx.ValidationSequentialClock=false;fx.ValidationTime=0;fx.Accept(v);yield return Wait();}
        void Hand(RoomView v){fx.ValidationPlay(0,"tsumo",v.result.details[0].tile);typeof(TableEffects).GetMethod("FinishCall",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(fx,null);typeof(TableEffects).GetField("cutInWin",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(fx,null);}
        Transform Palm()=>UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).First(r=>r.gameObject.name=="Source hand mesh"&&r.gameObject.activeInHierarchy).transform;
        IEnumerator Still(string name){yield return new WaitForEndOfFrame();var im=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+name+".png",im.EncodeToPNG());Destroy(im);}
        IEnumerator Run(){
            var w=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));w.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,new object[]{new Vector2(1746,810),"Tsumo contact without hover"});yield return Wait(8);
            foreach(string style in new[]{"female","male","cat"})for(int viewer=0;viewer<4;viewer++)yield return CheckHand(View(viewer,style),style);
            yield return CheckHand(View(0,"female",3),"female");yield return CheckHand(View(1,"cat",3),"cat");
            BeginEncoder();var slow=View();yield return Use(slow);Hand(slow);caption="修正后 · 拍桌即落稳，随后只收手 · 慢放";recordLanding=false;
            for(int i=0;i<90;i++){fx.ValidationTime=i/90f;yield return Frame();}
            var after=View();var before=Copy(after);before.phase="reaction";before.result=null;before.audioSeq=0;before.audioEvents=new PublicSound[0];yield return Use(before,false);fx.ValidationSequentialClock=true;fx.ValidationTime=0;caption="正常速度 · 自摸落牌与 Fate 特效";recordLanding=true;lastLanding=audio.LandingSounds;
            for(int i=0;i<18;i++)yield return Frame();client.ValidationApplyState(after);fx.Accept(after);bool hand=false,effect=false,settled=false;float resultStart=-1;
            for(int i=0;i<390;i++){float time=i/30f;fx.ValidationTime=time;hand|=fx.ValidationKind=="tsumo";effect|=fx.ValidationWin!=null&&fx.ValidationWin.UsesNewHuleBinding;if(effect&&!fx.ShowWinPresentation){settled=true;if(resultStart<0)resultStart=time;client.ValidationResult.Freeze=true;client.ValidationResult.FrozenAge=time-resultStart;}yield return Frame();}
            Check(hand&&effect&&settled,"Production tsumo, source-delayed Fate and settlement all complete");
        }
        IEnumerator CheckHand(RoomView v,string style){
            yield return Use(v);Hand(v);float contact=fx.ValidationHandoff,cue=fx.ValidationTsumoCue,release=fx.ValidationTsumoRelease,lifetime=fx.ValidationPlayLength;int sounds=audio.LandingSounds;var original=fx.ValidationTarget;float width=TableTileGeometry.WorldWidth(original);var point=original.position;var rotation=original.rotation;
            report.timings.Add(new Timing{style=style,viewer=v.you,n=v.n,contact=contact,sound=cue,release=release,lifetime=lifetime});string key=style+" view="+v.you+" n="+v.n;
            Check(contact>=cue&&contact<.6f&&contact<release,"Contact belongs to the slap, not rig release: "+key);Check(lifetime<=.91f&&lifetime>=release,"Hand lifetime comes from source config: "+key);Check(Mathf.Abs(Palm().parent.localScale.x/TableTileGeometry.SourceUnits-.48f)<.0001f,"Approved hand scale stays .48: "+key);
            float previous=float.PositiveInfinity;
            for(int i=0;i<5;i++){fx.ValidationTime=contact-.08f+i*.016f;yield return Wait(1);float height=fx.ValidationAnimatedTile.position.y-point.y;Check(height>0&&height<previous,"Continuous final descent before impact "+key+" sample="+i);previous=height;}
            fx.ValidationTime=contact;yield return Wait(1);Check(fx.ValidationHandedOff&&fx.ValidationTarget.gameObject.activeSelf&&!fx.ValidationAnimatedTile.gameObject.activeSelf,"Physical table tile takes over on impact: "+key);Check(Vector3.Distance(fx.ValidationAnimatedTile.position,point)<.0001f,"No position jump at impact: "+key);Check(Mathf.Abs(TableTileGeometry.WorldWidth(fx.ValidationTarget)-width)<.00001f,"Tile dimensions unchanged: "+key);Check(audio.LandingSounds==sounds+1,"Source contact sound plays exactly once: "+key);
            bool handMoved=false;var hand=Palmmatrix();
            foreach(float time in new[]{contact+.04f,contact+.1f,release-.02f}){
                if(time>=lifetime)continue;
                if(time==contact+.1f){var poll=Copy(v);poll.version++;client.ValidationApplyState(poll);fx.Accept(poll);}
                fx.ValidationTime=time;yield return Wait(1);var tile=table.WinningTarget(new PublicSound{seat=0,kind="tsumo",tile=v.result.details[0].tile});Check(tile.gameObject.activeSelf&&Vector3.Distance(tile.position,point)<.000001f&&Quaternion.Angle(tile.rotation,rotation)<.001f,"Settled tile cannot fall again, including poll: "+key+" t="+time);handMoved|=Palmmatrix()!=hand;
            }
            Check(handMoved,"Only the hand keeps retracting after contact: "+key);var win=fx.ValidationWin;Check(win!=null&&Mathf.Abs(win.StartedAt-release-.1f)<.0001f,"Effect waits for source release plus .1 seconds: "+key);Check(audio.LandingSounds==sounds+1,"No second landing sound on release: "+key);
            if(v.n==4&&v.you==0&&style=="female")yield return Still("settled-hand-retracting");fx.ValidationTime=lifetime+.02f;yield return Wait(1);Check(fx.ValidationKind==null,"Hand stops at its own source lifetime: "+key);
        }
        Matrix4x4 Palmmatrix()=>Palm().localToWorldMatrix*Matrix4x4.Translate(Palm().GetComponent<MeshFilter>().sharedMesh.bounds.center);
        void BeginEncoder(){
            var info=new System.Diagnostics.ProcessStartInfo{FileName="C:/Users/Rtyu3221/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe",Arguments="-X utf8 \""+repo+"/scripts/encode-game-capture.py\" --output \""+output+"/tsumo-r21-silent.mp4\" --width "+Screen.width+" --height "+Screen.height+" --fps 30",UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};encoder=new System.Diagnostics.Process{StartInfo=info};encoder.OutputDataReceived+=(s,e)=>{if(e.Data!=null)lock(logLock)File.AppendAllText(output+"/encoder.log",e.Data+"\n");};encoder.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)lock(logLock)File.AppendAllText(output+"/encoder.log",e.Data+"\n");};encoder.Start();encoder.BeginOutputReadLine();encoder.BeginErrorReadLine();pipe=encoder.StandardInput.BaseStream;recording=true;Time.captureFramerate=30;
        }
        void CloseEncoder(){recording=false;pipe.Close();encoder.WaitForExit();Check(encoder.ExitCode==0,"Video encoder completes");encoder.Dispose();encoder=null;pipe=null;}
        IEnumerator Frame(){yield return new WaitForEndOfFrame();if(recordLanding&&audio.LandingSounds>lastLanding){report.cues.Add(new Cue{cue="zimo",time=frame/30f});lastLanding=audio.LandingSounds;}var effect=fx.ValidationWin;if(recordLanding&&effect!=null&&effect.SoundTriggered&&sounded.Add(effect))report.cues.Add(new Cue{cue=effect.SoundCue,time=frame/30f});var im=ScreenCapture.CaptureScreenshotAsTexture();var pixels=im.GetPixels32();if(rgba==null||rgba.Length!=pixels.Length*4)rgba=new byte[pixels.Length*4];for(int i=0;i<pixels.Length;i++){int j=i*4;rgba[j]=pixels[i].r;rgba[j+1]=pixels[i].g;rgba[j+2]=pixels[i].b;rgba[j+3]=pixels[i].a;}pipe.Write(rgba,0,rgba.Length);Destroy(im);frame++;}
        void OnGUI(){if(!recording)return;int depth=GUI.depth;var matrix=GUI.matrix;var color=GUI.color;GUI.depth=-10000;GUI.matrix=Matrix4x4.identity;GUI.color=new Color(0,0,0,.65f);GUI.DrawTexture(new Rect(Screen.width*.2f,8,Screen.width*.6f,45),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(Screen.width*.2f,8,Screen.width*.6f,45),caption,new GUIStyle(GUI.skin.label){font=Resources.Load<Font>("Majsoul/Font/game"),fontSize=23,alignment=TextAnchor.MiddleCenter});GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;}
    }
}
