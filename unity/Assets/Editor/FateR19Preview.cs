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
    [InitializeOnLoad] public static class FateR19Preview {
        const string Key="FateR19Preview";
        static FateR19Preview(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<FateR19Capture>();}};}
        public static void Study(){SessionState.SetBool(Key+"Movie",false);Run();}
        public static void Movie(){SessionState.SetBool(Key+"Movie",true);Run();}
        static void Run(){AssetDatabase.Refresh();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class FateR19Capture:MonoBehaviour {
        [Serializable] public class Cue {public string cue;public float time;}
        [Serializable] public class CaptureReport {public int frames,fps=30;public List<Cue> cues=new List<Cue>();}
        RiichiClient client;TableRenderer table;TableEffects fx;string repo,output,caption;bool movie,recording,oldDynamic;float oldInset;int frame;
        readonly List<string> checks=new List<string>();readonly CaptureReport report=new CaptureReport();
        readonly HashSet<WinTileEffect> sounded=new HashSet<WinTileEffect>();
        System.Diagnostics.Process encoder;Stream pipe;byte[] rgba;readonly object logLock=new object();
        IEnumerator Start(){
            repo=Environment.GetEnvironmentVariable("YUNJIAN_R19_REPO")??Path.GetFullPath("..");output=repo+"/build/fate-r19/capture";Directory.CreateDirectory(output);
            movie=SessionState.GetBool("FateR19PreviewMovie",false);client=GetComponent<RiichiClient>();table=GetComponent<TableRenderer>();fx=GetComponent<TableEffects>();
            oldDynamic=AppPreferences.DynamicOutfits;oldInset=AppPreferences.ScreenInset;CharacterRoster.BeginPreview();WardrobeSelection.BeginPreview();AppPreferences.SetDynamicOutfits(false);AppPreferences.SetInset(0);AudioListener.pause=true;
            client.BeginLobbyValidation("fate-r19");client.SessionPreviewOnly=true;GetComponent<ResourceRoomCoordinator>().enabled=false;
            var stack=new Stack<IEnumerator>();stack.Push(movie?Movie():Study());
            while(stack.Count>0){object next;try{var task=stack.Peek();if(!task.MoveNext()){stack.Pop();continue;}next=task.Current;if(next is IEnumerator child){stack.Push(child);continue;}}catch(Exception e){File.WriteAllText(output+"/error-"+(movie?"movie":"study")+".txt",e.ToString());Debug.LogException(e);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            if(movie){CloseEncoder();report.frames=frame;File.WriteAllText(output+"/capture.json",JsonUtility.ToJson(report,true));}
            File.WriteAllLines(output+"/"+(movie?"movie":"study")+"-checks.txt",checks);Cleanup();Debug.Log("FATE R19 "+(movie?"MOVIE":"STUDY")+" PASS "+checks.Count+" frames="+frame);EditorApplication.Exit(0);
        }
        void Cleanup(){recording=false;if(encoder!=null){try{pipe?.Close();encoder.WaitForExit(3000);}catch{}encoder.Dispose();encoder=null;}fx.Reset();fx.ValidationFreeze=false;fx.ValidationSequentialClock=false;client.ValidationResult.Reset();Time.captureFramerate=0;AppPreferences.SetDynamicOutfits(oldDynamic);AppPreferences.SetInset(oldInset);WardrobeSelection.EndPreview();CharacterRoster.EndPreview();AudioListener.pause=false;}
        void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add("PASS "+message);}
        IEnumerator Wait(int n=3){for(int i=0;i<n;i++)yield return null;}
        IEnumerator Size(){var w=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));w.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,new object[]{new Vector2(1746,810),"Fate original Lua effect"});yield return Wait(8);}
        RoomView Copy(RoomView v)=>JsonUtility.FromJson<RoomView>(JsonUtility.ToJson(v));
        RoomView View(string kind,int viewer){
            var v=JsonUtility.FromJson<RoomView>(File.ReadAllText(repo+"/build/unity-smoke/win-4-"+kind+"-0-0.json"));v.you=viewer;v.resourceSetup=null;v.dealtAt=0;v.openingHand=new string[0];v.audioRoom="fate-r19-"+kind+"-"+viewer;
            foreach(var p in v.players){p.auto=false;p.character="yiji";p.skin=400101;p.wardrobe=new WardrobeLoadout{hand="hand-human",headFrame="headframe_default",cloth="cloth-default",tileFace="mjpface_default",tileBack="mjp_default",winEffect="ron-fate-hf",riichiEffect="riichi-blue",stick="stick-default"};}
            return v;
        }
        IEnumerator Use(RoomView v,bool result=true){client.ShowValidationFixture(v);client.ValidationTableOnly=!result;client.ValidationIntro.Reset();fx.Reset();fx.ValidationFreeze=true;fx.ValidationSequentialClock=true;fx.ValidationTime=0;fx.Accept(v);yield return Wait();}
        void Spawn(RoomView v){var d=v.result.details[0];var e=new PublicSound{id=1,seat=d.seat,from=d.from,tile=d.tile,kind=d.ron?"ron":"tsumo",winSource=d.winSource};typeof(TableEffects).GetMethod("SpawnWin",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(fx,new object[]{e,-.1f});}
        IEnumerator Still(string name){yield return new WaitForEndOfFrame();var im=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+name+".png",im.EncodeToPNG());Destroy(im);}
        IEnumerator Study(){
            yield return Size();var prefab=Resources.Load<GameObject>("Majsoul/NativeReview/FateR19/ron_25hf");Check(prefab!=null,"Source prefab exists");Check(prefab.GetComponentsInChildren<ParticleSystem>(true).Length==43,"All 43 native particle layers");Check(prefab.GetComponentsInChildren<MeshRenderer>(true).Length==114,"All 114 native mesh layers");
            var motion=prefab.GetComponent<NativeEffectMotion>();Check(motion!=null,"Original compiled animation is sampled");var data=JsonUtility.FromJson<NativeMotionData>(motion.source.text);Check(data.tracks.Length==1&&data.tracks[0].duration>6.4f,"Full source 6.483-second compiled clip");Check(data.tracks[0].curves.Any(c=>c.kind==4&&c.property=="_Alpha"),"Source alpha-driven visibility retained");Check(data.tracks[0].curves.Count(c=>c.kind>=3&&c.kind<=5)>100,"Animated material parameters retained");
            Check(prefab.GetComponentsInChildren<Renderer>(true).All(r=>r.sharedMaterials.All(m=>m!=null&&m.shader.name=="YunJian/FateParticle"&&m.shader.isSupported&&m.mainTexture!=null)),"Every native material and texture is renderable");
            for(int viewer=0;viewer<4;viewer++){
                var v=View("ron",viewer);yield return Use(v,false);Spawn(v);yield return Wait();var win=fx.ValidationWin;Check(win!=null&&win.UsesNativeSource&&win.UsesNewHuleBinding,"Source Lua binding view "+viewer);Check(win.PlaybackDuration==6&&win.FixedSourceLifetime,"Source six-second cleanup view "+viewer);
                bool particles=false,visible=false;float firstAlpha=-1,maxAlpha=0;int enabledMin=9999,enabledMax=0;
                foreach(float age in new[]{0f,.2f,.6f,1.2f,1.8f,2.6f,3.3f,4.5f,5.8f}){
                    if(age==1.2f){var poll=Copy(v);poll.version++;client.ValidationApplyState(poll);fx.Accept(poll);}
                    fx.ValidationTime=age;yield return Wait(1);var renderers=win.GetComponentsInChildren<Renderer>(true);int enabled=renderers.Count(r=>r.enabled);enabledMin=Math.Min(enabledMin,enabled);enabledMax=Math.Max(enabledMax,enabled);particles|=win.ParticleCount>0;
                    var queue=win.GetComponent<NativeWinningTileQueue>();Check(queue!=null&&queue.Queue==3090&&queue.Active==(age<5.3f),"Lua winning tile queue and restoration "+viewer+" age "+age);
                    foreach(var r in renderers){var block=new MaterialPropertyBlock();r.GetPropertyBlock(block,0);if(block.HasFloat("_Alpha")){float a=block.GetFloat("_Alpha");if(firstAlpha<0)firstAlpha=a;maxAlpha=Mathf.Max(maxAlpha,a);}}
                    var move=win.GetComponentsInChildren<Transform>(true).First(t=>t.name=="root_move");var origin=win.GetComponentsInChildren<Transform>(true).First(t=>t.name=="root_static");Check(Vector3.Distance(move.position,win.transform.position/TableTileGeometry.SourceUnits)<.00001f,"root_move tracks winning tile "+viewer+" age "+age);Check(origin.position.sqrMagnitude<.000001f,"root_static stays at scene origin "+viewer+" age "+age);
                    if(age==.6f||age==1.8f||age==3.3f||age==5.8f)yield return Still("study-"+viewer+"-"+age.ToString("F1",System.Globalization.CultureInfo.InvariantCulture));
                    visible|=renderers.Any(r=>r.enabled&&r.isVisible);
                }
                Check(particles&&visible,"Effect emits and reaches the real game camera view "+viewer);Check(firstAlpha<.01f,"Source alpha starts transparent view "+viewer);Check(maxAlpha>.8f,"Source alpha animation reaches full intensity view "+viewer);
                fx.ValidationTime=6.01f;yield return Wait();Check(fx.ValidationWin==null,"Effect is destroyed at source timeout view "+viewer);fx.Reset();
            }
            // A real public win event takes the normal catalog/frozen-loadout path.
            var after=View("ron",0);var before=Copy(after);before.phase="reaction";before.result=null;before.audioSeq=0;before.audioEvents=new PublicSound[0];yield return Use(before);client.ValidationApplyState(after);fx.Accept(after);for(int i=0;i<55;i++){fx.ValidationTime=i*.2f;yield return Wait(1);}Check(fx.SpawnedWinEffects>0,"Production public event triggers selected Fate outfit");int count=fx.SpawnedWinEffects;fx.Accept(Copy(after));yield return Wait();Check(fx.SpawnedWinEffects==count,"Presence poll does not replay the win effect");
        }
        void BeginEncoder(){
            var info=new System.Diagnostics.ProcessStartInfo{FileName="C:/Users/Rtyu3221/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe",Arguments="-X utf8 \""+repo+"/scripts/encode-game-capture.py\" --output \""+output+"/fate-r19-silent.mp4\" --width "+Screen.width+" --height "+Screen.height+" --fps 30",UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};encoder=new System.Diagnostics.Process{StartInfo=info};encoder.OutputDataReceived+=(s,e)=>{if(e.Data!=null)lock(logLock)File.AppendAllText(output+"/encoder.log",e.Data+"\n");};encoder.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)lock(logLock)File.AppendAllText(output+"/encoder.log",e.Data+"\n");};encoder.Start();encoder.BeginOutputReadLine();encoder.BeginErrorReadLine();pipe=encoder.StandardInput.BaseStream;recording=true;Time.captureFramerate=30;
        }
        void CloseEncoder(){recording=false;pipe.Close();encoder.WaitForExit();Check(encoder.ExitCode==0,"Video encoder exits successfully");encoder.Dispose();encoder=null;pipe=null;}
        IEnumerator Frame(){yield return new WaitForEndOfFrame();var effect=fx.ValidationWin;if(effect!=null&&effect.SoundTriggered&&sounded.Add(effect))report.cues.Add(new Cue{cue=effect.SoundCue,time=frame/30f});var im=ScreenCapture.CaptureScreenshotAsTexture();var pixels=im.GetPixels32();if(rgba==null||rgba.Length!=pixels.Length*4)rgba=new byte[pixels.Length*4];for(int i=0;i<pixels.Length;i++){int j=i*4;rgba[j]=pixels[i].r;rgba[j+1]=pixels[i].g;rgba[j+2]=pixels[i].b;rgba[j+3]=pixels[i].a;}pipe.Write(rgba,0,rgba.Length);Destroy(im);frame++;}
        IEnumerator Movie(){
            yield return Size();BeginEncoder();
            foreach(string kind in new[]{"ron","tsumo"}){
                int viewer=0;var after=View(kind,viewer);var before=Copy(after);before.phase="reaction";before.result=null;before.audioSeq=0;before.audioEvents=new PublicSound[0];yield return Use(before);caption="Fate联动 · 咒层界・恶念祝祭 · "+(kind=="ron"?"荣和":"自摸");
                for(int i=0;i<24;i++)yield return Frame();client.ValidationApplyState(after);fx.Accept(after);bool saw=false,ended=false;float resultStart=-1;
                for(int i=0;i<390;i++){
                    float time=i/30f;fx.ValidationTime=time;
                    if(fx.ValidationWin!=null)saw|=fx.ValidationWin.UsesNewHuleBinding;
                    if(saw&&!fx.ShowWinPresentation){ended=true;if(resultStart<0)resultStart=time;client.ValidationResult.Freeze=true;client.ValidationResult.FrozenAge=time-resultStart;}
                    yield return Frame();
                }
                Check(saw,"Movie: production "+kind+" plays native Fate effect");Check(ended,"Movie: "+kind+" restores yaku settlement after cleanup");
            }
        }
        void OnGUI(){if(!recording||string.IsNullOrEmpty(caption))return;int depth=GUI.depth;var matrix=GUI.matrix;var color=GUI.color;GUI.depth=-10000;GUI.matrix=Matrix4x4.identity;GUI.color=new Color(0,0,0,.6f);GUI.DrawTexture(new Rect(Screen.width*.17f,8,Screen.width*.66f,44),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(Screen.width*.17f,8,Screen.width*.66f,44),caption,new GUIStyle(GUI.skin.label){font=Resources.Load<Font>("Majsoul/Font/game"),fontSize=23,alignment=TextAnchor.MiddleCenter});GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;}
    }
}
