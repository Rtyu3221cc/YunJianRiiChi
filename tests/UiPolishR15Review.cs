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
 [InitializeOnLoad] public static class UiPolishR15Review {
  const string Key="UiPolishR15Review";
  static UiPolishR15Review(){EditorApplication.update+=()=>{if(EditorApplication.isPlaying&&!EditorApplication.isCompiling&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<UiPolishR15Capture>();}};}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Bootstrap(){if(!SessionState.GetBool(Key,false))return;PlayerRecord.PreviewOnly=true;CharacterRoster.PreviewOnly=true;PlayerPrefs.DeleteKey("riichi.wardrobe.presets.v3");CharacterPackService.RespectPreviewAvailability=true;WardrobePackService.RespectPreviewAvailability=true;CharacterPackService.RootOverride=Environment.GetEnvironmentVariable("YUNJIAN_R10_OUT")+"/unused-characters";WardrobePackService.RootOverride=Environment.GetEnvironmentVariable("YUNJIAN_R10_OUT")+"/unused-wardrobes";}
  public static void Run(){PlayerSettings.companyName="YunJianReview";PlayerSettings.productName="UiPolishR15";EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
 }
 public sealed class UiPolishR15Capture:MonoBehaviour {
  const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  [Serializable] class Report{public int checks;public List<string> passed=new List<string>();}
  readonly Report report=new Report();readonly List<string> chapters=new List<string>();RiichiClient client;LobbyController lobby;TableRenderer table;TableEffects effects;string output;System.Diagnostics.Process encoder;RenderTexture rt;Texture2D pixels;int frame;
  void Check(bool ok,string reason){if(!ok)throw new Exception(reason);report.checks++;report.passed.Add(reason);}
  IEnumerator Wait(int n=8){for(int i=0;i<n;i++)yield return null;}
  IEnumerator Shot(string name){yield return Wait();yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+name+".png",t.EncodeToPNG());Destroy(t);}
  int captureWidth,captureHeight;
  void StartFilm(){captureWidth=Screen.width;captureHeight=Screen.height;rt=new RenderTexture(captureWidth,captureHeight,0,RenderTextureFormat.ARGB32);rt.Create();pixels=new Texture2D(captureWidth,captureHeight,TextureFormat.RGB24,false);string repo=Environment.GetEnvironmentVariable("YUNJIAN_R10_REPO");encoder=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{FileName=@"C:\Users\Rtyu3221\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe",Arguments="-X utf8 \""+repo+"/scripts/encode-unity-capture-r40.py\" --output \""+output+"/ui-polish-raw.mp4\" --width "+captureWidth+" --height "+captureHeight+" --fps 20",UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true});}
  IEnumerator Film(string label,float seconds,Action<int> action=null){chapters.Add(frame+" "+label);for(int i=0;i<Mathf.RoundToInt(seconds*20);i++){action?.Invoke(i);yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);var before=RenderTexture.active;RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,captureWidth,captureHeight),0,0);RenderTexture.active=before;var bytes=pixels.GetRawTextureData();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);frame++;}}
  IEnumerator Start(){output=Environment.GetEnvironmentVariable("YUNJIAN_R10_OUT");Directory.CreateDirectory(output);client=GetComponent<RiichiClient>();table=GetComponent<TableRenderer>();effects=GetComponent<TableEffects>();client.SessionPreviewOnly=true;client.BeginLobbyValidation("november-local");lobby=client.Lobby;GetComponent<ResourceRoomCoordinator>().enabled=false;GetComponent<CharacterPackService>().StopAllCoroutines();GetComponent<WardrobePackService>().StopAllCoroutines();CharacterPackService.RespectPreviewAvailability=false;WardrobePackService.RespectPreviewAvailability=true;CharacterPackService.ResetForReview();WardrobePackService.ResetForReview();AudioListener.volume=0;client.ValidationCaptureRequests=true;
   var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){object next;try{var t=stack.Peek();if(!t.MoveNext()){stack.Pop();continue;}next=t.Current;if(next is IEnumerator nested){stack.Push(nested);continue;}}catch(Exception e){if(encoder!=null&&!encoder.HasExited)encoder.Kill();File.WriteAllText(output+"/failed.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
   encoder.StandardInput.Close();while(!encoder.HasExited)yield return null;Check(encoder.ExitCode==0,"video encoded");File.WriteAllText(output+"/report.json",JsonUtility.ToJson(report,true));File.WriteAllLines(output+"/chapters.txt",chapters);EditorApplication.Exit(0);
  }



  object Field(object obj,string key)=>obj.GetType().GetField(key,Private).GetValue(obj);
  void Set(object obj,string key,object value)=>obj.GetType().GetField(key,Private).SetValue(obj,value);
  IEnumerator Press(string id){lobby.ValidationPress(id);yield return Wait(5);}
  bool Near(float a,float b)=>Mathf.Abs(a-b)<.1f;
  RoomView Room(){var v=DressPreviewStates.Base("r15-room",new WardrobeLoadout(),"yiji",400101,"测试玩家");v.phase="lobby";v.host=true;v.pin="413373";return v;}
  [Serializable] class Geometry {public string context;public Rect pane,title,eye,view,use;public Rect[] cards;}
  Geometry Capture(string context){var q=lobby.ReviewWardrobeUi;return new Geometry{context=context,pane=q.pane,title=q.title,eye=q.eye,view=q.view,use=q.use,cards=q.cards.Values.Take(4).ToArray()};}
  void SameWardrobe(Geometry room,Geometry dorm){
   Check(room.cards.Length==4&&dorm.cards.Length==4,"both real grids draw four columns");
   for(int i=0;i<4;i++){Check(Near(room.cards[i].width,dorm.cards[i].width)&&Near(room.cards[i].height,dorm.cards[i].height),"matching original icon size column "+i);if(i>0)Check(Near(room.cards[i].x-room.cards[i-1].x,dorm.cards[i].x-dorm.cards[i-1].x),"matching icon spacing column "+i);}
   Check(Near(room.title.x-room.pane.x,dorm.title.x-dorm.pane.x)&&Near(room.title.y-room.pane.y,dorm.title.y-dorm.pane.y),"category labels share left and top inset");
   Check(Near(room.pane.xMax-room.eye.xMax,dorm.pane.xMax-dorm.eye.xMax)&&room.eye.size==dorm.eye.size,"preview button shares right inset and size");
   Check(room.use.size==dorm.use.size,"same active preset button size");
  }
  RoomView Score(int n,int you,string kind){
   var v=DressPreviewStates.Base("r15-"+n+"-"+you+"-"+kind,new WardrobeLoadout(),"yiji",400101,"旅人");v.n=n;v.you=you;v.players=v.players.Take(n).ToArray();v.turn=0;v.phase="result";
   for(int i=0;i<n;i++){v.players[i].name=i==0?"旅人":"电脑"+i;v.players[i].river=v.players[i].river.Where(t=>n==4||t.tile[0]!='m'||t.tile=="m1"||t.tile=="m9").ToArray();}
   if(n==3)v.hand=new[]{"m1","m9","p2","p3","p4","p5","p6","p7","s2","s3","s4","z5","z5"};
   int win=1;int[] delta=Enumerable.Range(0,n).Select(i=>kind=="ron"?(i==0?-8000:i==win?8000:0):i==win?(n-1)*1500:-1500).ToArray();
   for(int i=0;i<n;i++)v.players[i].score=(n==3?35000:25000)+delta[i];
   v.result=new ResultView{title=kind=="ron"?"荣和":kind=="tsumo"?"自摸":"荒牌流局",draw=kind=="draw",delta=delta,details=kind=="draw"?new WinDetail[0]:new[]{new WinDetail{seat=win,from=kind=="ron"?0:win,ron=kind=="ron",score=new ScoreDetail{delta=delta,defen=8000}}}};return v;
  }
  IEnumerator Run(){
   bool wide=Environment.GetCommandLineArgs().Contains("-r15Wide");var game=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));game.GetType().GetMethod("SetCustomResolution",Private).Invoke(game,new object[]{new Vector2(1600,wide?728:900),"UI polish R15"});yield return Wait(20);StartFilm();
   string[] samples={"三上千织","八木唯","小鸟游雏田","北见纱和子","福姬","青鸾","花语青","远坂凛","Saber"};
   var roles=samples.Select(name=>CharacterRoster.All.First(r=>r.name==name)).ToArray();foreach(var role in CharacterRoster.All.Where(r=>CharacterRoster.Favorite(r.key)).ToArray())CharacterRoster.Star(role.key);CharacterRoster.Reorder(roles.Select(r=>r.key).ToArray());
   var room=Room();client.ShowValidationFixture(room);yield return Wait(10);yield return Press("room-customize");Check(lobby.ReviewRoomCustomizationOpen,"room character page opens");
   foreach(var role in roles)Check(lobby.ValidationTarget("room-role-"+role.key).width>0,"long or Latin role is drawn: "+role.name);
   yield return Shot("room-long-names");yield return Film("房间角色 · 长名字与英文字母留出上下边距",2);
   var longest=CharacterRoster.All.OrderByDescending(r=>r.name.Length).Take(3).ToArray();CharacterRoster.Reorder(longest.Select(r=>r.key).Concat(roles.Select(r=>r.key)).ToArray());yield return Wait(8);yield return Shot("room-longest-names");yield return Film("最长名字 · 八字中文、标点与六字英文自适应字号",2);
   var available=CharacterRoster.Ordered(installedOnly:true);
   for(int row=0;row<Mathf.CeilToInt(available.Length/3f);row+=2){lobby.ReviewRoomRoleScroll(row*270);yield return Wait(3);}
   foreach(var role in available){Check(lobby.ReviewVerticalNames.ContainsKey(role.name),"actual role name rendered: "+role.name);var name=lobby.ReviewVerticalNames[role.name];Check(name.size+(role.name.Length-1)*name.step<=name.rect.height+.1f&&name.step>=name.size,"name stays inside banner without glyph overlap: "+role.name);}
   lobby.ReviewRoomRoleScroll(0);yield return Wait();
   yield return Press("room-tab-wardrobe");yield return Press("room-preset-1");yield return Press("room-preset-use");Check(WardrobePresets.Active==1,"room preset can still be worn");Check(client.ValidationRequests.Last().op=="character","room preset is published");
   Set(lobby,"roomCategoryScroll",new Vector2(0,8*130.2f));yield return Wait();yield return Press("room-category-tileBack");yield return Wait();
   Check(lobby.ValidationTarget("room-preset-rename").width==0,"preset title is read-only without pencil control");Check(WardrobePresets.Name(1)=="预设2","preset name remains unchanged");
   var roomGeometry=Capture("room");yield return Shot("room-wardrobe");yield return Film("房间装扮 · 统一图标、标题、预览与使用中状态",2);
   yield return Press("room-item-preview");Check((bool)Field(lobby,"dressEye")&&ReferenceEquals(client.State,room),"room static preview retains the live room");yield return Shot("room-preview");yield return Press("dress-preview-close");Check(!(bool)Field(lobby,"dressEye"),"preview returns to the shared item grid");
   string choice=lobby.ReviewWardrobeOrder.First(id=>WardrobePackService.Installed(id));yield return Press("room-item-"+choice);Check(WardrobePresets.Id(1,"tileBack")==choice&&client.ValidationRequests.Last().wardrobe.tileBack==choice,"room item change remains published");
   Set(lobby,"roomPresetScroll",new Vector2(0,2000));yield return Wait();yield return Press("room-preset-9");Check((int)Field(lobby,"dressPreset")==9&&WardrobePresets.Active==1,"room preset list scrolls to ten without wearing it");yield return Shot("room-preset-ten");yield return Film("预设分页 · 原版金色选中与绿色使用标记",1);
   Set(lobby,"roomPresetScroll",Vector2.zero);yield return Wait();yield return Press("room-preset-1");yield return Press("room-customize-close");client.BeginLobbyValidation("r15-dorm");lobby.PreviewWardrobe("tileBack");yield return Wait(16);
   var dormGeometry=Capture("dorm");SameWardrobe(roomGeometry,dormGeometry);yield return Shot("dorm-wardrobe");yield return Film("宿舍装扮 · 与房间使用同一套控件",2);
   Set(lobby,"dormPresetScroll",new Vector2(0,2000));yield return Wait();yield return Press("dress-preset-9");Check((int)Field(lobby,"dressPreset")==9&&WardrobePresets.Active==1,"dorm preset list scrolls to ten without wearing it");yield return Shot("dorm-preset-ten");
   yield return Press("dress-preset-use");Check(WardrobePresets.Active==9,"dorm use button retains preset behaviour");Set(lobby,"dormPresetScroll",Vector2.zero);yield return Wait();yield return Press("dress-preset-1");yield return Press("dress-preset-use");
   yield return Press("dress-eye");Check((bool)Field(lobby,"dressEye"),"same dorm static preview works");yield return Press("dress-preview-close");
   File.WriteAllText(output+"/wardrobe-room.json",JsonUtility.ToJson(roomGeometry,true));File.WriteAllText(output+"/wardrobe-dorm.json",JsonUtility.ToJson(dormGeometry,true));
   Set(lobby,"dormOpen",false);
   foreach(int n in new[]{3,4})foreach(int you in Enumerable.Range(0,n))foreach(string kind in new[]{"ron","tsumo","draw"}){
    var v=Score(n,you,kind);client.ShowValidationFixture(v);var display=(ResultPresentation)Field(client,"resultDisplay");display.Observe(v);Set(display,"scores",true);Set(display,"drawStage",false);display.Freeze=true;display.FrozenAge=1.4f;yield return Wait(8);
    var cards=display.ReviewScoreCards;Check(cards.Count==n,"all score cards drawn "+n+"/"+you+"/"+kind);
    foreach(var card in cards){int angle=Mathf.RoundToInt(table.SeatAngle(card.seat));int expected=angle==0?0:angle==-90?1:angle==180?2:3;Check(card.slot==expected,"score card follows actual table direction "+n+"/"+you+"/"+kind+"/"+card.seat);}
    foreach(int from in Enumerable.Range(0,n).Where(i=>v.result.delta[i]<0)){
     int to=1;if(kind=="ron"&&from!=0)continue;var expected=new Vector2Int(cards.Single(c=>c.seat==from).slot,cards.Single(c=>c.seat==to).slot);Check(display.ReviewScoreArrows.Contains(expected),"point arrow connects displayed cards "+n+"/"+you+"/"+kind+"/"+from);
    }
    if(n==3)Check(cards.All(c=>c.slot!=1)&&display.ReviewScoreArrows.All(a=>a.x!=1&&a.y!=1),"sanma never uses empty right-hand seat "+you+"/"+kind);
    if(you==0&&(n==3||kind=="ron")){yield return Shot("scores-"+n+"-"+kind);yield return Film(n+"人结算 · "+kind+" · 信息卡与点数流向匹配方位",3,i=>display.FrozenAge=i/20f+1);}
   }
  }
 }
}
