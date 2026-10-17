using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    [InitializeOnLoad] public static class Review1017cPreview {
        const string Key="Riichi.Review1017cPreview";
        static Review1017cPreview(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<Review1017cCapture>();}};}
        public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class Review1017cCapture:MonoBehaviour {
        string output;RiichiClient client;LobbyController lobby;
        static readonly BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        IEnumerator Start(){
            var routine=Capture();while(true){bool next;try{next=routine.MoveNext();}catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);yield break;}if(!next)break;yield return routine.Current;}
            Debug.Log("REVIEW 1017C STILL PREVIEW COMPLETE; no new recording, unit tests, APK or Git");EditorApplication.Exit(0);
        }
        IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());Destroy(image);}
        IEnumerator Size(int w,int h){var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));view.GetType().GetMethod("SetCustomResolution",Private).Invoke(view,new object[]{new Vector2(w,h),"Sound, wait offset and QR still preview"});for(int i=0;i<6;i++)yield return null;}
        RoomView Load(string name)=>JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/review-1017b/fixtures/"+name+".json"));
        void ReadScreenshotQr(string name){
            var picture=new Texture2D(2,2);picture.LoadImage(File.ReadAllBytes(Path.Combine(output,name+".png")));
            string recovered=RoomQr.Decode(picture.GetPixels32(),picture.width,picture.height);Destroy(picture);
            if(recovered!=client.InvitationPayload)throw new InvalidOperationException("Cannot read displayed invitation QR: "+name);
            Debug.Log("DISPLAYED QR PHOTO DECODE: "+name+" restored the exact invitation payload");
        }
        IEnumerator Capture(){
            output=Path.GetFullPath("../build/review-1017c/render");Directory.CreateDirectory(output);client=GetComponent<RiichiClient>();lobby=client.Lobby;client.BeginLobbyValidation("visual-rehearsal-only");
            yield return Size(1688,780);
            var waits=Load("before-discard");waits.audioEvents=new PublicSound[0];client.DesignPreviewSessionTable(waits);yield return null;yield return null;
            client.SelectTile(TableRenderer.DisplayHand(waits).FindIndex(t=>t=="z1"),"discard:z1");yield return null;yield return Shot("wait-down-5");
            var certain=Load("yakuman-certain");var audio=GetComponent<GameAudio>();audio.PlayedCues.Clear();client.DesignPreviewSessionTable(certain);client.ValidationYakuman.Freeze=true;client.ValidationYakuman.FrozenAge=.2f;yield return null;yield return null;
            client.ValidationApplyState(certain);client.ValidationIntro.Reset();client.ValidationYakuman.FrozenAge=.65f;yield return null;yield return Shot("yakuman-ready-sound");
            int count=audio.PlayedCues.Count(k=>k=="effect_yakumanready");var clip=Resources.Load<AudioClip>("Majsoul/Audio/effect_yakumanready");
            if(clip==null||count!=1)throw new InvalidOperationException("Original yakuman-ready cue missing or repeated");
            Debug.Log("YAKUMAN READY SOUND: native clip "+clip.length+" seconds, one cue across repeated state observations");
            foreach(var size in new[]{new Vector2(1132,635),new Vector2(1280,720),new Vector2(1688,780)}){
                yield return Size((int)size.x,(int)size.y);var room=Load("ready-full");room.pin="479009";client.DesignPreviewSessionTable(room);lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Room);lobby.DesignPreviewClock=200;yield return null;yield return null;
                lobby.DesignPreviewInvitation();lobby.DesignPreviewClock=201;yield return null;yield return null;
                foreach(bool themed in new[]{false,true}){
                    lobby.DesignPreviewQrTheme(themed);yield return null;string name="qr-"+(themed?"themed":"white")+"-"+(int)size.x;yield return Shot(name);ReadScreenshotQr(name);
                    if((int)size.x==1132)File.WriteAllBytes(Path.Combine(output,themed?"qr-themed-source.png":"qr-white-source.png"),lobby.DesignPreviewQrTexture.EncodeToPNG());
                }
            }
            lobby.DesignPreviewQrTheme(false);lobby.DesignPreviewClock=-1;
        }
    }
}
