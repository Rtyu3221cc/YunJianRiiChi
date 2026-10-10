using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi
{
    // Two local presentation stages; ready/next are still authoritative room operations.
    // Only published winning hands and scoring data are read, never private host state.
    public sealed class ResultPresentation
    {
        public enum Action { None, Ready, Next, Home }
        readonly Dictionary<string,Texture2D> art=new Dictionary<string,Texture2D>();
        string key; int winner; bool scores; float started, skipped;
        GUIStyle text, number; Font font;
        static readonly Color gold=new Color(1,.76f,.3f), white=new Color(.93f,.94f,.96f);
        public bool ShowingScores => scores;
        public int WinnerIndex => winner;
        public bool Freeze; public float FrozenAge;
        float Age => Freeze?FrozenAge:Time.unscaledTime-started+skipped;
        public void Reset(){key=null;winner=0;scores=false;skipped=0;Freeze=false;}
        public void Observe(RoomView v)
        {
            string next=v.audioRoom+":"+v.round+":"+v.honba+":"+v.result?.title+":"+string.Join(",",v.result?.delta??new int[0]);
            if(next==key)return;
            key=next;winner=0;scores=v.result?.details==null || v.result.details.Length==0;started=Time.unscaledTime;skipped=0;
        }
        float RevealEnd(WinDetail d) => .8f+(d.yaku?.Length??0)*.32f+1.1f;
        public void Advance(RoomView v)
        {
            if(scores){skipped=3.5f;return;}
            var d=v.result.details[winner];
            if(Age<RevealEnd(d)){skipped=RevealEnd(d);return;}
            if(++winner>=v.result.details.Length){scores=true;winner=v.result.details.Length-1;}
            started=Time.unscaledTime;skipped=0;FrozenAge=0;
        }
        Texture2D Art(string name)
        {
            if(!art.TryGetValue(name,out var t))art[name]=t=Resources.Load<Texture2D>("Majsoul/Results/"+name);
            return t;
        }
        void Image(string name,Rect r,ScaleMode mode=ScaleMode.StretchToFill)
        {var t=Art(name);if(t!=null)GUI.DrawTexture(r,t,mode);}
        void Label(Rect r,string value,int size,Color color,TextAnchor align=TextAnchor.MiddleCenter)
        {
            text.fontSize=size;text.alignment=align;text.normal.textColor=color;
            var old=GUI.color;GUI.color=Color.black;GUI.Label(new Rect(r.x+1,r.y+2,r.width,r.height),value,text);GUI.color=old;GUI.Label(r,value,text);
        }
        // Native APK sprite glyphs retain their brush shape, gradient and shadow.
        // Fit a complete run together; large scores never clip individual digits.
        void Glyphs(Rect r,string[] names,float height,TextAnchor align=TextAnchor.MiddleCenter)
        {
            var textures=names.Select(Art).Where(t=>t!=null).ToArray();
            float width=textures.Sum(t=>height*t.width/t.height)+Mathf.Max(0,textures.Length-1)*1.5f;
            float scale=Mathf.Min(1,Mathf.Min(r.width/Mathf.Max(1,width),r.height/height));height*=scale;width*=scale;
            float x=align==TextAnchor.MiddleRight?r.xMax-width:align==TextAnchor.MiddleLeft?r.x:r.center.x-width/2;
            foreach(var t in textures){float w=height*t.width/t.height;GUI.DrawTexture(new Rect(x,r.center.y-height/2,w,height),t);x+=w+1.5f*scale;}
        }
        static string[] Digits(int value,string prefix) => value.ToString(System.Globalization.CultureInfo.InvariantCulture).Select(c=>c=='-'?"ww_minus":prefix+c).ToArray();
        static string[] LimitGlyphs(string value)
        {
            var names=new List<string>();
            foreach(char c in value??""){
                string glyph=c=='满'?"man":c=='贯'?"guan":c=='跳'?"tiao":c=='倍'?"bei":c=='役'?"yi":c=='三'?"san":c=='累'?"lei":c=='计'?"ji":
                    c=='2'?"liang":c=='3'?"san":c=='4'?"si":c=='5'?"wu":c=='6'?"liu":null;
                if(glyph!=null)names.Add("word_"+glyph);else if(char.IsDigit(c))names.Add("h_"+c);
            }
            return names.ToArray();
        }
        void Tile(TableRenderer table,string tile,Rect r)
        {
            // Share the actual gameplay atlas, tile rim and shader; fixed 0.62 aspect.
            var t=table.HandTexture(tile);
            if(t!=null)GUI.DrawTexture(r,t,ScaleMode.ScaleToFit);
        }
        void Dora(RoomView v,TableRenderer table,string[] tiles,float x,float y,string title)
        {
            if(tiles==null || tiles.Length==0)return;
            Image(title,new Rect(x-80,y+4,76,36),ScaleMode.ScaleToFit);
            for(int i=0;i<5;i++){
                // Match the table HUD's indicators, including the selected back skin.
                GUI.DrawTexture(new Rect(x+i*29,y,28,43.5f),table.DoraTexture(i<tiles.Length?tiles[i]:null),ScaleMode.ScaleToFit);
            }
        }
        void WinningHand(WinDetail d,TableRenderer table)
        {
            var tiles=(d.tiles??new string[0]).ToList();
            var melds=d.melds??new string[0];int count=tiles.Count+melds.Sum(m=>m.Skip(1).Count(char.IsDigit));
            float pitch=Mathf.Min(40,625f/Mathf.Max(14,count+2)),x=494;
            var matrix=GUI.matrix;GUIUtility.RotateAroundPivot(-6,new Vector2(850,118));
            foreach(var tile in tiles){Tile(table,tile,new Rect(x,85,pitch-1,pitch*1.5f));x+=pitch;}
            foreach(var meld in melds){x+=8;int n=0;var digits=meld.Skip(1).Where(char.IsDigit).ToArray();foreach(char c in digits){
                // Concealed kans retain their face-down ends in the scoring hand.
                if(digits.Length==4 && !meld.Any(ch=>ch=='+'||ch=='-'||ch=='=' ) && (n==0||n==3)){
                    Tile(table,null,new Rect(x,85,pitch-1,pitch*1.5f));
                }else Tile(table,meld[0].ToString()+c,new Rect(x,85,pitch-1,pitch*1.5f));
                x+=pitch;n++;
            }}
            Tile(table,d.tile,new Rect(x+22,85,pitch-1,pitch*1.5f));GUI.matrix=matrix;
        }
        void Details(RoomView v,TableRenderer table,TableEffects effects)
        {
            var d=v.result.details[winner];float age=Age;float entrance=Mathf.SmoothStep(0,1,age/.45f);
            var matrix=GUI.matrix;GUI.matrix*=Matrix4x4.Translate(new Vector3((1-entrance)*1280,0,0));
            Image("bg_jiesuan",new Rect(0,5,1280,705));
            var portrait=effects.Art("yiji-full");if(portrait!=null)GUI.DrawTexture(new Rect(20,48,475,805),portrait,ScaleMode.ScaleToFit);
            WinningHand(d,table);
            Image("bg_nickname",new Rect(0,571,375,57));Label(new Rect(24,580,305,40),d.name,23,white);
            var word=effects.Art(d.ron?"show_rong":"show_zimo");if(word!=null)GUI.DrawTexture(new Rect(1122,137,115,75),word,ScaleMode.ScaleToFit);
            GUIUtility.RotateAroundPivot(-6,new Vector2(870,380));
            Dora(v,table,v.indicators,663,198,"bg_dora");Dora(v,table,d.ura,990,198,"bg_lidora");
            if(d.nuki>0)Label(new Rect(545,247,210,29),"拔北 × "+d.nuki,19,gold,TextAnchor.MiddleLeft);
            var yaku=d.yaku??new YakuDetail[0];int visible=Mathf.Clamp(Mathf.FloorToInt((age-.65f)/.32f),0,yaku.Length);
            int rows=Math.Max(1,(yaku.Length+1)/2);float step=Mathf.Min(43,252f/rows);
            for(int i=0;i<visible;i++){
                int col=i/rows,row=i%rows;float x=537+col*425,y=285+row*step;
                Image("bg_fan",new Rect(x,y,259,step));Label(new Rect(x+10,y,189,step),yaku[i].name,rows>7?18:22,white);
                var fanGlyphs=yaku[i].yakuman>0?(yaku[i].yakuman==1?new[]{"word_yiman"}:Digits(yaku[i].yakuman,"h_").Concat(new[]{"word_yiman"}).ToArray()):Digits(yaku[i].han,"h_").Concat(new[]{"word_fan"}).ToArray();
                Glyphs(new Rect(x+202,y,55,step),fanGlyphs,Mathf.Min(27,step-3));
            }
            if(age>=.8f+yaku.Length*.32f){
                Image("bg_center_fan",new Rect(824,375,101,101));
                var score=d.score??new ScoreDetail();
                if(score.damanguan>0)Glyphs(new Rect(833,387,83,78),new[]{"word_yiman"},49);
                else {
                    Glyphs(new Rect(837,380,65,49),Digits(score.fanshu,"h_"),44);
                    Image("word_fan",new Rect(880,419,31,37),ScaleMode.ScaleToFit);
                    Glyphs(new Rect(832,438,50,23),Digits(score.fu,"ww_").Concat(new[]{"word_fu"}).ToArray(),20);
                }
                Image("bg_point",new Rect(861,557,337,76));
                Glyphs(new Rect(856,533,346,88),Digits(score.defen,"ww_").Concat(new[]{"word_point"}).ToArray(),72,TextAnchor.MiddleRight);
                if(!string.IsNullOrEmpty(d.limit)){
                    Image("bg_yakuman",new Rect(553,529,295,94));
                    Glyphs(new Rect(566,523,272,104),LimitGlyphs(d.limit),94);
                }
            }
            GUI.matrix=matrix;
            if(v.result.details.Length>1)Label(new Rect(835,656,180,32),"和牌 "+(winner+1)+" / "+v.result.details.Length,17,white);
        }
        public static int Before(RoomView v,int seat) => v.players[seat].score-(v.result?.delta!=null&&seat<v.result.delta.Length?v.result.delta[seat]:0);
        public static int Rank(RoomView v,int seat,bool before) => 1+Enumerable.Range(0,v.n).Count(i=>{
            int a=before?Before(v,i):v.players[i].score,b=before?Before(v,seat):v.players[seat].score;
            return a>b || (a==b && i<seat);
        });
        public int DisplayScore(RoomView v,int seat) => Mathf.RoundToInt(Mathf.Lerp(Before(v,seat),v.players[seat].score,Mathf.SmoothStep(0,1,(Age-.8f)/1.65f))/100)*100;
        static Vector2 Center(int relative,int n) => relative==0?new Vector2(640,558):relative==n-1?new Vector2(232,343):relative==1&&n==4?new Vector2(1048,343):new Vector2(640,134);
        void Arrow(Vector2 a,Vector2 b,float alpha)
        {
            var matrix=GUI.matrix;var old=GUI.color;GUI.color=new Color(1,1,1,alpha);
            Vector2 delta=b-a;GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg+90,a);
            Image("scorechangearrow_0",new Rect(a.x-13,a.y-delta.magnitude,26,delta.magnitude));GUI.matrix=matrix;GUI.color=old;
        }
        void Scores(RoomView v,TableRenderer table)
        {
            Label(new Rect(415,25,450,42),v.result.matchOver?"终局 · 点数结算":v.result.title+" · 点数结算",27,gold);
            if(Age>.3f && Age<2.7f){
                var details=v.result.details??new WinDetail[0];
                foreach(var d in details)foreach(int seat in Enumerable.Range(0,v.n).Where(i=>v.result.delta[i]<0)){
                    Vector2 a=Center((seat-v.you+v.n)%v.n,v.n),b=Center((d.seat-v.you+v.n)%v.n,v.n);Vector2 dir=(b-a).normalized;
                    Arrow(a+dir*118,b-dir*118,Mathf.Clamp01((Age-.3f)/.3f)*Mathf.Clamp01((2.7f-Age)/.4f));
                }
            }
            for(int seat=0;seat<v.n;seat++){
                int relative=(seat-v.you+v.n)%v.n;Vector2 c=Center(relative,v.n);Rect r=new Rect(c.x-160,c.y-69,320,138);
                Image("scorechange_bg",r);var portrait=table.Portrait(v.players[seat]);if(portrait!=null)GUI.DrawTexture(new Rect(r.x+7,r.y+11,89,99),portrait,ScaleMode.ScaleToFit);
                Label(new Rect(r.x+107,r.y+3,198,30),v.players[seat].name,20,white,TextAnchor.MiddleLeft);
                Label(new Rect(r.x+109,r.y+32,189,40),DisplayScore(v,seat).ToString(),31,gold,TextAnchor.MiddleRight);
                int delta=v.result.delta!=null&&seat<v.result.delta.Length?v.result.delta[seat]:0;
                Label(new Rect(r.x+100,r.y+79,105,31),(delta>=0?"+":"")+delta,24,delta>=0?new Color(.64f,.85f,1):new Color(1,.42f,.34f));
                int prior=Rank(v,seat,true),rank=Rank(v,seat,false);
                Label(new Rect(r.x+215,r.y+83,90,29),Age<2.5f?prior+" 位":rank+" 位",25,rank==1?gold:white);
                if(Age>=2.5f && prior!=rank)Label(new Rect(r.x+194,r.y+84,28,29),prior>rank?"↑":"↓",22,prior>rank?new Color(.5f,.9f,1):new Color(1,.4f,.3f));
            }
            Label(new Rect(430,318,420,55),v.result.matchOver?"最终排名":v.result.repeat?"连庄":"本局结束",27,white);
        }
        public Action Draw(RoomView v,TableRenderer table,TableEffects effects,Font gameFont,Func<Rect,string,bool> button)
        {
            Observe(v);if(text==null){font=gameFont;text=new GUIStyle(GUI.skin.label){font=font,wordWrap=false,clipping=TextClipping.Clip};number=new GUIStyle(text);}
            if(!scores){Details(v,table,effects);if(button(new Rect(1050,650,180,46),Age<RevealEnd(v.result.details[winner])?"跳过":"确认"))Advance(v);}
            else {
                Scores(v,table);
                if(!v.players[v.you].ready && button(new Rect(1050,650,180,46),"确认结算")){Advance(v);return Action.Ready;}
                if(v.players[v.you].ready)Label(new Rect(850,654,180,38),"已确认 · 等待其他玩家",17,white);
                if(v.host && v.players.All(p=>p.ready) && button(new Rect(1050,650,180,46),v.result.matchOver?"再来一场":"下一局"))return Action.Next;
                if(button(new Rect(38,650,165,46),"返回大厅"))return Action.Home;
            }
            return Action.None;
        }
    }
}
