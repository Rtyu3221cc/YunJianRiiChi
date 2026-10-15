using System.Collections.Generic;
using UnityEngine;
namespace YunJian.Riichi
{
    // Original horizontal Sprite borders, with shared physical-pixel edges.
    // A single stretched ellipse distorts the caps; fractional independent
    // IMGUI slices can leave the one-pixel dark seams seen on popup buttons.
    public static class NativeUiChrome
    {
        static readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        static Texture2D Art(string key){if(!textures.TryGetValue(key,out var t))textures[key]=t=Resources.Load<Texture2D>(key);return t;}
        public static void Button(Rect bounds,bool primary)
        {Horizontal(bounds,Art("Majsoul/Lobby/Sprites/Necessary/"+(primary?"button_light":"button")),50,50);}
        public static void CaptionBar(Rect bounds)
        {Horizontal(bounds,Art("Majsoul/UI/button_text_bottom_s"),15,15);}
        static float Pixel(float coordinate,float scale,float offset)
        {return Mathf.Abs(scale)<.00001f?coordinate:(Mathf.Round(coordinate*scale+offset)-offset)/scale;}
        static void Horizontal(Rect bounds,Texture2D texture,float left,float right)
        {
            if(texture==null||bounds.width<=0||bounds.height<=0)return;
            float unit=bounds.height/texture.height,l=Mathf.Min(left*unit,bounds.width/2),r=Mathf.Min(right*unit,bounds.width/2);
            var m=GUI.matrix;float[] x={bounds.x,bounds.x+l,bounds.xMax-r,bounds.xMax};
            for(int i=0;i<x.Length;i++)x[i]=Pixel(x[i],m.m00,m.m03);
            float top=Pixel(bounds.y,m.m11,m.m13),bottom=Pixel(bounds.yMax,m.m11,m.m13);
            float[] u={0,left/texture.width,1-right/texture.width,1};
            for(int i=0;i<3;i++)GUI.DrawTextureWithTexCoords(Rect.MinMaxRect(x[i],top,x[i+1],bottom),texture,new Rect(u[i],0,u[i+1]-u[i],1));
        }
    }
}
