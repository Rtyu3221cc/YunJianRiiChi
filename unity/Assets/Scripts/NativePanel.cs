using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi
{
    // Draw the original raster art without changing its intrinsic transparency.
    public static class NativePanel
    {
        [Serializable] class Border { public string name; public float[] border; }
        [Serializable] class Borders { public Border[] entries; }
        static readonly Dictionary<string, Texture2D> art = new Dictionary<string, Texture2D>();
        static Borders borders;
        static Font brush, body, desc;
        static readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();
        public static readonly Color Gold = new Color(.9294f, .698f, .4353f), Text = new Color(.8314f, .8118f, .8118f), Muted = new Color(.5529f, .6f, .7333f);
        public static Texture2D Art(string name) { if (!art.TryGetValue(name, out var t)) art[name] = t = Resources.Load<Texture2D>("Majsoul/Settings/Sprites/" + name); return t; }
        public static void Image(string name, Rect r) { var t = Art(name); if (t != null && Event.current.type == EventType.Repaint) GUI.DrawTexture(r, t); }
        public static void Box(Rect r, Color c) { var old = GUI.color; GUI.color *= c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
        public static void ScreenShade(Color color) {
            // Backdrops cover the physical display, including cutout margins.
            // The dialog itself keeps using the safe-area canvas.
            var matrix = GUI.matrix; GUI.matrix = Matrix4x4.identity;
            Box(new Rect(0, 0, Screen.width, Screen.height), color); GUI.matrix = matrix;
        }
        public static void Slice(string name, Rect r, params float[] border) {
            if (Event.current.type != EventType.Repaint) return;
            var t = Art(name); if (t == null) return;
            if (border.Length == 0) {
                if (borders == null) borders = JsonUtility.FromJson<Borders>(Resources.Load<TextAsset>("Majsoul/Settings/art").text);
                border = borders.entries.FirstOrDefault(b => b.name == name)?.border ?? new float[4];
            }
            if (border.Length != 4 || border.All(v => v == 0)) { Image(name, r); return; }
            float l = Mathf.Min(border[0], r.width / 2), b = Mathf.Min(border[1], r.height / 2), rr = Mathf.Min(border[2], r.width / 2), top = Mathf.Min(border[3], r.height / 2);
            float[] x = { r.x, r.x + l, r.xMax - rr, r.xMax }, y = { r.y, r.y + top, r.yMax - b, r.yMax }, u = { 0, border[0] / t.width, 1 - border[2] / t.width, 1 }, v = { 0, border[3] / t.height, 1 - border[1] / t.height, 1 };
            // Unity rounds each GUI quad separately. Share physical pixel edges
            // so a fractional phone scale cannot open dark seams between slices.
            var matrix = GUI.matrix;
            for (int i = 0; i < 4; i++) { x[i] = Pixel(x[i], matrix.m00, matrix.m03); y[i] = Pixel(y[i], matrix.m11, matrix.m13); }
            for (int j = 0; j < 3; j++) for (int i = 0; i < 3; i++)
                if (u[i + 1] > u[i] && v[j + 1] > v[j]) GUI.DrawTextureWithTexCoords(Rect.MinMaxRect(x[i], y[j], x[i + 1], y[j + 1]), t, new Rect(u[i], 1 - v[j + 1], u[i + 1] - u[i], v[j + 1] - v[j]));
        }
        public static void Label(string value, float x, float y, int size = 34, Color? color = null, int font = 0, TextAnchor anchor = TextAnchor.MiddleLeft, float width = 1500, float height = 80, bool wrap = false) {
            if (brush == null) { brush = Resources.Load<Font>("Majsoul/Lobby/Fonts/brush"); body = Resources.Load<Font>("Majsoul/Settings/body"); desc = Resources.Load<Font>("Majsoul/Settings/description"); }
            string key = font + ":" + size + ":" + anchor + ":" + wrap;
            if (!styles.TryGetValue(key, out var s)) styles[key] = s = new GUIStyle { font = font == 1 ? brush : font == 2 ? desc : body, fontSize = size, alignment = anchor, clipping = TextClipping.Overflow, wordWrap = wrap, richText = false, padding = new RectOffset(0, 0, 0, 0) };
            s.normal.textColor = color ?? Text;
            float left = anchor == TextAnchor.MiddleCenter ? x - width / 2 : anchor == TextAnchor.MiddleRight ? x - width : x;
            bool enabled = GUI.enabled; GUI.enabled = true; GUI.Label(new Rect(left, y - height / 2, width, height), value, s); GUI.enabled = enabled;
        }
        public static bool Hit(Rect r) => GUI.Button(r, GUIContent.none, GUIStyle.none);
        static float Pixel(float value, float scale, float offset) => Mathf.Abs(scale) < .0001f ? value : (Mathf.Round(value * scale + offset) - offset) / scale;
        public static float BackOut(float t) { t = Mathf.Clamp01(t) - 1; return 1 + 2.70158f * t * t * t + 1.70158f * t * t; }
        public static void Popup(float width, float began) {
            float p = Mathf.Clamp01((Time.unscaledTime - began) / .15f), scale = .6f + .4f * BackOut(p);
            GUI.matrix *= Matrix4x4.TRS(new Vector3(width / 2 * (1 - scale), 540 * (1 - scale)), Quaternion.identity, new Vector3(scale, scale, 1));
            GUI.color *= new Color(1, 1, 1, Mathf.Clamp01(BackOut(p)));
        }
        public static void Frame(Rect r) {
            Slice("bg", new Rect(r.x + 20, r.y + 18, r.width - 40, r.height - 36));
            Image("decorate_left", new Rect(r.x + 22, r.yMax - 124, 247, 107)); Image("decorate_right", new Rect(r.xMax - 418, r.yMax - 121, 396, 104)); Slice("frame", r);
        }
        public static void Scrollbar(float x, float y, float height, float offset, float content) {
            if (content <= height) return;
            Slice("official-panel-slider_bg", new Rect(x, y, 8, height), 1, 5, 1, 5);
            float h = height * height / content; Slice("official-panel-slider", new Rect(x, y + (height - h) * Mathf.Clamp01(offset / (content - height)), 8, h), 1, 5, 1, 5);
        }
        public static float Slider(string name, Rect track, float value, bool disabled) {
            string pre = "official-config-setting_sound_progress_";
            Slice(pre + "base", track, 5, 1, 5, 1);
            if (value > 0) Slice(pre + (disabled ? "strip_dis" : "strip"), new Rect(track.x, track.y, track.width * value, track.height), 5, 1, 5, 1);
            Image(pre + (disabled ? "btn_dis" : "btn"), new Rect(track.x + track.width * value - 12, track.center.y - 25, 24, 50));
            Label(Mathf.RoundToInt(value * 100).ToString(), track.xMax + 24, track.center.y, 30, disabled ? Muted : Text);
            int id = GUIUtility.GetControlID(name.GetHashCode(), FocusType.Passive); var e = Event.current;
            if (!disabled && GUI.enabled && e.type == EventType.MouseDown && new Rect(track.x - 20, track.y - 30, track.width + 40, 72).Contains(e.mousePosition)) { GUIUtility.hotControl = id; value = Mathf.Clamp01((e.mousePosition.x - track.x) / track.width); e.Use(); }
            if (GUIUtility.hotControl == id && e.type == EventType.MouseDrag) { value = Mathf.Clamp01((e.mousePosition.x - track.x) / track.width); e.Use(); }
            if (GUIUtility.hotControl == id && e.type == EventType.MouseUp) { GUIUtility.hotControl = 0; e.Use(); }
            return Mathf.Round(value * 100) / 100;
        }
    }
    public sealed class NativeScroll
    {
        public float Offset;
        bool pressed, dragging; Vector2 start; float initial;
        public void Input(Rect view, float content) {
            var e = Event.current; float max = Mathf.Max(0, content - view.height);
            if (!GUI.enabled) return;
            if (e.type == EventType.ScrollWheel && view.Contains(e.mousePosition)) { Offset += e.delta.y * 35; e.Use(); }
            if (e.type == EventType.MouseDown && view.Contains(e.mousePosition)) { pressed = true; start = e.mousePosition; initial = Offset; dragging = false; }
            if (pressed && e.type == EventType.MouseDrag && Mathf.Abs(e.mousePosition.y - start.y) > 9) { dragging = true; GUIUtility.hotControl = 0; Offset = initial + start.y - e.mousePosition.y; e.Use(); }
            if (e.rawType == EventType.MouseUp) { pressed = false; if (dragging) { GUIUtility.hotControl = 0; e.Use(); } dragging = false; }
            Offset = Mathf.Clamp(Offset, 0, max);
        }
    }
}
