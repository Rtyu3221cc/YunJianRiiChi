using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YunJian.Riichi.Editor
{
    [InitializeOnLoad] public static class Settings060Validation
    {
        const string Key = "Riichi.Settings060";
        static Settings060Validation() {
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false)) {
                    SessionState.SetBool(Key, false); UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<Settings060Capture>();
                }
            };
        }
        public static void Run() {
            ProjectSetup.Configure();
            foreach (var path in Directory.GetFiles("Assets/Resources/Majsoul/Settings/Sprites", "*.png")) {
                string resource = "Majsoul/Settings/Sprites/" + Path.GetFileNameWithoutExtension(path);
                if (Resources.Load<Texture2D>(resource) == null) throw new Exception("Missing " + resource);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer.npotScale != TextureImporterNPOTScale.None || importer.mipmapEnabled) throw new Exception("Changed native UI dimensions: " + path);
            }
            var catalog = JsonUtility.FromJson<MusicCatalog>(Resources.Load<TextAsset>("Majsoul/Settings/music").text);
            if (catalog.tracks.Length != 10 || catalog.tracks.Any(t => Resources.Load<AudioClip>(t.resource) == null)) throw new Exception("Missing offline music");
            if (new NativeYakuUi().Count != 65) throw new Exception("Incomplete yaku data");
            EditorSceneManager.OpenScene("Assets/Scenes/Table.unity"); SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
        }
    }
    public sealed class Settings060Capture : MonoBehaviour
    {
        static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        readonly List<string> checks = new List<string>();
        string folder; RiichiClient client; NativeSettingsUi settings; NativeYakuUi yaku; EditorWindow view;
        readonly List<string> errors = new List<string>();
        void Log(string message, string trace, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message); }
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks.Add(message); }
        IEnumerator Start() {
            folder = Path.GetFullPath("../build/settings-060"); Directory.CreateDirectory(folder);
            Application.logMessageReceived += Log;
            var stack = new Stack<IEnumerator>(); stack.Push(Run());
            while (stack.Count > 0) {
                object current;
                try { var top = stack.Peek(); if (!top.MoveNext()) { stack.Pop(); continue; } current = top.Current; if (current is IEnumerator nested) { stack.Push(nested); continue; } }
                catch (Exception e) { File.WriteAllText(Path.Combine(folder, "failed.txt"), e.ToString()); Debug.LogException(e); EditorApplication.Exit(1); yield break; }
                yield return current;
            }
            File.WriteAllText(Path.Combine(folder, "validation.txt"), string.Join("\n", checks));
            Debug.Log("SETTINGS 0.6.0 PASS: " + checks.Count + " checks; native screenshots saved; device testing pending."); EditorApplication.Exit(0);
        }
        IEnumerator Size(int w, int h) {
            view = EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
            view.GetType().GetMethod("SetCustomResolution", Private).Invoke(view, new object[] { new Vector2(w, h), "Settings 0.6.0 validation" });
            for (int i = 0; i < 8; i++) yield return null;
        }
        IEnumerator Shot(string name) {
            yield return null; yield return new WaitForEndOfFrame();
            var pic = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.Combine(folder, name + ".png"), pic.EncodeToPNG()); Destroy(pic);
        }
        IEnumerator Run() {
            client = GetComponent<RiichiClient>(); client.BeginLobbyValidation("settings060-local");
            settings = (NativeSettingsUi)typeof(RiichiClient).GetField("nativeSettings", Private).GetValue(client);
            yaku = (NativeYakuUi)typeof(LobbyController).GetField("nativeYaku", Private).GetValue(client.Lobby);
            client.Lobby.DesignPreviewClock = 100; client.Lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Home);
            var audio = GetComponent<PlaylistAudio>(); yield return new WaitForSecondsRealtime(.4f);
            var originalVolume = (float[])AppPreferences.Volume.Clone(); var originalMute = (bool[])AppPreferences.Muted.Clone(); float originalInset = AppPreferences.ScreenInset; int originalFps = AppPreferences.Fps;
            for (int i = 0; i < 5; i++) { AppPreferences.Muted[i] = false; AppPreferences.Volume[i] = i == 0 ? 1 : .5f; }
            yield return Size(1688, 780); client.LobbyOpenSettings(); yield return new WaitForSecondsRealtime(.3f);
            yield return Shot("sound-1688"); settings.ValidationScrollToMusic(); yield return Shot("music-rows-1688");
            settings.ValidationDropdown(false); yield return Shot("graphics-1688");
            foreach (int fps in new[] { 30, 60, 120 }) { AppPreferences.SetFps(fps); settings.ValidationDropdown(true); yield return Shot("dropdown-" + fps + "-1688"); }
            settings.ValidationDropdown(false); settings.OpenMusic("lobby"); yield return new WaitForSecondsRealtime(.3f); yield return Shot("lobby-music-1688");
            settings.Back(); client.LobbyCloseSettings(); client.Lobby.DesignPreviewGardenHelp(); yaku.Section = 2; yaku.PointTab = 2; yaku.PointHelp = 1;
            yield return Shot("points-1688"); yaku.Section = 0; yield return Shot("yaku-1688");
            yaku.Section = 1; yield return Shot("tutorial-1688"); client.Lobby.CloseModal(); client.Lobby.DesignPreviewClock = 101; yield return null;
            AppPreferences.SetInset(0); yield return Shot("safe-0"); var left = client.Lobby.ValidationTarget("role"); var right = client.Lobby.ValidationTarget("settings");
            AppPreferences.SetInset(1); yield return Shot("safe-100");
            float moved = client.Lobby.ValidationTarget("role").x - left.x;
            Check(Mathf.Abs(moved - 60 * Screen.height / 1080f) < 1, "Safe inset moves left controls inward by 60 design px");
            Check(Mathf.Abs(right.x - client.Lobby.ValidationTarget("settings").x - moved) < 1, "Safe inset moves right controls inward by the same distance");
            AppPreferences.SetInset(0);
            yield return Size(1280, 720); client.LobbyOpenSettings(); settings.ValidationDropdown(true); yield return Shot("dropdown-120-1280");
            client.LobbyCloseSettings(); client.Lobby.DesignPreviewGardenHelp(); yaku.Section = 2; yaku.PointTab = 2; yaku.PointHelp = 1; yield return Shot("points-1280"); client.Lobby.CloseModal(); client.Lobby.DesignPreviewClock = 102;
            audio.SwitchTo(306003); yield return new WaitForSecondsRealtime(2.2f); audio.SwitchTo(306001); yield return new WaitForSecondsRealtime(.9f);
            var mid = audio.ChannelVolumes;
            Check(audio.Fading && mid.All(v => v > .1f), "Both real Unity AudioSources overlap during the 2-second crossfade");
            yield return new WaitForSecondsRealtime(1.4f);
            Check(!audio.Fading && audio.ChannelVolumes.Count(v => v > .1f) == 1, "Crossfade finishes and retires the old channel");
            AppPreferences.Muted[0] = true; yield return null;
            Check(audio.ChannelVolumes.All(v => v == 0), "Master mute silences both music channels");
            for (int i = 0; i < 5; i++) { AppPreferences.Muted[i] = originalMute[i]; AppPreferences.SetVolume(i, originalVolume[i]); }
            AppPreferences.SetInset(originalInset); AppPreferences.SetFps(originalFps);
            Check(Application.version == PlayerSettings.bundleVersion, "Runtime version matches the configured build");
            Check(errors.Count == 0, "No Unity runtime or GUI errors");
        }
        void OnDestroy() { Application.logMessageReceived -= Log; }
    }
}
