using System;
using System.Collections.Generic;
using UnityEngine;

namespace YunJian.Riichi
{
    public static class TableIndicators
    {
        [Serializable] class Source { public SpriteInfo[] sprites; }
        [Serializable] class SpriteInfo { public string name; public Vector4 border; }
        static readonly Dictionary<string,Texture2D> art=new Dictionary<string,Texture2D>();
        static Source source;
        public static Texture2D Art(string name)
        {
            if(!art.TryGetValue(name,out var t))art[name]=t=Resources.Load<Texture2D>("Majsoul/Indicators/"+name);
            return t;
        }
        public static void Image(string name,Rect r)
        {var t=Art(name);if(t!=null)GUI.DrawTexture(r,t,ScaleMode.ScaleToFit);}
        // Native CImage uses sprite borders. Keep the flowers and frame intact as
        // the panel grows for several chi combinations or thirteen-sided waits.
        public static void Panel(Rect r,string name="bg_chipengchoose")
        {
            var t=Art(name);if(t==null)return;
            if(source==null)source=JsonUtility.FromJson<Source>(Resources.Load<TextAsset>("Majsoul/Indicators/source").text);
            var b=new Vector4(55,45,55,45);
            foreach(var sprite in source.sprites)if(sprite.name==name && sprite.border.sqrMagnitude>0){b=sprite.border;break;}
            // The native frame only slices horizontally. Preserve its transparent
            // flower margins vertically and keep the inner fill within the frame.
            if(b.y==0 && b.w==0){b.y=35;b.w=35;}
            float s=Mathf.Min(.6f,Mathf.Min(r.width/(b.x+b.z+1),r.height/(b.y+b.w+1)));
            float[] dx={r.x,r.x+b.x*s,r.xMax-b.z*s,r.xMax},dy={r.y,r.y+b.w*s,r.yMax-b.y*s,r.yMax};
            float[] sx={0,b.x,t.width-b.z,t.width},sy={0,b.w,t.height-b.y,t.height};
            var old=GUI.color;GUI.color=new Color(.03f,.07f,.15f,.96f);GUI.DrawTexture(new Rect(dx[1],dy[1],dx[2]-dx[1],dy[2]-dy[1]),Texture2D.whiteTexture);GUI.color=old;
            for(int y=0;y<3;y++)for(int x=0;x<3;x++)
                if(dx[x+1]>dx[x] && dy[y+1]>dy[y])GUI.DrawTextureWithTexCoords(Rect.MinMaxRect(dx[x],dy[y],dx[x+1],dy[y+1]),t,new Rect(sx[x]/t.width,1-sy[y+1]/t.height,(sx[x+1]-sx[x])/t.width,(sy[y+1]-sy[y])/t.height));
        }
        public static Rect WaitRect(int count,Vector2 center,float bottom,float maxWidth,float tileHeight=76,bool badges=true)
        {
            float cell=tileHeight*.87f;int cols=Mathf.Max(1,Mathf.Min(count,Mathf.Min(badges?6:7,Mathf.FloorToInt((maxWidth-32)/cell))));
            int rows=(count+cols-1)/cols;float height=rows*(tileHeight+(badges?25:8))+(badges?42:18),width=cols*cell+32;
            return new Rect(Mathf.Clamp(center.x-width/2,8,1280-width-8),Mathf.Max(8,bottom-height),width,height);
        }
        public static void Waits(TableRenderer table,WaitDetail[] waits,Rect r,float tileHeight=76,bool badges=true,bool flowers=true)
        {
            if(waits==null || waits.Length==0)return;
            if(flowers)Panel(r);
            else {
                var old=GUI.color;GUI.color=new Color(.74f,.55f,.29f);GUI.DrawTexture(r,Texture2D.whiteTexture);
                GUI.color=new Color(.035f,.075f,.14f);GUI.DrawTexture(new Rect(r.x+2,r.y+2,r.width-4,r.height-4),Texture2D.whiteTexture);GUI.color=old;
            }
            float cell=tileHeight*.87f;int cols=Mathf.Max(1,Mathf.RoundToInt((r.width-32)/cell));
            for(int i=0;i<waits.Length;i++){
                float x=r.x+16+(i%cols)*cell,y=r.y+(badges?22:8)+(i/cols)*(tileHeight+(badges?25:8));
                GUI.DrawTexture(new Rect(x+(cell-tileHeight*.62f)/2,y,tileHeight*.62f,tileHeight),table.HandTexture(waits[i].tile),ScaleMode.ScaleToFit);
                if(badges && !waits[i].ron)Image("nofan",new Rect(x+6,y+tileHeight+3,cell-12,20));
            }
        }
    }
}
