using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    [InitializeOnLoad] public static class WardrobeFourViewsReview {
        const string Key="WardrobeFourViewsReview";
        static WardrobeFourViewsReview(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<WardrobeFourViewsProbe>();}};}
        public static void RunStudy(){SessionState.SetBool("WardrobeFourViewsFull",false);Run();}
        public static void RunFull(){SessionState.SetBool("WardrobeFourViewsFull",true);Run();}
        static void Run(){AssetDatabase.Refresh();foreach(var key in new[]{"effect_zhishi","mpzs_23winter"})ImportNativeReview.ImportDecoration(key);AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class WardrobeFourViewsProbe:MonoBehaviour {
        [Serializable] public class Shot {public string effect,title;public int viewer,start,end;public float[] detail;}
        [Serializable] public class Motion {public string effect;public int viewer,particles;public float averageUp,minimumUp;}
        [Serializable] public class Report {public int frames,fps=20,checks;public bool apkBuilt=false;public List<Shot> shots=new List<Shot>();public List<Motion> motion=new List<Motion>();}
        readonly Report report=new Report();string output,caption;bool full,live;float listener,inset;int frame;RiichiClient client;TableRenderer table;TableEffects fx;WardrobeResponseIndicator marker;
        IEnumerator Start(){
            full=SessionState.GetBool("WardrobeFourViewsFull",false);output=Path.GetFullPath(full?"../build/wardrobe-preview-r5/render":"../build/wardrobe-orientation-r5-study/render");Directory.CreateDirectory(output);Directory.CreateDirectory(Path.Combine(output,"frames"));File.Delete(Path.Combine(output,"error.txt"));File.WriteAllText(Path.Combine(output,"checks.txt"),"");File.WriteAllText(Path.Combine(output,"motion-study.txt"),"");
            listener=AudioListener.volume;AudioListener.volume=0;live=AppPreferences.DynamicOutfits;inset=AppPreferences.ScreenInset;AppPreferences.SetDynamicOutfits(false);AppPreferences.SetInset(0);CharacterRoster.BeginPreview();WardrobeSelection.BeginPreview();WardrobeSelection.Save(new Dictionary<string,string>{{"cloth","cloth-default"},{"tileFace","mjpface_default"},{"tileBack","mjp_default"}});Time.captureFramerate=20;
            client=GetComponent<RiichiClient>();table=GetComponent<TableRenderer>();fx=GetComponent<TableEffects>();marker=GetComponent<WardrobeResponseIndicator>();GetComponent<ResourceRoomCoordinator>().enabled=false;
            var stack=new Stack<IEnumerator>();stack.Push(Capture());while(stack.Count>0){object next;try{var it=stack.Peek();if(!it.MoveNext()){stack.Pop();continue;}next=it.Current;if(next is IEnumerator child){stack.Push(child);continue;}}catch(Exception e){File.WriteAllText(Path.Combine(output,"error.txt"),e.ToString());Debug.LogException(e);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            report.frames=frame;File.WriteAllText(Path.Combine(output,"capture.json"),JsonUtility.ToJson(report,true));Cleanup();Debug.Log("WARDROBE FOUR VIEWS PASS "+report.checks+" assertions, "+frame+" frames");EditorApplication.Exit(0);
        }
        void Cleanup(){marker.InspectionMode=false;marker.InspectionTarget=null;marker.Clear();fx.ValidationFreeze=false;Time.captureFramerate=0;AudioListener.volume=listener;AppPreferences.SetDynamicOutfits(live);AppPreferences.SetInset(inset);CharacterRoster.EndPreview();WardrobeSelection.EndPreview();}
        void Check(bool value,string message){if(!value)throw new Exception(message);report.checks++;File.AppendAllText(Path.Combine(output,"checks.txt"),"PASS "+message+"\n");}
        IEnumerator Wait(int n=2){for(int i=0;i<n;i++)yield return null;}
        RoomView Fixture(string kind,int viewer){
            var view=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/unity-smoke/"+(kind=="win"?"win-4-tsumo-0-0":"preview-4")+".json"));view.you=viewer;view.audioRoom="four-views-"+frame+"-"+viewer+"-"+caption;view.audioSeq=0;view.audioEvents=new PublicSound[0];view.resourceSetup=null;
            foreach(var p in view.players){p.character="yiji";p.skin=400101;p.wardrobe=new WardrobeLoadout{stick="stick-default",hand="hand-human",callout="mpzs_shandian",riichiEffect="riichi-ice",winEffect="ron-tornado"};}return view;
        }
        void Use(RoomView view){marker.InspectionMode=false;marker.InspectionTarget=null;marker.Clear();client.DesignPreviewSessionTable(view);client.ValidationTableOnly=true;client.ValidationIntro.Reset();fx.Reset();fx.ValidationFreeze=true;}
        float[] Detail(Vector3 point){var p=table.Camera.WorldToScreenPoint(point);return new[]{Mathf.Clamp(p.x-220,0,Screen.width-440),Mathf.Clamp(Screen.height-p.y-210,0,Screen.height-320),440f,320f};}
        IEnumerator Image(string path,bool png){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(path,png?t.EncodeToPNG():t.EncodeToJPG(90));Destroy(t);}
        IEnumerator Record(){yield return Image(Path.Combine(output,"frames",(frame++).ToString("D5")+".jpg"),false);}
        void BubbleDirection(int viewer){
            var ps=marker.Effect.GetComponentsInChildren<ParticleSystem>().First(p=>p.name=="paopao");var particles=new ParticleSystem.Particle[ps.main.maxParticles];int n=ps.GetParticles(particles);var directions=new List<float>();
            for(int i=0;i<n;i++)if(particles[i].velocity.sqrMagnitude>1e-8f){var v=ps.main.simulationSpace==ParticleSystemSimulationSpace.World?particles[i].velocity:ps.transform.TransformDirection(particles[i].velocity);directions.Add(Vector3.Dot(v.normalized,Vector3.up));}
            Check(directions.Count>0,"Bubble particles have measurable velocities at viewer "+viewer);float average=directions.Average();report.motion.Add(new Motion{effect="mpzs_22event",viewer=viewer,particles=directions.Count,averageUp=average,minimumUp=directions.Min()});File.AppendAllText(Path.Combine(output,"motion-study.txt"),"viewer="+viewer+" shape="+ps.shape.shapeType+" space="+ps.main.simulationSpace+" averageUp="+average+" minimumUp="+directions.Min()+" forward="+ps.transform.forward+" firstVelocity="+particles[0].velocity+"\n");
            // The original emitter is a hemisphere, so the expected mean upward
            // cosine is about 0.5, not 1.0. Preserve its natural sideways drift.
            Check(ps.shape.shapeType==ParticleSystemShapeType.Hemisphere,"Original bubble hemisphere is preserved");Check(average>.3f&&directions.Min()>-.05f,"Bubble hemisphere opens upward at viewer "+viewer);
        }
        IEnumerator Capture(){
            var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(1688,768),"Four player viewpoints"});yield return Wait(8);
            var ids=full?new[]{"mpzs_22event","mpzs_shandian","effect_zhishi","mpzs_23winter","ron-flame","ron-tornado","riichi-ice"}:new[]{"mpzs_22event","mpzs_shandian","effect_zhishi","mpzs_23winter"};
            foreach(string id in ids)for(int viewer=0;viewer<4;viewer++){
                var item=WardrobeCatalog.Find(id);caption=item.name+" · 玩家视角 "+(viewer+1)+"/4";bool callout=item.category=="callout",win=item.category=="winEffect";var view=Fixture(win?"win":"table",viewer);if(callout)view.players[viewer].wardrobe.callout=id;
                int last=view.players[0].river.Length-1;if(callout)view.last=new LastDiscard{from=0,tile=view.players[0].river[last].tile};if(!callout&&!win)view.players[0].river[last].riichi=true;Use(view);yield return Wait(3);Transform target;WinTileEffect effect=null;
                if(callout){target=table.FindDiscard(0,view.players[0].river[last].tile,last+1);marker.InspectionMode=true;marker.InspectionTarget=target;marker.InspectionKey=id+":"+viewer;marker.ValidationFreeze=true;marker.ValidationAge=0;yield return Wait(2);}
                else if(win){target=table.WinningTarget(new PublicSound{seat=0,kind="tsumo"});var go=new GameObject(id);go.transform.SetParent(table.transform,false);effect=go.AddComponent<WinTileEffect>();effect.Initialize(table.WinningPoint(new PublicSound{seat=0,kind="tsumo"}),table.SeatAngle(0),0,507,item.effect);}
                else{target=table.FindDiscard(0,view.players[0].river[last].tile,last+1);fx.Accept(view);fx.ValidationPlay(0,"discard",view.players[0].river[last].tile,true,last+1);}
                var shot=new Shot{effect=id,title=item.name,viewer=viewer,start=frame,detail=Detail(target.position+Vector3.up*table.Geometry.Depth*.5f)};report.shots.Add(shot);
                foreach(float age in full?Enumerable.Range(0,64).Select(i=>i/20f):new[]{.15f,.5f,1.2f}){
                    if(callout)marker.ValidationAge=age;else if(win)effect.Sample(age);else fx.ValidationTime=age;yield return Wait(full?1:2);
                    if(callout&&age>=.49f&&age<=.51f){var authored=marker.Effect.transform.GetChild(0).GetChild(0);Check(Vector3.Dot(authored.forward,Vector3.up)>.999f,id+" authored plane is horizontal at viewer "+viewer);if(id=="mpzs_22event")BubbleDirection(viewer);}
                    if(full)yield return Record();else yield return Image(Path.Combine(output,id+"-view-"+viewer+"-"+age.ToString("F2")+".png"),true);
                    if(full&&Mathf.Abs(age-.5f)<.001f)yield return Image(Path.Combine(output,id+"-view-"+viewer+".png"),true);
                }
                if(callout){Check(marker.Effect.ParticleCount>0,id+" remains alive in view "+viewer);foreach(var renderer in marker.Effect.GetComponentsInChildren<ParticleSystemRenderer>())Check(renderer.sharedMaterial.GetFloat("_SrcBlend")>0&&renderer.sharedMaterial.GetFloat("_DstBlend")>0,id+" material has valid source blending");marker.InspectionMode=false;marker.InspectionTarget=null;marker.Clear();}else if(win)Destroy(effect.gameObject);else fx.Reset();shot.end=frame;yield return Wait();
            }
        }
        void OnGUI(){if(!string.IsNullOrEmpty(caption))GUI.Label(new Rect(Screen.width/2-330,8,660,34),caption,new GUIStyle(GUI.skin.label){fontSize=20,alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(1,.84f,.5f)}});}
    }
}
