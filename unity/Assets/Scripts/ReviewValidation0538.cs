#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace YunJian.Riichi {
 public static class ReviewValidation0538 {
  static void Check(bool value,string message){if(!value)throw new Exception(message);}
  static void Size(int w,int h){var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(w,h),"Meld visible edge contact"});window.Repaint();}
  static RoomView Load(string folder,int n){var v=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"preview-"+n+".json")));v.audioEvents=new PublicSound[0];v.options=new Choice[0];v.audioRoom="meld-contact-0538";v.handSeq=0;return v;}
  static IEnumerator Shot(string folder,string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-0538-"+name+".png"));yield return new WaitForSecondsRealtime(.08f);}
  static Vector2 Span(BoxCollider box,Transform root){
   float min=float.MaxValue,max=float.MinValue;var model=box.transform.Find("Original tile");
   foreach(var mesh in model.GetComponentsInChildren<MeshFilter>(true)){
    if(mesh.name!="Original tile"&&mesh.name!="Original tile outline")continue;var bounds=mesh.sharedMesh.bounds;
    for(int i=0;i<8;i++){var corner=Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));var p=root.InverseTransformPoint(mesh.transform.TransformPoint(bounds.center+corner));min=Mathf.Min(min,p.x);max=Mathf.Max(max,p.x);}
   }return new Vector2(min,max);
  }
  static void Contact(TableRenderer table,int seat,string[] melds){
   var root=GameObject.Find("PublicTableState").transform.Find("Seat "+seat+"/Exposed melds");var spans=new List<Vector2>();
   foreach(string value in melds){var visual=table.EventMeld(new PublicSound{seat=seat,meld=value});Check(visual!=null,"Missing meld");foreach(var box in visual.group.GetComponentsInChildren<BoxCollider>(true)){
    Check(Mathf.Abs(TableTileGeometry.WorldWidth(box.transform)-.35f)<.0001f,"Meld size changed");if(box.transform!=visual.extra)spans.Add(Span(box,root));
   }if(visual.extra!=null){Check(Vector3.Distance(visual.extra.localPosition,visual.extraRest)<.0001f,"Added kan anchor moved");Check(visual.extra.position.y-visual.anchor.position.y>=table.Geometry.Depth,"Added kan penetrates base");}}
   var sorted=spans.OrderBy(s=>s.x).ToArray();for(int i=1;i<sorted.Length;i++)Check(Mathf.Abs(sorted[i].x-sorted[i-1].y)<.0001f,"Visible meld edges have a gap/overlap, seat "+seat+", gap "+(sorted[i].x-sorted[i-1].y));
  }
  public static IEnumerator Run(RiichiClient client,string folder){
   var table=client.GetComponent<TableRenderer>();var fx=client.GetComponent<TableEffects>();
   // Same fixtures as the approved 0.5.37 screenshots; only packing changes.
   foreach(var res in new[]{new Vector2(1688,780),new Vector2(1280,720)}){
    Size((int)res.x,(int)res.y);for(int i=0;i<8;i++)yield return null;
    for(int count=1;count<=4;count++){
     var v=Load(folder,4);v.players[0].melds=new[]{"z3333-","p777+","s1111","z555="}.Take(count).ToArray();v.hand=new[]{"m1","m9","p1","p2","p3","p4","p5","s1","s2","s3","z1","z1","z4","z4"}.Take(14-count*3).ToArray();v.drawn=v.hand.Last();foreach(var p in v.players)p.nuki=2;client.ShowValidationFixture(v);for(int i=0;i<5;i++)yield return null;Contact(table,0,v.players[0].melds);Check(Mathf.Abs(table.HandScreenBounds().height-OriginalTiles.HandHeight/18*Screen.height)<1.5f,"Foreground hand resized");yield return Shot(folder,"own-"+count+"-"+(int)res.x);
    }
   }
   Size(1688,780);for(int i=0;i<8;i++)yield return null;
   foreach(int n in new[]{3,4}){
    var v=Load(folder,n);foreach(var p in v.players){p.melds=new[]{"z3333-","p555+5","s1111","z555="};p.count=2;}v.hand=new[]{"m1","z1"};v.drawn="z1";client.ShowValidationFixture(v);for(int i=0;i<5;i++)yield return null;foreach(int seat in Enumerable.Range(0,n))Contact(table,seat,v.players[seat].melds);yield return Shot(folder,"all-seats-"+n);table.Render(v);fx.Accept(v);yield return null;foreach(int seat in Enumerable.Range(0,n))Contact(table,seat,v.players[seat].melds);
   }
   // The new packed group remains contiguous while moved by the original call hand.
   var call=Load(folder,4);call.players[0].melds=new[]{"p333-"};call.audioRoom="contact-call";call.audioSeq=0;client.ShowValidationFixture(call);fx.Accept(call);yield return null;call.audioSeq=1;call.audioEvents=new[]{new PublicSound{id=1,seat=0,kind="pon",meld="p333-",tile="p3"}};fx.ValidationFreeze=true;fx.ValidationTime=.2f;client.ValidationApplyState(call);fx.Accept(call);yield return null;Contact(table,0,call.players[0].melds);yield return Shot(folder,"pon-carried");fx.ValidationTime=fx.ValidationHandoff+.01f;yield return null;Contact(table,0,call.players[0].melds);fx.Reset();fx.ValidationFreeze=false;
   Debug.Log("RIICHI REVIEW 0538 PASS: actual meld visible edge contacts within/across groups in 3/4-seat directions, 1..4 own groups at 1688x780/1280x720, unchanged world/foreground sizes, hidden/added kans, polling and original pon motion.");
  }
 }
}
#endif
