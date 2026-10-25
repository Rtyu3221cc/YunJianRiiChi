using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi {
    // Native skin definitions select a scene and direction. All coordinates are
    // compiled from the APK's Unity defaults, with the original legacy face rule.
    public static class CharacterFraming {
        [Serializable] sealed class Placement {public string scene;public int index;public float[] rect;public bool flip,reverseArt;}
        [Serializable] sealed class Skin {public int id,direction;public string source;public Placement[] placements;public float[] full,head;public bool noReverse,lockReverse;}
        [Serializable] sealed class Bank {public float[] referenceFull,referenceHead;public Skin[] skins;}
        static Bank bank;
        static Dictionary<int,float> aspects;
        static Dictionary<int,CharacterSkin> metadata;
        static CharacterSkin Metadata(int id){if(metadata==null)metadata=CharacterRoster.All.SelectMany(r=>r.skins).ToDictionary(s=>s.id);return metadata.TryGetValue(id,out var skin)?skin:null;}
        static Rect Canvas(int id,Rect rect){
            if(aspects==null)aspects=CharacterRoster.All.SelectMany(r=>r.skins).Where(s=>s.artWidth>0&&s.artHeight>0).ToDictionary(s=>s.id,s=>(float)s.artWidth/s.artHeight);
            // Several APK stills have different padding from web full.png.
            // Preserve the imported Sprite canvas instead of stretching it.
            if(aspects.TryGetValue(id,out float aspect)&&Mathf.Abs(rect.width/rect.height-aspect)>.004f)rect.height=rect.width/aspect;return rect;
        }
        static string ScenePath(int id,string scene,int index=0){var pose=Get(id).placements.FirstOrDefault(p=>p.scene==scene&&p.index==index);return "Skins/"+id+(pose?.reverseArt==true?"/reverse-full":"/full");}
        public static Texture2D SceneTexture(int id,string scene,int index=0,bool asynchronous=false){string path=ScenePath(id,scene,index);return asynchronous?CharacterRoster.DisplayTexture(path):CharacterRoster.Texture(path);}
        public static string CardPath(int id)=>Get(id).placements.All(p=>p.scene!="F0")&&Metadata(id)?.hasWaitingroom==true?"Skins/"+id+"/waitingroom":ScenePath(id,"F0");
        public static IEnumerable<string> AppearancePaths(int id)=>new[]{"Skins/"+id+"/full","Skins/"+id+"/bighead","Skins/"+id+"/smallhead",CardPath(id),ScenePath(id,"I0"),ScenePath(id,"J0"),ScenePath(id,"K0"),ScenePath(id,"B1"),ScenePath(id,"B2")}.Distinct();
        static Skin Get(int id){if(bank==null)bank=JsonUtility.FromJson<Bank>(Resources.Load<TextAsset>("Majsoul/Characters/framing").text);return bank.skins.FirstOrDefault(s=>s.id==id)??bank.skins.First(s=>s.id==400101);}
        public static Rect NativeLocal(int id,string scene,int index,Rect reference,out bool flip){
            var skin=Get(id);var pose=skin.placements.FirstOrDefault(p=>p.scene==scene&&p.index==index);
            if(pose!=null){var p=pose.rect;flip=pose.flip;return Canvas(id,new Rect(p[0],p[1],p[2],p[3]));}
            // Older skins use the source crop rectangle and smallhead centre,
            // rather than fitting the entire drawing (curtains, props included).
            bool face=scene=="I0"||scene=="J0"||scene=="K0";flip=face&&skin.direction!=0&&!skin.lockReverse;
            if(skin.full[2]<=0)return reference;
            var full=bank.referenceFull;var head=bank.referenceHead;
            float unit=reference.width/full[2];
            if(!face){
                string value=scene=="A0"?Metadata(id)?.lobbyOffset:scene=="B0"?Metadata(id)?.dormOffset:null;var offset=(value??"").Split(',');float offsetX=0,offsetY=0,scale=1;
                if(offset.Length>=2){float.TryParse(offset[0],NumberStyles.Float,CultureInfo.InvariantCulture,out offsetX);float.TryParse(offset[1],NumberStyles.Float,CultureInfo.InvariantCulture,out offsetY);}if(offset.Length>=3&&float.TryParse(offset[2],NumberStyles.Float,CultureInfo.InvariantCulture,out float factor)&&factor>0)scale=factor;
                if(id==401902)scale*=.9f;
                return Canvas(id,new Rect(reference.x+(skin.full[0]-full[0]+offsetX)*unit,reference.y-(skin.full[1]-full[1]-offsetY)*reference.height/full[3],reference.width*skin.full[2]/full[2]*scale,reference.height*skin.full[3]/full[3]*scale));
            }
            float dx=(head[0]+head[2]/2-full[0])-(skin.head[0]+skin.head[2]/2-skin.full[0]);
            float dy=(head[1]+head[2]/2-full[1])-(skin.head[1]+skin.head[2]/2-skin.full[1]);
            float w=reference.width*skin.full[2]/full[2],h=reference.height*skin.full[3]/full[3];
            float x=reference.x+dx*unit;if(flip)x+=2*(skin.head[0]+skin.head[2]/2-skin.full[0])*unit;
            return Canvas(id,new Rect(x,reference.y-dy*unit,w,h));
        }
        public static Rect Intro(int id,int slot,Rect reference,Vector2 anchor,bool parentFlip,out bool flip){
            var local=NativeLocal(id,"I0",slot,new Rect(anchor,new Vector2(reference.width,reference.height)),out var localFlip);
            flip=parentFlip^localFlip;float origin=(parentFlip?reference.xMax:reference.x)+(local.x-anchor.x)*(parentFlip?-1:1);
            return new Rect(origin-(flip?local.width:0),reference.y-(local.y-anchor.y),local.width,local.height);
        }
        // Map the APK scene's yiji baseline into an existing viewport. The pose
        // remains skin-specific, including wide outfits and legacy face centres.
        public static Rect FrameRect(int id,string scene,Rect reference,out bool flip){
            var basis=NativeLocal(400101,scene,0,new Rect(0,0,858,1291),out _);
            var local=NativeLocal(id,scene,0,basis,out flip);float u=reference.width/basis.width;
            return new Rect(reference.x+(local.x-basis.x)*u-(flip?local.width*u:0),reference.y-(local.y-basis.y)*u,local.width*u,local.height*u);
        }
        public static void DrawScene(Rect reference,PlayerView player,string scene){
            var skin=CharacterRoster.Skin(player?.character,player?.skin??0);var rect=FrameRect(skin.id,scene,reference,out bool flip);var t=SceneTexture(skin.id,scene);
            if(t!=null)GUI.DrawTextureWithTexCoords(rect,t,new Rect(flip?1:0,0,flip?-1:1,1));
        }
        public static void DrawSkinCard(Rect area,int id,bool asynchronous=false){
            var skin=Get(id);var pose=skin.placements.FirstOrDefault(p=>p.scene=="F0");
            if(pose==null&&Metadata(id)?.hasWaitingroom==true){
                // UI_Character_Skin._setLoadedTexture binds the dedicated
                // waitingroom image directly to its original rectangle for
                // legacy outfits; it does not zoom that pre-cropped image.
                string path="Skins/"+id+"/waitingroom";var waiting=asynchronous?CharacterRoster.DisplayTexture(path):CharacterRoster.Texture(path);
                if(waiting!=null){GUI.BeginGroup(area);GUI.DrawTexture(new Rect(0,0,area.width,area.width*waiting.height/waiting.width),waiting);GUI.EndGroup();}return;
            }
            GUI.BeginGroup(area);var texture=SceneTexture(id,"F0",0,asynchronous);if(texture==null){GUI.EndGroup();return;}
            if(pose!=null){var p=pose.rect;float u=area.width/228;var r=Canvas(id,new Rect(p[0]*u,-p[1]*u,p[2]*u,p[3]*u));if(pose.flip)r.x-=r.width;GUI.DrawTextureWithTexCoords(r,texture,new Rect(pose.flip?1:0,0,pose.flip?-1:1,1));}
            else{var basis=Get(400101).placements.First(p=>p.scene=="F0").rect;var r=NativeLocal(id,"F0",0,new Rect(basis[0],basis[1],basis[2],basis[3]),out bool flip);float u=area.width/228;r=new Rect((r.x-(flip?r.width:0))*u,-r.y*u,r.width*u,r.height*u);GUI.DrawTextureWithTexCoords(r,texture,new Rect(flip?1:0,0,flip?-1:1,1));}
            GUI.EndGroup();
        }
    }
}
