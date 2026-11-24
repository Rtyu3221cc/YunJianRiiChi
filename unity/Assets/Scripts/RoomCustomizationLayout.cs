using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi {
    // RectTransform anchors and CImage borders from the original UI_ChangeDeco.
    // Resolving them as one tree keeps frame edges and content aligned at both widths.
    public sealed class RoomCustomizationLayout : IDisposable {
        [Serializable] public sealed class Node {public string path,parent,image;public string[] alternates;public float[] position,size,anchorMin,anchorMax,pivot,scale;}
        [Serializable] public sealed class Image {public string key,resource;public float[] border;public float width,height;}
        [Serializable] sealed class RoleStyle {public int id,ur;}
        [Serializable] sealed class Data {public RoleStyle[] roleStyles;public float characterWidth,wardrobeWidth;public Node[] nodes;public Image[] images;}
        readonly Data data=JsonUtility.FromJson<Data>(Resources.Load<TextAsset>("Majsoul/RoomCustomization/layout").text);
        readonly Dictionary<string,Node> nodes;readonly Dictionary<string,Rect> rects=new Dictionary<string,Rect>();
        readonly Dictionary<string,Vector2> scales=new Dictionary<string,Vector2>();
        readonly Dictionary<string,Image> images;readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        public bool Warm {get;private set;}
        public IEnumerator Preload(){
            foreach(var pair in images){
                if(textures.TryGetValue(pair.Key,out var cached)&&cached!=null)continue;
                string path=pair.Value.resource.StartsWith("Majsoul/")?pair.Value.resource:"Majsoul/RoomCustomization/"+pair.Value.resource;
                var request=Resources.LoadAsync<Texture2D>(path);yield return request;if(request.asset is Texture2D texture)textures[pair.Key]=texture;
            }
            Warm=images.Keys.All(key=>textures.TryGetValue(key,out var texture)&&texture!=null);
        }
        public Rect Frame {get;private set;}
        public Rect Clip;
        public RoomCustomizationLayout(){nodes=data.nodes.ToDictionary(n=>n.path);images=data.images.ToDictionary(n=>n.key);}
        public void Place(float screenWidth,bool wardrobe){
            float width=wardrobe?data.wardrobeWidth:data.characterWidth;
            Frame=new Rect(screenWidth/2+900.7f-width,146.8f,width,889.6f);rects.Clear();scales.Clear();rects[""]=Frame;scales[""]=Vector2.one;
            foreach(var n in data.nodes.Skip(1)){
                var p=rects[n.parent];var s=scales[n.parent];float w=(p.width*(n.anchorMax[0]-n.anchorMin[0])+n.size[0]*s.x)*n.scale[0],h=(p.height*(n.anchorMax[1]-n.anchorMin[1])+n.size[1]*s.y)*n.scale[1];
                float ax=Mathf.Lerp(n.anchorMin[0],n.anchorMax[0],n.pivot[0]),ay=Mathf.Lerp(n.anchorMin[1],n.anchorMax[1],n.pivot[1]);
                rects[n.path]=new Rect(p.x+p.width*ax+n.position[0]*s.x-w*n.pivot[0],p.y+p.height*(1-ay)-n.position[1]*s.y-h*(1-n.pivot[1]),w,h);
                scales[n.path]=Vector2.Scale(s,new Vector2(n.scale[0],n.scale[1]));
            }
        }
        public Rect Rect(string path,Vector2 offset=default){var r=rects[path];r.position+=offset;return r;}
        public void Draw(string path,Vector2 offset=default,int alternate=-1){var n=nodes[path];string key=alternate>=0&&n.alternates.Length>alternate?n.alternates[alternate]:n.image;DrawImage(Rect(path,offset),key);}
        // Every character has the contract unlocked in this project.
        public string RoleBorder(int id)=>data.roleStyles.Any(s=>s.id==id&&s.ur==1)?"ui/lobby/main/pic/bg_urhead_qy":"ui/lobby/main/pic/bg_head_qy2";
        public void DrawNative(string path,string key,Vector2 offset){
            var r=Rect(path,offset);if(images.TryGetValue(key,out var row)&&row.width>0){var n=nodes[path];var scale=scales[path];float w=row.width*scale.x,h=row.height*scale.y;r=new Rect(r.x+(r.width-w)*n.pivot[0],r.y+(r.height-h)*(1-n.pivot[1]),w,h);}DrawImage(r,key);
        }
        public void DrawImage(Rect r,string key,bool fit=false){
            if(Event.current.type!=EventType.Repaint||string.IsNullOrEmpty(key)||!images.TryGetValue(key,out var row))return;
            if(!textures.TryGetValue(key,out var texture)){texture=Resources.Load<Texture2D>(row.resource.StartsWith("Majsoul/")?row.resource:"Majsoul/RoomCustomization/"+row.resource);textures[key]=texture;}
            if(texture==null)return;var border=row.border;
            if(fit||border==null||border.All(v=>v==0)){GUI.DrawTexture(r,texture,fit?ScaleMode.ScaleToFit:ScaleMode.StretchToFill,true);return;}
            if(!materials.TryGetValue(key,out var material)){material=new Material(Resources.Load<Shader>("Majsoul/Wardrobe/NineSlice")){mainTexture=texture};material.SetVector("_Border",new Vector4(border[0],border[1],border[2],border[3]));material.SetVector("_SourceSize",new Vector4(texture.width,texture.height,0,0));materials[key]=material;}
            material.SetVector("_TargetSize",new Vector4(r.width,r.height,0,0));material.SetColor("_Tint",GUI.color);
            var visible=r;var uv=new Rect(0,0,1,1);
            if(Clip.width>0){visible=UnityEngine.Rect.MinMaxRect(Mathf.Max(r.xMin,Clip.xMin),Mathf.Max(r.yMin,Clip.yMin),Mathf.Min(r.xMax,Clip.xMax),Mathf.Min(r.yMax,Clip.yMax));if(visible.width<=0||visible.height<=0)return;uv=new Rect((visible.x-r.x)/r.width,1-(visible.yMax-r.y)/r.height,visible.width/r.width,visible.height/r.height);}
            Graphics.DrawTexture(visible,texture,uv,0,0,0,0,Color.white,material);
        }
        public void Dispose(){foreach(var m in materials.Values)UnityEngine.Object.Destroy(m);materials.Clear();}
    }
}
