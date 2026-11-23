using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace YunJian.Riichi.Editor {
    // Rank rules, local record and the player information page. Local only:
    // PlayerRecord.PreviewOnly keeps the user's real record untouched.
    [InitializeOnLoad] public static class PlayerRecordReview {
        const string Key="PlayerRecordReview";
        static PlayerRecordReview(){EditorApplication.update+=()=>{if(EditorApplication.isPlaying&&!EditorApplication.isCompiling&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<PlayerRecordCapture>();}};}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Bootstrap(){
            if(!SessionState.GetBool(Key,false))return;PlayerRecord.PreviewOnly=true;
            string roles=Environment.GetEnvironmentVariable("YUNJIAN_PLAYERINFO_ROLES");if(!string.IsNullOrEmpty(roles)){CharacterRoster.PreviewOnly=true;CharacterPackService.RootOverride=roles;CharacterPackService.RespectPreviewAvailability=true;CharacterPackService.ResetForReview();}
        }
        public static void Run(){PlayerSettings.companyName="YunJianReview";PlayerSettings.productName="PlayerInfoR1";EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class PlayerRecordCapture:MonoBehaviour {
        [Serializable] class Report {public int checks;public bool androidDeviceTested;public List<string> passed=new List<string>();public List<string> screenshots=new List<string>();}
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        Report report=new Report();RiichiClient client;LobbyController lobby;string output;
        void Check(bool ok,string reason){if(!ok)throw new Exception("FAILED: "+reason);report.checks++;report.passed.Add(reason);}
        IEnumerator Start(){
            output=Environment.GetEnvironmentVariable("YUNJIAN_PLAYERINFO_OUT");Directory.CreateDirectory(output);
            client=GetComponent<RiichiClient>();client.SessionPreviewOnly=true;client.BeginLobbyValidation("player-info-r1-local-only");lobby=client.Lobby;
            GetComponent<ResourceRoomCoordinator>().enabled=false;GetComponent<CharacterPackService>().StopAllCoroutines();AudioListener.volume=0;
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0){object next;try{var top=stack.Peek();if(!top.MoveNext()){stack.Pop();continue;}next=top.Current;if(next is IEnumerator nested){stack.Push(nested);continue;}}catch(Exception e){File.WriteAllText(output+"/failed.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
            File.WriteAllText(output+"/report.json",JsonUtility.ToJson(report,true));Debug.Log("PLAYER INFO R1 PASS "+report.checks);EditorApplication.Exit(0);
        }
        IEnumerator Size(int w,int h){var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));view.GetType().GetMethod("SetCustomResolution",Private).Invoke(view,new object[]{new Vector2(w,h),"Player info"});view.Repaint();for(int i=0;i<8;i++)yield return null;}
        IEnumerator Shot(string name){for(int i=0;i<6;i++)yield return null;yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+name+".png",texture.EncodeToPNG());Destroy(texture);report.screenshots.Add(name+".png");}
        IEnumerator Press(string id){lobby.ValidationPress(id);for(int i=0;i<6;i++)yield return null;}

        static RankChange Apply(int level,int points,int place,int score,int n=4,int rounds=2,bool allSoul=false)=>RankRules.Apply(new RankState(level,points),place,score,n,rounds,n==3?35000:25000,allSoul);
        void Rules(){
            // 雀士1 (level 3): 2nd place in 半庄 with 28400 → +40 +3 = +43.
            var c=Apply(3,550,2,28400);Check(c.delta==43&&c.after.level==3&&c.after.points==593,"雀士1 半庄二位 +40+素点3");
            c=Apply(3,590,1,26000);Check(c.promoted&&c.after.level==4&&c.after.points==400,"雀士1 达到 600 升雀士2 取初始分 400");
            c=Apply(6,30,4,10000);Check(c.demoted&&c.after.level==5&&c.after.points==500,"雀杰1 末位跌破 0 降雀士3 取初始分 500");
            Check(Apply(6,500,4,25000,4,1).delta==-40&&Apply(6,500,4,25000,4,2).delta==-80,"雀杰1 末位减分 东风 40 / 半庄 80");
            c=Apply(1,10,4,5000);Check(!c.demoted&&c.after.level==1&&c.after.points==0&&c.delta==-10,"初心最低 0 分且不降段");
            c=Apply(14,8990,1,40000);Check(c.promoted&&RankRules.Soul(c.after.level)&&c.after.points==100,"雀圣3 升入魂天 Lv1，10.0 魂珠");
            c=Apply(15,2,4,10000);Check(c.demoted&&c.after.level==14&&c.after.points==4500,"魂天 Lv1 跌破 0 降回雀圣3 4500");
            Check(Apply(15,100,1,40000,4,2).delta==5&&Apply(15,100,1,40000,4,2,true).delta==10,"魂天半庄一位 +0.5，同桌全魂天翻倍");
            Check(RankRules.LastPenalty(10,3,true)==190&&RankRules.LastPenalty(10,4,true)==180&&RankRules.LastPenalty(14,3,false)==160,"三麻表与四麻表的差异档位");
            Check(Apply(3,300,2,35000,3,2).delta==0&&Apply(3,300,1,40000,3,1).delta==55,"三麻 半庄二位 0、东风一位 +50+素点");
            Check(RankRules.Soten(25500,25000)==1&&RankRules.Soten(24500,25000)==-1&&RankRules.Soten(25400,25000)==0,"素点四舍五入远离零");
        }
        RoomView Final(string room,int seq,string category,int n,int place,int score,BestHand best=null){
            var v=DressPreviewStates.Tsumo(room,new WardrobeLoadout{tileBack="mjp_default",tileFace="mjpface_default"},"yiji",400101,"牌友");
            v.n=n;v.rounds=2;v.matchSeq=seq;v.audioRoom=room;v.lobby=new LobbyRoomView{category=category};v.phase="result";v.players=v.players.Take(n).ToArray();
            int[] scores=n==4?new[]{40000,30000,20000,10000}:new[]{50000,35000,20000};int[] order=Enumerable.Range(0,n).Select(i=>i).ToArray();
            var standings=new List<StandingView>();
            // Seat 0 is the viewer; other places fill the remaining scores.
            var others=Enumerable.Range(1,n).Where(p=>p!=place).ToList();
            for(int s=0;s<n;s++){int p=s==0?place:others[s-1];int sc=s==0?score:scores[p-1];v.players[s].score=sc;v.players[s].bot=s!=0;standings.Add(new StandingView{seat=s,place=p,score=sc,bot=s!=0,stats=new SeatStats{hands=8,wins=s==0?3:1,tsumo=s==0?1:0,dealIns=s==0?1:0,riichi=2,calls=1,winTurns=30,winPoints=24000,ura=1,best=s==0?best??new BestHand():new BestHand(),highlights=new HighlightCounts{baiman=s==0&&best!=null?1:0}}});}
            v.you=0;v.result=new ResultView{title="终局",delta=new int[n],details=new WinDetail[0],matchOver=true,standings=standings.ToArray(),rules=new ResultRules{n=n,rounds=2,start=n==3?35000:25000,category=category}};
            return v;
        }
        void Records(){
            PlayerRecord.Replace(new PlayerRecordData());var d=PlayerRecord.Data;
            Check(d.playerId.Length==9&&d.title=="traveler"&&d.owned.Contains("traveler"),"新档案有 9 位 ID 与默认称号");
            var best=new BestHand{tiles=new[]{"m2","m3","m4","p5","p5","p5","s6","s7","s8","z1","z1","z1","m9"},tile="m9",limit="倍满",fanshu=8,fu=40,defen=24000,yaku=new YakuDetail[0]};
            var v=Final("review-room",1,"ranked",4,2,28400,best);var c=PlayerRecord.Record(v);
            // A new record starts at 初心1 (upgrade at 20), so +43 promotes to 初心2 at 0.
            Check(c!=null&&c.delta==43&&c.promoted&&PlayerRecord.Rank(4).level==1&&PlayerRecord.Rank(4).points==0,"段位场二位 +43，初心1 升初心2");
            Check(PlayerRecord.Record(v)==c&&PlayerRecord.Mode("ranked",4).games.Count==1,"同一终局重复轮询只记一次");
            var mode=PlayerRecord.Mode("ranked",4);Check(mode.best.Exists&&mode.best.limit=="倍满"&&mode.highlights.baiman==1&&mode.totals.wins==3,"最佳一手、高光与累计统计");
            var f=Final("review-room",2,"friend",4,1,40000);Check(PlayerRecord.Record(f)?.delta==95&&PlayerRecord.Rank(4).level==2&&PlayerRecord.Rank(4).points==0&&PlayerRecord.Mode("friend",4).games.Count==1,"友人场记录对局并获得段位积分");
            var three=Final("review-room-3",1,"ranked",3,1,50000);var c3=PlayerRecord.Record(three);Check(c3!=null&&c3.delta==115&&PlayerRecord.Rank(3).level==1&&PlayerRecord.Rank(4).level==2&&PlayerRecord.Mode("ranked",3).games.Count==1,"三麻独立段位 +115");
            var style=PlayerRecord.Style(mode.totals);Check(style.attack>0&&style.defense>0&&style.speed>0&&style.attack<=100,"风格属性在 0–100 范围");
            var rich=new PlayerRecordData();rich.rank4=new RankState(12,2400);rich.peak4=12;rich.title="quesheng";
            PlayerRecord.Replace(rich);Check(PlayerRecord.Owns("queshi")&&PlayerRecord.Owns("quesheng")&&PlayerRecord.Title=="quesheng","按段位解锁称号并可佩戴");
        }
        PlayerRecordData Sample(){
            var d=new PlayerRecordData{rank4=new RankState(4,354),rank3=new RankState(2,103),peak4=4,peak3=2,title="queshi"};
            var places=new[]{2,1,3,4,2,2,1,3,4,4,3,2,1,1,2,3,4,1,2,1,1,2,3,2,1};
            var m4=new ModeRecord{category="ranked",n=4};foreach(var p in places)m4.games.Add(new MatchEntry{place=p,n=4,rounds=2,ranked=true});
            m4.totals=new ModeTotals{matches=places.Length,hands=210,wins=52,tsumo=21,dealIns=24,riichi=40,calls=60,winTurns=560,winPoints=330000,ura=9,places=new[]{8,8,5,4}};
            m4.best=new BestHand{tiles=new[]{"m6","m6"},melds=new[]{"z555","z777=","z1111","z4444-"},tile="z7",limit="倍满",fanshu=8,fu=60,defen=24000,yaku=new YakuDetail[0]};
            m4.highlights=new HighlightCounts{yakuman=1,baiman=2,doubleRiichi=1};
            var m3=new ModeRecord{category="ranked",n=3};foreach(var p in new[]{1,2,3,2,1,1,3,2,2,1})m3.games.Add(new MatchEntry{place=p,n=3,rounds=2,ranked=true});
            m3.totals=new ModeTotals{matches=10,hands=90,wins=26,tsumo=14,dealIns=10,riichi=20,calls=12,winTurns=250,winPoints=210000,ura=6,places=new[]{4,4,2,0}};
            m3.best=new BestHand{tiles=new[]{"p1","p1","p1","p9","p9","p9","s1","s1","s1","s9","s9","z7","z7"},tile="z7",limit="累计役满",fanshu=13,fu=40,defen=48000,yaku=new YakuDetail[0]};m3.highlights=new HighlightCounts{kazoe=1};
            d.modes.Add(m4);d.modes.Add(m3);return d;
        }
        IEnumerator Run(){
            Rules();Records();
            yield return Size(1920,1080);
            PlayerRecord.Replace(new PlayerRecordData());lobby.Home();for(int i=0;i<30;i++)yield return null;
            yield return Shot("00-lobby-badge-empty");
            lobby.OpenPlayerInfo();yield return Shot("01-info-empty");
            Check(lobby.Modal=="playerinfo","头像打开玩家信息页");
            PlayerRecord.Replace(Sample());yield return Shot("02-info-four");
            yield return Press("infoPlayers3");yield return Shot("03-info-three");
            yield return Press("infoRank");yield return Shot("04-rank-table-three");
            lobby.Back();for(int i=0;i<6;i++)yield return null;Check(lobby.Modal=="playerinfo","返回键先关闭子窗口");
            yield return Press("infoPlayers4");yield return Press("infoRank");yield return Shot("05-rank-table-four");lobby.Back();
            yield return Press("infoTitles");yield return Shot("06-title-book");
            yield return Press("infoTitle-traveler");Check(PlayerRecord.Title=="traveler","称号簿切换佩戴称号");lobby.Back();
            yield return Press("infoStyleHelp");yield return Shot("07-style-help");lobby.Back();
            yield return Press("infoRename");yield return Shot("08-rename");lobby.Back();
            yield return Press("infoTab1");yield return Shot("09-friend-tab-empty");
            lobby.Back();for(int i=0;i<20;i++)yield return null;Check(lobby.Modal=="","返回键关闭信息页");
            yield return Shot("10-lobby-badge-sample");
            yield return Size(2400,1080);lobby.OpenPlayerInfo();yield return Shot("11-info-wide");lobby.Back();for(int i=0;i<20;i++)yield return null;
            yield return Size(1920,1080);
            // Terminal settlement page with this device's rank change.
            PlayerRecord.Replace(Sample());var final=Final("review-final",7,"ranked",4,1,41200);final.players[0].ready=false;
            var change=PlayerRecord.Record(final);Check(change!=null&&change.delta==96,"终局一位 +80 +素点16");
            client.ShowValidationFixture(final);var result=(ResultPresentation)typeof(RiichiClient).GetField("resultDisplay",Private).GetValue(client);
            result.Observe(final);result.Freeze=true;result.FrozenAge=20;result.Advance(final);result.FrozenAge=4;
            Check(typeof(ResultPresentation).GetField("finalPage",Private).GetValue(result) is bool page&&page,"终局页已打开");
            yield return Shot("12-final-rank");
        }
    }
}
