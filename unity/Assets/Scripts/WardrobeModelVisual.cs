using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace YunJian.Riichi {
    // One owner per live model: table rebuilds release meshes and materials.
    public sealed class WardrobeModelVisual:MonoBehaviour {
        [Serializable] class Group {public int[] indices;}
        [Serializable] class Geometry {public float[] vertices,uv;public Group[] groups;}
        [Serializable] class Surface {public string texture;public bool transparent;}
        [Serializable] class Node {public string name;public int parent;public float[] position,rotation,scale;public Geometry mesh;public Surface[] materials;}
        [Serializable] class Model {public Node[] nodes;}
        readonly List<Mesh> meshes=new List<Mesh>();readonly List<Material> materials=new List<Material>();
        static Vector3 V(float[] a)=>new Vector3(a[0],a[1],a[2]);
        public void Initialize(WardrobeItem item,float length){
            var data=JsonUtility.FromJson<Model>(Resources.Load<TextAsset>(item.model).text);
            var content=new GameObject(item.id).transform;content.SetParent(transform,false);var nodes=new Transform[data.nodes.Length];
            for(int i=0;i<nodes.Length;i++){
                var n=data.nodes[i];var t=nodes[i]=new GameObject(n.name).transform;t.SetParent(n.parent<0?content:nodes[n.parent],false);t.localPosition=V(n.position);t.localRotation=new Quaternion(n.rotation[0],n.rotation[1],n.rotation[2],n.rotation[3]);t.localScale=V(n.scale);
                if(n.mesh==null||n.materials==null||n.materials.Length==0)continue;
                if(n.name.ToLowerInvariant().Contains("shadow")){t.gameObject.SetActive(false);continue;}
                var d=n.mesh;var v=new Vector3[d.vertices.Length/3];var uv=new Vector2[v.Length];for(int j=0;j<v.Length;j++){v[j]=new Vector3(d.vertices[3*j],d.vertices[3*j+1],d.vertices[3*j+2]);uv[j]=new Vector2(d.uv[j*2],d.uv[j*2+1]);}
                var mesh=new Mesh{name=n.name,vertices=v,uv=uv,subMeshCount=d.groups.Length};for(int j=0;j<d.groups.Length;j++)mesh.SetTriangles(d.groups[j].indices,j);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
                t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;
                renderer.sharedMaterials=n.materials.Select(s=>{var m=new Material(Shader.Find(s.transparent?"Unlit/Transparent":"Unlit/Texture")){mainTexture=WardrobeCatalog.Texture(s.texture)};materials.Add(m);return m;}).ToArray();
            }
            // Original models use different long axes; a deposited stick lies across X.
            var bounds=Bounds(content);var size=bounds.size;if(size.y>size.x&&size.y>size.z)content.localRotation=Quaternion.Euler(0,0,90);else if(size.z>size.x)content.localRotation=Quaternion.Euler(0,90,0);
            bounds=Bounds(content);float scale=length/Mathf.Max(.001f,bounds.size.x);content.localScale*=scale;bounds=Bounds(content);content.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        }
        static Bounds Bounds(Transform root){var r=root.GetComponentsInChildren<MeshRenderer>();var b=r.Length>0?r[0].bounds:new Bounds(Vector3.zero,Vector3.one);foreach(var x in r.Skip(1))b.Encapsulate(x.bounds);return b;}
        void OnDestroy(){foreach(var m in meshes)Destroy(m);foreach(var m in materials)Destroy(m);}
    }
}
