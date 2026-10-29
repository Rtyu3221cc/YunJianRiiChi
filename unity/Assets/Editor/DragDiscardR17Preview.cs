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
    [InitializeOnLoad] public static class DragDiscardR17Preview {
        const string Key="DragDiscardR17Preview";
        static DragDiscardR17Preview(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<DragDiscardR17Capture>();}};}
        public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class DragDiscardR17Capture:MonoBehaviour {
        RiichiClient client;TableRenderer table;string output;float clock=10;readonly List<string> checks=new List<string>();
        IEnumerator Start(){
            output=Path.GetFullPath("../dist/previews/2026-10-28-drag-discard-r17");Directory.CreateDirectory(output);client=GetComponent<RiichiClient>();table=GetComponent<TableRenderer>();
            client.BeginLobbyValidation("drag-r17");client.SessionPreviewOnly=true;GetComponent<ResourceRoomCoordinator>().enabled=false;client.ValidationCaptureActions=true;WardrobeSelection.BeginPreview();AudioListener.pause=true;
            var work=new Stack<IEnumerator>();work.Push(Run());while(work.Count>0){object next;try{var task=work.Peek();if(!task.MoveNext()){work.Pop();continue;}next=task.Current;if(next is IEnumerator child){work.Push(child);continue;}}catch(Exception e){File.WriteAllText(output+"/error.txt",e.ToString());Debug.LogException(e);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            File.WriteAllLines(output+"/checks.txt",checks);Cleanup();Debug.Log("DRAG DISCARD R17 PASS "+checks.Count);EditorApplication.Exit(0);
        }
        void Cleanup(){client.ValidationEndHandPointer();client.ValidationHandBusy(false);client.ValidationCaptureActions=false;table.ValidationSafeArea=null;WardrobeSelection.EndPreview();AudioListener.pause=false;}
        void Check(bool value,string label){if(!value)throw new Exception(label);checks.Add("PASS "+label);}
        IEnumerator Wait(int n=4){for(int i=0;i<n;i++)yield return null;}
        IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var im=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+name+".png",im.EncodeToPNG());Destroy(im);}
        RoomView Copy(RoomView v)=>JsonUtility.FromJson<RoomView>(JsonUtility.ToJson(v));
        RoomView Fixture(){var v=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/unity-smoke/hint-open.json"));v.audioRoom="drag-r17";v.audioEvents=new PublicSound[0];v.dealtAt=0;v.openingHand=new string[0];v.players[v.you].auto=false;return v;}
        IEnumerator Use(RoomView v){client.ValidationEndHandPointer();client.ValidationHandBusy(false);client.ShowValidationFixture(v);client.ValidationToggleRiichi(false);yield return new WaitForSecondsRealtime(.22f);yield return Wait();Physics.SyncTransforms();}
        bool Pointer(Vector2 p,bool down=false,bool held=false,bool up=false){clock+=.05f;return client.ValidationHandPointer(p,down,held,up,clock);}
        Vector2 Press(int index){var p=table.HandTileScreenPoint(index);Physics.SyncTransforms();Check(Pointer(p,true,true),"Hand press captures tile "+index);return p;}
        Vector2 MoveToCenter(){var zone=client.ValidationHandDropZone;Check(zone.width>0&&zone.height>0,"Central discard zone exists");var point=zone.center;Pointer(point,held:true);return point;}
        IEnumerator Run(){
            var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
            foreach(var size in new[]{new Vector2(1280,720),new Vector2(1746,810),new Vector2(2340,1080)}){
                window.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{size,"Drag discard device rehearsal"});yield return Wait(8);
                table.ValidationSafeArea=size.x>2000?new Rect(90,25,size.x-150,size.y-40):(Rect?)null;
                var view=Fixture();yield return Use(view);int count=client.ValidationActionCount;var p=Press(0);Pointer(p,up:true);yield return Wait();Check(client.SelectedIndex==0&&client.ValidationActionCount==count,"First tap only selects at "+size.x);
                p=Press(0);Pointer(p,up:true);Check(client.ValidationActionCount==count+1&&client.ValidationLastAction=="discard:p2","Second tap preserves discard behavior at "+size.x);
                yield return Use(view);count=client.ValidationActionCount;p=Press(0);var target=MoveToCenter();yield return Wait();
                Check(client.ValidationHandDragging,"Moving a held tile starts drag");Check(Vector2.Distance(table.HandTileScreenPoint(0),target)<2,"Actual tile follows the pointer at "+size.x);
                if(size.x==1746)yield return Shot("01-held-in-center");Pointer(target,up:true);Check(client.ValidationActionCount==count+1&&client.ValidationLastAction=="discard:p2","Central release submits one discard at "+size.x);Pointer(target,up:true);Check(client.ValidationActionCount==count+1,"Duplicate release cannot submit twice");
                yield return Use(view);count=client.ValidationActionCount;p=Press(0);target=MoveToCenter();yield return Wait();Pointer(p+Vector2.right*20,held:true);Pointer(p+Vector2.right*20,up:true);yield return Wait();
                Check(!client.ValidationHandDragging&&client.ValidationActionCount==count,"Return to hand cancels discard at "+size.x);Check(Vector3.Distance(table.ValidationHandPosition(0),table.ValidationHandBase(0))<.001f,"Cancelled tile returns to its original slot");if(size.x==1746)yield return Shot("02-returned-to-hand");
                // Preserve the physical copy and visual position across a presence poll.
                yield return Use(view);int duplicate=TableRenderer.DisplayHand(view).FindLastIndex(t=>t=="z2");count=client.ValidationActionCount;p=Press(duplicate);target=MoveToCenter();var poll=Copy(view);poll.version++;client.ValidationApplyState(poll);yield return Wait();
                Check(client.ValidationHandDragging&&client.SelectedIndex==duplicate,"Polling retains the chosen duplicate tile");Check(Vector2.Distance(table.HandTileScreenPoint(duplicate),target)<2,"Polling rebuild retains drag position");Pointer(target,up:true);Check(client.ValidationActionCount==count+1&&client.ValidationLastAction=="discard:z2","Duplicate tile submits its legal action");
                // Release during a poll waits for that poll, then revalidates once.
                yield return Use(view);count=client.ValidationActionCount;p=Press(0);target=MoveToCenter();client.ValidationHandBusy(true,true);Pointer(target,up:true);Check(client.ValidationDragQueued&&client.ValidationActionCount==count,"Poll in flight does not swallow the release");
                poll=Copy(view);poll.version++;client.ValidationApplyState(poll);client.ValidationHandBusy(false);Pointer(target);Check(!client.ValidationDragQueued&&client.ValidationActionCount==count+1,"Unchanged poll commits the queued release once");Pointer(target);Check(client.ValidationActionCount==count+1,"Queued release is deduplicated");
                yield return Use(view);count=client.ValidationActionCount;p=Press(0);target=MoveToCenter();client.ValidationHandBusy(true,true);Pointer(target,up:true);var changed=Copy(view);changed.turn=1;changed.options=new Choice[0];client.ValidationApplyState(changed);client.ValidationHandBusy(false);Pointer(target);Check(client.ValidationActionCount==count&&!client.ValidationDragQueued,"Turn change cancels a queued discard");
                yield return Use(view);count=client.ValidationActionCount;p=Press(0);target=MoveToCenter();changed=Copy(view);changed.handSeq++;client.ValidationApplyState(changed);Pointer(target,up:true);Check(client.ValidationActionCount==count&&!client.ValidationHandDragging,"New hand cancels an in-flight gesture");
                yield return Use(view);count=client.ValidationActionCount;p=Press(0);target=MoveToCenter();client.SendMessage("OnApplicationFocus",false);Pointer(target,up:true);Check(client.ValidationActionCount==count&&!client.ValidationHandDragging,"Focus loss cannot discard a held tile");
                yield return Use(view);count=client.ValidationActionCount;p=Press(0);target=MoveToCenter();Pointer(new Vector2(1,target.y),held:true);Pointer(new Vector2(1,target.y),up:true);Check(client.ValidationActionCount==count,"Release outside the central table cancels");
                yield return Use(view);count=client.ValidationActionCount;p=Press(0);target=MoveToCenter();Pointer(target,held:false);Pointer(target,up:true);Check(client.ValidationActionCount==count,"Lost pointer cancels instead of synthesizing a tap");
                yield return Use(view);count=client.ValidationActionCount;p=Press(0);target=MoveToCenter();client.ValidationHandPointer(target,false,true,false,clock,true);Pointer(target,up:true);Check(client.ValidationActionCount==count&&!client.ValidationHandDragging,"Second finger or captured UI cancels the gesture");
                yield return Use(view);count=client.ValidationActionCount;p=Press(0);target=MoveToCenter();client.ValidationHandBusy(true,false);Pointer(target,up:true);client.ValidationHandBusy(false);Pointer(target);Check(client.ValidationActionCount==count&&!client.ValidationDragQueued,"Another action in flight cannot queue a duplicate discard");
                yield return Use(view);count=client.ValidationActionCount;p=Press(0);target=MoveToCenter();client.ValidationHandBusy(true,true);Pointer(target,up:true);clock+=2;client.ValidationHandBusy(false);Pointer(target);Check(client.ValidationActionCount==count&&!client.ValidationDragQueued,"Slow or failed poll expires the released gesture");
                yield return Use(view);count=client.ValidationActionCount;p=Press(0);target=MoveToCenter();client.ValidationHandBusy(true,true);Pointer(target,up:true);client.ValidationHandBusy(false);client.ValidationHandPointer(target,false,true,false,clock,true);Check(client.ValidationActionCount==count&&!client.ValidationDragQueued,"Second finger also cancels a queued release");
                var other=Copy(view);other.turn=1;other.options=new Choice[0];yield return Use(other);count=client.ValidationActionCount;p=Press(0);target=MoveToCenter();Pointer(target,up:true);Check(client.ValidationActionCount==count,"Cannot discard during another seat's turn");
                var riichi=Copy(view);riichi.options=riichi.options.Concat(new[]{new Choice{id="riichi:p9_",kind="riichi",tile="p9"}}).ToArray();yield return Use(riichi);client.ValidationToggleRiichi(true);yield return Wait();Physics.SyncTransforms();count=client.ValidationActionCount;
                var bad=table.HandTileScreenPoint(0);Check(!Pointer(bad,true,true),"Illegal riichi tile is not draggable");Pointer(bad,up:true);Check(client.ValidationActionCount==count,"Illegal riichi drag cannot submit");
                int drawn=TableRenderer.DisplayHand(riichi).Count-1;p=Press(drawn);target=MoveToCenter();Pointer(target,up:true);Check(client.ValidationActionCount==count+1&&client.ValidationLastAction=="riichi:p9_","Legal riichi drag retains the declaration action");
            }
        }
    }
}
