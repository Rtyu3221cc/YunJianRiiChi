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
    [InitializeOnLoad] public static class WardrobeR16Preview {
        const string Key="WardrobeR16Preview";
        static WardrobeR16Preview(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<WardrobeR16Capture>();}};}
        public static void Run(){AssetDatabase.Refresh();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
    public sealed class WardrobeR16Capture:MonoBehaviour {
        string output;RiichiClient client;LobbyController lobby;TableEffects fx;TableRenderer table;bool dynamic;float inset;
        readonly List<string> checks=new List<string>();
        IEnumerator Start(){
            output=Path.GetFullPath("../dist/previews/2026-10-28-wardrobe-r16");Directory.CreateDirectory(output);
            dynamic=AppPreferences.DynamicOutfits;inset=AppPreferences.ScreenInset;AppPreferences.SetDynamicOutfits(false);AppPreferences.SetInset(0);CharacterRoster.BeginPreview();WardrobeSelection.BeginPreview();AudioListener.pause=true;
            client=GetComponent<RiichiClient>();client.BeginLobbyValidation("wardrobe-r16");client.SessionPreviewOnly=true;GetComponent<ResourceRoomCoordinator>().enabled=false;lobby=client.Lobby;fx=GetComponent<TableEffects>();table=GetComponent<TableRenderer>();
            var work=new Stack<IEnumerator>();work.Push(Run());
            while(work.Count>0){object next;try{var task=work.Peek();if(!task.MoveNext()){work.Pop();continue;}next=task.Current;if(next is IEnumerator child){work.Push(child);continue;}}catch(Exception e){File.WriteAllText(output+"/error.txt",e.ToString());Debug.LogException(e);Cleanup();EditorApplication.Exit(1);yield break;}yield return next;}
            File.WriteAllLines(output+"/capture-checks.txt",checks);Cleanup();Debug.Log("WARDROBE R16 SCREENSHOTS PASS "+checks.Count);EditorApplication.Exit(0);
        }
        void Cleanup(){fx.ValidationSequentialClock=false;fx.Reset();WardrobeSelection.EndPreview();CharacterRoster.EndPreview();AppPreferences.SetDynamicOutfits(dynamic);AppPreferences.SetInset(inset);AudioListener.pause=false;}
        void Check(bool pass,string text){if(!pass)throw new Exception(text);checks.Add("PASS "+text);}
        IEnumerator Wait(int count=4){for(int i=0;i<count;i++)yield return null;}
        IEnumerator Shot(string key){yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(output+"/"+key+".png",image.EncodeToPNG());Destroy(image);}
        IEnumerator Press(string id){Check(lobby.ValidationTarget(id).width>0,"Visible control "+id);lobby.ValidationPress(id);yield return Wait();}
        RoomView Load(string file){var v=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/unity-smoke/"+file+".json"));v.audioEvents=new PublicSound[0];v.dealtAt=0;v.openingHand=new string[0];return v;}
        IEnumerator Preview(string category,string id,string shot){
            WardrobeSelection.Save(new Dictionary<string,string>{{category,id}});lobby.PreviewWardrobe(category);yield return Wait(5);
            yield return Press("dress-eye");lobby.PreviewWardrobeAge(.65f);yield return Wait(5);yield return Shot(shot);yield return Press("dress-preview-close");
        }
        IEnumerator Run(){
            var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{new Vector2(1746,810),"Wardrobe R16"});yield return Wait(10);
            foreach(var category in new[]{"stick","cloth","tileBack","tileFace","headFrame","lobbyBackground","music"}){
                var items=WardrobeCatalog.Items(category);Check(items.Length>=3,"Expanded category "+category);
                foreach(var item in items){
                    foreach(var path in new[]{item.icon,item.texture,item.hand,item.table,item.preview,item.audio,item.modelPrefab}.Where(p=>!string.IsNullOrEmpty(p))){
                        var extensions=path==item.audio?new[]{".mp3"}:path==item.modelPrefab?new[]{".prefab"}:new[]{".png",".jpg"};
                        Check(extensions.Any(ext=>AssetDatabase.AssetPathToGUID("Assets/Resources/"+path+ext)!=""),"Imported runtime resource "+item.id+" / "+path);
                    }
                }
            }
            Check(WardrobeCatalog.Items("hand").Length==2,"Two visible hand choices retained");
            foreach(var item in WardrobeCatalog.Items("stick").Where(i=>!string.IsNullOrEmpty(i.modelPrefab))){
                var prefab=Resources.Load<GameObject>(item.modelPrefab);Check(prefab!=null&&prefab.GetComponentsInChildren<MeshFilter>(true).Length>0,"Stick has original mesh "+item.id);
                foreach(var r in prefab.GetComponentsInChildren<Renderer>(true))Check(r.sharedMaterials.All(m=>m!=null&&m.shader!=null&&m.mainTexture!=null),"Stick materials resolve "+item.id+" / "+r.name);
                var root=new GameObject("Remote-origin model probe");root.transform.position=new Vector3(600,600,600);root.AddComponent<WardrobeModelVisual>().Initialize(item,.74f);yield return Wait(1);
                var renderers=root.GetComponentsInChildren<MeshRenderer>();var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);
                Check(Mathf.Abs(b.center.x-600)<.01f&&Mathf.Abs(b.center.z-600)<.01f&&Mathf.Abs(b.size.x-.74f)<.02f,"Normalized model remains at its anchor "+item.id);Destroy(root);
            }
            yield return Resources.UnloadUnusedAssets();
            lobby.PreviewWardrobe("stick");yield return Wait(6);yield return Shot("01-stick-list");
            yield return Preview("stick","liqi_jinlongyu","02-goldfish-stick");
            yield return Preview("stick","liqi_25yh","03-glasses-stick");
            yield return Preview("stick","liqi_25summer","03b-fireworks-stick");
            yield return Preview("cloth","tablecloth_25xila","04-cloth-preview");
            yield return Preview("tileBack","mjp_tianrandaiyouling","05-tile-back-preview");
            yield return Preview("headFrame","headframe_25yh","06-frame-preview");
            yield return Preview("lobbyBackground","beijing_yuanlin","07-background-preview");
            yield return Preview("music","music-305026","08-riichi-music");
            lobby.Home();yield return Wait(5);yield return Shot("09-lobby-background");
            // Actual table uses the selected art and per-seat frozen stick loadouts.
            WardrobeSelection.Save(new Dictionary<string,string>{{"cloth","tablecloth_25xila"},{"tileBack","mjp_tianrandaiyouling"},{"tileFace","mjpface_25summer"}});
            var board=Load("preview-4");for(int i=0;i<board.n;i++){board.players[i].riichi=1;board.players[i].wardrobe=new WardrobeLoadout{stick=new[]{"liqi_jinlongyu","liqi_chocolate","liqi_25yh","liqi_akagi"}[i],headFrame="headframe_25yh"};}
            board.players[board.you].wardrobe.cloth="tablecloth_25xila";board.players[board.you].wardrobe.tileBack="mjp_tianrandaiyouling";board.players[board.you].wardrobe.tileFace="mjpface_25summer";
            client.ShowValidationFixture(board);yield return Wait(6);Check(table.StickTarget(0)!=null,"Equipped stick is deposited on table");yield return Shot("10-equipped-table");
            WardrobeSelection.Save(new Dictionary<string,string>{{"cloth","cloth-chigua"},{"tileBack","mjp_default"},{"tileFace","mjpface_default"}});table.Render(board);yield return Wait();
            Check(table.ValidationCloth==WardrobeCatalog.Texture(WardrobeCatalog.Find("tablecloth_25xila").texture)&&table.ValidationTileWardrobeKey=="mjpface_25summer/mjp_tianrandaiyouling","Polling keeps frozen cloth, back and face after local selection changes");
            // Test the boundaries using fu/han, including dealer and honba totals.
            foreach(var row in new[]{(1,110,false),(2,110,false),(3,60,false),(4,30,false),(3,70,true),(4,40,true),(5,20,true),(6,30,true),(8,30,true),(11,30,true),(13,20,true)}){
                var d=new WinDetail{score=new ScoreDetail{fanshu=row.Item1,fu=row.Item2,defen=18000}};Check(WinCutIn.Eligible(d)==row.Item3,"Cut-in threshold "+row.Item1+" han / "+row.Item2+" fu");
            }
            Check(WinCutIn.Eligible(new WinDetail{score=new ScoreDetail{damanguan=2}}),"Double yakuman has a banner");Check(!WinCutIn.Eligible((WinDetail)null),"Missing winner detail cannot trigger a banner");
            foreach(bool mangan in new[]{false,true}){
                var win=Load("win-4-tsumo-0-0");win.audioRoom="r16-win-"+mangan;var detail=win.result.details[0];detail.score.fanshu=mangan?5:2;detail.score.fu=30;detail.score.defen=mangan?12000:3000;detail.score.damanguan=0;detail.limit=mangan?"满贯":"";detail.yaku=mangan?new[]{new YakuDetail{name="立直",han=1},new YakuDetail{name="门前清自摸和",han=1},new YakuDetail{name="宝牌",han=3}}:new[]{new YakuDetail{name="立直",han=1},new YakuDetail{name="门前清自摸和",han=1}};
                client.ShowValidationFixture(win);fx.ValidationPlay(detail.seat,"tsumo",detail.tile);fx.ValidationTime=1.1f;yield return Wait(3);Check(fx.CutIn.Active==mangan,"Actual win action banner eligibility "+mangan);
                if(mangan){fx.CutIn.Freeze=true;fx.CutIn.FrozenAge=.55f;yield return Wait(2);yield return Shot("11-mangan-banner");}
                fx.Reset();var display=client.ValidationResult;display.Observe(win);display.Freeze=true;display.FrozenAge=2.2f;yield return Wait(4);Check(!display.ShowingScores,"Yaku page opens "+mangan);yield return Shot(mangan?"13-mangan-yaku":"12-ordinary-yaku");
            }
            var multi=Load("win-4-tsumo-0-0");multi.result.details=new[]{new WinDetail{seat=0,score=new ScoreDetail{fanshu=2,fu=30}},new WinDetail{seat=1,score=new ScoreDetail{fanshu=5,fu=30}}};Check(!WinCutIn.Eligible(multi,0)&&WinCutIn.Eligible(multi,1),"Double ron classifies each winner separately");
            foreach(bool mangan in new[]{false,true}){
                var after=Load("win-4-tsumo-0-0");after.audioRoom="r16-ron-"+mangan;after.result.details[0].score.fanshu=mangan?5:2;after.result.details[0].score.fu=30;after.result.details[0].score.damanguan=0;after.result.details[0].ron=true;
                var before=JsonUtility.FromJson<RoomView>(JsonUtility.ToJson(after));before.phase="reaction";before.result=null;before.audioSeq=0;
                client.ShowValidationFixture(before);fx.Accept(before);fx.ValidationFreeze=true;fx.ValidationSequentialClock=true;fx.ValidationTime=0;
                after.audioSeq=1;after.audioEvents=new[]{new PublicSound{id=1,seat=0,kind="ron",from=1,tile=after.result.details[0].tile}};client.ValidationApplyState(after);fx.Accept(after);fx.ValidationTime=1.1f;yield return Wait(3);
                Check(fx.CutIn.Active==mangan,"Queued ron banner classification "+mangan);Check(fx.ValidationWin!=null,"Queued ron retains its tile effect "+mangan);
                Check(Mathf.Abs(fx.ValidationWin.StartedAt-(1.2f+(mangan?WinCutIn.Duration+.18f:0)))<.001f,"No phantom banner delay for ordinary ron "+mangan);
                int spawned=fx.SpawnedWinEffects;fx.Accept(after);yield return Wait();Check(fx.SpawnedWinEffects==spawned,"Polling does not replay the ron presentation "+mangan);fx.ValidationSequentialClock=false;fx.Reset();
            }
            var playlist=GetComponent<PlaylistAudio>();Check(playlist.Tracks.Length==22,"All 22 original lobby/table music tracks available");
            var settings=(NativeSettingsUi)typeof(RiichiClient).GetField("nativeSettings",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(client);
            client.LobbyOpenSettings();settings.OpenMusic("mj");settings.ReviewMusicOffset=10000;yield return Wait(5);Check(settings.ReviewMusicOffset>0&&settings.ReviewMusicOffset<1000,"Table music scroll is clamped");
            Check(settings.ReviewTargets.ContainsKey("music-audition-"+playlist.Tracks.Last(t=>t.group=="mj").id),"Last table track can be auditioned");yield return Shot("14-table-music-scrolled");settings.Back();
        }
    }
}
