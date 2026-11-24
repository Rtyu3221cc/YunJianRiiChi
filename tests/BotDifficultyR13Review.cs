using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace YunJian.Riichi.Editor {
 [InitializeOnLoad] public static class BotDifficultyR13Review {
  const string Key="BotDifficultyR13Review";
  static BotDifficultyR13Review(){EditorApplication.update+=()=>{if(EditorApplication.isPlaying&&!EditorApplication.isCompiling&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<BotDifficultyR13Capture>();}};}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Bootstrap(){if(!SessionState.GetBool(Key,false))return;PlayerRecord.PreviewOnly=true;CharacterRoster.PreviewOnly=true;CharacterPackService.RespectPreviewAvailability=true;WardrobePackService.RespectPreviewAvailability=true;CharacterPackService.RootOverride=Environment.GetEnvironmentVariable("YUNJIAN_R10_OUT")+"/unused-characters";WardrobePackService.RootOverride=Environment.GetEnvironmentVariable("YUNJIAN_R10_OUT")+"/unused-wardrobes";}
  public static void Run(){PlayerSettings.companyName="YunJianReview";PlayerSettings.productName="BotDifficultyR13";EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
 }
 public sealed class BotDifficultyR13Capture:MonoBehaviour {
  const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  [Serializable] class Report{public int checks;public List<string> passed=new List<string>();}
  readonly Report report=new Report();readonly List<string> chapters=new List<string>();RiichiClient client;LobbyController lobby;TableRenderer table;TableEffects effects;string output;System.Diagnostics.Process encoder;RenderTexture rt;Texture2D pixels;int frame;
  void Check(bool ok,string reason){if(!ok)throw new Exception(reason);report.checks++;report.passed.Add(reason);}
  IEnumerator Wait(int n=8){for(int i=0;i<n;i++)yield return null;}
  IEnumerator Shot(string name){yield return Wait();yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+name+".png",t.EncodeToPNG());Destroy(t);}
  int captureWidth,captureHeight;
  void StartFilm(){captureWidth=Screen.width;captureHeight=Screen.height;rt=new RenderTexture(captureWidth,captureHeight,0,RenderTextureFormat.ARGB32);rt.Create();pixels=new Texture2D(captureWidth,captureHeight,TextureFormat.RGB24,false);string repo=Environment.GetEnvironmentVariable("YUNJIAN_R10_REPO");encoder=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{FileName=@"C:\Users\Rtyu3221\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe",Arguments="-X utf8 \""+repo+"/scripts/encode-unity-capture-r40.py\" --output \""+output+"/bot-difficulty-raw.mp4\" --width "+captureWidth+" --height "+captureHeight+" --fps 20",UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true});}
  IEnumerator Film(string label,float seconds,Action<int> action=null){chapters.Add(frame+" "+label);for(int i=0;i<Mathf.RoundToInt(seconds*20);i++){action?.Invoke(i);yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);var before=RenderTexture.active;RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,captureWidth,captureHeight),0,0);RenderTexture.active=before;var bytes=pixels.GetRawTextureData();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);frame++;}}
  IEnumerator Start(){output=Environment.GetEnvironmentVariable("YUNJIAN_R10_OUT");Directory.CreateDirectory(output);client=GetComponent<RiichiClient>();table=GetComponent<TableRenderer>();effects=GetComponent<TableEffects>();client.SessionPreviewOnly=true;client.BeginLobbyValidation("november-local");lobby=client.Lobby;GetComponent<ResourceRoomCoordinator>().enabled=false;GetComponent<CharacterPackService>().StopAllCoroutines();GetComponent<WardrobePackService>().StopAllCoroutines();CharacterPackService.RespectPreviewAvailability=false;WardrobePackService.RespectPreviewAvailability=false;CharacterPackService.ResetForReview();WardrobePackService.ResetForReview();AudioListener.volume=0;client.ValidationCaptureRequests=true;
   var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){object next;try{var t=stack.Peek();if(!t.MoveNext()){stack.Pop();continue;}next=t.Current;if(next is IEnumerator nested){stack.Push(nested);continue;}}catch(Exception e){if(encoder!=null&&!encoder.HasExited)encoder.Kill();File.WriteAllText(output+"/failed.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
   encoder.StandardInput.Close();while(!encoder.HasExited)yield return null;Check(encoder.ExitCode==0,"video encoded");File.WriteAllText(output+"/report.json",JsonUtility.ToJson(report,true));File.WriteAllLines(output+"/chapters.txt",chapters);EditorApplication.Exit(0);
  }



  object Field(object obj,string key)=>obj.GetType().GetField(key,Private).GetValue(obj);
  IEnumerator Press(string id){lobby.ValidationPress(id);yield return Wait(5);}
  RoomView Load(string file)=>JsonUtility.FromJson<RoomView>(File.ReadAllText(Environment.GetEnvironmentVariable("YUNJIAN_R10_REPO")+"/build/bot-difficulty-r13/fixtures/"+file+".json"));
  [Serializable] class CreateRecord {public int n,rounds,aiLevel;public bool red,lobbyFlow;}
  [Serializable] class CreateRecords {public List<CreateRecord> requests=new List<CreateRecord>();}
  IEnumerator Run(){
   var game=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));game.GetType().GetMethod("SetCustomResolution",Private).Invoke(game,new object[]{new Vector2(1600,900),"Bot difficulty R13"});yield return Wait(20);StartFilm();
   lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Create);lobby.DesignPreviewScrollBottom();yield return Wait(12);
   Check(lobby.AiLevel==1,"simple is selected by default");Check(lobby.ValidationTarget("advanced-ai-0").width>0&&lobby.ValidationTarget("advanced-ai-1").width>0,"two native computer controls are drawn");
   var computer=Resources.Load<Texture2D>("Majsoul/Lobby/Sprites/Create/room_create_computer_text");Check(computer!=null&&computer.width==185&&computer.height==63,"original computer label keeps native dimensions");
   yield return Shot("create-simple");yield return Film("高级设置 · 电脑（简单）",2);
   yield return Press("advanced-ai-1");Check(lobby.AiLevel==2,"normal option changes the actual room setting");yield return Shot("create-normal");yield return Film("高级设置 · 电脑（普通）",2);
   var records=new CreateRecords();
   foreach(int n in new[]{3,4})foreach(int level in new[]{1,2}){
    yield return Press(n==3?"three":"four");yield return Press("advanced-ai-"+(level-1));int before=client.ValidationRequests.Count;yield return Press("create");
    var req=client.ValidationRequests.LastOrDefault(x=>x.op=="create");Check(client.ValidationRequests.Count==before+1&&req!=null&&req.n==n&&req.aiLevel==level,"create serializes mode and difficulty "+n+"/"+level);records.requests.Add(JsonUtility.FromJson<CreateRecord>(JsonUtility.ToJson(req)));
   }
   yield return Press("advanced-ai-1");yield return Press("advancedOff");yield return Press("create");Check(client.ValidationRequests.Last(x=>x.op=="create").aiLevel==1,"advanced-off follows native default simple difficulty");File.WriteAllText(output+"/create-requests.json",JsonUtility.ToJson(records,true));
   foreach(int n in new[]{3,4})foreach(int level in new[]{1,2}){
    var room=Load("room-"+n+"-"+level);Check(room.aiLevel==level&&room.players.Length==n,"actual server room difficulty deserializes "+n+"/"+level);client.ShowValidationFixture(room);yield return Wait(15);
    foreach(var p in room.players.Where(p=>p.bot)){
     Check(RankRules.Known(p),"computer rank is visible "+n+"/"+level);Check(RankRules.Level(p,n)==(level==1?2:5)&&RankRules.Stars(RankRules.Level(p,n))==3,"computer fixed rank has three stars "+n+"/"+level);Check(RankRules.Badge(RankRules.Level(p,n),n)!=null,"native computer rank badge exists "+n+"/"+level);
     var profile=p.profile;p.profile=new PlayerRankProfile{set=true,rank4=14,rank3=14,title="inherited"};Check(RankRules.Level(p,n)==(level==1?2:5)&&RankRules.TitleOf(p)=="","computer cannot inherit a human rank or title");p.profile=profile;
    }
    Check(RankRules.Level(room.players[0],n)==(n==3?8:7),"human published rank is preserved "+n+"/"+level);
    yield return Shot("room-"+n+"-"+level);yield return Film((n==3?"三人":"四人")+"房间 · 电脑"+(level==1?"初心3星":"雀士3星"),2);
    var introView=Load("intro-"+n+"-"+level);client.ShowValidationFixture(introView);var intro=(MatchIntroduction)Field(client,"introduction");intro.Reenter(introView);intro.Freeze=true;intro.FrozenAge=2.5f;yield return Wait(12);Check(intro.Active&&intro.VisiblePlayers==n,"match introduction includes all computer ranks "+n+"/"+level);yield return Shot("intro-"+n+"-"+level);yield return Film("开局展示 · "+(n==3?"三人":"四人")+" · "+(level==1?"简单":"普通"),2);intro.Reset();
   }
   var human=new PlayerView{auto=true,aiLevel=2,profile=new PlayerRankProfile{set=true,rank4=10,rank3=11}};Check(RankRules.Level(human,4)==10&&RankRules.Level(human,3)==11,"human auto-play retains its own ranks");
  }
 }
}
