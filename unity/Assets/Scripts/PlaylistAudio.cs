using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi
{
    public sealed class PlaylistAudio : MonoBehaviour
    {
        public const float FadeSeconds = 2;
        public static PlaylistAudio Active { get; private set; }
        public MusicTrack[] Tracks { get; private set; }
        public int CurrentId { get; private set; }
        public int AuditionId { get; private set; }
        public bool AuditionPlaying => audition != null && audition.isPlaying;
        public bool Fading => fading;
        public float[] ChannelVolumes => channels.Select(s => s.volume).ToArray();
        public string Group { get; private set; } = "lobby";
        readonly Dictionary<string, List<int>> selected = new Dictionary<string, List<int>>();
        readonly Dictionary<string, bool> random = new Dictionary<string, bool>();
        readonly Dictionary<int, AudioClip> clips = new Dictionary<int, AudioClip>();
        AudioSource[] channels; AudioSource audition,riichi;string riichiKey;
        public bool RiichiPlaying=>riichi!=null&&riichi.isPlaying;
        public bool ExternalAudition {get;set;}
        RiichiClient client; int activeChannel, request; bool fading, loading, suspended, started;
        float fadeAt, fromGain = 1;
        public void Initialize(RiichiClient owner) { client = owner; }
        void Awake() {
            Active = this; AppPreferences.Load();
            Tracks = JsonUtility.FromJson<MusicCatalog>(Resources.Load<TextAsset>("Majsoul/Settings/music").text).tracks;
            channels = new[] { gameObject.AddComponent<AudioSource>(), gameObject.AddComponent<AudioSource>() };
            audition = gameObject.AddComponent<AudioSource>();riichi=gameObject.AddComponent<AudioSource>();riichi.playOnAwake=false;riichi.loop=true;riichi.spatialBlend=0;
            foreach (var s in channels.Concat(new[] { audition })) { s.playOnAwake = false; s.spatialBlend = 0; s.loop = false; s.volume = 0; }
            foreach (var group in new[] { "lobby", "mj" }) {
                var valid = Tracks.Where(t => t.group == group).Select(t => t.id).ToArray();
                var raw = PlayerPrefs.GetString("riichi.playlist." + group, string.Join(",", valid));
                selected[group] = raw.Split(',').Select(s => int.TryParse(s, out var n) ? n : -1).Where(valid.Contains).Distinct().ToList();
                random[group] = PlayerPrefs.GetInt("riichi.playlist.random." + group, 1) != 0;
            }
        }
        public bool Selected(string group, int id) => selected[group].Contains(id);
        public bool RandomOrder(string group) => random[group];
#if UNITY_EDITOR
        public void DesignPreviewDefaults() {
            foreach (var group in new[] { "lobby", "mj" }) { selected[group] = Tracks.Where(t => t.group == group).Select(t => t.id).ToList(); random[group] = true; }
        }
#endif
        public void Toggle(string group, int id) { var list = selected[group]; if (!list.Remove(id)) list.Add(id); Save(group); }
        public void ToggleMode(string group) { random[group] = !random[group]; Save(group); }
        void Save(string group) { PlayerPrefs.SetString("riichi.playlist." + group, string.Join(",", selected[group])); PlayerPrefs.SetInt("riichi.playlist.random." + group, random[group] ? 1 : 0); PlayerPrefs.Save(); }
        public string CurrentName(string group) { var t = Tracks.FirstOrDefault(x => x.id == CurrentId && x.group == group && Selected(group, x.id)) ?? Tracks.FirstOrDefault(x => x.group == group && Selected(group, x.id)); return t?.name ?? "未选择曲目"; }
        AudioClip Clip(int id) { if (!clips.TryGetValue(id, out var c)) { var t = Tracks.FirstOrDefault(x => x.id == id); clips[id] = c = t == null ? null : Resources.Load<AudioClip>(t.resource); } return c; }
        int Next(string group) {
            var list = selected[group]; if (list.Count == 0) return 0;
            if (random[group] && list.Count > 1) { var other = list.Where(x => x != CurrentId).ToArray(); return other[UnityEngine.Random.Range(0, other.Length)]; }
            return list[(list.IndexOf(CurrentId) + 1) % list.Count];
        }
        void Update() {
            if (client == null || suspended) return;
            string nextGroup = client.LobbyPlaying ? "mj" : "lobby";
            if (!started || nextGroup != Group) { started = true; Group = nextGroup; ApplyPlaylist(); }
            UpdateRiichi();RefreshVolumes();
            var source = channels[activeChannel];
            if (!loading && !fading && source.clip != null && source.isPlaying && source.clip.length - source.time <= FadeSeconds) SwitchTo(Next(Group));
        }
        void UpdateRiichi(){
            var view=client.State;bool play=client.LobbyPlaying&&view!=null&&(view.phase=="play"||view.phase=="reaction")&&view.you>=0&&view.you<view.players.Length&&view.players[view.you].riichi>0;
            string next=play?view.audioRoom+":"+view.handSeq+":"+WardrobeRuntime.Item(view,view.you,"music").id:null;
            if(next!=riichiKey){riichi.Stop();riichiKey=next;if(play){riichi.clip=Resources.Load<AudioClip>(WardrobeRuntime.Item(view,view.you,"music").audio);if(riichi.clip!=null)riichi.Play();}}
            riichi.volume=AppPreferences.Gain(1);
        }
        public void ApplyPlaylist() {
            StopAudition();
            if (!Selected(Group, CurrentId) || !channels[activeChannel].isPlaying) SwitchTo(Next(Group));
        }
        public void SwitchTo(int id) { int serial = ++request; StartCoroutine(LoadAndSwitch(id, serial)); }
        IEnumerator LoadAndSwitch(int id, int serial) {
            loading = true;
            if (id == 0) { foreach (var s in channels) s.Stop(); CurrentId = 0; fading = loading = false; yield break; }
            var clip = Clip(id); if (clip == null) { loading = false; yield break; }
            if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            while (clip.loadState == AudioDataLoadState.Loading) { yield return null; if (serial != request) yield break; }
            if (serial != request) yield break;
            if (clip.loadState == AudioDataLoadState.Failed) { loading = false; yield break; }
            RefreshVolumes();
            if (fading) { int keep = channels[1 - activeChannel].volume > channels[activeChannel].volume ? 1 - activeChannel : activeChannel; channels[1 - keep].Stop(); activeChannel = keep; fading = false; }
            bool running = channels[activeChannel].isPlaying;
            int next = running ? 1 - activeChannel : activeChannel;
            channels[next].Stop(); channels[next].clip = clip; channels[next].volume = 0; channels[next].Play();
            // Playback can be suspended while a streamed clip is becoming ready.
            if (suspended) channels[next].Pause();
            CurrentId = id; loading = false;
            if (running) { fromGain = AppPreferences.Gain(1) > 0 ? channels[activeChannel].volume / AppPreferences.Gain(1) : 1; fadeAt = Time.unscaledTime; fading = true; }
            else { activeChannel = next; fading = false; }
            RefreshVolumes();
        }
        void RefreshVolumes() {
            float gain = AppPreferences.Gain(1) * (AuditionPlaying || RiichiPlaying || ExternalAudition ? 0 : 1);
            audition.volume = AppPreferences.Gain(1);
            if (fading) {
                float t = Mathf.Clamp01((Time.unscaledTime - fadeAt) / FadeSeconds);
                channels[activeChannel].volume = gain * fromGain * Mathf.Cos(t * Mathf.PI / 2);
                channels[1 - activeChannel].volume = gain * Mathf.Sin(t * Mathf.PI / 2);
                if (t >= 1) { channels[activeChannel].Stop(); activeChannel = 1 - activeChannel; fading = false; }
            } else { channels[activeChannel].volume = gain; channels[1 - activeChannel].volume = 0; }
        }
        public void Audition(int id) {
            if (id == AuditionId && audition.isPlaying) { audition.Pause(); return; }
            if (id != AuditionId) { audition.Stop(); audition.clip = Clip(id); AuditionId = id; }
            audition.volume = AppPreferences.Gain(1); if (audition.clip != null) { audition.UnPause(); if (!audition.isPlaying) audition.Play(); }
        }
        public void StopAudition() { audition.Stop(); AuditionId = 0; }
        void OnApplicationPause(bool pause) { suspended = pause; foreach (var s in channels.Concat(new[] { audition,riichi })) { if (pause) s.Pause(); else s.UnPause(); } }
        void OnDestroy() { if (Active == this) Active = null; }
    }
}
