using System.Collections.Generic;
using UnityEngine;

namespace YunJian.Riichi {
    // Context lines share the lobby preview voice source. Calls and settlement
    // have priority and cancel ambient speech instead of talking over it.
    public sealed class CharacterVoicePlayback:MonoBehaviour {
        sealed class Pending {public CharacterVoiceCue cue;public string scope;public float expires;}
        readonly Queue<Pending> pending=new Queue<Pending>();
        readonly CharacterVoiceTriggers triggers=new CharacterVoiceTriggers();
        readonly Dictionary<string,int> interaction=new Dictionary<string,int>();
        CharacterVoices player;RoomView view;string scope;bool login,suspended;float blockedUntil,lastInteraction=-10;
        void Awake(){player=GetComponent<CharacterVoices>();}
        static string Scope(RoomView v)=>v==null?"home":v.phase=="lobby"?"lobby:"+v.audioRoom:"table:"+v.audioRoom+":"+v.matchSeq+":"+v.handSeq;
        public void Observe(RoomView next,bool returning=false){
            string nextScope=Scope(next);if(scope!=nextScope||returning){Cancel();player?.Stop();}view=next;scope=nextScope;
            foreach(var cue in triggers.Observe(next,returning))Enqueue(cue);
            if(next?.phase=="result")Cancel();
        }
        void Enqueue(CharacterVoiceCue cue){if(cue==null||suspended||AppPreferences.Gain(4)<=0)return;pending.Enqueue(new Pending{cue=cue,scope=scope,expires=Time.unscaledTime+10});while(pending.Count>3)pending.Dequeue();}
        public void Home(bool visible,string role){
            if(!visible||login||suspended)return;login=true;Enqueue(new CharacterVoiceCue(role,"lobby_playerlogin"));
        }
        public void Interact(string role){
            if(suspended||Time.unscaledTime-lastInteraction<1)return;lastInteraction=Time.unscaledTime;Cancel();
            var lines=CharacterVoiceCatalog.Available(role,"lobby_normal");if(lines.Length==0)return;
            int index=interaction.TryGetValue(role,out var prior)?(prior+1)%lines.Length:Random.Range(0,lines.Length);interaction[role]=index;
            player.TryPlay(role,lines[index].path,true);
        }
        public void Presented(PublicSound e){
            if(e.kind!="discard"){Cancel();blockedUntil=Time.unscaledTime+1.5f;return;}
            Enqueue(triggers.Discard(view,e));if(e.riichi)blockedUntil=Time.unscaledTime+2;
        }
        public void Tick(bool intro,bool blocked){
            if(PlaylistAudio.Active!=null)PlaylistAudio.Active.ContextVoicePlaying=!suspended&&player.Automatic&&Time.unscaledTime<player.EndsAt;
            if(suspended||AppPreferences.Gain(4)<=0){Cancel();return;}
            if(view?.phase=="result"){Cancel();return;}
            bool call=GetComponent<RoleVoice>()?.Speaking==true;
            if(call){if(player.Automatic)player.Stop();blockedUntil=Time.unscaledTime+.2f;}
            if(intro||blocked||call||Time.unscaledTime<blockedUntil||Time.unscaledTime<player.EndsAt)return;
            while(pending.Count>0){var item=pending.Dequeue();if(item.scope!=scope||Time.unscaledTime>item.expires)continue;if(player.TryPlay(item.cue.character,item.cue.key,true))break;}
        }
        public void Cancel(){pending.Clear();if(player!=null&&player.Automatic)player.Stop();if(PlaylistAudio.Active!=null)PlaylistAudio.Active.ContextVoicePlaying=false;}
        public void ResetRoom(){Cancel();view=null;scope="home";triggers.Reset();}
        public void Suspend(bool pause){suspended=pause;Cancel();player?.Stop();if(pause)triggers.Reset();}
        void OnDisable()=>Cancel();
    }
}
