using System;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi {
    // Move the original rig; avoid storing full duplicate meshes for every frame.
    public sealed class CharacterHandVisual:MonoBehaviour {
        Transform[] nodes;NativeMotionCurve[] curves;
        public void Initialize(CharacterHandClip clip){
            var frame=new GameObject("Source hand coordinate frame").transform;frame.SetParent(transform,false);frame.localRotation=Quaternion.Euler(0,180,0);
            var prefab=Resources.Load<GameObject>(clip.prefab);if(prefab==null)throw new InvalidOperationException("Missing character hand rig "+clip.prefab);
            var rig=Instantiate(prefab,frame,false);nodes=rig.GetComponentsInChildren<Transform>(true);curves=clip.pose.tracks.SelectMany(t=>t.curves).ToArray();
            foreach(var node in nodes)node.gameObject.layer=gameObject.layer;
            foreach(var renderer in rig.GetComponentsInChildren<Renderer>(true))if(renderer.name.IndexOf("shadow",StringComparison.OrdinalIgnoreCase)>=0||renderer.name.IndexOf("yingz",StringComparison.OrdinalIgnoreCase)>=0)renderer.enabled=false;
            SampleFrame(0);
        }
        public void SampleFrame(float frame){
            foreach(var curve in curves){
                int count=curve.values.Length/curve.components,a=Mathf.Clamp((int)frame,0,count-1),b=Mathf.Min(a+1,count-1);float p=Mathf.Clamp01(frame-a);int x=a*curve.components,y=b*curve.components;var v=curve.values;var node=nodes[curve.node];
                if(curve.kind==1)node.localRotation=Quaternion.SlerpUnclamped(new Quaternion(v[x],v[x+1],v[x+2],v[x+3]),new Quaternion(v[y],v[y+1],v[y+2],v[y+3]),p);
                else {var value=Vector3.LerpUnclamped(new Vector3(v[x],v[x+1],v[x+2]),new Vector3(v[y],v[y+1],v[y+2]),p);if(curve.kind==0)node.localPosition=value;else node.localScale=value;}
            }
        }
    }
}
