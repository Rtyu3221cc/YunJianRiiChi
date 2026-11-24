using System;
using UnityEngine;

namespace YunJian.Riichi {
    public sealed partial class LobbyController {
        Vector2 dormPresetScroll;readonly CharacterPaneDrag dormPresetDrag=new CharacterPaneDrag();
        const float WardrobeCardSize=143,WardrobeColumnPitch=178,WardrobeRowPitch=182;
#if UNITY_EDITOR
        public sealed class WardrobeUiReview {public Rect pane,title,eye,view,use,name;public readonly System.Collections.Generic.Dictionary<string,Rect> cards=new System.Collections.Generic.Dictionary<string,Rect>();}
        public readonly WardrobeUiReview ReviewWardrobeUi=new WardrobeUiReview();
#endif
        void DrawWardrobePresetList(Rect view,ref Vector2 scroll,CharacterPaneDrag drag,string prefix){
            roomDressLayout??=new RoomCustomizationLayout();
            RoomScroll(view,(WardrobePresets.Count-1)*DressPresetPitch+DressPresetHeight,ref scroll,drag,offset=>{
                float k=view.width/258.11f;
                for(int i=0;i<WardrobePresets.Count;i++){
                    var r=new Rect(0,view.y+offset.y+i*DressPresetPitch,view.width,DressPresetHeight);if(r.yMax<0||r.y>view.height)continue;
                    bool selected=i==DressPreset;string image=selected?"tab_bright":"tab_gray";
                    roomDressLayout.DrawImage(new Rect((selected?22.28f:20.83f)*k,r.y,(selected?234.64f:238.43f)*k,r.height),"ui/lobby/extend/liaoshe_extra/pic/"+image);
                    Label(new Rect(37.55f*k,r.y+21,179.35f*k,60),(i+1).ToString(),CText(36),selected?new Color(.2f,.1f,.04f):CharacterGold);
                    if(i==WardrobePresets.Active)roomDressLayout.DrawImage(new Rect(4.3f*k,r.y+21+(69-69*k)/2,67*k,69*k),"ui/lobby/main/pic/mode_confirm",true);
                    if(Hit(r,prefix+i)&&i!=DressPreset){LoadPresetDraft(i);dressEye=false;StopDressPreview();}
                }
            });
        }
        void DrawWardrobePresetName(Rect r){
            Label(r,WardrobePresets.Name(DressPreset),CText(32,TextAnchor.MiddleCenter,true),CharacterGold);
#if UNITY_EDITOR
            ReviewWardrobeUi.name=r;
#endif
        }
        void DrawWardrobePresetUse(Rect r,string id,bool publish){
            bool wearing=DressPreset==WardrobePresets.Active;WSlice(r,"bf_popout",wearing?.8f:0);
            Label(r,wearing?"使用中":"使用",CText(30,TextAnchor.MiddleCenter,true),wearing?new Color(.6f,.6f,.62f):CharacterGold);
            if(!wearing&&Hit(r,id)){
                int skipped=WardrobePresets.Use(DressPreset);StopDressPreview();dressEye=false;LoadPresetDraft(DressPreset);
                if(skipped>0)DressPresetNotice(skipped+" 件未下载的装扮保持原样。");if(publish)client.PublishCharacterSelection();
            }
            if(dressPresetNotice!=null&&Now-dressPresetNoticeAt<3)Label(new Rect(r.center.x-356,r.yMax+2,712,28),dressPresetNotice,CText(21),new Color(.9f,.9f,.94f));
#if UNITY_EDITOR
            ReviewWardrobeUi.use=r;
#endif
        }
        static bool RoomCanPreviewWardrobe(string category)=>category=="headFrame"||category=="cloth"||category=="tileBack"||category=="tileFace"||category=="lobbyBackground";
        void DrawWardrobeItems(Rect pane,ref Vector2 scroll,CharacterPaneDrag drag,string itemPrefix,string eyeId,bool inRoom){
            var entries=DressEntries();var category=Array.Find(dressCategories,c=>c.key==dressCategory);
            var title=new Rect(pane.x+42,pane.y+15,460,70);Label(title,category.name,CText(39,TextAnchor.MiddleLeft,true),CharacterGold);WImage(new Rect(pane.x+36,pane.y+91,pane.width-72,2),"line");
            var eye=new Rect(pane.xMax-112,pane.y+10,76,76);
            if(entries.Length>0&&(!inRoom||RoomCanPreviewWardrobe(dressCategory))){WImage(new Rect(pane.xMax-104,pane.y+18,60,60),"dress_up_check_button");if(Hit(eye,eyeId))ToggleDressEye();}
#if UNITY_EDITOR
            ReviewWardrobeUi.pane=pane;ReviewWardrobeUi.title=title;ReviewWardrobeUi.eye=eye;ReviewWardrobeUi.cards.Clear();
#endif
            if(entries.Length==0){WImage(new Rect(pane.center.x-88,pane.center.y-69,176,138),"noinfo",ScaleMode.ScaleToFit);return;}
            if(dressEye){DrawDressPreview(pane,Draft(dressCategory));return;}
            var view=new Rect(pane.x+26,pane.y+112,pane.width-62,pane.height-136);
            float content=Mathf.Max(view.height,22+(Mathf.CeilToInt(entries.Length/4f)-1)*WardrobeRowPitch+WardrobeCardSize+(dressCategory=="hand"?44:0));
            DressItemRail(view,content,ref scroll);DragCharacterPane(drag,view,new Vector2(view.width,content),ref scroll,false);
            GUI.BeginGroup(view);var before=targetOffset;targetOffset+=view.position;
            float left=(view.width-(3*WardrobeColumnPitch+WardrobeCardSize))/2;
            for(int i=0;i<entries.Length;i++){
                var item=entries[i];var card=new Rect(left+i%4*WardrobeColumnPitch,11+i/4*WardrobeRowPitch-scroll.y,WardrobeCardSize,WardrobeCardSize);if(card.yMax<0||card.y>view.height)continue;
                CImage(card,"kuangbg");DressImage(new Rect(card.x+3,card.y+3,card.width-6,card.height-6),item.icon,ScaleMode.ScaleAndCrop);CImage(card,"kuangbound");
                if(Draft(dressCategory)?.id==item.id)DressCorners(new Rect(card.x-5,card.y-5,card.width+10,card.height+10));
                bool installed=WardrobePackService.Installed(item.id);
                if(!installed){var service=WardrobePackService.Instance;string status=service.TryGetItemDownloadProgress(item.id,out float progress)?DownloadCardProgress(progress):service.ItemQueued(item.id)?"等待下载":"下载 "+CharacterResourceUi.Size(WardrobePackService.DownloadBytes(new[]{item.id}));DownloadCardBand(card,status);}
                if(Hit(new Rect(card.x-5,card.y-5,card.width+10,card.height+10),itemPrefix+item.id)){
                    if(installed){EquipDressItem(dressCategory,item.id);if(inRoom)client.PublishCharacterSelection();}
                    else{string key=dressCategory;WardrobeResourceUi.Current.Ask(item.id,id=>{EquipDressItem(key,id);if(inRoom)client.PublishCharacterSelection();});}
                }
                if(dressCategory=="hand")Label(new Rect(card.x-15,card.yMax+6,card.width+30,38),item.name,WardrobeNameText(25),CharacterGold);
#if UNITY_EDITOR
                ReviewWardrobeUi.cards[item.id]=card;
#endif
            }
            targetOffset=before;GUI.EndGroup();
#if UNITY_EDITOR
            ReviewWardrobeUi.view=view;
#endif
        }
    }
}
