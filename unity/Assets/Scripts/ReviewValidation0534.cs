#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi {
 public static class ReviewValidation0534 {
  static void Check(bool value,string message){if(!value)throw new Exception(message);}
  static void Size(int w,int h){var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(w,h),"0.5.34 riichi draw and scoring"});window.Repaint();}
  static IEnumerator Capture(string folder,string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-0534-"+name+".png"));yield return new WaitForSecondsRealtime(.06f);}
  static RoomView Load(string folder){var v=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"preview-3.json")));v.handSeq=0;v.audioRoom="review-0534";v.audioSeq=100;v.audioEvents=new PublicSound[0];v.options=new Choice[0];v.waitDetails=new WaitDetail[0];v.discardHints=new DiscardHint[0];v.players[0].melds=new string[0];v.players[0].riichi=1;v.players[0].score=17500;v.players[1].score=33500;v.players[2].score=53000;v.players[1].riichi=0;v.turn=0;v.phase="play";v.sticks=1;v.drawn="s4";v.hand=new[]{"p1","p2","p2","p3","p3","p5","p5","p7","p7","s2","s2","s5","s6","s4"};v.options=new[]{new Choice{id="discard:s4_",kind="discard",tile="s4"}};return v;}
  static void NormalHand(TableRenderer table,int count){for(int i=0;i<count;i++)Check(table.ValidationHandDim(i)==0,"Hand dimmed before draw completes: "+i);}
  public static IEnumerator Run(RiichiClient client,string folder){
   Size(1688,780);for(int i=0;i<8;i++)yield return null;var table=client.GetComponent<TableRenderer>();var fx=client.GetComponent<TableEffects>();var v=Load(folder);client.ShowValidationFixture(v);fx.Accept(v);yield return new WaitForSecondsRealtime(.2f);
   var next=Load(folder);next.audioSeq=101;next.audioEvents=new[]{new PublicSound{id=101,kind="discard",seat=2,tile=next.players[2].river.Last().tile,riverIndex=next.players[2].river.Length}};fx.ValidationFreeze=true;fx.ValidationTime=.2f;client.ValidationApplyState(next);fx.Accept(next);yield return null;yield return null;Check(fx.HasPendingActions&&table.DrawDeferred,"Opponent discard not queued before own draw");NormalHand(table,14);yield return Capture(folder,"01-previous-discard-normal");
   // Polling rebuild while the opponent's hand is still moving must not apply grey.
   client.ValidationApplyState(next);fx.Accept(next);yield return null;NormalHand(table,14);Check(table.DrawDeferred,"Polling exposes draw early");fx.ValidationTime=.7f;yield return null;yield return null;Check(table.RevealingDraw,"Missing own drawn-tile reveal");NormalHand(table,14);yield return Capture(folder,"02-own-draw-normal");yield return new WaitForSecondsRealtime(.2f);
   Check(!table.RevealingDraw&&client.OptionsReady,"Draw never finishes");for(int i=0;i<13;i++)Check(table.ValidationHandDim(i)==1,"Locked riichi tile remains selectable colour");Check(table.ValidationHandDim(13)==0,"Legal drawn s4 is grey");var target=GameObject.Find("Camera-facing hand").GetComponentsInChildren<TileTarget>().First(t=>t.Index==13);Check(target.Action=="discard:s4_"&&target.GetComponent<Collider>().enabled,"Drawn s4 is not pickable");yield return Capture(folder,"03-s4-legal");client.SelectTile(13,target.Action);Check(client.SelectedAction=="discard:s4_","Drawn tile selection lost");fx.Reset();fx.ValidationFreeze=false;
   // Exact copies and legacy unique-tile actions retain authoritative option IDs.
   v=Load(folder);Check(TableRenderer.HandChoice(v,"s2",false,false)==null,"Locked copy receives a made-up action");v.options[0].id="discard:s4";Check(TableRenderer.HandChoice(v,"s4",true,true)?.id=="discard:s4","Legacy unique drawn action or stale riichi mode rejects legal s4");client.ShowValidationFixture(v);yield return new WaitForSecondsRealtime(.2f);Check(table.ValidationHandDim(13)==0,"Legacy legal draw is grey");yield return Capture(folder,"04-s4-legacy-legal");
   foreach(bool north in new[]{false,true}){
    v=Load(folder);v.audioRoom="review-0534-special-"+north;v.hand=north?new[]{"p1","p2","p3","p4","p5","p6","s1","s2","s3","z1","z1","z1","z4","z4"}:new[]{"p1","p2","p3","s1","s2","s3","s4","s5","s6","p8","p8","p8","p8","z1"};v.drawn=north?"z4":"z1";v.options=new[]{new Choice{id="discard:"+v.drawn+"_",kind="discard",tile=v.drawn},north?new Choice{id="nuki",kind="nuki"}:new Choice{id="kan:p8888",kind="kan",meld="p8888"}};client.ShowValidationFixture(v);yield return new WaitForSecondsRealtime(.2f);for(int i=0;i<14;i++){bool normal=table.HandFace(i)==(north?"z4":"p8")||i==13;Check(table.ValidationHandDim(i)==(normal?0:1),"Kan/North colour regression");}yield return Capture(folder,"05-special-"+(north?"north":"kan"));
   }
   foreach(int sticks in new[]{0,1,2}){
    v=Load(folder);v.phase="result";v.drawn=null;v.players[0].score=29500+sticks*1000;v.players[1].score=21500;v.sticks=0;v.result=new ResultView{title="荣和",delta=new[]{12000+sticks*1000,-12000,0},details=new[]{new WinDetail{seat=0,from=1,ron=true,tile="s4",tiles=v.hand.Take(13).ToArray(),melds=new string[0],yaku=new[]{new YakuDetail{name="立直",han=1}},score=new ScoreDetail{fu=40,fanshu=6,defen=12000,delta=new[]{12000+sticks*1000,-12000,0}}}}};client.ShowValidationFixture(v);var result=client.ValidationResult;result.Observe(v);result.Freeze=true;result.FrozenAge=100;result.Advance(v);Check(result.ShowingScores,"Score screenshot is not on the score-transfer page");result.FrozenAge=1.05f;yield return null;yield return null;
    Check(ResultPresentation.Before(v,0)==17500&&ResultPresentation.RiichiAward(v,0)==sticks*1000,"Riichi pot double-counted");Check(v.result.delta[1]==-12000&&ResultPresentation.RiichiAward(v,1)==0,"Riichi pot deducted again from loser");Check(ResultPresentation.ScoreBreakdown(v,0)==(sticks==0?null:"和牌 +12000  供托 +"+sticks*1000),"Score breakdown differs from transfer");yield return Capture(folder,"06-scores-pot-"+sticks);
    if(sticks==1){result.FrozenAge=ResultPresentation.ScoreEnd(v.n)+.1f;yield return Capture(folder,"07-scores-final-30500");Check(result.DisplayScore(v,0)==30500,"Final winner score wrong");}
   }
   Debug.Log("RIICHI REVIEW 0534 PASS: no dim during queued opponent discard, polling rebuild or own draw reveal; legal s4 remains bright and pickable; legacy unique action/stale riichi mode, locked copies and available kan/North verified; 0/1/2 riichi pots separate from 12000 payment, loser -12000 and final winner 30500 with one pot.");
  }
 }
}
#endif
