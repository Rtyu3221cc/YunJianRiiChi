using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi {
    [Serializable] public sealed class CharacterHandBinding {public string key;public int sourceItemId;public int[] characters;public string[] variants;public SourceHandAction[] actions;}
    [Serializable] sealed class CharacterHandDirectory {public CharacterHandBinding[] hands;}
    [Serializable] public sealed class CharacterHandClip {
        public int frames,fps;public float duration,landTime;public float[] tilePoints,tileRotations;public NativeMotionData pose;
        [NonSerialized] public string prefab;
    }
    public static class CharacterHands {
        static CharacterHandDirectory data;
        static readonly Dictionary<string,WardrobeItem> items=new Dictionary<string,WardrobeItem>();
        static readonly Dictionary<string,CharacterHandClip> clips=new Dictionary<string,CharacterHandClip>();
        static CharacterHandDirectory Data=>data??(data=JsonUtility.FromJson<CharacterHandDirectory>(Resources.Load<TextAsset>("Majsoul/CharacterHands/catalog").text));
        public static CharacterHandBinding[] All=>Data.hands;
        public static WardrobeItem Default(string role){
            int id=CharacterRoster.Find(role)?.id??0;var binding=Data.hands.FirstOrDefault(h=>h.characters.Contains(id));if(binding==null)return null;
            if(!items.TryGetValue(binding.key,out var item))items[binding.key]=item=new WardrobeItem{id=binding.key.Replace('_','-'),category="hand",name="角色默认手部",builtin=true,hiddenFromSelection=true,sourceItemId=binding.sourceItemId,nativeHand=binding.key,animationType=binding.key,motion="character-hand",duration=1.2f};
            return item;
        }
        public static SourceHandAction Action(WardrobeItem item,string action)=>string.IsNullOrEmpty(item?.nativeHand)?null:Data.hands.First(h=>h.key==item.nativeHand).actions.First(a=>a.action==action);
        public static CharacterHandClip Clip(WardrobeItem item,int physical,string action){
            var binding=Data.hands.First(h=>h.key==item.nativeHand);string prefab=binding.variants[binding.variants.Length==1?0:Mathf.Clamp(physical,0,3)],path=prefab+"-"+action;
            if(!clips.TryGetValue(path,out var clip)){var text=Resources.Load<TextAsset>(path);if(text==null)throw new InvalidOperationException("Missing character hand motion "+path);clips[path]=clip=JsonUtility.FromJson<CharacterHandClip>(text.text);clip.prefab=prefab;}
            return clip;
        }
    }
}
