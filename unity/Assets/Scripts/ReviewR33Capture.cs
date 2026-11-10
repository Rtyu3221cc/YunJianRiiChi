#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    // Silent preview recording for the 2026-11-07 round: outfit presets, battle
    // skin memory, BGM ducking during voice preview, and call voice interruption.
    // Audio cannot be heard in the video, so an overlay prints the live values.
    public sealed class ReviewR33Capture:MonoBehaviour {
        const int Fps=30;
        string repo,output,caption;RiichiClient client;LobbyController lobby;
        System.Diagnostics.Process encoder;Stream pipe;byte[] rgba;int frames;
        readonly List<string> log=new List<string>();readonly List<string> errors=new List<string>();
        RoleVoice roleVoice;AudioSource roleOutput;string voiceEvent;
        void Note(string text){log.Add(frames+" "+text);Debug.Log("R33 "+text);}
        void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){if((stack??"").Contains("UnityEditor.Search"))return;errors.Add(message);}}
        IEnumerator Start(){
            repo=Environment.GetEnvironmentVariable("YUNJIAN_R33_REPO")??Path.GetFullPath("..");output=Environment.GetEnvironmentVariable("YUNJIAN_R33_OUT")??repo+"/build/claude-1107";Directory.CreateDirectory(output);Application.logMessageReceived+=OnLog;
            client=GetComponent<RiichiClient>();lobby=client.Lobby;roleVoice=GetComponent<RoleVoice>();roleOutput=(AudioSource)typeof(RoleVoice).GetField("output",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(roleVoice);
            client.BeginLobbyValidation("review-r33");client.SessionPreviewOnly=true;GetComponent<ResourceRoomCoordinator>().enabled=false;GetComponent<CharacterPackService>().StopAllCoroutines();
            PlayerPrefs.DeleteKey("riichi.wardrobe.presets.v3");CharacterRoster.BeginPreview();WardrobeSelection.BeginPreview();CharacterPackService.MarkStartupSeen();
            // Keep playback clocks running (the duck and the voice clip need them) but silent.
            AudioListener.pause=false;AudioListener.volume=0;
            var work=new Stack<IEnumerator>();work.Push(Run());
            while(work.Count>0){object next;try{var task=work.Peek();if(!task.MoveNext()){work.Pop();continue;}next=task.Current;if(next is IEnumerator child){work.Push(child);continue;}}catch(Exception e){Fail(e.ToString());yield break;}yield return next;}
            if(errors.Count>0){Fail(string.Join("\n",errors));yield break;}
            File.WriteAllLines(output+"/capture-log.txt",log);Cleanup();Debug.Log("REVIEW R33 CAPTURE PASS frames="+frames);EditorApplication.Exit(0);
        }
        void Fail(string text){File.WriteAllText(output+"/capture-failed.txt",text);File.WriteAllLines(output+"/capture-log.txt",log);Debug.LogError(text);Cleanup();EditorApplication.Exit(1);}
        void Cleanup(){if(encoder!=null){try{pipe?.Close();encoder.WaitForExit(30000);}catch{}encoder.Dispose();encoder=null;}Time.captureFramerate=0;WardrobeSelection.EndPreview();CharacterRoster.EndPreview();AudioListener.volume=1;Application.logMessageReceived-=OnLog;}
        IEnumerator Wait(int n){for(int i=0;i<n;i++)yield return null;}
        IEnumerator Size(int w,int h){var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));view.GetType().GetMethod("SetCustomResolution",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,new object[]{new Vector2(w,h),"Review R33"});yield return Wait(8);}
        void BeginRecording(){
            var python=Environment.GetEnvironmentVariable("YUNJIAN_R33_PYTHON");
            var start=new System.Diagnostics.ProcessStartInfo{FileName=python,Arguments="-X utf8 \""+repo+"/scripts/encode-game-capture.py\" --output \""+output+(MotionMode?"/preview-motion.mp4":TsumoMode?"/preview-tsumo.mp4":R37Mode?"/preview-r37.mp4":R39Mode?"/preview-r39.mp4":R40Mode?"/preview-r40.mp4":R41Mode?"/preview-r41.mp4":"/preview-r33.mp4")+"\" --width "+Screen.width+" --height "+Screen.height+" --fps "+Fps,UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
            encoder=new System.Diagnostics.Process{StartInfo=start};encoder.OutputDataReceived+=(s,e)=>{if(e.Data!=null)File.AppendAllText(output+"/encoder.log",e.Data+"\n");};encoder.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)File.AppendAllText(output+"/encoder.log",e.Data+"\n");};
            encoder.Start();encoder.BeginOutputReadLine();encoder.BeginErrorReadLine();pipe=encoder.StandardInput.BaseStream;Time.captureFramerate=Fps;
        }
        IEnumerator Frame(){yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();var pixels=image.GetPixels32();if(rgba==null||rgba.Length!=pixels.Length*4)rgba=new byte[pixels.Length*4];for(int i=0;i<pixels.Length;i++){int at=i*4;rgba[at]=pixels[i].r;rgba[at+1]=pixels[i].g;rgba[at+2]=pixels[i].b;rgba[at+3]=pixels[i].a;}pipe.Write(rgba,0,rgba.Length);Destroy(image);frames++;}
        IEnumerator Hold(int n){for(int i=0;i<n;i++)yield return Frame();}
        IEnumerator Press(string id,int hold=8){Note("press "+id);lobby.ValidationPress(id);yield return Hold(hold);}
        void Chapter(string text){caption=text;Note("chapter "+text);}

        IEnumerator Run(){
            yield return Size(1600,900);lobby.DesignPreviewGardenEntry();yield return Wait(30);
            if(OverlapMode){yield return NorthStable();yield return OverlapScan();yield return Size(1800,810);yield return NorthStable();yield return OverlapScan();yield break;}
            if(R41Mode){yield return Size(1800,810);yield return R41();pipe.Close();encoder.WaitForExit();if(encoder.ExitCode!=0)throw new Exception("Encoder failed "+encoder.ExitCode);encoder.Dispose();encoder=null;yield break;}
            if(R40Mode){yield return Size(1800,810);yield return R40();pipe.Close();encoder.WaitForExit();if(encoder.ExitCode!=0)throw new Exception("Encoder failed "+encoder.ExitCode);encoder.Dispose();encoder=null;yield break;}
            if(R39Mode){yield return Size(1800,810);yield return R39();pipe.Close();encoder.WaitForExit();if(encoder.ExitCode!=0)throw new Exception("Encoder failed "+encoder.ExitCode);encoder.Dispose();encoder=null;yield break;}
            if(R37Mode){yield return Size(1800,810);yield return R37();pipe.Close();encoder.WaitForExit();if(encoder.ExitCode!=0)throw new Exception("Encoder failed "+encoder.ExitCode);encoder.Dispose();encoder=null;yield break;}
            BeginRecording();
            if(MotionMode||TsumoMode){yield return MotionMode?Motion():Tsumo();pipe.Close();encoder.WaitForExit();if(encoder.ExitCode!=0)throw new Exception("Encoder failed "+encoder.ExitCode);encoder.Dispose();encoder=null;yield break;}
            yield return Presets();yield return EffectTables();
            if(Environment.GetEnvironmentVariable("YUNJIAN_R33_ONLY")=="roles"){yield return Roles();yield return Ducking();}
            // This editor environment reports AudioSource.isPlaying=false for every
            // clip (no audio output), so call-voice interruption cannot be shown
            // truthfully here. It is left to the phone test; Interrupt() is kept
            // for a machine where editor audio plays (YUNJIAN_R33_ONLY=interrupt).
            if(Environment.GetEnvironmentVariable("YUNJIAN_R33_ONLY")=="interrupt")yield return Interrupt();
            Chapter("以上为编辑器预览（无声）；以 0.6.25 测试包真机为准");yield return Hold(60);
            pipe.Close();encoder.WaitForExit();if(encoder.ExitCode!=0)throw new Exception("Encoder failed "+encoder.ExitCode);encoder.Dispose();encoder=null;
        }
        // Pick a category that has at least two installed items so wearing a preset visibly changes it.
        string PresetCategory(out string first,out string second){
            foreach(var key in new[]{"cloth","tileBack","stick","headFrame","lobbyBackground","tileFace"}){var ids=WardrobeCatalog.Items(key).Where(i=>WardrobePackService.Installed(i.id)).Select(i=>i.id).ToArray();if(ids.Length>=2){first=ids[0];second=ids[1];return key;}}
            first=second=null;return null;
        }
        IEnumerator Presets(){
            Chapter("1. 装扮预设：10 个预设竖排在面板左侧，与面板一起居中；金条标出正在使用的预设");
            var category=PresetCategory(out var a,out var b);if(category==null)throw new Exception("No wardrobe category with two installed items");Note("preset category "+category+" "+a+" / "+b);
            lobby.PreviewWardrobe(category);yield return Hold(45);
            var frame=lobby.ReviewDormFrame;float left=frame.x-142,right=lobby.ReviewLayoutWidth-frame.xMax;Note("layout left="+left+" right="+right);
            if(Mathf.Abs(left-right)>2)throw new Exception("Wardrobe is not centred: "+left+" / "+right);
            Chapter("在「使用中」的预设1里换装，立即生效");yield return Press("dress-item-"+a,45);
            if(WardrobeSelection.Get(category).id!=a)throw new Exception("Active preset edit was not worn");
            Chapter("切到预设2：新预设是空白的，各分类显示提示语（即默认装扮）");yield return Press("dress-preset-1",50);
            if(!string.IsNullOrEmpty(WardrobePresets.Id(1,category)))throw new Exception("New preset is not blank");
            Chapter("在预设2里选装扮：只改预设2，不影响当前穿着");yield return Press("dress-item-"+b,45);
            if(WardrobeSelection.Get(category).id!=a)throw new Exception("Editing an inactive preset changed the worn outfit");
            Chapter("点「使用」→ 换上预设2，按钮变为「使用中」");yield return Press("dress-preset-use",60);
            if(WardrobeSelection.Get(category).id!=b||WardrobePresets.Active!=1)throw new Exception("Preset 2 was not worn");
            Chapter("回到预设1并使用，装扮切回");yield return Press("dress-preset-0",35);yield return Press("dress-preset-use",60);
            if(WardrobeSelection.Get(category).id!=a||WardrobePresets.Active!=0)throw new Exception("Preset 1 was not worn again");
            Note("presets ok");
        }
        // Wardrobe eye on riichi / win / callout: full-screen preview table with the
        // entry crossfade, the looping script and the exit. All clocks are driven
        // per frame so the video plays at real speed.
        IEnumerator EffectTables(string only=null){
            var effects=GetComponent<TableEffects>();var table=GetComponent<TableRenderer>();var marker=GetComponent<WardrobeResponseIndicator>();
            var cases=new[]{("riichiEffect",6.6f,"2. 立直特效 · 点眼睛进入装扮预览牌桌：装扮页淡出、牌桌淡入；手把立直牌横着打出并播放特效（循环）"),
                            ("winEffect",9.5f,"和牌特效：自摸，手牌放倒、牌立着拍下后倒下，和牌特效在牌上爆开"),
                            ("callout",4.5f,"鸣牌指示：上家打出可碰的牌，牌上显示鸣牌指示特效")};
            foreach(var c in cases){
                if(only!=null&&c.Item1!=only)continue;
                lobby.PreviewWardrobe(c.Item1);
                var ids=WardrobeCatalog.Items(c.Item1).Where(i=>WardrobePackService.Installed(i.id)).Select(i=>i.id).ToArray();
                var pick=c.Item1=="callout"?ids.FirstOrDefault(id=>id.Contains("shandian"))??ids.LastOrDefault():ids.LastOrDefault();
                if(ids.Length>1)yield return Press("dress-item-"+pick,20);else yield return Hold(20);
                float origin=Time.unscaledTime;lobby.DesignPreviewClock=origin;
                effects.ValidationFreeze=effects.ValidationSequentialClock=true;effects.ValidationTime=0;table.ReviewRevealTime=0;marker.ValidationFreeze=true;marker.ValidationAge=0;
                Chapter(c.Item3);lobby.PreviewDressTable(c.Item1);
                for(int i=0;i<600&&!lobby.PreviewDressTableReady;i++)yield return Frame();
                if(!client.DressPreviewing||!lobby.PreviewDressTableReady)throw new Exception("Dress table did not open for "+c.Item1);
                int count=Mathf.CeilToInt(c.Item2*Fps);
                for(int f=0;f<count;f++){float t=f/(float)Fps;lobby.DesignPreviewClock=origin+t;effects.ValidationTime=t;table.ReviewRevealTime=t;marker.ValidationAge=t;yield return Frame();
                    if(c.Item1=="callout"&&f==Fps*2&&marker.EffectRoot==null)throw new Exception("Callout marker missing on the preview table");
                    if(c.Item1=="winEffect"&&f==Fps*5&&effects.SpawnedWinEffects==0)throw new Exception("Win effect did not play on the preview table");}
                Note(c.Item1+" previewed; win effects="+effects.SpawnedWinEffects);
                lobby.PreviewCloseDressTable();
                for(int f=0;f<15;f++){float t=c.Item2+f/(float)Fps;lobby.DesignPreviewClock=origin+t;effects.ValidationTime=t;table.ReviewRevealTime=t;yield return Frame();}
                if(client.DressPreviewing||client.State!=null)throw new Exception("Leaving the dress table did not restore the home state");
                effects.ValidationFreeze=effects.ValidationSequentialClock=false;table.ReviewRevealTime=-1;marker.ValidationFreeze=false;lobby.DesignPreviewClock=-1;yield return Hold(12);
            }
            Note("effect tables ok");
        }
        IEnumerator Roles(){
            lobby.PreviewHideCharacters();CharacterRoster.Use("yiji",400101);CharacterRoster.Use("erjietang",CharacterRoster.Find("erjietang").defaultSkin);CharacterRoster.Use("yiji",400101);
            Chapter("2. 切换雀士：给一姬选皮肤后切到其他角色，再切回");lobby.OpenRoleSelection();yield return Hold(40);
            yield return Press("active-character-skins",30);yield return Press("battle-skin-400103",40);
            Chapter("切到二阶堂：一姬的头像保持刚选的皮肤");yield return Press("battle-character-erjietang",60);
            if(CharacterRoster.BattleSkin("yiji")!=400103)throw new Exception("yiji battle skin was not remembered");
            Chapter("切回一姬：沿用上次选的皮肤并已保存");yield return Press("battle-character-yiji",60);
            if(CharacterRoster.Active.skin!=400103)throw new Exception("yiji did not return with remembered skin");
            Note("battle skin memory ok");
            lobby.PreviewHideCharacters();yield return Hold(10);
        }
        IEnumerator Ducking(){
            Chapter("3. 角色语音页：BGM 缓慢压低（右上角数值）");lobby.PreviewDorm("yiji",400101,"info");yield return Hold(30);
            yield return Press("voice-lantern",75);
            Chapter("退出语音页：BGM 平滑恢复");
            typeof(LobbyController).GetMethod("CharacterBack",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(lobby,null);Note("character back");yield return Hold(80);
            if(PlaylistAudio.Active!=null&&PlaylistAudio.Active.DuckGain<.99f)throw new Exception("BGM duck did not release: "+PlaylistAudio.Active.DuckGain);
            lobby.PreviewHideCharacters();yield return Hold(10);
        }
        static bool MotionMode=>Environment.GetEnvironmentVariable("YUNJIAN_R33_MODE")=="motion";
        // Tile motion preview on the validation clock, so the video plays at real speed
        // regardless of capture speed. rate<1 is slow motion.
        // lead>0: the hand is first shown in play (hands standing) for lead seconds,
        // so the switch into the result is visible.
        // skip: run the first seconds of the result unrecorded (slow motion of the hand only).
        IEnumerator PlayFixture(string file,string room,float seconds,float rate,bool events,bool expectRonTile=false,float lead=0,float skip=0){
            var effects=GetComponent<TableEffects>();var table=GetComponent<TableRenderer>();
            var view=JsonUtility.FromJson<RoomView>(File.ReadAllText(repo+"/build/unity-smoke/"+file));
            foreach(var p in view.players){p.character="yiji";p.skin=400101;p.wardrobe=WardrobeLoadout.Local();}
            view.audioRoom=room;view.resourceSetup=null;view.dealtAt=0;view.openingHand=new string[0];
            var sounds=view.audioEvents;int seq=view.audioSeq;view.audioEvents=new PublicSound[0];view.audioSeq=Math.Max(0,seq-1);
            effects.Reset();effects.ValidationFreeze=effects.ValidationSequentialClock=true;effects.ValidationTime=0;table.ReviewRevealTime=0;
            int frames=Mathf.CeilToInt(seconds/rate*Fps),leadFrames=Mathf.CeilToInt(lead/rate*Fps);
            if(lead>0){
                var before=JsonUtility.FromJson<RoomView>(JsonUtility.ToJson(view));before.phase="play";before.result=null;
                client.ShowValidationFixture(before);client.ValidationTableOnly=true;client.ValidationIntro.Reset();effects.Accept(before);
                for(int f=0;f<leadFrames;f++){float t=f*rate/Fps;effects.ValidationTime=t;table.ReviewRevealTime=t;yield return Frame();}
                client.ValidationApplyState(view);effects.Accept(view);
            }else{client.ShowValidationFixture(view);client.ValidationTableOnly=true;client.ValidationIntro.Reset();effects.Accept(view);}
            if(events){view.audioEvents=sounds;view.audioSeq=seq;effects.Accept(view);}
            for(float t=0;t<skip;t+=1f/Fps){effects.ValidationTime=t;table.ReviewRevealTime=t;yield return null;}
            if(skip>0){
                for(int f=0;f<frames;f++){float t=skip+f*rate/Fps;effects.ValidationTime=t;table.ReviewRevealTime=t;yield return Frame();}
                effects.ValidationFreeze=effects.ValidationSequentialClock=false;table.ReviewRevealTime=-1;effects.Reset();yield break;}
            for(int f=leadFrames;f<leadFrames+frames;f++){float t=f*rate/Fps;effects.ValidationTime=t;table.ReviewRevealTime=t;yield return Frame();}
            if(expectRonTile){var winner=view.result.details[0].seat;if(GameObject.Find("PublicTableState")?.GetComponentsInChildren<Transform>(true).Any(t=>t.name.StartsWith("Ron tile "))==true)throw new Exception("Ron tile was appended beside seat "+winner);Note("ron tile stays in river, seat "+winner);}
            effects.ValidationFreeze=effects.ValidationSequentialClock=false;table.ReviewRevealTime=-1;effects.Reset();
        }
        IEnumerator Motion(){
            lobby.PreviewHideCharacters();
            Chapter("荒牌流局·本家听牌（正常速度）：本家手牌先切到桌面立着，左右家扣倒 +0.35s，本家 +0.55s 朝自己推倒亮牌");yield return PlayFixture("draw-4-1.json","r35-draw-a",2.5f,1f,false,false,.8f);
            Chapter("荒牌流局·本家听牌（0.25 倍慢放）");yield return PlayFixture("draw-4-1.json","r35-draw-b",1.1f,.25f,false,false,.2f);
            Chapter("流局·全员未听牌：牌面始终朝向持有者，立着朝前扣倒；本家最后扣倒");yield return PlayFixture("draw-4-0.json","r35-noten-a",2.5f,1f,false,false,.8f);
            Chapter("九种九牌（本家）：手牌先切到桌面立着，0.55s 后朝自己推倒亮牌");yield return PlayFixture("nine-4-0.json","r35-nine-a",2.2f,1f,false,false,.8f);
            Chapter("九种九牌（0.25 倍慢放）");yield return PlayFixture("nine-4-0.json","r35-nine-b",1.0f,.25f,false,false,.2f);
            Chapter("别家自摸（下家）：拍牌时手牌仍立着，和牌特效爆开时才放倒亮牌");yield return PlayFixture("win-4-tsumo-1-0.json","r35-other-a",7f,1f,true);
            Chapter("别家自摸（0.5 倍慢放）");yield return PlayFixture("win-4-tsumo-1-0.json","r35-other-b",6f,.5f,true);
            Chapter("本家自摸（对照，未改）：手牌先放倒，牌立着拍下后倒下");yield return PlayFixture("win-4-tsumo-0-0.json","r35-tsumo-a",7f,1f,true);
            Chapter("荣和（对照，未改）：和牌特效爆开的同一帧，荣和的牌接到手牌右端");yield return PlayFixture("win-4-ron-0-0.json","r35-ron",6f,1f,true,true);
            Chapter("编辑器预览（无声）；节奏以真机为准");yield return Hold(45);
        }
        // ---- 2026-11-08 r37: tile interpenetration scan ----
        // Renders sanma and four-player tables with 0..4 kita tiles, 0..4 melds, the
        // drawn tile, a self draw and a ron for every seat, settles the reveal and
        // tests every pair of tile boxes (oriented, shrunk by a touching margin).
        static readonly string[] ScanMelds={"p555=","s1111","z6666","m999+"};
        static readonly string[] ScanFaces={"p1","p2","p3","p4","p5","p6","p7","s1","s2","s3","s4","z1","z1","z1"};
        static Vector3[] Corners(BoxCollider box){var c=new Vector3[8];var h=box.size*.5f;for(int i=0;i<8;i++)c[i]=box.transform.TransformPoint(box.center+Vector3.Scale(h,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));return c;}
        // Separating-axis test on two oriented boxes; returns the smallest penetration (<=0: apart).
        static float Penetration(Vector3[] a,Vector3[] b){
            Vector3[] Axes(Vector3[] c)=>new[]{(c[1]-c[0]).normalized,(c[2]-c[0]).normalized,(c[4]-c[0]).normalized};
            var axes=new List<Vector3>();axes.AddRange(Axes(a));axes.AddRange(Axes(b));foreach(var x in Axes(a))foreach(var y in Axes(b)){var z=Vector3.Cross(x,y);if(z.sqrMagnitude>1e-6f)axes.Add(z.normalized);}
            float least=float.MaxValue;
            foreach(var axis in axes){float a0=float.MaxValue,a1=float.MinValue,b0=float.MaxValue,b1=float.MinValue;foreach(var p in a){float d=Vector3.Dot(p,axis);a0=Mathf.Min(a0,d);a1=Mathf.Max(a1,d);}foreach(var p in b){float d=Vector3.Dot(p,axis);b0=Mathf.Min(b0,d);b1=Mathf.Max(b1,d);}
                least=Mathf.Min(least,Mathf.Min(a1,b1)-Mathf.Max(a0,b0));if(least<=0)return least;}
            return least;
        }
        static string PathOf(Transform t,Transform root){var parts=new List<string>();for(var x=t;x!=null&&x!=root;x=x.parent)parts.Insert(0,x.name);return string.Join("/",parts);}
        static string Group(string path)=>System.Text.RegularExpressions.Regex.Replace(path,@"\d+","#");
        RoomView ScanState(int n,string kind,int seat,int nuki,int melds,bool keepEvents=false){
            string file=kind=="play"?"preview-"+n+".json":kind=="draw"?"draw-"+n+"-0.json":"win-"+n+"-"+kind+"-"+seat+"-0.json";var path=repo+"/build/unity-smoke/"+file;if(!File.Exists(path))return null;
            var v=JsonUtility.FromJson<RoomView>(File.ReadAllText(path));if(!keepEvents){v.audioEvents=new PublicSound[0];v.audioSeq=0;}v.options=new Choice[0];v.pending=null;v.resourceSetup=null;v.dealtAt=0;v.openingHand=new string[0];v.audioRoom="r37-scan-"+n+kind+seat+nuki+melds;v.you=0;
            var open=ScanMelds.Take(melds).ToArray();
            for(int i=0;i<v.players.Length;i++){var p=v.players[i];p.character="yiji";p.skin=400101;p.wardrobe=WardrobeLoadout.Local();bool mine=i==seat;p.melds=mine?open:new string[0];p.nuki=mine?nuki:0;p.count=mine?(kind=="play"?14:13)-melds*3:13;}
            int concealed=13-melds*3;
            if(kind=="play"){v.phase="play";v.result=null;v.turn=seat;v.hand=ScanFaces.Take(seat==0?concealed+1:13).ToArray();v.drawn=seat==0?v.hand.Last():null;}
            else if(kind=="draw"){v.hand=ScanFaces.Take(seat==0?concealed:13).ToArray();foreach(var d in v.result.drawDetails){bool mine=d.seat==seat;d.tenpai=mine;d.tiles=mine?ScanFaces.Take(concealed).ToArray():new string[0];}}
            else{v.hand=ScanFaces.Take(13).ToArray();var d=v.result.details[0];d.melds=open;d.nuki=nuki;d.tiles=ScanFaces.Take(concealed).ToArray();if(seat==0)v.hand=ScanFaces.Take(concealed).ToArray();}
            return v;
        }
        IEnumerator OverlapScan(){
            var table=GetComponent<TableRenderer>();var effects=GetComponent<TableEffects>();lobby.PreviewHideCharacters();
            var report=new List<string>();var kinds=new Dictionary<string,int>();int states=0,tested=0,shots=0;
            foreach(int n in new[]{3,4})foreach(var kind in new[]{"play","tsumo","ron","draw"})for(int seat=0;seat<n;seat++)for(int nuki=0;nuki<=(n==3?4:0);nuki++)foreach(int melds in new[]{0,1,2,3,4}){
                var v=ScanState(n,kind,seat,nuki,melds);if(v==null)continue;
                effects.Reset();table.ReviewRevealTime=0;client.ShowValidationFixture(v);client.ValidationTableOnly=true;client.ValidationIntro.Reset();yield return null;
                table.ReviewRevealTime=20;yield return null;yield return null;states++;
                var root=GameObject.Find("PublicTableState")?.transform;if(root==null)throw new Exception("PublicTableState missing");
                var boxes=root.GetComponentsInChildren<BoxCollider>(false).Where(b=>b.gameObject.activeInHierarchy).Select(b=>(box:b,corners:Corners(b),path:PathOf(b.transform,root))).ToArray();
                for(int i=0;i<boxes.Length;i++)for(int j=i+1;j<boxes.Length;j++){
                    tested++;float depth=Penetration(boxes[i].corners,boxes[j].corners);if(depth<=.004f)continue;
                    string pair=Group(boxes[i].path)+"  ×  "+Group(boxes[j].path);kinds[pair]=kinds.TryGetValue(pair,out var c)?c+1:1;
                    var axis=root.Find("Seat "+seat);string Local(BoxCollider b){var p=axis!=null?axis.InverseTransformPoint(b.transform.TransformPoint(b.center)):b.transform.position;return "("+p.x.ToString("0.00")+","+p.z.ToString("0.00")+")";}
                    if(kinds[pair]<=3)report.Add("n="+n+" "+kind+" seat="+seat+" nuki="+nuki+" melds="+melds+" depth="+depth.ToString("0.000")+"  "+boxes[i].path+Local(boxes[i].box)+"  ×  "+boxes[j].path+Local(boxes[j].box));
                    if(shots<6&&kinds[pair]==1){shots++;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(output+"/overlap-"+shots+".png");yield return null;}
                }
            }
            table.ReviewRevealTime=-1;client.ValidationTableOnly=false;effects.Reset();
            var lines=new List<string>{Screen.width+"x"+Screen.height+": states "+states+", pairs tested "+tested+", overlapping pair kinds "+kinds.Count};lines.AddRange(kinds.OrderByDescending(k=>k.Value).Select(k=>k.Value+"  "+k.Key));lines.Add("");lines.AddRange(report);
            scanStates=states;File.WriteAllLines(output+"/overlap-scan-"+Screen.width+"x"+Screen.height+".txt",lines);Note("overlap scan: "+lines[0]);
            if(kinds.Count>0&&Environment.GetEnvironmentVariable("YUNJIAN_R33_ALLOW_OVERLAP")!="1")throw new Exception("Tile overlaps found: "+lines[0]+"\n"+string.Join("\n",lines.Skip(1).Take(12)));
        }
        // Kita tiles must sit in the same place before and after the hand is shown:
        // any difference reads as the tiles being nudged by the revealed row.
        IEnumerator NorthStable(){
            var table=GetComponent<TableRenderer>();var effects=GetComponent<TableEffects>();int checkedStates=0;float worst=0;
            for(int seat=1;seat<3;seat++)for(int nuki=1;nuki<=4;nuki++)for(int melds=0;melds<=4;melds++){
                Vector3[] Positions(){var root=GameObject.Find("PublicTableState").transform.Find("Seat "+seat);return root.GetComponentsInChildren<Transform>(true).Where(t=>t.parent!=null&&t.parent.name=="Extracted North").OrderBy(t=>t.name).ThenBy(t=>t.GetSiblingIndex()).Select(t=>t.position).ToArray();}
                Vector3[] reference=null;
                foreach(var kind in new[]{"play","tsumo","ron"}){
                    var v=ScanState(3,kind,seat,nuki,melds);if(v==null)continue;
                    effects.Reset();table.ReviewRevealTime=0;client.ShowValidationFixture(v);client.ValidationTableOnly=true;client.ValidationIntro.Reset();yield return null;table.ReviewRevealTime=20;yield return null;yield return null;
                    var now=Positions();if(now.Length!=nuki)throw new Exception("Kita count "+now.Length+" for nuki "+nuki);
                    if(reference==null){reference=now;continue;}
                    for(int i=0;i<now.Length;i++){float d=Vector3.Distance(now[i],reference[i]);worst=Mathf.Max(worst,d);if(d>.001f)throw new Exception("Kita moved "+d.ToString("0.000")+" at "+kind+" seat="+seat+" nuki="+nuki+" melds="+melds);}
                    checkedStates++;
                }
            }
            table.ReviewRevealTime=-1;client.ValidationTableOnly=false;effects.Reset();Note("kita stable across play/tsumo/ron: "+checkedStates+" comparisons, worst "+worst.ToString("0.0000"));
        }
        // ---- r39: long win effect hold and the AI outfit setting ----
        static bool R39Mode=>Environment.GetEnvironmentVariable("YUNJIAN_R33_MODE")=="r39";
        string status;
        IEnumerator LongWinEffect(string effectId,string label){
            var effects=GetComponent<TableEffects>();var table=GetComponent<TableRenderer>();
            var v=JsonUtility.FromJson<RoomView>(File.ReadAllText(repo+"/build/unity-smoke/win-4-ron-0-0.json"));v.audioRoom="r39-"+effectId+"-"+frames;v.resourceSetup=null;v.dealtAt=0;v.openingHand=new string[0];
            foreach(var p in v.players){p.character="yiji";p.skin=400101;p.wardrobe=WardrobeLoadout.Local();}
            int winner=v.result.details[0].seat;v.players[winner].wardrobe.winEffect=effectId;var sounds=v.audioEvents;
            Chapter("1. "+label+"：结算等和牌特效真正播完才出现（右上角为验证时钟上的实时状态）");
            float effectEnd=-1,resultAt=-1;
            int seq=sounds.Max(s=>s.id);v.audioEvents=new PublicSound[0];v.audioSeq=seq-1;
            effects.Reset();effects.ValidationFreeze=effects.ValidationSequentialClock=true;effects.ValidationTime=0;table.ReviewRevealTime=0;
            client.ShowValidationFixture(v);client.ValidationTableOnly=true;client.ValidationIntro.Reset();effects.Accept(v);v.audioEvents=sounds;v.audioSeq=seq;effects.Accept(v);
            bool started=false;
            for(int f=0;f<Fps*14;f++){
                float t=f/(float)Fps;effects.ValidationTime=t;table.ReviewRevealTime=t;
                bool playing=effects.WinEffectPlaying,showing=effects.ShowWinPresentation;started|=playing;
                if(started&&!playing&&effectEnd<0)effectEnd=t;if(started&&!showing&&resultAt<0)resultAt=t;
                status="t="+t.ToString("0.0")+"s · 和牌特效："+(playing?"播放中":started?"已结束":"未开始")+" · "+(showing?"牌桌演出中":"→ 进入结算");
                yield return Frame();
                if(resultAt>=0&&t>resultAt+1)break;
            }
            status=null;Note(effectId+" effect ended at "+effectEnd+"s, result allowed at "+resultAt+"s");
            if(effectEnd<0||resultAt<0||resultAt+.001f<effectEnd)throw new Exception("Settlement opened before the win effect ended: effect "+effectEnd+" result "+resultAt);
            effects.ValidationFreeze=effects.ValidationSequentialClock=false;table.ReviewRevealTime=-1;effects.Reset();
        }
        IEnumerator R39(){
            BeginRecording();lobby.PreviewHideCharacters();
            yield return LongWinEffect("ron-fate-hf","和牌-咒层界・恶念祝祭");
            client.BeginLobbyValidation("review-r33");client.SessionPreviewOnly=true;lobby.DesignPreviewGardenEntry();yield return Hold(20);
            Chapter("5. 设置 · 偏好：新增「电脑装扮」，可选默认装扮或从本机已下载的装扮中随机");
            client.LobbyOpenSettings();var ui=typeof(RiichiClient).GetField("nativeSettings",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(client);ui.GetType().GetField("Tab").SetValue(ui,3);
            AppPreferences.SetRandomBotWardrobe(false);yield return Hold(50);AppPreferences.SetRandomBotWardrobe(true);Note("bot wardrobe random on");yield return Hold(60);
            AppPreferences.SetRandomBotWardrobe(false);client.LobbyCloseSettings();
            Chapter("编辑器预览（无声）；以真机为准");yield return Hold(30);
        }
        // ---- r40: preparation panel, dorm backdrop, four-seat previews, win timing, double ron, reveal speed ----
        static bool R41Mode=>Environment.GetEnvironmentVariable("YUNJIAN_R33_MODE")=="r41";
        IEnumerator R41(){
            BeginRecording();lobby.PreviewHideCharacters();
            yield return WinToResult("win-double-ron.json",null,"一炮双响：两家同时喊和，满贯以上依次切入横幅，再播放第一家的和牌特效",18);
            client.BeginLobbyValidation("review-r33");client.SessionPreviewOnly=true;lobby.DesignPreviewGardenEntry();yield return Hold(10);
            yield return FourWayPreview("stick",2*6.2f+.4f,"立直棒预览：打出立直后放置立直棒，换家时不黑屏，右上角只有关闭按钮");
            Chapter("end");yield return Hold(10);
        }        static bool R40Mode=>Environment.GetEnvironmentVariable("YUNJIAN_R33_MODE")=="r40";
        IEnumerator PreparationPanel(){
            var v=JsonUtility.FromJson<RoomView>(File.ReadAllText(repo+"/build/unity-smoke/preview-4.json"));v.phase="lobby";v.host=true;v.pin="154925";v.result=null;v.audioEvents=new PublicSound[0];v.options=new Choice[0];v.pending=null;v.audioRoom="r40-prep";v.lobby=new LobbyRoomView{category="friend",red=true};
            for(int i=0;i<v.players.Length;i++){var p=v.players[i];p.character="yiji";p.skin=400101;p.ready=true;p.online=true;p.bot=i>0;p.name=i==0?"旅人":"AI · "+i;p.river=new RiverTile[0];}
            v.resourceSetup=new ResourceSetupView{revision=1,choices=v.players.Select(p=>new CharacterChoice(p.character,p.skin)).ToArray(),players=Enumerable.Range(0,v.players.Length).Select(i=>new ResourcePeerState{seat=i,state=i==0?"pending":"ready"}).ToArray()};
            var coordinator=GetComponent<ResourceRoomCoordinator>();coordinator.ReviewDisplayOnly=true;coordinator.enabled=true;
            Chapter("1. 开局资源准备：字号加大；进度条显示本机加载到哪一步、加载什么；每位玩家显示「正在加载/已就绪」，电脑无需加载");
            client.ShowValidationFixture(v);
            string[] labels={"角色立绘与语音","角色立绘与语音","和牌-咒层界・恶念祝祭","立直-炽焰","立直棒-小鱼"};
            for(int f=0;f<75;f++){int done=f*14/75;coordinator.ReviewProgress(done,14,labels[Mathf.Min(labels.Length-1,done/3)],false);yield return Frame();}
            coordinator.ReviewProgress(14,14,null,true);v.resourceSetup.players[0].state="ready";client.ShowValidationFixture(v);yield return Hold(45);
            coordinator.enabled=false;coordinator.ReviewDisplayOnly=false;
            client.BeginLobbyValidation("review-r33");client.SessionPreviewOnly=true;lobby.DesignPreviewGardenEntry();yield return Hold(10);
        }
        IEnumerator DormTransitions(){
            var night=WardrobeCatalog.Items("lobbyBackground").Where(i=>WardrobePackService.Installed(i.id)).Skip(1).FirstOrDefault();
            if(night!=null)WardrobeSelection.Save(new[]{new KeyValuePair<string,string>("lobbyBackground",night.id)});Note("lobby background "+night?.id);
            Chapter("3. 进出宿舍：淡入淡出时垫底的是自己选的大厅背景，不再闪出默认庭院");
            lobby.DesignPreviewGardenEntry();yield return Hold(40);
            lobby.OpenDorm();yield return Hold(45);
            typeof(LobbyController).GetMethod("CharacterBack",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(lobby,null);yield return Hold(50);
            lobby.OpenDorm();yield return Hold(30);typeof(LobbyController).GetMethod("CharacterBack",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(lobby,null);yield return Hold(40);
            lobby.PreviewHideCharacters();
        }
        IEnumerator FourWayPreview(string category,float seconds,string text){
            var effects=GetComponent<TableEffects>();var table=GetComponent<TableRenderer>();var marker=GetComponent<WardrobeResponseIndicator>();
            lobby.PreviewWardrobe(category);
            var ids=WardrobeCatalog.Items(category).Where(i=>WardrobePackService.Installed(i.id)).Select(i=>i.id).ToArray();
            var pick=category=="callout"?ids.FirstOrDefault(id=>id.Contains("shandian"))??ids.LastOrDefault():ids.LastOrDefault();
            if(ids.Length>1)yield return Press("dress-item-"+pick,10);else yield return Hold(10);
            float origin=Time.unscaledTime;lobby.DesignPreviewClock=origin;
            effects.ValidationFreeze=effects.ValidationSequentialClock=true;effects.ValidationTime=0;table.ReviewRevealTime=0;marker.ValidationFreeze=true;marker.ValidationAge=0;
            Chapter(text);lobby.PreviewDressTable(category);
            for(int i=0;i<600&&!lobby.PreviewDressTableReady;i++)yield return Frame();
            int count=Mathf.CeilToInt(seconds*Fps);
            for(int f=0;f<count;f++){float t=f/(float)Fps;lobby.DesignPreviewClock=origin+t;effects.ValidationTime=t;table.ReviewRevealTime=t;marker.ValidationAge=t;yield return Frame();}
            lobby.PreviewCloseDressTable();
            for(int f=0;f<12;f++){float t=seconds+f/(float)Fps;lobby.DesignPreviewClock=origin+t;effects.ValidationTime=t;table.ReviewRevealTime=t;yield return Frame();}
            effects.ValidationFreeze=effects.ValidationSequentialClock=false;table.ReviewRevealTime=-1;marker.ValidationFreeze=false;lobby.DesignPreviewClock=-1;lobby.PreviewHideCharacters();yield return Hold(6);
        }
        IEnumerator WinToResult(string file,string effectId,string text,float limit=14){
            var effects=GetComponent<TableEffects>();var table=GetComponent<TableRenderer>();
            var v=JsonUtility.FromJson<RoomView>(File.ReadAllText(repo+"/build/unity-smoke/"+file));v.audioRoom="r40-"+file+"-"+effectId+"-"+frames;v.resourceSetup=null;v.dealtAt=0;v.openingHand=new string[0];
            foreach(var p in v.players){p.character="yiji";p.skin=400101;p.wardrobe=WardrobeLoadout.Local();}
            if(effectId!=null)foreach(var d in v.result.details)v.players[d.seat].wardrobe.winEffect=effectId;
            foreach(var d in v.result.details)Note(file+" winner "+d.seat+" fanshu="+d.score?.fanshu+" fu="+d.score?.fu+" limit="+d.limit+" banner="+WinCutIn.Eligible(d));
            var sounds=v.audioEvents;int seq=sounds.Max(s=>s.id);v.audioEvents=new PublicSound[0];v.audioSeq=sounds.Min(s=>s.id)-1;
            Chapter(text);float effectEnd=-1,resultAt=-1;bool started=false;
            effects.Reset();effects.ValidationFreeze=effects.ValidationSequentialClock=true;effects.ValidationTime=0;table.ReviewRevealTime=0;
            client.ShowValidationFixture(v);client.ValidationTableOnly=true;client.ValidationIntro.Reset();effects.Accept(v);v.audioEvents=sounds;v.audioSeq=seq;effects.Accept(v);
            for(int f=0;f<Fps*limit;f++){
                float t=f/(float)Fps;effects.ValidationTime=t;table.ReviewRevealTime=t;
                bool playing=effects.WinEffectPlaying,showing=effects.ShowWinPresentation;started|=playing;
                if(started&&!playing&&effectEnd<0)effectEnd=t;if(started&&!showing&&resultAt<0)resultAt=t;
                status="t="+t.ToString("0.0")+"s · 和牌特效："+(playing?"播放中":started?"已结束":"未开始")+" · "+(showing?"牌桌演出中":"→ 进入结算")+(effects.CutIn.Active?" · 角色横幅":"");
                yield return Frame();if(resultAt>=0&&t>resultAt+.8f)break;
            }
            status=null;Note(file+" "+effectId+" effect ended "+effectEnd+"s, result allowed "+resultAt+"s");
            if(resultAt>=0&&effectEnd>=0&&resultAt+.001f<effectEnd)throw new Exception("Settlement opened before the effect ended: "+resultAt+" < "+effectEnd);
            effects.ValidationFreeze=effects.ValidationSequentialClock=false;table.ReviewRevealTime=-1;effects.Reset();
        }
        IEnumerator R40(){
            BeginRecording();lobby.PreviewHideCharacters();
            yield return PreparationPanel();
            yield return DormTransitions();
            yield return FourWayPreview("riichiEffect",4*5.2f+.6f,"5. 立直特效预览：依次由自家 → 上家 → 对家 → 下家打出立直（屏幕上顺时针），每家都穿着预览的装扮");
            yield return FourWayPreview("winEffect",4*8.5f+.6f,"5. 和牌特效预览：依次由自家 → 上家 → 对家 → 下家自摸");
            yield return FourWayPreview("callout",3*3.6f+.4f,"5. 鸣牌指示预览：依次由上家 → 对家 → 下家打出可碰的牌");
            lobby.PreviewHideCharacters();
            yield return WinToResult("win-4-ron-0-0.json","ron-flame","4. 普通和牌特效（炽焰）：特效约 2 秒就看不见了，按原版的结算时机进入结算，不再等满 6 秒");
            yield return WinToResult("win-4-ron-0-0.json","ron-fate-hf","4. 咒层界：特效比原版结算时机更长，等最后的粒子消失再进入结算");
            yield return WinToResult("win-double-ron.json",null,"6. 一炮双响：两家依次切入角色横幅（满贯以上才有横幅），之后同一张牌上播放和牌特效，再进入结算",18);
            Chapter("7. 流局推倒/扣倒（正常速度）：本家先立在桌上，0.55s 后扣倒，动作用时 0.10s（原为 0.05s）");yield return PlayFixture("draw-4-0.json","r40-draw-a",2.5f,1f,false,false,.8f);
            Chapter("7. 同上（0.25 倍慢放）");yield return PlayFixture("draw-4-0.json","r40-draw-b",1.1f,.25f,false,false,.2f);
            Chapter("编辑器预览（无声）；以真机为准");yield return Hold(30);
        }
        static bool OverlapMode=>Environment.GetEnvironmentVariable("YUNJIAN_R33_MODE")=="overlap";
        static bool R37Mode=>Environment.GetEnvironmentVariable("YUNJIAN_R33_MODE")=="r37";
        // Plays a prepared view: the table first without events, then the events on the validation clock.
        IEnumerator PlayView(RoomView view,float seconds,float rate,PublicSound[] sounds,float skip=0){
            var effects=GetComponent<TableEffects>();var table=GetComponent<TableRenderer>();
            int seq=sounds!=null&&sounds.Length>0?sounds.Max(s=>s.id):0;view.audioEvents=new PublicSound[0];view.audioSeq=Math.Max(0,seq-1);
            effects.Reset();effects.ValidationFreeze=effects.ValidationSequentialClock=true;effects.ValidationTime=0;table.ReviewRevealTime=0;
            client.ShowValidationFixture(view);client.ValidationTableOnly=true;client.ValidationIntro.Reset();effects.Accept(view);
            if(sounds!=null&&sounds.Length>0){view.audioEvents=sounds;view.audioSeq=seq;effects.Accept(view);}
            for(float t=0;t<skip;t+=1f/Fps){effects.ValidationTime=t;table.ReviewRevealTime=t;yield return null;}
            int frames=Mathf.CeilToInt(seconds/rate*Fps);
            for(int f=0;f<frames;f++){float t=skip+f*rate/Fps;effects.ValidationTime=t;table.ReviewRevealTime=t;yield return Frame();}
            effects.ValidationFreeze=effects.ValidationSequentialClock=false;table.ReviewRevealTime=-1;effects.Reset();
        }
        RoomView RiichiState(int seat){
            var v=JsonUtility.FromJson<RoomView>(File.ReadAllText(repo+"/build/unity-smoke/preview-4.json"));v.audioRoom="r37-riichi-"+seat+"-"+frames;v.options=new Choice[0];v.pending=null;v.resourceSetup=null;v.dealtAt=0;v.openingHand=new string[0];v.turn=seat;
            foreach(var p in v.players){p.character="yiji";p.skin=400101;p.wardrobe=WardrobeLoadout.Local();}
            var river=v.players[seat].river.ToList();river.Add(new RiverTile{tile="z1",riichi=true});v.players[seat].river=river.ToArray();v.players[seat].riichi=1;
            return v;
        }
        // The discard hand, then the hand that carries the riichi stick to the table centre.
        static PublicSound[] RiichiSounds(RoomView v,int seat)=>new[]{new PublicSound{id=1,kind="discard",seat=seat,tile="z1",riichi=true,riverIndex=v.players[seat].river.Length},new PublicSound{id=2,kind="riichi",seat=seat},new PublicSound{id=3,kind="riichi-stick",seat=seat}};
        IEnumerator RiichiArm(int seat,string side){
            var fx=GetComponent<TableEffects>();fx.SuppressShouts=true;
            foreach(bool authored in new[]{true,false}){
                TableEffects.ValidationAuthoredArm=authored;
                Chapter("5. 立直手部（"+side+"，20:9 屏）· "+(authored?"修改前：手臂原长，袖口末端露在画面里":"修改后：前臂沿自身方向加长，袖口移出画面；手掌与手腕不变")+"（本段关闭立直喊话，便于看手臂）");
                var v=RiichiState(seat);yield return PlayView(v,4.2f,1f,RiichiSounds(v,seat));
            }
            Chapter("5. 立直手部（"+side+"，修改后，0.5 倍慢放）");var slow=RiichiState(seat);yield return PlayView(slow,4.2f,.5f,RiichiSounds(slow,seat),0);
            TableEffects.ValidationAuthoredArm=false;fx.SuppressShouts=false;
        }
        IEnumerator FoldedResourceRoom(){
            var v=JsonUtility.FromJson<RoomView>(File.ReadAllText(repo+"/build/unity-smoke/preview-3.json"));v.phase="lobby";v.host=true;v.pin="154925";v.result=null;v.audioEvents=new PublicSound[0];v.options=new Choice[0];v.pending=null;v.audioRoom="r37-room";v.lobby=new LobbyRoomView{category="friend",red=true};
            for(int i=0;i<v.players.Length;i++){var p=v.players[i];p.character="yiji";p.skin=400101;p.ready=true;p.online=true;p.bot=i>0;p.name=i==0?"旅人":"电脑（简单）";p.river=new RiverTile[0];}
            v.resourceSetup=new ResourceSetupView{revision=1,choices=v.players.Select(p=>new CharacterChoice(p.character,p.skin)).ToArray(),players=new ResourcePeerState[0]};
            var coordinator=GetComponent<ResourceRoomCoordinator>();coordinator.ReviewFolded=true;
            Chapter("4/6. 收起开局资源准备后：「资源准备」按钮与「开始」同款同尺寸，并排对齐在同一行");
            client.ShowValidationFixture(v);yield return Hold(75);
            var reopen=lobby.ValidationTarget("resource-reopen");var start=lobby.ValidationTarget("start");Note("room buttons reopen="+reopen+" start="+start);
            if(reopen.width<=0)throw new Exception("Folded resource button was not drawn on the room page");
            if(Mathf.Abs(reopen.y-start.y)>1||Mathf.Abs(reopen.height-start.height)>1||Mathf.Abs(reopen.width-start.width)>1)throw new Exception("Resource button is not aligned with start: "+reopen+" / "+start);
            coordinator.ReviewFolded=false;
        }
        IEnumerator R37(){
            // Scan first (unrecorded); any interpenetration fails the run.
            Environment.SetEnvironmentVariable("YUNJIAN_R33_ALLOW_OVERLAP","0");yield return NorthStable();yield return OverlapScan();
            client.BeginLobbyValidation("review-r33");client.SessionPreviewOnly=true;lobby.DesignPreviewGardenEntry();yield return Wait(30);BeginRecording();
            Chapter("3. 预览装扮：进入预览牌桌时先播放开局入场动画（其余三家为「特效演示1/2/3」，角色固定一姬），加载在动画后面完成");
            yield return EffectTables("riichiEffect");
            lobby.PreviewHideCharacters();yield return FoldedResourceRoom();
            Chapter("1. 别家荣和：和的牌留在放铳者的牌河里，和牌者手牌旁不再多出一张（对照原版录像）");yield return PlayFixture("win-3-ron-2-0.json","r37-ron-a",6.5f,1f,true,true);
            var tsumo=ScanState(3,"tsumo",1,2,0,true);
            Chapter("7. 别家自摸 + 2 张拔北：北牌从打牌时起就让出亮牌和自摸牌的位置，和牌时不再被推动，与和的牌留出间隙");yield return PlayView(tsumo,6f,1f,tsumo.audioEvents);
            var kan=ScanState(3,"tsumo",0,2,3,true);
            Chapter("7. 本家 3 副露 + 2 张拔北：北牌让开副露，不再压在暗杠上");yield return PlayView(kan,5f,1f,kan.audioEvents);
            Chapter("7. 自动检查：三麻/四麻 × 0~4 张拔北 × 0~4 副露 × 摸牌/自摸/荣和 × 每个座位，共 "+scanStates+" 种牌桌，0 处牌与牌穿模");yield return Hold(75);
            yield return RiichiArm(3,"左家");yield return RiichiArm(1,"右家");
            Chapter("编辑器预览（无声）；以真机为准");yield return Hold(45);
        }
        int scanStates;
        static bool TsumoMode=>Environment.GetEnvironmentVariable("YUNJIAN_R33_MODE")=="tsumo";
        IEnumerator Tsumo(){
            // The dress table needs the lobby state, so it runs before the fixtures.
            yield return EffectTables("winEffect");
            lobby.PreviewHideCharacters();
            Chapter("本家自摸（正常速度）：手按原版比例（约五张牌宽），先抬向镜头再划弧砸到落点，牌立着落下后倒下");yield return PlayFixture("win-4-tsumo-0-0.json","r36-tsumo-a",4.2f,1f,true);
            Chapter("本家自摸（0.25 倍慢放）：手不再竖直下落，也不再贴着桌面穿过立着的牌");yield return PlayFixture("win-4-tsumo-0-0.json","r36-tsumo-b",1.5f,.25f,true,false,0,2.2f);
            Chapter("别家自摸（下家，正常速度）");yield return PlayFixture("win-4-tsumo-1-0.json","r36-other-a",5f,1f,true);
            Chapter("别家自摸（0.5 倍慢放）");yield return PlayFixture("win-4-tsumo-1-0.json","r36-other-b",2.2f,.5f,true,false,0,2.0f);
            Chapter("编辑器预览（无声）；节奏以真机为准");yield return Hold(45);
        }
        IEnumerator Interrupt(){
            Chapter("4. 连续拔北：新语音打断上一句，不再排队（右上角显示）");
            var view=JsonUtility.FromJson<RoomView>(File.ReadAllText(repo+"/build/unity-smoke/preview-4.json"));view.audioRoom="review-r33";view.audioSeq=0;view.audioEvents=new PublicSound[0];foreach(var p in view.players){p.character="yiji";p.skin=400101;}
            roleVoice.Observe(view);int before=roleVoice.Interruptions;
            foreach(var e in new[]{new PublicSound{id=1,kind="nuki",seat=0},new PublicSound{id=2,kind="nuki",seat=0},new PublicSound{id=3,kind="pon",seat=1}}){
                voiceEvent=e.kind=="nuki"?"拔北 #"+e.id:"下家碰";var clip=CharacterVoices.Clip(view.players[e.seat].character,e.kind);
                Note("before "+e.kind+" clip="+(clip!=null?clip.name+" "+clip.loadState:"null")+" playing="+roleOutput.isPlaying+" room="+typeof(RoleVoice).GetField("room",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(roleVoice)+" cursor="+typeof(RoleVoice).GetField("cursor",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(roleVoice)+" enabled="+typeof(RoleVoice).GetField("enabledVoice",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(roleVoice));
                roleVoice.QueuePresented("review-r33",e);yield return Hold(e.id==3?75:20);
                Note("after "+e.kind+" output="+(roleOutput.clip!=null?roleOutput.clip.name:"null")+" playing="+roleOutput.isPlaying+" t="+roleOutput.time+" vol="+roleOutput.volume);
            }
            Note("interruptions "+(roleVoice.Interruptions-before));if(roleVoice.Interruptions-before<2)throw new Exception("Expected the second kita and the pon to interrupt");
        }
        void OnGUI(){
            GUI.depth=-10000;var style=new GUIStyle(GUI.skin.label){fontSize=26,wordWrap=true,alignment=TextAnchor.MiddleCenter};style.normal.textColor=Color.white;
            if(!string.IsNullOrEmpty(caption)){var r=new Rect(0,caption.StartsWith("4/6")?8:Screen.height-70,Screen.width,60);GUI.color=new Color(0,0,0,.65f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(r,caption,style);}
            if(!string.IsNullOrEmpty(status)){var sr=new Rect(Screen.width-760,12,748,48);GUI.color=new Color(0,0,0,.7f);GUI.DrawTexture(sr,Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(sr,status,new GUIStyle(style){fontSize=24});}
            if(MotionMode||R39Mode||R40Mode||R41Mode)return;
            var box=new Rect(Screen.width-470,12,458,60);GUI.color=new Color(0,0,0,.6f);GUI.DrawTexture(box,Texture2D.whiteTexture);GUI.color=Color.white;
            var small=new GUIStyle(style){fontSize=20,alignment=TextAnchor.MiddleLeft};
            float duck=PlaylistAudio.Active!=null?PlaylistAudio.Active.DuckGain:1,music=AppPreferences.Gain(1)*(PlaylistAudio.Active!=null?PlaylistAudio.Active.RoundMusicGain:1)*duck;
            GUI.Label(new Rect(box.x+12,box.y+6,box.width-24,30),"BGM 实际音量 "+music.ToString("0.00")+"（压低系数 "+duck.ToString("0.00")+"）",small);
            var bar=new Rect(box.x+12,box.y+40,box.width-24,12);GUI.color=new Color(1,1,1,.25f);GUI.DrawTexture(bar,Texture2D.whiteTexture);GUI.color=new Color(.95f,.78f,.4f);GUI.DrawTexture(new Rect(bar.x,bar.y,bar.width*Mathf.Clamp01(music),bar.height),Texture2D.whiteTexture);GUI.color=Color.white;
            if(voiceEvent!=null){box=new Rect(box.x,box.yMax,box.width,64);GUI.color=new Color(0,0,0,.6f);GUI.DrawTexture(box,Texture2D.whiteTexture);GUI.color=Color.white;
                string voice=roleOutput!=null&&roleOutput.isPlaying&&roleOutput.clip!=null?voiceEvent+" · "+roleOutput.clip.name+" "+roleOutput.time.ToString("0.0")+"/"+roleOutput.clip.length.ToString("0.0")+"s":"角色语音：空闲";
                GUI.Label(new Rect(box.x+12,box.y+2,box.width-24,30),voice,small);GUI.Label(new Rect(box.x+12,box.y+32,box.width-24,30),"语音被打断次数 "+(roleVoice!=null?roleVoice.Interruptions:0),small);}
        }
    }
}
#endif
