using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;
namespace YunJian.Riichi.Editor {
    [InitializeOnLoad] public static class SocialPreferencesReview {
        const string Key="SocialPreferencesReview";
        static SocialPreferencesReview(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<SocialPreferencesCapture>();}};}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Bootstrap(){if(!SessionState.GetBool(Key,false))return;CharacterPackService.RootOverride=Path.GetFullPath("../build/portrait-fix-068/installed");CharacterPackService.RespectPreviewAvailability=true;CharacterPackService.ResetForReview();}
        public static void Run(){TableSocialAssetImport.Configure();ProjectSetup.Configure();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class SocialPreferencesCapture:MonoBehaviour {
        [Serializable] sealed class Word {public string kind;public int direction;public float width,height,ratio;}
        [Serializable] sealed class Report {public bool androidDeviceTested;public List<string> checks=new List<string>();public List<Word> winWords=new List<Word>();}
        readonly Report report=new Report();readonly List<string> errors=new List<string>();
        RiichiClient client;LobbyController lobby;NativeSettingsUi settings;string output;RoomReply reply;float clock=20;
        bool originalLive,originalBots;float originalInset;
        void Log(string value,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error)errors.Add(value);}
        void Check(bool ok,string message){if(!ok)throw new Exception(message);report.checks.Add(message);Debug.Log("SOCIAL PREVIEW CHECK "+message);}
        IEnumerator Start(){
            output=Path.GetFullPath("../build/social-preview-1022/render");Directory.CreateDirectory(output);if(File.Exists(Path.Combine(output,"failed.txt")))File.Delete(Path.Combine(output,"failed.txt"));Application.logMessageReceived+=Log;
            originalLive=AppPreferences.DynamicOutfits;originalBots=AppPreferences.RandomBotAppearance;originalInset=AppPreferences.ScreenInset;CharacterRoster.BeginPreview();
            client=GetComponent<RiichiClient>();client.BeginLobbyValidation("social-preview-1022");lobby=client.Lobby;settings=(NativeSettingsUi)typeof(RiichiClient).GetField("nativeSettings",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(client);GetComponent<ResourceRoomCoordinator>().enabled=false;AudioListener.pause=true;Application.targetFrameRate=30;
            var stack=new Stack<IEnumerator>();stack.Push(Capture());
            while(stack.Count>0){object next;try{var current=stack.Peek();if(!current.MoveNext()){stack.Pop();continue;}next=current.Current;if(next is IEnumerator child){stack.Push(child);continue;}}catch(Exception ex){File.WriteAllText(Path.Combine(output,"failed.txt"),ex.ToString());Debug.LogException(ex);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            Check(errors.Count==0,"No Unity errors while rendering and sending emotes");File.WriteAllText(Path.Combine(output,"checks.json"),JsonUtility.ToJson(report,true));Cleanup();Debug.Log("SOCIAL PREFERENCES PREVIEW PASS "+report.checks.Count);EditorApplication.Exit(0);
        }
        void Cleanup(){AppPreferences.SetDynamicOutfits(originalLive);AppPreferences.SetRandomBotAppearance(originalBots);AppPreferences.SetInset(originalInset);CharacterRoster.EndPreview();Application.logMessageReceived-=Log;AudioListener.pause=false;}
        IEnumerator Hold(int frames=6){clock+=frames/30f;lobby.DesignPreviewClock=clock;for(int i=0;i<frames;i++)yield return null;}
        IEnumerator Until(Func<bool> ready,string label){float limit=Time.realtimeSinceStartup+75;while(!ready()){if(Time.realtimeSinceStartup>limit)throw new Exception("Timeout: "+label);yield return null;}}
        IEnumerator Size(int width,int height){var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(width,height),"Emotes and preferences preview"});yield return Hold(8);}
        IEnumerator Shot(string name){yield return Hold(3);yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());Destroy(image);}
        IEnumerator PressSetting(string id){settings.ReviewPress=id;yield return Hold(3);Check(settings.ReviewPress==null,"Preference control responds: "+id);}
        IEnumerator Raw(RequestBody command){
            command.id=Guid.NewGuid().ToString("N");using(var web=new UnityWebRequest("http://127.0.0.1:8765/api","POST")){web.uploadHandler=new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(command)));web.downloadHandler=new DownloadHandlerBuffer();web.SetRequestHeader("Content-Type","application/json");web.timeout=10;yield return web.SendWebRequest();Check(web.result==UnityWebRequest.Result.Success,"Local room endpoint replies: "+command.op);reply=RoomReply.Parse(web.downloadHandler.text);Check(reply?.ok==true,"Room accepts: "+command.op+" "+reply?.error);}
        }
        string HostToken=>(string)typeof(RiichiClient).GetField("token",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(client);
        void UseState(RoomView state,string token=null){client.DesignPreviewSessionTable(state);client.ValidationIntro.Reset();if(token!=null)typeof(RiichiClient).GetField("token",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(client,token);typeof(RiichiClient).GetField("resourceSelectionRevision",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(client,CharacterPackService.Revision);typeof(RiichiClient).GetField("resourceSelectionDirty",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(client,false);}
        IEnumerator Capture(){
            AppPreferences.SetInset(0);AppPreferences.SetDynamicOutfits(true);AppPreferences.SetRandomBotAppearance(false);yield return Size(1688,768);lobby.DesignPreviewGardenEntry();yield return Hold(30);
            Check(RoomReply.Parse("{\"ok\":true,\"state\":{\"resourceSetup\":null}}").state.resourceSetup==null,"JSON null preparation is normalized so table controls remain enabled");Check(RoomReply.Parse("{\"ok\":true,\"state\":{\"resourceSetup\":{\"revision\":3}}}").state.resourceSetup.revision==3,"A real resource preparation keeps its positive revision");
            Check("偏好电脑形象动态服饰".All(c=>Resources.Load<Font>("Majsoul/Lobby/Fonts/brush").HasCharacter(c)),"Native UI font contains all new tab and preference labels");
            foreach(string kind in new[]{"ron","tsumo"}){var texture=NativeAnnouncements.WinTexture(kind);Check(texture.width==315&&texture.height==(kind=="ron"?130:128),kind+" retains the original PNG dimensions");
                foreach(int n in new[]{3,4}){var first=NativeAnnouncements.WinRect(kind,0,n,1280,600,.3f);for(int i=0;i<n;i++){var rect=NativeAnnouncements.WinRect(kind,i,n,1280,600,.3f);Check(Mathf.Abs(rect.width/rect.height-(float)texture.width/texture.height)<.0001f,kind+" original aspect, "+n+" players, direction "+i);Check(Mathf.Abs(rect.width-first.width)<.001f&&Mathf.Abs(rect.height-first.height)<.001f,kind+" equal size, direction "+i);}}
            }
            client.LobbyOpenSettings();yield return Hold(8);yield return PressSetting("settings-tab-3");yield return Shot("preferences-default");
            yield return PressSetting("preference-live-off");Check(!AppPreferences.DynamicOutfits,"Dynamic outfit switch disables animation globally");yield return Shot("preferences-live-off");
            using(var illustration=new CharacterIllustration()){var skin=CharacterRoster.Find("yiji").skins.Single(s=>s.id==400107);var still=illustration.Art(skin,0);Check(still is Texture2D,"Live OFF returns the original still image");yield return PressSetting("preference-live-on");yield return Until(()=>illustration.Art(skin,Time.unscaledTime) is RenderTexture,"live ON skeleton");Check(illustration.Art(skin,Time.unscaledTime) is RenderTexture,"Live ON restores the original skeleton animation");}
            yield return PressSetting("preference-bot-dropdown");yield return Shot("preferences-bot-options");yield return PressSetting("preference-bot-random");Check(AppPreferences.RandomBotAppearance,"Downloaded-character random bot appearance is selectable");yield return Shot("preferences-random-on");client.LobbyCloseSettings();
            CharacterRoster.Use("kawei",401004);client.LobbyCreate(4,1,true,"friend");yield return Until(()=>!client.Busy&&client.State?.phase=="lobby","create bot preview room");
            for(int i=0;i<3;i++){client.LobbyCommand("addBot");yield return Until(()=>!client.Busy,"add random bot");}
            Check(client.State.players.Skip(1).All(p=>CharacterRoster.Available(p.character)&&CharacterPackService.ContainsSkin(p.character,p.skin)),"Added bots only use locally installed characters and valid outfits");Check(client.State.players.Skip(1).Select(p=>p.character).Distinct().Count()==3,"Random bot portraits are varied when enough characters are installed");lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Room);yield return Hold(20);yield return Shot("room-random-bots");
            yield return Raw(new RequestBody{op="close",token=HostToken,version=client.State.version});client.BeginLobbyValidation("social-preview-1022");
            yield return Raw(new RequestBody{op="create",host="social-preview-1022",name="旅人",n=4,rounds=1,character="kawei",skin=401004,resourceProtocol=0});string host=reply.token,pin=reply.state.pin;var peers=new List<string>();
            foreach(var identity in new[]{new CharacterChoice("yiji",400101),new CharacterChoice("weilai",406903),new CharacterChoice("fuji",403805)}){yield return Raw(new RequestBody{op="join",pin=pin,name=CharacterRoster.Find(identity.character).name,character=identity.character,skin=identity.skin,resourceProtocol=0});peers.Add(reply.token);}
            yield return Raw(new RequestBody{op="start",token=host,version=reply.state.version,resourceProtocol=0});UseState(reply.state,host);yield return Hold(35);
            yield return Shot("table-before-picker");var emotes=client.ValidationEmotes;emotes.ReviewPress="emote-open";yield return Hold(5);Check(emotes.IsOpen&&emotes.BlocksTableInput,"Native emoji tab opens and blocks accidental tile taps (phase="+client.State.phase+", modal="+lobby.Modal+", intro="+client.ValidationIntro.Active+", press="+emotes.ReviewPress+", targets="+emotes.ReviewTargets.Count+", gui="+emotes.ReviewGuiEnabled+", resource="+(client.State.resourceSetup==null?"null":client.State.resourceSetup.revision.ToString())+", resourceUi="+CharacterResourceUi.Current.ModalOpen+", page="+typeof(RiichiClient).GetField("page",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(client)+")");
            yield return Until(()=>Enumerable.Range(0,9).All(i=>CharacterRoster.DisplayTexture("Emoji/kawei/"+i)!=null),"first nine own-character emojis");yield return Shot("emote-picker-ready");
            Check(emotes.ReviewTargets.Keys.Where(k=>k.StartsWith("emote-send-")).Count()>=9,"Picker exposes the current character's first nine emojis");emotes.ReviewPress="emote-send-0";yield return Until(()=>!client.Busy&&client.State.emoteSeq>0,"actual emote send through HTTP");Check(client.State.emotes.Last().character=="kawei","Emote sent through the actual UI keeps its seat character");
            for(int i=0;i<peers.Count;i++)yield return Raw(new RequestBody{op="emote",token=peers[i],emoji=i+1,version=-1});yield return Raw(new RequestBody{op="state",token=host});UseState(reply.state,host);
            yield return Until(()=>client.State.emotes.All(e=>CharacterRoster.DisplayTexture("Emoji/"+e.character+"/"+e.emoji)!=null),"four distinct public character emoji images");emotes.ReviewPress="emote-open";yield return Hold(5);yield return Shot("emotes-four-directions-cooldown");
            Check(emotes.ReviewBubbleCharacters.Count==4&&emotes.ReviewBubbleCharacters.Values.Distinct().Count()==4,"Four player directions display four character-specific bubble images");Check(emotes.Cooldown>0&&emotes.ReviewGrayTextures>0,"Cooldown picker uses actual grayscale render textures");int seq=client.State.emoteSeq;emotes.ReviewPress="emote-send-1";yield return Hold(2);Check(emotes.ReviewPress!=null&&client.State.emoteSeq==seq,"Cooldown cell cannot submit another emote");emotes.ReviewPress=null;
            int seen=emotes.ReviewSeenSequence;emotes.Observe(client.State);Check(emotes.ReviewSeenSequence==seen,"Repeated polling does not replay an emoji sequence");
            emotes.Close();yield return Shot("emotes-four-directions");emotes.ReviewPress="emote-open";yield return Hold(3);yield return Until(()=>emotes.Cooldown<=0,"emoji cooldown expiry");yield return Shot("emote-picker-after-cooldown");emotes.Close();
            var effects=GetComponent<TableEffects>();foreach(string kind in new[]{"ron","tsumo"})for(int seat=0;seat<4;seat++){effects.Reset();effects.ValidationFreeze=true;effects.ValidationTime=.3f;typeof(TableEffects).GetMethod("BeginCall",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(effects,new object[]{new PublicSound{seat=seat,kind=kind}});yield return Shot(kind+"-direction-"+seat);var viewport=TableViewport.Fit(Screen.width,Screen.height,Screen.safeArea,true);var rect=NativeAnnouncements.WinRect(kind,seat,4,viewport.Width,viewport.Height,.3f);report.winWords.Add(new Word{kind=kind,direction=seat,width=rect.width,height=rect.height,ratio=rect.width/rect.height});}effects.Reset();
            yield return Size(1280,720);client.LobbyOpenSettings();yield return Hold(8);yield return PressSetting("settings-tab-3");yield return Shot("preferences-16x9");client.LobbyCloseSettings();
            Check(Shader.Find("YunJian/EmoteGrayscale")!=null,"Grayscale shader is available for future player builds");emotes.ReviewPress="emote-open";yield return Hold(3);Check(emotes.IsOpen,"Emoji panel can reopen after a settings round trip");emotes.Observe(new RoomView{phase="result",audioRoom=client.State.audioRoom});Check(!emotes.IsOpen&&!emotes.BlocksTableInput,"Settlement closes the picker and releases its table input guard");
        }
    }
}
