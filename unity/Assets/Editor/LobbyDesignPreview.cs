using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace YunJian.Riichi.Editor
{
    // Render-only design preview. No room requests, assertions, tests or builds.
    [InitializeOnLoad] public static class LobbyDesignPreview
    {
        const string Key="Riichi.LobbyDesignPreview";
        static LobbyDesignPreview(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Key,false))return;
            SessionState.SetBool(Key,false);UnityEngine.Object.FindFirstObjectByType<RiichiClient>().gameObject.AddComponent<LobbyDesignPreviewCapture>();
        }
    }
    public sealed class LobbyDesignPreviewCapture : MonoBehaviour
    {
        IEnumerator Start()
        {
            var client=GetComponent<RiichiClient>();
            typeof(RiichiClient).GetField("nickname",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(client,"大厅验证");
            if(Environment.GetEnvironmentVariable("RIICHI_LOBBY_PREVIEW_PAGE")=="room"){
                yield return RoomPreview(client);Debug.Log("LOBBY ROOM PREVIEW SAVED: render only");EditorApplication.Exit(0);yield break;
            }
            if(Environment.GetEnvironmentVariable("RIICHI_LOBBY_PREVIEW_PAGE")=="profile"){
                yield return ProfilePreview(client);Debug.Log("LOBBY PROFILE PREVIEW SAVED: render only");EditorApplication.Exit(0);yield break;
            }
            if(Environment.GetEnvironmentVariable("RIICHI_LOBBY_PREVIEW_PAGE")=="garden"){
                yield return GardenPreview(client);Debug.Log("LOBBY GARDEN PREVIEW SAVED: render only");EditorApplication.Exit(0);yield break;
            }
            if(Environment.GetEnvironmentVariable("RIICHI_LOBBY_PREVIEW_PAGE")=="popups"||Environment.GetEnvironmentVariable("RIICHI_LOBBY_PREVIEW_PAGE")=="popups-motion"){
                yield return PopupsPreview(client,Environment.GetEnvironmentVariable("RIICHI_LOBBY_PREVIEW_PAGE")!="popups-motion");Debug.Log("LOBBY POPUPS PREVIEW SAVED: render only");EditorApplication.Exit(0);yield break;
            }
            if(Environment.GetEnvironmentVariable("RIICHI_LOBBY_PREVIEW_PAGE")=="advanced"){
                yield return AdvancedPreview(client);Debug.Log("LOBBY ADVANCED PREVIEW SAVED: render only");EditorApplication.Exit(0);yield break;
            }
            bool create=Environment.GetEnvironmentVariable("RIICHI_LOBBY_PREVIEW_PAGE")=="create";
            var dir=Path.GetFullPath(create?"../build/lobby-create-preview":"../build/lobby-detail-preview");Directory.CreateDirectory(dir);
            var win=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
            foreach(var size in new[]{new Vector2(1688,780),new Vector2(1280,720)}){
                win.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(win,new object[]{size,"Lobby detail preview"});win.Repaint();
                for(int i=0;i<8;i++)yield return null;
                if(create){
                    client.Lobby.OpenCreate();client.Lobby.ValidationScreen(LobbyController.ScreenPage.Create);yield return null;yield return null;yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(Path.Combine(dir,"create-default-"+(int)size.x+".png"));yield return new WaitForSecondsRealtime(.2f);
                    client.Lobby.DesignPreviewScrollBottom();yield return null;yield return null;yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(Path.Combine(dir,"create-lower-"+(int)size.x+".png"));yield return new WaitForSecondsRealtime(.2f);
                    continue;
                }
                client.Lobby.ValidationScreen(LobbyController.ScreenPage.Home);yield return null;yield return null;yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(dir,"home-"+(int)size.x+".png"));yield return new WaitForSecondsRealtime(.2f);
                client.Lobby.OpenCategory("friend");client.Lobby.ValidationScreen(LobbyController.ScreenPage.Modes);yield return null;yield return null;yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(dir,"friend-"+(int)size.x+".png"));yield return new WaitForSecondsRealtime(.2f);
            }
            Debug.Log("LOBBY DESIGN PREVIEW SAVED: render only");EditorApplication.Exit(0);
        }
        IEnumerator AdvancedPreview(RiichiClient client)
        {
            var lobby=client.Lobby;var output=Environment.GetEnvironmentVariable("RIICHI_LOBBY_PREVIEW_DIR");var dir=Path.GetFullPath(string.IsNullOrEmpty(output)?"../build/lobby-advanced-preview":output);Directory.CreateDirectory(dir);
            var win=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
            foreach(var size in new[]{new Vector2(1688,780),new Vector2(1280,720)}){
                win.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(win,new object[]{size,"Advanced setting style preview"});win.Repaint();for(int i=0;i<8;i++)yield return null;
                lobby.OpenCreate();lobby.ValidationScreen(LobbyController.ScreenPage.Create);yield return Screenshot(dir,"default-"+(int)size.x);
                lobby.DesignPreviewAdvancedTop();yield return Screenshot(dir,"upper-"+(int)size.x);
                lobby.DesignPreviewAdvancedLower();yield return Screenshot(dir,"lower-"+(int)size.x);
            }
            win.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(win,new object[]{new Vector2(1688,780),"Advanced click preview"});win.Repaint();for(int i=0;i<8;i++)yield return null;
            lobby.DesignPreviewInteractions=true;lobby.OpenCreate();lobby.ValidationScreen(LobbyController.ScreenPage.Create);lobby.DesignPreviewAdvancedTop();yield return Screenshot(dir,"numeric-fields");
            lobby.DesignPreviewOpenNumber();yield return new WaitForSecondsRealtime(.22f);yield return Screenshot(dir,"number-popup");lobby.DesignPreviewClose();yield return new WaitForSecondsRealtime(.22f);
            lobby.DesignPreviewAdvancedLower();yield return null;yield return null;lobby.DesignPreviewHelp();yield return new WaitForSecondsRealtime(.22f);yield return Screenshot(dir,"help-popup");lobby.DesignPreviewClose();yield return new WaitForSecondsRealtime(.22f);
            int frame=0;var frames=Path.Combine(dir,"frames");Directory.CreateDirectory(frames);
            for(int i=0;i<150;i++){
                if(i==10){lobby.DesignPreviewPulse("advanced-red-0");lobby.DesignPreviewChoose("red",0);}
                if(i==25){lobby.DesignPreviewPulse("advanced-red-1");lobby.DesignPreviewChoose("red",1);}
                if(i==42){lobby.DesignPreviewPulse("help-kuitan");lobby.DesignPreviewHelp();}
                if(i==65)lobby.DesignPreviewClose();
                if(i==78)lobby.DesignPreviewAdvancedTop();
                if(i==88){lobby.DesignPreviewPulse("number-initial");lobby.DesignPreviewOpenNumber();}
                if(i==120)lobby.DesignPreviewClose();
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(frames,"frame-"+(frame++).ToString("D4")+".png"));yield return new WaitForSecondsRealtime(.05f);
            }
            lobby.DesignPreviewInteractions=false;
        }
        IEnumerator GardenPreview(RiichiClient client)
        {
            var lobby=client.Lobby;var dir=Path.GetFullPath("../build/lobby-garden-review");Directory.CreateDirectory(dir);var win=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
            foreach(var size in new[]{new Vector2(1688,780),new Vector2(1280,720)}){
                win.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(win,new object[]{size,"Lobby garden refinement preview"});win.Repaint();for(int i=0;i<8;i++)yield return null;
                lobby.ValidationScreen(LobbyController.ScreenPage.Home);yield return Screenshot(dir,"home-"+(int)size.x);
                lobby.OpenCategory("friend");lobby.ValidationScreen(LobbyController.ScreenPage.Modes);yield return Screenshot(dir,"friend-"+(int)size.x);
                lobby.DesignPreviewJoin();yield return new WaitForSecondsRealtime(.22f);yield return Screenshot(dir,"join-"+(int)size.x);lobby.DesignPreviewClose();yield return new WaitForSecondsRealtime(.22f);
            }
        }
        RoomView RoomPicture(int n,int occupied=1,bool host=true)
        {
            var state=new RoomView{phase="lobby",pin="37958",n=n,rounds=2,you=host?0:1,host=host,lobby=new LobbyRoomView{category="friend",managed=true,red=true},players=new PlayerView[occupied]};
            for(int i=0;i<occupied;i++)state.players[i]=new PlayerView{name=i==0?"紫砂型焰梓二号":i==1?"牌友":("电脑"+i),bot=i>1,ready=true,online=true,score=n==3?35000:25000,wind=i};
            return state;
        }
        IEnumerator RoomPreview(RiichiClient client)
        {
            var lobby=client.Lobby;var output=Environment.GetEnvironmentVariable("RIICHI_LOBBY_PREVIEW_DIR");var dir=Path.GetFullPath(string.IsNullOrEmpty(output)?"../build/lobby-room-review":output);Directory.CreateDirectory(dir);var win=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
            foreach(var size in new[]{new Vector2(1688,780),new Vector2(1280,720)}){
                win.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(win,new object[]{size,"Ready room appearance preview"});win.Repaint();for(int i=0;i<8;i++)yield return null;
                foreach(int n in new[]{3,4}){
                    client.ShowValidationFixture(RoomPicture(n));lobby.ValidationScreen(LobbyController.ScreenPage.Room);lobby.DesignPreviewRoomScroll(false);yield return Screenshot(dir,"room-"+n+"-"+(int)size.x);
                    lobby.DesignPreviewRoomScroll(true);yield return Screenshot(dir,"room-"+n+"-lower-"+(int)size.x);
                }
                client.ShowValidationFixture(RoomPicture(4,4));lobby.ValidationScreen(LobbyController.ScreenPage.Room);lobby.DesignPreviewRoomScroll(false);yield return Screenshot(dir,"room-ready-"+(int)size.x);
                var bot=RoomPicture(3,2);bot.players[1].bot=true;client.ShowValidationFixture(bot);lobby.ValidationScreen(LobbyController.ScreenPage.Room);yield return Screenshot(dir,"room-bot-"+(int)size.x);
            }
            win.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(win,new object[]{new Vector2(1688,780),"Room confirmation appearance preview"});win.Repaint();for(int i=0;i<8;i++)yield return null;
            client.ShowValidationFixture(RoomPicture(3));lobby.ValidationScreen(LobbyController.ScreenPage.Room);lobby.OpenModal("leave");yield return new WaitForSecondsRealtime(.22f);yield return Screenshot(dir,"host-leave");lobby.DesignPreviewClose();yield return new WaitForSecondsRealtime(.22f);
            client.ShowValidationFixture(RoomPicture(3,2,false));lobby.ValidationScreen(LobbyController.ScreenPage.Room);lobby.OpenModal("leave");yield return new WaitForSecondsRealtime(.22f);yield return Screenshot(dir,"guest-leave");lobby.DesignPreviewClose();yield return new WaitForSecondsRealtime(.22f);
            client.ShowValidationFixture(RoomPicture(3));lobby.ValidationScreen(LobbyController.ScreenPage.Room);lobby.OpenModal("invite");yield return new WaitForSecondsRealtime(.22f);yield return Screenshot(dir,"invite");lobby.DesignPreviewClose();yield return new WaitForSecondsRealtime(.22f);
            foreach(int n in new[]{3,4}){
                client.ShowValidationFixture(RoomPicture(n));lobby.ValidationScreen(LobbyController.ScreenPage.Room);yield return null;yield return null;
                lobby.ValidationPress("rules");yield return new WaitForSecondsRealtime(.22f);yield return Screenshot(dir,"room-rules-"+n);lobby.DesignPreviewClose();yield return new WaitForSecondsRealtime(.22f);
            }
            lobby.ValidationScreen(LobbyController.ScreenPage.Room);yield return null;yield return null;lobby.ValidationPress("yaku");yield return new WaitForSecondsRealtime(.22f);yield return Screenshot(dir,"room-yaku");lobby.DesignPreviewClose();yield return new WaitForSecondsRealtime(.22f);
            typeof(RiichiClient).GetMethod("EnterHome",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(client,null);yield return null;yield return null;
            lobby.OpenCreate();lobby.ValidationScreen(LobbyController.ScreenPage.Create);yield return Screenshot(dir,"create-header");
            lobby.DesignPreviewInteractions=true;lobby.DesignPreviewHelp();yield return new WaitForSecondsRealtime(.22f);yield return Screenshot(dir,"food-help");lobby.DesignPreviewClose();yield return new WaitForSecondsRealtime(.22f);
            lobby.DesignPreviewOpenNumber();yield return new WaitForSecondsRealtime(.22f);yield return Screenshot(dir,"number-popup");lobby.DesignPreviewClose();yield return new WaitForSecondsRealtime(.22f);
            lobby.DesignPreviewRules(0);yield return new WaitForSecondsRealtime(.22f);yield return Screenshot(dir,"create-rules");lobby.DesignPreviewClose();yield return new WaitForSecondsRealtime(.22f);
            lobby.OpenCategory("friend");lobby.ValidationScreen(LobbyController.ScreenPage.Modes);lobby.DesignPreviewJoin();yield return new WaitForSecondsRealtime(.22f);yield return Screenshot(dir,"join-popup");lobby.DesignPreviewClose();yield return new WaitForSecondsRealtime(.22f);
            lobby.DesignPreviewFriendHelp(0);yield return new WaitForSecondsRealtime(.22f);yield return Screenshot(dir,"friend-help");lobby.DesignPreviewClose();yield return new WaitForSecondsRealtime(.22f);
            lobby.DesignPreviewInteractions=false;
        }
        IEnumerator ProfilePreview(RiichiClient client)
        {
            var lobby=client.Lobby;var dir=Path.GetFullPath("../build/lobby-profile-review");Directory.CreateDirectory(dir);var win=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
            foreach(var size in new[]{new Vector2(1688,780),new Vector2(1280,720)}){
                win.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(win,new object[]{size,"Profile typography preview"});win.Repaint();for(int i=0;i<8;i++)yield return null;
                lobby.ValidationScreen(LobbyController.ScreenPage.Home);yield return Screenshot(dir,"short-name-"+(int)size.x);
                typeof(RiichiClient).GetField("nickname",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(client,"紫砂型焰梓二号");yield return Screenshot(dir,"reference-name-"+(int)size.x);
                typeof(RiichiClient).GetField("nickname",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(client,"大厅验证");
            }
        }
        IEnumerator PopupsPreview(RiichiClient client,bool captureViews=true)
        {
            var lobby=client.Lobby;var dir=Path.GetFullPath("../build/lobby-popups-preview");Directory.CreateDirectory(dir);
            var win=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
            if(captureViews)foreach(var size in new[]{new Vector2(1688,780),new Vector2(1280,720)}){
                win.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(win,new object[]{size,"Lobby popups review"});win.Repaint();for(int i=0;i<8;i++)yield return null;
                lobby.OpenCategory("friend");lobby.ValidationScreen(LobbyController.ScreenPage.Modes);yield return Screenshot(dir,"friend-"+(int)size.x);
                lobby.DesignPreviewFriendHelp(0);yield return new WaitForSecondsRealtime(.22f);yield return Screenshot(dir,"create-help-"+(int)size.x);lobby.DesignPreviewClose();yield return new WaitForSecondsRealtime(.22f);
                lobby.DesignPreviewFriendHelp(1);yield return new WaitForSecondsRealtime(.22f);yield return Screenshot(dir,"join-help-"+(int)size.x);lobby.DesignPreviewClose();yield return new WaitForSecondsRealtime(.22f);
                lobby.DesignPreviewJoin();yield return new WaitForSecondsRealtime(.22f);yield return Screenshot(dir,"join-"+(int)size.x);
                lobby.ValidationNumber("123456");yield return Screenshot(dir,"join-number-"+(int)size.x);lobby.DesignPreviewClose();yield return new WaitForSecondsRealtime(.22f);
                lobby.OpenCreate();lobby.ValidationScreen(LobbyController.ScreenPage.Create);
                foreach(int mode in new[]{0,1})foreach(int tab in new[]{0,1}){
                    lobby.DesignPreviewRules(mode,tab);yield return new WaitForSecondsRealtime(.22f);yield return Screenshot(dir,"rules-"+(mode==0?"four":"three")+"-"+(tab==0?"intro":"detail")+"-"+(int)size.x);
                }
                lobby.DesignPreviewClose();yield return new WaitForSecondsRealtime(.22f);
            }
            win.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(win,new object[]{new Vector2(1688,780),"Lobby popups click preview"});win.Repaint();for(int i=0;i<8;i++)yield return null;
            lobby.OpenCategory("friend");lobby.ValidationScreen(LobbyController.ScreenPage.Modes);var frames=Path.Combine(dir,"frames");Directory.CreateDirectory(frames);
            for(int i=0;i<250;i++){
                if(i==8){lobby.DesignPreviewPulse("help0");lobby.DesignPreviewFriendHelp(0);}
                if(i==26||i==58||i==114||i==232)lobby.DesignPreviewClose();
                if(i==36){lobby.DesignPreviewPulse("help1");lobby.DesignPreviewFriendHelp(1);}
                if(i==68){lobby.DesignPreviewPulse("mode1");lobby.DesignPreviewJoin();}
                if(i==85||i==92||i==99||i==106){int key=i==85?0:i==92?1:i==99?2:9;lobby.DesignPreviewPulse("digit"+key);lobby.ValidationPress("digit"+key);}
                if(i==126){lobby.OpenCreate();lobby.ValidationScreen(LobbyController.ScreenPage.Create);lobby.DesignPreviewRules();}
                if(i==146){lobby.DesignPreviewPulse("ruleTab1");lobby.DesignPreviewRules(0,1);}
                if(i==166){lobby.DesignPreviewPulse("ruleMode1");lobby.DesignPreviewRules(1,1);}
                if(i==188){lobby.DesignPreviewPulse("ruleTab0");lobby.DesignPreviewRules(1,0);}
                if(i==210){lobby.DesignPreviewPulse("ruleTab1");lobby.DesignPreviewRules(1,1);}
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(frames,"frame-"+i.ToString("D4")+".png"));yield return new WaitForSecondsRealtime(.05f);
            }
        }
        static IEnumerator Screenshot(string dir,string name){yield return null;yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(dir,name+".png"));yield return new WaitForSecondsRealtime(.15f);}
    }
}
