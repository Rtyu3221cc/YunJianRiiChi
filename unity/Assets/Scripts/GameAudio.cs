using UnityEngine;
namespace YunJian.Riichi
{
    // Music never occupies the voice queue. Table sounds belong to animation landings.
    public sealed class GameAudio : MonoBehaviour
    {
        AudioSource music, effects;
        AudioClip deal, discard, meld, stick, tsumo;
        bool inTable, suspended, musicEnabled=true, effectsEnabled=true;
        float musicVolume=.35f;
        string room,drawKey;
        readonly System.Collections.Generic.Dictionary<string,AudioClip> cues=new System.Collections.Generic.Dictionary<string,AudioClip>();
        public readonly System.Collections.Generic.List<string> PlayedCues=new System.Collections.Generic.List<string>();
        public int DealSounds { get; private set; }
        public int LandingSounds { get; private set; }
        public bool HasResources => music.clip!=null && deal!=null && discard!=null && meld!=null && stick!=null && tsumo!=null;
        public bool MusicPlaying => music.isPlaying;
        public bool MusicLoops => music.loop;
        public float MusicTime => music.time;
        public float MusicVolume => music.volume;
        public bool EffectsPlaying => effects.isPlaying;
        void Awake()
        {
            music=gameObject.AddComponent<AudioSource>();effects=gameObject.AddComponent<AudioSource>();
            foreach(var source in new[]{music,effects}){source.playOnAwake=false;source.spatialBlend=0;source.ignoreListenerPause=false;}
            music.clip=Resources.Load<AudioClip>("Majsoul/Audio/game");music.loop=true;music.volume=0;
            deal=Load("newround_pais");discard=Load("discard_tile");meld=Load("fulu");stick=Load("put_liqi");tsumo=Load("zimo");
        }
        static AudioClip Load(string key)=>Resources.Load<AudioClip>("Majsoul/Audio/"+key);
        public void Configure(bool bgm,float bgmVolume,bool sfx,float sfxVolume)
        {
            musicEnabled=bgm;effectsEnabled=sfx;musicVolume=Mathf.Clamp01(bgmVolume);effects.volume=Mathf.Clamp01(sfxVolume);
            if(!sfx)effects.Stop();RefreshMusic();
        }
        public void SetScene(RoomView view)
        {
            bool table=view!=null && (view.phase=="play" || view.phase=="reaction" || view.phase=="result");
            if(!table){Reset();return;}
            if(room!=view.audioRoom){music.Stop();music.volume=0;effects.Stop();room=view.audioRoom;}
            inTable=true;RefreshMusic();
        }
        void RefreshMusic()
        {
            if(!inTable){music.Stop();return;}
            if(suspended || !musicEnabled){music.Pause();return;}
            if(!music.isPlaying){music.UnPause();if(!music.isPlaying && music.clip!=null)music.Play();}
        }
        void Update(){music.volume=Mathf.MoveTowards(music.volume,musicVolume,Time.unscaledDeltaTime*1.5f);}
        public void Suspend(bool pause){suspended=pause;if(pause)effects.Stop();RefreshMusic();}
        public void Reset(){inTable=false;room=null;drawKey=null;music.Stop();effects.Stop();music.volume=0;}
        void Play(AudioClip clip){if(clip!=null && effectsEnabled && !suspended)effects.PlayOneShot(clip);}
        public void Deal(){if(!inTable || suspended)return;DealSounds++;Play(deal);}
        public void Landing(string kind)
        {
            if(!inTable || suspended)return;
            AudioClip clip=kind=="discard" || kind=="nuki"?discard:kind=="riichi-stick"?stick:kind=="tsumo"?tsumo:kind=="chi" || kind=="pon" || kind=="kan"?meld:null;
            if(clip==null)return;LandingSounds++;Play(clip);
        }
        public void Cue(string name,float gain=1){
            if(!inTable || suspended || string.IsNullOrEmpty(name))return;
            if(!cues.TryGetValue(name,out var clip))cues[name]=clip=Load(name);
            if(clip==null)return;PlayedCues.Add(name);if(effectsEnabled)effects.PlayOneShot(clip,gain);
        }
        public void DrawReveal(string next){if(next==drawKey)return;drawKey=next;Cue("showtile");}
        public void PreviewDiscard(){Play(discard);}
    }
}
