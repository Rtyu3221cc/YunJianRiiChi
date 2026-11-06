#if UNITY_EDITOR
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
    public sealed partial class PhoneFeedbackR25Capture:MonoBehaviour {
        [Serializable] class Report {public List<string> checks=new List<string>();public List<string> editorDiagnostics=new List<string>();public int riichiCases,calloutCases,models;public List<Stick> sticks=new List<Stick>();public List<Chapter> chapters=new List<Chapter>();public List<AudioCue> cues=new List<AudioCue>();public int characters,voiceLookups,frames,fps=30;public bool apkBuilt=false,committed=false;}
        [Serializable] class Stick {public string id,name;public Vector3 bounds,vertices;public List<Part> parts=new List<Part>();}
        [Serializable] class Part {public string name;public bool enabled;public Vector3 bounds,vertices;}
        [Serializable] class VoiceManifest {public VoiceEntry[] clips;}
        [Serializable] class VoiceEntry {public string key;}
        readonly Report report=new Report();readonly List<string> errors=new List<string>();
        string repo,output;RiichiClient client;TableRenderer table;TableEffects effects;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);report.checks.Add(text);}
        void Log(string message,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;if((stack??"").Contains("UnityEditor.Search.SearchDatabase")&&!(stack??"").Contains("YunJian.Riichi")){report.editorDiagnostics.Add(message+"\n"+stack);return;}errors.Add(message);}
        IEnumerator Start(){
            repo=Environment.GetEnvironmentVariable("YUNJIAN_R25_REPO")??Path.GetFullPath("..");output=repo+"/build/"+(R31Mode?"wardrobe-r31":R30Mode?"wardrobe-r30":R28Mode?"review-r28":R27Mode?"review-r27":R26Mode?"review-r26":"review-r25")+"/capture";Directory.CreateDirectory(output);Application.logMessageReceived+=Log;
            client=GetComponent<RiichiClient>();table=GetComponent<TableRenderer>();effects=GetComponent<TableEffects>();client.BeginLobbyValidation("review-r25");client.SessionPreviewOnly=true;GetComponent<ResourceRoomCoordinator>().enabled=false;GetComponent<CharacterPackService>().StopAllCoroutines();
            CharacterRoster.BeginPreview();if(!MovieMode||R31Mode)WardrobeSelection.BeginPreview();CharacterPackService.MarkStartupSeen();AudioListener.pause=true;
            var work=new Stack<IEnumerator>();work.Push(R30Mode?R30Run():R28Mode?R28Run():R27Mode?R27Run():R26Mode?R26Run():TailMode?TailMovie():MovieMode?Movie():ExtraMode?ExtraAudit():Study());while(work.Count>0){object next;try{var task=work.Peek();if(!task.MoveNext()){work.Pop();continue;}next=task.Current;if(next is IEnumerator child){work.Push(child);continue;}}catch(Exception e){File.WriteAllText(output+"/failed.txt",e.ToString());File.WriteAllText(output+"/partial-checks.json",JsonUtility.ToJson(report,true));Debug.LogException(e);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            if(errors.Count>0){File.WriteAllText(output+"/failed.txt",string.Join("\n",errors));File.WriteAllText(output+"/partial-checks.json",JsonUtility.ToJson(report,true));Cleanup();EditorApplication.Exit(1);yield break;}
            Check(errors.Count==0,"No Unity runtime errors");File.WriteAllText(output+(R30Mode?"/checks.json":R28Mode?"/"+(Environment.GetEnvironmentVariable("YUNJIAN_R25_MODE")=="r28-final"?"final":"probe")+"-checks.json":R27Mode?(Environment.GetEnvironmentVariable("YUNJIAN_R25_MODE")=="r27-final"?"/final-checks.json":"/probe-checks.json"):R26Mode?(R26Probe?"/probe-checks.json":Environment.GetEnvironmentVariable("YUNJIAN_R25_MODE")=="r26-four"?"/four-checks.json":"/movie-checks.json"):TailMode?"/tail-checks.json":MovieMode?"/movie-checks.json":ExtraMode?"/extra-checks.json":"/study.json"),JsonUtility.ToJson(report,true));Cleanup();Debug.Log("PHONE FEEDBACK R25 STUDY PASS "+report.checks.Count+" voices="+report.voiceLookups);EditorApplication.Exit(0);
        }
        void Cleanup(){CloseRecording();WardrobeSelection.EndPreview();CharacterRoster.EndPreview();CharacterPackService.ResetForReview();WardrobePackService.ResetForReview();AudioListener.pause=false;Application.logMessageReceived-=Log;}
        IEnumerator Wait(int n=4){for(int i=0;i<n;i++)yield return null;}
        IEnumerator Size(int w,int h){var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));view.GetType().GetMethod("SetCustomResolution",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,new object[]{new Vector2(w,h),"Phone feedback R25"});yield return Wait(8);}
        IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+name+".png",image.EncodeToPNG());Destroy(image);}
        RoomView Fixture(){var v=JsonUtility.FromJson<RoomView>(File.ReadAllText(repo+"/build/unity-smoke/preview-4.json"));v.audioRoom="review-r25";v.matchSeq=25;v.handSeq=1;v.phase="play";v.you=v.turn=0;v.pin="123456";v.audioSeq=100;v.audioEvents=new PublicSound[0];v.options=new Choice[0];v.waitDetails=new WaitDetail[0];v.discardHints=new DiscardHint[0];v.resourceSetup=null;v.result=null;foreach(var p in v.players){p.character="yiji";p.skin=400101;p.wardrobe=WardrobeLoadout.Local();p.riichi=0;}return v;}
        static Bounds Geometry(Transform root,bool vertices){bool first=true;var result=new Bounds();foreach(var mf in root.GetComponentsInChildren<MeshFilter>()){var renderer=mf.GetComponent<MeshRenderer>();if(renderer==null||!renderer.enabled||mf.sharedMesh==null)continue;var points=vertices?mf.sharedMesh.vertices:new[]{mf.sharedMesh.bounds.min,mf.sharedMesh.bounds.max};foreach(var point in points){var p=root.InverseTransformPoint(mf.transform.TransformPoint(point));if(first){first=false;result=new Bounds(p,Vector3.zero);}else result.Encapsulate(p);}}return result;}
        IEnumerator Study(){
            yield return Size(1600,900);client.Lobby.DesignPreviewGardenEntry();yield return Wait(20);
            var manifest=JsonUtility.FromJson<VoiceManifest>(Resources.Load<TextAsset>("Majsoul/SettlementVoice/manifest").text);var keys=manifest.clips.Select(x=>x.key).Where(x=>x.StartsWith("fan_")||x.StartsWith("gameend_")).Concat(new[]{"act_rich","act_drich","act_chi","act_pon","act_kan","act_ron","act_tumo","act_babei"}).Distinct().ToArray();
            foreach(var role in CharacterRoster.All){foreach(var key in keys){var clip=CharacterVoices.Clip(role.key,key);Check(clip!=null&&clip.length>0,"Runtime voice "+role.key+"/"+key);report.voiceLookups++;}report.characters++;if(report.characters%20==0)Debug.Log("R25 VOICE LOOKUP "+report.characters);yield return null;}
            Check(report.characters==122,"Every current character checked through runtime bundle lookup");
            Check(WardrobeRuntime.ResolveHand(WardrobeCatalog.Find("hand-human"),"wangcilang").id=="hand-dog","Wangcilang uses the original dog paw");
            Check(WardrobeRuntime.ResolveHand(WardrobeCatalog.Find("hand-cat-orange"),"wangcilang").id=="hand-cat-orange","Explicit orange cat selection overrides character default");
            foreach(var item in WardrobeCatalog.Items("stick").Where(i=>i.HasModel)){
                var go=new GameObject("R25 size inspection "+item.id);go.transform.position=new Vector3(100,100,100);go.AddComponent<WardrobeModelVisual>().Initialize(item,.74f);var row=new Stick{id=item.id,name=item.name,bounds=Geometry(go.transform,false).size,vertices=Geometry(go.transform,true).size};
                if(item.id=="liqi_25hf")foreach(var mf in go.GetComponentsInChildren<MeshFilter>()){var renderer=mf.GetComponent<MeshRenderer>();row.parts.Add(new Part{name=mf.name,enabled=renderer!=null&&renderer.enabled,bounds=mf.sharedMesh.bounds.size,vertices=mf.sharedMesh.vertices.Length>0?VertexBounds(mf.sharedMesh.vertices).size:Vector3.zero});}
                report.sticks.Add(row);Destroy(go);yield return null;
            }
            File.WriteAllText(output+"/stick-size-study.json",JsonUtility.ToJson(report,true));
            var play=Fixture();client.ShowValidationFixture(play);effects.Accept(play);yield return new WaitForSecondsRealtime(.8f);
            var result=JsonUtility.FromJson<RoomView>(JsonUtility.ToJson(play));result.phase="result";result.audioSeq=101;result.result=new ResultView{title="荣和",delta=new[]{8000,-8000,0,0},details=new[]{new WinDetail{seat=0,from=1,ron=true,tile="z1",tiles=new[]{"m1","m2","m3","p1","p2","p3","s1","s2","s3","z1","z1","z1","z2","z2"},melds=new string[0],yaku=new[]{new YakuDetail{name="立直",han=1},new YakuDetail{name="宝牌",han=4}},limit="满贯",score=new ScoreDetail{fanshu=5,fu=30,defen=8000}}}};result.audioEvents=new[]{new PublicSound{id=101,seat=0,kind="ron",tile="z1",from=1,winSource="discard"}};client.ValidationApplyState(result);effects.Accept(result);yield return new WaitForSecondsRealtime(1.3f);Check(effects.CutIn.Active,"Mangan live event enters the cut-in");yield return Shot("mangan-banner-study");yield return Wait(25);
            client.Lobby.PreviewDorm("yiji",400101);client.Lobby.PreviewWardrobeCategory("stick");yield return Wait(8);yield return Shot("autosave-list-study");
        }
        static Bounds VertexBounds(Vector3[] points){var b=new Bounds(points[0],Vector3.zero);foreach(var p in points)b.Encapsulate(p);return b;}
    }
}

#endif
