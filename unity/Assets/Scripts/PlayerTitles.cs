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
        public static void DrawPill(Rect r,string name,GUIStyle style,string id=null) {
            if(string.IsNullOrEmpty(name))return;
            DrawTitleEntry(r,Find(id)??All.FirstOrDefault(item=>item.name==name),name,style);
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
        public static bool Earned(PlayerTitle t,PlayerRecordData d)=>t.condition=="default"||t.condition=="free"||t.condition=="achievement"&&TitleAchievements.GroupComplete(t.value,d);
    }
    [Serializable] public sealed class TitleCounter { public string name;public int value; }
    [Serializable] public sealed class TitleRule { public int id,group,type,target;public bool automatic;public string[] param;public string description; }
    [Serializable] sealed class TitleRuleCatalog { public TitleRule[] rules;public int freeAchievementCount; }
    // Source achievement IDs, task types, parameters and targets are preserved in
    // title-rules.json. No title is substituted with an approximate match count.
    public static class TitleAchievements {
        static TitleRuleCatalog catalog;
        static TitleRuleCatalog Catalog=>catalog??=JsonUtility.FromJson<TitleRuleCatalog>(Resources.Load<TextAsset>("Majsoul/Profile/title-rules").text);
        public static TitleRule[] Rules=>Catalog.rules;
        static int Number(string s)=>int.TryParse(s,out var n)?n:0;
        static int Value(PlayerRecordData d,int id)=>d.titleProgress.FirstOrDefault(x=>x.name==id.ToString())?.value??0;
        static void Store(PlayerRecordData d,int id,int value){var row=d.titleProgress.FirstOrDefault(x=>x.name==id.ToString());if(row==null)d.titleProgress.Add(row=new TitleCounter{name=id.ToString()});row.value=value;}
        static bool Complete(TitleRule r,PlayerRecordData d,int depth=0){
            if(depth>8)return false;if(r.automatic)return true;
            if(r.type==61)return Catalog.freeAchievementCount+Rules.Count(item=>item.type!=61&&Complete(item,d,depth+1))>=r.target;
            if(r.type==30){int a=Number(r.param[0]),b=Number(r.param[1]);int Level(int id)=>id==0?0:((id%10000)/100-1)*3;return (a==0||d.peak4>=Level(a))&&(b==0||d.peak3>=Level(b));}
            if(r.type==47)return r.param.Count(id=>Rules.FirstOrDefault(x=>x.id==Number(id)) is TitleRule item&&Complete(item,d,depth+1))>=r.target;
            return Value(d,r.id)>=r.target;
        }
        public static bool GroupComplete(int group,PlayerRecordData d){var items=Rules.Where(r=>r.group==group).ToArray();return items.Length>0&&items.All(r=>Complete(r,d));}
        public static string Progress(int group,PlayerRecordData d){var items=Rules.Where(r=>r.group==group).ToArray();return items.Count(r=>Complete(r,d))+"/"+items.Length;}
        static bool Applies(TitleRule r,string category,int n,int rounds){
            var p=r.param;if(p.Length<4||r.automatic||r.type==30||r.type==47||r.type==61)return false;
            // These source rules belong to special competition modes, which are
            // not represented by ordinary riichi tables. Never count normal play.
            if(r.group==13)return false;
            if(p[1]=="1"){if(category!="friend")return false;}
            else if(p[1]=="2"){if(p[2]!="1,2,3,4,6"||category!="ranked")return false;}
            else return false;
            int mode=(n==3?10:0)+(rounds==1?1:2);
            return p[3]=="0"||p[3].Split(',').Any(v=>Number(v)==mode);
        }
        public static void Record(PlayerRecordData d,RoomView v,StandingView me,string category,int n,int rounds){
            var stats=me.stats;var facts=stats?.titleFacts??Array.Empty<TitleCounter>();
            int Fact(string name)=>facts.FirstOrDefault(x=>x.name==name)?.value??0;
            foreach(var r in Rules){
                if(!Applies(r,category,n,rounds))continue;
                int add=0;bool maximum=false;var p=r.param;
                switch(r.type){
                    case 22:add=1;break;
                    case 24:add=p[4]=="4"?(stats?.wins??0)-(stats?.tsumo??0):p[4]=="3"?stats?.tsumo??0:0;break;
                    case 39:add=me.place==Number(p[4])?1:0;break;
                    case 13:add=me.score>=Number(p[4])?1:0;break;
                    case 49:add=me.score==Number(p[4])?1:0;break;
                    case 67:if(Value(d,r.id)<r.target)Store(d,r.id,me.place<=Number(p[4])?Value(d,r.id)+1:0);continue;
                    case 73:int initial=v.result.rules?.initialDealer??0;int wind=(v.you-initial+n)%n;add=me.place==1&&p.Length>5+wind&&p[5+wind]=="1"?1:0;break;
                    case 35:string[] limits={"满贯","跳满","倍满","三倍满","役满","累计役满"};for(int i=0;i<limits.Length&&i+4<p.Length;i++)if(p[i+4]=="1")add+=Fact("limit:"+limits[i]);break;
                    case 68:
                        string desc=r.description;int start=desc.IndexOf('「'),end=desc.IndexOf('」');string name=start>=0&&end>start?desc.Substring(start+1,end-start-1):"";
                        name=name.Replace("枪杠","抢杠").Replace("红宝牌","赤宝牌");
                        if(name=="役牌:门风牌")add=facts.Where(x=>x.name.StartsWith("yaku:自风")).Sum(x=>x.value);
                        else if(name=="役牌:场风牌")add=facts.Where(x=>x.name.StartsWith("yaku:场风")).Sum(x=>x.value);
                        else add=Fact("yaku:"+name);break;
                    case 71:for(int i=0;i<4;i++)if(p.Length>6+i&&p[6+i]=="1")add+=Fact("wind:"+"东南西北"[i]);break;
                    case 72:add=p.Last()=="0"?stats?.highlights?.kazoe??0:(stats?.highlights?.yakuman??0)+(stats?.highlights?.kazoe??0);break;
                    case 70:add=stats?.highlights?.nagashi??0;break;
                    case 34:add=Fact("bonus");break;
                    case 74:add=Fact("bonusMax");maximum=true;break;
                }
                if(add>0)Store(d,r.id,Math.Min(r.target,maximum?Math.Max(Value(d,r.id),add):Value(d,r.id)+add));
            }
        }
    }
}
