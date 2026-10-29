using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

namespace YunJian.Riichi.Editor {
    public static class NativeEffectR11Import {
        const string Folder="Assets/Resources/Majsoul/NativeReview/";
        [Serializable] class Manifest {public Entry[] effects;}
        [Serializable] class Entry {public string sourceKey,nativeEffect;public string[] nativeVariants;}
        [Serializable] class MeshGroup {public int[] indices;}
        [Serializable] class MeshData {public string builtin;public float[] vertices,uv,colors;public int[] indices;public MeshGroup[] groups;}
        [Serializable] class AnimationSources {public NativeR11AnimationStudy.Row[] rows;}
        static readonly Dictionary<string,AssetBundle> bundles=new Dictionary<string,AssetBundle>();
        static readonly List<string> checks=new List<string>();
        static string referenceOverride;
        static string Reference=>referenceOverride??Path.GetFullPath("../dist/previews/2026-10-27-effects-reference/source/");
        static Vector3 V(float[] p)=>new Vector3(p[0],p[1],p[2]);
        static Color C(float[] p)=>new Color(p[0],p[1],p[2],p[3]);
        static void TextureSettings(string path,int filter,int wrapU,int wrapV){
            var importer=AssetImporter.GetAtPath(path)as TextureImporter;if(importer==null)return;
            if(importer.filterMode==(FilterMode)filter&&importer.wrapModeU==(TextureWrapMode)wrapU&&importer.wrapModeV==(TextureWrapMode)wrapV&&importer.textureCompression==TextureImporterCompression.Uncompressed)return;
            importer.filterMode=(FilterMode)filter;importer.wrapModeU=(TextureWrapMode)wrapU;importer.wrapModeV=(TextureWrapMode)wrapV;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        }
        public static void Run(){
            AssetDatabase.Refresh();
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder+"EffectsR11/manifest.json"));
            var animationSources=JsonUtility.FromJson<AnimationSources>("{\"rows\":"+File.ReadAllText(Reference+"animation-sources.json")+"}").rows;
            foreach(string name in manifest.effects.SelectMany(e=>e.nativeVariants??new[]{e.nativeEffect}).Distinct())Import(name,animationSources);
            foreach(var bundle in bundles.Values)bundle.Unload(false);bundles.Clear();AssetDatabase.SaveAssets();
            File.WriteAllLines(Reference+"native-import-checks.txt",checks);
            Debug.Log("NATIVE R11 IMPORT COMPLETE "+checks.Count+" checks");EditorApplication.Exit(0);
        }
        // An explicit separate batch protects the approved action effects.
        public static void RunWardrobeR16(){
            referenceOverride=Path.GetFullPath("../build/wardrobe-r16/");checks.Clear();AssetDatabase.Refresh();
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder+"StaticR16/manifest.json"));
            var sources=JsonUtility.FromJson<AnimationSources>("{\"rows\":"+File.ReadAllText(Reference+"animation-sources.json")+"}").rows;
            // Original clips are kept as local inputs, never runtime bundle downloads.
            referenceOverride=Path.GetFullPath("../build/wardrobe-r16/source/");
            foreach(var entry in manifest.effects)Import(entry.nativeEffect,sources);
            foreach(var bundle in bundles.Values)bundle.Unload(false);bundles.Clear();AssetDatabase.SaveAssets();
            File.WriteAllLines(Path.GetFullPath("../build/wardrobe-r16/native-import-checks.txt"),checks);
            Debug.Log("STATIC WARDROBE R16 IMPORT COMPLETE "+checks.Count);EditorApplication.Exit(0);
        }
        public static void RunFateR19(){
            checks.Clear();AssetDatabase.Refresh();
            string repo=Environment.GetEnvironmentVariable("YUNJIAN_R19_REPO")??Path.GetFullPath("..");
            var sources=JsonUtility.FromJson<AnimationSources>("{\"rows\":"+File.ReadAllText(repo+"/build/fate-r19/source/animation-sources.json")+"}").rows;
            referenceOverride=repo+"/dist/previews/2026-10-27-effects-reference/source/";
            Import("FateR19/ron_25hf",sources);
            foreach(var bundle in bundles.Values)bundle.Unload(false);bundles.Clear();AssetDatabase.SaveAssets();
            File.WriteAllLines(repo+"/build/fate-r19/native-import-checks.txt",checks);
            Debug.Log("FATE R19 IMPORT PASS "+checks.Count);EditorApplication.Exit(0);
        }
        static Mesh Mesh(string path,string output){
            var data=JsonUtility.FromJson<MeshData>(File.ReadAllText(Folder+path));
            var mesh=new Mesh{name=Path.GetFileNameWithoutExtension(path)};
            if(data.builtin=="Quad"){
                var primitive=GameObject.CreatePrimitive(PrimitiveType.Quad);UnityEngine.Object.DestroyImmediate(mesh);mesh=UnityEngine.Object.Instantiate(primitive.GetComponent<MeshFilter>().sharedMesh);UnityEngine.Object.DestroyImmediate(primitive);
                if(AssetDatabase.LoadAssetAtPath<Mesh>(output)!=null)AssetDatabase.DeleteAsset(output);AssetDatabase.CreateAsset(mesh,output);return mesh;
            }
            mesh.vertices=Enumerable.Range(0,data.vertices.Length/3).Select(i=>new Vector3(data.vertices[i*3],data.vertices[i*3+1],data.vertices[i*3+2])).ToArray();
            mesh.uv=Enumerable.Range(0,data.uv.Length/2).Select(i=>new Vector2(data.uv[i*2],data.uv[i*2+1])).ToArray();
            if(data.colors?.Length==mesh.vertexCount*4)mesh.colors=Enumerable.Range(0,mesh.vertexCount).Select(i=>new Color(data.colors[i*4],data.colors[i*4+1],data.colors[i*4+2],data.colors[i*4+3])).ToArray();
            if(mesh.vertexCount>65535)mesh.indexFormat=IndexFormat.UInt32;
            if(data.groups!=null&&data.groups.Length>0){mesh.subMeshCount=data.groups.Length;for(int i=0;i<data.groups.Length;i++)mesh.SetTriangles(data.groups[i].indices,i);}
            else mesh.triangles=data.indices;
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            if(AssetDatabase.LoadAssetAtPath<Mesh>(output)!=null)AssetDatabase.DeleteAsset(output);
            AssetDatabase.CreateAsset(mesh,output);return mesh;
        }
        static Material Material(NativeMaterialSource source,string output){
            var shader=Shader.Find("YunJian/NativeEffectParticle");if(shader==null)throw new Exception("Native effect shader did not compile");
            bool decoration=output.Contains("/StaticR16/");
            bool fate=output.Contains("/FateR19/");if(fate)shader=Resources.Load<Shader>("Majsoul/NativeReview/FateR19/FateParticle");
            if(decoration)shader=Resources.Load<Shader>("Majsoul/Wardrobe/WardrobeParticle");
            if(decoration&&(source.shaderName.StartsWith("Catfood/")||source.shaderName.StartsWith("Unlit/")))shader=Resources.Load<Shader>("Majsoul/Wardrobe/StaticDecoration");
            var material=new Material(shader){name=source.materialName};
            foreach(var value in source.floats??new NativeNamedFloat[0])if(material.HasProperty(value.name))material.SetFloat(value.name,value.value);
            foreach(var value in source.colors??new NativeNamedColor[0])if(material.HasProperty(value.name))material.SetColor(value.name,C(value.value));
            string texturePath=Folder+source.texture+".png";
            var importer=AssetImporter.GetAtPath(texturePath)as TextureImporter;
            if(importer!=null){
                bool changed=importer.filterMode!=(FilterMode)source.filterMode||importer.wrapModeU!=(TextureWrapMode)source.wrapU||importer.wrapModeV!=(TextureWrapMode)source.wrapV||importer.textureCompression!=TextureImporterCompression.Uncompressed;
                if(changed){importer.filterMode=(FilterMode)source.filterMode;importer.wrapModeU=(TextureWrapMode)source.wrapU;importer.wrapModeV=(TextureWrapMode)source.wrapV;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
            }
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);if(material.mainTexture==null)throw new Exception("Missing native texture "+texturePath);
            material.mainTextureScale=new Vector2(source.textureScale[0],source.textureScale[1]);material.mainTextureOffset=new Vector2(source.textureOffset[0],source.textureOffset[1]);
            foreach(var extra in source.extraTextures??new NativeNamedTexture[0]){
                TextureSettings(Folder+extra.texture+".png",extra.filterMode,extra.wrapU,extra.wrapV);
                material.SetTexture(extra.name,AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+extra.texture+".png"));
                material.SetTextureScale(extra.name,new Vector2(extra.scale[0],extra.scale[1]));material.SetTextureOffset(extra.name,new Vector2(extra.offset[0],extra.offset[1]));
            }
            if(material.HasProperty("_SourceFamily"))material.SetFloat("_SourceFamily",source.shaderName=="FizzFX/Normal"?2:source.shaderName=="Mobile/Particles/Additive"?1:0);
            if(material.HasProperty("_UseMask"))material.SetFloat("_UseMask",source.extraTextures!=null&&source.extraTextures.Any(t=>t.name=="_MaskTex")?1:0);
            if(material.HasProperty("_UseDissolve"))material.SetFloat("_UseDissolve",source.extraTextures!=null&&source.extraTextures.Any(t=>t.name=="_DissolveTex")?1:0);
            if(fate)foreach(string feature in new[]{"MainTex","MaskTex","DissolveTex","DistortionTex","DMaskTex","AMBlightTex"})material.SetFloat("_Use"+feature,(source.keywords??new string[0]).Contains("DEF_"+feature)?1:0);
            material.SetFloat("_SrcBlend",source.srcBlend);material.SetFloat("_DstBlend",source.dstBlend);
            if(decoration&&material.HasProperty("_RenderSrcBlend"))material.SetFloat("_RenderSrcBlend",source.srcBlend);
            material.SetFloat("_ZTest",source.zTest);material.SetFloat("_SourceZWrite",source.zWrite);material.SetFloat("_SourceCull",source.cull);
            if(decoration){material.renderQueue=source.zWrite>.5f?2000:3000;if(source.shaderName=="Catfood/Cartoon")material.SetFloat("_Cartoon",1);}
            if(AssetDatabase.LoadAssetAtPath<Material>(output)!=null)AssetDatabase.DeleteAsset(output);
            AssetDatabase.CreateAsset(material,output);return material;
        }
        static void Import(string name,NativeR11AnimationStudy.Row[] animations){
            var config=JsonUtility.FromJson<NativeEffectSource>(File.ReadAllText(Folder+name+"-source.json"));
            var root=new GameObject(Path.GetFileName(name));var nodes=new Transform[config.nodes.Length];
            for(int i=0;i<nodes.Length;i++){
                var n=config.nodes[i];var node=new GameObject(n.name).transform;node.SetParent(n.parent<0?root.transform:nodes[n.parent],false);
                node.localPosition=V(n.position);node.localRotation=new Quaternion(n.rotation[0],n.rotation[1],n.rotation[2],n.rotation[3]);node.localScale=V(n.scale);nodes[i]=node;
                Renderer renderer=null;
                if(!string.IsNullOrEmpty(n.particle)){
                    var ps=node.gameObject.AddComponent<ParticleSystem>();ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                    EditorJsonUtility.FromJsonOverwrite(File.ReadAllText(Folder+n.particle),ps);
                    renderer=ps.GetComponent<ParticleSystemRenderer>();EditorJsonUtility.FromJsonOverwrite(File.ReadAllText(Folder+n.renderer),renderer);
                    if((int)((ParticleSystemRenderer)renderer).renderMode!=n.renderMode)throw new Exception("Native renderer module was not restored "+n.name);
                    var main=ps.main;main.playOnAwake=false;
                    if(n.renderMode==4){var meshes=(n.meshes??new[]{n.mesh}).Select((p,j)=>Mesh(p,Folder+name+"-"+i+"-"+j+".asset")).ToArray();((ParticleSystemRenderer)renderer).SetMeshes(meshes);}
                }else if(n.staticMesh){node.gameObject.AddComponent<MeshFilter>().sharedMesh=Mesh(n.mesh,Folder+name+"-"+i+"-model.asset");renderer=node.gameObject.AddComponent<MeshRenderer>();renderer.sortingOrder=n.sortingOrder;}
                if(renderer!=null){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.sharedMaterials=n.materials.Select((m,j)=>Material(m,Folder+name+"-"+i+"-"+j+".mat")).ToArray();}
                if(config.preserveActiveState)node.gameObject.SetActive(n.active);
            }
            var tracks=new List<NativeMotionTrack>();
            for(int index=0;index<nodes.Length;index++)foreach(string path in config.nodes[index].animations??new string[0]){
                var row=animations.First(a=>a.output==Path.ChangeExtension(path,".anim"));
                var track=Bake(root,nodes,index,row,name.StartsWith("FateR19/"));tracks.Add(track);
                string emptyAsset=Folder+row.output;if(AssetDatabase.LoadAssetAtPath<AnimationClip>(emptyAsset)!=null)AssetDatabase.DeleteAsset(emptyAsset);
            }
            if(tracks.Count>0){
                string motionPath=Folder+name+"-motion.json";File.WriteAllText(motionPath,JsonUtility.ToJson(new NativeMotionData{tracks=tracks.ToArray()}));AssetDatabase.ImportAsset(motionPath);
                var motion=root.AddComponent<NativeEffectMotion>();motion.nodes=nodes;motion.source=AssetDatabase.LoadAssetAtPath<TextAsset>(motionPath);
            }
            PrefabUtility.SaveAsPrefabAsset(root,Folder+name+".prefab");
            checks.Add(name+" particles="+root.GetComponentsInChildren<ParticleSystem>(true).Length+" models="+root.GetComponentsInChildren<MeshRenderer>(true).Length+" animationTracks="+tracks.Count);
            UnityEngine.Object.DestroyImmediate(root);Debug.Log("Native imported "+name);
        }
        sealed class Channel {
            public NativeMotionCurve curve;public Func<float[]> read;public float[] baseline;public List<float> samples=new List<float>();
        }
        static void AnimatedBlock(Renderer renderer,MaterialPropertyBlock block,int material,int property){renderer.GetPropertyBlock(block,material);if(!block.HasProperty(property))renderer.GetPropertyBlock(block);}
        static NativeMotionTrack Bake(GameObject root,Transform[] nodes,int owner,NativeR11AnimationStudy.Row row,bool includeRendererVisibility=false){
            if(!bundles.TryGetValue(row.bundle,out var bundle)){
                bundle=AssetBundle.LoadFromFile(Reference+"bundles/"+row.bundle);if(bundle==null)throw new Exception("Could not load original clip bundle");bundles[row.bundle]=bundle;
            }
            var clip=bundle.LoadAllAssets<AnimationClip>().First(c=>c.name==row.name);
            var channels=new List<Channel>();
            Action<int,int,int,string,int,Func<float[]>> add=(node,kind,mat,property,components,read)=>channels.Add(new Channel{curve=new NativeMotionCurve{node=node,kind=kind,material=mat,property=property,components=components},read=read,baseline=read()});
            var initial=nodes.Select(t=>(t.localPosition,t.localRotation,t.localScale,t.gameObject.activeSelf)).ToArray();
            for(int index=0;index<nodes.Length;index++){
                int i=index;var t=nodes[i];
                add(i,0,0,"",3,()=>new[]{t.localPosition.x,t.localPosition.y,t.localPosition.z});
                add(i,1,0,"",4,()=>new[]{t.localRotation.x,t.localRotation.y,t.localRotation.z,t.localRotation.w});
                add(i,2,0,"",3,()=>new[]{t.localScale.x,t.localScale.y,t.localScale.z});
                if(referenceOverride!=null)add(i,6,0,"",1,()=>new[]{t.gameObject.activeSelf?1f:0f});
                var renderer=t.GetComponent<Renderer>();if(renderer==null)continue;
                if(includeRendererVisibility)add(i,7,0,"",1,()=>new[]{renderer.enabled?1f:0f});
                for(int materialIndex=0;materialIndex<renderer.sharedMaterials.Length;materialIndex++){
                    int mi=materialIndex;var material=renderer.sharedMaterials[mi];var shader=material.shader;
                    for(int propertyIndex=0;propertyIndex<shader.GetPropertyCount();propertyIndex++){
                        string property=shader.GetPropertyName(propertyIndex);int id=Shader.PropertyToID(property);var block=new MaterialPropertyBlock();
                        var type=shader.GetPropertyType(propertyIndex);
                        if(type==ShaderPropertyType.Color)add(i,3,mi,property,4,()=>{AnimatedBlock(renderer,block,mi,id);var v=block.HasProperty(id)?block.GetColor(id):material.GetColor(id);return new[]{v.r,v.g,v.b,v.a};});
                        else if(type==ShaderPropertyType.Float||type==ShaderPropertyType.Range)add(i,4,mi,property,1,()=>{AnimatedBlock(renderer,block,mi,id);return new[]{block.HasProperty(id)?block.GetFloat(id):material.GetFloat(id)};});
                        else if(type==ShaderPropertyType.Vector)add(i,5,mi,property,4,()=>{AnimatedBlock(renderer,block,mi,id);var v=block.HasProperty(id)?block.GetVector(id):material.GetVector(id);return new[]{v.x,v.y,v.z,v.w};});
                    }
                    var textureNames=referenceOverride==null?new[]{"_MainTex","_MaskTex"}:Enumerable.Range(0,shader.GetPropertyCount()).Where(p=>shader.GetPropertyType(p)==ShaderPropertyType.Texture).Select(shader.GetPropertyName).ToArray();
                    foreach(string texture in textureNames){
                        if(!material.HasProperty(texture))continue;
                        string property=texture+"_ST";int id=Shader.PropertyToID(property);var block=new MaterialPropertyBlock();
                        add(i,5,mi,property,4,()=>{AnimatedBlock(renderer,block,mi,id);if(block.HasProperty(id)){var v=block.GetVector(id);return new[]{v.x,v.y,v.z,v.w};}var scale=material.GetTextureScale(texture);var offset=material.GetTextureOffset(texture);return new[]{scale.x,scale.y,offset.x,offset.y};});
                    }
                }
            }
            var animator=nodes[owner].gameObject.AddComponent<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var graph=PlayableGraph.Create("Source effect clip bake");var playable=AnimationClipPlayable.Create(graph,clip);
            var output=AnimationPlayableOutput.Create(graph,"Source",animator);output.SetSourcePlayable(playable);graph.Play();
            const float fps=60;int frames=Mathf.CeilToInt(clip.length*fps)+1;
            for(int frame=0;frame<frames;frame++){playable.SetTime(Mathf.Min(frame/fps,clip.length));graph.Evaluate(0);foreach(var channel in channels)channel.samples.AddRange(channel.read());}
            graph.Destroy();UnityEngine.Object.DestroyImmediate(animator);
            for(int i=0;i<nodes.Length;i++){nodes[i].localPosition=initial[i].Item1;nodes[i].localRotation=initial[i].Item2;nodes[i].localScale=initial[i].Item3;if(referenceOverride!=null)nodes[i].gameObject.SetActive(initial[i].Item4);var renderer=nodes[i].GetComponent<Renderer>();if(renderer!=null){renderer.SetPropertyBlock(null);for(int m=0;m<renderer.sharedMaterials.Length;m++)renderer.SetPropertyBlock(null,m);}}
            var changed=channels.Where(c=>c.samples.Where((v,i)=>Mathf.Abs(v-c.baseline[i%c.curve.components])>0.000001f).Any()).ToArray();
            foreach(var channel in changed)channel.curve.values=channel.samples.ToArray();
            checks.Add("Original clip "+row.effect+" / "+row.name+" duration="+clip.length+" sampled="+frames+" changedChannels="+changed.Length);
            if(changed.Length==0)throw new Exception("Original animation produced no supported channels: "+row.effect+" / "+row.name);
            return new NativeMotionTrack{duration=clip.length,fps=fps,loop=referenceOverride!=null&&clip.isLooping,curves=changed.Select(c=>c.curve).ToArray()};
        }
    }
}
