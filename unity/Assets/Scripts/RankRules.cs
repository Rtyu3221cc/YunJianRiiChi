using System;
using UnityEngine;

namespace YunJian.Riichi {
    // Rank level 0-14: 初心/雀士/雀杰/雀豪/雀圣 1-3 stars. 15+: 魂天 Lv(level-14).
    // Tier levels store rank points; 魂天 stores soul pearls in tenths (10.0 = 100).
    [Serializable] public sealed class RankState { public int level, points; public RankState(){} public RankState(int l,int p){level=l;points=p;} public RankState Copy()=>new RankState(level,points); }
    [Serializable] public sealed class RankChange { public RankState before, after; public int delta, placement, raw; public bool promoted, demoted; }
    [Serializable] sealed class PlacementTable { public int[] fourEast={40,20,0}, fourSouth={80,40,0}, threeEast={50,0}, threeSouth={100,0}; }

    public static class RankRules {
        public const int SoulLevel=15, SoulStart=100, SoulPromote=200;
        public static readonly string[] Tiers={"初心","雀士","雀杰","雀豪","雀圣","魂天"};
        static readonly int[] start={0,0,0,300,400,500,600,700,1000,1400,1600,1800,2000,3000,4500};
        static readonly int[] promote={20,80,200,600,800,1000,1200,1400,2000,2800,3200,3600,4000,6000,9000};
        // Last-place deduction per level as {东风, 半庄}; the reference table differs for 三麻 from 雀豪2.
        static readonly int[,] lastFour={{0,0},{0,0},{0,0},{10,20},{20,40},{30,60},{40,80},{50,100},{60,120},{80,165},{90,180},{100,195},{110,210},{120,225},{130,240}};
        static readonly int[,] lastThree={{0,0},{0,0},{0,0},{10,20},{20,40},{30,60},{40,80},{50,100},{60,120},{80,165},{95,190},{110,215},{125,240},{140,265},{160,290}};
        static readonly int[][] soulFour={new[]{3,1,-1,-3},new[]{5,2,-2,-5}}, soulThree={new[]{3,0,-3},new[]{5,0,-5}};
        static PlacementTable table;
        static PlacementTable Table {get{
            if(table!=null)return table;
            var asset=Resources.Load<TextAsset>("Majsoul/Profile/rank-rules");
            try{table=asset==null?null:JsonUtility.FromJson<PlacementTable>(asset.text);}catch(Exception e){Debug.LogWarning("rank-rules.json: "+e.Message);table=null;}
            return table??=new PlacementTable();
        }}
        static readonly string[] badgeKeys={"fish","queshi","quejie","quehao","quesheng","huntian"};
        static readonly System.Collections.Generic.Dictionary<string,Texture2D> badges=new System.Collections.Generic.Dictionary<string,Texture2D>();
        public static Texture2D Badge(int level,int n){string key=(n==3?"sanma_":"sima_")+badgeKeys[Tier(level)];if(!badges.TryGetValue(key,out var t))badges[key]=t=Resources.Load<Texture2D>("Majsoul/Profile/Rank/"+key);return t;}
        // Humans publish ranks; computer ranks are fixed per room difficulty.
        public static bool Known(PlayerView p)=>p!=null&&(p.bot||p.profile!=null&&p.profile.set);
        public static int BotLevel(int aiLevel)=>aiLevel==2?5:2;
        public static string BotDifficultyName(int aiLevel)=>aiLevel==2?"普通":"简单";
        public static int Level(PlayerView p,int n)=>p?.bot==true?BotLevel(p.aiLevel):Known(p)?(n==3?p.profile.rank3:p.profile.rank4):0;
        public static string TitleOf(PlayerView p)=>Known(p)&&!p.bot?p.profile.title??"":"";
        public static bool Soul(int level)=>level>=SoulLevel;
        public static int Tier(int level)=>Soul(level)?5:Mathf.Clamp(level,0,14)/3;
        public static int Stars(int level)=>Soul(level)?0:level%3+1;
        public static int Start(int level)=>Soul(level)?SoulStart:start[Mathf.Clamp(level,0,14)];
        public static int Promote(int level)=>Soul(level)?SoulPromote:promote[Mathf.Clamp(level,0,14)];
        public static int LastPenalty(int level,int n,bool south)=>Soul(level)?0:(n==3?lastThree:lastFour)[Mathf.Clamp(level,0,14),south?1:0];
        public static string Name(int level)=>Soul(level)?"魂天 Lv"+(level-SoulLevel+1):Tiers[Tier(level)]+" "+Stars(level)+"星";
        public static string ShortName(int level)=>Soul(level)?"魂天"+(level-SoulLevel+1):Tiers[Tier(level)]+Stars(level);
        public static string Points(RankState s)=>Soul(s.level)?(s.points/10f).ToString("0.0"):s.points.ToString();
        public static string Progress(RankState s)=>Points(s)+"/"+(Soul(s.level)?(SoulPromote/10f).ToString("0.0"):Promote(s.level).ToString());
        public static string Delta(RankChange c)=>c==null?"":Soul(c.before.level)?(c.delta>=0?"+":"")+(c.delta/10f).ToString("0.0"):(c.delta>=0?"+":"")+c.delta;
        // Half away from zero, so ±500 points of 素点 count as ±1.
        public static int Soten(int score,int startScore){double v=(score-startScore)/1000.0;return (int)Math.Round(v,MidpointRounding.AwayFromZero);}
        public static int Placement(int level,int place,int n,int rounds){
            bool south=rounds!=1;if(place>=n)return -LastPenalty(level,n,south);
            var t=Table;var row=n==3?(south?t.threeSouth:t.threeEast):(south?t.fourSouth:t.fourEast);
            return row!=null&&place-1<row.Length?row[place-1]:0;
        }
        // One match changes at most one level; points restart at the new level's initial value.
        public static RankChange Apply(RankState current,int place,int score,int n,int rounds,int startScore,bool allSoul){
            var c=new RankChange{before=current.Copy()};var s=current.Copy();bool south=rounds!=1;
            if(Soul(s.level)){
                var row=(n==3?soulThree:soulFour)[south?1:0];int d=row[Mathf.Clamp(place-1,0,row.Length-1)]*(allSoul?2:1);
                c.placement=d;c.raw=d;c.delta=d;s.points+=d;
                if(s.points>=SoulPromote){s.level++;s.points=SoulStart;c.promoted=true;}
                else if(s.points<0){c.demoted=true;if(s.level==SoulLevel){s.level=14;s.points=start[14];}else{s.level--;s.points=SoulStart;}}
            }else{
                c.placement=Placement(s.level,place,n,rounds);c.raw=c.placement+Soten(score,startScore);c.delta=c.raw;s.points+=c.raw;
                if(s.points>=Promote(s.level)){s.level++;s.points=Start(s.level);c.promoted=true;}
                else if(s.points<0){if(s.level<3){c.delta-=s.points;s.points=0;}else{s.level--;s.points=Start(s.level);c.demoted=true;}}
            }
            c.after=s;return c;
        }
    }
}
