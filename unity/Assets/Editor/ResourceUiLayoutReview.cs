using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace YunJian.Riichi.Editor {
    [InitializeOnLoad] public static class ResourceUiLayoutReview {
        const string Key="Riichi.ResourceUiLayoutReview";
        static ResourceUiLayoutReview(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<ResourceUiLayoutCapture>();}};}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Bootstrap(){if(!SessionState.GetBool(Key,false))return;CharacterPackService.RootOverride=SessionState.GetString(Key+"Root","");CharacterPackService.RespectPreviewAvailability=true;CharacterPackService.ResetForReview();}
        public static void Run(){AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);SessionState.SetString(Key+"Root",Path.GetFullPath("../build/resource-ui-r3/clients/"+Guid.NewGuid().ToString("N")));EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class ResourceUiLayoutCapture:MonoBehaviour {
        [Serializable]class Report {public List<string>checks=new List<string>();public bool androidDeviceTested=false;}
        readonly Report report=new Report();readonly List<string> errors=new List<string>();RiichiClient client;LobbyController lobby;CharacterResourceUi ui;string output;float clock=10;
        void Log(string value,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception)errors.Add(value);}
        void Check(bool value,string message){if(!value)throw new Exception(message);report.checks.Add(message);Debug.Log("RESOURCE LAYOUT CHECK "+message);}
        IEnumerator Start(){
            output=Path.GetFullPath("../build/resource-ui-r3/render");Directory.CreateDirectory(output);Application.logMessageReceived+=Log;var work=new Stack<IEnumerator>();work.Push(Capture());
            while(work.Count>0){object next=null;try{var top=work.Peek();if(!top.MoveNext()){work.Pop();continue;}next=top.Current;if(next is IEnumerator nested){work.Push(nested);continue;}}catch(Exception ex){File.WriteAllText(Path.Combine(output,"failed.txt"),ex.ToString());Debug.LogException(ex);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            Check(errors.Count==0,"No Unity runtime errors");File.WriteAllText(Path.Combine(output,"checks.json"),JsonUtility.ToJson(report,true));Cleanup();Debug.Log("RESOURCE LAYOUT REVIEW PASS "+report.checks.Count);EditorApplication.Exit(0);
        }
        IEnumerator Hold(int n=5){clock+=n/30f;lobby.DesignPreviewClock=clock;for(int i=0;i<n;i++)yield return null;}
        IEnumerator Size(int w,int h){var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));view.GetType().GetMethod("SetCustomResolution",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,new object[]{new Vector2(w,h),"Resource layout correction"});yield return Hold(8);}
        IEnumerator Shot(string name){yield return Hold(3);yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());Destroy(image);}
        void InstallFixture(string key){var p=CharacterPackService.Find(key);string source=Path.GetFullPath("../build/character-resources-v1/windows/"+p.file);Check(new FileInfo(source).Length==p.bytes&&CharacterPackService.HashFile(source)==p.sha256,"Verified isolated fixture: "+key);string target=CharacterPackService.FilePath(p);Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(source,target,true);File.WriteAllText(target+".ok",p.sha256);Check(CharacterPackService.Asset<Texture2D>("Skins/"+p.skins[0]+"/bighead")!=null&&CharacterPackService.Ready(key),"Fixture is really loaded: "+key);}
        IEnumerator Capture(){
            client=GetComponent<RiichiClient>();client.BeginLobbyValidation("resource-layout-review");lobby=client.Lobby;ui=CharacterResourceUi.Current;CharacterRoster.BeginPreview();CharacterPackService.MarkStartupSeen();AudioListener.pause=true;Application.targetFrameRate=30;
            InstallFixture("fuji");InstallFixture("kawei");CharacterRoster.Reorder(new[]{"fuji","yiji","erjietang","kawei","bamuwei","xiangyuanwu","huiyeji","qingluan","beijianshahezi","xiaoniaoyouchutian","wangcilang"});
            yield return Size(1750,800);lobby.DesignPreviewGardenEntry();yield return Hold(30);lobby.PreviewDorm("fuji",403807);yield return Shot("dorm-no-checks");
            ui.Open("fuji");yield return Hold();Check(ui.ReviewFocusKey=="fuji"&&ui.ReviewScrollOffset>0,"Target character remains automatically located");Check(ui.ReviewListBounds.height>600,"Standalone list uses the lower area and fits five full rows");yield return Shot("standalone-highlight");
            ui.ReviewPress="resources-close";yield return Hold();Check(ui.ReviewFocusKey==null&&!ui.ModalOpen,"Closing standalone view clears its outline");ui.Open();yield return Hold();Check(ui.ReviewFocusKey==null,"Generic reopen does not restore the previous outline");yield return Shot("standalone-reopened");ui.Close();
            client.LobbyOpenSettings();var settings=(NativeSettingsUi)typeof(RiichiClient).GetField("nativeSettings",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(client);settings.Tab=2;ui.ReviewFocus("kawei");yield return Hold(8);Check(ui.ReviewFocusKey=="kawei","Embedded resource view can show the current target outline");Check(ui.ReviewListBounds.height>=489,"Settings fits four full character rows without reducing portraits or text");yield return Shot("settings-highlight");
            client.LobbyCloseSettings();Check(ui.ReviewFocusKey==null,"Leaving settings clears its resource outline");client.LobbyOpenSettings();settings.Tab=2;yield return Hold(8);Check(ui.ReviewFocusKey==null,"Reopening settings has no stale outline");yield return Shot("settings-reopened");client.LobbyCloseSettings();
            yield return Size(1280,720);ui.Open("kawei");yield return Shot("standalone-16x9-highlight");Check(ui.ReviewListBounds.height>600,"Expanded list remains available at 16 by 9");ui.Close();ui.Open();yield return Shot("standalone-16x9-reopened");Check(ui.ReviewFocusKey==null,"Reopened 16 by 9 view also clears the outline");ui.Close();
        }
        void Cleanup(){Application.logMessageReceived-=Log;CharacterPackService.ResetForReview();CharacterRoster.EndPreview();AudioListener.pause=false;}
    }
}
