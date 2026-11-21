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
 [InitializeOnLoad] public static class Feedback1114Review {
  const string Key="Feedback1114Review";
  public static readonly List<string> Errors=new List<string>();
  static Feedback1114Review(){EditorApplication.update+=()=>{if(EditorApplication.isPlaying&&!EditorApplication.isCompiling&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<Feedback1114Capture>();}};}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Bootstrap(){if(!SessionState.GetBool(Key,false))return;string repo=Environment.GetEnvironmentVariable("YUNJIAN_1114_REPO");Application.logMessageReceived+=(m,s,t)=>{if((t==LogType.Error||t==LogType.Exception)&&!(s??"").Contains("UnityEditor.Search"))Errors.Add(m);};CharacterRoster.PreviewOnly=true;CharacterPackService.RootOverride=repo+"/build/character-voice-r34/roles";CharacterPackService.RespectPreviewAvailability=true;CharacterPackService.ResetForReview();WardrobePackService.RootOverride=repo+"/build/feedback-1114-r39/wardrobes";WardrobePackService.RespectPreviewAvailability=true;WardrobePackService.ResetForReview();}
  public static void Run(){PlayerSettings.companyName="YunJianReview";PlayerSettings.productName="Feedback1114";EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
 }
 public sealed class Feedback1114Capture:MonoBehaviour {
  [Serializable] class Tile {public int seat;public float age;public int enabledAlwaysOutlines,enabledTexturedFaces;public string shot;}
  [Serializable] class Card {public string name;public int width,height,screenWidth,screenHeight;}
  [Serializable] class Report {public string mode;public int checks;public bool androidDeviceTested;public List<Tile> tiles=new List<Tile>();public List<Card> cards=new List<Card>();}
  const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  RiichiClient client;TableEffects effects;Report report=new Report();string output,mode,presets;bool had;
  void Check(bool ok,string why){if(!ok)throw new Exception(why);report.checks++;}
  void Restore(){if(had)PlayerPrefs.SetString("riichi.wardrobe.presets.v3",presets);else PlayerPrefs.DeleteKey("riichi.wardrobe.presets.v3");PlayerPrefs.Save();}
  IEnumerator Start(){mode=Environment.GetEnvironmentVariable("YUNJIAN_1114_MODE")??"after";report.mode=mode;output=Environment.GetEnvironmentVariable("YUNJIAN_1114_REPO")+"/build/feedback-1114-r39/"+mode;Directory.CreateDirectory(output);had=PlayerPrefs.HasKey("riichi.wardrobe.presets.v3");presets=PlayerPrefs.GetString("riichi.wardrobe.presets.v3","");client=GetComponent<RiichiClient>();client.SessionPreviewOnly=true;client.BeginLobbyValidation("1114-review");GetComponent<ResourceRoomCoordinator>().enabled=false;GetComponent<CharacterPackService>().StopAllCoroutines();CharacterRoster.BeginPreview();WardrobeSelection.BeginPreview();AudioListener.volume=0;effects=GetComponent<TableEffects>();
   var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){object next;try{var task=stack.Peek();if(!task.MoveNext()){stack.Pop();continue;}next=task.Current;if(next is IEnumerator nested){stack.Push(nested);continue;}}catch(Exception e){Restore();File.WriteAllText(output+"/failed.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}Restore();Check(Feedback1114Review.Errors.Count==0,string.Join("\n",Feedback1114Review.Errors));File.WriteAllText(output+"/report.json",JsonUtility.ToJson(report,true));Debug.Log("FEEDBACK1114 "+mode+" PASS "+report.checks);EditorApplication.Exit(0);
  }
  IEnumerator Size(int w,int h){var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));view.GetType().GetMethod("SetCustomResolution",Private).Invoke(view,new object[]{new Vector2(w,h),"1114 Feedback"});for(int i=0;i<6;i++)yield return null;}
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+name+".png",t.EncodeToPNG());Destroy(t);}
  IEnumerator Run(){
   yield return Size(1800,810);
   yield return WardrobePackService.Preload(new[]{"effect_liqi_25hf"});Check(WardrobePackService.Installed("effect_liqi_25hf"),"Verified Fate wardrobe pack installed");
   var loadout=new WardrobeLoadout{riichiEffect="effect_liqi_25hf"};
   for(int seat=0;seat<4;seat++){
    var state=DressPreviewStates.Riichi("1114-riichi-"+seat,loadout,"yiji",400101,"虚影祝祷",seat);client.ShowValidationFixture(state);GetComponent<RiichiDiscardVisual>().Observe(state,false);effects.SuppressShouts=true;for(int wait=0;wait<6;wait++)yield return null;effects.ValidationPlay(seat,"discard",DressPreviewStates.RiichiDiscard,true,state.players[seat].river.Length);effects.ValidationTime=0;yield return null;
    foreach(float at in new[]{.6f,1.1f,1.6f,2.4f}){
     effects.ValidationTime=at;yield return null;yield return null;
     var queues=GetComponentsInChildren<NativeWinningTileQueue>(true);var rigs=GetComponentsInChildren<NativeWinTileModel>(true);
     var renderers=queues.SelectMany(q=>q.GetComponentsInChildren<MeshRenderer>()).Concat(rigs.SelectMany(q=>q.GetComponentsInChildren<MeshRenderer>())).Distinct().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
     var row=new Tile{seat=seat,age=at,enabledAlwaysOutlines=renderers.Count(r=>r.sharedMaterials.Any(m=>m.shader.name=="YunJian/OriginalTileOutline"&&m.GetFloat("_ZTest")==8)),enabledTexturedFaces=renderers.Count(r=>r.sharedMaterials.Any(m=>m.shader.name=="YunJian/OriginalTileFace"&&m.mainTexture!=null)),shot="fate-"+seat+"-"+at.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)};report.tiles.Add(row);
     if(mode!="before"){
      if(row.enabledAlwaysOutlines>0){File.WriteAllLines(output+"/queue-debug.txt",queues.Select(q=>"active="+q.Active+" legacy="+q.LegacyQueues+" begin="+q.Begin+" end="+q.End+" outlines="+string.Join(",",(List<bool>)typeof(NativeWinningTileQueue).GetField("outlines",Private).GetValue(q))+" renderers="+string.Join(",",((List<MeshRenderer>)typeof(NativeWinningTileQueue).GetField("clones",Private).GetValue(q)).Select(r=>r.enabled.ToString()))));File.WriteAllLines(output+"/outline-debug.txt",renderers.Select(r=>r.name+" hierarchy="+PathOf(r.transform)+" materials="+string.Join(";",r.sharedMaterials.Select(m=>m.name+"/"+m.shader.name+"/"+(m.HasProperty("_ZTest")?m.GetFloat("_ZTest"):0)))));yield return Shot("failed-outline");}
      Check(row.enabledAlwaysOutlines==0,"No opaque Always outline covering Fate tile "+seat+"/"+at);if(at<=1.1f)Check(row.enabledTexturedFaces>0,"Actual tile face rendered during Fate effect "+seat+"/"+at);}
     if(at==1.1f||at==1.6f)yield return Shot(row.shot);
    }effects.Reset();yield return null;
   }
   foreach(var size in new[]{new Vector2Int(2760,1256),new Vector2Int(1920,1080)}){
    yield return Size(size.x,size.y);var state=DressPreviewStates.Base("1114-intro",new WardrobeLoadout(),"saber",40011002,"旅人");state.n=3;state.players=state.players.Take(3).ToArray();state.players[0].character="saber";state.players[0].skin=40011002;state.players[1].character="xiaoniaoyouchutian";state.players[1].skin=401906;state.players[2].character="fuji";state.players[2].skin=403807;client.ShowValidationFixture(state);client.ValidationIntro.Reenter(state);client.ValidationIntro.Freeze=true;client.ValidationIntro.FrozenAge=1.8f;for(int i=0;i<8;i++)yield return null;yield return Shot("intro-"+size.x+"x"+size.y);
    var cards=(IDictionary)typeof(MatchIntroduction).GetField("characterCards",Private).GetValue(client.ValidationIntro);Check(cards.Count>=3,"Three actual character compositions rendered");foreach(DictionaryEntry entry in cards){var t=(Texture)entry.Value;report.cards.Add(new Card{name=(string)entry.Key,width=t.width,height=t.height,screenWidth=size.x,screenHeight=size.y});if(mode!="before")Check(t.width>900,"Intro composition retains screen pixel density "+entry.Key);}
    client.ValidationIntro.Reset();
   }
   if(mode=="before")Check(report.tiles.Any(t=>t.enabledAlwaysOutlines>0),"Baseline reproduces Always outline fault");
   effects.ValidationFreeze=false;client.BeginLobbyValidation("1114-preview");client.Lobby.PreviewWardrobe("riichiEffect");typeof(LobbyController).GetMethod("EquipDressItem",Private).Invoke(client.Lobby,new object[]{"riichiEffect","effect_liqi_25hf"});client.Lobby.PreviewDressTable("riichiEffect");float end=Time.realtimeSinceStartup+20;while(!client.Lobby.PreviewDressTableReady||GetComponent<RiichiDiscardVisual>().Latest==null||Time.unscaledTime-GetComponent<RiichiDiscardVisual>().Latest.StartedAt<.85f){Check(Time.realtimeSinceStartup<end,"Actual wardrobe preview reaches visible effect");yield return null;}yield return Shot("fate-real-preview");client.Lobby.PreviewCloseDressTable();yield return new WaitForSecondsRealtime(.5f);Check(!client.DressPreviewing,"Real preview exits");
  }
  static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
 }
}
