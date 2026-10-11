using System;
using System.Collections.Generic;
using UnityEngine;
namespace YunJian.Riichi
{
    // Native NewGame: four-tile groups at 0/.3/.6/.9, 200 ms fade/drop,
    // LiPai at 1.2 s, then constant 25 tile widths/s horizontal movement.
    public sealed class HandOpening
    {
        const float End=1.76f;
        string key;
        string[] deal=new string[0];
        float began;
        int played=-1;
        bool running;
        List<Transform> lastTiles;List<Vector3> lastBases;
#if UNITY_EDITOR
        public bool Freeze;
        public float FrozenAge;
#endif
        float Age {
            get {
#if UNITY_EDITOR
                if(Freeze)return FrozenAge;
#endif
                return Time.unscaledTime-began;
            }
        }
        public bool Active=>running && Age<End;
        public int Started { get; private set; }
        public void Observe(RoomView view)
        {
            if(view.handSeq<=0){running=false;return;}
            string next=view.audioRoom+":"+view.handSeq;
            if(next==key)return;
            key=next;running=false;
            long age=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()-view.dealtAt;
            var p=view.you>=0 && view.you<view.players.Length?view.players[view.you]:null;
            if((view.phase!="play" && view.phase!="reaction") || age<0 || age>2500 || p==null || p.river.Length>0 || p.melds.Length>0 || p.nuki>0 || view.openingHand.Length<13 || !(view.openingHand.Length==view.hand.Length || view.openingHand.Length==13 && view.hand.Length==14 && !string.IsNullOrEmpty(view.drawn)))return;
            deal=(string[])view.openingHand.Clone();began=Time.unscaledTime;played=-1;running=true;Started++;
        }
        public void Apply(List<Transform> tiles,List<Vector3> bases,GameAudio audio)
        {
            if(!running)return;
            lastTiles=tiles;lastBases=bases;
            float age=Age;
            bool extraDraw=deal.Length==13 && tiles.Count==14;
            if(tiles.Count!=deal.Length && !extraDraw){Cancel();return;}
            var used=new bool[deal.Length];var slots=new int[tiles.Count];
            for(int i=0;i<tiles.Count;i++){
                if(extraDraw && i==tiles.Count-1){slots[i]=-1;continue;}
                string face=tiles[i].name.Substring(5);int at=-1;
                // Preserve the dealer's actual drawn copy when faces repeat.
                if(i==tiles.Count-1 && deal.Length==14 && deal[deal.Length-1]==face)at=deal.Length-1;
                else for(int j=0;j<deal.Length;j++)if(!used[j] && deal[j]==face){at=j;break;}
                if(at<0){Cancel();return;}used[at]=true;slots[i]=at;
            }
            int batch=Mathf.Min(3,(int)(age/.3f));
            while(played<batch){played++;audio?.Deal();}
            for(int i=0;i<tiles.Count;i++){
                int slot=slots[i];bool extra=slot<0;float fade=extra?(age>=End?1:0):Mathf.Clamp01((age-slot/4*.3f)/.2f);
                Vector3 from=new Vector3(bases[0].x+slot*OriginalTiles.HandWidth,0,0);
                var pos=from;
                if(age>=1.2f)pos.x=Mathf.MoveTowards(from.x,bases[i].x,(age-1.2f)*OriginalTiles.HandWidth*25);
                pos.y=OriginalTiles.HandWidth*(.5f/2.55f)*(1-fade);
                if(extra)pos=bases[i];
                tiles[i].localPosition=pos;
                var renderer=tiles[i].GetComponent<MeshRenderer>();var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetFloat("_Opacity",fade);renderer.SetPropertyBlock(block);
            }
            if(age>=End){running=false;for(int i=0;i<tiles.Count;i++)tiles[i].localPosition=bases[i];}
        }
        public void Cancel(){running=false;
            if(lastTiles!=null)for(int i=0;i<lastTiles.Count;i++)if(lastTiles[i]!=null){
                lastTiles[i].localPosition=lastBases[i];var renderer=lastTiles[i].GetComponent<MeshRenderer>();
                var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetFloat("_Opacity",1);renderer.SetPropertyBlock(block);
            }
        }
        public void Reset(){Cancel();key=null;deal=new string[0];}
    }
}
