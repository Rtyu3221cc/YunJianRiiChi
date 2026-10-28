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
    [InitializeOnLoad] public static class WardrobeR10Review {
        const string Key="WardrobeR10Review";
        static WardrobeR10Review(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<WardrobeR10Probe>();}};}
        public static void RunStudy(){SessionState.SetBool("WardrobeR10Full",false);Run();}
        public static void RunFull(){SessionState.SetBool("WardrobeR10Full",true);Run();}
        static void Run(){AssetDatabase.Refresh();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class WardrobeR10Probe:MonoBehaviour {
        [Serializable] public class Shot {public string key,title,group;public int viewer,start,end;public float[] detail;public float tailDot;public string discardMotion,stickMotion;}
        [Serializable] public class Report {public int frames,fps=20,checks;public List<Shot> shots=new List<Shot>();}
        readonly Report report=new Report();string output,caption;bool full,dynamic;float inset,volume;int frame;RiichiClient client;TableRenderer table;TableEffects fx;
        IEnumerator Start(){
            full=SessionState.GetBool("WardrobeR10Full",false);output=Path.GetFullPath(full?"../build/wardrobe-r10-review/render":"../build/wardrobe-r10-review/study");Directory.CreateDirectory(output);Directory.CreateDirectory(Path.Combine(output,"frames"));File.Delete(Path.Combine(output,"error.txt"));File.WriteAllText(Path.Combine(output,"checks.txt"),"");
            dynamic=AppPreferences.DynamicOutfits;inset=AppPreferences.ScreenInset;volume=AudioListener.volume;CharacterRoster.BeginPreview();WardrobeSelection.BeginPreview();AppPreferences.SetDynamicOutfits(false);AppPreferences.SetInset(0);AudioListener.volume=0;WardrobeSelection.Save(new Dictionary<string,string>{{"cloth","cloth-default"},{"tileFace","mjpface_default"},{"tileBack","mjp_default"}});Time.captureFramerate=report.fps;
            client=GetComponent<RiichiClient>();table=GetComponent<TableRenderer>();fx=GetComponent<TableEffects>();client.BeginLobbyValidation("wardrobe-r10");client.SessionPreviewOnly=true;GetComponent<ResourceRoomCoordinator>().enabled=false;
            var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){object next;try{var it=stack.Peek();if(!it.MoveNext()){stack.Pop();continue;}next=it.Current;if(next is IEnumerator child){stack.Push(child);continue;}}catch(Exception e){File.WriteAllText(Path.Combine(output,"error.txt"),e.ToString());Debug.LogException(e);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            report.frames=frame;File.WriteAllText(Path.Combine(output,"capture.json"),JsonUtility.ToJson(report,true));Cleanup();Debug.Log("WARDROBE R10 PASS "+report.checks+" checks, "+frame+" frames");EditorApplication.Exit(0);
        }
        void Cleanup(){Time.captureFramerate=0;fx.ValidationSequentialClock=false;fx.ValidationFreeze=false;AudioListener.volume=volume;WardrobeSelection.EndPreview();CharacterRoster.EndPreview();AppPreferences.SetDynamicOutfits(dynamic);AppPreferences.SetInset(inset);}
        void Check(bool ok,string text){if(!ok)throw new Exception(text);report.checks++;File.AppendAllText(Path.Combine(output,"checks.txt"),"PASS "+text+"\n");}
        IEnumerator Wait(int n=2){for(int i=0;i<n;i++)yield return null;}
        RoomView View(string state,int viewer,string hand,string effect){var v=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/wardrobe-r6-study/riichi-"+state+".json"));v.you=viewer;v.resourceSetup=null;v.audioRoom="r10-"+hand+":"+effect+":"+viewer;foreach(var p in v.players){p.character="yiji";p.skin=400101;p.wardrobe=new WardrobeLoadout{hand=hand,stick="stick-default",riichiEffect=effect,winEffect="win-default",callout="mpzs_shandian",music="music-riichi"};}return v;}
        float[] Detail(Vector3 point){const float w=680,h=560;var p=table.Camera.WorldToScreenPoint(point);return new[]{Mathf.Clamp(p.x-w/2,0,Screen.width-w),Mathf.Clamp(Screen.height-p.y-h*.55f,0,Screen.height-h),w,h};}
        IEnumerator Record(){yield return new WaitForEndOfFrame();if(full){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,"frames",(frame++).ToString("D5")+".jpg"),t.EncodeToJPG(90));Destroy(t);}}
        int Pixels(Renderer renderer)=>(int)typeof(RuntimeValidation).GetMethod("VisibleEffectPixels",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{table,renderer});
        IEnumerator Run(){
            var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(1688,768),"R10 source playback contracts"});yield return Wait(8);
            client.Lobby.PreviewWardrobe("callout");yield return Wait(5);foreach(var item in WardrobeCatalog.Data.items.Where(i=>i.category=="callout"))Check((client.Lobby.ValidationTarget("dress-item-"+item.id).width>0)==!item.hiddenFromSelection,"Default selector respects experimental status "+item.id);
            foreach(string effect in new[]{"riichi-afterimage","riichi-feather","riichi-blue"})foreach(string hand in new[]{"hand-human","hand-cat"})for(int viewer=0;viewer<4;viewer++){
                var before=View("before",viewer,hand,effect);var declared=View("declared",viewer,hand,effect);var committed=View("committed",viewer,hand,effect);
                client.DesignPreviewSessionTable(before);client.ValidationTableOnly=true;client.ValidationIntro.Reset();fx.Reset();fx.ValidationFreeze=true;fx.Accept(before);fx.ValidationSequentialClock=true;fx.ValidationTime=0;yield return Wait(3);
                var tile=declared.audioEvents.First(e=>e.kind=="discard");var target=table.FindDiscard(0,tile.tile,tile.riverIndex);var item=WardrobeCatalog.Find(effect);var plan=SourcePlaybackContracts.Declaration(WardrobeCatalog.Find(hand),item);var shot=new Shot{key=effect+"-"+hand,title=item.name+" · "+WardrobeCatalog.Find(hand).name,group=effect,viewer=viewer,start=frame,detail=Detail(target!=null?target.position:table.DiscardPoint(0,tile.tile,tile.riverIndex))};report.shots.Add(shot);
                bool sawDiscard=false,sawStick=false,sawFx=false;int handPixels=0,stickPixels=0,tailSamples=0;float tailSum=0;Vector3 previous=Vector3.zero;bool hadPrevious=false;bool checkedPlan=false,checkedFrame=false;
                for(int i=0;i<(full?80:40);i++){
                    float t=full?i/(float)report.fps:i*.1f;fx.ValidationTime=t;
                    if(i==(full?4:2)){client.ValidationApplyState(declared);fx.Accept(declared);typeof(TableEffects).GetMethod("FinishCall",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(fx,null);var placed=table.FindDiscard(0,tile.tile,tile.riverIndex);if(placed!=null)shot.detail=Detail(placed.position);}if(i==(full?28:14)){client.ValidationApplyState(committed);fx.Accept(committed);}
                    caption=shot.title+" · 视角"+(viewer+1)+(t<1.4f?" · 宣言牌":" · 宣言成立后放立直棒（省略等待）");yield return Record();
                    var kind=fx.ValidationKind;var mesh=GameObject.Find("Source hand mesh");
                    if(kind=="discard"){
                        sawDiscard=true;shot.discardMotion=fx.ValidationMotion;
                        if(!checkedPlan){Check(Mathf.Abs(fx.ValidationRiichiCue-plan.cue)<.0001f&&Mathf.Abs(fx.ValidationHandoff-plan.release)<.0001f&&Mathf.Abs(fx.ValidationPlayLength-plan.lifetime)<.0001f,"Original timing is used "+shot.key);Check(fx.ValidationMotion==WardrobeRuntime.Motion(WardrobeCatalog.Find(hand),plan.action=="Dapai"?"discard-hand":"riichi-hand"),"Original action branch is used "+shot.key);checkedPlan=true;}
                        if(mesh!=null&&(i==(full?6:3)||i==(full?9:4)))handPixels=Mathf.Max(handPixels,Pixels(mesh.GetComponent<MeshRenderer>()));
                    }
                    if(kind=="riichi-stick"){sawStick=true;shot.stickMotion=fx.ValidationMotion;if(mesh!=null&&(i==(full?32:16)||i==(full?36:18)))stickPixels=Mathf.Max(stickPixels,Pixels(mesh.GetComponent<MeshRenderer>()));}
                    var active=GetComponent<RiichiDiscardVisual>().Latest;
                    if(active!=null){sawFx|=active.ParticleCount>0;
                        if(!SourcePlaybackContracts.Binding(item).ground&&kind=="discard"&&fx.ValidationAnimatedTile!=null){var carried=fx.ValidationAnimatedTile;if(!checkedFrame){Check(Vector3.Dot(active.transform.TransformDirection(Vector3.back),carried.up)>.999f,"Actual source face normal follows the tile front "+shot.key);Check(Vector3.Dot(active.transform.TransformDirection(Vector3.up),carried.forward)>.999f,"Actual source UV up tangent follows tile glyph orientation "+shot.key);checkedFrame=true;}
                            var point=active.transform.position;var velocity=point-previous;if(hadPrevious&&effect=="riichi-afterimage"&&hand=="hand-human"&&t>.67f&&t<.84f&&velocity.sqrMagnitude>1e-7f){var direction=Vector3.ProjectOnPlane(active.transform.TransformDirection(Vector3.up),carried.up).normalized;tailSum+=Vector3.Dot(direction,Vector3.ProjectOnPlane(velocity,carried.up).normalized);tailSamples++;}previous=point;hadPrevious=true;}
                    }
                    if(i==(full?14:7)||i==(full?34:17)){yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,shot.key+"-"+viewer+"-"+i+".png"),image.EncodeToPNG());Destroy(image);}
                }
                Check(sawDiscard&&sawStick&&sawFx,"Complete public declaration and stick sequence "+shot.key+" view "+viewer);Check(handPixels>20&&stickPixels>20,"Both hand actions have visible rendered pixels "+shot.key+" view "+viewer);
                if(effect=="riichi-afterimage"&&hand=="hand-human"){shot.tailDot=tailSamples>0?tailSum/tailSamples:1;Check(tailSamples>0&&shot.tailDot<-.65f,"Afterimage trails behind actual carried-tile movement "+viewer+" dot="+shot.tailDot);}
                shot.end=frame;fx.ValidationSequentialClock=false;fx.Reset();yield return Wait();
            }
        }
    }
}
