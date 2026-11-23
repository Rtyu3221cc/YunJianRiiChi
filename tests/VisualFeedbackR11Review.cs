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
 [InitializeOnLoad] public static class VisualFeedbackR11Review {
  const string Key="VisualFeedbackR11Review";
  static VisualFeedbackR11Review(){EditorApplication.update+=()=>{if(EditorApplication.isPlaying&&!EditorApplication.isCompiling&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<VisualFeedbackR11Capture>();}};}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Bootstrap(){if(!SessionState.GetBool(Key,false))return;PlayerRecord.PreviewOnly=true;CharacterRoster.PreviewOnly=true;CharacterPackService.RespectPreviewAvailability=true;WardrobePackService.RespectPreviewAvailability=true;CharacterPackService.RootOverride=Environment.GetEnvironmentVariable("YUNJIAN_R10_OUT")+"/unused-characters";WardrobePackService.RootOverride=Environment.GetEnvironmentVariable("YUNJIAN_R10_OUT")+"/unused-wardrobes";}
  public static void Run(){PlayerSettings.companyName="YunJianReview";PlayerSettings.productName="VisualFeedbackR11";EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
 }
 public sealed class VisualFeedbackR11Capture:MonoBehaviour {
  const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  [Serializable] class Report{public int checks;public List<string> passed=new List<string>();}
  readonly Report report=new Report();readonly List<string> chapters=new List<string>();RiichiClient client;LobbyController lobby;TableRenderer table;TableEffects effects;string output;System.Diagnostics.Process encoder;RenderTexture rt;Texture2D pixels;int frame;
  void Check(bool ok,string reason){if(!ok)throw new Exception(reason);report.checks++;report.passed.Add(reason);}
  IEnumerator Wait(int n=8){for(int i=0;i<n;i++)yield return null;}
  IEnumerator Shot(string name){yield return Wait();yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+name+".png",t.EncodeToPNG());Destroy(t);}
  void StartFilm(){rt=new RenderTexture(1600,900,0,RenderTextureFormat.ARGB32);rt.Create();pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);string repo=Environment.GetEnvironmentVariable("YUNJIAN_R10_REPO");encoder=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{FileName=@"C:\Users\Rtyu3221\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe",Arguments="-X utf8 \""+repo+"/scripts/encode-unity-capture-r40.py\" --output \""+output+"/feedback-raw.mp4\" --width 1600 --height 900 --fps 20",UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true});}
  IEnumerator Film(string label,float seconds,Action<int> action=null){chapters.Add(frame+" "+label);for(int i=0;i<Mathf.RoundToInt(seconds*20);i++){action?.Invoke(i);yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);var before=RenderTexture.active;RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1600,900),0,0);RenderTexture.active=before;var bytes=pixels.GetRawTextureData();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);frame++;}}
  IEnumerator Start(){output=Environment.GetEnvironmentVariable("YUNJIAN_R10_OUT");Directory.CreateDirectory(output);client=GetComponent<RiichiClient>();table=GetComponent<TableRenderer>();effects=GetComponent<TableEffects>();client.SessionPreviewOnly=true;client.BeginLobbyValidation("november-local");lobby=client.Lobby;GetComponent<ResourceRoomCoordinator>().enabled=false;GetComponent<CharacterPackService>().StopAllCoroutines();GetComponent<WardrobePackService>().StopAllCoroutines();CharacterPackService.RespectPreviewAvailability=false;WardrobePackService.RespectPreviewAvailability=false;CharacterPackService.ResetForReview();WardrobePackService.ResetForReview();AudioListener.volume=0;
   var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){object next;try{var t=stack.Peek();if(!t.MoveNext()){stack.Pop();continue;}next=t.Current;if(next is IEnumerator nested){stack.Push(nested);continue;}}catch(Exception e){if(encoder!=null&&!encoder.HasExited)encoder.Kill();File.WriteAllText(output+"/failed.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
   encoder.StandardInput.Close();while(!encoder.HasExited)yield return null;Check(encoder.ExitCode==0,"video encoded");File.WriteAllText(output+"/report.json",JsonUtility.ToJson(report,true));File.WriteAllLines(output+"/chapters.txt",chapters);EditorApplication.Exit(0);
  }

  [Serializable] class SkinBank {public CharacterDefinition[] roles;}
  [Serializable] class RenderRow {public string path;public int sortingOrder;public RenderMaterial[] materials;}
  [Serializable] class RenderMaterial {public int customQueue;}
  [Serializable] class RenderBank {public RenderRow[] rows;}
  bool HomeOnly=>Environment.GetCommandLineArgs().Contains("-r11HomeOnly");
  void CheckRenderOrder(WinTileEffect win,int seat){
   var original=JsonUtility.FromJson<RenderBank>(File.ReadAllText(Environment.GetEnvironmentVariable("YUNJIAN_R10_REPO")+"/docs/reference/november-2026/flower-render-order-r11.json"));
   var native=win.GetComponent<NativeParticleEffect>();var root=(Transform)typeof(NativeParticleEffect).GetField("authoredRoot",Private).GetValue(native);
   foreach(var r in win.GetComponentsInChildren<ParticleSystemRenderer>(true)){
    var parts=new List<string>();var t=r.transform;while(t!=null&&t!=root){parts.Insert(0,t.name);t=t.parent;}string path=root.name+"/"+string.Join("/",parts);var row=original.rows.Single(v=>v.path==path);
    Check(r.sharedMaterials.Select((m,i)=>m.renderQueue==(row.materials[i].customQueue>=0?row.materials[i].customQueue:3000)).All(v=>v),"original queue "+seat+" "+path);
   }
  }
  bool Before=>Environment.GetCommandLineArgs().Contains("-r11Before");
  RoomView Final(){var v=DressPreviewStates.Base("rank-r11",new WardrobeLoadout(),"yiji",400101,"云间牌友");v.phase="result";v.lobby.category="friend";v.players[0].score=41200;v.result=new ResultView{title="终局",delta=new int[4],details=new WinDetail[0],matchOver=true,rules=new ResultRules{n=4,rounds=2,start=25000,category="friend"},standings=Enumerable.Range(0,4).Select(i=>new StandingView{seat=i,place=i+1,score=i==0?41200:19600,bot=i!=0,stats=new SeatStats()}).ToArray()};return v;}
  IEnumerator Run(){
   var game=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));game.GetType().GetMethod("SetCustomResolution",Private).Invoke(game,new object[]{new Vector2(1600,900),"Visual feedback R11"});yield return Wait(20);StartFilm();
   foreach(string id in (HomeOnly?Array.Empty<string>():new[]{"liqi_hl","liqi_21chunjie","liqi_21catball","liqi_22summer","liqi_xzw22121","liqi_23wansheng"})){
    var v=DressPreviewStates.Base("r11-"+id,new WardrobeLoadout(),"yiji",400101,"云间牌友");for(int s=0;s<4;s++){v.players[s].wardrobe=new WardrobeLoadout{stick=id};v.players[s].riichi=1;}client.ShowValidationFixture(v);yield return Wait(16);
    for(int s=0;s<4;s++){
     var stick=table.StickTarget(s);var renderers=stick.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&!WardrobeModelVisual.IsShadow(r)).ToArray();Check(renderers.Length>0,id+" body visible seat "+s);
     var body=renderers.Where(r=>r.sharedMaterials.Any(m=>m.renderQueue<3000)).ToArray();if(body.Length==0)body=renderers;var b=body[0].bounds;foreach(var r in body.Skip(1))b.Encapsulate(r.bounds);
     File.AppendAllText(output+"/stick-bounds.txt",id+" seat="+s+" body="+b+"\n");
     if(!Before&&id=="liqi_23wansheng"){Check(stick.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="yinzi").All(r=>!r.gameObject.activeInHierarchy),"original wand shadow excluded seat "+s);Check(Mathf.Max(b.size.x,b.size.z)>.55f,"wand body fills normal stick length seat "+s);}
    }
    yield return Shot("stick-"+id);yield return Film(WardrobeCatalog.Find(id).name,2);
   }
   client.BeginLobbyValidation("r11-home");lobby=client.Lobby;
   foreach(string key in new[]{"fulilian","feilun","xiutaerke","youbeier"}){
    var role=CharacterRoster.Find(key);var skin=role.skins.Last();
    if(Before){var bank=JsonUtility.FromJson<SkinBank>(File.ReadAllText(Environment.GetEnvironmentVariable("YUNJIAN_R10_REPO")+"/build/visual-feedback-r11/baseline/unity/Assets/Resources/Majsoul/CharacterDirectory/catalog.json"));var saved=bank.roles.First(r=>r.key==key).skins.First(x=>x.id==skin.id);skin.spineWidth=saved.spineWidth;skin.spineHeight=saved.spineHeight;skin.pivotX=saved.pivotX;skin.pivotY=saved.pivotY;}
    Check(!(bool)typeof(RiichiClient).GetField("active",Private).GetValue(client),"actual lobby view "+key);
    CharacterRoster.Use(key,skin.id);lobby.DesignPreviewClock=1000;lobby.Home();lobby.DesignPreviewClock=1002;yield return Wait(30);
    if(!Before){int index=Array.IndexOf(new[]{"fulilian","feilun","xiutaerke","youbeier"},key);Check(skin.spineWidth==new[]{3690,4079,4117,5003}[index]&&skin.pivotX==new[]{1748,2159,2017,2446}[index]&&skin.pivotY==new[]{3455,2500,4151,2802}[index],"original v2 Spine canvas and pivot "+key);}
    yield return Shot("skin-"+skin.id);yield return Film(role.name+" · 原版动态构图",3,i=>lobby.DesignPreviewClock=1002+i/20f);
   }
   if(HomeOnly)yield break;
   PlayerRecord.Replace(new PlayerRecordData{rank4=new RankState(4,10)});var final=Final();var change=PlayerRecord.Record(final);change.before.points=10;change.after=new RankState(4,790);change.delta=780;client.ShowValidationFixture(final);var display=(ResultPresentation)typeof(RiichiClient).GetField("resultDisplay",Private).GetValue(client);display.Observe(final);display.Freeze=true;display.FrozenAge=100;display.Advance(final);display.Advance(final);Check(display.ShowingRank,"rank progress page active");yield return Film("段位进度 1% → 99%",5,i=>display.FrozenAge=i/20f);
   foreach(int point in new[]{80,200,400,600,790}){change.before.points=point;change.after.points=point;display.FrozenAge=5;yield return Shot("rank-"+point);}
   effects.ValidationFreeze=true;effects.ValidationSequentialClock=true;
   var load=new WardrobeLoadout{cloth="tablecloth_26fll",tileBack="mjp_26fll",headFrame="headframe_26fll",winEffect="ron_26fll"};
   client.ShowValidationFixture(DressPreviewStates.Base("r11-win-table",load,"fulilian",40012801,"芙莉莲"));yield return Wait(8);
   for(int seat=0;seat<4;seat++){
    var next=DressPreviewStates.Tsumo("r11-win-"+seat,load,"fulilian",40012801,"芙莉莲",seat);client.ShowValidationFixture(next);client.ValidationTableOnly=true;effects.ValidationFreeze=true;effects.ValidationTime=0;yield return Wait(8);var d=next.result.details[0];var sound=new PublicSound{id=81+seat,kind="tsumo",seat=seat,from=seat,tile=d.tile,winSource="tsumo"};var win=new GameObject("R11 source win").AddComponent<WinTileEffect>();win.transform.SetParent(table.transform,false);win.WinningEvent=sound;win.Initialize(table.WinningPoint(sound),table.SeatAngle(seat),0,71,"Majsoul/NativeReview/NovR10/ron_26fll-playback");win.BindWinningTile(table.WinningTarget(sound));Check(win.LayerCount==54,"all original ron layers seat "+seat);if(!Before)CheckRenderOrder(win,seat);
    yield return Film("变出花田的魔法 · "+DressPreviewStates.SeatName(seat),6,i=>win.Sample(i/20f));
    foreach(float age in new[]{.5f,1.5f,2.5f,4f}){win.Sample(age);yield return Shot("win-"+seat+"-"+age.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));}
    File.AppendAllText(output+"/fx-transforms.txt", "SEAT "+seat+" target="+table.WinningTarget(sound).rotation.eulerAngles+"\n");
    foreach(var t in win.GetComponentsInChildren<Transform>(true).Where(t=>new[]{"root_static","root_rot","root_move"}.Contains(t.name)))File.AppendAllText(output+"/fx-transforms.txt",t.name+" pos="+t.position+" rot="+t.rotation.eulerAngles+" scale="+t.lossyScale+"\n");
    foreach(var p in win.GetComponentsInChildren<ParticleSystem>(true).Where(p=>p.name=="mask"||p.transform.parent.name=="light"))File.AppendAllText(output+"/fx-transforms.txt",p.name+" size="+p.main.startSize.constant+" pos="+p.transform.position+" rot="+p.transform.rotation.eulerAngles+" bounds="+p.GetComponent<ParticleSystemRenderer>().bounds+"\n");
    var authored=win.GetComponentsInChildren<Transform>(true).First(t=>t.name=="root_static");File.AppendAllText(output+"/environment.txt",seat+" "+authored.rotation.eulerAngles+"\n");
    if(!Before)Check(Quaternion.Angle(authored.rotation,Quaternion.Euler(0,180,0))<.1f,"scene lighting independent of winner seat "+seat);
    Destroy(win.gameObject);yield return Wait();
   }
  }
 }
}
