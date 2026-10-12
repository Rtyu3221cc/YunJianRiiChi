using System;
using UnityEngine;
namespace YunJian.Riichi {
 // The bitmap, tile-face quad, UVs and texture tiling are native APK assets.
 [DefaultExecutionOrder(100)] public sealed class NativeDoraShine:MonoBehaviour {
  [Serializable] class Geometry {public float[] vertices,uv,position,scale;public int[] indices;}
  static Mesh tableMesh;static Mesh handMesh;Material material;Renderer sourceFace;MaterialPropertyBlock sourceBlock;
  public static float? ValidationAge;
  public const float Brightness=.85f;
  public void Initialize(bool hand){
   if(hand){sourceFace=transform.parent.GetComponent<Renderer>();sourceBlock=new MaterialPropertyBlock();}
   if(tableMesh==null){
    var d=JsonUtility.FromJson<Geometry>(Resources.Load<TextAsset>("Majsoul/NativeReview/dora-geometry").text);var vertices=new Vector3[d.vertices.Length/3];var uv=new Vector2[vertices.Length];
    for(int i=0;i<vertices.Length;i++){float x=d.vertices[i*3]*d.scale[0]+d.position[0],y=d.vertices[i*3+1]*d.scale[1]+d.position[1],z=d.vertices[i*3+2]*d.scale[2]+d.position[2];vertices[i]=new Vector3(-x,z+.01913448f/2,y)/.02947767f;uv[i]=new Vector2(d.uv[i*2],d.uv[i*2+1]);}
    tableMesh=new Mesh{name="APK dora quad",vertices=vertices,uv=uv,triangles=d.indices};tableMesh.RecalculateBounds();
    handMesh=new Mesh{name="Gameplay hand shine surface",vertices=new[]{new Vector3(-OriginalTiles.HandWidth/2,-OriginalTiles.HandHeight/2,-.012f),new Vector3(-OriginalTiles.HandWidth/2,OriginalTiles.HandHeight/2,-.012f),new Vector3(OriginalTiles.HandWidth/2,OriginalTiles.HandHeight/2,-.012f),new Vector3(OriginalTiles.HandWidth/2,-OriginalTiles.HandHeight/2,-.012f)},uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right},triangles=new[]{0,1,2,0,2,3}};handMesh.RecalculateBounds();
   }
   gameObject.AddComponent<MeshFilter>().sharedMesh=hand?handMesh:tableMesh;
   material=new Material(Shader.Find("YunJian/WinParticle")){mainTexture=Resources.Load<Texture2D>("Majsoul/NativeReview/Textures/"+(hand?"dora_shine1":"dora_shine")),mainTextureScale=new Vector2(.8f,.2f),renderQueue=3001};material.SetColor("_TintColor",Color.white);material.SetFloat("_DstBlend",1);var r=gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
   Apply();
  }
  public void Apply(){if(material==null)return;material.mainTextureOffset=new Vector2(0,Mathf.Repeat((ValidationAge??Time.unscaledTime)*.8f,1));float opacity=1,dim=0;if(sourceFace!=null){sourceFace.GetPropertyBlock(sourceBlock);opacity=sourceBlock.HasFloat(Shader.PropertyToID("_Opacity"))?sourceBlock.GetFloat("_Opacity"):1;dim=sourceBlock.GetFloat("_Dim");}float light=Brightness*(1-dim*.5f);material.SetColor("_TintColor",new Color(light,light,light,opacity));}
  void LateUpdate()=>Apply();
  void OnDestroy(){if(material!=null)Destroy(material);}
 }
}
