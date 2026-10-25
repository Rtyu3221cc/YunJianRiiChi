using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi {
    public sealed partial class LobbyController {
        enum DormView { Select,Info,Skins,Appreciate,Wardrobe }
        DormView dormView,skinReturn=DormView.Select;bool dormOpen,dormStars,sortOpen,roleOpen,roleClosing,randomTab,roleStars,skinExpanded;
        float characterTransition=-10,roleStarted,roleClosed;int skinIndex;string rolePreview,sortDrag;Vector2 infoScroll,appreciationPan,panStart,pointerStart;float appreciationZoom=1,pinchStart;
        readonly CharacterIllustration characterArt=new CharacterIllustration();
        string[] sortOrder;readonly HashSet<string> sortStars=new HashSet<string>();
        Vector2 sortDragOffset,sortPointer;bool appreciationDragging;int expression;
        static readonly Color CharacterGold=new Color(.94f,.76f,.45f);
        CharacterDefinition CurrentCharacter=>CharacterRoster.Dorm;
        CharacterSkin CurrentOutfit=>CharacterRoster.Skin(CurrentCharacter.key,CharacterRoster.Outfit(CurrentCharacter.key));
        CharacterSkin BrowsingSkin=>CurrentCharacter.skins[Mathf.Clamp(skinIndex,0,CurrentCharacter.skins.Length-1)];
        bool CharactersOverlay=>roleOpen||roleClosing||sortOpen;
        void CImage(Rect r,string key,ScaleMode mode=ScaleMode.StretchToFill){var t=CharacterRoster.Texture("UI/"+key);if(t!=null)GUI.DrawTexture(r,t,mode,true);}
        void CSlice(Rect r,string key){
            var t=CharacterRoster.Texture("UI/"+key);var ui=CharacterRoster.Ui(key);if(t==null)return;var b=ui?.border;
            if(b==null||b.All(x=>x<=0)){GUI.DrawTexture(r,t,ScaleMode.StretchToFill,true);return;}
            float l=Mathf.Min(b[0],r.width/3),bb=Mathf.Min(b[1],r.height/3),rr=Mathf.Min(b[2],r.width/3),tt=Mathf.Min(b[3],r.height/3);
            float[] xs={r.x,r.x+l,r.xMax-rr,r.xMax},ys={r.y,r.y+tt,r.yMax-bb,r.yMax},us={0,b[0]/t.width,1-b[2]/t.width,1},vs={0,b[3]/t.height,1-b[1]/t.height,1};
            for(int y=0;y<3;y++)for(int x=0;x<3;x++)GUI.DrawTextureWithTexCoords(Rect.MinMaxRect(xs[x],ys[y],xs[x+1],ys[y+1]),t,new Rect(us[x],1-vs[y+1],us[x+1]-us[x],vs[y+1]-vs[y]));
        }
        GUIStyle CText(int size=30,TextAnchor align=TextAnchor.MiddleCenter,bool calligraphy=false)=>new GUIStyle(text){fontSize=size,font=calligraphy?brush:body,alignment=align,wordWrap=true};
        bool CButton(Rect r,string label,string id,bool blue=true){CSlice(r,blue?"button":"button_light");Label(r,label,CText(36,TextAnchor.MiddleCenter,true),blue?CharacterGold:new Color(.2f,.1f,.05f));return Hit(r,id);}
#if UNITY_EDITOR
        public void PreviewDorm(string role="weilai",int skin=0,string page="select"){CharacterRoster.Dorm=CharacterRoster.Find(role);if(skin!=0)CharacterRoster.Equip(role,skin);OpenDorm();dormView=page=="info"?DormView.Info:page=="skins"?DormView.Skins:page=="appreciate"?DormView.Appreciate:DormView.Select;skinIndex=Array.FindIndex(CurrentCharacter.skins,s=>s.id==CurrentOutfit.id);characterTransition=Now-1;ReviewSkinIndex(skinIndex,false);ReviewSettleDorm();}
        public void PreviewRandom(){OpenRoleSelection();randomTab=true;rolePreview="weilai";skinExpanded=true;roleStarted=Now-1;}
        public void PreviewRole(string role){OpenRoleSelection();rolePreview=role;skinExpanded=true;roleStarted=Now-1;}
        public void PreviewSkin(int index){ReviewSkinIndex(index,false);characterTransition=Now-1;}
        public void PreviewZoom(float zoom,Vector2 pan){appreciationZoom=zoom;appreciationPan=pan;}
        public void PreviewSort(){OpenCharacterSort();sortBegan=Now-1;}
        public void PreviewProfileExpressions(){infoScroll.y=575;}
        public void PreviewSortPointer(EventType type,Vector2 point){SortPointer(type,point);}
        public void PreviewHideCharacters(){dormOpen=sortOpen=roleOpen=roleClosing=homeIllustrationEditing=dormLeaving=pageMoving=sortClosing=false;}
#endif
    }
}
