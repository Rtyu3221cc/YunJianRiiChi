using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace YunJian.Riichi.Editor {
    [InitializeOnLoad] public static class CharacterReviewR1 {
        const string Key="Riichi.CharacterReviewR1";
        static CharacterReviewR1(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<CharacterReviewR1Capture>();}};}
        public static void Run(){CharacterAssetImport.Configure();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class CharacterReviewR1Capture:MonoBehaviour {
        [Serializable] class Section {public int frame;public string title;}
        [Serializable] class Movie {public int fps=30,frames;public List<Section> sections=new List<Section>();public List<string> checks=new List<string>();}
        readonly Movie movie=new Movie();readonly List<string> errors=new List<string>();RiichiClient client;LobbyController lobby;string output;float clock=10;int frame;
        bool screenOnly;
        bool Quick=>Array.IndexOf(Environment.GetCommandLineArgs(),"-r1Quick")>=0;
        void Log(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception)errors.Add(m);}
        IEnumerator Start(){
            output=Path.GetFullPath("../build/character-review-r1/render"+(Quick?"-quick":""));Directory.CreateDirectory(output);Directory.CreateDirectory(Path.Combine(output,"frames"));if(File.Exists(Path.Combine(output,"failed.txt")))File.Delete(Path.Combine(output,"failed.txt"));Application.logMessageReceived+=Log;var stack=new Stack<IEnumerator>();stack.Push(Capture());
            while(stack.Count>0){object current;try{var top=stack.Peek();if(!top.MoveNext()){stack.Pop();continue;}current=top.Current;if(current is IEnumerator nested){stack.Push(nested);continue;}}catch(Exception e){File.WriteAllText(Path.Combine(output,"failed.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return current;}
            movie.frames=frame;File.WriteAllText(Path.Combine(output,"movie.json"),JsonUtility.ToJson(movie,true));File.WriteAllText(Path.Combine(output,"render-notes.txt"),"Actual Unity render; no table or role-selection changes; no APK;\n"+string.Join("\n",errors));CharacterRoster.EndPreview();AudioListener.pause=false;Debug.Log("CHARACTER REVIEW R1 COMPLETE errors="+errors.Count);EditorApplication.Exit(errors.Count==0?0:1);
        }
        void Chapter(string title){movie.sections.Add(new Section{frame=frame,title=title});}
        void Check(bool condition,string message){if(!condition)throw new Exception(message);movie.checks.Add(message);}
        IEnumerator Size(int w,int h){var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(w,h),"Character UI correction review"});for(int i=0;i<8;i++)yield return null;}
        IEnumerator Shot(string name,bool movieFrame=false){yield return null;yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();if(t==null)throw new Exception("Missing screenshot "+name);File.WriteAllBytes(Path.Combine(output,name+(movieFrame?".jpg":".png")),movieFrame?t.EncodeToJPG(93):t.EncodeToPNG());Destroy(t);}
        IEnumerator Hold(int count){if(Quick||screenOnly){yield return null;yield return new WaitForEndOfFrame();clock+=count/30f;lobby.DesignPreviewClock=clock;for(int i=0;i<3;i++)yield return null;yield break;}for(int i=0;i<count;i++){lobby.DesignPreviewClock=clock;clock+=1f/30;yield return Shot("frames/frame-"+(frame++).ToString("D4"),true);}}
        void Press(string id){lobby.ValidationPress(id);}
        IEnumerator Capture(){
            CharacterRoster.BeginPreview();client=GetComponent<RiichiClient>();client.BeginLobbyValidation("character-ui-r1");lobby=client.Lobby;AudioListener.pause=true;yield return Size(1688,768);lobby.DesignPreviewClock=clock;CharacterRoster.Use("weilai",406903);lobby.DesignPreviewGardenEntry();yield return Hold(60);Chapter("大厅 · 默认立绘与宿舍保持同一实际大小");yield return Shot("home-default");yield return Hold(24);
            Chapter("进入宿舍 · 原包 0.333 秒淡入与右侧面板滑入");Press("bottom0");yield return Hold(24);yield return Shot("dorm-main");yield return Hold(28);Press("dorm-star-weilai");yield return Hold(12);Check(CharacterRoster.Favorite("weilai"),"selected dorm diamond directly toggles favorite");yield return Shot("dorm-quick-star");
            Chapter("拜访 · 面板转场、灯笼与原版 16 帧木牌翻面");Press("character-info");yield return Hold(20);yield return Shot("profile-main");yield return Hold(35);lobby.ReviewInfoScroll(500);yield return Hold(12);yield return Shot("profile-emojis");lobby.ReviewInfoScroll(0);yield return Hold(10);
            Chapter("档案进入服饰 · 初始、契约依次排列，非循环滑动");Press("info-skins");yield return Hold(20);yield return Shot("skins-current");Press("skin-previous");yield return Hold(12);Press("skin-previous");yield return Hold(12);yield return Shot("skins-first");Check(lobby.ValidationTarget("skin-previous").width==0,"first skin has no previous arrow");
            float width=Screen.width/(Screen.height/1080f);var from=new Vector2(width-670,360);var to=from+new Vector2(-195,0);lobby.ReviewCarouselPointer(EventType.MouseDown,from);for(int i=0;i<20;i++){lobby.ReviewCarouselPointer(EventType.MouseDrag,Vector2.Lerp(from,to,i/19f));yield return Hold(1);}lobby.ReviewCarouselPointer(EventType.MouseUp,to);yield return Hold(14);yield return Shot("skins-swiped");
            Press("equip-skin");yield return Hold(12);Check(CharacterRoster.Outfit("weilai")==406902,"swipe selects contract skin and equip applies it");yield return Shot("skins-equipped");Press("back");yield return Hold(20);yield return Shot("profile-equipped");Press("back");yield return Hold(20);
            lobby.PreviewDorm("weilai",406903);yield return Hold(4);Chapter("立绘细看 · 原版 MAX／MIN 资源、滑条与拖动缩放");Press("character-appreciate");yield return Hold(20);yield return Shot("appreciate-default");for(int i=0;i<30;i++){lobby.ReviewZoomSlider(Mathf.Lerp((2.3f-.65f)/1.95f,.2f,i/29f));yield return Hold(1);}lobby.PreviewZoom(1.9f,new Vector2(-120,80));yield return Hold(15);yield return Shot("appreciate-zoomed");Press("appreciation-close");yield return Hold(22);yield return Shot("dorm-after-appreciate");
            Chapter("Live2D 切换 · 三个按钮同尺寸，切换实际静态立绘与骨骼动画");lobby.PreviewDorm("yiji",400107);yield return Hold(45);yield return Shot("live-on");Press("character-live");yield return Hold(24);Check(!lobby.ReviewLiveEnabled,"live button switches to static illustration");yield return Shot("live-static");Press("character-live");yield return Hold(45);Check(lobby.ReviewLiveEnabled,"live button restores original skeleton animation");yield return Shot("live-restored");
            Chapter("雀士排序 · 全宽背景、正确标题、底部提示与长按拖动");lobby.PreviewDorm("weilai",406903);if(!CharacterRoster.Favorite("siqiqiansuili"))CharacterRoster.Star("siqiqiansuili");yield return Hold(3);Press("character-sort");yield return Hold(16);yield return Shot("sort-main");var a=lobby.ReviewSortRect(true,1).center;var b=lobby.ReviewSortRect(true,0).center;lobby.PreviewSortPointer(EventType.MouseDown,a);yield return Hold(7);for(int i=0;i<24;i++){lobby.PreviewSortPointer(EventType.MouseDrag,Vector2.Lerp(a,b,i/23f));yield return Hold(1);}lobby.PreviewSortPointer(EventType.MouseUp,b);yield return Hold(8);Press("sort-save");yield return Hold(18);Check(CharacterRoster.Ordered()[0].key=="siqiqiansuili","long-press sort order survives confirm");yield return Shot("dorm-sorted");
            Chapter("返回大厅 · 淡出宿舍，恢复大厅原版进场动画");Press("back");yield return Hold(42);yield return Shot("home-returned");
            Chapter("大厅立绘设置 · 点击相机图标，保存位置与大小后再次打开");Press("capture");yield return Hold(14);yield return Shot("home-edit");for(int i=0;i<24;i++){float q=i/23f;lobby.ReviewHomePose(90*q,-75*q,1+.18f*q);yield return Hold(1);}yield return Shot("home-edit-moved");Press("lobby-illustration-save");yield return Hold(12);var pose=LobbyIllustrationSettings.Get(406903);Check(Mathf.Abs(pose.x-90)<.01f&&Mathf.Abs(pose.y+75)<.01f&&Mathf.Abs(pose.scale-1.18f)<.001f,"lobby illustration pose is saved per skin");yield return Shot("home-saved");Press("capture");yield return Hold(10);yield return Shot("home-edit-reopened");Press("lobby-illustration-cancel");yield return Hold(10);
            screenOnly=true;lobby.PreviewHideCharacters();lobby.OpenCreate();lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Create);yield return Hold(15);yield return Shot("create-header-reference");
            yield return Size(1280,720);lobby.PreviewDorm("weilai",406903);yield return Hold(1);yield return Shot("dorm-16x9");lobby.PreviewDorm("weilai",406903,"info");yield return Hold(1);yield return Shot("profile-16x9");
        }
        void OnDestroy(){Application.logMessageReceived-=Log;CharacterRoster.EndPreview();AudioListener.pause=false;}
    }
}
