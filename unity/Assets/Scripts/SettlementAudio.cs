using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi
{
    public sealed class SettlementAudio : MonoBehaviour
    {
        [Serializable] public class ClipEntry { public string key,resource;public float duration; }
        [Serializable] public class YakuEntry { public int id,order;public string[] names;public string sound; }
        [Serializable] class Manifest { public ClipEntry[] clips;public YakuEntry[] yaku; }
        public sealed class Row {public YakuDetail yaku;public string voice;public float at;}
        public sealed class Plan {public Row[] rows;public float fuAt,pointsAt,titleAt,end;public string titleVoice,drum;}
        static Manifest manifest;
        static readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        static readonly Dictionary<WinDetail,Plan> plans=new Dictionary<WinDetail,Plan>();
        static readonly Dictionary<WinDetail,string> characters=new Dictionary<WinDetail,string>();
        public static void BindCharacters(RoomView view){if(characters.Count>64){characters.Clear();plans.Clear();}foreach(var d in view?.result?.details??new WinDetail[0])if(d.seat>=0&&d.seat<view.players.Length)characters[d]=view.players[d.seat].character??"yiji";}
        static string Character(WinDetail detail)=>characters.TryGetValue(detail,out var c)?c:"yiji";
        AudioSource output;
        bool enabledVoice=true,suspended;
        string key;float previous=-1;int winner=-1,skipSerial;bool wasScores,wasFinal;
        public int VoicesPlayed {get;private set;}
        public readonly List<string> PlayedKeys=new List<string>();
        public bool HasResources=>Data.clips.All(c=>Clip(c.key)!=null);
        static Manifest Data=>manifest??(manifest=JsonUtility.FromJson<Manifest>(Resources.Load<TextAsset>("Majsoul/SettlementVoice/manifest").text));
        static AudioClip Clip(string name){if(string.IsNullOrEmpty(name))return null;if(!clips.TryGetValue(name,out var c)){var entry=Data.clips.FirstOrDefault(x=>x.key==name);clips[name]=c=entry==null?null:Resources.Load<AudioClip>(entry.resource);}return c;}
        public static float VoiceDuration(string name){var c=Clip(name);var entry=Data.clips.FirstOrDefault(x=>x.key==name);return Mathf.Max(.5f,Mathf.Max(entry?.duration??0,c?.length??0));}
        static YakuEntry Entry(YakuDetail y){return Data.yaku.FirstOrDefault(e=>e.names.Contains(y.name));}
        static string Wind(string name){if(name.Contains("東") || name.Contains("东"))return "dong";if(name.Contains("南"))return "nan";if(name.Contains("西"))return "xi";if(name.Contains("北"))return "bei";return null;}
        public static string YakuVoice(YakuDetail y,WinDetail d){
            var e=Entry(y);if(e!=null && !string.IsNullOrEmpty(e.sound))return e.sound;
            if(y.name.Contains("宝牌") || y.name.Contains("ドラ"))return y.han>0?"fan_dora"+Mathf.Min(13,y.han):null;
            string wind=Wind(y.name);if(wind!=null && (y.name.Contains("風") || y.name.Contains("风")))return "fan_"+(y.han==2 || (y.name.StartsWith("場風") || y.name.StartsWith("场风")) && (d.yaku??new YakuDetail[0]).Any(other=>(other.name.StartsWith("自風") || other.name.StartsWith("自风")) && Wind(other.name)==wind)?"double":"")+wind;
            return null;
        }
        public static string LimitVoice(WinDetail d){
            if(d.score!=null && d.score.damanguan>0)return "gameend_yiman"+Mathf.Clamp(d.score.damanguan,1,6);
            switch(d.limit){case "满贯":return "gameend_manguan";case "跳满":return "gameend_tiaoman";case "倍满":return "gameend_beiman";case "三倍满":return "gameend_sanbeiman";case "累计役满":case "累积役满":return "gameend_leijiyiman";case "役满":return "gameend_yiman1";default:return null;}
        }
        public static string LimitDrum(WinDetail d){
            string title=LimitVoice(d);if(title==null)return null;
            return title=="gameend_manguan" || title=="gameend_tiaoman"?"mangan":title=="gameend_beiman" || title=="gameend_sanbeiman"?"beiman":"yakuman";
        }
        public static Plan Sequence(WinDetail d){
            if(plans.TryGetValue(d,out var plan))return plan;
            if(plans.Count>32)plans.Clear();float at=.6f;
            var rows=(d.yaku??new YakuDetail[0]).Select((y,i)=>new{y,i,order=Entry(y)?.order??(Wind(y.name)!=null?(y.name.StartsWith("場風") || y.name.StartsWith("场风")?160:150):1000)}).OrderBy(x=>x.order).ThenBy(x=>x.i).Select(x=>new Row{yaku=x.y,voice=YakuVoice(x.y,d)}).ToArray();
            string character=Character(d);
            foreach(var row in rows){row.at=at;at+=character=="yiji"?VoiceDuration(row.voice):Mathf.Max(.5f,CharacterVoices.Clip(character,row.voice)?.length??.5f);}
            plan=new Plan{rows=rows,fuAt=at+(d.score?.damanguan>0?.7f:.3f),titleVoice=LimitVoice(d),drum=LimitDrum(d)};
            plan.pointsAt=plan.fuAt+.7f;plan.titleAt=plan.pointsAt+(plan.titleVoice==null?0:.7f);plan.end=plan.titleAt+(plan.titleVoice==null?.3f:.5f+(CharacterVoices.Clip(character,plan.titleVoice)?.length??Clip(plan.titleVoice)?.length??.5f)+.3f);plans[d]=plan;return plan;
        }
        void Awake(){output=gameObject.AddComponent<AudioSource>();output.playOnAwake=false;output.spatialBlend=0;}
        public void Configure(bool enabled,float volume){enabledVoice=enabled;output.volume=Mathf.Clamp01(volume);if(!enabled)output.Stop();}
        public void Suspend(bool flag){suspended=flag;if(flag)output.Stop();}
        public void Reset(){output.Stop();key=null;previous=-1;winner=-1;wasScores=false;wasFinal=false;}
        void Voice(string name,string character="yiji"){if(string.IsNullOrEmpty(name))return;PlayedKeys.Add(name);var clip=CharacterVoices.Clip(character,name);if(clip==null&&character=="yiji")clip=Clip(name);if(enabledVoice && !suspended && clip!=null){output.clip=clip;output.Play();VoicesPlayed++;}}
        // Called once per frame from the client, never from repeated GUI passes.
        public void Tick(RoomView view,ResultPresentation display,bool visible,GameAudio audio,bool validation=false){
            if(view?.result==null || view.phase!="result"){if(key!=null)Reset();return;}
            string next=view.audioRoom+":"+view.round+":"+view.honba+":"+view.result.title+":"+string.Join(",",view.result.delta??new int[0]);
            if(key!=next){Reset();key=next;}
            if(!visible || suspended)return;
            float age=display.ElapsedAge;
            if(winner!=display.WinnerIndex || wasScores!=display.ShowingScores || wasFinal!=display.ShowingFinal){output.Stop();if(display.ShowingFinal)audio.ClearHandEffects();previous=-1;winner=display.WinnerIndex;wasScores=display.ShowingScores;wasFinal=display.ShowingFinal;}
            if(display.Freeze && !validation){previous=age;return;}
            bool crossed(float at)=>previous<at && age>=at;
            // A skipped/frozen page jumps to the final display without bursting every old sound.
            bool jump=display.SkipSerial!=skipSerial || previous<0 && age>.35f;skipSerial=display.SkipSerial;
            if(jump)output.Stop();
            if(!jump){
                if(display.ShowingDraw){
                    if(crossed(0)){string abort=view.result.title=="九种九牌"?"gameend_jiuzhongjiupai":view.result.title=="四风连打"?"gameend_sifenglianda":view.result.title=="四家立直"?"gameend_sijializhi":view.result.title=="四杠散了"?"gameend_sigangliuju":null;Voice(abort,view.players[view.result.abortHand?.seat??view.you].character);}
                }else if(display.ShowingFinal){}
                else if(display.ShowingScores){
                    const float step=.15f;
                    int beat=Mathf.FloorToInt((age-ResultPresentation.ScoreCountStart)/step),priorBeat=Mathf.FloorToInt((previous-ResultPresentation.ScoreCountStart)/step);
                    if(age>=ResultPresentation.ScoreCountStart&&age<ResultPresentation.ScoreCountEnd&&beat>priorBeat&&view.result.delta?.Any(d=>d!=0)==true)audio.Cue("score_change");
                }
                else if(view.result.details!=null && winner<view.result.details.Length){
                    var detail=view.result.details[winner];var plan=Sequence(detail);string character=view.players[detail.seat].character;foreach(var row in plan.rows)if(crossed(row.at)){Voice(row.voice,character);audio.Cue("hule_fan_out",row.voice==null?1:.5f);}
                    if(crossed(plan.fuAt))audio.Cue("fu_appear");if(crossed(plan.pointsAt))audio.Cue("score_appear");
                    if(plan.titleVoice!=null){if(crossed(plan.titleAt+(plan.drum=="yakuman"?.15f:.3f)))audio.Cue(plan.drum);if(crossed(plan.titleAt+.5f))Voice(plan.titleVoice,character);}
                }
            }
            // Rank arrival is still audible when the player skips its entrance.
            if(display.ShowingScores&&!display.ShowingFinal&&crossed(ResultPresentation.RankAt(view.n,1)))audio.Cue("rank1");
            previous=age;
        }
    }
}
