using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using static YunJian.Riichi.NativePanel;

namespace YunJian.Riichi
{
    public sealed class NativeYakuUi
    {
        [Serializable] public class Entry { public int id, tag; public string name, desc, limit, example; }
        [Serializable] class Data { public Entry[] entries; }
        [Serializable] class Point { public int id; public float x, y, w, h, anchorX, anchorY; }
        [Serializable] class PointData { public Point[] popups, hits; }
        readonly Entry[] entries;
        readonly PointData points;
        readonly NativeScroll scroll = new NativeScroll();
        public int Section, Tag = 1, TutorialPage, PointTab, PointHelp;
        public int Count => entries.Length;
        float helpAt;
#if UNITY_EDITOR
        public float DesignPreviewClock = -1;
#endif
        float Now {
            get {
#if UNITY_EDITOR
                if (DesignPreviewClock >= 0) return DesignPreviewClock;
#endif
                return Time.unscaledTime;
            }
        }
        public NativeYakuUi() {
            entries = JsonUtility.FromJson<Data>(Resources.Load<TextAsset>("Majsoul/Settings/yaku").text).entries;
            points = JsonUtility.FromJson<PointData>(Resources.Load<TextAsset>("Majsoul/Settings/points").text);
            // Load before a click so the first animation frame never waits for a texture.
            for (int i = 1; i <= 10; i++) Art("illustration" + i);
        }
        public void Open() { Section = 0; Tag = 1; scroll.Offset = 0; PointHelp = 0; }
        public void Draw(float width, Action close) {
            var matrix = GUI.matrix; var color = GUI.color;
            ScreenShade(new Color(0, 0, 0, .47f));
            // A centred 1754-wide native panel fits both 16:9 and wider phones.
            GUI.matrix *= Matrix4x4.Translate(new Vector3((width - 2337) / 2, 0));
            var r = new Rect(292, 24, 1754, 1032); Frame(r);
            Image("btn_close", new Rect(r.xMax - 83, r.y + 39, 50, 50));
            if (Hit(new Rect(r.xMax - 103, r.y + 19, 90, 90))) close();
            string[] sections = { "役种一览", "新手入门", "点数计算" };
            for (int i = 0; i < 3; i++) {
                float x = 266, y = 235 - 25f * 1080 / 780 + i * 249;
                Image(Section == i ? "native-info_tab_chosen" : "info_tab_dark", new Rect(x, y, 72, 250));
                for (int j = 0; j < sections[i].Length; j++) Label(sections[i][j].ToString(), x + 36, y + 35 + j * 46 + 17f * 1080 / 780, 40, Gold, 1, TextAnchor.MiddleCenter);
                if (Hit(new Rect(x - 5, y, 85, 250))) { Section = i; PointHelp = 0; }
            }
            if (Section == 0) DrawEntries(); else if (Section == 1) DrawTutorial(); else DrawPoints();
            GUI.matrix = matrix; GUI.color = color;
        }
        void DrawEntries() {
            string[] names = { "一番", "二番", "三番", "六番", "满贯", "役满", "双倍役满", "流局" };
            float x = 448;
            for (int i = 0; i < names.Length; i++) {
                float w = i == 6 ? 232 : 174;
                Image(Tag == i + 1 ? "tab_choosed" : "tab_unchoose", new Rect(x, 116, w, 73));
                Label(names[i], x + w / 2, 154, 40, Gold, 1, TextAnchor.MiddleCenter);
                if (Hit(new Rect(x, 116, w, 73))) { Tag = i + 1; scroll.Offset = 0; }
                x += w + 4;
            }
            var list = entries.Where(e => e.tag == Tag).ToArray(); var view = new Rect(461, 214, 1450, 783);
            scroll.Input(view, list.Length * 275); GUI.BeginGroup(view);
            for (int i = 0; i < list.Length; i++) {
                float y = i * 275 - scroll.Offset; if (y > view.height || y + 267 < 0) continue;
                var e = list[i]; Slice("bg_fan_rules", new Rect(0, y, 1445, 267));
                Label(e.name, 25, y + 30.15f, 40, Gold, 1);
                Label(e.limit, 1414, y + 27, 29, new Color(.5f, .847f, .294f), 2, TextAnchor.MiddleRight);
                Label(e.desc, 30, y + 86.15f, 29, new Color(.651f, .682f, .745f), 2, TextAnchor.MiddleLeft, 1375, 72, true);
                Tiles(e.example, 43, y + 130);
            }
            GUI.EndGroup(); Scrollbar(1928, 214, 783, scroll.Offset, list.Length * 275);
        }
        static void Tiles(string example, float x, float y) {
            foreach (Match match in Regex.Matches(example ?? "", @"\d[mpsz]|[|#b]")) {
                string s = match.Value;
                if (s == "|" || s == "#") { x += 22; continue; }
                if (s == "b") Box(new Rect(x, y, 64, 105), new Color(.93f, .91f, .88f));
                else { var t = Resources.Load<Texture2D>("Majsoul/Tiles/" + s[1] + s[0]); if (t != null) GUI.DrawTexture(new Rect(x, y, 65, 105), t); }
                x += 65;
            }
        }
        void DrawTutorial() {
            Image("course" + TutorialPage, new Rect(448, 82, 1450, 916));
            Label((TutorialPage + 1) + "/9", 1937, 885, 29, Muted, 2, TextAnchor.MiddleRight);
            var matrix = GUI.matrix;
            if (TutorialPage < 8) {
                GUI.matrix = matrix * Matrix4x4.TRS(new Vector3(1954, 964), Quaternion.Euler(0, 0, 90), Vector3.one);
                Image("course_arrow", new Rect(-30, -53, 60, 106)); GUI.matrix = matrix;
                if (Hit(new Rect(1860, 910, 145, 115))) TutorialPage++;
            }
            if (TutorialPage > 0) {
                GUI.matrix = matrix * Matrix4x4.TRS(new Vector3(1954, 799), Quaternion.Euler(0, 0, -90), Vector3.one);
                Image("course_arrow", new Rect(-30, -53, 60, 106)); GUI.matrix = matrix;
                if (Hit(new Rect(1860, 740, 145, 115))) TutorialPage--;
            }
        }
        void DrawPoints() {
            string[] names = { "闲家点数表", "庄家点数表", "符数计算表" };
            for (int i = 0; i < 3; i++) {
                float x = 438 + i * 495; Slice(PointTab == i ? "tab_choosed" : "tab_unchoose", new Rect(x, 91, 495, 77), 5, 5, 5, 5);
                Label(names[i], x + 247.5f, 130, 40, Gold, 1, TextAnchor.MiddleCenter);
                if (Hit(new Rect(x, 91, 495, 77))) { PointTab = i; PointHelp = 0; }
            }
            Image("page" + (PointTab + 1), new Rect(438, 187, 1485, 841));
            if (PointTab != 2) return;
            if (PointHelp != 0) {
                var p = points.popups.First(e => e.id == PointHelp); float scale = BackOut((Now - helpAt) / .2f);
                var pivot = new Vector2(p.x + p.w * p.anchorX, p.y + p.h * p.anchorY);
                // A zero GUI matrix can display an unscaled first frame on Android.
                // Scale the image bounds directly, keeping the original pivot fixed.
                if (scale > .0001f) Image("illustration" + p.id, new Rect(pivot.x + (p.x - pivot.x) * scale, pivot.y + (p.y - pivot.y) * scale, p.w * scale, p.h * scale));
                if (Hit(new Rect(438, 187, 1485, 841))) PointHelp = 0;
            } else foreach (var p in points.hits) if (Hit(new Rect(p.x, p.y, p.w, p.h))) ShowPointHelp(p.id);
        }
        public void ShowPointHelp(int id) { PointHelp = id; helpAt = Now; }
    }
}
