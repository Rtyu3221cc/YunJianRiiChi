using System;
using System.Linq;
using UnityEngine;
namespace YunJian.Riichi {
    public sealed partial class LobbyController {
        bool characterVoiceTab;
        readonly CharacterPaneDrag characterVoiceDrag=new CharacterPaneDrag();Vector2 characterVoiceOffset;
        int characterVoiceIndex=-1;string characterVoiceRole,characterVoiceNotice;
        CharacterVoices VoicePlayer=>GetComponent<CharacterVoices>();
        void StopCharacterVoicePreview(){VoicePlayer?.Stop();if(PlaylistAudio.Active!=null)PlaylistAudio.Active.CharacterVoicePreview=false;characterVoiceIndex=-1;characterVoiceNotice=null;}
        void SetCharacterInfoTab(bool voice){if(characterVoiceTab==voice)return;StopCharacterVoicePreview();characterVoiceTab=voice;infoScroll=Vector2.zero;characterVoiceOffset.y=0;}
        void DrawCharacterVoicePage(float x){
            // The outgoing page keeps drawing during the exit transition; only the visible page ducks music.
            if(PlaylistAudio.Active!=null&&dormOpen&&!dormLeaving&&dormView==DormView.Info)PlaylistAudio.Active.CharacterVoicePreview=true;var role=CurrentCharacter.key;if(characterVoiceRole!=role){StopCharacterVoicePreview();characterVoiceRole=role;characterVoiceOffset.y=0;}
            var rows=CharacterVoiceCatalog.Lines(role);bool playing=VoicePlayer!=null&&VoicePlayer.Playing&&VoicePlayer.PlayingCharacter==role;
            if(!playing)characterVoiceIndex=-1;
            Label(new Rect(x+84,178,644,52),"角色语音 · "+rows.Count(r=>r.available)+" 条可播放",NText("profile-heading",32,TextAnchor.MiddleLeft),CharacterGold);
            var view=new Rect(x+84,245,644,675);float total=rows.Length*94;DragCharacterPane(characterVoiceDrag,view,new Vector2(view.width,total),ref characterVoiceOffset,false);GUI.BeginGroup(view);var oldOffset=targetOffset;targetOffset+=view.position;
            for(int i=0;i<rows.Length;i++){
                var line=rows[i];float y=i*94-characterVoiceOffset.y;if(y+82<0||y>view.height)continue;
                var row=new Rect(0,y,644,82);CSlice(row,"bf_bg_sound");if(i==characterVoiceIndex)Tint(row,new Color(.3f,.21f,.08f,.3f));
                Label(new Rect(25,y+15,470,52),line.name,NText("profile-body",28,TextAnchor.MiddleLeft),line.available?CharacterGold:new Color(.55f,.55f,.62f));
                var button=new Rect(512.5f,y+4.5f,129,79);CImage(new Rect(button.center.x-29,button.center.y-29,58,58),i==characterVoiceIndex?"bf_pause":"bf_play",ScaleMode.ScaleToFit);
                if(!line.available||!CharacterRoster.CanDisplay(role)){Label(new Rect(492,y+24,145,38),"暂未收录",NText("profile-body",23),new Color(.55f,.55f,.62f));continue;}
                if(Hit(button,"character-voice-play-"+i)){
                    if(i==characterVoiceIndex){StopCharacterVoicePreview();}
                    else if(VoicePlayer!=null&&VoicePlayer.TryPlay(role,line.path)){characterVoiceIndex=i;characterVoiceNotice=line.text;}
                    else characterVoiceNotice=AppPreferences.Gain(4)<=0?"请先在设置中开启角色语音。":"该条语音暂时无法加载，请检查角色资源。";
                }
            }
            targetOffset=oldOffset;GUI.EndGroup();
            if(total>view.height){var rail=new Rect(view.xMax+19,view.y+4,6.4f,view.height-8);CImage(rail,"ss_slidebar");float p=Mathf.Clamp01(characterVoiceOffset.y/(total-view.height));CImage(new Rect(rail.center.x-17,rail.y+p*(rail.height-34),34,34),"ss_slidebtn");}
            if(!string.IsNullOrEmpty(characterVoiceNotice))Label(new Rect(x+84,946,644,84),characterVoiceNotice,NText("profile-body",25,TextAnchor.UpperLeft,true),new Color(.86f,.85f,.9f));
        }
#if UNITY_EDITOR
        public bool ReviewCharacterVoiceTab=>characterVoiceTab;
        public string ReviewCharacterVoicePath=>characterVoiceIndex>=0?CharacterVoiceCatalog.Lines(CurrentCharacter.key)[characterVoiceIndex].path:null;
        public void ReviewVoiceScroll(float y)=>characterVoiceOffset.y=y;
        public float ReviewVoiceOffset=>characterVoiceOffset.y;
#endif
    }
}
