using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spine;
using UnityEngine;
using UnityEngine.Rendering;
using AnimationState=Spine.AnimationState;
using Event=UnityEngine.Event;

namespace YunJian.Riichi {
    // Original binary skeletons: weighted meshes, deforms, clipping, constraints,
    // draw order, tint and blend modes are evaluated by the matching 4.2 runtime.
    public sealed class CharacterIllustration:IDisposable {
        sealed class Pages:Spine.TextureLoader {
            readonly string folder;public Pages(string f){folder=f;}
            public void Load(AtlasPage page,string path){var t=CharacterRoster.Texture(folder+"/"+Path.GetFileNameWithoutExtension(path));if(t==null)throw new FileNotFoundException(path);page.rendererObject=t;if(page.width==0)page.width=t.width;if(page.height==0)page.height=t.height;}
            public void Unload(object texture){}
        }
        sealed class Part:IDisposable {
            readonly Skeleton skeleton;readonly AnimationState state;readonly Mesh mesh=new Mesh();readonly SkeletonClipping clipper=new SkeletonClipping();
            readonly List<Vector3> vertices=new List<Vector3>();readonly List<Vector2> uv=new List<Vector2>();readonly List<Color> colors=new List<Color>();readonly List<Vector3> dark=new List<Vector3>();
            readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();readonly List<Material> drawMaterials=new List<Material>();readonly List<List<int>> triangles=new List<List<int>>();
            readonly float[] world=new float[65536];static readonly int[] Quad={0,1,2,2,3,0};
            readonly CommandBuffer commands=new CommandBuffer{name="Original character skeleton"};readonly Material unpremultiply;
            RenderTexture pma,result;Rect bounds;float time=-1;int frame=-1;public string[] Animations;
            public Part(string folder){
                var atlas=new Atlas(new StringReader(Resources.Load<TextAsset>("Majsoul/Characters/"+folder+"/atlas").text),"",new Pages(folder));atlas.FlipV();
                var binary=new SkeletonBinary(new AtlasAttachmentLoader(atlas));var bytes=Resources.Load<TextAsset>("Majsoul/Characters/"+folder+"/skeleton").bytes;
                var data=binary.ReadSkeletonData(new MemoryStream(bytes));skeleton=new Skeleton(data);state=new AnimationState(new AnimationStateData(data){DefaultMix=.18f});
                Animations=data.Animations.Select(a=>a.Name).ToArray();string idle=Animations.FirstOrDefault(a=>a=="idle")??Animations.FirstOrDefault(a=>a.IndexOf("idle",StringComparison.OrdinalIgnoreCase)>=0)??Animations.FirstOrDefault();
                if(idle!=null)state.SetAnimation(0,idle,true);state.Apply(skeleton);skeleton.UpdateWorldTransform(Skeleton.Physics.Update);
                bounds=VisibleBounds();
                int rh=1536,rw=Mathf.Clamp(Mathf.RoundToInt(rh*bounds.width/bounds.height),256,2048);pma=new RenderTexture(rw,rh,0,RenderTextureFormat.ARGB32);pma.Create();result=new RenderTexture(rw,rh,0,RenderTextureFormat.ARGB32);result.Create();
                unpremultiply=new Material(Shader.Find("YunJian/CharacterStraightAlpha"));mesh.indexFormat=IndexFormat.UInt32;
                Debug.Log("CHARACTER SPINE "+folder+" animations="+string.Join(",",Animations)+" bounds="+bounds);
            }
            Rect VisibleBounds(){
                float minX=float.MaxValue,minY=float.MaxValue,maxX=float.MinValue,maxY=float.MinValue;
                foreach(var slot in skeleton.DrawOrder){
                    if(!slot.Bone.Active||slot.A<.025f)continue;int count=0;
                    if(slot.Attachment is RegionAttachment r&&r.A>.025f){r.ComputeWorldVertices(slot,world,0);count=8;}
                    else if(slot.Attachment is MeshAttachment m&&m.A>.025f){count=m.WorldVerticesLength;m.ComputeWorldVertices(slot,0,count,world,0);}
                    for(int i=0;i<count;i+=2){minX=Mathf.Min(minX,world[i]);maxX=Mathf.Max(maxX,world[i]);minY=Mathf.Min(minY,world[i+1]);maxY=Mathf.Max(maxY,world[i+1]);}
                }
                float w=maxX-minX,h=maxY-minY;return new Rect(minX-w*.035f,minY-h*.035f,w*1.07f,h*1.07f);
            }
            public void Play(string animation){if(Animations.Contains(animation)){state.SetAnimation(0,animation,false);string idle=Animations.FirstOrDefault(a=>a=="idle")??Animations[0];state.AddAnimation(0,idle,true,0);}}
            Material Material(AtlasPage page,Spine.BlendMode blend){
                string k=page.name+":"+blend;if(materials.TryGetValue(k,out var m))return m;
                m=new Material(Shader.Find("YunJian/CharacterSpine")){mainTexture=(Texture2D)page.rendererObject};
                m.SetFloat("_SrcBlend",(float)(blend==Spine.BlendMode.Multiply?UnityEngine.Rendering.BlendMode.DstColor:blend==Spine.BlendMode.Screen?UnityEngine.Rendering.BlendMode.OneMinusDstColor:UnityEngine.Rendering.BlendMode.One));
                m.SetFloat("_DstBlend",(float)(blend==Spine.BlendMode.Additive?UnityEngine.Rendering.BlendMode.One:UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));m.SetFloat("_PMA",page.pma?1:0);materials[k]=m;return m;
            }
            public Texture Draw(float now){
                if(frame==Time.frameCount&&Mathf.Approximately(time,now))return result;frame=Time.frameCount;
                float delta=time<0?0:Mathf.Max(0,now-time);time=now;state.Update(delta);state.Apply(skeleton);skeleton.Update(delta);skeleton.UpdateWorldTransform(Skeleton.Physics.Update);
                vertices.Clear();uv.Clear();colors.Clear();dark.Clear();triangles.Clear();drawMaterials.Clear();clipper.ClipEnd();
                foreach(var slot in skeleton.DrawOrder){
                    var a=slot.Attachment;if(a is ClippingAttachment clipping){clipper.ClipStart(slot,clipping);continue;}
                    float[] uvs;int[] tris;int length;AtlasRegion region;Color color;
                    if(a is RegionAttachment ra){ra.ComputeWorldVertices(slot,world,0,2);uvs=ra.UVs;tris=Quad;length=8;region=(AtlasRegion)ra.Region;color=new Color(ra.R*slot.R,ra.G*slot.G,ra.B*slot.B,ra.A*slot.A);}
                    else if(a is MeshAttachment ma){length=ma.WorldVerticesLength;ma.ComputeWorldVertices(slot,0,length,world,0,2);uvs=ma.UVs;tris=ma.Triangles;region=(AtlasRegion)ma.Region;color=new Color(ma.R*slot.R,ma.G*slot.G,ma.B*slot.B,ma.A*slot.A);}
                    else{clipper.ClipEnd(slot);continue;}
                    float[] v=world;
                    if(clipper.IsClipping){clipper.ClipTriangles(world,tris,tris.Length,uvs);v=clipper.ClippedVertices.Items;length=clipper.ClippedVertices.Count;uvs=clipper.ClippedUVs.Items;tris=clipper.ClippedTriangles.ToArray();}
                    if(length>0){
                        var material=Material(region.page,slot.Data.BlendMode);
                        if(drawMaterials.Count==0||drawMaterials[drawMaterials.Count-1]!=material){drawMaterials.Add(material);triangles.Add(new List<int>());}
                        int start=vertices.Count;for(int i=0;i<length;i+=2){vertices.Add(new Vector3(v[i],v[i+1],0));uv.Add(new Vector2(uvs[i],uvs[i+1]));colors.Add(color);dark.Add(slot.HasSecondColor?new Vector3(slot.R2,slot.G2,slot.B2):Vector3.zero);}
                        foreach(int t in tris)triangles[triangles.Count-1].Add(start+t);
                    }
                    clipper.ClipEnd(slot);
                }
                clipper.ClipEnd();mesh.Clear();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetUVs(1,dark);mesh.subMeshCount=triangles.Count;for(int i=0;i<triangles.Count;i++)mesh.SetTriangles(triangles[i],i,false);mesh.RecalculateBounds();
                // Do not change Unity's current GUI view/projection matrices. Render
                // with a material-local projection and restore the active framebuffer.
                var previous=RenderTexture.active;var projection=GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(bounds.xMin,bounds.xMax,bounds.yMin,bounds.yMax,-1,1),true);
                commands.Clear();commands.SetRenderTarget(pma);commands.ClearRenderTarget(false,true,Color.clear);
                for(int i=0;i<drawMaterials.Count;i++){drawMaterials[i].SetMatrix("_CharacterProjection",projection);commands.DrawMesh(mesh,Matrix4x4.identity,drawMaterials[i],i,0);}
                Graphics.ExecuteCommandBuffer(commands);Graphics.Blit(pma,result,unpremultiply);RenderTexture.active=previous;return result;
            }
            public void Dispose(){foreach(var m in materials.Values)UnityEngine.Object.Destroy(m);UnityEngine.Object.Destroy(mesh);UnityEngine.Object.Destroy(unpremultiply);commands.Dispose();pma.Release();result.Release();UnityEngine.Object.Destroy(pma);UnityEngine.Object.Destroy(result);}
        }
        readonly Dictionary<int,Part[]> dynamics=new Dictionary<int,Part[]>();
        public bool Animate=true;
        public Texture Art(CharacterSkin skin,float now){
            if(!skin.dynamic||!Animate)return CharacterRoster.Texture("Skins/"+skin.id+"/full");
            if(!dynamics.TryGetValue(skin.id,out var parts)){parts=skin.parts.Select(p=>new Part(p)).ToArray();dynamics[skin.id]=parts;}
            return parts.Length==0?CharacterRoster.Texture("Skins/"+skin.id+"/full"):parts[0].Draw(now);
        }
        public void Play(CharacterSkin skin,string name){if(dynamics.TryGetValue(skin.id,out var parts))foreach(var p in parts)p.Play(name);}
        public void Draw(Rect rect,CharacterSkin skin,float now){if(Event.current.type!=EventType.Repaint)return;var t=Art(skin,now);if(t!=null)GUI.DrawTexture(rect,t,ScaleMode.ScaleToFit,true);}
        public void Dispose(){foreach(var parts in dynamics.Values)foreach(var p in parts)p.Dispose();dynamics.Clear();}
    }
}
