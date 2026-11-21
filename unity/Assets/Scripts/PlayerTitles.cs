using System;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi {
    [Serializable] public sealed class PlayerTitle { public string id, name, description, condition, resource; public int value, sourceId, priority; }
    [Serializable] sealed class PlayerTitleCatalog { public PlayerTitle[] titles = new PlayerTitle[0]; }
    // Local unlock conditions retain saved IDs. Visuals use the configured original title images.
    public static class PlayerTitles {
        static PlayerTitle[] all;
        public static PlayerTitle[] All {get{
            if(all!=null)return all;
            var asset=Resources.Load<TextAsset>("Majsoul/Profile/titles");
            try{all=asset==null?null:JsonUtility.FromJson<PlayerTitleCatalog>(asset.text)?.titles;}catch(Exception e){Debug.LogWarning("titles.json: "+e.Message);}
            if(all==null||all.Length==0)all=new[]{new PlayerTitle{id="traveler",name="云间旅人",description="初入云间的牌友",condition="default"}};
            return all;
        }}
        public static string Default=>All.FirstOrDefault(t=>t.condition=="default")?.id??All[0].id;
        public static PlayerTitle Find(string id)=>All.FirstOrDefault(t=>t.id==id);
        public static string NameOf(string id)=>Find(id)?.name??"";
        static readonly System.Collections.Generic.Dictionary<string,Texture2D> images = new System.Collections.Generic.Dictionary<string,Texture2D>();
        public static void DrawTitle(Rect r,string id,GUIStyle style) {
            var entry=Find(id);
            DrawTitleEntry(r,entry,entry?.name??"",style);
        }
        public static void DrawPill(Rect r,string name,GUIStyle style) {
            if(string.IsNullOrEmpty(name))return;
            DrawTitleEntry(r,All.FirstOrDefault(item=>item.name==name),name,style);
        }
        static void DrawTitleEntry(Rect r,PlayerTitle entry,string name,GUIStyle style) {
            string resource=entry?.resource;
            if(!string.IsNullOrEmpty(resource)) {
                if(!images.TryGetValue(resource,out var image))images[resource]=image=Resources.Load<Texture2D>("Majsoul/Profile/"+resource);
                if(image!=null){GUI.DrawTexture(r,image,ScaleMode.ScaleToFit,true);return;}
            }
            var label=new GUIStyle(style){alignment=TextAnchor.MiddleCenter,wordWrap=false,clipping=TextClipping.Clip,fontSize=Mathf.Max(8,Mathf.RoundToInt(r.height*.5f))};
            label.normal.textColor=Color.white;
            GUI.Label(r,name,label);
        }
        public static bool Earned(PlayerTitle t,PlayerRecordData d){
            switch(t.condition){
                case "default":return true;
                case "rank":return Math.Max(d.rank4.level,d.rank3.level)>=t.value||Math.Max(d.peak4,d.peak3)>=t.value;
                case "yakuman":return d.modes.Sum(m=>m.highlights.yakuman)>=t.value;
                case "matches":return d.modes.Sum(m=>m.totals.matches)>=t.value;
                default:return false;
            }
        }
    }
}
