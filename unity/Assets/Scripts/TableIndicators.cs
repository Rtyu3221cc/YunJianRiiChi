using System;
using System.Collections.Generic;
using UnityEngine;

namespace YunJian.Riichi
{
    public static class TableIndicators
    {
        [Serializable] class Source { public SpriteInfo[] sprites; }
        [Serializable] class SpriteInfo { public string name; public Vector4 border,visibleBounds; }
        static readonly Dictionary<string,Texture2D> art=new Dictionary<string,Texture2D>();
        static readonly Dictionary<string,Rect> visible=new Dictionary<string,Rect>();
        static Source source;
        static GUIStyle countStyle;
        public static Texture2D Art(string name)
        {
            if(!art.TryGetValue(name,out var t))art[name]=t=Resources.Load<Texture2D>("Majsoul/Indicators/"+name);
            return t;
        }
        public static void Image(string name,Rect r)
        {var t=Art(name);if(t!=null)GUI.DrawTexture(r,t,ScaleMode.ScaleToFit);}
        // Position the visible lettering, including its outline, at the table axis.
        public static Rect CenteredImageRect(string name,Vector2 center,float width)
        {
            var t=Art(name);if(t==null)return new Rect(center.x-width/2,center.y-40,width,80);
            if(!visible.TryGetValue(name,out var b)){
                if(source==null)source=JsonUtility.FromJson<Source>(Resources.Load<TextAsset>("Majsoul/Indicators/source").text);
                b=new Rect(0,0,t.width,t.height);
                foreach(var sprite in source.sprites)if(sprite.name==name && sprite.visibleBounds.z>0){var v=sprite.visibleBounds;b=new Rect(v.x,v.y,v.z,v.w);break;}
                visible[name]=b;
            }
            float scale=width/b.width;
            return new Rect(center.x-b.center.x*scale,center.y-b.center.y*scale,t.width*scale,t.height*scale);
        }
        // pop_bound is the original compact blue/gold popup, also used for chi.
        // Native Sprite borders keep the two flowers and clipped corners intact.
        public static void Panel(Rect r,string name="pop_bound",bool containTiles=false)
        {
            var t=Art(name);if(t==null)return;
            if(source==null)source=JsonUtility.FromJson<Source>(Resources.Load<TextAsset>("Majsoul/Indicators/source").text);
            var b=new Vector4(76,87,63,118);
            foreach(var sprite in source.sprites)if(sprite.name==name && sprite.border.sqrMagnitude>0){b=sprite.border;break;}
            float scale=Mathf.Min(.40f,Mathf.Min(r.width/(b.x+b.z+1),r.height/(b.y+b.w+1)));
            float[] dx={r.x,r.x+b.x*scale,r.xMax-b.z*scale,r.xMax},dy={r.y,r.y+b.w*scale,r.yMax-b.y*scale,r.yMax};
            float[] sx={0,b.x,t.width-b.z,t.width},sy={0,b.w,t.height-b.y,t.height};
            foreach(var layer in name=="pop_bound"?new[]{Art("pop_bg"),Art("pop_frame")}:new[]{t}){
                if(layer==null)continue;
                for(int y=0;y<3;y++)for(int x=0;x<3;x++)
                    if(dx[x+1]>dx[x] && dy[y+1]>dy[y])GUI.DrawTextureWithTexCoords(Rect.MinMaxRect(dx[x],dy[y],dx[x+1],dy[y+1]),layer,new Rect(sx[x]/t.width,1-sy[y+1]/t.height,(sx[x+1]-sx[x])/t.width,(sy[y+1]-sy[y])/t.height));
            }
        }
        public static Rect WaitRect(int count,Vector2 center,float bottom,float maxWidth,float tileHeight=76,bool badges=true)
        {
            float cell=tileHeight*.87f;int cols=Mathf.Max(1,Mathf.Min(count,Mathf.Min(badges?6:7,Mathf.FloorToInt((maxWidth-48)/cell))));
            int rows=(count+cols-1)/cols;float height=rows*(tileHeight+(badges?26:8))+(badges?32:16),width=cols*cell+(badges?48:24);
            return new Rect(Mathf.Clamp(center.x-width/2,8,1280-width-8),Mathf.Max(8,bottom-height),width,height);
        }
        public static Rect WaitTileRect(int index,int count,Rect r,float tileHeight=76,bool badges=true)
        {
            float cell=tileHeight*.87f;int cols=Mathf.Max(1,Mathf.RoundToInt((r.width-(badges?48:24))/cell));
            int row=index/cols,rowCount=Mathf.Min(cols,count-row*cols),rows=(count+cols-1)/cols;
            float rowHeight=tileHeight+(badges?26:8),top=r.center.y-rows*rowHeight/2+(badges?0:4);
            float x=r.center.x-rowCount*cell/2+(index%cols)*cell+(cell-tileHeight*.62f)/2;
            return new Rect(x,top+row*rowHeight,tileHeight*.62f,tileHeight);
        }
        public static Rect WaitBadgeRect(Rect tile)=>new Rect(tile.x-2,tile.yMax+4,tile.width+4,18);
        public static void Waits(TableRenderer table,WaitDetail[] waits,Rect r,float tileHeight=76,bool badges=true,bool flowers=true)
        {
            if(waits==null || waits.Length==0)return;
            if(flowers)Panel(r);
            else{
                var old=GUI.color;GUI.color=new Color(.74f,.55f,.29f);GUI.DrawTexture(r,Texture2D.whiteTexture);
                GUI.color=new Color(.035f,.075f,.14f);GUI.DrawTexture(new Rect(r.x+2,r.y+2,r.width-4,r.height-4),Texture2D.whiteTexture);GUI.color=old;
            }
            for(int i=0;i<waits.Length;i++){
                var tile=WaitTileRect(i,waits.Length,r,tileHeight,badges);
                GUI.DrawTexture(tile,table.HandTexture(waits[i].tile),ScaleMode.ScaleToFit);
                if(!badges)continue;
                var badge=WaitBadgeRect(tile);Image("bg_3",badge);
                if(countStyle==null)countStyle=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,font=Resources.Load<Font>("Majsoul/Font/game"),fontSize=16,padding=new RectOffset(0,0,0,0)};
                countStyle.normal.textColor=Color.white;GUI.Label(badge,waits[i].remaining+"张",countStyle);
                if(!waits[i].ron)Image("nofan",new Rect(badge.x,badge.yMax+2,badge.width,18));
            }
        }
    }
}
