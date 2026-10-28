using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace YunJian.Riichi {
 [Serializable] public class NativeEffectNode {
  public string name,particle,renderer,texture,mesh,shaderBundle,shaderName;public int parent,renderMode;public float[] position,rotation,scale,tint,textureScale,textureOffset,uvScroll;public float srcBlend,dstBlend,sortingFudge,lengthScale;
  public NativeMaterialSource[] materials;public string[] meshes,animations;public bool staticMesh,active;public int sortingOrder;
 }
 [Serializable] public class NativeEffectSource {public NativeEffectNode[] nodes;public bool preserveActiveState;}
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
  NativeEffectMotion[] motions;
  Transform authoredSpace;NativeEffectCamera sourceCamera;
  void SyncSpace(){if(authoredSpace!=null){authoredSpace.position=transform.position/TableTileGeometry.SourceUnits;authoredSpace.rotation=transform.rotation;authoredSpace.localScale=Vector3.one;var parent=transform.lossyScale;authoredSpace.localScale=new Vector3(1/parent.x,1/parent.y,1/parent.z);}}
  public void SetOverlay(bool enabled){
   foreach(var renderer in GetComponentsInChildren<ParticleSystemRenderer>(true)){
    var materials=renderer.sharedMaterials;
    for(int i=0;i<materials.Length;i++){
     if(!copies.Contains(materials[i])){materials[i]=new Material(materials[i]);copies.Add(materials[i]);}
     materials[i].SetFloat("_ZTest",enabled?8:4);materials[i].renderQueue=enabled?3100:3000;
    }
    renderer.sharedMaterials=materials;
   }
  }
  public void SetTileReadability(Transform tile,float intensity,float faceOpacity,bool enabled){CalloutReadability.Apply(copies,tile,intensity,faceOpacity,enabled);}
  public static Quaternion SurfaceMount(string name){
   var prefab=Resources.Load<GameObject>("Majsoul/NativeReview/"+name);if(prefab==null)throw new InvalidOperationException("Missing imported APK prefab "+name);
   var root=prefab.transform.childCount>0?prefab.transform.GetChild(0):prefab.transform;return Quaternion.FromToRotation(root.localRotation*Vector3.forward,Vector3.up);
  }
  public void Initialize(string name,uint seed=507,bool useApkShader=false,bool normalizeSurfaceOrigin=false){
   var prefab=Resources.Load<GameObject>("Majsoul/NativeReview/"+name);if(prefab==null)throw new InvalidOperationException("Missing imported APK prefab "+name);
   var clone=Instantiate(prefab,transform,false);particles=clone.GetComponentsInChildren<ParticleSystem>(true);
   var table=GetComponentInParent<TableRenderer>();if(name.StartsWith("EffectsR11/")&&table!=null){authoredSpace=clone.transform;SyncSpace();foreach(var child in clone.GetComponentsInChildren<Transform>(true))child.gameObject.layer=NativeEffectCamera.Layer;sourceCamera=NativeEffectCamera.Ensure(table);sourceCamera.Register(this);}
   motions=clone.GetComponentsInChildren<NativeEffectMotion>(true);
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
   if(descriptor!=null&&!name.StartsWith("EffectsR11/")){var sourceUv=JsonUtility.FromJson<NativeEffectSource>(descriptor.text);foreach(var node in sourceUv.nodes.Where(n=>n.uvScroll!=null&&n.uvScroll.Length==2&&(n.uvScroll[0]!=0||n.uvScroll[1]!=0))){
    var renderer=clone.GetComponentsInChildren<ParticleSystemRenderer>(true).FirstOrDefault(r=>r.name==node.name);if(renderer==null)continue;var material=new Material(renderer.sharedMaterial);copies.Add(material);renderer.sharedMaterial=material;scrolls.Add(new Scroll{material=material,origin=material.mainTextureOffset,speed=new Vector2(node.uvScroll[0],node.uvScroll[1])});
   }}
   Sample(-1);
  }
  public void Sample(float age){
   SyncSpace();
   foreach(var scroll in scrolls)scroll.material.mainTextureOffset=scroll.origin+scroll.speed*Mathf.Max(0,age);
   if(age<0){foreach(var p in particles)p.Clear(false);previous=-1;return;}
   if(motions!=null&&motions.Length>0){
    if(previous<0||age<previous){foreach(var motion in motions)motion.Sample(0);foreach(var p in particles){p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);p.Simulate(0,false,true,false);}previous=0;}
    while(previous<age-.000001f){float next=Mathf.Min(age,previous+1f/60);foreach(var motion in motions)motion.Sample(next);foreach(var p in particles)p.Simulate(next-previous,false,false,false);previous=next;}
    foreach(var motion in motions)motion.Sample(age);return;
   }
   bool restart=previous<0 || age<previous;float dt=restart?age:age-previous;
   foreach(var p in particles)p.Simulate(dt,false,restart,false);previous=age;
  }
  void OnDestroy(){if(sourceCamera!=null)sourceCamera.Remove(this);foreach(var m in copies)Destroy(m);}
 }
}
