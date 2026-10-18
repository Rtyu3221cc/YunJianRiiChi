using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YunJian.Riichi.Editor
{
    // Render-only review of the requested corrections. No unit-test runner,
    // player build, publication, room requests or preference writes.
    [InitializeOnLoad] public static class Review1017fPreview
    {
        const string Key = "Riichi.Review1017f";
        static Review1017fPreview() {
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false)) {
                    SessionState.SetBool(Key, false); UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<Review1017fCapture>();
                }
            };
        }
        public static void Run() { EditorSceneManager.OpenScene("Assets/Scenes/Table.unity"); SessionState.SetBool(Key, true); EditorApplication.isPlaying = true; }
    }
    public sealed class Review1017fCapture : MonoBehaviour
    {
        static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        string output; RiichiClient client; LobbyController lobby; NativeSettingsUi settings; NativeYakuUi yaku;
        readonly List<string> errors = new List<string>();
        void Log(string message, string trace, LogType kind) { if (kind == LogType.Error || kind == LogType.Exception) errors.Add(message); }
        IEnumerator Start() {
            Application.logMessageReceived += Log;
            var stack = new Stack<IEnumerator>(); stack.Push(Capture());
            while (stack.Count > 0) {
                object next;
                try { var top = stack.Peek(); if (!top.MoveNext()) { stack.Pop(); continue; } next = top.Current; if (next is IEnumerator child) { stack.Push(child); continue; } }
                catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); yield break; }
                yield return next;
            }
            File.WriteAllText(Path.Combine(output, "render-notes.txt"), "Unity actual renders. No APK, unit tests or preference writes.\n" + string.Join("\n", errors));
            Debug.Log("REVIEW 1017F RENDER COMPLETE; no APK or unit tests; runtime errors=" + errors.Count); EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
        }
        IEnumerator Size(int width, int height, Rect safe) {
            var window = EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
            window.GetType().GetMethod("SetCustomResolution", Private).Invoke(window, new object[] { new Vector2(width, height), "Review 1017f" });
            lobby.DesignPreviewSafeArea = safe; settings.DesignPreviewSafeArea = safe;
            for (int i = 0; i < 7; i++) yield return null;
        }
        IEnumerator Shot(string name) {
            yield return null; yield return new WaitForEndOfFrame();
            var frame = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.Combine(output, name + ".png"), frame.EncodeToPNG()); Destroy(frame);
        }
        void Home(float clock) {
            while (client.LobbyOverlayVisible) client.LobbyCloseSettings();
            lobby.DesignPreviewClock = clock; lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Home);
        }
        IEnumerator Capture() {
            output = Path.GetFullPath("../build/review-1017f/render"); Directory.CreateDirectory(output);
            client = GetComponent<RiichiClient>(); lobby = client.Lobby; client.BeginLobbyValidation("visual-only");
            typeof(RiichiClient).GetField("nickname", Private).SetValue(client, "旅人");
            settings = (NativeSettingsUi)typeof(RiichiClient).GetField("nativeSettings", Private).GetValue(client);
            yaku = (NativeYakuUi)typeof(LobbyController).GetField("nativeYaku", Private).GetValue(lobby);
            GetComponent<PlaylistAudio>().DesignPreviewDefaults(); AudioListener.pause = true;
            yield return Size(1688, 780, new Rect(96, 0, 1592, 780)); Home(100); yield return Shot("home-safe");
            lobby.DesignPreviewRules(); lobby.DesignPreviewClock = 101; yield return Shot("rules-fullscreen");
            lobby.DesignPreviewRules(0, 1); yield return Shot("rules-detail");
            Home(110); lobby.DesignPreviewGardenHelp(); yaku.Section = 0; yaku.Tag = 7; yield return Shot("yaku-position");
            yaku.Section = 2; yaku.PointTab = 2; yaku.PointHelp = 0; yield return Shot("points-position");
            yaku.DesignPreviewClock = 100; yaku.ShowPointHelp(8); yaku.DesignPreviewClock = 100.2f; yield return Shot("points-help-right-pivot");
            Home(120); client.LobbyOpenSettings(); yield return new WaitForSecondsRealtime(.3f); yield return Shot("settings-song-info");
            settings.ValidationScrollToMusic(); settings.OpenMusic("lobby"); yield return new WaitForSecondsRealtime(.3f); yield return Shot("music-lobby-all-random");
            settings.OpenMusic("mj"); yield return new WaitForSecondsRealtime(.3f); yield return Shot("music-game-all-random");
            Home(130); yield return Size(1280, 720, new Rect(64, 12, 1184, 696));
            lobby.DesignPreviewRules(); lobby.DesignPreviewClock = 131; yield return Shot("rules-safe-both-1280");
            Home(132); lobby.DesignPreviewGardenHelp(); yield return Shot("yaku-safe-both-1280");
            Home(133); client.LobbyOpenSettings(); settings.ValidationScrollToMusic(); settings.OpenMusic("lobby"); yield return new WaitForSecondsRealtime(.3f); yield return Shot("music-safe-both-1280");
            Home(140); yield return Size(1280, 592, new Rect(73, 0, 1207, 592));
            Directory.CreateDirectory(Path.Combine(output, "points-motion")); lobby.DesignPreviewGardenHelp(); yaku.Section = 2; yaku.PointTab = 2;
            const int fps = 60, perItem = 48;
            for (int id = 1; id <= 10; id++) {
                yaku.DesignPreviewClock = 200 + id;
                yaku.ShowPointHelp(id);
                for (int frame = 0; frame < perItem; frame++) {
                    yaku.DesignPreviewClock = 200 + id + frame / (float)fps;
                    if (frame >= 38) yaku.PointHelp = 0;
                    yield return Shot("points-motion/frame-" + ((id - 1) * perItem + frame).ToString("D4"));
                }
            }
            Home(300); Directory.CreateDirectory(Path.Combine(output, "rules-switch")); lobby.DesignPreviewRules(); lobby.DesignPreviewClock = 301;
            for (int frame = 0; frame < 180; frame++) {
                lobby.DesignPreviewClock = 301 + frame / 60f;
                if (frame % 30 == 12) lobby.ValidationPress("ruleTab" + ((frame / 30 + 1) % 2));
                yield return Shot("rules-switch/frame-" + frame.ToString("D4"));
            }
            Home(400); lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Modes); Directory.CreateDirectory(Path.Combine(output, "create-entry"));
            for (int frame = 0; frame < 120; frame++) {
                lobby.DesignPreviewClock = 400 + frame / 60f;
                if (frame == 30) lobby.OpenCreate();
                yield return Shot("create-entry/frame-" + frame.ToString("D4"));
            }
            Home(500); AudioListener.pause = false;
        }
        void OnDestroy() { Application.logMessageReceived -= Log; AudioListener.pause = false; }
    }
}
