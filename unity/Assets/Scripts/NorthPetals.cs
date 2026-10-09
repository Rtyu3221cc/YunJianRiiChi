using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YunJian.Riichi
{
    // The APK's 4x4 yinghua2 atlas supplies the petal silhouettes and flutter frames.
    // This short burst is adapted to a North tile; the lobby emitter is not copied.
    public sealed class NorthPetals : MonoBehaviour
    {
        public const float Lifetime = 1.65f;
        ParticleSystem particles;
        Material material;
        float sampledAge;
        public float StartedAt { get; private set; }
        public int ParticleCount => particles == null ? 0 : particles.particleCount;

        public void Initialize(Vector3 point, float seatAngle, float startedAt, uint seed)
        {
            StartedAt = startedAt;
            transform.position = point + Vector3.up * .14f;
            transform.rotation = Quaternion.Euler(0, seatAngle, 0);
            particles = gameObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.useAutoRandomSeed = false; particles.randomSeed = seed == 0 ? 1u : seed;
            var main = particles.main;
            main.playOnAwake = false; main.loop = false; main.duration = .1f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.5f);
            main.startSpeed = 0; main.startSize = new ParticleSystem.MinMaxCurve(.13f, .23f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            main.startColor = Color.white; main.maxParticles = 12;
            var emission = particles.emission; emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 9) });
            var shape = particles.shape; shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .12f;
            var velocity = particles.velocityOverLifetime; velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-.23f, .23f);
            velocity.y = new ParticleSystem.MinMaxCurve(.48f, .85f);
            velocity.z = new ParticleSystem.MinMaxCurve(-.17f, .17f);
            var force = particles.forceOverLifetime; force.enabled = true;
            force.space = ParticleSystemSimulationSpace.Local;
            force.y = -.55f;
            var rotation = particles.rotationOverLifetime; rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-1.8f, 1.8f);
            var color = particles.colorOverLifetime; color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .08f), new GradientAlphaKey(1, .55f), new GradientAlphaKey(0, 1) });
            color.color = gradient;
            var sheet = particles.textureSheetAnimation; sheet.enabled = true;
            sheet.numTilesX = 4; sheet.numTilesY = 4; sheet.animation = ParticleSystemAnimationType.WholeSheet;
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 0, 1, 1));
            sheet.startFrame = new ParticleSystem.MinMaxCurve(0, 1); sheet.cycleCount = 2;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View; renderer.enableGPUInstancing = false;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream> {
                ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV });
            material = new Material(Shader.Find("YunJian/NorthPetal")) {
                mainTexture = Resources.Load<Texture2D>("Majsoul/Actions/north-petals") };
            renderer.sharedMaterial = material;
            // Manual, unscaled simulation shares the hand's clock and can be frozen in QA.
            Sample(0);
        }
        public void Sample(float age)
        {
            age = Mathf.Max(.0001f, age);
            bool restart = sampledAge == 0 || age < sampledAge;
            particles.Simulate(restart ? age : age - sampledAge, false, restart, false);
            sampledAge = age;
        }
        void OnDestroy() { if (material != null) Destroy(material); }
    }
}
