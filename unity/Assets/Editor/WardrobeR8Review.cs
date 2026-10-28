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
    [InitializeOnLoad] public static class WardrobeR8Review {
        const string Key="WardrobeR8Review";
        static WardrobeR8Review(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<WardrobeR8Probe>();}};}
        public static void RunStudy(){SessionState.SetBool("WardrobeR8Full",false);Run();}
        public static void RunFull(){SessionState.SetBool("WardrobeR8Full",true);Run();}
        static void Run(){AssetDatabase.Refresh();AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class WardrobeR8Probe:MonoBehaviour {
        [Serializable] public class Shot {public string key,title;public int viewer,start,end;public float sampleAge;public float[] detail;public double oldInkError,newInkError;public int oldOuterPixels,newOuterPixels;}
        [Serializable] public class Report {public int frames,fps=30,checks;public bool apkBuilt,githubUploaded;public List<Shot> shots=new List<Shot>();}
        readonly Report report=new Report();string output,caption;bool full,dynamic;float inset,volume;int frame;
        RiichiClient client;TableRenderer table;TableEffects fx;WardrobeResponseIndicator marker;
        IEnumerator Start(){
            full=SessionState.GetBool("WardrobeR8Full",false);output=Path.GetFullPath(full?"../build/wardrobe-r8-review/render":"../build/wardrobe-r8-review/study");Directory.CreateDirectory(output);Directory.CreateDirectory(Path.Combine(output,"frames"));File.Delete(Path.Combine(output,"error.txt"));File.WriteAllText(Path.Combine(output,"checks.txt"),"");
            dynamic=AppPreferences.DynamicOutfits;inset=AppPreferences.ScreenInset;volume=AudioListener.volume;CharacterRoster.BeginPreview();WardrobeSelection.BeginPreview();AppPreferences.SetDynamicOutfits(false);AppPreferences.SetInset(0);AudioListener.volume=0;WardrobeSelection.Save(new Dictionary<string,string>{{"cloth","cloth-default"},{"tileFace","mjpface_default"},{"tileBack","mjp_default"}});Time.captureFramerate=report.fps;
            client=GetComponent<RiichiClient>();table=GetComponent<TableRenderer>();fx=GetComponent<TableEffects>();marker=GetComponent<WardrobeResponseIndicator>();client.BeginLobbyValidation("wardrobe-r8");client.SessionPreviewOnly=true;GetComponent<ResourceRoomCoordinator>().enabled=false;
            var work=new Stack<IEnumerator>();work.Push(Run());while(work.Count>0){object next;try{var it=work.Peek();if(!it.MoveNext()){work.Pop();continue;}next=it.Current;if(next is IEnumerator child){work.Push(child);continue;}}catch(Exception e){File.WriteAllText(Path.Combine(output,"error.txt"),e.ToString());Debug.LogException(e);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            report.frames=frame;File.WriteAllText(Path.Combine(output,"capture.json"),JsonUtility.ToJson(report,true));Cleanup();Debug.Log("WARDROBE R8 PASS "+report.checks+" checks, "+frame+" frames");EditorApplication.Exit(0);
        }
        void Cleanup(){Time.captureFramerate=0;fx.ValidationFreeze=false;marker.InspectionMode=false;marker.ValidationRawOverlay=false;marker.Clear();AudioListener.volume=volume;WardrobeSelection.EndPreview();CharacterRoster.EndPreview();AppPreferences.SetDynamicOutfits(dynamic);AppPreferences.SetInset(inset);}
        void Check(bool ok,string label){if(!ok)throw new Exception(label);report.checks++;File.AppendAllText(Path.Combine(output,"checks.txt"),"PASS "+label+"\n");}
        IEnumerator Wait(int n=2){for(int i=0;i<n;i++)yield return null;}
        RoomView Fixture(int viewer,string callout){var v=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/unity-smoke/preview-4.json"));v.you=viewer;v.audioRoom="r8-"+viewer+":"+callout;v.audioEvents=new PublicSound[0];v.audioSeq=0;v.resourceSetup=null;foreach(var p in v.players){p.character="yiji";p.skin=400101;p.wardrobe=new WardrobeLoadout{hand="hand-human",stick="stick-default",riichiEffect="riichi-blue",winEffect="win-default",callout=callout,music="music-riichi"};}return v;}
        void Use(RoomView v){marker.InspectionMode=false;marker.Clear();fx.ValidationSequentialClock=false;client.DesignPreviewSessionTable(v);client.ValidationTableOnly=true;client.ValidationIntro.Reset();fx.Reset();fx.ValidationFreeze=true;fx.Accept(v);}
        float[] Detail(Vector3 point){const float w=480,h=360;var p=table.Camera.WorldToScreenPoint(point);return new[]{Mathf.Clamp(p.x-w*.5f,0,Screen.width-w),Mathf.Clamp(Screen.height-p.y-h*.55f,0,Screen.height-h),w,h};}
        IEnumerator Record(){yield return new WaitForEndOfFrame();if(full){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,"frames",(frame++).ToString("D5")+".jpg"),t.EncodeToJPG(90));Destroy(t);}}
        Color32[] Capture(string name){
            var camera=table.Camera;var previous=camera.targetTexture;var active=RenderTexture.active;var target=RenderTexture.GetTemporary(1688,768,24);var pixels=new Texture2D(1688,768,TextureFormat.RGB24,false);
            camera.targetTexture=target;table.HandCamera.targetTexture=target;table.FitCamera(1688f/768);camera.Render();table.HandCamera.Render();fx.RenderOverlay(target);RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1688,768),0,0);pixels.Apply();if(name!=null)File.WriteAllBytes(Path.Combine(output,name+".png"),pixels.EncodeToPNG());var colors=pixels.GetPixels32();camera.targetTexture=previous;table.HandCamera.targetTexture=null;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);Destroy(pixels);return colors;
        }
        static int Difference(Color32 a,Color32 b)=>Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b);
        void Measure(Transform tile,Color32[] baseline,Color32[] old,Color32[] current,Shot shot){
            var box=tile.GetComponent<BoxCollider>();var center=tile.TransformPoint(box.center+Vector3.up*box.size.y*.5f);var plane=new Plane(tile.up,center);var screen=table.Camera.WorldToScreenPoint(center);double oldError=0,newError=0;int ink=0;
            for(int y=Mathf.Max(0,(int)screen.y-160);y<Mathf.Min(768,(int)screen.y+160);y++)for(int x=Mathf.Max(0,(int)screen.x-160);x<Mathf.Min(1688,(int)screen.x+160);x++){
                var ray=table.Camera.ScreenPointToRay(new Vector3(x+.5f,y+.5f));if(!plane.Raycast(ray,out float distance))continue;var local=tile.InverseTransformPoint(ray.GetPoint(distance))-box.center;float ax=Mathf.Abs(local.x)/(box.size.x*.5f),az=Mathf.Abs(local.z)/(box.size.z*.5f);int p=y*1688+x;
                if(ax<.62f&&az<.62f){var b=baseline[p];if(b.r*.299f+b.g*.587f+b.b*.114f<150){ink++;oldError+=Difference(old[p],b);newError+=Difference(current[p],b);}}
                if(Mathf.Max(ax,az)>1.1f){if(Difference(old[p],baseline[p])>30)shot.oldOuterPixels++;if(Difference(current[p],baseline[p])>30)shot.newOuterPixels++;}
            }
            Check(ink>10,"Readable glyph sample exists "+shot.key+" view "+shot.viewer);shot.oldInkError=oldError/ink;shot.newInkError=newError/ink;
            if(shot.key=="effect_zhishi")Check(Math.Abs(shot.oldInkError-shot.newInkError)<2,"Ordinary guide keeps tile-face appearance view "+shot.viewer);
            else{Check(shot.newInkError<=Math.Max(shot.oldInkError*.60,2),"Tile glyph obstruction reduced "+shot.key+" view "+shot.viewer+" age="+shot.sampleAge+" old="+shot.oldInkError.ToString("F2")+" new="+shot.newInkError.ToString("F2"));if(shot.oldOuterPixels>20)Check(shot.newOuterPixels>=shot.oldOuterPixels*.15,"Surrounding indicator remains visible "+shot.key+" view "+shot.viewer);}
        }
        IEnumerator Run(){
            var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(1688,768),"Wardrobe R8 tile readability"});yield return Wait(8);
            foreach(var item in WardrobeCatalog.Items("callout"))for(int viewer=0;viewer<4;viewer++){
                var v=Fixture(viewer,item.id);Use(v);yield return Wait();int last=v.players[0].river.Length-1;var target=table.FindDiscard(0,v.players[0].river[last].tile,last+1);marker.InspectionMode=true;marker.InspectionTarget=target;marker.InspectionKey=item.id+":"+viewer;marker.ValidationFreeze=true;marker.ValidationAge=.5f;marker.ValidationRawOverlay=true;yield return Wait(3);
                var shot=new Shot{key=item.id,title=item.name,viewer=viewer,detail=Detail(target.position),sampleAge=.5f};report.shots.Add(shot);
                foreach(float age in new[]{.15f,.5f,1.1f}){
                    marker.ValidationAge=age;marker.ValidationRawOverlay=true;yield return Wait(2);bool save=age==.5f;var old=Capture(save?item.id+"-old-"+viewer:null);
                    // Hide only renderers: disabling a particle object resets its
                    // simulation. All three renders use the same simulation/frame.
                    var renderers=marker.EffectRoot.GetComponentsInChildren<ParticleSystemRenderer>();foreach(var renderer in renderers)renderer.enabled=false;var baseline=Capture(null);foreach(var renderer in renderers)renderer.enabled=true;
                    marker.ValidationRawOverlay=false;marker.ValidationApplyReadability();var current=Capture(save?item.id+"-new-"+viewer:null);Measure(target,baseline,old,current,save?shot:new Shot{key=item.id,viewer=viewer,sampleAge=age});
                }
                foreach(var renderer in marker.EffectRoot.GetComponentsInChildren<ParticleSystemRenderer>())Check(renderer.sharedMaterial.GetFloat("_ZTest")== (item.id=="effect_zhishi"?4:8),"Existing neighbor-depth policy retained");
                caption=item.name+" · 优化后 · 视角 "+(viewer+1);shot.start=frame;
                for(int i=0;i<(full?78:8);i++){marker.ValidationAge=full?i/(float)report.fps:i*.1f;yield return Record();}shot.end=frame;marker.InspectionMode=false;marker.Clear();yield return Wait();
            }
        }
        void OnGUI(){if(!string.IsNullOrEmpty(caption))GUI.Label(new Rect(25,7,Screen.width-50,34),caption,new GUIStyle(GUI.skin.label){fontSize=22,alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(1,.84f,.6f)}});}
    }
}
