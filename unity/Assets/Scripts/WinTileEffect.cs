using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace YunJian.Riichi
{
    // Original fire curves with a video-matched flame spiral: eight particle layers, original textures, additive tint,
    // lifetimes, emission and curves. Coordinates adapt to the native Unity table.
    public sealed class WinTileEffect : MonoBehaviour
    {
        [Serializable] public class Key { public float time, value; }
        [Serializable] public class Burst { public float time; public int min, max; }
        [Serializable] public class Layer {
            public string name, texture;
            public float[] position, rotation, color, colorMin, colorMax, tint, tiles, size3d, velocityMin, velocityMax;
            public int blendDst; public bool randomFrame;
            public float duration, delay, lifeMin, lifeMax, sizeMin, sizeMax, speedMin, speedMax,
                rotationMin, rotationMax, gravity, rate, stretch, radius, coneAngle;
            public int maxParticles, renderMode, shape; public uint seed;
            public Key[] alpha, size; public Burst[] bursts;
        }
        [Serializable] public class Config { public Layer[] layers; }
        public const float Lifetime = 1.8f;
        readonly List<ParticleSystem> systems = new List<ParticleSystem>();
        readonly List<Material> materials = new List<Material>();
        Mesh plane; float sampledAge; FlameSpiral spiral;
        public float StartedAt { get; private set; }
        public int ParticleCount => systems.Sum(p => p.particleCount);
        public int LayerCount => systems.Count;
        static Color ColorOf(float[] c) => new Color(c[0], c[1], c[2], c[3]);
        static AnimationCurve Curve(Key[] keys) {
            var curve = new AnimationCurve(keys.Select(k => new Keyframe(k.time,k.value)).ToArray());
            for(int i=0;i<keys.Length;i++) {
                var k=curve.keys[i];
                k.inTangent=i==0?0:(keys[i].value-keys[i-1].value)/(keys[i].time-keys[i-1].time);
                k.outTangent=i==keys.Length-1?0:(keys[i+1].value-keys[i].value)/(keys[i+1].time-keys[i].time);
                curve.MoveKey(i,k);
            }
            return curve;
        }
        public void Initialize(Vector3 point,float angle,float now,uint seed)
        {
            StartedAt=now; transform.position=point+Vector3.up*.045f;
            transform.rotation=Quaternion.Euler(0,angle,0); transform.localScale=Vector3.one*2;
            plane=new Mesh {name="Original win ground particle",vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(.5f,0,-.5f)},uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right},triangles=new[]{0,1,2,0,2,3}};
            plane.RecalculateBounds();
            var config=JsonUtility.FromJson<Config>(Resources.Load<TextAsset>("Majsoul/Actions/fire-effect").text);
            foreach(var l in config.layers) {
                var go=new GameObject(l.name);go.transform.SetParent(transform,false);
                go.transform.localPosition=new Vector3(l.position[0],l.position[1],l.position[2]);
                // Laya is right-handed; mirror the emitter orientation across Z.
                if(l.renderMode!=2)go.transform.localRotation=Quaternion.identity;
                var p=go.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                p.useAutoRandomSeed=false;p.randomSeed=seed+(uint)systems.Count+1;
                var m=p.main;m.playOnAwake=false;m.loop=false;m.duration=l.duration;m.startDelay=l.delay;
                m.simulationSpace=ParticleSystemSimulationSpace.Local;m.scalingMode=ParticleSystemScalingMode.Hierarchy;
                m.startLifetime=new ParticleSystem.MinMaxCurve(l.lifeMin,l.lifeMax);
                m.startSize=new ParticleSystem.MinMaxCurve(l.sizeMin,l.sizeMax);
                m.startSpeed=new ParticleSystem.MinMaxCurve(l.speedMin,l.speedMax);
                m.startRotation=new ParticleSystem.MinMaxCurve(l.rotationMin*Mathf.Deg2Rad,l.rotationMax*Mathf.Deg2Rad);
                m.startColor=new ParticleSystem.MinMaxGradient(ColorOf(l.colorMin),ColorOf(l.colorMax));
                if(l.size3d.Length==3){m.startSize3D=true;m.startSizeX=l.size3d[0];m.startSizeY=Mathf.Min(.5f,l.size3d[1]);go.transform.localPosition+=Vector3.up*.25f;m.startSizeZ=l.size3d[2];}m.gravityModifier=l.gravity;m.maxParticles=l.maxParticles;
                var emission=p.emission;emission.rateOverTime=l.rate;
                // Each committed win emits the configured burst, including one ground flash.
                emission.SetBursts(l.bursts.Select(b=>new ParticleSystem.Burst(b.time,(short)b.max)).ToArray());
                var shape=p.shape;shape.enabled=l.shape>=0;
                if(shape.enabled){shape.shapeType=l.shape==2?ParticleSystemShapeType.Cone:ParticleSystemShapeType.Hemisphere;shape.radius=l.radius;shape.angle=l.coneAngle;shape.rotation=new Vector3(-90,0,0);}
                if(l.velocityMin.Length==3){var vel=p.velocityOverLifetime;vel.enabled=true;vel.space=ParticleSystemSimulationSpace.Local;vel.x=new ParticleSystem.MinMaxCurve(l.velocityMin[0],l.velocityMax[0]);vel.y=new ParticleSystem.MinMaxCurve(l.velocityMin[2],l.velocityMax[2]);vel.z=new ParticleSystem.MinMaxCurve(-l.velocityMax[1],-l.velocityMin[1]);}
                if(l.alpha.Length>0){var col=p.colorOverLifetime;col.enabled=true;var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},l.alpha.Select(k=>new GradientAlphaKey(k.value,k.time)).ToArray());col.color=g;}
                if(l.size.Length>0){var size=p.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,Curve(l.size));}
                if(l.tiles[0]>1){var sheet=p.textureSheetAnimation;sheet.enabled=true;sheet.numTilesX=(int)l.tiles[0];sheet.numTilesY=(int)l.tiles[1];sheet.frameOverTime=l.randomFrame?new ParticleSystem.MinMaxCurve(0,.9999f):new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,0,1,.9999f));sheet.cycleCount=1;}
                var renderer=p.GetComponent<ParticleSystemRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.enableGPUInstancing=false;
                renderer.renderMode=l.renderMode==2?ParticleSystemRenderMode.Mesh:l.renderMode==1?ParticleSystemRenderMode.Stretch:l.renderMode==3?ParticleSystemRenderMode.VerticalBillboard:ParticleSystemRenderMode.Billboard;
                if(l.renderMode==2){renderer.mesh=plane;renderer.alignment=ParticleSystemRenderSpace.Local;}else renderer.alignment=ParticleSystemRenderSpace.View;
                renderer.lengthScale=l.stretch;renderer.velocityScale=0;renderer.cameraVelocityScale=0;
                renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV});
                var material=new Material(Shader.Find("YunJian/WinParticle")){mainTexture=Resources.Load<Texture2D>("Majsoul/Actions/"+l.texture)};
                material.SetColor("_TintColor",ColorOf(l.tint));material.SetFloat("_DstBlend",l.blendDst==1?1:10);renderer.sharedMaterial=material;
                systems.Add(p);materials.Add(material);
            }
            spiral=gameObject.AddComponent<FlameSpiral>();spiral.Initialize();
            Sample(-1);
        }
        public void Sample(float age) {
            if(age<0){foreach(var p in systems)p.Clear();spiral.Sample(age);sampledAge=0;return;}
            spiral.Sample(age);age=Mathf.Max(.0001f,age);bool restart=sampledAge==0 || age<sampledAge;
            foreach(var p in systems)p.Simulate(restart?age:age-sampledAge,false,restart,false);
            sampledAge=age;
        }
        void OnDestroy(){foreach(var m in materials)Destroy(m);if(plane!=null)Destroy(plane);}
    }
}
