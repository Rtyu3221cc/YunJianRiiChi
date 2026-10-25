using System;
using System.Linq;
using UnityEngine;
namespace YunJian.Riichi {
    public sealed partial class LobbyController {
        sealed class CharacterPaneDrag {public bool pending,dragging;public Vector2 press,origin;public void Reset(){pending=dragging=false;}}
        readonly CharacterPaneDrag roleRosterDrag=new CharacterPaneDrag(),rolePoolDrag=new CharacterPaneDrag(),roleSkinsDrag=new CharacterPaneDrag();
        Vector2 roleRosterScroll,rolePoolScroll,roleSkinsScroll;
        bool rolePopupFromPool;
        float RoleOrigin=>width/2-960;
        Rect RoleRect(float x,float y,float w,float h)=>new Rect(RoleOrigin+x,y,w,h);
        // Native knob padding is outside the 140x40 track; include its shadow
        // rather than placing the full padded sprite inside the track.
        static Rect RoleRandomKnob(Rect track,bool random)=>new Rect(random?track.xMax-65:track.x-5,track.y-4,70,49);
        // Share rounded physical-pixel boundaries between the nine slices.
        // The original tab has 10px of shadow on each side; trim only that padding.
        float RolePixel(float value,float scale,float offset)=>Mathf.Abs(scale)<.00001f?value:(Mathf.Round(value*scale+offset)-offset)/scale;
        void RoleSlice(Rect r,string key,float trimX=0){
            var t=CharacterRoster.Texture("UI/"+key);var b=CharacterRoster.Ui(key)?.border;if(t==null)return;if(b==null||b.All(v=>v<=0)){GUI.DrawTexture(r,t,ScaleMode.StretchToFill,true);return;}
            float left=Mathf.Max(0,b[0]-trimX),right=Mathf.Max(0,b[2]-trimX),l=Mathf.Min(left,r.width/3),rr=Mathf.Min(right,r.width/3),bb=Mathf.Min(b[1],r.height/3),tt=Mathf.Min(b[3],r.height/3);
            float[] xs={r.x,r.x+l,r.xMax-rr,r.xMax},ys={r.y,r.y+tt,r.yMax-bb,r.yMax},us={trimX/t.width,b[0]/t.width,1-b[2]/t.width,1-trimX/t.width},vs={0,b[3]/t.height,1-b[1]/t.height,1};var m=GUI.matrix;
            for(int i=0;i<4;i++){xs[i]=RolePixel(xs[i],m.m00,m.m03);ys[i]=RolePixel(ys[i],m.m11,m.m13);}
            for(int y=0;y<3;y++)for(int x=0;x<3;x++)GUI.DrawTextureWithTexCoords(Rect.MinMaxRect(xs[x],ys[y],xs[x+1],ys[y+1]),t,new Rect(us[x],1-vs[y+1],us[x+1]-us[x],vs[y+1]-vs[y]));
        }
        void ResetRolePanes(){roleRosterScroll=rolePoolScroll=roleSkinsScroll=Vector2.zero;roleRosterDrag.Reset();rolePoolDrag.Reset();roleSkinsDrag.Reset();rolePopupFromPool=false;}
        void RoleTab(bool random){randomTab=random;skinExpanded=false;ResetRolePanes();Cue("btn_click");}
        bool PanePointer(CharacterPaneDrag drag,Rect view,out EventType type,out Vector2 point,out Vector2 delta){
            type=Event.current.type;point=Event.current.mousePosition;delta=Event.current.delta;
#if UNITY_EDITOR
            if(type==EventType.Repaint&&reviewGuiPointer.Count>0){var queued=reviewGuiPointer.Peek();var local=GUI.matrix.inverse.MultiplyPoint3x4(new Vector3(queued.point.x,queued.point.y,0));bool covered=skinExpanded&&(drag==roleRosterDrag||drag==rolePoolDrag)&&RolePopupRect().Contains(local);if(!covered&&(view.Contains(local)||drag.pending)){reviewGuiPointer.Dequeue();type=queued.type;point=local;delta=queued.delta/Mathf.Max(.001f,GUI.matrix.m00);return true;}}
#endif
            return false;
        }
        void DragCharacterPane(CharacterPaneDrag drag,Rect view,Vector2 content,ref Vector2 scroll,bool horizontal){
            if(!GUI.enabled)return;bool injected=PanePointer(drag,view,out var type,out var point,out var delta);int control=GUIUtility.GetControlID(drag.GetHashCode(),FocusType.Passive);Vector2 max=new Vector2(Mathf.Max(0,content.x-view.width),Mathf.Max(0,content.y-view.height));bool consumed=false;
            if(type==EventType.MouseDown){drag.pending=view.Contains(point)&&(horizontal?max.x:max.y)>0;drag.dragging=false;if(drag.pending){drag.press=point;drag.origin=scroll;}}
            if(type==EventType.MouseDrag&&drag.pending&&(drag.dragging||Mathf.Abs(horizontal?point.x-drag.press.x:point.y-drag.press.y)>10)){drag.dragging=true;if(horizontal)scroll.x=drag.origin.x+drag.press.x-point.x;else scroll.y=drag.origin.y+drag.press.y-point.y;consumed=true;}
            if(type==EventType.MouseUp){consumed=drag.dragging;drag.Reset();}
            if(type==EventType.ScrollWheel&&view.Contains(point)){if(horizontal)scroll.x+=(Mathf.Abs(delta.x)>Mathf.Abs(delta.y)?delta.x:delta.y)*32;else scroll.y+=delta.y*32;consumed=true;}
            scroll.x=Mathf.Clamp(scroll.x,0,max.x);scroll.y=Mathf.Clamp(scroll.y,0,max.y);
            if(consumed&&!injected){GUIUtility.hotControl=type==EventType.MouseUp?0:control;Event.current.Use();}
        }
        void DrawRoleSelection(){
            float age=Now-(roleClosing?roleClosed:roleStarted);if(roleClosing&&age>=.25f){roleClosing=false;return;}
            string clip=roleClosing?"ui_popout":"ui_pop";float alpha=Mathf.Clamp01(Sample(clip,1574349066,0,age,1)),scale=Mathf.Max(.001f,Sample(clip,3,0,age,1));
            Tint(new Rect(bleedLeft,0,bleedWidth,1080),new Color(0,0,0,.32f*alpha));var matrix=GUI.matrix;var color=GUI.color;bool enabled=GUI.enabled;GUI.color=new Color(color.r,color.g,color.b,color.a*alpha);GUI.matrix*=Matrix4x4.Translate(new Vector3(width/2,540))*Matrix4x4.Scale(new Vector3(scale,scale,1))*Matrix4x4.Translate(new Vector3(-width/2,-540));GUI.enabled&=!roleClosing&&age>.12f&&Modal=="";
            if(GUI.enabled&&Event.current.type==EventType.MouseDown&&!RoleRect(238.2f,43.66f,1437.44f,992.29f).Contains(Event.current.mousePosition)){CloseRoleSelection();Event.current.Use();}
            RoleSlice(RoleRect(261.145f,61.02f,1396.57f,956.73f),"Role/bg");CImage(RoleRect(261.145f,908.75f,247,107),"Role/decorate_left",ScaleMode.ScaleToFit);CImage(RoleRect(1261.715f,911.75f,396,104),"Role/decorate_right",ScaleMode.ScaleToFit);RoleSlice(RoleRect(238.201f,43.66f,1437.44f,992.29f),"Role/frame");
            for(int i=0;i<2;i++){bool selected=randomTab==(i==1);var tab=RoleRect(398.3f+i*563,84,563,77);RoleSlice(tab,selected?"Role/tabs_selected":"Role/tabs_unselected",10);Label(tab,i==0?"切换雀士":"设定对局随机雀士",CText(40,TextAnchor.MiddleCenter,true),selected?CharacterGold:new Color(.73f,.76f,.82f));if(Hit(tab,i==0?"role-tab-single":"role-tab-random"))RoleTab(i==1);}
            CImage(RoleRect(1566.99f,86,42,42),"Role/btn_close",ScaleMode.ScaleToFit);if(Hit(RoleRect(1541,60,94,94),"role-close"))CloseRoleSelection();
            if(skinExpanded&&Event.current.type==EventType.MouseDown&&!RolePopupRect().Contains(Event.current.mousePosition)&&!RoleAvatarAt(Event.current.mousePosition))skinExpanded=false;
            if(randomTab)DrawRandomRoster();else DrawActiveRoster();
            if(skinExpanded&&(!randomTab||CharacterRoster.Random))DrawRoleSkinPopover();
            GUI.matrix=matrix;GUI.color=color;GUI.enabled=enabled;
        }
        void RolePane(Rect bounds,string heading,Rect title,Rect divider){CSlice(bounds,"Role/block_base");CSlice(divider,"Role/divider");Label(title,heading,CText(40,TextAnchor.MiddleLeft,true),CharacterGold);}
        void RoleFilter(Rect r,string id){CImage(r,"Role/star_slider",ScaleMode.ScaleToFit);CImage(new Rect(roleStars?r.xMax-76:r.x+5,r.y+5,71.31f,49),"Role/star_btn",ScaleMode.ScaleToFit);if(Hit(r,id)){roleStars=!roleStars;skinExpanded=false;roleRosterScroll=Vector2.zero;}}
        void RoleHead(Rect r,CharacterDefinition role,int skin,bool chosen=false,bool favorite=true,int count=0,bool equipped=false){
            CSlice(r,"Role/kuangbg");var picture=CharacterRoster.ListPortrait(role,skin);if(picture!=null)GUI.DrawTexture(new Rect(r.x+2,r.y+2,r.width-4,r.height-4),picture,ScaleMode.ScaleToFit,true);CSlice(r,"Role/kuangbound");
            if(favorite&&CharacterRoster.Favorite(role.key))CImage(new Rect(r.x+3,r.yMax-34,30,30),"Role/collect_marking",ScaleMode.ScaleToFit);
            if(count>0){CImage(new Rect(r.xMax-58,r.yMax-58,58,58),"Role/select_base",ScaleMode.ScaleToFit);Label(new Rect(r.xMax-31,r.yMax-30,30,29),count.ToString(),CText(26),new Color(.015f,.08f,.13f));}
            if(equipped)CImage(new Rect(r.xMax-67,r.yMax-53,78.06f,60.04f),"Role/using",ScaleMode.ScaleToFit);
            if(chosen)CSlice(new Rect(r.x-7,r.y-7,r.width+14,r.height+14),"Role/select_box_blue");
        }
        Rect RoleRosterView=>randomTab?RoleRect(871.3f,318.23f,724,660.99f):RoleRect(691.39f,248.65f,910,729.51f);
        Rect RolePoolView=>RoleRect(333.53f,311.4f,395.56f,682.55f);
        Rect RoleRosterCell(int i){var v=RoleRosterView;return new Rect(v.x+26+i%(randomTab?4:5)*174,v.y+25+i/(randomTab?4:5)*185-roleRosterScroll.y,162,162);}
        Rect RolePoolCell(int i){var v=RolePoolView;return new Rect(v.x+26+i%2*175,v.y+28+i/2*160-rolePoolScroll.y,124,124);}
        CharacterChoice[] RolePoolGroups()=>CharacterRoster.Pool.GroupBy(p=>p.character).Select(g=>g.First()).ToArray();
        void OpenRoleSkins(string key,bool fromPool=false){rolePreview=key;rolePopupFromPool=fromPool;skinExpanded=true;roleSkinsScroll=Vector2.zero;roleSkinsDrag.Reset();Cue("btn_click");}
        void DrawActiveRoster(){
            var active=CharacterRoster.Active;var role=CharacterRoster.Find(active.character);var large=RoleRect(360.4f,394.67f,209.47f,209.83f);RoleHead(large,role,active.skin,false,false);CImage(RoleRect(512.68f,383.15f,104,163),"Role/using_1_text_right2",ScaleMode.ScaleToFit);if(Hit(RoleRect(349.88f,384.55f,230,230),"active-character-skins"))OpenRoleSkins(active.character);
            var outfit=CharacterRoster.Skin(active.character,active.skin);CSlice(RoleRect(277.14f,647.15f,375.49f,100),"Role/name_base");Label(RoleRect(291.31f,654.51f,350.74f,51.12f),role.name,CText(36,TextAnchor.MiddleCenter,true),CharacterGold);Label(RoleRect(286.38f,699.65f,357,40),outfit.id==role.defaultSkin?outfit.description.Split('\n')[0]:outfit.name,CText(28),new Color(.84f,.87f,.92f));
            RolePane(RoleRect(669.52f,172.65f,933.15f,805.51f),"宿舍",RoleRect(707.87f,181.51f,521,61.91f),RoleRect(700.2f,243.43f,880.54f,5.03f));RoleFilter(RoleRect(1440.49f,185.46f,139,59),"role-favorites");DrawRoleRoster();
        }
        void DrawRoleRoster(){
            var roles=CharacterRoster.Ordered(roleStars,true);var view=RoleRosterView;int columns=randomTab?4:5;var extent=new Vector2(view.width,Mathf.Max(view.height,Mathf.CeilToInt(roles.Length/(float)columns)*185+40));
            bool was=GUI.enabled;GUI.enabled&=!(skinExpanded&&RolePopupRect().Contains(Event.current.mousePosition));DragCharacterPane(roleRosterDrag,view,extent,ref roleRosterScroll,false);
            GUI.BeginGroup(view);var offset=targetOffset;targetOffset+=view.position;
            for(int i=0;i<roles.Length;i++){var role=roles[i];var r=RoleRosterCell(i);r.position-=view.position;if(r.yMax<0||r.y>view.height)continue;int skin=role.key==CharacterRoster.Active.character?CharacterRoster.Active.skin:CharacterRoster.Outfit(role.key);int count=CharacterRoster.Pool.Count(p=>p.character==role.key);RoleHead(r,role,skin,skinExpanded&&rolePreview==role.key&&!rolePopupFromPool,true,randomTab?count:0,!randomTab&&role.key==CharacterRoster.Active.character);
                if(Hit(r,(randomTab?"random-character-":"battle-character-")+role.key)){if(randomTab)OpenRoleSkins(role.key);else if(role.key==CharacterRoster.Active.character)OpenRoleSkins(role.key);else{rolePreview=role.key;skinExpanded=false;CharacterRoster.Use(role.key,CharacterRoster.Outfit(role.key));client.PublishCharacterSelection();PlayCharacterGreeting(role.key);}}
            }
            targetOffset=offset;GUI.EndGroup();GUI.enabled=was;
            if(roles.Length==0)RoleEmpty(view,"暂无星标雀士");
        }
        void RoleEmpty(Rect r,string message){CImage(new Rect(r.center.x-87.5f,r.center.y-70,175,60),"Role/noinfo",ScaleMode.ScaleToFit);Label(new Rect(r.x+12,r.center.y,r.width-24,100),message,CText(32),new Color(.49f,.53f,.61f));}
        void DrawRandomRoster(){
            bool random=CharacterRoster.Random;var toggle=RoleRect(310,176,140,40);RoleSlice(toggle,"Role/btn_rand_bg");Label(new Rect(toggle.x+(random?0:65),toggle.y,75,40),random?"随机":"指定",CText(26,TextAnchor.MiddleCenter,true),CharacterGold);CImage(RoleRandomKnob(toggle,random),"Role/btn_rand_slider");
            if(Hit(toggle,"random-enabled")){CharacterRoster.Random=!random;random=CharacterRoster.Random;skinExpanded=false;client.PublishCharacterSelection();}
            string modeText=random?"当前模式：随机角色，对局时会从已选择的雀士／服饰中随机选择":"当前模式：指定角色，使用当前选择的雀士与服饰";var modeStyle=CText(28,TextAnchor.MiddleLeft);modeStyle.wordWrap=false;float textWidth=Mathf.Min(1065,modeStyle.CalcSize(new GUIContent(modeText)).x);Label(RoleRect(460,161.55f,textWidth,68.9f),modeText,modeStyle,new Color(.83f,.85f,.9f));var helpRect=RoleRect(470+textWidth,171,50,50);CImage(helpRect,"Role/what",ScaleMode.ScaleToFit);
            if(Hit(helpRect,"random-help")){help="切换至随机后，可在宿舍头像下展开并选择服饰。随机结果在整场对局开始前确定，局内不更换。列表为空时沿用当前指定雀士。";OpenModal("help");}
            if(!random){var box=RoleRect(310,245,1300,750);CSlice(box,"Role/block_base");RoleEmpty(box,"切换至随机模式后，可配置对局随机雀士");return;}
            RolePane(RoleRect(840.43f,239.25f,770.29f,756.17f),"宿舍",RoleRect(881.08f,248.11f,358.19f,62.21f),RoleRect(874.35f,310.32f,705.9f,5.03f));RoleFilter(RoleRect(1440.47f,252.06f,139,59),"random-stars");
            RolePane(RoleRect(308.2f,240.39f,420.85f,753.55f),"随机列表",RoleRect(341.57f,250.12f,201.23f,59.75f),RoleRect(340,309.87f,358.17f,5));CImage(RoleRect(552.88f,255.08f,139.85f,51.57f),"Role/btn_clear",ScaleMode.ScaleToFit);Label(RoleRect(556.9f,257.07f,132.33f,47.34f),"清空",CText(30,TextAnchor.MiddleCenter,true),CharacterGold);if(Hit(RoleRect(552.88f,255.08f,139.85f,51.57f),"random-clear")){CharacterRoster.ClearPool();skinExpanded=false;client.PublishCharacterSelection();}
            DrawRolePool();CImage(RoleRect(753.8f,563.5f,68,105),"Role/decorate_arrow",ScaleMode.ScaleToFit);DrawRoleRoster();
        }
        void DrawRolePool(){
            var groups=RolePoolGroups();var view=RolePoolView;var extent=new Vector2(view.width,Mathf.Max(view.height,Mathf.CeilToInt(groups.Length/2f)*160+50));bool was=GUI.enabled;GUI.enabled&=!(skinExpanded&&RolePopupRect().Contains(Event.current.mousePosition));DragCharacterPane(rolePoolDrag,view,extent,ref rolePoolScroll,false);GUI.BeginGroup(view);var offset=targetOffset;targetOffset+=view.position;
            for(int i=0;i<groups.Length;i++){var item=groups[i];var r=RolePoolCell(i);r.position-=view.position;if(r.yMax<0||r.y>view.height)continue;RoleHead(r,CharacterRoster.Find(item.character),item.skin,skinExpanded&&rolePreview==item.character&&rolePopupFromPool,true,CharacterRoster.Pool.Count(p=>p.character==item.character));var remove=new Rect(r.xMax-24,r.y-12,40,40);CImage(remove,"Role/btn_del",ScaleMode.ScaleToFit);if(Hit(remove,"random-remove-"+item.character)){CharacterRoster.RemovePoolCharacter(item.character);skinExpanded=false;client.PublishCharacterSelection();break;}if(Hit(r,"random-pool-"+item.character))OpenRoleSkins(item.character,true);}
            targetOffset=offset;GUI.EndGroup();GUI.enabled=was;if(groups.Length==0)RoleEmpty(view,"这里什么都没有喵");
        }
        Rect RolePopupAnchor(){
            if(randomTab&&rolePopupFromPool){var groups=RolePoolGroups();int i=Array.FindIndex(groups,p=>p.character==rolePreview);return i>=0?RolePoolCell(i):RoleRect(360,340,124,124);}
            var roles=CharacterRoster.Ordered(roleStars,true);int index=Array.FindIndex(roles,r=>r.key==rolePreview);return index>=0?RoleRosterCell(index):RoleRect(360,395,210,210);
        }
        Rect RolePopupRect(){var role=CharacterRoster.Find(rolePreview)??CharacterRoster.Find(CharacterRoster.Active.character);var anchor=RolePopupAnchor();float w=Mathf.Min(480,Mathf.Max(226,role.skins.Length*124+28));return new Rect(Mathf.Clamp(anchor.center.x-w/2,RoleOrigin+275,RoleOrigin+1610-w),Mathf.Min(anchor.yMax+18,800),w,154);}
        bool RoleAvatarAt(Vector2 p){if(!randomTab&&RoleRect(349,384,230,230).Contains(p))return true;var roles=CharacterRoster.Ordered(roleStars,true);for(int i=0;i<roles.Length;i++)if(RoleRosterCell(i).Contains(p))return true;if(randomTab){var groups=RolePoolGroups();for(int i=0;i<groups.Length;i++)if(RolePoolCell(i).Contains(p))return true;}return false;}
        void DrawRoleSkinPopover(){
            var role=CharacterRoster.Find(rolePreview);if(role==null){skinExpanded=false;return;}var r=RolePopupRect();var anchor=RolePopupAnchor();RoleSlice(r,"Role/unfold_base");CImage(new Rect(Mathf.Clamp(anchor.center.x-15,r.x+24,r.xMax-54),r.y-2,30,15),"Role/select_arrow",ScaleMode.ScaleToFit);var view=new Rect(r.x+12,r.y+10,r.width-24,126);var extent=new Vector2(Mathf.Max(view.width,role.skins.Length*124+12),126);DragCharacterPane(roleSkinsDrag,view,extent,ref roleSkinsScroll,true);GUI.BeginGroup(view);var offset=targetOffset;targetOffset+=view.position;
            for(int i=0;i<role.skins.Length;i++){var skin=role.skins[i];var card=new Rect((role.skins.Length==1?(view.width-110)/2:8+i*124-roleSkinsScroll.x),8,110,110);bool chosen=randomTab?CharacterRoster.InPool(role.key,skin.id):CharacterRoster.Active.character==role.key&&CharacterRoster.Active.skin==skin.id;RoleHead(card,role,skin.id,chosen,false);CImage(new Rect(card.x+4,card.yMax-41,40.48f,40.48f),"Role/flag_skin",ScaleMode.ScaleToFit);if(skin.dynamic)CImage(new Rect(card.x+4,card.y+3,41.95f,20.41f),"Role/spine_tip",ScaleMode.ScaleToFit);
                if(Hit(card,(randomTab?"random-skin-":"battle-skin-")+skin.id)){if(randomTab){CharacterRoster.TogglePool(role.key,skin.id);if(rolePopupFromPool&&!CharacterRoster.Pool.Any(p=>p.character==role.key))skinExpanded=false;}else{CharacterRoster.Use(role.key,skin.id);skinExpanded=false;PlayCharacterGreeting(role.key);}client.PublishCharacterSelection();}
            }
            targetOffset=offset;GUI.EndGroup();if(extent.x>view.width){var rail=new Rect(r.x+20,r.yMax-17,r.width-40,7);CSlice(rail,"Role/bar_bg");float thumb=Mathf.Max(35,rail.width*view.width/extent.x);CSlice(new Rect(rail.x+(rail.width-thumb)*roleSkinsScroll.x/(extent.x-view.width),rail.y,thumb,7),"Role/bar");}
        }
#if UNITY_EDITOR
        public float ReviewSkinDescriptionOffset=>skinDescriptionScroll.y;
        public bool ReviewRoleRandomTab=>randomTab;
        public Rect ReviewRoleSkinPopover=>RolePopupRect();
        public float ReviewRoleSkinsOffset=>roleSkinsScroll.x;
        public bool ReviewRolePopoverOpen=>skinExpanded;
#endif
    }
}
