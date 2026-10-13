#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi {
 public static class ReviewValidation0536 {
  static void Check(bool v,string s){if(!v)throw new Exception(s);}
  static IEnumerator Shot(string f,string n){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(f,"review-0536-"+n+".png"));yield return new WaitForSecondsRealtime(.06f);}
  static RoomView Load(string folder){var v=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"preview-4.json")));v.audioRoom="river-0536";v.audioSeq=0;v.audioEvents=new PublicSound[0];v.handSeq=0;v.turn=1;v.drawn=null;v.options=new Choice[0];return v;}
  public static IEnumerator Run(RiichiClient client,string folder){
   var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(1688,780),"0.5.36 replacement sideways marker"});for(int i=0;i<8;i++)yield return null;var table=client.GetComponent<TableRenderer>();var fx=client.GetComponent<TableEffects>();
   foreach(string meld in new[]{"p123-","p333-","p3333-"}){
    var v=Load(folder);v.players[0].riichi=1;v.players[0].river=new[]{new RiverTile{tile="p1"},new RiverTile{tile="p2"},new RiverTile{tile="s1"},new RiverTile{tile="s2"},new RiverTile{tile="z1"},new RiverTile{tile="p3",riichi=true,called=true},new RiverTile{tile="p9"},new RiverTile{tile="p8"}};v.players[1].melds=new[]{meld};client.ShowValidationFixture(v);yield return null;yield return null;
    var seat=GameObject.Find("PublicTableState").transform.Find("Seat 0");var marker=seat.Find("River 5 p9");var next=seat.Find("River 6 p8");Check(marker!=null&&next!=null,"Called declaration not removed from physical river");Check(Mathf.Abs(Mathf.DeltaAngle(marker.localEulerAngles.y,90))<2,"Replacement tile not sideways");Check(Mathf.Abs(Mathf.DeltaAngle(next.localEulerAngles.y,0))<2,"Following normal tile also sideways");Check(!v.players[0].river[6].riichi,"Presentation mutated the declaration flag");yield return Shot(folder,meld=="p123-"?"01-chi-next-sideways":meld=="p333-"?"02-pon-next-sideways":"03-kan-next-sideways");table.Render(v);yield return null;Check(Mathf.Abs(Mathf.DeltaAngle(GameObject.Find("PublicTableState").transform.Find("Seat 0/River 5 p9").localEulerAngles.y,90))<2,"Polling loses replacement rotation");
   }
   var history=new PlayerView{river=new[]{new RiverTile{tile="p3",riichi=true,called=true},new RiverTile{tile="p9",called=true},new RiverTile{tile="s1"},new RiverTile{tile="s2"}}};Check(TableRenderer.RiichiSideways(history,2)&&!TableRenderer.RiichiSideways(history,3),"Repeated calls fail to carry marker");
   var state=Load(folder);state.players[0].riichi=1;state.players[0].river=history.river;state.audioSeq=0;client.ShowValidationFixture(state);fx.Accept(state);state.audioSeq=1;state.audioEvents=new[]{new PublicSound{id=1,seat=0,kind="discard",tile="s1",riverIndex=3}};fx.ValidationFreeze=true;fx.ValidationTime=.2f;fx.Accept(state);yield return null;yield return null;var target=table.FindDiscard(0,"s1",3);Check(fx.ValidationKind=="discard"&&!fx.ValidationRiichiDiscard&&fx.ValidationCallKind==null,"Replacement generates another riichi declaration");Check(Quaternion.Angle(fx.ValidationAnimatedTile.rotation,target.rotation)<.01f,"Carried discard does not match sideways landing");yield return Shot(folder,"04-sideways-normal-discard-hand");fx.ValidationTime=.7f;yield return null;yield return null;Check(target.gameObject.activeSelf,"Replacement discard never lands");yield return Shot(folder,"05-sideways-landed");fx.Reset();fx.ValidationFreeze=false;
   Debug.Log("RIICHI REVIEW 0536 PASS: chi/pon/kan claimed declarations carry rotation to next physical discard, normal following tile upright, polling/history stable, repeated claims transfer marker, plain discard hand matches sideways target without another riichi call.");
  }
 }
}
#endif
