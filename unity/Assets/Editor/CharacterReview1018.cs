using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    [InitializeOnLoad] public static class CharacterReview1018 {
        const string Key="Riichi.CharacterReview1018";
        static CharacterReview1018(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<CharacterReviewCapture>();}};}
        public static void Run(){CharacterAssetImport.Configure();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class CharacterReviewCapture:MonoBehaviour {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;RiichiClient client;LobbyController lobby;TableEffects effects;string output;readonly List<string> errors=new List<string>();int frame;float clock=10;
        [Serializable] class Section {public int frame;public string title,voice,character;}
        [Serializable] class Movie {public int fps=20,mainEnd;public List<Section> sections=new List<Section>();}
        readonly Movie movie=new Movie();
        void SectionAt(string title,string character=null,string voice=null){movie.sections.Add(new Section{frame=frame,title=title,character=character,voice=voice});}
        bool Quick=>Array.IndexOf(Environment.GetCommandLineArgs(),"-characterQuick")>=0;
        void Log(string m,string stack,LogType kind){if(kind==LogType.Error||kind==LogType.Exception)errors.Add(m);}
        IEnumerator Start(){
            output=Path.GetFullPath("../build/characters-1018/render");Directory.CreateDirectory(output);if(File.Exists(Path.Combine(output,"failed.txt")))File.Delete(Path.Combine(output,"failed.txt"));Application.logMessageReceived+=Log;
            var work=new Stack<IEnumerator>();work.Push(Capture());while(work.Count>0){object next;try{var top=work.Peek();if(!top.MoveNext()){work.Pop();continue;}next=top.Current;if(next is IEnumerator child){work.Push(child);continue;}}catch(Exception e){File.WriteAllText(Path.Combine(output,"failed.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
            File.WriteAllText(Path.Combine(output,"movie.json"),JsonUtility.ToJson(movie,true));CharacterRoster.EndPreview();AudioListener.pause=false;File.WriteAllText(Path.Combine(output,"render-notes.txt"),"Actual Unity editor rendering. Public table fixture; no Android build.\n"+string.Join("\n",errors));Debug.Log("CHARACTER REVIEW COMPLETE errors="+errors.Count);EditorApplication.Exit(errors.Count==0?0:1);
        }
        IEnumerator Size(int w,int h){var win=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));win.GetType().GetMethod("SetCustomResolution",Private).Invoke(win,new object[]{new Vector2(w,h),"Character system review"});for(int i=0;i<8;i++)yield return null;}
        IEnumerator Shot(string name){yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();if(t==null){yield return null;yield return new WaitForEndOfFrame();t=ScreenCapture.CaptureScreenshotAsTexture();}if(t==null)throw new Exception("Screenshot failed: "+name);bool movieFrame=name.Contains("/");File.WriteAllBytes(Path.Combine(output,name+(movieFrame?".jpg":".png")),movieFrame?t.EncodeToJPG(93):t.EncodeToPNG());Destroy(t);}
        IEnumerator Frames(string folder,int count){
            Directory.CreateDirectory(Path.Combine(output,folder));for(int i=0;i<count;i++){lobby.DesignPreviewClock=clock;clock+=.05f;yield return Shot(folder+"/frame-"+(frame++).ToString("D4"));}
        }
        IEnumerator Hold(int count){if(!Quick)yield return Frames("frames",count);}
        void Press(string id){lobby.ValidationPress(id);}
        RoomView Table(){var v=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/unity-smoke/preview-4.json"));v.you=0;v.phase="play";v.audioRoom="character-review";v.audioSeq=0;v.audioEvents=new PublicSound[0];v.result=null;v.options=new Choice[0];string[] keys={"weilai","siqiqiansuili","erjietang","yiji"};int[] skins={406903,403301,400207,400107};for(int i=0;i<4;i++){v.players[i].character=keys[i];v.players[i].skin=skins[i];v.players[i].name=CharacterRoster.Find(keys[i]).name;v.players[i].takeoverMask=false;v.players[i].bot=i!=0;}return v;}
        IEnumerator Capture(){
            CharacterRoster.BeginPreview();client=GetComponent<RiichiClient>();effects=GetComponent<TableEffects>();client.BeginLobbyValidation("character-visual-only");lobby=client.Lobby;AudioListener.pause=true;yield return Size(1688,768);lobby.DesignPreviewClock=clock;lobby.DesignPreviewGardenEntry();clock+=2;lobby.DesignPreviewClock=clock;
            foreach(var role in CharacterRoster.All)foreach(string action in new[]{"riichi","double-riichi","ron","tsumo","chi","pon","kan","nuki","fan_pinghu","lobby/lobby_normal1"})if(CharacterVoices.Clip(role.key,action)==null)throw new Exception("Missing voice: "+role.key+"/"+action);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-characterCallsOnly")>=0){
                var prior=JsonUtility.FromJson<Movie>(File.ReadAllText(Path.Combine(output,"movie.json")));movie.sections.AddRange(prior.sections);movie.mainEnd=prior.mainEnd;var table=Table();client.DesignPreviewSessionTable(table);
                for(int seat=0;seat<4;seat++){effects.Reset();effects.ValidationFreeze=true;effects.ValidationTime=.25f;typeof(TableEffects).GetMethod("BeginCall",Private).Invoke(effects,new object[]{new PublicSound{seat=seat,kind="riichi"}});yield return Shot("call-seat-"+seat);foreach(var s in movie.sections)if(s.character==table.players[seat].character&&s.voice=="act_rich"){frame=s.frame;break;}yield return Hold(40);}effects.Reset();yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-characterSupplement")>=0){
                var prior=JsonUtility.FromJson<Movie>(File.ReadAllText(Path.Combine(output,"movie.json")));frame=prior.mainEnd>0?prior.mainEnd:Directory.GetFiles(Path.Combine(output,"frames"),"frame-*.jpg").Length;if(prior.mainEnd==0)foreach(var s in prior.sections)if(s.title.StartsWith("聊天表情")){frame=s.frame;break;}movie.mainEnd=frame;foreach(var s in prior.sections)if(s.frame<frame)movie.sections.Add(s);clock=100;lobby.DesignPreviewClock=clock;
                SectionAt("聊天表情 · 四名雀士全部解锁，共 71 张");
                foreach(var role in new[]{"weilai","yiji","erjietang","siqiqiansuili"}){lobby.PreviewDorm(role,role=="weilai"?406903:0,"info");lobby.PreviewProfileExpressions();yield return Shot(role=="weilai"?"profile-expressions":"expressions-"+role);yield return Hold(32);}
                CharacterRoster.Star("weilai");CharacterRoster.Star("siqiqiansuili");lobby.PreviewDorm("weilai",406903);lobby.PreviewSort();SectionAt("排序操作 · 拖动星标雀士头像，保存新的显示顺序");yield return Hold(20);
                float x=Screen.width/(Screen.height/1080f)/2-680;var from=new Vector2(x+160+232+76,235+76+76);var to=new Vector2(x+160+76,from.y);lobby.PreviewSortPointer(EventType.MouseDown,from);
                for(int i=0;i<30;i++){lobby.PreviewSortPointer(EventType.MouseDrag,Vector2.Lerp(from,to,i/29f));yield return Hold(1);}lobby.PreviewSortPointer(EventType.MouseUp,to);yield return Hold(15);Press("sort-save");yield return null;yield return Hold(20);yield return Shot("dorm-reordered");
                lobby.PreviewHideCharacters();CharacterRoster.Use("weilai",406903);lobby.PreviewRole("weilai");SectionAt("星标过滤与弹窗收起 · 大厅立绘随出战选择更新");Press("role-favorites");yield return null;yield return Hold(28);yield return Shot("battle-favorites");Press("role-close");yield return null;yield return Hold(25);
                lobby.PreviewHideCharacters();var introFixture=Table();client.DesignPreviewSessionTable(introFixture);client.ValidationIntro.Reenter(introFixture);client.ValidationIntro.Freeze=true;client.ValidationIntro.FrozenAge=1.1f;SectionAt("开局介绍 · 保留原版斜边遮罩，展示每位玩家的出战角色");yield return Shot("match-introduction");yield return Hold(35);client.ValidationIntro.Reset();
                effects.Reset();effects.ValidationFreeze=true;effects.ValidationTime=.25f;typeof(TableEffects).GetMethod("BeginCall",Private).Invoke(effects,new object[]{new PublicSound{seat=3,kind="riichi"}});yield return Shot("call-seat-3");int tail=frame;foreach(var section in movie.sections)if(section.character=="yiji"&&section.voice=="act_rich"){frame=section.frame;break;}yield return Hold(40);frame=tail;effects.Reset();
                yield break;
            }
            SectionAt("大厅 · 点击宿舍进入角色页面");yield return Shot("home-yiji");yield return Hold(22);
            Press("bottom0");yield return null;yield return Hold(30);lobby.PreviewDorm("weilai",406903);SectionAt("宿舍 · 四名雀士，契约头像与名牌");yield return Shot("dorm-mirai");yield return Hold(38);
            foreach(var key in new[]{"yiji","erjietang","siqiqiansuili"}){Press("dorm-character-"+key);yield return null;yield return Hold(12);if(Quick)lobby.PreviewDorm(key);yield return Shot("dorm-"+key);yield return Hold(26);}
            lobby.PreviewDorm("weilai",406903,"info");SectionAt("角色档案 · 五星好感与原版人物介绍");yield return Shot("profile-mirai");yield return Hold(44);
            lobby.PreviewDorm("weilai",406903,"skins");SectionAt("更换服饰 · 浏览全部服饰与原版介绍");yield return Shot("skins-mirai");yield return Hold(25);
            for(int i=0;i<4;i++){lobby.PreviewSkin(i);yield return Shot("mirai-skin-"+i);yield return Hold(25);}
            lobby.PreviewDorm("weilai",406903,"appreciate");SectionAt("皮肤鉴赏 · 缩放与自由移动");yield return Shot("appreciate-normal");if(!Quick)for(int i=0;i<50;i++){lobby.PreviewZoom(1+i*.018f,new Vector2(-i*3,i*1.5f));yield return Hold(1);}lobby.PreviewZoom(1.8f,new Vector2(-160,80));yield return Shot("appreciate-zoom");yield return Hold(20);
            lobby.PreviewDorm("yiji",400107);SectionAt("原始动态立绘 · 一姬 / 校园微风");yield return Shot("live-yiji-0");yield return Hold(120);yield return Shot("live-yiji-1");
            lobby.PreviewDorm("siqiqiansuili",403304);SectionAt("原始动态立绘 · 寺崎千穗理 / 玩转夏日");yield return Shot("live-chihori-0");yield return Hold(120);yield return Shot("live-chihori-1");
            lobby.PreviewDorm("weilai",406903);CharacterRoster.Star("weilai");CharacterRoster.Star("siqiqiansuili");lobby.PreviewSort();SectionAt("雀士星标与排序 · 拖动头像调整顺序");yield return Shot("favorites-sort");yield return Hold(50);
            lobby.PreviewHideCharacters();lobby.PreviewRole("weilai");SectionAt("大厅切换 · 角色与服饰独立选择");yield return Shot("battle-selection");yield return Hold(35);Press("battle-skin-406903");yield return null;yield return Hold(10);CharacterRoster.Use("weilai",406903);lobby.PreviewHideCharacters();yield return Shot("home-mirai");yield return Hold(35);
            lobby.PreviewRole("siqiqiansuili");yield return Shot("battle-skins");yield return Hold(35);CharacterRoster.Use("siqiqiansuili",403301);lobby.PreviewHideCharacters();yield return Shot("home-chihori");yield return Hold(35);
            foreach(var choice in new[]{new CharacterChoice("weilai",406903),new CharacterChoice("siqiqiansuili",403301),new CharacterChoice("erjietang",400201)})CharacterRoster.TogglePool(choice.character,choice.skin);CharacterRoster.Random=true;lobby.PreviewRandom();SectionAt("随机出战 · 从指定雀士和服饰中选择，一局内固定");yield return Shot("battle-random");yield return Hold(55);
            lobby.PreviewHideCharacters();var fixture=Table();client.DesignPreviewSessionTable(fixture);SectionAt("四人视角 · 每位玩家使用自己的角色与服饰");yield return Shot("four-characters-table");yield return Hold(35);
            foreach(int seat in new[]{0,1,2,3}){effects.Reset();effects.ValidationFreeze=true;effects.ValidationTime=.25f;typeof(TableEffects).GetMethod("BeginCall",Private).Invoke(effects,new object[]{new PublicSound{seat=seat,kind="riichi"}});SectionAt("立直演出与原版语音 · "+CharacterRoster.Find(fixture.players[seat].character).name,fixture.players[seat].character,"act_rich");yield return Shot("call-seat-"+seat);yield return Hold(40);}
            effects.Reset();lobby.PreviewHideCharacters();
            movie.mainEnd=frame;
            yield return Size(1280,720);client.BeginLobbyValidation("character-visual-only");lobby.PreviewDorm("weilai",406903);yield return Shot("dorm-16x9");lobby.PreviewDorm("weilai",406903,"info");yield return Shot("profile-16x9");
        }
        void OnDestroy(){Application.logMessageReceived-=Log;CharacterRoster.EndPreview();AudioListener.pause=false;}
    }
}
