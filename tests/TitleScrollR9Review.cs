using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace YunJian.Riichi.Editor {
 [InitializeOnLoad] public static class TitleScrollR9Review {
  const string Key="TitleScrollR9";
  static TitleScrollR9Review(){EditorApplication.update+=()=>{if(EditorApplication.isPlaying&&!EditorApplication.isCompiling&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<TitleScrollR9Capture>();}};}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Bootstrap(){if(!SessionState.GetBool(Key,false))return;PlayerRecord.PreviewOnly=true;CharacterRoster.PreviewOnly=true;CharacterPackService.RootOverride=Environment.GetEnvironmentVariable("YUNJIAN_R9_REPO")+"/build/character-voice-r34/roles";CharacterPackService.RespectPreviewAvailability=true;CharacterPackService.ResetForReview();WardrobePackService.RespectPreviewAvailability=true;WardrobePackService.RootOverride=Environment.GetEnvironmentVariable("YUNJIAN_R9_OUT")+"/empty-packs";}
  public static void Run(){PlayerSettings.companyName="YunJianReview";PlayerSettings.productName="TitleScrollR9";EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
 }
 public sealed class TitleScrollR9Capture:MonoBehaviour {
  const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  [Serializable] class Report {public string phase;public bool wheel,drag,dragDidNotEquip,outsideDrag,bottom,top,titleSelectionWorks,greenCheck,releaseConsumed;public float wheelOffset,dragOffset,bottomOffset,maxOffset;public int checks;public bool androidDeviceTested;}
  RiichiClient client;LobbyController lobby;string output;Report report=new Report();bool ownDraw;Event pending;Exception guiError;int injected;
  void Check(bool condition,string reason){if(!condition)throw new Exception(reason);report.checks++;}
  float Offset {get {var value=typeof(LobbyController).GetField("titleScroll",Private).GetValue(lobby);return value is NativeScroll scroll?scroll.Offset:((Vector2)value).y;}}
  Event restore;EventType injectedType;
  void BeforeTitleInput(){if(Event.current.type!=EventType.Repaint||pending==null)return;restore=Event.current;injectedType=pending.type;Event.current=new Event(pending);Event.current.mousePosition=pending.mousePosition;pending=null;}
  void AppliedTitleInput(){if(restore!=null&&injectedType==EventType.MouseUp){report.releaseConsumed=Event.current.type==EventType.Used&&GUIUtility.hotControl==0;Debug.Log("R9 RELEASE "+Event.current.type+" hot="+GUIUtility.hotControl);}}
  void AfterTitleInput(){if(restore==null)return;Event.current=restore;restore=null;injected++;}
  IEnumerator Send(EventType type,Vector2 at,Vector2 delta=default){int before=injected;pending=new Event{type=type,mousePosition=at,delta=delta,button=0};for(int i=0;i<10&&injected==before;i++)yield return null;Check(injected>before,"title-list GUI event delivered: "+type);yield return Frames(3);}
  IEnumerator Frames(int count=8){for(int i=0;i<count;i++)yield return null;}
  IEnumerator Shot(string name){yield return Frames();yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+name+".png",t.EncodeToPNG());Destroy(t);}
  IEnumerator Start(){output=Environment.GetEnvironmentVariable("YUNJIAN_R9_OUT");Directory.CreateDirectory(output);report.phase=Environment.GetEnvironmentVariable("YUNJIAN_R9_PHASE")??"after";client=GetComponent<RiichiClient>();client.SessionPreviewOnly=true;client.BeginLobbyValidation("title-scroll-r9");lobby=client.Lobby;GetComponent<ResourceRoomCoordinator>().enabled=false;GetComponent<CharacterPackService>().StopAllCoroutines();GetComponent<WardrobePackService>().StopAllCoroutines();AudioListener.volume=0;
   var tasks=new Stack<IEnumerator>();tasks.Push(Run());while(tasks.Count>0){object next;try{var task=tasks.Peek();if(!task.MoveNext()){tasks.Pop();continue;}next=task.Current;if(next is IEnumerator nested){tasks.Push(nested);continue;}}catch(Exception e){File.WriteAllText(output+"/failed.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
   File.WriteAllText(output+"/report.json",JsonUtility.ToJson(report,true));EditorApplication.Exit(0);
  }
  IEnumerator Run(){
   var game=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));game.GetType().GetMethod("SetCustomResolution",Private).Invoke(game,new object[]{new Vector2(1920,1080),"Title scroll R9"});yield return Frames(15);
   PlayerRecord.Replace(new PlayerRecordData());PlayerRecord.Equip("600008");lobby.Home();lobby.ReviewTitleInput=BeforeTitleInput;lobby.ReviewTitleInputApplied=AppliedTitleInput;lobby.ReviewTitleInputComplete=AfterTitleInput;yield return Frames(45);lobby.OpenPlayerInfo();yield return Frames(20);typeof(LobbyController).GetMethod("OpenInfoDialog",Private).Invoke(lobby,new object[]{"titles"});yield return Frames(25);
   var entries=PlayerTitles.All.Where(x=>PlayerRecord.Owns(x.id)&&x.condition!="default").OrderByDescending(x=>x.priority).ToArray();report.maxOffset=Mathf.Max(0,entries.Length*99.2f-671);string equipped=PlayerRecord.Title;
   yield return Shot("01-title-top");yield return Send(EventType.ScrollWheel,new Vector2(900,600),new Vector2(0,12));report.wheelOffset=Offset;report.wheel=Offset>0;Check(report.wheel,"title list responds to mouse wheel");yield return Shot("02-title-wheel");
   yield return Send(EventType.ScrollWheel,new Vector2(900,600),new Vector2(0,-10000));Check(Offset<.1f,"wheel clamps at top");
   yield return Send(EventType.MouseDown,new Vector2(750,650));yield return Send(EventType.MouseDrag,new Vector2(750,430),new Vector2(0,-220));report.dragOffset=Offset;report.drag=Offset>0;yield return Shot("03-title-drag");Check(report.drag,"title list responds to drag");yield return Send(EventType.MouseUp,new Vector2(750,430));report.dragDidNotEquip=PlayerRecord.Title==equipped;
   yield return Send(EventType.MouseDown,new Vector2(750,650));float before=Offset;yield return Send(EventType.MouseDrag,new Vector2(750,220),new Vector2(0,-430));report.outsideDrag=Offset>before+400;yield return Send(EventType.MouseUp,new Vector2(750,220));
   yield return Send(EventType.ScrollWheel,new Vector2(900,600),new Vector2(0,10000));report.bottomOffset=Offset;report.bottom=Mathf.Abs(Offset-report.maxOffset)<1;Check(report.bottom,"last owned title is reachable and scroll clamps at bottom");yield return Shot("04-title-bottom");
   yield return Send(EventType.ScrollWheel,new Vector2(900,600),new Vector2(0,-10000));report.top=Offset<.1f;Check(report.top,"can return to first title");
   lobby.ValidationPress("infoTitle-"+entries[0].id);yield return Frames();report.titleSelectionWorks=PlayerRecord.Title==entries[0].id;Check(report.titleSelectionWorks,"existing title selection action still equips the visible row");yield return Shot("05-title-selection");
   if(report.phase=="after"){Check(report.dragDidNotEquip&&report.releaseConsumed,"drag release is consumed and cannot equip a title");Check(report.outsideDrag,"drag remains captured beyond list viewport");}
   ownDraw=false;client.enabled=true;client.BeginLobbyValidation("r9-resource");var v=DressPreviewStates.Base("r9-ready",new WardrobeLoadout(),"yiji",400101,"房间第1位");v.phase="lobby";v.host=true;v.you=0;v.resourceSetup=new ResourceSetupView{revision=9,choices=v.players.Select(p=>new CharacterChoice{character=p.character,skin=p.skin}).ToArray(),players=Enumerable.Range(0,4).Select(i=>new ResourcePeerState{seat=i,state=i==0?"pending":"ready",progress=i==0?80:100}).ToArray()};for(int i=0;i<4;i++)v.players[i].name="房间第"+(i+1)+"位";client.ShowValidationFixture(v);var coordinator=GetComponent<ResourceRoomCoordinator>();coordinator.ReviewDisplayOnly=true;coordinator.enabled=true;coordinator.ReviewProgress(80,100,"共享装扮资源",false);yield return Frames(20);yield return Shot("06-resource-green-check");
   var green=Resources.Load<Texture2D>("Majsoul/Characters/UI/skin_using");report.greenCheck=green!=null;Check(report.greenCheck,"original skin selection green check is available");
  }
 }
}
