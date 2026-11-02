using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static YunJian.Riichi.NativePanel;
namespace YunJian.Riichi {
    public sealed class WardrobeResourceUi {
        public static WardrobeResourceUi Current;
        public static readonly string[] Categories={"stick","winEffect","riichiEffect","callout","hand","music","headFrame","cloth","tileBack","tileFace","lobbyBackground","bgm"};
        public static readonly string[] Names={"立直棒","和牌特效","立直特效","鸣牌指示","手部","立直音乐","头像框","桌布","牌背","牌面","大厅背景","背景音乐"};
        readonly NativeScroll scroll=new NativeScroll();readonly ResourceDownloadBatch batch=new ResourceDownloadBatch();string category,prompt,pending,notice;Action<string> equip,pendingEquip;bool picker,pickerDownload,deleteAll,open,installedOnly;
        public readonly Dictionary<string,Rect> Targets=new Dictionary<string,Rect>();public string ReviewPress;
        public bool ModalOpen=>prompt!=null||picker||deleteAll||open||batch.Visible;
        public static string CategoryName(string key){int i=Array.IndexOf(Categories,key);return i<0?"全部装扮":Names[i];}
        public void Ask(string id,Action<string> select=null){prompt=id;equip=select;}
        public void Open(string filter=null){open=true;category=filter;scroll.Offset=0;}
        public void Close(){open=picker=deleteAll=false;prompt=null;batch.Close();}
        bool Control(Rect r,string id){var m=GUI.matrix;var a=m.MultiplyPoint3x4(r.position);var b=m.MultiplyPoint3x4(new Vector3(r.xMax,r.yMax));Targets[id]=Rect.MinMaxRect(a.x,a.y,b.x,b.y);if(ReviewPress==id&&GUI.enabled){ReviewPress=null;return true;}return Hit(r);}
        bool Button(Rect r,string label,string id,bool primary=false,int size=28){NativeUiChrome.Button(r,primary);Label(label,r.center.x,r.center.y,size,primary?new Color(.16f,.09f,.03f):Gold,1,TextAnchor.MiddleCenter,r.width,r.height);return Control(r,id);}
        void AskGroup(string filter){var ids=WardrobePackService.Directory.items.Where(x=>filter==null||x.category==filter).Select(x=>x.id).ToArray();batch.Open(filter==null?"下载全部装扮资源":"下载「"+CategoryName(filter)+"」资源",()=>ResourceDownloadUi.Wardrobes(ids),()=>{foreach(string id in ids)WardrobePackService.Instance.DownloadItem(id);});}
        static string Name(string id)=>WardrobeCatalog.Find(id)?.name??WardrobePackService.ForItem(id)?.name??id;
        public void DrawSettings(Rect view)=>DrawList(view,false);
        void DrawList(Rect view,bool standalone){
            var service=WardrobePackService.Instance;var layout=new ResourceListLayout(view,standalone);float width=layout.width,rowWidth=layout.rowWidth;
            Label("装扮资源 · "+CategoryName(category),20,30,standalone?46:40,Gold,1);
            if(Button(layout.Tool(0),"全部下载","deco-download-all",true,layout.buttonSize))AskGroup(null);
            if(Button(layout.Tool(1),"部分下载","deco-download-group",false,layout.buttonSize)){picker=true;pickerDownload=true;}
            if(Button(layout.Tool(2),"选择种类","deco-filter",false,layout.buttonSize)){picker=true;pickerDownload=false;}
            if(Button(layout.Tool(3),installedOnly?"全部装扮":"已下载","deco-installed",false,layout.buttonSize)){installedOnly=!installedOnly;scroll.Offset=0;}
            if(Button(layout.Tool(4),"全部删除","deco-delete-all",false,layout.buttonSize))deleteAll=true;
            var items=WardrobePackService.Directory.items.Where(x=>category==null||x.category==category).Where(x=>!installedOnly||WardrobePackService.Installed(x.id)).Where(x=>WardrobeCatalog.Find(x.id)?.hiddenFromSelection!=true).ToArray();
            var panel=layout.panel;float total=items.Length*125;scroll.Input(panel,total);
#if UNITY_EDITOR
            ReviewListBounds=panel;
#endif
            GUI.BeginGroup(panel);
            for(int i=0;i<items.Length;i++){
                var item=items[i];float y=i*125-scroll.Offset;if(y+114<0||y>panel.height)continue;var p=WardrobePackService.Find(item.pack);bool installed=WardrobePackService.Installed(item.id),available=WardrobePackService.Available(item.id);
                Slice("official-config-setting_entry_base",new Rect(0,y,rowWidth,114),20,20,20,20);var art=WardrobeCatalog.Texture(WardrobeCatalog.Find(item.id)?.icon);if(art!=null)GUI.DrawTexture(new Rect(14,y+13,88,88),art,ScaleMode.ScaleToFit);
                Label(Name(item.id),125,y+34,standalone?36:32,Gold,0,TextAnchor.MiddleLeft,rowWidth-390,42);
                string status=p.builtin?"内置基础 · 不可删除":installed?"已安装":!available?"资源尚未开放":service.ItemQueued(item.id)?service.DownloadKey==p.key?service.Stage+" "+Mathf.RoundToInt(service.Progress*100)+"%":"等待下载":"未下载";
                long bytes=WardrobePackService.Closure(p.key).Select(WardrobePackService.Find).Where(x=>!x.builtin).Sum(x=>x.bytes);Label(status+(p.builtin?"":" · "+CharacterResourceUi.Size(bytes)),125,y+81,standalone?28:25,Muted,0,TextAnchor.MiddleLeft,rowWidth-390,36);
                float actionWidth=standalone?260:220;var action=new Rect(rowWidth-25-actionWidth,y+28,actionWidth,58);if(p.builtin)continue;
                if(installed){if(Button(action,"删除","deco-delete-"+item.id)&&!WardrobePackService.DeleteItem(item.id,out notice))notice=notice??"删除失败";}
                else if(service.ItemQueued(item.id)){if(Button(action,"取消下载","deco-cancel-"+item.id))service.Cancel();}
                else{bool enabled=GUI.enabled;GUI.enabled&=available;if(Button(action,"下载","deco-download-"+item.id,true))Ask(item.id);GUI.enabled=enabled;}
            }
            GUI.EndGroup();Scrollbar(panel.x+rowWidth+18,panel.y+8,panel.height-16,scroll.Offset,total);
            string footer=!string.IsNullOrEmpty(service.Error)?service.Error:notice??(service.Busy?service.Stage+" · "+ResourceDownloadUi.ActiveNetworkText:"基础装扮可离线使用，其他装扮可单件或按种类下载。");
            Label(footer,15,layout.footerY,standalone?27:24,Muted,0,TextAnchor.MiddleLeft,width-(standalone?330:360),44,true);
        }
        public void DrawOverlay(){
            if(pending!=null&&WardrobePackService.Installed(pending)){var id=pending;var action=pendingEquip;pending=null;pendingEquip=null;action?.Invoke(id);}
            if(!ModalOpen)return;Targets.Clear();var matrix=GUI.matrix;var color=GUI.color;float scale=Mathf.Min(Screen.width/1920f,Screen.height/1080f);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1920*scale)/2,(Screen.height-1080*scale)/2),Quaternion.identity,new Vector3(scale,scale,1));GUI.color=Color.white;
            if(open){ScreenShade(new Color(0,0,0,.55f));Slice("official-panel-bg",new Rect(120,60,1680,960),30,30,30,30);Slice("official-panel-frame",new Rect(100,40,1720,1000),90,90,90,90);GUI.BeginGroup(new Rect(180,100,1560,920));DrawList(new Rect(0,0,1560,920),true);GUI.EndGroup();if(ResourceDownloadUi.Close(new Rect(1660,90,44,44),Control,"deco-resources-close"))Close();}
            if(prompt!=null||picker||deleteAll){ScreenShade(new Color(0,0,0,.62f));Slice("official-panel-bg",new Rect(400,220,1120,640),30,30,30,30);Slice("official-panel-frame",new Rect(380,200,1160,680),90,90,90,90);
                if(picker){Label("选择装扮种类",960,285,44,Gold,1,TextAnchor.MiddleCenter);var keys=new[]{""}.Concat(Categories).ToArray();for(int i=0;i<keys.Length;i++){string key=keys[i];if(Button(new Rect(455+i%3*345,350+i/3*88,320,66),CategoryName(key),"deco-category-"+key,(key==""?null:key)==category,30)){category=key==""?null:key;picker=false;scroll.Offset=0;if(pickerDownload)AskGroup(category);}}if(ResourceDownloadUi.Close(new Rect(1420,250,44,44),Control,"deco-picker-close"))picker=false;}
                else if(deleteAll){Label("删除可选装扮资源",960,305,44,Gold,1,TextAnchor.MiddleCenter);Label("基础装扮保留。\n对局、预览或传输中使用的资源会保留。\n已删除装扮将恢复为对应的基础款。",960,485,31,Text,0,TextAnchor.MiddleCenter,980,175,true);if(Button(new Rect(600,700,300,70),"取消","deco-delete-cancel"))deleteAll=false;if(Button(new Rect(1020,700,300,70),"确认删除","deco-delete-confirm",true)){WardrobePackService.DeleteAll();notice="已删除可清理的可选装扮，基础和正在使用的资源保留。";deleteAll=false;}}
                else{
                    var p=WardrobePackService.ForItem(prompt);Label("下载装扮资源",960,305,44,Gold,1,TextAnchor.MiddleCenter);var art=WardrobeCatalog.Texture(WardrobeCatalog.Find(prompt)?.icon);if(art!=null)GUI.DrawTexture(new Rect(530,390,150,150),art,ScaleMode.ScaleToFit);Label(Name(prompt),730,425,34,Gold,0,TextAnchor.MiddleLeft,690,70);var estimate=ResourceDownloadUi.Wardrobes(new[]{prompt});Label("需要下载 "+ResourceDownloadUi.Size(estimate.remainingBytes)+" · 完成后可离线使用",730,510,28,Muted,0,TextAnchor.MiddleLeft,690,90,true);Label(ResourceDownloadUi.NetworkText,960,620,28,ResourceDownloadUi.Network==NetworkReachability.ReachableViaCarrierDataNetwork?Gold:Muted,0,TextAnchor.MiddleCenter,980,60,true);
                    if(Button(new Rect(470,700,300,70),"取消","deco-prompt-cancel")){prompt=null;equip=null;}
                    bool enabled=GUI.enabled;GUI.enabled&=p!=null&&WardrobePackService.Available(prompt);
                    if(Button(new Rect(810,700,300,70),"下载","deco-prompt-download",true)){WardrobePackService.Instance.DownloadItem(prompt);prompt=null;equip=null;}
                    GUI.enabled&=equip!=null;if(Button(new Rect(1150,700,300,70),"下载并选用","deco-prompt-equip",true)){pending=prompt;pendingEquip=equip;WardrobePackService.Instance.DownloadItem(prompt);prompt=null;equip=null;}GUI.enabled=enabled;
                }
            }
            batch.Draw((r,label,id,primary)=>Button(r,label,id,primary),Control,"deco-batch");
            GUI.matrix=matrix;GUI.color=color;
        }
#if UNITY_EDITOR
        public Rect ReviewListBounds{get;private set;}
#endif
    }
    public sealed class WardrobeResourceOverlay:MonoBehaviour {void OnGUI(){GUI.depth=-102;WardrobeResourceUi.Current?.DrawOverlay();}}
}
