using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi.Editor {
 public static class ImportNativeReview {
  [Serializable] class MeshData {public float[] vertices,uv;public int[] indices;}
  [Serializable] class BurstCurve {public int minMaxState;public float scalar;}
  [Serializable] class SourceBurst {public BurstCurve countCurve;}
  [Serializable] class SourceEmission {public SourceBurst[] m_Bursts;}
  [Serializable] class ParticleBody {public float lengthInSec;public bool looping;public SourceEmission EmissionModule;}
  [Serializable] class ParticleEnvelope {public ParticleBody ParticleSystem;}
  static Vector3 V(float[] v)=>new Vector3(v[0],v[1],v[2]);
  public static void Run(){
   AssetDatabase.Refresh();
   foreach(string name in new[]{"effect_yiman","effect_yiman2","effect_chipeng"})Import("Assets/Resources/Majsoul/NativeReview/",name);
   AssetDatabase.SaveAssets();Debug.Log("NATIVE APK MODULE IMPORT PASS");
  }
  // scene_mj/room/sun_shine exported by scripts/import-table-look.py.
  public static void RunTableLook(){
   AssetDatabase.Refresh();Import("Assets/Resources/Majsoul/TableLook/","sun_shine");
   AssetDatabase.SaveAssets();Debug.Log("TABLE LOOK MODULE IMPORT PASS");
  }
  public static void RunYakumanIndicators(){
   AssetDatabase.Refresh();
   foreach(string name in new[]{"effect_ui_yimanjihui_chs","effect_ui_yimanqueding_chs"})Import("Assets/Resources/Majsoul/NativeReview/",name);
   AssetDatabase.SaveAssets();Debug.Log("YAKUMAN INDICATOR APK MODULE IMPORT COMPLETE");
  }
  public static void RunDoubleYakuman(){AssetDatabase.Refresh();Import("Assets/Resources/Majsoul/NativeReview/","effect_yiman2");AssetDatabase.SaveAssets();Debug.Log("DOUBLE YAKUMAN PARTICLES IMPORTED");}
  public static void ImportDecoration(string name){Import("Assets/Resources/Majsoul/NativeReview/",name);}
  static void Import(string folder,string name){
   {
    var config=JsonUtility.FromJson<NativeEffectSource>(File.ReadAllText(folder+name+"-source.json"));var root=new GameObject(name);var nodes=new Transform[config.nodes.Length];
    for(int i=0;i<nodes.Length;i++){
     var n=config.nodes[i];nodes[i]=new GameObject(n.name).transform;nodes[i].SetParent(n.parent<0?root.transform:nodes[n.parent],false);nodes[i].localPosition=V(n.position);nodes[i].localRotation=new Quaternion(n.rotation[0],n.rotation[1],n.rotation[2],n.rotation[3]);nodes[i].localScale=V(n.scale);
     if(string.IsNullOrEmpty(n.particle))continue;
     var p=nodes[i].gameObject.AddComponent<ParticleSystem>();p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);string data=File.ReadAllText(folder+n.particle);EditorJsonUtility.FromJsonOverwrite(data,p);var expected=JsonUtility.FromJson<ParticleEnvelope>(data).ParticleSystem;if(Mathf.Abs(p.main.duration-expected.lengthInSec)>.0001f||p.main.loop!=expected.looping)throw new Exception("Source particle modules were not imported: "+n.name);
     if(name.StartsWith("effect_ui_yiman")){
      var bursts=expected.EmissionModule.m_Bursts;if(p.emission.burstCount!=bursts.Length)throw new Exception("Original burst count was not imported: "+n.name);
      for(int j=0;j<bursts.Length;j++)if(bursts[j].countCurve.minMaxState==0&&Mathf.Abs(p.emission.GetBurst(j).count.constant-bursts[j].countCurve.scalar)>.001f)throw new Exception("Original emission amount was not imported: "+n.name);
     }
     var renderer=p.GetComponent<ParticleSystemRenderer>();EditorJsonUtility.FromJsonOverwrite(File.ReadAllText(folder+n.renderer),renderer);
     if((int)renderer.renderMode!=n.renderMode)throw new Exception("Source renderer mode was not imported: "+n.name);
     var material=new Material(Shader.Find("YunJian/WinParticle")){name="APK "+n.name,mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+n.texture+".png")};material.SetColor("_TintColor",new Color(n.tint[0],n.tint[1],n.tint[2],n.tint[3]));material.SetFloat("_SrcBlend",n.srcBlend);material.SetFloat("_DstBlend",n.dstBlend);
     if(n.textureScale!=null)material.mainTextureScale=new Vector2(n.textureScale[0],n.textureScale[1]);if(n.textureOffset!=null)material.mainTextureOffset=new Vector2(n.textureOffset[0],n.textureOffset[1]);
     string matPath=folder+name+"-"+i+".mat";if(AssetDatabase.LoadAssetAtPath<Material>(matPath)!=null)AssetDatabase.DeleteAsset(matPath);AssetDatabase.CreateAsset(material,matPath);renderer.sharedMaterial=material;
     if(!string.IsNullOrEmpty(n.mesh)){
      var d=JsonUtility.FromJson<MeshData>(File.ReadAllText(folder+n.mesh));var mesh=new Mesh{name="APK "+n.name};var vertices=new Vector3[d.vertices.Length/3];var uv=new Vector2[d.uv.Length/2];for(int j=0;j<vertices.Length;j++)vertices[j]=new Vector3(d.vertices[j*3],d.vertices[j*3+1],d.vertices[j*3+2]);for(int j=0;j<uv.Length;j++)uv[j]=new Vector2(d.uv[j*2],d.uv[j*2+1]);mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=d.indices;mesh.RecalculateBounds();string meshPath=folder+name+"-"+i+".asset";if(AssetDatabase.LoadAssetAtPath<Mesh>(meshPath)!=null)AssetDatabase.DeleteAsset(meshPath);AssetDatabase.CreateAsset(mesh,meshPath);renderer.mesh=mesh;
     }
    }
    PrefabUtility.SaveAsPrefabAsset(root,folder+name+".prefab");UnityEngine.Object.DestroyImmediate(root);
   }
  }
 }
}
