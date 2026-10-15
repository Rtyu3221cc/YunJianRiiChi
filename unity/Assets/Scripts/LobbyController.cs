using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace YunJian.Riichi
{
    // This component owns all non-playing screens. It never selects a tile,
    // sends a game action, changes a table camera or changes a scoring rule.
    public sealed class LobbyController : MonoBehaviour
    {
        public enum ScreenPage { Home, Modes, Create, Room }
        public ScreenPage Page { get; private set; }
        public string Category { get; private set; }="friend";
        public int Players { get; private set; }=4;
        public int Rounds { get; private set; }=2;
        public bool Red { get; private set; }=true;
        public bool Advanced { get; private set; }
        public string Modal { get; private set; }="";
        public bool Searching { get; private set; }
        public bool Visible=>client!=null&&!client.LobbyPlaying;
        public bool CanStart=>client.State?.phase=="lobby"&&client.State.host&&client.State.players.Length==client.State.n&&(!(client.State.lobby?.managed??false)||client.State.players.All(p=>p.ready));
        RiichiClient client;
        readonly Dictionary<string,Texture2D> art=new Dictionary<string,Texture2D>();
        readonly Dictionary<string,Rect> targets=new Dictionary<string,Rect>();
        Vector2 targetOffset;
        readonly LanRoomDiscovery discovery=new LanRoomDiscovery();
        CancellationTokenSource search;
        AudioSource music,sounds;
        Font brush,body;
        GUIStyle text,title,small,input;
        MotionBank motion;
        BorderBank borders;
        AdvancedStyle advancedStyle;
        GUIStyle numberText,optionText,descriptionText;
        readonly Dictionary<string,float> controlPulses=new Dictionary<string,float>();
        string advancedHeading="",advancedExplanation="",editingNumber="",editingNumberKey="";
        float width=1920,age,started,modalStarted,nextFly;
        int flyCount,kickSeat=-1,ruleMode,ruleTab;
        string number="",manualAddress="",recovery="",notice="",help="",profileName="",kickName="";
        int friendHelpKind;
        Vector2 ruleScroll;
        Vector2 roomRuleScroll;
        bool roomRuleDragging;
        Vector2 roomRulePress;
        bool connectionSettings,wasVisible;
        bool modalClosing;
        float closeStarted;
        Vector2 scroll;
        bool scrollDragging;
        Vector2 scrollPress;
        ScreenPage outgoing;
        float transitionStarted=-10;
        [Serializable] class MotionBank { public MotionClip[] animations; }
        [Serializable] class MotionClip { public string name; public float duration; public MotionTrack[] tracks; }
        [Serializable] class MotionTrack { public uint path,attribute;public int part;public float[] values; }
        [Serializable] class BorderBank {public BorderEntry[] borders;}
        [Serializable] class BorderEntry {public string key;public float[] values;}
        [Serializable] class AdvancedStyle {public float rowHeight,numberWidth,numberHeight,optionWidth,optionHeight,clickScale,buttonClickScale,helpSize,buttonFade;public int numberFontSize,descriptionFontSize,optionFontSize;}
        public void Initialize(RiichiClient owner)
        {
            client=owner;brush=Resources.Load<Font>("Majsoul/Lobby/Fonts/brush");body=Resources.Load<Font>("Majsoul/Font/game");
            motion=JsonUtility.FromJson<MotionBank>(Resources.Load<TextAsset>("Majsoul/Lobby/motion").text);
            borders=JsonUtility.FromJson<BorderBank>(Resources.Load<TextAsset>("Majsoul/Lobby/borders").text);
            advancedStyle=JsonUtility.FromJson<AdvancedStyle>(Resources.Load<TextAsset>("Majsoul/Lobby/advanced-style").text);
            music=gameObject.AddComponent<AudioSource>();sounds=gameObject.AddComponent<AudioSource>();
            music.playOnAwake=sounds.playOnAwake=false;music.loop=true;music.spatialBlend=sounds.spatialBlend=0;
            music.clip=Resources.Load<AudioClip>("Majsoul/Lobby/Audio/lobby");
            discovery.Start();Home();
        }
        void Update()
        {
            if(client==null)return;discovery.Publish(client.State);
            if(modalClosing&&Time.unscaledTime-closeStarted>=.15f){Modal="";modalClosing=false;}
            bool visible=Visible;
            if(visible&&!wasVisible)Home();
            if(!visible){music.Stop();sounds.Stop();if(Searching)CancelSearch();}
            else {
                bool bgm=PlayerPrefs.GetInt("riichi.musicMuted",0)==0;
                music.volume=PlayerPrefs.GetFloat("riichi.musicVolume",.35f);
                if(!bgm)music.Pause();else if(music.clip!=null&&!music.isPlaying){music.UnPause();if(!music.isPlaying)music.Play();}
                if(Page==ScreenPage.Home&&flyCount<1&&Time.unscaledTime>=nextFly){Cue("flyin");flyCount++;}
                if(Input.GetKeyDown(KeyCode.Escape))Back();
            }
            wasVisible=visible;
            if(client.State?.phase=="lobby"&&client.LobbyActive&&Page!=ScreenPage.Room){CancelSearch();Modal="";Go(ScreenPage.Room);}
            if(Page==ScreenPage.Room&&(!client.LobbyActive||client.State?.phase!="lobby")){Home();}
        }
        public void Home()
        {
            CancelSearch();Modal="";modalClosing=false;Advanced=false;scroll=Vector2.zero;Go(ScreenPage.Home);
            flyCount=0;nextFly=Time.unscaledTime+.06f;
        }
        void Go(ScreenPage next)
        {
            outgoing=Page;Page=next;started=Time.unscaledTime;transitionStarted=started;
        }
        public void OpenCategory(string value){Category=value;Go(ScreenPage.Modes);Cue("normalflyin");}
        public void OpenCreate(){Advanced=true;scroll=Vector2.zero;Go(ScreenPage.Create);Cue("btn_appear");}
        public void OpenModal(string name){Modal=name;modalClosing=false;modalStarted=Time.unscaledTime;notice="";Cue("btn_appear");}
        public void CloseModal(){if(Modal==""||modalClosing)return;CancelSearch();modalClosing=true;closeStarted=Time.unscaledTime;Cue("btn_cancel");}
        public void Back()
        {
            if(client.Busy)return;
            if(client.LobbyCloseSettings())return;
            if(Modal!=""){CloseModal();return;}
            if(Page==ScreenPage.Room){OpenModal("leave");return;}
            Home();Cue("btn_cancel");
        }
        void CancelSearch(){search?.Cancel();search?.Dispose();search=null;Searching=false;}
        void Cue(string name)
        {
            if(sounds==null||PlayerPrefs.GetInt("riichi.effectsMuted",0)!=0)return;
            var clip=Resources.Load<AudioClip>("Majsoul/Lobby/Audio/"+name);if(clip!=null)sounds.PlayOneShot(clip,PlayerPrefs.GetFloat("riichi.effectsVolume",.8f));
        }
        float Sample(string name,uint attribute,int part,float time,float fallback=1,int trackIndex=0)
        {
            var clip=motion?.animations.FirstOrDefault(c=>c.name==name);var track=clip?.tracks.Where(t=>t.attribute==attribute&&t.part==part).Skip(trackIndex).FirstOrDefault();
            if(track==null||track.values.Length==0)return fallback;
            float f=Mathf.Clamp(time*60,0,track.values.Length-1);int i=(int)f;return Mathf.Lerp(track.values[i],track.values[Mathf.Min(i+1,track.values.Length-1)],f-i);
        }
        Texture2D Art(string key)
        {
            if(!art.TryGetValue(key,out var texture)){texture=Resources.Load<Texture2D>("Majsoul/Lobby/"+key);art[key]=texture;}
            return texture;
        }
        void Image(Rect r,string key,ScaleMode mode=ScaleMode.StretchToFill)
        {var t=Art(key);if(t!=null)GUI.DrawTexture(r,t,mode,true);}
        static void Tint(Rect r,Color c){var old=GUI.color;GUI.color=new Color(c.r*old.r,c.g*old.g,c.b*old.b,c.a*old.a);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        void Slice(Rect r,string key,float edge=18,bool center=true)
        {
            var t=Art(key);if(t==null)return;
            var native=borders?.borders.FirstOrDefault(b=>b.key==key)?.values??new[]{edge,edge,edge,edge};
            if(native.All(v=>v==0)){Image(r,key);return;}
            float l=Mathf.Min(native[0],r.width/3),b=Mathf.Min(native[1],r.height/3),rr=Mathf.Min(native[2],r.width/3),tt=Mathf.Min(native[3],r.height/3);
            float[] xs={r.x,r.x+l,r.xMax-rr,r.xMax},ys={r.y,r.y+tt,r.yMax-b,r.yMax},us={0,native[0]/t.width,1-native[2]/t.width,1},vs={0,native[3]/t.height,1-native[1]/t.height,1};
            for(int y=0;y<3;y++)for(int x=0;x<3;x++)if(center||x!=1||y!=1)GUI.DrawTextureWithTexCoords(Rect.MinMaxRect(xs[x],ys[y],xs[x+1],ys[y+1]),t,new Rect(us[x],1-vs[y+1],us[x+1]-us[x],vs[y+1]-vs[y]));
        }
        void Styles()
        {
            if(text!=null)return;
            text=new GUIStyle{font=body,fontSize=32,alignment=TextAnchor.MiddleCenter,wordWrap=true,clipping=TextClipping.Overflow};text.normal.textColor=Color.white;
            title=new GUIStyle(text){font=brush,fontSize=52};title.normal.textColor=new Color(.94f,.76f,.44f);
            small=new GUIStyle(text){fontSize=24};
            input=new GUIStyle(GUI.skin.textField){font=body,fontSize=32,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(12,12,5,5)};input.normal.background=null;input.focused.background=null;input.normal.textColor=input.focused.textColor=Color.white;
            foreach(var state in new[]{input.normal,input.hover,input.active,input.focused,input.onNormal,input.onHover,input.onActive,input.onFocused}){state.background=null;state.textColor=Color.white;}
            var advancedFont=Resources.Load<Font>("Majsoul/Lobby/Fonts/advanced");
            numberText=new GUIStyle(text){font=advancedFont,fontSize=advancedStyle.numberFontSize};
            optionText=new GUIStyle(text){font=advancedFont,fontSize=advancedStyle.optionFontSize};
            descriptionText=new GUIStyle(text){font=advancedFont,fontSize=advancedStyle.descriptionFontSize,alignment=TextAnchor.MiddleLeft};
        }
        void Label(Rect r,string value,GUIStyle style=null,Color? color=null)
        {
            var s=style??text;var old=s.normal.textColor;s.normal.textColor=color??old;
            // Background interaction is disabled while a modal is open; its
            // non-interactive captions retain the same opacity as native art.
            bool enabled=GUI.enabled;var contentColor=GUI.contentColor;GUI.enabled=true;GUI.contentColor=Color.white;
            GUI.Label(r,value,s);GUI.contentColor=contentColor;GUI.enabled=enabled;s.normal.textColor=old;
        }
        bool Hit(Rect r,string id,bool enabled=true)
        {
            var a=GUI.matrix.MultiplyPoint3x4(new Vector3(r.x+targetOffset.x,r.y+targetOffset.y));var b=GUI.matrix.MultiplyPoint3x4(new Vector3(r.xMax+targetOffset.x,r.yMax+targetOffset.y));targets[id]=Rect.MinMaxRect(a.x,a.y,b.x,b.y);
            bool was=GUI.enabled;GUI.enabled=was&&enabled&&!client.Busy&&!Searching;
            bool result=GUI.Button(r,GUIContent.none,GUIStyle.none);GUI.enabled=was;
#if UNITY_EDITOR
            if(validationPress==id&&was&&enabled&&!client.Busy&&!Searching){result=true;validationPress=null;}
#endif
            if(result)Cue("btn_click");return result;
        }
        bool Button(Rect r,string value,string id,bool selected=false,bool enabled=true)
        {
            var old=GUI.color;GUI.color=enabled?old:new Color(old.r*.65f,old.g*.65f,old.b*.65f,old.a);
            Slice(r,"Sprites/Create/room_rule_option_"+(selected?"select":"unselect"),14);
            Label(new Rect(r.x+8,r.y-3,r.width-16,r.height),value,optionText,selected?new Color(.1451f,.07451f,.07451f):Color.white);GUI.color=old;
            return Hit(r,id,enabled);
        }
        bool Gold(Rect r,string value,string id,bool enabled=true,float captionOffset=0)
        {
            Slice(r,enabled?"Sprites/Room/room_start_btn":"Sprites/Room/room_gray_btn",18);var caption=r;caption.y+=captionOffset;Label(caption,value,title,new Color(.16f,.08f,.04f));return Hit(r,id,enabled);
        }
        string CategoryName=>Category=="ranked"?"段位场":Category=="competition"?"比赛场":"友人场";
        void ReturnHeader(string value)
        {
            // UI_CreatRoom/top: 401x81 band, 45pt title scaled by 1.4.
            var decoration=new Rect(118,46.9f,401,81);
            Image(decoration,"Sprites/Main/img_return1_bg");Image(new Rect(12.4f,21.4f,135,134),"Sprites/Main/img_return1",ScaleMode.ScaleToFit);
            var headerTitle=new GUIStyle(title){fontSize=63,alignment=TextAnchor.MiddleLeft};
            Label(new Rect(172.6f,decoration.y,269.69f,decoration.height),value,headerTitle,new Color(.89412f,.72941f,.52549f));
            if(Hit(new Rect(5,5,514,155),"back"))Back();
        }
        void Header(string value)
        {
            ReturnHeader(value);
            Image(new Rect(width-260,15,120,130),"Sprites/Main/btn_rule_desc",ScaleMode.ScaleToFit);
            if(Hit(new Rect(width-260,15,120,130),"rules"))OpenRules();
        }
        public void Draw()
        {
            Styles();targets.Clear();age=Time.unscaledTime-started;
            var saved=GUI.matrix;var color=GUI.color;bool enabled=GUI.enabled;
            // Fill the physical screen proportionally, then lay controls within
            // the safe rectangle using one uniform scale. No stretched art.
            GUI.matrix=Matrix4x4.identity;Image(new Rect(0,0,Screen.width,Screen.height),Page==ScreenPage.Room?"indoor":"yard",ScaleMode.ScaleAndCrop);
            var safe=Screen.safeArea;float scale=Mathf.Min(safe.height/1080f,safe.width/1920f);width=safe.width/scale;
            GUI.matrix=Matrix4x4.TRS(new Vector3(safe.x,Screen.height-safe.yMax+(safe.height-1080*scale)/2),Quaternion.identity,new Vector3(scale,scale,1));
            bool modal=Modal!="";GUI.enabled=enabled&&!modal&&!client.LobbyOverlayVisible;
            if(Page==ScreenPage.Home||Page==ScreenPage.Modes)DrawGarden();
            else if(Page==ScreenPage.Create)DrawCreate();else DrawRoom();
            GUI.enabled=enabled;
            if(modal){GUI.enabled=enabled&&!modalClosing;DrawModal();}
            GUI.matrix=saved;GUI.color=color;GUI.enabled=enabled;
        }
        void DrawGarden()
        {
            float enter=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.45f));
            var old=GUI.color;GUI.color=new Color(1,1,1,Page==ScreenPage.Home?enter:1);
            float portraitH=1370;Image(new Rect(width*.265f-portraitH*.657f/2,125+(1-enter)*75,portraitH*.657f,portraitH),"yiji-full",ScaleMode.ScaleToFit);
            GUI.color=old;
            DrawProfileBadge();
            Image(new Rect(width-122,28,88,88),"Sprites/Main/btn_set",ScaleMode.ScaleToFit);
            if(Hit(new Rect(width-133,17,110,110),"settings"))client.LobbyOpenSettings();
            // The camera sprite has less transparent padding than the gear.
            // 76px gives both round silhouettes the same visible diameter.
            Image(new Rect(width-116,138,76,76),"Sprites/Main/btn_priscreen",ScaleMode.ScaleToFit);
            if(Hit(new Rect(width-133,121,110,110),"capture")){string file=System.IO.Path.Combine(Application.persistentDataPath,"yunjian-lobby-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".png");ScreenCapture.CaptureScreenshot(file);help="大厅截图已保存。";OpenModal("notice");}
            Image(new Rect(35,970,92,92),"Sprites/Main/btn_roleset",ScaleMode.ScaleToFit);
            if(Hit(new Rect(25,960,115,115),"role")){help="当前雀士：一姬";OpenModal("help");}
            string[] bottom={"btn1","btn4","btn0","btn2"};
            const float bottomScale=1.15f,iconWidth=134*bottomScale,iconHeight=130*bottomScale,pitch=140*bottomScale;
            float bottomStart=width-66-(3*pitch+iconWidth),bottomTop=1060-iconHeight;
            for(int i=0;i<4;i++){
                var r=new Rect(bottomStart+i*pitch,bottomTop,iconWidth,iconHeight);Image(r,"Sprites/Main/"+bottom[i],ScaleMode.ScaleToFit);
                if(Hit(r,"bottom"+i)){help=i==0?"当前雀士：一姬":i==1?"与同一热点内的牌友创建或加入房间。":"功能暂未开放";OpenModal("help");}
            }
            float x=width-980;
            if(Page==ScreenPage.Home){
                string[] signs={"btn_yibanchang","btn_dajiangsai","btn_yourenchang"};string[] categories={"ranked","competition","friend"};
                const float signScale=1.15f;
                for(int i=0;i<3;i++){
                    float t=Mathf.Clamp01((age-i*.13f)/.35f),offset=(1-Mathf.SmoothStep(0,1,t))*450;
                    // Enlarge the complete sign group about its original centre,
                    // including the pitch so the three artworks stay separated.
                    var r=new Rect(x+offset-630*(signScale-1)/2,542.5f+(i-1)*195*signScale-205*signScale/2,630*signScale,205*signScale);var c=GUI.color;GUI.color=new Color(c.r,c.g,c.b,c.a*t);Image(r,"Sprites/Main/"+signs[i],ScaleMode.ScaleToFit);GUI.color=c;
                    if(Hit(r,categories[i],t>.95f))OpenCategory(categories[i]);
                }
            }else{
                float ty=Sample("lobby_title_in",538195251,0,age,338.63f)-338.63f;
                Image(new Rect(x-5,218-ty*.6f,635,80),"Sprites/Main/bg_back0");
                Image(new Rect(x+10,228-ty*.6f,138,55),"Sprites/Main/img_return2",ScaleMode.ScaleToFit);
                Label(new Rect(x+160,223-ty*.6f,380,70),CategoryName,title);
                if(Hit(new Rect(x,210,620,90),"modeBack"))Back();
                DrawModeCards(x,age,false);
            }
            if(Page==ScreenPage.Home&&outgoing==ScreenPage.Modes&&age<.333f){
                var c=GUI.color;GUI.color=new Color(c.r,c.g,c.b,c.a*Sample("lobby_title_out",1574349066,0,age,0));
                float ty=Sample("lobby_title_out",538195251,0,age,400)-338.63f;
                Image(new Rect(x-5,218-ty*.6f,635,80),"Sprites/Main/bg_back0");Label(new Rect(x+160,223-ty*.6f,380,70),CategoryName,title);GUI.color=c;
                DrawModeCards(x,age,true);
            }
        }
        void DrawProfileBadge()
        {
            // UI_Lobby/lefttop: preserve the background, icon and decoration
            // coordinates rather than centring the icon in the whole nameplate.
            Image(new Rect(28,14,472,178),"Sprites/Main/bg_2");
            var rank=Resources.Load<Texture2D>("Majsoul/Review1012c/sima_queshi");if(rank!=null)GUI.DrawTexture(new Rect(50.1f,38.2f,127,127),rank,ScaleMode.ScaleToFit);
            Image(new Rect(12.6f,99.6f,46,46),"Sprites/Necessary/starbg");Image(new Rect(39.9f,143.8f,36,36),"Sprites/Necessary/starbg");Image(new Rect(73.8f,162.4f,28,28),"Sprites/Necessary/star");
            Image(new Rect(139.3f,123.6f,92,92),"Sprites/Main/bg_6");Image(new Rect(154.4f,136.5f,60,60),"Sprites/Common/sanma_fish",ScaleMode.ScaleToFit);
            var noTitle=Resources.Load<Texture2D>("Majsoul/TableLook/Sprites/notitle");
            if(noTitle!=null)GUI.DrawTexture(new Rect(191,48.9f,240,44),noTitle,ScaleMode.ScaleToFit,true);
            var nameBounds=new Rect(196,100.5f,230,46);
            var playerName=new GUIStyle(text){fontSize=26,wordWrap=false,clipping=TextClipping.Clip,alignment=TextAnchor.MiddleCenter};
            float nameWidth=playerName.CalcSize(new GUIContent(client.LobbyNickname)).x;
            if(nameWidth>nameBounds.width-8)playerName.fontSize=Mathf.FloorToInt(26*(nameBounds.width-8)/nameWidth);
            Label(nameBounds,client.LobbyNickname,playerName,Color.white);
            Image(new Rect(424.4f,55.4f,85,85),"Sprites/Main/selfinfo",ScaleMode.ScaleToFit);
            if(Hit(new Rect(5,5,510,215),"profile")){profileName=client.LobbyNickname;OpenModal("profile");}
        }
        void DrawModeCards(float x,float time,bool leaving)
        {
                string[] labels={"w_createroom","w_joinroom"};
                const float k=615f/547f;
                for(int i=0;i<2;i++){
                    float native=Sample(leaving?"friend_room_out":"friend_room_in",538195251,0,time,i==0?85:251,i);float delta=(native-(i==0?85:251))*.6f;
                    var old=GUI.color;if(leaving)GUI.color=new Color(old.r,old.g,old.b,old.a*Sample("friend_room_out",1574349066,0,time,0));
                    // UI_Lobby/page2: 547x186 card; title 360x120 at (90,27).
                    // node_desc uses 42pt x .8; d_time strikes the first two words.
                    var r=new Rect(x,330+i*166*k-delta,615,186*k);Image(r,"Sprites/Main/bg_yourenchang");
                    Image(new Rect(r.x+90*k,r.y+27*k,360*k,120*k),"Sprites/Main/"+labels[i]);
                    var freeStyle=new GUIStyle(title){font=Resources.Load<Font>("Majsoul/Lobby/Fonts/room-footer"),fontSize=Mathf.RoundToInt(42*.8f*k)};
                    Label(new Rect(r.x+205*k,r.y+137*k,178*.8f*k,51*.8f*k),"限時免費",freeStyle,new Color(.92549f,.89804f,.84706f));
                    Image(new Rect(r.x+207*k,r.y+147*k,91*.7f*k,35*.7f*k),"Sprites/Main/d_time");
                    var info=new Rect(r.x+467*k,r.y+33*k,50*k,50*k);Image(info,"Sprites/Main/tips",ScaleMode.ScaleToFit);
                    if(!leaving&&Hit(info,"help"+i))OpenFriendHelp(i);
                    if(!leaving&&Hit(new Rect(r.x,r.y,r.width-85,r.height),"mode"+i)){if(i==0)OpenCreate();else{number="";connectionSettings=false;OpenModal("join");}}
                    GUI.color=old;
                }
        }
        void CreateLabel(float y,string key,string fallback)
        {
            Image(new Rect(width/2-650,y+10,250,62),"Sprites/Create/"+key,ScaleMode.ScaleToFit);
            if(Art("Sprites/Create/"+key)==null)Label(new Rect(width/2-650,y,250,80),fallback,title);
        }
        void BasicCreateLabel(float center,string key)
        {
            // Keep the native icon and lettering in separate, aligned columns.
            Image(new Rect(0,center-37.5f,75,75),"Sprites/Create/room_create_"+key+"_icon",ScaleMode.ScaleToFit);
            Image(new Rect(96,center-33,194,66),"Sprites/Create/room_create_"+key+"_text",ScaleMode.ScaleToFit);
        }
        void AdvancedDivider(float center)
        {
            // Retain the complete native double-line arrow and move its apex.
            var texture=Art("Sprites/Create/room_rule_advance_line");if(texture==null)return;
            const float splitLeft=590,splitRight=751,lineWidth=1450;
            float middle=(splitRight-splitLeft)*lineWidth/texture.width,left=center-middle/2,right=center+middle/2;
            GUI.DrawTextureWithTexCoords(new Rect(0,434,left,25),texture,new Rect(0,0,splitLeft/texture.width,1));
            GUI.DrawTextureWithTexCoords(new Rect(left,434,middle,25),texture,new Rect(splitLeft/texture.width,0,(splitRight-splitLeft)/texture.width,1));
            GUI.DrawTextureWithTexCoords(new Rect(right,434,lineWidth-right,25),texture,new Rect(splitRight/texture.width,0,1-splitRight/texture.width,1));
        }
        void DrawCreate()
        {
            Header("建立房间");
            float panelHeight=Advanced?700:454;
            var region=new Rect(0,195,width,panelHeight);Tint(region,new Color(.04f,.04f,.09f,.57f));
            Image(new Rect(0,182,width,10),"Sprites/Room/room_info_line1");Image(new Rect(0,195+panelHeight,width,10),"Sprites/Room/room_info_line1");
            float content=Advanced?479+8*advancedStyle.rowHeight+20:400;var view=new Rect(width/2-760,210,1520,panelHeight-45);
            float max=Mathf.Max(0,content-view.height);int scrollId=GUIUtility.GetControlID(410131,FocusType.Passive);var ev=Event.current;
            if(ev.type==EventType.MouseDown&&view.Contains(ev.mousePosition)){scrollPress=ev.mousePosition;scrollDragging=false;}
            if(ev.type==EventType.MouseDrag&&max>0&&(scrollDragging||view.Contains(scrollPress)&&Mathf.Abs(ev.mousePosition.y-scrollPress.y)>12)){
                scrollDragging=true;GUIUtility.hotControl=scrollId;scroll.y=Mathf.Clamp(scroll.y-ev.delta.y,0,max);ev.Use();
            }
            if(ev.type==EventType.MouseUp&&scrollDragging){scrollDragging=false;GUIUtility.hotControl=0;ev.Use();}
            scroll.y=Mathf.Clamp(scroll.y,0,max);
            scroll=GUI.BeginScrollView(view,scroll,new Rect(0,0,1475,content),false,false,GUIStyle.none,GUIStyle.none);
            targetOffset=view.position-scroll;
            float x=350;
            BasicCreateLabel(46,"match");
            if(Button(new Rect(x,5,245,82),"四人麻将","four",Players==4))Players=4;
            if(Button(new Rect(x+270,5,245,82),"三人麻将","three",Players==3))Players=3;
            BasicCreateLabel(151,"jushu");
            if(Button(new Rect(x,110,245,82),"东风","east",Rounds==1))Rounds=1;
            if(Button(new Rect(x+270,110,245,82),"半庄","south",Rounds==2))Rounds=2;
            BasicCreateLabel(255,"time");
            // The only available timing choice is permanently selected.
            Button(new Rect(x,214,245,82),"无限制","unlimited",true);
            BasicCreateLabel(359,"advance");
            if(Button(new Rect(x,318,245,82),"关闭","advancedOff",!Advanced)){Advanced=false;scroll=Vector2.zero;}
            if(Button(new Rect(x+270,318,245,82),"开启","advancedOn",Advanced))Advanced=true;
            if(Advanced){
                AdvancedDivider(x+270+122.5f);
                string[] keys={"room_create_start_point_text","room_create_point_text","room_create_jifei_text","room_create_guyi_text","room_create_hongbaopai_text","room_create_shiduan_text","room_create_fanfu_text","room_create_tip_text"};
                float[] labelWidths={195,256,191,195,186,189,188,191};
                for(int i=0;i<keys.Length;i++){
                    float y=479+i*advancedStyle.rowHeight;
                    Image(new Rect(290-labelWidths[i],y+13,labelWidths[i],66),"Sprites/Create/"+keys[i],ScaleMode.ScaleToFit);
                    if(i==0||i==1){
                        string key=i==0?"initial":"target",value=AdvancedNumber(key);
                        var fieldRect=new Rect(x,y+6,advancedStyle.numberWidth,advancedStyle.numberHeight);
                        var visual=ControlRect(fieldRect,"number-"+key,advancedStyle.buttonClickScale);
                        Slice(visual,"Sprites/Create/room_rule_advance_number_bg");ControlLabel(visual,fieldRect,value,numberText);
                        if(Hit(fieldRect,"number-"+key)){Pulse("number-"+key);OpenNumber(key,value);}
                        string desc=i==0?"标准规则：四人25000点，三人35000点":"标准规则：四人30000点，三人40000点\nAll Last 未达目标时按当前规则延长";
                        Label(new Rect(x+advancedStyle.numberWidth+28,y,810,100),desc,descriptionText,new Color(.87f,.87f,.87f));
                    }else{
                        string key=i==2?"bust":i==3?"ancient":i==4?"red":i==5?"kuitan":i==6?"minimum":"hints";
                        string[] labels=i==4?new[]{"无赤",Players==3?"2赤":"3赤",Players==3?"3赤":"4赤"}:i==6?new[]{"一番缚","二番缚","四番缚"}:new[]{"关闭","开启"};
                        int fixedChoice=i==3||i==6?0:i==4?(Red?1:0):1;
                        DrawAdvancedChoices(x,y,key,labels,fixedChoice);
                        if(i==5||i==7){
                            var q=new Rect((75-advancedStyle.helpSize)/2,y+(advancedStyle.optionHeight-advancedStyle.helpSize)/2,advancedStyle.helpSize,advancedStyle.helpSize);Image(ControlRect(q,"help-"+key,advancedStyle.buttonClickScale),"Sprites/Common/what",ScaleMode.ScaleToFit);
                            if(Hit(q,"help-"+key)){Pulse("help-"+key);AdvancedHelp(key);}
                        }
                    }
                }
            }
            GUI.EndScrollView();
            targetOffset=Vector2.zero;
            if(max>0){Image(new Rect(view.xMax-10,view.y,8,view.height),"Sprites/Necessary/match_scroll_bg");Image(new Rect(view.xMax-10,view.y+(view.height-view.height*view.height/content)*scroll.y/max,8,view.height*view.height/content),"Sprites/Necessary/match_scroll_btn");}
            if(Gold(new Rect(width/2-147.5f,Advanced?941.5f:699.5f,295,105),"创建","create"))client.LobbyCreate(Players,Rounds,Red,Category);
        }
        Rect ControlRect(Rect bounds,string id,float peak)
        {
            if(!controlPulses.TryGetValue(id,out float start))return bounds;
            float age=Time.unscaledTime-start;if(age>.22f)return bounds;
            float amount=age<.07f?Mathf.SmoothStep(0,1,age/.07f):1-Mathf.SmoothStep(0,1,(age-.07f)/.15f);
            float scale=Mathf.Lerp(1,peak,amount);return new Rect(bounds.center-bounds.size*scale/2,bounds.size*scale);
        }
        void Pulse(string id){controlPulses[id]=Time.unscaledTime;}
        void ControlLabel(Rect visual,Rect bounds,string value,GUIStyle style,Color? color=null){var scaled=new GUIStyle(style){fontSize=Mathf.RoundToInt(style.fontSize*visual.width/bounds.width)};Label(visual,value,scaled,color);}
        int AdvancedChoice(string key,int fallback)
        {
#if UNITY_EDITOR
            if(DesignPreviewInteractions&&designChoices.TryGetValue(key,out int value))return value;
#endif
            return fallback;
        }
        string AdvancedNumber(string key)
        {
#if UNITY_EDITOR
            if(DesignPreviewInteractions&&designNumbers.TryGetValue(key,out string value))return value;
#endif
            return key=="initial"?(Players==3?"35000":"25000"):(Players==3?"40000":"30000");
        }
        void DrawAdvancedChoices(float x,float y,string key,string[] labels,int fixedChoice)
        {
            int selected=AdvancedChoice(key,fixedChoice);
            for(int i=0;i<labels.Length;i++){
                string id="advanced-"+key+"-"+i;var r=new Rect(x+i*(advancedStyle.optionWidth+13),y,advancedStyle.optionWidth,advancedStyle.optionHeight);
                var visual=ControlRect(r,id,advancedStyle.clickScale);Image(visual,"Sprites/Create/room_rule_option_"+(i==selected?"select":"unselect"));ControlLabel(visual,r,labels[i],optionText,i==selected?new Color(.12f,.07f,.02f):Color.white);
                if(!Hit(r,id))continue;Pulse(id);
#if UNITY_EDITOR
                if(DesignPreviewInteractions){designChoices[key]=i;continue;}
#endif
                if(key=="red"&&i<2)Red=i==1;
                else if(i!=fixedChoice){advancedHeading="标准规则";advancedExplanation="当前房间的这一项固定为“"+labels[fixedChoice]+"”。";OpenModal("advanced-help");}
            }
        }
        void OpenNumber(string key,string value)
        {
#if UNITY_EDITOR
            if(DesignPreviewInteractions){editingNumberKey=key;editingNumber=value;OpenModal("advanced-number");return;}
#endif
            advancedHeading="标准点数";advancedExplanation=(key=="initial"?"初始点数":"一位目标点数")+"当前为 "+value+" 点。\n本房间使用标准规则，此项暂不可更改。";OpenModal("advanced-help");
        }
        void AdvancedHelp(string key)
        {
            advancedHeading=key=="kuitan"?"食断":"便捷提示";
            advancedExplanation=key=="kuitan"?"有食断时，\u201c断么九\u201d在副露的情况下算役\n无食断时，\u201c断么九\u201d只能在门前清的情况下算役":"开启时可查看已有的听牌提示和可听牌舍牌信息。";
            OpenModal("advanced-help");
        }
        void DrawRoom()
        {
            var state=client.State;if(state?.phase!="lobby")return;
            Category=state.lobby?.category??Category;DrawRoomHeader(state.pin);
            const float left=70,cardWidth=376,cardHeight=779,pitch=387;
            float rulesX=width-48-258.53f;
            float cardScale=Mathf.Min(1,(rulesX-left-42)/(state.n*cardWidth+(state.n-1)*(pitch-cardWidth)));
            for(int i=0;i<state.n;i++){
                var r=new Rect(left+i*pitch*cardScale,162,cardWidth*cardScale,cardHeight*cardScale);
                Image(r,"Sprites/Main/character_bg");
                if(i<state.players.Length){
                    var p=state.players[i];GUI.BeginGroup(new Rect(r.x+13.2f*cardScale,r.y+20.5f*cardScale,350.9f*cardScale,743.9f*cardScale));
                    float h=1060*cardScale,w=h*.657f;Image(new Rect((350.9f*cardScale-w)/2,-10,w,h),"yiji-full",ScaleMode.ScaleToFit);GUI.EndGroup();
                    Image(r,"Sprites/Main/frame_gold");
                    if(i==0)Image(new Rect(r.x+28*cardScale,r.y+24*cardScale,107*cardScale,100*cardScale),"Sprites/Room/roomowner",ScaleMode.ScaleToFit);
                    if(p.ready)Image(new Rect(r.x+150*cardScale,r.y+512*cardScale,213*cardScale,158*cardScale),"Sprites/Main/ready",ScaleMode.ScaleToFit);
                    if(!p.online&&!p.bot)RoomCentered(new Rect(r.x+15,r.y+500*cardScale,r.width-30,70),"重连中",new GUIStyle(descriptionText){fontSize=28},new Color(1,.7f,.4f));
                    DrawRoomName(r,p,cardScale);
                    if(state.host&&i>0){
                        var close=new Rect(r.xMax-69*cardScale,r.y+11*cardScale,74*cardScale,70*cardScale);Image(ControlRect(close,"kick"+i,advancedStyle.buttonClickScale),"Sprites/Main/btn_close",ScaleMode.ScaleToFit);
                        if(Hit(close,"kick"+i)){Pulse("kick"+i);client.LobbyCommand("kick",i);}
                    }
                }else{
                    Image(r,"Sprites/Main/frame_gold");
                    if(!state.host)continue;
                    var invite=new Rect(r.center.x-112.535f*cardScale,r.y+272.5f*cardScale,225.07f*cardScale,70.65f*cardScale);
                    var bot=new Rect(r.center.x-113.475f*cardScale,r.y+430.8f*cardScale,226.95f*cardScale,71.28f*cardScale);
                    if(RoomInviteButton(invite,"邀请好友","invite"+i,cardScale))OpenModal("invite");
                    if(RoomInviteButton(bot,"新增电脑","bot"+i,cardScale))client.LobbyCommand("addBot");
                }
            }
            DrawRoomRules(rulesX,state);
            var start=new Rect(width/2-147.5f,959.9f,295,105);
            if(state.host){if(RoomActionButton(start,"开始","start",CanStart))client.LobbyCommand("start");}
            else{bool ready=state.players.ElementAtOrDefault(state.you)?.ready??false;if(RoomActionButton(start,ready?"取消准备":"准备","ready",true))client.LobbyReady(!ready);}
        }
        void RoomCentered(Rect r,string value,GUIStyle style,Color color,float lift=0)
        {r.y-=lift;Label(r,value,style,color);}
        void DrawRoomHeader(string pin)
        {
            // Match the already reviewed create-room return band and caption.
            // Its caption is 10.9px lower than the previous room caption.
            ReturnHeader(CategoryName);
            Image(new Rect(width/2-230.5f,47.5f,447,85.26f),"Sprites/Main/title_bg");
            RoomCentered(new Rect(width/2-220,47.5f,369,85.26f),"房间号："+pin,new GUIStyle(title){fontSize=46},new Color(.88627f,.72941f,.57255f),6);
            Image(new Rect(width/2+154,55,70,70),"Sprites/Room/room_share_btn",ScaleMode.ScaleToFit);
            if(Hit(new Rect(width/2+148,49,82,82),"share")){GUIUtility.systemCopyBuffer=client.LobbyInviteText;OpenModal("invite");}
            Image(new Rect(width-330,24,121,119),"Sprites/Main/room_rule_btn",ScaleMode.ScaleToFit);
            if(Hit(new Rect(width-335,19,131,129),"rules"))OpenRules();
            var yaku=new Rect(width-174,24,124,118);Image(ControlRect(yaku,"yaku",advancedStyle.buttonClickScale),"Sprites/Main/btn_yi",ScaleMode.ScaleToFit);
            if(Hit(new Rect(width-179,19,134,128),"yaku")){Pulse("yaku");ruleScroll=Vector2.zero;OpenModal("yaku");}
        }
        bool RoomInviteButton(Rect r,string value,string id,float scale)
        {
            var visual=ControlRect(r,id,advancedStyle.buttonClickScale);Image(visual,"Sprites/Room/room_invite_btn");
            var caption=new GUIStyle(text){font=Resources.Load<Font>("Majsoul/Lobby/Fonts/room-caption"),fontSize=Mathf.RoundToInt(34*scale)};
            var textBounds=visual;textBounds.y-=3*scale;ControlLabel(textBounds,r,value,caption,new Color(.50196f,.57647f,.68235f));
            if(!Hit(r,id))return false;Pulse(id);return true;
        }
        bool RoomActionButton(Rect r,string value,string id,bool enabled)
        {
            var visual=ControlRect(r,id,advancedStyle.buttonClickScale);Image(visual,enabled?"Sprites/Room/room_start_btn":"Sprites/Room/room_gray_btn");
            var caption=visual;caption.y-=7;ControlLabel(caption,r,value,new GUIStyle(title){fontSize=50},new Color(.19608f,.19608f,.19608f));
            if(!Hit(r,id,enabled))return false;Pulse(id);return true;
        }
        void DrawRoomName(Rect card,PlayerView player,float scale)
        {
            // UI_FriendRoom/container_name: 828x176 at .6 scale. Restore its
            // rank child (.8 scale), rather than shrinking the badge separately.
            // Clip only the brush band at the inner bottom edge of the card.
            GUI.BeginGroup(new Rect(card.x,card.y+673*scale,496.8f*scale,98*scale));
            Image(new Rect(0,0,496.8f*scale,105.6f*scale),"Sprites/Common/namebg");GUI.EndGroup();
            var rankBg=Resources.Load<Texture2D>("Majsoul/Review1012c/rank_bg");if(rankBg!=null)GUI.DrawTexture(new Rect(card.x-13.44f*scale,card.y+667.06f*scale,117.6f*scale,109.44f*scale),rankBg,ScaleMode.ScaleToFit);
            Image(new Rect(card.x+11.23f*scale,card.y+684.29f*scale,76.8f*scale,76.8f*scale),"Sprites/Common/sanma_fish",ScaleMode.ScaleToFit);
            Image(new Rect(card.x-17.52f*scale,card.y+716.02f*scale,32.16f*scale,30.72f*scale),"Sprites/Necessary/"+(player.bot?"starbg":"star"));
            Image(new Rect(card.x+2.18f*scale,card.y+743.19f*scale,22.51f*scale,21.5f*scale),"Sprites/Necessary/"+(player.bot?"starbg":"star"));
            Image(new Rect(card.x+22.2f*scale,card.y+756.34f*scale,16.08f*scale,15.36f*scale),"Sprites/Necessary/star");
            var noTitle=Resources.Load<Texture2D>("Majsoul/TableLook/Sprites/notitle");if(noTitle!=null)GUI.DrawTexture(new Rect(card.x+91.8f*scale,card.y+640*scale,263.64f*scale,50.7f*scale),noTitle,ScaleMode.ScaleToFit,true);
            var nameStyle=new GUIStyle(text){fontSize=Mathf.RoundToInt(28.8f*scale),alignment=TextAnchor.MiddleLeft,wordWrap=false,clipping=TextClipping.Clip};
            string name=player.bot?"电脑（简单）":player.name;var bounds=new Rect(card.x+97.2f*scale,card.y+705.1f*scale,283.8f*scale,34.92f*scale);
            float pixels=nameStyle.CalcSize(new GUIContent(name)).x;if(pixels>bounds.width)nameStyle.fontSize=Mathf.FloorToInt(nameStyle.fontSize*bounds.width/pixels);
            Label(bounds,name,nameStyle,Color.white);
        }
        void RoomRuleLine(Rect r,bool down)
        {
            var texture=Art("Sprites/Create/room_rule_advance_line");if(texture==null)return;
            var color=GUI.color;GUI.color=new Color(color.r*.42f,color.g*.546f,color.b,color.a*.7f);
            // Original chevron art, recoloured to the room's grey-blue dividers.
            float l=590f/texture.width,m=161f/texture.width;float y=down?1:0,h=down?-1:1;
            GUI.DrawTextureWithTexCoords(new Rect(r.x,r.y,r.width/2-34,r.height),texture,new Rect(0,y,l,h));
            GUI.DrawTextureWithTexCoords(new Rect(r.center.x-34,r.y,68,r.height),texture,new Rect(l,y,m,h));
            GUI.DrawTextureWithTexCoords(new Rect(r.center.x+34,r.y,r.width/2-34,r.height),texture,new Rect(l+m,y,1-l-m,h));GUI.color=color;
        }
        void DrawRoomRules(float x,RoomView state)
        {
            const float ruleWidth=258.53f;Slice(new Rect(x-8,158,ruleWidth+16,772),"Sprites/Room/room_info_bg");
            Slice(new Rect(x,175,ruleWidth,124.8f),"Sprites/Room/room_info_advance_bg");
            var header=new GUIStyle(descriptionText){fontSize=30,alignment=TextAnchor.MiddleCenter};var muted=new Color(.80784f,.85098f,.92941f);
            Label(new Rect(x+5,182,ruleWidth-10,42),state.n==3?"三人麻将":"四人麻将",header,Color.white);
            var secondary=new GUIStyle(header){fontSize=28};
            Label(new Rect(x+5,225,ruleWidth-10,35),state.rounds==1?"东风":"半庄",secondary,muted);
            Label(new Rect(x+5,262,ruleWidth-10,35),"无限制",secondary,muted);
            RoomRuleLine(new Rect(x+3,307,252,13),false);RoomRuleLine(new Rect(x+3,907,252,13),true);
            string[] labels={"初始点数","一位必要点数","击飞","赤宝牌数量","古役","食断","番缚","便捷提示"};
            string[] values={(state.n==3?35000:25000).ToString(),(state.n==3?40000:30000).ToString(),"开启",state.lobby?.red==false?"无赤牌":state.n==3?"2赤":"3赤","关闭","开启","一番缚","开启"};
            var view=new Rect(x,328,ruleWidth,566);float content=labels.Length*78;float max=content-view.height;int id=GUIUtility.GetControlID(101420,FocusType.Passive);var ev=Event.current;
            if(ev.type==EventType.MouseDown&&view.Contains(ev.mousePosition)){roomRulePress=ev.mousePosition;roomRuleDragging=false;}
            if(ev.type==EventType.MouseDrag&&(roomRuleDragging||view.Contains(roomRulePress)&&Mathf.Abs(ev.mousePosition.y-roomRulePress.y)>10)){roomRuleDragging=true;GUIUtility.hotControl=id;roomRuleScroll.y=Mathf.Clamp(roomRuleScroll.y-ev.delta.y,0,max);ev.Use();}
            if(ev.type==EventType.MouseUp&&roomRuleDragging){roomRuleDragging=false;GUIUtility.hotControl=0;ev.Use();}
            roomRuleScroll.y=Mathf.Clamp(roomRuleScroll.y,0,max);roomRuleScroll=GUI.BeginScrollView(view,roomRuleScroll,new Rect(0,0,ruleWidth,content),false,false,GUIStyle.none,GUIStyle.none);
            var detail=new GUIStyle(descriptionText){fontSize=26,alignment=TextAnchor.MiddleCenter};
            for(int i=0;i<labels.Length;i++){
                float y=i*78;Slice(new Rect(0,y,ruleWidth,73),"Sprites/Room/room_info_small_bg");
                Label(new Rect(4,y+4,ruleWidth-8,34),labels[i],detail,muted);
                Label(new Rect(4,y+37,ruleWidth-8,34),values[i],detail,Color.white);
            }
            GUI.EndScrollView();
        }
        Rect Popup(float w,float h,string background="Sprites/Necessary/pop_bg",string frame="Sprites/Common/pop_bound",float offset=0)
        {
            var r=new Rect((width-w)/2+offset,(1080-h)/2,w,h);float age=Time.unscaledTime-(modalClosing?closeStarted:modalStarted);string clip=modalClosing?"ui_popout":"ui_pop";
            float scale=Mathf.Max(.001f,Sample(clip,3,0,age,1));float alpha=Mathf.Clamp01(Sample(clip,1574349066,0,age,1));
            GUI.matrix=GUI.matrix*Matrix4x4.TRS(new Vector3(r.center.x*(1-scale),r.center.y*(1-scale)),Quaternion.identity,new Vector3(scale,scale,1));
            var c=GUI.color;GUI.color=new Color(c.r,c.g,c.b,c.a*alpha);
            Slice(r,background,20);Slice(r,frame,26,false);
            Image(new Rect(r.xMax-78,r.y+18,52,52),"Sprites/Necessary/btn_close",ScaleMode.ScaleToFit);
            if(Hit(new Rect(r.xMax-92,r.y+7,83,83),"closeModal"))CloseModal();return r;
        }
        void DrawModal()
        {
            string current=Modal;Rect logical;
            if(current=="join")logical=new Rect(width/2-474.1f,connectionSettings?30:193.81f,768.32f,connectionSettings?1020:802);
            else if(current=="rules")logical=new Rect(0,59,width,962);
            else if(current=="friend-help")logical=new Rect((width-1200)/2,210,1200,700);
            else if(current=="leave"||current=="invite")logical=new Rect((width-852.3f)/2,285,852.3f,510);
            else if(current=="yaku")logical=new Rect((width-1100)/2,170,1100,740);
            else if(current=="advanced-number")logical=new Rect(width/2-89.94f-384.16f,193.81f,768.32f,748.05f);
            else if(current=="advanced-help")logical=new Rect((width-852.3f)/2,285,852.3f,510);
            else logical=new Rect((width-1100)/2,240,1100,600);
            if(Event.current.type==EventType.MouseDown&&!logical.Contains(Event.current.mousePosition)&&!client.Busy&&!modalClosing){CloseModal();Event.current.Use();return;}
            var matrix=GUI.matrix;var color=GUI.color;
            if(current=="join")DrawJoin();else if(current=="rules")DrawRules();else if(current=="yaku")DrawYaku();else if(current=="friend-help")DrawFriendHelp();else if(current=="leave"||current=="invite")DrawRoomDialog(current=="invite");else if(current=="advanced-number")DrawAdvancedNumber();else if(current=="advanced-help")DrawAdvancedHelp();else{
                var r=Popup(1100,600);string heading=current=="profile"?"个人设置":current=="invite"?"邀请牌友":current=="leave"?"返回大厅":current=="kick"?"移除座位":"说明";
                Label(new Rect(r.x+100,r.y+28,r.width-200,80),heading,title);
                if(current=="profile"){
                    Label(new Rect(r.x+80,r.y+145,250,60),"昵称",text);Image(new Rect(r.x+330,r.y+145,610,70),"Sprites/Common/bg_textinput");profileName=GUI.TextField(new Rect(r.x+330,r.y+145,610,70),profileName,12,input);
                    if(Gold(new Rect(r.x+260,r.y+260,580,80),"继续本机房间","recover",true,5)){CloseModal();client.LobbyRecover();}
                    if(Gold(new Rect(r.center.x-145,r.yMax-135,290,85),"保存","saveProfile",true,5)){if(!string.IsNullOrWhiteSpace(profileName)){client.LobbyNickname=profileName.Trim();CloseModal();}}
                }else{
                    string value=current=="invite"?client.LobbyInviteText:current=="leave"?(client.State?.host==true?"返回大厅将解散尚未开始的房间。":"返回大厅将退出当前房间并释放座位。"):help;
                    Label(new Rect(r.x+70,r.y+132,r.width-140,300),value,text);
                    if(Gold(new Rect(r.center.x-145,r.yMax-135,290,85),"确认","confirm",true,5)){
                        if(current=="kick"){
                            CloseModal();if(client.State?.players.ElementAtOrDefault(kickSeat)?.name==kickName)client.LobbyCommand("kick",kickSeat);
                        }
                        else if(current=="leave"){CloseModal();client.LobbyLeave();}
                        else if(current=="invite"){GUIUtility.systemCopyBuffer=client.LobbyInviteText;CloseModal();}
                        else CloseModal();
                    }
                }
            }
            GUI.matrix=matrix;GUI.color=color;
        }
        void ReferencePopup(Rect frame,Rect background)
        {
            float elapsed=Time.unscaledTime-(modalClosing?closeStarted:modalStarted);string clip=modalClosing?"ui_popout":"ui_pop";
            float scale=Mathf.Max(.001f,Sample(clip,3,0,elapsed,1)),alpha=Mathf.Clamp01(Sample(clip,1574349066,0,elapsed,1));
            GUI.matrix=GUI.matrix*Matrix4x4.TRS(new Vector3(frame.center.x*(1-scale),frame.center.y*(1-scale)),Quaternion.identity,new Vector3(scale,scale,1));
            var color=GUI.color;GUI.color=new Color(color.r,color.g,color.b,color.a*alpha);
            // Cover only the interior under subpixel nine-slice seams; retain
            // the native cut corners instead of stretching them vertically.
            var fill=new Color(.062f,.079f,.126f);
            Tint(new Rect(background.x+24,background.y,background.width-48,background.height),fill);
            Tint(new Rect(background.x,background.y+24,background.width,background.height-48),fill);
            Slice(background,"Sprites/Necessary/bg");
            var decorationColor=GUI.color;GUI.color=new Color(decorationColor.r,decorationColor.g,decorationColor.b,decorationColor.a*.6f);
            Image(new Rect(background.x,background.yMax-107,247,107),"Sprites/Necessary/decorate_left");
            Image(new Rect(background.xMax-396,background.yMax-104,396,104),"Sprites/Necessary/decorate_right");
            GUI.color=decorationColor;
            Slice(frame,"Sprites/Necessary/frame",90,false);
            var close=new Rect(frame.xMax-86,frame.y+44,42,42);Image(close,"Sprites/Necessary/btn_close",ScaleMode.ScaleToFit);
            if(Hit(new Rect(close.x-22,close.y-22,86,86),"closeModal"))CloseModal();
        }
        GUIStyle ReferenceBrush(int size)
        {return new GUIStyle(title){font=brush,fontSize=size};}
        bool PopupConfirm(Rect bounds,string value,string id,float captionOffset=5,bool sliced=true)
        {
            var visual=ControlRect(bounds,id,advancedStyle.buttonClickScale);
            if(sliced)Slice(visual,"Sprites/Necessary/button_light");else Image(visual,"Sprites/Necessary/button_light");
            var caption=visual;caption.y+=captionOffset;ControlLabel(caption,bounds,value,ReferenceBrush(42),new Color(.42745f,.18039f,.22745f));
            if(!Hit(bounds,id))return false;Pulse(id);return true;
        }
        void OpenFriendHelp(int kind)
        {
            friendHelpKind=kind;
            help=kind==0?"模式：\n四人：使用四人麻将规则\n三人：使用三人麻将规则\n局数：\n东风：与玩家或电脑进行东风庄的对局\n半庄：与玩家或电脑进行半庄的对局\n友人场只结算对局点数。":"输入房间号，或按好友发送的邀请信息加入对应房间进行游戏。\n友人场只结算对局点数。";
            OpenModal("friend-help");
        }
        void DrawFriendHelp()
        {
            var r=new Rect((width-1200)/2,210,1200,700);
            ReferencePopup(r,new Rect(r.x+20,r.y+18,r.width-40,r.height-36));
            var prose=new GUIStyle(descriptionText){fontSize=32,alignment=TextAnchor.UpperLeft};var color=new Color(.83137f,.81176f,.81176f);
            Label(new Rect(r.x+68,r.y+80,r.width-136,360),help,prose,color);
            string guidance="提示：\n友人场用于朋友之间休闲娱乐、切磋牌技。\n创建或加入房间时，请先连接到同一热点。";
            Label(new Rect(r.x+68,r.y+(friendHelpKind==0?460:230),r.width-136,130),guidance,prose,color);
            if(PopupConfirm(new Rect(r.center.x-158.435f,r.yMax-120,316.87f,87.5f),"确认","friend-help-confirm"))CloseModal();
        }
        void DrawRoomDialog(bool invite)
        {
            var r=new Rect((width-852.3f)/2,285,852.3f,510);ReferencePopup(r,new Rect(r.x+20.45f,r.y+17.55f,r.width-40.89f,r.height-35.1f));
            string heading=invite?"邀请牌友":client.State?.host==true?"解散房间":"退出房间";
            RoomCentered(new Rect(r.x+80,r.y+32,r.width-160,78),heading,ReferenceBrush(48),new Color(.9098f,.68627f,.44314f),6);
            string message=invite?client.LobbyInviteText:client.State?.host==true?"确认解散当前房间并返回大厅？":"确认退出当前房间并返回大厅？";
            var prose=new GUIStyle(descriptionText){fontSize=28,alignment=invite?TextAnchor.UpperLeft:TextAnchor.MiddleCenter};
            Label(new Rect(r.x+80,r.y+135,r.width-160,205),message,prose,new Color(.83137f,.81176f,.81176f));
            if(!PopupConfirm(new Rect(r.center.x-158.435f,r.yMax-144,316.87f,87.5f),invite?"复制邀请":"确认","confirm",-1))return;
            CloseModal();if(invite)GUIUtility.systemCopyBuffer=client.LobbyInviteText;else client.LobbyLeave();
        }
        void DrawNumberPad(Vector2 first,string prefix,Action<int> pressed)
        {
            string[] keys={"1","2","3","4","5","6","7","8","9","清空","0",""};
            for(int i=0;i<12;i++){
                string id=prefix+i;var cell=new Rect(first.x+i%3*167,first.y+i/3*89,162,84);var visual=ControlRect(cell,id,advancedStyle.buttonClickScale);
                Image(visual,controlPulses.TryGetValue(id,out var at)&&Time.unscaledTime-at<.12f?"Sprites/Main/num_pressed":"Sprites/Main/num");
                float scale=visual.width/cell.width;
                if(i==11)Image(new Rect(visual.x+32*scale,visual.y+18*scale,99*scale,48*scale),"Sprites/Number/back",ScaleMode.ScaleToFit);
                else ControlLabel(visual,cell,keys[i],new GUIStyle(text){fontSize=i==9?45:54,fontStyle=i==9?FontStyle.Normal:FontStyle.Bold},new Color(.62745f,.63529f,.66667f));
                if(Hit(cell,id)){Pulse(id);pressed(i);}
            }
        }
        void DrawAdvancedHelp()
        {
            var r=new Rect((width-852.3f)/2,285,852.3f,510);
            ReferencePopup(r,new Rect(r.x+20.45f,r.y+17.55f,r.width-40.89f,r.height-35.1f));
            var description=new GUIStyle(descriptionText){fontSize=36,alignment=TextAnchor.UpperLeft};
            Label(new Rect(r.x+80.8f,r.y+106,r.width-161.6f,234),advancedHeading+"：\n"+advancedExplanation,description,new Color(.83137f,.81176f,.81176f));
            if(PopupConfirm(new Rect(r.center.x-158.435f,r.yMax-144,316.87f,87.5f),"确认","advanced-help-confirm"))CloseModal();
        }
        void DrawAdvancedNumber()
        {
            // UI_NumberInput uses distinct background and outer-frame rectangles.
            var r=new Rect(width/2-89.94f-384.16f,193.81f,768.32f,748.05f);
            ReferencePopup(r,new Rect(width/2-87.3f-362.805f,212.03f,725.61f,711.01f));
            var heading=ReferenceBrush(48);
            Label(new Rect(width/2-93.3f-254.45f,218,508.9f,80),"输入数值",heading,new Color(.92941f,.69804f,.43529f));
            Slice(new Rect(width/2-95-228,324,456,80),"Sprites/Common/inputbox");
            var digits=new GUIStyle(numberText){font=Resources.Load<Font>("Majsoul/Settlement/score-font"),fontSize=50};Label(new Rect(width/2-316.3f,331,445,70),editingNumber,digits);
            DrawNumberPad(new Vector2(width/2-342,428),"number-key-",EditNumberKey);
            if(PopupConfirm(new Rect(width/2-189,806.5f,200,82.19f),"确认","number-confirm",5,false)){
#if UNITY_EDITOR
                if(DesignPreviewInteractions&&!string.IsNullOrEmpty(editingNumber))designNumbers[editingNumberKey]=editingNumber;
#endif
                CloseModal();
            }
        }
        void EditNumberKey(int i)
        {
            if(i==9)editingNumber="";else if(i==11){if(editingNumber.Length>0)editingNumber=editingNumber.Substring(0,editingNumber.Length-1);}
            else if(editingNumber.Length<6)editingNumber+=i==10?"0":(i+1).ToString();
        }
        void DrawJoin()
        {
            var r=new Rect(width/2-474.1f,connectionSettings?30:193.81f,768.32f,connectionSettings?1020:748.05f);
            ReferencePopup(r,new Rect(r.x+24,r.y+18,r.width-42,r.height-37));
            Label(new Rect(r.center.x-254.45f,r.y+24.19f,508.9f,80),"加入房间",ReferenceBrush(48),new Color(.92941f,.69804f,.43529f));
            var field=new Rect(r.center.x-233,r.y+130.19f,456,80);Slice(field,"Sprites/Common/inputbox");
            var pinStyle=new GUIStyle{font=Resources.Load<Font>("Majsoul/Settlement/score-font"),fontSize=50,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(6,6,0,0)};
            // The freshly created style has no scaled backgrounds. That API is
            // editor-only; clearing the normal background also works on Android.
            foreach(var state in new[]{pinStyle.normal,pinStyle.hover,pinStyle.active,pinStyle.focused,pinStyle.onNormal,pinStyle.onHover,pinStyle.onActive,pinStyle.onFocused}){state.background=null;state.textColor=Color.white;}
            var typed=GUI.TextField(new Rect(field.x+6,field.y+7,field.width-12,70),number,6,pinStyle);number=new string(typed.Where(char.IsDigit).Where(c=>c>='0'&&c<='9').Take(6).ToArray());
            DrawNumberPad(new Vector2(r.center.x-252,r.y+234.19f),"digit",i=>{if(i==9)number="";else if(i==11){if(number.Length>0)number=number.Substring(0,number.Length-1);}else if(number.Length<6)number+=i==10?"0":(i+1).ToString();});
            if(connectionSettings){
                Label(new Rect(r.x+42,r.y+640,175,50),"房主地址",small);Image(new Rect(r.x+224,r.y+640,450,56),"Sprites/Common/bg_textinput");manualAddress=GUI.TextField(new Rect(r.x+224,r.y+640,450,56),manualAddress,80,input);
                Label(new Rect(r.x+42,r.y+705,175,50),"恢复码",small);Image(new Rect(r.x+224,r.y+705,450,56),"Sprites/Common/bg_textinput");recovery=GUI.TextField(new Rect(r.x+224,r.y+705,450,56),recovery,12,input);
            }
            var confirm=new Rect(r.center.x-100,connectionSettings?r.yMax-190:r.y+612.69f,200,82.19f);var visual=ControlRect(confirm,"join",advancedStyle.buttonClickScale);
            Image(visual,"Sprites/Necessary/button_light");var caption=visual;caption.y+=5;ControlLabel(caption,confirm,Searching?"查找中…":"确认",ReferenceBrush(42),new Color(.42745f,.18039f,.22745f));
            if(Hit(confirm,"join",number.Length==6))StartCoroutine(FindAndJoin());
            var settings=new Rect(r.center.x-230,connectionSettings?r.yMax-60:r.yMax+6,460,42);
            Label(settings,connectionSettings?"收起连接设置":"连接设置",small,new Color(.9f,.74f,.46f));
            if(Hit(settings,"connection")){connectionSettings=!connectionSettings;manualAddress=manualAddress.Length==0?client.LobbyEndpoint:manualAddress;}
            if(!string.IsNullOrEmpty(notice))Label(new Rect(r.x-200,connectionSettings?r.yMax-110:r.yMax+50,r.width+400,40),notice,small,new Color(1,.85f,.65f));
        }
        IEnumerator FindAndJoin()
        {
            CancelSearch();search=new CancellationTokenSource();Searching=true;notice="";string pin=number,address=manualAddress.Trim();
            if(!connectionSettings||string.IsNullOrEmpty(address)){
                var task=discovery.Find(pin,search.Token);while(!task.IsCompleted){yield return null;if(!Searching)yield break;}
                if(task.IsCanceled||!Searching)yield break;
                address=task.IsFaulted?null:task.Result;
                if(string.IsNullOrEmpty(address)){Searching=false;notice="未找到房间，请确认连接同一热点，或填写房主地址。";connectionSettings=true;returnAddress();yield break;}
            }
            Searching=false;client.LobbyJoin(address,pin,connectionSettings?recovery:"");
        }
        void returnAddress(){if(string.IsNullOrEmpty(manualAddress))manualAddress=client.LobbyEndpoint;}
        void OpenRules(){ruleMode=(Page==ScreenPage.Room?client.State?.n:Players)==3?1:0;ruleTab=0;ruleScroll=Vector2.zero;OpenModal("rules");}
        void DrawYaku()
        {
            var r=new Rect((width-1100)/2,170,1100,740);ReferencePopup(r,new Rect(r.x+20,r.y+18,r.width-40,r.height-36));
            Label(new Rect(r.x+80,r.y+28,r.width-160,78),"役种一览",ReferenceBrush(48),new Color(.9098f,.68627f,.44314f));
            const string list="一番役\n立直、门前清自摸和、平和、断幺九、一杯口、役牌、海底、河底、岭上开花、抢杠\n\n二番役\n双立直、七对子、对对和、三暗刻、三色同顺、一气通贯、混全带幺九、小三元\n\n高番役\n混一色、纯全带幺九、二杯口、清一色\n\n役满\n国士无双、四暗刻、大三元、小四喜、大四喜、字一色、清老头、绿一色、九莲宝灯、四杠子、天和、地和";
            var prose=new GUIStyle(descriptionText){fontSize=30,alignment=TextAnchor.UpperLeft};var view=new Rect(r.x+80,r.y+130,r.width-160,420);
            ruleScroll=GUI.BeginScrollView(view,ruleScroll,new Rect(0,0,view.width,prose.CalcHeight(new GUIContent(list),view.width)),false,false,GUIStyle.none,GUIStyle.none);
            Label(new Rect(0,0,view.width,900),list,prose,new Color(.83137f,.81176f,.81176f));GUI.EndScrollView();
            if(PopupConfirm(new Rect(r.center.x-158.435f,r.yMax-144,316.87f,87.5f),"确认","yaku-confirm"))CloseModal();
        }
        void DrawRules()
        {
            // UI_Rules_Room has a full-width shade and a separate page panel.
            var saved=GUI.color;GUI.color=new Color(saved.r,saved.g,saved.b,saved.a*.28f);Image(new Rect(0,0,width,1080),"Sprites/Necessary/bbg");GUI.color=saved;
            float elapsed=Time.unscaledTime-(modalClosing?closeStarted:modalStarted);string clip=modalClosing?"ui_popout":"ui_pop";
            float scale=Mathf.Max(.001f,Sample(clip,3,0,elapsed,1)),alpha=Mathf.Clamp01(Sample(clip,1574349066,0,elapsed,1));
            GUI.matrix=GUI.matrix*Matrix4x4.TRS(new Vector3(width/2*(1-scale),540*(1-scale)),Quaternion.identity,new Vector3(scale,scale,1));GUI.color=new Color(saved.r,saved.g,saved.b,saved.a*alpha);
            Slice(new Rect(-100,59,width+200,962),"Sprites/Rules/room_rule_bg");
            Label(new Rect(width/2-931.95f,91.58f,340.46f,133.24f),"规则说明",ReferenceBrush(66),new Color(.88627f,.72941f,.57255f));
            for(int i=0;i<2;i++){
                string id="ruleTab"+i;var bounds=new Rect(width/2-575.49f+i*690,129,690,86);var visual=ControlRect(bounds,id,advancedStyle.clickScale);
                Slice(visual,"Sprites/Rules/room_rule_tab_"+(ruleTab==i?"select":"unselect"));
                ControlLabel(visual,bounds,i==0?"图文简介":"详细规则",ReferenceBrush(40),ruleTab==i?Color.white:new Color(.39608f,.43922f,.47059f));
                if(Hit(bounds,id)){Pulse(id);ruleTab=i;ruleScroll=Vector2.zero;}
            }
            var close=new Rect(width/2+828.27f,100.7f,65,65);Image(ControlRect(close,"closeModal",advancedStyle.buttonClickScale),"Sprites/Necessary/btn_close",ScaleMode.ScaleToFit);
            if(Hit(new Rect(close.x-27,close.y-27,119,119),"closeModal"))CloseModal();
            var page=new Rect(width/2-665.14f,212.41f,1546.68f,782.18f);Slice(page,"Sprites/Rules/room_rule_bg_small");
            for(int i=0;i<2;i++){
                string id="ruleMode"+i;bool selected=ruleMode==i;float cx=width/2-773.4f,cy=293+i*96;
                var bounds=new Rect(cx-(selected?153:124.5f),cy-(selected?68:43),selected?306:249,selected?136:86);var visual=ControlRect(bounds,id,advancedStyle.clickScale);
                Image(visual,"Sprites/Rules/room_rule_tab_small_"+(selected?"select":"unselect"));
                ControlLabel(visual,bounds,i==0?"四人麻将":"三人麻将",ReferenceBrush(36),selected?new Color(.08627f,.04706f,.04706f):Color.white);
                if(Hit(new Rect(cx-153,cy-48,306,96),id)){Pulse(id);ruleMode=i;ruleScroll=Vector2.zero;}
            }
            if(ruleTab==0)DrawRuleIntro(new Rect(width/2-541.91f,279,1299,644),ruleMode==1);
            else{
                string rules=ruleMode==0?"常规的立直麻将规则。\n房间按高级设置中显示的标准规则进行对局。":"三人麻将规则说明：\n－不使用二万～八万的牌\n－不允许吃，允许碰和杠\n－北风牌可以拔北，拔北计宝牌\n－初始35000点，一位目标40000点\n－有赤牌时，五筒与五索各一张\n－一番缚，允许食断，古役关闭\n－不限出牌时间，负分击飞\n－东风打东场，半庄打东、南场\n－本地房间只结算对局点数";
                var view=new Rect(width/2-623.91f,260,1355.52f,680.97f);var prose=new GUIStyle(descriptionText){fontSize=36,alignment=TextAnchor.UpperLeft};
                float height=Mathf.Max(view.height,prose.CalcHeight(new GUIContent(rules),view.width));
                ruleScroll=GUI.BeginScrollView(view,ruleScroll,new Rect(0,0,view.width,height),false,false,GUIStyle.none,GUIStyle.none);
                Label(new Rect(0,0,view.width,height),rules,prose,new Color(.83137f,.81176f,.81176f));GUI.EndScrollView();
            }
        }
        void DrawRuleIntro(Rect canvas,bool three)
        {
            // Keep the native page's alpha, complete meld frames and riichi
            // stick together. Cropping those parts independently lost edges.
            Image(canvas,"Sprites/RulePages/"+(three?"sanren":"siren"),ScaleMode.ScaleToFit);
        }
        void OnApplicationPause(bool paused){if(paused){sounds?.Stop();music?.Pause();CancelSearch();}}
        void OnDestroy(){CancelSearch();discovery.Dispose();}
#if UNITY_EDITOR
        public bool DesignPreviewInteractions;
        readonly Dictionary<string,int> designChoices=new Dictionary<string,int>();
        readonly Dictionary<string,string> designNumbers=new Dictionary<string,string>();
        public void DesignPreviewAdvancedTop(){scroll.y=434;}
        public void DesignPreviewAdvancedLower(){scroll.y=479+2*advancedStyle.rowHeight;}
        public void DesignPreviewChoose(string key,int value){designChoices[key]=value;}
        public void DesignPreviewPulse(string id){Pulse(id);var r=ValidationTarget(id);if(r.width>0)client.Tap.Play(new Vector2(r.center.x,Screen.height-r.center.y));}
        public void DesignPreviewOpenNumber(){OpenNumber("initial",AdvancedNumber("initial"));}
        public void DesignPreviewHelp(){AdvancedHelp("kuitan");}
        public void DesignPreviewClose(){CloseModal();}
        public void DesignPreviewFriendHelp(int kind){OpenFriendHelp(kind);}
        public void DesignPreviewJoin(){number="";connectionSettings=false;OpenModal("join");}
        public void DesignPreviewRules(int mode=0,int tab=0){if(Modal!="rules")OpenRules();ruleMode=mode;ruleTab=tab;ruleScroll=Vector2.zero;}
        public void DesignPreviewRoomScroll(bool bottom){roomRuleScroll.y=bottom?9999:0;}
        string validationPress;
        public void ValidationPress(string target){validationPress=target;}
        public Rect ValidationTarget(string name)=>targets.TryGetValue(name,out var r)?r:new Rect();
        public void ValidationScreen(ScreenPage page){Modal="";Go(page);started=Time.unscaledTime-2;flyCount=3;}
        public void ValidationAdvanced(bool value){Advanced=value;}
        public void DesignPreviewScrollBottom(){scroll.y=float.MaxValue;}
        public void ValidationNumber(string value){number=value;}
#endif
    }
}
