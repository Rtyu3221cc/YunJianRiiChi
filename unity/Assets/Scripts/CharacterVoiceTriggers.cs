using System;
using System.Collections.Generic;
using System.Linq;

namespace YunJian.Riichi {
    public sealed class CharacterVoiceCue {
        public readonly string character,key;
        public CharacterVoiceCue(string character,string key){this.character=character;this.key=key;}
    }
    // Decisions use RoomView only. Private waits are evaluated for this viewer,
    // and never become room audio events or reveal another player's hand.
    public sealed class CharacterVoiceTriggers {
        string room,phase;int match,hand,remaining,discardCursor;bool ready;
        readonly HashSet<string> spoken=new HashSet<string>();
        public void Reset(){room=phase=null;match=hand=remaining=discardCursor=0;ready=false;spoken.Clear();}
        static bool Playing(RoomView v)=>v.phase=="play"||v.phase=="reaction";
        public List<CharacterVoiceCue> Observe(RoomView v,bool returning=false){
            var cues=new List<CharacterVoiceCue>();
            if(v==null||v.you<0||v.you>=v.players.Length||string.IsNullOrEmpty(v.audioRoom)){Reset();return cues;}
            var me=v.players[v.you];bool sameRoom=room==v.audioRoom,sameHand=sameRoom&&match==v.matchSeq&&hand==v.handSeq;
            if(!sameHand){spoken.Clear();discardCursor=v.audioSeq;}else if(returning)discardCursor=v.audioSeq;
            if(!returning){
                if(v.phase=="lobby"&&!sameRoom)cues.Add(new CharacterVoiceCue(me.character,"lobby_room_in"));
                else if(v.phase=="lobby"&&phase=="lobby"&&!ready&&me.ready)cues.Add(new CharacterVoiceCue(me.character,"lobby_room_ready"));
                if(sameRoom&&phase=="lobby"&&Playing(v)&&v.matchSeq!=match)cues.Add(new CharacterVoiceCue(me.character,"ingame_start"));
                if(sameHand&&Playing(v)&&remaining>=10&&v.remaining<10&&spoken.Add("ingame_remain10"))cues.Add(new CharacterVoiceCue(me.character,"ingame_remain10"));
            }
            room=v.audioRoom;phase=v.phase;match=v.matchSeq;hand=v.handSeq;remaining=v.remaining;ready=me.ready;
            return cues;
        }
        public CharacterVoiceCue Discard(RoomView v,PublicSound e){
            if(v==null||!Playing(v)||v.audioRoom!=room||v.matchSeq!=match||v.handSeq!=hand||e.id<=discardCursor)return null;
            discardCursor=e.id;
            if(e.kind!="discard"||e.seat!=v.you||v.you<0||v.you>=v.players.Length)return null;
            var river=v.players[v.you].river;int count=e.riverIndex;
            if(count<=0||count>river.Length)return null;
            var waits=v.waitDetails??Array.Empty<WaitDetail>();string key=null;
            // Only the latest committed discard can use the current private waits.
            if(count==river.Length){
                if(waits.Any(w=>w.yakumanRon>0||w.yakumanTsumo>0||w.ronHan>=13||w.tsumoHan>=13)&&!spoken.Contains("ingame_yiman"))key="ingame_yiman";
                else if(waits.Any(w=>w.ronHan>=8||w.tsumoHan>=8)&&!spoken.Contains("ingame_beiman"))key="ingame_beiman";
            }
            if(key==null&&count>=3&&Tile(river[count-1].tile)==Tile(river[count-2].tile)&&Tile(river[count-1].tile)==Tile(river[count-3].tile)&&!spoken.Contains("ingame_lianda"))key="ingame_lianda";
            if(key==null&&IsDora(v,e.tile)&&!spoken.Contains("ingame_baopai"))key="ingame_baopai";
            if(key==null)return null;spoken.Add(key);return new CharacterVoiceCue(v.players[v.you].character,key);
        }
        static string Tile(string tile)=>string.IsNullOrEmpty(tile)||tile.Length<2?"":tile.Substring(0,1)+(tile[1]=='0'?'5':tile[1]);
        public static bool IsDora(RoomView v,string tile){
            if(string.IsNullOrEmpty(tile)||tile.Length<2)return false;if(tile[1]=='0')return true;
            return (v.indicators??Array.Empty<string>()).Any(i=>Dora(i,v.n)==Tile(tile));
        }
        static string Dora(string indicator,int players){
            string tile=Tile(indicator);if(tile.Length!=2)return "";int n=tile[1]-'0';
            if(tile[0]=='z')return "z"+(n<=4?n%4+1:5+(n-4)%3);
            if(players==3&&tile[0]=='m')return n==1?"m9":n==9?"m1":"";
            return tile[0].ToString()+(n%9+1);
        }
        public static string TopKey(RoomView v,int seat){
            var win=v.result?.details?.FirstOrDefault(d=>d.seat==seat);
            if(win!=null&&(win.score?.damanguan>0||win.score?.fanshu>=8||win.limit=="倍满"||win.limit=="三倍满"))return "game_top_big";
            return win?.ron==true?"game_top_ron":"game_top";
        }
    }
}
