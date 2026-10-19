using System;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi {
    // Native skin definitions select a scene and direction. All coordinates are
    // compiled from the APK's Unity defaults, with the original legacy face rule.
    public static class CharacterFraming {
        [Serializable] sealed class Placement {public string scene;public int index;public float[] rect;public bool flip;}
        [Serializable] sealed class Skin {public int id,direction;public string source;public Placement[] placements;public float[] full,head;public bool noReverse,lockReverse;}
        [Serializable] sealed class Bank {public float[] referenceFull,referenceHead;public Skin[] skins;}
        static Bank bank;
        static Skin Get(int id){if(bank==null)bank=JsonUtility.FromJson<Bank>(Resources.Load<TextAsset>("Majsoul/Characters/framing").text);return bank.skins.First(s=>s.id==id);}
        public static Rect NativeLocal(int id,string scene,int index,Rect reference,out bool flip){
            var skin=Get(id);var pose=skin.placements.FirstOrDefault(p=>p.scene==scene&&p.index==index);
            if(pose!=null){var p=pose.rect;flip=pose.flip;return new Rect(p[0],p[1],p[2],p[3]);}
            // Older skins use the source crop rectangle and smallhead centre,
            // rather than fitting the entire drawing (curtains, props included).
            flip=skin.direction!=0&&!skin.lockReverse;
            if(skin.full[2]<=0)return reference;
            var full=bank.referenceFull;var head=bank.referenceHead;
            float unit=reference.width/full[2];
            float dx=(head[0]+head[2]/2-full[0])-(skin.head[0]+skin.head[2]/2-skin.full[0]);
            float dy=(head[1]+head[2]/2-full[1])-(skin.head[1]+skin.head[2]/2-skin.full[1]);
            float w=reference.width*skin.full[2]/full[2],h=reference.height*skin.full[3]/full[3];
            float x=reference.x+dx*unit;if(flip)x+=2*(skin.head[0]+skin.head[2]/2-skin.full[0])*unit;
            return new Rect(x,reference.y-dy*unit,w,h);
        }
        public static Rect Intro(int id,int slot,Rect reference,Vector2 anchor,bool parentFlip,out bool flip){
            var local=NativeLocal(id,"I0",slot,new Rect(anchor,new Vector2(reference.width,reference.height)),out var localFlip);
            flip=parentFlip^localFlip;float origin=(parentFlip?reference.xMax:reference.x)+(local.x-anchor.x)*(parentFlip?-1:1);
            return new Rect(origin-(flip?local.width:0),reference.y-(local.y-anchor.y),local.width,local.height);
        }
        public static void DrawSkinCard(Rect area,int id){
            var skin=Get(id);var pose=skin.placements.FirstOrDefault(p=>p.scene=="F0");
            GUI.BeginGroup(area);var texture=CharacterRoster.Texture("Skins/"+id+"/full");
            if(pose!=null){var p=pose.rect;float u=area.width/228;var r=new Rect(p[0]*u,-p[1]*u,p[2]*u,p[3]*u);if(pose.flip)r.x-=r.width;GUI.DrawTextureWithTexCoords(r,texture,new Rect(pose.flip?1:0,0,pose.flip?-1:1,1));}
            else{texture=CharacterRoster.Texture("Skins/"+id+"/waitingroom")??texture;float h=area.height*1.6f,w=h*texture.width/texture.height;GUI.DrawTexture(new Rect((area.width-w)/2,15*area.width/228,w,h),texture,ScaleMode.ScaleToFit);}
            GUI.EndGroup();
        }
    }
}
