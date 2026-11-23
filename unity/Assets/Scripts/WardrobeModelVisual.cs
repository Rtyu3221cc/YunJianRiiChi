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
        NativeEffectMotion[] motions;float began;
        static Vector3 V(float[] a)=>new Vector3(a[0],a[1],a[2]);
        public void Initialize(WardrobeItem item,float length){
            if(!string.IsNullOrEmpty(item.modelPrefab)){
                var prefab=WardrobePackService.Asset<GameObject>(item.modelPrefab);if(prefab==null)throw new InvalidOperationException("Missing wardrobe model "+item.id);WardrobePackService.Lease(item.modelPrefab,gameObject);
                var fit=new GameObject("Stick fit").transform;fit.SetParent(transform,false);
                var instance=Instantiate(prefab,fit,false).transform;
                RestoreModelShaders(instance);
                foreach(var r in instance.GetComponentsInChildren<MeshRenderer>(true))if(IsShadow(r))r.gameObject.SetActive(false);
                Normalize(fit,length);motions=instance.GetComponentsInChildren<NativeEffectMotion>(true);began=Time.unscaledTime;
                foreach(var ps in instance.GetComponentsInChildren<ParticleSystem>(true)){var main=ps.main;main.scalingMode=ParticleSystemScalingMode.Hierarchy;ps.Play(false);}
                return;
            }
            WardrobePackService.Lease(item.model,gameObject);var data=JsonUtility.FromJson<Model>(WardrobePackService.Asset<TextAsset>(item.model).text);
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
            Normalize(content,length);
        }
        void RestoreModelShaders(Transform instance){
            var shader=Resources.Load<Shader>("Majsoul/Wardrobe/StaticDecoration");
            foreach(var renderer in instance.GetComponentsInChildren<MeshRenderer>(true)){
                var surfaces=renderer.sharedMaterials;
                for(int i=0;i<surfaces.Length;i++){
                    if(surfaces[i]==null||surfaces[i].shader.name!="YunJian/StaticDecoration")continue;
                    // Published packs can carry an older embedded shader. Use
                    // the current app's implementation while preserving values.
                    var copy=new Material(surfaces[i]){shader=shader};copy.renderQueue=surfaces[i].renderQueue;materials.Add(copy);surfaces[i]=copy;
                }
                renderer.sharedMaterials=surfaces;
            }
        }
        // Some source nodes have their body/shadow names interchanged. Materials
        // identify the actual shadow; node names alone can hide the complete stick.
        public static bool IsShadow(MeshRenderer renderer){
            var surfaces=renderer.sharedMaterials;if(surfaces.Length==0)return false;
            if(surfaces.All(m=>m!=null&&m.name.IndexOf("shadow",StringComparison.OrdinalIgnoreCase)>=0))return true;
            // The original star wand names its transparent shadow "yinzi" and
            // its material "Material #73". It must not enlarge the fitting bounds.
            return renderer.name=="yinzi"&&surfaces.All(m=>m!=null&&m.shader.name=="YunJian/StaticDecoration");
        }
        static void Normalize(Transform content,float length){var bounds=Bounds(content);var size=bounds.size;if(size.y>size.x&&size.y>size.z)content.localRotation=Quaternion.Euler(0,0,90);else if(size.z>size.x)content.localRotation=Quaternion.Euler(0,90,0);bounds=Bounds(content);content.localScale*=length/Mathf.Max(.001f,bounds.size.x);bounds=Bounds(content);content.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);}
        static Bounds Bounds(Transform root){
            var filters=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh!=null&&f.GetComponent<MeshRenderer>()?.enabled==true).ToArray();
            var body=filters.Where(f=>f.GetComponent<MeshRenderer>().sharedMaterials.Any(m=>m!=null&&(m.renderQueue<3000||m.shader.name=="YunJian/StaticDecoration"||m.shader.name.StartsWith("Unlit/",StringComparison.Ordinal)))).ToArray();
            // Transparent glass is still body geometry. Particle/glow planes
            // must not shrink it, even if their bounds are several times wider.
            if(body.Length>0)filters=body;
            bool first=true,firstSolid=true;var bounds=new Bounds(Vector3.zero,Vector3.one);var solids=new Bounds();
            foreach(var filter in filters){
                var local=filter.sharedMesh.bounds;bool solid=filter.GetComponent<MeshRenderer>().sharedMaterials.Any(m=>m!=null&&m.renderQueue<3000);
                for(int i=0;i<8;i++){
                    var point=local.center+Vector3.Scale(local.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    point=root.parent.InverseTransformPoint(filter.transform.TransformPoint(point));
                    if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
                    if(solid){if(firstSolid){solids=new Bounds(point,Vector3.zero);firstSolid=false;}else solids.Encapsulate(point);}
                }
            }
            float all=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z)),opaque=Mathf.Max(solids.size.x,Mathf.Max(solids.size.y,solids.size.z));
            return firstSolid||opaque<all*.4f?bounds:solids;
        }
        public void Sample(float age){foreach(var motion in motions??new NativeEffectMotion[0])motion.Sample(age);}
        void Update()=>Sample(Time.unscaledTime-began);
        void OnDestroy(){foreach(var m in meshes)Destroy(m);foreach(var m in materials)Destroy(m);}
    }
}
