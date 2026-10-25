using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    [InitializeOnLoad] public static class WardrobePreview {
        const string Key="WardrobePreview1025";
        static WardrobePreview(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<WardrobePreviewCapture>();}};}
        public static void Run(){AssetDatabase.Refresh();foreach(string name in new[]{"mpzs_shandian","mpzs_22event"})ImportNativeReview.ImportDecoration(name);AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        public static void RunParticleStudy(){SessionState.SetBool("WardrobeParticleStudy",true);Run();}
    }
    public sealed class WardrobePreviewCapture:MonoBehaviour {
        [Serializable] sealed class Section {public string key,title;public int start,end;}
        [Serializable] sealed class ShotInfo {public string key,title;}
        [Serializable] sealed class Report {public int frames,fps=15;public bool formalTestsRun,apkBuilt;public string source="Unity editor actual rendering";public List<Section> sections=new List<Section>();public List<ShotInfo> shots=new List<ShotInfo>();}
        readonly Report report=new Report();string output;int frame;RiichiClient client;LobbyController lobby;bool live;float inset;
        IEnumerator Start(){
            bool study=SessionState.GetBool("WardrobeParticleStudy",false);SessionState.SetBool("WardrobeParticleStudy",false);output=Path.GetFullPath(study?"../build/wardrobe-preview-r2-study/render":"../build/wardrobe-preview-r3/render");Directory.CreateDirectory(output);Directory.CreateDirectory(Path.Combine(output,"frames"));
            live=AppPreferences.DynamicOutfits;inset=AppPreferences.ScreenInset;AppPreferences.SetDynamicOutfits(false);AppPreferences.SetInset(0);CharacterRoster.BeginPreview();WardrobeSelection.BeginPreview();AudioListener.pause=true;Application.targetFrameRate=15;Time.captureFramerate=15;
            client=GetComponent<RiichiClient>();client.BeginLobbyValidation("wardrobe-preview");lobby=client.Lobby;GetComponent<ResourceRoomCoordinator>().enabled=false;
            var work=new Stack<IEnumerator>();work.Push(study?ParticleStudy():Capture());while(work.Count>0){object next;try{var top=work.Peek();if(!top.MoveNext()){work.Pop();continue;}next=top.Current;if(next is IEnumerator child){work.Push(child);continue;}}catch(Exception e){File.WriteAllText(Path.Combine(output,"render-error.txt"),e.ToString());Debug.LogException(e);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            report.frames=frame;File.WriteAllText(Path.Combine(output,"capture.json"),JsonUtility.ToJson(report,true));Cleanup();Debug.Log("WARDROBE PREVIEW CAPTURE COMPLETE "+frame+" frames");EditorApplication.Exit(0);
        }
        void Cleanup(){Time.captureFramerate=0;WardrobeSelection.EndPreview();CharacterRoster.EndPreview();AppPreferences.SetDynamicOutfits(live);AppPreferences.SetInset(inset);AudioListener.pause=false;}
        IEnumerator Wait(int frames=5){for(int i=0;i<frames;i++)yield return null;}
        IEnumerator Record(int frames){for(int i=0;i<frames;i++){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,"frames",(frame++).ToString("D5")+".jpg"),t.EncodeToJPG(88));Destroy(t);}}
        IEnumerator Shot(string name,string title=null){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),t.EncodeToPNG());Destroy(t);report.shots.Add(new ShotInfo{key=name,title=title??name});}
        IEnumerator Press(string id){lobby.DesignPreviewPulse(id);lobby.ValidationPress(id);yield return Wait(3);}
        IEnumerator Category(string key){lobby.PreviewWardrobeCategory(key);yield return Wait(2);yield return Press("dress-category-"+key);yield return Record(9);}
        IEnumerator Preview(string shot,string title,float peak=.4f){lobby.DesignPreviewPulse("dress-eye");lobby.ValidationPress("dress-eye");yield return Record(45);lobby.PreviewWardrobeAge(peak);yield return Wait(2);yield return Shot(shot,title);yield return Press("dress-preview-close");}
        IEnumerator SectionCapture(string category,string title,string[] ids,string[] labels){
            var section=new Section{key=category,title=title,start=frame};report.sections.Add(section);yield return Category(category);yield return Shot(category+"-list",title+"：选择界面");
            for(int i=0;i<ids.Length;i++){yield return Press("dress-item-"+ids[i]);yield return Record(4);yield return Preview(category+"-"+i,labels[i],category=="callout"?.15f:category=="riichiEffect"?.28f:.4f);}
            section.end=frame;
        }
        IEnumerator Capture(){
            var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(1688,768),"Wardrobe visual preview"});yield return Wait(10);
            lobby.PreviewDorm("yiji",400101);yield return Wait(8);yield return Record(12);yield return Press("dress-tab");yield return Wait(6);yield return Shot("01-overview","装扮页面：分类、高亮、页签与扩大后的滚动区");
            yield return SectionCapture("stick","立直棒",new[]{"liqi_fish","liqi_scallion","liqi_bone"},new[]{"咸鱼立直棒原模型","大葱立直棒原模型","狗骨头立直棒原模型"});
            yield return SectionCapture("riichiEffect","立直特效",new[]{"riichi-fire","riichi-ice"},new[]{"原版烈焰立直粒子","原版冰霜立直粒子"});
            yield return SectionCapture("winEffect","和牌特效",new[]{"ron-tornado","win-default"},new[]{"原版火龙卷和牌粒子","原版雷光和牌粒子"});
            yield return SectionCapture("callout","鸣牌指示",new[]{"mpzs_shandian","mpzs_22event"},new[]{"鸣牌指示－雷驰电掣","鸣牌指示－咕噜咕噜"});
            yield return SectionCapture("hand","手的样式",new[]{"hand-cat","hand-cat-orange","hand-human"},new[]{"猫爪：原始骨骼动作","橘猫爪：原始骨骼动作","人类手部：原始骨骼动作"});
            lobby.PreviewWardrobeCategory("tileBack");yield return Wait(2);yield return Press("dress-item-mjp_2308event");
            yield return SectionCapture("cloth","桌布",new[]{"cloth-chigua","cloth-default"},new[]{"原版桌布预览：吃瓜","原版桌布预览：深蓝"});
            yield return SectionCapture("tileBack","牌背",new[]{"mjp_2308event","mjp_default"},new[]{"原版牌背预览条：猫咪","默认牌背"});
            yield return SectionCapture("tileFace","牌面",new[]{"mjpface_2308event","mjpface_default"},new[]{"原版牌面预览条：猫咪","默认牌面"});
            yield return SectionCapture("headFrame","头像框",new[]{"headframe_catear3","headframe_default"},new[]{"猫耳框与头像贴合","默认头像框"});
            yield return SectionCapture("lobbyBackground","大厅背景",new[]{"background-autumn","background-yard"},new[]{"神社庭院·秋深预览","神社庭院·春樱预览"});
            yield return SectionCapture("music","立直音乐",new[]{"music-riichi","music-8bit"},new[]{"原版立直音乐试听","原版八位立直音乐试听"});
            yield return Press("dress-save");yield return Record(12);yield return Shot("08-equipped","本地保存后的使用中状态");
            lobby.PreviewHideCharacters();lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Home);yield return Wait(10);yield return Record(20);yield return Shot("09-lobby-applied","秋深背景应用后的大厅");
            var state=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/unity-smoke/preview-4.json"));state.audioEvents=new PublicSound[0];state.audioRoom="wardrobe-preview";state.resourceSetup=null;for(int i=0;i<state.players.Length;i++){state.players[i].character=i%2==0?"yiji":"erjietang";state.players[i].skin=i%2==0?400101:400201;}
            client.DesignPreviewSessionTable(state);client.ValidationIntro.Reset();yield return Wait(10);yield return Record(25);yield return Shot("10-table-applied","修正头像与头像框间隙后的牌桌");
            yield return Gameplay();
        }
        void Check(bool value,string message){if(!value)throw new Exception(message);File.AppendAllText(Path.Combine(output,"gameplay-checks.txt"),"PASS "+message+"\n");}
        RoomView TableState(int pack,string kind){
            var state=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/unity-smoke/"+(kind=="tsumo"?"win-4-tsumo-0-0":"preview-4")+".json"));
            state.audioRoom="wardrobe-gameplay-"+pack+"-"+kind;state.audioEvents=new PublicSound[0];state.resourceSetup=null;state.audioSeq=0;
            foreach(var player in state.players){player.character="yiji";player.skin=400101;player.wardrobe=new WardrobeLoadout{hand=pack==0?"hand-cat":"hand-cat-orange",stick=pack==0?"liqi_fish":"liqi_bone",riichiEffect=pack==0?"riichi-fire":"riichi-ice",winEffect=pack==0?"ron-tornado":"win-default",callout=pack==0?"mpzs_shandian":"mpzs_22event",headFrame=pack==0?"headframe_catear3":"headframe_default",music=pack==0?"music-riichi":"music-8bit"};}
            if(kind=="discard")state.players[0].river[0].riichi=true;
            if(kind=="riichi-stick")state.players[0].riichi=1;
            if(kind=="pon")state.players[0].melds=new[]{"p555+"};
            return state;
        }
        IEnumerator Gameplay(){
            var fx=GetComponent<TableEffects>();var table=GetComponent<TableRenderer>();
            for(int pack=0;pack<2;pack++){
                var section=new Section{key="table-pack-"+pack,title=pack==0?"牌桌实录 A：猫爪、咸鱼棒、烈焰、龙卷、雷驰电掣":"牌桌实录 B：橘猫爪、骨头棒、冰霜、雷光、咕噜咕噜",start=frame};report.sections.Add(section);
                foreach(string kind in new[]{"discard","riichi-stick","pon","tsumo"}){
                    var state=TableState(pack,kind);client.DesignPreviewSessionTable(state);client.ValidationIntro.Reset();client.ValidationTableOnly=true;fx.Accept(state);yield return Wait(3);
                    string tile=kind=="tsumo"?state.result.details[0].tile:kind=="discard"?state.players[0].river[0].tile:null;
                    fx.ValidationPlay(0,kind,tile,kind=="discard",kind=="discard"?1:0);fx.CutIn.Freeze=true;fx.CutIn.FrozenAge=WinCutIn.Duration+1;
                    float length=kind=="discard"?3.3f:kind=="tsumo"?4.3f:kind=="pon"?3:1.7f;bool sawRiichi=false,sawWin=false,sawMarker=false;
                    for(int i=0;i<Mathf.CeilToInt(length*15);i++){
                        fx.ValidationTime=i/15f;fx.CutIn.FrozenAge=WinCutIn.Duration+1;yield return Wait(1);
                        sawRiichi|=GetComponent<RiichiDiscardVisual>().ActiveCount>0;sawWin|=fx.ValidationWin!=null&&fx.ValidationWin.ParticleCount>0;sawMarker|=fx.ValidationImpact!=null&&fx.ValidationImpact.Find("Equipped meld marker")!=null;
                        if(i==3){var hand=GameObject.Find("Source hand mesh");Check(hand!=null&&hand.GetComponent<MeshFilter>().sharedMesh.vertexCount==837,"Pack "+pack+" "+kind+" uses original cat action mesh");}
                        if(i==(kind=="discard"?8:kind=="tsumo"?22:5))yield return Shot("table-"+pack+"-"+kind,section.title+"："+kind);
                        yield return Record(1);
                    }
                    if(kind=="discard")Check(sawRiichi,"Pack "+pack+" equipped riichi effect triggered on declaration discard");
                    if(kind=="tsumo")Check(sawWin,"Pack "+pack+" equipped winning tile particles emitted");
                    if(kind=="pon")Check(!sawMarker,"Pack "+pack+" response indicator is not attached to melds");
                    if(kind=="riichi-stick")Check(table.StickTarget(0).GetComponent<WardrobeModelVisual>()!=null,"Pack "+pack+" deposited stick uses equipped source model");
                    fx.Reset();fx.ValidationFreeze=false;
                }
                section.end=frame;
            }
            client.ValidationTableOnly=false;report.formalTestsRun=true;
        }
        IEnumerator ParticleStudy(){
            var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(1688,768),"Original particle sampling"});yield return Wait(10);lobby.PreviewWardrobe("callout");yield return Wait(3);
            foreach(string id in new[]{"mpzs_shandian","mpzs_22event"}){
                yield return Press("dress-item-"+id);yield return Press("dress-eye");
                foreach(float age in new[]{0.01f,.04f,.08f,.15f,.3f,.5f,.8f}){lobby.PreviewWardrobeAge(age);yield return Wait(2);yield return Shot(id+"-"+age.ToString("F2"));File.AppendAllText(Path.Combine(output,"particles.txt"),id+" "+age+"\n"+lobby.PreviewWardrobeDiagnostics()+"\n");}
                yield return Press("dress-preview-close");
            }
        }
    }
}
