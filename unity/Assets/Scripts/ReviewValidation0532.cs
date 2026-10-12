#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi {
 public static class ReviewValidation0532 {
  static void Check(bool v,string m){if(!v)throw new Exception(m);}
  static RoomView Load(string folder,string name)=>JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,name+".json")));
  static void Size(int w,int h){var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(w,h),"0.5.32 reveal/audio/meld"});window.Repaint();}
  static IEnumerator Capture(string folder,string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-0532-"+name+".png"));yield return new WaitForSecondsRealtime(.08f);}
  public static IEnumerator Run(RiichiClient client,string folder){
   var table=client.GetComponent<TableRenderer>();var fx=client.GetComponent<TableEffects>();var audio=client.GetComponent<GameAudio>();audio.Configure(true,.2f,true,1);
   Size(1688,780);for(int i=0;i<8;i++)yield return null;
   foreach(var sound in new[]{"cutin","winlose"}){var clip=Resources.Load<AudioClip>("Majsoul/Audio/"+sound);Check(clip!=null&&clip.length>1&&clip.samples>40000,"Missing/empty original APK sound "+sound);}
   foreach(string kind in new[]{"ron","tsumo"}){
    var win=Load(folder,"win-3-"+kind+"-0-0");win.audioRoom="reveal-review-0532-"+kind;
    var before=Load(folder,"preview-3");before.audioRoom=win.audioRoom;before.audioSeq=win.audioSeq-1;before.audioEvents=new PublicSound[0];before.hand=win.result.details[0].tiles.Concat(new[]{win.result.details[0].tile}).ToArray();before.players[0].melds=new string[0];before.phase="play";before.drawn=win.result.details[0].tile;before.options=new Choice[0];
    client.ShowValidationFixture(before);fx.Accept(before);yield return null;Check(table.ValidationStandingHandVisible,"Pre-win hand missing");
    int cutins=audio.PlayedCues.Count(s=>s=="cutin");fx.ValidationFreeze=true;fx.ValidationTime=.25f;client.ValidationApplyState(win);fx.Accept(win);Check(!table.ValidationWinningRowVisible,"Pushed-down hand flashes on result acceptance");
    yield return Capture(folder,"01-"+kind+"-before-hide");bool captured=false;
    for(int frame=0;frame<65;frame++){
     if(frame==8)fx.ValidationTime=1.06f;
     if(frame==10){Check(fx.CutIn.Active,"Win banner missing");fx.CutIn.Freeze=true;fx.CutIn.FrozenAge=.45f;Check(audio.PlayedCues.Count(s=>s=="cutin")==cutins+1&&audio.EffectsPlaying,"Cut-in sound did not play exactly once");yield return Capture(folder,"02-"+kind+"-cutin-sound");}
     if(frame==15){fx.CutIn.FrozenAge=1.21f;fx.ValidationTime=0;}
     if(frame==40&&kind=="tsumo")fx.ValidationTime=fx.ValidationHandoff+.005f;
     yield return null;
     Check(!(table.ValidationStandingHandVisible&&table.ValidationWinningRowVisible),"Duplicate standing/pushed hand in "+kind+" frame "+frame);
     if(!captured&&table.ValidationWinningRowVisible){captured=true;yield return Capture(folder,"01-"+kind+"-after-hide");}
    }
    Check(captured&&!table.ValidationStandingHandVisible,"Winning hand never transitions to revealed row");table.Render(win);fx.Accept(win);yield return null;Check(!table.ValidationStandingHandVisible&&table.ValidationWinningRowVisible,"Polling restored the winning standing hand");Check(audio.PlayedCues.Count(s=>s=="cutin")==cutins+1,"Polling duplicated cut-in sound");fx.Reset();fx.ValidationFreeze=false;
   }
   // A losing player's own foreground hand stays visible.
   var other=Load(folder,"win-3-tsumo-1-0");other.audioEvents=new PublicSound[0];table.Clear();client.ShowValidationFixture(other);yield return null;yield return null;Check(table.ValidationStandingHandVisible,"Opponent win hides the losing player's hand");
   int starts=audio.PlayedCues.Count(s=>s=="winlose");var intro=Load(folder,"preview-4");intro.audioRoom="intro-sound-0532";intro.matchSeq=532;intro.matchStartedAt=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();foreach(var p in intro.players)p.river=new RiverTile[0];intro.phase="play";
   client.ShowValidationFixture(intro);client.ValidationApplyState(intro);client.ValidationIntro.Freeze=true;client.ValidationIntro.FrozenAge=.8f;yield return null;yield return null;Check(audio.PlayedCues.Count(s=>s=="winlose")==starts,"Intro sound starts before victory title");audio.Suspend(true);client.ValidationIntro.FrozenAge=1.3f;yield return null;yield return null;Check(audio.PlayedCues.Count(s=>s=="winlose")==starts,"Suspended intro audio accepted prematurely");audio.Suspend(false);yield return null;yield return null;Check(audio.PlayedCues.Count(s=>s=="winlose")==starts+1&&audio.EffectsPlaying,"Original FightBegin sound missing after resume");client.ValidationIntro.FrozenAge=1.65f;yield return Capture(folder,"03-intro-sound");client.ValidationApplyState(intro);yield return null;Check(audio.PlayedCues.Count(s=>s=="winlose")==starts+1,"Polling duplicates introduction sound");client.ValidationIntro.Reset();
   foreach(var res in new[]{new Vector2(936,435),new Vector2(1280,720)}){
    Size((int)res.x,(int)res.y);for(int i=0;i<8;i++)yield return null;var v=Load(folder,"preview-3");v.players[0].melds=new[]{"z3333-"};v.hand=v.hand.Take(11).ToArray();v.drawn=v.hand.Last();v.audioEvents=new PublicSound[0];v.options=new Choice[0];
    table.ValidationPreviousMeldPosition=true;client.ShowValidationFixture(v);for(int i=0;i<4;i++)yield return null;var root=GameObject.Find("PublicTableState").transform.Find("Seat 0/Exposed melds");var tile=root.GetComponentsInChildren<BoxCollider>().OrderBy(c=>c.transform.localRotation.eulerAngles.y).First().transform;var a=table.ValidationTileOutline(tile);var hand=table.HandScreenBounds();yield return Capture(folder,"04-before-"+(int)res.x);
    table.ValidationPreviousMeldPosition=false;client.ShowValidationFixture(v);for(int i=0;i<4;i++)yield return null;root=GameObject.Find("PublicTableState").transform.Find("Seat 0/Exposed melds");tile=root.GetComponentsInChildren<BoxCollider>().OrderBy(c=>c.transform.localRotation.eulerAngles.y).First().transform;var b=table.ValidationTileOutline(tile);Check(Mathf.Abs(a.yMin-b.yMin-7)<2,"Meld does not move down seven pixels: "+(a.yMin-b.yMin));Check(Mathf.Abs(a.width-b.width)<.7f&&Mathf.Abs(hand.height-table.HandScreenBounds().height)<.2f,"Meld/hand size changed while moving down");yield return Capture(folder,"04-after-"+(int)res.x);
   }
   Debug.Log("RIICHI REVIEW 0532 PASS: no simultaneous standing/revealed own hand across ron/tsumo and polling; losing hand retained; actual APK cutin/winlose clips play once, resources present; own meld down seven pixels with unchanged size at two resolutions.");
  }
 }
}
#endif
