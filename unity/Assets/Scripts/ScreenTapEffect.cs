using System.Collections.Generic;
using UnityEngine;

namespace YunJian.Riichi
{
    // Majsoul's default tap effect (scene/effect_click_default.lh): 6-12 sakura petals and a
    // pink flash. scene_click_effect.ls shows it through an orthographic camera 18 units
    // high; RiichiClient renders that camera last in OnGUI so the petals sit above the HUD.
    public sealed class ScreenTapEffect : MonoBehaviour
    {
        const int Layer = 28;
        public const float Lifetime = 1.5f;
        Camera eye;
        Transform root;
        readonly List<WinTileEffect> live = new List<WinTileEffect>();
        uint seed = 17;
        public int ActiveCount => live.Count;
        public int ParticleCount { get { int n = 0; foreach (var e in live) n += e.ParticleCount; return n; } }
        void Awake()
        {
            root = new GameObject("Screen tap effects").transform; root.position = new Vector3(0, -8000, 0);
            eye = new GameObject("Screen tap camera").AddComponent<Camera>(); eye.transform.SetParent(root, false);
            // Laya z is mirrored on import, so the source camera at z=-30 looks down -z here.
            eye.transform.localPosition = new Vector3(0, 0, 30); eye.transform.localRotation = Quaternion.Euler(0, 180, 0);
            eye.orthographic = true; eye.orthographicSize = 9; eye.clearFlags = CameraClearFlags.Nothing;
            eye.cullingMask = 1 << Layer; eye.nearClipPlane = .3f; eye.farClipPlane = 100; eye.allowMSAA = false; eye.enabled = false;
        }
        // Rendering inside OnGUI does not keep the screen's aspect; pin it for both mapping and drawing.
        void FitScreen() { eye.rect = new Rect(0, 0, 1, 1); eye.aspect = Screen.width / (float)Mathf.Max(1, Screen.height); }
        public void Play(Vector2 screen)
        {
            FitScreen();
            var effect = new GameObject("Tap sakura").AddComponent<WinTileEffect>(); effect.transform.SetParent(root, false);
            var p = eye.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 30)); effect.transform.position = new Vector3(p.x, p.y, root.position.z);
            effect.InitializeUi("Majsoul/UiEffects/effect_click_default", 1, Time.unscaledTime, seed += 7);
            foreach (var t in effect.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;
            live.Add(effect);
        }
        void LateUpdate()
        {
            float now = Time.unscaledTime;
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var e = live[i]; float age = e == null ? Lifetime : now - e.StartedAt;
                if (age >= Lifetime) { if (e != null) { e.gameObject.SetActive(false); Destroy(e.gameObject); } live.RemoveAt(i); }
                else e.Sample(age);
            }
        }
        // Called at the end of the GUI pass; draws over everything already on screen.
        public void Render() { if (live.Count > 0 && Event.current.type == EventType.Repaint) { FitScreen(); eye.Render(); } }
#if UNITY_EDITOR
        // Screen position (bottom-left origin) where a live effect's centre is drawn.
        public Vector2 ValidationScreenPoint(int index) { FitScreen(); return eye.WorldToScreenPoint(live[index].transform.position); }
#endif
        public void Clear() { foreach (var e in live) if (e != null) Destroy(e.gameObject); live.Clear(); }
        void OnDestroy() { if (root != null) Destroy(root.gameObject); }
    }
}
