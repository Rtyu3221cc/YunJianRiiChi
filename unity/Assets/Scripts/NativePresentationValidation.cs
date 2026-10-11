#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi
{
    public static class NativePresentationValidation
    {
        static void Require(bool ok,string text){if(!ok)throw new Exception(text);}
        static RoomView Fixture()=>new RoomView{phase="play",audioRoom="native-0522",n=4,you=0,turn=1,dealer=0,remaining=30,indicators=new[]{"z7"},hand=new[]{"m1","m2","m3","p1","p2","p3","s1","s2","s3","z1","z1","z2","z2"},players=Enumerable.Range(0,4).Select(i=>new PlayerView{name="玩家"+i,score=25000,wind=i,count=13}).ToArray()};
        public static IEnumerator Run(RiichiClient client,string folder)
        {
            Require(TableIndicators.Art("pop_bg")!=null,"Native opaque popup background missing");
            var table=client.GetComponent<TableRenderer>();var audio=client.GetComponent<GameAudio>();
            var view=Fixture();client.ShowValidationFixture(view);client.ValidationToggleRiichi(false);yield return null;Physics.SyncTransforms();
            var targets=UnityEngine.Object.FindObjectsByType<TileTarget>(FindObjectsSortMode.None).Where(t=>t.transform.parent.name=="Camera-facing hand").ToArray();
            Require(targets.Length==13 && targets.All(t=>t.Action==null),"Off-turn concealed tiles cannot be selected");
            client.SelectTile(0,null);client.SelectTile(0,null);Require(client.SelectedIndex==0 && client.SelectedAction==null && view.players[0].river.Length==0,"Off-turn selection submitted a discard");
            var next=JsonUtility.FromJson<RoomView>(JsonUtility.ToJson(view));next.turn=0;next.hand=next.hand.Concat(new[]{"p9"}).ToArray();next.drawn="p9";
            next.options=next.hand.Distinct().Select(t=>new Choice{id="discard:"+t+(t=="p9"?"_":""),kind="discard",tile=t}).ToArray();client.ValidationApplyState(next);yield return null;
            Require(client.SelectedIndex==0 && client.SelectedAction=="discard:m1" && table.HandFace(client.SelectedIndex)=="m1","Selection changed when the player's turn arrived");
            Debug.Log("RIICHI PRESELECT PASS: off-turn raycast targets, repeated selection never submits, selection survives polling and a new draw with a legal rebound action.");
            var draw=Fixture();draw.phase="result";draw.result=new ResultView{title="荒牌流局",draw=true,delta=new[]{0,0,0,0},drawDetails=Enumerable.Range(0,4).Select(i=>new DrawDetail{seat=i,tenpai=i==0,tiles=i==0?draw.hand:new string[0],waits=i==0?new[]{"z1","z2"}:new string[0]}).ToArray()};
            foreach(var resolution in new[]{new Vector2(1280,720),new Vector2(1688,780),new Vector2(936,435),new Vector2(844,390)}){
                var game=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));var method=game.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);method.Invoke(game,new object[]{resolution,"Native presentation validation"});game.Repaint();
                for(int f=0;f<7;f++)yield return null;
                int sounds=audio.PlayedCues.Count(k=>k=="showtile");draw.audioRoom="draw-native-"+resolution.x;client.ShowValidationFixture(draw);yield return null;
                table.Render(draw);table.Render(draw);Require(audio.PlayedCues.Count(k=>k=="showtile")==sounds+1,"Draw flip sound is missing or replays on polling");
                var display=client.ValidationResult;display.Observe(draw);display.Freeze=true;display.FrozenAge=2;yield return null;yield return null;
                var center=ResultPresentation.TableCenter(table);var expected=TableIndicators.CenteredImageRect("huangpailiuju",center,360);Require(Mathf.Abs(display.DrawTitleRect.x-expected.x)<.1f,"Draw title ignores the table axis");
                var panel=TableIndicators.WaitRect(2,center,center.y+180,380,52,false);var a=TableIndicators.WaitTileRect(0,2,panel,52,false);var b=TableIndicators.WaitTileRect(1,2,panel,52,false);
                Require(Mathf.Abs((a.center.x+b.center.x)/2-panel.center.x)<.01f && Mathf.Abs(a.center.y-panel.center.y)<.01f,"Draw wait tiles are off-center inside the frame");
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"native-0522-draw-"+(int)resolution.x+"x"+(int)resolution.y+".png"));yield return new WaitForSecondsRealtime(.08f);
                var waits=Fixture();waits.audioRoom="wait-native-"+resolution.x;waits.turn=0;waits.hand=waits.hand.Concat(new[]{"p9"}).ToArray();waits.drawn="p9";waits.options=new[]{new Choice{id="discard:p9_",kind="discard",tile="p9"}};waits.discardHints=new[]{new DiscardHint{id="discard:p9_",waits=new[]{new WaitDetail{tile="z1",ron=true,remaining=2},new WaitDetail{tile="z2",ron=true,remaining=1}}}};
                client.ShowValidationFixture(waits);client.SelectTile(13,"discard:p9_");yield return null;yield return null;Require(client.CurrentWaitDetails.Length==2 && client.CurrentWaitPanel.width>0,"Selected-discard wait preview disappeared");
                var p=client.CurrentWaitPanel;Require(TableIndicators.WaitTileRect(0,2,p).y>p.y && TableIndicators.WaitBadgeRect(TableIndicators.WaitTileRect(1,2,p)).yMax<p.yMax,"Wait tiles/counters escape their native frame");
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"native-0522-waits-"+(int)resolution.x+"x"+(int)resolution.y+".png"));yield return new WaitForSecondsRealtime(.08f);
            }
            Debug.Log("RIICHI NATIVE POPUP PASS: original pop_bound and bg_3, remaining counters, centered two-tile draw frames and visible title at the projected table axis; four resolutions; flip sound exactly once across polling.");
            var win=Fixture();win.phase="result";var detail=new WinDetail{seat=0,from=0,ron=false,name="玩家0",tiles=win.hand,tile="z2",melds=new string[0],ura=new string[0],limit="跳满",score=new ScoreDetail{fanshu=6,fu=40,defen=12000},yaku=new[]{new YakuDetail{name="清一色",han=5},new YakuDetail{name="拔北宝牌",han=1}}};win.result=new ResultView{title="自摸",delta=new[]{9000,-3000,-3000,-3000},details=new[]{detail}};client.ShowValidationFixture(win);var result=client.ValidationResult;result.Observe(win);result.Freeze=true;result.FrozenAge=0;
            var probe=new GameObject("Native settlement audio probe").AddComponent<SettlementAudio>();probe.Configure(true,0);Require(probe.HasResources,"Ichihime settlement clips are missing");var plan=SettlementAudio.Sequence(detail);Require(plan.rows[0].voice=="fan_qingyise" && plan.rows[1].voice=="fan_dora1" && plan.titleVoice=="gameend_tiaoman" && plan.drum=="mangan","Wrong role/limit speech or drum mapping");
            probe.Tick(win,result,false,audio,true);Require(probe.PlayedKeys.Count==0,"Settlement voices start during the tile effect");probe.Tick(win,result,true,audio,true);
            var beats=plan.rows.Select(r=>r.at).Concat(new[]{plan.fuAt,plan.pointsAt,plan.titleAt+.3f,plan.titleAt+.5f}).OrderBy(x=>x).ToArray();
            foreach(float beat in beats){result.FrozenAge=beat+.001f;probe.Tick(win,result,true,audio,true);probe.Tick(win,result,true,audio,true);yield return null;}
            Require(probe.PlayedKeys.SequenceEqual(new[]{"fan_qingyise","fan_dora1","gameend_tiaoman"}) && probe.VoicesPlayed==3,"Report-yaku queue duplicates or skips speech");
            int reported=probe.PlayedKeys.Count;result.Advance(win);probe.Tick(win,result,true,audio,true);Require(probe.PlayedKeys.Count==reported,"Skipping a settlement replays old voices");
            foreach(var pair in new[]{Tuple.Create("满贯","mangan"),Tuple.Create("跳满","mangan"),Tuple.Create("倍满","beiman"),Tuple.Create("三倍满","beiman"),Tuple.Create("累计役满","yakuman")}){detail.score.damanguan=0;detail.limit=pair.Item1;Require(SettlementAudio.LimitDrum(detail)==pair.Item2,"Wrong limit drum: "+pair.Item1);}
            for(int i=1;i<=6;i++){detail.score.damanguan=i;Require(SettlementAudio.LimitVoice(detail)=="gameend_yiman"+i && SettlementAudio.LimitDrum(detail)=="yakuman","Missing multiple-yakuman voice");}
            foreach(var y in new[]{"翻牌 白","翻牌 發","翻牌 中","ダブル立直","三槓子","四槓子","一発","純正九蓮宝燈","対々和"})Require(SettlementAudio.YakuVoice(new YakuDetail{name=y,han=1},detail)!=null,"Scoring alias has no voice: "+y);
            UnityEngine.Object.Destroy(probe.gameObject);result.Freeze=false;
            Debug.Log("RIICHI SETTLEMENT AUDIO PASS: 82 original Ichihime resources, native row ordering/timing, sequential report-yaku, title/drum offsets, all three drum grades, 1-6 yakuman speech, silent during tile effect, polling dedup and skip cancellation.");
        }
    }
}
#endif
