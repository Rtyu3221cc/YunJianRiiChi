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
    [InitializeOnLoad] public static class WardrobeR15Preview {
        const string Key="WardrobeR15Preview";
        static WardrobeR15Preview(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<WardrobeR15Capture>();}};}
        public static void Run(){AssetDatabase.Refresh();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        public static void RunForR16(){SessionState.SetString("WardrobeR15.Output","../build/wardrobe-r16/r15-regression");Run();}
    }
    public sealed class WardrobeR15Capture:MonoBehaviour {
        string output;RiichiClient client;LobbyController lobby;bool dynamic;float inset;
        readonly List<string> checks=new List<string>();
        IEnumerator Start(){
            output=Path.GetFullPath(SessionState.GetString("WardrobeR15.Output","../dist/previews/2026-10-28-wardrobe-r15"));SessionState.EraseString("WardrobeR15.Output");Directory.CreateDirectory(output);
            dynamic=AppPreferences.DynamicOutfits;inset=AppPreferences.ScreenInset;AppPreferences.SetDynamicOutfits(false);AppPreferences.SetInset(0);CharacterRoster.BeginPreview();WardrobeSelection.BeginPreview();AudioListener.pause=true;
            client=GetComponent<RiichiClient>();client.BeginLobbyValidation("wardrobe-r15");client.SessionPreviewOnly=true;GetComponent<ResourceRoomCoordinator>().enabled=false;lobby=client.Lobby;
            var work=new Stack<IEnumerator>();work.Push(Run());
            while(work.Count>0){object next;try{var task=work.Peek();if(!task.MoveNext()){work.Pop();continue;}next=task.Current;if(next is IEnumerator child){work.Push(child);continue;}}catch(Exception e){File.WriteAllText(output+"/error.txt",e.ToString());Debug.LogException(e);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            File.WriteAllLines(output+"/capture-checks.txt",checks);Cleanup();Debug.Log("WARDROBE R15 SCREENSHOTS PASS "+checks.Count);EditorApplication.Exit(0);
        }
        void Cleanup(){WardrobeSelection.EndPreview();CharacterRoster.EndPreview();AppPreferences.SetDynamicOutfits(dynamic);AppPreferences.SetInset(inset);AudioListener.pause=false;}
        void Check(bool pass,string text){if(!pass)throw new Exception(text);checks.Add("PASS "+text);}
        IEnumerator Wait(int frames=4){for(int i=0;i<frames;i++)yield return null;}
        IEnumerator Shot(string key){yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+key+".png",image.EncodeToPNG());Destroy(image);}
        RoomView Load(string path){var v=JsonUtility.FromJson<RoomView>(File.ReadAllText(path));v.audioEvents=new PublicSound[0];v.dealtAt=0;v.openingHand=new string[0];return v;}
        RoomView Copy(RoomView v)=>JsonUtility.FromJson<RoomView>(JsonUtility.ToJson(v));
        void TapWait(){var p=client.CurrentWaitButton.center;Check(client.ValidationWaitPointer(p,true,true,false,0),"Wait press is consumed");Check(client.ValidationWaitPointer(p,false,false,true,.1f),"Wait release is consumed");}
        void Poll(RoomView v){var next=Copy(v);next.version++;client.ValidationApplyState(next);}
        IEnumerator Run(){
            var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(1746,810),"Wardrobe R15 still images"});yield return Wait(10);
            WardrobeSelection.Save(new Dictionary<string,string>{{"hand","hand-human"}});lobby.PreviewWardrobeForRole("mingzhiyingshu",401401);yield return Wait(6);
            Check(WardrobeCatalog.Items("hand").Length==2,"Exactly two hand options");Check(WardrobeRuntime.ResolveHand(WardrobeCatalog.Find("hand-human"),"mingzhiyingshu").id=="hand-man","Default male hand");Check(WardrobeRuntime.ResolveHand(WardrobeCatalog.Find("hand-human"),"yiji").id=="hand-human","Default female hand");yield return Shot("01-hand-options");
            lobby.PreviewWardrobeCategory("winEffect");yield return Wait(4);var point=lobby.ValidationTarget("dress-item-ron-ko").center;
            lobby.ReviewQueueGuiPointer(EventType.MouseDown,point,Vector2.zero);yield return Wait(2);lobby.ReviewQueueGuiPointer(EventType.MouseDrag,point+Vector2.up*-240,Vector2.zero);yield return Wait(2);lobby.ReviewQueueGuiPointer(EventType.MouseUp,point+Vector2.up*-240,Vector2.zero);yield return Wait(3);Check(lobby.PreviewWardrobeGridScroll>0,"Effect list scrolls by touch inside the frame");yield return Shot("02-effects-scrolled");
            lobby.PreviewWardrobeGridOffset(0);lobby.ReviewQueueGuiPointer(EventType.ScrollWheel,point,new Vector2(0,4));yield return Wait(3);Check(lobby.PreviewWardrobeGridScroll>0,"Effect list scrolls by wheel");lobby.Home();
            foreach(int n in new[]{4,3}){
                var v=Load("../build/unity-smoke/win-"+n+"-tsumo-0-0.json");v.audioRoom="wardrobe-r15-score-"+n;v.result.matchOver=true;
                for(int i=0;i<n;i++){v.players[i].name=i==0?"旅人":"牌友"+i;v.players[i].character=i%2==0?"yiji":"erjietang";v.players[i].skin=i%2==0?400101:400201;v.players[i].wardrobe=new WardrobeLoadout{headFrame=i%2==0?"headframe_catear3":"headframe_default"};}
                client.ShowValidationFixture(v);var display=client.ValidationResult;display.Observe(v);display.Freeze=true;display.FrozenAge=100;display.Advance(v);display.FrozenAge=ResultPresentation.ScoreEnd(n)+.2f;yield return Wait(5);
                Check(display.ShowingScores&&!display.ShowingFinal,"Score settlement page "+n+" players");Check(display.EquippedAvatarFramesDrawn==(n+1)/2,"Score settlement draws each equipped frame "+n+" players");yield return Shot("03-score-"+n);
                display.FrozenAge=100;display.Advance(v);display.FrozenAge=3;yield return Wait(4);Check(display.ShowingFinal,"Final standings page "+n+" players");Check(display.EquippedAvatarFramesDrawn==(n+1)/2,"Final standings draws each equipped frame "+n+" players");yield return Shot("04-final-"+n);
            }
            foreach(string kind in new[]{"ron","tsumo"}){
                var v=Load("../build/wardrobe-r15/"+kind+"-waits.json");client.ShowValidationFixture(v);yield return Wait(5);Check(client.WaitIndicatorVisible&&v.options.Any(o=>o.kind==kind),kind+" and wait controls coexist");
                if(client.CurrentWaitDetails.Length>0){TapWait();yield return Wait();}
                Check(client.CurrentWaitDetails.Length==0,kind+" wait popup closed");Poll(v);yield return Wait(2);Check(client.CurrentWaitDetails.Length==0,kind+" poll preserves closed state");
                var p=client.CurrentWaitButton.center;client.ValidationWaitPointer(p,true,true,false,1);Poll(v);yield return Wait(2);client.ValidationWaitPointer(p,false,false,true,1.1f);yield return Wait(2);
                Check(client.CurrentWaitDetails.Length>0,kind+" poll during tap preserves the gesture");
                for(int i=0;i<6;i++){Poll(v);yield return new WaitForSecondsRealtime(.65f);Check(client.CurrentWaitDetails.Length>0,kind+" open popup survives poll "+i);}
                Check(client.CurrentWaitPanel.width>0,kind+" visible wait panel");yield return Shot("05-waits-"+kind);
                TapWait();Poll(v);yield return Wait();Check(client.CurrentWaitDetails.Length==0,kind+" manual close survives polling");
                TapWait();yield return Wait();var changed=Copy(v);changed.phase="result";changed.result=new ResultView{title="回归测试结束",delta=new int[v.n],details=new WinDetail[0]};client.ValidationApplyState(changed);yield return Wait();Check(client.CurrentWaitDetails.Length==0,kind+" result transition closes waits");
            }
            // Preserve the existing discard-preview, thirteen-sided wait and
            // cancelled-gesture behavior alongside the new win-button cases.
            var hint=Load("../build/unity-smoke/hint-open.json");client.ShowValidationFixture(hint);yield return Wait();Check(client.CurrentWaitDetails.Length==hint.waitDetails.Length&&client.CurrentWaitPanel.width>0,"New discard decision auto-opens current waits");
            var tile=UnityEngine.Object.FindObjectsByType<TileTarget>(FindObjectsSortMode.None).First(t=>t.Action!=null&&t.Action.StartsWith("discard:p9"));client.SelectTile(tile.Index,tile.Action);yield return Wait();Check(client.CurrentWaitDetails.Length==2&&client.CurrentWaitDetails.All(w=>!w.ron&&!w.tsumo),"Selected discard keeps its no-yaku waits");Poll(hint);yield return Wait();Check(client.CurrentWaitDetails.Length==2,"Selected discard waits survive polling");
            var thirteen=Load("../build/unity-smoke/hint-thirteen.json");client.ShowValidationFixture(thirteen);yield return Wait();Check(client.CurrentWaitDetails.Length==0,"Other-turn waits start closed");TapWait();Poll(thirteen);yield return Wait();Check(client.CurrentWaitDetails.Length==13,"Thirteen-sided waits survive polling");
            var button=client.CurrentWaitButton.center;client.ValidationWaitPointer(button,true,true,false,2);client.ValidationWaitPointer(button+new Vector2(-220,-90),false,true,false,2.1f);client.ValidationWaitPointer(button,false,false,true,2.2f);yield return Wait();Check(client.CurrentWaitDetails.Length==13,"Cancelled drag preserves the open popup");
            client.SendMessage("OnApplicationFocus",false);yield return Wait();Check(client.CurrentWaitDetails.Length==0,"Focus loss closes the private wait popup");
        }
    }
}
