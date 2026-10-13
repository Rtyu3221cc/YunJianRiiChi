#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi {
 public static class ReviewValidation0537 {
  [Serializable] public class Sample {public string scene,path;public float worldWidth,pixelWidth,pixelHeight;public Vector3 position,scale;}
  [Serializable] public class Report {public Sample[] samples;}
  static readonly List<Sample> samples=new List<Sample>();
  static void Check(bool value,string message){if(!value)throw new Exception(message);}
  static RoomView Load(string folder,string name){var v=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,name+".json")));v.audioEvents=new PublicSound[0];v.audioRoom="tile-size-review";v.options=new Choice[0];v.handSeq=0;return v;}
  static void Size(int w,int h){var x=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));x.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(x,new object[]{new Vector2(w,h),"Table geometry review"});x.Repaint();}
  static IEnumerator Shot(string folder,string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return new WaitForSecondsRealtime(.08f);}
  static void Record(TableRenderer table,string scene){foreach(var box in GameObject.Find("PublicTableState").GetComponentsInChildren<BoxCollider>(true)){var t=box.transform;var model=t.Find("Original tile");if(model==null)continue;var rect=table.ValidationTileOutline(t);samples.Add(new Sample{scene=scene,path=t.parent.name+"/"+t.name,worldWidth=model.lossyScale.x,pixelWidth=rect.width,pixelHeight=rect.height,position=t.position,scale=t.lossyScale});}}
  static void GeometryCheck(TableRenderer table){foreach(var box in GameObject.Find("PublicTableState").GetComponentsInChildren<BoxCollider>(true)){var tile=box.transform;if(tile.Find("Original tile")==null)continue;Check(Mathf.Abs(TableTileGeometry.WorldWidth(tile)-TableTileGeometry.Width)<.0001f,"World width differs for "+tile.name);Check(Vector3.Distance(tile.lossyScale,Vector3.one)<.0001f,"Tile inherits presentation scaling: "+tile.name);}}
  static Rect Bounds(Transform root,Camera camera){Vector2 min=Vector2.one*float.MaxValue,max=Vector2.one*float.MinValue;foreach(var c in root.GetComponentsInChildren<BoxCollider>(true))for(int i=0;i<8;i++){var p=camera.WorldToScreenPoint(c.transform.TransformPoint(c.center+Vector3.Scale(c.size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));min=Vector2.Min(min,p);max=Vector2.Max(max,p);}return Rect.MinMaxRect(min.x,min.y,max.x,max.y);}
  public static IEnumerator Run(RiichiClient client,string folder){
   bool baseline=Environment.GetEnvironmentVariable("RIICHI_TILE_BASELINE")=="1";string prefix=baseline?"review-0537-before-":"review-0537-after-";samples.Clear();var table=client.GetComponent<TableRenderer>();var fx=client.GetComponent<TableEffects>();
   client.ValidationTableOnly=true;
   Size(1688,780);for(int i=0;i<8;i++)yield return null;
   foreach(int n in new[]{3,4})foreach(int seat in Enumerable.Range(0,n)){
    var win=Load(folder,"win-"+n+"-tsumo-"+seat+"-0");table.Clear();client.ShowValidationFixture(win);yield return new WaitForSecondsRealtime(.85f);string name="tsumo-"+n+"-seat-"+seat;Record(table,name);yield return Shot(folder,prefix+name);
    if(!baseline){GeometryCheck(table);var target=table.WinningTarget(new PublicSound{seat=seat,kind="tsumo"});var row=GameObject.Find("PublicTableState").transform.Find("Seat "+seat+"/Revealed winning hand");var local=Quaternion.Inverse(row.rotation)*target.position;var boxes=row.GetComponentsInChildren<BoxCollider>(true);Check(local.x>boxes.Max(b=>b.transform.localPosition.x+b.size.x/2)+.1f,"Self draw touches revealed row");Check(Mathf.Abs(target.position.y-boxes[0].transform.position.y)<.0001f,"Self draw rests at a different height");fx.ValidationPlay(seat,"tsumo",win.result.details[0].tile);fx.ValidationTime=fx.ValidationHandoff-.01f;yield return null;yield return null;Check(Mathf.Abs(TableTileGeometry.WorldWidth(fx.ValidationAnimatedTile)-TableTileGeometry.WorldWidth(target))<.0001f,"Carried self draw size differs");Check(Quaternion.Angle(fx.ValidationAnimatedTile.rotation,target.rotation)<.001f,"Carried self draw orientation differs");yield return Shot(folder,prefix+"carried-"+n+"-seat-"+seat);fx.ValidationTime=fx.ValidationHandoff+.01f;yield return null;Check(target.gameObject.activeSelf&&!fx.ValidationAnimatedTile.gameObject.activeSelf,"Self draw handoff has duplicate tiles");fx.Reset();fx.ValidationFreeze=false;}
   }
   client.ValidationTableOnly=false;
   foreach(var res in new[]{new Vector2(1688,780),new Vector2(1280,720)}){
    Size((int)res.x,(int)res.y);for(int i=0;i<8;i++)yield return null;
    foreach(int count in baseline?new[]{1,4}:new[]{1,2,3,4}){
     var v=Load(folder,"preview-4");v.players[0].melds=new[]{"z3333-","p777+","s1111","z555="}.Take(count).ToArray();v.hand=new[]{"m1","m9","p1","p2","p3","p4","p5","s1","s2","s3","z1","z1","z4","z4"}.Take(14-count*3).ToArray();v.drawn=v.hand.Last();foreach(var p in v.players)p.nuki=2;client.ShowValidationFixture(v);for(int i=0;i<5;i++)yield return null;string name="meld-"+count+"-"+(int)res.x;Record(table,name);yield return Shot(folder,prefix+name);
     if(!baseline){GeometryCheck(table);var root=GameObject.Find("PublicTableState").transform.Find("Seat 0/Exposed melds");var rect=Bounds(root,table.Camera);Check(!rect.Overlaps(table.HandScreenBounds()),"Meld overlaps foreground hand");Check(rect.xMax<=Screen.safeArea.xMax&&rect.xMin>=Screen.safeArea.xMin&&rect.yMin>=Screen.safeArea.yMin,"Meld clipped");var first=root.GetComponentsInChildren<BoxCollider>(true).OrderBy(c=>Mathf.Abs(c.transform.localRotation.eulerAngles.y)).First();float reference=OriginalTiles.HandWidth/18*Screen.height*.98f;Check(table.ValidationTileOutline(first.transform).width>=reference*.98f,"Enlarged own meld shrank");Check(Mathf.Abs(table.HandScreenBounds().height-OriginalTiles.HandHeight/18*Screen.height)<1.5f,"Foreground hand was resized");foreach(int seat in Enumerable.Range(0,4)){var a=table.NorthTarget(seat,1);var b=table.NorthTarget(seat,2);Check(Mathf.Abs(Vector3.Distance(a.position,b.position)-TableTileGeometry.Width-.001f)<.0001f,"North tiles not adjacent");}if(count>=3){var added=table.EventMeld(new PublicSound{seat=0,meld="p777+"});Check(added!=null,"Meld group lookup missing");}}
    }
   }
   Size(1688,780);for(int i=0;i<8;i++)yield return null;
   foreach(bool tenpai in new[]{true,false}){var v=Load(folder,"draw-3-0");v.result.drawDetails[0].tenpai=tenpai;v.result.drawDetails[0].tiles=Load(folder,"preview-3").hand.Take(13).ToArray();client.ShowValidationFixture(v);yield return new WaitForSecondsRealtime(.85f);string name="draw-"+(tenpai?"up":"down");Record(table,name);yield return Shot(folder,prefix+name);if(!baseline)GeometryCheck(table);}
   if(!baseline){
    // Every direction and physical instance keeps its destination shape through
    // discard/North handoffs. State polling must not expose queued tiles early.
    foreach(string kind in new[]{"discard","nuki"})foreach(int seat in Enumerable.Range(0,4)){var v=Load(folder,"preview-4");foreach(var p in v.players)p.nuki=2;client.ShowValidationFixture(v);yield return null;string tile=kind=="nuki"?"z4":v.players[seat].river.Last().tile;v.last=new LastDiscard{from=seat,tile=tile};table.Render(v);fx.Accept(v);yield return null;fx.ValidationPlay(seat,kind,tile);fx.ValidationTime=fx.ValidationHandoff-.01f;yield return null;yield return null;var target=fx.ValidationTarget;Check(target!=null,"Action target missing");Check(Mathf.Abs(TableTileGeometry.WorldWidth(fx.ValidationAnimatedTile)-TableTileGeometry.WorldWidth(target))<.0001f,"Carried "+kind+" changes size");Check(!target.gameObject.activeSelf,"Action destination appears before handoff");table.Render(v);fx.Accept(v);yield return null;Check(fx.ValidationTarget!=null,"Polling lost "+kind+" destination, seat "+seat);Check(!fx.ValidationTarget.gameObject.activeSelf,"Polling exposes action destination");fx.ValidationTime=fx.ValidationHandoff+.01f;yield return null;Check(fx.ValidationTarget.gameObject.activeSelf,"Action destination never lands");fx.Reset();fx.ValidationFreeze=false;}
    // Added kan uses the model's physical thickness; concealed kan uses the same size.
    var kan=Load(folder,"preview-4");kan.players[0].melds=new[]{"p555+5","s1111"};kan.hand=new[]{"m1","m9","p1","p2","p3","s1","z1","z1"};kan.drawn="z1";client.ShowValidationFixture(kan);yield return null;var meld=table.EventMeld(new PublicSound{seat=0,meld="p555+5"});Check(meld?.extra!=null,"Added kan tile missing");Check(meld.extra.position.y-meld.anchor.position.y>=table.Geometry.Depth,"Added kan penetrates lower tile");GeometryCheck(table);yield return Shot(folder,prefix+"kan-stack");
    // An inherited scale is included when a destination is copied to an unparented
    // animation tile, even though normal layout groups are now unit scale.
    var source=table.NorthTarget(0,1);if(source==null){kan.players[0].nuki=1;client.ShowValidationFixture(kan);yield return null;source=table.NorthTarget(0,1);}source.parent.localScale=Vector3.one*1.25f;var clone=table.CreateEffectTile("z4",source);Check(Mathf.Abs(TableTileGeometry.WorldWidth(clone)-TableTileGeometry.WorldWidth(source))<.0001f,"Shape copy drops inherited scale");UnityEngine.Object.Destroy(clone.gameObject);
   }
   fx.Reset();File.WriteAllText(Path.Combine(folder,prefix+"geometry.json"),JsonUtility.ToJson(new Report{samples=samples.ToArray()},true));Debug.Log("RIICHI REVIEW 0537 "+(baseline?"BASELINE":"PASS")+": actual camera screenshots and geometry samples");
  }
 }
}
#endif
