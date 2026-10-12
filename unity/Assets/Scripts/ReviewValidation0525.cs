#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace YunJian.Riichi {
 public static class ReviewValidation0525 {
  static void Check(bool value,string message){if(!value)throw new Exception(message);}
  static RoomView Load(string folder,string name)=>JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,name+".json")));
  static void Size(int w,int h){var game=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));game.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(game,new object[]{new Vector2(w,h),"1011 reference validation"});game.Repaint();}
  static float Pixels(Texture texture){var rt=(RenderTexture)texture;var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();float sum=image.GetPixels().Sum(c=>c.r+c.g+c.b);UnityEngine.Object.Destroy(image);RenderTexture.active=old;return sum;}
  public static IEnumerator Run(RiichiClient client,string folder){
   var table=client.GetComponent<TableRenderer>();var fx=client.GetComponent<TableEffects>();var audio=client.GetComponent<GameAudio>();Size(936,435);for(int i=0;i<7;i++)yield return null;
   // Since 0.5.26 an indicator shines only when it is itself dora: p3 makes p4 dora.
   var v=Load(folder,"preview-4");v.indicators=new[]{"p4","p3"};v.hand=new[]{"m0","p5","p0","s0","s3","s4","s6","s7","s8","z1","z2","z3","z4"};foreach(var p in v.players){p.melds=new string[0];p.nuki=2;}
   client.ShowValidationFixture(v);yield return null;Check(table.IsDora("p5")&&table.IsDora("s0")&&!table.IsDora("s3"),"Dora/red detection is wrong");
   NativeDoraShine.ValidationAge=.05f;float a=Pixels(table.DoraTexture("p4"));NativeDoraShine.ValidationAge=.55f;float b=Pixels(table.DoraTexture("p4"));Check(Mathf.Abs(a-b)>1,"Dora indicator is frozen in a cached texture");
   foreach(int n in new[]{3,4})foreach(int you in Enumerable.Range(0,n)){
    var layout=Load(folder,"preview-"+n);layout.you=you;foreach(var p in layout.players){p.melds=new string[0];p.nuki=3;}client.ShowValidationFixture(layout);yield return null;
    for(int seat=0;seat<n;seat++){var first=table.NorthTarget(seat,1);var second=table.NorthTarget(seat,2);Check(Quaternion.Angle(first.localRotation,Quaternion.identity)<.01f,"North glyph uses the old sideways rotation");Check(second.localPosition.x<first.localPosition.x,"North accumulation is not player-left");var expected=NativeReviewLayout.NorthLocal(NativeReviewLayout.Physical(table.RelativeSeat(seat),n),0,table.SeatAngle(seat));Check(Vector3.Distance(first.localPosition,expected)<.001f,"North anchor differs from APK");}
    if(you==0){ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-0525-north-"+n+".png"));yield return new WaitForSecondsRealtime(.08f);}
   }
   v=Load(folder,"preview-4");v.you=0;v.players[0].melds=new[]{"p777-","m1111"};client.ShowValidationFixture(v);fx.Accept(v);int seq=v.audioSeq;
   var evt=new PublicSound{id=seq+1,seat=0,kind="kan",tile="m1",meld="m1111",meldKind="ankan",meldIndex=1};v.audioSeq++;v.audioEvents=new[]{evt};fx.ValidationFreeze=true;fx.ValidationTime=0;fx.Accept(v);yield return null;
   var group=table.EventMeld(evt);var oldGroup=table.EventMeld(new PublicSound{seat=0,meld="p777-"});Check(!group.group.gameObject.activeSelf&&oldGroup.group.gameObject.activeSelf,"New kan flashes early or hides existing meld");
   foreach(float age in new[]{.05f,.12f,.25f,.36f,.50f}){fx.ValidationTime=age;yield return null;yield return null;Check(oldGroup.group.gameObject.activeSelf,"Previous meld disappeared during kan");ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-0525-kan-"+age.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".png"));yield return new WaitForSecondsRealtime(.08f);}
   Check(group.group.gameObject.activeSelf&&group.group.gameObject.layer==0,"Kan never hands back the complete group");int played=fx.PlayedEvents;table.Render(v);fx.Accept(v);yield return null;Check(fx.PlayedEvents==played,"Polling replays committed kan");fx.Reset();
   foreach(string kind in new[]{"minkan","kakan"}){
    var scene=Load(folder,"preview-4");scene.you=0;scene.players[0].melds=new[]{"p777-",kind=="kakan"?"m333+3":"m1111-"};client.ShowValidationFixture(scene);fx.Accept(scene);var e=new PublicSound{id=scene.audioSeq+1,seat=0,kind="kan",tile=kind=="kakan"?"m3":"m1",meld=scene.players[0].melds[1],meldKind=kind,meldIndex=1,from=3};scene.audioSeq++;scene.audioEvents=new[]{e};fx.ValidationFreeze=true;fx.ValidationTime=0;fx.Accept(scene);yield return null;
    var m=table.EventMeld(e);if(kind=="kakan")Check(m.group.gameObject.activeSelf&&!m.extra.gameObject.activeSelf,"Kakan hides the old pon instead of only its fourth tile");else Check(!m.group.gameObject.activeSelf,"Minkan appears before the hand reveal");
    foreach(float age in new[]{.05f,.12f,.25f,.36f}){fx.ValidationTime=age;yield return null;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-0525-"+kind+"-"+age.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".png"));yield return new WaitForSecondsRealtime(.08f);}
    Check((kind=="kakan"?m.extra:m.group).gameObject.activeSelf,"Kan fourth tile/group never settles");fx.Reset();
   }
   var win=Load(folder,"win-4-tsumo-0-0");win.audioEvents=new PublicSound[0];win.result.matchOver=true;
   foreach(int n in new[]{3,4}){
    var score=JsonUtility.FromJson<RoomView>(JsonUtility.ToJson(win));score.n=n;score.players=score.players.Take(n).ToArray();score.result.delta=n==4?new[]{16000,-16000,0,0}:new[]{12000,-12000,0};for(int i=0;i<n;i++)score.players[i].score=(n==3?35000:25000)+score.result.delta[i];score.result.details[0].seat=0;score.result.details[0].from=1;score.result.details[0].ron=true;score.result.details[0].score.damanguan=0;score.result.details[0].limit="";score.audioRoom="score-review-"+n;
    score.result.details[0].score.delta=score.result.delta;client.ShowValidationFixture(score);var display=client.ValidationResult;display.Observe(score);display.Freeze=true;display.FrozenAge=100;display.Advance(score);
    foreach(float age in new[]{1f,2.55f,2.8f,3.4f}){display.FrozenAge=age;yield return null;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-0525-score-"+n+"-"+age.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".png"));yield return new WaitForSecondsRealtime(.08f);}
    display.Advance(score);Check(display.ShowingFinal,"Match end does not open final standings");display.FrozenAge=3;yield return null;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-0525-final-"+n+".png"));yield return new WaitForSecondsRealtime(.08f);
   }
   for(int from=0;from<4;from++)for(int to=0;to<4;to++)if(from!=to){var arrow=NativeReviewLayout.Arrow(from,to);Check((Mathf.Abs(from-to)==2)==(arrow.size[0]<100),"Straight/bent source arrow mapping differs");}
   foreach(string title in new[]{"累计役满","役满","两倍役满","六倍役满"}){
    win.audioRoom="fire-review-"+title;var d=win.result.details[0];d.limit=title;d.score.damanguan=title=="累计役满"?0:title=="两倍役满"?2:title=="六倍役满"?6:1;d.score.defen=32000*Mathf.Max(1,d.score.damanguan);client.ShowValidationFixture(win);var display=client.ValidationResult;display.Observe(win);display.Freeze=true;
    foreach(float offset in new[]{.35f,.70f,1.3f}){display.FrozenAge=SettlementAudio.Sequence(d).titleAt+offset;yield return null;yield return null;Check(display.LimitFireLayers==6,"Original APK emitter count changed");ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-0525-fire-"+title+"-"+offset.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".png"));yield return new WaitForSecondsRealtime(.08f);}
   }
   NativeDoraShine.ValidationAge=null;Debug.Log("RIICHI REVIEW 0525 PASS: animated dora HUD, red/dora membership, seven rotated sanma/yonma views, native North positions, complete kan group handoff/polling, 12 original arrow transforms, score-before-rank and terminal standings, six original APK fire modules for cumulative/single/multiple yakuman.");
  }
 }
}
#endif
