using System;
using System.Collections.Generic;
using UnityEngine;
namespace YunJian.Riichi
{
    public sealed class MatchIntroduction
    {
        [Serializable] class Config {public Cell[] cells;}
        [Serializable] class ImageInfo {public string name;public float[] rect;}
        [Serializable] class Cell {public int seat;public float[] start,end,body,name;public float duration;public ImageInfo[] images;public string portraitResource;}
        public const float Duration=3.5f;
        readonly HashSet<string> seen=new HashSet<string>();readonly NativeUiEffect effect=new NativeUiEffect();
        readonly Dictionary<string,Texture2D> art=new Dictionary<string,Texture2D>();
        string observedRoom;int observedMatch;bool observedLobby;
        RoomView view;Config config;GUIStyle nameStyle,titleStyle;string key;float began;bool running,sound;
        public int Started {get;private set;}
        public bool Freeze;public float FrozenAge;
        public float Age=>Freeze?FrozenAge:Time.unscaledTime-began;
        public bool Active=>running && Age<Duration;
        public float Remaining=>Active?Duration-Mathf.Max(0,Age):0;
        public int VisiblePlayers=>view?.players?.Length??0;
        public void Observe(RoomView next,bool validation=false)
        {
            if(next==null || next.phase=="empty" || next.phase=="lobby" || next.phase=="join"){Reset();observedRoom=next?.audioRoom;observedMatch=next?.matchSeq??0;observedLobby=next?.phase=="lobby";return;}
            bool fresh=observedRoom==next.audioRoom && next.matchSeq>observedMatch && (observedLobby || observedMatch>0);
            observedRoom=next.audioRoom;observedMatch=next.matchSeq;observedLobby=false;
            if(next.matchSeq<=0 || next.matchStartedAt<=0){running=false;return;}
            string id=next.audioRoom+":"+next.matchSeq;if(id==key){view=next;return;}
            key=id;view=next;running=false;effect.Dispose();
            string pref="riichi.intro:"+id;long elapsed=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()-next.matchStartedAt;
            if(seen.Contains(id) || !validation && PlayerPrefs.GetInt(pref,0)==1)return;
            seen.Add(id);
            if(next.phase!="play" || !fresh && (elapsed<0 || elapsed>6000) || next.players==null || Array.Exists(next.players,p=>p.river.Length>0))return;
            if(!validation){PlayerPrefs.SetInt(pref,1);PlayerPrefs.Save();}
            began=Time.unscaledTime;running=true;sound=false;Started++;
        }
        public void Tick(GameAudio audio){if(!Active){if(running){running=false;effect.Dispose();}return;}if(!sound && Age>=1.2f && audio!=null)sound=audio.Cue("winlose");}
        public void Reset(){running=false;key=null;observedRoom=null;observedMatch=0;observedLobby=false;Freeze=false;effect.Dispose();}
        Texture2D Art(string name){if(!art.TryGetValue(name,out var t))art[name]=t=Resources.Load<Texture2D>("Majsoul/MatchIntro/"+name)??Resources.Load<Texture2D>("Majsoul/Presentation0535/"+name)??Resources.Load<Texture2D>("Majsoul/Review1012c/"+name)??Resources.Load<Texture2D>("Majsoul/TableLook/Sprites/"+name);return t;}
        void BadgeImage(string name,Vector2 center,Vector2 offset,Vector2 size,float unit,float px=.5f,float py=.5f){
            var t=Art(name);var at=center+new Vector2(offset.x,-offset.y)*unit;
            if(t!=null)GUI.DrawTexture(new Rect(at.x-size.x*unit*px,at.y-size.y*unit*(1-py),size.x*unit,size.y*unit),t);
        }
        void NamePanel(Rect r,string name){
            GUI.DrawTexture(r,Art("namebg"));const float u=2f/3;
            // UI_FightBegin/container_name: native label 45 px; rank scale .55.
            var rank=r.center+new Vector2(-283,-9)*u;
            BadgeImage("rank_bg",rank,new Vector2(-127.5f,126.5f),new Vector2(245,228),.55f*u,0,1);
            BadgeImage("sima_queshi",rank,new Vector2(3.9f,10.6f),new Vector2(160,160),.55f*u);
            foreach(var p in new[]{new Vector2(-36.5f,-75.5f),new Vector2(-71.5f,-54.5f),new Vector2(-102.5f,-7.5f)})BadgeImage("starbg",rank,p,new Vector2(67,64),.55f*u);
            BadgeImage("star",rank,new Vector2(-36.5f,-75.5f),new Vector2(67,64),.55f*u);
            // Keep a small gap between the title and the player name.
            BadgeImage("notitle",r.center,new Vector2(-236,64),new Vector2(263,50),u,0,.5f);
            GUI.Label(new Rect(r.center.x-209.9f*u,r.center.y-26*u,r.width*.65f,45*u),name,nameStyle);
        }
        static Rect RectAt(float[] d,Vector2 offset)=>new Rect(offset.x+d[0]*2/3,offset.y+d[1]*2/3,d[2]*2/3,d[3]*2/3);
        public void Draw()
        {
            if(!Active)return;if(config==null)config=JsonUtility.FromJson<Config>(Resources.Load<TextAsset>("Majsoul/MatchIntro/layout").text);
            var matrix=GUI.matrix;GUI.matrix=Matrix4x4.identity;var color=GUI.color;GUI.color=new Color(0,0,0,Age<3?1:1-Mathf.Clamp01((Age-3.1f)/.3f));GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=color;
            var viewport=TableViewport.Fit(Screen.width,Screen.height,Screen.safeArea,false);GUI.matrix=viewport.Matrix;
            if(nameStyle==null){var font=Resources.Load<Font>("Majsoul/Font/game");nameStyle=new GUIStyle(GUI.skin.label){font=font,fontSize=30,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(),clipping=TextClipping.Overflow};nameStyle.normal.textColor=Color.white;}
            foreach(var c in config.cells){int seat=view.n==4?(view.you+c.seat)%4:c.seat==0?view.you:c.seat==1?(view.you+1)%3:c.seat==3?(view.you+2)%3:-1;if(seat<0)continue;
                float enter=Mathf.Pow(Mathf.Clamp01((Age-.5f)/.4f),5),exit=Mathf.Pow(Mathf.Clamp01((Age-3)/.4f),5);var from=new Vector2(c.start[0],c.start[1]);var to=new Vector2(c.end[0],c.end[1]);var offset=Vector2.LerpUnclamped(Vector2.LerpUnclamped(from,to,enter),from,exit)*2/3;
                var bg=c.images[0];GUI.DrawTexture(RectAt(bg.rect,offset),Art(c.portraitResource),ScaleMode.StretchToFill,true);
                var r=RectAt(c.name,offset);NamePanel(r,view.players[seat].name);
                if(c.seat==0 && c.images.Length>1)GUI.DrawTexture(RectAt(c.images[1].rect,offset),Art("myself"),ScaleMode.ScaleToFit,true);
            }
            if(Age>=1.2f && Age<3){
                float age=Age-1.2f;effect.Draw(new Rect(0,0,1280,720),"Majsoul/Presentation0535/victory-particles",age,1);
                float size=VictorySize(age);var old=GUI.color;GUI.color=new Color(1,1,1,Mathf.Clamp01(age/.035f));GUI.DrawTexture(new Rect(632-size/2,358-size*189/205/2,size,size*189/205),Art("winlose"),ScaleMode.ScaleToFit);GUI.color=old;
            }
            GUI.matrix=matrix;GUI.color=color;
        }
        public static float VictorySize(float age)=>200*Mathf.Lerp(1.8f,1,Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.22f)));
    }
}
