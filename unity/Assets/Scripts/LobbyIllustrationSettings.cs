using System;
using System.Collections.Generic;
using UnityEngine;
namespace YunJian.Riichi {
    [Serializable] public struct LobbyIllustrationPose {public float x,y,scale;public static LobbyIllustrationPose Default=>new LobbyIllustrationPose{scale=1};}
    public static class LobbyIllustrationSettings {
        static readonly Dictionary<int,LobbyIllustrationPose> preview=new Dictionary<int,LobbyIllustrationPose>();
        public static LobbyIllustrationPose Get(int skin){if(CharacterRoster.PreviewOnly)return preview.TryGetValue(skin,out var p)?p:LobbyIllustrationPose.Default;string json=PlayerPrefs.GetString("riichi.lobbyIllustration.v1."+skin,"");if(string.IsNullOrEmpty(json))return LobbyIllustrationPose.Default;try{var p=JsonUtility.FromJson<LobbyIllustrationPose>(json);p.scale=Mathf.Clamp(p.scale,.35f,2.3f);return p;}catch{return LobbyIllustrationPose.Default;}}
        public static void Save(int skin,LobbyIllustrationPose p){p.scale=Mathf.Clamp(p.scale,.35f,2.3f);if(CharacterRoster.PreviewOnly){preview[skin]=p;return;}PlayerPrefs.SetString("riichi.lobbyIllustration.v1."+skin,JsonUtility.ToJson(p));PlayerPrefs.Save();}
    }
}
