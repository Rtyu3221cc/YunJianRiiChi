using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace YunJian.Riichi.Editor {
    [InitializeOnLoad] public static class CharacterReviewR3 {
        const string Key="Riichi.CharacterReviewR3";
        static CharacterReviewR3(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<CharacterReviewR3Capture>();}};}
        public static void Run(){CharacterAssetImport.Configure();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class CharacterReviewR3Capture:MonoBehaviour {
        [Serializable] class Section {public int frame;public string title;}
        [Serializable] class Movie {public int fps=30,frames;public List<Section> sections=new List<Section>();public List<string> checks=new List<string>();}
        readonly Movie movie=new Movie();readonly List<string> errors=new List<string>();RiichiClient client;LobbyController lobby;string output;float clock=10;int frame;bool screenOnly;
        bool Quick=>Array.IndexOf(Environment.GetCommandLineArgs(),"-r3Quick")>=0;
        void Log(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception)errors.Add(m);}
        IEnumerator Start(){
            output=Path.GetFullPath("../build/character-review-r3/render"+(Quick?"-quick":""));Directory.CreateDirectory(output);Directory.CreateDirectory(Path.Combine(output,"frames"));if(File.Exists(Path.Combine(output,"failed.txt")))File.Delete(Path.Combine(output,"failed.txt"));Application.logMessageReceived+=Log;var work=new Stack<IEnumerator>();work.Push(Capture());
            while(work.Count>0){object next;try{var top=work.Peek();if(!top.MoveNext()){work.Pop();continue;}next=top.Current;if(next is IEnumerator nested){work.Push(nested);continue;}}catch(Exception e){File.WriteAllText(Path.Combine(output,"failed.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
            movie.frames=frame;File.WriteAllText(Path.Combine(output,"movie.json"),JsonUtility.ToJson(movie,true));File.WriteAllText(Path.Combine(output,"render-notes.txt"),"Actual Unity render; scripted pointer input through shared production photo/profile routing and hit regions; navigation uses validation controls. No APK or publication.\n"+string.Join("\n",errors));CharacterRoster.EndPreview();AudioListener.pause=false;Debug.Log("CHARACTER REVIEW R3 COMPLETE errors="+errors.Count);EditorApplication.Exit(errors.Count==0?0:1);
        }
        void Chapter(string title){movie.sections.Add(new Section{frame=frame,title=title});}
        void Check(bool value,string label){if(!value)throw new Exception(label);movie.checks.Add(label);Debug.Log("R3 INPUT CHECK "+label);}
        IEnumerator Size(int w,int h){var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(w,h),"Character interaction repair"});for(int i=0;i<8;i++)yield return null;}
        IEnumerator Shot(string name,bool video=false){yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+(video?".jpg":".png")),video?t.EncodeToJPG(93):t.EncodeToPNG());Destroy(t);}
        IEnumerator Hold(int count){if(Quick||screenOnly){clock+=count/30f;lobby.DesignPreviewClock=clock;for(int i=0;i<3;i++)yield return null;yield break;}for(int i=0;i<count;i++){lobby.DesignPreviewClock=clock;clock+=1f/30;yield return Shot("frames/frame-"+(frame++).ToString("D4"),true);}}
        void InputEvent(EventType type,Vector2 point,Vector2 delta=default){
            lobby.ReviewQueueGuiPointer(type,point,delta);
        }
        IEnumerator Tap(string id){yield return Hold(2);var r=lobby.ValidationTarget(id);Check(r.width>0,"visible control "+id);if(id.StartsWith("lobby-illustration-")){InputEvent(EventType.MouseDown,r.center);yield return Hold(2);InputEvent(EventType.MouseUp,r.center);}else lobby.ValidationPress(id);yield return Hold(3);}
        IEnumerator Drag(Vector2 a,Vector2 b,int steps=24){InputEvent(EventType.MouseDown,a);yield return Hold(2);var previous=a;for(int i=1;i<=steps;i++){var p=Vector2.Lerp(a,b,i/(float)steps);InputEvent(EventType.MouseDrag,p,p-previous);previous=p;yield return Hold(1);}InputEvent(EventType.MouseUp,b);yield return Hold(3);}
        Vector2 ScreenPoint(float x,float y)=>new Vector2(x,y)*(Screen.height/1080f);
        void TextureShot(string name,Texture texture){var rt=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;Graphics.Blit(texture,rt);RenderTexture.active=rt;var result=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);result.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);result.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),result.EncodeToPNG());Destroy(result);RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
        IEnumerator Capture(){
            CharacterRoster.BeginPreview();client=GetComponent<RiichiClient>();client.BeginLobbyValidation("character-r3-input");lobby=client.Lobby;AudioListener.pause=true;yield return Size(1750,800);lobby.DesignPreviewClock=clock;CharacterRoster.Use("weilai",406903);lobby.DesignPreviewGardenEntry();yield return Hold(65);
            Chapter("宿舍 · 竖排姓名完整显示，灯笼文字间距收紧");yield return Tap("bottom0");yield return Hold(18);yield return Shot("dorm-names");yield return Tap("character-info");yield return Hold(18);yield return Shot("profile-top");
            Chapter("拜访 · 从介绍文字拖动到全部聊天表情");float width=Screen.width/(Screen.height/1080f);var from=ScreenPoint(width-750,850);var to=ScreenPoint(width-750,410);yield return Drag(from,to);Check(lobby.ReviewInfoOffset>300,"profile content scrolls with a pointer drag");yield return Shot("profile-dragged");yield return Hold(25);
            Chapter("拜访 · 拖动右侧菱形滑块");yield return Drag(ScreenPoint(width-329.8f,530),ScreenPoint(width-329.8f,950));Check(lobby.ReviewInfoOffset>600,"profile scroll thumb responds to a pointer drag");yield return Shot("profile-scrollbar");yield return Tap("back");yield return Hold(18);
            foreach(var pair in new[]{new CharacterChoice("yiji",400107),new CharacterChoice("siqiqiansuili",403304)}){
                lobby.PreviewDorm(pair.character,pair.skin);yield return Hold(20);if(!lobby.ReviewLiveEnabled)yield return Tap("character-live");yield return Hold(18);Chapter("动态／静态同一画布 · "+CharacterRoster.Find(pair.character).name);yield return Shot("live-"+pair.skin);var moving=lobby.ReviewCharacterTexture;var still=CharacterRoster.Texture("Skins/"+pair.skin+"/full");Check(Mathf.Abs((float)moving.width/moving.height-(float)still.width/still.height)<.002f,"animated canvas matches original still aspect for "+pair.skin);TextureShot("art-live-"+pair.skin,moving);yield return Tap("character-live");yield return Hold(10);yield return Shot("static-"+pair.skin);TextureShot("art-static-"+pair.skin,lobby.ReviewCharacterTexture);Check(!lobby.ReviewLiveEnabled,"LIVE/OFF selects actual still for "+pair.skin);yield return Hold(18);yield return Tap("character-live");yield return Hold(18);
            }
            Chapter("大厅 · 拖动立绘，点击还原恢复默认构图");lobby.PreviewHideCharacters();CharacterRoster.Use("yiji",400107);lobby.DesignPreviewGardenEntry();yield return Hold(65);yield return Tap("capture");Check(lobby.ReviewHomeEditing,"camera opens illustration editor through pointer events");yield return Shot("photo-default");yield return Drag(ScreenPoint(900,520),ScreenPoint(1015,440));Check(lobby.ReviewHomeDraft.x>80&&lobby.ReviewHomeDraft.y< -50,"photo pointer drag moves the illustration");yield return Shot("photo-moved");yield return Tap("lobby-illustration-reset");Check(Mathf.Abs(lobby.ReviewHomeDraft.x)<.01f&&Mathf.Abs(lobby.ReviewHomeDraft.y)<.01f&&Mathf.Abs(lobby.ReviewHomeDraft.scale-1)<.001f,"restore button responds and resets default hall pose");yield return Shot("photo-restored");yield return Hold(18);
            Chapter("大厅 · 缩放、保存后返回大厅，再打开仍保留设置");yield return Drag(ScreenPoint(900,520),ScreenPoint(1010,450));yield return Drag(ScreenPoint(width-310,590),ScreenPoint(width-310,510),16);var draft=lobby.ReviewHomeDraft;yield return Shot("photo-save-pose");yield return Tap("lobby-illustration-save");Check(!lobby.ReviewHomeEditing,"save button responds to a pointer tap");var saved=LobbyIllustrationSettings.Get(400107);Check(Mathf.Abs(saved.x-draft.x)<.01f&&Mathf.Abs(saved.y-draft.y)<.01f&&Mathf.Abs(saved.scale-draft.scale)<.001f,"save persists hall position and scale");yield return Shot("hall-saved");yield return Hold(20);yield return Tap("capture");Check(Mathf.Abs(lobby.ReviewHomeDraft.x-draft.x)<.01f,"reopening editor restores saved position");yield return Shot("photo-reopened");
            Chapter("透明相机 · 隐藏全部控件，再次点屏幕恢复");yield return Tap("lobby-illustration-hide");Check(lobby.ReviewHomeControlsHidden,"transparent camera hides every editing control");Check(lobby.ValidationTarget("lobby-illustration-save").width==0&&lobby.ValidationTarget("lobby-illustration-hide").width==0,"hidden photo mode exposes no control hit areas");yield return Shot("photo-clean");yield return Hold(45);InputEvent(EventType.MouseDown,ScreenPoint(1100,500));yield return Hold(2);InputEvent(EventType.MouseUp,ScreenPoint(1100,500));yield return Hold(4);Check(!lobby.ReviewHomeControlsHidden,"a screen tap restores photo controls");yield return Shot("photo-controls-restored");yield return Hold(22);yield return Tap("lobby-illustration-cancel");
            screenOnly=true;yield return Size(1280,720);lobby.PreviewDorm("weilai",406903,"info");yield return Hold(4);yield return Shot("profile-16x9");
        }
        void OnDestroy(){Application.logMessageReceived-=Log;CharacterRoster.EndPreview();AudioListener.pause=false;}
    }
}
