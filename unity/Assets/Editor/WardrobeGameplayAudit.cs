using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    [InitializeOnLoad] public static class WardrobeGameplayAudit {
        const string Key="WardrobeGameplayAudit";
        static WardrobeGameplayAudit(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<WardrobeGameplayProbe>();}};}
        public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class WardrobeGameplayProbe:MonoBehaviour {
        int checks;float volume;string output;
        void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
        IEnumerator Start(){
            output=Path.GetFullPath("../build/wardrobe-gameplay-audit");Directory.CreateDirectory(output);File.Delete(Path.Combine(output,"error.txt"));File.Delete(Path.Combine(output,"passed.txt"));volume=AudioListener.volume;AudioListener.volume=0;WardrobeSelection.BeginPreview();
            var stack=new Stack<IEnumerator>();stack.Push(Audit());while(stack.Count>0){object next;try{var it=stack.Peek();if(!it.MoveNext()){stack.Pop();continue;}next=it.Current;if(next is IEnumerator child){stack.Push(child);continue;}}catch(Exception e){File.WriteAllText(Path.Combine(output,"error.txt"),e.ToString());Debug.LogException(e);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            File.WriteAllText(Path.Combine(output,"passed.txt"),checks+" assertions: three hand styles; 3/4 seats and every direction; carried/deposited stick, discard handoff, source particle emission, riichi music start/stop. Unity editor, no physical device.");Cleanup();Debug.Log("WARDROBE GAMEPLAY AUDIT PASS "+checks);EditorApplication.Exit(0);
        }
        void Cleanup(){AudioListener.volume=volume;WardrobeSelection.EndPreview();}
        IEnumerator Wait(){yield return null;yield return null;}
        [Serializable] class MeshInfo {public int vertices;}
        RoomView Fixture(int n,int seat,string kind,string hand){
            var view=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/unity-smoke/"+(kind=="tsumo"?"win-"+n+"-tsumo-"+seat+"-0":"preview-"+n)+".json"));view.audioRoom="wardrobe-audit-"+checks;view.audioSeq=0;view.audioEvents=new PublicSound[0];view.resourceSetup=null;
            foreach(var p in view.players){p.wardrobe=new WardrobeLoadout{hand="hand-human",stick="liqi_fish",riichiEffect="riichi-fire",winEffect="ron-tornado",callout="mpzs_shandian",music="music-riichi"};}
            view.players[seat].wardrobe.hand=hand;view.players[seat].wardrobe.stick=hand=="hand-cat"?"liqi_fish":"liqi_bone";view.players[seat].wardrobe.callout=hand=="hand-cat"?"mpzs_shandian":"mpzs_22event";view.players[seat].wardrobe.riichiEffect=hand=="hand-cat"?"riichi-fire":"riichi-ice";
            if(kind=="riichi-stick")view.players[seat].riichi=1;if(kind=="pon")view.players[seat].melds=new[]{"p555+"};return view;
        }
        IEnumerator Audit(){
            var client=GetComponent<RiichiClient>();var table=GetComponent<TableRenderer>();var fx=GetComponent<TableEffects>();client.SessionPreviewOnly=true;GetComponent<ResourceRoomCoordinator>().enabled=false;
            foreach(int n in new[]{3,4})foreach(string style in new[]{"hand-human","hand-cat","hand-cat-orange"})for(int seat=0;seat<n;seat++)foreach(string kind in new[]{"discard","riichi-stick","pon","tsumo"}){
                var view=Fixture(n,seat,kind,style);client.DesignPreviewSessionTable(view);client.ValidationTableOnly=true;client.ValidationIntro.Reset();fx.Accept(view);yield return Wait();
                string tile=kind=="tsumo"?view.result.details[0].tile:kind=="discard"?view.players[seat].river.First(p=>!p.called).tile:null;
                fx.ValidationPlay(seat,kind,tile,false,kind=="discard"?1:0);fx.ValidationTime=.1f;yield return Wait();
                var hand=GameObject.Find("Source hand mesh");var item=WardrobeCatalog.Find(style);var source=WardrobeRuntime.Motion(item,kind=="discard"?"discard-hand":kind=="pon"?"fulu-hand":kind=="tsumo"?"tsumo-hand":"lizhi-hand");
                Check(hand!=null&&hand.GetComponent<MeshFilter>().sharedMesh.vertexCount==JsonUtility.FromJson<MeshInfo>(Resources.Load<TextAsset>(source).text).vertices,n+" seat "+seat+" "+style+" "+kind+" mesh");
                Check(hand.GetComponent<MeshRenderer>().sharedMaterials[0].mainTexture==WardrobeCatalog.Texture(item.texture),"Selected hand texture");
                if(kind=="pon"){
                    fx.ValidationTime=.4f;yield return Wait();Check(fx.ValidationImpact!=null&&fx.ValidationImpact.Find("Equipped meld marker")==null,"Meld landing keeps its impact without a misplaced response indicator");
                }else{
                    fx.ValidationTime=fx.ValidationHandoff+.002f;yield return Wait();
                    var target=kind=="tsumo"?table.WinningTarget(new PublicSound{seat=seat,kind=kind}):kind=="riichi-stick"?table.StickTarget(seat):table.FindDiscard(seat,tile,1);
                    var carried=fx.ValidationAnimatedTile;string label=n+" seat "+seat+" "+style+" "+kind;
                    Check(target!=null&&target.gameObject.activeSelf,label+" landing target restored");Check(carried!=null&&!carried.gameObject.activeSelf,label+" carried object hidden at handoff");Check(Vector3.Distance(target.position,carried.position)<.003f,label+" no carried-object position jump");
                    if(kind=="riichi-stick")Check(target.GetComponent<WardrobeModelVisual>()!=null,"Equipped stick model remains after handoff");
                }
                fx.Reset();fx.ValidationFreeze=false;
            }
            foreach(string id in new[]{"music-riichi","music-8bit"}){
                var view=Fixture(4,0,"riichi-stick","hand-cat");view.players[0].wardrobe.music=id;view.phase="play";client.DesignPreviewSessionTable(view);client.ValidationIntro.Reset();yield return Wait();yield return Wait();
                Check(GetComponent<PlaylistAudio>().RiichiPlaying,"Selected riichi music starts: "+id);view.phase="result";yield return Wait();yield return new WaitForSecondsRealtime(RoundMusicEnvelope.Duration+.05f);Check(!GetComponent<PlaylistAudio>().RiichiPlaying,"Riichi music stops after the win fade");
            }
        }
    }
}
