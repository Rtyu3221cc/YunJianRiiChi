using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    [InitializeOnLoad] public static class WardrobeR6Review {
        const string Key="WardrobeR6Review";
        static WardrobeR6Review(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<WardrobeR6Probe>();}};}
        public static void RunStudy(){SessionState.SetBool("WardrobeR6Patch",false);SessionState.SetBool("WardrobeR6Full",false);Run();}
        public static void RunFull(){SessionState.SetBool("WardrobeR6Patch",false);SessionState.SetBool("WardrobeR6Full",true);Run();}
        public static void RunSakuraPatch(){Patch("sakura");}
        public static void RunCalloutPatch(){Patch("callout");}
        public static void RunSoundPatch(){Patch("sound");}
        static void Patch(string kind){SessionState.SetBool("WardrobeR6Patch",true);SessionState.SetString("WardrobeR6PatchKind",kind);SessionState.SetBool("WardrobeR6Full",true);Run();}
        static void Run(){AssetDatabase.Refresh();AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class WardrobeR6Probe:MonoBehaviour {
        [Serializable] public class Shot {public string key,title,group,audio;public int viewer,start,end;public float[] detail;}
        [Serializable] public class Report {public int frames,fps=20,checks;public bool apkBuilt,uploaded;public List<Shot> shots=new List<Shot>();public List<string> notes=new List<string>();}
        readonly Report report=new Report();string output,caption,patchKind;bool full,patch,dynamic;float listener,inset;int frame;
        float[] volumes;bool[] muted;RiichiClient client;TableRenderer table;TableEffects fx;WardrobeResponseIndicator marker;PlaylistAudio music;LobbyController lobby;
        BinaryWriter audio;int audioSamples,audioChannels;bool recordingAudio;
        IEnumerator Start(){
            full=SessionState.GetBool("WardrobeR6Full",false);patch=SessionState.GetBool("WardrobeR6Patch",false);patchKind=SessionState.GetString("WardrobeR6PatchKind","sakura");output=Path.GetFullPath(patch?"../build/wardrobe-r6-review/"+patchKind:full?"../build/wardrobe-r6-review/render":"../build/wardrobe-r6-review/study");Directory.CreateDirectory(output);Directory.CreateDirectory(Path.Combine(output,"frames"));File.Delete(Path.Combine(output,"error.txt"));File.WriteAllText(Path.Combine(output,"checks.txt"),"");
            listener=AudioListener.volume;AudioListener.volume=0;dynamic=AppPreferences.DynamicOutfits;inset=AppPreferences.ScreenInset;volumes=(float[])AppPreferences.Volume.Clone();muted=(bool[])AppPreferences.Muted.Clone();AppPreferences.SetDynamicOutfits(false);AppPreferences.SetInset(0);CharacterRoster.BeginPreview();WardrobeSelection.BeginPreview();WardrobeSelection.Save(WardrobeCatalog.Data.items.GroupBy(i=>i.category).ToDictionary(g=>g.Key,g=>g.First().id));Time.captureFramerate=report.fps;
            client=GetComponent<RiichiClient>();client.BeginLobbyValidation("wardrobe-r6");client.SessionPreviewOnly=true;GetComponent<ResourceRoomCoordinator>().enabled=false;table=GetComponent<TableRenderer>();fx=GetComponent<TableEffects>();marker=GetComponent<WardrobeResponseIndicator>();music=GetComponent<PlaylistAudio>();lobby=client.Lobby;
            var work=new Stack<IEnumerator>();work.Push(Run());while(work.Count>0){object next;try{var it=work.Peek();if(!it.MoveNext()){work.Pop();continue;}next=it.Current;if(next is IEnumerator child){work.Push(child);continue;}}catch(Exception e){File.WriteAllText(Path.Combine(output,"error.txt"),e.ToString());Debug.LogException(e);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            report.frames=frame;File.WriteAllText(Path.Combine(output,"capture.json"),JsonUtility.ToJson(report,true));Cleanup();Debug.Log("WARDROBE R6 PASS "+report.checks+" checks, "+frame+" frames");EditorApplication.Exit(0);
        }
        void Cleanup(){StopAudio();marker?.Clear();if(marker!=null)marker.InspectionMode=false;if(fx!=null){fx.ValidationSequentialClock=false;fx.ValidationFreeze=false;}if(table!=null)table.ReviewRevealTime=-1;if(music!=null)music.ReviewRoundTime=-1;Time.captureFramerate=0;AudioListener.volume=listener;Array.Copy(volumes,AppPreferences.Volume,volumes.Length);Array.Copy(muted,AppPreferences.Muted,muted.Length);WardrobeSelection.EndPreview();CharacterRoster.EndPreview();AppPreferences.SetDynamicOutfits(dynamic);AppPreferences.SetInset(inset);}
        void Check(bool ok,string message){if(!ok)throw new Exception(message);report.checks++;File.AppendAllText(Path.Combine(output,"checks.txt"),"PASS "+message+"\n");}
        void Note(string text){report.notes.Add(text);File.AppendAllText(Path.Combine(output,"diagnostics.txt"),text+"\n");Debug.Log(text);}
        IEnumerator Wait(int count=2){for(int i=0;i<count;i++)yield return null;}
        RoomView Read(string key,bool custom=false)=>JsonUtility.FromJson<RoomView>(File.ReadAllText((custom?"../build/wardrobe-r6-study/":"../build/unity-smoke/")+key+".json"));
        void Outfit(RoomView view,string hand="hand-human",string riichi="riichi-fire",string win="win-default",string callout="mpzs_shandian"){
            view.resourceSetup=null;foreach(var p in view.players){p.character="yiji";p.skin=400101;p.wardrobe=new WardrobeLoadout{hand=hand,stick="stick-default",riichiEffect=riichi,winEffect=win,callout=callout,music="music-riichi"};}
        }
        RoomView Fixture(string key,int viewer=0){var view=Read(key);Outfit(view);view.you=viewer;view.audioRoom="r6-"+key+"-"+viewer;view.audioEvents=new PublicSound[0];view.audioSeq=0;return view;}
        void Use(RoomView view){marker.InspectionMode=false;marker.Clear();table.ReviewRevealTime=-1;fx.ValidationSequentialClock=false;client.DesignPreviewSessionTable(view);client.ValidationTableOnly=true;client.ValidationIntro.Reset();fx.Reset();fx.ValidationFreeze=true;fx.Accept(view);}
        float[] Detail(Vector3 point,float w=540,float h=400){var p=table.Camera.WorldToScreenPoint(point);return new[]{Mathf.Clamp(p.x-w/2,0,Screen.width-w),Mathf.Clamp(Screen.height-p.y-h*.7f,0,Screen.height-h),w,h};}
        Shot Begin(string key,string title,string group,int viewer=0,float[] detail=null){caption=title+(group=="win"||group=="riichi"||group=="callout"?" · 视角 "+(viewer+1):"");var s=new Shot{key=key,title=title,group=group,viewer=viewer,start=frame,detail=detail};report.shots.Add(s);return s;}
        IEnumerator Image(string file){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,file+".png"),t.EncodeToPNG());Destroy(t);}
        IEnumerator Record(){yield return new WaitForEndOfFrame();if(full){var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,"frames",(frame++).ToString("D5")+".jpg"),t.EncodeToJPG(88));Destroy(t);}if(recordingAudio)ReadAudio();}
        IEnumerator Run(){
            var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(1688,768),"Wardrobe r6 actual rendering"});yield return Wait(8);
            if(patch){if(patchKind=="sound")yield return BgmSequence();else{if(patchKind=="sakura")yield return EffectSequences("win");yield return EffectSequences("callout");}yield break;}
            CheckEnvelope();yield return LightningStudy();yield return HandSequences();yield return DrawSequence();yield return TabsSequence();yield return NorthSequence();
            yield return BgmSequence();if(full){yield return EffectSequences("win");yield return EffectSequences("riichi");yield return EffectSequences("callout");}
        }
        void CheckEnvelope(){
            var v=Fixture("preview-4");var e=new RoundMusicEnvelope();e.Observe(v,true,0);Check(e.Gain(0)==1,"BGM plays during the hand");v.phase="result";v.result=new ResultView{draw=false};e.Observe(v,true,2);Check(Mathf.Abs(e.Gain(2.5f)-.5f)<.001f,"Win BGM fades linearly for one second");e.Observe(v,true,2.7f);Check(e.Gain(3.2f)==0,"Polling cannot restart the fade");e.Observe(v,true,8);Check(e.Gain(8)==0,"Result remains silent");v.phase="play";v.handSeq++;e.Observe(v,true,9);Check(e.Gain(9)==0&&Mathf.Abs(e.Gain(9.5f)-.5f)<.001f&&e.Gain(10)==1,"New hand restores BGM gradually");
        }
        IEnumerator LightningStudy(){
            for(int viewer=0;viewer<4;viewer++){
                var view=Fixture("win-4-tsumo-0-0",viewer);Use(view);yield return Wait();var go=new GameObject("R6 lightning source");var effect=go.AddComponent<WinTileEffect>();var point=table.WinningPoint(new PublicSound{seat=0,kind="tsumo"});effect.Initialize(point,0,0,507,"Majsoul/Actions/default-win-effect");effect.Sample(.12f);yield return Wait();
                var ps=effect.GetComponentsInChildren<ParticleSystem>().First(p=>p.name=="light"||p.name=="light source-local emitter");var particles=new ParticleSystem.Particle[ps.main.maxParticles];int n=ps.GetParticles(particles);Check(n>0,"Lightning has particles in view "+viewer);float up=Enumerable.Range(0,n).Average(i=>Vector3.Dot((ps.main.simulationSpace==ParticleSystemSimulationSpace.World?particles[i].velocity:ps.transform.TransformDirection(particles[i].velocity)).normalized,Vector3.up));
                var renderer=ps.GetComponent<ParticleSystemRenderer>();var mesh=new Mesh();renderer.BakeMesh(mesh,table.Camera,true);Note("LIGHTNING view="+viewer+" velocityUp="+up+" rendererPivot="+renderer.pivot+" baked="+mesh.bounds+" emitter="+ps.transform.position+" anchor="+point);var head=(mesh.vertices[0]+mesh.vertices[3])*.5f;var particle=ps.transform.TransformVector(particles[0].position);Check(Mathf.Abs(head.y-particle.y)<.002f,"Lightning geometry starts at the source particle head");Destroy(mesh);
                Check(up<-.95f,"Lightning travels downwards in view "+viewer);yield return Image("lightning-view-"+viewer);Destroy(go);yield return Wait();
            }
        }
        IEnumerator HandSequences(){
            foreach(string id in new[]{"hand-human","hand-cat","hand-cat-orange","hand-dog","hand-man","hand-cat-ice"}){
                string hand=id=="hand-cat-ice"?"hand-cat":id,riichi=id=="hand-cat-ice"?"riichi-ice":"riichi-fire";
                var before=Read("riichi-before",true);var declared=Read("riichi-declared",true);var committed=Read("riichi-committed",true);foreach(var v in new[]{before,declared,committed}){Outfit(v,hand,riichi);v.audioRoom="r6-full-declaration-"+id;}
                Use(before);fx.ValidationSequentialClock=true;fx.ValidationTime=0;yield return Wait(4);var shot=Begin(id,WardrobeCatalog.Find(hand).name+(id=="hand-cat-ice"?" · 冰霜原版分支":" · 宣言牌＋放棒"),"hand");bool sawDiscard=false,sawStick=false,sawVisible=false;float turn=0;string clips="";
                for(int i=0;i<90;i++){
                    float t=i/(float)report.fps;fx.ValidationTime=t;
                    if(i==4){client.ValidationApplyState(declared);fx.Accept(declared);}if(i==15){client.ValidationApplyState(committed);fx.Accept(committed);}
                    string kind=fx.ValidationKind;sawDiscard|=kind=="discard";sawStick|=kind=="riichi-stick";
                    if(kind=="discard"&&fx.ValidationAnimatedTile!=null&&fx.ValidationTarget!=null)turn=Mathf.Max(turn,Quaternion.Angle(fx.ValidationAnimatedTile.rotation,fx.ValidationTarget.rotation));
                    if(kind!=null&&fx.ValidationMotion!=null&&!clips.Contains(fx.ValidationMotion))clips+=" "+fx.ValidationMotion;
                    if(kind=="riichi-stick"){
                        var mesh=GameObject.Find("Source hand mesh");if(mesh!=null){var r=mesh.GetComponent<MeshRenderer>();sawVisible|=r.enabled&&r.bounds.size.sqrMagnitude>0;}
                    }
                    if(i==29)yield return Image(id+"-riichi");yield return Record();
                }
                Check(sawDiscard&&sawStick,hand+" public events include declaration and dedicated stick motion");Check(sawVisible,hand+" stick-placement hand is rendered");Check(table.StickTarget(0)!=null&&table.StickTarget(0).gameObject.activeInHierarchy,hand+" stick remains after handoff");if(id!="hand-cat-ice")Check(turn>10,hand+" retains its source declaration tile rotation");Note(id+" played motion sources:"+clips+" maxTileTurn="+turn);shot.end=frame;fx.ValidationSequentialClock=false;fx.Reset();
            }
        }
        IEnumerator DrawSequence(){
            var view=Fixture("preview-4");Use(view);yield return Wait();var last=view.players[1].river.Last();fx.ValidationPlay(1,"discard",last.tile,false,view.players[1].river.Length);fx.ValidationTime=.1f;
            var result=Read("draw-4-2");view.phase="result";view.result=result.result;table.ReviewRevealTime=0;client.ValidationApplyState(view);
            var first=GameObject.Find("Draw hand seat 0").transform.GetChild(0);Check(Mathf.Abs(Vector3.Dot(first.up,Vector3.up))<.01f,"Draw rebuild starts standing before the first LateUpdate");var shot=Begin("draw-polling","荒牌流局 · 等待动作结束，轮询不中断摊牌","flow");
            for(int i=0;i<100;i++){
                float t=i/(float)report.fps;table.ReviewRevealTime=t;fx.ValidationTime=t<1.2f?.1f:.7f;
                if(i==10||i==20||i==28||i==38||i==60){typeof(TableRenderer).GetMethod("UpdateDrawReveal",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(table,null);var old=GameObject.Find("Draw hand seat 0").transform.GetChild(0).rotation;client.ValidationApplyState(view);var current=GameObject.Find("Draw hand seat 0").transform.GetChild(0);Check(Quaternion.Angle(old,current.rotation)<.02f,"Draw polling keeps the pose at frame "+i);}
                if(i==15||i==36||i==60)yield return Image("draw-stage-"+i);yield return Record();
            }
            shot.end=frame;table.ReviewRevealTime=-1;fx.Reset();
        }
        IEnumerator TabsSequence(){
            client.BeginLobbyValidation("wardrobe-r6");client.SessionPreviewOnly=true;client.ValidationTableOnly=false;lobby.DesignPreviewClock=100;lobby.PreviewDorm("yiji",400101);yield return Wait(5);var shot=Begin("tabs-switch","角色／装扮 · 页签固定、面板展开收拢、立绘淡入淡出","ui");
            for(int i=0;i<140;i++){
                lobby.DesignPreviewClock=100+i/(float)report.fps;if(i==15||i==85)lobby.ValidationPress("dress-tab");if(i==55)lobby.ValidationPress("dress-role");
                if(i==40)yield return Image("wardrobe-page");yield return Record();
            }
            shot.end=frame;
            foreach(string category in new[]{"winEffect","riichiEffect","callout"}){lobby.PreviewWardrobeCategory(category);yield return Wait(3);Check(lobby.ValidationTarget("dress-eye").width==0,category+" standalone preview entry is hidden");yield return Image("preview-disabled-"+category);}
            lobby.PreviewHideCharacters();lobby.DesignPreviewClock=-1;
        }
        IEnumerator NorthSequence(){
            for(int viewer=0;viewer<3;viewer++){
                var view=Fixture("preview-3",viewer);view.players[0].nuki=4;Use(view);yield return Wait(3);var a=table.NorthTarget(0,1);var b=table.NorthTarget(0,2);Check(Mathf.Abs(Vector3.Distance(a.position,b.position)-table.Geometry.MeldSlot(false))<.0001f,"North keeps the same outline spacing as melds");
                var shot=Begin("north-outlines","拔北牌 · 完整描边与副露等距", "north",viewer,Detail(a.position,600,350));for(int i=0;i<40;i++)yield return Record();yield return Image("north-view-"+viewer);shot.end=frame;
            }
        }
        IEnumerator EffectSequences(string group){
            string category=group=="win"?"winEffect":group=="riichi"?"riichiEffect":"callout";
            foreach(var item in WardrobeCatalog.Items(category))for(int viewer=0;viewer<4;viewer++){
                if(patch&&patchKind=="sakura"&&item.id!="ron-sakura"&&item.id!="callout-sakura")continue;
                var view=Fixture(group=="win"?"win-4-ron-0-0":"preview-4",viewer);Outfit(view,group=="riichi"?"hand-cat":"hand-human",group=="riichi"?item.id:"riichi-fire",group=="win"?item.id:"win-default",group=="callout"?item.id:"mpzs_shandian");Use(view);yield return Wait(3);Transform target;WinTileEffect effect=null;
                if(group=="callout"){
                    int from=0,last=view.players[from].river.Length-1;target=table.FindDiscard(from,view.players[from].river[last].tile,last+1);marker.InspectionMode=true;marker.InspectionTarget=target;marker.InspectionKey=item.id+":"+viewer;marker.ValidationFreeze=true;marker.ValidationAge=0;
                }else if(group=="win"){
                    var d=view.result.details[0];var e=new PublicSound{seat=0,kind="ron",from=d.from,tile=d.tile};target=table.WinningTarget(e);var go=new GameObject(item.id);effect=go.AddComponent<WinTileEffect>();effect.Initialize(table.WinningPoint(e),0,0,507,item.effect);
                }else{
                    int last=view.players[0].river.Length-1;view.players[0].river[last].riichi=true;target=table.FindDiscard(0,view.players[0].river[last].tile,last+1);fx.ValidationPlay(0,"discard",view.players[0].river[last].tile,true,last+1);
                }
                var shot=Begin(item.id,item.name,group,viewer,Detail(target.position+Vector3.up*(group=="win"?.7f:0),group=="win"?700:540,group=="win"?620:400));bool visible=false;int count=group=="riichi"?80:64;
                for(int i=0;i<count;i++){
                    float age=i/(float)report.fps;if(group=="callout")marker.ValidationAge=age;else if(effect!=null)effect.Sample(age);else fx.ValidationTime=age;
                    if(group=="callout")visible|=marker.ParticleCount>0;else if(effect!=null)visible|=effect.ParticleCount>0;else visible|=GetComponent<RiichiDiscardVisual>().ActiveCount>0;
                    if(i==16)yield return Image(item.id+"-view-"+viewer);yield return Record();
                    if(patchKind=="callout"&&patch&&i==10){var root=marker.EffectRoot;int pixels=root.GetComponentsInChildren<ParticleSystemRenderer>().Max(r=>(int)typeof(RuntimeValidation).GetMethod("VisibleEffectPixels",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{table,r}));Note(item.id+" view="+viewer+" actualPixels="+pixels+" scale="+root.lossyScale+" firstBounds="+root.GetComponentsInChildren<ParticleSystemRenderer>()[0].bounds);Check(pixels>10,item.id+" has visible rendered pixels in view "+viewer);}
                }
                Check(visible,item.id+" has visible source particles in viewpoint "+viewer);
                if(patch&&patchKind=="sakura"){var owner=effect!=null?effect:marker.SourceEffect;var petals=owner.GetComponentsInChildren<ParticleSystem>().First(p=>p.name.StartsWith("yezi"));var velocity=petals.velocityOverLifetime;Check(velocity.x.mode==ParticleSystemCurveMode.Curve&&Mathf.Abs(velocity.x.curve.Evaluate(.25f)-.245f)<.001f&&Mathf.Abs(velocity.y.curve.Evaluate(.5f)+.245f)<.001f,item.id+" preserves original time-varying petal velocity");}
                shot.end=frame;marker.InspectionMode=false;marker.Clear();if(effect!=null)Destroy(effect.gameObject);fx.Reset();
            }
        }
        IEnumerator BgmSequence(){
            AudioListener.volume=0;for(int i=0;i<AppPreferences.Muted.Length;i++)AppPreferences.Muted[i]=false;for(int i=2;i<AppPreferences.Volume.Length;i++)AppPreferences.SetVolume(i,0);AppPreferences.SetVolume(0,1);AppPreferences.SetVolume(1,.45f);GetComponent<RoleVoice>()?.Reset();music.ReviewRoundTime=-1;music.DesignPreviewDefaults();var before=Fixture("preview-4");Use(before);yield return Wait(4);music.SwitchTo(music.Tracks.First(t=>t.group=="mj").id);float deadline=Time.realtimeSinceStartup+10;while((music.Fading||music.ChannelVolumes.Max()<.01f)&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(music.ChannelVolumes.Max()>.01f,"Actual BGM channel is ready for sound recording");var win=Fixture("win-4-ron-0-0");Outfit(win);win.audioRoom=before.audioRoom;win.audioSeq=before.audioSeq+1;win.audioEvents=new[]{new PublicSound{id=win.audioSeq,kind="ron",seat=0,from=win.result.details[0].from,tile=win.result.details[0].tile}};win.handSeq=before.handSeq;win.round=before.round;win.honba=before.honba;
            fx.ValidationSequentialClock=true;fx.ValidationTime=0;float audioClock=Time.unscaledTime;music.ReviewRoundTime=audioClock;var shot=Begin("bgm-fade","雷击和牌＋BGM 渐停，结算保持静音，下一局渐入","sound");shot.audio="bgm-fade.wav";StartAudio(Path.Combine(output,shot.audio));AudioListener.volume=1;
            var audioWatch=System.Diagnostics.Stopwatch.StartNew();
            for(int i=0;i<240;i++){
                float t=i/(float)report.fps;music.ReviewRoundTime=audioClock+t;fx.ValidationTime=t;
                if(i==40){client.ValidationApplyState(win);fx.Accept(win);}if(i==180){var next=Fixture("preview-4");next.audioRoom=before.audioRoom;next.handSeq=before.handSeq+1;next.dealtAt=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();next.openingHand=next.hand;table.Opening.Freeze=true;table.Opening.FrozenAge=0;Use(next);fx.ValidationSequentialClock=true;}
                if(i>=180)table.Opening.FrozenAge=t-9;
                caption=(t<2?"对局 BGM":t<3?"和牌 · BGM 渐停":t<9?"本局结算 · BGM 保持静音":"下一局 · BGM 渐入")+"  "+Mathf.RoundToInt(music.RoundMusicGain*100)+"%";
                if(i==80)Check(music.RoundMusicGain==0,"Actual mixer is silent after win fade");if(i==160)Check(music.RoundMusicGain==0,"Actual mixer remains silent throughout result");if(i==215)Check(music.RoundMusicGain>.99f,"Actual mixer returns in the next hand");yield return Record();
                if(i==140){Check(music.ChannelVolumes.All(v=>v<.0001f),"Actual playlist channel volumes are zero during result");foreach(var s in FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Where(s=>s.isPlaying&&s.volume>.001f))Note("RESULT AUDIO clip="+s.clip?.name+" volume="+s.volume+" loop="+s.loop);}
                int wait=(int)(((i+1)/(double)report.fps-audioWatch.Elapsed.TotalSeconds)*1000);if(wait>0)System.Threading.Thread.Sleep(Mathf.Min(wait,50));
            }
            StopAudio();shot.end=frame;AudioListener.volume=0;music.ReviewRoundTime=-1;fx.ValidationSequentialClock=false;
        }
        void StartAudio(string path){audioChannels=AudioSettings.speakerMode==AudioSpeakerMode.Mono?1:2;audioSamples=0;audio=new BinaryWriter(File.Create(path));audio.Write(new byte[44]);recordingAudio=AudioRenderer.Start();Check(recordingAudio,"Unity native audio output recording started");}
        void ReadAudio(){int count=AudioRenderer.GetSampleCountForCaptureFrame()*audioChannels;using(var samples=new NativeArray<float>(count,Allocator.Temp)){AudioRenderer.Render(samples);for(int i=0;i<count;i++){audio.Write((short)Mathf.RoundToInt(Mathf.Clamp(samples[i],-1,1)*32767));audioSamples++;}}}
        void StopAudio(){if(audio==null)return;AudioRenderer.Stop();recordingAudio=false;int bytes=audioSamples*2;audio.Seek(0,SeekOrigin.Begin);audio.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));audio.Write(bytes+36);audio.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));audio.Write(16);audio.Write((short)1);audio.Write((short)audioChannels);audio.Write(AudioSettings.outputSampleRate);audio.Write(AudioSettings.outputSampleRate*audioChannels*2);audio.Write((short)(audioChannels*2));audio.Write((short)16);audio.Write(System.Text.Encoding.ASCII.GetBytes("data"));audio.Write(bytes);audio.Dispose();audio=null;}
        void OnGUI(){if(!string.IsNullOrEmpty(caption))GUI.Label(new Rect(30,8,Screen.width-60,36),caption,new GUIStyle(GUI.skin.label){fontSize=22,alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(1,.85f,.55f)}});}
    }
}
