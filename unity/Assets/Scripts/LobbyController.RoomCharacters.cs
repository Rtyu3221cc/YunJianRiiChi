using System.Collections.Generic;
using UnityEngine;

namespace YunJian.Riichi {
    public sealed partial class LobbyController {
        readonly Dictionary<int,int> roomCharacterSkins=new Dictionary<int,int>();
        readonly Dictionary<int,float> roomCharacterReady=new Dictionary<int,float>();
#if UNITY_EDITOR
        public readonly Dictionary<int,bool> ReviewRoomCardReady=new Dictionary<int,bool>();
#endif
        void DrawRoomCharacter(Rect area,PlayerView player,int seat){
            int id=CharacterRoster.Skin(player.character,player.skin).id;
            if(!roomCharacterSkins.TryGetValue(seat,out int previous)||previous!=id){roomCharacterSkins[seat]=id;roomCharacterReady.Remove(seat);}
            var image=CharacterRoster.DisplayTexture(CharacterFraming.CardPath(id));
#if UNITY_EDITOR
            ReviewRoomCardReady[seat]=image!=null;
#endif
            if(image==null){
                roomCharacterReady.Remove(seat);
                var thumbnail=CharacterPackService.Thumbnail(player.character);float side=Mathf.Min(area.width*.55f,160);
                if(thumbnail!=null)GUI.DrawTexture(new Rect(area.center.x-side/2,area.y+area.height*.28f,side,side),thumbnail,ScaleMode.ScaleToFit,true);
                var caption=new GUIStyle(descriptionText){alignment=TextAnchor.MiddleCenter,fontSize=Mathf.RoundToInt(area.width*.075f)};
                Label(new Rect(area.x,area.y+area.height*.6f,area.width,45),"形象加载中…",caption,new Color(.83f,.81f,.81f,.65f+.2f*Mathf.Sin(Time.unscaledTime*4)));
                return;
            }
            if(!roomCharacterReady.TryGetValue(seat,out float began))roomCharacterReady[seat]=began=Time.unscaledTime;
            var color=GUI.color;GUI.color=new Color(color.r,color.g,color.b,color.a*Mathf.Clamp01((Time.unscaledTime-began)/.18f));
            CharacterFraming.DrawSkinCard(area,id,true);GUI.color=color;
        }
    }
}
