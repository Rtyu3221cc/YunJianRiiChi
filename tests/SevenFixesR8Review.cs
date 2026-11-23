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
    [InitializeOnLoad] public static class SevenFixesR8Review {
        const string Key="SevenFixesR8Review";
        static SevenFixesR8Review(){EditorApplication.update+=()=>{if(EditorApplication.isPlaying&&!EditorApplication.isCompiling&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<SevenFixesR8Capture>();}};}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Bootstrap(){
            if(!SessionState.GetBool(Key,false))return;PlayerRecord.PreviewOnly=true;WardrobePackService.RespectPreviewAvailability=true;WardrobePackService.RootOverride=Environment.GetEnvironmentVariable("YUNJIAN_PLAYERINFO_OUT")+"/empty-wardrobe-cache";
            string roles=Environment.GetEnvironmentVariable("YUNJIAN_PLAYERINFO_ROLES");if(!string.IsNullOrEmpty(roles)){CharacterRoster.PreviewOnly=true;CharacterPackService.RootOverride=roles;CharacterPackService.RespectPreviewAvailability=true;CharacterPackService.ResetForReview();}
        }
        public static void Run(){PlayerSettings.companyName="YunJianReview";PlayerSettings.productName="SevenFixesR8";EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class SevenFixesR8Capture:MonoBehaviour {
        [Serializable] class Report {public int checks;public bool androidDeviceTested;public List<string> passed=new List<string>();public List<string> screenshots=new List<string>();}
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        Report report=new Report();RiichiClient client;LobbyController lobby;string output;
        void Check(bool ok,string reason){if(!ok)throw new Exception("FAILED: "+reason);report.checks++;report.passed.Add(reason);}
        IEnumerator Start(){
            output=Environment.GetEnvironmentVariable("YUNJIAN_PLAYERINFO_OUT");Directory.CreateDirectory(output);
            client=GetComponent<RiichiClient>();client.SessionPreviewOnly=true;client.BeginLobbyValidation("player-info-r1-local-only");lobby=client.Lobby;
            GetComponent<ResourceRoomCoordinator>().enabled=false;GetComponent<CharacterPackService>().StopAllCoroutines();GetComponent<WardrobePackService>().StopAllCoroutines();WardrobePackService.RespectPreviewAvailability=false;WardrobePackService.ResetForReview();AudioListener.volume=0;
            var stack=new Stack<IEnumerator>();stack.Push(SevenChecks());
            while(stack.Count>0){object next;try{var top=stack.Peek();if(!top.MoveNext()){stack.Pop();continue;}next=top.Current;if(next is IEnumerator nested){stack.Push(nested);continue;}}catch(Exception e){if(videoEncoder!=null&&!videoEncoder.HasExited)videoEncoder.Kill();File.WriteAllText(output+"/failed.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
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

        const int VideoWidth=1600, VideoHeight=900, VideoRate=20;
        System.Diagnostics.Process videoEncoder;
        RenderTexture videoRender;
        Texture2D videoPixels;
        int videoFrame;
        readonly List<string> videoChapters=new List<string>();

        IEnumerator Film(string label,float seconds,string click=null,Action<int> act=null){
            videoChapters.Add(videoFrame+" "+label);
            Debug.Log("PROFILE FILM "+label+" "+videoFrame);
            for(int i=0;i<Mathf.RoundToInt(seconds*VideoRate);i++){
                if(i==9&&click!=null){
                    Check(lobby.ValidationTarget(click).width>0,"visible control "+click);
                    lobby.DesignPreviewPulse(click);
                    lobby.ValidationPress(click);
                }
                act?.Invoke(i);
                yield return null;
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshotIntoRenderTexture(videoRender);
                var active=RenderTexture.active;
                RenderTexture.active=videoRender;
                videoPixels.ReadPixels(new Rect(0,0,VideoWidth,VideoHeight),0,0);
                RenderTexture.active=active;
                var bytes=videoPixels.GetRawTextureData();
                videoEncoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);
                videoFrame++;
            }
        }

        IEnumerator SevenChecks(){
            Rules();
            TitleCounterChecks();
            Check(PlayerTitles.All.Length==130,"130 original non-test titles");
            foreach(var entry in PlayerTitles.All){var texture=Resources.Load<Texture2D>("Majsoul/Profile/"+entry.resource);Check(texture!=null,"original art: "+entry.sourceId);var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));Check(importer.npotScale==TextureImporterNPOTScale.None&&!importer.mipmapEnabled,"original title proportions: "+entry.sourceId);}
            foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Majsoul/Profile/Native"})){var path=AssetDatabase.GUIDToAssetPath(guid);var image=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var bytes=File.ReadAllBytes(path);int Number(int at)=>(bytes[at]<<24)|(bytes[at+1]<<16)|(bytes[at+2]<<8)|bytes[at+3];Check(image.width==Number(16)&&image.height==Number(20),"native source dimensions: "+Path.GetFileName(path));}
            PlayerRecord.Replace(new PlayerRecordData());Check(PlayerRecord.Data.owned.Count==122,"new profile receives every non-gameplay title");Check(!PlayerRecord.Owns("firstYakuman")&&!PlayerRecord.Owns("hundred")&&!PlayerRecord.Owns("quehao")&&!PlayerRecord.Owns("600039"),"gameplay and mixed title groups stay locked");
            PlayerRecord.Data.rank4=new RankState(15,100);PlayerRecord.Data.peak4=15;PlayerRecord.Data.rank3=new RankState(14,8000);PlayerRecord.Data.peak3=14;
            Check(!TitleAchievements.GroupComplete(1,PlayerRecord.Data),"growth requires both four-player and three-player soul ranks");
            var final=Final("r8-unlock",1,"friend",3,1,50000);PlayerRecord.Data.rank3=new RankState(15,100);PlayerRecord.Data.peak3=15;PlayerRecord.Record(final);
            Check(PlayerRecord.Owns("quehao")&&PlayerRecord.PendingTitle=="quehao","newly completed gameplay group queues a title notice");PlayerRecord.Record(final);Check(PlayerRecord.Data.pendingTitles.Count==1,"repeated terminal polls do not duplicate title notices");
            var saved=JsonUtility.ToJson(PlayerRecord.Data);PlayerRecord.Replace(JsonUtility.FromJson<PlayerRecordData>(saved));Check(PlayerRecord.PendingTitle=="quehao","pending title survives saved record reload");
            yield return Size(1920,1080);lobby.Home();for(int i=0;i<90;i++)yield return null;Check(lobby.Modal=="title-earned","earned title appears after returning home");yield return Shot("01-title-earned");yield return Press("title-earned-confirm");for(int i=0;i<20;i++)yield return null;Check(PlayerRecord.PendingTitle==null,"confirm consumes the persisted title notice");
            PlayerRecord.Replace(Sample());PlayerRecord.Equip("600008");client.LobbyNickname="云间牌友";lobby.Home();for(int i=0;i<25;i++)yield return null;yield return Shot("02-lobby-original-title");
            lobby.OpenPlayerInfo();for(int i=0;i<20;i++)yield return null;yield return Shot("03-profile-16x9");Check(lobby.ValidationTarget("infoCopy").width==0&&lobby.ValidationTarget("infoAchievement").width==0,"profile has no player ID or achievement hit targets");
            yield return Press("infoTab1");yield return Shot("04-profile-empty-friend");yield return Press("infoTitles");yield return Shot("05-original-title-book");lobby.Back();yield return Size(2400,1080);yield return Shot("06-profile-20x9");lobby.Back();for(int i=0;i<20;i++)yield return null;
            var coordinator=GetComponent<ResourceRoomCoordinator>();coordinator.ReviewDisplayOnly=true;coordinator.enabled=true;
            foreach(int n in new[]{4,3}){
                var setup=DressPreviewStates.Base("r8-resource-"+n,new WardrobeLoadout(),"yiji",400101,"房间首位");setup.phase="lobby";setup.n=n;setup.you=1;setup.host=false;setup.players=setup.players.Take(n).ToArray();for(int i=0;i<n;i++)setup.players[i].name="房间第"+(i+1)+"位";
                setup.resourceSetup=new ResourceSetupView{revision=10+n,choices=setup.players.Select(p=>new CharacterChoice{character=p.character,skin=p.skin}).ToArray(),players=Enumerable.Range(0,n).Select(i=>new ResourcePeerState{seat=i,state=i==0?"ready":"pending",progress=i==0?100:i==1?82:37+i*9}).Reverse().ToArray()};
                client.ShowValidationFixture(setup);coordinator.ReviewProgress(71,87,"共享装扮资源",false);for(int i=0;i<20;i++)yield return null;yield return Shot("07-resource-"+n+"-players");Check(coordinator.PreparationPercent==82,"local preparation percentage uses completed steps, "+n+" seats");
                coordinator.ReviewProgress(87,87,"共享装扮资源",true);yield return Shot("08-resource-"+n+"-ready");
            }
            coordinator.enabled=false;yield return AudioChecks();
            client.BeginLobbyValidation("r8-after");lobby.Home();PlayerRecord.Replace(Sample());PlayerRecord.Equip("600010");yield return Size(VideoWidth,VideoHeight);for(int i=0;i<25;i++)yield return null;
            videoRender=new RenderTexture(VideoWidth,VideoHeight,0);videoRender.Create();videoPixels=new Texture2D(VideoWidth,VideoHeight,TextureFormat.RGB24,false);
            string repo=Environment.GetEnvironmentVariable("YUNJIAN_PLAYERINFO_REPO");string python=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe");
            videoEncoder=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{FileName=python,Arguments="-X utf8 \""+repo+"/scripts/encode-unity-capture-r40.py\" --output \""+output+"/r8-raw.mp4\" --width 1600 --height 900 --fps 20",UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true});
            Time.captureFramerate=20;yield return Film("lobby-original-title",1.5f);yield return Film("profile",3f,"profile");yield return Film("original-titles",2.5f,"infoTitles");lobby.Back();yield return Film("friend-tab",2.5f,"infoTab1");
            coordinator.enabled=true;
            foreach(int n in new[]{4,3}){
                var loading=DressPreviewStates.Base("r8-video-loading-"+n,new WardrobeLoadout(),"yiji",400101,"房间首位");loading.phase="lobby";loading.n=n;loading.you=n==4?0:1;loading.host=n==4;loading.players=loading.players.Take(n).ToArray();for(int seat=0;seat<n;seat++)loading.players[seat].name="房间第"+(seat+1)+"位";
                loading.resourceSetup=new ResourceSetupView{revision=30+n,choices=loading.players.Select(p=>new CharacterChoice{character=p.character,skin=p.skin}).ToArray(),players=Enumerable.Range(0,n).Select(i=>new ResourcePeerState{seat=i,state="pending"}).ToArray()};client.ShowValidationFixture(loading);
                yield return Film("resource-"+n+"-players",3.5f,act:i=>{int done=Mathf.Min(100,i*2);coordinator.ReviewProgress(done,100,"共享装扮资源",done==100);foreach(var peer in loading.resourceSetup.players){peer.progress=Mathf.Min(100,i*(peer.seat+2));peer.state=peer.progress==100?"ready":"pending";}});
            }
            coordinator.enabled=false;yield return RonChecks();
            videoEncoder.StandardInput.Close();while(!videoEncoder.HasExited)yield return null;Check(videoEncoder.ExitCode==0,"preview video encoded");videoEncoder.Dispose();videoEncoder=null;videoRender.Release();Destroy(videoRender);Destroy(videoPixels);File.WriteAllLines(output+"/chapters.txt",videoChapters);
        }
        [Serializable] class AudioTrace {public float time;public float[] weights,volumes;public string track;}
        void TitleCounterChecks(){
            PlayerRecord.Replace(new PlayerRecordData());
            var v=Final("r8-title-facts",1,"ranked",4,1,50000);var stats=v.result.standings.First(s=>s.seat==v.you).stats;
            stats.titleFacts=new[]{new TitleCounter{name="yaku:平和",value=2},new TitleCounter{name="yaku:役牌 白",value=1},new TitleCounter{name="bonus",value=12},new TitleCounter{name="bonusMax",value=8}};stats.highlights.yakuman=1;PlayerRecord.Record(v);
            int Progress(int id)=>PlayerRecord.Data.titleProgress.FirstOrDefault(x=>x.name==id.ToString())?.value??0;
            Check(Progress(100039)==1&&Progress(100065)==2,"source yaku tasks retain distinct thresholds and count each win once");Check(Progress(100226)==8&&Progress(100230)==12,"source bonus maximum and cumulative bonus use different counters");
            Check(!PlayerRecord.Owns("firstYakuman")&&!PlayerRecord.Owns("hundred"),"a single yakuman or one match does not grant category-completion titles");
            PlayerRecord.Record(v);Check(Progress(100065)==2,"repeated result poll does not increment gameplay counters");
            var friend=Final("r8-title-facts",2,"friend",4,1,50000);friend.result.standings[0].stats.titleFacts=stats.titleFacts;PlayerRecord.Record(friend);Check(Progress(100065)==2,"friend matches do not advance ranked-only yaku tasks");
            PlayerRecord.Replace(new PlayerRecordData());for(int i=0;i<4;i++)PlayerRecord.Record(Final("r8-streak",i,"ranked",4,1,40000));Check(Progress(800013)==4,"four-player first-place streak accumulates");PlayerRecord.Record(Final("r8-streak",4,"ranked",4,4,10000));Check(Progress(800013)==0,"an unfinished winning streak resets after losing");
            for(int n=3;n<=4;n++)for(int i=0;i<10;i++)PlayerRecord.Record(Final("r8-complete-streak-"+n,i,"ranked",n,1,50000));
            Check(PlayerRecord.Owns("600032")&&PlayerRecord.PendingTitle=="600032","all original sanma and yonma streak requirements unlock 连胜之道 once");
            PlayerRecord.Replace(new PlayerRecordData());var zero=Final("r8-zero",0,"ranked",4,4,0);PlayerRecord.Record(zero);Check(Progress(100224)==1&&!PlayerRecord.Owns("600039"),"mixed group retains zero-score condition and still requires original achievement-count goals");
            PlayerRecord.Replace(new PlayerRecordData());
        }
        [Serializable] class AudioTraces {public List<AudioTrace> rows=new List<AudioTrace>();}
        IEnumerator AudioChecks(){
            var mix=new RiichiMusicMix();mix.Target(0,0);Check(mix.Sample(0)[0]==0&&mix.Sample(2)[0]==1,"first riichi fades in over two seconds");mix.Target(1,2);var before=mix.Sample(2.7f).ToArray();mix.Target(2,2.7f);Check(before.Zip(mix.Sample(2.7f),(a,b)=>Mathf.Abs(a-b)<.00001f).All(x=>x),"third riichi retarget preserves all instantaneous gains");var last=mix.Sample(4.7f);Check(last[2]>.999f&&last.Where((x,i)=>i!=2).All(x=>x<.001f),"latest riichi becomes the only track");
            var view=DressPreviewStates.Base("r8-music",new WardrobeLoadout{music="music-riichi"},"yiji",400101,"音乐验证");view.players[0].riichi=1;view.players[0].riichiOrder=10;view.players[1].wardrobe=new WardrobeLoadout{music="music-8bit"};view.players[2].wardrobe=new WardrobeLoadout{music="music-riichi"};client.ShowValidationFixture(view);var audio=GetComponent<PlaylistAudio>();audio.ReviewRoundTime=10;
            for(int i=0;i<120&&audio.RiichiTrack==null;i++)yield return null;Check(audio.RiichiTrack!=null,"first equipped riichi track starts");audio.ReviewRoundTime=12;yield return null;view.players[1].riichi=1;view.players[1].riichiOrder=20;
            for(int i=0;i<120&&!(audio.RiichiTrack??"").EndsWith(":1:20");i++)yield return null;Check((audio.RiichiTrack??"").EndsWith(":1:20"),"later opponent declaration replaces local music");
            var trace=new AudioTraces();foreach(float at in new[]{12f,12.5f,13f}){audio.ReviewRoundTime=at;yield return null;trace.rows.Add(new AudioTrace{time=at,volumes=audio.RiichiVolumes,track=audio.RiichiTrack});}
            Check(audio.RiichiVolumes[0]>0&&audio.RiichiVolumes[1]>0,"old and new actual AudioSources overlap during crossfade");view.players[2].riichi=1;view.players[2].riichiOrder=30;
            for(int i=0;i<120&&!(audio.RiichiTrack??"").EndsWith(":2:30");i++)yield return null;Check((audio.RiichiTrack??"").EndsWith(":2:30"),"third declaration can interrupt a running crossfade");
            foreach(float at in new[]{13f,14f,15.1f}){audio.ReviewRoundTime=at;yield return null;trace.rows.Add(new AudioTrace{time=at,volumes=audio.RiichiVolumes,track=audio.RiichiTrack});}
            Check(audio.RiichiVolumes[0]<.001f&&audio.RiichiVolumes[1]<.001f&&audio.RiichiVolumes[2]>0,"only newest player's source remains audible");File.WriteAllText(output+"/riichi-audio-trace.json",JsonUtility.ToJson(trace,true));audio.ReviewRoundTime=-1;
        }
        [Serializable] class RonTrace {public int winners;public List<int> order=new List<int>();public List<float> times=new List<float>();public bool presentationHeld;}
        [Serializable] class RonTraces {public List<RonTrace> rows=new List<RonTrace>();}
        IEnumerator RonChecks(){
            var effects=GetComponent<TableEffects>();var traces=new RonTraces();
            foreach(int count in new[]{2,3}){
                var v=DressPreviewStates.Tsumo("r8-ron-"+count,new WardrobeLoadout(),"yiji",400101,"多响验证");v.last=new LastDiscard{from=3,tile="p6"};v.audioSeq=0;v.audioEvents=Array.Empty<PublicSound>();
                int[] seats=count==2?new[]{2,0}:new[]{2,0,1};v.result.details=seats.Select((seat,i)=>new WinDetail{seat=seat,from=3,ron=true,winSource="discard",tile="p6",name=v.players[seat].name,tiles=new[]{"p6","p6"},melds=Array.Empty<string>(),yaku=new[]{new YakuDetail{name="立直",han=1}},limit=i==0?"满贯":"",score=new ScoreDetail{fanshu=i==0?5:1,fu=30,defen=i==0?8000:1000}}).ToArray();
                string[] choices={"ron-flame","ron-wind","win-default"};for(int i=0;i<count;i++)v.players[seats[i]].wardrobe=new WardrobeLoadout{winEffect=choices[i]};client.ShowValidationFixture(v);yield return null;effects.Accept(v);effects.ValidationFreeze=true;effects.ValidationSequentialClock=true;effects.ValidationTime=0;
                var events=seats.Select((seat,i)=>new PublicSound{id=i+1,seat=seat,kind="ron",from=3,tile="p6",winSource="discard"}).ToArray();v.audioSeq=count;v.audioEvents=events;effects.Accept(v);int baseline=effects.SpawnedWinEffects;var row=new RonTrace{winners=count};traces.rows.Add(row);
                for(int step=0;step<500;step++){
                    effects.ValidationTime=step*.1f;yield return null;
                    int played=effects.SpawnedWinEffects-baseline;if(played>row.order.Count){var active=GetComponentsInChildren<WinTileEffect>().FirstOrDefault(e=>e.gameObject.activeSelf);Check(active!=null,"winner effect instantiated");row.order.Add(active.WinningEvent.seat);row.times.Add(effects.ValidationTime);yield return Shot("09-ron-"+count+"-winner-"+active.WinningEvent.seat);}
                    if(row.order.Count<count)Check(effects.ShowWinPresentation,"settlement remains held until every winner effect starts");
                    if(videoEncoder!=null)yield return Film("ron-"+count+"-t"+effects.ValidationTime.ToString("F1"),.1f);
                    if(row.order.Count==count&&!effects.ShowWinPresentation){row.presentationHeld=true;break;}
                }
                Check(row.order.SequenceEqual(seats),count+" ron effects preserve committed winner order, including same tile and source");Check(effects.SpawnedWinEffects-baseline==count,"one effect per winner");Check(row.presentationHeld,"settlement releases only after last effect");effects.Accept(v);yield return null;Check(effects.SpawnedWinEffects-baseline==count,"same result poll does not replay bursts");effects.Reset();
            }
            File.WriteAllText(output+"/multi-ron-trace.json",JsonUtility.ToJson(traces,true));
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
