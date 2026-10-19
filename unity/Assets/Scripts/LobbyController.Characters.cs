using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi {
    public sealed partial class LobbyController {
        enum DormView { Select,Info,Skins,Appreciate }
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
        void DrawRoleSelection(){
            float p=roleClosing?1-Mathf.Clamp01((Now-roleClosed)/.16f):Mathf.Clamp01((Now-roleStarted)/.18f);if(roleClosing&&p<=0){roleClosing=false;return;}
            Tint(new Rect(bleedLeft,0,bleedWidth,1080),new Color(0,0,0,.32f*p));var old=GUI.color;var matrix=GUI.matrix;GUI.color=new Color(old.r,old.g,old.b,old.a*p);float scale=Mathf.Lerp(.93f,1,Mathf.SmoothStep(0,1,p));GUI.matrix*=Matrix4x4.Translate(new Vector3(width/2,540,0))*Matrix4x4.Scale(new Vector3(scale,scale,1))*Matrix4x4.Translate(new Vector3(-width/2,-540,0));
            float x=width/2-700;Tint(new Rect(x+5,67,1390,945),new Color(.035f,.055f,.11f,.99f));CSlice(new Rect(x,62,1400,955),"frame");
            CImage(new Rect(x+140,83,560,72),randomTab?"tabs_unselected":"tabs_selected");CImage(new Rect(x+700,83,560,72),randomTab?"tabs_selected":"tabs_unselected");
            Label(new Rect(x+140,80,560,75),"切换雀士",CText(40,TextAnchor.MiddleCenter,true),randomTab?Color.white:CharacterGold);Label(new Rect(x+700,80,560,75),"设定对局随机雀士",CText(40,TextAnchor.MiddleCenter,true),randomTab?CharacterGold:Color.white);
            if(Hit(new Rect(x+140,80,560,75),"role-tab-single")){randomTab=false;skinExpanded=false;}if(Hit(new Rect(x+700,80,560,75),"role-tab-random")){randomTab=true;skinExpanded=false;}
            CImage(new Rect(x+1305,82,70,70),"btn_close",ScaleMode.ScaleToFit);if(Hit(new Rect(x+1290,70,100,100),"role-close"))CloseRoleSelection();
            if(randomTab)DrawRandomRoster(x);else DrawActiveRoster(x);
            GUI.matrix=matrix;GUI.color=old;
        }
        void DrawActiveRoster(float x){
            Label(new Rect(x+100,188,1200,48),"在此设置大厅立绘与出战雀士",CText(28),new Color(.82f,.85f,.93f));
            DrawStarFilter(new Rect(x+1170,252,130,45),roleStars,"role-favorites",()=>roleStars=!roleStars);
            var roles=CharacterRoster.Ordered(roleStars);for(int i=0;i<roles.Length;i++){
                var role=roles[i];var r=new Rect(x+635+i*170,337,150,150);int sid=role.key==CharacterRoster.Active.character?CharacterRoster.Active.skin:CharacterRoster.Outfit(role.key);DrawCharacterCard(r,role,sid,role.key==rolePreview,false);
                if(Hit(r,"battle-character-"+role.key)){rolePreview=role.key;skinExpanded=true;CharacterRoster.Use(role.key,CharacterRoster.Outfit(role.key));client.PublishCharacterSelection();PlayCharacterGreeting(role.key);}
            }
            var selected=CharacterRoster.Find(rolePreview)??CharacterRoster.Find(CharacterRoster.Active.character);int activeSkin=selected.key==CharacterRoster.Active.character?CharacterRoster.Active.skin:CharacterRoster.Outfit(selected.key);DrawCharacterCard(new Rect(x+155,350,155,155),selected,activeSkin,false,false);Label(new Rect(x+90,520,290,60),selected.name,CText(32,TextAnchor.MiddleCenter,true),CharacterGold);
            Label(new Rect(x+90,588,400,88),"选择雀士头像可展开服饰",CText(27),new Color(.81f,.85f,.93f));
            if(skinExpanded){
                float popupWidth=Mathf.Min(725,selected.skins.Length*127+60),popupHeight=Mathf.CeilToInt(selected.skins.Length/5f)*132+46;CSlice(new Rect(x+565,520,popupWidth,popupHeight),"unfold_base");for(int i=0;i<selected.skins.Length;i++){
                    var s=selected.skins[i];var r=new Rect(x+595+(i%5)*127,550+(i/5)*132,110,110);DrawCharacterCard(r,selected,s.id,s.id==activeSkin,false,false);if(s.dynamic)CImage(new Rect(r.xMax-35,r.y,35,25),"spine_tip",ScaleMode.ScaleToFit);
                    if(Hit(r,"battle-skin-"+s.id)){CharacterRoster.Use(selected.key,s.id);client.PublishCharacterSelection();PlayCharacterGreeting(selected.key);}
                }
            }
            Label(new Rect(x+115,770,350,75),"当前出战雀士",CText(32,TextAnchor.MiddleCenter,true),CharacterGold);
            Label(new Rect(x+590,899,660,50),"当前出战："+CharacterRoster.Find(CharacterRoster.Active.character).name+" · "+CharacterRoster.Skin(CharacterRoster.Active.character,CharacterRoster.Active.skin).name,CText(27),CharacterGold);
        }
        void DrawRandomRoster(float x){
            Label(new Rect(x+215,175,1020,60),"当前模式：随机角色，对局开始时从已选雀士／服饰中随机选择",CText(26,TextAnchor.MiddleLeft),Color.white);
            var toggle=new Rect(x+55,185,145,45);CImage(toggle,"btn_rand_bg");Label(new Rect(CharacterRoster.Random?toggle.x+5:toggle.x+70,toggle.y,70,45),"随机",CText(27,TextAnchor.MiddleCenter,true),CharacterGold);CImage(new Rect(CharacterRoster.Random?toggle.xMax-60:toggle.x+5,toggle.y,60,45),"btn_rand_slider",ScaleMode.ScaleToFit);if(Hit(toggle,"random-enabled")&&CharacterRoster.Pool.Length>0){CharacterRoster.Random=!CharacterRoster.Random;client.PublishCharacterSelection();}
            CSlice(new Rect(x+50,245,425,715),"block_base");CSlice(new Rect(x+585,245,760,715),"block_base");Label(new Rect(x+80,252,260,65),"随机列表",CText(38,TextAnchor.MiddleLeft,true),CharacterGold);if(CButton(new Rect(x+313,266,130,44),"清空","random-clear")){CharacterRoster.ClearPool();client.PublishCharacterSelection();}
            Label(new Rect(x+625,252,520,65),"宿舍",CText(38,TextAnchor.MiddleLeft,true),CharacterGold);DrawStarFilter(new Rect(x+1185,263,130,45),roleStars,"random-stars",()=>roleStars=!roleStars);
            var pool=CharacterRoster.Pool;for(int i=0;i<pool.Length;i++){
                var item=pool[i];var r=new Rect(x+85+(i%3)*120,350+(i/3)*135,104,104);DrawCharacterCard(r,CharacterRoster.Find(item.character),item.skin,false,false);CImage(new Rect(r.xMax-27,r.y-5,33,33),"btn_del",ScaleMode.ScaleToFit);if(Hit(r,"random-remove-"+item.character+"-"+item.skin)){CharacterRoster.TogglePool(item.character,item.skin);client.PublishCharacterSelection();}
            }
            if(pool.Length==0)Label(new Rect(x+85,460,340,100),"选择右侧雀士的服饰\n加入随机列表",CText(29),new Color(.72f,.77f,.88f));
            var roles=CharacterRoster.Ordered(roleStars);for(int i=0;i<roles.Length;i++){
                var role=roles[i];var r=new Rect(x+620+i*170,350,150,150);DrawCharacterCard(r,role,CharacterRoster.Outfit(role.key),role.key==rolePreview,false);int n=pool.Count(c=>c.character==role.key);if(n>0)Label(new Rect(r.xMax-40,r.yMax-40,40,40),n.ToString(),CText(25),CharacterGold);if(Hit(r,"random-character-"+role.key)){rolePreview=role.key;skinExpanded=true;}
            }
            if(skinExpanded){var role=CharacterRoster.Find(rolePreview);CSlice(new Rect(x+605,530,Mathf.Min(710,role.skins.Length*130+50),Mathf.CeilToInt(role.skins.Length/5f)*136+44),"unfold_base");for(int i=0;i<role.skins.Length;i++){
                var s=role.skins[i];var r=new Rect(x+630+(i%5)*130,558+(i/5)*136,110,110);DrawCharacterCard(r,role,s.id,CharacterRoster.InPool(role.key,s.id),false,false);if(Hit(r,"random-skin-"+s.id)){CharacterRoster.TogglePool(role.key,s.id);if(CharacterRoster.Pool.Length>0)CharacterRoster.Random=true;client.PublishCharacterSelection();}
            }}
            CImage(new Rect(x+498,565,58,100),"decorate_arrow",ScaleMode.ScaleToFit);
        }
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
