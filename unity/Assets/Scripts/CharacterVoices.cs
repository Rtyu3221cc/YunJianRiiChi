using System.Collections.Generic;
using UnityEngine;
namespace YunJian.Riichi {
    public sealed class CharacterVoices:MonoBehaviour {
        AudioSource source;
        static readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        static readonly Dictionary<string,string> Actions=new Dictionary<string,string>{{"riichi","act_rich"},{"double-riichi","act_drich"},{"ron","act_ron"},{"tsumo","act_tumo"},{"chi","act_chi"},{"pon","act_pon"},{"kan","act_kan"},{"nuki","act_babei"}};
        void Awake(){source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;}
        public static AudioClip Clip(string character,string action){
            if(string.IsNullOrEmpty(action))return null;
            character=CharacterRoster.Find(character)?.key??"yiji";if(!CharacterRoster.CanDisplay(character))character="yiji";string file=Actions.TryGetValue(action,out var mapped)?mapped:action,key=character+"/"+file;
            if(!clips.TryGetValue(key,out var c)||c==null){
                c=CharacterPackService.Asset<AudioClip>("Voice/"+key);
                if(c==null&&file.Contains("/"))c=CharacterPackService.Asset<AudioClip>("Voice/"+character+"/"+System.IO.Path.GetFileName(file));
                if(c==null&&character=="yiji")c=Resources.Load<AudioClip>("Majsoul/Voice/yiji-"+action);
                clips[key]=c;
            }
            return c;
        }
        public void Play(string character,string action){var c=Clip(character,action);if(c==null||AppPreferences.Gain(4)<=0)return;source.Stop();source.clip=c;source.volume=AppPreferences.Gain(4);source.Play();}
    }
}
