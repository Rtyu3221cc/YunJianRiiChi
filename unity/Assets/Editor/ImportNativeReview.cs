using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi.Editor {
 public static class ImportNativeReview {
  [Serializable] class MeshData {public float[] vertices,uv;public int[] indices;}
  [Serializable] class ParticleBody {public float lengthInSec;public bool looping;}
  [Serializable] class ParticleEnvelope {public ParticleBody ParticleSystem;}
  static Vector3 V(float[] v)=>new Vector3(v[0],v[1],v[2]);
  public static void Run(){
   const string folder="Assets/Resources/Majsoul/NativeReview/";AssetDatabase.Refresh();
   foreach(string name in new[]{"effect_yiman","effect_yiman2","effect_chipeng"}){
    var config=JsonUtility.FromJson<NativeEffectSource>(File.ReadAllText(folder+name+"-source.json"));var root=new GameObject(name);var nodes=new Transform[config.nodes.Length];
    for(int i=0;i<nodes.Length;i++){
     var n=config.nodes[i];nodes[i]=new GameObject(n.name).transform;nodes[i].SetParent(n.parent<0?root.transform:nodes[n.parent],false);nodes[i].localPosition=V(n.position);nodes[i].localRotation=new Quaternion(n.rotation[0],n.rotation[1],n.rotation[2],n.rotation[3]);nodes[i].localScale=V(n.scale);
     if(string.IsNullOrEmpty(n.particle))continue;
     var p=nodes[i].gameObject.AddComponent<ParticleSystem>();p.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);string data=File.ReadAllText(folder+n.particle);EditorJsonUtility.FromJsonOverwrite(data,p);var expected=JsonUtility.FromJson<ParticleEnvelope>(data).ParticleSystem;if(Mathf.Abs(p.main.duration-expected.lengthInSec)>.0001f||p.main.loop!=expected.looping)throw new Exception("Source particle modules were not imported: "+n.name);
     var renderer=p.GetComponent<ParticleSystemRenderer>();EditorJsonUtility.FromJsonOverwrite(File.ReadAllText(folder+n.renderer),renderer);
     if((int)renderer.renderMode!=n.renderMode)throw new Exception("Source renderer mode was not imported: "+n.name);
     var material=new Material(Shader.Find("YunJian/WinParticle")){name="APK "+n.name,mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+n.texture+".png")};material.SetColor("_TintColor",new Color(n.tint[0],n.tint[1],n.tint[2],n.tint[3]));material.SetFloat("_SrcBlend",n.srcBlend);material.SetFloat("_DstBlend",n.dstBlend);
     string matPath=folder+name+"-"+i+".mat";if(AssetDatabase.LoadAssetAtPath<Material>(matPath)!=null)AssetDatabase.DeleteAsset(matPath);AssetDatabase.CreateAsset(material,matPath);renderer.sharedMaterial=material;
     if(!string.IsNullOrEmpty(n.mesh)){
      var d=JsonUtility.FromJson<MeshData>(File.ReadAllText(folder+n.mesh));var mesh=new Mesh{name="APK "+n.name};var vertices=new Vector3[d.vertices.Length/3];var uv=new Vector2[d.uv.Length/2];for(int j=0;j<vertices.Length;j++)vertices[j]=new Vector3(d.vertices[j*3],d.vertices[j*3+1],d.vertices[j*3+2]);for(int j=0;j<uv.Length;j++)uv[j]=new Vector2(d.uv[j*2],d.uv[j*2+1]);mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=d.indices;mesh.RecalculateBounds();string meshPath=folder+name+"-"+i+".asset";if(AssetDatabase.LoadAssetAtPath<Mesh>(meshPath)!=null)AssetDatabase.DeleteAsset(meshPath);AssetDatabase.CreateAsset(mesh,meshPath);renderer.mesh=mesh;
     }
    }
    PrefabUtility.SaveAsPrefabAsset(root,folder+name+".prefab");UnityEngine.Object.DestroyImmediate(root);
   }
   AssetDatabase.SaveAssets();Debug.Log("NATIVE APK MODULE IMPORT PASS");
  }
 }
}
