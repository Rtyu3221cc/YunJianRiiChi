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
    [InitializeOnLoad] public static class CharacterReviewR5 {
        const string Key="Riichi.CharacterReviewR5";
        static CharacterReviewR5(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<CharacterReviewR5Capture>();}};}
        public static void Run(){CharacterAssetImport.Configure();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class CharacterReviewR5Capture:MonoBehaviour {
        [Serializable] class Section {public int frame;public string title;}
        [Serializable] class Movie {public int fps=30,frames;public List<Section> sections=new List<Section>();public List<string> checks=new List<string>();}
        readonly Movie movie=new Movie();readonly List<string> errors=new List<string>();RiichiClient client;LobbyController lobby;string output;float clock=10;int frame;bool screenOnly;
        bool Quick=>Array.IndexOf(Environment.GetCommandLineArgs(),"-r5Quick")>=0;
        void Log(string m,string s,LogType t){if(t==LogType.Error||t==LogType.Exception)errors.Add(m);}
        IEnumerator Start(){
            output=Path.GetFullPath("../build/character-review-r5/render"+(Quick?"-quick":""));Directory.CreateDirectory(output);Directory.CreateDirectory(Path.Combine(output,"frames"));if(File.Exists(Path.Combine(output,"failed.txt")))File.Delete(Path.Combine(output,"failed.txt"));Application.logMessageReceived+=Log;var work=new Stack<IEnumerator>();work.Push(Capture());
            while(work.Count>0){object next;try{var top=work.Peek();if(!top.MoveNext()){work.Pop();continue;}next=top.Current;if(next is IEnumerator nested){work.Push(nested);continue;}}catch(Exception e){File.WriteAllText(Path.Combine(output,"failed.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
            movie.frames=frame;File.WriteAllText(Path.Combine(output,"movie.json"),JsonUtility.ToJson(movie,true));File.WriteAllText(Path.Combine(output,"render-notes.txt"),"Actual Unity render; scripted pointer input through shared production scroll routing and hit regions; navigation uses validation controls. No APK or publication.\n"+string.Join("\n",errors));CharacterRoster.EndPreview();AudioListener.pause=false;Debug.Log("CHARACTER REVIEW R5 COMPLETE errors="+errors.Count);EditorApplication.Exit(errors.Count==0?0:1);
        }
        void Chapter(string title){movie.sections.Add(new Section{frame=frame,title=title});}
        void Check(bool value,string label){if(!value)throw new Exception(label);movie.checks.Add(label);Debug.Log("R5 INPUT CHECK "+label);}
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
        string PoolSignature()=>string.Join("|",CharacterRoster.Pool.Select(p=>p.character+":"+p.skin));
        void CheckRandomFilter(bool stars){
            Check(lobby.ValidationTarget("random-character-weilai").width>0&&lobby.ValidationTarget("random-character-siqiqiansuili").width>0,"random filter retains both starred characters");
            Check((lobby.ValidationTarget("random-character-yiji").width==0)==stars&&(lobby.ValidationTarget("random-character-erjietang").width==0)==stars,"random filter "+(stars?"hides":"restores")+" both unstarred characters");
        }
        IEnumerator Capture(){
            CharacterRoster.BeginPreview();client=GetComponent<RiichiClient>();client.BeginLobbyValidation("character-r5-preview");lobby=client.Lobby;AudioListener.pause=true;yield return Size(1750,800);lobby.DesignPreviewClock=clock;CharacterRoster.Use("siqiqiansuili",403301);CharacterRoster.Equip("weilai",406903);CharacterRoster.Star("weilai");CharacterRoster.Star("siqiqiansuili");CharacterRoster.Reorder(new[]{"weilai","siqiqiansuili","erjietang","yiji"});lobby.DesignPreviewGardenEntry();yield return Hold(65);
            Chapter("切换雀士 · 页签相接、拼接缝修正、使用中文字右移 2px");yield return Tap("role");yield return Hold(18);yield return Shot("switch-main");
            Chapter("服饰展开 · 三角与边框相接，缩小底部空白");yield return Tap("battle-character-siqiqiansuili");yield return Hold(12);yield return Shot("switch-skins");
            Chapter("随机配置 · 候选数量数字右移 2px");yield return Tap("role-tab-random");yield return Tap("random-enabled");yield return Tap("random-character-weilai");yield return Tap("random-skin-406901");yield return Tap("random-skin-406903");yield return Tap("random-character-siqiqiansuili");yield return Tap("random-skin-403301");yield return Tap("random-character-yiji");yield return Hold(5);var popup=lobby.ReviewRoleSkinPopover;yield return Drag(ScreenPoint(popup.xMax-35,popup.center.y),ScreenPoint(popup.x+25,popup.center.y),24);yield return Tap("random-skin-400107");yield return Hold(10);yield return Shot("random-all-popup");
            string candidates=PoolSignature();Check(CharacterRoster.Pool.Length==4,"four outfit candidates grouped under three characters");CheckRandomFilter(false);
            Chapter("随机页面星标过滤 · 仅显示星标，已选列表保留");yield return Tap("random-stars");yield return Hold(10);CheckRandomFilter(true);Check(!lobby.ReviewRolePopoverOpen,"changing random star filter closes the outfit popover");Check(PoolSignature()==candidates,"enabling random star filter preserves all candidates");Check(lobby.ValidationTarget("random-pool-yiji").width>0,"unstarred selected candidate stays visible in the random list");yield return Shot("random-favorites");
            yield return Tap("random-character-weilai");yield return Hold(8);Check(lobby.ValidationTarget("random-skin-406901").width>0&&lobby.ValidationTarget("random-skin-406903").width>0,"starred character outfit selection remains available while filtered");yield return Shot("random-favorites-skins");
            Chapter("关闭星标过滤 · 恢复全部四名雀士");yield return Tap("random-stars");yield return Hold(10);CheckRandomFilter(false);Check(!lobby.ReviewRolePopoverOpen&&PoolSignature()==candidates,"disabling random star filter restores roster without changing candidates");yield return Shot("random-all");
            Chapter("没有星标时 · 显示空提示，候选列表仍保留");CharacterRoster.Star("weilai");CharacterRoster.Star("siqiqiansuili");yield return Tap("random-stars");yield return Hold(8);Check(CharacterRoster.Ordered(true).Length==0&&lobby.ValidationTarget("random-character-weilai").width==0&&lobby.ValidationTarget("random-character-yiji").width==0,"empty star filter exposes no roster avatars");Check(PoolSignature()==candidates&&lobby.ValidationTarget("random-pool-yiji").width>0,"empty star filter preserves existing candidates");yield return Shot("random-no-favorites");CharacterRoster.Star("weilai");CharacterRoster.Star("siqiqiansuili");yield return Hold(8);CheckRandomFilter(true);yield return Tap("random-stars");yield return Hold(6);yield return Shot("random-restored");
            screenOnly=true;yield return Size(1280,720);yield return Hold(6);yield return Shot("random-16x9");yield return Tap("role-tab-single");yield return Tap("battle-character-siqiqiansuili");yield return Hold(6);yield return Shot("switch-16x9");
        }
        void OnDestroy(){Application.logMessageReceived-=Log;CharacterRoster.EndPreview();AudioListener.pause=false;}
    }
}
