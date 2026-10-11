using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace YunJian.Riichi
{
    [Serializable] public class VoiceManifest { public string provider; public VoiceEntry[] clips; }
    [Serializable] public class VoiceEntry { public string key, resource, webPath, source; }
    public class RoleVoice : MonoBehaviour
    {
        AudioSource output;
        Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly Queue<Tuple<string, float>> queue = new Queue<Tuple<string, float>>();
        string room;
        int cursor;
        bool enabledVoice = true, suspended;
        float last;
        public bool HasYiji { get; private set; }
        void Awake()
        {
            output = gameObject.AddComponent<AudioSource>(); output.spatialBlend = 0; output.playOnAwake = false;
            var file = Resources.Load<TextAsset>("Majsoul/voice-manifest");
            if (file == null) return;
            var manifest = JsonUtility.FromJson<VoiceManifest>(file.text);
            HasYiji = manifest.provider == "yiji";
            foreach (var e in manifest.clips) { var clip = Resources.Load<AudioClip>(e.resource); if (clip != null) clips[e.key] = clip; }
        }
        public void Configure(bool enabled, float volume) { enabledVoice = enabled; output.volume = Mathf.Clamp01(volume); if (!enabled) Stop(); }
        void Stop() { output.Stop(); queue.Clear(); }
        public void Reset() { Stop(); room = null; cursor = 0; }
        public void Suspend(bool flag) { suspended = flag; Reset(); }
        public void Preview() { Stop(); if (clips.TryGetValue("ron", out var clip)) output.PlayOneShot(clip); }
        public void Observe(RoomView state){if(string.IsNullOrEmpty(state.audioRoom)){Reset();return;}if(room!=state.audioRoom){Reset();room=state.audioRoom;cursor=state.audioSeq;last=Time.unscaledTime;}}
        public void QueuePresented(string key,PublicSound e){
            if(room!=key || e.id<=cursor)return;cursor=e.id;if(!enabledVoice || suspended)return;
            if(e.kind=="ron" || e.kind=="tsumo")Stop();string name=e.kind=="discard"?e.tile:e.kind;
            if(name!=null && clips.ContainsKey(name))queue.Enqueue(Tuple.Create(name,Time.unscaledTime));
        }
        public void Accept(RoomView state)
        {
            if (string.IsNullOrEmpty(state.audioRoom)) { Reset(); return; }
            if (room != state.audioRoom) { Reset(); room = state.audioRoom; cursor = state.audioSeq; last = Time.unscaledTime; return; }
            bool stale = Time.unscaledTime - last >= 8; last = Time.unscaledTime;
            var events = (state.audioEvents ?? new PublicSound[0]).Where(e => e.id > cursor).ToArray(); cursor = Math.Max(cursor, state.audioSeq);
            if (!enabledVoice || suspended || stale) return;
            if (events.Any(e => e.kind == "ron" || e.kind == "tsumo")) { Stop(); events = events.Where(e => e.kind == "ron" || e.kind == "tsumo").ToArray(); }
            foreach (var e in events)
            {
                string key = e.kind == "discard" ? e.tile : e.kind;
                if (clips.ContainsKey(key)) queue.Enqueue(Tuple.Create(key, Time.unscaledTime));
            }
            while (queue.Count > 8) queue.Dequeue();
        }
        void Update()
        {
            if (output.isPlaying || !enabledVoice || suspended) return;
            while (queue.Count > 0)
            {
                var item = queue.Dequeue(); if (Time.unscaledTime - item.Item2 >= 8) continue;
                output.clip = clips[item.Item1]; output.Play(); return;
            }
        }
    }
}
