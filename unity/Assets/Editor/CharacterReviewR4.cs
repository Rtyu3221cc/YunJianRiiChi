using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace YunJian.Riichi.Editor {
    [InitializeOnLoad] public static class CharacterReviewR4 {
        const string Key="Riichi.CharacterReviewR4";
        static CharacterReviewR4(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<CharacterReviewR4Capture>();}};}
        public static void Run(){CharacterAssetImport.Configure();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class CharacterReviewR4Capture:MonoBehaviour {
        [Serializable] class Section {public int frame;public string title;}
        [Serializable] class Movie {public int fps=30,frames;public List<Section> sections=new List<Section>();public List<string> checks=new List<string>();}
        readonly Movie movie=new Movie();readonly List<string> errors=new List<string>();RiichiClient client;LobbyController lobby;string output;float clock=10;int frame;bool screenOnly;
        bool Quick=>Array.IndexOf(Environment.GetCommandLineArgs(),"-r4Quick")>=0;
        void Log(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception)errors.Add(m);}
        IEnumerator Start(){
            output=Path.GetFullPath("../build/character-review-r4/render"+(Quick?"-quick":""));Directory.CreateDirectory(output);Directory.CreateDirectory(Path.Combine(output,"frames"));if(File.Exists(Path.Combine(output,"failed.txt")))File.Delete(Path.Combine(output,"failed.txt"));Application.logMessageReceived+=Log;var work=new Stack<IEnumerator>();work.Push(Capture());
            while(work.Count>0){object next;try{var top=work.Peek();if(!top.MoveNext()){work.Pop();continue;}next=top.Current;if(next is IEnumerator nested){work.Push(nested);continue;}}catch(Exception e){File.WriteAllText(Path.Combine(output,"failed.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
            movie.frames=frame;File.WriteAllText(Path.Combine(output,"movie.json"),JsonUtility.ToJson(movie,true));File.WriteAllText(Path.Combine(output,"render-notes.txt"),"Actual Unity render; scripted pointer input through shared production scroll routing and hit regions; navigation uses validation controls. No APK or publication.\n"+string.Join("\n",errors));CharacterRoster.EndPreview();AudioListener.pause=false;Debug.Log("CHARACTER REVIEW R4 COMPLETE errors="+errors.Count);EditorApplication.Exit(errors.Count==0?0:1);
        }
        void Chapter(string title){movie.sections.Add(new Section{frame=frame,title=title});}
        void Check(bool value,string label){if(!value)throw new Exception(label);movie.checks.Add(label);Debug.Log("R4 INPUT CHECK "+label);}
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
            CharacterRoster.BeginPreview();client=GetComponent<RiichiClient>();client.BeginLobbyValidation("character-r4-preview");lobby=client.Lobby;AudioListener.pause=true;yield return Size(1750,800);lobby.DesignPreviewClock=clock;CharacterRoster.Use("weilai",406903);CharacterRoster.Star("weilai");CharacterRoster.Star("siqiqiansuili");CharacterRoster.Reorder(new[]{"weilai","siqiqiansuili","erjietang","yiji"});lobby.DesignPreviewGardenEntry();
            Chapter("宿舍 · 角色与装扮文字上移 3px");lobby.PreviewDorm("weilai",406903);yield return Hold(16);yield return Shot("dorm-tabs");
            Chapter("服饰 · 无左右箭头，介绍支持上下拖动");lobby.PreviewDorm("weilai",406903,"skins");yield return Hold(8);yield return Shot("skins-description-top");float width=Screen.width/(Screen.height/1080f);yield return Drag(ScreenPoint(width-680,875),ScreenPoint(width-680,745),20);Check(lobby.ReviewSkinDescriptionOffset>20,"skin description responds to vertical pointer scrolling");Check(lobby.ValidationTarget("skin-previous").width==0&&lobby.ValidationTarget("skin-next").width==0,"skin selection exposes no arrow controls");yield return Shot("skins-description-scrolled");
            var a=new Vector2(width-650,400);lobby.ReviewCarouselPointer(EventType.MouseDown,a);for(int i=1;i<=20;i++){lobby.ReviewCarouselPointer(EventType.MouseDrag,a+new Vector2(i*10,0));yield return Hold(1);}lobby.ReviewCarouselPointer(EventType.MouseUp,a+new Vector2(200,0));yield return Hold(14);yield return Shot("skins-swiped");
            Chapter("切换雀士 · 原版面板、头像与使用中标记");lobby.PreviewHideCharacters();lobby.DesignPreviewGardenEntry();yield return Hold(65);yield return Tap("role");yield return Hold(18);yield return Shot("switch-main");yield return Tap("battle-character-siqiqiansuili");yield return Hold(12);yield return Shot("switch-character");Check(CharacterRoster.Active.character=="siqiqiansuili","single roster switches the active character");
            Chapter("切换雀士 · 头像下展开服饰，选择后应用");yield return Tap("battle-character-siqiqiansuili");yield return Hold(10);yield return Shot("switch-skins");yield return Tap("battle-skin-403304");yield return Hold(20);yield return Shot("switch-equipped");Check(CharacterRoster.Active.skin==403304,"single roster applies the selected outfit");yield return Tap("role-favorites");yield return Hold(8);yield return Shot("switch-favorites");Check(lobby.ValidationTarget("battle-character-yiji").width==0,"single roster favorite filter hides unstarred characters");yield return Tap("role-favorites");
            Chapter("随机页面 · 指定模式不显示候选配置");yield return Tap("role-tab-random");yield return Hold(8);yield return Shot("random-specified");Check(!CharacterRoster.Random&&lobby.ValidationTarget("random-character-weilai").width==0,"specified mode does not expose random configuration");
            Chapter("随机页面 · 切换随机后可从空列表开始配置");yield return Tap("random-enabled");yield return Hold(8);yield return Shot("random-empty");Check(CharacterRoster.Random&&CharacterRoster.Pool.Length==0,"random mode can be enabled before candidates are selected");yield return Tap("random-character-weilai");yield return Hold(8);yield return Shot("random-skins");yield return Tap("random-skin-406901");yield return Tap("random-skin-406903");yield return Hold(8);yield return Shot("random-two-skins");Check(CharacterRoster.Pool.Count(p=>p.character=="weilai")==2,"multiple outfits are grouped under one candidate character");
            Chapter("随机页面 · 多雀士候选与服饰数量标记");yield return Tap("random-character-siqiqiansuili");yield return Tap("random-skin-403301");yield return Hold(10);yield return Shot("random-candidates");
            Chapter("随机页面 · 服饰弹层可横向拖动");yield return Tap("random-character-yiji");yield return Hold(5);var popup=lobby.ReviewRoleSkinPopover;yield return Drag(ScreenPoint(popup.xMax-35,popup.center.y),ScreenPoint(popup.x+25,popup.center.y),24);Check(lobby.ReviewRoleSkinsOffset>200,"outfit popover scrolls horizontally");yield return Shot("random-skins-scrolled");yield return Tap("random-skin-400107");yield return Hold(8);yield return Shot("random-three-characters");
            Chapter("指定／随机开关保留候选；清空不关闭随机模式");int selected=CharacterRoster.Pool.Length;yield return Tap("random-enabled");yield return Hold(8);yield return Shot("random-disabled");Check(!CharacterRoster.Random&&CharacterRoster.Pool.Length==selected&&lobby.ValidationTarget("random-clear").width==0,"specified mode preserves candidates and disables editing");yield return Tap("random-enabled");yield return Hold(6);yield return Shot("random-restored");yield return Tap("random-clear");yield return Hold(8);yield return Shot("random-cleared");Check(CharacterRoster.Random&&CharacterRoster.Pool.Length==0,"clearing candidates keeps the random configuration mode enabled");yield return Tap("role-close");yield return Hold(22);yield return Shot("hall-after-close");
            screenOnly=true;yield return Size(1280,720);lobby.PreviewRole("weilai");yield return Hold(3);yield return Shot("switch-16x9");lobby.PreviewRandom();CharacterRoster.Random=true;yield return Hold(3);yield return Shot("random-16x9");
        }
        void OnDestroy(){Application.logMessageReceived-=Log;CharacterRoster.EndPreview();AudioListener.pause=false;}
    }
}
