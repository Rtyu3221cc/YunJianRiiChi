#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace YunJian.Riichi {
 public static class ReviewValidation0539 {
  static readonly string[] Melds={"z3333-","p555+5","s1111","z555="};
  static readonly string[] Faces={"m1","m9","p1","p2","p3","p4","p5","s1","s2","s3","z1","z1","z4","z4"};
  static int checks;
  static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
  static RoomView Load(string folder,string name){var v=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,name+".json")));v.audioEvents=new PublicSound[0];v.options=new Choice[0];v.handSeq=0;v.audioRoom="clearance-0539-"+name;return v;}
  static void Size(int w,int h){var win=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));win.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(win,new object[]{new Vector2(w,h),"Opponent hand clearance regression"});win.Repaint();}
  static IEnumerator Shot(string folder,string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-0539-"+name+".png"));yield return new WaitForSecondsRealtime(.08f);}
  static Transform Seat(int i)=>GameObject.Find("PublicTableState").transform.Find("Seat "+i);
  static Vector2 Span(Transform root,Transform seat){float lo=float.MaxValue,hi=float.MinValue;foreach(var f in root.GetComponentsInChildren<MeshFilter>(true)){if(f.name!="Original tile"&&f.name!="Original tile outline")continue;var b=f.sharedMesh.bounds;var matrix=seat.worldToLocalMatrix*f.transform.localToWorldMatrix;for(int i=0;i<8;i++){var p=matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));lo=Mathf.Min(lo,p.x);hi=Mathf.Max(hi,p.x);}}return new Vector2(lo,hi);}
  static void Gap(TableRenderer table,RoomView view,int seat,Transform hand,bool winning=false){
   Check(hand!=null,"Hand root missing at seat "+seat);var axis=Seat(seat);var meld=axis.Find("Exposed melds");
   foreach(var box in hand.GetComponentsInChildren<BoxCollider>(true))Check(Mathf.Abs(TableTileGeometry.WorldWidth(box.transform)-.35f)<.0001f,"Hand width changed");
   var h=Span(hand,axis);if(winning){var target=table.WinningTarget(new PublicSound{seat=seat,kind="tsumo"});Check(target!=null,"Self draw target missing");var t=Span(target,axis);h.y=Mathf.Max(h.y,t.y);Check(Mathf.Abs(TableTileGeometry.WorldWidth(target)-.35f)<.0001f,"Self draw size changed");}
   if(view.players[seat].melds.Length==0){Check(Mathf.Abs(axis.InverseTransformPoint(hand.position).x)<.0001f,"Unblocked hand moved");return;}
   var m=Span(meld,axis);float gap=m.x-h.y;
   Check(gap>=table.Geometry.GroupGap*3-.002f,"Hand/meld overlap or insufficient gap: n="+view.n+", viewer="+view.you+", seat="+seat+", gap="+gap+", pose="+hand.name);
   Debug.Log("CLEARANCE 0539 n="+view.n+" viewer="+view.you+" seat="+seat+" pose="+hand.name+" gap="+gap);
  }
  static void Setup(RoomView view,int count,int viewer,bool ownOpen){view.you=viewer;foreach(var p in view.players){p.melds=Melds.Take(count).ToArray();p.count=Math.Max(1,13-count*3);}view.players[viewer].melds=ownOpen?Melds.Take(count).ToArray():new string[0];view.hand=Faces.Take(ownOpen?14-count*3:14).ToArray();view.drawn=view.hand.Last();}
  public static IEnumerator Run(RiichiClient client,string folder){
   checks=0;var table=client.GetComponent<TableRenderer>();var fx=client.GetComponent<TableEffects>();
   // Actual visible geometry after camera/safe-area adjustment, with the owner
   // either fully concealed or also open. This includes the previous collision fixture.
   foreach(var size in new[]{new Vector2(1688,780),new Vector2(1280,720)}){
    Size((int)size.x,(int)size.y);for(int i=0;i<8;i++)yield return null;
    foreach(int n in new[]{3,4})foreach(int viewer in new[]{0,1})foreach(bool ownOpen in new[]{false,true})for(int count=0;count<=4;count++){
     var view=Load(folder,"preview-"+n);Setup(view,count,viewer,ownOpen);client.ShowValidationFixture(view);yield return new WaitForSecondsRealtime(.18f);
     foreach(int seat in Enumerable.Range(0,n).Where(s=>s!=viewer))Gap(table,view,seat,GameObject.Find("Standing hand seat "+seat).transform);
     table.Render(view);fx.Accept(view);yield return null;yield return null;foreach(int seat in Enumerable.Range(0,n).Where(s=>s!=viewer))Gap(table,view,seat,GameObject.Find("Standing hand seat "+seat).transform);
     if(count==4&&viewer==0&&ownOpen)yield return Shot(folder,"standing-"+n+"-"+(int)size.x);
    }
   }
   Size(1688,780);for(int i=0;i<8;i++)yield return null;client.ValidationTableOnly=true;
   foreach(int n in new[]{3,4})foreach(int seat in Enumerable.Range(1,n-1)){
    var view=Load(folder,"win-"+n+"-tsumo-"+seat+"-0");foreach(var p in view.players){p.melds=Melds;p.count=2;}view.result.details[0].tiles=new[]{"m1","z1"};table.Clear();client.ShowValidationFixture(view);yield return new WaitForSecondsRealtime(.85f);
    var row=Seat(seat).Find("Revealed winning hand");Gap(table,view,seat,row,true);yield return Shot(folder,"tsumo-seat-"+seat+"-"+n);
    fx.ValidationPlay(seat,"tsumo",view.result.details[0].tile);fx.ValidationTime=fx.ValidationHandoff-.01f;yield return null;yield return null;Check(Mathf.Abs(TableTileGeometry.WorldWidth(fx.ValidationAnimatedTile)-.35f)<.0001f,"Carried self draw changed size");fx.ValidationTime=fx.ValidationHandoff+.01f;yield return null;Check(fx.ValidationTarget.gameObject.activeSelf,"Self draw did not land");fx.Reset();fx.ValidationFreeze=false;
   }
   client.ValidationTableOnly=false;
   foreach(bool tenpai in new[]{true,false})foreach(int n in new[]{3,4}){
    var view=Load(folder,"draw-"+n+"-0");foreach(var p in view.players){p.melds=Melds;p.count=2;}foreach(var d in view.result.drawDetails){d.tiles=new[]{"m1","z1"};d.tenpai=tenpai;}table.Clear();client.ShowValidationFixture(view);yield return new WaitForSecondsRealtime(.85f);foreach(int seat in Enumerable.Range(1,n-1))Gap(table,view,seat,Seat(seat).Find("Draw hand seat "+seat));yield return Shot(folder,"draw-"+(tenpai?"up":"down")+"-"+n);
   }
   Debug.Log("RIICHI REVIEW 0539 PASS: "+checks+" actual geometry assertions; 3/4 seats, rotated viewers, two resolutions, 0..4 melds, unchanged sizes, standing/tsumo/draw gaps, polling and carried/landed self-draw.");
  }
 }
}
#endif
