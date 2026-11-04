using System;
using System.Collections.Generic;
using UnityEngine;
namespace YunJian.Riichi {
    public sealed class CharacterVoices:MonoBehaviour {
        AudioSource source;
        public bool Playing=>source!=null&&source.isPlaying;
        public string PlayingCharacter{get;private set;}
        public string PlayingKey{get;private set;}
#if UNITY_EDITOR
        public event Action<string,string,AudioClip> ReviewPlayed;
        public event Action ReviewStopped;
#endif
        static readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        static readonly Dictionary<string,string> Actions=new Dictionary<string,string>{{"riichi","act_rich"},{"double-riichi","act_drich"},{"ron","act_ron"},{"tsumo","act_tumo"},{"chi","act_chi"},{"pon","act_pon"},{"kan","act_kan"},{"nuki","act_babei"}};
        // The first five original voice sets use the older Japanese action names.
        // Resolve within the same character; never replace their voice with another role.
        static readonly Dictionary<string,string> LegacyYaku=new Dictionary<string,string>{{"fan_liqi","fan_rich"},{"fan_dliqi","fan_drich"},{"fan_zimo","fan_tumo"}};
        void Awake(){source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;}
        public static AudioClip Clip(string character,string action){
            if(string.IsNullOrEmpty(action))return null;
            character=CharacterRoster.Find(character)?.key??"yiji";if(!CharacterRoster.CanDisplay(character))character="yiji";string file=Actions.TryGetValue(action,out var mapped)?mapped:action,key=character+"/"+file;
            CharacterPackService.Touch("Voice/"+key);if(!clips.TryGetValue(key,out var c)||c==null){
                c=CharacterPackService.Asset<AudioClip>("Voice/"+key);
                if(c==null&&LegacyYaku.TryGetValue(file,out var legacy))c=CharacterPackService.Asset<AudioClip>("Voice/"+character+"/"+legacy);
                if(c==null&&file.Contains("/"))c=CharacterPackService.Asset<AudioClip>("Voice/"+character+"/"+System.IO.Path.GetFileName(file));
                if(c==null&&character=="yiji")c=Resources.Load<AudioClip>("Majsoul/Voice/yiji-"+action);
                clips[key]=c;
            }
            return c;
        }
        public void Stop(){
#if UNITY_EDITOR
            if(PlayingKey!=null)ReviewStopped?.Invoke();
#endif
            if(source!=null)source.Stop();PlayingCharacter=PlayingKey=null;}
        public bool TryPlay(string character,string action){var c=Clip(character,action);if(c==null||AppPreferences.Gain(4)<=0){Stop();return false;}Stop();source.clip=c;source.volume=AppPreferences.Gain(4);PlayingCharacter=character;PlayingKey=action;source.Play();
#if UNITY_EDITOR
            ReviewPlayed?.Invoke(character,action,c);
#endif
            return true;}
        public void Play(string character,string action)=>TryPlay(character,action);
        void OnDisable()=>Stop();
    }
}
