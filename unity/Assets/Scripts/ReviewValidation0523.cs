#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace YunJian.Riichi
{
    public static class ReviewValidation0523
    {
        static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
        static void Resolution(int w,int h){var game=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));game.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(game,new object[]{new Vector2(w,h),"Review validation"});game.Repaint();}
        static RoomView Load(string folder,string file)=>JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,file+".json")));
        public static IEnumerator Run(RiichiClient client,string folder)
        {
            var table=client.GetComponent<TableRenderer>();var fx=client.GetComponent<TableEffects>();Resolution(936,435);for(int f=0;f<6;f++)yield return null;
            // Actual before/after result state; losing foreground hand must survive rebuilding.
            var losing=Load(folder,"win-4-tsumo-1-0");var before=JsonUtility.FromJson<RoomView>(JsonUtility.ToJson(losing));before.phase="play";before.result=null;before.audioEvents=new PublicSound[0];before.audioSeq=0;before.audioRoom="losing-hand-review";
            client.ShowValidationFixture(before);yield return null;var hand=GameObject.Find("Camera-facing hand");Check(hand!=null && hand.transform.childCount==losing.hand.Length,"Before-win own hand missing");
            losing.audioRoom=before.audioRoom;client.ValidationApplyState(losing);Check(GameObject.Find("Camera-facing hand")==hand && table.HandScreenBounds().height>20,"Losing hand was destroyed at opponent win");
            fx.ValidationFreeze=true;fx.ValidationTime=.5f;fx.Accept(before);fx.Accept(losing);yield return null;yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-0523-opponent-win-hand.png"));yield return new WaitForSecondsRealtime(.1f);
            // scene_mj babei_n carries the same rotation as that seat's river anchor hai_n,
            // so each North faces its owner like a discard; position is the APK anchor.
            var layout=Load(folder,"effects");foreach(var p in layout.players)p.nuki=3;client.ShowValidationFixture(layout);yield return null;
            for(int seat=0;seat<layout.n;seat++){var tile=table.NorthTarget(seat,1);var expected=NativeReviewLayout.NorthLocal(NativeReviewLayout.Physical(table.RelativeSeat(seat),layout.n),0,table.SeatAngle(seat));Check(Quaternion.Angle(tile.localRotation,Quaternion.identity)<.01f && Vector3.Distance(tile.localPosition,expected)<.001f,"North anchor/yaw differs from APK babei/hai");}
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-0523-north.png"));yield return new WaitForSecondsRealtime(.1f);
            // Same material/native plane in all winning directions; its column faces the camera.
            for(int seat=0;seat<4;seat++){
                var win=Load(folder,"win-4-tsumo-"+seat+"-0");win.audioRoom="review-facing-"+seat;var events=win.audioEvents;int seq=win.audioSeq;win.audioEvents=new PublicSound[0];win.audioSeq=seq-1;client.ShowValidationFixture(win);yield return null;
                fx.ValidationFreeze=true;fx.ValidationTime=.2f;fx.Accept(win);win.audioEvents=events;win.audioSeq=seq;fx.Accept(win);fx.ValidationTime=1.55f;yield return null;yield return null;
                fx.ValidationTime=1.06f;yield return null;fx.CutIn.Freeze=true;fx.CutIn.FrozenAge=WinCutIn.Duration+.01f;fx.ValidationTime=0;yield return null;yield return null;fx.ValidationTime=1.55f;yield return null;yield return null;
                var effect=fx.ValidationWin;Check(effect!=null,"Winning effect not queued after APK cutin");fx.ValidationTime=effect.StartedAt+.45f;yield return null;
                var ray=effect.transform.position-table.Camera.transform.position;ray.y=0;Check(Vector3.Dot(effect.transform.forward,ray.normalized)>.999f,"Side winner has an edge-on fire sheet");
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-0523-fire-seat-"+seat+".png"));yield return new WaitForSecondsRealtime(.1f);
            }
            // Gate a real reaction UI behind an unfinished discard, including the call banner.
            var reaction=Load(folder,"effects");reaction.audioRoom="review-ordered";reaction.audioEvents=new PublicSound[0];reaction.audioSeq=0;client.ShowValidationFixture(reaction);yield return null;
            fx.Accept(reaction);var tileId=reaction.players[1].river.Last().tile;reaction.audioEvents=new[]{new PublicSound{id=1,seat=1,kind="discard",tile=tileId,riverIndex=reaction.players[1].river.Length},new PublicSound{id=2,seat=0,kind="pon"}};reaction.audioSeq=2;
            fx.ValidationFreeze=true;fx.ValidationTime=.2f;fx.Accept(reaction);yield return null;
            Check(!client.OptionsReady && fx.ValidationCallKind==null && fx.ValidationKind=="discard","Call started before previous discard finished");
            fx.ValidationTime=.65f;yield return null;yield return null;Check(fx.ValidationCallKind=="pon" && !client.OptionsReady,"Queued meld did not follow previous hand");fx.Reset();yield return null;Check(client.OptionsReady,"Actions never unlocked after animation");
            Check(HandRevealMotion.Progress(.12f)<.08f && HandRevealMotion.Progress(.36f)>1 && HandRevealMotion.Progress(.44f)<1,"Push-down lacks acceleration/contact/settle");
            Debug.Log("RIICHI REVIEW 0523 PASS: preserved losing hand, every North at its APK anchor facing its owner, camera-facing fire in all four seats, chronological calls and gated options, nonlinear reveal.");
            Resolution(754,347);for(int f=0;f<7;f++)yield return null;
            var data=Load(folder,"win-4-tsumo-0-0");data.audioEvents=new PublicSound[0];var d=data.result.details[0];
            var limits=new[]{"普通和牌","满贯","跳满","倍满","三倍满","累计役满","役满","两倍役满","三倍役满","四倍役满","五倍役满","六倍役满"};var points=new[]{1000,8000,12000,16000,24000,32000,32000,64000,96000,128000,160000,192000};
            for(int i=0;i<limits.Length;i++){
                data.audioRoom="review-limit-"+i;d.limit=i==0?"":limits[i];d.score=new ScoreDetail{fanshu=i<6?Mathf.Max(1,i*2+2):0,fu=40,defen=points[i],damanguan=i>=6?i-5:0};d.yaku=new[]{new YakuDetail{name=i>=6?"字一色":"清一色",han=i>=6?0:6,yakuman=i>=6?i-5:0}};
                client.ShowValidationFixture(data);var presentation=client.ValidationResult;presentation.Observe(data);presentation.Freeze=true;presentation.FrozenAge=100;yield return null;yield return null;
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-0523-limit-"+i.ToString("D2")+".png"));yield return new WaitForSecondsRealtime(.08f);
            }
            Debug.Log("RIICHI LIMIT GALLERY PASS: ordinary, all mangan levels, accumulated yakuman and 1-6 yakuman; actual 754x347 rendered source glyphs.");
        }
    }
}
#endif
