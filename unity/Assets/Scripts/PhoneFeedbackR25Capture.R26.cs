#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    public sealed partial class PhoneFeedbackR25Capture {
        bool R26Mode=>(Environment.GetEnvironmentVariable("YUNJIAN_R25_MODE")??"").StartsWith("r26");
        bool R26Probe=>Environment.GetEnvironmentVariable("YUNJIAN_R25_MODE")=="r26-probe";
        [Serializable] class R26Sample {public string id,action;public int seat;public bool previous,compensation;public Vector3 centroid,groundSize,groundPosition;public string groundMode;}
        [Serializable] class R26Samples {public List<R26Sample> rows=new List<R26Sample>();}
        readonly R26Samples r26samples=new R26Samples();
        CharacterVoices r26speaker;int r26cue=-1;
        void R26Voice(string role,string key,AudioClip clip){if(encoder==null)return;R26VoiceStop();string folder=repo+"/unity/Assets/"+(role=="yiji"||role=="erjietang"?"Resources/Majsoul/Characters":"CharacterPacks/Source/"+role)+"/Voice/"+role;string path=folder+"/"+key+".mp3";if(!File.Exists(path))path=folder+"/"+Path.GetFileName(key)+".mp3";Check(File.Exists(path),"Actual voice-page audio source exists: "+role+"/"+key);report.cues.Add(new AudioCue{role=role,key=key,source=path,time=report.frames/30f,duration=clip.length});r26cue=report.cues.Count-1;}
        void R26VoiceStop(){if(r26cue<0)return;var cue=report.cues[r26cue];cue.duration=Mathf.Min(cue.duration,Mathf.Max(.001f,report.frames/30f-cue.time));r26cue=-1;}
        IEnumerator R26Run(){
            yield return Size(1600,900);client.BeginLobbyValidation("r26-review");client.SessionPreviewOnly=true;
            if(R26Probe){yield return R26Directions(false);yield break;}
            if(Environment.GetEnvironmentVariable("YUNJIAN_R25_MODE")=="r26-four"){yield return R26Four();yield break;}
            BeginRecording();r26speaker=GetComponent<CharacterVoices>();r26speaker.ReviewPlayed+=R26Voice;r26speaker.ReviewStopped+=R26VoiceStop;AudioListener.pause=false;
            yield return R26VoicePage();AudioListener.pause=true;r26speaker.Stop();r26speaker.ReviewPlayed-=R26Voice;r26speaker.ReviewStopped-=R26VoiceStop;
            yield return R26Directions(true);
            ChapterAt("本轮仅预览：方向按款式复核；角色语音分页已接通，未生成 APK");yield return Hold(60);pipe.Close();encoder.WaitForExit();Check(encoder.ExitCode==0,"R26 original game capture encoding completes");encoder.Dispose();encoder=null;pipe=null;Time.captureFramerate=0;
            foreach(var role in CharacterRoster.All){foreach(var line in CharacterVoiceCatalog.Lines(role.key)){Check(line.available,"Catalog row has original audio: "+role.key+"/"+line.path);var clip=CharacterVoices.Clip(role.key,line.path);Check(clip!=null&&clip.length>0,"Voice-page runtime lookup: "+role.key+"/"+line.path);report.voiceLookups++;}report.characters++;yield return null;}
            Check(report.characters==122&&report.voiceLookups==16226,"All 122 actual voice pages resolve all visible source rows");
        }
        IEnumerator R26VoicePage(){
            var lobby=client.Lobby;var roles=CharacterResourceUi.Current;var outfits=WardrobeResourceUi.Current;lobby.DesignPreviewGardenEntry();yield return Wait(15);
            var pack=CharacterPackService.Find("qianzhi");string target=CharacterPackService.FilePath(pack);Check(target.StartsWith(repo+"/build/review-r26/")||Path.GetFullPath(target).StartsWith(Path.GetFullPath(repo+"/build/review-r26/")),"Only the isolated R26 client is reset");foreach(string ext in new[]{"",".ok",".part"})if(File.Exists(target+ext))File.Delete(target+ext);CharacterPackService.ResetForReview();CharacterRoster.Reorder(new[]{"yiji","qianzhi","erjietang"});lobby.PreviewDorm("yiji",400101);yield return Wait(8);yield return Press("dorm-character-qianzhi");Check(roles.ModalOpen,"Real missing-role card opens its resource prompt");
            ChapterAt("下载弹窗：高度 105 → 84，文字统一放大至 36");Check(Mathf.Abs(roles.Targets["resource-prompt-download"].height-70)<1,"84-unit character button has the expected physical touch height");yield return Hold(65);yield return Shot("01-role-prompt-84-font36");roles.ReviewPress="resource-prompt-cancel";yield return Wait(4);
            outfits.Ask("liqi_25hf",id=>{});yield return Wait(5);Check(Mathf.Abs(outfits.Targets["deco-prompt-download"].height-70)<1,"Outfit prompt shares the same button and label sizing");yield return Hold(50);yield return Shot("02-outfit-prompt-84-font36");outfits.ReviewPress="deco-prompt-cancel";yield return Wait(4);
            sourceServer=new CharacterPackShareServer();sourceServer.Enabled=true;string original=repo+"/build/character-resources-all/windows/"+pack.file;if(!File.Exists(original))original=repo+"/build/character-resources-v1/windows/"+pack.file;sourceServer.SetEntries(new[]{new CharacterPackShareServer.Entry{key=pack.key,hash=pack.sha256,bytes=pack.bytes,path=original}});CharacterPackService.Instance.OverrideUrl=key=>"http://127.0.0.1:"+sourceServer.Port+"/packs/"+CharacterPackService.Find(key).sha256+"?ticket="+sourceServer.Ticket;
            yield return Press("dorm-character-qianzhi");roles.ReviewPress="resource-prompt-equip";yield return Wait(5);float deadline=Time.realtimeSinceStartup+90;while(!CharacterPackService.Ready("qianzhi")){Check(Time.realtimeSinceStartup<deadline,"Real local role download completes: "+CharacterPackService.Instance.Error);yield return Frame();}yield return Wait(8);Check(CharacterRoster.Dorm.key=="qianzhi","Download-and-select enters the real character");CharacterPackService.Instance.OverrideUrl=null;sourceServer.Dispose();sourceServer=null;
            yield return Press("character-info");lobby.ReviewSettleDorm();yield return Wait(5);ChapterAt("实际操作：人物档案 → 语音分页");yield return Hold(35);yield return Press("voice-lantern");Check(lobby.ReviewCharacterVoiceTab,"Voice lantern opens the actual voice page");yield return Hold(60);yield return Shot("03-qianzhi-voice-page");
            var rows=CharacterVoiceCatalog.Lines("qianzhi");int riichi=Array.FindIndex(rows,x=>x.type=="fan_liqi"),intro=Array.FindIndex(rows,x=>x.type=="lobby_selfintro");Check(riichi>=0&&intro>=0,"Original yaku and lobby voice names are available");lobby.ReviewVoiceScroll(Mathf.Max(0,(riichi-2)*94));yield return Wait(4);ChapterAt("三上千织：在语音列表点击“立直”，播放同一角色原始语音");yield return Press("character-voice-play-"+riichi);Check(r26speaker.Playing&&r26speaker.PlayingCharacter=="qianzhi","Actual audio source starts from the voice-page play button");yield return Hold(45);
            var point=new Vector2((Screen.width-1080*Screen.width/1920f)+300*Screen.width/1920f,450*Screen.height/1080f);float before=lobby.ReviewVoiceOffset;lobby.ReviewQueueGuiPointer(EventType.ScrollWheel,point,new Vector2(0,5));yield return Wait(4);Check(lobby.ReviewVoiceOffset>before,"Voice page accepts the real dorm wheel/drag event path");ChapterAt("语音列表可以滚动；保留原版名称、台词和播放／停止状态");yield return Hold(45);
            lobby.ReviewVoiceScroll(Mathf.Max(0,(intro-2)*94));yield return Wait(4);yield return Press("character-voice-play-"+intro);Check(r26speaker.Playing,"Long character voice is playing");yield return Hold(15);yield return Press("character-voice-play-"+intro);Check(!r26speaker.Playing,"Pressing the same voice stops it");yield return Hold(30);yield return Shot("04-voice-scrolled-and-stopped");
            yield return Press("profile-lantern");Check(!lobby.ReviewCharacterVoiceTab&&!r26speaker.Playing,"Returning to profile stops voice preview");yield return Hold(20);
            lobby.PreviewDorm("yiji",400101,"info");yield return Wait(6);yield return Press("voice-lantern");ChapterAt("切换角色后展示对应语音：一姬");yield return Press("character-voice-play-0");Check(r26speaker.PlayingCharacter=="yiji","Voice page switches character instead of reusing old role audio");yield return Hold(45);yield return Press("profile-lantern");Check(!r26speaker.Playing,"Leaving the voice page stops audition");yield return Hold(20);
        }
        IEnumerator R26Directions(bool movie){
            var visual=GetComponent<RiichiDiscardVisual>();var focus=new HashSet<string>{"riichi-afterimage","riichi-fire","effect-liqi-ding"};
            foreach(var item in WardrobeCatalog.Items("riichiEffect")){
                bool both=focus.Contains(item.id);foreach(bool previous in both?new[]{true,false}:new[]{false}){
                    WinTileEffect.ReviewPreviousRiichiMount=previous;yield return UseDeclaration(item,0);yield return new WaitForSecondsRealtime(.15f);ChapterAt((previous?"上一轮方向 · ":"本轮按挂载分类校准 · ")+item.name);
                    if(movie){for(int f=0;f<120;f++){effects.ValidationTime=f/30f;yield return Frame();}}else{effects.ValidationTime=.75f;yield return Wait(4);}
                    effects.ValidationTime=1.2f;yield return Wait(3);var fx=visual.Latest;Check(fx!=null,"Direction review effect exists: "+item.id);r26samples.rows.Add(new R26Sample{id=item.id,seat=0,previous=previous,action=SourcePlaybackContracts.Binding(item).action,compensation=fx.LegacySidewaysCompensation,centroid=R26ParticleCentroid(fx.transform)});yield return Shot(item.id+(previous?"-before":"-after"));effects.Reset();
                }
            }
            WinTileEffect.ReviewPreviousRiichiMount=false;var response=GetComponent<WardrobeResponseIndicator>();
            foreach(var item in WardrobeCatalog.Items("callout"))foreach(bool previous in item.id=="mpzs_shandian"?new[]{true,false}:new[]{false}){
                var view=Fixture();view.phase="reaction";view.turn=1;var row=view.players[1].river.Last();view.pending=new PendingView{from=1,tile=row.tile,kind="discard",waiting=new[]{0}};view.options=new[]{new Choice{id="pon:test",kind="pon",tile=row.tile}};view.players[0].wardrobe.callout=item.id;client.ShowValidationFixture(view);client.ValidationTableOnly=true;response.ReviewPreviousRootPose=previous;response.ValidationFreeze=true;response.ValidationAge=0;yield return Wait(5);ChapterAt((previous?"上一轮制作姿态叠加 · ":"本轮牌底平面挂载 · ")+item.name);
                if(movie){for(int f=0;f<80;f++){response.ValidationAge=f/30f;yield return Frame();}}else{response.ValidationAge=.75f;yield return Wait(4);}
                Check(response.EffectRoot!=null,"Callable tile marker exists: "+item.id);var di=response.EffectRoot.GetComponentsInChildren<ParticleSystem>().FirstOrDefault(x=>x.name=="di");var renderer=di!=null?di.GetComponent<ParticleSystemRenderer>():null;r26samples.rows.Add(new R26Sample{id=item.id,seat=1,previous=previous,groundMode=renderer?.renderMode.ToString(),groundSize=renderer!=null?renderer.bounds.size:Vector3.zero,groundPosition=renderer!=null?renderer.bounds.center:Vector3.zero});yield return Shot(item.id+(previous?"-before":"-after"));response.Clear();
            }
            response.ReviewPreviousRootPose=response.ValidationFreeze=false;client.ValidationTableOnly=false;effects.ValidationFreeze=effects.ValidationSequentialClock=false;File.WriteAllText(output+"/direction-study.json",JsonUtility.ToJson(r26samples,true));
        }
        IEnumerator R26Four(){
            var visual=GetComponent<RiichiDiscardVisual>();var directional=new HashSet<string>{"riichi-afterimage","riichi-fire","effect-liqi-ding"};
            foreach(var item in WardrobeCatalog.Items("riichiEffect"))for(int seat=0;seat<4;seat++){
                WinTileEffect.ReviewPreviousRiichiMount=false;yield return UseDeclaration(item,seat);yield return new WaitForSecondsRealtime(.15f);effects.ValidationTime=.6f;yield return Wait(3);effects.ValidationTime=1.0f;yield return Wait(3);var fx=visual.Latest;Check(fx!=null,"R26 four-seat effect exists "+item.id+" seat "+seat);
                if(directional.Contains(item.id)){var tile=table.FindDiscard(seat,"z1",client.State.players[seat].river.Length);var delta=R26ParticleCentroid(fx.transform)-SourcePlaybackContracts.TileModelOrigin(tile);var right=Quaternion.Euler(0,table.SeatAngle(seat),0)*Vector3.right;Check(Vector3.Dot(delta,right)>0,"Directional trail follows the reviewed seat-local side "+item.id+" seat "+seat);}
                if(item.id=="effect-liqi-23ex-liandao")Check(Quaternion.Angle(fx.transform.rotation,Quaternion.Euler(0,180,0))<.01f,"Approved scythe direction retained");effects.ValidationTime=6;yield return Wait(3);Check(visual.ActiveCount==0,"R26 declaration cleans up after source lifetime");report.riichiCases++;effects.Reset();
            }
            var response=GetComponent<WardrobeResponseIndicator>();foreach(var item in WardrobeCatalog.Items("callout"))for(int seat=0;seat<4;seat++){
                var view=Fixture();view.players[0].wardrobe.callout=item.id;client.ShowValidationFixture(view);client.ValidationTableOnly=true;var tile=table.FindDiscard(seat,view.players[seat].river.Last().tile,view.players[seat].river.Length);response.InspectionMode=true;response.InspectionTarget=tile;response.InspectionKey=item.id+seat;response.ReviewPreviousRootPose=false;response.ValidationFreeze=true;response.ValidationAge=.7f;yield return Wait(4);Check(response.EffectRoot!=null,"R26 marker binds at every source seat "+item.id);if(item.id=="mpzs_shandian"){var di=response.EffectRoot.GetComponentsInChildren<ParticleSystem>().First(x=>x.name=="di").GetComponent<ParticleSystemRenderer>();Check(Mathf.Abs(di.bounds.center.y-tile.position.y)<.05f,"Lightning base stays at tile bottom for seat "+seat);}report.calloutCases++;response.Clear();
            }response.InspectionMode=response.ValidationFreeze=false;client.ValidationTableOnly=false;effects.ValidationFreeze=effects.ValidationSequentialClock=false;
        }
        Vector3 R26ParticleCentroid(Transform root){var points=new List<Vector3>();foreach(var ps in root.GetComponentsInChildren<ParticleSystem>()){if(!ps.name.ToLowerInvariant().Contains("trail"))continue;var parts=new ParticleSystem.Particle[ps.main.maxParticles];int n=ps.GetParticles(parts);for(int i=0;i<n;i++){var p=ps.main.simulationSpace==ParticleSystemSimulationSpace.World?parts[i].position:ps.transform.TransformPoint(parts[i].position);if(ps.gameObject.layer==NativeEffectCamera.Layer)p*=TableTileGeometry.SourceUnits;points.Add(p);}}return points.Count>0?points.Aggregate(Vector3.zero,(s,p)=>s+p)/points.Count:Vector3.zero;}
    }
}
#endif
