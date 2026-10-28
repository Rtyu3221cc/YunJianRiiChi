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
    [InitializeOnLoad] public static class WardrobeR14Preview {
        const string Key="WardrobeR14Preview";
        static WardrobeR14Preview(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<WardrobeR14Capture>();}};}
        public static void Run(){AssetDatabase.Refresh();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class WardrobeR14Capture:MonoBehaviour {
        string output;LobbyController lobby;bool dynamic;float inset;int checks;
        IEnumerator Start(){
            output=Path.GetFullPath("../dist/previews/2026-10-28-wardrobe-r14");Directory.CreateDirectory(output);dynamic=AppPreferences.DynamicOutfits;inset=AppPreferences.ScreenInset;AppPreferences.SetDynamicOutfits(false);AppPreferences.SetInset(0);CharacterRoster.BeginPreview();WardrobeSelection.BeginPreview();AudioListener.pause=true;
            var client=GetComponent<RiichiClient>();client.BeginLobbyValidation("wardrobe-r14");GetComponent<ResourceRoomCoordinator>().enabled=false;lobby=client.Lobby;
            var work=new Stack<IEnumerator>();work.Push(Run());while(work.Count>0){object next;try{var task=work.Peek();if(!task.MoveNext()){work.Pop();continue;}next=task.Current;if(next is IEnumerator child){work.Push(child);continue;}}catch(Exception e){File.WriteAllText(output+"/error.txt",e.ToString());Debug.LogException(e);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            File.WriteAllText(output+"/capture-checks.txt",checks+" capture and selection checks passed\n");Cleanup();Debug.Log("WARDROBE R14 SCREENSHOTS PASS "+checks);EditorApplication.Exit(0);
        }
        void Cleanup(){WardrobeSelection.EndPreview();CharacterRoster.EndPreview();AppPreferences.SetDynamicOutfits(dynamic);AppPreferences.SetInset(inset);AudioListener.pause=false;}
        void Check(bool pass,string text){if(!pass)throw new Exception(text);checks++;}
        IEnumerator Wait(int count=4){for(int i=0;i<count;i++)yield return null;}
        IEnumerator Shot(string key){yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+key+".png",image.EncodeToPNG());Destroy(image);}
        IEnumerator Press(string id){Check(lobby.ValidationTarget(id).width>0,"Visible control "+id);lobby.ValidationPress(id);yield return Wait();}
        IEnumerator Run(){
            var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(1746,810),"Wardrobe R14 still images"});yield return Wait(10);
            WardrobeSelection.Save(new Dictionary<string,string>{{"hand","hand-human"}});lobby.PreviewWardrobe("winEffect");yield return Wait(6);yield return Shot("01-effect-list-top");
            var card=lobby.ValidationTarget("dress-item-ron-ko");var point=card.center;lobby.ReviewQueueGuiPointer(EventType.MouseDown,point,Vector2.zero);yield return Wait(2);lobby.ReviewQueueGuiPointer(EventType.MouseDrag,point+Vector2.up*-240,Vector2.zero);yield return Wait(2);lobby.ReviewQueueGuiPointer(EventType.MouseUp,point+Vector2.up*-240,Vector2.zero);yield return Wait(3);Check(lobby.PreviewWardrobeGridScroll>0,"Right effect pane scrolls by touch drag");yield return Shot("02-effect-list-bottom");
            lobby.PreviewWardrobeGridOffset(0);lobby.ReviewQueueGuiPointer(EventType.ScrollWheel,point,new Vector2(0,4));yield return Wait(3);Check(lobby.PreviewWardrobeGridScroll>0,"Right effect pane scrolls by mouse wheel");
            lobby.PreviewWardrobeCategory("hand");yield return Wait(4);Check(lobby.PreviewWardrobeGridScroll==0,"Category change resets the right pane");Check(WardrobeCatalog.Items("hand").Length==2,"Only default and orange cat are visible");yield return Shot("03-hand-options-female");
            var human=WardrobeCatalog.Find("hand-human");Check(WardrobeRuntime.ResolveHand(human,"yiji").id=="hand-human","Female character resolves the default female hand");Check(WardrobeRuntime.ResolveHand(human,"mingzhiyingshu").id=="hand-man","Male character resolves the default male hand");Check(WardrobeRuntime.ResolveHand(WardrobeCatalog.Find("hand-cat"),"mingzhiyingshu").id=="hand-cat-orange","Old cat selection resolves the single orange option");
            yield return Press("dress-eye");lobby.PreviewWardrobeAge(.6f);yield return Wait();yield return Shot("04-default-female-preview");
            lobby.PreviewWardrobeForRole("mingzhiyingshu",401401);yield return Wait(6);yield return Shot("05-hand-options-male");yield return Press("dress-eye");lobby.PreviewWardrobeAge(.6f);yield return Wait();yield return Shot("06-default-male-preview");
            yield return Press("dress-preview-close");yield return Press("dress-item-hand-cat-orange");yield return Press("dress-eye");lobby.PreviewWardrobeAge(.6f);yield return Wait();yield return Shot("07-orange-cat-preview");
        }
    }
}
