using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    [InitializeOnLoad] public static class WardrobeEffectsReview {
        const string Key="WardrobeEffectsReview";
        static WardrobeEffectsReview(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<WardrobeEffectsProbe>();}};}
        public static void RunStudy(){SessionState.SetBool("WardrobeEffectsFull",false);Run();}
        public static void RunFull(){SessionState.SetBool("WardrobeEffectsFull",true);Run();}
        static void Run(){AssetDatabase.Refresh();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class WardrobeEffectsProbe:MonoBehaviour {
        [Serializable] class Chapter {public string name,title;public int start,end;}
        [Serializable] class Report {public int frames,fps=20,checks;public List<Chapter> chapters=new List<Chapter>();}
        Report report=new Report();bool full;string output;RiichiClient client;TableRenderer table;TableEffects fx;int frame;string caption;float listener;bool live;float inset;
        IEnumerator Start(){
            full=SessionState.GetBool("WardrobeEffectsFull",false);output=Path.GetFullPath(full?"../build/wardrobe-preview-r4/render":"../build/wardrobe-r4-study/render");Directory.CreateDirectory(output);Directory.CreateDirectory(Path.Combine(output,"frames"));File.Delete(Path.Combine(output,"error.txt"));
            listener=AudioListener.volume;AudioListener.volume=0;live=AppPreferences.DynamicOutfits;inset=AppPreferences.ScreenInset;AppPreferences.SetDynamicOutfits(false);AppPreferences.SetInset(0);CharacterRoster.BeginPreview();WardrobeSelection.BeginPreview();WardrobeSelection.Save(new Dictionary<string,string>{{"cloth","cloth-default"},{"tileFace","mjpface_default"},{"tileBack","mjp_default"}});Time.captureFramerate=20;
            client=GetComponent<RiichiClient>();table=GetComponent<TableRenderer>();fx=GetComponent<TableEffects>();GetComponent<ResourceRoomCoordinator>().enabled=false;
            var work=new Stack<IEnumerator>();work.Push(Capture());while(work.Count>0){object next;try{var it=work.Peek();if(!it.MoveNext()){work.Pop();continue;}next=it.Current;if(next is IEnumerator child){work.Push(child);continue;}}catch(Exception e){File.WriteAllText(Path.Combine(output,"error.txt"),e.ToString());Debug.LogException(e);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            report.frames=frame;File.WriteAllText(Path.Combine(output,"capture.json"),JsonUtility.ToJson(report,true));Cleanup();Debug.Log("WARDROBE EFFECTS REVIEW PASS "+report.checks);EditorApplication.Exit(0);
        }
        void Cleanup(){Time.captureFramerate=0;AudioListener.volume=listener;AppPreferences.SetDynamicOutfits(live);AppPreferences.SetInset(inset);CharacterRoster.EndPreview();WardrobeSelection.EndPreview();}
        void Check(bool value,string message){if(!value)throw new Exception(message);report.checks++;File.AppendAllText(Path.Combine(output,"checks.txt"),"PASS "+message+"\n");}
        IEnumerator Wait(int n=2){for(int i=0;i<n;i++)yield return null;}
        RoomView Fixture(string win=null,int seat=0){
            var view=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/unity-smoke/"+(win==null?"preview-4":"win-4-tsumo-"+seat+"-0")+".json"));view.audioRoom="effects-review-"+frame+"-"+seat;view.audioSeq=0;view.audioEvents=new PublicSound[0];view.resourceSetup=null;
            foreach(var p in view.players){p.character="yiji";p.skin=400101;p.wardrobe=new WardrobeLoadout{stick="stick-default",hand="hand-human",callout="mpzs_22event",riichiEffect="riichi-ice",winEffect=win??"ron-tornado"};}return view;
        }
        void Use(RoomView view){client.DesignPreviewSessionTable(view);client.ValidationTableOnly=true;client.ValidationIntro.Reset();fx.Reset();fx.ValidationFreeze=true;}
        IEnumerator Shot(string key){yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,key+".png"),image.EncodeToPNG());Destroy(image);}
        IEnumerator Frame(){yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,"frames",(frame++).ToString("D5")+".jpg"),image.EncodeToJPG(90));Destroy(image);}
        IEnumerator Capture(){
            var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(1688,768),"Original effects comparison"});yield return Wait(8);
            foreach(string id in new[]{"ron-flame","ron-tornado","win-default"}){
                caption=WardrobeCatalog.Find(id).name;var chapter=new Chapter{name=id,title=caption,start=frame};report.chapters.Add(chapter);var state=Fixture(id);Use(state);yield return Wait(3);
                var item=WardrobeCatalog.Find(id);var go=new GameObject(id);go.transform.SetParent(table.transform,false);var effect=go.AddComponent<WinTileEffect>();effect.Initialize(table.WinningPoint(new PublicSound{kind="tsumo",seat=0}),0,0,507,item.effect);
                foreach(float age in full?Enumerable.Range(0,66).Select(i=>i/20f):new[]{.08f,.2f,.4f,.7f,1.1f,1.6f,2.2f}){
                    effect.Sample(age);yield return Wait(1);if(full)yield return Frame();else yield return Shot(id+"-"+age.ToString("F2"));
                }
                Check(effect.LayerCount==(id=="ron-flame"?14:id=="ron-tornado"?9:7),id+" full source layer count");
                foreach(var p in effect.GetComponentsInChildren<ParticleSystem>())if(p.GetComponent<ParticleSystemRenderer>().renderMode==ParticleSystemRenderMode.VerticalBillboard)Check(p.main.startSizeY.constantMax>p.main.startSizeX.constantMax,"Vertical source pillar keeps tall Y size");
                Destroy(go);yield return Wait();chapter.end=frame;
            }
            foreach(string id in new[]{"mpzs_shandian","mpzs_22event"})foreach(int from in full?new[]{1,2,3}:new[]{1,2}){
                caption=WardrobeCatalog.Find(id).name+" · 牌河方向 "+from;var chapter=new Chapter{name=id+"-seat-"+from,title=caption,start=frame};report.chapters.Add(chapter);var state=Fixture();state.phase="reaction";var river=state.players[from].river;state.last=new LastDiscard{from=from,tile=river[river.Length-1].tile};state.pending=new PendingView{from=from,tile=state.last.tile,kind="discard",waiting=new[]{0}};state.options=new[]{new Choice{kind="pon",id="review-pon"},new Choice{kind="pass",id="review-pass"}};state.players[0].wardrobe.callout=id;Use(state);yield return Wait(3);
                var marker=GetComponent<WardrobeResponseIndicator>();marker.ValidationFreeze=true;
                foreach(float age in full?Enumerable.Range(0,72).Select(i=>i/20f):new[]{.05f,.2f,.5f,1.2f,2.4f}){marker.ValidationAge=age;yield return Wait();if(full)yield return Frame();else yield return Shot(id+"-seat-"+from+"-"+age.ToString("F2"));}
                Check(marker.Effect!=null&&marker.Effect.ParticleCount>0,"River marker stays live while operation is pending");Check(Vector3.Distance(marker.Effect.transform.position,table.ResponseTarget.position+Vector3.up*table.Geometry.Depth*.5f)<.001f,"Marker follows callable river tile");
                Check(marker.Effect.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith(id)),"Marker uses the responding viewer's outfit");var prior=marker.Effect;client.ValidationApplyState(state);yield return Wait();Check(marker.Effect==prior,"Polling preserves the same marker and animation phase");
                state.options=new[]{new Choice{kind="pass"}};client.ValidationApplyState(state);yield return Wait();Check(marker.Effect==null||!marker.Effect.gameObject.activeSelf,"No marker without a legal response");marker.Clear();chapter.end=frame;
            }
            caption="冰霜立直 · 原版竖式动作与固定桌面冰环";var ice=new Chapter{name="riichi-ice",title=caption,start=frame};report.chapters.Add(ice);var v=Fixture();int index=v.players[0].river.Length-1;v.players[0].river[index].riichi=true;Use(v);fx.Accept(v);yield return Wait(3);fx.ValidationPlay(0,"discard",v.players[0].river[index].tile,true,index+1);
            foreach(float age in full?Enumerable.Range(0,80).Select(i=>i/20f):new[]{.15f,.3f,.5f,.8f,1.2f,2f,3f}){fx.ValidationTime=age;yield return Wait(2);if(full)yield return Frame();else yield return Shot("ice-"+age.ToString("F2"));}
            ice.end=frame;fx.Reset();fx.ValidationFreeze=false;
            caption="普通立直棒 · 恢复默认装扮";var standard=new Chapter{name="default-stick",title=caption,start=frame};report.chapters.Add(standard);var normal=Fixture();normal.players[0].riichi=1;Use(normal);yield return Wait(3);Check(table.StickTarget(0).GetComponent<WardrobeModelVisual>()==null,"Default is the original plain riichi stick");yield return Shot("default-stick");if(full)for(int i=0;i<20;i++)yield return Frame();
            client.BeginLobbyValidation("effects-review");WardrobeSelection.Save(new Dictionary<string,string>{{"stick","stick-default"}});client.Lobby.PreviewWardrobe("stick");yield return Wait(5);yield return Shot("stick-list");client.Lobby.ValidationPress("dress-eye");yield return Wait(4);yield return Shot("stick-preview");if(full)for(int i=0;i<20;i++)yield return Frame();standard.end=frame;
        }
        void OnGUI(){if(string.IsNullOrEmpty(caption))return;GUI.Label(new Rect(Screen.width/2-280,8,560,34),caption,new GUIStyle(GUI.skin.label){fontSize=19,alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(1,.84f,.5f)}});}
    }
}
