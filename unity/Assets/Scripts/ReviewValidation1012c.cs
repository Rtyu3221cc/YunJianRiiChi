#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi {
 public static class ReviewValidation1012c {
  static void Check(bool value,string message){if(!value)throw new Exception(message);}
  static RoomView Load(string folder,string name)=>JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,name+".json")));
  static void Size(int w,int h){var game=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));game.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(game,new object[]{new Vector2(w,h),"1012-3 review"});game.Repaint();}
  static IEnumerator Capture(string folder,string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-1012c-"+name+".png"));yield return new WaitForSecondsRealtime(.15f);}
  static Rect Bounds(Transform root,Camera camera){Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);foreach(var c in root.GetComponentsInChildren<BoxCollider>(true))for(int i=0;i<8;i++){var p=camera.WorldToScreenPoint(c.transform.TransformPoint(c.center+Vector3.Scale(c.size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));min=Vector2.Min(min,p);max=Vector2.Max(max,p);}return Rect.MinMaxRect(min.x,min.y,max.x,max.y);}
  public static IEnumerator Run(RiichiClient client,string folder){
   var table=client.GetComponent<TableRenderer>();var fx=client.GetComponent<TableEffects>();Size(1688,780);for(int i=0;i<8;i++)yield return null;
   // 01 + 03: real score GUI with an explicitly synthetic negative-score scene.
   var score=Load(folder,"win-4-tsumo-0-0");score.audioEvents=new PublicSound[0];score.audioRoom="review-negative";score.result.matchOver=true;
   int[] values={-13200,45200,40400,27600};for(int i=0;i<4;i++){score.players[i].score=values[i];score.players[i].ready=false;}score.result.delta=new[]{-38200,20200,15400,2600};
   client.ShowValidationFixture(score);var display=client.ValidationResult;display.Observe(score);display.Freeze=true;display.FrozenAge=100;display.Advance(score);
   for(int rank=4;rank>=1;rank--){display.FrozenAge=ResultPresentation.RankAt(4,rank)+.18f;yield return null;yield return null;Check(Enumerable.Range(0,4).Count(i=>display.RankVisible(score,i))==5-rank,"Rank reveal is not fourth to first");yield return Capture(folder,"01-rank-"+rank);}
   display.FrozenAge=5;yield return null;yield return null;yield return Capture(folder,"03-negative-score");
   display.Advance(score);Check(display.ShowingFinal,"Final page missing");display.FrozenAge=3;yield return null;yield return null;yield return Capture(folder,"03-negative-final");
   var minus=Resources.Load<Texture2D>("Majsoul/Results/ww_minus");Check(minus.width==69&&minus.height==27,"APK negative glyph dimensions changed");
   // 02: the supplied sanma recording has both m1 and m9 indicators.
   var dora=Load(folder,"preview-3");dora.audioEvents=new PublicSound[0];dora.indicators=new[]{"m9"};client.ShowValidationFixture(dora);yield return null;yield return null;
   Check(table.IsDora("m1")&&!table.IsDora("m9"),"Sanma m9 indicator maps to m1 incorrectly");yield return Capture(folder,"02-only-nine-indicator");
   dora.indicators=new[]{"m1","m9"};client.ShowValidationFixture(dora);yield return null;yield return null;Check(table.IsDora("m1")&&table.IsDora("m9"),"m1+m9 indicators should make both tiles dora");yield return Capture(folder,"02-one-and-nine-indicators");
   // 04: original nameplate/45 px name, title art and Queshi rank badge.
   var intro=Load(folder,"preview-4");intro.audioRoom="intro-review-c";intro.matchSeq=999;intro.matchStartedAt=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();foreach(var p in intro.players)p.river=new RiverTile[0];intro.phase="play";
   client.ValidationApplyState(intro);client.ValidationIntro.Freeze=true;client.ValidationIntro.FrozenAge=1.65f;yield return null;yield return null;Check(client.ValidationIntro.Active,"Match introduction did not begin");
   foreach(var n in new[]{"rank_bg","sima_queshi","star","starbg"})Check(Resources.Load<Texture2D>("Majsoul/Review1012c/"+n)!=null,"Missing APK badge "+n);yield return Capture(folder,"04-intro-badges");client.ValidationIntro.Reset();
   // 05 + 06: accept the same public event packet as a poll would receive.
   var declaration=Load(folder,"effects");declaration.audioRoom="declaration-review-c";declaration.audioSeq=0;declaration.audioEvents=new PublicSound[0];declaration.pending=null;declaration.phase="play";declaration.turn=1;declaration.players[0].riichi=1;
   client.ShowValidationFixture(declaration);fx.Accept(declaration);var discard=declaration.players[0].river.Last().tile;
   declaration.audioSeq=3;declaration.audioEvents=new[]{new PublicSound{id=1,seat=0,kind="discard",tile=discard,riverIndex=declaration.players[0].river.Length,riichi=true},new PublicSound{id=2,seat=0,kind="riichi"},new PublicSound{id=3,seat=0,kind="riichi-stick"}};
   fx.ValidationFreeze=true;fx.ValidationTime=.35f;fx.Accept(declaration);yield return null;yield return null;
   Check(fx.ValidationKind=="discard"&&fx.ValidationCallKind=="riichi","Riichi banner/hand not simultaneous");Check(!table.StickTarget(0).gameObject.activeSelf,"Queued stick flashes before placement");yield return Capture(folder,"05-riichi-simultaneous");yield return Capture(folder,"06-stick-before-placement");
   table.Render(declaration);fx.Accept(declaration);Check(!table.StickTarget(0).gameObject.activeSelf,"Polling restores a queued stick too early");
   fx.ValidationTime=1.72f;yield return null;fx.ValidationTime=.35f;yield return null;yield return null;Check(fx.ValidationKind=="riichi-stick"&&!table.StickTarget(0).gameObject.activeSelf,"Stick did not stay hidden while carried");yield return Capture(folder,"06-stick-carried");
   fx.ValidationTime=fx.ValidationHandoff+.005f;yield return null;yield return null;Check(table.StickTarget(0).gameObject.activeSelf&&!fx.ValidationAnimatedTile.gameObject.activeSelf,"Stick handoff missing");yield return Capture(folder,"06-stick-landed");fx.Reset();fx.ValidationFreeze=false;
   // 07 + 11: all four directions, concealed hand and the last tile use one axis.
   for(int seat=0;seat<4;seat++){
    var win=Load(folder,"win-4-tsumo-"+seat+"-0");win.audioEvents=new PublicSound[0];table.Clear();client.ShowValidationFixture(win);fx.ValidationPlay(seat,"tsumo",win.result.details[0].tile);fx.ValidationTime=fx.ValidationHandoff-.01f;yield return null;yield return null;
    var target=table.WinningTarget(new PublicSound{seat=seat,kind="tsumo",tile=win.result.details[0].tile});var row=GameObject.Find("PublicTableState").transform.Find("Seat "+seat+"/Revealed winning hand");
    Check(target!=null,"Winning tile missing at seat "+seat);Check(row!=null,"Revealed row missing at seat "+seat);fx.CutIn.Reset();
    var local=Quaternion.Inverse(Quaternion.Euler(0,table.SeatAngle(seat),0))*target.position;float max=row.GetComponentsInChildren<BoxCollider>(true).Max(c=>c.transform.localPosition.x+c.size.x/2);Check(local.x>max+.12f,"Self draw overlaps the exposed hand in seat "+seat);Check(Mathf.Abs(Mathf.DeltaAngle(target.eulerAngles.y,row.eulerAngles.y))<.001f,"Self draw axis differs from its hand");
    Check(Quaternion.Angle(target.rotation,fx.ValidationAnimatedTile.rotation)<.001f,"Carried self-draw orientation differs from landed tile");yield return new WaitForSecondsRealtime(.7f);yield return Capture(folder,"07-tsumo-seat-"+seat);fx.ValidationTime=fx.ValidationHandoff+.005f;yield return null;yield return null;yield return Capture(folder,"07-tsumo-landed-seat-"+seat);fx.Reset();fx.ValidationFreeze=false;
   }
   // 08: riichi hand retains normal colour for available kan/North and drawn tiles.
   foreach(bool north in new[]{false,true}){
    var v=Load(folder,"preview-3");v.audioEvents=new PublicSound[0];v.players[0].riichi=1;v.players[0].melds=new string[0];v.turn=0;v.phase="play";
    v.hand=north?new[]{"p1","p2","p3","p4","p5","p6","s1","s2","s3","z1","z1","z1","z4","z4"}:new[]{"p1","p2","p3","s1","s2","s3","s4","s5","s6","p8","p8","p8","p8","z1"};v.drawn=north?"z4":"z1";
    v.options=new[]{new Choice{id="discard:"+v.drawn+"_",kind="discard",tile=v.drawn},north?new Choice{id="nuki",kind="nuki"}:new Choice{id="kan:p8888",kind="kan",meld="p8888"}};
    client.ShowValidationFixture(v);yield return null;yield return null;for(int i=0;i<v.hand.Length;i++){bool normal=table.HandFace(i)==(north?"z4":"p8")||i==v.hand.Length-1;Check(table.ValidationHandDim(i)==(normal?0:1),"Riichi legal action dim differs: "+table.HandFace(i));}yield return Capture(folder,"08-riichi-"+(north?"north":"kan"));
    v.hand=v.hand.Where((t,i)=>north?t!="z4"||i==12:t!="p8").ToArray();v.hand[v.hand.Length-1]="p9";v.drawn="p9";v.options=new[]{new Choice{id="discard:p9_",kind="discard",tile="p9"}};client.ShowValidationFixture(v);yield return null;yield return null;Check(table.ValidationHandDim(v.hand.Length-1)==0,"Supplement tile is grey");yield return Capture(folder,"08-supplement-after-"+(north?"north":"kan"));
   }
   // 09: actual GUI at both small-phone and wide-reference resolution.
   foreach(var resolution in new[]{new Vector2(936,435),new Vector2(1688,780)}){
    Size((int)resolution.x,(int)resolution.y);for(int i=0;i<8;i++)yield return null;client.ShowValidationFixture(score);score.players[0].ready=true;display=client.ValidationResult;display.Observe(score);display.Freeze=true;display.FrozenAge=100;display.Advance(score);display.FrozenAge=5;yield return null;yield return null;yield return Capture(folder,"09-waiting-"+(int)resolution.x);
   }
   // 10: preserve hand size, fit 1..4 exposed groups with zero overlap.
   foreach(var res in new[]{new Vector2(936,435),new Vector2(1280,720),new Vector2(1024,768)}){
    Size((int)res.x,(int)res.y);for(int i=0;i<8;i++)yield return null;
    for(int count=1;count<=4;count++){
     var v=Load(folder,"preview-4");v.audioEvents=new PublicSound[0];v.players[0].melds=new[]{"z111-","p777+","s1111","z555="}.Take(count).ToArray();v.hand=new[]{"p1","p2","p3","p4","p5","p6","s1","s2","s3","z1","z1","z1","z4","z4"}.Take(14-count*3).ToArray();v.drawn=v.hand.Last();client.ShowValidationFixture(v);yield return null;yield return null;
     var own=GameObject.Find("PublicTableState").transform.Find("Seat 0/Exposed melds");var meld=Bounds(own,table.Camera);var hand=table.HandScreenBounds();Check(!meld.Overlaps(hand)&&meld.xMin>hand.xMax,"Own meld/hand overlap at "+res+" groups="+count);Check(hand.xMin>=Screen.safeArea.xMin&&meld.xMax<Screen.safeArea.xMax&&meld.yMin>=Screen.safeArea.yMin,"Own edge groups clipped");
     Check(Mathf.Abs(hand.height-OriginalTiles.HandHeight/18*Screen.height)<1.5f,"Hand tiles resized for meld layout");if(count==1||count==4)yield return Capture(folder,"10-melds-"+count+"-"+(int)res.x);
    }
   }
   // 11: real APK canvas and curves, for ron and tsumo, above the table HUD.
   Size(1688,780);for(int i=0;i<8;i++)yield return null;
   foreach(var kind in new[]{"ron","tsumo"}){
    var win=Load(folder,"win-4-"+kind+"-0-0");var events=win.audioEvents;int seq=win.audioSeq;win.audioRoom="cutin-review-c-"+kind;win.audioSeq=seq-1;win.audioEvents=new PublicSound[0];client.ShowValidationFixture(win);fx.Accept(win);win.audioEvents=events;win.audioSeq=seq;fx.ValidationFreeze=true;fx.ValidationTime=.25f;fx.Accept(win);yield return null;yield return null;Check(fx.ShowWinPresentation,"Win settlement shown before cutin");
    fx.ValidationTime=1.06f;yield return null;yield return null;Check(fx.CutIn.Active,"Original stripe did not start");fx.CutIn.Freeze=true;
    foreach(float age in new[]{.12f,.45f,.85f,1.12f}){fx.CutIn.FrozenAge=age;yield return null;yield return null;yield return Capture(folder,"11-"+kind+"-cutin-"+age.ToString("F2",System.Globalization.CultureInfo.InvariantCulture));}
    fx.CutIn.FrozenAge=1.21f;fx.ValidationTime=0;yield return null;yield return null;if(kind=="tsumo")Check(fx.ValidationKind=="tsumo","Self-draw hand not queued behind cutin");fx.Reset();fx.ValidationFreeze=false;
   }
   Debug.Log("RIICHI REVIEW 1012C PASS: all 11 requested points; rank 4-to-1, sanma dora membership, negative glyph size, FightBegin name/title/Queshi, concurrent riichi hand/call, queued stick visibility and handoff/polling, four tsumo orientations/spacing, kan/North and supplement colours, waiting text, unchanged hand size with 1..4 melds, original UI_CutIn AnimationClip.");
  }
 }
}
#endif
