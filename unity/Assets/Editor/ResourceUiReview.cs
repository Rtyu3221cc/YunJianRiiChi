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
    [InitializeOnLoad] public static class ResourceUiReview {
        const string Key="Riichi.ResourceUiReview";
        static ResourceUiReview(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<ResourceUiReviewCapture>();}};}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Bootstrap(){if(!SessionState.GetBool(Key,false))return;CharacterPackService.RootOverride=SessionState.GetString(Key+"Root","");CharacterPackService.RespectPreviewAvailability=true;CharacterPackService.ResetForReview();}
        public static void Run(){AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);SessionState.SetString(Key+"Root",Path.GetFullPath("../build/resource-ui-r2/clients/"+Guid.NewGuid().ToString("N")));EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class ResourceUiReviewCapture:MonoBehaviour {
        [Serializable]class Report {public List<string>checks=new List<string>();public int expectedLoadErrors;public bool androidDeviceTested=false;}
        readonly Report report=new Report();readonly List<string> errors=new List<string>();RiichiClient client;LobbyController lobby;CharacterPackService service;CharacterResourceUi ui;CharacterPackShareServer source;bool expectingLoadFailure;string output;float clock=10;
        void Log(string value,string trace,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;if(expectingLoadFailure){report.expectedLoadErrors++;return;}errors.Add(value);}
        void Check(bool value,string message){if(!value)throw new Exception(message);report.checks.Add(message);Debug.Log("RESOURCE UI CHECK "+message);}
        IEnumerator Start(){
            output=Path.GetFullPath("../build/resource-ui-r2/render");Directory.CreateDirectory(output);Application.logMessageReceived+=Log;var work=new Stack<IEnumerator>();work.Push(Capture());
            while(work.Count>0){object next=null;try{var top=work.Peek();if(!top.MoveNext()){work.Pop();continue;}next=top.Current;if(next is IEnumerator nested){work.Push(nested);continue;}}catch(Exception ex){File.WriteAllText(Path.Combine(output,"failed.txt"),ex.ToString());Debug.LogException(ex);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            Check(errors.Count==0,"No unexpected Unity runtime errors");File.WriteAllText(Path.Combine(output,"checks.json"),JsonUtility.ToJson(report,true));Cleanup();Debug.Log("RESOURCE UI REVIEW PASS "+report.checks.Count);EditorApplication.Exit(0);
        }
        IEnumerator Hold(int n=5){clock+=n/30f;lobby.DesignPreviewClock=clock;for(int i=0;i<n;i++)yield return null;}
        IEnumerator Shot(string name){yield return Hold(3);yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());Destroy(texture);}
        IEnumerator Size(int w,int h){var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));view.GetType().GetMethod("SetCustomResolution",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,new object[]{new Vector2(w,h),"Resource UI review"});yield return Hold(8);}
        IEnumerator Until(Func<bool> ready,string label,float seconds=60){float deadline=Time.realtimeSinceStartup+seconds;while(!ready()){if(Time.realtimeSinceStartup>deadline)throw new Exception(label);yield return null;}}
        IEnumerator Ui(string id){ui.ReviewPress=id;yield return Hold(6);Check(ui.ReviewPress==null,"Visible resource control handled: "+id);}
        IEnumerator Downloaded(string key){yield return Until(()=>!service.Busy,"Download completion: "+key);Check(string.IsNullOrEmpty(service.Error),"Transfer succeeds: "+key);Check(CharacterPackService.Installed(key)&&CharacterPackService.Ready(key),"Successful load enables green marker: "+key);}
        void Toolbar(){var ids=new[]{"resources-all","resources-recommended","resources-custom","resources-filter","resources-delete-all"};Check(ids.All(i=>ui.Targets.ContainsKey(i)),"All toolbar buttons are visible");Check(ids.All(i=>Mathf.Abs(ui.Targets[i].width-ui.Targets[ids[0]].width)<.1f),"All five toolbar buttons have equal width");}
        IEnumerator Capture(){
            client=GetComponent<RiichiClient>();client.BeginLobbyValidation("resource-ui-review");lobby=client.Lobby;service=CharacterPackService.Instance;ui=CharacterResourceUi.Current;CharacterRoster.BeginPreview();CharacterPackService.MarkStartupSeen();AudioListener.pause=true;Application.targetFrameRate=30;
            CharacterRoster.Reorder(new[]{"fuji","yiji","erjietang","bamuwei","kawei","xiangyuanwu","huiyeji","qingluan","beijianshahezi","xiaoniaoyouchutian","wangcilang"});
            source=new CharacterPackShareServer{Enabled=true,ReviewChunkDelayMs=10};source.SetEntries(new[]{"fuji","kawei"}.Select(key=>{var p=CharacterPackService.Find(key);return new CharacterPackShareServer.Entry{key=key,hash=p.sha256,bytes=p.bytes,path=Path.GetFullPath("../build/character-resources-v1/windows/"+p.file)};}));
            service.OverrideUrl=key=>{var p=CharacterPackService.Find(key);return "http://127.0.0.1:"+source.Port+"/packs/"+p.sha256+"?ticket="+source.Ticket;};
            yield return Size(1750,800);lobby.DesignPreviewGardenEntry();yield return Hold(35);
            var font=Resources.Load<Font>("Majsoul/Lobby/Fonts/brush");Check("未下载部分下载热门角色".All(c=>font.HasCharacter(c)),"Original brush font covers the replacement and unavailable labels");
            Check(CharacterRoster.Ui("bound_emo").border.All(v=>v==3),"Original emoji frame uses three-pixel sliced borders");
            client.LobbyOpenSettings();var settings=(NativeSettingsUi)typeof(RiichiClient).GetField("nativeSettings",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(client);settings.Tab=2;yield return Hold();yield return Ui("resources-filter");client.LobbyCloseSettings();
            lobby.PreviewDorm("yiji");yield return Shot("dorm-unavailable-font");lobby.ValidationPress("dorm-character-fuji");yield return Hold();yield return Shot("fuji-download-confirm");yield return Ui("resource-prompt-download");
            yield return Until(()=>service.Progress>.08f&&service.Busy,"Progress window");Check(ui.ReviewFocusKey=="fuji"&&ui.ReviewScrollOffset>0,"Dorm download restores all filter and automatically focuses the target row");Check(!CharacterPackService.Ready("fuji"),"Downloading character has no completion marker");Toolbar();yield return Shot("standalone-focused-progress");yield return Downloaded("fuji");yield return Shot("standalone-loaded-check");
            Check(ui.Targets["resources-close"].width>175&&ui.Targets["resources-close"].height>50,"Standalone close button is enlarged");yield return Ui("resources-close");
            lobby.PreviewDorm("fuji",403807);yield return Shot("dorm-loaded-check");lobby.PreviewDorm("fuji",403807,"info");lobby.ReviewInfoScroll(495);yield return Shot("profile-thin-emoji-frames");
            client.LobbyOpenSettings();settings.Tab=2;yield return Hold();Toolbar();yield return Shot("settings-equal-buttons");ui.Ask("kawei",true);yield return Hold();yield return Ui("resource-prompt-download");
            Check(!ui.ModalOpen&&ui.ReviewFocusKey=="kawei","Download from settings stays in settings and focuses its row");yield return Shot("settings-focused-progress");yield return Downloaded("kawei");yield return Shot("settings-loaded-check");
            CharacterPackService.ResetForReview();Check(CharacterPackService.Installed("kawei")&&!CharacterPackService.Ready("kawei"),"Restored receipt waits for a successful resource load");
            yield return (IEnumerator)typeof(CharacterPackService).GetMethod("ValidateInstalled",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(service,null);
            Check(CharacterPackService.Ready("kawei")&&CharacterPackService.Ready("fuji"),"Startup verification reloads installed packs and restores their completion markers");
            CharacterRoster.Use("kawei",401004);CharacterRoster.Star("kawei");CharacterRoster.TogglePool("kawei",401004);yield return Ui("resource-delete-kawei");Check(!CharacterPackService.Ready("kawei")&&!CharacterPackService.Installed("kawei"),"Deleting resource removes its completion marker");Check(!CharacterRoster.Favorite("kawei")&&!CharacterRoster.Pool.Any(p=>p.character=="kawei")&&CharacterRoster.Active.character=="yiji","Deletion also clears character selection, favorite and random candidates");yield return Shot("settings-marker-after-delete");client.LobbyCloseSettings();
            var pack=CharacterPackService.Find("kawei");string file=CharacterPackService.FilePath(pack);Directory.CreateDirectory(Path.GetDirectoryName(file));using(var stream=new FileStream(file,FileMode.Create,FileAccess.Write))stream.SetLength(pack.bytes);File.WriteAllText(file+".ok",pack.sha256);
            Check(CharacterPackService.Installed("kawei")&&!CharacterPackService.Ready("kawei"),"A receipt alone never enables a completion marker");expectingLoadFailure=true;var bad=CharacterPackService.Asset<Texture2D>("Skins/401001/full");expectingLoadFailure=false;
            Check(bad==null&&!CharacterPackService.Installed("kawei")&&!CharacterPackService.Ready("kawei")&&!File.Exists(file+".ok"),"Load failure invalidates receipt and removes completion marker");Check(service.Results["kawei"].Contains("加载失败"),"Failed load offers a fresh download");ui.Open("kawei");yield return Shot("standalone-load-failure");yield return Size(1280,720);Toolbar();yield return Shot("standalone-16x9");ui.Close();
            using(var art=new CharacterIllustration())Check(art.Art(CharacterRoster.Find("kawei").skins.Single(s=>s.id==401004),0)==null,"A disappearing dynamic pack does not dereference missing skeleton data");
            File.WriteAllText(file+".ok",pack.sha256);expectingLoadFailure=true;var fallback=CharacterRoster.PresentationPortrait(new PlayerView{character="kawei",skin=401004});expectingLoadFailure=false;Check(fallback!=null&&!CharacterPackService.Ready("kawei"),"Failed portrait load falls back to the protected character within the same frame");
        }
        void Cleanup(){source?.Dispose();Application.logMessageReceived-=Log;CharacterPackService.ResetForReview();CharacterRoster.EndPreview();AudioListener.pause=false;}
    }
}
