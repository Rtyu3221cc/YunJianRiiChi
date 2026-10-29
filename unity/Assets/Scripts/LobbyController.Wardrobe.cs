using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi {
    public sealed partial class LobbyController {
        sealed class DressCategory {
            public string key,name,icon,hint;
            public DressCategory(string key,string name,string icon,string hint){this.key=key;this.name=name;this.icon=icon;this.hint=hint;}
        }
        static readonly DressCategory[] dressCategories={
            new DressCategory("stick","立直棒","slot_liqibang","指定立直棒的样式"),
            new DressCategory("winEffect","和牌特效","slot_hule","指定和牌时的特效样式"),
            new DressCategory("riichiEffect","立直特效","slot_liqi","指定立直时的特效样式"),
            new DressCategory("callout","鸣牌指示","slot_effect_tip","指定鸣牌指示的特效样式"),
            new DressCategory("hand","手的样式","slot_hand","指定使用的手的样式"),
            new DressCategory("music","立直音乐","liqibgm_0","指定立直之后的音乐"),
            new DressCategory("headFrame","头像框","slot_head_frame","默认头像框"),
            new DressCategory("cloth","桌布","slot_desktop","默认桌布"),
            new DressCategory("tileBack","牌背","slot_hule","默认牌背"),
            new DressCategory("tileFace","牌面","slot_mjpface","默认牌面"),
            new DressCategory("lobbyBackground","大厅背景","slot_hule","庭院")
        };
        const float DressPitch=130,DressViewHeight=652,DressRowWidth=638;
        readonly Dictionary<string,string> dressDraft=new Dictionary<string,string>();
        readonly Dictionary<string,Material> dressSlices=new Dictionary<string,Material>();
        readonly WardrobeMotionPreview dressMotion=new WardrobeMotionPreview();
        readonly WardrobeTablePreview dressTable=new WardrobeTablePreview();
        readonly CharacterPaneDrag dressDrag=new CharacterPaneDrag();
        readonly CharacterPaneDrag dressItemDrag=new CharacterPaneDrag();
        Vector2 dressItemScroll;bool dressItemRailDragging;float dressItemRailGrab;
        Vector2 dressScroll;Rect dressSliceClip;bool dressEye;string dressCategory="stick";float dressPreviewBegan;AudioSource dressAudio;
#if UNITY_EDITOR
        float dressReviewAge=-1;
#endif
        bool DressDirty=>dressDraft.Any(p=>WardrobeSelection.Get(p.Key)?.id!=p.Value);
        static bool DressCanPreview(string category)=>category!="winEffect"&&category!="riichiEffect"&&category!="callout";
        WardrobeItem Draft(string key)=>dressDraft.TryGetValue(key,out var id)?WardrobeCatalog.Find(id):WardrobeSelection.Get(key);
        void OpenWardrobe(){
            StopDressPreview();dressDraft.Clear();foreach(var i in WardrobeCatalog.Data.items)if(!dressDraft.ContainsKey(i.category))dressDraft[i.category]=WardrobeSelection.Get(i.category).id;
            dressEye=false;dressCategory="stick";dressScroll=dressItemScroll=Vector2.zero;dressItemDrag.Reset();dressItemRailDragging=false;SetDormPage(DormView.Wardrobe);
        }
        void StopDressPreview(){dressTable.Hide();dressMotion.Hide();if(dressAudio!=null)dressAudio.Stop();if(PlaylistAudio.Active!=null)PlaylistAudio.Active.ExternalAudition=false;}
        void DisposeWardrobe(){StopDressPreview();dressMotion.Dispose();foreach(var m in dressSlices.Values)Destroy(m);dressSlices.Clear();}
        void TickWardrobePreview(){
            if(!dressEye)return;
            var item=Draft(dressCategory);if(item==null)return;if(dressCategory=="hand")item=WardrobeRuntime.ResolveHand(item,CurrentCharacter.key);
            float age=Time.unscaledTime-dressPreviewBegan;
#if UNITY_EDITOR
            if(dressReviewAge>=0)age=dressReviewAge;
#endif
            if(item.standardStick||item.HasModel||!string.IsNullOrEmpty(item.effect)||!string.IsNullOrEmpty(item.nativeEffect)||!string.IsNullOrEmpty(item.motion))dressMotion.Tick(item,age);
        }
        void SelectDressCategory(string key){
            StopDressPreview();dressCategory=key;dressEye=false;dressItemScroll=Vector2.zero;dressItemDrag.Reset();dressItemRailDragging=false;int i=Array.FindIndex(dressCategories,c=>c.key==key);
            float top=i*DressPitch;if(top<dressScroll.y)dressScroll.y=top;else if(top+150>dressScroll.y+DressViewHeight)dressScroll.y=top+150-DressViewHeight;
            dressScroll.y=Mathf.Clamp(dressScroll.y,0,dressCategories.Length*DressPitch+12-DressViewHeight);
        }
        void ToggleDressEye(){
            if(!DressCanPreview(dressCategory)){dressEye=false;StopDressPreview();return;}
            dressEye=!dressEye;StopDressPreview();dressPreviewBegan=Time.unscaledTime;
#if UNITY_EDITOR
            dressReviewAge=-1;
#endif
            var item=Draft(dressCategory);if(dressEye&&!string.IsNullOrEmpty(item?.audio)){
                if(dressAudio==null){dressAudio=gameObject.AddComponent<AudioSource>();dressAudio.playOnAwake=false;dressAudio.spatialBlend=0;}
                dressAudio.clip=Resources.Load<AudioClip>(item.audio);dressAudio.volume=AppPreferences.Gain(1);dressAudio.loop=true;dressAudio.Play();if(PlaylistAudio.Active!=null)PlaylistAudio.Active.ExternalAudition=true;
            }
        }
        void DressImage(Rect r,string path,ScaleMode mode=ScaleMode.ScaleToFit){var t=WardrobeCatalog.Texture(path);if(t!=null)GUI.DrawTexture(r,t,mode,true);}
        void WImage(Rect r,string key,ScaleMode mode=ScaleMode.StretchToFill)=>DressImage(r,"Majsoul/Wardrobe/UI/"+key,mode);
        void WSlice(Rect r,string key,float desaturate=0){
            if(Event.current.type!=EventType.Repaint)return;var t=WardrobeCatalog.Texture("Majsoul/Wardrobe/UI/"+key);if(t==null)return;var b=WardrobeCatalog.Ui(key)?.border;
            if((b==null||b.All(v=>v==0))&&desaturate<=0){GUI.DrawTexture(r,t,ScaleMode.StretchToFill,true);return;}if(b==null)b=new float[4];
            // One quad avoids joins between separately blended sliced GUI draws.
            string id=key+"/"+r.width+"/"+r.height+"/"+desaturate;
            if(!dressSlices.TryGetValue(id,out var m)){
                m=new Material(Resources.Load<Shader>("Majsoul/Wardrobe/NineSlice")){mainTexture=t};m.SetVector("_Border",new Vector4(b[0],b[1],b[2],b[3]));m.SetVector("_SourceSize",new Vector4(t.width,t.height,0,0));m.SetVector("_TargetSize",new Vector4(r.width,r.height,0,0));dressSlices[id]=m;
            }
            var visible=r;var uv=new Rect(0,0,1,1);
            if(dressSliceClip.width>0){
                visible=Rect.MinMaxRect(Mathf.Max(r.xMin,dressSliceClip.xMin),Mathf.Max(r.yMin,dressSliceClip.yMin),Mathf.Min(r.xMax,dressSliceClip.xMax),Mathf.Min(r.yMax,dressSliceClip.yMax));
                if(visible.width<=0||visible.height<=0)return;
                uv=new Rect((visible.x-r.x)/r.width,1-(visible.yMax-r.y)/r.height,visible.width/r.width,visible.height/r.height);
            }
            m.SetFloat("_Desaturate",desaturate);m.SetColor("_Tint",GUI.color);Graphics.DrawTexture(visible,t,uv,0,0,0,0,Color.white,m);
        }
        void DressTabs(float right,bool wardrobe){
            // Native page: 38.53px overlap and 45px right inset.
            float deco=right-45-284,character=deco-245.47f;
            if(wardrobe){WImage(new Rect(character,66.8f,284,84),"tab_gray3");WSlice(new Rect(deco,66.8f,284,84),"tab_bright2");}
            else{WImage(new Rect(deco,66.8f,284,84),"tab_gray3");WSlice(new Rect(character,66.8f,284,84),"tab_bright2");}
            Label(new Rect(character+64.5f,86.7f,180,64),"角色",CText(42,TextAnchor.MiddleCenter,true),wardrobe?CharacterGold:new Color(.2f,.1f,.04f));Label(new Rect(deco+64.5f,86.7f,180,64),"装扮",CText(42,TextAnchor.MiddleCenter,true),wardrobe?new Color(.2f,.1f,.04f):CharacterGold);
            if(Hit(new Rect(character,67,245.47f,84),"dress-role")&&wardrobe){StopDressPreview();SetDormPage(DormView.Select);}
            if(Hit(new Rect(deco,67,284,84),"dress-tab")&&!wardrobe)OpenWardrobe();
        }
        void DressCorners(Rect r){
            float s=30;var t=WardrobeCatalog.Texture("Majsoul/Wardrobe/UI/select_corner");if(t==null)return;
            GUI.DrawTextureWithTexCoords(new Rect(r.x-5,r.y-5,s,s),t,new Rect(0,0,1,1));GUI.DrawTextureWithTexCoords(new Rect(r.xMax-s+5,r.y-5,s,s),t,new Rect(1,0,-1,1));GUI.DrawTextureWithTexCoords(new Rect(r.x-5,r.yMax-s+5,s,s),t,new Rect(0,1,1,-1));GUI.DrawTextureWithTexCoords(new Rect(r.xMax-s+5,r.yMax-s+5,s,s),t,new Rect(1,1,-1,-1));
        }
        void DrawDormDressTransition(){
            bool toDress=dormView==DormView.Wardrobe;float t=Mathf.Clamp01((Now-pageBegan)/PageDuration),q=Mathf.SmoothStep(0,1,t);
            var role=new Rect(width-1016,146.8f,766,892);var dress=new Rect(width-1910,155,1660,855);
            var frame=InterpolateRect(toDress?role:dress,toDress?dress:role,q);
            var color=GUI.color;bool enabled=GUI.enabled;GUI.enabled=false;
            float portrait=toDress?1-Mathf.SmoothStep(0,1,t/.7f):Mathf.SmoothStep(0,1,t);
            GUI.color=new Color(color.r,color.g,color.b,color.a*portrait);
            var skin=CurrentOutfit;var r=DormPortrait(skin);CharacterFraming.NativeLocal(skin.id,"B0",0,r,out bool flip);characterArt.Draw(r,skin,Now,flip);DrawDormLeftButtons();DrawCharacterNameplate();
            GUI.color=color;Tint(new Rect(frame.x+15,frame.y+14,frame.width-30,frame.height-28),new Color(.015f,.012f,.028f,.91f));WSlice(frame,"bg_bound");
            float outgoing=1-Mathf.Clamp01(t/.36f),incoming=Mathf.Clamp01((t-.65f)/.35f);
            GUI.color=new Color(color.r,color.g,color.b,color.a*(toDress?outgoing:incoming));if(GUI.color.a>.001f)DrawDormCards(false);
            GUI.color=new Color(color.r,color.g,color.b,color.a*(toDress?incoming:outgoing));if(GUI.color.a>.001f)DrawWardrobe(false);
            GUI.color=color;DressTabs(role.xMax,toDress);GUI.enabled=enabled;
        }
        void DrawWardrobe(bool shell=true){
            float x=width-1910;var outer=new Rect(x,155,1660,855);
            if(shell){Tint(new Rect(x+14,169,1632,827),new Color(.015f,.012f,.028f,.91f));WSlice(outer,"bg_bound");DressTabs(x+1660,true);}
            var viewport=new Rect(x+39,193,724,DressViewHeight);float total=dressCategories.Length*DressPitch+12;DragCharacterPane(dressDrag,viewport,new Vector2(viewport.width,total),ref dressScroll,false);GUI.BeginGroup(viewport);var offset=targetOffset;targetOffset+=viewport.position;
            dressSliceClip=new Rect(0,0,viewport.width,viewport.height);float k=DressRowWidth/612.99f;
            for(int i=0;i<dressCategories.Length;i++){
                var cat=dressCategories[i];var row=new Rect(12,i*DressPitch-dressScroll.y+10,DressRowWidth,114.53f*k);if(row.yMax<0||row.y>viewport.height)continue;
                WSlice(row,"bf_popout");WImage(new Rect(row.center.x-139.01f*k,row.center.y-45.53f*k,3*k,92.5f*k),"line");var item=Draft(cat.key);
                Label(new Rect(row.center.x-294.66f*k,row.center.y-42.63f*k,148.79f*k,84.5f*k),cat.name,CText(32,TextAnchor.MiddleCenter,true),CharacterGold);
                Label(new Rect(row.center.x-119.69f*k,row.center.y-45*k,304*k,90*k),item?.name??cat.hint,CText(28,TextAnchor.MiddleLeft,true),CharacterGold);
                var icon=new Rect(row.center.x+201.315f*k,row.center.y-47.555f*k,94.95f*k,94.95f*k);if(item!=null)DressImage(icon,item.icon,ScaleMode.ScaleAndCrop);else WImage(icon,cat.icon,ScaleMode.ScaleToFit);
                if(cat.key==dressCategory)WSlice(new Rect(row.center.x+(11.79f-326.235f)*k,row.center.y+(.52f-66.065f)*k,652.47f*k,132.13f*k),"choose");
                if(Hit(row,"dress-category-"+cat.key))SelectDressCategory(cat.key);
            }
            dressSliceClip=Rect.zero;targetOffset=offset;GUI.EndGroup();
            var rail=new Rect(x+754,205,6.4f,DressViewHeight-24);CImage(rail,"ss_slidebar");CImage(new Rect(rail.center.x-17,rail.y+dressScroll.y/(total-viewport.height)*(rail.height-34),34,34),"ss_slidebtn");
            var save=new Rect(x+250.875f,869.16f,246.45f,82.08f);WSlice(save,"btn_bright",DressDirty?0:.9f);
            Label(save,DressDirty?"保存":"使用中",CText(43,TextAnchor.MiddleCenter,true),new Color(.2f,.13f,.08f));if(Hit(save,"dress-save",DressDirty)){WardrobeSelection.Save(dressDraft);StopDressPreview();}
            var pane=new Rect(x+797,194,816,782);WSlice(pane,"bf_popout");var category=dressCategories.First(c=>c.key==dressCategory);var entries=WardrobeCatalog.Items(dressCategory);
            Label(new Rect(pane.x+42,pane.y+15,460,70),category.name,CText(39,TextAnchor.MiddleLeft,true),CharacterGold);WImage(new Rect(pane.x+36,pane.y+91,pane.width-72,2),"line");
            if(entries.Length>0&&DressCanPreview(dressCategory)){WImage(new Rect(pane.xMax-104,pane.y+18,60,60),"dress_up_check_button");if(Hit(new Rect(pane.xMax-112,pane.y+10,76,76),"dress-eye"))ToggleDressEye();}
            if(entries.Length==0){WImage(new Rect(pane.center.x-88,pane.center.y-69,176,138),"noinfo",ScaleMode.ScaleToFit);return;}
            if(dressEye){DrawDressPreview(pane,Draft(dressCategory));return;}
            var itemView=new Rect(pane.x+26,pane.y+112,pane.width-62,pane.height-136);float itemHeight=Mathf.Max(itemView.height,22+(Mathf.CeilToInt(entries.Length/4f)-1)*182+143);
            DressItemRail(itemView,itemHeight);DragCharacterPane(dressItemDrag,itemView,new Vector2(itemView.width,itemHeight),ref dressItemScroll,false);
            GUI.BeginGroup(itemView);offset=targetOffset;targetOffset+=itemView.position;
            for(int i=0;i<entries.Length;i++){
                var item=entries[i];var card=new Rect(33+i%4*178,11+i/4*182-dressItemScroll.y,143,143);if(card.yMax<0||card.y>itemView.height)continue;CImage(card,"kuangbg");DressImage(new Rect(card.x+3,card.y+3,card.width-6,card.height-6),item.icon,ScaleMode.ScaleAndCrop);CImage(card,"kuangbound");
                if(Draft(dressCategory)?.id==item.id)DressCorners(new Rect(card.x-5,card.y-5,card.width+10,card.height+10));
                if(Hit(new Rect(card.x-5,card.y-5,card.width+10,card.height+10),"dress-item-"+item.id)){dressDraft[dressCategory]=item.id;StopDressPreview();}
                if(dressCategory=="hand")Label(new Rect(card.x-15,card.yMax+6,card.width+30,38),item.name,CText(25,TextAnchor.MiddleCenter,true),CharacterGold);
            }
            targetOffset=offset;GUI.EndGroup();
        }
        void DressItemRail(Rect view,float content){
            float max=Mathf.Max(0,content-view.height);if(max<=0)return;var rail=new Rect(view.xMax+7,view.y+6,6.4f,view.height-12);var hit=new Rect(rail.x-17,rail.y-8,40,rail.height+16);var ev=Event.current;int control=GUIUtility.GetControlID("dress-item-rail".GetHashCode(),FocusType.Passive);
            float top=rail.y+Mathf.Clamp01(dressItemScroll.y/max)*(rail.height-34);
            if(GUI.enabled&&ev.type==EventType.MouseDown&&hit.Contains(ev.mousePosition)){dressItemRailDragging=true;dressItemRailGrab=Mathf.Abs(ev.mousePosition.y-top-17)<25?ev.mousePosition.y-top:17;GUIUtility.hotControl=control;}
            if(dressItemRailDragging){if(ev.type==EventType.MouseDown||ev.type==EventType.MouseDrag){dressItemScroll.y=max*Mathf.Clamp01((ev.mousePosition.y-rail.y-dressItemRailGrab)/(rail.height-34));ev.Use();}else if(ev.rawType==EventType.MouseUp){dressItemRailDragging=false;GUIUtility.hotControl=0;ev.Use();}}
            CImage(rail,"ss_slidebar");CImage(new Rect(rail.center.x-17,rail.y+Mathf.Clamp01(dressItemScroll.y/max)*(rail.height-34),34,34),"ss_slidebtn");
        }
        void DrawDressPreview(Rect pane,WardrobeItem item){
            var area=new Rect(pane.x+24,pane.y+110,pane.width-48,pane.height-132);
            var caption=new Rect(area.x+20,area.yMax-91,area.width-40,72);
            var close=new Rect(area.xMax-66,area.y+20,38,38);WImage(close,"preview_close",ScaleMode.ScaleToFit);if(Hit(new Rect(close.x-7,close.y-7,52,52),"dress-preview-close")){dressEye=false;StopDressPreview();return;}
            if(item.category=="headFrame"){
                var head=new Rect(area.center.x-93,area.center.y-112,186,186);var role=CharacterRoster.Active;var t=CharacterRoster.ListPortrait(CharacterRoster.Find(role.character),role.skin);CImage(head,"kuangbg");if(t!=null)GUI.DrawTexture(new Rect(head.x-1,head.y-1,head.width+2,head.height+2),t,ScaleMode.ScaleAndCrop,true);
                if(item.id=="headframe_default")CImage(head,"kuangbound");else DressImage(WardrobeCatalog.AvatarFrameRect(head,item),item.texture);
            }else if(item.category=="lobbyBackground"){
                var picture=new Rect(area.center.x-240,area.y+44,480,480);DressImage(picture,string.IsNullOrEmpty(item.preview)?item.texture:item.preview);caption=new Rect(picture.center.x-300,picture.y+464.5f,600,100);
            }else if(item.category=="cloth"||item.category=="tileBack"||item.category=="tileFace"){
                var board=new Rect(area.center.x-240,area.y+42,480,480);var cloth=Draft("cloth");DressImage(board,string.IsNullOrEmpty(cloth.preview)?cloth.texture:cloth.preview);
                var strip=Draft(item.category=="tileFace"?"tileFace":"tileBack");DressImage(new Rect(board.x+9.75f,board.y+11.1f,229.5f,69),strip.preview);caption=new Rect(board.center.x-300,board.y+464.5f,600,100);
            }else if(item.category=="music"){
                DressImage(new Rect(area.center.x-112,area.center.y-144,224,224),item.icon);Label(new Rect(area.x+30,area.center.y+108,area.width-60,48),"试听中",CText(28),CharacterGold);
            }else if(dressMotion.Image!=null)dressMotion.Draw(new Rect(area.center.x-265,area.y+8,530,530));
            Label(caption,item.name,CText(32,TextAnchor.MiddleCenter,true),CharacterGold);
        }
#if UNITY_EDITOR
        public void PreviewWardrobe(string category="stick"){PreviewDorm("yiji",400101);OpenWardrobe();SelectDressCategory(category);ReviewSettleDorm();}
        public void PreviewWardrobeCategory(string category)=>SelectDressCategory(category);
        public void PreviewWardrobeAge(float age){dressReviewAge=age;}
        public string PreviewWardrobeDiagnostics()=>dressMotion.Describe();
        public void PreviewWardrobeGridOffset(float y){dressItemScroll.y=y;}
        public float PreviewWardrobeGridScroll=>dressItemScroll.y;
        public void PreviewWardrobeForRole(string role,int skin,string category="hand"){PreviewDorm(role,skin);OpenWardrobe();SelectDressCategory(category);ReviewSettleDorm();}
#endif
    }
}

