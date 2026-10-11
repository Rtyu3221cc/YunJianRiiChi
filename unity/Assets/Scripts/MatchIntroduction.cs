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
        public void Tick(GameAudio audio){if(!Active){if(running){running=false;effect.Dispose();}return;}if(!sound && Age>=.7f){sound=true;audio?.Cue("winlose");}}
        public void Reset(){running=false;key=null;observedRoom=null;observedMatch=0;observedLobby=false;Freeze=false;effect.Dispose();}
        Texture2D Art(string name){if(!art.TryGetValue(name,out var t))art[name]=t=Resources.Load<Texture2D>("Majsoul/MatchIntro/"+name);return t;}
        static Rect RectAt(float[] d,Vector2 offset)=>new Rect(offset.x+d[0]*2/3,offset.y+d[1]*2/3,d[2]*2/3,d[3]*2/3);
        public void Draw()
        {
            if(!Active)return;if(config==null)config=JsonUtility.FromJson<Config>(Resources.Load<TextAsset>("Majsoul/MatchIntro/layout").text);
            var matrix=GUI.matrix;GUI.matrix=Matrix4x4.identity;var color=GUI.color;GUI.color=new Color(0,0,0,Age<3?1:1-Mathf.Clamp01((Age-3.1f)/.3f));GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=color;
            var viewport=TableViewport.Fit(Screen.width,Screen.height,Screen.safeArea,false);GUI.matrix=viewport.Matrix;
            if(nameStyle==null){var font=Resources.Load<Font>("Majsoul/Font/game");nameStyle=new GUIStyle(GUI.skin.label){font=font,fontSize=23,alignment=TextAnchor.MiddleLeft,padding=new RectOffset()};nameStyle.normal.textColor=Color.white;titleStyle=new GUIStyle(nameStyle){fontSize=16};titleStyle.normal.textColor=new Color(.77f,.80f,.88f);}
            foreach(var c in config.cells){int seat=view.n==4?(view.you+c.seat)%4:c.seat==0?view.you:c.seat==1?(view.you+1)%3:c.seat==3?(view.you+2)%3:-1;if(seat<0)continue;
                float enter=Mathf.Pow(Mathf.Clamp01((Age-.5f)/.4f),5),exit=Mathf.Pow(Mathf.Clamp01((Age-3)/.4f),5);var from=new Vector2(c.start[0],c.start[1]);var to=new Vector2(c.end[0],c.end[1]);var offset=Vector2.LerpUnclamped(Vector2.LerpUnclamped(from,to,enter),from,exit)*2/3;
                var bg=c.images[0];GUI.DrawTexture(RectAt(bg.rect,offset),Art(c.portraitResource),ScaleMode.StretchToFill,true);
                var r=RectAt(c.name,offset);GUI.DrawTexture(r,Art("namebg"),ScaleMode.StretchToFill,true);GUI.Label(new Rect(r.x+26,r.y+22,r.width-30,r.height-22),view.players[seat].name,nameStyle);GUI.Label(new Rect(r.x+28,r.y-2,r.width-32,27),"暂无称号",titleStyle);
                if(c.seat==0 && c.images.Length>1)GUI.DrawTexture(RectAt(c.images[1].rect,offset),Art("myself"),ScaleMode.ScaleToFit,true);
            }
            if(Age>=1.2f && Age<3)effect.Draw(new Rect(440,180,400,360),"Majsoul/UiEffects/effect_winlose",Age-1.2f,1);
            GUI.matrix=matrix;GUI.color=color;
        }
    }
}
