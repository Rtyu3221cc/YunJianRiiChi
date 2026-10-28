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
    [InitializeOnLoad] public static class WardrobeR7Review {
        const string Key="WardrobeR7Review";
        static WardrobeR7Review(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<WardrobeR7Probe>();}};}
        public static void RunStudy(){SessionState.SetBool("WardrobeR7Full",false);Run();}
        public static void RunFull(){SessionState.SetBool("WardrobeR7Full",true);Run();}
        public static void StudySheet(){
            AssetDatabase.Refresh();var go=new GameObject("R7 sheet diagnostic");var cameraObject=new GameObject("R7 sheet camera");var camera=cameraObject.AddComponent<Camera>();camera.transform.position=new Vector3(0,6,-7);camera.transform.LookAt(Vector3.zero);
            var effect=go.AddComponent<WinTileEffect>();effect.InitializeTable("Majsoul/Actions/default-win-effect",Vector3.zero,Quaternion.identity,0,507);
            var ps=effect.GetComponentsInChildren<ParticleSystem>().First(p=>p.name.StartsWith("light"));var renderer=ps.GetComponent<ParticleSystemRenderer>();var lines=new List<string>();
            foreach(float age in new[]{.01f,.04f,.06f,.10f,.14f,.18f,.22f,.26f,.30f,.34f,.38f}){effect.Sample(age);var mesh=new Mesh();renderer.BakeMesh(mesh,camera,true);var uv=mesh.uv.Take(4).ToArray();int actual=Mathf.RoundToInt((1-uv.Max(v=>v.y))*4),expected=Mathf.FloorToInt(Mathf.Repeat(age/.4f*5,1)*2);lines.Add(age+" actualRow="+actual+" sourceRow="+expected+" minV="+uv.Min(v=>v.y)+" maxV="+uv.Max(v=>v.y));UnityEngine.Object.DestroyImmediate(mesh);}
            File.WriteAllLines("../build/wardrobe-r7-study/sheet-samples.txt",lines);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(cameraObject);EditorApplication.Exit(0);
        }
        static void Run(){AssetDatabase.Refresh();AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class WardrobeR7Probe:MonoBehaviour {
        [Serializable] public class Shot {public string key,title,group;public int viewer,start,end;public float[] detail;}
        [Serializable] public class Report {public int frames,fps=30,checks;public bool apkBuilt,githubUploaded;public List<Shot> shots=new List<Shot>();}
        readonly Report report=new Report();string output,caption;bool full,dynamic;float inset,volume;int frame;
        RiichiClient client;TableRenderer table;TableEffects fx;WardrobeResponseIndicator marker;
        IEnumerator Start(){
            full=SessionState.GetBool("WardrobeR7Full",false);output=Path.GetFullPath(full?"../build/wardrobe-r7-review/render":"../build/wardrobe-r7-review/study");Directory.CreateDirectory(output);Directory.CreateDirectory(Path.Combine(output,"frames"));File.Delete(Path.Combine(output,"error.txt"));File.WriteAllText(Path.Combine(output,"checks.txt"),"");
            dynamic=AppPreferences.DynamicOutfits;inset=AppPreferences.ScreenInset;volume=AudioListener.volume;CharacterRoster.BeginPreview();WardrobeSelection.BeginPreview();AppPreferences.SetDynamicOutfits(false);AppPreferences.SetInset(0);AudioListener.volume=0;WardrobeSelection.Save(new Dictionary<string,string>{{"cloth","cloth-default"},{"tileFace","mjpface_default"},{"tileBack","mjp_default"}});Time.captureFramerate=report.fps;
            client=GetComponent<RiichiClient>();table=GetComponent<TableRenderer>();fx=GetComponent<TableEffects>();marker=GetComponent<WardrobeResponseIndicator>();client.BeginLobbyValidation("wardrobe-r7");client.SessionPreviewOnly=true;GetComponent<ResourceRoomCoordinator>().enabled=false;
            var work=new Stack<IEnumerator>();work.Push(Run());while(work.Count>0){object next;try{var it=work.Peek();if(!it.MoveNext()){work.Pop();continue;}next=it.Current;if(next is IEnumerator child){work.Push(child);continue;}}catch(Exception e){File.WriteAllText(Path.Combine(output,"error.txt"),e.ToString());Debug.LogException(e);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            report.frames=frame;File.WriteAllText(Path.Combine(output,"capture.json"),JsonUtility.ToJson(report,true));Cleanup();Debug.Log("WARDROBE R7 PASS "+report.checks+" checks, "+frame+" frames");EditorApplication.Exit(0);
        }
        void Cleanup(){Time.captureFramerate=0;fx.ValidationSequentialClock=false;fx.ValidationFreeze=false;marker.InspectionMode=false;marker.Clear();AudioListener.volume=volume;WardrobeSelection.EndPreview();CharacterRoster.EndPreview();AppPreferences.SetDynamicOutfits(dynamic);AppPreferences.SetInset(inset);}
        void Check(bool ok,string label){if(!ok)throw new Exception(label);report.checks++;File.AppendAllText(Path.Combine(output,"checks.txt"),"PASS "+label+"\n");}
        void Note(string text){File.AppendAllText(Path.Combine(output,"diagnostics.txt"),text+"\n");Debug.Log(text);}
        IEnumerator Wait(int n=2){for(int i=0;i<n;i++)yield return null;}
        RoomView Fixture(string file,int viewer){var v=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/unity-smoke/"+file+".json"));v.you=viewer;v.audioRoom="r7-"+file+"-"+viewer;v.audioEvents=new PublicSound[0];v.audioSeq=0;v.resourceSetup=null;foreach(var p in v.players){p.character="yiji";p.skin=400101;p.wardrobe=new WardrobeLoadout{hand="hand-human",stick="stick-default",riichiEffect="riichi-blue",winEffect="win-default",callout="mpzs_shandian",music="music-riichi"};}return v;}
        void Use(RoomView v){marker.InspectionMode=false;marker.Clear();fx.ValidationSequentialClock=false;client.DesignPreviewSessionTable(v);client.ValidationTableOnly=true;client.ValidationIntro.Reset();fx.Reset();fx.ValidationFreeze=true;fx.Accept(v);}
        float[] Detail(Vector3 point,float w,float h){var p=table.Camera.WorldToScreenPoint(point);return new[]{Mathf.Clamp(p.x-w*.5f,0,Screen.width-w),Mathf.Clamp(Screen.height-p.y-h*.65f,0,Screen.height-h),w,h};}
        Shot Begin(string key,string title,string group,int viewer,float[] detail){caption=title+" · 玩家视角 "+(viewer+1);var s=new Shot{key=key,title=title,group=group,viewer=viewer,start=frame,detail=detail};report.shots.Add(s);return s;}
        IEnumerator Image(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),t.EncodeToPNG());Destroy(t);}
        IEnumerator Record(){yield return new WaitForEndOfFrame();if(full){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,"frames",(frame++).ToString("D5")+".jpg"),t.EncodeToJPG(90));Destroy(t);}}
        int Pixels(ParticleSystemRenderer renderer)=>(int)typeof(RuntimeValidation).GetMethod("VisibleEffectPixels",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{table,renderer});
        int Pixels(Transform root)=>root.GetComponentsInChildren<ParticleSystemRenderer>().Max(Pixels);
        IEnumerator Run(){
            var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(1688,768),"Wardrobe r7 reference comparison"});yield return Wait(8);
            yield return Lightning();yield return Blue();yield return Callouts();
        }
        IEnumerator Lightning(){
            WardrobeSelection.Save(new Dictionary<string,string>{{"cloth","cloth-chigua"}});
            for(int viewer=0;viewer<4;viewer++){
                var v=Fixture("win-4-ron-0-0",viewer);Use(v);yield return Wait();var d=v.result.details[0];var e=new PublicSound{seat=d.seat,from=d.from,tile=d.tile,kind="ron"};var target=table.WinningTarget(e);var go=new GameObject("R7 lightning");var effect=go.AddComponent<WinTileEffect>();effect.Initialize(table.WinningPoint(e),0,0,507,"Majsoul/Actions/default-win-effect");
                var ps=effect.GetComponentsInChildren<ParticleSystem>().First(p=>p.name.StartsWith("light"));Check(ps.textureSheetAnimation.enabled&&ps.textureSheetAnimation.numTilesX==1&&ps.textureSheetAnimation.numTilesY==4,"Lightning enables its one-column four-row sheet in view "+viewer);Check(ps.textureSheetAnimation.cycleCount==5,"Lightning preserves source sheet cycles");effect.Sample(.06f);yield return Wait();var renderer=ps.GetComponent<ParticleSystemRenderer>();var mesh=new Mesh();renderer.BakeMesh(mesh,table.Camera,true);var uv=mesh.uv.Take(4).ToArray();float span=uv.Max(p=>p.y)-uv.Min(p=>p.y);Check(Mathf.Abs(span-.25f)<.001f,"Lightning renders one sheet row instead of the entire texture");Destroy(mesh);
                Check(ps.particleCount==2,"Lightning uses the original emission seed and two bolts, rather than maximum three");
                var shot=Begin("lightning","雷击 · 序列与原粒子数量", "win",viewer,Detail(target.position+Vector3.up*.5f,640,620));
                for(int i=0;i<(full?84:12);i++){float age=full?i/(float)report.fps-.2f:i*.04f;effect.Sample(age);if(i==(full?9:3))yield return Image("lightning-view-"+viewer);yield return Record();}
                shot.end=frame;Destroy(go);yield return Wait();
            }
        }
        IEnumerator Blue(){
            WardrobeSelection.Save(new Dictionary<string,string>{{"cloth","cloth-default"}});
            for(int viewer=0;viewer<4;viewer++){
                var v=Fixture("preview-4",viewer);int last=v.players[0].river.Length-1;v.players[0].river[last].riichi=true;Use(v);yield return Wait();var target=table.FindDiscard(0,v.players[0].river[last].tile,last+1);fx.ValidationPlay(0,"discard",v.players[0].river[last].tile,true,last+1);fx.ValidationTime=fx.ValidationRiichiCue;yield return Wait(2);fx.ValidationTime=fx.ValidationRiichiCue+.15f;yield return Wait(2);var effect=GetComponent<RiichiDiscardVisual>().Latest;Check(effect!=null,"Blue flame starts at the configured source cue");var carried=fx.ValidationAnimatedTile;Check(Vector3.Dot(effect.transform.TransformDirection(Vector3.back),carried.up)>.999f,"Blue flame follows the front-face model basis");Check(Mathf.Abs(effect.transform.lossyScale.x-TableTileGeometry.WorldWidth(carried)/SourcePlaybackContracts.TileWidth)<.001f,"Blue flame scales with the actual tile model");Check(Mathf.Abs(fx.ValidationRiichiCue-.275f)<.001f&&Mathf.Abs(fx.ValidationPlayLength-.8075f)<.001f,"Human riichi applies original speed two to cue and lifetime");int visible=Pixels(effect.transform);Note("BLUE view="+viewer+" pixels="+visible+" front="+effect.transform.forward+" tileUp="+carried.up+" scale="+effect.transform.lossyScale);Check(visible>30,"Blue flame has visible front-face pixels");yield return Image("blue-peak-"+viewer);
                v.audioRoom+="-record";Use(v);yield return Wait();target=table.FindDiscard(0,v.players[0].river[last].tile,last+1);var shot=Begin("riichi-blue","苍火 · 原牌面坐标与播放速度", "riichi",viewer,Detail(target.position,640,430));fx.ValidationPlay(0,"discard",v.players[0].river[last].tile,true,last+1);
                for(int i=0;i<(full?90:15);i++){fx.ValidationTime=full?i/(float)report.fps:i*.12f;if(i==(full?15:4))yield return Image("blue-view-"+viewer);yield return Record();}
                shot.end=frame;fx.Reset();
            }
        }
        IEnumerator Callouts(){
            WardrobeSelection.Save(new Dictionary<string,string>{{"cloth","cloth-default"}});
            foreach(var item in WardrobeCatalog.Items("callout"))for(int viewer=0;viewer<4;viewer++){
                var v=Fixture("preview-4",viewer);v.players[viewer].wardrobe.callout=item.id;Use(v);yield return Wait();int last=v.players[0].river.Length-1;var target=table.FindDiscard(0,v.players[0].river[last].tile,last+1);marker.InspectionMode=true;marker.InspectionTarget=target;marker.InspectionKey=item.id+":"+viewer;marker.ValidationFreeze=true;marker.ValidationAge=.5f;yield return Wait(3);
                bool overlay=item.id!="effect_zhishi";foreach(var r in marker.EffectRoot.GetComponentsInChildren<ParticleSystemRenderer>())Check(r.sharedMaterial.GetFloat("_ZTest")== (overlay?8:4),item.id+" preserves the intended depth policy");
                var blocker=GameObject.CreatePrimitive(PrimitiveType.Quad);blocker.name="R7 occlusion probe";blocker.transform.SetPositionAndRotation(table.Camera.transform.position+table.Camera.transform.forward*2,table.Camera.transform.rotation);blocker.transform.localScale=Vector3.one*40;var material=new Material(Shader.Find("Unlit/Color")){color=Color.black};blocker.GetComponent<MeshRenderer>().sharedMaterial=material;yield return Wait();int pixels=Pixels(marker.EffectRoot);Check(overlay?pixels>10:pixels==0,item.id+" actual opaque-occluder pixel test view "+viewer);Note("OCCLUSION "+item.id+" view="+viewer+" overlay="+overlay+" visiblePixels="+pixels);Destroy(blocker);Destroy(material);yield return Wait();
                var shot=Begin(item.id,item.name+(overlay?" · 邻牌不遮挡":" · 保留普通深度"),"callout",viewer,Detail(target.position,540,400));
                for(int i=0;i<(full?78:8);i++){marker.ValidationAge=full?i/(float)report.fps:i*.1f;if(i==(full?15:5))yield return Image(item.id+"-view-"+viewer);yield return Record();}
                shot.end=frame;marker.InspectionMode=false;marker.Clear();yield return Wait();
            }
        }
        void OnGUI(){if(!string.IsNullOrEmpty(caption))GUI.Label(new Rect(25,7,Screen.width-50,34),caption,new GUIStyle(GUI.skin.label){fontSize=22,alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(1,.84f,.6f)}});}
    }
}
