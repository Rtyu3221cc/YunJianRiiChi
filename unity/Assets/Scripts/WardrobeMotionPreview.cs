using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace YunJian.Riichi {
    public sealed class WardrobeMotionPreview:IDisposable {
        [Serializable] class Group {public int[] indices;}
        [Serializable] class MeshData {public float[] vertices,uv;public Group[] groups;}
        [Serializable] class MaterialData {public string texture;public bool transparent;}
        [Serializable] class Node {public string name;public int parent;public float[] position,rotation,scale;public MeshData mesh;public MaterialData[] materials;}
        [Serializable] class Model {public Node[] nodes;}
        [Serializable] class HandData {public int vertices,frames,fps;public float duration;public float[] uv;public int[] body,nails;}
        GameObject root;Camera camera;RenderTexture image;string key;NativeParticleEffect native;WinTileEffect effect;OriginalTiles previewTiles;HandData hand;Mesh handMesh;float[] handFrames;Vector3[] vertices,normals;
        readonly List<Material> materials=new List<Material>();readonly List<Mesh> meshes=new List<Mesh>();
        Material composite;
        public Texture Image=>image;
        public void Draw(Rect area){if(Event.current.type!=EventType.Repaint||image==null)return;if(composite==null)composite=new Material(Resources.Load<Shader>("Majsoul/UiParticleComposite"));composite.SetColor("_Tint",GUI.color);Graphics.DrawTexture(area,image,new Rect(0,0,1,1),0,0,0,0,Color.white,composite);}
#if UNITY_EDITOR
        public string Describe(){
            if(root==null)return "No preview";var s=new System.Text.StringBuilder();
            foreach(var p in root.GetComponentsInChildren<ParticleSystem>()){
                var particles=new ParticleSystem.Particle[p.particleCount];int count=p.GetParticles(particles);s.Append(p.name+" count="+count+" bounds="+p.GetComponent<ParticleSystemRenderer>().bounds+" local="+p.transform.localPosition+" world="+p.transform.position+" space="+p.main.simulationSpace+" size="+p.main.startSize.constant+" scale="+p.main.scalingMode);
                if(count>0)s.Append(" firstPos="+particles[0].position+" color="+particles[0].GetCurrentColor(p)+" size="+particles[0].GetCurrentSize(p));s.AppendLine();
            }
            return s.ToString();
        }
#endif
        static Vector3 V(float[] a)=>new Vector3(a[0],a[1],a[2]);
        public void Tick(WardrobeItem item,float age){
            if(item==null)return;if(key!=item.id){Build(item);key=item.id;}
            float sample=age%Mathf.Max(1,item.duration);
            if(hand!=null)HandFrame(Mathf.Min(hand.duration,Mathf.Max(0,sample-.15f)));
            effect?.Sample(sample);native?.Sample(sample);
            var active=RenderTexture.active;camera.Render();RenderTexture.active=active;
        }
        void Build(WardrobeItem item){
            Hide();root=new GameObject("Original decoration preview "+item.id);root.transform.position=new Vector3(600,600,600);
            camera=new GameObject("Decoration model camera").AddComponent<Camera>();camera.transform.SetParent(root.transform,false);camera.enabled=false;camera.orthographic=true;camera.nearClipPlane=.001f;camera.farClipPlane=200;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<27;camera.allowMSAA=true;
            image=new RenderTexture(1024,1024,24,RenderTextureFormat.ARGB32){name="Original animated decoration",antiAliasing=4};image.Create();camera.targetTexture=image;
            if(item.standardStick)BuildStandardStick();
            else if(!string.IsNullOrEmpty(item.model))BuildModel(item);
            else if(!string.IsNullOrEmpty(item.motion))BuildHand(item);
            else{
                var go=new GameObject(item.id);go.transform.SetParent(root.transform,false);
                if(!string.IsNullOrEmpty(item.effect)){
                    effect=go.AddComponent<WinTileEffect>();if(item.category=="winEffect")effect.Initialize(root.transform.position,0,0,507,item.effect,item.effectScale>0?item.effectScale:1);else effect.InitializeTable(item.effect,root.transform.position,Quaternion.identity,0,507);
                    camera.transform.position=root.transform.position+new Vector3(0,6,-8);camera.transform.LookAt(root.transform.position+Vector3.up*(item.category=="winEffect"?2:.7f));camera.orthographicSize=item.category=="winEffect"?6:2.0f;
                }else{
                    go.transform.localRotation=NativeParticleEffect.SurfaceMount(item.nativeEffect);go.transform.localScale=Vector3.one*(item.category=="callout"?TableTileGeometry.SourceUnits:1);native=go.AddComponent<NativeParticleEffect>();native.Initialize(item.nativeEffect,507,false,item.category=="callout");
                    go.transform.localScale=Vector3.one*(item.category=="callout"?TableTileGeometry.SourceUnits:1);
                    // The source root rotates these mesh particles onto XZ.
                    // Derive the view from the source plane, keeping Local scaling.
                    var sourcePlane=native.GetComponentsInChildren<ParticleSystem>().First().transform.forward;
                    camera.transform.position=root.transform.position+(Mathf.Abs(Vector3.Dot(sourcePlane,Vector3.up))>.8f?Vector3.up:-sourcePlane);
                    camera.transform.LookAt(root.transform.position,Mathf.Abs(Vector3.Dot(sourcePlane,Vector3.up))>.8f?Vector3.forward:Vector3.up);camera.orthographicSize=item.category=="callout"?.11f:4.2f;
                    if(item.category=="callout"){
                        // In the game this graph surrounds an existing tile. Use
                        // the same blank tile here instead of a black empty centre.
                        previewTiles=new OriginalTiles(WardrobeCatalog.Find("mjpface_default"),WardrobeCatalog.Find("mjp_default"));
                        var tile=previewTiles.CreateTable(root.transform,"z5",new Vector3(0,-.14f,0));tile.localScale=Vector3.one*.105f;
                    }
                }
            }
            foreach(var renderer in root.GetComponentsInChildren<ParticleSystemRenderer>(true)){var material=new Material(renderer.sharedMaterial){shader=Resources.Load<Shader>("Majsoul/UiParticleBuffer")};materials.Add(material);renderer.sharedMaterial=material;}
            foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=27;
        }
        Material Material(string path,bool transparent){var m=new Material(Shader.Find(transparent?"Unlit/Transparent":"Unlit/Texture")){mainTexture=WardrobeCatalog.Texture(path)};materials.Add(m);return m;}
        void BuildStandardStick(){
            foreach(bool dot in new[]{false,true}){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);UnityEngine.Object.Destroy(go.GetComponent<Collider>());go.transform.SetParent(root.transform,false);go.transform.localPosition=dot?new Vector3(0,.024f,0):Vector3.zero;go.transform.localScale=dot?new Vector3(.045f,.004f,.045f):new Vector3(.74f,.045f,.075f);var m=new Material(Shader.Find("Unlit/Color")){color=dot?new Color(.78f,.08f,.08f):new Color(.94f,.92f,.84f)};materials.Add(m);go.GetComponent<Renderer>().sharedMaterial=m;}
            camera.transform.position=root.transform.position+new Vector3(.3f,.7f,-1);camera.transform.LookAt(root.transform);camera.orthographicSize=.55f;
        }
        void BuildModel(WardrobeItem item){
            var data=JsonUtility.FromJson<Model>(Resources.Load<TextAsset>(item.model).text);var nodes=new Transform[data.nodes.Length];
            for(int i=0;i<nodes.Length;i++){
                var n=data.nodes[i];var t=nodes[i]=new GameObject(n.name).transform;t.SetParent(n.parent<0?root.transform:nodes[n.parent],false);t.localPosition=V(n.position);t.localRotation=new Quaternion(n.rotation[0],n.rotation[1],n.rotation[2],n.rotation[3]);t.localScale=V(n.scale);
                if(n.mesh==null||n.materials==null||n.materials.Length==0)continue;var d=n.mesh;var mesh=new Mesh{name=n.name};var v=new Vector3[d.vertices.Length/3];var uv=new Vector2[v.Length];for(int j=0;j<v.Length;j++){v[j]=new Vector3(d.vertices[j*3],d.vertices[j*3+1],d.vertices[j*3+2]);if(d.uv.Length>j*2+1)uv[j]=new Vector2(d.uv[j*2],d.uv[j*2+1]);}mesh.vertices=v;mesh.uv=uv;mesh.subMeshCount=d.groups.Length;for(int j=0;j<d.groups.Length;j++)mesh.SetTriangles(d.groups[j].indices,j);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterials=n.materials.Select(m=>Material(m.texture,m.transparent)).ToArray();
            }
            var renderers=root.GetComponentsInChildren<MeshRenderer>().Where(r=>!r.name.ToLowerInvariant().Contains("shadow")).ToArray();var bounds=renderers.Length>0?renderers[0].bounds:new Bounds(root.transform.position,Vector3.one);foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
            float size=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);camera.orthographicSize=Mathf.Max(.03f,size*.67f);camera.transform.position=bounds.center+new Vector3(size*.5f,size*.9f,-size*1.5f);camera.transform.LookAt(bounds.center);
        }
        void BuildHand(WardrobeItem item){
            hand=JsonUtility.FromJson<HandData>(Resources.Load<TextAsset>(item.motion).text);var raw=Resources.Load<TextAsset>(item.motion+"-frames").bytes;handFrames=new float[raw.Length/4];Buffer.BlockCopy(raw,0,handFrames,0,raw.Length);vertices=new Vector3[hand.vertices];normals=new Vector3[hand.vertices];var uv=new Vector2[hand.vertices];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(hand.uv[i*2],1-hand.uv[i*2+1]);
            handMesh=new Mesh{name="Original hand motion",subMeshCount=2};handMesh.vertices=vertices;handMesh.uv=uv;handMesh.SetTriangles(hand.body,0);handMesh.SetTriangles(hand.nails??new int[0],1);handMesh.MarkDynamic();meshes.Add(handMesh);var go=new GameObject("Original hand surface");go.transform.SetParent(root.transform,false);go.AddComponent<MeshFilter>().sharedMesh=handMesh;go.AddComponent<MeshRenderer>().sharedMaterials=new[]{Material(item.texture,false),Material(string.IsNullOrEmpty(item.nailTexture)?item.texture:item.nailTexture,false)};
            HandFrame(hand.duration*.5f);var bounds=handMesh.bounds;float size=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);var center=root.transform.position+bounds.center;camera.orthographicSize=size*.57f;camera.transform.position=center+new Vector3(size*.15f,size*.8f,-size*1.6f);camera.transform.LookAt(center);
        }
        void HandFrame(float time){
            int f=Mathf.Clamp(Mathf.RoundToInt(time*hand.fps),0,hand.frames-1);for(int i=0;i<vertices.Length;i++){int p=(f*2*hand.vertices+i)*3,n=p+hand.vertices*3;vertices[i]=new Vector3(handFrames[p],handFrames[p+1],handFrames[p+2]);normals[i]=new Vector3(handFrames[n],handFrames[n+1],handFrames[n+2]);}handMesh.vertices=vertices;handMesh.normals=normals;handMesh.RecalculateBounds();
        }
        public void Hide(){if(root!=null){root.SetActive(false);UnityEngine.Object.Destroy(root);}previewTiles?.Dispose();previewTiles=null;foreach(var m in materials)UnityEngine.Object.Destroy(m);foreach(var m in meshes)UnityEngine.Object.Destroy(m);materials.Clear();meshes.Clear();if(image!=null){image.Release();UnityEngine.Object.Destroy(image);}root=null;camera=null;image=null;key=null;effect=null;native=null;hand=null;handMesh=null;handFrames=null;vertices=null;normals=null;}
        public void Dispose(){Hide();if(composite!=null)UnityEngine.Object.Destroy(composite);composite=null;}
    }
}
