using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static YunJian.Riichi.NativePanel;
namespace YunJian.Riichi {
    public sealed class ResourceRoomCoordinator:MonoBehaviour {
        RiichiClient client;CharacterPackShareServer share;string room,offerSent,reportSent;int revision;float nextOffer,nextStart;bool downloading,preloaded,preloading;
        int promptRevision=-1;bool promptHidden;int preloadDone,preloadTotal;string preloadLabel;
        public bool PromptFolded=>(promptHidden&&enabled||ReviewFolded)&&client.State?.phase=="lobby"&&client.State.resourceSetup!=null&&(ReviewFolded||promptRevision==client.State.resourceSetup.revision);
        // Editor recordings keep this component disabled (it talks to the room host).
        public bool ReviewFolded;
        public void Unfold(){promptHidden=false;}
        readonly HashSet<string> declined=new HashSet<string>(),pinned=new HashSet<string>();
        readonly HashSet<string> declinedWardrobes=new HashSet<string>(),pinnedWardrobes=new HashSet<string>();int preparationGeneration;
        public readonly Dictionary<string,Rect> Targets=new Dictionary<string,Rect>();public string ReviewPress;
        public string[] Missing=>(client.State?.resourceSetup?.choices??new CharacterChoice[0]).Where(c=>!CharacterPackService.Supports(c.character,c.skin)).Select(c=>c.character).Distinct().Where(k=>!declined.Contains(k)).ToArray();
        public string[] WantedWardrobes=>(client.State?.resourceSetup?.wardrobes??client.State?.players?.Select(p=>p.wardrobe).ToArray()??new WardrobeLoadout[0]).Where(w=>w!=null).SelectMany(w=>typeof(WardrobeLoadout).GetFields().Select(f=>f.GetValue(w)as string)).Where(id=>!string.IsNullOrEmpty(id)&&WardrobeCatalog.Find(id)!=null).Distinct().ToArray();
        public string[] MissingWardrobes=>WantedWardrobes.Where(id=>!declinedWardrobes.Contains(id)&&!WardrobePackService.Installed(id)).ToArray();
        bool Offered(string key)=>CharacterPackService.Find(key)?.available==true&&(client.State.resourceSetup?.choices??new CharacterChoice[0]).Where(c=>c.character==key).All(c=>CharacterPackService.ContainsSkin(key,c.skin));
        void Awake(){client=GetComponent<RiichiClient>();}
        void ResetPreparation(){preparationGeneration++;foreach(var key in pinned)CharacterPackService.Unpin(key);foreach(var key in pinnedWardrobes)WardrobePackService.Unpin(key);pinnedWardrobes.Clear();WardrobePackService.RemoteFallbackIds.Clear();declinedWardrobes.Clear();pinned.Clear();declined.Clear();CharacterRoster.RemoteFallbackKeys.Clear();downloading=preloaded=preloading=false;reportSent=null;revision=0;}
#if UNITY_EDITOR
        // Recordings draw the panel without any host traffic and set progress directly.
        public bool ReviewDisplayOnly;
        public void ReviewProgress(int done,int total,string label,bool finished){preloadDone=done;preloadTotal=total;preloadLabel=label;preloaded=finished;}
#endif
        void Update(){
#if UNITY_EDITOR
            if(ReviewDisplayOnly)return;
#endif
            var state=client.State;if(!client.LobbyActive||state==null||state.phase=="empty"){if(room!=null){ResetPreparation();room=null;}if(share!=null)share.Enabled=false;return;}
            if(room!=state.audioRoom){ResetPreparation();room=state.audioRoom;if(share==null)share=new CharacterPackShareServer();share.Rotate();offerSent=null;}
            share.Enabled=true;
            if(Time.unscaledTime>=nextOffer&&!client.Busy){nextOffer=Time.unscaledTime+.8f;UpdateShare(state);}
            if(state.phase!="lobby"){PinWardrobes();return;}
            var setup=state.resourceSetup;if(setup==null||setup.revision<=0||setup.choices==null){if(revision!=0)ResetPreparation();return;}
            if(setup.revision!=revision){ResetPreparation();revision=setup.revision;}
            foreach(string key in setup.choices.Select(c=>c.character).Distinct().Where(CharacterRoster.Available))if(pinned.Add(key))CharacterPackService.Pin(key);
            PinWardrobes();if(Missing.Length==0&&MissingWardrobes.Length==0&&!preloaded&&!preloading)StartCoroutine(Preload(setup));
            string report=Missing.Length==0&&MissingWardrobes.Length==0&&preloaded?(declined.Count+declinedWardrobes.Count>0?"fallback":"ready"):downloading?"downloading":"pending";
            string signature=revision+":"+report+":"+string.Join(",",declined.OrderBy(k=>k))+":"+string.Join(",",declinedWardrobes.OrderBy(k=>k))+(report=="downloading"?":"+Mathf.FloorToInt(Time.unscaledTime/30):"");if(reportSent!=signature&&!client.Busy){if(client.SendResourceRequest(new RequestBody{op="resourceAck",setupRevision=revision,resourceStatus=report,fallbackCharacters=declined.ToArray(),fallbackWardrobes=declinedWardrobes.ToArray()}))reportSent=signature;}
            if(state.host&&setup.ready&&preloaded&&!client.Busy&&Time.unscaledTime>=nextStart){nextStart=Time.unscaledTime+1.5f;client.SendResourceRequest(new RequestBody{op="start",setupRevision=revision});}
        }
        void PinWardrobes(){foreach(string key in WantedWardrobes.Where(WardrobePackService.Installed).Select(WardrobePackService.ForItem).Where(p=>p!=null).SelectMany(p=>WardrobePackService.Closure(p.key)).Distinct().Where(k=>!WardrobePackService.Find(k).builtin))if(pinnedWardrobes.Add(key))WardrobePackService.Pin(key);}
        IEnumerator Preload(ResourceSetupView setup){
            preloading=true;int version=revision,generation=preparationGeneration;bool Stale()=>version!=revision||generation!=preparationGeneration;
            // Request every character asset at once and wait for them together; the
            // former one-frame-per-asset walk (and repeating shared characters) made
            // even an all-AI table wait several seconds.
            var choices=setup.choices.GroupBy(c=>c.character+":"+c.skin).Select(g=>g.First()).ToArray();
            var textures=choices.SelectMany(c=>CharacterFraming.AppearancePaths(CharacterRoster.Skin(c.character,c.skin).id)).Distinct().ToArray();
            var voices=choices.Select(c=>CharacterRoster.CanDisplay(c.character)?c.character:"yiji").Distinct().SelectMany(ch=>new[]{"act_rich","act_drich","act_ron","act_tumo","act_chi","act_pon","act_kan","act_babei"}.Select(a=>"Voice/"+ch+"/"+a)).ToArray();
            var wanted=WantedWardrobes.Where(id=>!declinedWardrobes.Contains(id)).ToArray();
            int characterSteps=textures.Length+voices.Length,packs=WardrobePackService.PreloadPackCount(wanted);
            preloadTotal=characterSteps+packs;preloadDone=0;preloadLabel="角色立绘与语音";
            foreach(string path in textures)CharacterRoster.DisplayTexture(path);
            foreach(string path in voices)CharacterPackService.DisplayAsset<AudioClip>(path);
            while(true){if(Stale())yield break;int pending=textures.Count(CharacterPackService.DisplayPending)+voices.Count(CharacterPackService.DisplayPending);preloadDone=characterSteps-pending;if(pending==0)break;yield return null;}
            yield return WardrobePackService.Preload(wanted,(done,total,name)=>{preloadDone=characterSteps+done;if(!string.IsNullOrEmpty(name))preloadLabel=name;});
            if(!Stale()){preloadDone=preloadTotal;preloading=false;preloaded=true;}
        }
        void UpdateShare(RoomView state){
            var local=CharacterPackService.All.Where(p=>!p.builtin&&p.available&&CharacterPackService.Installed(p.key)&&File.Exists(CharacterPackService.FilePath(p))).ToArray();var entries=local.Select(p=>new CharacterPackShareServer.Entry{key=p.key,hash=p.sha256,bytes=p.bytes,path=CharacterPackService.FilePath(p)}).ToList();
            // Any room member can reach the host even when hotspot peer isolation prevents a direct transfer.
            if(state.host)foreach(var p in CharacterPackService.All.Where(p=>p.available&&!p.builtin&&!local.Any(x=>x.key==p.key))){var peer=state.resourcePeers?.FirstOrDefault(x=>x.ticket!=share.Ticket&&x.hashes.Contains(p.sha256));if(peer!=null)entries.Add(new CharacterPackShareServer.Entry{key=p.key,hash=p.sha256,bytes=p.bytes,url=PeerUrl(peer,p.sha256)});}
            var outfits=WardrobePackService.All.Where(p=>!p.builtin&&p.available&&WardrobePackService.RawInstalled(p)&&File.Exists(WardrobePackService.FilePath(p))).ToArray();entries.AddRange(outfits.Select(p=>new CharacterPackShareServer.Entry{key=p.key,hash=p.sha256,bytes=p.bytes,path=WardrobePackService.FilePath(p),wardrobe=true}));
            if(state.host)foreach(var p in WardrobePackService.All.Where(p=>p.available&&!p.builtin&&!outfits.Any(x=>x.key==p.key))){var peer=state.resourcePeers?.FirstOrDefault(x=>x.ticket!=share.Ticket&&x.hashes.Contains(p.sha256));if(peer!=null)entries.Add(new CharacterPackShareServer.Entry{key=p.key,hash=p.sha256,bytes=p.bytes,url=PeerUrl(peer,p.sha256),wardrobe=true});}
            var hashes=local.Select(p=>p.sha256).Concat(outfits.Select(p=>p.sha256)).Distinct().OrderBy(x=>x).ToArray();share.SetEntries(entries);string signature=room+":"+string.Join(",",hashes);if(offerSent!=signature){if(client.SendResourceRequest(new RequestBody{op="resourceOffer",resourceShare=new ResourceShareRequest{port=share.Port,ticket=share.Ticket,hashes=hashes}}))offerSent=signature;}
        }
        static string PeerUrl(ResourcePeer peer,string hash)=>"http://"+peer.address+":"+peer.port+"/packs/"+hash+"?ticket="+peer.ticket;
        public string LanUrl(string key){var p=CharacterPackService.Find(key);if(p==null||!Offered(key))return null;var state=client.State;var peer=state?.resourcePeers?.FirstOrDefault(x=>x.hashes.Contains(p.sha256));if(peer==null)return null;if(!state.host){var host=state.resourcePeers.FirstOrDefault(x=>x.seat==0);if(host!=null){string address=new Uri(client.LobbyEndpoint).Host;return "http://"+address+":"+host.port+"/packs/"+p.sha256+"?ticket="+host.ticket;}}return PeerUrl(peer,p.sha256);}
        public string WardrobeLanUrl(string key){var p=WardrobePackService.Find(key);if(p==null||!p.available)return null;var state=client.State;var peer=state?.resourcePeers?.FirstOrDefault(x=>x.hashes.Contains(p.sha256));if(peer==null)return null;if(!state.host){var host=state.resourcePeers.FirstOrDefault(x=>x.seat==0);if(host!=null)return "http://"+new Uri(client.LobbyEndpoint).Host+":"+host.port+"/packs/"+p.sha256+"?ticket="+host.ticket;}return PeerUrl(peer,p.sha256);}
        string[] MissingWardrobePacks()=>MissingWardrobes.Select(WardrobePackService.ForItem).Where(p=>p!=null).SelectMany(p=>WardrobePackService.Closure(p.key)).Distinct().Where(k=>!WardrobePackService.RawInstalled(WardrobePackService.Find(k))).ToArray();
        void Choose(bool lan){foreach(string key in Missing){string url=lan?LanUrl(key):null;if(lan&&url==null)continue;CharacterPackService.Instance.Download(key,url);}downloading=true;StartCoroutine(QueueWardrobeDownloads(lan,revision));}
        IEnumerator QueueWardrobeDownloads(bool lan,int version){int generation=preparationGeneration;while(CharacterPackService.Instance.Busy){if(version!=revision||generation!=preparationGeneration)yield break;yield return null;}if(version!=revision||generation!=preparationGeneration)yield break;foreach(string key in MissingWardrobePacks()){string url=lan?WardrobeLanUrl(key):null;if(!lan||url!=null)WardrobePackService.Instance.Download(key,url);}}
        void Fallback(){CharacterPackService.Instance.Cancel();WardrobePackService.Instance.Cancel();foreach(string key in Missing){declined.Add(key);CharacterRoster.RemoteFallbackKeys.Add(key);}foreach(string id in MissingWardrobes){declinedWardrobes.Add(id);WardrobePackService.RemoteFallbackIds.Add(id);}downloading=false;preloaded=false;}
        bool Button(Rect r,string label,string id,bool primary=false,int size=29){NativeUiChrome.Button(r,primary);Label(label,r.center.x,r.center.y,size,primary?new Color(.15f,.09f,.03f):Gold,1,TextAnchor.MiddleCenter,r.width,r.height);var m=GUI.matrix;var a=m.MultiplyPoint3x4(r.position);var b=m.MultiplyPoint3x4(new Vector3(r.xMax,r.yMax));Targets[id]=Rect.MinMaxRect(a.x,a.y,b.x,b.y);if(ReviewPress==id){ReviewPress=null;return true;}return Hit(r);}
        void OnGUI(){var state=client.State;if(!client.LobbyActive||state?.phase!="lobby"||state.resourceSetup==null)return;GUI.depth=-110;var matrix=GUI.matrix;var color=GUI.color;float scale=Mathf.Min(Screen.width/1920f,Screen.height/1080f);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1920*scale)/2,(Screen.height-1080*scale)/2),Quaternion.identity,new Vector3(scale,scale,1));GUI.color=Color.white;Targets.Clear();
            if(promptRevision!=state.resourceSetup.revision){promptRevision=state.resourceSetup.revision;promptHidden=false;}
            // While folded, the room page draws the reopen button beside 开始 in that button's style.
            if(promptHidden){GUI.matrix=matrix;GUI.color=color;return;}
            if(DismissOutside(new Rect(380,215,1160,650),()=>promptHidden=true)){GUI.matrix=matrix;GUI.color=color;return;}
            ScreenShade(new Color(0,0,0,.64f));Slice("official-panel-bg",new Rect(400,235,1120,610),30,30,30,30);Slice("official-panel-frame",new Rect(380,215,1160,650),90,90,90,90);Label("开局资源准备",960,305,46,Gold,1,TextAnchor.MiddleCenter);
            var missing=Missing;var service=CharacterPackService.Instance;
            if(missing.Length>0||MissingWardrobes.Length>0){var names=missing.Select(k=>CharacterPackService.Find(k)?.name??k).Concat(MissingWardrobes.Select(id=>WardrobeCatalog.Find(id)?.name??id)).ToArray();Label("本机缺少："+string.Join("、",names.Take(5))+(names.Length>5?" 等"+names.Length+"项":""),960,395,32,Text,0,TextAnchor.MiddleCenter,1000,70,true);Label("需要下载 "+CharacterResourceUi.Size(missing.Sum(k=>CharacterPackService.Find(k)?.bytes??0)+WardrobePackService.DownloadBytes(MissingWardrobes))+"；本场角色和装扮已固定，确认资源后开始。",960,480,28,Muted,0,TextAnchor.MiddleCenter,1000,90,true);if(downloading){var deco=WardrobePackService.Instance;string status=service.Busy?service.Stage+" "+Mathf.RoundToInt(service.Progress*100)+"%":deco.Busy?deco.Stage+" "+Mathf.RoundToInt(deco.Progress*100)+"%":!string.IsNullOrEmpty(service.Error)?service.Error:deco.Error;Label(status,960,565,30,Gold,0,TextAnchor.MiddleCenter);}
                bool enabled=GUI.enabled;GUI.enabled&=missing.All(Offered)&&MissingWardrobes.All(WardrobePackService.Available);if(Button(new Rect(450,665,310,68),"GitHub下载","room-resource-github",true))Choose(false);GUI.enabled=enabled;GUI.enabled&=missing.All(k=>LanUrl(k)!=null)&&MissingWardrobePacks().All(k=>WardrobeLanUrl(k)!=null);if(Button(new Rect(805,665,310,68),"局域网接收","room-resource-lan"))Choose(true);GUI.enabled=enabled;if(Button(new Rect(1160,665,310,68),missing.Length>0?"暂用基础形象":"暂用基础装扮","room-resource-fallback"))Fallback();
            }else DrawPreparationProgress(state);
            if(state.host&&Button(new Rect(780,752,360,84),"取消本次开局准备","room-resource-cancel",false,34))client.SendResourceRequest(new RequestBody{op="cancelResourceSetup"});GUI.matrix=matrix;GUI.color=color;
        }
        // Every phone loads this match's characters, voices and outfits into memory
        // before the deal, so nobody stalls mid-hand. Show how far this phone is and
        // which players are still loading; AI seats need nothing.
        void DrawPreparationProgress(RoomView state){
            float p=preloaded?1:preloadTotal>0?Mathf.Clamp01(preloadDone/(float)preloadTotal):0;
            Label(preloaded?"本机已加载完成，等待其他玩家":"本机正在加载本场角色与装扮",960,375,38,Text,0,TextAnchor.MiddleCenter);
            var bar=new Rect(530,420,860,26);var c=GUI.color;GUI.color=new Color(0,0,0,.45f);GUI.DrawTexture(bar,Texture2D.whiteTexture);GUI.color=Gold;GUI.DrawTexture(new Rect(bar.x+3,bar.y+3,(bar.width-6)*p,bar.height-6),Texture2D.whiteTexture);GUI.color=c;
            Label(preloaded?"100%":Mathf.RoundToInt(p*100)+"%  ·  "+preloadDone+"/"+Mathf.Max(1,preloadTotal)+(string.IsNullOrEmpty(preloadLabel)?"":"  ·  "+preloadLabel),960,478,30,Muted,0,TextAnchor.MiddleCenter,1000,50);
            var me=state.you;var rows=state.resourceSetup.players.OrderBy(x=>x.seat).Select(x=>{var player=x.seat<state.players.Length?state.players[x.seat]:null;string name=x.seat==me?"你":player?.name??"玩家"+(x.seat+1);string s=x.state=="ready"?"已就绪":x.state=="fallback"?"已就绪（部分用基础形象）":x.state=="downloading"?"正在下载":"正在加载";return name+"："+s;}).ToArray();
            for(int i=0;i<rows.Length;i++)Label(rows[i],i%2==0?560:980,548+i/2*48,32,rows[i].EndsWith("就绪")||rows[i].Contains("就绪（")?Text:Gold,0,TextAnchor.MiddleLeft,400,46);
       }
        void OnDestroy(){ResetPreparation();share?.Dispose();}
    }
}
