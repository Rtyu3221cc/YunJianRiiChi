using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    public static class NativeEffectR12Validation {
        static int checks;
        static void Check(bool pass,string label){if(!pass)throw new Exception(label);checks++;Debug.Log("R12 PASS "+label);}
        static Color Pixel(Material material,Texture2D main,RenderTexture rt,int x=12,int y=4){
            Graphics.Blit(main,rt,material);var old=RenderTexture.active;RenderTexture.active=rt;
            var read=new Texture2D(16,16,TextureFormat.RGBA32,false);read.ReadPixels(new Rect(0,0,16,16),0,0);read.Apply();var color=read.GetPixel(x,y);UnityEngine.Object.DestroyImmediate(read);RenderTexture.active=old;return color;
        }
        public static void Run(){
            var material=new Material(Shader.Find("YunJian/NativeEffectParticle"));var main=new Texture2D(4,4,TextureFormat.RGBA32,false);var mask=new Texture2D(4,4,TextureFormat.RGBA32,false);mask.filterMode=FilterMode.Point;mask.wrapMode=TextureWrapMode.Clamp;
            main.SetPixels(Enumerable.Repeat(Color.white,16).ToArray());main.Apply();
            material.SetFloat("_SourceFamily",2);material.SetFloat("_SrcBlend",1);material.SetFloat("_DstBlend",0);material.SetFloat("_ZTest",8);material.SetFloat("_SourceCull",0);material.SetFloat("_UseMask",1);material.SetTexture("_MaskTex",mask);material.SetColor("_MainColor",new Color(1,1,1,.1f));
            var rt=new RenderTexture(16,16,0,RenderTextureFormat.ARGB32);rt.Create();
            mask.SetPixels(Enumerable.Repeat(new Color(1,1,1,0),16).ToArray());mask.Apply();Check(Pixel(material,main,rt).a<.02f,"Mask transparency uses alpha even when RGB is white");
            mask.SetPixels(Enumerable.Repeat(new Color(0,0,0,1),16).ToArray());mask.Apply();Check(Pixel(material,main,rt).a>.98f,"Opaque black mask remains visible; MainColor alpha does not attenuate source alpha");
            var values=Enumerable.Range(0,16).Select(i=>new Color(1,1,1,i%4<2?1:0)).ToArray();mask.SetPixels(values);mask.Apply();
            float before=Pixel(material,main,rt).a;material.SetVector("_MaskAnim",new Vector4(0,0,0,Mathf.PI));float after=Pixel(material,main,rt).a;Check(before<.02f&&after>.98f,"Authored static mask angle rotates alpha coverage in radians");
            rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(main);UnityEngine.Object.DestroyImmediate(mask);
            var source=Resources.Load<GameObject>("Majsoul/NativeReview/EffectsR11/effect_liqi_23ex_xueqiu");var wrapper=JsonUtility.FromJson<WinTileEffect.Config>(Resources.Load<TextAsset>("Majsoul/Actions/native-effect_liqi_23ex_xueqiu").text);
            var root=source.transform.GetChild(0);Vector3 outward=Quaternion.Euler(wrapper.nativeTileEuler[0],wrapper.nativeTileEuler[1],wrapper.nativeTileEuler[2])*root.localRotation*Vector3.up;
            Check(Vector3.Dot(outward,Vector3.forward)>.999f,"Snowball travel runs along the tile surface toward its long edge");
            var cat=Resources.Load<GameObject>("Majsoul/NativeReview/EffectsR11/effect_liqi_23ex_maozhua").transform.GetChild(0);Check(Vector3.Dot(Quaternion.Euler(0,180,0)*cat.localRotation*Vector3.up,Vector3.forward)>.999f,"Cat paw trail expands along the tile surface");
            File.WriteAllText("../build/effects-r12-review/shader-and-direction-checks.txt",checks+" GPU/material and direction checks passed\n");Debug.Log("NATIVE R12 VALIDATION PASS "+checks);EditorApplication.Exit(0);
        }
    }
}
