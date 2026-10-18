using System;
using UnityEngine;

namespace YunJian.Riichi
{
    // One persistent volume model for the lobby, table, voices and auditions.
    public static class AppPreferences
    {
        public static readonly float[] Volume = { 1, .5f, .5f, .5f, .8f };
        public static readonly bool[] Muted = new bool[5];
        public static int Fps { get; private set; }
        public static float ScreenInset { get; private set; }
        public static int Revision { get; private set; }
        static bool loaded;
        public static void Load()
        {
            if (loaded) return; loaded = true;
            Volume[1] = PlayerPrefs.GetFloat("riichi.musicVolume", .5f);
            Volume[2] = PlayerPrefs.GetFloat("riichi.effectsVolume", .5f);
            Volume[4] = PlayerPrefs.GetFloat("riichi.volume", .8f);
            Muted[1] = PlayerPrefs.GetInt("riichi.musicMuted", 0) != 0;
            Muted[2] = PlayerPrefs.GetInt("riichi.effectsMuted", 0) != 0;
            Muted[4] = PlayerPrefs.GetInt("riichi.muted", 0) != 0;
            for (int i = 0; i < 5; i++) {
                Volume[i] = Mathf.Clamp01(PlayerPrefs.GetFloat("riichi.setting.volume." + i, Volume[i]));
                Muted[i] = PlayerPrefs.GetInt("riichi.setting.muted." + i, Muted[i] ? 1 : 0) != 0;
            }
            Fps = PlayerPrefs.GetInt("riichi.setting.fps", 60);
            if (Fps != 30 && Fps != 60 && Fps != 120) Fps = 60;
            ScreenInset = Mathf.Clamp01(PlayerPrefs.GetFloat("riichi.setting.inset", 0));
            Application.targetFrameRate = Fps;
        }
        public static float Gain(int channel) {
            Load(); return Muted[0] || Muted[channel] ? 0 : Volume[0] * (channel == 0 ? 1 : Volume[channel]);
        }
        public static void SetVolume(int channel, float value) { Volume[channel] = Mathf.Clamp01(value); Save(); }
        public static void ToggleMute(int channel) { Muted[channel] = !Muted[channel]; Save(); }
        public static void SetFps(int value) { Fps = value; Application.targetFrameRate = value; Save(); }
        public static void SetInset(float value) { ScreenInset = Mathf.Clamp01(value); Save(); }
        static void Save() {
            for (int i = 0; i < 5; i++) {
                PlayerPrefs.SetFloat("riichi.setting.volume." + i, Volume[i]);
                PlayerPrefs.SetInt("riichi.setting.muted." + i, Muted[i] ? 1 : 0);
            }
            PlayerPrefs.SetInt("riichi.setting.fps", Fps); PlayerPrefs.SetFloat("riichi.setting.inset", ScreenInset);
            PlayerPrefs.Save(); Revision++;
        }
    }

    [Serializable] public sealed class MusicTrack { public int id; public string name, resource, group; }
    [Serializable] public sealed class MusicCatalog { public MusicTrack[] tracks; }
}
