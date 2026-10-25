using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    [InitializeOnLoad] public static class EmoteButtonPreview {
        const string Key="EmoteButtonPreview";
        static EmoteButtonPreview(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<EmoteButtonCapture>();}};}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Bootstrap(){if(!SessionState.GetBool(Key,false))return;CharacterPackService.RootOverride=Path.GetFullPath("../build/emote-button-1023-smaller/installed");CharacterPackService.RespectPreviewAvailability=false;CharacterPackService.ResetForReview();}
        public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class EmoteButtonCapture:MonoBehaviour {
        string output;RiichiClient client;float originalInset;bool failed;
        void Log(string message,string stack,LogType type){if(type!=LogType.Error&&type!=LogType.Exception)return;failed=true;File.AppendAllText(Path.Combine(output,"errors.txt"),message+"\n"+stack+"\n");}
        IEnumerator Hold(int frames=4){for(int i=0;i<frames;i++)yield return null;}
        IEnumerator Shot(string name){yield return Hold();yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());Destroy(image);}
        IEnumerator Start(){
            output=Path.GetFullPath("../build/emote-button-1023-smaller/render");Directory.CreateDirectory(output);Application.logMessageReceived+=Log;
            originalInset=AppPreferences.ScreenInset;CharacterRoster.BeginPreview();AppPreferences.SetInset(0);AudioListener.pause=true;Application.targetFrameRate=30;
            client=GetComponent<RiichiClient>();client.BeginLobbyValidation("emote-button-preview");GetComponent<ResourceRoomCoordinator>().enabled=false;
            var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(1688,768),"Emoji button preview"});yield return Hold(8);
            var fixture=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.GetFullPath("../build/unity-smoke/preview-4.json")));fixture.audioRoom="emote-button-preview";fixture.audioEvents=new PublicSound[0];fixture.audioSeq=0;fixture.resourceSetup=null;fixture.emotes=new PublicEmote[0];fixture.emoteCooldownMs=0;
            string[] roles={"kawei","weilai","fuji","yiji"};int[] skins={401004,406903,403805,400101};string[] names={"旅人","未来","福姬","一姬"};
            for(int i=0;i<4;i++){fixture.players[i].character=roles[i];fixture.players[i].skin=skins[i];fixture.players[i].name=names[i];}
            client.DesignPreviewSessionTable(fixture);client.ValidationIntro.Reset();typeof(RiichiClient).GetField("resourceSelectionRevision",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(client,CharacterPackService.Revision);typeof(RiichiClient).GetField("resourceSelectionDirty",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(client,false);yield return Hold(20);
            var emotes=client.ValidationEmotes;for(int i=0;i<9;i++)CharacterRoster.DisplayTexture("Emoji/kawei/"+i);emotes.Sending();emotes.ReviewPress="emote-open";yield return Hold();
            if(!emotes.IsOpen)throw new Exception("Emoji picker did not open");yield return Shot("expanded");
            File.WriteAllText(Path.Combine(output,"expanded-handle.json"),JsonUtility.ToJson(emotes.ReviewTargets["emote-close"],true));
            emotes.ReviewPress="emote-close";yield return Hold();if(emotes.IsOpen)throw new Exception("Emoji picker did not close");yield return Shot("collapsed");
            File.WriteAllText(Path.Combine(output,"collapsed-button.json"),JsonUtility.ToJson(emotes.ReviewTargets["emote-open"],true));
            AppPreferences.SetInset(originalInset);CharacterRoster.EndPreview();AudioListener.pause=false;Application.logMessageReceived-=Log;
            Debug.Log("EMOTE BUTTON PREVIEW "+(failed?"FAIL":"PASS"));EditorApplication.Exit(failed?1:0);
        }
    }
}
