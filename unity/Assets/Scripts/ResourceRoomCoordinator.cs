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
        readonly HashSet<string> declined=new HashSet<string>(),pinned=new HashSet<string>();
        public readonly Dictionary<string,Rect> Targets=new Dictionary<string,Rect>();public string ReviewPress;
        public string[] Missing=>(client.State?.resourceSetup?.choices??new CharacterChoice[0]).Where(c=>!CharacterPackService.Supports(c.character,c.skin)).Select(c=>c.character).Distinct().Where(k=>!declined.Contains(k)).ToArray();
        bool Offered(string key)=>CharacterPackService.Find(key)?.available==true&&(client.State.resourceSetup?.choices??new CharacterChoice[0]).Where(c=>c.character==key).All(c=>CharacterPackService.ContainsSkin(key,c.skin));
        void Awake(){client=GetComponent<RiichiClient>();}
        void ResetPreparation(){foreach(var key in pinned)CharacterPackService.Unpin(key);pinned.Clear();declined.Clear();CharacterRoster.RemoteFallbackKeys.Clear();downloading=preloaded=preloading=false;reportSent=null;revision=0;}
        void Update(){
            var state=client.State;if(!client.LobbyActive||state==null||state.phase=="empty"){if(room!=null){ResetPreparation();room=null;}if(share!=null)share.Enabled=false;return;}
            if(room!=state.audioRoom){ResetPreparation();room=state.audioRoom;if(share==null)share=new CharacterPackShareServer();share.Rotate();offerSent=null;}
            share.Enabled=true;
            if(Time.unscaledTime>=nextOffer&&!client.Busy){nextOffer=Time.unscaledTime+.8f;UpdateShare(state);}
            if(state.phase!="lobby")return;
            var setup=state.resourceSetup;if(setup==null||setup.revision<=0||setup.choices==null){if(revision!=0)ResetPreparation();return;}
            if(setup.revision!=revision){ResetPreparation();revision=setup.revision;}
            foreach(string key in setup.choices.Select(c=>c.character).Distinct().Where(CharacterRoster.Available))if(pinned.Add(key))CharacterPackService.Pin(key);
            if(Missing.Length==0&&!preloaded&&!preloading)StartCoroutine(Preload(setup));
            string report=Missing.Length==0&&preloaded?(declined.Count>0?"fallback":"ready"):downloading?"downloading":"pending";
            string signature=revision+":"+report+":"+string.Join(",",declined.OrderBy(k=>k));if(reportSent!=signature&&!client.Busy){if(client.SendResourceRequest(new RequestBody{op="resourceAck",setupRevision=revision,resourceStatus=report,fallbackCharacters=declined.ToArray()}))reportSent=signature;}
            if(state.host&&setup.ready&&preloaded&&!client.Busy&&Time.unscaledTime>=nextStart){nextStart=Time.unscaledTime+1.5f;client.SendResourceRequest(new RequestBody{op="start",setupRevision=revision});}
        }
        IEnumerator Preload(ResourceSetupView setup){
            preloading=true;int version=revision;
            foreach(var c in setup.choices){
                var skin=CharacterRoster.Skin(c.character,c.skin);
                foreach(string path in CharacterFraming.AppearancePaths(skin.id)){
                    if(version!=revision)yield break;CharacterRoster.DisplayTexture(path);
                    while(CharacterPackService.DisplayPending(path)){if(version!=revision)yield break;yield return null;}yield return null;
                }
                string character=CharacterRoster.CanDisplay(c.character)?c.character:"yiji";
                foreach(string action in new[]{"act_rich","act_drich","act_ron","act_tumo","act_chi","act_pon","act_kan","act_babei"}){
                    if(version!=revision)yield break;string path="Voice/"+character+"/"+action;CharacterPackService.DisplayAsset<AudioClip>(path);
                    while(CharacterPackService.DisplayPending(path)){if(version!=revision)yield break;yield return null;}yield return null;
                }
            }
            if(version==revision){preloading=false;preloaded=true;}
        }
        void UpdateShare(RoomView state){
            var local=CharacterPackService.All.Where(p=>!p.builtin&&p.available&&CharacterPackService.Installed(p.key)&&File.Exists(CharacterPackService.FilePath(p))).ToArray();var entries=local.Select(p=>new CharacterPackShareServer.Entry{key=p.key,hash=p.sha256,bytes=p.bytes,path=CharacterPackService.FilePath(p)}).ToList();
            // Any room member can reach the host even when hotspot peer isolation prevents a direct transfer.
            if(state.host)foreach(var p in CharacterPackService.All.Where(p=>p.available&&!p.builtin&&!local.Any(x=>x.key==p.key))){var peer=state.resourcePeers?.FirstOrDefault(x=>x.ticket!=share.Ticket&&x.hashes.Contains(p.sha256));if(peer!=null)entries.Add(new CharacterPackShareServer.Entry{key=p.key,hash=p.sha256,bytes=p.bytes,url=PeerUrl(peer,p.sha256)});}
            share.SetEntries(entries);string signature=room+":"+string.Join(",",local.Select(p=>p.sha256).OrderBy(x=>x));if(offerSent!=signature){if(client.SendResourceRequest(new RequestBody{op="resourceOffer",resourceShare=new ResourceShareRequest{port=share.Port,ticket=share.Ticket,hashes=local.Select(p=>p.sha256).ToArray()}}))offerSent=signature;}
        }
        static string PeerUrl(ResourcePeer peer,string hash)=>"http://"+peer.address+":"+peer.port+"/packs/"+hash+"?ticket="+peer.ticket;
        public string LanUrl(string key){var p=CharacterPackService.Find(key);if(p==null||!Offered(key))return null;var state=client.State;var peer=state?.resourcePeers?.FirstOrDefault(x=>x.hashes.Contains(p.sha256));if(peer==null)return null;if(!state.host){var host=state.resourcePeers.FirstOrDefault(x=>x.seat==0);if(host!=null){string address=new Uri(client.LobbyEndpoint).Host;return "http://"+address+":"+host.port+"/packs/"+p.sha256+"?ticket="+host.ticket;}}return PeerUrl(peer,p.sha256);}
        void Choose(bool lan){foreach(string key in Missing){string url=lan?LanUrl(key):null;if(lan&&url==null)continue;CharacterPackService.Instance.Download(key,url);}downloading=true;}
        void Fallback(){CharacterPackService.Instance.Cancel();foreach(string key in Missing){declined.Add(key);CharacterRoster.RemoteFallbackKeys.Add(key);}downloading=false;preloaded=false;}
        bool Button(Rect r,string label,string id,bool primary=false){NativeUiChrome.Button(r,primary);Label(label,r.center.x,r.center.y,29,primary?new Color(.15f,.09f,.03f):Gold,1,TextAnchor.MiddleCenter,r.width,r.height);var m=GUI.matrix;var a=m.MultiplyPoint3x4(r.position);var b=m.MultiplyPoint3x4(new Vector3(r.xMax,r.yMax));Targets[id]=Rect.MinMaxRect(a.x,a.y,b.x,b.y);if(ReviewPress==id){ReviewPress=null;return true;}return Hit(r);}
        void OnGUI(){var state=client.State;if(!client.LobbyActive||state?.phase!="lobby"||state.resourceSetup==null)return;GUI.depth=-110;var matrix=GUI.matrix;var color=GUI.color;float scale=Mathf.Min(Screen.width/1920f,Screen.height/1080f);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1920*scale)/2,(Screen.height-1080*scale)/2),Quaternion.identity,new Vector3(scale,scale,1));GUI.color=Color.white;Targets.Clear();ScreenShade(new Color(0,0,0,.64f));Slice("official-panel-bg",new Rect(400,235,1120,610),30,30,30,30);Slice("official-panel-frame",new Rect(380,215,1160,650),90,90,90,90);Label("开局资源准备",960,305,46,Gold,1,TextAnchor.MiddleCenter);
            var missing=Missing;var service=CharacterPackService.Instance;
            if(missing.Length>0){Label("本机缺少："+string.Join("、",missing.Select(k=>CharacterPackService.Find(k)?.name??k)),960,395,32,Text,0,TextAnchor.MiddleCenter,1000,70,true);Label("需要下载 "+CharacterResourceUi.Size(missing.Sum(k=>CharacterPackService.Find(k)?.bytes??0))+"；本场角色已固定，完成资源确认后开始。",960,480,28,Muted,0,TextAnchor.MiddleCenter,1000,90,true);if(downloading){Label(!string.IsNullOrEmpty(service.Error)?service.Error:service.Stage+" "+Mathf.RoundToInt(service.Progress*100)+"%",960,565,30,Gold,0,TextAnchor.MiddleCenter);}
                bool enabled=GUI.enabled;GUI.enabled&=missing.All(Offered);if(Button(new Rect(450,665,310,68),"GitHub下载","room-resource-github",true))Choose(false);GUI.enabled=enabled;GUI.enabled&=missing.All(k=>LanUrl(k)!=null);if(Button(new Rect(805,665,310,68),"局域网接收","room-resource-lan"))Choose(true);GUI.enabled=enabled;if(Button(new Rect(1160,665,310,68),"暂不下载，用一姬","room-resource-fallback"))Fallback();
            }else{Label(preloaded?"本机资源已确认，等待其他玩家…":"正在加载本场立绘与语音…",960,440,34,Text,0,TextAnchor.MiddleCenter);Label(string.Join("   ",state.resourceSetup.players.Select(p=>"玩家"+(p.seat+1)+"："+(p.state=="ready"?"已完成":p.state=="fallback"?"一姬替代":p.state=="downloading"?"下载中":"等待确认"))),960,540,28,Muted,0,TextAnchor.MiddleCenter,1040,90,true);}
            if(state.host&&Button(new Rect(790,770,340,56),"取消本次开局准备","room-resource-cancel"))client.SendResourceRequest(new RequestBody{op="cancelResourceSetup"});GUI.matrix=matrix;GUI.color=color;
        }
        void OnDestroy(){ResetPreparation();share?.Dispose();}
    }
}
