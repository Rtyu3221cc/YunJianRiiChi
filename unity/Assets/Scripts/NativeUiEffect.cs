using System;
using UnityEngine;
namespace YunJian.Riichi
{
    // Original scene particles rendered through their own transparent UI camera.
    public sealed class NativeUiEffect : IDisposable
    {
        GameObject root;Camera camera;RenderTexture image;WinTileEffect particles;NativeParticleEffect native;string resource;
        static int serial;
        readonly System.Collections.Generic.List<Material> uiMaterials=new System.Collections.Generic.List<Material>();Material composite;
        public int LayerCount=>native!=null?native.LayerCount:particles==null?0:particles.LayerCount;
        public int ParticleCount=>native!=null?native.ParticleCount:particles==null?0:particles.ParticleCount;
        public void Draw(Rect r,string path,float age,float nativeScale)
        {
            if(age<0)return;
            // Include the additive glow tail instead of cutting it at the title's
            // tight viewport. Increasing both viewport and projection preserves size.
            if(path.EndsWith("effect_yiman")||path.EndsWith("effect_yiman2"))r=new Rect(r.x-r.width/2,r.y-r.height/2,r.width*2,r.height*2);
            if(resource!=path){Dispose();resource=path;root=new GameObject("Native UI "+path);root.transform.position=new Vector3(5000+100*serial++,5000,0);
                var effect=new GameObject("Original particles");effect.transform.SetParent(root.transform,false);
                if(path.EndsWith("effect_yiman") || path.EndsWith("effect_yiman2")){
                    native=effect.AddComponent<NativeParticleEffect>();effect.transform.localScale=Vector3.one*nativeScale;native.Initialize(path.Substring(path.LastIndexOf('/')+1));
                    foreach(var renderer in effect.GetComponentsInChildren<ParticleSystemRenderer>(true)){var material=new Material(renderer.sharedMaterial){shader=Resources.Load<Shader>("Majsoul/UiParticleBuffer")};uiMaterials.Add(material);renderer.sharedMaterial=material;}
                    composite=new Material(Resources.Load<Shader>("Majsoul/UiParticleComposite"));
                }
                else {particles=effect.AddComponent<WinTileEffect>();particles.InitializeUi(path,nativeScale,0,507);}
                foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=29;
                var eye=new GameObject("UI effect camera");eye.transform.SetParent(root.transform,false);eye.transform.localPosition=new Vector3(0,0,-50);camera=eye.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<29;camera.nearClipPlane=.1f;camera.farClipPlane=100;
                image=new RenderTexture(1024,512,16,RenderTextureFormat.ARGB32){name="Original UI effect texture"};image.Create();camera.targetTexture=image;
            }
            if(Event.current.type==EventType.Repaint){camera.aspect=r.width/r.height;camera.orthographicSize=r.height/80;if(native!=null)native.Sample(age);else particles.Sample(age);camera.Render();}
            if(composite!=null){
                if(Event.current.type==EventType.Repaint){composite.SetColor("_Tint",GUI.color);Graphics.DrawTexture(r,image,new Rect(0,0,1,1),0,0,0,0,Color.white,composite);}
            }else GUI.DrawTexture(r,image,ScaleMode.StretchToFill,true);
        }
        public void Dispose(){if(root!=null)UnityEngine.Object.Destroy(root);if(image!=null){image.Release();UnityEngine.Object.Destroy(image);}foreach(var material in uiMaterials)UnityEngine.Object.Destroy(material);uiMaterials.Clear();if(composite!=null)UnityEngine.Object.Destroy(composite);composite=null;root=null;camera=null;image=null;particles=null;native=null;resource=null;}
    }
}
