using System;
using System.Linq;
using UnityEngine;
using static YunJian.Riichi.NativePanel;

namespace YunJian.Riichi
{
    public sealed class NativeSettingsUi
    {
        readonly PlaylistAudio audio;
        readonly NativeScroll scroll = new NativeScroll();
        public int Tab;
        public string MusicGroup;
        bool dropdown;
        float opened, musicOpened;
#if UNITY_EDITOR
        public Rect DesignPreviewSafeArea;
#endif
        public NativeSettingsUi(PlaylistAudio value) { audio = value; }
        public void Open() { opened = Time.unscaledTime; Tab = 0; MusicGroup = null; dropdown = false; scroll.Offset = 0; }
        public bool Back() { if (MusicGroup != null) { CloseMusic(); return false; } audio.StopAudition(); return true; }
        void CloseMusic() { MusicGroup = null; audio.ApplyPlaylist(); }
        public void OpenMusic(string group) { MusicGroup = group; musicOpened = Time.unscaledTime; }
        public void Draw(Action close) {
            var matrix = GUI.matrix; var color = GUI.color;
            var safe = Screen.safeArea;
#if UNITY_EDITOR
            if (DesignPreviewSafeArea.width > 0) safe = DesignPreviewSafeArea;
#endif
            float scale = Mathf.Min(safe.height / 1080f, safe.width / 1920f), width = safe.width / scale;
            GUI.matrix = Matrix4x4.TRS(new Vector3(safe.x, Screen.height - safe.yMax + (safe.height - 1080 * scale) / 2), Quaternion.identity, new Vector3(scale, scale, 1));
            GUI.color = Color.white;
            // Native settings has no full-screen dark mask. Its centre stays fixed when the edge inset changes.
            var baseMatrix = GUI.matrix; if (MusicGroup == null) Popup(width, opened);
            float rx = width / 2 - 838, vx = rx + 258, vy = 220;
            bool enabled = GUI.enabled; GUI.enabled &= MusicGroup == null;
            Slice("official-panel-bg", new Rect(rx + 208, 120, 1260, 840), 30, 30, 30, 30);
            var dl = Art("official-panel-deco_l"); var dr = Art("official-panel-deco_r");
            Image("official-panel-deco_l", new Rect(rx + 208, 957 - dl.height, dl.width, dl.height));
            Image("official-panel-deco_r", new Rect(rx + 1469 - dr.width, 958 - dr.height, dr.width, dr.height));
            Slice("official-panel-frame", new Rect(rx + 188, 101, 1298, 874), 90, 90, 90, 90);
            Label("设置", rx + 842, 170, 51, new Color(.8784f, .6667f, .4314f), 1, TextAnchor.MiddleCenter);
            Image("native-pop2_close", new Rect(rx + 1376, 148, 44, 44));
            if (Hit(new Rect(rx + 1358, 130, 80, 80))) { audio.StopAudition(); close(); }
            for (int i = 0; i < 3; i++) {
                float x = rx + 2, y = 186 + i * 104; bool active = Tab == i;
                Image("official-config-common_popup_tab_btn" + (active ? "_selected" : ""), new Rect(x, y, active ? 248 : 207, 108));
                Label(i == 0 ? "声音" : i==1?"画面":"资源", x + 123, y + 55, 40, Gold, 1, TextAnchor.MiddleCenter);
                if (Hit(new Rect(x, y, 207, 108))) { Tab = i; dropdown = false; scroll.Offset = 0; }
            }
            Label("当前版本  Unity " + Application.version, rx + 1419, 901, 30, new Color(.608f, .631f, .698f), 0, TextAnchor.MiddleRight);
            var view = new Rect(vx, vy, 1150, 630);
            if (Tab == 0) {
                scroll.Input(view, 875); GUI.BeginGroup(view);
                DrawSound(-scroll.Offset);
                GUI.EndGroup(); Scrollbar(vx + 1155, vy, 630, scroll.Offset, 875);
            } else { GUI.BeginGroup(view);if(Tab==1)DrawGraphics();else CharacterResourceUi.Current.DrawSettings(view); GUI.EndGroup(); }
            GUI.enabled = enabled; GUI.matrix = baseMatrix; GUI.color = Color.white;
            if (MusicGroup != null) DrawMusic(width);
            GUI.matrix = matrix; GUI.color = color;
        }
        static void Section(string title, float y, float textOffset = 0) { Slice("official-config-setting_title_base", new Rect(0, y, 1120, 60), 10, 1, 310, 1); Label(title, 16, y + 30 + textOffset, 40, Gold, 1); }
        static void Row(float y, string title) { Slice("official-config-setting_entry_base", new Rect(10, y, 1100, 90), 20, 20, 20, 20); Label(title, 36, y + 43); }
        static bool Pill(string title, Rect r) { Slice("official-config-setting_entry_btn", r, 35, 1, 35, 1); Label(title, r.center.x, r.center.y, 30, Text, 0, TextAnchor.MiddleCenter); return Hit(r); }
        void DrawSound(float offset) {
            Section("音量", offset);
            string[] names = { "全域音量", "背景音乐", "音效", "立直音乐", "雀士语音" };
            for (int i = 0; i < 5; i++) {
                float y = offset + 70 + i * 95; Row(y, names[i]);
                bool disabled = AppPreferences.Muted[0] || AppPreferences.Muted[i];
                Image("official-config-" + (disabled ? "setting_sound_vol_btn_close" : "setting_sound_vol_btnl_open"), new Rect(609, y + 13, 64, 64));
                bool enabled = GUI.enabled; GUI.enabled &= i == 0 || !AppPreferences.Muted[0];
                if (Hit(new Rect(609, y + 13, 64, 64))) AppPreferences.ToggleMute(i);
                GUI.enabled = enabled;
                float value = Slider("volume-" + i, new Rect(700, y + 38, 310, 12), AppPreferences.Volume[i], disabled);
                if (value != AppPreferences.Volume[i]) AppPreferences.SetVolume(i, value);
            }
            float sy = offset + 580, textOffset = -3f * 1080 / 780; Section("曲目设置", sy, textOffset);
            float infoY = sy + 31 - 2f * 1080 / 780;
            Image("official-config-setting_sound_tip", new Rect(710, infoY - 12, 24, 24)); Label("大厅音乐：", 742, infoY, 28, Muted);
            Label(audio.CurrentName("lobby"), 1078, infoY, 30, Text, 0, TextAnchor.MiddleRight);
            for (int i = 0; i < 2; i++) {
                float y = sy + 70 + i * 95; Row(y, i == 0 ? "大厅曲目" : "对局曲目");
                if (Pill("前往设置", new Rect(607, y + 14, 484, 64))) OpenMusic(i == 0 ? "lobby" : "mj");
                Image("official-config-setting_go_tip", new Rect(1055, y + 34, 16, 24));
            }
        }
        void DrawGraphics() {
            Section("画面", 0); Row(70, "帧率");
            if (Pill(AppPreferences.Fps + " 帧", new Rect(607, 84, 484, 64))) dropdown = !dropdown;
            Image("official-config-setting_dropdown_tip", new Rect(1049, 109, 24, 13));
            Row(165, "异形屏适配"); Image("official-config-setting_note_btn", new Rect(214, 195, 26, 26));
            if (!dropdown) {
                float value = Slider("safe-inset", new Rect(607, 203, 400, 12), AppPreferences.ScreenInset, false);
                if (value != AppPreferences.ScreenInset) AppPreferences.SetInset(value);
            }
            if (dropdown) {
                // Original UI_Config_Popup_Dropdown: -8px from the button,
                // native cutout aligned with its lower half, 31px top inset,
                // 83px cells and 60px selection strips. The strips stay well
                // inside the rounded ends rather than crossing their corners.
                const float x = 600, y = 116.5f;
                Slice("official-config-setting_dropdown_base", new Rect(x, y, 496, 315), 50, 50, 50, 50);
                int[] options = { 30, 60, 120 };
                for (int i = 0; i < options.Length; i++) {
                    float rowY = y + 31 + i * 83;
                    if (options[i] == AppPreferences.Fps) Slice("official-config-setting_dropdown_tip_selected", new Rect(x + 8, rowY + 18, 480, 60), 1, 1, 1, 1);
                    Label(options[i] + " 帧", x + 247.5f, rowY + 46, 30, Text, 0, TextAnchor.MiddleCenter);
                    if (Hit(new Rect(x + 8, rowY, 480, 83))) { AppPreferences.SetFps(options[i]); dropdown = false; }
                }
            }
        }
        void DrawMusic(float width) {
            ScreenShade(new Color(0, 0, 0, .5f));
            var matrix = GUI.matrix; var color = GUI.color; Popup(width, musicOpened);
            float ox = width / 2 - 959, oy = 80;
            Slice("official-config-setting_voice_popup_base", new Rect(ox, oy, 1919, 920), 1, 10, 1, 10);
            Label(MusicGroup == "lobby" ? "大厅音乐" : "对局音乐", width / 2, oy + 64.54f, 54, Gold, 1, TextAnchor.MiddleCenter);
            Image("native-pop2_close", new Rect(ox + 1618, oy + 43, 44, 44));
            if (Hit(new Rect(ox + 1600, oy + 25, 80, 80))) { CloseMusic(); GUI.matrix = matrix; GUI.color = color; return; }
            var tracks = audio.Tracks.Where(t => t.group == MusicGroup).ToArray();
            for (int i = 0; i < tracks.Length; i++) {
                var t = tracks[i]; float x = ox + 357 + i % 2 * 612, y = oy + 121 + i / 2 * 110;
                bool selected = audio.Selected(MusicGroup, t.id), playing = audio.AuditionId == t.id && audio.AuditionPlaying;
                Slice("official-config-setting_voice_entry_base", new Rect(x, y, 592, 99), 12, 12, 12, 12);
                Image("checkbox2", new Rect(x + 25, y + 26, 48, 47)); if (selected) Image("check_green", new Rect(x + 25, y + 26, 48, 48));
                Label(t.name, x + 92, y + 48, 36, selected ? new Color(.553f, 1, .145f) : Muted);
                if (Hit(new Rect(x + 4, y + 6, 450, 90))) audio.Toggle(MusicGroup, t.id);
                if (playing) for (int j = 0; j < 4; j++) { float h = 12 + Mathf.Pow(Mathf.Sin(Time.unscaledTime * 9 + j * 1.7f), 2) * 19; Box(new Rect(x + 458 + j * 8, y + 48 - h / 2, 4, h), new Color(.65f, .68f, .78f)); }
                Image("official-config-setting_song_" + (playing ? "pause" : "play") + "_btn", new Rect(x + 503, y + 17, 70, 70));
                if (Hit(new Rect(x + 495, y + 8, 90, 90))) audio.Audition(t.id);
            }
            float bx = ox + 1339, by = oy + 836; bool random = audio.RandomOrder(MusicGroup);
            Slice("official-config-setting_song_btn", new Rect(bx - 5, by - 6, 233, 65), 20, 1, 20, 1);
            Image("official-config-setting_song_" + (random ? "random" : "cycle") + "_icon", new Rect(bx + 16, by + 13, 28, random ? 24 : 26));
            Label(random ? "随机播放" : "循环播放", bx + 131, by + 25, 30, Gold, 0, TextAnchor.MiddleCenter);
            if (Hit(new Rect(bx, by, 222, 55))) audio.ToggleMode(MusicGroup);
            GUI.matrix = matrix; GUI.color = color;
        }
#if UNITY_EDITOR
        public void ValidationScrollToMusic() { scroll.Offset = 245; opened = musicOpened = Time.unscaledTime - 1; }
        public void ValidationDropdown(bool value) { Tab = 1; dropdown = value; opened = Time.unscaledTime - 1; }
#endif
    }
}
