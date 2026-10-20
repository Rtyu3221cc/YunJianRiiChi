using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi {
    public sealed partial class LobbyController {
        DormView outgoingDorm;
        float dormOpened=-10,pageBegan=-10,dormCloseBegan,sortBegan,sortCloseBegan,skinMotionBegan=-10;
        bool dormLeaving,pageMoving,sortClosing,skinDragging,skinWasDragged,zoomDragging;
        float skinPosition,skinFrom,skinPressPosition,skinPressX,sortPressedAt;
        int skinPressedCard=-1;
        string sortPressed;
        readonly NativeScroll sortScroller=new NativeScroll();bool sortRailDragging;float sortRailGrab;
        CharacterSkin previousIllustration;
        float previousSkinBegan=-10;
        Vector2 skinDescriptionScroll;
        Vector2 dormRoleScroll;readonly CharacterPaneDrag dormRoleDrag=new CharacterPaneDrag();
        readonly CharacterPaneDrag skinDescriptionDrag=new CharacterPaneDrag();
        MotionBank dormNativeMotion;
        bool homeIllustrationEditing;
        bool homeControlsHidden,homeRevealPointer;
        string homePressedControl;
        bool infoPointerPending,infoPointerDragging,infoRailDragging;
        Vector2 infoPointerStart;
        float infoScrollStart,infoRailOffset;
        LobbyIllustrationPose homeDraft;
        readonly LobbySakura sakura=new LobbySakura();
        [Serializable] class NativeCharacterFont {public string key,resource;public int size;}
        [Serializable] class NativeCharacterFonts {public NativeCharacterFont[] fonts;}
        NativeCharacterFonts nativeCharacterFonts;
        readonly Dictionary<string,Font> nativeCharacterFontCache=new Dictionary<string,Font>();
        GUIStyle NText(string key,int size,TextAnchor align=TextAnchor.MiddleCenter,bool wrap=false){
            if(nativeCharacterFonts==null)nativeCharacterFonts=JsonUtility.FromJson<NativeCharacterFonts>(Resources.Load<TextAsset>("Majsoul/Characters/native-fonts").text);
            var entry=nativeCharacterFonts.fonts.First(f=>f.key==key);string resource=entry.resource.EndsWith("/fengyucumaokai")?"Majsoul/Lobby/Fonts/brush":entry.resource;if(!nativeCharacterFontCache.TryGetValue(resource,out var font))nativeCharacterFontCache[resource]=font=Resources.Load<Font>(resource);return new GUIStyle(text){font=font,fontSize=size,alignment=align,wordWrap=wrap};
        }
        void VerticalCharacterLabel(Rect r,string value,int size,float pitch,Color color){
            var style=CText(size,TextAnchor.MiddleCenter,true);style.wordWrap=false;style.clipping=TextClipping.Overflow;
            float step=Mathf.Min(pitch,r.height/Mathf.Max(1,value.Length)),cell=size*2;
            for(int i=0;i<value.Length;i++)Label(new Rect(r.x,r.center.y+(i-(value.Length-1)*.5f)*step-cell/2,r.width,cell),value[i].ToString(),style,color);
        }
        float PageDuration=>.33333334f;
        void CharacterHeader(string title){ReturnHeader(title);}
        void TickCharacterPages(){
            if(dormLeaving&&Now-dormCloseBegan>=.25f){dormOpen=false;dormLeaving=false;}
            if(pageMoving&&Now-pageBegan>=PageDuration)pageMoving=false;
            if(sortClosing&&Now-sortCloseBegan>=.18f){sortOpen=false;sortClosing=false;}
        }
        float NativeDormSample(bool exit,uint path,uint attribute,float time,float fallback){
            if(dormNativeMotion==null)dormNativeMotion=JsonUtility.FromJson<MotionBank>(Resources.Load<TextAsset>("Majsoul/Characters/review-motion").text);
            var clip=dormNativeMotion.animations.First(c=>c.name==(exit?"ui_liaosheselect_out":"ui_liaosheselect_in"));var track=clip.tracks.FirstOrDefault(t=>t.path==path&&t.attribute==attribute);
            if(track==null||track.values.Length==0)return fallback;float f=Mathf.Clamp(time*60,0,track.values.Length-1);int i=(int)f;return Mathf.Lerp(track.values[i],track.values[Mathf.Min(i+1,track.values.Length-1)],f-i);
        }
        float DormOpacity=>dormLeaving?1-Mathf.SmoothStep(0,1,(Now-dormCloseBegan)/.25f):Mathf.Clamp01((Now-dormOpened)/.25f);
        void DrawDormBackground(){
            Image(new Rect(0,0,Screen.width,Screen.height),"yard",ScaleMode.ScaleAndCrop);var color=GUI.color;GUI.color=new Color(color.r,color.g,color.b,color.a*DormOpacity);Image(new Rect(0,0,Screen.width,Screen.height),"indoor",ScaleMode.ScaleAndCrop);GUI.color=color;
        }
        public void OpenDorm(){
            dormOpen=true;dormLeaving=false;homeIllustrationEditing=false;roleOpen=roleClosing=false;dormView=outgoingDorm=DormView.Select;sortOpen=sortClosing=false;infoScroll=Vector2.zero;dormOpened=pageBegan=Now;pageMoving=true;characterTransition=Now-.2f;expression=0;Cue("normalflyin");
        }
        public void OpenRoleSelection(){dormOpen=false;roleOpen=true;roleClosing=false;rolePreview=CharacterRoster.Active.character;skinExpanded=false;randomTab=false;ResetRolePanes();roleStarted=Now;Cue("btn_appear");}
        void CloseRoleSelection(){roleClosing=true;roleOpen=false;roleClosed=Now;Cue("btn_cancel");}
        void SetDormPage(DormView page){if(page==dormView)return;if(dormView==DormView.Skins&&page!=DormView.Skins&&BrowsingSkin.id!=CurrentOutfit.id){previousIllustration=BrowsingSkin;previousSkinBegan=Now;}outgoingDorm=dormView;dormView=page;pageBegan=Now;pageMoving=true;appreciationDragging=zoomDragging=skinDragging=false;infoPointerPending=infoPointerDragging=infoRailDragging=false;skinWasDragged=false;skinPressedCard=-1;Cue("btn_appear");}
        bool CharacterBack(){
            if(homeIllustrationEditing){CloseLobbyIllustrationEditor(false);return true;}
            if(sortOpen){CloseCharacterSort(false);return true;}
            if(roleOpen){CloseRoleSelection();return true;}if(roleClosing)return true;if(!dormOpen)return false;if(dormLeaving)return true;
            if(dormView==DormView.Appreciate)SetDormPage(DormView.Select);
            else if(dormView==DormView.Skins)SetDormPage(skinReturn);
            else if(dormView==DormView.Info)SetDormPage(DormView.Select);
            else{dormLeaving=true;dormCloseBegan=Now;EnterHome(true);}
            Cue("btn_cancel");return true;
        }
        void OpenSkins(DormView from){skinReturn=from;skinIndex=Array.FindIndex(CurrentCharacter.skins,s=>s.id==CurrentOutfit.id);skinPosition=skinFrom=skinIndex;skinMotionBegan=Now-1;skinDescriptionScroll=Vector2.zero;SetDormPage(DormView.Skins);}
        void SelectDorm(CharacterDefinition role){previousIllustration=CurrentOutfit;previousSkinBegan=Now;CharacterRoster.Dorm=role;expression=0;characterTransition=Now;infoScroll=Vector2.zero;PlayCharacterGreeting(role.key);}
        void PlayCharacterGreeting(string key){GetComponent<CharacterVoices>()?.Play(key,"lobby_normal1");}
        Rect DormPortrait(CharacterSkin skin){bool wide=skin.dynamic&&IllustrationAspect(skin)>1.1f;float h=wide?1100:1400,w=h*IllustrationAspect(skin);return new Rect(width*.315f-w/2,wide?-30:130,w,h);}
        Rect AppreciatePortrait(CharacterSkin skin){var r=DormPortrait(skin);r.width*=appreciationZoom;r.height*=appreciationZoom;r.x=width*.45f-r.width/2+appreciationPan.x;r.y=100+(1080-r.height)*.35f+appreciationPan.y;return r;}
        static Rect InterpolateRect(Rect a,Rect b,float p)=>new Rect(Mathf.Lerp(a.x,b.x,p),Mathf.Lerp(a.y,b.y,p),Mathf.Lerp(a.width,b.width,p),Mathf.Lerp(a.height,b.height,p));
        void DrawHomeCharacter(Rect original){
            var active=CharacterRoster.Active;var skin=CharacterRoster.Skin(active.character,active.skin);var pose=homeIllustrationEditing?homeDraft:LobbyIllustrationSettings.Get(skin.id);
            bool wide=skin.dynamic&&IllustrationAspect(skin)>1.1f;float h=(wide?1100:1400)*pose.scale,w=h*IllustrationAspect(skin);
            var r=new Rect(original.center.x-w/2+pose.x,(wide?-30:130)+pose.y,w,h);characterArt.Draw(r,skin,Now);
        }
        void DrawDorm(){
            var saved=GUI.color;GUI.color=new Color(saved.r,saved.g,saved.b,saved.a*DormOpacity);
            var skin=dormView==DormView.Skins?BrowsingSkin:CurrentOutfit;Rect r=DormPortrait(skin);float pageProgress=Mathf.SmoothStep(0,1,(Now-pageBegan)/PageDuration);
            if(dormView==DormView.Appreciate)r=InterpolateRect(r,AppreciatePortrait(skin),pageMoving?pageProgress:1);
            else if(pageMoving&&outgoingDorm==DormView.Appreciate)r=InterpolateRect(AppreciatePortrait(skin),r,pageProgress);
            float change=Mathf.Clamp01((Now-previousSkinBegan)/.18f);var c=GUI.color;
            if(previousIllustration!=null&&change<1&&previousIllustration.id!=skin.id){GUI.color=new Color(c.r,c.g,c.b,c.a*(1-change));characterArt.Draw(DormPortrait(previousIllustration),previousIllustration,Now);GUI.color=c;}
            GUI.color=new Color(c.r,c.g,c.b,c.a*(previousIllustration!=null&&change<1?change:1));characterArt.Draw(r,skin,Now);GUI.color=c;
            bool enabled=GUI.enabled;GUI.enabled&=!dormLeaving;
            if(pageMoving&&outgoingDorm!=dormView)DrawDormPageLayer(outgoingDorm,true);
            DrawDormPageLayer(dormView,false);
            bool showBadge=dormView==DormView.Select||dormView==DormView.Info||pageMoving&&(outgoingDorm==DormView.Select||outgoingDorm==DormView.Info);
            if(showBadge){float alpha=(dormView==DormView.Skins||dormView==DormView.Appreciate)?1-pageProgress:1;var old=GUI.color;GUI.color=new Color(old.r,old.g,old.b,old.a*alpha);DrawCharacterNameplate();GUI.color=old;}
            GUI.enabled=enabled;GUI.color=saved;
        }
        void DrawDormPageLayer(DormView view,bool exiting){
            float t=Now-pageBegan;if(exiting&&!pageMoving)return;float alpha=1,shift=0;
            if(exiting){alpha=view==DormView.Select?NativeDormSample(true,0,1574349066,t,0):1-Mathf.SmoothStep(0,1,t/.25f);shift=view==DormView.Select?NativeDormSample(true,3792757650,1460864421,t,-73)+73:70*(1-alpha);}
            else if(pageMoving){alpha=view==DormView.Select?NativeDormSample(false,0,1574349066,t,1):Mathf.SmoothStep(0,1,(t-.08f)/.25f);shift=view==DormView.Select?NativeDormSample(false,3792757650,1460864421,t,-73)+73:60*(1-alpha);}
            alpha=Mathf.Clamp01(alpha);if(alpha<=.001f)return;var matrix=GUI.matrix;var color=GUI.color;bool enabled=GUI.enabled;GUI.color=new Color(color.r,color.g,color.b,color.a*alpha);GUI.enabled&=!exiting&&(!pageMoving||t>=.20f);
            if(view==DormView.Appreciate)DrawAppreciation();
            else{
                CharacterHeader(view==DormView.Select?"宿舍":view==DormView.Info?"拜访":"更换服饰");
                GUI.matrix=matrix*Matrix4x4.Translate(new Vector3(shift,0,0));
                if(view==DormView.Select){DrawDormLeftButtons();DrawDormCards();}
                else if(view==DormView.Info)DrawCharacterInfo();else DrawSkinSelection();
            }
            GUI.matrix=matrix;GUI.color=color;GUI.enabled=enabled;
        }
        void DrawCharacterNameplate(){
            float x=width/2-881;CImage(new Rect(x,907,502,78),"sushe_name_bottom");CImage(new Rect(x+15,963,388,62),"sushe_cv_bottom");CImage(new Rect(x-64,874,150,144),"sushe_visit_flower");
            Label(new Rect(x+69,904.2f,282,75),CurrentCharacter.name,CText(CurrentCharacter.name.Length>5?45:50,TextAnchor.MiddleCenter,true),Color.white);
            Label(new Rect(x+69,974.5f,282,45),"CV："+CurrentCharacter.cv,CText(30,TextAnchor.MiddleCenter,true),Color.white);
            bool back=dormView==DormView.Info;int frame=back?16:1;
            if(pageMoving&&(outgoingDorm==DormView.Select&&dormView==DormView.Info||outgoingDorm==DormView.Info&&dormView==DormView.Select)){float q=Mathf.Clamp01((Now-pageBegan)/.25f);frame=back?1+Mathf.FloorToInt(q*15):16-Mathf.FloorToInt(q*15);}
            CImage(new Rect(x-58.5f,873.5f,145,145),"btn_anim_2_"+Mathf.Clamp(frame,1,16));
            if(Hit(new Rect(x-64,866,150,160),"character-info")){if(dormView==DormView.Info)SetDormPage(DormView.Select);else if(dormView==DormView.Select){infoScroll=Vector2.zero;SetDormPage(DormView.Info);}}
        }
        void DrawDormLeftButtons(){
            float x=Mathf.Max(220,width*.12f);CImage(new Rect(x,238,88,88),"sushe_cloth_button");if(Hit(new Rect(x-21,217,130,130),"character-skins"))OpenSkins(DormView.Select);
            CImage(new Rect(x,369,88,88),"sushe_enlarge_button");if(Hit(new Rect(x-21,348,130,130),"character-appreciate")){appreciationZoom=.65f;appreciationPan=Vector2.zero;SetDormPage(DormView.Appreciate);}
            if(CurrentOutfit.dynamic){CImage(new Rect(x,499,88,88),characterArt.Animate?"sushe_live_on":"sushe_live_off");if(Hit(new Rect(x-21,478,130,130),"character-live")){characterArt.Animate=!characterArt.Animate;Cue("btn_click");}}
            if(Hit(new Rect(x+150,180,Mathf.Max(250,width-x-1200),650),"character-touch")){characterArt.Play(CurrentOutfit,"click");GetComponent<CharacterVoices>()?.Play(CurrentCharacter.key,"lobby_normal"+UnityEngine.Random.Range(1,6));}
        }
        float IllustrationAspect(CharacterSkin skin){var t=CharacterRoster.Texture("Skins/"+skin.id+"/full");return t==null?.7f:(float)t.width/t.height;}
        void DrawDormCards(){
            float x=width-1016;var panel=new Rect(x,146.8f,766,892);Tint(new Rect(x+17.5f,164.3f,731,857),new Color(.008f,.006f,.02f,.72f));CSlice(panel,"bg_bound");
            CImage(new Rect(x+437,66.8f,284,84),"tab_gray3");CImage(new Rect(x+191.53f,66.766f,284,84),"tab_bright2");Label(new Rect(x+256.03f,86.676f,180,63.98f),"角色",CText(42,TextAnchor.MiddleCenter,true),new Color(.2f,.1f,.04f));Label(new Rect(x+501.5f,86.676f,180,63.98f),"装扮",CText(42,TextAnchor.MiddleCenter,true),CharacterGold);
            var roles=CharacterRoster.Ordered(dormStars);var cardView=new Rect(x+30,190,700,720);DragCharacterPane(dormRoleDrag,cardView,new Vector2(700,Mathf.CeilToInt(roles.Length/3f)*198+30),ref dormRoleScroll,false);GUI.BeginGroup(cardView);var previousOffset=targetOffset;targetOffset+=cardView.position;for(int i=0;i<roles.Length;i++){
                // The opaque sorting panel covers these portraits after its
                // transition. Avoid keeping hidden cards alive in the cache.
                if(sortOpen&&!sortClosing&&Now-sortBegan>=.18f)continue;
                var r=new Rect(16.05f+i%3*220,18.3f+i/3*198-dormRoleScroll.y,204,192);if(r.yMax<0||r.y>cardView.height)continue;var role=roles[i];bool available=CharacterRoster.Available(role.key),selected=available&&role.key==CurrentCharacter.key;DrawCharacterCard(r,role,CharacterRoster.Outfit(role.key),selected,true);
                if(!available){Tint(r,new Color(0,0,0,.66f));Label(new Rect(r.x+45,r.y+140,140,42),"未下载",CText(25,TextAnchor.MiddleCenter,true),new Color(.85f,.85f,.85f));if(Hit(r,"dorm-character-"+role.key))CharacterResourceUi.Current.Ask(role.key);continue;}
                var star=new Rect(r.x+.15f,r.y+147,40,40);if((selected||CharacterRoster.Favorite(role.key))&&Hit(star,"dorm-star-"+role.key)){CharacterRoster.Star(role.key);continue;}if(Hit(r,"dorm-character-"+role.key))SelectDorm(role);
            }
            targetOffset=previousOffset;GUI.EndGroup();
            if(roles.Length==0)Label(new Rect(x+40,450,670,120),"还没有星标雀士",CText(32),CharacterGold);
            CImage(new Rect(x+472,934.3f,74,74),"btn_sort");if(Hit(new Rect(x+465,928,88,88),"character-sort"))OpenCharacterSort();DrawStarFilter(new Rect(x+551,943.8f,150,60),dormStars,"dorm-favorites",()=>dormStars=!dormStars);
        }
        void DrawCharacterCard(Rect r,CharacterDefinition role,int skin,bool selected,bool contract,bool star=true){
            if(contract){float u=r.width/204;CImage(new Rect(r.x+19.45f*u,r.y+8.5f*u,174*u,174*u),"sushe_favor_bg");var head=CharacterRoster.Texture("Skins/"+skin+"/bighead")??CharacterPackService.Thumbnail(role.key);if(head!=null)GUI.DrawTexture(new Rect(r.x+19.45f*u,r.y+8.5f*u,174*u,174*u),head,ScaleMode.ScaleToFit,true);CImage(r,"sushe_card_favor_outline");VerticalCharacterLabel(new Rect(r.x+2.45f*u,r.y+19.5f*u,35*u,128*u),role.name,Mathf.RoundToInt((role.name.Length>5?21:22)*u),24*u,CharacterGold);
                if(selected){var color=GUI.color;GUI.color=new Color(color.r,color.g,color.b,color.a*(.88f+.12f*Mathf.Sin(Now*4)));CImage(new Rect(r.x-16*u,r.y-10*u,228*u,214*u),"dorm-contour-glow");GUI.color=color;}
                if(star&&(selected||CharacterRoster.Favorite(role.key)))CImage(new Rect(r.x+.15f*u,r.y+147*u,40*u,40*u),CharacterRoster.Favorite(role.key)?"sushe_card_favor_star_light":"sushe_card_favor_star_dark");return;
            }
            CImage(r,"kuangbg");var picture=CharacterRoster.Texture("Skins/"+skin+"/bighead");if(picture!=null)GUI.DrawTexture(new Rect(r.x+15,r.y+3,r.width-18,r.height-6),picture,ScaleMode.ScaleToFit,true);CImage(r,"kuangbound");if(selected)CSlice(new Rect(r.x-6,r.y-6,r.width+12,r.height+12),"select_box_blue");if(star&&CharacterRoster.Favorite(role.key))CImage(new Rect(r.x+5,r.yMax-40,37,37),"sort_favor_on",ScaleMode.ScaleToFit);
        }
        void DrawStarFilter(Rect r,bool on,string id,Action toggle){CImage(r,"star_slider");CImage(new Rect(on?r.x+r.width*.51f:r.x,r.y,r.width*.49f,r.height),"star_btn");if(Hit(r,id))toggle();}
        void DrawCharacterInfo(){
            float x=width-1080;var frame=new Rect(x,146.5f,840,914);var backing=new Rect(x+50,150.5f,740,888);CImage(backing,"gift_bbg");CSlice(frame,"bg_frame");
            CImage(new Rect(x-.4f,159.5f,11,514),"bf_lampline");
            foreach(int i in new[]{0,1}){var lamp=new Rect(x-55.9f,183.5f+i*140,121,164);CImage(lamp,i==0?"bf_chosen":"bf_unchooesd");VerticalCharacterLabel(new Rect(lamp.x+32,lamp.y+8.2f,55,147.6f),i==0?"档案":"语音",42,44,new Color(.12f,.09f,.08f));if(i==0)Hit(lamp,"profile-lantern");}
            CSlice(new Rect(x+63,173.4f,715,151),"gift_heart_bg");CImage(new Rect(x+99,230.9f,94,36),"heart_deco");FlipImage(new Rect(x+645.4f,230.9f,94,36),"heart_deco",true,false);
            CImage(new Rect(x+198.8f,197.5f,440,108),"heart_normal");for(int i=0;i<5;i++){var heart=new Rect(x+219+i*80.5f,219,77,67);CImage(heart,"bf_heart_bg");CImage(heart,"bf_heart");}
            var c=CurrentCharacter;var viewport=new Rect(x+84,381,644,626);float introHeight=NText("profile-body",26,TextAnchor.UpperLeft,true).CalcHeight(new GUIContent(c.description),578);float emojiTop=325+introHeight+35,contentHeight=emojiTop+85+Mathf.CeilToInt(c.emojis.Length/4f)*170;
            ProfileScrollPointer(viewport,contentHeight);
            infoScroll=GUI.BeginScrollView(viewport,infoScroll,new Rect(0,0,625,contentHeight),false,false,GUIStyle.none,GUIStyle.none);
            CImage(new Rect(0,0,294,51),"bf_title_bg");Label(new Rect(19.1f,-.4f,240,48),"人物介绍",NText("profile-heading",34,TextAnchor.MiddleLeft),CharacterGold);
            string[] keys={"身高：","生日：","年龄：","血型：","声优：","爱好："},values={c.height,c.birth,c.age,c.blood,c.cv,c.hobby};
            for(int i=0;i<6;i++){Label(new Rect(4,70.5f+i*36,86,32),keys[i],NText("profile-label",26,TextAnchor.MiddleLeft),CharacterGold);Label(new Rect(96,74.8f+i*36,300,32),values[i],NText("profile-body",26,TextAnchor.MiddleLeft),new Color(.85f,.85f,.9f));}
            var portrait=new Rect(405.3f,76,204,204);CImage(portrait,"kuangbg");var head=CharacterRoster.Texture("Skins/"+CurrentOutfit.id+"/bighead");if(head!=null)GUI.DrawTexture(new Rect(portrait.x+3,portrait.y+3,198,198),head,ScaleMode.ScaleToFit);CImage(portrait,"kuangbound");CImage(new Rect(portrait.x+140,portrait.y+139,75,75),"bf_skin");if(Hit(portrait,"info-skins"))OpenSkins(DormView.Info);
            Label(new Rect(4,286,100,32),"简介：",NText("profile-label",26,TextAnchor.MiddleLeft),CharacterGold);Label(new Rect(35,325,578,introHeight),c.description,NText("profile-body",26,TextAnchor.UpperLeft,true),new Color(.85f,.85f,.9f));
            CImage(new Rect(0,emojiTop,294,51),"bf_title_bg");Label(new Rect(19.8f,emojiTop-1.5f,240,48),"聊天表情",NText("profile-heading",34,TextAnchor.MiddleLeft),CharacterGold);
            for(int i=0;i<c.emojis.Length;i++){var r=new Rect(i%4*155,emojiTop+84+i/4*170,150,150);var t=CharacterRoster.Texture(c.emojis[i]);if(t!=null)GUI.DrawTexture(r,t,ScaleMode.ScaleToFit);CSlice(r,"bound_emo");if(Hit(r,"character-expression-"+i))expression=i;}
            GUI.EndScrollView();DrawInfoScrollbar(viewport,contentHeight);
        }
        void DrawInfoScrollbar(Rect r,float total){var rail=new Rect(r.xMax+19,383.5f,6.4f,622);CImage(rail,"ss_slidebar");float p=total<=r.height?0:Mathf.Clamp01(infoScroll.y/(total-r.height));CImage(new Rect(rail.center.x-17,rail.y+p*(rail.height-34),34,34),"ss_slidebtn");}
        bool CharacterPointer(out EventType type,out Vector2 point,out Vector2 delta){
            type=Event.current.type;point=Event.current.mousePosition;delta=Event.current.delta;
#if UNITY_EDITOR
            if(type==EventType.Repaint&&reviewGuiPointer.Count>0){var queued=reviewGuiPointer.Dequeue();type=queued.type;point=GUI.matrix.inverse.MultiplyPoint3x4(new Vector3(queued.point.x,queued.point.y,0));delta=queued.delta/Mathf.Max(.001f,GUI.matrix.m00);return true;}
#endif
            return false;
        }
        void ProfileScrollPointer(Rect view,float total){
            if(!GUI.enabled)return;bool injected=CharacterPointer(out var type,out var point,out var delta);int control=GUIUtility.GetControlID("character-profile-scroll".GetHashCode(),FocusType.Passive);
            bool consumed=ProfilePointer(type,point,delta,view,total);
            if(!injected&&consumed){GUIUtility.hotControl=type==EventType.MouseUp?0:control;Event.current.Use();}
        }
        bool ProfilePointer(EventType type,Vector2 point,Vector2 delta,Rect view,float total){
            float max=Mathf.Max(0,total-view.height);var rail=new Rect(view.xMax-2,383.5f,48,622);bool consumed=false;
            if(type==EventType.MouseDown){
                if(rail.Contains(point)){infoRailDragging=true;float y=rail.y+(rail.height-34)*(max>0?infoScroll.y/max:0);infoRailOffset=Mathf.Abs(point.y-(y+17))<25?point.y-y:17;consumed=true;}
                else if(view.Contains(point)){infoPointerPending=true;infoPointerDragging=false;infoPointerStart=point;infoScrollStart=infoScroll.y;}
            }
            if(infoRailDragging&&(type==EventType.MouseDown||type==EventType.MouseDrag)){infoScroll.y=max*Mathf.Clamp01((point.y-rail.y-infoRailOffset)/(rail.height-34));consumed=true;}
            if(type==EventType.MouseDrag&&infoPointerPending&&(infoPointerDragging||Mathf.Abs(point.y-infoPointerStart.y)>10)){infoPointerDragging=true;infoScroll.y=Mathf.Clamp(infoScrollStart+infoPointerStart.y-point.y,0,max);consumed=true;}
            if(type==EventType.MouseUp){consumed=infoPointerDragging||infoRailDragging;infoPointerPending=infoPointerDragging=infoRailDragging=false;}
            if(type==EventType.ScrollWheel&&view.Contains(point)){infoScroll.y=Mathf.Clamp(infoScroll.y+delta.y*32,0,max);consumed=true;}
            infoScroll.y=Mathf.Clamp(infoScroll.y,0,max);return consumed;
        }
        void SetSkinIndex(int next){next=Mathf.Clamp(next,0,CurrentCharacter.skins.Length-1);if(next==skinIndex)return;previousIllustration=BrowsingSkin;previousSkinBegan=Now;skinFrom=SkinPosition();skinIndex=next;skinMotionBegan=Now;skinDescriptionScroll=Vector2.zero;Cue("btn_click");}
        float SkinPosition(){if(skinDragging)return skinPosition;return Mathf.Lerp(skinFrom,skinIndex,Mathf.SmoothStep(0,1,(Now-skinMotionBegan)/.25f));}
        Rect SkinCardRect(int index,float position){float d=index-position,f=Mathf.Max(.60f,1-Mathf.Abs(d)*.12f);return new Rect(width-780+d*130,128+(1-f)*160,228*f,476*f);}
        bool SkinCarouselPointer(EventType type,Vector2 point){
            var region=new Rect(width-1040,100,830,530);
            if(type==EventType.MouseDown&&region.Contains(point)){skinPosition=SkinPosition();skinPressPosition=skinPosition;skinPressX=point.x;skinDragging=true;skinWasDragged=false;skinPressedCard=-1;foreach(int i in Enumerable.Range(0,CurrentCharacter.skins.Length).OrderByDescending(i=>Mathf.Abs(i-skinPosition)))if(SkinCardRect(i,skinPosition).Contains(point))skinPressedCard=i;return true;}
            if(type==EventType.MouseDrag&&skinDragging){skinWasDragged|=Mathf.Abs(point.x-skinPressX)>10;skinPosition=Mathf.Clamp(skinPressPosition-(point.x-skinPressX)/160,0,CurrentCharacter.skins.Length-1);return skinWasDragged;}
            if(type==EventType.MouseUp&&skinDragging){bool moved=skinWasDragged;float value=skinPosition;skinDragging=false;if(moved){SetSkinIndex(Mathf.RoundToInt(value));skinFrom=value;skinMotionBegan=Now;}else if(skinPressedCard>=0&&region.Contains(point))SetSkinIndex(skinPressedCard);skinPressedCard=-1;return true;}return false;
        }
        void DrawSkinSelection(){
            DrawSkinPageMask();var ev=Event.current;if(SkinCarouselPointer(ev.type,ev.mousePosition))ev.Use();float position=SkinPosition();var c=CurrentCharacter;
            var clip=new Rect(width-1050,100,850,530);GUI.BeginGroup(clip);var targetBefore=targetOffset;targetOffset+=clip.position;
            foreach(int i in Enumerable.Range(0,c.skins.Length).OrderByDescending(i=>Mathf.Abs(i-position))){
                float d=i-position,f=Mathf.Max(.60f,1-Mathf.Abs(d)*.12f);var s=c.skins[i];var r=new Rect(width-780+d*130-clip.x,128+(1-f)*160-clip.y,228*f,476*f);if(r.xMax<0||r.x>clip.width)continue;
                var color=GUI.color;GUI.color=new Color(color.r,color.g,color.b,color.a*Mathf.Lerp(1,.50f,Mathf.Clamp01(Mathf.Abs(d)/3)));Tint(r,new Color(.035f,.05f,.13f,.96f));var im=CharacterRoster.Texture("Skins/"+s.id+"/full");
                if(im!=null)CharacterFraming.DrawSkinCard(r,s.id);CSlice(r,"skin_bound");
                bool front=Mathf.Abs(d)<.05f;if(front)CharacterCorners(r);
                if(s.id==CurrentOutfit.id)CImage(front?new Rect(r.x+88,r.yMax-100,156,119):new Rect(r.x+r.width*.60f,r.yMax-69*f,67*f,69*f),front?"using":"skin_using");
                if(s.dynamic)CImage(new Rect(r.x+10*f,r.y+10*f,74*f,36*f),"spine_tip");GUI.color=color;
                Hit(r,"skin-card-"+i,false);
            }
            targetOffset=targetBefore;GUI.EndGroup();
            float x=width-920;Label(new Rect(x,640,329,60),BrowsingSkin.name,NText("skin-title",46,TextAnchor.MiddleLeft),CharacterGold);Label(new Rect(x+329.5f,655.5f,245,45),c.name,CText(30,TextAnchor.MiddleRight,true),new Color(.84f,.78f,.64f));CImage(new Rect(x+6,702,566,22),"dividing_line");
            var style=NText("skin-body",30,TextAnchor.UpperLeft,true);float h=style.CalcHeight(new GUIContent(BrowsingSkin.description),575);var descriptionView=new Rect(x,735.5f,600,181);DragCharacterPane(skinDescriptionDrag,descriptionView,new Vector2(575,Mathf.Max(181,h)),ref skinDescriptionScroll,false);skinDescriptionScroll=GUI.BeginScrollView(descriptionView,skinDescriptionScroll,new Rect(0,0,575,Mathf.Max(181,h)),false,false,GUIStyle.none,GUIStyle.none);Label(new Rect(0,0,575,h),BrowsingSkin.description,style,new Color(.88f,.87f,.89f));GUI.EndScrollView();
            var button=new Rect(width-787.6f,931.3f,313,93);bool equipped=BrowsingSkin.id==CurrentOutfit.id;NativeUiChrome.CharacterButton(button,equipped);Label(new Rect(button.x+35,button.y+15,243,63),"选择服饰",NText("skin-title",42),equipped?new Color(.62f,.61f,.61f):CharacterGold);if(Hit(button,"equip-skin",!equipped)){CharacterRoster.Equip(c.key,BrowsingSkin.id);Cue("btn_click");}
        }
        void DrawSkinPageMask(){var r=new Rect(width-1080,0,864,1080);CImage(r,"skin_page_black_bg");var t=CharacterRoster.Texture("UI/skin_page_black_bg");if(t!=null)GUI.DrawTextureWithTexCoords(new Rect(r.xMax,0,Mathf.Max(0,width+bleedLeft-r.xMax+30),1080),t,new Rect(863f/864,0,1f/864,1));}
        void FlipImage(Rect r,string key,bool x,bool y){var t=CharacterRoster.Texture("UI/"+key);if(t!=null)GUI.DrawTextureWithTexCoords(r,t,new Rect(x?1:0,y?1:0,x?-1:1,y?-1:1));}
        void CharacterCorners(Rect r){CImage(new Rect(r.x-15,r.y-15,44,44),"select_corner");FlipImage(new Rect(r.xMax-29,r.y-15,44,44),"select_corner",true,false);FlipImage(new Rect(r.x-15,r.yMax-29,44,44),"select_corner",false,true);FlipImage(new Rect(r.xMax-29,r.yMax-29,44,44),"select_corner",true,true);}
        bool ZoomPointer(EventType type,Vector2 point,ref float value){var rail=new Rect(width-344,199.99f,88,581.643f);if(type==EventType.MouseDown&&rail.Contains(point))zoomDragging=true;if(zoomDragging&&(type==EventType.MouseDown||type==EventType.MouseDrag)){value=Mathf.Lerp(2.3f,.35f,Mathf.Clamp01((point.y-rail.y)/rail.height));return true;}if(type==EventType.MouseUp&&zoomDragging){zoomDragging=false;return true;}return false;}
        void DrawNativeZoomControls(ref float zoom,bool photo=false){
            var ev=Event.current;if(ZoomPointer(ev.type,ev.mousePosition,ref zoom))ev.Use();float x=width-341.23f;CSlice(new Rect(x,199.99f,64,581.643f),photo?"Photo/slide_scale_bottom":"bg");CImage(new Rect(x-12,158.49f,88,50),photo?"Photo/MAX":"MAX");CImage(new Rect(x-12,773.134f,88,50),photo?"Photo/MIN":"MIN");float y=Mathf.Lerp(199.99f,781.633f,Mathf.InverseLerp(2.3f,.35f,zoom));CImage(new Rect(x, y-32,64,64),photo?"Photo/slide_scale_handle":"handle");
        }
        void MoveIllustration(ref Vector2 pan,ref float zoom){
            var ev=Event.current;var area=new Rect(0,130,width-390,homeIllustrationEditing?780:900);if(ev.type==EventType.MouseDown&&area.Contains(ev.mousePosition)&&Input.touchCount<2){appreciationDragging=true;pointerStart=ev.mousePosition;panStart=pan;ev.Use();}
            if(ev.type==EventType.MouseDrag&&appreciationDragging&&Input.touchCount<2){pan=panStart+ev.mousePosition-pointerStart;ev.Use();}if(ev.type==EventType.MouseUp)appreciationDragging=false;
            if(ev.type==EventType.ScrollWheel&&area.Contains(ev.mousePosition)){zoom=Mathf.Clamp(zoom-ev.delta.y*.08f,.35f,2.3f);ev.Use();}
            if(Input.touchCount==2){appreciationDragging=false;float d=Vector2.Distance(Input.GetTouch(0).position,Input.GetTouch(1).position);if(pinchStart>0)zoom=Mathf.Clamp(zoom*d/pinchStart,.35f,2.3f);pinchStart=d;}else pinchStart=0;
        }
        void DrawAppreciation(){MoveIllustration(ref appreciationPan,ref appreciationZoom);DrawNativeZoomControls(ref appreciationZoom);CImage(new Rect(width-292,31,61.5f,63),"bf_close");if(Hit(new Rect(width-305,15,100,100),"appreciation-close"))CharacterBack();}
        void OpenCharacterSort(){sortOpen=true;sortClosing=false;sortBegan=Now;sortOrder=CharacterRoster.Ordered(false,true).Select(r=>r.key).ToArray();sortStars.Clear();foreach(var r in CharacterRoster.All)if(CharacterRoster.Favorite(r.key))sortStars.Add(r.key);sortDrag=sortPressed=null;sortScroller.Offset=0;sortRailDragging=false;Cue("btn_appear");}
        void CloseCharacterSort(bool save){if(sortClosing)return;if(save){foreach(var role in CharacterRoster.All)if(CharacterRoster.Favorite(role.key)!=sortStars.Contains(role.key))CharacterRoster.Star(role.key);CharacterRoster.Reorder(sortOrder);}sortClosing=true;sortCloseBegan=Now;Cue("btn_cancel");}
        Rect SortViewport=>new Rect(width/2-820,80,1640,784);
        int SortStarCount=>sortOrder.Count(k=>sortStars.Contains(k));
        int SortStarRows=>Mathf.Max(1,Mathf.CeilToInt(SortStarCount/8f));
        float SortOtherHeading=>354+(SortStarRows-1)*184;
        float SortContentHeight=>SortOtherHeading+81+(Mathf.Max(1,Mathf.CeilToInt((sortOrder.Length-SortStarCount)/8f))-1)*184+184-SortViewport.y;
        Rect SortCardRect(bool starred,int index)=>new Rect(width/2-724+index%8*184,(starred?170:SortOtherHeading+81)+index/8*184-sortScroller.Offset,160,160);
        bool SortPointer(EventType type,Vector2 point){
            if(!sortOpen||sortClosing||type!=EventType.MouseDown&&type!=EventType.MouseDrag&&type!=EventType.MouseUp)return false;sortPointer=point;if(!SortViewport.Contains(point)&&sortPressed==null&&sortDrag==null)return false;
            foreach(bool starred in new[]{true,false}){var keys=sortOrder.Where(k=>sortStars.Contains(k)==starred).ToArray();for(int i=0;i<keys.Length;i++){var r=SortCardRect(starred,i);var star=new Rect(r.x+2,r.yMax-38,38,38);if(type==EventType.MouseDown&&r.Contains(point)&&!star.Contains(point)){sortPressed=keys[i];sortPressedAt=Now;pointerStart=point;sortDragOffset=point-r.position;return true;}if(type==EventType.MouseUp&&sortDrag!=null&&r.Contains(point)){if(sortDrag!=keys[i]){var list=sortOrder.ToList();list.Remove(sortDrag);list.Insert(Math.Max(0,list.IndexOf(keys[i])),sortDrag);sortOrder=list.ToArray();}sortDrag=sortPressed=null;return true;}}}
            if(type==EventType.MouseDrag&&sortPressed!=null&&(Now-sortPressedAt>.18f||Vector2.Distance(point,pointerStart)>12))sortDrag=sortPressed;
            if(type==EventType.MouseUp){bool was=sortPressed!=null;sortPressed=sortDrag=null;return was;}return sortDrag!=null;
        }
        void DrawSortPortrait(Rect r,string key,bool dragging){var role=CharacterRoster.Find(key);CImage(new Rect(r.x+5,r.y+5,r.width-10,r.height-10),"kuangbg");var t=CharacterRoster.Texture("Skins/"+CharacterRoster.Outfit(key)+"/bighead")??CharacterPackService.Thumbnail(key);if(t!=null)GUI.DrawTexture(new Rect(r.x+4,r.y+4,r.width-8,r.height-8),t,ScaleMode.ScaleToFit);CImage(r,"kuangbound");if(dragging)CSlice(r,"select_box_blue");}
        void DrawCharacterSort(){
            float alpha=sortClosing?1-Mathf.SmoothStep(0,1,(Now-sortCloseBegan)/.18f):Mathf.SmoothStep(0,1,(Now-sortBegan)/.18f);var color=GUI.color;bool enabled=GUI.enabled;GUI.color=new Color(color.r,color.g,color.b,color.a*alpha);GUI.enabled&=!sortClosing;Tint(new Rect(bleedLeft,0,bleedWidth,1080),new Color(0,0,0,.30f));CSlice(new Rect(bleedLeft,57,bleedWidth,966),"sort_bg");
            var ev=Event.current;var view=SortViewport;float content=SortContentHeight,max=Mathf.Max(0,content-view.height);
            var rail=new Rect(view.xMax-20,view.y,42,view.height);float thumb=Mathf.Min(view.height,view.height*view.height/content);
            if(ev.type==EventType.MouseDown&&max>0&&rail.Contains(ev.mousePosition)){sortRailDragging=true;float top=view.y+(view.height-thumb)*Mathf.Clamp01(sortScroller.Offset/max);sortRailGrab=ev.mousePosition.y>=top&&ev.mousePosition.y<=top+thumb?ev.mousePosition.y-top:thumb/2;}
            if(sortRailDragging){if(ev.type==EventType.MouseDown||ev.type==EventType.MouseDrag)sortScroller.Offset=Mathf.Clamp01((ev.mousePosition.y-view.y-sortRailGrab)/Mathf.Max(1,view.height-thumb))*max;if(ev.type==EventType.MouseUp)sortRailDragging=false;if(ev.type==EventType.MouseDown||ev.type==EventType.MouseDrag||ev.type==EventType.MouseUp)ev.Use();}
            else if(SortPointer(ev.type,ev.mousePosition))ev.Use();
            if(sortPressed==null&&sortDrag==null&&!sortRailDragging)sortScroller.Input(view,content);
            if(sortDrag!=null&&ev.type==EventType.Repaint){float direction=sortPointer.y<view.y+45?-1:sortPointer.y>view.yMax-45?1:0;sortScroller.Offset=Mathf.Clamp(sortScroller.Offset+direction*500*Mathf.Min(.05f,Time.unscaledDeltaTime),0,max);}
            GUI.BeginGroup(view);var savedOffset=targetOffset;targetOffset+=view.position;
            foreach(bool starred in new[]{true,false}){
                float y=(starred?89:SortOtherHeading)-sortScroller.Offset-view.y;string heading=starred?"雀士排序 · 星标":"雀士排序 · 非星标";
                Label(new Rect(width/2-205-view.x,y,410,60),heading,NText("sort",39),CharacterGold);CImage(new Rect(width/2-430-view.x,y+23,184,22),"sort_deco_left");CImage(new Rect(width/2+246-view.x,y+23,184,22),"sort_deco_right");
                var keys=sortOrder.Where(k=>sortStars.Contains(k)==starred).ToArray();
                for(int i=0;i<keys.Length;i++){var global=SortCardRect(starred,i);if(global.yMax<view.y||global.y>view.yMax)continue;var r=new Rect(global.position-view.position,global.size);DrawSortPortrait(r,keys[i],sortDrag==keys[i]);var sr=new Rect(r.x+2,r.yMax-38,38,38);CImage(sr,starred?"sort_favor_on":"sort_favor_off");if(Hit(sr,"sort-star-"+keys[i])){if(!sortStars.Add(keys[i]))sortStars.Remove(keys[i]);}}
            }
            targetOffset=savedOffset;GUI.EndGroup();NativePanel.Scrollbar(view.xMax-12,view.y+4,view.height-8,sortScroller.Offset,content);
            if(sortDrag!=null)DrawSortPortrait(new Rect(sortPointer-sortDragOffset,new Vector2(160,160)),sortDrag,true);
            var button=new Rect(width/2-207.5f,892.4f,415,93);NativeUiChrome.Button(button,true);Label(new Rect(button.x+35,button.y+15,345,63),"确认",CText(42,TextAnchor.MiddleCenter,true),new Color(.16f,.08f,.04f));if(Hit(button,"sort-save"))CloseCharacterSort(true);
            CImage(new Rect(width-127,101,69.7f,71.4f),"bf_close");if(Hit(new Rect(width-155,66,130,130),"sort-cancel"))CloseCharacterSort(false);Label(new Rect(width/2-800,1019,1600,45),"上下滑动浏览，长按拖动头像排序，点击星标切换",NText("sort-help",26),new Color(.75f,.75f,.8f));GUI.color=color;GUI.enabled=enabled;
        }
        Rect PhotoReset=>new Rect(width/2-340,930,300,117.26f);
        Rect PhotoSave=>new Rect(width/2+40,930,300,117.26f);
        Rect PhotoHide=>new Rect(width-228,448.3f,107,85);
        Rect PhotoClose=>new Rect(width-305,15,100,100);
        string PhotoControl(Vector2 point)=>PhotoReset.Contains(point)?"reset":PhotoSave.Contains(point)?"save":PhotoHide.Contains(point)?"hide":PhotoClose.Contains(point)?"close":null;
        void PhotoAction(string control){
            if(control=="reset"){homeDraft=LobbyIllustrationPose.Default;appreciationDragging=zoomDragging=false;Cue("btn_click");}
            else if(control=="save")CloseLobbyIllustrationEditor(true);
            else if(control=="close")CloseLobbyIllustrationEditor(false);
            else if(control=="hide"){homeControlsHidden=true;appreciationDragging=zoomDragging=false;Cue("btn_click");}
        }
        bool PhotoPointer(EventType type,Vector2 point,Vector2 delta){
            if(homeControlsHidden){if(type==EventType.MouseDown){homeRevealPointer=true;return true;}if(type==EventType.MouseUp&&homeRevealPointer){homeControlsHidden=homeRevealPointer=false;return true;}return type==EventType.MouseDrag;}
            if(type==EventType.MouseDown){homePressedControl=PhotoControl(point);if(homePressedControl!=null)return true;}
            if(homePressedControl!=null){if(type==EventType.MouseUp){var pressed=homePressedControl;homePressedControl=null;if(PhotoControl(point)==pressed)PhotoAction(pressed);}return type==EventType.MouseDrag||type==EventType.MouseUp;}
            if(ZoomPointer(type,point,ref homeDraft.scale))return true;
            var area=new Rect(0,130,width-390,780);
            if(type==EventType.MouseDown&&area.Contains(point)&&Input.touchCount<2){appreciationDragging=true;pointerStart=point;panStart=new Vector2(homeDraft.x,homeDraft.y);return true;}
            if(type==EventType.MouseDrag&&appreciationDragging&&Input.touchCount<2){var pan=panStart+point-pointerStart;homeDraft.x=pan.x;homeDraft.y=pan.y;return true;}
            if(type==EventType.MouseUp&&appreciationDragging){appreciationDragging=false;return true;}
            if(type==EventType.ScrollWheel&&area.Contains(point)){homeDraft.scale=Mathf.Clamp(homeDraft.scale-delta.y*.08f,.35f,2.3f);return true;}return false;
        }
        void OpenLobbyIllustrationEditor(){homeIllustrationEditing=true;homeControlsHidden=homeRevealPointer=false;homePressedControl=null;var active=CharacterRoster.Active;homeDraft=LobbyIllustrationSettings.Get(active.skin);appreciationDragging=zoomDragging=false;Cue("btn_appear");}
        void CloseLobbyIllustrationEditor(bool save){if(save)LobbyIllustrationSettings.Save(CharacterRoster.Active.skin,homeDraft);homeIllustrationEditing=homeControlsHidden=homeRevealPointer=false;homePressedControl=null;appreciationDragging=zoomDragging=false;Cue("btn_cancel");}
        void DrawLobbyIllustrationEditor(){
            if(GUI.enabled){bool injected=CharacterPointer(out var type,out var point,out var delta);if(PhotoPointer(type,point,delta)&&!injected)Event.current.Use();}
            if(homeControlsHidden||!homeIllustrationEditing)return;
            DrawNativeZoomControls(ref homeDraft.scale,true);
            CImage(new Rect(width-305,15,84,84),"Photo/btn_close");if(Hit(PhotoClose,"lobby-illustration-cancel"))PhotoAction("close");
            CImage(PhotoReset,"Photo/btn_reset");if(Hit(PhotoReset,"lobby-illustration-reset"))PhotoAction("reset");
            CImage(PhotoSave,"Photo/btn_save");if(Hit(PhotoSave,"lobby-illustration-save"))PhotoAction("save");
            CImage(PhotoHide,"Photo/btn_hide");if(Hit(PhotoHide,"lobby-illustration-hide"))PhotoAction("hide");
            if(Event.current.type==EventType.Repaint&&Input.touchCount==2){appreciationDragging=false;float distance=Vector2.Distance(Input.GetTouch(0).position,Input.GetTouch(1).position);if(pinchStart>0)homeDraft.scale=Mathf.Clamp(homeDraft.scale*distance/pinchStart,.35f,2.3f);pinchStart=distance;}else if(Input.touchCount!=2)pinchStart=0;
        }
#if UNITY_EDITOR
        sealed class ReviewPointer {public EventType type;public Vector2 point,delta;}
        readonly Queue<ReviewPointer> reviewGuiPointer=new Queue<ReviewPointer>();
        public void ReviewQueueGuiPointer(EventType type,Vector2 point,Vector2 delta){reviewGuiPointer.Enqueue(new ReviewPointer{type=type,point=point,delta=delta});}
        public void ReviewSettleDorm(){dormOpened=pageBegan=Now-1;pageMoving=false;dormLeaving=false;sortBegan=Now-1;previousIllustration=null;previousSkinBegan=Now-1;}
        public void ReviewSkinIndex(int index,bool animate=true){if(animate)SetSkinIndex(index);else{skinIndex=index;skinPosition=skinFrom=index;skinMotionBegan=Now-1;}}
        public void ReviewCarouselPointer(EventType type,Vector2 p){SkinCarouselPointer(type,p);}
        public void ReviewEditHome(){OpenLobbyIllustrationEditor();}
        public void ReviewHomePose(float x,float y,float scale){homeDraft=new LobbyIllustrationPose{x=x,y=y,scale=scale};}
        public void ReviewInfoScroll(float y){infoScroll.y=y;}
        public void ReviewZoomSlider(float p,bool home=false){var point=new Vector2(width-300,199.99f+581.643f*Mathf.Clamp01(p));if(home){ZoomPointer(EventType.MouseDown,point,ref homeDraft.scale);ZoomPointer(EventType.MouseUp,point,ref homeDraft.scale);}else{ZoomPointer(EventType.MouseDown,point,ref appreciationZoom);ZoomPointer(EventType.MouseUp,point,ref appreciationZoom);}}
        public bool ReviewLiveEnabled=>characterArt.Animate;
        public bool ReviewHomeEditing=>homeIllustrationEditing;
        public bool ReviewHomeControlsHidden=>homeControlsHidden;
        public LobbyIllustrationPose ReviewHomeDraft=>homeDraft;
        public float ReviewInfoOffset=>infoScroll.y;
        public Texture ReviewCharacterTexture=>characterArt.Art(CurrentOutfit,Now);
        public Rect ReviewSortRect(bool starred,int index)=>SortCardRect(starred,index);
        public void ReviewOpenSort()=>OpenCharacterSort();
        public void ReviewSortScrollToEnd(){sortScroller.Offset=Mathf.Max(0,SortContentHeight-SortViewport.height);}
#endif
    }
}
