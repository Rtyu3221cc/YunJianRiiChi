using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace YunJian.Riichi {
 [Serializable] public class NativeEffectNode {
  public string name,particle,renderer,texture,mesh,shaderBundle,shaderName;public int parent,renderMode;public float[] position,rotation,scale,tint,textureScale,textureOffset,uvScroll;public float srcBlend,dstBlend,sortingFudge,lengthScale;
  public NativeMaterialSource[] materials;public string[] meshes,animations;public bool staticMesh,active,skinnedMesh;public int sortingOrder,rootBone;public int[] bones;
  public string spineSkeleton,spineAtlas,spineAnimation;public string[] spineTextures;public float spineScale,spineSpeed;public bool spineLoop;
 }
 [Serializable] public class NativeExtraMaterial {public string name,source,asset;}
 [Serializable] public class NativeEffectSource {public NativeEffectNode[] nodes;public bool preserveActiveState;public NativeExtraMaterial[] extraMaterials;}
 // Components in these prefabs are imported from whole APK serialized modules.
 public sealed class NativeParticleEffect:MonoBehaviour {
  ParticleSystem[] particles;float previous=-1;readonly List<Material> copies=new List<Material>();
  sealed class Scroll {public Material material;public Vector2 origin,speed;}
  readonly List<Scroll> scrolls=new List<Scroll>();
  readonly List<Mesh> repairedSkins=new List<Mesh>();
  static readonly Dictionary<string,AssetBundle> shaders=new Dictionary<string,AssetBundle>();
  public int LayerCount=>particles?.Length??0;
  public int ParticleCount=>particles?.Sum(p=>p.particleCount)??0;
  public bool UsesApkShader {get;private set;}
  public Quaternion SurfaceMountRotation {get;private set;}=Quaternion.identity;
  NativeEffectMotion[] motions;
  NativeSpineEffect[] skeletons;
  Transform authoredSpace,authoredRoot;NativeEffectCamera sourceCamera;
  NativeWinContract winContract;public NativeWinPlayback Playback {get;private set;}
  string sourceName;
  Transform plumGround;
  readonly Dictionary<ParticleSystem,bool> activation=new Dictionary<ParticleSystem,bool>();
  bool luaNewWin;Transform rootMove,rootRot,rootAni,rootAniTsumo,rootStatic;
  public bool UsesNewHuleBinding=>luaNewWin;
  void SyncSpace(){if(authoredSpace!=null){authoredSpace.position=luaNewWin||winContract?.newStyle==true?Vector3.zero:transform.position/TableTileGeometry.SourceUnits;authoredSpace.rotation=luaNewWin?Quaternion.Euler(0,180,0):transform.rotation;authoredSpace.localScale=Vector3.one;var parent=transform.lossyScale;authoredSpace.localScale=new Vector3(1/parent.x,1/parent.y,1/parent.z);}}
  public void SynchronizeSourceSpace(){SyncSpace();}
  // DesktopMgr.setNewHuleEffect: scene origin and winning-tile position are
  // independent. The port's table faces the opposite world-Z direction.
  void BindLuaRoots(){
   if(!luaNewWin)return;var point=transform.position/TableTileGeometry.SourceUnits;
   if(rootMove!=null)rootMove.position=point;
   if(rootRot!=null){rootRot.position=point;rootRot.rotation=transform.rotation;}
   if(rootAni!=null)rootAni.position=point;
   if(rootAniTsumo!=null)rootAniTsumo.position=point;
   if(rootStatic!=null)rootStatic.position=Vector3.zero;
  }
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
  public void BindLegacyRiichiRoot(bool resetRotation){if(authoredRoot==null)return;authoredRoot.localPosition=Vector3.zero;if(resetRotation)authoredRoot.localRotation=Quaternion.Euler(0,0,-180);Sample(-1);}
  public void BindLegacyRiichiSeat(int seat){
   if(!sourceName.StartsWith("WardrobeR30/"))return;
   void Euler(string path,float x,float y,float z){var node=authoredRoot.Find(path);if(node!=null)node.localRotation=Quaternion.Euler(x,y,z);}
   float angle=seat*90;
   switch(sourceName.Substring("WardrobeR30/".Length)){
    case "effect_liqi_xiyuansi":Euler("buzhuan",0,0,seat==3?-90:angle);break;
    case "effect_liqi_nanfenghua":Euler("fx_liqi_073/buzhuan",0,angle,0);break;
    case "effect_liqi_beiyuan":Euler("zhengdui",angle,-90,-90);break;
    case "effect_liqi_22summer":
     for(int i=1;i<=3;i++){var node=authoredRoot.Find("guangyun/ya/root"+i);if(node==null)continue;node.localRotation=Quaternion.Euler(0,0,angle);if(seat==2)node.localScale=new Vector3(1,-1,1);}break;
    case "effect_liqi_22chunjie":plumGround=authoredRoot.Find("hongdi");break;
    case "effect_liqi_2211saki":authoredRoot.localRotation=Quaternion.Euler(-180,-90,270);Euler("zhuan",270,0,angle);break;
    case "effect_liqi_23rpg":Euler("root",angle,-90,-90);break;
    case "effect_liqi_yly":authoredRoot.localRotation=Quaternion.Euler(-90,0,-180);Euler("fx_skill_062_liqi",45,angle+90,0);break;
    case "effect_liqi_24summer":authoredRoot.localRotation=Quaternion.Euler(0,0,angle-180);break;
   }
  }
  // Keep the callable marker plane in the tile-model frame. Source prefabs
  // also retain a standalone authoring tilt; do not stack it on the mount.
  public void BindCallableTileRoot(){if(authoredRoot==null)return;authoredRoot.localPosition=Vector3.zero;authoredRoot.localRotation=Quaternion.identity;Sample(-1);}
  public static Quaternion SurfaceMount(string name){
   var prefab=WardrobePackService.Asset<GameObject>("Majsoul/NativeReview/"+name);if(prefab==null)throw new InvalidOperationException("Missing imported APK prefab "+name);
   var root=prefab.transform.childCount>0?prefab.transform.GetChild(0):prefab.transform;return Quaternion.FromToRotation(root.localRotation*Vector3.forward,Vector3.up);
  }
  void RestoreSourceMaterials(GameObject clone,NativeEffectSource descriptor){
   var shader=Resources.Load<Shader>("Majsoul/NativeReview/WinsR22/WardrobeEffectParticle");
   var nodes=clone.GetComponentsInChildren<Transform>(true).Skip(1).ToArray();
   for(int i=0;i<Mathf.Min(nodes.Length,descriptor.nodes.Length);i++){
    var source=descriptor.nodes[i].materials;
    var renderer=nodes[i].GetComponent<Renderer>();
    if(source==null||renderer==null)continue;
    var materials=renderer.sharedMaterials;
    bool changed=false;
    for(int j=0;j<Mathf.Min(source.Length,materials.Length);j++){
     if(materials[j]==null)continue;
     var original=source[j];
     float Field(string key,float fallback)=>original.floats?.FirstOrDefault(f=>f.name==key)?.value??fallback;
     var material=new Material(materials[j]);
     material.shader=shader;
     if(original.shaderName=="FizzFX/Standard"){
      foreach(var f in original.floats??Array.Empty<NativeNamedFloat>())if(material.HasProperty(f.name))material.SetFloat(f.name,f.value);
      foreach(var c in original.colors??Array.Empty<NativeNamedColor>())if(material.HasProperty(c.name))material.SetVector(c.name,new Vector4(c.value[0],c.value[1],c.value[2],c.value[3]));
      foreach(var t in original.extraTextures??Array.Empty<NativeNamedTexture>())if(material.HasProperty(t.name)&&material.GetTexture(t.name)==null){var tex=WardrobePackService.Asset<Texture2D>("Majsoul/NativeReview/"+t.texture);if(tex!=null)material.SetTexture(t.name,tex);}
      material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",Field("_BlendMode",10));
      material.SetFloat("_ZTest",Field("_ZTestMode",4));material.SetFloat("_SourceZWrite",Field("_ZWriteMode",0));material.SetFloat("_SourceCull",Field("_CullMode",0));
      material.SetFloat("_StandardDissolve",(original.keywords??Array.Empty<string>()).Contains("_DISSOLVEENABLE_ON")?1:0);
      if(renderer is ParticleSystemRenderer particle){
       var streams=new List<ParticleSystemVertexStream>();particle.GetActiveVertexStreams(streams);
       material.SetFloat("_CustomStreams",streams.Contains(ParticleSystemVertexStream.Custom2XYZW)?1:0);
      }
     }else if(original.shaderName.StartsWith("Legacy Shaders/Particles/")||original.shaderName=="Mobile/Particles/Additive"){
      // Literal render states must never be read from an unnamed material slot.
      material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",original.shaderName.EndsWith("Additive")?1:10);
      material.SetFloat("_ZTest",4);material.SetFloat("_SourceZWrite",0);material.SetFloat("_SourceCull",0);
     }
     copies.Add(material);materials[j]=material;changed=true;
    }
    if(changed)renderer.sharedMaterials=materials;
   }
  }
  void RestoreCompressedSkin(GameObject clone){
   foreach(var skin in clone.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
    var mesh=skin.sharedMesh;if(mesh==null)continue;
    var weights=mesh.boneWeights;bool changed=false;
    for(int i=0;i<weights.Length;i++){
     var weight=weights[i];
     // Compressed skin stores weights in 31 steps. Earlier exports used the
     // integer sum to calculate the implicit fourth weight in normalized units.
     float sum=weight.weight0+weight.weight1+weight.weight2;
     if(weight.weight3<0&&sum>=0&&sum<1&&Mathf.Abs(weight.weight3-(1-Mathf.Round(sum*31)))<.001f){
      weight.weight3=1-sum;weights[i]=weight;changed=true;
     }
    }
    if(!changed)continue;
    var repaired=Instantiate(mesh);repaired.boneWeights=weights;
    repairedSkins.Add(repaired);skin.sharedMesh=repaired;
   }
  }
  public void Initialize(string name,uint seed=507,bool useApkShader=false,bool normalizeSurfaceOrigin=false,NativeWinContract contract=null){
   winContract=contract;sourceName=name;
   WardrobePackService.Lease("Majsoul/NativeReview/"+name,gameObject);var prefab=WardrobePackService.Asset<GameObject>("Majsoul/NativeReview/"+name);if(prefab==null)throw new InvalidOperationException("Missing imported APK prefab "+name);
   var clone=Instantiate(prefab,transform,false);particles=clone.GetComponentsInChildren<ParticleSystem>(true);
   if(name=="WardrobeR30/effect_liqi_nanfenghua")RestoreCompressedSkin(clone);
   luaNewWin=name.StartsWith("FateR19/");
   var table=GetComponentInParent<TableRenderer>();if((name.StartsWith("EffectsR11/")||name.StartsWith("WinsR22/")||name.StartsWith("WardrobeR30/effect_liqi_")||contract!=null||luaNewWin)&&table!=null){authoredSpace=clone.transform;SyncSpace();foreach(var child in clone.GetComponentsInChildren<Transform>(true))child.gameObject.layer=NativeEffectCamera.Layer;sourceCamera=NativeEffectCamera.Ensure(table);sourceCamera.Register(this);}
   motions=clone.GetComponentsInChildren<NativeEffectMotion>(true);
   skeletons=clone.GetComponentsInChildren<NativeSpineEffect>(true);
   // Imported prefabs have a neutral wrapper and an authored root. Some roots
   // already face the cloth; others retain the tile's original XY model plane.
   var authored=authoredRoot=clone.transform.childCount>0?clone.transform.GetChild(0):clone.transform;
   if(contract!=null){authored.localPosition=Vector3.zero;Playback=gameObject.AddComponent<NativeWinPlayback>();Playback.Initialize(this,authored,clone.transform,contract);}
   if(luaNewWin){rootMove=authored.Find("root_move");rootRot=authored.Find("root_rot");rootAni=authored.Find("root_ani");rootAniTsumo=authored.Find("root_ani_zimo");rootStatic=authored.Find("root_static");BindLuaRoots();}
   if(normalizeSurfaceOrigin)authored.localPosition=Vector3.zero;
   SurfaceMountRotation=Quaternion.FromToRotation(authored.localRotation*Vector3.forward,Vector3.up);
   for(int i=0;i<particles.Length;i++){particles[i].Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);particles[i].useAutoRandomSeed=false;particles[i].randomSeed=seed+(uint)i+1;}
#if UNITY_ANDROID && !UNITY_EDITOR
   var source=JsonUtility.FromJson<NativeEffectSource>(WardrobePackService.Asset<TextAsset>("Majsoul/NativeReview/"+name+"-source").text);var renderers=clone.GetComponentsInChildren<ParticleSystemRenderer>(true);
   foreach(var node in source.nodes.Where(n=>useApkShader&&!string.IsNullOrEmpty(n.shaderBundle))){
    if(!shaders.TryGetValue(node.shaderBundle,out var bundle)){var data=WardrobePackService.Asset<TextAsset>("Majsoul/NativeReview/"+node.shaderBundle);shaders[node.shaderBundle]=bundle=data==null?null:AssetBundle.LoadFromMemory(data.bytes);}
    var shader=bundle?.LoadAllAssets<Shader>().FirstOrDefault(s=>s.name==node.shaderName);if(shader==null || !shader.isSupported)continue;
    var renderer=renderers.FirstOrDefault(r=>r.name==node.name);if(renderer==null)continue;var material=new Material(renderer.sharedMaterial){shader=shader};copies.Add(material);renderer.sharedMaterial=material;UsesApkShader=true;
   }
#endif
   var descriptor=WardrobePackService.Asset<TextAsset>("Majsoul/NativeReview/"+name+"-source");
   if(descriptor!=null&&name.StartsWith("WardrobeR30/"))RestoreSourceMaterials(clone,JsonUtility.FromJson<NativeEffectSource>(descriptor.text));
   if(descriptor!=null&&!name.StartsWith("EffectsR11/")&&!name.StartsWith("WinsR22/")&&!name.StartsWith("WardrobeR30/")&&!luaNewWin){var sourceUv=JsonUtility.FromJson<NativeEffectSource>(descriptor.text);foreach(var node in sourceUv.nodes.Where(n=>n.uvScroll!=null&&n.uvScroll.Length==2&&(n.uvScroll[0]!=0||n.uvScroll[1]!=0))){
    var renderer=clone.GetComponentsInChildren<ParticleSystemRenderer>(true).FirstOrDefault(r=>r.name==node.name);if(renderer==null)continue;var material=new Material(renderer.sharedMaterial);copies.Add(material);renderer.sharedMaterial=material;scrolls.Add(new Scroll{material=material,origin=material.mainTextureOffset,speed=new Vector2(node.uvScroll[0],node.uvScroll[1])});
   }}
   Sample(-1);
  }
  public void Sample(float age){
   SyncSpace();BindLuaRoots();
   if(plumGround!=null){plumGround.position=new Vector3(-.01f,.2f,.3f);plumGround.rotation=Quaternion.Euler(-60,0,0);}
   foreach(var scroll in scrolls)scroll.material.mainTextureOffset=scroll.origin+scroll.speed*Mathf.Max(0,age);
   if(age<0){Playback?.AfterPose(age);foreach(var p in particles)p.Clear(false);foreach(var s in skeletons)s.Sample(-1);activation.Clear();previous=-1;return;}
   if(motions!=null&&motions.Length>0){
    if(previous<0||age<previous){foreach(var motion in motions)motion.Sample(0);BindLuaRoots();Playback?.AfterPose(0);activation.Clear();foreach(var p in particles){p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);p.Simulate(0,false,true,false);}previous=0;}
    while(previous<age-.000001f){float next=Mathf.Min(age,previous+1f/60);foreach(var motion in motions)motion.Sample(Playback?.SourceAge(next)??next);BindLuaRoots();Playback?.AfterPose(next);foreach(var p in particles)Simulate(p,next-previous,false);previous=next;}
    foreach(var motion in motions)motion.Sample(Playback?.SourceAge(age)??age);BindLuaRoots();Playback?.AfterPose(age);foreach(var s in skeletons)s.Sample(age);return;
   }
   bool restart=previous<0 || age<previous;float dt=restart?age:age-previous;
   Playback?.AfterPose(age);foreach(var p in particles)Simulate(p,dt,restart);foreach(var s in skeletons)s.Sample(age);previous=age;
  }
  void Simulate(ParticleSystem particle,float dt,bool restart){if(winContract==null){particle.Simulate(dt,false,restart,false);return;}bool active=particle.gameObject.activeInHierarchy,was=activation.TryGetValue(particle,out var value)&&value;if(!active){particle.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);activation[particle]=false;return;}particle.Simulate(dt,false,restart||!was,false);activation[particle]=true;}
  void OnDestroy(){if(sourceCamera!=null)sourceCamera.Remove(this);foreach(var m in copies)Destroy(m);foreach(var mesh in repairedSkins)Destroy(mesh);}
 }
}
