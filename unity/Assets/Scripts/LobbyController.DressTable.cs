using System.Collections;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi {
    // Full-screen wardrobe preview on the real table for effects that only
    // exist there: riichi, win and callout. It plays a short local script in a
    // loop with the outfit being viewed. Entering crossfades from the wardrobe
    // into the table; leaving crossfades back.
    public sealed partial class LobbyController {
        static bool DressNeedsTable(string category)=>category=="riichiEffect"||category=="winEffect"||category=="callout"||category=="stick";
        const float DressTableEnter=.45f,DressTableExit=.30f,DressTableDip=.22f;
        string dressTableCategory,dressTableItem;WardrobeLoadout dressTableLoadout;
        float dressTableBegan,dressTableCycleBegan,dressTableClosing=-1;int dressTableCycle,dressTableStep;bool dressTableReady;
        public bool DressTableOpen=>dressTableCategory!=null;
        float DressTableStepAt=>dressTableCycle==1?DressTableEnter+.55f:.55f;
        // Each loop moves to the next seat, clockwise on screen from the viewer, so
        // the effect is seen from all four directions (the callout from the three
        // seats whose discard the viewer can call).
        float DressTableCycleLength=>dressTableCategory=="winEffect"?8.5f:dressTableCategory=="riichiEffect"?5.2f:dressTableCategory=="stick"?6.2f:3.6f;
        int DressTableSeat{get{var order=dressTableCategory=="callout"?DressPreviewStates.CallingSeats:DressPreviewStates.ActingSeats;return order[Mathf.Max(0,dressTableCycle-1)%order.Length];}}
        void OpenDressTable(string category){
            if(DressTableOpen||!client.BeginDressPreview())return;
            dressTableCategory=category;dressTableItem=Draft(category)?.name;dressTableLoadout=new WardrobeLoadout();
            foreach(var f in typeof(WardrobeLoadout).GetFields())f.SetValue(dressTableLoadout,Draft(f.Name)?.id);
            dressTableBegan=Now;dressTableClosing=-1;dressTableCycle=0;dressTableReady=false;
            // Entering the preview plays the match-entry intro, so loading a freshly
            // downloaded pack never shows as a frozen wardrobe page.
            client.PlayDressIntro(DressTableState(false));
            StartCoroutine(PrepareDressTable());
        }
        IEnumerator PrepareDressTable(){
            string category=dressTableCategory;
            yield return WardrobePackService.Preload(new[]{dressTableLoadout.Id(category),dressTableLoadout.Id("hand"),dressTableLoadout.Id("cloth"),dressTableLoadout.Id("tileBack"),dressTableLoadout.Id("tileFace")}.Where(id=>!string.IsNullOrEmpty(id)));
            if(dressTableCategory!=category||dressTableClosing>=0)yield break;
            // Build the table once behind the intro so its first render hitch stays hidden.
            client.ShowDressPreviewState(DressTableState(false));
            while(client.DressIntroActive){yield return null;if(dressTableCategory!=category||dressTableClosing>=0)yield break;}
            dressTableReady=true;dressTableBegan=Now;BeginDressTableCycle();
        }
        RoomView DressTableState(bool second){
            var role=CharacterRoster.Active;string room="dress-preview-"+dressTableCycle,name=client.LobbyNickname;int seat=DressTableSeat;
            if(dressTableCategory=="riichiEffect")return second?DressPreviewStates.Riichi(room,dressTableLoadout,role.character,role.skin,name,seat):DressPreviewStates.BeforeRiichi(room,dressTableLoadout,role.character,role.skin,name,seat);
            if(dressTableCategory=="stick")return second?DressPreviewStates.RiichiStick(room,dressTableLoadout,role.character,role.skin,name,seat):DressPreviewStates.BeforeRiichi(room,dressTableLoadout,role.character,role.skin,name,seat);
            if(dressTableCategory=="winEffect")return second?DressPreviewStates.Tsumo(room,dressTableLoadout,role.character,role.skin,name,seat):DressPreviewStates.BeforeWin(room,dressTableLoadout,role.character,role.skin,name,seat);
            return DressPreviewStates.Callable(room,dressTableLoadout,role.character,role.skin,name,seat);
        }
        void BeginDressTableCycle(){dressTableCycle++;dressTableStep=0;dressTableCycleBegan=Now;client.ShowDressPreviewState(DressTableState(false));}
        void TickDressTable(){
            if(!DressTableOpen)return;
            if(!client.DressPreviewing){dressTableCategory=null;return;}
            if(dressTableClosing>=0){if(Now-dressTableClosing>=DressTableExit)FinishDressTable();return;}
            if(!dressTableReady)return;
            float t=Now-dressTableCycleBegan;
            if(dressTableStep==0&&dressTableCategory!="callout"&&t>=DressTableStepAt){dressTableStep=1;client.ShowDressPreviewState(DressTableState(true));}
            if(t>=DressTableCycleLength)BeginDressTableCycle();
        }
        void CloseDressTable(){if(DressTableOpen&&dressTableClosing<0){dressTableClosing=Now;Cue("btn_cancel");}}
        void FinishDressTable(){dressTableCategory=null;dressTableClosing=-1;dressTableReady=false;client.EndDressPreview();}
        // Seats switch straight to the next state; the former black dip between
        // loops read as the screen flickering.
        float DressTableCurtain()=>0;
        // Drawn by RiichiClient instead of the lobby while the preview owns the screen.
        public void DrawDressPreviewOverlay(){
            Styles();var matrix=GUI.matrix;var color=GUI.color;bool enabled=GUI.enabled;
            // The intro hands over straight to the table; the wardrobe page fades back in on exit.
            float cover=dressTableClosing>=0?Mathf.SmoothStep(0,1,(Now-dressTableClosing)/DressTableExit):0;
            float curtain=dressTableReady?DressTableCurtain():1;
            if(!dressTableReady&&dressTableClosing<0){
                GUI.matrix=Matrix4x4.identity;GUI.color=Color.black;GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=color;
                var s=Screen.safeArea;float k=Mathf.Min(s.height/1080f,s.width/1920f);GUI.matrix=Matrix4x4.TRS(new Vector3(s.x+(s.width-1920*k)/2,Screen.height-s.yMax+(s.height-1080*k)/2,0),Quaternion.identity,new Vector3(k,k,1));
                Label(new Rect(0,500,1920,80),"正在载入预览…",WardrobeNameText(32),CharacterGold);GUI.matrix=matrix;return;
            }
            if(curtain>0){GUI.matrix=Matrix4x4.identity;GUI.color=new Color(0,0,0,curtain);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=color;}
            var safe=Screen.safeArea;float scale=Mathf.Min(safe.height/1080f,safe.width/1920f);width=safe.width/scale;
            GUI.matrix=Matrix4x4.TRS(new Vector3(safe.x,Screen.height-safe.yMax+(safe.height-1080*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
            float shown=1-cover,drop=(1-shown)*-70;
            GUI.color=new Color(color.r,color.g,color.b,color.a*shown);
            // A thin strip above the across player's hand.
            var banner=new Rect(width/2-380,6+drop,760,48);NativePanel.Box(banner,new Color(0,0,0,.55f));
            var category=dressCategories.FirstOrDefault(c=>c.key==dressTableCategory);
            Label(new Rect(banner.x+16,banner.y,banner.width-32,banner.height),"装扮预览 · "+(category?.name??"")+(string.IsNullOrEmpty(dressTableItem)?"":" · "+dressTableItem)+" · "+(dressTableCategory=="callout"?DressPreviewStates.SeatName(DressTableSeat)+"打出":DressPreviewStates.SeatName(DressTableSeat)),WardrobeNameText(26),CharacterGold);
            var close=new Rect(width-118,24+drop,80,80);NativePanel.Box(close,new Color(0,0,0,.55f));WImage(new Rect(close.x+18,close.y+18,44,44),"preview_close",ScaleMode.ScaleToFit);
            GUI.enabled=enabled&&dressTableClosing<0&&shown>.9f;if(Hit(new Rect(close.x-8,close.y-8,96,96),"dress-table-close"))CloseDressTable();
            GUI.enabled=enabled;GUI.color=color;GUI.matrix=matrix;
            if(cover>.001f){GUI.color=new Color(color.r,color.g,color.b,color.a*cover);GUI.enabled=false;Draw();GUI.enabled=enabled;GUI.color=color;GUI.matrix=matrix;}
        }
#if UNITY_EDITOR
        public void PreviewDressTable(string category){SelectDressCategory(category);OpenDressTable(category);}
        public bool PreviewDressTableReady=>dressTableReady;
        public void PreviewCloseDressTable()=>CloseDressTable();
#endif
    }
}
