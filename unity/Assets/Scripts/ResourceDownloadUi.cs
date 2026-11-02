using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static YunJian.Riichi.NativePanel;
namespace YunJian.Riichi {
    public sealed class ResourceDownloadEstimate {
        public int packs;public long totalBytes,remainingBytes;
        public string Summary=>packs==0?"所选资源已全部下载，无需重复下载。":"待下载 "+packs+" 个资源包 · 合计 "+ResourceDownloadUi.Size(totalBytes)+"\n本次尚需下载／新增占用约 "+ResourceDownloadUi.Size(remainingBytes);
    }
    public static class ResourceDownloadUi {
#if UNITY_EDITOR
        public static NetworkReachability? ReviewNetwork;
#endif
        public static NetworkReachability Network {
            get{
#if UNITY_EDITOR
                if(ReviewNetwork.HasValue)return ReviewNetwork.Value;
#endif
                return Application.internetReachability;
            }
        }
        public static string NetworkText=>Network==NetworkReachability.NotReachable?"当前网络：未连接":Network==NetworkReachability.ReachableViaCarrierDataNetwork?"当前网络：移动网络 · 继续下载将使用流量":"当前网络：Wi-Fi / 有线网络";
        public static string ActiveNetworkText=>Network==NetworkReachability.ReachableViaCarrierDataNetwork?"当前正在使用移动网络进行下载":NetworkText;
        public static string Size(long bytes)=>bytes>=1000000000?(bytes/1000000000d).ToString("0.00")+" GB":(Math.Max(0,bytes)/1000000d).ToString("0.0")+" MB";
        static long Remaining(string file,long bytes){try{string partial=file+".part";long length=File.Exists(partial)?new FileInfo(partial).Length:File.Exists(file)&&!File.Exists(file+".ok")?new FileInfo(file).Length:0;return bytes-(length>=0&&length<=bytes?length:0);}catch(IOException){return bytes;}}
        public static ResourceDownloadEstimate Characters(IEnumerable<string> keys){var packs=keys.Distinct().Select(CharacterPackService.Find).Where(p=>p!=null&&p.available&&!p.builtin&&!CharacterPackService.Installed(p.key)).ToArray();return new ResourceDownloadEstimate{packs=packs.Length,totalBytes=packs.Sum(p=>p.bytes),remainingBytes=packs.Sum(p=>Remaining(CharacterPackService.FilePath(p),p.bytes))};}
        public static ResourceDownloadEstimate Wardrobes(IEnumerable<string> ids){var packs=ids.Distinct().Select(WardrobePackService.ForItem).Where(p=>p!=null).SelectMany(p=>WardrobePackService.Closure(p.key)).Distinct().Select(WardrobePackService.Find).Where(p=>p.available&&!p.builtin&&!WardrobePackService.RawInstalled(p)).ToArray();return new ResourceDownloadEstimate{packs=packs.Length,totalBytes=packs.Sum(p=>p.bytes),remainingBytes=packs.Sum(p=>Remaining(WardrobePackService.FilePath(p),p.bytes))};}
        public static bool Close(Rect image,Func<Rect,string,bool> hit,string id){Image("native-pop2_close",image);return hit(new Rect(image.x-18,image.y-18,image.width+36,image.height+36),id);}
        public static void DownloadDetails(ResourceDownloadEstimate estimate,float y=485){Label(estimate.Summary,960,y,30,Text,0,TextAnchor.MiddleCenter,960,125,true);Label(NetworkText,960,y+112,28,Network==NetworkReachability.ReachableViaCarrierDataNetwork?Gold:Muted,0,TextAnchor.MiddleCenter,980,65,true);}
    }
    public readonly struct ResourceListLayout {
        public readonly float width,rowWidth,buttonWidth,buttonHeight,footerY;public readonly int buttonSize;public readonly Rect panel;
        public ResourceListLayout(Rect view,bool standalone){width=view.width-40;rowWidth=width-32;buttonWidth=(width-80)/5;buttonHeight=standalone?66:58;buttonSize=standalone?34:30;footerY=view.height-(standalone?45:34);float top=standalone?158:150;panel=new Rect(10,top,width,footerY-36-top);}
        public Rect Tool(int i)=>new Rect(10+i*(buttonWidth+20),74,buttonWidth,buttonHeight);
    }
    public sealed class ResourceDownloadBatch {
        string title;Func<ResourceDownloadEstimate> estimate;Action confirm;ResourceDownloadEstimate snapshot;float nextEstimate;public bool Visible=>confirm!=null;
        public void Open(string heading,Func<ResourceDownloadEstimate> budget,Action download){title=heading;estimate=budget;confirm=download;snapshot=null;nextEstimate=0;}
        public void Close(){confirm=null;estimate=null;}
        public void Draw(Func<Rect,string,string,bool,bool> button,Func<Rect,string,bool> hit,string prefix){
            if(!Visible)return;ScreenShade(new Color(0,0,0,.62f));Slice("official-panel-bg",new Rect(400,220,1120,640),30,30,30,30);Slice("official-panel-frame",new Rect(380,200,1160,680),90,90,90,90);Label(title,960,300,44,Gold,1,TextAnchor.MiddleCenter);
            if(ResourceDownloadUi.Close(new Rect(1420,255,44,44),hit,prefix+"-close")){Close();return;}if(snapshot==null||Time.realtimeSinceStartup>=nextEstimate){snapshot=estimate();nextEstimate=Time.realtimeSinceStartup+.75f;}var budget=snapshot;ResourceDownloadUi.DownloadDetails(budget,465);
            if(button(new Rect(600,700,300,70),"取消",prefix+"-cancel",false)){Close();return;}
            bool enabled=GUI.enabled;GUI.enabled&=budget.packs>0&&ResourceDownloadUi.Network!=NetworkReachability.NotReachable;
            if(button(new Rect(1020,700,300,70),ResourceDownloadUi.Network==NetworkReachability.ReachableViaCarrierDataNetwork?"使用流量下载":"开始下载",prefix+"-confirm",true)){var action=confirm;Close();action();}GUI.enabled=enabled;
        }
    }
}
