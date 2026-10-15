using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YunJian.Riichi.Editor
{
    // Render fixed public-state pictures only. No assertions, requests, APK or tests.
    [InitializeOnLoad] public static class SessionFlowPreview
    {
        const string Key="Riichi.SessionFlowPreview";
        static SessionFlowPreview(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        static void Changed(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindFirstObjectByType<RiichiClient>().gameObject.AddComponent<SessionFlowCapture>();}}
    }
    public sealed class SessionFlowCapture:MonoBehaviour
    {
        string output;RiichiClient client;LobbyController lobby;RoomView play,finalView;
        IEnumerator Start()
        {
            string custom=Environment.GetEnvironmentVariable("RIICHI_SESSION_PREVIEW_DIR");output=Path.GetFullPath(string.IsNullOrEmpty(custom)?"../build/session-flow-review/render":custom);Directory.CreateDirectory(output);client=GetComponent<RiichiClient>();lobby=client.Lobby;
            play=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.GetFullPath("../build/session-flow-review/play.json")));
            finalView=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.GetFullPath("../build/session-flow-review/final.json")));
            typeof(RiichiClient).GetField("nickname",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(client,"紫砂型焰梓二号");
            foreach(var size in new[]{new Vector2(1688,780),new Vector2(1280,720)}){
                yield return Resolution(size);lobby.DesignPreviewClock=10;client.DesignPreviewPendingSession(play);yield return Still("home-"+(int)size.x);
                lobby.ShowPendingSession();lobby.DesignPreviewClock+=1;yield return Still("resume-"+(int)size.x);
                lobby.ShowMediumNotice("牌局","当前无进行中的牌局");lobby.DesignPreviewClock+=1;yield return Still("no-match-"+(int)size.x);
                lobby.ShowSessionCancelled();lobby.DesignPreviewClock+=1;yield return Still("cancelled-"+(int)size.x);
                client.DesignPreviewSessionTable(play);lobby.DesignPreviewClock+=1;yield return new WaitForSecondsRealtime(2);yield return Still("ai-off-"+(int)size.x);
                play.players[play.you].auto=true;play.players[play.you].autoReason="manual";yield return Still("ai-on-"+(int)size.x);
                lobby.ShowMatchExit();lobby.DesignPreviewClock+=1;yield return Still("host-exit-"+(int)size.x);
                play.host=false;lobby.ShowMatchExit();lobby.DesignPreviewClock+=1;yield return Still("guest-exit-"+(int)size.x);play.host=true;play.players[play.you].auto=false;
                client.DesignPreviewSessionTable(finalView);lobby.DesignPreviewClock+=1;yield return null;
                var result=client.ValidationResult;result.Observe(finalView);result.Freeze=true;result.FrozenAge=100;result.Advance(finalView);result.FrozenAge=.99f;yield return Still("final-9-"+(int)size.x);
                result.FrozenAge=5;yield return Still("final-4-"+(int)size.x);result.FrozenAge=9;yield return Still("final-0-"+(int)size.x);
                finalView.players[finalView.you].ready=true;result.FrozenAge=5;yield return Still("final-waiting-"+(int)size.x);finalView.players[finalView.you].ready=false;
                var fourPath=Path.GetFullPath("../build/session-flow-review/final-four.json");
                if(File.Exists(fourPath)){
                    var four=JsonUtility.FromJson<RoomView>(File.ReadAllText(fourPath));client.DesignPreviewSessionTable(four);yield return null;
                    result.Observe(four);result.Freeze=true;result.FrozenAge=100;result.Advance(four);result.FrozenAge=2;yield return Still("final-four-7-"+(int)size.x);
                }
            }
            if(Environment.GetEnvironmentVariable("RIICHI_SESSION_STILLS_ONLY")=="1"){Debug.Log("SESSION UI STILLS COMPLETE");EditorApplication.Exit(0);yield break;}
            yield return Resolution(new Vector2(1280,592));client.DesignPreviewPendingSession(play);lobby.DesignPreviewClock=30;
            var pop=Path.Combine(output,"modal-animation");Directory.CreateDirectory(pop);
            for(int i=0;i<84;i++){lobby.DesignPreviewClock=30+i/60f;if(i==12)lobby.ShowPendingSession();if(i==54)lobby.CloseModal();yield return Frame(pop,i);}
            client.DesignPreviewSessionTable(play);lobby.DesignPreviewClock=40;client.DesignPreviewReentry();
            var entry=Path.Combine(output,"reentry");Directory.CreateDirectory(entry);
            for(int i=0;i<117;i++){client.ValidationIntro.FrozenAge=i/30f;yield return Frame(entry,i);}
            var movieFour=Path.GetFullPath("../build/session-flow-review/final-four.json");if(File.Exists(movieFour))finalView=JsonUtility.FromJson<RoomView>(File.ReadAllText(movieFour));
            client.DesignPreviewSessionTable(finalView);lobby.DesignPreviewClock=45;yield return null;
            var countdown=client.ValidationResult;countdown.Observe(finalView);countdown.Freeze=true;countdown.FrozenAge=100;countdown.Advance(finalView);countdown.FrozenAge=0;
            var count=Path.Combine(output,"countdown");Directory.CreateDirectory(count);
            for(int i=0;i<300;i++){countdown.FrozenAge=i/30f;yield return Frame(count,i);}
            Debug.Log("SESSION FLOW REVIEW RENDER COMPLETE: screenshots and videos only");EditorApplication.Exit(0);
        }
        static IEnumerator Resolution(Vector2 size){var win=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));win.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(win,new object[]{size,"Session flow review"});win.Repaint();for(int i=0;i<8;i++)yield return null;}
        IEnumerator Still(string name){yield return null;yield return null;yield return new WaitForEndOfFrame();Save(Path.Combine(output,name+".png"));}
        IEnumerator Frame(string dir,int i){yield return null;yield return new WaitForEndOfFrame();Save(Path.Combine(dir,"frame-"+i.ToString("D4")+".png"));}
        void Save(string path){var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(path,texture.EncodeToPNG());Destroy(texture);}
    }
}
