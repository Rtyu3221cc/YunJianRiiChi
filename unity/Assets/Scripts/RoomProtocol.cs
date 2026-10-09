using System;

namespace YunJian.Riichi
{
    // This DTO contains only Room.view() public state, never the host snapshot.
    [Serializable] public class RoomReply { public bool ok, networkError; public string error, token; public RoomView state; }
    [Serializable] public class RoomView
    {
        public string phase, pin, recovery, drawn, audioRoom;
        public int version, you, n, rounds, round, dealer, honba, sticks, turn, remaining, audioSeq;
        public bool host, furiten;
        public PlayerView[] players = new PlayerView[0];
        public string[] hand = new string[0], indicators = new string[0], waits = new string[0], log = new string[0];
        public Choice[] options = new Choice[0];
        public PublicSound[] audioEvents = new PublicSound[0];
        public PendingView pending;
        public LastDiscard last;
        public ResultView result;
    }
    [Serializable] public class PlayerView
    {
        public string name, avatar;
        public bool bot, ready, online;
        public int score, wind, riichi, nuki, count;
        public string[] melds = new string[0];
        public RiverTile[] river = new RiverTile[0];
    }
    [Serializable] public class RiverTile { public string tile; public bool riichi, called, tsumogiri; }
    [Serializable] public class LastDiscard { public int from; public string tile; }
    [Serializable] public class PendingView { public int from; public string tile, kind; public bool riichi; public int[] waiting; }
    [Serializable] public class Choice { public string id, kind, tile, meld; }
    // 'riichi' marks the declaration discard so the client plays the sideways clip.
    [Serializable] public class PublicSound { public int id, seat; public string kind, tile; public bool riichi; }
    [Serializable] public class ResultView { public string title; public int[] delta; public bool matchOver, repeat; public WinDetail[] details; }
    [Serializable] public class WinDetail { public string name, hand, tile; public bool ron; public ScoreDetail score; }
    // 'fanshu' can be numeric or '*' upstream, so keep yaku parsing out of JsonUtility.
    [Serializable] public class ScoreDetail { public int fu, fanshu, defen, damanguan; }
    [Serializable] public class RequestBody
    {
        public string op, token, host, name, pin, recovery, action, id;
        public int version, n, rounds, seat;
        public bool red = true, bots, fillBots;
    }
    [Serializable] public class HostConfig { public string host, error; public string[] addresses; public bool ready; }
}
