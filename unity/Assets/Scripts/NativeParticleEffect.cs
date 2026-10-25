using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace YunJian.Riichi {
 [Serializable] public class NativeEffectNode {
  public string name,particle,renderer,texture,mesh,shaderBundle,shaderName;public int parent,renderMode;public float[] position,rotation,scale,tint,textureScale,textureOffset,uvScroll;public float srcBlend,dstBlend,sortingFudge,lengthScale;
 }
 [Serializable] public class NativeEffectSource {public NativeEffectNode[] nodes;}
 // Components in these prefabs are imported from whole APK serialized modules.
 public sealed class NativeParticleEffect:MonoBehaviour {
  ParticleSystem[] particles;float previous=-1;readonly List<Material> copies=new List<Material>();
  sealed class Scroll {public Material material;public Vector2 origin,speed;}
  readonly List<Scroll> scrolls=new List<Scroll>();
  static readonly Dictionary<string,AssetBundle> shaders=new Dictionary<string,AssetBundle>();
  public int LayerCount=>particles?.Length??0;
  public int ParticleCount=>particles?.Sum(p=>p.particleCount)??0;
  public bool UsesApkShader {get;private set;}
  public Quaternion SurfaceMountRotation {get;private set;}=Quaternion.identity;
  public static Quaternion SurfaceMount(string name){
   var prefab=Resources.Load<GameObject>("Majsoul/NativeReview/"+name);if(prefab==null)throw new InvalidOperationException("Missing imported APK prefab "+name);
   var root=prefab.transform.childCount>0?prefab.transform.GetChild(0):prefab.transform;return Quaternion.FromToRotation(root.localRotation*Vector3.forward,Vector3.up);
  }
  public void Initialize(string name,uint seed=507,bool useApkShader=false,bool normalizeSurfaceOrigin=false){
   var prefab=Resources.Load<GameObject>("Majsoul/NativeReview/"+name);if(prefab==null)throw new InvalidOperationException("Missing imported APK prefab "+name);
   var clone=Instantiate(prefab,transform,false);particles=clone.GetComponentsInChildren<ParticleSystem>(true);
   // Imported prefabs have a neutral wrapper and an authored root. Some roots
   // already face the cloth; others retain the tile's original XY model plane.
   var authored=clone.transform.childCount>0?clone.transform.GetChild(0):clone.transform;
   if(normalizeSurfaceOrigin)authored.localPosition=Vector3.zero;
   SurfaceMountRotation=Quaternion.FromToRotation(authored.localRotation*Vector3.forward,Vector3.up);
   for(int i=0;i<particles.Length;i++){particles[i].Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);particles[i].useAutoRandomSeed=false;particles[i].randomSeed=seed+(uint)i+1;}
#if UNITY_ANDROID && !UNITY_EDITOR
   var source=JsonUtility.FromJson<NativeEffectSource>(Resources.Load<TextAsset>("Majsoul/NativeReview/"+name+"-source").text);var renderers=clone.GetComponentsInChildren<ParticleSystemRenderer>(true);
   foreach(var node in source.nodes.Where(n=>useApkShader&&!string.IsNullOrEmpty(n.shaderBundle))){
    if(!shaders.TryGetValue(node.shaderBundle,out var bundle)){var data=Resources.Load<TextAsset>("Majsoul/NativeReview/"+node.shaderBundle);shaders[node.shaderBundle]=bundle=data==null?null:AssetBundle.LoadFromMemory(data.bytes);}
    var shader=bundle?.LoadAllAssets<Shader>().FirstOrDefault(s=>s.name==node.shaderName);if(shader==null || !shader.isSupported)continue;
    var renderer=renderers.FirstOrDefault(r=>r.name==node.name);if(renderer==null)continue;var material=new Material(renderer.sharedMaterial){shader=shader};copies.Add(material);renderer.sharedMaterial=material;UsesApkShader=true;
   }
#endif
   var descriptor=Resources.Load<TextAsset>("Majsoul/NativeReview/"+name+"-source");
   if(descriptor!=null){var sourceUv=JsonUtility.FromJson<NativeEffectSource>(descriptor.text);foreach(var node in sourceUv.nodes.Where(n=>n.uvScroll!=null&&n.uvScroll.Length==2&&(n.uvScroll[0]!=0||n.uvScroll[1]!=0))){
    var renderer=clone.GetComponentsInChildren<ParticleSystemRenderer>(true).FirstOrDefault(r=>r.name==node.name);if(renderer==null)continue;var material=new Material(renderer.sharedMaterial);copies.Add(material);renderer.sharedMaterial=material;scrolls.Add(new Scroll{material=material,origin=material.mainTextureOffset,speed=new Vector2(node.uvScroll[0],node.uvScroll[1])});
   }}
   Sample(-1);
  }
  public void Sample(float age){
   foreach(var scroll in scrolls)scroll.material.mainTextureOffset=scroll.origin+scroll.speed*Mathf.Max(0,age);
   if(age<0){foreach(var p in particles)p.Clear(false);previous=-1;return;}
   bool restart=previous<0 || age<previous;float dt=restart?age:age-previous;
   foreach(var p in particles)p.Simulate(dt,false,restart,false);previous=age;
  }
  void OnDestroy(){foreach(var m in copies)Destroy(m);}
 }
}
