using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YunJian.Riichi.Editor {
 [InitializeOnLoad] public static class WinSettlementValidation {
  const string Key="Riichi.WinSettlementValidation";
  static WinSettlementValidation(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindFirstObjectByType<RiichiClient>().gameObject.AddComponent<WinSettlementProbe>();}};}
  public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
 }
 public sealed class WinSettlementProbe:MonoBehaviour {
  IEnumerator Start(){var run=Run();while(true){bool next;try{next=run.MoveNext();}catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);yield break;}if(!next)break;yield return run.Current;}Debug.Log("WIN SETTLEMENT VALIDATION PASS");EditorApplication.Exit(0);}
  int checks;
  void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
  IEnumerator Shot(string dir,string name){yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(dir,name+".png"),image.EncodeToPNG());Destroy(image);}
  IEnumerator Run(){
   var client=GetComponent<RiichiClient>();var table=GetComponent<TableRenderer>();var fx=GetComponent<TableEffects>();client.ValidationTableOnly=true;
   var dir=Path.GetFullPath("../build/win-settlement-0543");Directory.CreateDirectory(dir);
   var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
   foreach(var size in new[]{new Vector2(1280,720),new Vector2(1688,780)}){
    window.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{size,"Win settlement regression"});for(int i=0;i<8;i++)yield return null;
    foreach(int n in new[]{3,4})foreach(int viewer in new[]{0,1})for(int relative=0;relative<n;relative++){
     int seat=(viewer+relative)%n;
     var view=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/unity-smoke/win-"+n+"-tsumo-"+seat+"-"+viewer+".json"));view.audioRoom="winning-probe";view.audioEvents=new PublicSound[0];
     client.DesignPreviewSessionTable(view);client.ValidationTableOnly=true;fx.Accept(view);yield return new WaitForSecondsRealtime(.75f);
     fx.ValidationPlay(seat,"tsumo",view.result.details[0].tile);fx.CutIn.Reset();
     foreach(float at in new[]{0f,.3f,.6f,.8f,1f,1.13f}){
      fx.ValidationTime=at;yield return null;yield return null;
      var target=table.WinningTarget(new PublicSound{seat=seat,kind="tsumo"});var tile=fx.ValidationAnimatedTile;
      Check(!target.gameObject.activeInHierarchy&&tile.gameObject.activeInHierarchy,"Two visible self-draw tiles before handoff");
      Check(Mathf.Abs(tile.position.x-target.position.x)<.0001f&&Mathf.Abs(tile.position.z-target.position.z)<.0001f,"Self-draw sweeps out of its reserved slot");
      Check(Mathf.Abs(TableTileGeometry.WorldWidth(tile)-TableTileGeometry.WorldWidth(target))<.0001f,"Carried self-draw changes size");
      Check(tile.GetComponentsInChildren<Transform>().All(t=>t.gameObject.layer==0),"Carried self-draw ignores table depth");
      if(at==.6f){table.Render(view);fx.Accept(view);yield return null;Check(!table.WinningTarget(new PublicSound{seat=seat,kind="tsumo"}).gameObject.activeInHierarchy,"Polling duplicates the carried tile");}
      if(n==4&&relative==1&&viewer==0&&at==1f){fx.CutIn.Reset();yield return Shot(dir,"carried-"+(int)size.x);}
     }
     fx.ValidationTime=fx.ValidationHandoff+.003f;yield return null;yield return null;
     var landed=table.WinningTarget(new PublicSound{seat=seat,kind="tsumo"});
     Check(landed.gameObject.activeInHierarchy&&!fx.ValidationAnimatedTile.gameObject.activeInHierarchy,"Duplicate self-draw at landing");
     Check(Vector3.Distance(fx.ValidationAnimatedTile.position,landed.position)<.002f,"Self-draw snaps at handoff");
     if(n==4&&relative==1&&viewer==0){fx.CutIn.Reset();yield return Shot(dir,"landed-"+(int)size.x);}
     fx.CutIn.Freeze=true;fx.CutIn.FrozenAge=WinCutIn.Duration+.1f;fx.ValidationTime=10;yield return null;yield return null;Check(!fx.ShowWinPresentation,"Self-draw effect did not finish");fx.Reset();fx.ValidationFreeze=false;
    }
   }
   client.ValidationTableOnly=false;
   foreach(int n in new[]{3,4})foreach(bool final in new[]{false,true})foreach(bool auto in new[]{false,true}){
    var view=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/unity-smoke/win-"+n+"-tsumo-1-0.json"));view.audioEvents=new PublicSound[0];view.result.matchOver=final;view.players[view.you].ready=false;
    var d=new ResultPresentation();d.Observe(view);d.Freeze=true;
    while(!d.ShowingScores){d.FrozenAge=100;d.Advance(view);}
    float end=ResultPresentation.ScoreEnd(n);
    foreach(float elapsed in new[]{-.01f,0f,.999f,1f,1.999f,2f,2.999f}){
     d.FrozenAge=end+elapsed;Check(d.Tick(view,auto)==ResultPresentation.Action.None,"Confirmation occurs before three complete seconds");
     if(elapsed>=0)Check(d.ScoreConfirmRemaining(view)==3-Mathf.FloorToInt(elapsed),"Incorrect confirmation numeral");
     d.Observe(view);Check(d.ShowingScores&&!d.ShowingFinal,"Polling resets/advances score stage");
    }
    d.FrozenAge=end+3;var action=d.Tick(view,auto);
    Check(final?d.ShowingFinal&&action==ResultPresentation.Action.None:action==ResultPresentation.Action.Ready,"Three-second confirmation did not advance correctly");
    if(final){d.FrozenAge=8.999f;Check(d.Tick(view,auto)==ResultPresentation.Action.None,"Final countdown shortened");d.FrozenAge=9;Check(d.Tick(view,auto)==ResultPresentation.Action.Ready,"Final nine-second countdown missing");}
    view.players[view.you].ready=true;Check(d.Tick(view,auto)==ResultPresentation.Action.None,"Already confirmed player sends again");view.players[view.you].ready=false;
    d.Reset();d.Observe(view);d.Freeze=true;while(!d.ShowingScores){d.FrozenAge=100;d.Advance(view);}d.FrozenAge=end+1;d.Advance(view);Check(final?d.ShowingFinal:d.ShowingScores,"Early manual confirmation failed");
    view.handSeq++;d.Observe(view);Check(!d.ShowingFinal&&!d.ShowingScores,"Countdown leaks into the next hand");
   }
   var screen=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/unity-smoke/win-4-tsumo-1-0.json"));screen.audioEvents=new PublicSound[0];screen.result.matchOver=true;client.DesignPreviewSessionTable(screen);var display=client.ValidationResult;display.Observe(screen);display.Freeze=true;while(!display.ShowingScores){display.FrozenAge=100;display.Advance(screen);}
   foreach(int elapsed in new[]{0,1,2}){display.FrozenAge=ResultPresentation.ScoreEnd(4)+elapsed;yield return null;yield return Shot(dir,"confirm-"+(3-elapsed));}
   File.WriteAllText(Path.Combine(dir,"passed.txt"),"Unity 0.5.43: "+checks+" assertions; 3/4 seats, two viewers, two resolutions, self-draw slot/depth/scale/polling/handoff; manual and AI 3-second confirmation, manual early click, unchanged final 9 seconds, ready dedup and hand reset. Synthetic scenes; no Android device.");
   Debug.Log("WIN SETTLEMENT CHECKS "+checks);
  }
 }
}
