#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi {
 public static class ReviewValidation0533 {
  static void Size(int w,int h){var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(w,h),"0.5.33 hand centering and meld position"});window.Repaint();}
  static IEnumerator Capture(string folder,string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-0533-"+name+".png"));yield return new WaitForSecondsRealtime(.08f);}
  static void Check(bool value,string message){if(!value)throw new Exception(message);}
  static RoomView Load(string folder,int n){var v=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"preview-"+n+".json")));v.audioEvents=new PublicSound[0];v.options=new Choice[0];v.waitDetails=new WaitDetail[0];v.discardHints=new DiscardHint[0];v.handSeq=0;v.drawn=null;v.turn=1;foreach(var p in v.players){p.melds=new string[0];p.nuki=0;}v.hand=new[]{"p1","p3","p5","p6","p8","p9","s1","s2","s3","s5","s6","z1","z1"};return v;}
  static Transform Meld()=>GameObject.Find("PublicTableState").transform.Find("Seat 0/Exposed melds");
  static Transform Upright(Transform root)=>root.GetComponentsInChildren<BoxCollider>(true).OrderBy(c=>Mathf.Abs(c.transform.localRotation.eulerAngles.y)).First().transform;
  static Rect Bounds(Transform root,Camera camera){Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);foreach(var c in root.GetComponentsInChildren<BoxCollider>(true))for(int i=0;i<8;i++){var p=camera.WorldToScreenPoint(c.transform.TransformPoint(c.center+Vector3.Scale(c.size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));min=Vector2.Min(min,p);max=Vector2.Max(max,p);}return Rect.MinMaxRect(min.x,min.y,max.x,max.y);}
  public static IEnumerator Run(RiichiClient client,string folder){
   var table=client.GetComponent<TableRenderer>();var metrics=new StringBuilder();Size(1751,799);for(int i=0;i<8;i++)yield return null;
   var v=Load(folder,3);table.ValidationSafeArea=new Rect(90,0,1661,799);table.ValidationPrevious0533Layout=true;client.ShowValidationFixture(v);for(int i=0;i<4;i++)yield return null;
   float axis=table.Camera.WorldToScreenPoint(Vector3.zero).x;var before=table.HandScreenBounds();Check(Mathf.Abs(before.center.x-axis-45)<.1f,"Old asymmetric-safe-area offset not reproduced");yield return Capture(folder,"01-hand-before-left-safe");
   table.ValidationPrevious0533Layout=false;client.ShowValidationFixture(v);for(int i=0;i<4;i++)yield return null;var after=table.HandScreenBounds();Check(Mathf.Abs(after.center.x-axis)<.1f,"Hand does not share the cloth/hub axis");Check(Mathf.Abs(after.width-before.width)<.1f&&Mathf.Abs(after.height-before.height)<.1f,"Center correction resizes the hand");metrics.AppendLine("Asymmetric left safe area 90px: old hand offset="+(before.center.x-axis)+"px; new offset="+(after.center.x-axis)+"px; unchanged dimensions="+after.size);yield return Capture(folder,"02-hand-after-left-safe");
   table.ValidationSafeArea=new Rect(0,0,1661,799);client.ShowValidationFixture(v);for(int i=0;i<4;i++)yield return null;Check(Mathf.Abs(table.HandScreenBounds().center.x-axis)<.1f,"Right safe inset moves hand off the table axis");yield return Capture(folder,"03-hand-after-right-safe");
   table.ValidationSafeArea=null;
   foreach(var res in new[]{new Vector2(936,435),new Vector2(1280,720),new Vector2(1751,799),new Vector2(1024,768)}){
    Size((int)res.x,(int)res.y);for(int i=0;i<8;i++)yield return null;
    foreach(int n in new[]{3,4})foreach(bool drawn in new[]{false,true}){
     v=Load(folder,n);if(drawn){v.hand=v.hand.Concat(new[]{"m9"}).ToArray();v.drawn="m9";v.turn=0;}client.ShowValidationFixture(v);for(int i=0;i<4;i++)yield return null;var hand=table.HandScreenBounds();axis=table.Camera.WorldToScreenPoint(Vector3.zero).x;Check(Mathf.Abs(hand.center.x-axis)<.1f,"Unobstructed 13/14-tile hand not centered at "+res+" n="+n);Check(hand.xMin>=Screen.safeArea.xMin&&hand.xMax<=Screen.safeArea.xMax,"Centered hand clips");
     if(res.x==1280&&n==4&&drawn)yield return Capture(folder,"04-hand-14-1280");
    }
    v=Load(folder,3);v.players[0].melds=new[]{"z3333-"};v.hand=v.hand.Take(11).ToArray();v.drawn=v.hand.Last();
    table.ValidationPrevious0533Layout=true;client.ShowValidationFixture(v);for(int i=0;i<4;i++)yield return null;var oldBounds=Bounds(Meld(),table.Camera);var oldTile=table.ValidationTileOutline(Upright(Meld()));float handHeight=table.HandScreenBounds().height;if(res.x==936||res.x==1280)yield return Capture(folder,"05-meld-before-"+(int)res.x);
    table.ValidationPrevious0533Layout=false;client.ShowValidationFixture(v);for(int i=0;i<4;i++)yield return null;var newBounds=Bounds(Meld(),table.Camera);var newTile=table.ValidationTileOutline(Upright(Meld()));
    // On 4:3 the existing overlap fallback lifts both versions above the hand.
    // The requested corner offset applies when the horizontal layout fits.
    if(res.x/res.y>1.6f)Check(Mathf.Abs(oldBounds.yMin-newBounds.yMin-8)<.2f,"Meld downshift differs from 8px at "+res+": "+(oldBounds.yMin-newBounds.yMin));
    if(res.x/res.y>1.6f)Check(Mathf.Abs(oldTile.width-newTile.width)<.2f,"Meld resizes while moving down");
    Check(Mathf.Abs(handHeight-table.HandScreenBounds().height)<.1f,"Hand resizes while moving melds");metrics.AppendLine(res+": meld down="+(oldBounds.yMin-newBounds.yMin)+"px; old/new visible tile width="+oldTile.width+"/"+newTile.width+"px"+(res.x/res.y<=1.6f?"; narrow-viewport overlap fallback":""));if(res.x==936||res.x==1280)yield return Capture(folder,"06-meld-after-"+(int)res.x);
    for(int count=1;count<=4;count++){
     v=Load(folder,4);v.players[0].melds=new[]{"z3333-","p777+","s1111","z555="}.Take(count).ToArray();v.hand=new[]{"m1","m9","p1","p2","p3","p4","p5","s1","s2","s3","z1","z1","z4","z4"}.Take(14-count*3).ToArray();v.drawn=v.hand.Last();client.ShowValidationFixture(v);for(int i=0;i<4;i++)yield return null;var meld=Bounds(Meld(),table.Camera);var hand=table.HandScreenBounds();Check(!meld.Overlaps(hand),"Meld overlaps hand at "+res+" groups="+count);Check(meld.yMin>=Screen.safeArea.yMin&&meld.xMin>=Screen.safeArea.xMin&&meld.xMax<=Screen.safeArea.xMax&&hand.xMin>=Screen.safeArea.xMin,"Meld/hand clipped at "+res);Check(Mathf.Abs(hand.height-OriginalTiles.HandHeight/18*Screen.height)<1.5f,"Hand shrunk to avoid melds");
     var hud=TableViewport.Fit(Screen.width,Screen.height,Screen.safeArea,true);float ax=hud.Origin.x+(hud.Width*.26f-3)*hud.Scale,ay=Screen.height-(hud.Origin.y+(hud.Height*.83f-39)*hud.Scale);Check(!meld.Overlaps(new Rect(ax,ay,68*hud.Scale,68*hud.Scale)),"Meld covers own portrait");if(count==4&&(res.x==936||res.x==1280))yield return Capture(folder,"07-four-melds-"+(int)res.x);
    }
   }
   File.WriteAllText(Path.Combine(folder,"review-0533-metrics.txt"),metrics.ToString());Debug.Log(metrics.ToString());Debug.Log("RIICHI REVIEW 0533 PASS: reproduced asymmetric-safe-area offset; hand shares hub axis with unchanged size, 13/14 tiles in 3/4-seat games at four resolutions; own meld down exactly 8px at three landscape resolutions with unchanged visible width, narrow viewport overlap fallback retained; 1..4 melds without hand/portrait overlap or clipping.");
  }
 }
}
#endif
