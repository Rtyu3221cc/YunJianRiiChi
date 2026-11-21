using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

namespace YunJian.Riichi.Editor {
    public static class CharacterHandImport {
        const string Folder="Assets/Resources/Majsoul/CharacterHands/";
        [Serializable] class Manifest {public Hand[] hands;public Mat[] materials;public MeshFile[] meshes;}
        [Serializable] class Hand {public string key;public int id;public int[] characters;public Timing[] timing;public Variant[] variants;}
        [Serializable] class Timing {public int id;public string name,type;public float lifetime,speed;public float[] keypoint;}
        [Serializable] class Variant {public string name;public Node[] nodes;public Animation[] animations;}
        [Serializable] class Animation {public string name,action,bundle;public float duration;public int owner;}
        [Serializable] class Node {public string name,mesh;public int parent,rootBone;public int[] bones;public string[] materials;public float[] position,rotation,scale;public bool active,skinned,shadow,enabled;}
        [Serializable] class Mat {public string key,name,texture,sourceShader;public float[] tint,scale,offset;public bool transparent,brdf;public Tex[] textures;public Scalar[] floats;public Tint[] colors;}
        [Serializable] class Tex {public string name,file;public bool srgb,mipmaps;public int filter,wrap;}
        [Serializable] class Scalar {public string name;public float value;}
        [Serializable] class Tint {public string name;public float[] value;}
        [Serializable] class MeshFile {public string key,file;}
        [Serializable] class Group {public int[] indices;}
        [Serializable] class Geometry {public float[] vertices,normals,tangents,uv,weights,bindposes;public int[] boneIndices;public Group[] groups;}
        [Serializable] class Catalog {public Binding[] hands;}
        [Serializable] class Binding {public string key;public int sourceItemId;public int[] characters;public string[] variants;public SourceHandAction[] actions;}
        [Serializable] class Motion {public int frames,fps;public float duration,landTime;public float[] tilePoints,tileRotations;public NativeMotionData pose;}
        static string source,output;static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();static readonly Dictionary<string,AssetBundle> bundles=new Dictionary<string,AssetBundle>();static readonly List<string> checks=new List<string>();
        static Vector3 V(float[] a)=>new Vector3(a[0],a[1],a[2]);
        static Quaternion Q(float[] a)=>new Quaternion(a[0],a[1],a[2],a[3]);
        static void SaveAsset(UnityEngine.Object obj,string path){var previous=AssetDatabase.LoadMainAssetAtPath(path);if(previous==null)AssetDatabase.CreateAsset(obj,path);else{EditorUtility.CopySerialized(obj,previous);UnityEngine.Object.DestroyImmediate(obj);EditorUtility.SetDirty(previous);}}
        static void PrepareSourceTextures(Manifest manifest){
            foreach(var entry in manifest.materials.Where(m=>m.brdf).SelectMany(m=>m.textures).Where(t=>!string.IsNullOrEmpty(t.file)).GroupBy(t=>t.file).Select(g=>g.First())){
                string path=Folder+"Textures/"+Path.GetFileName(entry.file);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.sRGBTexture=entry.srgb;importer.mipmapEnabled=entry.mipmaps;importer.filterMode=(FilterMode)entry.filter;importer.alphaIsTransparency=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=(TextureWrapMode)entry.wrap;importer.SaveAndReimport();
            }
        }
        static void SourceMaterial(Material material,Mat row){
            if(!row.brdf){material.SetFloat("_SourceBRDF",0);return;}
            // Texture reimport can reload dependent materials; finish imports
            // before assigning any material properties.
            material.SetFloat("_SourceBRDF",1);foreach(var entry in row.textures)if(!string.IsNullOrEmpty(entry.file))material.SetTexture(entry.name,AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"Textures/"+Path.GetFileName(entry.file)));
            foreach(var entry in row.floats)if(material.HasProperty(entry.name))material.SetFloat(entry.name,entry.value);
            foreach(var entry in row.colors)if(material.HasProperty(entry.name)){var c=entry.value;if(entry.name=="_SpecDirection2")material.SetVector(entry.name,new Vector4(-c[0],c[1],-c[2],c[3]));else material.SetColor(entry.name,new Color(c[0],c[1],c[2],c[3]));}
            material.SetVector("_HandLightDir",TableLook.LightDirection);material.SetColor("_HandLightColor",TableLook.LightColor);
        }
        public static void RepairMaterials(){
            string repo=Environment.GetEnvironmentVariable("YUNJIAN_HAND_REPO")??Path.GetFullPath("..");source=repo+"/build/character-hands-r37/source/";var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(source+"manifest.json"));
            foreach(string file in Directory.GetFiles(source+"textures","*.png"))File.Copy(file,Folder+"Textures/"+Path.GetFileName(file),true);AssetDatabase.Refresh();
            PrepareSourceTextures(manifest);foreach(var row in manifest.materials.Where(m=>m.brdf)){var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Materials/"+row.key+".mat");SourceMaterial(material,row);EditorUtility.SetDirty(material);}
            foreach(string key in manifest.hands.First(h=>h.key=="hand_yasina").variants.SelectMany(v=>v.nodes).Where(n=>!string.IsNullOrEmpty(n.mesh)&&!n.shadow).Select(n=>n.mesh).Distinct()){
                var row=manifest.meshes.First(m=>m.key==key);var data=JsonUtility.FromJson<Geometry>(File.ReadAllText(source+row.file));var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"Meshes/"+key+".asset");mesh.tangents=Enumerable.Range(0,data.tangents.Length/4).Select(i=>new Vector4(data.tangents[4*i],data.tangents[4*i+1],data.tangents[4*i+2],data.tangents[4*i+3])).ToArray();EditorUtility.SetDirty(mesh);
            }AssetDatabase.SaveAssets();Debug.Log("CHARACTER HAND SOURCE MATERIAL REPAIR PASS");EditorApplication.Exit(0);
        }
        public static void FixShadows(){
            int changed=0;foreach(string path in Directory.GetFiles(Folder,"*.prefab",SearchOption.AllDirectories)){
                var root=PrefabUtility.LoadPrefabContents(path);bool dirty=false;foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))if(renderer.enabled&&(renderer.name.IndexOf("shadow",StringComparison.OrdinalIgnoreCase)>=0||renderer.name.IndexOf("yingz",StringComparison.OrdinalIgnoreCase)>=0)){renderer.enabled=false;dirty=true;}
                if(dirty){PrefabUtility.SaveAsPrefabAsset(root,path);changed++;}PrefabUtility.UnloadPrefabContents(root);
            }AssetDatabase.SaveAssets();Debug.Log("CHARACTER HAND SHADOW PREFABS FIXED "+changed);EditorApplication.Exit(0);
        }
        public static void Run(){
            string repo=Environment.GetEnvironmentVariable("YUNJIAN_HAND_REPO")??Path.GetFullPath("..");source=repo+"/build/character-hands-r37/source/";output=repo+"/build/character-hands-r37/";
            Directory.CreateDirectory(Folder);Directory.CreateDirectory(Folder+"Textures");Directory.CreateDirectory(Folder+"Meshes");Directory.CreateDirectory(Folder+"Materials");
            foreach(var file in Directory.GetFiles(source+"textures","*.png"))File.Copy(file,Folder+"Textures/"+Path.GetFileName(file),true);
            AssetDatabase.Refresh();var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(source+"manifest.json"));
            foreach(var row in manifest.meshes){
                var data=JsonUtility.FromJson<Geometry>(File.ReadAllText(source+row.file));var mesh=new Mesh{name="Character hand "+row.key,indexFormat=data.vertices.Length/3>65535?IndexFormat.UInt32:IndexFormat.UInt16};
                mesh.vertices=Enumerable.Range(0,data.vertices.Length/3).Select(i=>new Vector3(data.vertices[3*i],data.vertices[3*i+1],data.vertices[3*i+2])).ToArray();mesh.uv=Enumerable.Range(0,mesh.vertexCount).Select(i=>new Vector2(data.uv[2*i],data.uv[2*i+1])).ToArray();
                if(data.normals?.Length==data.vertices.Length)mesh.normals=Enumerable.Range(0,mesh.vertexCount).Select(i=>new Vector3(data.normals[3*i],data.normals[3*i+1],data.normals[3*i+2])).ToArray();
                if(data.tangents?.Length==mesh.vertexCount*4)mesh.tangents=Enumerable.Range(0,mesh.vertexCount).Select(i=>new Vector4(data.tangents[4*i],data.tangents[4*i+1],data.tangents[4*i+2],data.tangents[4*i+3])).ToArray();
                mesh.subMeshCount=data.groups.Length;for(int i=0;i<data.groups.Length;i++)mesh.SetTriangles(data.groups[i].indices,i);
                if(data.weights?.Length==mesh.vertexCount*4){mesh.boneWeights=Enumerable.Range(0,mesh.vertexCount).Select(i=>new BoneWeight{weight0=data.weights[4*i],weight1=data.weights[4*i+1],weight2=data.weights[4*i+2],weight3=data.weights[4*i+3],boneIndex0=data.boneIndices[4*i],boneIndex1=data.boneIndices[4*i+1],boneIndex2=data.boneIndices[4*i+2],boneIndex3=data.boneIndices[4*i+3]}).ToArray();mesh.bindposes=Enumerable.Range(0,data.bindposes.Length/16).Select(n=>{var m=new Matrix4x4();for(int i=0;i<4;i++)for(int j=0;j<4;j++)m[i,j]=data.bindposes[16*n+4*i+j];return m;}).ToArray();}
                if(data.normals==null||data.normals.Length!=data.vertices.Length)mesh.RecalculateNormals();mesh.RecalculateBounds();string path=Folder+"Meshes/"+row.key+".asset";SaveAsset(mesh,path);meshes[row.key]=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            }
            PrepareSourceTextures(manifest);foreach(var row in manifest.materials){
                var material=new Material(Shader.Find("YunJian/CharacterHand")){name=row.name};if(!string.IsNullOrEmpty(row.texture)){string path=Folder+"Textures/"+Path.GetFileName(row.texture);var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(!row.brdf&&(importer.textureCompression!=TextureImporterCompression.Uncompressed||!importer.mipmapEnabled||importer.wrapMode!=TextureWrapMode.Repeat)){importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.SaveAndReimport();}material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);}
                material.color=new Color(row.tint[0],row.tint[1],row.tint[2],row.tint[3]);material.mainTextureScale=new Vector2(row.scale[0],row.scale[1]);material.mainTextureOffset=new Vector2(row.offset[0],row.offset[1]);SourceMaterial(material,row);string dest=Folder+"Materials/"+row.key+".mat";SaveAsset(material,dest);materials[row.key]=AssetDatabase.LoadAssetAtPath<Material>(dest);
            }
            var catalog=new List<Binding>();
            foreach(var hand in manifest.hands){
                Directory.CreateDirectory(Folder+hand.key);var paths=new List<string>();
                foreach(var variant in hand.variants){
                    var nodes=new Transform[variant.nodes.Length];GameObject root=null;
                    for(int i=0;i<nodes.Length;i++){var d=variant.nodes[i];var go=new GameObject(d.name);nodes[i]=go.transform;if(i==0)root=go;else nodes[i].SetParent(nodes[d.parent],false);nodes[i].localPosition=V(d.position);nodes[i].localRotation=Q(d.rotation);nodes[i].localScale=V(d.scale);go.SetActive(d.active);}
                    for(int i=0;i<nodes.Length;i++){var n=variant.nodes[i];if(string.IsNullOrEmpty(n.mesh))continue;Renderer r;if(n.skinned){var skin=nodes[i].gameObject.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=meshes[n.mesh];skin.bones=n.bones.Select(b=>b<0?null:nodes[b]).ToArray();skin.rootBone=n.rootBone<0?null:nodes[n.rootBone];skin.updateWhenOffscreen=true;skin.localBounds=new Bounds(Vector3.zero,Vector3.one*5);r=skin;}else{nodes[i].gameObject.AddComponent<MeshFilter>().sharedMesh=meshes[n.mesh];r=nodes[i].gameObject.AddComponent<MeshRenderer>();}r.sharedMaterials=n.materials.Select(m=>materials[m]).ToArray();r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.enabled=n.enabled&&!n.shadow;}
                    string stem=Folder+hand.key+"/"+variant.name;PrefabUtility.SaveAsPrefabAsset(root,stem+".prefab");paths.Add("Majsoul/CharacterHands/"+hand.key+"/"+variant.name);
                    foreach(var animation in variant.animations.Where(a=>new[]{"Dapai","LiQiPai","LiZhi","Fulu","Zimo"}.Contains(a.action))){var timing=hand.timing.First(t=>t.name==animation.action);var data=Bake(root,nodes,animation,timing);File.WriteAllText(stem+"-"+animation.action+".json",JsonUtility.ToJson(data));checks.Add(variant.name+"/"+animation.action+" frames="+data.frames+" curves="+data.pose.tracks[0].curves.Length);}
                    UnityEngine.Object.DestroyImmediate(root);
                }
                catalog.Add(new Binding{key=hand.key,sourceItemId=hand.id,characters=hand.characters,variants=paths.ToArray(),actions=hand.timing.Select(t=>new SourceHandAction{type=hand.key,sourceId=t.id,action=t.name,speed=t.speed,lifetime=t.lifetime/1000/t.speed,cue=t.keypoint[0]/1000/t.speed,release=t.keypoint.Last()/1000/t.speed}).ToArray()});
            }
            File.WriteAllText(Folder+"catalog.json",JsonUtility.ToJson(new Catalog{hands=catalog.ToArray()},true));AssetDatabase.Refresh();AssetDatabase.SaveAssets();foreach(var bundle in bundles.Values)bundle.Unload(false);
            File.WriteAllLines(output+"import-checks.txt",checks);Debug.Log("CHARACTER HAND IMPORT PASS "+catalog.Count+" hands / "+checks.Count+" clips");EditorApplication.Exit(0);
        }
        static Motion Bake(GameObject root,Transform[] nodes,Animation row,Timing timing){
            if(!bundles.TryGetValue(row.bundle,out var bundle)){bundles[row.bundle]=bundle=AssetBundle.LoadFromFile(source+"bundles/"+row.bundle);if(bundle==null)throw new Exception("Cannot load source animation bundle "+row.bundle);}
            var clip=bundle.LoadAllAssets<AnimationClip>().First(c=>c.name==row.name);var initial=nodes.Select(t=>(t.localPosition,t.localRotation,t.localScale)).ToArray();
            var animator=nodes[row.owner].gameObject.AddComponent<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;var graph=PlayableGraph.Create("Original character hand");var playable=AnimationClipPlayable.Create(graph,clip);var sink=AnimationPlayableOutput.Create(graph,"Hand",animator);sink.SetSourcePlayable(playable);graph.Play();
            const int fps=30;int frames=Mathf.CeilToInt(clip.length*fps)+1;var values=nodes.Select(t=>new List<float>[] {new List<float>(),new List<float>(),new List<float>()}).ToArray();var points=new List<float>();var rotations=new List<float>();var anchor=nodes.First(t=>t.name==(row.action=="LiZhi"?"node_liqibang":"node_tile"));
            for(int f=0;f<frames;f++){float time=Mathf.Min(f/(float)fps,clip.length);playable.SetTime(time);graph.Evaluate(0);for(int i=0;i<nodes.Length;i++){var t=nodes[i];values[i][0].AddRange(new[]{t.localPosition.x,t.localPosition.y,t.localPosition.z});values[i][1].AddRange(new[]{t.localRotation.x,t.localRotation.y,t.localRotation.z,t.localRotation.w});values[i][2].AddRange(new[]{t.localScale.x,t.localScale.y,t.localScale.z});}var p=root.transform.InverseTransformPoint(anchor.position);points.AddRange(new[]{-p.x,p.y,-p.z});var q=Quaternion.Inverse(root.transform.rotation)*anchor.rotation;rotations.AddRange(new[]{-q.x,q.y,-q.z,q.w});}
            graph.Destroy();UnityEngine.Object.DestroyImmediate(animator);var curves=new List<NativeMotionCurve>();for(int i=0;i<nodes.Length;i++){for(int kind=0;kind<3;kind++){int components=kind==1?4:3;var a=values[i][kind].ToArray();if(Enumerable.Range(components,a.Length-components).Any(n=>Mathf.Abs(a[n]-a[n%components])>.000001f))curves.Add(new NativeMotionCurve{node=i,kind=kind,components=components,values=a});}nodes[i].localPosition=initial[i].Item1;nodes[i].localRotation=initial[i].Item2;nodes[i].localScale=initial[i].Item3;}
            if(curves.Count==0)throw new Exception("No changing bones in "+root.name+" / "+row.name);
            return new Motion{frames=frames,fps=fps,duration=clip.length,landTime=timing.keypoint.Last()/1000,tilePoints=points.ToArray(),tileRotations=rotations.ToArray(),pose=new NativeMotionData{tracks=new[]{new NativeMotionTrack{owner=row.owner,duration=clip.length,fps=fps,curves=curves.ToArray()}}}};
        }
    }
}
