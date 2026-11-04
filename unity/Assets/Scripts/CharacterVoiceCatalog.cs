using System;
using System.Linq;
using UnityEngine;
namespace YunJian.Riichi {
    [Serializable] public sealed class CharacterVoiceLine {public string type,name,text,path;public bool available;}
    [Serializable] public sealed class CharacterVoicePage {public string key;public CharacterVoiceLine[] lines;}
    [Serializable] sealed class CharacterVoiceDirectory {public int schema;public CharacterVoicePage[] roles;}
    public static class CharacterVoiceCatalog {
        static CharacterVoiceDirectory data;
        public static CharacterVoiceLine[] Lines(string role){if(data==null)data=JsonUtility.FromJson<CharacterVoiceDirectory>(Resources.Load<TextAsset>("Majsoul/Characters/voice-directory").text);return data.roles.FirstOrDefault(x=>x.key==role)?.lines??Array.Empty<CharacterVoiceLine>();}
    }
}
