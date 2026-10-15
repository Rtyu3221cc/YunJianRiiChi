using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace YunJian.Riichi.Editor
{
    // Drives the actual client against a disposable loopback room service.
    // Uses no test framework, assertions, phone state or player build.
    [InitializeOnLoad] public static class SessionFlowLiveReview
    {
        const string Key="Riichi.SessionFlowLiveReview";
        static SessionFlowLiveReview(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        static void Changed(PlayModeStateChange value){if(value==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindFirstObjectByType<RiichiClient>().gameObject.AddComponent<SessionFlowLiveCapture>();}}
    }
    public sealed class SessionFlowLiveCapture:MonoBehaviour
    {
        RiichiClient client;string output;bool timedOut;FieldInfo pollField;
        void Update(){if(client!=null)pollField?.SetValue(client,float.PositiveInfinity);}
        IEnumerator Start()
        {
            output=Path.GetFullPath("../build/session-flow-review/live");Directory.CreateDirectory(output);client=GetComponent<RiichiClient>();
            // Slow headless render frames can exceed the normal poll interval.
            // Drive each request explicitly so the operator never races a poll.
            pollField=typeof(RiichiClient).GetField("nextPoll",BindingFlags.Instance|BindingFlags.NonPublic);pollField.SetValue(client,float.PositiveInfinity);
            var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(1280,592),"Live session review"});window.Repaint();for(int i=0;i<8;i++)yield return null;
            client.BeginLobbyValidation("local-session-csharp-review");typeof(RiichiClient).GetField("savedSession",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(client,null);
            client.LobbyCreate(3,1,false,"friend");yield return Wait(()=>!client.Busy&&client.State?.phase=="lobby");if(timedOut)yield break;
            Debug.Log("SESSION LIVE: create n="+client.State.n+" rounds="+client.State.rounds);
            client.LobbyCommand("fillBots");yield return Wait(()=>!client.Busy&&client.State.players.Length==3);if(timedOut)yield break;
            client.LobbyCommand("start");yield return Wait(()=>client.State?.phase=="play"&&!client.Busy);if(timedOut)yield break;
            yield return new WaitForSecondsRealtime(4);yield return Shot("table-live");yield return Wait(()=>!client.Busy);if(timedOut)yield break;
            typeof(RiichiClient).GetMethod("EnterHome",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(client,null);client.Lobby.Home();
            // Rehearse an upgrade from releases that stored only address/token.
            var credential=(string)typeof(RiichiClient).GetField("token",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(client);
            var migrated=typeof(RiichiClient).GetMethod("LegacyConnection",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{client.LobbyEndpoint,credential});
            typeof(RiichiClient).GetField("savedSession",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(client,migrated);
            client.InspectSession("friend");yield return Wait(()=>!client.Busy&&client.Lobby.Modal=="session");if(timedOut)yield break;
            yield return new WaitForSecondsRealtime(.25f);yield return Shot("resume-live");Debug.Log("SESSION LIVE: pending "+client.PendingSessionDescription);
            client.ResumeSession();yield return Wait(()=>!client.Busy&&client.State?.phase=="play");if(timedOut)yield break;
            Debug.Log("SESSION LIVE: resumed intro="+client.IntroRemaining);yield return new WaitForSecondsRealtime(4);yield return Wait(()=>!client.Busy);if(timedOut)yield break;
            var request=new RequestBody{op="autoPlay",enabled=true};var routine=(IEnumerator)typeof(RiichiClient).GetMethod("Send",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(client,new object[]{request,null,false});yield return client.StartCoroutine(routine);
            yield return Wait(()=>client.AutoPlaying&&!client.Busy);if(timedOut)yield break;yield return Shot("auto-live");yield return Wait(()=>!client.Busy);if(timedOut)yield break;Debug.Log("SESSION LIVE: AI enabled");
            client.EndCurrentMatch();yield return Wait(()=>!client.Busy&&!client.LobbyActive);if(timedOut)yield break;
            Debug.Log("SESSION LIVE: ended and home");client.InspectSession();yield return Wait(()=>!client.Busy&&client.Lobby.Modal=="advanced-help");if(timedOut)yield break;yield return new WaitForSecondsRealtime(.25f);yield return Shot("no-match-live");
            client.Lobby.CloseModal();yield return new WaitForSecondsRealtime(.2f);client.LobbyCreate(4,2,true,"competition");yield return Wait(()=>!client.Busy&&client.State?.phase=="lobby");if(timedOut)yield break;
            Debug.Log("SESSION LIVE: recreated n="+client.State.n+" rounds="+client.State.rounds+" category="+client.State.lobby.category);
            client.EndCurrentMatch();yield return Wait(()=>!client.Busy&&!client.LobbyActive);if(timedOut)yield break;
            Debug.Log("SESSION LIVE FLOW COMPLETE");EditorApplication.Exit(0);
        }
        IEnumerator Wait(Func<bool> ready){float deadline=Time.realtimeSinceStartup+30;while(!ready()&&Time.realtimeSinceStartup<deadline)yield return null;if(!ready()){timedOut=true;Debug.LogError("SESSION LIVE stalled; phase="+client.State?.phase+" busy="+client.Busy+" modal="+client.Lobby.Modal);EditorApplication.Exit(1);}}
        IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),t.EncodeToPNG());Destroy(t);}
    }
}
