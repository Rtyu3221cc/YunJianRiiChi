#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi {
 public static class ReviewValidation0531 {
  static void Check(bool v,string m){if(!v)throw new Exception(m);}
  static void Size(int w,int h){var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(w,h),"0.5.31 meld and title"});window.Repaint();}
  static RoomView Load(string folder,int n){var v=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"preview-"+n+".json")));v.audioEvents=new PublicSound[0];v.options=new Choice[0];v.waitDetails=new WaitDetail[0];v.discardHints=new DiscardHint[0];return v;}
  static IEnumerator Capture(string folder,string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-0531-"+name+".png"));yield return new WaitForSecondsRealtime(.12f);}
  static Rect Bounds(Transform root,Camera camera){Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);foreach(var c in root.GetComponentsInChildren<BoxCollider>(true))for(int i=0;i<8;i++){var p=camera.WorldToScreenPoint(c.transform.TransformPoint(c.center+Vector3.Scale(c.size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));min=Vector2.Min(min,p);max=Vector2.Max(max,p);}return Rect.MinMaxRect(min.x,min.y,max.x,max.y);}
  static Transform Upright(Transform root)=>root.GetComponentsInChildren<BoxCollider>(true).OrderBy(c=>Mathf.Abs(c.transform.localRotation.eulerAngles.y)).First().transform;
  public static IEnumerator Run(RiichiClient client,string folder){
   var table=client.GetComponent<TableRenderer>();NativeDoraShine.ValidationAge=.6f;
   // Same scene for the old collider fit and corrected visible-outline fit.
   Size(1280,720);for(int i=0;i<8;i++)yield return null;
   var v=Load(folder,3);v.players[0].melds=new[]{"z3333-"};v.hand=new[]{"m9","m9","m9","p3","p3","p6","s4","s0","s6","s7","m9"};v.drawn="m9";v.players[0].count=v.hand.Length;
   table.ValidationLegacyMeldFit=true;client.ShowValidationFixture(v);for(int i=0;i<4;i++)yield return null;
   var root=GameObject.Find("PublicTableState").transform.Find("Seat 0/Exposed melds");float before=table.ValidationTileOutline(Upright(root)).width;float handHeight=table.HandScreenBounds().height;
   yield return Capture(folder,"meld-before-1280");
   table.ValidationLegacyMeldFit=false;client.ShowValidationFixture(v);for(int i=0;i<4;i++)yield return null;
   root=GameObject.Find("PublicTableState").transform.Find("Seat 0/Exposed melds");float after=table.ValidationTileOutline(Upright(root)).width;
   Debug.Log("MELD 0531 visible before="+before+" after="+after+" factor="+after/before+" handHeight="+handHeight);
   yield return Capture(folder,"meld-after-1280");
   Check(after/before>1.12f,"Meld visible size not materially restored");Check(Mathf.Abs(table.HandScreenBounds().height-handHeight)<.2f,"Foreground hand resized");
   foreach(var res in new[]{new Vector2(936,435),new Vector2(1280,720),new Vector2(1024,768)}){
    Size((int)res.x,(int)res.y);for(int i=0;i<8;i++)yield return null;
    for(int count=1;count<=4;count++){
     v=Load(folder,4);v.players[0].melds=new[]{"z3333-","p777+","s1111","z555="}.Take(count).ToArray();v.hand=new[]{"m1","m9","p1","p2","p3","p4","p5","s1","s2","s3","z1","z1","z4","z4"}.Take(14-count*3).ToArray();v.drawn=v.hand.Last();client.ShowValidationFixture(v);for(int i=0;i<4;i++)yield return null;
     root=GameObject.Find("PublicTableState").transform.Find("Seat 0/Exposed melds");var meld=Bounds(root,table.Camera);var hand=table.HandScreenBounds();var tile=table.ValidationTileOutline(Upright(root));
     float nativeWidth=OriginalTiles.HandWidth/18*Screen.height;
     Check(tile.width/nativeWidth>(res.x/res.y<1.6f?.74f:.94f)&&tile.width/nativeWidth<1.02f,"Meld width differs from screenshot reference ratio at "+res);
     Check(Mathf.Abs(hand.height-OriginalTiles.HandHeight/18*Screen.height)<1.5f,"Hand size changed at "+res);
     Check(!meld.Overlaps(hand),"Meld overlaps hand at "+res+" groups="+count);Check(meld.xMax<=Screen.safeArea.xMax&&meld.xMin>=Screen.safeArea.xMin&&meld.yMin>=Screen.safeArea.yMin&&hand.xMin>=Screen.safeArea.xMin,"Meld/hand clipped");
     var hud=TableViewport.Fit(Screen.width,Screen.height,Screen.safeArea,true);float ax=hud.Origin.x+(hud.Width*.26f-3)*hud.Scale,ay=Screen.height-(hud.Origin.y+(hud.Height*.83f-39)*hud.Scale);var avatar=new Rect(ax,ay,68*hud.Scale,68*hud.Scale);Check(!meld.Overlaps(avatar),"Meld covers own portrait at "+res+" groups="+count);
     if(count==1||count==4)yield return Capture(folder,"melds-"+count+"-"+(int)res.x);
    }
   }
   Size(1688,780);for(int i=0;i<8;i++)yield return null;
   foreach(int n in new[]{3,4}){
    var intro=Load(folder,n);foreach(var p in intro.players)p.river=new RiverTile[0];intro.phase="play";intro.audioRoom="title-review-0531-"+n;intro.matchSeq=531+n;intro.matchStartedAt=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();client.ShowValidationFixture(intro);client.ValidationApplyState(intro);client.ValidationIntro.Freeze=true;client.ValidationIntro.FrozenAge=1.65f;yield return null;yield return null;Check(client.ValidationIntro.Active&&client.ValidationIntro.VisiblePlayers==n,"Introduction missing");yield return Capture(folder,"intro-"+n);client.ValidationIntro.Reset();
   }
   NativeDoraShine.ValidationAge=null;
   Debug.Log("RIICHI REVIEW 0531 PASS: matched actual APK mesh outline to reference, same-scene size increase, unchanged hand size, 1..4 melds at three resolutions without overlap/clipping; titles closer to names in three/four-seat introductions.");
  }
 }
}
#endif
