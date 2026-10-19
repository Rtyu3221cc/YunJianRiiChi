using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi {
    [Serializable] public sealed class CharacterChoice { public string character; public int skin; public CharacterChoice(){} public CharacterChoice(string c,int s){character=c;skin=s;} }
    [Serializable] public sealed class CharacterSkin {
        public int id,spineWidth,spineHeight,pivotX,pivotY; public string key,name,description,lobbyOffset,dormOffset; public bool dynamic; public string[] parts;
    }
    [Serializable] public sealed class CharacterDefinition {
        public int id,defaultSkin,bondSkin; public string key,name,cv,height,birth,age,blood,hobby,description; public CharacterSkin[] skins; public string[] emojis;
    }
    [Serializable] public sealed class CharacterUiArt {public string key;public int width,height;public float[] border;}
    [Serializable] sealed class CharacterCatalog { public CharacterDefinition[] roles; public CharacterUiArt[] ui; }
    [Serializable] sealed class CharacterPreferences {
        public string dorm="yiji"; public CharacterChoice active=new CharacterChoice("yiji",400101);
        public CharacterChoice[] outfits=new CharacterChoice[0],pool=new CharacterChoice[0];public string[] favorites=new string[0],order=new string[0];public bool random;
    }
    // Dorm outfits, home illustration and the server's per-match choice are independent.
    public static class CharacterRoster {
        const string PrefKey="riichi.characters.v1";static CharacterCatalog catalog;static CharacterPreferences prefs;
        static readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        public static bool PreviewOnly;
        public static readonly HashSet<string> RemoteFallbackKeys=new HashSet<string>();
        static void Load(){
            if(catalog!=null)return;
            catalog=JsonUtility.FromJson<CharacterCatalog>((Resources.Load<TextAsset>("Majsoul/CharacterDirectory/catalog")??Resources.Load<TextAsset>("Majsoul/Characters/catalog")).text);
            try{prefs=JsonUtility.FromJson<CharacterPreferences>(PlayerPrefs.GetString(PrefKey,"{}"));}catch{prefs=null;}
            if(prefs==null)prefs=new CharacterPreferences();
            prefs.outfits=prefs.outfits??new CharacterChoice[0];prefs.pool=prefs.pool??new CharacterChoice[0];prefs.favorites=prefs.favorites??new string[0];prefs.order=prefs.order??new string[0];
            if(Find(prefs.dorm)==null||!Available(prefs.dorm))prefs.dorm="yiji";
            if(prefs.active==null||!Valid(prefs.active)||!Available(prefs.active.character))prefs.active=new CharacterChoice("yiji",400101);
            prefs.pool=prefs.pool.Where(x=>Valid(x)&&Available(x.character)).GroupBy(x=>x.character+":"+x.skin).Select(x=>x.First()).ToArray();
        }
        static bool Valid(CharacterChoice c)=>c!=null&&Find(c.character)?.skins.Any(s=>s.id==c.skin)==true;
        public static CharacterDefinition[] All {get{Load();return catalog.roles;}}
        public static CharacterDefinition Find(string key){if(catalog==null)Load();return catalog.roles.FirstOrDefault(r=>r.key==key);}
        public static bool Available(string key)=>CharacterPackService.Installed(key);
        public static bool CanDisplay(string key)=>Available(key)&&!RemoteFallbackKeys.Contains(key);
        public static CharacterSkin Skin(string character,int id){var r=Find(character)??Find("yiji");if(!CanDisplay(r.key))r=Find("yiji");return r.skins.FirstOrDefault(s=>s.id==id)??r.skins.First(s=>s.id==r.defaultSkin);}
        public static CharacterDefinition Dorm {get{Load();return Find(prefs.dorm);}set{Load();if(value!=null&&Available(value.key)){prefs.dorm=value.key;Save();}}}
        public static CharacterChoice Active {get{Load();return new CharacterChoice(prefs.active.character,prefs.active.skin);}}
        public static bool Random {get{Load();return prefs.random;}set{Load();prefs.random=value;Save();}}
        public static CharacterChoice[] Pool {get{Load();return prefs.pool.Select(c=>new CharacterChoice(c.character,c.skin)).ToArray();}}
        public static int Outfit(string key){Load();return prefs.outfits.FirstOrDefault(c=>c.character==key)?.skin??(key=="weilai"?406903:Find(key).defaultSkin);}
        public static void Equip(string key,int skin){Load();if(!Valid(new CharacterChoice(key,skin))||!Available(key))return;prefs.outfits=prefs.outfits.Where(c=>c.character!=key).Append(new CharacterChoice(key,skin)).ToArray();Save();}
        public static void Use(string key,int skin){Load();var c=new CharacterChoice(key,skin);if(!Valid(c)||!Available(key))return;prefs.active=c;Save();}
        public static bool Favorite(string key){Load();return prefs.favorites.Contains(key);}
        public static void Star(string key){Load();if(!Available(key))return;prefs.favorites=Favorite(key)?prefs.favorites.Where(c=>c!=key).ToArray():prefs.favorites.Append(key).ToArray();Save();}
        public static CharacterDefinition[] Ordered(bool favoritesOnly=false,bool installedOnly=false){Load();return catalog.roles.Where(r=>(!favoritesOnly||Favorite(r.key))&&(!installedOnly||Available(r.key))).OrderByDescending(r=>Favorite(r.key)).ThenBy(r=>{int i=Array.IndexOf(prefs.order,r.key);return i<0?100+Array.IndexOf(catalog.roles,r):i;}).ToArray();}
        public static void Reorder(string[] keys){Load();prefs.order=keys.Where(k=>Find(k)!=null).Distinct().ToArray();Save();}
        public static bool InPool(string key,int skin){Load();return prefs.pool.Any(c=>c.character==key&&c.skin==skin);}
        public static void TogglePool(string key,int skin){Load();if(InPool(key,skin))prefs.pool=prefs.pool.Where(c=>c.character!=key||c.skin!=skin).ToArray();else if(Available(key)&&Valid(new CharacterChoice(key,skin)))prefs.pool=prefs.pool.Append(new CharacterChoice(key,skin)).ToArray();Save();}
        public static void RemovePoolCharacter(string key){Load();prefs.pool=prefs.pool.Where(c=>c.character!=key).ToArray();Save();}
        public static void ClearPool(){Load();prefs.pool=new CharacterChoice[0];Save();}
        public static Texture2D Texture(string relative){if(!textures.TryGetValue(relative,out var t)||t==null){t=CharacterPackService.Asset<Texture2D>(relative);if(t!=null)textures[relative]=t;}return t;}
        public static Texture2D Portrait(PlayerView p,string kind="full") {var s=Skin(p?.character,p?.skin??0);return Texture("Skins/"+s.id+"/"+kind)??Texture("Skins/"+s.id+"/bighead");}
        public static Texture2D PresentationPortrait(PlayerView p){var full=Portrait(p);return full.width>full.height*1.1f?Portrait(p,"bighead"):full;}
        public static CharacterUiArt Ui(string key){Load();return catalog.ui.FirstOrDefault(x=>x.key==key);}
        public static void FillRequest(RequestBody request){var a=Active;var pool=Pool;request.character=a.character;request.skin=a.skin;request.randomCharacters=Random&&pool.Length>0;request.characterPool=pool;}
        static void Save(){if(PreviewOnly)return;PlayerPrefs.SetString(PrefKey,JsonUtility.ToJson(prefs));PlayerPrefs.Save();}
        public static void ReconcileInstalled(){textures.Clear();if(prefs==null)return;prefs.favorites=prefs.favorites.Where(Available).ToArray();prefs.pool=prefs.pool.Where(p=>Available(p.character)).ToArray();prefs.outfits=prefs.outfits.Where(p=>Available(p.character)).ToArray();if(!Available(prefs.active.character))prefs.active=new CharacterChoice("yiji",400101);if(!Available(prefs.dorm))prefs.dorm="yiji";Save();}
        public static void BeginPreview(){PreviewOnly=true;catalog=null;prefs=null;textures.Clear();Load();prefs=new CharacterPreferences();}
        public static void EndPreview(){catalog=null;prefs=null;PreviewOnly=false;}
    }
}
