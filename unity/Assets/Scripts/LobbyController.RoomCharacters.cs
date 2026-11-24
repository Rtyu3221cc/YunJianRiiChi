using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi {
    public sealed partial class LobbyController {
        readonly Dictionary<int,int> roomCharacterSkins=new Dictionary<int,int>();
        readonly Dictionary<int,float> roomCharacterReady=new Dictionary<int,float>();
#if UNITY_EDITOR
        public readonly Dictionary<int,bool> ReviewRoomCardReady=new Dictionary<int,bool>();
#endif
        [Serializable] sealed class RoomWarmPlan {public int[] skins=new int[0];}
        readonly HashSet<string> roomVisualPins=new HashSet<string>();string roomWarmSignature;readonly HashSet<string> startupVisualPins=new HashSet<string>();float startupVisualPinUntil;
        string RoomWarmPath=>Path.Combine(CharacterPackService.Root,"room-preview-plan-v1.json");
        int[] StartupRoomSkins(int selected){
            var ids=new List<int>{selected};try{if(File.Exists(RoomWarmPath))ids.AddRange(JsonUtility.FromJson<RoomWarmPlan>(File.ReadAllText(RoomWarmPath)).skins??new int[0]);}catch(Exception){}
            return ids.Distinct().Where(id=>CharacterRoster.All.Any(r=>CharacterRoster.Available(r.key)&&r.skins.Any(s=>s.id==id))).Take(4).ToArray();
        }
        void PinStartupRoomVisuals(int[] ids){
            startupVisualPinUntil=Time.realtimeSinceStartup+30;
            foreach(int id in ids){var role=CharacterRoster.All.FirstOrDefault(r=>r.skins.Any(s=>s.id==id));if(role!=null&&startupVisualPins.Add(role.key))CharacterPackService.Pin(role.key);}
        }
        void ReleaseStartupVisualPins(){foreach(string key in startupVisualPins)CharacterPackService.Unpin(key);startupVisualPins.Clear();}
        void ReleaseRoomVisualPins(){foreach(string key in roomVisualPins)CharacterPackService.Unpin(key);roomVisualPins.Clear();ReleaseStartupVisualPins();}
        void SynchronizeRoomVisuals(){
            var room=client.State;var roles=room?.phase=="lobby"?room.players.Where(p=>p!=null&&CharacterRoster.Available(p.character)).Select(p=>p.character).Distinct().ToArray():new string[0];
            foreach(string key in roomVisualPins.Where(key=>!roles.Contains(key)).ToArray()){CharacterPackService.Unpin(key);roomVisualPins.Remove(key);}
            foreach(string key in roles)if(roomVisualPins.Add(key))CharacterPackService.Pin(key);
            if(startupVisualPins.Count>0&&(room?.phase=="lobby"||client.LobbyPlaying||Time.realtimeSinceStartup>=startupVisualPinUntil))ReleaseStartupVisualPins();
            if(room?.phase!="lobby")return;
            var ready=room.players.Select(p=>CharacterRoster.Skin(p.character,p.skin).id).Distinct().Where(id=>CharacterRoster.DisplayTexture(CharacterFraming.CardPath(id),true)!=null).Take(4).ToArray();
            if(ready.Length==0)return;string signature=string.Join(",",ready);if(signature==roomWarmSignature)return;roomWarmSignature=signature;
            if(CharacterRoster.PreviewOnly&&CharacterPackService.RootOverride==null)return;
            try{Directory.CreateDirectory(CharacterPackService.Root);File.WriteAllText(RoomWarmPath,JsonUtility.ToJson(new RoomWarmPlan{skins=ready}));}catch(IOException){}catch(UnauthorizedAccessException){}
        }
        void DrawRoomCharacter(Rect area,PlayerView player,int seat){
            int id=CharacterRoster.Skin(player.character,player.skin).id;
            if(!roomCharacterSkins.TryGetValue(seat,out int previous)||previous!=id){roomCharacterSkins[seat]=id;roomCharacterReady.Remove(seat);}
            var image=CharacterRoster.DisplayTexture(CharacterFraming.CardPath(id),true);
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
