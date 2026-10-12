using UnityEngine;

namespace YunJian.Riichi
{
    // scene_mj/room/sun_shine: the APK's three additive light-streak and dust emitters,
    // imported as whole serialized modules (Editor/ImportNativeReview.cs, TableLook).
    public sealed class SunShine : MonoBehaviour
    {
        public ParticleSystem[] Particles { get; private set; } = new ParticleSystem[0];
        void Awake()
        {
            var prefab = Resources.Load<GameObject>("Majsoul/TableLook/sun_shine");
            if (prefab == null) { Debug.LogWarning("Missing imported APK prefab TableLook/sun_shine"); return; }
            // Source scene axes map to ours by a half turn about Y, scaled to table units.
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 180, 0));
            transform.localScale = Vector3.one * TableLook.SourceUnits;
            var clone = Instantiate(prefab, transform, false);
            Particles = clone.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var p in Particles)
            {
                // Source modules use Local scaling; the unit conversion lives on our parent.
                var main = p.main; main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                p.Play(false);
            }
        }
    }
}
