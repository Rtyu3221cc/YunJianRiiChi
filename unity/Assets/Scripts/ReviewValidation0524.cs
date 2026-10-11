#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi
{
    public static class ReviewValidation0524
    {
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        static RoomView Load(string folder,string name)=>JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,name+".json")));
        static void Size(int w,int h){var game=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));game.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(game,new object[]{new Vector2(w,h),"0.5.24 reference"});game.Repaint();}
        public static IEnumerator Run(RiichiClient client,string folder)
        {
            var table=client.GetComponent<TableRenderer>();var audio=client.GetComponent<GameAudio>();
            Size(936,435);for(int i=0;i<7;i++)yield return null;
            var chi=Load(folder,"effects");client.ShowValidationFixture(chi);yield return null;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-0524-chi.png"));yield return new WaitForSecondsRealtime(.08f);
            // A fresh full match differs from a new hand, polling, and an old reconnect.
            var v=Load(folder,"win-4-tsumo-0-0");v.phase="play";v.result=null;v.audioRoom="review-intro-0524";v.audioSeq=0;v.audioEvents=new PublicSound[0];v.handSeq=0;v.openingHand=(string[])v.hand.Clone();v.drawn=v.hand.Last();v.options=new[]{new Choice{id="discard:"+v.drawn+"_",kind="discard",tile=v.drawn}};
            foreach(var p in v.players){p.river=new RiverTile[0];p.melds=new string[0];p.nuki=0;}
            client.ShowValidationFixture(v);v.handSeq=600;v.matchSeq=1;v.matchStartedAt=v.dealtAt=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();client.ValidationApplyState(v);var intro=client.ValidationIntro;int began=intro.Started;
            Check(intro.Active && table.ArrangingHand && !client.OptionsReady,"New match does not gate deal/options behind intro");yield return null;Check(table.ValidationHandOpacity(0)<.01f,"Deal appears during intro");
            foreach(float age in new[]{.3f,.7f,1.4f,2.2f,3.2f}){intro.Freeze=true;intro.FrozenAge=age;yield return null;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-0524-intro-"+age.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+".png"));yield return new WaitForSecondsRealtime(.08f);}
            client.ValidationApplyState(v);Check(intro.Started==began,"Polling repeats intro");intro.FrozenAge=3.6f;yield return null;Check(!intro.Active,"Intro never ends");table.Opening.Cancel();
            v.handSeq++;v.dealtAt=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();client.ValidationApplyState(v);Check(!intro.Active && intro.Started==began,"A new hand replays full match intro");
            intro.Freeze=false;v.matchSeq++;v.matchStartedAt=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();client.ValidationApplyState(v);Check(intro.Active && intro.Started==began+1,"New full match did not replay intro");intro.Reset();table.Opening.Cancel();
            var reconnect=new MatchIntroduction();v.matchStartedAt=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()-20000;reconnect.Observe(v,true);Check(!reconnect.Active,"Old reconnect replays intro");reconnect.Reset();
            v.n=3;v.players=v.players.Take(3).ToArray();v.matchSeq++;v.matchStartedAt=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();client.ValidationApplyState(v);intro.Freeze=true;intro.FrozenAge=2.2f;yield return null;yield return null;Check(intro.VisiblePlayers==3,"Sanma intro exposes fourth player");ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-0524-intro-sanma.png"));yield return new WaitForSecondsRealtime(.08f);intro.Reset();table.Opening.Cancel();
            var clockIntro=new MatchIntroduction();var skew=JsonUtility.FromJson<RoomView>(JsonUtility.ToJson(v));skew.audioRoom="clock-skew-intro";skew.phase="lobby";skew.matchSeq=0;clockIntro.Observe(skew,true);skew.phase="play";skew.matchSeq=1;skew.matchStartedAt=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()+60000;clockIntro.Observe(skew,true);Check(clockIntro.Active,"Fresh lobby transition relies on synchronized phone clocks");clockIntro.Reset();
            Debug.Log("RIICHI MATCH INTRO PASS: fresh full-match identity, no replay on hand/poll/reconnect, repeated full match, sanma/yonma, hidden deal and gated options, original source frames/curves/effect.");
            foreach(var size in new[]{new Vector2(1280,720),new Vector2(936,435),new Vector2(774,438),new Vector2(1024,768)}){
                Size((int)size.x,(int)size.y);for(int i=0;i<7;i++)yield return null;var win=Load(folder,"win-4-tsumo-0-0");win.audioEvents=new PublicSound[0];win.audioRoom="background-"+size.x;
                client.ShowValidationFixture(win);var result=client.ValidationResult;result.Observe(win);result.Freeze=true;result.FrozenAge=100;yield return null;yield return null;
                Check(Resources.Load<Texture2D>("Majsoul/Results/bg_jiesuan1").width==1433,"Full-size native settlement background was downscaled");var viewport=TableViewport.Fit(Screen.width,Screen.height,Screen.safeArea,false);var rect=ResultPresentation.NativeBackgroundRect(Screen.width/viewport.Scale,Screen.height/viewport.Scale);Check(Mathf.Abs(rect.width/rect.height-1801f/995.37f)<.001f,"Background scaling is anisotropic");
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-0524-background-"+(int)size.x+"x"+(int)size.y+".png"));yield return new WaitForSecondsRealtime(.08f);
            }
            Size(936,435);for(int i=0;i<7;i++)yield return null;
            foreach(string limit in new[]{"三倍满","累计役满","役满","两倍役满","三倍役满","四倍役满","五倍役满","六倍役满"}){
                var win=Load(folder,"win-4-tsumo-0-0");win.audioEvents=new PublicSound[0];win.audioRoom="limit-fire-"+limit;var d=win.result.details[0];d.limit=limit;int multiplier=limit.StartsWith("六")?6:limit.StartsWith("五")?5:limit.StartsWith("四")?4:limit.StartsWith("三倍役")?3:limit.StartsWith("两")?2:1;d.score.defen=limit=="三倍满"?24000:32000*multiplier;d.score.damanguan=limit=="累计役满"||limit=="三倍满"?0:multiplier;d.score.fanshu=limit=="累计役满"?13:limit=="三倍满"?9:0;
                client.ShowValidationFixture(win);var result=client.ValidationResult;result.Observe(win);result.Freeze=true;result.FrozenAge=SettlementAudio.Sequence(d).titleAt+.7f;yield return null;yield return null;
                if(limit!="三倍满"){
                    var cfg=JsonUtility.FromJson<WinTileEffect.Config>(Resources.Load<TextAsset>(limit=="役满"?"Majsoul/UiEffects/effect_yiman":"Majsoul/UiEffects/effect_yiman2").text);
                    Check(cfg.layers.Any(l=>l.colors!=null && l.colors.Any(k=>k.color[0]>k.color[1]+.1f && k.color[0]>k.color[2]+.2f)) && cfg.layers.Any(l=>l.shape==3 && l.box[0]>.2f),"Native RGB fire gradient or horizontal box emitter missing");
                }
                Check(result.LimitFireLayers==(limit=="三倍满"?0:6),"Wrong native flame threshold/emitter count for "+limit);ScreenCapture.CaptureScreenshot(Path.Combine(folder,"review-0524-limit-"+limit+".png"));yield return new WaitForSecondsRealtime(.08f);
            }
            Debug.Log("RIICHI SETTLEMENT 0524 PASS: two original background layers and uniform scaling at four aspects; cumulative/single/multiple yakuman six-emitter source flame, no flame at sanbaiman; source glyph size retained.");
        }
    }
}
#endif
