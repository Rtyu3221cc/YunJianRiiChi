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
    [InitializeOnLoad] public static class WardrobeR9Review {
        const string Key="WardrobeR9Review";
        static WardrobeR9Review(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<WardrobeR9Probe>();}};}
        public static void RunStudy(){SessionState.SetBool("WardrobeR9Patch",false);SessionState.SetBool("WardrobeR9Full",false);Run();}
        public static void RunFull(){SessionState.SetBool("WardrobeR9Patch",false);SessionState.SetBool("WardrobeR9Full",true);Run();}
        public static void RunReactorPatch(){SessionState.SetBool("WardrobeR9Patch",true);SessionState.SetBool("WardrobeR9Full",true);Run();}
        static void Run(){AssetDatabase.Refresh();ProjectSetup.Configure();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class WardrobeR9Probe:MonoBehaviour {
        [Serializable] public class Shot {public string key,title,group;public int viewer,start,end,sourceLayers;public float[] detail;}
        [Serializable] public class Report {public int frames,fps=30,checks;public bool apkBuilt;public List<Shot> shots=new List<Shot>();}
        readonly Report report=new Report();string output,caption;bool full,dynamic,patch;float inset,volume;int frame;
        RiichiClient client;TableRenderer table;TableEffects fx;WardrobeResponseIndicator marker;
        readonly string[] callouts={"callout-sakura","callout-frost","callout-vortex"},riichi={"riichi-afterimage","riichi-feather"},wins={"ron-blackfire","ron-reactor"};
        IEnumerator Start(){
            full=SessionState.GetBool("WardrobeR9Full",false);patch=SessionState.GetBool("WardrobeR9Patch",false);SessionState.SetBool("WardrobeR9Patch",false);output=Path.GetFullPath(patch?"../build/wardrobe-r9-review/reactor-patch":full?"../build/wardrobe-r9-review/render":"../build/wardrobe-r9-review/study");Directory.CreateDirectory(output);Directory.CreateDirectory(Path.Combine(output,"frames"));File.Delete(Path.Combine(output,"error.txt"));File.WriteAllText(Path.Combine(output,"checks.txt"),"");
            dynamic=AppPreferences.DynamicOutfits;inset=AppPreferences.ScreenInset;volume=AudioListener.volume;CharacterRoster.BeginPreview();WardrobeSelection.BeginPreview();AppPreferences.SetDynamicOutfits(false);AppPreferences.SetInset(0);AudioListener.volume=0;WardrobeSelection.Save(new Dictionary<string,string>{{"cloth","cloth-default"},{"tileFace","mjpface_default"},{"tileBack","mjp_default"}});Time.captureFramerate=report.fps;
            client=GetComponent<RiichiClient>();table=GetComponent<TableRenderer>();fx=GetComponent<TableEffects>();marker=GetComponent<WardrobeResponseIndicator>();client.BeginLobbyValidation("wardrobe-r9");client.SessionPreviewOnly=true;GetComponent<ResourceRoomCoordinator>().enabled=false;
            var work=new Stack<IEnumerator>();work.Push(Run());while(work.Count>0){object next;try{var it=work.Peek();if(!it.MoveNext()){work.Pop();continue;}next=it.Current;if(next is IEnumerator child){work.Push(child);continue;}}catch(Exception e){File.WriteAllText(Path.Combine(output,"error.txt"),e.ToString());Debug.LogException(e);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            report.frames=frame;File.WriteAllText(Path.Combine(output,"capture.json"),JsonUtility.ToJson(report,true));Cleanup();Debug.Log("WARDROBE R9 PASS "+report.checks+" checks, "+frame+" frames");EditorApplication.Exit(0);
        }
        void Cleanup(){Time.captureFramerate=0;fx.ValidationFreeze=false;marker.InspectionMode=false;marker.Clear();AudioListener.volume=volume;WardrobeSelection.EndPreview();CharacterRoster.EndPreview();AppPreferences.SetDynamicOutfits(dynamic);AppPreferences.SetInset(inset);}
        void Check(bool ok,string label){if(!ok)throw new Exception(label);report.checks++;File.AppendAllText(Path.Combine(output,"checks.txt"),"PASS "+label+"\n");}
        IEnumerator Wait(int n=2){for(int i=0;i<n;i++)yield return null;}
        IEnumerator Image(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),t.EncodeToPNG());Destroy(t);}
        IEnumerator Record(){yield return new WaitForEndOfFrame();if(full){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,"frames",(frame++).ToString("D5")+".jpg"),t.EncodeToJPG(90));Destroy(t);}}
        RoomView Fixture(string file,int viewer,WardrobeItem item){var v=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/unity-smoke/"+file+".json"));v.you=viewer;v.audioRoom="r9-"+item.id+":"+viewer;v.audioEvents=new PublicSound[0];v.audioSeq=0;v.resourceSetup=null;foreach(var p in v.players){p.character="yiji";p.skin=400101;p.wardrobe=new WardrobeLoadout{hand="hand-human",stick="stick-default",riichiEffect=item.category=="riichiEffect"?item.id:"riichi-blue",winEffect=item.category=="winEffect"?item.id:"win-default",callout=item.category=="callout"?item.id:"mpzs_shandian",music="music-riichi"};}return v;}
        void Use(RoomView v){marker.InspectionMode=false;marker.Clear();fx.ValidationSequentialClock=false;client.DesignPreviewSessionTable(v);client.ValidationTableOnly=true;client.ValidationIntro.Reset();fx.Reset();fx.ValidationFreeze=true;fx.Accept(v);}
        float[] Detail(Vector3 point,string group){float w=group=="win"?700:540,h=group=="win"?620:420;var p=table.Camera.WorldToScreenPoint(point);return new[]{Mathf.Clamp(p.x-w*.5f,0,Screen.width-w),Mathf.Clamp(Screen.height-p.y-h*.65f,0,Screen.height-h),w,h};}
        int Pixels(Transform root)=>root.GetComponentsInChildren<ParticleSystemRenderer>().Max(r=>(int)typeof(RuntimeValidation).GetMethod("VisibleEffectPixels",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{table,r}));
        IEnumerator Run(){
            var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(1688,768),"Wardrobe R9 additions"});yield return Wait(8);
            foreach(var pair in new[]{new KeyValuePair<string,string[]>("callout",callouts),new KeyValuePair<string,string[]>("riichiEffect",riichi),new KeyValuePair<string,string[]>("winEffect",wins)}){
                if(patch)continue;
                var lobby=client.Lobby;lobby.PreviewWardrobe(pair.Key);yield return Wait(5);
                foreach(var id in pair.Value){Check(lobby.ValidationTarget("dress-item-"+id).width>0,"New choice is visible in wardrobe "+id);lobby.ValidationPress("dress-item-"+id);yield return Wait(2);lobby.ValidationPress("dress-save");yield return Wait(2);Check(WardrobeSelection.Get(pair.Key)?.id==id,"New choice saves and reloads "+id);}
                Check(lobby.ValidationTarget("dress-eye").width==0,"Table-effect preview remains disabled in wardrobe "+pair.Key);yield return Image("menu-"+pair.Key);
            }
            foreach(string group in new[]{"callout","riichi","win"})foreach(string id in group=="callout"?callouts:group=="riichi"?riichi:wins)for(int viewer=0;viewer<4;viewer++){
                if(patch&&id!="ron-reactor")continue;
                var item=WardrobeCatalog.Find(id);var view=Fixture(group=="win"?"win-4-ron-0-0":"preview-4",viewer,item);int last=view.players[0].river.Length-1;if(group=="riichi")view.players[0].river[last].riichi=true;Use(view);yield return Wait();Transform target;WinTileEffect win=null;
                if(group=="win"){
                    var d=view.result.details[0];var e=new PublicSound{seat=d.seat,from=d.from,tile=d.tile,kind="ron",winSource="discard"};target=table.WinningTarget(e);typeof(TableEffects).GetMethod("SpawnWin",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(fx,new object[]{e,0f});win=fx.ValidationWin;Check(win!=null&&win.SoundCue==item.cue,"Actual winning choice selects its source graph and sound "+id);
                }else{
                    target=table.FindDiscard(0,view.players[0].river[last].tile,last+1);
                    if(group=="callout"){marker.InspectionMode=true;marker.InspectionTarget=target;marker.InspectionKey=id+":"+viewer;marker.ValidationFreeze=true;marker.ValidationAge=0;}
                    else fx.ValidationPlay(0,"discard",view.players[0].river[last].tile,true,last+1);
                }
                var config=JsonUtility.FromJson<WinTileEffect.Config>(Resources.Load<TextAsset>(item.effect).text);var shot=new Shot{key=id,title=item.name,group=group,viewer=viewer,start=frame,sourceLayers=config.layers.Length,detail=Detail(target.position+Vector3.up*(group=="win"?.7f:0),group)};report.shots.Add(shot);caption=item.name+" · 玩家视角 "+(viewer+1);int maximumPixels=0;bool particles=false,validated=false;
                int count=full?(group=="win"?120:90):12;
                for(int i=0;i<count;i++){
                    float age=full?i/(float)report.fps:i*.12f;if(group=="callout")marker.ValidationAge=age;else fx.ValidationTime=age;
                    yield return Record();var owner=group=="callout"?marker.SourceEffect:group=="win"?win:GetComponent<RiichiDiscardVisual>().Latest;
                    if(owner!=null){particles|=owner.ParticleCount>0;if(!validated){Check(owner.LayerCount==config.layers.Length,id+" keeps every source layer in view "+viewer);Check(owner.GetComponentsInChildren<ParticleSystemRenderer>().All(r=>r.sharedMaterial.mainTexture!=null&&r.sharedMaterial.shader.isSupported),id+" has usable materials and textures");validated=true;}
                        if((full&&(i==12||i==20||i==35))||(!full&&(i==3||i==5||i==9)))maximumPixels=Mathf.Max(maximumPixels,Pixels(owner.transform));}
                    if(i==(full?20:5))yield return Image(id+"-view-"+viewer);
                }
                Check(particles&&maximumPixels>10,id+" renders actual visible pixels in view "+viewer);shot.end=frame;fx.Reset();marker.InspectionMode=false;marker.Clear();yield return Wait();
            }
        }
        void OnGUI(){if(!string.IsNullOrEmpty(caption))GUI.Label(new Rect(25,7,Screen.width-50,34),caption,new GUIStyle(GUI.skin.label){fontSize=22,alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(1,.84f,.6f)}});}
    }
}
