using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    [InitializeOnLoad] public static class Review1017bPreview {
        const string Key="Riichi.Review1017bPreview";
        static Review1017bPreview(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<Review1017bCapture>();}};}
        public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class Review1017bCapture:MonoBehaviour {
        string output;RiichiClient client;LobbyController lobby;
        static readonly BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        IEnumerator Start(){
            var routine=Capture();while(true){bool next;try{next=routine.MoveNext();}catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);yield break;}if(!next)break;yield return routine.Current;}
            Debug.Log("REVIEW 1017B VISUAL REHEARSAL COMPLETE; no unit tests, APK or Git");EditorApplication.Exit(0);
        }
        IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());Destroy(image);}
        IEnumerator Size(int w,int h){var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));view.GetType().GetMethod("SetCustomResolution",Private).Invoke(view,new object[]{new Vector2(w,h),"Yakuman and waits visual review"});for(int i=0;i<6;i++)yield return null;}
        RoomView Load(string name)=>JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/review-1017b/fixtures/"+name+".json"));
        IEnumerator Capture(){
            output=Path.GetFullPath("../build/review-1017b/render");Directory.CreateDirectory(output);client=GetComponent<RiichiClient>();lobby=client.Lobby;client.BeginLobbyValidation("visual-rehearsal-only");
            bool effectsOnly=Environment.GetEnvironmentVariable("RIICHI_REVIEW1017B_EFFECTS_ONLY")=="1";
            if(!effectsOnly){
            yield return Size(1688,780);lobby.DesignPreviewClock=100;lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Create);lobby.ValidationAdvanced(false);
            typeof(LobbyController).GetProperty("Rounds").SetValue(lobby,4);yield return null;yield return Shot("create-full-match");
            var before=Load("before-discard");before.audioEvents=new PublicSound[0];client.DesignPreviewSessionTable(before);lobby.DesignPreviewClock=-1;yield return null;yield return null;
            yield return Shot("wait-button-before-selection");
            int index=TableRenderer.DisplayHand(before).FindIndex(t=>t=="z1");client.SelectTile(index,"discard:z1");yield return null;yield return Shot("wait-selected-discard");
            Debug.Log("WAIT PREVIEW before discard: button="+client.WaitIndicatorVisible+", candidate waits="+client.CurrentWaitDetails.Length);
            var after=Load("after-discard");client.ValidationApplyState(after);client.ValidationIntro.Reset();yield return null;yield return null;yield return Shot("wait-after-discard-hidden");
            Debug.Log("WAIT PREVIEW after discard: button="+client.WaitIndicatorVisible+", open waits="+client.CurrentWaitDetails.Length+", candidates="+client.CurrentDiscardWaits.Length);
            }
            yield return Size(1688,780);
            foreach(string scenario in new[]{"yakuman-chance","yakuman-certain"}){
                client.DesignPreviewSessionTable(Load(scenario));client.ValidationYakuman.Freeze=true;client.ValidationYakuman.FrozenAge=0;
                yield return null;Directory.CreateDirectory(Path.Combine(output,scenario));
                for(int frame=0;frame<36;frame++){client.ValidationYakuman.FrozenAge=frame/20f;yield return null;yield return Shot(scenario+"/frame-"+frame.ToString("D3"));}
                client.ValidationYakuman.FrozenAge=.65f;yield return null;yield return Shot(scenario);
                Debug.Log("YAKUMAN VISUAL "+scenario+": active="+client.ValidationYakuman.Active+", level="+client.ValidationYakuman.Level+", layers="+client.ValidationYakuman.LayerCount+", particles="+client.ValidationYakuman.ParticleCount);
            }
            if(effectsOnly)yield break;
            var win=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/unity-smoke/win-4-tsumo-0-0.json"));win.audioEvents=new PublicSound[0];win.players[win.you].ready=false;client.DesignPreviewSessionTable(win);
            var presentation=client.ValidationResult;presentation.Observe(win);presentation.Freeze=true;presentation.FrozenAge=.3f;presentation.Advance(win);
            Debug.Log("YAKU CONFIRM after manual skip: remaining="+presentation.YakuConfirmRemaining(win));
            float end=SettlementAudio.Sequence(win.result.details[0]).end;
            for(int i=0;i<3;i++){presentation.FrozenAge=end+i;yield return null;yield return Shot("yaku-confirm-"+(3-i));}
            presentation.FrozenAge=end+3;presentation.Tick(win,false);yield return null;yield return Shot("yaku-countdown-to-scores");
            Debug.Log("YAKU CONFIRM after three seconds: scores="+presentation.ShowingScores+", winner="+presentation.WinnerIndex);
            foreach(var size in new[]{new Vector2(1688,780),new Vector2(1280,720)}){
                yield return Size((int)size.x,(int)size.y);var room=Load("ready-full");room.pin="479009";client.DesignPreviewSessionTable(room);lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Room);lobby.DesignPreviewClock=200;lobby.DesignPreviewInvitation();lobby.DesignPreviewClock=201;yield return null;yield return null;yield return Shot("invite-qr-large-"+(int)size.x);
            }
            lobby.DesignPreviewClock=-1;
        }
    }
}
