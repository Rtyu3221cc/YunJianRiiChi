using UnityEngine;

namespace YunJian.Riichi {
    // Original AudioMgr.StopMusic / PlayMJBgm default to a 1000 ms ramp.
    // A held result must not restart that ramp on state polling or reconnect.
    public sealed class RoundMusicEnvelope {
        public const float Duration=1f;
        string playedRound,heldRound;
        float from=1,target=1,began;
        public bool Held=>heldRound!=null;
        public float Gain(float now)=>Mathf.Lerp(from,target,Mathf.Clamp01((now-began)/Duration));
        static string Key(RoomView view)=>view.audioRoom+":"+view.handSeq+":"+view.round+":"+view.honba;
        void To(float value,float now){if(target==value)return;from=Gain(now);target=value;began=now;}
        public void Observe(RoomView view,bool table,float now){
            if(!table||view==null){playedRound=heldRound=null;To(1,now);return;}
            string key=Key(view);
            if(view.phase=="result"&&view.result!=null&&!view.result.draw){
                if(heldRound!=key){heldRound=key;if(playedRound!=key){from=target=0;began=now;}else To(0,now);}
                return;
            }
            if(view.phase=="play"||view.phase=="reaction"){
                if(heldRound!=null&&heldRound!=key){heldRound=null;To(1,now);}
                playedRound=key;
            }
        }
    }
}
