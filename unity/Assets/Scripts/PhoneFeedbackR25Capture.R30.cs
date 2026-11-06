#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    public sealed partial class PhoneFeedbackR25Capture {
        bool R31Mode=>Environment.GetEnvironmentVariable("YUNJIAN_R25_MODE")=="r31";
        bool R30Mode=>R31Mode||Environment.GetEnvironmentVariable("YUNJIAN_R25_MODE")=="r30";
        [Serializable] class R30Registration { public WardrobeItem[] items; }
        static readonly string[] FeedbackCases={"effect_liqi_23wansheng","effect_liqi_23winter","effect_liqi_nanfenghua","effect_liqi_25dongri","effect_liqi_xhzy","effect_liqi_21winter","effect_liqi_22summer","effect_liqi_23rpg","effect_liqi_chilin","ron_26shuiyi"};

        IEnumerator R30Run(){
            yield return Size(960,540);
            var work=repo+"/build/wardrobe-r30";
            var registered=JsonUtility.FromJson<R30Registration>(File.ReadAllText(work+"/registered.json"));
            foreach(var pack in WardrobePackService.All.Where(p=>p.key.StartsWith("deco-r30-"))){
                string source=work+"/packs/windows/"+pack.file;
                Check(File.Exists(source)&&WardrobePackService.HashFile(source)==pack.sha256,"Windows bundle digest "+pack.key);
                string dest=WardrobePackService.FilePath(pack);
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                if(!File.Exists(dest))File.Copy(source,dest);
                File.WriteAllText(dest+".ok",pack.sha256);
            }
            WardrobePackService.ResetForReview();
            client.Lobby.DesignPreviewGardenEntry();
            yield return Wait(8);
            if(R31Mode)yield return VerifyLiveHandAtlas(registered.items);
            report.fps=20;
            BeginRecording();
            var entries=R31Mode?FeedbackCases.Select(id=>registered.items.First(i=>i.id==id)):registered.items.OrderBy(i=>i.id=="ron_26shuiyi"?0:i.category=="winEffect"?1:i.category=="riichiEffect"?3:2);
            foreach(var row in entries){
                var item=WardrobeCatalog.Find(row.id);
                Check(item!=null&&WardrobePackService.Installed(row.id),"Registered and installed "+row.id);
                yield return WardrobePackService.Preload(new[]{row.id});
                if(item.category=="winEffect"){
                    var view=JsonUtility.FromJson<RoomView>(File.ReadAllText(repo+"/build/unity-smoke/win-4-ron-0-0.json"));
                    foreach(var player in view.players){player.character="yiji";player.skin=400101;player.wardrobe=WardrobeLoadout.Local();player.wardrobe.winEffect=item.id;}
                    view.audioRoom="r30-"+item.id;view.resourceSetup=null;view.dealtAt=0;view.openingHand=new string[0];
                    client.ShowValidationFixture(view);client.ValidationTableOnly=true;client.ValidationIntro.Reset();effects.Reset();
                    effects.ValidationFreeze=effects.ValidationSequentialClock=true;effects.ValidationTime=0;effects.Accept(view);
                    yield return Wait(3);
                    var win=view.result.details[0];var evt=new PublicSound{id=501,seat=win.seat,from=win.from,tile=win.tile,kind="ron",winSource=win.winSource};
                    typeof(TableEffects).GetMethod("SpawnWin",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(effects,new object[]{evt,-.1f});
                    Check(effects.ValidationWin!=null,"Gameplay win path "+item.id);
                    ChapterAt(item.name);
                    for(int f=0;f<115;f++){effects.ValidationTime=f/20f;yield return Frame();}
                    effects.Reset();
                }else if(item.category=="riichiEffect"){
                    var visual=GetComponent<RiichiDiscardVisual>();int before=visual.PlayedCount;
                    yield return UseDeclaration(item,0);
                    ChapterAt(item.name);
                    for(int f=0;f<(R31Mode?105:85);f++){
                        effects.ValidationTime=f/20f;
                        if(R31Mode&&(f==16||f==25))InspectFeedbackPose(item.id,visual.Latest,f);
                        yield return Frame();
                    }
                    Check(visual.PlayedCount==before+1,"Gameplay riichi path "+item.id);
                    if(R31Mode)Check(visual.ActiveCount==0,"Four-second cleanup "+item.id);
                    effects.Reset();
                }else if(item.category=="callout"){
                    yield return R28Setup(item.id,1);
                    var marker=GetComponent<WardrobeResponseIndicator>();
                    Check(marker.EffectRoot!=null,"Callable tile effect "+item.id);
                    ChapterAt(item.name);
                    for(int f=0;f<60;f++){marker.ValidationAge=f/20f;yield return Frame();}
                    marker.Clear();marker.ValidationFreeze=false;
                }else{
                    client.BeginLobbyValidation("r30-"+item.id);client.SessionPreviewOnly=true;client.ValidationTableOnly=false;
                    client.Lobby.PreviewWardrobe(item.category);
                    yield return Wait(5);
                    yield return FocusItem(item.id);
                    yield return Press("dress-item-"+item.id);
                    Check(WardrobeSelection.Get(item.category)?.id==item.id,"Wardrobe selection "+item.id);
                    yield return Press("dress-eye");
                    ChapterAt(item.name);
                    yield return Hold(40);
                }
            }
            if(R31Mode)yield return VerifyFeedbackDirections(registered.items);
            pipe.Close();encoder.WaitForExit();Check(encoder.ExitCode==0,"R30 video encoder completed");
            encoder.Dispose();encoder=null;pipe=null;Time.captureFramerate=0;
        }

        IEnumerator VerifyLiveHandAtlas(WardrobeItem[] items){
            var skin=WardrobeCatalog.Find("mjp_26gaoxiaosai");
            WardrobeSelection.Save(new System.Collections.Generic.Dictionary<string,string>{{"tileBack","mjp_default"}});
            yield return WardrobePackService.Preload(new[]{skin.id});
            using(var tiles=new OriginalTiles(back:skin)){
                var root=new GameObject("Live hand atlas stress test");
                try{
                    var hand=tiles.CreateHand(root.transform,"p1",Vector3.one*200);
                    var material=hand.GetComponent<MeshRenderer>().sharedMaterial;
                    var atlas=material.GetTexture("_BackTex");
                    var pack=WardrobePackService.ForItem(skin.id);
                    Check(atlas!=null&&WardrobePackService.Pinned(pack.key),"Live hand atlas owns a pack lease");
                    Check(WardrobeSelection.Get("tileBack").id!=skin.id,"Atlas lease survives independently of global selection");
                    foreach(var item in items.Where(i=>i.category=="riichiEffect"))yield return WardrobePackService.Preload(new[]{item.id});
                    yield return Wait(4);
                    Check(atlas!=null&&material.GetTexture("_BackTex")==atlas,"Hand texture survives loading all 46 riichi packs");
                }finally{Destroy(root);}
            }
        }

        IEnumerator VerifyFeedbackDirections(WardrobeItem[] items){
            var visual=GetComponent<RiichiDiscardVisual>();
            foreach(string id in new[]{"effect_liqi_23wansheng","effect_liqi_23winter","effect_liqi_25dongri","effect_liqi_21winter","effect_liqi_chilin"}){
                var item=items.First(i=>i.id==id);
                yield return WardrobePackService.Preload(new[]{id});
                for(int seat=1;seat<4;seat++){
                    int before=visual.PlayedCount;
                    yield return UseDeclaration(item,seat);
                    effects.ValidationTime=.7f;yield return Wait(2);
                    Check(visual.PlayedCount==before+1&&visual.Latest!=null,"Opponent seat "+seat+" plays "+id);
                    effects.ValidationTime=5.5f;yield return Wait(2);
                    Check(visual.ActiveCount==0,"Opponent seat "+seat+" cleans up "+id);
                    effects.Reset();
                }
            }
        }

        void InspectFeedbackPose(string id,WinTileEffect effect,int frame){
            if(effect==null)return;
            var rows=effect.GetComponentsInChildren<ParticleSystem>(true)
                .Where(p=>p.name.StartsWith("ya0")||p.name=="xian"||p.name.StartsWith("bao01"))
                .Select(p=>p.name+"="+p.particleCount+" pos="+p.transform.position);
            foreach(var skin in effect.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
                var mesh=new Mesh();skin.BakeMesh(mesh);
                Debug.Log("R31 skin "+id+" frame="+frame+" "+skin.name+" size="+mesh.bounds.size.ToString("F4"));
                Destroy(mesh);
            }
            Debug.Log("R31 pose "+id+" frame="+frame+" "+string.Join("; ",rows));
        }
    }
}
#endif
