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
    [InitializeOnLoad] public static class CharacterSubtitleReview {
        const string Key="CharacterSubtitleReview";
        static CharacterSubtitleReview(){EditorApplication.update+=()=>{if(EditorApplication.isPlaying&&!EditorApplication.isCompiling&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<CharacterSubtitleCapture>();}};}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Bootstrap(){
            if(!SessionState.GetBool(Key,false))return;
            CharacterRoster.PreviewOnly=true;CharacterPackService.RootOverride=Environment.GetEnvironmentVariable("YUNJIAN_SUBTITLE_ROLES");CharacterPackService.RespectPreviewAvailability=true;CharacterPackService.ResetForReview();
        }
        public static void Run(){PlayerSettings.companyName="YunJianReview";PlayerSettings.productName="CharacterSubtitleR36";EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class CharacterSubtitleCapture:MonoBehaviour {
        [Serializable] class Sample {public string file,role,key,text,fullText;public float elapsed,clipLength;public bool sourcePlaying;public int width,height;}
        [Serializable] class Report {public int checks,catalogLines;public bool androidDeviceTested;public List<string> passed=new List<string>();public List<Sample> screenshots=new List<Sample>();}
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        Report report=new Report();RiichiClient client;LobbyController lobby;CharacterVoices voice;CharacterVoicePlayback playback;string output,savedPresets;bool hadPresets;
        static T Field<T>(object owner,string name)=>(T)owner.GetType().GetField(name,Private).GetValue(owner);
        void Check(bool ok,string reason){if(!ok)throw new Exception(reason);report.checks++;report.passed.Add(reason);}
        string Subtitle=>(string)typeof(LobbyController).GetProperty("CharacterSubtitleText",Private).GetValue(lobby);
        void Restore(){if(hadPresets)PlayerPrefs.SetString("riichi.wardrobe.presets.v3",savedPresets);else PlayerPrefs.DeleteKey("riichi.wardrobe.presets.v3");PlayerPrefs.Save();}
        IEnumerator Start(){
            output=Environment.GetEnvironmentVariable("YUNJIAN_SUBTITLE_OUT");hadPresets=PlayerPrefs.HasKey("riichi.wardrobe.presets.v3");savedPresets=PlayerPrefs.GetString("riichi.wardrobe.presets.v3","");
            client=GetComponent<RiichiClient>();client.SessionPreviewOnly=true;client.BeginLobbyValidation("subtitle-r36-local-only");lobby=client.Lobby;voice=GetComponent<CharacterVoices>();playback=GetComponent<CharacterVoicePlayback>();GetComponent<ResourceRoomCoordinator>().enabled=false;GetComponent<CharacterPackService>().StopAllCoroutines();
            CharacterRoster.BeginPreview();WardrobeSelection.BeginPreview();AppPreferences.Load();AppPreferences.Volume[0]=AppPreferences.Volume[4]=1;AppPreferences.Volume[1]=0;AppPreferences.Muted[0]=AppPreferences.Muted[4]=false;AppPreferences.SetDynamicOutfits(false);AudioListener.volume=1;AudioListener.pause=false;
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0){object next;try{var top=stack.Peek();if(!top.MoveNext()){stack.Pop();continue;}next=top.Current;if(next is IEnumerator nested){stack.Push(nested);continue;}}catch(Exception e){Restore();File.WriteAllText(output+"/failed.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
            Restore();File.WriteAllText(output+"/report.json",JsonUtility.ToJson(report,true));Debug.Log("CHARACTER SUBTITLE R36 PASS "+report.checks);EditorApplication.Exit(0);
        }
        IEnumerator Size(int w,int h){var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));view.GetType().GetMethod("SetCustomResolution",Private).Invoke(view,new object[]{new Vector2(w,h),"Character subtitles"});view.Repaint();for(int i=0;i<8;i++)yield return null;}
        IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+name+".png",texture.EncodeToPNG());Destroy(texture);report.screenshots.Add(new Sample{file=name+".png",role=voice.PlayingCharacter,key=voice.PlayingKey,text=Subtitle,fullText=voice.Caption.Text,elapsed=Time.unscaledTime-voice.Caption.StartedAt,clipLength=voice.EndsAt-voice.Caption.StartedAt,sourcePlaying=voice.Playing,width=Screen.width,height=Screen.height});}
        IEnumerator Press(string id){lobby.ValidationPress(id);for(int i=0;i<5;i++)yield return null;}
        IEnumerator Run(){
            Check(CharacterPackService.Installed("weilai"),"verified Weilai Windows pack mounted");CharacterPackService.Pin("weilai");CharacterRoster.Use("weilai",406903);
            // Every exact path must retain its own text, including ordinary and
            // maximum-bond rows that deliberately share a type.
            foreach(var role in CharacterRoster.All)foreach(var line in CharacterVoiceCatalog.Lines(role.key).Where(x=>x.available&&x.type.StartsWith("lobby_"))){var resolved=CharacterVoiceCatalog.Line(role.key,line.path);Check(resolved==line,role.key+"/"+line.path+" exact text");report.catalogLines++;}
            var model=new CharacterVoiceCaption();model.Begin("你好{name}，欢迎！","A😀e\u0301",10,4);Check(model.Text=="你好A😀e\u0301，欢迎！","nickname replaced before reveal");
            Check(model.Revealed(10.21f)=="你好A😀","typewriter keeps surrogate pairs whole");Check(model.Revealed(10.26f)=="你好A😀e\u0301","typewriter keeps combining marks whole");Check(model.Revealed(14)==""&&model.Opacity(14)==0,"caption ends on audio clock");
            model.Begin("短语音也需要完整显示这段台词。","",0,.8f);Check(model.Revealed(.6f)==model.Text,"short clip reveals complete text before end");model.Clear();Check(model.Text==""&&model.Revealed(.2f)=="","clear removes pending text");
            yield return Size(2374,1080);yield return new WaitForSecondsRealtime(1);playback.Cancel();Field<Dictionary<string,int>>(playback,"interaction")["weilai"]=4;
            yield return Press("home-character-touch");Check(voice.PlayingKey=="lobby/lobby_normalmax1","lobby portrait tap starts reference voice");yield return new WaitForSecondsRealtime(.55f);string early=Subtitle;Check(early.Length>0&&early.Length<voice.Caption.Text.Length,"lobby progressive text from actual playing clip");yield return Shot("lobby-2374-early");yield return new WaitForSecondsRealtime(1);Check(Subtitle.Length>early.Length,"text grows during playback");yield return Shot("lobby-2374");
            lobby.OpenModal("help");Check(Subtitle=="","modal hides floating subtitle");lobby.CloseModal();yield return new WaitForSecondsRealtime(.25f);
            lobby.OpenDorm();Check(voice.Caption.Text==""&&Subtitle=="","entering dorm clears lobby speech");yield return new WaitForSecondsRealtime(.5f);
            Field<Dictionary<string,int>>(playback,"interaction")["weilai"]=5;CharacterRoster.Dorm=CharacterRoster.Find("weilai");yield return Press("character-touch");Check(voice.PlayingKey=="lobby/lobby_normalmax2","dorm portrait tap starts reference voice");yield return new WaitForSecondsRealtime(2.5f);Check(Subtitle.StartsWith("明天一起去图书馆吧"),"dorm subtitle matches the new line");yield return Shot("dorm-2374");
            // Subtitle labels must not eat the portrait tap that interrupts them.
            yield return Press("character-touch");Check(voice.PlayingKey=="lobby/lobby_normalmax3"&&!voice.Caption.Text.StartsWith("明天"),"second portrait tap interrupts caption and speech together");
            AppPreferences.Muted[4]=true;yield return null;yield return null;Check(!voice.Playing&&voice.Caption.Text==""&&Subtitle=="","muting stops sound and clears caption");AppPreferences.Muted[4]=false;
            Check(!voice.TryPlay("weilai","missing-subtitle-clip",true)&&voice.Caption.Text=="","unavailable clip leaves no caption");
            voice.TryPlay("weilai","act_ron",true);yield return new WaitForSecondsRealtime(.1f);Check(voice.Caption.Text==""&&Subtitle=="","game action has no floating lobby caption");voice.Stop();
            voice.TryPlay("weilai","lobby/lobby_normalmax2",true);yield return new WaitForSecondsRealtime(.2f);lobby.OpenRoleSelection();Check(Subtitle==""&&voice.Caption.Text=="","role selection clears caption");
            lobby.PreviewHideCharacters();lobby.Home();yield return Size(1920,1080);voice.TryPlay("weilai","lobby/lobby_normalmax1",true);yield return new WaitForSecondsRealtime(1.4f);yield return Shot("lobby-1920");
            CharacterRoster.Use("yiji",400101);Check(Subtitle=="","caption cannot appear over a different lobby character");CharacterRoster.Use("weilai",406903);
            lobby.PreviewDorm("weilai",406903);yield return new WaitForSecondsRealtime(.4f);voice.TryPlay("weilai","lobby/lobby_normalmax2",true);yield return new WaitForSecondsRealtime(2.5f);yield return Shot("dorm-1920");
            voice.TryPlay("weilai","lobby/lobby_levelup2",true);yield return new WaitForSecondsRealtime(10);Check(voice.Caption.Text.Length>180&&Subtitle==voice.Caption.Text&&Field<GUIStyle>(lobby,"characterSubtitleStyle").CalcHeight(new GUIContent(Subtitle),578)>186,"long speech reaches complete text and follows overflowing lines");yield return Shot("dorm-long-text-1920");
            voice.TryPlay("weilai","lobby/lobby_normalmax2",true);yield return new WaitForSecondsRealtime(.3f);lobby.Back();yield return new WaitForSecondsRealtime(.4f);Check(Subtitle==""&&voice.Caption.Text==""&&!voice.Playing,"leaving dorm clears sound and text");
            // Observe an actual short lobby clip through its natural end.
            voice.TryPlay("yiji","lobby_room_ready",true);float end=voice.EndsAt;Check(end>Time.unscaledTime&&voice.Caption.Text.Length>0,"short lobby clip starts with matching caption");while(Time.unscaledTime<end+.15f)yield return null;Check(!voice.Playing&&voice.Caption.Text==""&&Subtitle=="","natural audio completion clears caption");
            lobby.PreviewDorm("weilai",406903,"info");yield return new WaitForSecondsRealtime(.4f);typeof(LobbyController).GetMethod("SetCharacterInfoTab",Private).Invoke(lobby,new object[]{true});voice.TryPlay("weilai","lobby/lobby_normalmax2");yield return new WaitForSecondsRealtime(.3f);Check(Subtitle=="","manual voice page does not duplicate its existing text");
            playback.Suspend(true);Check(voice.Caption.Text==""&&!voice.Playing,"application suspension clears caption");
        }
    }
}
