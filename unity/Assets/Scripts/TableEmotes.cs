using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi {
    // Cosmetic events have their own sequence and never invalidate a tile action.
    public sealed class TableEmotes:IDisposable {
        sealed class Bubble {public PublicEmote message;public float began;}
        readonly Dictionary<int,Bubble> bubbles=new Dictionary<int,Bubble>();
        readonly Dictionary<Texture2D,RenderTexture> gray=new Dictionary<Texture2D,RenderTexture>();
        static readonly Dictionary<string,Texture2D> art=new Dictionary<string,Texture2D>();
        readonly NativeScroll scroll=new NativeScroll();
        Material grayscale;RoomView observed;string room,character;int lastSeq;float cooldownUntil;bool open,suppressRelease;
        public const float Duration=5;
        public bool IsOpen=>open;
        public bool BlocksTableInput=>open||suppressRelease;
        public float Cooldown=>Mathf.Max(0,cooldownUntil-Time.unscaledTime);
#if UNITY_EDITOR
        public string ReviewPress;public readonly Dictionary<string,Rect> ReviewTargets=new Dictionary<string,Rect>();
        public readonly Dictionary<int,string> ReviewBubbleCharacters=new Dictionary<int,string>();
        public int ReviewSeenSequence=>lastSeq;
        public int ReviewGrayTextures=>gray.Count;
        public bool ReviewGuiEnabled;
#endif
        static Texture2D Art(string name){if(!art.TryGetValue(name,out var t))art[name]=t=Resources.Load<Texture2D>("Majsoul/TableEmotes/"+name);return t;}
        public void Observe(RoomView view){
            if(ReferenceEquals(view,observed))return;observed=view;
            if(view==null||room!=view.audioRoom){bubbles.Clear();ClearGray();lastSeq=0;room=view?.audioRoom;cooldownUntil=0;Close();}
            if(view==null)return;cooldownUntil=Time.unscaledTime+Mathf.Clamp(view.emoteCooldownMs,0,5000)/1000f;
            if(view.phase!="play"&&view.phase!="reaction"){Close();bubbles.Clear();return;}
            foreach(var message in (view.emotes??new PublicEmote[0]).OrderBy(e=>e.id)){
                if(message.id<=lastSeq)continue;lastSeq=message.id;
                if(message.seat<0||message.seat>=view.players.Length||message.ageMs>=5000||view.players[message.seat].character!=message.character)continue;
                var role=CharacterRoster.Find(message.character);string path="Emoji/"+message.character+"/"+message.emoji;
                if(role?.emojis.Contains(path)!=true)continue;
                bubbles[message.seat]=new Bubble{message=message,began=Time.unscaledTime-Mathf.Max(0,message.ageMs)/1000f};
            }
        }
        public void Close(){open=false;scroll.Offset=0;suppressRelease=false;}
        public void TickPointer(){if(!Input.GetMouseButton(0)&&!Input.GetMouseButtonUp(0))suppressRelease=false;}
        public bool Back(){if(!open)return false;Close();return true;}
        public void Sending(){cooldownUntil=Time.unscaledTime+Duration;Close();suppressRelease=true;}
        public static Vector2 PortraitAnchor(int relative,int n,float w,float h)=>relative==0?new Vector2(w*.26f,h*.83f-104):relative==n-1?new Vector2(64,h*.265f):relative==1&&n==4?new Vector2(w-132,h*.52f):new Vector2(w*.676f,26);
        public static Rect BubbleRect(int relative,int n,float w,float h){var a=PortraitAnchor(relative,n,w,h);const float bw=108.5f,bh=93;bool right=relative!=0&&relative!=n-1;return new Rect(right?a.x-bw-7:a.x+69,relative==0?a.y-bh+19:a.y+5,bw,bh);}
        static void Slice(Rect r,string name,Vector4 border,float factor=1,bool flipX=false,bool flipY=false){
            var t=Art(name);if(t==null||Event.current.type!=EventType.Repaint)return;var saved=GUI.matrix;if(flipX||flipY)GUIUtility.ScaleAroundPivot(new Vector2(flipX?-1:1,flipY?-1:1),r.center);
            float l=Mathf.Min(border.x*factor,r.width/2),b=Mathf.Min(border.y*factor,r.height/2),rr=Mathf.Min(border.z*factor,r.width/2),top=Mathf.Min(border.w*factor,r.height/2);
            float[] xs={r.x,r.x+l,r.xMax-rr,r.xMax},ys={r.y,r.y+top,r.yMax-b,r.yMax},us={0,border.x/t.width,1-border.z/t.width,1},vs={0,border.w/t.height,1-border.y/t.height,1};
            var m=GUI.matrix;for(int i=0;i<4;i++){if(Mathf.Abs(m.m00)>.0001f)xs[i]=(Mathf.Round(xs[i]*m.m00+m.m03)-m.m03)/m.m00;if(Mathf.Abs(m.m11)>.0001f)ys[i]=(Mathf.Round(ys[i]*m.m11+m.m13)-m.m13)/m.m11;}
            for(int y=0;y<3;y++)for(int x=0;x<3;x++)GUI.DrawTextureWithTexCoords(Rect.MinMaxRect(xs[x],ys[y],xs[x+1],ys[y+1]),t,new Rect(us[x],1-vs[y+1],us[x+1]-us[x],vs[y+1]-vs[y]));GUI.matrix=saved;
        }
        bool Hit(Rect r,string id){
#if UNITY_EDITOR
            var m=GUI.matrix;var a=m.MultiplyPoint3x4(r.position);var b=m.MultiplyPoint3x4(new Vector3(r.xMax,r.yMax));ReviewTargets[id]=Rect.MinMaxRect(a.x,a.y,b.x,b.y);if(ReviewPress==id&&GUI.enabled){ReviewPress=null;return true;}
#endif
            return GUI.Button(r,GUIContent.none,GUIStyle.none);
        }
        Texture Gray(Texture2D source){
            if(gray.TryGetValue(source,out var result)&&result!=null)return result;
            if(grayscale==null)grayscale=new Material(Shader.Find("YunJian/EmoteGrayscale"));
            float scale=Mathf.Min(1,160f/Mathf.Max(source.width,source.height));result=new RenderTexture(Mathf.Max(1,Mathf.RoundToInt(source.width*scale)),Mathf.Max(1,Mathf.RoundToInt(source.height*scale)),0,RenderTextureFormat.ARGB32);result.Create();var previous=RenderTexture.active;Graphics.Blit(source,result,grayscale);RenderTexture.active=previous;gray[source]=result;return result;
        }
        void ClearGray(){foreach(var image in gray.Values){image.Release();UnityEngine.Object.Destroy(image);}gray.Clear();}
        public void Draw(RoomView state,float width,float height,Func<int,bool> send,bool busy,Action<Rect> block){
            Observe(state);if(state==null||!new[]{"play","reaction"}.Contains(state.phase))return;
#if UNITY_EDITOR
            ReviewGuiEnabled=GUI.enabled;
            ReviewTargets.Clear();ReviewBubbleCharacters.Clear();
#endif
            var saved=GUI.color;
            foreach(var pair in bubbles.ToArray()){
                float age=Time.unscaledTime-pair.Value.began;if(age>=Duration){bubbles.Remove(pair.Key);continue;}
                int relative=(pair.Key-state.you+state.n)%state.n;var rect=BubbleRect(relative,state.n,width,height);float alpha=Mathf.Clamp01(age/.12f)*Mathf.Clamp01((Duration-age)/.25f);GUI.color=new Color(saved.r,saved.g,saved.b,saved.a*alpha);
                bool right=relative!=0&&relative!=state.n-1;Slice(rect,"bg_chat_content",new Vector4(49,18,18,40),.5f,right,relative==0);
                var message=pair.Value.message;var picture=CharacterRoster.DisplayTexture("Emoji/"+message.character+"/"+message.emoji);var face=new Rect(rect.x+(right?4:24.5f),rect.y+6,80,80);
                if(picture!=null)GUI.DrawTexture(face,picture,ScaleMode.ScaleToFit,true);else{var thumb=CharacterPackService.Thumbnail(message.character);if(thumb!=null)GUI.DrawTexture(face,thumb,ScaleMode.ScaleToFit,true);}
#if UNITY_EDITOR
                ReviewBubbleCharacters[relative]=message.character;
#endif
            }
            GUI.color=saved;
            var own=state.players.ElementAtOrDefault(state.you);var role=CharacterRoster.Find(own?.character);if(role==null)return;
            if(character!=role.key){character=role.key;scroll.Offset=0;ClearGray();}
            // These small adjustments are screen pixels, independent of the table UI scale.
            float pixelX=1/Mathf.Max(.0001f,Mathf.Abs(GUI.matrix.m00));
            // bg_func_big has seven source pixels of shadow outside its right gold border.
            float rightEdge=(Screen.width-GUI.matrix.m03)*pixelX;
            const float buttonScale=1.2f;
            var button=new Rect(rightEdge-34*buttonScale+7*.55f*buttonScale,height*.48f-50*buttonScale,34*buttonScale,100*buttonScale);var panel=new Rect(width-284,Mathf.Clamp(height*.29f,75,Mathf.Max(75,height-305)),274,270);
            var handle=open?new Rect(panel.x-28+3*pixelX,panel.center.y-60,32,120):button;
            bool wasOpen=open;var e=Event.current;
            if(open&&e.type==EventType.MouseDown&&!panel.Contains(e.mousePosition)&&!handle.Contains(e.mousePosition)){Close();suppressRelease=true;e.Use();}
            if(e.rawType==EventType.MouseUp)suppressRelease=false;
            if(wasOpen&&!open)return;
            if(!open){const float iconSize=22*.85f*.85f*buttonScale;Slice(button,"bg_func_big",new Vector4(30,30,18,30),.55f*buttonScale);GUI.DrawTexture(new Rect(button.center.x-iconSize/2,button.center.y-iconSize/2,iconSize,iconSize),Art("btn_emo"));block(button);if(Hit(button,"emote-open")){open=true;suppressRelease=true;}return;}
            block(panel);block(handle);Slice(panel,"bg_func_big",new Vector4(30,30,18,30),.6f);Slice(handle,"bg_arrow",new Vector4(0,40,0,40),.55f);GUI.DrawTexture(new Rect(handle.center.x-7+8*pixelX,handle.center.y-10,14,20),Art("lifu_arrow"));
            if(Hit(handle,"emote-close")){Close();suppressRelease=true;return;}
            var choices=role.emojis.Select(path=>new{path,id=int.Parse(path.Substring(path.LastIndexOf('/')+1))}).OrderBy(p=>p.id).ToArray();var view=new Rect(panel.x+17,panel.y+14,240,240);float content=Mathf.CeilToInt(choices.Length/3f)*80;scroll.Input(view,content);GUI.BeginGroup(view);
            for(int i=0;i<choices.Length;i++){
                var r=new Rect(i%3*80,i/3*80-scroll.Offset,76,76);if(r.yMax<0||r.y>view.height)continue;var image=CharacterRoster.DisplayTexture(choices[i].path);if(image!=null&&Event.current.type==EventType.Repaint)GUI.DrawTexture(r,Cooldown>0?Gray(image):image,ScaleMode.ScaleToFit,true);
                bool enabled=GUI.enabled;GUI.enabled&=Cooldown<=0&&!busy&&image!=null;
                if(Hit(r,"emote-send-"+choices[i].id)&&send(choices[i].id)){Sending();GUI.enabled=enabled;break;}GUI.enabled=enabled;
            }
            GUI.EndGroup();if(content>view.height)NativePanel.Scrollbar(panel.xMax-10,view.y+3,234,scroll.Offset,content);
        }
        public void Dispose(){ClearGray();if(grayscale!=null)UnityEngine.Object.Destroy(grayscale);bubbles.Clear();}
    }
}
