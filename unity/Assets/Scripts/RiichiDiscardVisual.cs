using System.Collections.Generic;
using UnityEngine;

namespace YunJian.Riichi
{
    // This ledger survives presentation resets and phone suspension. A new hand
    // clears it; polling, table rebuilds and repeated declaration events do not.
    public sealed class RiichiDiscardVisual : MonoBehaviour
    {
        sealed class Burst { public PublicSound action; public WinTileEffect effect; public Transform carried,bound; public bool settled,ground,sceneOrigin; }
        readonly HashSet<int> shown = new HashSet<int>();
        readonly List<Burst> live = new List<Burst>();
        string hand; int players;
        TableRenderer table; TableEffects owner;
        public int PlayedCount { get; private set; }
        public int ActiveCount => live.Count;
        public WinTileEffect Latest => live.Count == 0 ? null : live[live.Count - 1].effect;
        void Awake() { table=GetComponent<TableRenderer>();owner=GetComponent<TableEffects>(); }
        public void Observe(RoomView view,bool baseline) {
            string next=view==null||string.IsNullOrEmpty(view.audioRoom)?null:view.audioRoom+":"+view.matchSeq+":"+view.handSeq+":"+view.round+":"+view.honba;
            if(next!=hand){StopVisuals();shown.Clear();hand=next;}
            players=view?.n??0;
            // Joining or resuming an already-declared hand must not replay old FX.
            if(baseline&&view!=null)for(int i=0;i<view.players.Length;i++)if(view.players[i].riichi>0)shown.Add(i);
        }
        Vector3 Point(Transform tile)=>SourcePlaybackContracts.LegacyRiichiPoint(tile);
        // Some source effects already encode the four seat orientations. Those
        // follow the tile's position while retaining their authored world plane.
        static Quaternion Rotation(Transform tile,WinTileEffect effect) => effect.TableRotation(tile.rotation);
        public bool TryPlay(PublicSound action,Transform carried,float now) {
            if(hand==null||action==null||action.kind!="discard"||!action.riichi||action.seat<0||action.seat>=players||carried==null||!shown.Add(action.seat))return false;
            var effect=new GameObject("Original fire riichi seat "+action.seat).AddComponent<WinTileEffect>();effect.transform.SetParent(transform,false);
            var choice=WardrobeRuntime.Item(table.Client.State,action.seat,"riichiEffect");
            // Block_QiPai parents both Shu and Heng effects to the carried model.
            // Shu chooses Dapai; it does not pin the effect to the cloth.
            bool ground=false;
            var binding=SourcePlaybackContracts.Binding(choice);
            bool sceneOrigin=binding.controller=="native-new";
            var point=sceneOrigin?SourcePlaybackContracts.TileModelOrigin(carried):Point(carried);
            bool native=!string.IsNullOrEmpty(choice.nativeEffect);
            float scale=ground?1:TableTileGeometry.WorldWidth(carried)/((native?TableTileGeometry.SourceWidth:SourcePlaybackContracts.TileWidth)*TableTileGeometry.SourceUnits);
            int sourceView=((int)Mathf.Round(Mathf.Repeat(-table.SeatAngle(action.seat),360)/90))%4;
            effect.InitializeTable(choice.effect,point,Quaternion.identity,now,(uint)(action.id+71),false,scale,sourceView);
            if(sceneOrigin){effect.WinningEvent=action;effect.BindWinningTile(carried);}
            else effect.BindLegacyRiichi(binding.action=="LiQiPai");
            if(!ground)effect.transform.rotation=Rotation(carried,effect);
            effect.Sample(-1);
            live.Add(new Burst{action=action,effect=effect,carried=carried,bound=carried,ground=ground,sceneOrigin=sceneOrigin});PlayedCount++;
            GetComponent<GameAudio>()?.Cue(choice.cue);return true;
        }
        public void Settle(PublicSound action) {
            foreach(var burst in live)if(burst.action==action){burst.settled=true;burst.carried=null;}
        }
        void LateUpdate() { Tick(owner!=null&&owner.ValidationFreeze?owner.ValidationTime:Time.unscaledTime); }
        public void Tick(float now) {
            for(int i=live.Count-1;i>=0;i--){
                var burst=live[i];var effect=burst.effect;if(effect==null){live.RemoveAt(i);continue;}
                var target=burst.settled?table.FindDiscard(burst.action.seat,burst.action.tile,burst.action.riverIndex):burst.carried;
                if(target!=null&&!burst.ground){
                    effect.transform.position=burst.sceneOrigin?SourcePlaybackContracts.TileModelOrigin(target):Point(target);
                    effect.transform.rotation=Rotation(target,effect);
                    if(burst.sceneOrigin&&target!=burst.bound){effect.BindWinningTile(target);burst.bound=target;}
                }
                float age=now-effect.StartedAt;effect.Sample(age);
                if(age>=4){effect.gameObject.SetActive(false);Destroy(effect.gameObject);live.RemoveAt(i);}
            }
        }
        public void StopVisuals() { foreach(var burst in live)if(burst.effect!=null){burst.effect.gameObject.SetActive(false);Destroy(burst.effect.gameObject);}live.Clear(); }
        void OnDestroy(){StopVisuals();}
    }
}
