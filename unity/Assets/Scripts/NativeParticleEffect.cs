using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace YunJian.Riichi {
 [Serializable] public class NativeEffectNode {
  public string name,particle,renderer,texture,mesh,shaderBundle,shaderName;public int parent,renderMode;public float[] position,rotation,scale,tint;public float srcBlend,dstBlend,sortingFudge,lengthScale;
 }
 [Serializable] public class NativeEffectSource {public NativeEffectNode[] nodes;}
 // Components in these prefabs are imported from whole APK serialized modules.
 public sealed class NativeParticleEffect:MonoBehaviour {
  ParticleSystem[] particles;float previous=-1;readonly List<Material> copies=new List<Material>();
  static readonly Dictionary<string,AssetBundle> shaders=new Dictionary<string,AssetBundle>();
  public int LayerCount=>particles?.Length??0;
  public int ParticleCount=>particles?.Sum(p=>p.particleCount)??0;
  public bool UsesApkShader {get;private set;}
  public void Initialize(string name,uint seed=507,bool useApkShader=false){
   var prefab=Resources.Load<GameObject>("Majsoul/NativeReview/"+name);if(prefab==null)throw new InvalidOperationException("Missing imported APK prefab "+name);
   var clone=Instantiate(prefab,transform,false);particles=clone.GetComponentsInChildren<ParticleSystem>(true);
   for(int i=0;i<particles.Length;i++){particles[i].Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);particles[i].useAutoRandomSeed=false;particles[i].randomSeed=seed+(uint)i+1;}
#if UNITY_ANDROID && !UNITY_EDITOR
   var source=JsonUtility.FromJson<NativeEffectSource>(Resources.Load<TextAsset>("Majsoul/NativeReview/"+name+"-source").text);var renderers=clone.GetComponentsInChildren<ParticleSystemRenderer>(true);
   foreach(var node in source.nodes.Where(n=>useApkShader&&!string.IsNullOrEmpty(n.shaderBundle))){
    if(!shaders.TryGetValue(node.shaderBundle,out var bundle)){var data=Resources.Load<TextAsset>("Majsoul/NativeReview/"+node.shaderBundle);shaders[node.shaderBundle]=bundle=data==null?null:AssetBundle.LoadFromMemory(data.bytes);}
    var shader=bundle?.LoadAllAssets<Shader>().FirstOrDefault(s=>s.name==node.shaderName);if(shader==null || !shader.isSupported)continue;
    var renderer=renderers.FirstOrDefault(r=>r.name==node.name);if(renderer==null)continue;var material=new Material(renderer.sharedMaterial){shader=shader};copies.Add(material);renderer.sharedMaterial=material;UsesApkShader=true;
   }
#endif
   Sample(-1);
  }
  public void Sample(float age){
   if(age<0){foreach(var p in particles)p.Clear(false);previous=-1;return;}
   bool restart=previous<0 || age<previous;float dt=restart?age:age-previous;
   foreach(var p in particles)p.Simulate(dt,false,restart,false);previous=age;
  }
  void OnDestroy(){foreach(var m in copies)Destroy(m);}
 }
}
