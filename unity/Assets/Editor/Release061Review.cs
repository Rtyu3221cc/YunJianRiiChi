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
    [InitializeOnLoad] public static class Release061Review
    {
        const string Key = "Riichi.Release061Review";
        static Release061Review() {
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false)) {
                    SessionState.SetBool(Key, false); UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<Release061Capture>();
                }
            };
        }
        public static void Run() { ProjectSetup.Configure(); EditorSceneManager.OpenScene("Assets/Scenes/Table.unity"); SessionState.SetBool(Key, true); EditorApplication.isPlaying = true; }
    }
    public sealed class Release061Capture : MonoBehaviour
    {
        static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        readonly List<string> errors = new List<string>();
        string output; RiichiClient client; LobbyController lobby; NativeSettingsUi settings;
        void Log(string message, string trace, LogType type) { if (type == LogType.Error || type == LogType.Exception) errors.Add(message); }
        IEnumerator Start() {
            output = Path.GetFullPath("../build/settings-061/render"); Directory.CreateDirectory(output); Application.logMessageReceived += Log;
            var stack = new Stack<IEnumerator>(); stack.Push(Capture());
            while (stack.Count > 0) {
                object next;
                try { var top = stack.Peek(); if (!top.MoveNext()) { stack.Pop(); continue; } next = top.Current; if (next is IEnumerator child) { stack.Push(child); continue; } }
                catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); yield break; }
                yield return next;
            }
            File.WriteAllText(Path.Combine(output, "review.txt"), "Version " + Application.version + "; actual Unity renders; errors=" + errors.Count + "\n" + string.Join("\n", errors));
            Debug.Log("RELEASE 0.6.1 RENDER PASS; version=" + Application.version + "; errors=" + errors.Count); EditorApplication.Exit(errors.Count == 0 && Application.version == "0.6.1" ? 0 : 1);
        }
        IEnumerator Size(int w, int h, Rect safe) {
            var view = EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
            view.GetType().GetMethod("SetCustomResolution", Private).Invoke(view, new object[] { new Vector2(w, h), "0.6.1 final review" });
            lobby.DesignPreviewSafeArea = safe; settings.DesignPreviewSafeArea = safe;
            for (int i = 0; i < 7; i++) yield return null;
        }
        IEnumerator Shot(string name) {
            yield return null; yield return new WaitForEndOfFrame(); var image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(output, name + ".png"), image.EncodeToPNG()); Destroy(image);
        }
        void Home(float now) { while (client.LobbyOverlayVisible) client.LobbyCloseSettings(); lobby.DesignPreviewClock = now; lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Home); }
        IEnumerator Capture() {
            client = GetComponent<RiichiClient>(); lobby = client.Lobby; client.BeginLobbyValidation("release061-local");
            typeof(RiichiClient).GetField("nickname", Private).SetValue(client, "旅人");
            settings = (NativeSettingsUi)typeof(RiichiClient).GetField("nativeSettings", Private).GetValue(client);
            GetComponent<PlaylistAudio>().DesignPreviewDefaults(); AudioListener.pause = true;
            foreach (int width in new[] { 1688, 1280 }) {
                int height = width == 1688 ? 780 : 720;
                var safe = width == 1688 ? new Rect(96, 10, 1560, 760) : new Rect(64, 12, 1184, 696);
                yield return Size(width, height, safe); Home(100); yield return Shot("home-" + width);
                lobby.DesignPreviewRules(); lobby.DesignPreviewClock = 101; yield return Shot("rules-" + width);
                lobby.DesignPreviewRules(0, 1); yield return Shot("rules-detail-" + width);
                Home(110); client.LobbyOpenSettings(); yield return new WaitForSecondsRealtime(.3f); yield return Shot("settings-" + width);
                settings.ValidationScrollToMusic(); settings.OpenMusic("lobby"); yield return new WaitForSecondsRealtime(.3f); yield return Shot("music-lobby-" + width);
            }
            Home(120); AudioListener.pause = false;
        }
        void OnDestroy() { Application.logMessageReceived -= Log; AudioListener.pause = false; }
    }
}
