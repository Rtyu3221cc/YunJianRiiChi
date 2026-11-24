using System;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi {
    public sealed partial class LobbyController {
        RoomCustomizationLayout roomDressLayout;
        bool roomCustomizationOpen,roomDressTab;
        string roomRole;
        Vector2 roomRoleScroll,roomSkinScroll,roomPresetScroll,roomCategoryScroll,roomItemScroll;
        readonly CharacterPaneDrag roomRoleDrag=new CharacterPaneDrag(),roomSkinDrag=new CharacterPaneDrag(),roomPresetDrag=new CharacterPaneDrag(),roomCategoryDrag=new CharacterPaneDrag(),roomItemDrag=new CharacterPaneDrag();
        bool RoomCustomizationAllowed=>client.State?.phase=="lobby"&&client.State.resourceSetup==null&&!client.CommandBusy&&(client.State.host||!(client.State.players.ElementAtOrDefault(client.State.you)?.ready??false));
        void OpenRoomCustomization(){
            if(!RoomCustomizationAllowed)return;roomDressLayout=roomDressLayout??new RoomCustomizationLayout();
            roomRole=CharacterRoster.Active.character;roomDressTab=false;roomCustomizationOpen=true;
            roomRoleScroll=roomSkinScroll=roomPresetScroll=roomCategoryScroll=roomItemScroll=Vector2.zero;
            roomRoleDrag.Reset();roomSkinDrag.Reset();roomPresetDrag.Reset();roomCategoryDrag.Reset();roomItemDrag.Reset();
            WardrobePresets.SyncActive();LoadPresetDraft(WardrobePresets.Active);dressCategory="stick";dressEye=false;
        }
        void CloseRoomCustomization(){roomCustomizationOpen=false;dressEye=false;StopDressPreview();client.PublishCharacterSelection();}
        void WearRoomCharacter(string key,int skin){CharacterRoster.Use(key,skin);roomRole=key;WarmSelectedIllustrations();client.PublishCharacterSelection();}
        void RoomText(string path,string value,int size=36,Color? color=null,Vector2 offset=default){Label(roomDressLayout.Rect(path,offset),value,CText(size,TextAnchor.MiddleCenter,true),color??CharacterGold);}
        void DrawRoomCustomization(){
            if(client.State?.phase!="lobby"||client.State.resourceSetup!=null){CloseRoomCustomization();return;}
            roomDressLayout.Place(width,roomDressTab);var ui=roomDressLayout;GUI.enabled&=!CharactersOverlay&&!client.LobbyOverlayVisible;
            Tint(new Rect(bleedLeft,0,bleedWidth,1080),new Color(0,0,0,.7019608f));
            ui.Draw("");ui.Draw("mask");ui.Draw("bound");
            ui.Draw("btn_tab/btn_char",alternate:roomDressTab?1:0);ui.Draw("btn_tab/btn_deco",alternate:roomDressTab?0:1);
            RoomText("btn_tab/btn_char/Text","角色",42,roomDressTab?CharacterGold:new Color(.2f,.1f,.04f));RoomText("btn_tab/btn_deco/Text","装扮",42,roomDressTab?new Color(.2f,.1f,.04f):CharacterGold);
            if(Hit(ui.Rect("btn_tab/btn_char"),"room-tab-character")){roomDressTab=false;dressEye=false;StopDressPreview();}
            if(Hit(ui.Rect("btn_tab/btn_deco"),"room-tab-wardrobe")){roomDressTab=true;dressEye=false;StopDressPreview();}
            ui.Draw("btn_tab/btn_close");if(Hit(ui.Rect("btn_tab/btn_close"),"room-customize-close")){CloseRoomCustomization();return;}
            bool enabled=GUI.enabled;GUI.enabled&=RoomCustomizationAllowed;
            if(roomDressTab)DrawRoomWardrobe();else DrawRoomCharacters();
            GUI.enabled=enabled;
        }
        void RoomScroll(Rect view,float content,ref Vector2 scroll,CharacterPaneDrag drag,Action<Vector2> draw){
            DragCharacterPane(drag,view,new Vector2(view.width,Mathf.Max(view.height,content)),ref scroll,false);
            var offset=targetOffset;var clip=roomDressLayout.Clip;GUI.BeginGroup(view);roomDressLayout.Clip=new Rect(0,0,view.width,view.height);targetOffset+=view.position;draw(-view.position-new Vector2(0,scroll.y));targetOffset=offset;roomDressLayout.Clip=clip;GUI.EndGroup();
        }
        void DrawRoomCharacters(){
            var ui=roomDressLayout;ui.Draw("page_char/PagePreview");ui.Draw("page_char/PagePreview/line");RoomText("page_char/PagePreview/title","外观选择",40);
            var roles=CharacterRoster.Ordered(installedOnly:true);var active=CharacterRoster.Active;
            const string root="page_char/scroll/content/templete/btn";var view=ui.Rect("page_char/scroll");
            RoomScroll(view,Mathf.CeilToInt(roles.Length/3f)*270,ref roomRoleScroll,roomRoleDrag,offset=>{
                for(int i=0;i<roles.Length;i++){
                    var delta=offset+new Vector2(i%3*285,i/3*270);var rect=ui.Rect(root,delta);if(rect.yMax<0||rect.y>view.height)continue;
                    var role=roles[i];if(roomRole==role.key)ui.Draw(root+"/choose",delta);ui.Draw(root+"/bg",delta);var head=CharacterRoster.ListPortrait(role,CharacterRoster.BattleSkin(role.key));if(head!=null)GUI.DrawTexture(ui.Rect(root+"/head",delta),head,ScaleMode.ScaleToFit,true);
                    ui.DrawNative(root+"/bound",ui.RoleBorder(role.id),delta);
                    if(active.character==role.key)ui.Draw(root+"/using",delta);
                    if(CharacterRoster.Favorite(role.key))ui.Draw(root+"/star",delta);
                    var name=ui.Rect(root+"/label_name",delta);name.y+=12;name.height-=24;VerticalCharacterLabel(name,role.name,role.name.Length>5?27:29,31,CharacterGold);
                    if(Hit(rect,"room-role-"+role.key)){roomSkinScroll=Vector2.zero;WearRoomCharacter(role.key,CharacterRoster.BattleSkin(role.key));}
                }
            });
            var rail=ui.Rect("page_char/Slider/line");ui.Draw("page_char/Slider/line");float max=Mathf.Max(0,Mathf.CeilToInt(roles.Length/3f)*270-view.height);
            if(max>0)ui.DrawImage(new Rect(rail.center.x-17,rail.y+(rail.height-34)*roomRoleScroll.y/max,34,34),"ui/lobby/main/pic/ss_slidebtn");
            var roleChoice=CharacterRoster.Find(roomRole)??CharacterRoster.Find(active.character);var skins=roleChoice.skins;
            const string skinRoot="page_char/PagePreview/page_item/Scroll View/Viewport/Content/templete";var skinView=ui.Rect("page_char/PagePreview/page_item/Scroll View/Viewport");
            RoomScroll(skinView,Mathf.CeilToInt(skins.Length/3f)*210,ref roomSkinScroll,roomSkinDrag,offset=>{
                for(int i=0;i<skins.Length;i++){
                    var skin=skins[i];var delta=offset+new Vector2(i%3*220,i/3*210);var rect=ui.Rect(skinRoot,delta);if(rect.yMax<0||rect.y>skinView.height)continue;
                    ui.Draw(skinRoot+"/btn/bg",delta);var head=CharacterRoster.ListPortrait(roleChoice,skin.id);if(head!=null)GUI.DrawTexture(ui.Rect(skinRoot+"/btn/icon",delta),head,ScaleMode.ScaleToFit,true);ui.Draw(skinRoot+"/btn/bound",delta);
                    if(active.character==roleChoice.key&&active.skin==skin.id){DressCorners(new Rect(rect.x-2,rect.y-2,183,183));}
                    if(skin.dynamic)CImage(new Rect(rect.x+2,rect.y+2,64,31),"Role/spine_tip",ScaleMode.ScaleToFit);
                    if(Hit(rect,"room-skin-"+skin.id))WearRoomCharacter(roleChoice.key,skin.id);
                }
            });
        }
        void DrawRoomWardrobe(){
            var ui=roomDressLayout;var presetView=ui.Rect("page_deco/Scroll View/Viewport");
            DrawWardrobePresetList(presetView,ref roomPresetScroll,roomPresetDrag,"room-preset-");
            DrawWardrobePresetName(ui.Rect("page_deco/btn_edit_name"));
            var nativeUse=ui.Rect("page_deco/btn_use");DrawWardrobePresetUse(new Rect(nativeUse.center.x-110,nativeUse.center.y-31,220,62),"room-preset-use",true);
            var categoryView=ui.Rect("page_deco/ContainerEffect/Viewport");
            const string cat="page_deco/ContainerEffect/Viewport/Content/slot0/bg";
            RoomScroll(categoryView,dressCategories.Length*130.2f,ref roomCategoryScroll,roomCategoryDrag,offset=>{
                for(int i=0;i<dressCategories.Length;i++){
                    var category=dressCategories[i];var delta=offset+new Vector2(0,i*130.2f);var rect=ui.Rect(cat,delta);if(rect.yMax<0||rect.y>categoryView.height)continue;
                    ui.Draw(cat,delta);ui.Draw(cat+"/line",delta);RoomText(cat+"/title",category.name,32,CharacterGold,delta);
                    var item=Draft(category.key);Label(ui.Rect(cat+"/desc",delta),item?.name??category.hint,WardrobeNameText(27,TextAnchor.MiddleLeft),CharacterGold);
                    DressImage(ui.Rect(cat+"/icon",delta),item?.icon??"Majsoul/Wardrobe/UI/"+category.icon);
                    if(category.key==dressCategory)ui.Draw(cat+"/chosen",delta);
                    if(Hit(rect,"room-category-"+category.key)){dressCategory=category.key;roomItemScroll=Vector2.zero;dressEye=false;StopDressPreview();}
                }
            });
            var pane=ui.Rect("page_deco/PagePreview");ui.Draw("page_deco/PagePreview");
            DrawWardrobeItems(pane,ref roomItemScroll,roomItemDrag,"room-item-","room-item-preview",true);
        }
        static string CharacterResourceUiSize(string id)=>CharacterResourceUi.Size(WardrobePackService.DownloadBytes(new[]{id}));
#if UNITY_EDITOR
        public void ReviewOpenRoomCustomization()=>OpenRoomCustomization();
        public bool ReviewRoomCustomizationOpen=>roomCustomizationOpen;
        public bool ReviewRoomCustomizationAllowed=>RoomCustomizationAllowed;
        public Rect ReviewRoomCustomizationRect(string path){roomDressLayout.Place(width,roomDressTab);return roomDressLayout.Rect(path);}
        public void ReviewRoomRoleScroll(float y){roomRoleScroll.y=y;}
#endif
    }
}
