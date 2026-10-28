using System;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi {
    [Serializable] public class NativeNamedFloat {public string name;public float value;}
    [Serializable] public class NativeNamedColor {public string name;public float[] value;}
    [Serializable] public class NativeNamedTexture {public string name,texture;public float[] scale,offset;public int filterMode=1,wrapU,wrapV;}
    [Serializable] public class NativeMaterialSource {
        public string texture,shaderName,materialName,materialBundle;
        public float[] tint,textureScale,textureOffset,uvScroll;
        public float srcBlend,dstBlend,zTest,zWrite,cull;
        public int filterMode,wrapU,wrapV;
        public NativeNamedFloat[] floats;
        public NativeNamedColor[] colors;
        public NativeNamedTexture[] extraTextures;
        public string[] keywords;
    }
    [Serializable] public class NativeMotionCurve {
        public int node,kind,material,components;
        public string property;
        public float[] values;
    }
    [Serializable] public class NativeMotionTrack {
        public float duration,fps;
        public bool loop;
        public NativeMotionCurve[] curves;
    }
    [Serializable] public class NativeMotionData {public NativeMotionTrack[] tracks;}
    // The editor samples the original compiled clip through Unity's Playables.
    // Runtime data contains only the channels that actually changed.
    public sealed class NativeEffectMotion:MonoBehaviour {
        public TextAsset source;
        public Transform[] nodes;
        NativeMotionData data;
        Renderer[] renderers;
        MaterialPropertyBlock[][] blocks;
        void Ensure(){
            if(data!=null)return;
            data=JsonUtility.FromJson<NativeMotionData>(source.text);
            renderers=nodes.Select(t=>t.GetComponent<Renderer>()).ToArray();
            blocks=renderers.Select(r=>r==null?null:r.sharedMaterials.Select(m=>new MaterialPropertyBlock()).ToArray()).ToArray();
        }
        public void Sample(float age){
            Ensure();
            foreach(var track in data.tracks){
                float frame=(track.loop&&age>track.duration?Mathf.Repeat(age,track.duration):Mathf.Clamp(age,0,track.duration))*track.fps;
                foreach(var curve in track.curves){
                    int count=curve.values.Length/curve.components,a=Mathf.Min((int)frame,count-1),b=Mathf.Min(a+1,count-1);float blend=frame-a;
                    Func<int,float> v=i=>Mathf.LerpUnclamped(curve.values[a*curve.components+i],curve.values[b*curve.components+i],blend);
                    var target=nodes[curve.node];
                    if(curve.kind==0)target.localPosition=new Vector3(v(0),v(1),v(2));
                    else if(curve.kind==1){int x=a*4,y=b*4;target.localRotation=Quaternion.SlerpUnclamped(new Quaternion(curve.values[x],curve.values[x+1],curve.values[x+2],curve.values[x+3]),new Quaternion(curve.values[y],curve.values[y+1],curve.values[y+2],curve.values[y+3]),blend);}
                    else if(curve.kind==2)target.localScale=new Vector3(v(0),v(1),v(2));
                    else if(curve.kind==6)target.gameObject.SetActive(v(0)>.5f);
                    else {
                        var block=blocks[curve.node][curve.material];
                        if(curve.kind==3)block.SetColor(curve.property,new Color(v(0),v(1),v(2),v(3)));
                        else if(curve.kind==4)block.SetFloat(curve.property,v(0));
                        else block.SetVector(curve.property,new Vector4(v(0),v(1),v(2),v(3)));
                    }
                }
            }
            for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)for(int m=0;m<blocks[i].Length;m++)if(!blocks[i][m].isEmpty)renderers[i].SetPropertyBlock(blocks[i][m],m);
        }
    }
}
