using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace YunJian.Riichi.Editor
{
    // Render-only animation review. Fixed UI clock, no assertions, room API,
    // unit tests, player build or publication operations.
    [InitializeOnLoad] public static class LobbyMotionPreview
    {
        const string Key="Riichi.LobbyMotionPreview";
        static LobbyMotionPreview(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        static void Changed(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){
                SessionState.SetBool(Key,false);UnityEngine.Object.FindFirstObjectByType<RiichiClient>().gameObject.AddComponent<LobbyMotionCapture>();
            }
        }
    }
    public sealed class LobbyMotionCapture : MonoBehaviour
    {
        const int Fps=60;
        string output;RiichiClient client;LobbyController lobby;
        IEnumerator Start()
        {
            bool buttons=Environment.GetEnvironmentVariable("RIICHI_GARDEN_BUTTON_PREVIEW")=="1";
            output=Path.GetFullPath(buttons?"../build/lobby-buttons-preview":"../build/lobby-motion-preview");Directory.CreateDirectory(output);client=GetComponent<RiichiClient>();lobby=client.Lobby;
            typeof(RiichiClient).GetField("nickname",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(client,"紫砂型焰梓二号");
            yield return Resolution(1280,592);lobby.DesignPreviewClock=10;
            string selected=Environment.GetEnvironmentVariable("RIICHI_MOTION_ROUTES");
            foreach(string route in new[]{"friend-in","friend-back","create-back","home-entry"})if(string.IsNullOrEmpty(selected)||Array.IndexOf(selected.Split(','),route)>=0)yield return Capture(route);
            if(buttons){
                foreach(var size in new[]{new Vector2(1688,780),new Vector2(1280,720)}){
                    yield return Resolution((int)size.x,(int)size.y);lobby.DesignPreviewClock+=5;lobby.DesignPreviewSafeArea=default;
                    lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Home);yield return Still("home-"+(int)size.x);
                    lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Modes);yield return Still("friend-"+(int)size.x);
                    lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Home);lobby.DesignPreviewSafeArea=new Rect(0,0,size.x*.945f,size.y);yield return Still("home-notch-right-"+(int)size.x);
                }
                lobby.DesignPreviewSafeArea=default;lobby.DesignPreviewGardenHelp();lobby.DesignPreviewClock+=1;yield return Still("help-popup");
                Debug.Log("LOBBY BUTTON PREVIEW SAVED: render only, no tests/builds");EditorApplication.Exit(0);yield break;
            }
            if(!string.IsNullOrEmpty(selected)){Debug.Log("LOBBY MOTION PREVIEW SAVED: selected render-only routes");EditorApplication.Exit(0);yield break;}
            foreach(var size in new[]{new Vector2(1688,780),new Vector2(1280,720)}){
                yield return Resolution((int)size.x,(int)size.y);lobby.DesignPreviewClock+=5;lobby.DesignPreviewSafeArea=default;
                lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Create);yield return null;lobby.ValidationPress("three");yield return null;yield return Still("create-"+(int)size.x);
                lobby.DesignPreviewSafeArea=new Rect(size.x*.055f,0,size.x*.945f,size.y);yield return Still("create-notch-left-"+(int)size.x);
                lobby.DesignPreviewSafeArea=new Rect(0,0,size.x*.945f,size.y);yield return Still("create-notch-right-"+(int)size.x);
            }
            Debug.Log("LOBBY MOTION PREVIEW SAVED: fixed-clock render only, no tests/builds");EditorApplication.Exit(0);
        }
        static IEnumerator Resolution(int width,int height)
        {
            var win=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));win.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(win,new object[]{new Vector2(width,height),"Lobby animation review"});win.Repaint();for(int i=0;i<8;i++)yield return null;
        }
        IEnumerator Capture(string route)
        {
            var dir=Path.Combine(output,route);Directory.CreateDirectory(dir);lobby.DesignPreviewSafeArea=default;
            lobby.DesignPreviewClock+=5;float start=lobby.DesignPreviewClock;
            var page=route=="friend-in"?LobbyController.ScreenPage.Home:route=="friend-back"?LobbyController.ScreenPage.Modes:LobbyController.ScreenPage.Create;
            lobby.OpenCategory("friend");lobby.DesignPreviewGardenPage(page);
            if(route=="home-entry")lobby.DesignPreviewGardenEntry();
            for(int i=0;i<144;i++){
                lobby.DesignPreviewClock=start+i/(float)Fps;
                if(i==24){if(route=="friend-in")lobby.OpenCategory("friend");else if(route!="home-entry")lobby.Back();}
                yield return null;yield return new WaitForEndOfFrame();
                var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(dir,"frame-"+i.ToString("D4")+".png"),texture.EncodeToPNG());Destroy(texture);
            }
            File.WriteAllText(Path.Combine(dir,"timing.json"),"{\"fps\":60,\"frames\":144,\"actionAt\":0.4}");
        }
        IEnumerator Still(string name){yield return null;yield return null;yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());Destroy(texture);}
    }
}
