using UnityEngine;

namespace YunJian.Riichi {
    public sealed partial class LobbyController {
        GUIStyle characterSubtitleStyle;
        bool CharacterSubtitleVisible {
            get {
                if(!Visible||StartupCovered||OverlayActive||client.LobbyOverlayVisible||CharactersOverlay||DressTableOpen||homeIllustrationEditing)return false;
                if(dormOpen)return !dormLeaving&&!pageMoving&&dormView!=DormView.Wardrobe&&!(dormView==DormView.Info&&characterVoiceTab);
                return Page==ScreenPage.Home&&!client.LobbyActive;
            }
        }
        string CharacterSubtitleText {
            get {
                var voice=VoicePlayer;
                if(!CharacterSubtitleVisible||voice==null||!voice.Playing||voice.PlayingCharacter!=(dormOpen?CurrentCharacter.key:CharacterRoster.Active.character))return "";
                return voice.Caption.Revealed(Time.unscaledTime);
            }
        }
        void DrawCharacterSubtitle(bool dorm){
            if(dorm!=dormOpen)return;
            string value=CharacterSubtitleText;if(value.Length==0)return;
            var caption=VoicePlayer.Caption;float alpha=caption.Opacity(Time.unscaledTime);if(alpha<=0)return;
            if(characterSubtitleStyle==null)characterSubtitleStyle=new GUIStyle(text){font=body,fontSize=28,alignment=TextAnchor.UpperLeft,wordWrap=true,richText=false,clipping=TextClipping.Clip,padding=new RectOffset()};
            // Original lower-character placement, in the existing 1080-high UI.
            float center=width*(dorm?.315f:.265f)+(dorm?55:110);
            if(dorm&&dormView!=DormView.Appreciate)center=Mathf.Min(center,width-(dormView==DormView.Select?1016:1080)-24-317);
            var panel=new Rect(Mathf.Clamp(center-317,24,width-658),dorm?680:734,634,234);
            var color=GUI.color;bool enabled=GUI.enabled;GUI.enabled=true;
            GUI.color=new Color(color.r,color.g,color.b,color.a*alpha*.65f);CSlice(panel,"mask_dialogue");
            GUI.color=new Color(color.r,color.g,color.b,color.a*alpha);
            var content=new Rect(panel.x+28,panel.y+26,panel.width-56,panel.height-48);
            // Keep the newest line readable even for the few very long speeches.
            float height=characterSubtitleStyle.CalcHeight(new GUIContent(value),content.width);
            GUI.BeginGroup(content);Label(new Rect(0,-Mathf.Max(0,height-content.height),content.width,Mathf.Max(content.height,height)),value,characterSubtitleStyle,new Color(.94f,.94f,.96f));GUI.EndGroup();
            GUI.color=color;GUI.enabled=enabled;
        }
    }
}
