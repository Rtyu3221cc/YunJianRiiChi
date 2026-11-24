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
 [InitializeOnLoad] public static class RoomCacheR14Review {
  const string Key="RoomCacheR14Review";
  static RoomCacheR14Review(){EditorApplication.update+=()=>{if(EditorApplication.isPlaying&&!EditorApplication.isCompiling&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<RoomCacheR14Capture>();}};}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Bootstrap(){
   if(!SessionState.GetBool(Key,false))return;
   PlayerRecord.PreviewOnly=true;CharacterRoster.PreviewOnly=true;
   CharacterPackService.RootOverride=Environment.GetEnvironmentVariable("YUNJIAN_R14_CACHE");
   CharacterPackService.RespectPreviewAvailability=true;WardrobePackService.RespectPreviewAvailability=true;
   WardrobePackService.RootOverride=Environment.GetEnvironmentVariable("YUNJIAN_R10_OUT")+"/unused-wardrobes";
   string repo=Environment.GetEnvironmentVariable("YUNJIAN_R10_REPO");
   foreach(string key in new[]{"fulilian","feilun","xiutaerke","youbeier"}){
    var pack=CharacterPackService.Find(key);string dest=CharacterPackService.FilePath(pack);
    if(!File.Exists(dest)){Directory.CreateDirectory(Path.GetDirectoryName(dest));File.Copy(repo+"/build/rank-stick-r10/characters/windows/"+pack.file,dest);File.SetLastWriteTimeUtc(dest,DateTime.UtcNow);File.WriteAllText(dest+".ok",pack.sha256);File.SetLastWriteTimeUtc(dest+".ok",File.GetLastWriteTimeUtc(dest).AddSeconds(-2));}
   }
   if(Environment.GetCommandLineArgs().Contains("-r14Changed")){
    string path=CharacterPackService.FilePath(CharacterPackService.Find("fulilian"));File.SetLastWriteTimeUtc(path,File.GetLastWriteTimeUtc(path).AddSeconds(5));
   }
   CharacterPackService.RefreshInstalledCache();CharacterRoster.Use("youbeier",CharacterRoster.Find("youbeier").skins.Last().id);AppPreferences.SetDynamicOutfits(true);
  }
  public static void Run(){PlayerSettings.companyName="YunJianReview";PlayerSettings.productName="RoomCacheR14";EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
 }
 public sealed class RoomCacheR14Capture:MonoBehaviour {
  const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  [Serializable] class Report{public int checks;public List<string> passed=new List<string>();}
  readonly Report report=new Report();readonly List<string> chapters=new List<string>();RiichiClient client;LobbyController lobby;TableRenderer table;TableEffects effects;string output;System.Diagnostics.Process encoder;RenderTexture rt;Texture2D pixels;int frame;
  void Check(bool ok,string reason){if(!ok)throw new Exception(reason);report.checks++;report.passed.Add(reason);}
  IEnumerator Wait(int n=8){for(int i=0;i<n;i++)yield return null;}
  IEnumerator Shot(string name){yield return Wait();yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+name+".png",t.EncodeToPNG());Destroy(t);}
  int captureWidth,captureHeight;
  void StartFilm(){captureWidth=Screen.width;captureHeight=Screen.height;rt=new RenderTexture(captureWidth,captureHeight,0,RenderTextureFormat.ARGB32);rt.Create();pixels=new Texture2D(captureWidth,captureHeight,TextureFormat.RGB24,false);string repo=Environment.GetEnvironmentVariable("YUNJIAN_R10_REPO");encoder=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{FileName=@"C:\Users\Rtyu3221\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe",Arguments="-X utf8 \""+repo+"/scripts/encode-unity-capture-r40.py\" --output \""+output+"/room-cache-raw.mp4\" --width "+captureWidth+" --height "+captureHeight+" --fps 20",UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true});}
  IEnumerator Film(string label,float seconds,Action<int> action=null){chapters.Add(frame+" "+label);for(int i=0;i<Mathf.RoundToInt(seconds*20);i++){action?.Invoke(i);yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);var before=RenderTexture.active;RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,captureWidth,captureHeight),0,0);RenderTexture.active=before;var bytes=pixels.GetRawTextureData();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);frame++;}}
  IEnumerator Start(){output=Environment.GetEnvironmentVariable("YUNJIAN_R10_OUT");Directory.CreateDirectory(output);client=GetComponent<RiichiClient>();table=GetComponent<TableRenderer>();effects=GetComponent<TableEffects>();client.SessionPreviewOnly=true;client.BeginLobbyValidation("november-local");lobby=client.Lobby;GetComponent<ResourceRoomCoordinator>().enabled=false;GetComponent<WardrobePackService>().StopAllCoroutines();CharacterPackService.RespectPreviewAvailability=true;WardrobePackService.RespectPreviewAvailability=false;WardrobePackService.ResetForReview();AudioListener.volume=0;client.ValidationCaptureRequests=true;
   var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){object next;try{var t=stack.Peek();if(!t.MoveNext()){stack.Pop();continue;}next=t.Current;if(next is IEnumerator nested){stack.Push(nested);continue;}}catch(Exception e){if(encoder!=null&&!encoder.HasExited)encoder.Kill();File.WriteAllText(output+"/failed.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
   encoder.StandardInput.Close();while(!encoder.HasExited)yield return null;Check(encoder.ExitCode==0,"video encoded");File.WriteAllText(output+"/report.json",JsonUtility.ToJson(report,true));File.WriteAllLines(output+"/chapters.txt",chapters);EditorApplication.Exit(0);
  }




  object Field(object obj,string key)=>obj.GetType().GetField(key,Private).GetValue(obj);
  void Set(object obj,string key,object value)=>obj.GetType().GetField(key,Private).SetValue(obj,value);
  void Busy(bool busy,bool poll){typeof(RiichiClient).GetProperty("Busy").GetSetMethod(true).Invoke(client,new object[]{busy});Set(client,"requestIsPoll",poll);}
  bool Allowed=>(bool)typeof(LobbyController).GetProperty("RoomCustomizationAllowed",Private).GetValue(lobby);
  IEnumerator Press(string id){lobby.ValidationPress(id);yield return Wait(6);}
  bool Warm=>!Environment.GetCommandLineArgs().Contains("-r14Cold");
  bool Changed=>Environment.GetCommandLineArgs().Contains("-r14Changed");
  RoomView Room(){
   var v=JsonUtility.FromJson<RoomView>(File.ReadAllText(Environment.GetEnvironmentVariable("YUNJIAN_R10_REPO")+"/build/bot-difficulty-r13/fixtures/room-4-2.json"));
   string[] keys={"youbeier","fulilian","feilun","xiutaerke"};
   for(int i=0;i<4;i++){v.players[i].character=keys[i];v.players[i].skin=CharacterRoster.Find(keys[i]).skins.Last().id;v.players[i].wardrobe=new WardrobeLoadout();}
   return v;
  }
  string buttonHash;
  IEnumerator ButtonHash(){
   yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();var r=lobby.ValidationTarget("room-customize");
   int x=Mathf.RoundToInt(r.x)+8,y=Screen.height-Mathf.RoundToInt(r.yMax)+8,w=Mathf.RoundToInt(r.width)-16,h=Mathf.RoundToInt(r.height)-16;
   var p=t.GetPixels(x,y,w,h);byte[] bytes=new byte[p.Length*3];for(int i=0;i<p.Length;i++){bytes[i*3]=(byte)Mathf.RoundToInt(p[i].r*255);bytes[i*3+1]=(byte)Mathf.RoundToInt(p[i].g*255);bytes[i*3+2]=(byte)Mathf.RoundToInt(p[i].b*255);}
   using(var sha=System.Security.Cryptography.SHA256.Create())buttonHash=BitConverter.ToString(sha.ComputeHash(bytes));Destroy(t);
  }
  void Fingerprints(){
   string path=output+"/fingerprint.bundle";byte[] bytes={1,2,3,4,5};File.WriteAllBytes(path,bytes);
   var p=new CharacterPackRecord{key="fixture",bytes=bytes.Length,sha256=CharacterPackService.HashFile(path)};
   File.WriteAllText(path+".ok",p.sha256);CharacterPackFingerprint.Remember(p,path);
   Check(CharacterPackFingerprint.Current(p,path),"unchanged file fingerprint is reused");
   File.SetLastWriteTimeUtc(path,File.GetLastWriteTimeUtc(path).AddSeconds(4));Check(!CharacterPackFingerprint.Current(p,path),"changed modification time invalidates fingerprint");
   Check(CharacterPackService.HashFile(path)==p.sha256,"changed-time file remains valid after rehash");CharacterPackFingerprint.Remember(p,path);Check(CharacterPackFingerprint.Current(p,path),"rehash refreshes the fingerprint");
   File.WriteAllBytes(path,new byte[]{1,2,3,4,5,6});Check(!CharacterPackFingerprint.Current(p,path),"changed length invalidates fingerprint");
   File.WriteAllBytes(path,bytes);CharacterPackFingerprint.Remember(p,path);File.WriteAllText(path+".verified.json","{broken");Check(!CharacterPackFingerprint.Current(p,path),"corrupt verification cache is rejected");
   CharacterPackFingerprint.Remember(p,path);File.WriteAllText(path+".ok","wrong");Check(!CharacterPackFingerprint.Current(p,path),"wrong install receipt is rejected");
   string legacy=output+"/legacy.bundle";File.WriteAllBytes(legacy,bytes);File.SetLastWriteTimeUtc(legacy,DateTime.UtcNow.AddSeconds(-4));File.WriteAllText(legacy+".ok",p.sha256);
   Check(CharacterPackFingerprint.Current(p,legacy)&&File.Exists(legacy+".verified.json"),"verified legacy install receipt migrates without rehash");
   p.sha256="different";Check(!CharacterPackFingerprint.Current(p,legacy),"changed catalog digest invalidates cache");
  }
  [Serializable] class Timing {public bool warm,changed;public int fullHashes,verificationHits,bundleLoadsBeforeRoom,bundleLoadsAfterRoom;public float startupSeconds,roomSeconds;}
  IEnumerator Run(){
   var game=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));game.GetType().GetMethod("SetCustomResolution",Private).Invoke(game,new object[]{new Vector2(1600,900),"Room cache R14"});yield return Wait(3);StartFilm();
   Set(lobby,"startupCover",true);float start=Time.realtimeSinceStartup;
   yield return Film(Warm?"重启 · 复用未变资源的校验缓存":"首次准备 · 黑屏预热房间与装扮资源",2);
   float limit=Time.realtimeSinceStartup+20;while(!lobby.StartupPreloadDone&&Time.realtimeSinceStartup<limit)yield return null;
   var timing=new Timing{warm=Warm,changed=Changed,startupSeconds=Time.realtimeSinceStartup-start,fullHashes=CharacterPackService.ReviewFullHashChecks,verificationHits=CharacterPackService.ReviewVerificationCacheHits};
   Check(lobby.StartupPreloadDone,"startup preload finishes");Check((bool)Field(lobby,"roomInterfaceWarm"),"room customization interface preloads behind startup cover");
   var active=CharacterRoster.Active;Check(CharacterRoster.DisplayTexture(CharacterFraming.CardPath(active.skin),true)!=null,"selected room illustration is loaded before first room entry");
   Check(timing.fullHashes==(Warm?(Changed?1:0):4),"only changed or unverified packs require SHA "+timing.fullHashes);
   Check(timing.verificationHits==(Warm?(Changed?3:4):0),"unchanged installed pack fingerprints are reused "+timing.verificationHits);
   var room=Room();if(Warm)Check(room.players.All(p=>CharacterRoster.DisplayTexture(CharacterFraming.CardPath(p.skin),true)!=null),"last room illustrations are all ready before first room entry");
   timing.bundleLoadsBeforeRoom=CharacterPackService.ReviewAsyncBundleLoads;float roomStart=Time.realtimeSinceStartup;client.ShowValidationFixture(room);lobby.DesignPreviewClock=100;
   limit=Time.realtimeSinceStartup+15;while(Time.realtimeSinceStartup<limit&&!room.players.All(p=>CharacterRoster.DisplayTexture(CharacterFraming.CardPath(p.skin),true)!=null))yield return null;
   yield return Wait(30);timing.roomSeconds=Time.realtimeSinceStartup-roomStart;timing.bundleLoadsAfterRoom=CharacterPackService.ReviewAsyncBundleLoads;
   Check(room.players.All(p=>CharacterRoster.DisplayTexture(CharacterFraming.CardPath(p.skin),true)!=null),"all four real bundle room illustrations load");
   Check(room.players.All(p=>CharacterPackService.Pinned(p.character)),"visible room character resources stay pinned");
   if(Warm)Check(timing.bundleLoadsBeforeRoom==timing.bundleLoadsAfterRoom,"first room visit after restart opens no additional bundles");
   yield return Shot("room-ready");yield return Film("房间准备 · 立绘缓存就绪",2);
   Busy(false,false);yield return Wait(20);Check(Allowed,"customization enabled while idle");yield return ButtonHash();string gold=buttonHash;
   for(int i=0;i<3;i++){Busy(true,true);yield return Wait(3);Check(Allowed&&!client.CommandBusy,"background poll keeps customization available");yield return ButtonHash();Check(buttonHash==gold,"poll keeps identical gold button pixels");Busy(false,false);yield return Wait(3);}
   yield return Film("连续后台轮询 · 调整装扮按钮保持原色",6,i=>Busy(i%20<10,true));Busy(false,false);
   Busy(true,false);yield return Wait(4);Check(!Allowed&&client.CommandBusy,"real command still blocks conflicting changes");yield return ButtonHash();Check(buttonHash!=gold,"real command uses disabled button");Busy(false,false);
   Busy(true,true);yield return Press("room-customize");Check((bool)Field(lobby,"roomCustomizationOpen"),"customization opens during a background poll");yield return Shot("customization-ready");yield return Film("首次打开调整装扮 · 素材已预热",2);
   int requests=client.ValidationRequests.Count;yield return Press("room-role-feilun");Check(client.ValidationRequests.Count==requests&&(bool)Field(client,"characterSelectionDirty"),"selection made during poll is queued");
   Busy(false,false);yield return Wait(8);Check(client.ValidationRequests.Count==requests+1&&client.ValidationRequests.Last().character=="feilun","queued selection publishes once after poll");
   yield return Press("room-customize-close");yield return Wait(8);Busy(true,true);requests=client.ValidationRequests.Count;client.PublishCharacterSelection();room.resourceSetup=new ResourceSetupView{revision=55};Busy(false,false);yield return Wait(8);Check(client.ValidationRequests.Count==requests&&!(bool)Field(client,"characterSelectionDirty"),"resource preparation cancels stale queued character changes");
   room.resourceSetup=null;client.ShowValidationFixture(room);yield return Wait(8);client.BeginLobbyValidation("r14-end");yield return Wait(8);
   Check(((HashSet<string>)Field(lobby,"roomVisualPins")).Count==0&&((HashSet<string>)Field(lobby,"startupVisualPins")).Count==0,"leaving room releases room-owned pins");
   Fingerprints();File.WriteAllText(output+"/timing.json",JsonUtility.ToJson(timing,true));
  }
 }
}
