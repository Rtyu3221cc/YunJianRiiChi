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
        readonly NativeUiEffect limitFire=new NativeUiEffect();
        public int LimitFireLayers=>limitFire.LayerCount;
        public void ClearEffects()=>limitFire.Dispose();
        readonly Dictionary<string,Texture2D> art=new Dictionary<string,Texture2D>();
        string key; int winner; bool scores,drawStage,finalPage; float started, skipped;
        public bool ShowingFinal=>finalPage;
        GUIStyle text, number, roundText; Font font;
        [Serializable] sealed class FinalItem {public string name;public float[] rect;public float fontSize;}
        [Serializable] sealed class FinalCard {public float[] rect;public float scale;public FinalItem[] items;}
        [Serializable] sealed class FinalLayout {public FinalCard[] cards;}
        FinalLayout finalLayout;
        static readonly Color gold=new Color(1,.76f,.3f), white=new Color(.93f,.94f,.96f);
        public bool ShowingScores => scores;
        public bool ShowingDraw => drawStage;
        public int WinnerIndex => winner;
        public bool Freeze; public float FrozenAge;
        float Age => Freeze?FrozenAge:Time.unscaledTime-started+skipped;
        public float ElapsedAge=>Age;
        public int SkipSerial {get;private set;}
        public void Reset(){limitFire.Dispose();key=null;winner=0;scores=false;drawStage=false;finalPage=false;skipped=0;Freeze=false;}
        public void Observe(RoomView v)
        {
            string next=v.audioRoom+":"+v.matchSeq+":"+v.handSeq+":"+v.round+":"+v.honba+":"+v.result?.title+":"+string.Join(",",v.result?.delta??new int[0]);
            if(next==key)return;
            limitFire.Dispose();key=next;winner=0;drawStage=v.result!=null && v.result.draw && ((!v.result.abort && (v.result.drawDetails?.Length??0)>0) || v.result.title=="九种九牌");
            finalPage=false;scores=!drawStage && (v.result?.details==null || v.result.details.Length==0);started=Time.unscaledTime;skipped=0;
        }
        float RevealEnd(WinDetail d) => SettlementAudio.Sequence(d).end;
        public void Advance(RoomView v)
        {
            limitFire.Dispose();
            if(drawStage){drawStage=false;scores=true;started=Time.unscaledTime;skipped=0;FrozenAge=0;return;}
            if(scores){if(Age<ScoreEnd(v.n)){if(Freeze)FrozenAge=ScoreEnd(v.n)+.1f;else skipped+=ScoreEnd(v.n)+.1f-Age;SkipSerial++;}else if(v.result.matchOver){finalPage=true;started=Time.unscaledTime;skipped=0;FrozenAge=0;}return;}
            var d=v.result.details[winner];
            if(Age<RevealEnd(d)){if(Freeze)FrozenAge=RevealEnd(d);else skipped+=RevealEnd(d)-Age;SkipSerial++;return;}
            if(++winner>=v.result.details.Length){scores=true;winner=v.result.details.Length-1;}
            started=Time.unscaledTime;skipped=0;FrozenAge=0;
        }
        // Auto-play uses the same pages and timing as a human. Run once per
        // Update, after table effects and settlement audio, never per GUI pass.
        public int ScoreConfirmRemaining(RoomView v)=>Mathf.Max(0,3-Mathf.FloorToInt(Age-ScoreEnd(v.n)));
        public int YakuConfirmRemaining(RoomView v)=>Mathf.Max(0,3-Mathf.FloorToInt(Age-RevealEnd(v.result.details[winner])));
        public Action TickAuto(RoomView v)=>Tick(v,true);
        public Action Tick(RoomView v,bool autoPlay)
        {
            Observe(v);
            if(v.players[v.you].ready)return Action.None;
            if(finalPage)return Age>=9?Action.Ready:Action.None;
            if(drawStage){if(autoPlay&&Age>=1.7f)Advance(v);return Action.None;}
            if(!scores){if(YakuConfirmRemaining(v)==0)Advance(v);return Action.None;}
            if(ScoreConfirmRemaining(v)>0)return Action.None;
            if(v.result.matchOver){Advance(v);return Action.None;}
            return Action.Ready;
        }
        Texture2D Art(string name)
        {
            if(!art.TryGetValue(name,out var t))art[name]=t=Resources.Load<Texture2D>("Majsoul/Settlement/"+name)??Resources.Load<Texture2D>("Majsoul/Results/"+name)??Resources.Load<Texture2D>("Majsoul/NativeReview/Sprites/"+name)??Resources.Load<Texture2D>("Majsoul/TablePolish/"+name);
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
            // ww_minus is 69x27; the digits are 69x108. Keep the same pixel scale,
            // rather than stretching the short dash to the full digit height.
            float GlyphHeight(Texture2D t)=>t.name=="ww_minus"?height*t.height/108f:height;
            float width=textures.Sum(t=>GlyphHeight(t)*t.width/t.height)+Mathf.Max(0,textures.Length-1)*1.5f;
            float scale=Mathf.Min(1,Mathf.Min(r.width/Mathf.Max(1,width),r.height/height));height*=scale;width*=scale;
            float x=align==TextAnchor.MiddleRight?r.xMax-width:align==TextAnchor.MiddleLeft?r.x:r.center.x-width/2;
            foreach(var t in textures){float h=GlyphHeight(t),w=h*t.width/t.height;GUI.DrawTexture(new Rect(x,r.center.y-h/2,w,h),t);x+=w+1.5f*scale;}
        }
        static string[] Digits(int value,string prefix) => value.ToString(System.Globalization.CultureInfo.InvariantCulture).Select(c=>c=='-'?"ww_minus":prefix+c).ToArray();
        static string[] LimitGlyphs(string value)
        {
            var names=new List<string>();
            foreach(char c in value??""){
                string glyph=c=='满'?"man":c=='贯'?"guan":c=='跳'?"tiao":c=='倍'?"bei":c=='役'?"yi":c=='三'?"san":c=='累'?"lei":c=='计'?"ji":
                    c=='两'||c=='2'?"liang":c=='3'?"san":c=='4'||c=='四'?"si":c=='5'||c=='五'?"wu":c=='6'||c=='六'?"liu":null;
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
                var preview=table.DoraTexture(i<tiles.Length?tiles[i]:null);if(preview!=null)GUI.DrawTexture(new Rect(x+i*29,y,28,43.5f),preview,ScaleMode.ScaleToFit);
            }
        }
        // UI_Win prefab coordinates are 1920 x 1080, center anchored.
        // Content fits the reference canvas; the background extends to the safe edges.
        static Vector2 NativePoint(float x,float y) => new Vector2(640+x*2/3,360-y*2/3);
        static Rect NativeRect(float x,float y,float w,float h,float px=0,float py=1,float scale=1)
        {
            var p=NativePoint(x,y);w*=scale*2/3;h*=scale*2/3;
            return new Rect(p.x-w*px,p.y-h*(1-py),w,h);
        }
        void WinningHand(WinDetail d,TableRenderer table)
        {
            var tiles=(d.tiles??new string[0]).ToList();var melds=d.melds??new string[0];
            int count=tiles.Count+melds.Sum(m=>m.Skip(1).Count(char.IsDigit))+1;
            // Only two section breaks: concealed hand | all melds | winning tile.
            bool hasMelds=melds.Any(m=>m.Skip(1).Any(char.IsDigit));
            float gaps=.5f+(hasMelds && tiles.Count>0?.5f:0);
            float pitch=Mathf.Min(51.2f,760f/(count+gaps)),x=0,y=-133*.96f*2/3;
            // Fill each pitch with the shared 0.62-aspect gameplay texture, without
            // a narrower drawing box or per-tile padding that creates visible seams.
            float height=pitch*OriginalTiles.HandHeight/OriginalTiles.HandWidth;
            var matrix=GUI.matrix;var anchor=NativePoint(-251.6f,275.9f);
            GUI.matrix*=Matrix4x4.TRS(new Vector3(anchor.x,anchor.y,0),Quaternion.Euler(0,0,-6),Vector3.one);
            foreach(var tile in tiles){Tile(table,tile,new Rect(x,y,pitch,height));x+=pitch;}
            if(hasMelds && tiles.Count>0)x+=pitch*.5f;
            foreach(var meld in melds){int n=0;var digits=meld.Skip(1).Where(char.IsDigit).ToArray();foreach(char c in digits){
                bool back=digits.Length==4 && !meld.Any(ch=>ch=='+'||ch=='-'||ch=='=') && (n==0||n==3);
                Tile(table,back?null:meld[0].ToString()+c,new Rect(x,y,pitch,height));x+=pitch;n++;
            }}
            Tile(table,d.tile,new Rect(x+pitch*.5f,y,pitch,height));GUI.matrix=matrix;
        }
        void NativeDora(RoomView v,TableRenderer table,string[] tiles,float x,float y,string title)
        {
            if(tiles==null || tiles.Length==0)return;
            var matrix=GUI.matrix;var anchor=NativePoint(x,y);
            GUI.matrix*=Matrix4x4.TRS(new Vector3(anchor.x,anchor.y,0),Quaternion.Euler(0,0,-6),Vector3.one);
            Image(title,new Rect(-80.27f,5.13f,100.33f,53.67f),ScaleMode.ScaleToFit);
            for(int i=0;i<5;i++){var preview=table.DoraTexture(i<tiles.Length?tiles[i]:null);if(preview!=null)GUI.DrawTexture(new Rect(i*37.33f,0,37.33f,56),preview,ScaleMode.ScaleToFit);}
            GUI.matrix=matrix;
        }
        static readonly Vector2[] NativeYakuOffsets={new Vector2(119.72f,12.6f),new Vector2(70.72f,-81.4f),new Vector2(103.72f,-170.4f),new Vector2(214.72f,-243.4f),new Vector2(655.72f,82.6f),new Vector2(744.72f,10.6f),new Vector2(772.72f,-78.4f),new Vector2(665.72f,-184.4f)};
        public static Rect NativeBackgroundRect(float virtualWidth,float virtualHeight=720){
            float scale=Mathf.Max(1,Mathf.Max((virtualWidth+4)/1801f,virtualHeight/720));
            return new Rect(640-900.5f*scale,360-134.67f*scale-497.685f*scale,1801f*scale,995.37f*scale);
        }
        void DrawNativeBackground(TableViewport viewport){
            float width=Screen.width/viewport.Scale,height=Screen.height/viewport.Scale,scale=Mathf.Max(width/1280,height/720);
            var color=GUI.color;GUI.color=new Color(color.r,color.g,color.b,color.a*.4f);Image("bg_jiesuan",new Rect(640-640*scale,360-360*scale,1280*scale,720*scale));GUI.color=color;
            Image("bg_jiesuan1",NativeBackgroundRect(width,height));
        }
        void Details(RoomView v,TableRenderer table,TableEffects effects)
        {
            var d=v.result.details[winner];float age=Age,entrance=Mathf.SmoothStep(0,1,age/.45f);
            var matrix=GUI.matrix;GUI.matrix*=Matrix4x4.Translate(new Vector3((1-entrance)*1280,0,0));
            var viewport=TableViewport.Fit(Screen.width,Screen.height,Screen.safeArea,false);
            float extra=(Screen.safeArea.width/viewport.Scale-1280)/2;
            DrawNativeBackground(viewport);
            CharacterFraming.DrawScene(NativeRect(-947,424,924.567f,1392.131f),v.players[d.seat],"B1");
            WinningHand(d,table);
            Image("bg_nickname",NativeRect(-965,-370.9f,396,83,.5f,.5f,1.947f));
            // UI_Win container_name/container_name has a 4.748-degree rotation.
            // Compose locally so scaled canvases retain the same nameplate slope.
            // Center the text in the visible slanted strip, not the sprite's bounds.
            var nameRect=new Rect(32,572.5f,220,33);
            var nameMatrix=GUI.matrix;var namePivot=new Vector3(nameRect.center.x,nameRect.center.y,0);
            GUI.matrix*=Matrix4x4.TRS(namePivot,Quaternion.Euler(0,0,-4.748f),Vector3.one)*Matrix4x4.Translate(-namePivot);
            Label(nameRect,d.name,22,white);GUI.matrix=nameMatrix;
            Image(d.ron?"pg_he":"pg_zimo",NativeRect(834.3f,419.6f,104,108),ScaleMode.ScaleToFit);
            NativeDora(v,table,v.indicators,27.5f,250,"bg_dora");
            NativeDora(v,table,d.ura,536.5f,303,"bg_lidora");
            var sequence=SettlementAudio.Sequence(d);var yaku=sequence.rows.Select(row=>row.yaku).ToArray();int visible=sequence.rows.Count(row=>age>=row.at);
            for(int i=0;i<visible;i++){
                float x,y,h=35.2f;
                if(yaku.Length<=8){var offset=NativeYakuOffsets[i]-NativeYakuOffsets[0];x=611+offset.x*2/3;y=303.7f-offset.y*2/3;}
                else {int rows=(yaku.Length+1)/2;x=583+i/rows*357;y=276+i%rows*Mathf.Min(35,238f/rows);h=Mathf.Min(35,238f/rows);}
                bool yakuman=yaku[i].yakuman>0;
                Image(yakuman?"bg_yakuman":"bg_fan",yakuman?new Rect(x-32.4f,y-35.3f,278.4f,h+60):new Rect(x+2,y-38.5f,220,h+37.6f));
                if(yakuman){x-=33;y+=4.4f;}
                var rowMatrix=GUI.matrix;
                GUI.matrix*=Matrix4x4.TRS(new Vector3(x,y,0),Quaternion.Euler(0,0,-6),Vector3.one);
                Label(new Rect(0,0,yakuman?243.2f:176,h),yaku[i].name,h<30?18:23,white);
                GUI.matrix=rowMatrix;
                if(yaku[i].yakuman==0)Glyphs(new Rect(x+179,y-28,42,h+5),Digits(yaku[i].han,"h_").Concat(new[]{"word_fan"}).ToArray(),26);
            }
            if(age>=sequence.fuAt){
                var circle=NativeRect(365.5f,-48,185,179,.12972973f,.16759777f);
                Image("bg_center_fan",circle);var score=d.score??new ScoreDetail();
                if(score.damanguan>0)Glyphs(new Rect(circle.x+12,circle.y+16,circle.width-24,circle.height-32),new[]{"word_yiman"},60);
                else {
                    Glyphs(new Rect(circle.x+15,circle.y+5,65,61),Digits(score.fanshu,"h_"),55);
                    Image("word_fan",new Rect(circle.x+64,circle.y+52,38,45),ScaleMode.ScaleToFit);
                    Glyphs(new Rect(circle.x+12,circle.y+80,68,26),Digits(score.fu,"ww_").Concat(new[]{"word_fu"}).ToArray(),24);
                }
                if(age>=sequence.pointsAt){
                var scoreRect=NativeRect(555,-315,589,110,.49235994f,.5f,1.2f);
                var scoreMatrix=GUI.matrix;var scoreAnchor=NativePoint(555,-315);
                GUI.matrix*=Matrix4x4.TRS(new Vector3(scoreAnchor.x,scoreAnchor.y,0),Quaternion.Euler(0,0,-6),Vector3.one)*Matrix4x4.Translate(new Vector3(-scoreAnchor.x,-scoreAnchor.y,0));
                Image("bg_point",new Rect(scoreRect.x+40,scoreRect.y+60,scoreRect.width-40,60));
                Glyphs(scoreRect,Digits(score.defen,"ww_").Concat(new[]{"word_point"}).ToArray(),88,TextAnchor.MiddleRight);
                GUI.matrix=scoreMatrix;
                }
                if(age>=sequence.titleAt && !string.IsNullOrEmpty(d.limit)){
                    var pointBox=NativeRect(555,-315,589,110,.49235994f,.5f,1.2f);
                    var pointTextures=Digits(score.defen,"ww_").Concat(new[]{"word_point"}).Select(Art).Where(t=>t!=null).ToArray();
                    float pointWidth=pointTextures.Sum(t=>88f*t.width/t.height)+Mathf.Max(0,pointTextures.Length-1)*1.5f;
                    pointWidth*=Mathf.Min(1,Mathf.Min(pointBox.width/pointWidth,pointBox.height/88));
                    float right=Mathf.Min(905,pointBox.xMax-pointWidth-42);
                    var names=LimitGlyphs(d.limit);var titleTextures=names.Select(Art).Where(t=>t!=null).ToArray();
                    float titleWidth=titleTextures.Sum(t=>124f*t.width/t.height)+Mathf.Max(0,titleTextures.Length-1)*1.5f;
                    var limit=new Rect(right-titleWidth,558,titleWidth+1,135);
                    if(d.limit.Contains("役满") && age>=sequence.titleAt+.05f)limitFire.Draw(new Rect(limit.center.x-300,limit.center.y-145,600,260),d.limit=="役满"?"Majsoul/UiEffects/effect_yiman":"Majsoul/UiEffects/effect_yiman2",age-sequence.titleAt-.05f,25);
                    Glyphs(limit,names,124,TextAnchor.MiddleRight);
                }
            }
            GUI.matrix=matrix;
            if(v.result.details.Length>1)Label(new Rect(835,656,180,32),"和牌 "+(winner+1)+" / "+v.result.details.Length,17,white);
        }
        public static Vector2 TableCenter(TableRenderer table)=>TableViewport.Fit(Screen.width,Screen.height,Screen.safeArea,false).FromScreen(table.Camera.WorldToScreenPoint(Vector3.zero),Screen.height);
        public Rect DrawTitleRect {get;private set;}
        void DrawRound(RoomView v,TableRenderer table)
        {
            float age=Age;var old=GUI.color;GUI.color=new Color(1,1,1,Mathf.Clamp01(age/.5f));
            var center=TableCenter(table);
            string title=v.result.title=="九种九牌"?"jiuzhongjiupai":v.result.title=="荒牌流局"?"huangpailiuju":null;
            if(title!=null){DrawTitleRect=TableIndicators.CenteredImageRect(title,center,title=="huangpailiuju"?360:308);TableIndicators.Image(title,DrawTitleRect);}
            else Label(new Rect(center.x-205,center.y-55,410,110),v.result.title,42,gold);
            GUI.color=old;
            if(age<.7f || v.result.abort)return;
            foreach(var d in v.result.drawDetails){
                int relative=(d.seat-v.you+v.n)%v.n;
                Vector2 c=center+(relative==0?new Vector2(0,160):relative==v.n-1?new Vector2(-322,60):relative==1&&v.n==4?new Vector2(322,60):new Vector2(0,-114));
                if(!d.tenpai){TableIndicators.Image("noting",new Rect(c.x-93,c.y-22,186,44));continue;}
                var waits=(d.waits??new string[0]).Select(t=>new WaitDetail{tile=t,ron=true}).ToArray();
                if(waits.Length==0){TableIndicators.Image("show_tingpai",new Rect(c.x-88,c.y-22,176,44));continue;}
                var r=TableIndicators.WaitRect(waits.Length,c,c.y+34,380,52,false);
                TableIndicators.Image("show_tingpai",new Rect(c.x-96,r.y-40,192,42));
                TableIndicators.Waits(table,waits,r,52,false,false);
            }
        }
        public static int Before(RoomView v,int seat) => v.players[seat].score-(v.result?.delta!=null&&seat<v.result.delta.Length?v.result.delta[seat]:0);
        public static int RiichiAward(RoomView v,int seat)=>Mathf.Max(0,(v.result?.details??new WinDetail[0]).Where(d=>d.seat==seat && d.score?.delta?.Length==v.n).Sum(d=>d.score.delta.Sum()));
        public static string ScoreBreakdown(RoomView v,int seat){int award=RiichiAward(v,seat);if(award==0)return null;int delta=v.result.delta[seat];return "和牌 "+(delta-award>=0?"+":"")+(delta-award)+"  供托 +"+award;}
        public static int Rank(RoomView v,int seat,bool before) => 1+Enumerable.Range(0,v.n).Count(i=>{
            int a=before?Before(v,i):v.players[i].score,b=before?Before(v,seat):v.players[seat].score;
            return a>b || (a==b && i<seat);
        });
        public const float ScoreCountStart=.8f,ScoreCountEnd=2.45f;
        public int DisplayScore(RoomView v,int seat) => Mathf.RoundToInt(Mathf.Lerp(Before(v,seat),v.players[seat].score,Mathf.SmoothStep(0,1,(Age-ScoreCountStart)/(ScoreCountEnd-ScoreCountStart)))/100)*100;
        public static float RankAt(int players,int rank)=>2.7f+(players-rank)*.36f;
        public static float ScoreEnd(int players)=>RankAt(players,1)+.6f;
        public bool RankVisible(RoomView v,int seat)=>Age<2.45f || Age>=RankAt(v.n,Rank(v,seat,false));
        static Vector2 Center(int relative,int n) => relative==0?new Vector2(640,570):relative==n-1?new Vector2(400,364):relative==1&&n==4?new Vector2(900,364):n==3?new Vector2(900,364):new Vector2(640,164);
        void Arrow(int from,int to,int n,float alpha)
        {
            var matrix=GUI.matrix;var old=GUI.color;GUI.color=new Color(1,1,1,alpha);
            var source=NativeReviewLayout.Arrow(NativeReviewLayout.ScoreSlot(from,n),NativeReviewLayout.ScoreSlot(to,n));var center=NativeReviewLayout.ArrowCenter(source);var q=new Quaternion(source.rotation[0],source.rotation[1],source.rotation[2],source.rotation[3]);
            GUI.matrix=matrix*Matrix4x4.TRS(new Vector3(center.x,center.y,0),Quaternion.Euler(0,0,-q.eulerAngles.z),new Vector3(source.scale[0],source.scale[1],1));
            Image(source.size[0]>100?"scorechangearrow_1":"scorechangearrow_0",new Rect(-source.size[0]/3,-source.size[1]/3,source.size[0]*2/3,source.size[1]*2/3));
            GUI.matrix=matrix;GUI.color=old;
        }
        public static Vector2[] TransferEnds(Vector2 a,Vector2 b)
        {
            Vector2 dir=(b-a).normalized;
            float margin=Mathf.Min(128/Mathf.Max(.001f,Mathf.Abs(dir.x)),55/Mathf.Max(.001f,Mathf.Abs(dir.y)))+10;
            return new[]{a+dir*margin,b-dir*margin};
        }
        void Scores(RoomView v,TableRenderer table)
        {
            // Original score-change title uses a brush font, not the action/result title.
            roundText.normal.textColor=gold;GUI.Label(new Rect(425,30,430,44),"东南西北"[Mathf.Clamp(v.round/v.n,0,3)]+(v.round%v.n+1).ToString()+"局  "+v.honba+"本场",roundText);
            if(Age>.3f){
                if(v.result.draw)foreach(int from in Enumerable.Range(0,v.n).Where(i=>v.result.delta[i]<0))foreach(int to in Enumerable.Range(0,v.n).Where(i=>v.result.delta[i]>0)){
                    Arrow((from-v.you+v.n)%v.n,(to-v.you+v.n)%v.n,v.n,Mathf.Clamp01((Age-.3f)/.3f));
                }
                var details=v.result.details??new WinDetail[0];
                foreach(var d in details)foreach(int seat in Enumerable.Range(0,v.n).Where(i=>d.score?.delta?.Length==v.n?d.score.delta[i]<0:d.ron?i==d.from:v.result.delta[i]<0)){
                    if(seat!=d.seat)Arrow((seat-v.you+v.n)%v.n,(d.seat-v.you+v.n)%v.n,v.n,Mathf.Clamp01((Age-.3f)/.3f));
                }
            }
            for(int seat=0;seat<v.n;seat++){
                int relative=(seat-v.you+v.n)%v.n;Rect r=NativeReviewLayout.ScoreRect(relative,v.n);Vector2 c=r.center;
                var cardMatrix=GUI.matrix;
                Image("scorechange_bg",r);
                var portrait=table.Portrait(v.players[seat]);
                Rect head=new Rect(r.x+219,r.y-23,85,85);
                Image("bg_head1",head);
                if(portrait!=null)GUI.DrawTexture(new Rect(head.x+4,head.y+4,head.width-8,head.height-8),portrait,ScaleMode.ScaleToFit);
                Label(new Rect(r.x+23,r.y+4,190,30),v.players[seat].name,19,white,TextAnchor.MiddleCenter);
                Glyphs(new Rect(r.x+160,r.y+75,143,45),Digits(DisplayScore(v,seat),"ww_"),36,TextAnchor.MiddleRight);
                int delta=v.result.delta!=null&&seat<v.result.delta.Length?v.result.delta[seat]:0;
                // This native control is CText with haolong_0, not a sprite digit run.
                if(Age<2.5f){
                    number.normal.textColor=delta>=0?new Color(.38f,.86f,.22f):new Color(1,.20f,.17f);
                    GUI.Label(new Rect(r.x+108,r.y+39,130,36),(delta>=0?"+":"")+delta,number);
                }
                string breakdown=ScoreBreakdown(v,seat);
                if(breakdown!=null)Label(new Rect(r.x+12,r.yMax+3,r.width-24,23),breakdown,17,white);
                int prior=Rank(v,seat,true),rank=Rank(v,seat,false),shown=Age<2.45f?prior:rank;
                if(!RankVisible(v,seat)){GUI.matrix=cardMatrix;continue;}
                var rankMatrix=GUI.matrix;float rankScale=Age>=2.45f?NativeReviewLayout.RankScale(Age-RankAt(v.n,rank)):1;
                Vector3 rankPivot=new Vector3(r.x+72,r.y+86,0);GUI.matrix*=Matrix4x4.TRS(rankPivot,Quaternion.identity,Vector3.one*rankScale)*Matrix4x4.Translate(-rankPivot);
                Image("bg_point",new Rect(r.x+29,r.y+88,111,37));
                Image("s_"+shown,new Rect(r.x+34,r.y+40,42,79),ScaleMode.ScaleToFit);
                Image("s_"+shown+"_"+shown,new Rect(r.x+72,r.y+83,37,37),ScaleMode.ScaleToFit);
                if(Age>=2.5f && prior!=rank)Image(prior>rank?"upgrade_arrow":"rank_down_arrow",new Rect(r.x+112,r.y+89,31,24),ScaleMode.ScaleToFit);
                GUI.matrix=rankMatrix;
                GUI.matrix=cardMatrix;
            }

        }
        static Rect FinalRect(FinalCard card,string name){var r=card.items.First(i=>i.name==name).rect;return new Rect(r[0],r[1],r[2],r[3]);}
        void FinalValue(FinalCard card,string name,string value,Color color){
            var item=card.items.First(i=>i.name==name);var r=FinalRect(card,name);
            var style=new GUIStyle(number){fontSize=Mathf.RoundToInt(item.fontSize),alignment=TextAnchor.UpperLeft,normal={textColor=color},padding=new RectOffset()};
            GUI.Label(r,value,style);
        }
        void Final(RoomView v,TableRenderer table,TableEffects effects){
            var heading=new GUIStyle(roundText){fontSize=36,normal={textColor=white},padding=new RectOffset()};
            GUI.Label(new Rect(470.7f,23.333f,313.274f,69.333f),"终局",heading);
            CharacterFraming.DrawScene(new Rect(-15,80,600,755),v.players[v.you],"B2");
            if(finalLayout==null)finalLayout=JsonUtility.FromJson<FinalLayout>(Resources.Load<TextAsset>("Majsoul/Settlement/Final/gameend-layout").text);
            var seats=Enumerable.Range(0,v.n).OrderBy(i=>Rank(v,i,false)).ToArray();
            for(int rank=0;rank<seats.Length;rank++){
                int seat=seats[rank];float at=.4f+rank*.22f;float entrance=Mathf.Clamp01((Age-at)/.22f);if(entrance<=0)continue;
                // Keep every GameEnd child in the APK card's own coordinate space.
                // The independent rank digit, 位, rewards and self tag are not
                // inferred from the slanted frame's overall bounding box.
                var card=finalLayout.cards[rank];var matrix=GUI.matrix;
                GUI.matrix*=Matrix4x4.TRS(new Vector3(card.rect[0]+(1-entrance)*650,card.rect[1],0),Quaternion.identity,Vector3.one*card.scale);
                Image(rank==0?"bg_end0":"bg_end123",new Rect(0,0,card.rect[2],card.rect[3]));
                Image("bg_end",FinalRect(card,"rank"));
                Image("Final/s_"+(rank+1),FinalRect(card,"rank/node"));
                Image("Final/s_"+(rank+1)+"_"+(rank+1),FinalRect(card,"rank/ASPECIALTEXTNODE!@#1"));
                var head=FinalRect(card,"head/node");var face=table.Portrait(v.players[seat]);
                Image("bg_head1",FinalRect(card,"head/bg"));if(face!=null)GUI.DrawTexture(head,face,ScaleMode.ScaleToFit);
                var name=FinalRect(card,"container_name/label_name");var self=FinalRect(card,"myself");
                name.width=(seat==v.you?self.x:FinalRect(card,"img_g0").x)-name.x-12;
                var nameItem=card.items.First(i=>i.name=="container_name/label_name");int nameSize=Mathf.RoundToInt(nameItem.fontSize);
                var nameStyle=new GUIStyle(text){fontSize=nameSize,alignment=TextAnchor.MiddleLeft};
                while(nameSize>24 && nameStyle.CalcSize(new GUIContent(v.players[seat].name)).x>name.width)nameStyle.fontSize=--nameSize;
                Label(name,v.players[seat].name,nameSize,white,TextAnchor.MiddleLeft);
                var points=FinalRect(card,"container_score");Glyphs(points,Digits(v.players[seat].score,"ww_"),points.height,TextAnchor.MiddleLeft);
                var rewardColor=new Color(.28f,.76f,.45f);
                FinalValue(card,"val","+0.0",rewardColor);
                Image("gold0",FinalRect(card,"img_g0"));Image("PT",FinalRect(card,"img_g1"));
                FinalValue(card,"img_g0/val","+0",rewardColor);FinalValue(card,"img_g1/val","+0",rewardColor);
                if(seat==v.you)Image("myself",self);
                GUI.matrix=matrix;
            }
        }
        public static Rect FinalHomeRect=>new Rect(886.2606f,630.8333f,175.4927f,58.3333f);
        public static Rect FinalNextRect=>new Rect(1075.194f,630.8333f,175.4927f,58.3333f);
        bool FinalButton(Rect r,string value,bool primary,Func<Rect,string,bool> button){
            NativeUiChrome.Button(r,primary);
            var style=new GUIStyle(roundText){fontSize=28,normal={textColor=primary?new Color(.42745f,.18039f,.22745f):gold},padding=new RectOffset()};
            // UI_GameEnd btn_next/node: 240 x 62 at (-.4, 33.5),
            // independently anchored inside its 263.239 x 87.5 button.
            GUI.Label(new Rect(r.center.x-80.2667f,r.y+6.8333f,160,41.3333f),value,style);
            return button(r,value);
        }
        bool NativeButton(Rect r,string value,Func<Rect,string,bool> button)
        {
            Image("Btn_common",r);
            // UI_Win / UI_ScoreChange use Btn_common + CText in this font.
            // The unrelated "confirm" sprite carries a second frame of its own.
            var style=new GUIStyle(roundText){fontSize=31,normal={textColor=gold}};
            GUI.Label(r,value=="确认结算"?"确认":value,style);
            return button(r,value);
        }
        public Rect WaitingRect=>new Rect(765,648,342,50);
        void Waiting(){Label(WaitingRect,"已确认，等待其他玩家",22,white,TextAnchor.MiddleRight);}
        public Action Draw(RoomView v,TableRenderer table,TableEffects effects,Font gameFont,Func<Rect,string,bool> button)
        {
            Observe(v);if(text==null){font=gameFont;text=new GUIStyle(GUI.skin.label){font=font,wordWrap=false,clipping=TextClipping.Clip};number=new GUIStyle(text){font=Resources.Load<Font>("Majsoul/Settlement/score-font"),fontSize=31,alignment=TextAnchor.MiddleLeft};
                roundText=new GUIStyle(text){font=Resources.Load<Font>("Majsoul/Settlement/round-font-chs"),fontSize=31,alignment=TextAnchor.MiddleCenter};}
            if(finalPage){
                Final(v,table,effects);
                int remaining=Mathf.Max(0,9-Mathf.FloorToInt(Age));
                if(!v.players[v.you].ready && FinalButton(FinalNextRect,"下一局("+remaining+")",true,button))return Action.Ready;
                if(v.players[v.you].ready){
                    bool restart=v.host&&v.players.All(p=>p.ready);
                    if(FinalButton(FinalNextRect,restart?"再来一场":"等待牌友",true,restart?button:(r,caption)=>false)&&restart)return Action.Next;
                }
                if(FinalButton(FinalHomeRect,"返回大厅",false,button))return Action.Home;
            }
            else if(drawStage){DrawRound(v,table);if(NativeButton(new Rect(1125,648,128,50),Age<.7f?"跳过":"确认",button))Advance(v);}
            else if(!scores){Details(v,table,effects);if(NativeButton(new Rect(1125,648,128,50),Age<RevealEnd(v.result.details[winner])?"跳过":"确认("+YakuConfirmRemaining(v)+")",button))Advance(v);}
            else {
                Scores(v,table);
                if(!v.players[v.you].ready && NativeButton(new Rect(1125,648,128,50),Age<ScoreEnd(v.n)?"跳过":"确认("+ScoreConfirmRemaining(v)+")",button)){bool finished=Age>=ScoreEnd(v.n);Advance(v);if(finished&&!v.result.matchOver)return Action.Ready;}
                if(v.players[v.you].ready)Waiting();
                if(v.host && v.players.All(p=>p.ready) && NativeButton(new Rect(1125,648,128,50),v.result.matchOver?"再来一场":"下一局",button))return Action.Next;
                if(NativeButton(new Rect(28,650,142,49),"返回大厅",button))return Action.Home;
            }
            return Action.None;
        }
    }
}
