using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi {
    [Serializable] public sealed class WardrobeItem {
        public string id,category,name,icon,texture,hand,table,preview,model,effect,nativeEffect,motion,motionSet,nailTexture,audio,cue;
        public float effectScale;
        public string animationType;
        public float calloutIntensity,calloutFaceOpacity;
        public float riichiNormalPoint,riichiSidewaysPoint;
        public float riichiSpeed,riichiLifetime,riichiCue;
        public bool standardStick,groundEffect;
        public float duration;
        public bool builtin;
        public bool trialStyle;
        public bool hiddenFromSelection;
        public float[] avatarWindow;
    }
    // Only catalog identifiers cross the LAN protocol. Paths always resolve locally.
    [Serializable] public sealed class WardrobeLoadout {
        public string stick,winEffect,riichiEffect,callout,hand,music,headFrame,cloth,tileBack,tileFace,lobbyBackground;
        public string Id(string category){var field=typeof(WardrobeLoadout).GetField(category);return field?.GetValue(this) as string;}
        public static WardrobeLoadout Local(){var value=new WardrobeLoadout();foreach(var f in typeof(WardrobeLoadout).GetFields())f.SetValue(value,WardrobeSelection.Get(f.Name)?.id);return value;}
    }
    public static class WardrobeRuntime {
        public static WardrobeItem Item(RoomView view,int seat,string category){
            var loadout=view!=null&&seat>=0&&seat<view.players.Length?view.players[seat].wardrobe:null;
            var item=WardrobeCatalog.Find(loadout?.Id(category));
            if(item==null||item.category!=category)item=view!=null&&seat==view.you?WardrobeSelection.Get(category):WardrobeCatalog.Items(category).FirstOrDefault();
            return category=="hand"?ResolveHand(item,view!=null&&seat>=0&&seat<view.players.Length?view.players[seat].character:CharacterRoster.Active.character):item;
        }
        public static WardrobeItem ResolveHand(WardrobeItem item,string character){
            if(item==null||item.category!="hand")return item;
            if(WardrobeCatalog.CanonicalHand(item.id)=="hand-cat-orange")return WardrobeCatalog.Find("hand-cat-orange");
            return WardrobeCatalog.Find(CharacterRoster.Find(character)?.sex==2?"hand-man":"hand-human");
        }
        public static string Motion(WardrobeItem item,string action){
            string suffix=action=="discard-hand"?"discard":action=="riichi-hand"?"riichi":action=="fulu-hand"?"meld":action=="tsumo-hand"?"tsumo":"hand";
            if(!string.IsNullOrEmpty(item?.motionSet))return item.motionSet+suffix;
            if(item!=null&&!string.IsNullOrEmpty(item.motion)&&item.motion.StartsWith("Majsoul/Wardrobe/Motion/cat-"))return "Majsoul/Wardrobe/Motion/cat-"+suffix;
            return "Majsoul/Actions/"+action;
        }
    }
    [Serializable] public sealed class WardrobeUi {public string key;public float[] border;}
    [Serializable] public sealed class WardrobeData {public int schema;public WardrobeItem[] items;public WardrobeUi[] ui;}
    public static class WardrobeCatalog {
        static WardrobeData data;
        static readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        public static WardrobeData Data=>data??(data=JsonUtility.FromJson<WardrobeData>(Resources.Load<TextAsset>("Majsoul/Wardrobe/catalog").text));
        public static WardrobeItem[] Items(string category)=>Data.items.Where(i=>i.category==category&&!i.hiddenFromSelection).ToArray();
        public static WardrobeItem Find(string id)=>Data.items.FirstOrDefault(i=>i.id==id);
        public static string CanonicalHand(string id)=>id=="hand-cat"?"hand-cat-orange":id=="hand-man"||id=="hand-dog"?"hand-human":id;
        public static Texture2D Texture(string path){if(string.IsNullOrEmpty(path))return null;if(!textures.TryGetValue(path,out var t))textures[path]=t=Resources.Load<Texture2D>(path);return t;}
        public static WardrobeUi Ui(string key)=>Data.ui.FirstOrDefault(i=>i.key==key);
        public static Rect AvatarFrameRect(Rect portrait,WardrobeItem item){
            var w=item.avatarWindow;if(w==null||w.Length!=4)return portrait;
            float width=portrait.width/w[2],height=portrait.height/w[3];return new Rect(portrait.x-w[0]*width,portrait.y-w[1]*height,width,height);
        }
    }
    public static class WardrobeSelection {
        const string Prefix="riichi.wardrobe.";
        static readonly Dictionary<string,string> selected=new Dictionary<string,string>();
        public static int Revision {get;private set;}
        public static WardrobeItem Get(string category){
            var items=WardrobeCatalog.Items(category);if(items.Length==0)return null;
            if(!selected.TryGetValue(category,out var id))selected[category]=id=PlayerPrefs.GetString(Prefix+category,items[0].id);
            if(category=="hand")selected[category]=id=WardrobeCatalog.CanonicalHand(id);
            return items.FirstOrDefault(i=>i.id==id)??items[0];
        }
        public static void Save(IEnumerable<KeyValuePair<string,string>> choices){
            foreach(var pair in choices){var item=WardrobeCatalog.Find(pair.Key=="hand"?WardrobeCatalog.CanonicalHand(pair.Value):pair.Value);if(item==null||item.category!=pair.Key)continue;selected[pair.Key]=item.id;
#if UNITY_EDITOR
                if(preview)continue;
#endif
                PlayerPrefs.SetString(Prefix+pair.Key,item.id);
            }
#if UNITY_EDITOR
            if(!preview)
#endif
            PlayerPrefs.Save();Revision++;
        }
#if UNITY_EDITOR
        static bool preview;static Dictionary<string,string> previous;
        public static void BeginPreview(){previous=new Dictionary<string,string>(selected);preview=true;}
        public static void EndPreview(){if(!preview)return;selected.Clear();foreach(var p in previous)selected[p.Key]=p.Value;previous=null;preview=false;Revision++;}
#endif
    }
}
