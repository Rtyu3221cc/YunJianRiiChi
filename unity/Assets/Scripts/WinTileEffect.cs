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
            public ColorKey[] colors;public float[] box;public bool nativeShape;public float randomDirection;
            public string name,texture;public int node,blendDst,maxParticles,renderMode,shape,cycles;
            public float[] colorMin,colorMax,tint,tiles,size3d,size3dMin,velocityMin,velocityMax;
            public float angularSpeed;public float duration,delay,lifeMin,lifeMax,sizeMin,sizeMax,speedMin,speedMax,rotationMin,rotationMax,gravity,rate,stretch,radius,coneAngle;
            public bool randomFrame;public Key[] alpha,size,sizeX,sizeY,sizeZ,frames;public Burst[] bursts;public MeshData mesh;
        }
        [Serializable] public class Config { public Node[] nodes;public Layer[] layers; }
        public const float Lifetime=1.8f;
        readonly List<ParticleSystem> systems=new List<ParticleSystem>();
        readonly List<Material> materials=new List<Material>();
        readonly List<Mesh> meshes=new List<Mesh>();
        float sampledAge;
        public float StartedAt {get;private set;}
        public bool SoundTriggered {get;set;}
        public int ParticleCount=>systems.Sum(p=>p.particleCount);
        public int LayerCount=>systems.Count;
        public int MeshLayerCount {get;private set;}
        public static int SourceLayerCount=>JsonUtility.FromJson<Config>(Resources.Load<TextAsset>("Majsoul/Actions/fire-effect").text).layers.Length;
        static Vector3 V(float[] v)=>new Vector3(v[0],v[1],v[2]);
        static Color C(float[] c)=>new Color(c[0],c[1],c[2],c[3]);
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
        public void Initialize(Vector3 point,float angle,float now,uint seed)
        {
            StartedAt=now;transform.position=point+Vector3.up*.045f;var camera=GetComponentInParent<TableRenderer>()?.Camera;float yaw=camera==null?angle:Mathf.Atan2(point.x-camera.transform.position.x,point.z-camera.transform.position.z)*Mathf.Rad2Deg;transform.rotation=Quaternion.Euler(0,yaw,0);transform.localScale=Vector3.one*(.33f/.02947767f);
            Build("Majsoul/Actions/fire-effect","Majsoul/Actions/",seed);
        }
        public void InitializeUi(string path,float scale,float now,uint seed){StartedAt=now;transform.localScale=Vector3.one*scale;Build(path,"Majsoul/UiEffects/",seed);}
        void Build(string path,string textures,uint seed)
        {
            var plane=new Mesh{name="Native horizontal billboard",vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(.5f,0,-.5f)},uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right},triangles=new[]{0,1,2,0,2,3}};
            plane.RecalculateBounds();meshes.Add(plane);
            var config=JsonUtility.FromJson<Config>(Resources.Load<TextAsset>(path).text);var nodes=new Transform[config.nodes.Length];
            for(int i=0;i<nodes.Length;i++){
                var n=config.nodes[i];var go=new GameObject(n.name);nodes[i]=go.transform;nodes[i].SetParent(n.parent<0?transform:nodes[n.parent],false);
                nodes[i].localPosition=V(n.position);nodes[i].localRotation=new Quaternion(n.rotation[0],n.rotation[1],n.rotation[2],n.rotation[3]);nodes[i].localScale=V(n.scale);
            }
            foreach(var l in config.layers){
                var p=nodes[l.node].gameObject.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);p.useAutoRandomSeed=false;p.randomSeed=seed+(uint)systems.Count+1;
                var m=p.main;m.playOnAwake=false;m.loop=false;m.duration=l.duration;m.startDelay=l.delay;m.simulationSpace=ParticleSystemSimulationSpace.Local;m.scalingMode=ParticleSystemScalingMode.Hierarchy;
                m.startLifetime=new ParticleSystem.MinMaxCurve(l.lifeMin,l.lifeMax);m.startSpeed=new ParticleSystem.MinMaxCurve(l.speedMin,l.speedMax);m.startSize=new ParticleSystem.MinMaxCurve(l.sizeMin,l.sizeMax);
                // Source rotations are degrees; keep the full height of vertical flames.
                m.startRotation=new ParticleSystem.MinMaxCurve(l.rotationMin*Mathf.Deg2Rad,l.rotationMax*Mathf.Deg2Rad);m.startColor=new ParticleSystem.MinMaxGradient(C(l.colorMin),C(l.colorMax));
                if(l.size3d.Length==3){m.startSize3D=true;var a=l.size3dMin.Length==3?l.size3dMin:l.size3d;m.startSizeX=new ParticleSystem.MinMaxCurve(Mathf.Min(a[0],l.size3d[0]),Mathf.Max(a[0],l.size3d[0]));m.startSizeY=new ParticleSystem.MinMaxCurve(Mathf.Min(a[1],l.size3d[1]),Mathf.Max(a[1],l.size3d[1]));m.startSizeZ=new ParticleSystem.MinMaxCurve(Mathf.Min(a[2],l.size3d[2]),Mathf.Max(a[2],l.size3d[2]));}
                m.gravityModifier=l.gravity;m.maxParticles=l.maxParticles;var emission=p.emission;emission.rateOverTime=l.rate;emission.SetBursts(l.bursts.Select(b=>new ParticleSystem.Burst(b.time,(short)b.max)).ToArray());
                var shape=p.shape;shape.enabled=l.shape>=0;
                if(shape.enabled){shape.shapeType=l.shape==2?ParticleSystemShapeType.Cone:ParticleSystemShapeType.Hemisphere;shape.radius=l.radius;shape.angle=l.coneAngle;shape.rotation=new Vector3(0,180,0);
                    if(l.nativeShape){shape.shapeType=l.shape==0?ParticleSystemShapeType.Sphere:l.shape==1?ParticleSystemShapeType.Hemisphere:l.shape==2?ParticleSystemShapeType.Cone:l.shape==3?ParticleSystemShapeType.Box:ParticleSystemShapeType.Circle;shape.randomDirectionAmount=l.randomDirection;if(l.box!=null && l.box.Length==3)shape.scale=V(l.box);}}
                if(l.velocityMin.Length==3){var vel=p.velocityOverLifetime;vel.enabled=true;vel.space=ParticleSystemSimulationSpace.Local;vel.x=new ParticleSystem.MinMaxCurve(l.velocityMin[0],l.velocityMax[0]);vel.y=new ParticleSystem.MinMaxCurve(l.velocityMin[1],l.velocityMax[1]);vel.z=new ParticleSystem.MinMaxCurve(-l.velocityMax[2],-l.velocityMin[2]);}
                if(l.alpha.Length>0){var col=p.colorOverLifetime;col.enabled=true;var g=new Gradient();g.SetKeys(l.colors!=null && l.colors.Length>0?l.colors.Select(k=>new GradientColorKey(C(k.color),k.time)).ToArray():new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},l.alpha.Select(k=>new GradientAlphaKey(k.value,k.time)).ToArray());col.color=g;}
                if(l.size.Length>0){var size=p.sizeOverLifetime;size.enabled=true;
                    if(l.sizeX.Length>0){size.separateAxes=true;size.x=new ParticleSystem.MinMaxCurve(1,Curve(l.sizeX));size.y=new ParticleSystem.MinMaxCurve(1,Curve(l.sizeY));size.z=new ParticleSystem.MinMaxCurve(1,Curve(l.sizeZ));}
                    else size.size=new ParticleSystem.MinMaxCurve(1,Curve(l.size));}
                if(l.tiles[0]>1){var sheet=p.textureSheetAnimation;sheet.enabled=true;sheet.numTilesX=(int)l.tiles[0];sheet.numTilesY=(int)l.tiles[1];sheet.frameOverTime=l.randomFrame?new ParticleSystem.MinMaxCurve(0,.9999f):new ParticleSystem.MinMaxCurve(1,l.frames.Length>0?Curve(l.frames):AnimationCurve.Linear(0,0,1,.9999f));sheet.cycleCount=l.cycles;}
                if(Mathf.Abs(l.angularSpeed)>.001f){var rotation=p.rotationOverLifetime;rotation.enabled=true;rotation.z=-l.angularSpeed*Mathf.Deg2Rad;}
                var renderer=p.GetComponent<ParticleSystemRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.enableGPUInstancing=false;
                renderer.renderMode=l.renderMode==4 || l.renderMode==2?ParticleSystemRenderMode.Mesh:l.renderMode==1?ParticleSystemRenderMode.Stretch:l.renderMode==3?ParticleSystemRenderMode.VerticalBillboard:ParticleSystemRenderMode.Billboard;
                if(l.renderMode==4){renderer.mesh=SourceMesh(l.mesh);renderer.alignment=ParticleSystemRenderSpace.Local;MeshLayerCount++;}
                else if(l.renderMode==2){renderer.mesh=plane;renderer.alignment=ParticleSystemRenderSpace.World;}
                else renderer.alignment=ParticleSystemRenderSpace.View;
                renderer.lengthScale=l.stretch;renderer.velocityScale=0;renderer.cameraVelocityScale=0;renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV});
                // A name with a folder ("Actions/win-shanguang") reuses a texture from another Majsoul folder.
                var material=new Material(Shader.Find("YunJian/WinParticle")){mainTexture=Resources.Load<Texture2D>(l.texture.Contains("/")?"Majsoul/"+l.texture:textures+l.texture)};material.SetColor("_TintColor",C(l.tint));material.SetFloat("_DstBlend",l.blendDst==1?1:10);renderer.sharedMaterial=material;materials.Add(material);systems.Add(p);
            }
            Sample(-1);
        }
        public void Sample(float age){
            if(age<0){foreach(var p in systems)p.Clear();sampledAge=0;return;}
            age=Mathf.Max(.0001f,age);bool restart=sampledAge==0 || age<sampledAge;foreach(var p in systems)p.Simulate(restart?age:age-sampledAge,false,restart,false);sampledAge=age;
        }
        void OnDestroy(){foreach(var m in materials)Destroy(m);foreach(var m in meshes)Destroy(m);}
    }
}
