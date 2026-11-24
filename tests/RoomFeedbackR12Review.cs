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
 [InitializeOnLoad] public static class RoomFeedbackR12Review {
  const string Key="RoomFeedbackR12Review";
  static RoomFeedbackR12Review(){EditorApplication.update+=()=>{if(EditorApplication.isPlaying&&!EditorApplication.isCompiling&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<RoomFeedbackR12Capture>();}};}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Bootstrap(){if(!SessionState.GetBool(Key,false))return;PlayerRecord.PreviewOnly=true;CharacterRoster.PreviewOnly=true;CharacterPackService.RespectPreviewAvailability=true;WardrobePackService.RespectPreviewAvailability=true;CharacterPackService.RootOverride=Environment.GetEnvironmentVariable("YUNJIAN_R10_OUT")+"/unused-characters";WardrobePackService.RootOverride=Environment.GetEnvironmentVariable("YUNJIAN_R10_OUT")+"/unused-wardrobes";}
  public static void Run(){PlayerSettings.companyName="YunJianReview";PlayerSettings.productName="RoomFeedbackR12";EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
 }
 public sealed class RoomFeedbackR12Capture:MonoBehaviour {
  const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  [Serializable] class Report{public int checks;public List<string> passed=new List<string>();}
  readonly Report report=new Report();readonly List<string> chapters=new List<string>();RiichiClient client;LobbyController lobby;TableRenderer table;TableEffects effects;string output;System.Diagnostics.Process encoder;RenderTexture rt;Texture2D pixels;int frame;
  void Check(bool ok,string reason){if(!ok)throw new Exception(reason);report.checks++;report.passed.Add(reason);}
  IEnumerator Wait(int n=8){for(int i=0;i<n;i++)yield return null;}
  IEnumerator Shot(string name){yield return Wait();yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+name+".png",t.EncodeToPNG());Destroy(t);}
  int captureWidth,captureHeight;
  void StartFilm(){captureWidth=Screen.width;captureHeight=Screen.height;rt=new RenderTexture(captureWidth,captureHeight,0,RenderTextureFormat.ARGB32);rt.Create();pixels=new Texture2D(captureWidth,captureHeight,TextureFormat.RGB24,false);string repo=Environment.GetEnvironmentVariable("YUNJIAN_R10_REPO");encoder=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{FileName=@"C:\Users\Rtyu3221\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe",Arguments="-X utf8 \""+repo+"/scripts/encode-unity-capture-r40.py\" --output \""+output+"/room-feedback-raw.mp4\" --width "+captureWidth+" --height "+captureHeight+" --fps 20",UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true});}
  IEnumerator Film(string label,float seconds,Action<int> action=null){chapters.Add(frame+" "+label);for(int i=0;i<Mathf.RoundToInt(seconds*20);i++){action?.Invoke(i);yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);var before=RenderTexture.active;RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,captureWidth,captureHeight),0,0);RenderTexture.active=before;var bytes=pixels.GetRawTextureData();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);frame++;}}
  IEnumerator Start(){output=Environment.GetEnvironmentVariable("YUNJIAN_R10_OUT");Directory.CreateDirectory(output);client=GetComponent<RiichiClient>();table=GetComponent<TableRenderer>();effects=GetComponent<TableEffects>();client.SessionPreviewOnly=true;client.BeginLobbyValidation("november-local");lobby=client.Lobby;GetComponent<ResourceRoomCoordinator>().enabled=false;GetComponent<CharacterPackService>().StopAllCoroutines();GetComponent<WardrobePackService>().StopAllCoroutines();CharacterPackService.RespectPreviewAvailability=false;WardrobePackService.RespectPreviewAvailability=false;CharacterPackService.ResetForReview();WardrobePackService.ResetForReview();AudioListener.volume=0;client.ValidationCaptureRequests=true;
   var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){object next;try{var t=stack.Peek();if(!t.MoveNext()){stack.Pop();continue;}next=t.Current;if(next is IEnumerator nested){stack.Push(nested);continue;}}catch(Exception e){if(encoder!=null&&!encoder.HasExited)encoder.Kill();File.WriteAllText(output+"/failed.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
   encoder.StandardInput.Close();while(!encoder.HasExited)yield return null;Check(encoder.ExitCode==0,"video encoded");File.WriteAllText(output+"/report.json",JsonUtility.ToJson(report,true));File.WriteAllLines(output+"/chapters.txt",chapters);EditorApplication.Exit(0);
  }


  object Field(object obj,string key)=>obj.GetType().GetField(key,Private).GetValue(obj);
  void Set(object obj,string key,object value)=>obj.GetType().GetField(key,Private).SetValue(obj,value);
  IEnumerator Press(string id){lobby.ValidationPress(id);yield return Wait(4);}
  RoomView Room(int n=4){var active=CharacterRoster.Active;var v=DressPreviewStates.Base("room-r12-"+n,new WardrobeLoadout{headFrame="headframe_26fll"},active.character,active.skin,"旅人");v.phase="lobby";v.host=true;v.pin="437947";v.n=n;v.players=v.players.Take(n).ToArray();v.players[0].profile=PlayerRecord.Profile;v.players[0].ready=true;return v;}

  [Serializable] class LoadingTiming {public string character;public float stillSeconds=-1,dynamicSeconds=-1;public int backgroundPending;}
  IEnumerator AsyncRun(){
   lobby.StopAllCoroutines();var art=(CharacterIllustration)Field(lobby,"characterArt");art.Dispose();CharacterPackService.ResetForReview();CharacterPackService.RootOverride=output+"/installed";CharacterPackService.RespectPreviewAvailability=true;CharacterPackService.ReviewDisplayOrder.Clear();AppPreferences.SetDynamicOutfits(true);
   string repo=Environment.GetEnvironmentVariable("YUNJIAN_R10_REPO");
   foreach(string key in new[]{"fulilian","feilun","xiutaerke","youbeier"}){var p=CharacterPackService.Find(key);string source=repo+"/build/rank-stick-r10/characters/windows/"+p.file;Check(File.Exists(source)&&new FileInfo(source).Length==p.bytes,"reuse published character cache "+key);string dest=CharacterPackService.FilePath(p);Directory.CreateDirectory(Path.GetDirectoryName(dest));File.Copy(source,dest,true);File.WriteAllText(dest+".ok",p.sha256);}
   CharacterPackService.RefreshInstalledCache();client.BeginLobbyValidation("r12-async");lobby=client.Lobby;CharacterRoster.Use("yiji",400101);lobby.DesignPreviewClock=1002;lobby.Home();lobby.DesignPreviewClock=1004;yield return Wait(12);yield return Shot("loading-home-before");
   foreach(string key in new[]{"fulilian","feilun","xiutaerke"})foreach(var skin in CharacterRoster.Find(key).skins){CharacterPackService.DisplayAsset<Texture2D>("Skins/"+skin.id+"/bighead");CharacterPackService.DisplayAsset<Texture2D>("Skins/"+skin.id+"/smallhead");}
   var selected=CharacterRoster.Find("youbeier").skins.Last();CharacterRoster.Use("youbeier",selected.id);var timing=new LoadingTiming{character="youbeier"};float began=Time.realtimeSinceStartup;
   yield return Film("大厅切换 · 实际资源包异步加载",5,i=>{lobby.DesignPreviewClock=1004+i/20f;float elapsed=Time.realtimeSinceStartup-began;if(timing.stillSeconds<0&&CharacterRoster.DisplayTexture("Skins/"+selected.id+"/full",true)!=null)timing.stillSeconds=elapsed;if(timing.dynamicSeconds<0&&art.Ready(selected))timing.dynamicSeconds=elapsed;});
   Check(timing.stillSeconds>=0&&timing.dynamicSeconds>=0,"selected dynamic outfit loaded from real bundle");Check(CharacterPackService.ReviewSyncBundleLoads==0,"lobby display loads avoid synchronous bundle loading");
   var trace=CharacterPackService.ReviewDisplayOrder;int full=trace.FindIndex(p=>p=="Skins/"+selected.id+"/full"),background=trace.FindIndex(p=>p.EndsWith("/bighead")||p.EndsWith("/smallhead"));Check(full>=0&&background>full,"selected illustration jumps pending thumbnail backlog");
   object parts=((System.Collections.IDictionary)Field(art,"dynamics"))[selected.id];int version=CharacterPackService.VisualVersion("youbeier");typeof(CharacterPackService).GetMethod("ReleaseCached",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"fulilian"});art.Art(selected,1006);Check(art.Ready(selected)&&ReferenceEquals(parts,((System.Collections.IDictionary)Field(art,"dynamics"))[selected.id])&&CharacterPackService.VisualVersion("youbeier")==version,"unrelated bundle eviction retains active Spine rig");
   yield return Shot("loading-home-after");yield return Film("加载完成 · 保留原版动态构图",2,i=>lobby.DesignPreviewClock=1010+i/20f);
   File.WriteAllText(output+"/loading-timing.json",JsonUtility.ToJson(timing,true));File.WriteAllLines(output+"/display-request-order.txt",trace);
   CharacterPackService.RespectPreviewAvailability=false;
  }

  IEnumerator WideRun(){
   var roomBrush=Resources.Load<Font>("Majsoul/Lobby/Fonts/brush");var roleBrush=Resources.Load<Font>("Majsoul/Characters/Fonts/character-name-brush");var roleNames=CharacterRoster.All.SelectMany(r=>r.name).Distinct().ToArray();
   Check(roomBrush!=null&&roleNames.All(c=>roomBrush.HasCharacter(c)),"lobby native brush covers all 127 role names");Check(roleBrush!=null&&roleNames.All(c=>roleBrush.HasCharacter(c)),"role native brush covers all 127 role names");
   foreach(char c in "伦尤泠")Check(roomBrush.HasCharacter(c)&&roleBrush.HasCharacter(c),"native brush glyph present: "+c);
   var nameStyle=(GUIStyle)typeof(LobbyController).GetMethod("CharacterNameText",Private).Invoke(lobby,new object[]{29,TextAnchor.MiddleCenter});Check(nameStyle.font==roleBrush,"room and dorm names use the same native role font");
   Check(Screen.width==1600&&Screen.height==728,"wide phone reference aspect");CharacterRoster.Use("youbeier",CharacterRoster.Find("youbeier").skins.Last().id);
   var room=Room(3);client.ShowValidationFixture(room);yield return Wait(15);yield return Shot("wide-room-three");yield return Film("房间准备 · 与参考视频相同宽屏比例",2);
   yield return Press("room-customize");var roles=CharacterRoster.Ordered(installedOnly:true);int role=Array.FindIndex(roles,r=>r.key=="youbeier");lobby.ReviewRoomRoleScroll(role/3*270);yield return Shot("wide-room-characters");Check(lobby.ReviewRoomCustomizationOpen,"wide character page interactive");yield return Film("房间内调整装扮 · 角色与外观选择",3);
   yield return Press("room-tab-wardrobe");yield return Shot("wide-room-wardrobe");yield return Film("房间内调整装扮 · 预设与物品",3);
   var frame=lobby.ReviewRoomCustomizationRect("");Check(frame.xMin>0&&frame.xMax<lobby.ReviewLayoutWidth,"wide native frame stays inside phone bounds");
   yield return Press("room-customize-close");room=Room(4);client.ShowValidationFixture(room);yield return Wait(12);yield return Shot("wide-room-four");yield return Film("四人房间 · 房主标记与称号",2);
   yield return Press("room-customize");Check(lobby.ReviewRoomCustomizationOpen,"can reopen room customization before preparing");int requests=client.ValidationRequests.Count;room.resourceSetup=new ResourceSetupView{revision=6};yield return Wait(8);Check(!lobby.ReviewRoomCustomizationOpen&&client.ValidationRequests.Count==requests,"preparation closes dialog without a stale character request");client.PublishCharacterSelection();yield return Wait(2);Check(client.ValidationRequests.Count==requests,"resource preparation also guards direct character publication");
  }
  IEnumerator Run(){
   var game=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));game.GetType().GetMethod("SetCustomResolution",Private).Invoke(game,new object[]{new Vector2(1600,Environment.GetCommandLineArgs().Contains("-r12Wide")?728:900),"Room feedback R12"});yield return Wait(20);StartFilm();if(Environment.GetCommandLineArgs().Contains("-r12Wide")){yield return WideRun();yield break;}if(Environment.GetCommandLineArgs().Contains("-r12Async")){yield return AsyncRun();yield break;}
   CharacterRoster.Use("yiji",400101);var room=Room();client.ShowValidationFixture(room);yield return Wait(12);
   Check(lobby.Page==LobbyController.ScreenPage.Room,"actual ready-room page");Check(lobby.ReviewRoomCustomizationAllowed,"host can customize despite own ready flag");
   yield return Shot("room-initial");yield return Film("房间 · 原版简体称号与右上房主标记",2);
   yield return Press("room-customize");Check(lobby.ReviewRoomCustomizationOpen,"room button opens in-room character dialog");var nativeLayout=(RoomCustomizationLayout)Field(lobby,"roomDressLayout");Check(nativeLayout.RoleBorder(200001).EndsWith("bg_head_qy2"),"all unlocked roles use native contract frame");foreach(int ur in new[]{200052,200061,200076,200095,20000112})Check(nativeLayout.RoleBorder(ur).EndsWith("bg_urhead_qy"),"native UR contract frame "+ur);yield return Shot("room-character-page");yield return Film("角色页 · 三列角色与外观选择",2);
   var roles=CharacterRoster.Ordered(installedOnly:true);int roleIndex=Array.FindIndex(roles,r=>r.key=="youbeier");lobby.ReviewRoomRoleScroll(roleIndex/3*270);yield return Wait();yield return Shot("room-character-scroll");
   yield return Press("room-role-youbeier");Check(CharacterRoster.Active.character=="youbeier","character selection applied without leaving room");Check(client.ValidationRequests.Last().character=="youbeier","character published to room peers");
   int skin=CharacterRoster.Find("youbeier").skins.Last().id;yield return Press("room-skin-"+skin);Check(CharacterRoster.Active.skin==skin&&client.ValidationRequests.Last().skin==skin,"outfit selection applied and published");yield return Shot("room-skin-selected");yield return Film("尤贝尔 · 选择角色与动态外观",2);
   yield return Press("room-tab-wardrobe");yield return Shot("room-wardrobe-page");yield return Film("装扮页 · 预设、分类与物品列表",2);
   yield return Press("room-preset-1");yield return Press("room-preset-use");Check(WardrobePresets.Active==1,"wear preset from ready room");
   Set(lobby,"roomCategoryScroll",new Vector2(0,6*130.2f));yield return Wait();yield return Press("room-category-headFrame");
   var entries=WardrobeCatalog.Items("headFrame").OrderBy(i=>WardrobePackService.Installed(i.id)?0:1).ToArray();int entry=Array.FindIndex(entries,i=>i.id=="headframe_26fll");Set(lobby,"roomItemScroll",new Vector2(0,entry/4*163));yield return Wait();yield return Press("room-item-headframe_26fll");Check(WardrobePresets.Id(1,"headFrame")=="headframe_26fll","item stored in viewed preset");Check(client.ValidationRequests.Last().wardrobe.headFrame=="headframe_26fll","wardrobe selection included in character room operation");yield return Press("room-item-preview");Check((bool)Field(lobby,"dressEye")&&ReferenceEquals(client.State,room),"static wardrobe preview retains live room state");yield return Shot("room-headframe-preview");yield return Press("dress-preview-close");Check(!(bool)Field(lobby,"dressEye"),"static preview returns to wardrobe grid");yield return Shot("room-wardrobe-selected");yield return Film("房间内切换装扮并同步",2);
   yield return Press("room-preset-rename");Set(lobby,"roomPresetName","尤贝尔装扮");yield return Press("room-rename-save");Check(WardrobePresets.Name(1)=="尤贝尔装扮","preset rename saved");
   yield return Press("room-customize-close");Check(!lobby.ReviewRoomCustomizationOpen,"dialog closes back to room");room.players[0].character="youbeier";room.players[0].skin=skin;room.players[0].wardrobe=WardrobeLoadout.Local();client.ValidationApplyState(room);yield return Shot("room-after-selection");yield return Film("返回房间 · 保留角色、皮肤与装扮",2);
   room.host=false;room.players[0].ready=true;Check(!lobby.ReviewRoomCustomizationAllowed,"ready guest cannot alter frozen choice");room.players[0].ready=false;Check(lobby.ReviewRoomCustomizationAllowed,"unready guest may customize");room.resourceSetup=new ResourceSetupView{revision=3};Check(!lobby.ReviewRoomCustomizationAllowed,"resource preparation locks customization");room.resourceSetup=null;
   var hand=CharacterHands.Default("youbeier");Check(hand!=null&&hand.sourceItemId==30990020&&hand.nativeHand=="hand_youbeier","original Ubel hand binding");
   for(int seat=0;seat<4;seat++)foreach(string action in new[]{"Dapai","LiQiPai","LiZhi","Fulu","Zimo"}){
    var clip=CharacterHands.Clip(hand,seat,action);Check(clip.frames>10&&clip.pose.tracks.SelectMany(t=>t.curves).Any(),"original changing bones "+seat+" "+action);Check(clip.tilePoints.Length==clip.frames*3&&clip.tileRotations.Length==clip.frames*4,"carried object track "+seat+" "+action);
    var prefab=Resources.Load<GameObject>(clip.prefab);Check(prefab!=null,"original hand prefab "+seat);var mats=prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
    foreach(var m in mats.Where(m=>m.GetFloat("_SourceBRDF")>0)){Check(m.shader.isSupported&&m.GetTexture("_RimMaskMap")!=null,"Ubel source rim mask "+m.name);Check(m.GetFloat("_SourceBRDF")>=2,"original Custom2/CustomView material "+m.name);}
   }
   for(int n=3;n<=4;n++){
    var v=DressPreviewStates.Base("hud-r12-"+n,new WardrobeLoadout(),"youbeier",skin,"玩家自己的名称");v.n=n;v.players=v.players.Take(n).ToArray();for(int seat=0;seat<n;seat++){v.players[seat].wardrobe=new WardrobeLoadout{headFrame="headframe_26fll"};v.players[seat].profile=PlayerRecord.Profile;}
    client.ShowValidationFixture(v);yield return Wait(12);Check(client.ReviewHudRows.Count==n,"all HUD seats drawn "+n);foreach(var row in client.ReviewHudRows){Check(row.name.yMin>=row.frame.yMax,"name clear of avatar frame "+n+"/"+row.seat);Check(Mathf.Abs(row.title.width-client.ReviewHudRows[0].title.width)<.01f,"equal title scale "+n+"/"+row.seat);}
    yield return Shot("hud-"+n);yield return Film(n+"人 · 头像框、名称与称号",2);
   }
   for(int seat=0;seat<4;seat++){
    var v=DressPreviewStates.Base("hand-r12-"+seat,new WardrobeLoadout(),"youbeier",skin,"尤贝尔");for(int p=0;p<4;p++){v.players[p].character="youbeier";v.players[p].skin=skin;v.players[p].wardrobe=new WardrobeLoadout{headFrame="headframe_26fll"};}client.ShowValidationFixture(v);client.ValidationTableOnly=false;yield return Wait(8);effects.ValidationSequentialClock=false;effects.ValidationPlay(seat,"discard",v.players[seat].river[0].tile,false,1);effects.ValidationTime=.25f;yield return Wait(2);Check(UnityEngine.Object.FindObjectsByType<CharacterHandVisual>(FindObjectsInactive.Include).Length>0,"native special hand instantiated "+seat);yield return Shot("hand-"+seat);yield return Film("尤贝尔原版专属手型 · "+DressPreviewStates.SeatName(seat),1.5f,i=>effects.ValidationTime=(i%13)/20f);
   }
   foreach(string title in new[]{"四风连打","四家立直","四杠散了","九种九牌"}){
    var v=DressPreviewStates.Base("abort-r12-"+title,new WardrobeLoadout(),"yiji",400101,"旅人");v.phase="result";v.result=new ResultView{draw=true,abort=true,title=title,delta=new int[4],details=Array.Empty<WinDetail>()};client.ShowValidationFixture(v);var display=(ResultPresentation)Field(client,"resultDisplay");display.Observe(v);display.Freeze=true;display.FrozenAge=1;
    Check(display.ShowingDraw&&!display.ShowingScores,"abort title precedes scores: "+title);display.TickAuto(v);Check(display.ShowingDraw,"title not skipped before minimum duration: "+title);yield return Shot("abort-"+title);Check(display.DrawTitleRect.width>0,"visible title geometry: "+title);
    if(title=="四风连打")yield return Film("四风连打 · 先提示再进入结算",2,i=>display.FrozenAge=Mathf.Min(1.6f,i/20f));
    display.Advance(v);Check(!display.ShowingDraw&&display.ShowingScores,"abort advances to score page: "+title);if(title=="四风连打")yield return Film("四风连打 · 点数结算",1);
   }
  }
 }
}
