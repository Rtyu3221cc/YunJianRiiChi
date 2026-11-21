using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi {
    [Serializable] public sealed class MatchEntry { public int place, score, n, rounds, rankDelta, levelAfter; public bool ranked; public long at; }
    [Serializable] public sealed class ModeTotals {
        public int matches, hands, wins, tsumo, dealIns, riichi, calls, winTurns, winPoints, ura; public int[] places = new int[4];
        public void Add(SeatStats s,int place){matches++;if(place>=1&&place<=4)places[place-1]++;if(s==null)return;hands+=s.hands;wins+=s.wins;tsumo+=s.tsumo;dealIns+=s.dealIns;riichi+=s.riichi;calls+=s.calls;winTurns+=s.winTurns;winPoints+=s.winPoints;ura+=s.ura;}
    }
    [Serializable] public sealed class ModeRecord {
        public string category; public int n;
        public List<MatchEntry> games = new List<MatchEntry>();
        public ModeTotals totals = new ModeTotals();
        public BestHand best = new BestHand();
        public HighlightCounts highlights = new HighlightCounts();
    }
    [Serializable] public sealed class PlayerRecordData {
        public string playerId = "", title = "";
        public RankState rank4 = new RankState(), rank3 = new RankState();
        public int peak4, peak3;
        public List<string> owned = new List<string>();
        public List<string> processed = new List<string>();
        public List<ModeRecord> modes = new List<ModeRecord>();
    }
    // Style values for the profile radar, each 0-100.
    public struct PlayStyle { public int attack, defense, speed, luck; }

    // Local device record; PlayerPrefs JSON like CharacterRoster.
    public static class PlayerRecord {
        public const string PrefKey = "riichi.player.v1";
        public const int KeepGames = 100, KeepProcessed = 20;
        static PlayerRecordData data;
        // Editor previews and validation must not write the user's record.
        public static bool PreviewOnly;
        public static event Action Changed;
        public static string LastMatchKey { get; private set; } = "";
        public static RankChange LastChange { get; private set; }
        public static string LastCategory { get; private set; } = "";

        public static PlayerRecordData Data { get { Load(); return data; } }
        static void Load() {
            if (data != null) return;
            try { data = JsonUtility.FromJson<PlayerRecordData>(PlayerPrefs.GetString(PrefKey, "{}")); } catch { data = null; }
            Normalize(data ??= new PlayerRecordData());
            if (!PreviewOnly) Save();
        }
        static void Normalize(PlayerRecordData d) {
            d.rank4 ??= new RankState(); d.rank3 ??= new RankState(); d.owned ??= new List<string>(); d.processed ??= new List<string>(); d.modes ??= new List<ModeRecord>();
            foreach (var m in d.modes) { m.games ??= new List<MatchEntry>(); m.totals ??= new ModeTotals(); if (m.totals.places == null || m.totals.places.Length < 4) m.totals.places = new int[4]; m.best ??= new BestHand(); m.highlights ??= new HighlightCounts(); }
            if (string.IsNullOrEmpty(d.playerId) || d.playerId.Length != 9 || !d.playerId.All(char.IsDigit)) d.playerId = NewId();
            d.peak4 = Math.Max(d.peak4, d.rank4.level); d.peak3 = Math.Max(d.peak3, d.rank3.level);
            Unlock(d);
            if (string.IsNullOrEmpty(d.title) || !d.owned.Contains(d.title)) d.title = PlayerTitles.Default;
        }
        static string NewId() { var r = new System.Random(Guid.NewGuid().GetHashCode()); return (r.Next(1, 10) * 100000000 + r.Next(0, 100000000)).ToString(); }
        static void Save() { if (PreviewOnly) return; PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(data)); PlayerPrefs.Save(); }
        static void Unlock(PlayerRecordData d) { foreach (var t in PlayerTitles.All) if (!d.owned.Contains(t.id) && PlayerTitles.Earned(t, d)) d.owned.Add(t.id); }

        // Used by editor previews and validation; never persisted while PreviewOnly.
        public static void Replace(PlayerRecordData value) { data = value ?? new PlayerRecordData(); Normalize(data); LastChange = null; LastMatchKey = ""; Save(); Changed?.Invoke(); }

        public static RankState Rank(int n) => n == 3 ? Data.rank3 : Data.rank4;
        public static string Title => Data.title;
        public static string TitleName => PlayerTitles.NameOf(Data.title);
        public static bool Owns(string id) => Data.owned.Contains(id);
        public static void Equip(string id) { if (!Owns(id) || Data.title == id) return; Data.title = id; Save(); Changed?.Invoke(); }
        public static PlayerRankProfile Profile => new PlayerRankProfile { set = true, rank4 = Data.rank4.level, rank3 = Data.rank3.level, title = TitleName };
        public static ModeRecord Mode(string category, int n, bool create = false) {
            var m = Data.modes.FirstOrDefault(x => x.category == category && x.n == n);
            if (m == null && create) { m = new ModeRecord { category = category, n = n }; Data.modes.Add(m); }
            return m;
        }

        // Records the viewer's seat once per terminal result; repeated polls return the same change.
        public static RankChange Record(RoomView v) {
            var res = v?.result;
            if (res == null || !res.matchOver || res.standings == null || res.standings.Length == 0 || v.you < 0) return null;
            string key = (v.audioRoom ?? "") + ":" + v.matchSeq;
            Load();
            if (data.processed.Contains(key)) return key == LastMatchKey ? LastChange : null;
            var me = res.standings.FirstOrDefault(s => s.seat == v.you); if (me == null) return null;
            string category = !string.IsNullOrEmpty(res.rules?.category) ? res.rules.category : v.lobby?.category ?? "friend";
            int n = res.rules != null && res.rules.n > 0 ? res.rules.n : v.n, rounds = res.rules != null && res.rules.rounds > 0 ? res.rules.rounds : v.rounds, start = res.rules != null && res.rules.start > 0 ? res.rules.start : (n == 3 ? 35000 : 25000);
            var mode = Mode(category, n, true);
            var entry = new MatchEntry { place = me.place, score = me.score, n = n, rounds = rounds, at = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), ranked = category == "ranked" };
            mode.totals.Add(me.stats, me.place);
            var stats = me.stats;
            if (stats?.best != null && stats.best.Exists && (!mode.best.Exists || Better(stats.best, mode.best))) mode.best = stats.best;
            if (stats?.highlights != null) { var h = mode.highlights; var s = stats.highlights; h.yakuman += s.yakuman; h.kazoe += s.kazoe; h.sanbaiman += s.sanbaiman; h.baiman += s.baiman; h.nagashi += s.nagashi; h.doubleRiichi += s.doubleRiichi; }
            RankChange change = null;
            if (entry.ranked) {
                var rank = n == 3 ? data.rank3 : data.rank4;
                bool allSoul = RankRules.Soul(rank.level) && res.standings.Where(s => s.seat != v.you).All(s => !s.bot && s.seat < (v.players?.Length ?? 0) && v.players[s.seat].profile != null && v.players[s.seat].profile.set && RankRules.Soul(n == 3 ? v.players[s.seat].profile.rank3 : v.players[s.seat].profile.rank4));
                change = RankRules.Apply(rank, me.place, me.score, n, rounds, start, allSoul);
                if (n == 3) { data.rank3 = change.after; data.peak3 = Math.Max(data.peak3, change.after.level); } else { data.rank4 = change.after; data.peak4 = Math.Max(data.peak4, change.after.level); }
                entry.rankDelta = change.delta; entry.levelAfter = change.after.level;
            }
            mode.games.Add(entry); if (mode.games.Count > KeepGames) mode.games.RemoveRange(0, mode.games.Count - KeepGames);
            data.processed.Add(key); if (data.processed.Count > KeepProcessed) data.processed.RemoveRange(0, data.processed.Count - KeepProcessed);
            Unlock(data);
            LastMatchKey = key; LastChange = change; LastCategory = category;
            Save(); Changed?.Invoke();
            return change;
        }
        static bool Better(BestHand a, BestHand b) {
            int[] x = { a.damanguan, a.fanshu, a.fu, a.defen }, y = { b.damanguan, b.fanshu, b.fu, b.defen };
            for (int i = 0; i < x.Length; i++) if (x[i] != y[i]) return x[i] > y[i];
            return false;
        }
        public static bool Rename(string value, out string error) {
            value = (value ?? "").Trim(); int length = new System.Globalization.StringInfo(value).LengthInTextElements;
            if (length < 1 || length > 7) { error = "昵称需要 1–7 个字"; return false; }
            error = ""; return true;
        }

        // Profile style: 攻 win rate and value, 防 avoiding deal-ins, 速 early wins, 运 self-draws and ura hits.
        public static PlayStyle Style(ModeTotals t) {
            if (t == null || t.hands <= 0) return default;
            float winRate = t.wins / (float)t.hands, dealRate = t.dealIns / (float)t.hands, avgPoints = t.wins > 0 ? t.winPoints / (float)t.wins : 0, avgTurn = t.wins > 0 ? t.winTurns / (float)t.wins : 0;
            float tsumoRate = t.wins > 0 ? t.tsumo / (float)t.wins : 0, uraRate = t.riichi > 0 ? t.ura / (float)t.riichi : 0;
            int Clamp(float v) => Mathf.Clamp(Mathf.RoundToInt(v), 0, 100);
            return new PlayStyle {
                attack = Clamp(winRate / .3f * 60 + avgPoints / 8000f * 40),
                defense = Clamp((1 - dealRate / .2f) * 100),
                speed = t.wins > 0 ? Clamp((18 - avgTurn) / 12f * 100) : 0,
                luck = t.wins > 0 ? Clamp(tsumoRate * 60 + uraRate * 100 * .4f) : 0
            };
        }
    }
}
