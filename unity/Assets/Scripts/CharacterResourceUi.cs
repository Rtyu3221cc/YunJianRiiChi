using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static YunJian.Riichi.NativePanel;
namespace YunJian.Riichi {
    public sealed class CharacterResourceUi {
        readonly NativeScroll scroll=new NativeScroll();readonly ResourceDownloadBatch batch=new ResourceDownloadBatch();string filter="all",prompt,pending,notice,focusKey;Action<string> equip,pendingEquip;bool startup,open,deleteAll,selectMode,focusPending,promptInSettings;readonly HashSet<string> selected=new HashSet<string>();public int SelectedCount=>selected.Count;const float RowPitch=125;
        public static CharacterResourceUi Current;
        public readonly Dictionary<string,Rect> Targets=new Dictionary<string,Rect>();public string ReviewPress;
        public bool ModalOpen=>prompt!=null||startup||deleteAll||open||batch.Visible;
        public void Open(string key=null){open=true;startup=false;ClearFocus();if(key!=null)Focus(key);}
        public void ClearFocus(){focusKey=null;focusPending=false;}
        public void Close(){open=false;prompt=null;deleteAll=false;batch.Close();ClearFocus();}
        public void OfferStartup(){if(!CharacterPackService.StartupSeen)startup=true;}
        public void Ask(string key,bool embedded=false,Action<string> select=null){prompt=key;promptInSettings=embedded;equip=select;startup=false;}
        void Focus(string key){focusKey=key;focusPending=true;filter="all";selectMode=false;selected.Clear();}
        public static string Size(long bytes)=>bytes<=0?"—":(bytes/1000000f).ToString("0.0")+" MB";
        bool Control(Rect r,string id){var m=GUI.matrix;var a=m.MultiplyPoint3x4(r.position);var b=m.MultiplyPoint3x4(new Vector3(r.xMax,r.yMax));Targets[id]=Rect.MinMaxRect(a.x,a.y,b.x,b.y);if(ReviewPress==id&&GUI.enabled){ReviewPress=null;return true;}return Hit(r);}
        bool Button(Rect r,string value,string id,bool primary=false,int size=30){NativeUiChrome.Button(r,primary);Label(value,r.center.x,r.center.y,size,primary?new Color(.16f,.09f,.03f):Gold,1,TextAnchor.MiddleCenter,r.width,r.height);return Control(r,id);}
        void AskGroup(bool recommended){var keys=CharacterPackService.All.Where(p=>p.available&&!p.builtin&&(!recommended||p.recommended)).Select(p=>p.key).ToArray();AskGroup(keys,recommended?"下载热门角色资源":"下载全部角色资源");}
        void AskGroup(string[] keys,string title){batch.Open(title,()=>ResourceDownloadUi.Characters(keys),()=>{foreach(string key in keys)CharacterPackService.Instance.Download(key);selected.Clear();selectMode=false;});}
        public void DrawSettings(Rect view)=>DrawResources(view,false);
        void DrawResources(Rect view,bool standalone){
            var service=CharacterPackService.Instance;var layout=new ResourceListLayout(view,standalone);float contentWidth=layout.width,rowWidth=layout.rowWidth;int buttonSize=layout.buttonSize;
            Label("角色资源",20,30,standalone?46:40,Gold,1);
            Rect Tool(int index)=>layout.Tool(index);
            if(Button(Tool(0),"全部下载","resources-all",true,buttonSize))AskGroup(false);
            if(Button(Tool(1),"部分下载","resources-recommended",false,buttonSize))AskGroup(true);
            if(Button(Tool(2),!selectMode?"自选下载":selected.Count>0?"下载所选("+selected.Count+")":"取消自选","resources-custom",false,buttonSize)){
                if(!selectMode)selectMode=true;else if(selected.Count>0)AskGroup(selected.ToArray(),"下载自选角色资源");else selectMode=false;
            }
            if(Button(Tool(3),"已下载","resources-filter",false,buttonSize)){filter=filter=="installed"?"all":"installed";scroll.Offset=0;focusKey=null;focusPending=false;}
            if(Button(Tool(4),"全部删除","resources-delete-all",false,buttonSize))deleteAll=true;
            var items=CharacterPackService.All.Where(p=>p.builtin||p.available).Where(p=>filter!="installed"||CharacterPackService.Installed(p.key)).ToArray();
            float footerY=layout.footerY;var panel=layout.panel;
#if UNITY_EDITOR
            ReviewListBounds=panel;
#endif
            if(focusPending){int index=Array.FindIndex(items,p=>p.key==focusKey);int rows=Mathf.Max(1,Mathf.FloorToInt((panel.height-114)/RowPitch)+1);if(index>=0)scroll.Offset=Mathf.Clamp((index-(rows-1)/2)*RowPitch,0,Mathf.Max(0,items.Length*RowPitch-panel.height));focusPending=false;}
            scroll.Input(panel,items.Length*RowPitch);GUI.BeginGroup(panel);
            for(int i=0;i<items.Length;i++){
                var p=items[i];float y=i*RowPitch-scroll.Offset;if(y+114<0||y>panel.height)continue;
                Slice("official-config-setting_entry_base",new Rect(0,y,rowWidth,114),20,20,20,20);
                if(p.key==focusKey){var tint=new Color(Gold.r,Gold.g,Gold.b,.95f);Box(new Rect(0,y,rowWidth,5),tint);Box(new Rect(0,y+109,rowWidth,5),tint);Box(new Rect(0,y,5,114),tint);Box(new Rect(rowWidth-5,y,5,114),tint);}
                var thumb=CharacterPackService.Thumbnail(p.key);if(thumb!=null)GUI.DrawTexture(new Rect(14,y+13,88,88),thumb,ScaleMode.ScaleToFit);
                bool installed=CharacterPackService.Installed(p.key);
                // Only these row labels move down by the requested five design pixels.
                Label(p.name,125,y+34,standalone?36:32,Gold);
                string state=p.builtin?"内置基础 · 不可删除":installed?(CharacterPackService.Ready(p.key)?"已安装":"校验加载中"):service.DownloadKey==p.key?service.Stage+"  "+Mathf.RoundToInt(service.Progress*100)+"%":service.Queued(p.key)?"等待下载":service.Results.TryGetValue(p.key,out var outcome)?outcome:"未下载";
                Label(state+(!p.builtin?" · "+Size(p.bytes):""),125,y+81,standalone?28:25,Muted);
                float actionWidth=standalone?260:220;var action=new Rect(rowWidth-25-actionWidth,y+28,actionWidth,58);
                if(!p.builtin){
                    if(selectMode&&!installed){var box=new Rect(action.x-65,y+37,40,40);Image("checkbox2",box);if(selected.Contains(p.key))Image("check_green",box);if(Button(action,selected.Contains(p.key)?"已选择":"选择","resource-select-"+p.key,false,buttonSize)){if(!selected.Add(p.key))selected.Remove(p.key);}}
                    else if(installed){if(Button(action,"删除","resource-delete-"+p.key,false,buttonSize)&&!CharacterPackService.Delete(p.key,out notice))notice=notice??"删除失败";}
                    else if(service.DownloadKey==p.key){if(Button(action,"取消下载","resource-cancel-"+p.key,false,buttonSize))service.Cancel();}
                    else if(Button(action,"下载","resource-download-"+p.key,true,buttonSize))Ask(p.key,!standalone);
                }
            }
            GUI.EndGroup();Scrollbar(panel.x+rowWidth+18,panel.y+8,panel.height-16,scroll.Offset,items.Length*RowPitch);
            Label(!string.IsNullOrEmpty(service.Error)?service.Error:service.Busy?ResourceDownloadUi.ActiveNetworkText:notice??"部分下载：热门角色；当前仅显示已发布的角色资源。",15,footerY,standalone?27:24,Muted,0,TextAnchor.MiddleLeft,contentWidth-(standalone?330:360),44,true);
        }
        public void DrawOverlay(){if(pending!=null&&CharacterPackService.Ready(pending)){string id=pending;var action=pendingEquip;pending=null;pendingEquip=null;action?.Invoke(id);}if(!ModalOpen)return;Targets.Clear();var matrix=GUI.matrix;var color=GUI.color;float s=Mathf.Min(Screen.width/1920f,Screen.height/1080f);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1920*s)/2,(Screen.height-1080*s)/2),Quaternion.identity,new Vector3(s,s,1));GUI.color=Color.white;
            if(open){ScreenShade(new Color(0,0,0,.55f));Slice("official-panel-bg",new Rect(120,60,1680,960),30,30,30,30);Slice("official-panel-frame",new Rect(100,40,1720,1000),90,90,90,90);GUI.BeginGroup(new Rect(180,100,1560,920));DrawResources(new Rect(0,0,1560,920),true);GUI.EndGroup();if(ResourceDownloadUi.Close(new Rect(1660,90,44,44),Control,"resources-close"))Close();}
            if(startup||prompt!=null||deleteAll){ScreenShade(new Color(0,0,0,.62f));bool single=prompt!=null&&!startup&&!deleteAll;Slice("official-panel-bg",single?new Rect(400,220,1120,640):new Rect(430,260,1060,560),30,30,30,30);Slice("official-panel-frame",single?new Rect(380,200,1160,680):new Rect(410,240,1100,600),90,90,90,90);
                if(startup){Label("下载角色资源",960,327,46,Gold,1,TextAnchor.MiddleCenter);long all=CharacterPackService.All.Where(p=>p.available&&!p.builtin&&!CharacterPackService.Installed(p.key)).Sum(p=>p.bytes),rec=CharacterPackService.All.Where(p=>p.available&&p.recommended&&!p.builtin&&!CharacterPackService.Installed(p.key)).Sum(p=>p.bytes);Label("一姬与二阶堂美树已内置，可直接离线游玩。",960,425,30,Text,0,TextAnchor.MiddleCenter);Label("全部可用资源："+Size(all)+"\n热门角色：目前可下载 "+Size(rec),960,525,29,Muted,0,TextAnchor.MiddleCenter,940,100,true);if(Button(new Rect(485,682,290,70),"全部下载","startup-all",true)){DismissStartup();Open();AskGroup(false);}if(Button(new Rect(815,682,290,70),"部分下载","startup-recommended")){DismissStartup();Open();AskGroup(true);}if(Button(new Rect(1145,682,290,70),"暂不下载","startup-skip"))DismissStartup();}
                else if(deleteAll){Label("删除可选角色资源",960,335,44,Gold,1,TextAnchor.MiddleCenter);Label("保留一姬与二阶堂美树。\n被删除角色将从出战、随机列表和星标中移除。\n对局或传输中使用的资源会保留。",960,490,30,Text,0,TextAnchor.MiddleCenter,920,160,true);if(Button(new Rect(620,690,290,65),"取消","delete-all-cancel"))deleteAll=false;if(Button(new Rect(1010,690,290,65),"确认删除","delete-all-confirm",true)){CharacterPackService.DeleteAll();notice=CharacterPackService.All.Any(p=>!p.builtin&&CharacterPackService.Installed(p.key))?"正在使用的资源已保留，其余可选资源已删除。":"已清除可选资源，一姬与二阶堂美树保留。";deleteAll=false;}}
                else{var p=CharacterPackService.Find(prompt);if(p==null){prompt=null;}else{Label("下载角色资源",960,305,44,Gold,1,TextAnchor.MiddleCenter);var thumb=CharacterPackService.Thumbnail(p.key);if(thumb!=null)GUI.DrawTexture(new Rect(530,390,150,150),thumb,ScaleMode.ScaleToFit);Label(p.name,730,425,34,Gold,0,TextAnchor.MiddleLeft,690,70);var estimate=ResourceDownloadUi.Characters(new[]{p.key});Label(p.available?"需要下载 "+ResourceDownloadUi.Size(estimate.remainingBytes)+" · 完成后可离线使用":"该角色资源尚未发布",730,510,28,Muted,0,TextAnchor.MiddleLeft,690,90,true);Label(ResourceDownloadUi.NetworkText,960,620,28,ResourceDownloadUi.Network==NetworkReachability.ReachableViaCarrierDataNetwork?Gold:Muted,0,TextAnchor.MiddleCenter,980,60,true);
                    if(Button(new Rect(470,700,300,70),"取消","resource-prompt-cancel")){prompt=null;equip=null;}
                    bool en=GUI.enabled;GUI.enabled&=p.available;if(Button(new Rect(810,700,300,70),"下载","resource-prompt-download",true)){CharacterPackService.Instance.Download(p.key);prompt=null;equip=null;if(promptInSettings)Focus(p.key);else Open(p.key);}
                    GUI.enabled&=equip!=null;if(Button(new Rect(1150,700,300,70),"下载并选用","resource-prompt-equip",true)){pending=p.key;pendingEquip=equip;CharacterPackService.Instance.Download(p.key);prompt=null;equip=null;}GUI.enabled=en;}}
            }
            batch.Draw((r,label,id,primary)=>Button(r,label,id,primary),Control,"resource-batch");
            GUI.matrix=matrix;GUI.color=color;
        }
#if UNITY_EDITOR
        public float ReviewScrollOffset=>scroll.Offset;
        public string ReviewFocusKey=>focusKey;
        public Rect ReviewListBounds{get;private set;}
        public void ReviewFocus(string key)=>Focus(key);
#endif
        void DismissStartup(){startup=false;CharacterPackService.MarkStartupSeen();}
    }
}
