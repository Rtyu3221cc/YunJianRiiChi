using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace YunJian.Riichi {
    // Launch cover and preloading. After the Unity logo the screen stays in the
    // splash colour, with a small loading line, until the home character (its
    // still and, with dynamic outfits on, its animated parts) and the lobby
    // background are really drawable. Then the cover fades and the lobby entry
    // plays once. Before (0.6.29–0.6.31) the lobby appeared with the character
    // still loading from its pack and the entry was replayed when it arrived,
    // which read as a stutter (1102/進游戲加載卡頓.mp4).
    // Afterwards the outfit page's font, chrome and icons are warmed in the
    // background (all built-in resources, no pack files).
    public sealed partial class LobbyController {
        bool wardrobeWarm,wardrobeWarming,startupCover,roomInterfaceWarming,roomInterfaceWarm;float startupCoverEnd=-1;GUIStyle startupCoverText;
        const float StartupCoverFade=.35f,StartupCoverLimit=8f;
        static readonly Color SplashColor=new Color(.1216f,.1216f,.1255f);
        public bool StartupPreloadDone{get;private set;}
        public bool StartupCovered=>startupCover||startupCoverEnd>=0&&Now-startupCoverEnd<StartupCoverFade;
        // Editor recordings run in batch mode and start from their own lobby state.
        void BeginStartupPreload(){startupCover=!(Application.isEditor&&Application.isBatchMode);StartCoroutine(StartupPreload());}
        IEnumerator StartupPreload(){
            float began=Time.realtimeSinceStartup;
            var active=CharacterRoster.Active;var home=CharacterRoster.Skin(active.character,active.skin);
            var roomSkins=StartupRoomSkins(home.id);PinStartupRoomVisuals(roomSkins);StartCoroutine(WarmRoomInterface());
            LobbyIllustrationSettings.Get(home.id);CharacterFraming.FrameRect(home.id,"A0",new Rect(0,0,100,100),out _);
            string background=WardrobeSelection.Get("lobbyBackground")?.texture;
            // Pack assets arrive asynchronously; keep asking until the art is drawable.
            while(Time.realtimeSinceStartup-began<StartupCoverLimit){
                WardrobeCatalog.Texture(background);characterArt.Art(home,Now);
                bool cardsReady=true;foreach(int skin in roomSkins)if(CharacterRoster.DisplayTexture(CharacterFraming.CardPath(skin),true)==null)cardsReady=false;
                if(characterArt.Ready(home)&&cardsReady&&roomInterfaceWarm)break;yield return null;
            }
            var dorm=CurrentOutfit;CharacterFraming.FrameRect(dorm.id,"B0",new Rect(0,0,100,100),out _);
            if(startupCover){
                while(!SplashScreen.isFinished)yield return null;
                yield return null; // one frame of the finished lobby under the cover
                startupCover=false;startupCoverEnd=Now;
                if(Page==ScreenPage.Home&&!dormOpen&&!DressTableOpen&&!client.LobbyActive&&Modal=="")EnterHome(true);
            }
            if(dorm.id!=home.id){characterArt.Art(dorm,Now);yield return null;}
            yield return WarmWardrobe();
            StartupPreloadDone=true;
        }
        void DrawStartupCover(){
            float a=startupCover?1:startupCoverEnd>=0?1-Mathf.Clamp01((Now-startupCoverEnd)/StartupCoverFade):0;if(a<=0)return;
            var matrix=GUI.matrix;var color=GUI.color;GUI.matrix=Matrix4x4.identity;
            GUI.color=new Color(SplashColor.r,SplashColor.g,SplashColor.b,a);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
            if(startupCover&&SplashScreen.isFinished){
                if(startupCoverText==null){startupCoverText=new GUIStyle(GUI.skin.label){font=body,alignment=TextAnchor.MiddleCenter,clipping=TextClipping.Overflow};startupCoverText.normal.textColor=new Color(.62f,.63f,.68f);}
                startupCoverText.fontSize=Mathf.RoundToInt(Screen.height*.032f);
                GUI.color=new Color(1,1,1,a);GUI.Label(new Rect(0,Screen.height*.84f,Screen.width,Screen.height*.08f),"正在加载"+new string('·',1+Mathf.FloorToInt(Time.unscaledTime*2.5f)%3),startupCoverText);
            }
            GUI.color=color;GUI.matrix=matrix;
        }
        int warmedActiveSkin,warmedDormSkin,illustrationWarmSerial;
        void WarmSelectedIllustrations(){
            var active=CharacterRoster.Active;var skin=CharacterRoster.Skin(active.character,active.skin);
            int dorm=dormOpen?(dormView==DormView.Skins?BrowsingSkin.id:CurrentOutfit.id):0;
            if(warmedActiveSkin==skin.id&&warmedDormSkin==dorm)return;
            warmedActiveSkin=skin.id;warmedDormSkin=dorm;StartCoroutine(WarmIllustration(skin,++illustrationWarmSerial));
        }
        IEnumerator WarmIllustration(CharacterSkin skin,int serial){
            float limit=Time.realtimeSinceStartup+8;
            while(serial==illustrationWarmSerial&&Time.realtimeSinceStartup<limit){
                CharacterRoster.DisplayTexture(CharacterFraming.CardPath(skin.id),true);characterArt.Art(skin,Now);if(dormOpen)characterArt.Art(dormView==DormView.Skins?BrowsingSkin:CurrentOutfit,Now);
                if(characterArt.Ready(skin)&&(!dormOpen||characterArt.Ready(dormView==DormView.Skins?BrowsingSkin:CurrentOutfit)))yield break;
                yield return null;
            }
        }
        IEnumerator WarmRoomInterface(){
            if(roomInterfaceWarming||roomInterfaceWarm)yield break;roomInterfaceWarming=true;
            roomDressLayout??=new RoomCustomizationLayout();yield return roomDressLayout.Preload();
            Resources.Load<Font>("Majsoul/Characters/Fonts/character-name-brush");
            foreach(var role in CharacterRoster.Ordered(installedOnly:true).Take(9)){CharacterRoster.ListPortrait(role,CharacterRoster.BattleSkin(role.key));yield return null;}
            var active=CharacterRoster.Active;var selected=CharacterRoster.Find(active.character);
            foreach(var skin in selected.skins.OrderBy(s=>s.id==active.skin?0:1).Take(6)){CharacterRoster.DisplayTexture("Skins/"+skin.id+"/bighead",true);yield return null;}
            roomInterfaceWarm=roomDressLayout.Warm;roomInterfaceWarming=false;
        }
        IEnumerator WarmWardrobe(){
            if(wardrobeWarm||wardrobeWarming)yield break;wardrobeWarming=true;
            WardrobeNameText(26);yield return null;
            int n=0;
            foreach(var key in WardrobeCatalog.UiKeys){WardrobeCatalog.Texture("Majsoul/Wardrobe/UI/"+key);if(++n%6==0)yield return null;}
            foreach(var category in dressCategories){WardrobeCatalog.Texture(category.icon);WardrobeCatalog.Texture(Draft(category.key)?.icon);}
            yield return null;
            // The page opens on the riichi stick list; its icons come first.
            foreach(var category in dressCategories.OrderBy(c=>c.key=="stick"?0:1))foreach(var item in WardrobeCatalog.Items(category.key)){WardrobeCatalog.Texture(item.icon);if(++n%8==0)yield return null;}
            wardrobeWarm=true;wardrobeWarming=false;
        }
    }
}
