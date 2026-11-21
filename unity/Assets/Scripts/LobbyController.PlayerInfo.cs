using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi {
    public sealed partial class LobbyController {
        const string PlayerInfoModal = "playerinfo";
        static readonly string[] RankArt = { "fish", "queshi", "quejie", "quehao", "quesheng", "huntian" };
        static readonly string[] InfoTabs = { "ranked", "friend", "competition" };
        static readonly string[] InfoModeNames = { "段位場", "友人場", "活動場" };
        static readonly int[] TrendCounts = { 10, 20, 50 };
        static readonly Color InfoText = new Color(.86f,.86f,.92f), InfoMuted = new Color(.55f,.58f,.68f), InfoAccent = new Color(.98f,.67f,.18f);
        readonly Dictionary<string,Texture2D> profileArt = new Dictionary<string,Texture2D>();
        int infoPlayers=4, infoTab, trendCount=50;
        string infoDialog="", renameText="", renameError="", radarKey="", trendKey="";
        float infoDialogStarted,trendBegan;
        Texture2D radarTexture, trendTexture;
        Vector2 titleScroll;
        readonly NativeScroll playerInfoScroll = new NativeScroll();
        [Serializable] class ProfileBorder { public string name; public float[] border; }
        [Serializable] class ProfileBorders { public ProfileBorder[] entries; }
        ProfileBorders profileBorders;
        List<Rect> profilePortraitClips;
        Font profileNumberFont;
        Font profileSourceBrush,profileAchievementFont;
        GUIStyle ProfileSourceLabel(int size) {
            profileSourceBrush ??= Resources.Load<Font>("Majsoul/Characters/Fonts/fengyucumaokai");
            return new GUIStyle(text){font=profileSourceBrush,fontSize=size,alignment=TextAnchor.MiddleCenter,wordWrap=false};
        }
        GUIStyle ProfileAchievementNumber() {
            profileAchievementFont ??= Resources.Load<Font>("Majsoul/Profile/Fonts/haolong_0");
            return new GUIStyle(text){font=profileAchievementFont,fontSize=42,alignment=TextAnchor.MiddleLeft,wordWrap=false};
        }
        GUIStyle ProfileNumber(int size,TextAnchor align=TextAnchor.MiddleCenter) {
            profileNumberFont ??= Resources.Load<Font>("Majsoul/Characters/Fonts/HYWenHei-75W");
            return new GUIStyle(text){font=profileNumberFont,fontSize=size,alignment=align,wordWrap=false};
        }
        static Color ProfileRgb(int rgb)=>new Color(((rgb>>16)&255)/255f,((rgb>>8)&255)/255f,(rgb&255)/255f);
        void ProfileTint(Rect r,string key,Color tint) {
            var color=GUI.color;GUI.color*=tint;ProfileImage(r,key,ScaleMode.StretchToFill);GUI.color=color;
        }

        Texture2D ProfileArt(string key) {
            if(!profileArt.TryGetValue(key,out var texture)) profileArt[key]=texture=Resources.Load<Texture2D>("Majsoul/Profile/"+key);
            return texture;
        }
        void ProfileImage(Rect r,string key,ScaleMode mode=ScaleMode.ScaleToFit) {
            var texture=ProfileArt(key);
            if(texture!=null)GUI.DrawTexture(r,texture,mode,true);
        }
        void ProfileSlice(Rect r,string key) {
            var texture=ProfileArt("Native/"+key);
            if(texture==null)return;
            profileBorders ??= JsonUtility.FromJson<ProfileBorders>(Resources.Load<TextAsset>("Majsoul/Profile/native-art").text);
            var border=profileBorders.entries.FirstOrDefault(item=>item.name==key)?.border;
            if(border==null||border.All(value=>value==0)){GUI.DrawTexture(r,texture);return;}
            float left=Mathf.Min(border[0],r.width/2), bottom=Mathf.Min(border[1],r.height/2);
            float right=Mathf.Min(border[2],r.width/2), top=Mathf.Min(border[3],r.height/2);
            float[] xs={r.x,r.x+left,r.xMax-right,r.xMax}, ys={r.y,r.y+top,r.yMax-bottom,r.yMax};
            float[] us={0,border[0]/texture.width,1-border[2]/texture.width,1};
            float[] vs={0,border[3]/texture.height,1-border[1]/texture.height,1};
            for(int y=0;y<3;y++)for(int x=0;x<3;x++)
                GUI.DrawTextureWithTexCoords(Rect.MinMaxRect(xs[x],ys[y],xs[x+1],ys[y+1]),texture,
                    new Rect(us[x],1-vs[y+1],us[x+1]-us[x],vs[y+1]-vs[y]));
        }
        List<Rect> ProfilePortraitClips() {
            if(profilePortraitClips!=null)return profilePortraitClips;
            var mask=ProfileArt("Native/waitingroom_mask");
            var pixels=mask.GetPixels32();
            profileBorders ??= JsonUtility.FromJson<ProfileBorders>(Resources.Load<TextAsset>("Majsoul/Profile/native-art").text);
            var border=profileBorders.entries.First(item=>item.name=="waitingroom_mask").border;
            const float w=448,h=957;
            profilePortraitClips=new List<Rect>();
            // UI_PlayerInfo uses a sliced waitingroom_mask, with hidden mask
            // graphics. Reproduce its alpha-test silhouette as merged GUI clips.
            float TargetX(float x)=>x<border[0]?x:x>mask.width-border[2]?w-(mask.width-x):
                border[0]+(x-border[0])*(w-border[0]-border[2])/(mask.width-border[0]-border[2]);
            for(int y=0;y<h;y++) {
                float sample=y+.5f;
                float sourceY=sample<border[3]?sample:sample>h-border[1]?mask.height-(h-sample):
                    border[3]+(sample-border[3])*(mask.height-border[3]-border[1])/(h-border[3]-border[1]);
                int row=(mask.height-1-Mathf.Clamp(Mathf.FloorToInt(sourceY),0,mask.height-1))*mask.width;
                int left=0,right=mask.width;
                while(left<right&&pixels[row+left].a==0)left++;
                while(right>left&&pixels[row+right-1].a==0)right--;
                if(left==right)continue;
                float x=TargetX(left),width=TargetX(right)-x;
                if(profilePortraitClips.Count>0) {
                    var last=profilePortraitClips[profilePortraitClips.Count-1];
                    if(last.x==x&&last.width==width&&last.yMax==y) {
                        last.height++;profilePortraitClips[profilePortraitClips.Count-1]=last;continue;
                    }
                }
                profilePortraitClips.Add(new Rect(x,y,width,1));
            }
            return profilePortraitClips;
        }
        void DrawProfilePortrait(Rect area,int skin) {
            foreach(var clip in ProfilePortraitClips()) {
                GUI.BeginGroup(new Rect(area.x+clip.x,area.y+clip.y,clip.width,clip.height));
                CharacterFraming.DrawSkinCard(new Rect(-clip.x,-clip.y,area.width,area.height),skin,true);
                GUI.EndGroup();
            }
        }
        Rect ProfileRect(float x,float y,float w,float h)=>new Rect(width/2-960+x,y,w,h);
        public void OpenPlayerInfo(){infoDialog="";infoTab=0;trendKey="";playerInfoScroll.Offset=0;OpenModal(PlayerInfoModal);}
        Rect PlayerInfoFrame=>ProfileRect(40,14,1836,1052);
        Rect InfoDialogRect=>infoDialog=="rank"?new Rect(width/2-560,52,1120,976):infoDialog=="titles"?ProfileRect(259.35f,179.4f,1314.94f,841.55f):infoDialog=="rename"?ProfileRect(534.02f,267.73f,835.48f,550.12f):new Rect(width/2-520,250,1040,560);
        Rect PlayerInfoBounds=>infoDialog==""?PlayerInfoFrame:InfoDialogRect;
        void PlayerInfoDismiss(){if(infoDialog!=""){infoDialog="";Cue("btn_cancel");}else CloseModal();}
        bool PlayerInfoBack(){if(Modal!=PlayerInfoModal||infoDialog=="")return false;infoDialog="";Cue("btn_cancel");return true;}
        void OpenInfoDialog(string name){infoDialog=name;infoDialogStarted=Now;titleScroll=Vector2.zero;renameError="";if(name=="rename")renameText="";Cue("btn_appear");}

        bool InfoButton(Rect r,string value,string id,bool selected=false) {
            var visual=ControlRect(r,id,advancedStyle.buttonClickScale);
            ProfileSlice(visual,"smallButtonBg");
            ControlLabel(new Rect(visual.x+9,visual.y+7,visual.width-18,visual.height-14),r,value,NText("profile-heading",40),selected?CharacterGold:ProfileRgb(0x5e83af));
            if(!Hit(r,id))return false;
            Pulse(id);return true;
        }
        void InfoHeading(Rect r,string value) {
            ProfileImage(new Rect(r.x,r.y+27,242,28),"Native/ClassTitleBg",ScaleMode.StretchToFill);
            Label(new Rect(r.x+11,r.y,r.width-11,50),value,NText("profile-heading",40,TextAnchor.MiddleLeft),CharacterGold);
        }
        // Draws a rank badge with stars (tiers) or the 魂天 level digits.
        void RankBadge(Rect r,int level,int players){
            ProfileImage(r,"Rank/"+(players==3?"sanma_":"sima_")+RankArt[RankRules.Tier(level)]);
            if(RankRules.Soul(level)){
                string digits=(level-RankRules.SoulLevel+1).ToString();float h=r.height*.2f,w=h*26/36f,lv=h*41/36f,x=r.center.x-(lv+digits.Length*w)/2,y=r.yMax-h*.9f;
                ProfileImage(new Rect(x,y,lv,h),"Rank/Lv");x+=lv;foreach(char c in digits){ProfileImage(new Rect(x,y,w,h),"Rank/ht_level_"+c);x+=w;}
            }
        }
        void RankStars(Rect r,int level){
            if(RankRules.Soul(level))return;int stars=RankRules.Stars(level);
            for(int i=0;i<3;i++)ProfileImage(new Rect(r.x+(i==2?101.25f:i*49.5f),r.y,49.5f,47.7f),i<stars?"Rank/star":"Rank/starbg");
        }
        static void Fill(Rect r,Color c,float radius=0){GUI.DrawTexture(r,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,c,0,radius);}
        static void Outline(Rect r,Color c,float width,float radius){GUI.DrawTexture(r,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,c,width,radius);}

        // Coordinates are transcribed from UI_PlayerInfo.prefab on its 1920x1080 canvas.
        void DrawPlayerInfo() {
            NativePanel.ScreenShade(new Color(0,0,0,.35f));
            var savedMatrix=GUI.matrix;var savedColor=GUI.color;bool wasEnabled=GUI.enabled;
            if(infoDialog!="")GUI.enabled=false;
            var frame=PlayerInfoFrame;
            float age=Now-(modalClosing?closeStarted:modalStarted);string clip=modalClosing?"ui_popout":"ui_pop";
            float pop=Mathf.Max(.001f,Sample(clip,3,0,age,1)),alpha=Mathf.Clamp01(Sample(clip,1574349066,0,age,1));
            GUI.matrix*=Matrix4x4.TRS(new Vector3(frame.center.x*(1-pop),frame.center.y*(1-pop)),Quaternion.identity,new Vector3(pop,pop,1));
            GUI.color*=new Color(1,1,1,alpha);
            DrawProfileFrame(frame,ProfileRect(62,32,1796,1016));
            ProfileImage(ProfileRect(1788,58.5f,44,43),"Native/pop2_close",ScaleMode.StretchToFill);
            if(Hit(ProfileRect(1760,30,100,100),"closeModal"))CloseModal();
            ProfileTint(ProfileRect(600,51.525f,4,976.95f),"Native/seperateLine1",new Color(1,1,1,.298f));
            var portrait=ProfileRect(85.5f,61.5f,448,957);
            ProfileImage(ProfileRect(84.5f,59,450,962),"Native/illustBg",ScaleMode.StretchToFill);
            var active=CharacterRoster.Active;var skin=CharacterRoster.Skin(active.character,active.skin);
            DrawProfilePortrait(portrait,skin.id);
            ProfileSlice(ProfileRect(77.5f,51.5f,464,976),"illustFrame");
            DrawProfileIdentity();
            DrawProfileTabs();
            DrawProfileAchievement();
            var mode=PlayerRecord.Mode(InfoTabs[infoTab],infoPlayers);
            var view=ProfileRect(675,379.5f,1128,627);
            ProfileSlice(ProfileRect(665,364.5f,1180,650),"PageBg");
            const float contentHeight=655;
            playerInfoScroll.Input(view,contentHeight);
            var offset=targetOffset;
            GUI.BeginGroup(view);
            GUI.BeginGroup(new Rect(0,-playerInfoScroll.Offset,1128,contentHeight));
            targetOffset=offset+view.position-new Vector2(0,playerInfoScroll.Offset);
            DrawFeaturedHand(new Rect(0,0,1128,210),mode);
            DrawStyle(new Rect(25,210,480,220),mode);
            DrawHighlights(new Rect(524.1f,210,580,220),mode);
            DrawTrend(new Rect(25,430,1075,225),mode);
            GUI.EndGroup();GUI.EndGroup();targetOffset=offset;
            NativePanel.Scrollbar(view.xMax+18,view.y,view.height,playerInfoScroll.Offset,contentHeight);
            var id=ProfileRect(1492.95f,1025,339,46);
            ProfileSlice(id,"IDBg");
            Label(new Rect(id.x+8,id.y,id.width-49,id.height),"我的ID："+PlayerRecord.Data.playerId,ProfileNumber(28),ProfileRgb(0xe8af71));
            ProfileImage(new Rect(id.xMax-36,id.y+8,30,30),"Native/IconCopy");
            if(Hit(id,"infoCopy")){GUIUtility.systemCopyBuffer=PlayerRecord.Data.playerId;ShowInfoNotice("已复制 ID");}
            if(infoNotice!=""&&Now-infoNoticeAt<1.6f)Label(new Rect(id.x,id.y-40,id.width,35),infoNotice,NText("profile-body",24),Color.white);
            GUI.matrix=savedMatrix;GUI.color=savedColor;GUI.enabled=wasEnabled;
            if(infoDialog!="")DrawInfoDialog();
        }
        string infoNotice="";float infoNoticeAt;
        void ShowInfoNotice(string value){infoNotice=value;infoNoticeAt=Now;}
        void DrawProfileFrame(Rect frame,Rect background) {
            Slice(background,"Sprites/Necessary/bg");
            Image(new Rect(background.x,background.yMax-107,247,107),"Sprites/Necessary/decorate_left");
            Image(new Rect(background.xMax-396,background.yMax-104,396,104),"Sprites/Necessary/decorate_right");
            Slice(frame,"Sprites/Necessary/frame",90,false);
        }
        void DrawProfileAchievement() {
            ProfileSlice(ProfileRect(665,220,1180,70),"PageBg");
            Label(ProfileRect(675.6f,228.15f,150,53.7f),"成就：",ProfileSourceLabel(40),ProfileRgb(0x5e83af));
            ProfileTint(ProfileRect(835,235,5,40),"Native/seperateLine2",new Color(1,1,1,.3f));
            string[] names={"trophy_gold","trophy_sliver","trophy_copper"};
            float[] xs={960.45f,1225.05f,1490.15f};
            for(int i=0;i<3;i++) {
                ProfileTint(ProfileRect(xs[i],230,52.5f,50),"Native/"+names[i],new Color(1,1,1,.55f));
                Label(ProfileRect(xs[i]+70,230,160,50),"—",ProfileAchievementNumber(),ProfileRgb(0x73798c));
            }
            ProfileTint(ProfileRect(1747,228,40,54),"Native/iconGoToAchievement",new Color(1,1,1,.35f));
            if(Hit(ProfileRect(665,220,1180,70),"infoAchievement"))ShowInfoNotice("成就统计暂不可用");
        }

        void DrawProfileIdentity() {
            PlayerTitles.DrawTitle(ProfileRect(670,60,339,65),PlayerRecord.Title,text);
            ProfileSlice(ProfileRect(671.5f,131,336,76),"nameBg");
            Label(ProfileRect(679,149.5f,321,39),client.LobbyNickname,NText("profile-body",30),Color.white);
            if(InfoButton(ProfileRect(1032,56,210,76),"查看称号","infoTitles"))OpenInfoDialog("titles");
            if(InfoButton(ProfileRect(1032,132,210,76),"更改昵称","infoRename"))OpenInfoDialog("rename");
            var rank=PlayerRecord.Rank(infoPlayers);
            RankBadge(ProfileRect(1610.9f,58,153.6f,153.6f),rank.level,infoPlayers);
            var target=ProfileRect(1410,50,360,180);
            if(Hit(target,"infoRank"))OpenInfoDialog("rank");
            RankStars(ProfileRect(1445.8f,77.65f,49.5f,47.7f),rank.level);
            Label(ProfileRect(1431,128.8f,174.5f,32.76f),RankRules.Progress(rank),ProfileNumber(30),ProfileRgb(0x62a9da));
            ProfileImage(ProfileRect(1446.7f,159.8f,143.2f,21),"Native/bg-exp",ScaleMode.StretchToFill);
            float fraction=Mathf.Clamp01(rank.points/(float)RankRules.Promote(rank.level));
            if(fraction>0)GUI.DrawTextureWithTexCoords(ProfileRect(1446.7f,159.8f,143.2f*fraction,21),ProfileArt("Native/rankexp"),new Rect(0,0,fraction,1));
        }
        void DrawProfileTabs() {
            for(int i=0;i<2;i++) {
                int players=i==0?4:3;
                var r=ProfileRect(546.8f,235+i*202,110,208);
                var art=new Rect(r.x+5,r.y,100,r.height);
                ProfileImage(art,"Native/tabBgNormal",ScaleMode.StretchToFill);
                if(infoPlayers==players)ProfileImage(art,"Native/tabBgSelected",ScaleMode.StretchToFill);
                ProfileImage(art,"Native/"+(players==4?"iconInfo4":"iconInfo3"),ScaleMode.StretchToFill);
                if(Hit(r,"infoPlayers"+players)&&infoPlayers!=players){infoPlayers=players;trendKey="";playerInfoScroll.Offset=0;}
            }
            for(int i=0;i<3;i++) {
                var r=ProfileRect(i==0?694.8f:i==1?902.8f:1109.8f,i==1?295.4f:294.2f,225,76);
                ProfileSlice(r,i==infoTab?"tabInsideSelected":"tabInsideNormal");
                Label(new Rect(r.x+21,r.y+15,174,46),InfoModeNames[i],ProfileSourceLabel(40),i==infoTab?ProfileRgb(0xedb26f):ProfileRgb(0x9ba1b2));
                if(Hit(r,"infoTab"+i)&&infoTab!=i){infoTab=i;trendKey="";playerInfoScroll.Offset=0;}
            }
        }
        void DrawFeaturedHand(Rect r,ModeRecord mode) {
            InfoHeading(new Rect(23.7f,3.3f,300,50),"精选和牌");
            float editX=35.5f+NText("profile-heading",40).CalcSize(new GUIContent("精选和牌")).x;
            ProfileImage(new Rect(editX,3.3f,50,50),"Native/IconEdit");
            if(Hit(new Rect(editX-10,-6.7f,70,70),"infoFeaturedHelp"))OpenInfoDialog("featured");
            var best=mode?.best;
            if(best==null||!best.Exists) {
                ProfileSlice(new Rect(35,70,1052,116.5f),"emptyBg");
                ProfileImage(new Rect(972.5f,103.75f,101,75),"Native/emptyCat");
                Label(new Rect(226,97.35f,670,61.8f),"暂无记录，请加油",ProfileNumber(30),ProfileRgb(0x73798c));return;
            }
            string background=best.limit!=null&&best.limit.Contains("役满")?"bigHubg3":"bigHuBg2";
            ProfileImage(new Rect(24,20,1080,180),"Native/"+background,ScaleMode.StretchToFill);
            ResultPresentation.DrawLimit(new Rect(829,4.9f,260,62),best.limit);
            DrawProfileHand(best);
        }
        void DrawProfileHand(BestHand best) {
            var table=GetComponent<TableRenderer>();if(table==null)return;
            var concealed=best.tiles??Array.Empty<string>();var melds=best.melds??Array.Empty<string>();
            bool hasMelds=melds.Length>0;
            float count=concealed.Length+melds.Sum(m=>m.Skip(1).Count(char.IsDigit))+(hasMelds?.5f:0)+1.5f;
            float scale=Mathf.LerpUnclamped(.92f,.7f,(count-14)/5);
            scale=Mathf.Clamp(scale,.5f,.92f);
            float pitch=80*scale,x=35.2f+.5f*scale,y=134.5f-67*scale;
            void Tile(string value){var texture=table.HandTexture(value);if(texture!=null)GUI.DrawTexture(new Rect(x,y,pitch,120*scale),texture,ScaleMode.StretchToFill,true);x+=pitch;}
            foreach(var tile in concealed)Tile(tile);
            if(hasMelds)x+=pitch*.5f;
            foreach(var meld in melds) {
                var digits=meld.Skip(1).Where(char.IsDigit).ToArray();bool closed=digits.Length==4&&!meld.Any(ch=>ch=='+'||ch=='-'||ch=='=');
                for(int i=0;i<digits.Length;i++)Tile(closed&&(i==0||i==3)?null:meld[0].ToString()+digits[i]);
            }
            x+=pitch*.5f;Tile(best.tile);
        }
        void DrawStyle(Rect r,ModeRecord mode) {
            InfoHeading(new Rect(r.x,r.y+3.6f,280,50),"风格属性");
            float helpX=r.x+11.8f+NText("profile-heading",40).CalcSize(new GUIContent("风格属性")).x;
            var help=new Rect(helpX,r.y+3.6f,50,50);ProfileImage(help,"Native/iconHelp");
            if(Hit(help,"infoStyleHelp"))OpenInfoDialog("style");
            var style=PlayerRecord.Style(mode?.totals);int[] values={style.attack,style.defense,style.speed,style.luck};
            string[] names={"攻","防","速","运"};Color[] colors={ProfileRgb(0xff637d),ProfileRgb(0x46d277),ProfileRgb(0x39b9be),ProfileRgb(0xbe7b39)};
            for(int i=0;i<4;i++) {
                float row=i==2?65.9f:i==3?97.9f:i*32.7f;
                Label(new Rect(r.x+24.5f,r.y+65.1f+row,28,35),names[i],ProfileNumber(28,TextAnchor.MiddleLeft),colors[i]);
                Label(new Rect(r.x+68.5f,r.y+65.1f+row,70,35),values[i].ToString(),ProfileNumber(28,TextAnchor.MiddleLeft),ProfileRgb(0xd2f2f2));
            }
            var chart=new Rect(r.x+235,r.y+35.4f,160,160);
            ProfileImage(chart,"Native/radarBg");GUI.DrawTexture(chart,Radar(values),ScaleMode.ScaleToFit,true);
            var axis=ProfileNumber(24);
            Label(new Rect(chart.center.x-40,chart.y-26.75f,80,38),"攻",axis,colors[0]);
            Label(new Rect(chart.center.x-40,chart.y+148.79f,80,38),"防",axis,colors[1]);
            Label(new Rect(chart.x-75.8f,chart.center.y-19,80,38),"速",ProfileNumber(24,TextAnchor.MiddleRight),colors[2]);
            Label(new Rect(chart.x+157.1f,chart.center.y-19,80,38),"运",ProfileNumber(24,TextAnchor.MiddleLeft),colors[3]);
        }
        void DrawHighlights(Rect r,ModeRecord mode) {
            var h=mode?.highlights??new HighlightCounts();
            var badges=new[]{(3,h.yakuman),(4,h.kazoe),(1,h.sanbaiman),(5,h.baiman),(6,h.nagashi),(2,h.doubleRiichi)}.Where(item=>item.Item2>0).ToArray();
            InfoHeading(new Rect(r.x,r.y+2.5f,300,50),"高光时刻");
            if(badges.Length==0)return;
            for(int i=0;i<badges.Length;i++) {
                float pitch=badges.Length<=5?114:94;
                var icon=new Rect(r.x+15.9f+i*pitch,r.y+66.4f,90,90);
                ProfileImage(icon,"Native/"+badges[i].Item1);
                Label(new Rect(icon.x-5,icon.y+87.5f,100,36),badges[i].Item2.ToString(),ProfileNumber(26),ProfileRgb(0xd2f2f2));
            }
        }
        void DrawTrend(Rect r,ModeRecord mode) {
            InfoHeading(new Rect(24,r.y,300,50),"最近走势");
            int index=Array.IndexOf(TrendCounts,trendCount);if(index<0)index=2;
            var counter=ProfileNumber(26);string caption="最近"+trendCount+"战";
            float captionWidth=counter.CalcSize(new GUIContent(caption)).x;
            var plusHit=new Rect(1059.25f,r.y-4,60,60);
            var minusHit=new Rect(plusHit.x-captionWidth-60,r.y-4,60,60);
            var minus=new Rect(minusHit.x+10,r.y+6,40,40);var plus=new Rect(plusHit.x+10,r.y+6,40,40);
            ProfileImage(minus,"Native/"+(index>0?"trendBtnMinus":"trendBtnMinusGray"));
            ProfileImage(plus,"Native/"+(index<2?"trendBtnAdd":"trendBtnAddGray"));
            Label(new Rect(minusHit.xMax,r.y+6,captionWidth,40),caption,counter,ProfileRgb(0xd4cfcf));
            if(Hit(minusHit,"infoTrendLess",index>0))trendCount=TrendCounts[index-1];
            if(Hit(plusHit,"infoTrendMore",index<2))trendCount=TrendCounts[index+1];
            var plot=new Rect(100.1f,r.y+76,1000,infoPlayers==4?102:68);
            for(int i=0;i<infoPlayers;i++) {
                Label(new Rect(35.5f,plot.y+i*34-20,50,40),(i+1)+"位",ProfileNumber(24),ProfileRgb(0xd2f2f2));
                Fill(new Rect(plot.x,plot.y+i*34,plot.width,1),new Color(1,1,1,.06f));
            }
            var games=(mode?.games??new List<MatchEntry>()).Where(game=>game!=null&&game.place>=1&&game.place<=infoPlayers).ToList();
            games=games.Skip(Mathf.Max(0,games.Count-trendCount)).ToList();
            if(games.Count==0)return;
            string key=infoPlayers+":"+trendCount+":"+string.Join(",",games.Select(game=>game.place));
            if(trendTexture==null)trendTexture=new Texture2D(1040,142,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            if(key!=trendKey) {
                trendKey=key;trendBegan=Now;var pixels=new Color32[1040*142];
                float dx=trendCount==10?110:trendCount==20?52:20.2f;
                Vector2 Point(int i)=>new Vector2(10+i*dx,120-(games[i].place-1)*34);
                void Dot(Vector2 at,float radius,Color32 color) {
                    for(int y=Mathf.FloorToInt(at.y-radius);y<=Mathf.CeilToInt(at.y+radius);y++)
                        for(int x=Mathf.FloorToInt(at.x-radius);x<=Mathf.CeilToInt(at.x+radius);x++)
                            if(x>=0&&x<1040&&y>=0&&y<142&&(new Vector2(x,y)-at).sqrMagnitude<=radius*radius)pixels[y*1040+x]=color;
                }
                for(int i=1;i<games.Count;i++) {
                    Vector2 a=Point(i-1),b=Point(i);int count=Mathf.CeilToInt(Vector2.Distance(a,b)*2);
                    for(int j=0;j<=count;j++)Dot(Vector2.Lerp(a,b,j/(float)Mathf.Max(1,count)),1.1f,new Color32(214,133,68,255));
                }
                trendTexture.SetPixels32(pixels);trendTexture.Apply(false);
            }
            float spacing=trendCount==10?110:trendCount==20?52:20.2f;
            float stepsPerLine=Mathf.Ceil(1/(trendCount*.02f));
            float revealed=Mathf.Min(games.Count-1,Mathf.Floor((Now-trendBegan)*60)/stepsPerLine);
            var savedOffset=targetOffset;
            var lineRect=new Rect(plot.x-10,plot.y-22,Mathf.Min(1040,10+spacing*revealed),142);
            GUI.BeginGroup(lineRect);targetOffset+=lineRect.position;
            GUI.DrawTexture(new Rect(0,0,1040,142),trendTexture,ScaleMode.StretchToFill,true);
            GUI.EndGroup();targetOffset=savedOffset;
            for(int i=0;i<games.Count&&i<=Mathf.FloorToInt(revealed);i++) {
                bool excellent=games[i].place==1&&games[i].score>(infoPlayers==4?50000:70000);
                float size=excellent?36:10;
                ProfileImage(new Rect(plot.x+i*spacing-size/2,plot.y+(games[i].place-1)*34-size/2,size,size),"Native/"+(excellent?"trendPointCat":"trendPoint"),ScaleMode.StretchToFill);
            }
        }
        // Rasterized once per value set: IMGUI cannot fill polygons directly.
        Texture2D Radar(int[] v){
            string key=string.Join(",",v);if(radarTexture!=null&&key==radarKey)return radarTexture;radarKey=key;
            const int S=144;if(radarTexture==null)radarTexture=new Texture2D(S,S,TextureFormat.RGBA32,false){name="Profile style radar",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var px=new Color32[S*S];float c=(S-1)/2f,R=c-6;
            // Axes: 攻 up, 运 right, 防 down, 速 left (texture rows grow upward).
            Vector2[] outer={new Vector2(c,c+R),new Vector2(c+R,c),new Vector2(c,c-R),new Vector2(c-R,c)};
            float[] f={v[0]/100f,v[3]/100f,v[1]/100f,v[2]/100f};var inner=new Vector2[4];for(int i=0;i<4;i++)inner[i]=Vector2.Lerp(new Vector2(c,c),outer[i],Mathf.Max(.03f,f[i]));
            bool Inside(Vector2 p,Vector2[] poly){bool r=false;for(int i=0,j=3;i<4;j=i++)if((poly[i].y>p.y)!=(poly[j].y>p.y)&&p.x<(poly[j].x-poly[i].x)*(p.y-poly[i].y)/(poly[j].y-poly[i].y)+poly[i].x)r=!r;return r;}
            float Edge(Vector2 p,Vector2[] poly){float d=float.MaxValue;for(int i=0,j=3;i<4;j=i++){var a=poly[j];var b=poly[i];var ab=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);d=Mathf.Min(d,Vector2.Distance(p,a+ab*t));}return d;}
            for(int y=0;y<S;y++)for(int x=0;x<S;x++){
                var p=new Vector2(x,y);var col=new Color(0,0,0,0);
                if(Inside(p,inner))col=new Color(1,.78f,.18f,1);
                if(Edge(p,inner)<1.4f)col=new Color(1,.89f,.42f,1);
                px[y*S+x]=col;
            }
            radarTexture.SetPixels32(px);radarTexture.Apply(false);return radarTexture;
        }
        void DrawInfoDialog(){
            NativePanel.ScreenShade(new Color(0,0,0,.3f));
            var r=InfoDialogRect;float age=Now-infoDialogStarted;float scale=Mathf.Max(.001f,Sample("ui_pop",3,0,age,1));
            var matrix=GUI.matrix;var color=GUI.color;GUI.matrix=GUI.matrix*Matrix4x4.TRS(new Vector3(r.center.x*(1-scale),r.center.y*(1-scale)),Quaternion.identity,new Vector3(scale,scale,1));
            GUI.color=new Color(color.r,color.g,color.b,color.a*Mathf.Clamp01(Sample("ui_pop",1574349066,0,age,1)));
            if(infoDialog=="titles")DrawProfileFrame(r,ProfileRect(282.4f,197.49f,1274.01f,805.82f));
            else if(infoDialog=="rename")DrawProfileFrame(r,ProfileRect(558.13f,286.19f,793.48f,513.14f));
            else NativePanel.Frame(r);
            var close=infoDialog=="titles"?ProfileRect(1494.5f,217,42,42):infoDialog=="rename"?ProfileRect(1289.181f,306.3f,42,42):new Rect(r.xMax-86,r.y+44,42,42);
            Image(close,"Sprites/Necessary/btn_close",ScaleMode.ScaleToFit);
            if(Hit(new Rect(close.x-22,close.y-22,86,86),"infoDialogClose")){infoDialog="";Cue("btn_cancel");}
            var heading=ReferenceBrush(44);
            if(infoDialog=="rank")DrawRankTable(r,heading);
            else if(infoDialog=="titles")DrawTitleBook(r,heading);
            else if(infoDialog=="style")DrawStyleHelp(r,heading);
            else if(infoDialog=="rename")DrawRename(r,heading);
            else if(infoDialog=="featured") {
                Label(new Rect(r.x,r.y+30,r.width,70),"精选和牌",heading,CharacterGold);
                Label(new Rect(r.x+80,r.y+150,r.width-160,200),"自动显示本场别已记录的最佳和牌。\n四麻、三麻与各场别分别保存。",NText("profile-body",30,TextAnchor.MiddleLeft),InfoText);
            }
            GUI.matrix=matrix;GUI.color=color;
        }
        void DrawRankTable(Rect r,GUIStyle heading){
            bool three=infoPlayers==3;Label(new Rect(r.x,r.y+30,r.width,70),(three?"三麻":"四麻")+"段位说明",heading,CharacterGold);
            string[] head={"段位级别","初始分","升级分",(three?"第三名":"第四名")+"减分"};float[] col={230,190,190,250};float tx=r.center.x-col.Sum()/2,ty=r.y+104,row=41;
            var cell=NText("profile-body",24);var cur=PlayerRecord.Rank(infoPlayers);
            for(int i=-1;i<15;i++){
                float rx=tx;var bg=new Rect(tx,ty+(i+1)*row,col.Sum(),row-2);Fill(bg,i==-1?new Color(.16f,.2f,.3f):i==cur.level?new Color(.35f,.28f,.12f):new Color(.1f,.13f,.2f));
                string[] v=i<0?head:new[]{RankRules.Tiers[i/3]+(i%3+1)+"星",RankRules.Start(i).ToString(),RankRules.Promote(i).ToString(),RankRules.LastPenalty(i,infoPlayers,false)+" / "+RankRules.LastPenalty(i,infoPlayers,true)};
                for(int k=0;k<4;k++){Label(new Rect(rx,bg.y,col[k],bg.height),v[k],cell,i<0?CharacterGold:InfoText);rx+=col[k];}
            }
            string soul=three?"东风 1/2/3 名：魂珠 +0.3/0/−0.3\n＊半庄 1/2/3 名：魂珠 +0.5/0/−0.5":"东风 1/2/3/4 名：魂珠 +0.3/+0.1/−0.1/−0.3\n＊半庄 1/2/3/4 名：魂珠 +0.5/+0.2/−0.2/−0.5";
            int[] e=three?new[]{RankRules.Placement(0,1,3,1),RankRules.Placement(0,2,3,1)}:new[]{RankRules.Placement(0,1,4,1),RankRules.Placement(0,2,4,1),RankRules.Placement(0,3,4,1)};
            int[] s=three?new[]{RankRules.Placement(0,1,3,2),RankRules.Placement(0,2,3,2)}:new[]{RankRules.Placement(0,1,4,2),RankRules.Placement(0,2,4,2),RankRules.Placement(0,3,4,2)};
            string place="＊段位分 = 顺位分 + (终局点数 − 起始点数) ÷ 1000；顺位分 东风 "+string.Join("/",e.Select(x=>(x>0?"+":"")+x))+"、半庄 "+string.Join("/",s.Select(x=>(x>0?"+":"")+x))+"，末位按上表减分\n";
            Label(new Rect(tx-20,ty+16*row+8,col.Sum()+40,200),place+"＊魂天各 Lv 初始分为 10 魂珠，升级分为 20 魂珠\n＊"+soul+"\n＊同桌玩家均为魂天时魂珠变动翻倍",NText("profile-body",22,TextAnchor.UpperLeft,true),InfoMuted);
        }
        void DrawTitleBook(Rect r,GUIStyle heading) {
            Label(ProfileRect(803.5f,200.5f,240,76),"称号簿",ReferenceBrush(42),ProfileRgb(0xe8af71));
            var titles=PlayerTitles.All.Where(item=>PlayerRecord.Owns(item.id)&&item.condition!="default").OrderByDescending(item=>item.priority).ToArray();
            var view=ProfileRect(307.3f,296.3f,1222,671);
            const float rowHeight=99.2f;
            float total=titles.Length*rowHeight;
            var evt=Event.current;
            if(evt.type==EventType.MouseDrag&&view.Contains(evt.mousePosition)) {titleScroll.y=Mathf.Clamp(titleScroll.y-evt.delta.y,0,Mathf.Max(0,total-view.height));evt.Use();}
            titleScroll=GUI.BeginScrollView(view,titleScroll,new Rect(0,0,view.width,total),false,false,GUIStyle.none,GUIStyle.none);
            var prior=targetOffset;targetOffset+=view.position-titleScroll;
            for(int i=0;i<titles.Length;i++) {
                var entry=titles[i];var row=new Rect(0,i*rowHeight,1218,rowHeight);
                PlayerTitles.DrawTitle(new Rect(47.52f,row.y+5.87f,314.16f,60.42f),entry.id,text);
                Label(new Rect(421.2f,row.y+9.2f,703.5f,78),entry.description,NText("profile-body",32,TextAnchor.MiddleLeft),ProfileRgb(0xcfcfcf));
                ProfileImage(new Rect(16,row.y+94.6f,1204,6),"Title/title_spilit",ScaleMode.StretchToFill);
                if(PlayerRecord.Title==entry.id)ProfileImage(new Rect(1012,row.y-10.4f,151,115),"Native/using");
                if(Hit(row,"infoTitle-"+entry.id)) {PlayerRecord.Equip(PlayerRecord.Title==entry.id?PlayerTitles.Default:entry.id);client.PublishRankProfile();}
            }
            targetOffset=prior;GUI.EndScrollView();
            if(titles.Length==0)Label(ProfileRect(378.444f,340,1113.2f,76),"暂无已获得的称号",ProfileNumber(28),ProfileRgb(0xe8af71));
        }

        void DrawStyleHelp(Rect r,GUIStyle heading){
            Label(new Rect(r.x,r.y+30,r.width,70),"风格属性说明",heading,CharacterGold);
            Label(new Rect(r.x+80,r.y+120,r.width-160,r.height-160),"统计本页签与模式下全部已记录对局，每项 0–100：\n攻：和牌率与平均和牌打点\n防：放铳率越低越高\n速：平均和牌巡目越早越高\n运：自摸比例与立直后里宝命中率\n没有和牌时，速与运为 0。",NText("profile-body",28,TextAnchor.UpperLeft,true),InfoText);
        }
        void DrawRename(Rect r,GUIStyle heading){
            Label(ProfileRect(705,311,520,53),"更改昵称",ReferenceBrush(42),ProfileRgb(0xe8af71));
            var field=ProfileRect(661,426,585,111);
            Slice(ProfileRect(644,424,622,111),"Sprites/Common/inputbox");
            var edit=new GUIStyle(GUIStyle.none){font=Resources.Load<Font>("Majsoul/Characters/Fonts/SimHei"),fontSize=36,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(8,8,0,0)};
            edit.normal.textColor=edit.focused.textColor=edit.active.textColor=edit.hover.textColor=Color.white;
            renameText=GUI.TextField(field,renameText??"",7,edit);
            if(string.IsNullOrEmpty(renameText))Label(new Rect(field.x+8,field.y,field.width-16,field.height),"昵称（1–7字）",NText("profile-body",36,TextAnchor.MiddleLeft),Color.white);
            Label(ProfileRect(646.189f,543.435f,619.07f,36.53f),string.IsNullOrEmpty(renameError)?"昵称（1–7 字）":renameError,ProfileNumber(25),string.IsNullOrEmpty(renameError)?ProfileRgb(0x5e83af):new Color(.95f,.45f,.4f));
            var confirm=ProfileRect(753.519f,664.595f,404,102.81f);var visual=ControlRect(confirm,"infoRenameSave",advancedStyle.buttonClickScale);
            NativeUiChrome.Button(visual,true);ControlLabel(visual,confirm,"我取好了",ReferenceBrush(52),new Color(.42745f,.18039f,.22745f));
            if(Hit(confirm,"infoRenameSave")){
                if(PlayerRecord.Rename(renameText,out renameError)){client.LobbyNickname=renameText.Trim();infoDialog="";}
            }
        }
    }
}


