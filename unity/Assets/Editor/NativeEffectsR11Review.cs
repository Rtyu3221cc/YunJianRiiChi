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
    [InitializeOnLoad] public static class NativeEffectsR11Review {
        const string Key="NativeEffectsR11Review";
        static NativeEffectsR11Review(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<NativeEffectsR11Probe>();}};}
        public static void RunStudy(){SessionState.SetBool("NativeEffectsR11Full",false);Run();}
        public static void RunFull(){SessionState.SetBool("NativeEffectsR11Full",true);Run();}
        public static void RunCorrections(){SessionState.SetBool("NativeEffectsR11Full",false);SessionState.SetBool("NativeEffectsR12",true);Run();}
        public static void RecordCorrections(){SessionState.SetBool("NativeEffectsR11Full",true);SessionState.SetBool("NativeEffectsR12",true);Run();}
        public static void RecordScytheR13(){SessionState.SetBool("NativeEffectsR11Full",true);SessionState.SetBool("NativeEffectsR13",true);Run();}
        static void Run(){AssetDatabase.Refresh();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class NativeEffectsR11Probe:MonoBehaviour {
        [Serializable] class Entry {public string id,sourceKey,category,nativeEffect;public int particleLayers;}
        [Serializable] class Manifest {public Entry[] effects;}
        [Serializable] public class Shot {public string key,title,group,cue;public int viewer,start,end;public float cueAt;public float[] detail;}
        [Serializable] class Report {public int frames,fps=20,checks;public string graphicsSource="WebGL 4.0.51 / 0.16.289";public List<Shot> shots=new List<Shot>();}
        Report report=new Report();string output,caption;bool full,dynamic,corrections,scythe;float inset,volume;int frame;
        RiichiClient client;TableRenderer table;TableEffects fx;
        IEnumerator Start(){
            full=SessionState.GetBool("NativeEffectsR11Full",false);corrections=SessionState.GetBool("NativeEffectsR12",false);scythe=SessionState.GetBool("NativeEffectsR13",false);SessionState.SetBool("NativeEffectsR12",false);SessionState.SetBool("NativeEffectsR13",false);if(corrections||scythe)report.fps=60;output=Path.GetFullPath("../build/"+(scythe?"effects-r13-review/":corrections?"effects-r12-review/":"effects-r11-review/")+(full?"render":"study"));Directory.CreateDirectory(output);Directory.CreateDirectory(output+"/frames");File.WriteAllText(output+"/checks.txt","");File.Delete(output+"/error.txt");
            dynamic=AppPreferences.DynamicOutfits;inset=AppPreferences.ScreenInset;volume=AudioListener.volume;CharacterRoster.BeginPreview();WardrobeSelection.BeginPreview();AppPreferences.SetDynamicOutfits(false);AppPreferences.SetInset(0);AudioListener.volume=0;
            WardrobeSelection.Save(new Dictionary<string,string>{{"cloth","cloth-default"},{"tileFace","mjpface_default"},{"tileBack","mjp_default"}});Time.captureFramerate=report.fps;
            client=GetComponent<RiichiClient>();table=GetComponent<TableRenderer>();fx=GetComponent<TableEffects>();client.BeginLobbyValidation("native-effects-r11");client.SessionPreviewOnly=true;GetComponent<ResourceRoomCoordinator>().enabled=false;
            var work=new Stack<IEnumerator>();work.Push(Run());while(work.Count>0){object next;try{var task=work.Peek();if(!task.MoveNext()){work.Pop();continue;}next=task.Current;if(next is IEnumerator child){work.Push(child);continue;}}catch(Exception e){File.WriteAllText(output+"/error.txt",e.ToString());Debug.LogException(e);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            report.frames=frame;File.WriteAllText(output+"/capture.json",JsonUtility.ToJson(report,true));Cleanup();Debug.Log("NATIVE EFFECTS R11 REVIEW PASS "+report.checks+" checks; "+frame+" frames");EditorApplication.Exit(0);
        }
        void Cleanup(){Time.captureFramerate=0;fx.ValidationSequentialClock=false;fx.ValidationFreeze=false;fx.Reset();AudioListener.volume=volume;WardrobeSelection.EndPreview();CharacterRoster.EndPreview();AppPreferences.SetDynamicOutfits(dynamic);AppPreferences.SetInset(inset);}
        void Check(bool ok,string label){if(!ok)throw new Exception(label);report.checks++;File.AppendAllText(output+"/checks.txt","PASS "+label+"\n");}
        IEnumerator Wait(int n=2){for(int i=0;i<n;i++)yield return null;}
        RoomView View(string file,int viewer,Entry entry,bool declaration=false){
            var view=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/"+(declaration?"wardrobe-r6-study/riichi-":"unity-smoke/")+file+".json"));view.you=viewer;view.resourceSetup=null;view.audioRoom="native-r11-"+entry.id+":"+viewer;
            foreach(var p in view.players){p.character="yiji";p.skin=400101;p.wardrobe=new WardrobeLoadout{hand="hand-human",stick="stick-default",riichiEffect=entry.category=="riichiEffect"?entry.id:"riichi-blue",winEffect=entry.category=="winEffect"?entry.id:"win-default",callout="effect_zhishi",music="music-riichi"};}
            return view;
        }
        void Use(RoomView view){fx.ValidationSequentialClock=false;client.DesignPreviewSessionTable(view);client.ValidationTableOnly=true;client.ValidationIntro.Reset();fx.Reset();fx.ValidationFreeze=true;fx.Accept(view);}
        float[] Detail(Vector3 point){const float w=700,h=600;var p=table.Camera.WorldToScreenPoint(point);return new[]{Mathf.Clamp(p.x-w*.5f,0,Screen.width-w),Mathf.Clamp(Screen.height-p.y-h*.6f,0,Screen.height-h),w,h};}
        IEnumerator Capture(){yield return new WaitForEndOfFrame();if(full){var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/frames/"+(frame++).ToString("D5")+".jpg",image.EncodeToJPG(88));Destroy(image);}}
        IEnumerator Still(string name){yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+name+".png",image.EncodeToPNG());Destroy(image);}
        int Visible(Transform effect){
            var renderers=effect.GetComponentsInChildren<Renderer>(true);var previous=renderers.Select(r=>r.enabled).ToArray();var camera=table.Camera;var saved=camera.targetTexture;var savedHand=table.HandCamera.targetTexture;var active=RenderTexture.active;
            var texture=new RenderTexture(800,432,24);var pixels=new Texture2D(800,432,TextureFormat.RGB24,false);camera.targetTexture=texture;table.HandCamera.targetTexture=texture;
            foreach(var renderer in renderers)renderer.enabled=false;camera.Render();table.GetComponent<NativeEffectCamera>()?.RenderTo(texture);table.HandCamera.Render();fx.RenderOverlay(texture);RenderTexture.active=texture;pixels.ReadPixels(new Rect(0,0,800,432),0,0);pixels.Apply();var before=pixels.GetPixels32();
            for(int i=0;i<renderers.Length;i++)renderers[i].enabled=previous[i];camera.Render();table.GetComponent<NativeEffectCamera>()?.RenderTo(texture);table.HandCamera.Render();fx.RenderOverlay(texture);pixels.ReadPixels(new Rect(0,0,800,432),0,0);pixels.Apply();var after=pixels.GetPixels32();
            int changed=0;for(int i=0;i<before.Length;i++)if(Mathf.Abs(after[i].r-before[i].r)+Mathf.Abs(after[i].g-before[i].g)+Mathf.Abs(after[i].b-before[i].b)>20)changed++;
            camera.targetTexture=saved;table.HandCamera.targetTexture=savedHand;RenderTexture.active=active;texture.Release();Destroy(texture);Destroy(pixels);return changed;
        }
        IEnumerator Run(){
            var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(1600,864),"Original native pool effects"});yield return Wait(8);
            var entries=JsonUtility.FromJson<Manifest>(Resources.Load<TextAsset>("Majsoul/NativeReview/EffectsR11/manifest").text).effects;
            if(corrections)entries=entries.Where(e=>new[]{"ron-ko","effect-liqi-23ex-maozhua","effect-liqi-23ex-liandao","effect-liqi-23ex-xueqiu"}.Contains(e.id)).ToArray();
            if(scythe)entries=entries.Where(e=>e.id=="effect-liqi-23ex-liandao").ToArray();
            foreach(var entry in entries)for(int viewer=0;viewer<4;viewer++){
                var item=WardrobeCatalog.Find(entry.id);Check(item!=null&&item.nativeEffect==entry.nativeEffect,"Catalog uses the native source "+entry.id);
                var shot=new Shot{key=entry.id,title=item.name,group=entry.category,viewer=viewer,start=frame,cue=item.cue};report.shots.Add(shot);
                WinTileEffect win=null;RoomView before=null,declared=null,committed=null;PublicSound declaration=null;Transform target=null;
                if(entry.category=="winEffect"){
                    var view=View("win-4-ron-0-0",viewer,entry);Use(view);yield return Wait();var detail=view.result.details[0];var sound=new PublicSound{seat=detail.seat,from=detail.from,tile=detail.tile,kind="ron",winSource="discard"};target=table.WinningTarget(sound);
                    typeof(TableEffects).GetMethod("SpawnWin",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(fx,new object[]{sound,0f});win=fx.ValidationWin;shot.cueAt=.1f;
                }else{
                    before=View("before",viewer,entry,true);declared=View("declared",viewer,entry,true);committed=View("committed",viewer,entry,true);Use(before);fx.ValidationSequentialClock=true;fx.ValidationTime=0;yield return Wait();declaration=declared.audioEvents.First(e=>e.kind=="discard");
                    shot.cueAt=.2f+SourcePlaybackContracts.Declaration(WardrobeCatalog.Find("hand-human"),item).cue;
                }
                bool sawParticles=false,sawSource=false,sawStick=false;int visible=0;int count=full?Mathf.RoundToInt((entry.category=="winEffect"?5.5f:5)*report.fps):34;
                for(int i=0;i<count;i++){
                    float time=full?i/(float)report.fps:i*.15f;fx.ValidationTime=time;
                    if(entry.category=="riichiEffect"){
                        if(i==(full?Mathf.RoundToInt(.2f*report.fps):2)){client.ValidationApplyState(declared);fx.Accept(declared);typeof(TableEffects).GetMethod("FinishCall",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(fx,null);target=table.FindDiscard(0,declaration.tile,declaration.riverIndex);}
                        if(i==(full?Mathf.RoundToInt(1.4f*report.fps):10)){client.ValidationApplyState(committed);fx.Accept(committed);}
                        sawStick|=fx.ValidationKind=="riichi-stick";
                    }
                    caption=item.name+" · 云间日麻 · 视角"+(viewer+1);yield return Capture();
                    var effect=entry.category=="winEffect"?win:GetComponent<RiichiDiscardVisual>().Latest;
                    if(effect!=null){
                        sawParticles|=effect.ParticleCount>0;
                        if(!sawSource){Check(effect.UsesNativeSource,"Actual table playback uses native resources "+entry.id+" view "+viewer);Check(effect.LayerCount==entry.particleLayers,"Every original particle layer is present "+entry.id+" view "+viewer);Check(effect.GetComponentsInChildren<Renderer>(true).All(r=>r.sharedMaterials.All(m=>m!=null&&m.shader.isSupported&&m.mainTexture!=null)),"Every native material can render "+entry.id+" view "+viewer);sawSource=true;}
                        if(!full&&(i==5||i==8||i==11))visible=Mathf.Max(visible,Visible(effect.transform));
                        if(target!=null)shot.detail=Detail(target.position+Vector3.up*(entry.category=="winEffect"?.6f:0));
                    }
                    if(corrections&&!full&&time>=.3f&&time<=1.8f||i==(full?20:5)||i==(full?38:9)||i==(full?60:14))yield return Still(entry.id+"-"+viewer+"-"+i);
                }
                Check(sawParticles&&sawSource,"Native particles actually play "+entry.id+" view "+viewer);
                if(!full)Check(visible>12,"Effect contributes visible pixels "+entry.id+" view "+viewer+" pixels="+visible);
                if(entry.category=="riichiEffect")Check(sawStick,"Complete declaration includes the stick handoff "+entry.id+" view "+viewer);
                shot.end=frame;fx.ValidationSequentialClock=false;fx.Reset();yield return Wait();
                Debug.Log("R11 reviewed "+entry.id+" view "+viewer);
            }
        }
        void OnGUI(){if(!string.IsNullOrEmpty(caption))GUI.Label(new Rect(20,7,Screen.width-40,32),caption,new GUIStyle(GUI.skin.label){fontSize=22,alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(1,.84f,.6f)}});}
    }
}
