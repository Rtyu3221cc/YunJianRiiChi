using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    // A short visual rehearsal, not a unit-test runner or Android build.
    [InitializeOnLoad] public static class Review1017Preview {
        const string Key="Riichi.Review1017Preview";
        static Review1017Preview(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<Review1017Capture>();}};}
        public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class Review1017Capture:MonoBehaviour {
        string output;RiichiClient client;LobbyController lobby;
        static readonly BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        IEnumerator Start(){
            var routine=Capture();while(true){bool next;try{next=routine.MoveNext();}catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);yield break;}if(!next)break;yield return routine.Current;}
            Debug.Log("REVIEW 1017 VISUAL PREVIEW COMPLETE; no unit tests or APK");EditorApplication.Exit(0);
        }
        IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());Destroy(image);}
        IEnumerator Size(int w,int h){var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));view.GetType().GetMethod("SetCustomResolution",Private).Invoke(view,new object[]{new Vector2(w,h),"Lobby and QR visual review"});for(int i=0;i<6;i++)yield return null;}
        RoomView Load(string name)=>JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/unity-smoke/"+name+".json"));
        IEnumerator Capture(){
            output=Path.GetFullPath("../build/review-1016b/render");Directory.CreateDirectory(output);client=GetComponent<RiichiClient>();lobby=client.Lobby;
            client.BeginLobbyValidation("qr-visual-only");yield return Size(1688,780);
            if(Environment.GetEnvironmentVariable("RIICHI_REVIEW1017_CHROME_ONLY")=="1"){
                var pending=Load("preview-4");pending.pin="479009";pending.audioEvents=new PublicSound[0];
                client.DesignPreviewPendingSession(pending);lobby.DesignPreviewClock=100;lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Home);yield return null;yield return null;yield return Shot("home-session-redpoint");
                if(Environment.GetEnvironmentVariable("RIICHI_REVIEW1017_REDPOINT_ONLY")=="1")yield break;
                lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Modes);lobby.DesignPreviewScanner();lobby.DesignPreviewClock=101;yield return null;yield return Shot("scanner");
                yield break;
            }
            if(Environment.GetEnvironmentVariable("RIICHI_REVIEW1017_WAIT_ONLY")=="1"){
                foreach(var size in new[]{new Vector2(1688,780),new Vector2(1280,720)}){
                    yield return Size((int)size.x,(int)size.y);
                    var waits=Load("hint-open");waits.players[waits.you].auto=false;waits.waitDetails=new[]{new WaitDetail{tile="p6",remaining=2,ron=false,tsumo=true},new WaitDetail{tile="s5",remaining=1,ron=false,tsumo=true}};
                    client.DesignPreviewSessionTable(waits);yield return null;yield return null;yield return Shot("wait-off-"+(int)size.x);
                    var point=client.CurrentWaitButton.center;client.ValidationWaitPointer(point,true,true,false,10);client.ValidationWaitPointer(point,false,false,true,10.06f);yield return null;yield return Shot("wait-on-"+(int)size.x);client.ValidationEndWaitPointer();
                }
                yield break;
            }
            var hand=Load("preview-4");hand.pin="479009";hand.audioEvents=new PublicSound[0];hand.lobby=new LobbyRoomView{category="friend",red=true,managed=true};
            client.DesignPreviewPendingSession(hand);lobby.DesignPreviewClock=100;lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Home);yield return null;yield return null;yield return Shot("home-session-redpoint");
            lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Modes);lobby.OpenCreate();Directory.CreateDirectory(Path.Combine(output,"create-frames"));
            for(int i=0;i<=30;i++){lobby.DesignPreviewClock=100+i/30f;yield return null;yield return Shot("create-frames/frame-"+i.ToString("D3"));}
            var room=Load("preview-4");room.phase="lobby";room.pin="479009";room.host=true;room.you=0;room.audioEvents=new PublicSound[0];room.players=room.players.Take(2).ToArray();room.lobby=new LobbyRoomView{category="friend",red=true,managed=true};
            client.DesignPreviewSessionTable(room);lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Room);lobby.DesignPreviewClock=110;yield return null;yield return null;yield return Shot("ready-room");
            lobby.DesignPreviewInvitation();lobby.DesignPreviewClock=111;yield return null;yield return Shot("invite-ready-qr");
            var qr=RoomQr.Image(client.InvitationPayload);File.WriteAllBytes(Path.Combine(output,"invitation-qr.png"),qr.EncodeToPNG());Destroy(qr);
            client.DesignPreviewPendingSession(hand);lobby.DesignPreviewClock=120;lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Modes);lobby.DesignPreviewJoin();lobby.DesignPreviewClock=121;yield return null;yield return Shot("join-scan-button");
            lobby.DesignPreviewScanner();lobby.DesignPreviewClock=122;yield return null;yield return Shot("scanner");
            var invalid=RoomQr.Image("not-a-yunjian-room");string bad=Path.Combine(output,"invalid-qr.png");File.WriteAllBytes(bad,invalid.EncodeToPNG());Destroy(invalid);
            GetComponentInChildren<RoomQrScanner>().OnImagePicked(bad);lobby.DesignPreviewClock=123;yield return null;yield return Shot("invalid-qr-dialog");
            lobby.DesignPreviewScanner();lobby.DesignPreviewClock=124;GetComponentInChildren<RoomQrScanner>().OnImagePicked(Path.Combine(output,"invitation-qr.png"));
            float deadline=Time.realtimeSinceStartup+8;while((client.State?.phase!="lobby"||client.State.host)&&Time.realtimeSinceStartup<deadline)yield return null;
            lobby.DesignPreviewClock=125;yield return null;yield return null;yield return Shot("gallery-joined-room");
            File.WriteAllText(Path.Combine(output,"gallery-flow.txt"),"Actual shared photo decode and room join: phase="+client.State?.phase+", room="+client.State?.pin+", host="+client.State?.host+". Camera and Android photo picker require device rehearsal; no APK built.");
            client.DesignPreviewSessionTable(hand);lobby.DesignPreviewClock=130;yield return null;lobby.OpenInvitation(true);lobby.DesignPreviewClock=131;yield return null;yield return Shot("invite-table-qr");
            lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Home);lobby.DesignPreviewClock=-1;
            foreach(var size in new[]{new Vector2(1688,780),new Vector2(1280,720)}){
                yield return Size((int)size.x,(int)size.y);
                var waits=Load("hint-open");waits.players[waits.you].auto=false;waits.waitDetails=new[]{new WaitDetail{tile="p6",remaining=2,ron=false,tsumo=true},new WaitDetail{tile="s5",remaining=1,ron=false,tsumo=true}};
                client.DesignPreviewSessionTable(waits);yield return null;yield return null;yield return Shot("wait-off-"+(int)size.x);
                var point=client.CurrentWaitButton.center;client.ValidationWaitPointer(point,true,true,false,10);client.ValidationWaitPointer(point,false,false,true,10.06f);yield return null;yield return Shot("wait-on-"+(int)size.x);
                client.ValidationEndWaitPointer();
                var chi=Load("preview-4");chi.phase="reaction";chi.turn=1;chi.options=new[]{new Choice{id="chi:m123-",kind="chi",meld="m123-"},new Choice{id="chi:m234-",kind="chi",meld="m234-"},new Choice{id="pass",kind="pass"}};
                client.DesignPreviewSessionTable(chi);yield return null;yield return null;yield return Shot("chi-panel-"+(int)size.x);
            }
        }
    }
}
