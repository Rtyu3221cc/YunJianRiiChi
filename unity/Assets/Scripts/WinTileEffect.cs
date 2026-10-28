using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace YunJian.Riichi
{
    // Native fire-tornado skin 305034, including its real ring and flame-column meshes.
    // Hierarchy, sizes, per-axis curves and animation sheets remain source data.
    public sealed class WinTileEffect : MonoBehaviour
    {
        [Serializable] public class Key { public float time,value; }
        [Serializable] public class ColorKey { public float time;public float[] color; }
        [Serializable] public class Burst { public float time;public int min,max; }
        [Serializable] public class Node { public string name;public int parent;public float[] position,rotation,scale; }
        [Serializable] public class MeshData { public float[] positions,uv;public int[] indices; }
        [Serializable] public class Layer {
            public bool sourceBursts,sourceAutoSeed;public uint sourceSeed;
            public ColorKey[] colors;public float[] box;public bool nativeShape;public float randomDirection;
            public string name,texture;public int node,blendDst,blendSrc,maxParticles,renderMode,shape,cycles,scaleMode,simulationSpace,velocitySpace,velocityType;
            public float[] colorMin,colorMax,tint,tiles,size3d,size3dMin,velocityMin,velocityMax,rotation3d,rotation3dMin;
            public float angularSpeed,angularMin,angularMax;public float duration,delay,lifeMin,lifeMax,sizeMin,sizeMax,speedMin,speedMax,rotationMin,rotationMax,gravity,rate,stretch,radius,coneAngle;
            public float frameMin,frameMax,startFrame;public Key[] rotationCurve;
            public bool randomFrame;public Key[] alpha,size,sizeX,sizeY,sizeZ,frames;public Burst[] bursts;public MeshData mesh;
            public Key[] velocityX,velocityY,velocityZ;
        }
        [Serializable] public class Config { public int schema;public Node[] nodes;public Layer[] layers;public string nativeEffect;public string[] nativeVariants;public float[] nativeTileEuler,nativeWorldEuler; }
        public const float Lifetime=1.8f;
        public float PlaybackDuration {get;private set;}=Lifetime;
        readonly List<ParticleSystem> systems=new List<ParticleSystem>();
        readonly List<Material> materials=new List<Material>();
        readonly List<Mesh> meshes=new List<Mesh>();
        float sampledAge;
        bool looping;
        NativeParticleEffect native;
        int nativeView;
        Quaternion nativeWorldRotation=Quaternion.identity;
        public bool HasFixedWorldRotation {get;private set;}
        public Quaternion TileMountRotation {get;private set;}
        public Quaternion TableRotation(Quaternion tileRotation)=>HasFixedWorldRotation?nativeWorldRotation:tileRotation*TileMountRotation;
        public bool UsesNativeSource=>native!=null;
        public float StartedAt {get;private set;}
        public bool SoundTriggered {get;set;}
        public int ParticleCount=>systems.Sum(p=>p.particleCount);
        public int LayerCount=>systems.Count;
        public int MeshLayerCount {get;private set;}
        public void SetOverlay(bool enabled){foreach(var material in materials){material.SetFloat("_ZTest",enabled?8:4);material.renderQueue=enabled?3100:3000;}}
        public void SetTileReadability(Transform tile,float intensity,float faceOpacity,bool enabled){CalloutReadability.Apply(materials,tile,intensity,faceOpacity,enabled);}
        public static int SourceLayerCount=>JsonUtility.FromJson<Config>(Resources.Load<TextAsset>("Majsoul/Actions/fire-effect").text).layers.Length;
        static Vector3 V(float[] v)=>new Vector3(v[0],v[1],v[2]);
        static Color C(float[] c)=>new Color(c[0],c[1],c[2],c[3]);
        // Laya gives emission its own random stream. Its float burst count is
        // consumed by `for (i=0; i<count; i++)`, so positive fractions round up.
        // Reproduce the original JS double multiply before the uint conversion.
        static uint SourceSeedStep(uint s)=>unchecked((uint)(ulong)(1812433253d*s+1d));
        static double SourceRandom(ref uint seed){
            uint s0=seed,s1=SourceSeedStep(s0),s2=SourceSeedStep(s1),s3=SourceSeedStep(s2),t=s0^(s0<<11);
            seed=s1;return ((s3^(s3>>19)^t^(t>>8))&8388607u)/8388607d;
        }
        static ParticleSystem.Burst[] SourceBursts(Layer layer,uint seed){
            uint state=unchecked((layer.sourceAutoSeed?seed:layer.sourceSeed)+592910910u);
            return layer.bursts.Select(b=>new ParticleSystem.Burst(b.time,(short)(layer.sourceBursts?Math.Ceiling(b.min+(b.max-b.min)*SourceRandom(ref state)):b.max))).ToArray();
        }
        static AnimationCurve Curve(Key[] keys){
            var c=new AnimationCurve(keys.Select(k=>new Keyframe(k.time,k.value)).ToArray());
            for(int i=0;i<keys.Length;i++){
                var k=c.keys[i];k.inTangent=i==0?0:(keys[i].value-keys[i-1].value)/(keys[i].time-keys[i-1].time);
                k.outTangent=i==keys.Length-1?0:(keys[i+1].value-keys[i].value)/(keys[i+1].time-keys[i].time);c.MoveKey(i,k);
            }
            return c;
        }
        Mesh SourceMesh(MeshData data){
            var m=new Mesh{name="Native fire particle mesh"};var vertices=new Vector3[data.positions.Length/3];var uv=new Vector2[vertices.Length];
            for(int i=0;i<vertices.Length;i++){vertices[i]=new Vector3(data.positions[i*3],data.positions[i*3+1],data.positions[i*3+2]);uv[i]=new Vector2(data.uv[i*2],data.uv[i*2+1]);}
            m.vertices=vertices;m.uv=uv;m.triangles=data.indices;m.RecalculateBounds();meshes.Add(m);return m;
        }
        public string SoundCue {get;set;}="ron_huolongjuan";
        public void Initialize(Vector3 point,float angle,float now,uint seed,string path="Majsoul/Actions/fire-effect",float scale=1)
        {
            StartedAt=now;transform.position=point+Vector3.up*.006f;transform.rotation=Quaternion.identity;transform.localScale=Vector3.one*TableTileGeometry.SourceUnits;
            transform.localScale*=scale;Build(path,"Majsoul/Actions/",seed);
            if(native!=null)transform.rotation=nativeWorldRotation;
        }
        public void InitializeUi(string path,float scale,float now,uint seed){StartedAt=now;transform.localScale=Vector3.one*scale;Build(path,"Majsoul/UiEffects/",seed);}
        public void InitializeTable(string path,Vector3 point,Quaternion rotation,float now,uint seed,bool repeat=false,float scale=1,int sourceView=0) {
            StartedAt=now;PlaybackDuration=0;transform.position=point;transform.rotation=rotation;looping=repeat;
            nativeView=sourceView;
            transform.localScale=Vector3.one*(TableTileGeometry.SourceUnits*scale);
            Build(path,"Majsoul/Actions/",seed);
            if(HasFixedWorldRotation)transform.rotation=nativeWorldRotation;
        }
        void Build(string path,string textures,uint seed)
        {
            var config=JsonUtility.FromJson<Config>(Resources.Load<TextAsset>(path).text);
            TileMountRotation=SourcePlaybackContracts.TileMount;
            if(!string.IsNullOrEmpty(config.nativeEffect)){
                TileMountRotation=config.nativeTileEuler?.Length==3?Quaternion.Euler(V(config.nativeTileEuler)):SourcePlaybackContracts.NativeTileMount;
                HasFixedWorldRotation=config.nativeWorldEuler?.Length==3;
                nativeWorldRotation=HasFixedWorldRotation?Quaternion.Euler(V(config.nativeWorldEuler)):Quaternion.identity;
                string source=config.nativeVariants!=null&&config.nativeVariants.Length==4?config.nativeVariants[(nativeView%4+4)%4]:config.nativeEffect;
                native=gameObject.AddComponent<NativeParticleEffect>();native.Initialize(source,seed);
                systems.AddRange(native.GetComponentsInChildren<ParticleSystem>(true));
                foreach(var renderer in native.GetComponentsInChildren<Renderer>(true)){
                    var local=renderer.sharedMaterials.Select(m=>new Material(m)).ToArray();renderer.sharedMaterials=local;materials.AddRange(local);
                }
                foreach(var ps in systems){
                    var main=ps.main;var emission=ps.emission;float last=emission.rateOverTime.constantMax>0?main.duration:0;
                    for(int i=0;i<emission.burstCount;i++){var burst=emission.GetBurst(i);last=Mathf.Max(last,burst.time+Mathf.Max(0,burst.cycleCount-1)*burst.repeatInterval);}
                    PlaybackDuration=Mathf.Max(PlaybackDuration,main.startDelay.constantMax+last+main.startLifetime.constantMax);
                }
                MeshLayerCount=native.GetComponentsInChildren<ParticleSystemRenderer>(true).Count(r=>r.renderMode==ParticleSystemRenderMode.Mesh)+native.GetComponentsInChildren<MeshRenderer>(true).Length;
                Sample(-1);return;
            }
            var plane=new Mesh{name="Native horizontal billboard",vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(.5f,0,-.5f)},uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right},triangles=new[]{0,1,2,0,2,3}};
            plane.RecalculateBounds();meshes.Add(plane);
            var nodes=new Transform[config.nodes.Length];
            for(int i=0;i<nodes.Length;i++){
                var n=config.nodes[i];var go=new GameObject(n.name);nodes[i]=go.transform;nodes[i].SetParent(n.parent<0?transform:nodes[n.parent],false);
                nodes[i].localPosition=V(n.position);nodes[i].localRotation=new Quaternion(n.rotation[0],n.rotation[1],n.rotation[2],n.rotation[3]);nodes[i].localScale=V(n.scale);
            }
            foreach(var l in config.layers){
                float lastEmission=Mathf.Max(l.rate>0?l.duration:0,l.bursts.Select(b=>b.time).DefaultIfEmpty(0).Max());
                PlaybackDuration=Mathf.Max(PlaybackDuration,l.delay+lastEmission+l.lifeMax);
                var emitter=nodes[l.node];
                if(config.schema>=2&&l.scaleMode==1){
                    // Laya Local scale ignores ancestors. Keep scene-unit conversion,
                    // but do not accidentally multiply the source parent's scale.
                    var leaf=new GameObject(l.name+" source-local emitter").transform;leaf.SetParent(transform,false);leaf.position=emitter.position;leaf.rotation=emitter.rotation;leaf.localScale=V(config.nodes[l.node].scale);emitter=leaf;
                }
                var p=emitter.gameObject.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);p.useAutoRandomSeed=false;p.randomSeed=seed+(uint)systems.Count+1;
                var m=p.main;m.playOnAwake=false;m.loop=looping;m.duration=l.duration;m.startDelay=l.delay;m.simulationSpace=ParticleSystemSimulationSpace.Local;m.scalingMode=ParticleSystemScalingMode.Hierarchy;
                m.startLifetime=new ParticleSystem.MinMaxCurve(l.lifeMin,l.lifeMax);m.startSpeed=new ParticleSystem.MinMaxCurve(l.speedMin,l.speedMax);m.startSize=new ParticleSystem.MinMaxCurve(l.sizeMin,l.sizeMax);
                if(config.schema>=2&&l.simulationSpace==0)m.simulationSpace=ParticleSystemSimulationSpace.World;
                // Source rotations are degrees; keep the full height of vertical flames.
                m.startRotation=new ParticleSystem.MinMaxCurve(l.rotationMin*Mathf.Deg2Rad,l.rotationMax*Mathf.Deg2Rad);m.startColor=new ParticleSystem.MinMaxGradient(C(l.colorMin),C(l.colorMax));
                if(l.rotation3d!=null&&l.rotation3d.Length==3){
                    var r=l.rotation3dMin!=null&&l.rotation3dMin.Length==3?l.rotation3dMin:l.rotation3d;m.startRotation3D=true;
                    m.startRotationX=new ParticleSystem.MinMaxCurve(r[0]*Mathf.Deg2Rad,l.rotation3d[0]*Mathf.Deg2Rad);m.startRotationY=new ParticleSystem.MinMaxCurve(r[1]*Mathf.Deg2Rad,l.rotation3d[1]*Mathf.Deg2Rad);m.startRotationZ=new ParticleSystem.MinMaxCurve(r[2]*Mathf.Deg2Rad,l.rotation3d[2]*Mathf.Deg2Rad);
                }else if(config.schema>=2&&l.renderMode==4){
                    // Laya's 1D mesh spin is about the authored local Z axis.
                    // Unity's scalar mesh rotation changes that basis; explicit
                    // XYZ keeps planar rings flat instead of flipping them.
                    m.startRotation3D=true;m.startRotationX=0;m.startRotationY=0;m.startRotationZ=new ParticleSystem.MinMaxCurve(l.rotationMin*Mathf.Deg2Rad,l.rotationMax*Mathf.Deg2Rad);
                }
                if(l.size3d.Length==3){m.startSize3D=true;var a=l.size3dMin.Length==3?l.size3dMin:l.size3d;m.startSizeX=new ParticleSystem.MinMaxCurve(Mathf.Min(a[0],l.size3d[0]),Mathf.Max(a[0],l.size3d[0]));m.startSizeY=new ParticleSystem.MinMaxCurve(Mathf.Min(a[1],l.size3d[1]),Mathf.Max(a[1],l.size3d[1]));m.startSizeZ=new ParticleSystem.MinMaxCurve(Mathf.Min(a[2],l.size3d[2]),Mathf.Max(a[2],l.size3d[2]));}
                if(config.schema>=2&&(l.renderMode==2||l.renderMode==3)){
                    // Both original billboard shader branches multiply corners by cos(45°).
                    var x=m.startSizeX; x.constantMin*=.70710678f;x.constantMax*=.70710678f;m.startSizeX=x;
                    if(m.startSize3D){var y=m.startSizeY;y.constantMin*=.70710678f;y.constantMax*=.70710678f;m.startSizeY=y;}
                }
                m.gravityModifier=l.gravity;m.maxParticles=l.maxParticles;var emission=p.emission;emission.rateOverTime=l.rate;emission.SetBursts(SourceBursts(l,p.randomSeed));
                var shape=p.shape;shape.enabled=l.shape>=0;
                if(config.schema>=2&&l.shape<0){shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=0;shape.radius=0;shape.rotation=new Vector3(0,180,0);}
                if(l.shape>=0){shape.shapeType=l.shape==2?ParticleSystemShapeType.Cone:ParticleSystemShapeType.Hemisphere;shape.radius=l.radius;shape.angle=l.coneAngle;shape.rotation=new Vector3(0,180,0);
                    if(l.nativeShape){shape.shapeType=l.shape==0?ParticleSystemShapeType.Sphere:l.shape==1?ParticleSystemShapeType.Hemisphere:l.shape==2?ParticleSystemShapeType.Cone:l.shape==3?ParticleSystemShapeType.Box:ParticleSystemShapeType.Circle;shape.randomDirectionAmount=l.randomDirection;if(l.box!=null && l.box.Length==3)shape.scale=V(l.box);}}
                // Unity's box keeps its +Z emission direction when the volume
                // rotates. Reflect speed explicitly for Laya's +Z -> Unity -Z.
                if(config.schema>=2&&l.shape==3){shape.rotation=Vector3.zero;m.startSpeed=new ParticleSystem.MinMaxCurve(-l.speedMax,-l.speedMin);}
                if(l.velocityType==1&&l.velocityX?.Length>0){
                    var vel=p.velocityOverLifetime;vel.enabled=true;vel.space=l.velocitySpace==1?ParticleSystemSimulationSpace.World:ParticleSystemSimulationSpace.Local;
                    vel.x=new ParticleSystem.MinMaxCurve(1,Curve(l.velocityX));vel.y=new ParticleSystem.MinMaxCurve(1,Curve(l.velocityY));vel.z=new ParticleSystem.MinMaxCurve(-1,Curve(l.velocityZ));
                }else if(l.velocityMin.Length==3){var vel=p.velocityOverLifetime;vel.enabled=true;vel.space=l.velocitySpace==1?ParticleSystemSimulationSpace.World:ParticleSystemSimulationSpace.Local;vel.x=new ParticleSystem.MinMaxCurve(l.velocityMin[0],l.velocityMax[0]);vel.y=new ParticleSystem.MinMaxCurve(l.velocityMin[1],l.velocityMax[1]);vel.z=new ParticleSystem.MinMaxCurve(-l.velocityMax[2],-l.velocityMin[2]);}
                if(l.alpha.Length>0){var col=p.colorOverLifetime;col.enabled=true;var g=new Gradient();g.SetKeys(l.colors!=null && l.colors.Length>0?l.colors.Select(k=>new GradientColorKey(C(k.color),k.time)).ToArray():new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},l.alpha.Select(k=>new GradientAlphaKey(k.value,k.time)).ToArray());col.color=g;}
                if(l.size.Length>0){var size=p.sizeOverLifetime;size.enabled=true;
                    if(l.sizeX.Length>0){size.separateAxes=true;size.x=new ParticleSystem.MinMaxCurve(1,Curve(l.sizeX));size.y=new ParticleSystem.MinMaxCurve(1,Curve(l.sizeY));size.z=new ParticleSystem.MinMaxCurve(1,Curve(l.sizeZ));}
                    else size.size=new ParticleSystem.MinMaxCurve(1,Curve(l.size));}
                if(l.tiles[0]>1||l.tiles[1]>1){var sheet=p.textureSheetAnimation;sheet.enabled=true;sheet.numTilesX=(int)l.tiles[0];sheet.numTilesY=(int)l.tiles[1];sheet.frameOverTime=l.randomFrame?new ParticleSystem.MinMaxCurve(l.frameMin,l.frameMax>0?l.frameMax:.9999f):new ParticleSystem.MinMaxCurve(1,l.frames.Length>0?Curve(l.frames):AnimationCurve.Linear(0,0,1,.9999f));sheet.cycleCount=l.cycles;sheet.startFrame=l.startFrame;}
                if(Mathf.Abs(l.angularSpeed)>.001f||Mathf.Abs(l.angularMin)>.001f||Mathf.Abs(l.angularMax)>.001f){var rotation=p.rotationOverLifetime;rotation.enabled=true;if(config.schema>=2&&l.renderMode==4){rotation.separateAxes=true;rotation.x=0;rotation.y=0;}rotation.z=config.schema>=2?new ParticleSystem.MinMaxCurve(-l.angularMax*Mathf.Deg2Rad,-l.angularMin*Mathf.Deg2Rad):new ParticleSystem.MinMaxCurve(-l.angularSpeed*Mathf.Deg2Rad);}
                var renderer=p.GetComponent<ParticleSystemRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.enableGPUInstancing=false;
                renderer.renderMode=l.renderMode==4?ParticleSystemRenderMode.Mesh:l.renderMode==2?(config.schema>=2?ParticleSystemRenderMode.HorizontalBillboard:ParticleSystemRenderMode.Mesh):l.renderMode==1?ParticleSystemRenderMode.Stretch:l.renderMode==3?ParticleSystemRenderMode.VerticalBillboard:ParticleSystemRenderMode.Billboard;
                if(l.renderMode==4){renderer.mesh=SourceMesh(l.mesh);renderer.alignment=ParticleSystemRenderSpace.Local;MeshLayerCount++;}
                else if(l.renderMode==2){if(config.schema<2)renderer.mesh=plane;renderer.alignment=ParticleSystemRenderSpace.World;}
                else if(l.renderMode==3)renderer.alignment=ParticleSystemRenderSpace.World;
                else renderer.alignment=ParticleSystemRenderSpace.View;
                if(config.schema>=2)renderer.maxParticleSize=100;
                // BakeMesh confirms Stretch already uses the original head
                // anchor and texture-U long axis. Keep its zero pivot and UVs.
                renderer.lengthScale=l.stretch;renderer.velocityScale=0;renderer.cameraVelocityScale=0;renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV});
                // A name with a folder ("Actions/win-shanguang") reuses a texture from another Majsoul folder.
                var material=new Material(Shader.Find("YunJian/WinParticle")){mainTexture=Resources.Load<Texture2D>(l.texture.Contains("/")?"Majsoul/"+l.texture:textures+l.texture)};material.SetColor("_TintColor",C(l.tint));material.SetFloat("_SrcBlend",l.blendSrc==1?1:5);material.SetFloat("_DstBlend",l.blendDst==1?1:10);renderer.sharedMaterial=material;materials.Add(material);systems.Add(p);
            }
            Sample(-1);
        }
        public void Sample(float age){
            if(native!=null){native.Sample(age);return;}
            if(age<0){foreach(var p in systems)p.Clear();sampledAge=0;return;}
            age=Mathf.Max(.0001f,age);bool restart=sampledAge==0 || age<sampledAge;foreach(var p in systems)p.Simulate(restart?age:age-sampledAge,false,restart,false);sampledAge=age;
        }
        void OnDestroy(){foreach(var m in materials)Destroy(m);foreach(var m in meshes)Destroy(m);}
    }
}
