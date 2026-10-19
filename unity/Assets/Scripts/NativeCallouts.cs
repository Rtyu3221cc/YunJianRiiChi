using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YunJian.Riichi {
    // UI_DesktopInfo's four separate hierarchies, including mirrored portraits,
    // soft alpha masks and native background/word transforms. See Callouts1018/source.
    public sealed class NativeCallouts:IDisposable {
        [Serializable] class Node {public int slot,fixMode;public string path,image;public float[] position,size,pivot,anchor,scale,rotation;}
        [Serializable] class Layout {public Node[] nodes;}
        [Serializable] class WordSprite {public string name;public float[] inkBounds;}
        [Serializable] class WordSprites {public WordSprite[] sprites;}
        sealed class Picture {public Texture2D texture;public Vector2 wordCenter;}
        const float CharacterInkHeight=80f;
        readonly Dictionary<int,Picture> pictures=new Dictionary<int,Picture>();
        readonly Dictionary<string,Texture2D> words=new Dictionary<string,Texture2D>();
        readonly Dictionary<string,Rect> wordInk=new Dictionary<string,Rect>();
        WordSprites wordSource;
        readonly Dictionary<int,RectInt> cropBounds=new Dictionary<int,RectInt>();
        Layout layout;
        Camera camera;RenderTexture target;
#if UNITY_EDITOR
        public Rect LastWordScreenRect {get;private set;}
        public Rect LastWordInkScreenRect {get;private set;}
        public string LastWord {get;private set;}
        public int LastWordSlot {get;private set;}
#endif
        public Rect WordRect(string word,Vector2 center){
            if(!words.TryGetValue(word,out var lettering))words[word]=lettering=Resources.Load<Texture2D>("Majsoul/Callouts1018/"+word);
            if(lettering==null)return new Rect(center,Vector2.zero);
            if(!wordInk.TryGetValue(word,out var ink)){
                if(wordSource==null)wordSource=JsonUtility.FromJson<WordSprites>(Resources.Load<TextAsset>("Majsoul/Callouts1018/word-sprites").text);
                ink=new Rect(0,0,lettering.width,lettering.height);
                foreach(var sprite in wordSource.sprites)if(sprite.name==word){var b=sprite.inkBounds;ink=new Rect(b[0],b[1],b[2],b[3]);break;}
                wordInk[word]=ink;
            }
            float scale=CharacterInkHeight/ink.height;
            return new Rect(center.x-ink.center.x*scale,center.y-ink.center.y*scale,lettering.width*scale,lettering.height*scale);
        }
        public void Draw(string word,int relative,int n,float width,float height,float age,PlayerView player=null){
            if(age<0||age>=.8f||Event.current.type!=EventType.Repaint)return;
            int slot=relative==0?0:relative==n-1?3:relative==1&&n==4?1:2;
            int pictureKey=slot*1000000+CharacterRoster.Skin(player?.character,player?.skin??0).id;
            if(!pictures.TryGetValue(pictureKey,out var picture))pictures[pictureKey]=picture=Compose(slot,CharacterRoster.Portrait(player),CharacterRoster.Skin(player?.character,player?.skin??0).id);
            if(!words.TryGetValue(word,out var lettering))words[word]=lettering=Resources.Load<Texture2D>("Majsoul/Callouts1018/"+word);
            // These direction-specific scales affect only the portrait/background.
            float unit=slot==0?.55f:slot==1?.50f:slot==2?.49f:.48f;
            float w=picture.texture.width*unit,h=picture.texture.height*unit;
            Vector2 pos=slot==0?new Vector2(width*.475f-w/2,height*.84f-h):slot==1?new Vector2(width-12-w,height*.70f-h):slot==2?new Vector2(width*.512f-w/2,height*.39f-h):new Vector2(width*.03f,height*.70f-h);
            // Official shout(): linear 70 ms entrance, hold to .6 s, fade for .15 s.
            float entry=1-Mathf.Clamp01(age/.07f);
            pos+=unit*entry*(slot==0?new Vector2(-105,0):slot==1?new Vector2(500,0):slot==2?new Vector2(0,-300):new Vector2(-500,0));
            var old=GUI.color;GUI.color=new Color(old.r,old.g,old.b,old.a*Mathf.Clamp01(age/.07f)*Mathf.Clamp01((.75f-age)/.15f));
            GUI.DrawTexture(new Rect(pos.x,pos.y,w,h),picture.texture,ScaleMode.StretchToFill,true);
            if(lettering!=null){
                // Equal visible character height, not equal width for the entire word.
                // Two-character sprites expand naturally, with uniform scaling of the
                // complete native canvas, including its original transparent margins.
                var textRect=WordRect(word,pos+picture.wordCenter*unit);
                GUI.DrawTexture(textRect,lettering,ScaleMode.StretchToFill,true);
#if UNITY_EDITOR
                var ink=wordInk[word];float scale=textRect.width/lettering.width;
                var tl=GUI.matrix.MultiplyPoint3x4(new Vector3(textRect.x,textRect.y,0));var br=GUI.matrix.MultiplyPoint3x4(new Vector3(textRect.xMax,textRect.yMax,0));
                LastWordScreenRect=Rect.MinMaxRect(tl.x,tl.y,br.x,br.y);LastWord=word;LastWordSlot=slot;
                var itl=GUI.matrix.MultiplyPoint3x4(new Vector3(textRect.x+ink.x*scale,textRect.y+ink.y*scale,0));var ibr=GUI.matrix.MultiplyPoint3x4(new Vector3(textRect.x+ink.xMax*scale,textRect.y+ink.yMax*scale,0));
                LastWordInkScreenRect=Rect.MinMaxRect(itl.x,itl.y,ibr.x,ibr.y);
#endif
            }
            GUI.color=old;
        }
        Picture Compose(int slot,Texture2D character,int skin){
            if(layout==null)layout=JsonUtility.FromJson<Layout>(Resources.Load<TextAsset>("Majsoul/Callouts1018/layout").text);
            if(camera==null){
                camera=new GameObject("Native shout composition camera").AddComponent<Camera>();camera.enabled=false;camera.transform.position=new Vector3(6000,5000,-20);camera.orthographic=true;camera.orthographicSize=1024;camera.aspect=1;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<15;camera.nearClipPlane=.1f;camera.farClipPlane=100;
                target=new RenderTexture(2048,2048,0,RenderTextureFormat.ARGB32){name="APK native shout composition"};target.Create();camera.targetTexture=target;
            }
            var transforms=new Dictionary<string,Transform>();var source=new Dictionary<string,Node>();var meshes=new List<Mesh>();var materials=new List<Material>();GameObject root=null;Material portrait=null;
            foreach(var template in layout.nodes){
                if(template.slot!=slot)continue;
                var node=JsonUtility.FromJson<Node>(JsonUtility.ToJson(template));
                bool illustration=node.path=="illust/mask/father/illust",background=node.path=="container_shout",letter=node.path=="img_content";
                if(illustration){var placed=CharacterFraming.NativeLocal(skin,"J0",slot,new Rect(node.position[0],node.position[1],node.size[0],node.size[1]),out var flip);node.position=new[]{placed.x,placed.y};node.size=new[]{placed.width,placed.height};node.scale[0]=flip?-1:1;}
                var go=new GameObject("Native shout "+node.path);go.layer=15;var t=go.transform;
                if(node.path.Length==0){root=go;t.position=new Vector3(6000,5000,0);}
                else{
                    string parent=node.path.Contains("/")?node.path.Substring(0,node.path.LastIndexOf('/')):"";var pn=source[parent];t.SetParent(transforms[parent],false);
                    t.localPosition=new Vector3(node.position[0]+pn.size[0]*(node.anchor[0]-pn.pivot[0]),node.position[1]+pn.size[1]*(node.anchor[1]-pn.pivot[1]),0);
                    t.localScale=new Vector3(node.scale[0],node.scale[1],node.scale[2]);t.localRotation=new Quaternion(node.rotation[0],node.rotation[1],node.rotation[2],node.rotation[3]);
                }
                transforms[node.path]=t;source[node.path]=node;
                if(!illustration&&!background&&!letter)continue;
                var texture=illustration?character:Resources.Load<Texture2D>("Majsoul/Callouts1018/"+(letter?"shout_lizhi":node.image));
                // APK capsui.CImage.FixImage: EnumFixMode 1 = FORCE_KEEP_WIDTH.
                // These textures retain Sprite.rect and its transparent trim margins;
                // stretching a tight Sprite.image crop into this rectangle deforms text.
                if(!illustration&&node.fixMode==1&&texture!=null)node.size[1]=node.size[0]*texture.height/texture.width;
                else if(!illustration&&node.fixMode==2&&texture!=null)node.size[0]=node.size[1]*texture.width/texture.height;
                // The word is drawn independently by Draw(), after this composition.
                if(letter)continue;
                float l=-node.pivot[0]*node.size[0],r=l+node.size[0],b=-node.pivot[1]*node.size[1],u=b+node.size[1];
                var mesh=new Mesh{name="APK shout quad",vertices=new[]{new Vector3(l,b,0),new Vector3(l,u,0),new Vector3(r,u,0),new Vector3(r,b,0)},uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right},triangles=new[]{0,1,2,0,2,3}};mesh.RecalculateBounds();meshes.Add(mesh);
                var material=new Material(Shader.Find("YunJian/NativeCallout"));materials.Add(material);material.renderQueue=3000+(background?0:illustration?1:2);
                material.mainTexture=texture;
                if(illustration)portrait=material;
                go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
            var mask=source["illust/mask"];
            portrait.SetTexture("_MaskTex",Resources.Load<Texture2D>("Majsoul/Callouts1018/"+mask.image));portrait.SetFloat("_UseMask",1);
            portrait.SetMatrix("_WorldToMask",Matrix4x4.Translate(new Vector3(mask.pivot[0],mask.pivot[1],0))*Matrix4x4.Scale(new Vector3(1/mask.size[0],1/mask.size[1],1))*transforms["illust/mask"].worldToLocalMatrix);
            camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
            var read=new Texture2D(2048,2048,TextureFormat.RGBA32,false);read.ReadPixels(new Rect(0,0,2048,2048),0,0);read.Apply();RenderTexture.active=previous;
            var pixels=read.GetPixels32();int xmin=2048,ymin=2048,xmax=0,ymax=0;
            for(int y=0;y<2048;y++)for(int x=0;x<2048;x++)if(pixels[y*2048+x].a>2){xmin=Mathf.Min(xmin,x);xmax=Mathf.Max(xmax,x);ymin=Mathf.Min(ymin,y);ymax=Mathf.Max(ymax,y);}
            if(!cropBounds.TryGetValue(slot,out var bounds)){
                // Include the entire native word rectangle, so swapping one/two
                // characters cannot shift the portrait or its background.
                var letter=source["img_content"];var lt=transforms["img_content"];
                foreach(float x in new[]{-letter.pivot[0]*letter.size[0],(1-letter.pivot[0])*letter.size[0]})foreach(float y in new[]{-letter.pivot[1]*letter.size[1],(1-letter.pivot[1])*letter.size[1]}){
                    var p=lt.TransformPoint(new Vector3(x,y,0))-new Vector3(6000-1024,5000-1024,0);xmin=Mathf.Min(xmin,Mathf.FloorToInt(p.x));xmax=Mathf.Max(xmax,Mathf.CeilToInt(p.x));ymin=Mathf.Min(ymin,Mathf.FloorToInt(p.y));ymax=Mathf.Max(ymax,Mathf.CeilToInt(p.y));
                }
                cropBounds[slot]=bounds=new RectInt(xmin,ymin,xmax-xmin+1,ymax-ymin+1);
            }
            xmin=bounds.x;ymin=bounds.y;xmax=bounds.xMax-1;ymax=bounds.yMax-1;
            int width=xmax-xmin+1,height=ymax-ymin+1;var crop=new Color32[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++){
                var c=pixels[(y+ymin)*2048+x+xmin];
                // Render targets contain premultiplied colour; GUI expects straight alpha.
                if(c.a>0){c.r=(byte)Mathf.Min(255,c.r*255/c.a);c.g=(byte)Mathf.Min(255,c.g*255/c.a);c.b=(byte)Mathf.Min(255,c.b*255/c.a);}crop[y*width+x]=c;
            }
            var wn=source["img_content"];var wc=transforms["img_content"].TransformPoint(new Vector3((.5f-wn.pivot[0])*wn.size[0],(.5f-wn.pivot[1])*wn.size[1],0))-new Vector3(6000-1024,5000-1024,0);
            var wordCenter=new Vector2(wc.x-bounds.x,bounds.yMax-wc.y);
            var result=new Texture2D(width,height,TextureFormat.RGBA32,false){name="APK shout background "+slot,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};result.SetPixels32(crop);result.Apply(false,true);
            root.SetActive(false);UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(read);foreach(var m in meshes)UnityEngine.Object.Destroy(m);foreach(var m in materials)UnityEngine.Object.Destroy(m);
            return new Picture{texture=result,wordCenter=wordCenter};
        }
        public void Dispose(){foreach(var p in pictures.Values)UnityEngine.Object.Destroy(p.texture);pictures.Clear();words.Clear();wordInk.Clear();cropBounds.Clear();if(camera!=null)UnityEngine.Object.Destroy(camera.gameObject);if(target!=null){target.Release();UnityEngine.Object.Destroy(target);}camera=null;target=null;}
    }
}
