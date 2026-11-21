using System;
using System.Linq;
using YunJian.Riichi;
// Only JSON parsing and unused DTO dependencies are stubbed. Trigger decisions
// compile from the production source and run without a Unity license/device.
namespace UnityEngine {public static class JsonUtility {public static T FromJson<T>(string text)=>default(T);}}
namespace YunJian.Riichi {public class WardrobeLoadout {} public class CharacterChoice {}}
static class CharacterVoiceTriggerTests {
    static int checks;
    static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
    static RoomView View(string phase="lobby",int hand=0)=>new RoomView{audioRoom="room",phase=phase,n=4,you=0,matchSeq=phase=="lobby"?0:1,handSeq=hand,remaining=40,players=Enumerable.Range(0,4).Select(i=>new PlayerView{character="role"+i}).ToArray()};
    static PublicSound Discard(int id,string tile="p5",int seat=0,int count=1)=>new PublicSound{id=id,kind="discard",seat=seat,tile=tile,riverIndex=count};
    static CharacterVoiceTriggers Playing(RoomView view){var t=new CharacterVoiceTriggers();t.Observe(view);return t;}
    static void Main(){
        var t=new CharacterVoiceTriggers();var v=View();
        Check(t.Observe(v).Single().key=="lobby_room_in","room entry");Check(t.Observe(v).Count==0,"poll dedupe");
        v.players[0].ready=true;Check(t.Observe(v).Single().key=="lobby_room_ready","own ready edge");Check(t.Observe(v).Count==0,"ready poll dedupe");
        v.players[1].ready=true;Check(t.Observe(v).Count==0,"other ready is not local speech");
        v.phase="play";v.matchSeq=1;v.handSeq=1;Check(t.Observe(v).Single().key=="ingame_start","match entry");Check(t.Observe(v).Count==0,"start once");
        v.remaining=10;Check(t.Observe(v).Count==0,"10 is not below 10");v.remaining=9;Check(t.Observe(v).Single().key=="ingame_remain10","wall threshold");v.remaining=8;Check(t.Observe(v).Count==0,"low wall once");
        v.handSeq=2;Check(t.Observe(v).Count==0,"new hand does not replay start or low wall");
        t=new CharacterVoiceTriggers();Check(t.Observe(v,true).Count==0,"resume skips history");t.Reset();Check(t.Observe(v).Count==0,"joining existing match skips start");
        v=View("reaction",1);v.players[0].river=new[]{new RiverTile{tile="p5"}};v.indicators=new[]{"p4"};t=Playing(v);
        Check(t.Discard(v,Discard(1)).key=="ingame_baopai","discard visible dora");Check(t.Discard(v,Discard(1))==null,"discard dedupe");Check(t.Discard(v,Discard(2))==null,"dora chatter once per hand");
        v=View("reaction",1);v.players[0].river=new[]{new RiverTile{tile="p1"},new RiverTile{tile="p1"},new RiverTile{tile="p1"}};t=Playing(v);
        Check(t.Discard(v,Discard(1,"p1",0,3)).key=="ingame_lianda","own three identical discards");
        v=View("reaction",1);v.players[0].river=new[]{new RiverTile{tile="p5"}};v.waitDetails=new[]{new WaitDetail{tile="z1",yakumanTsumo=1}};t=Playing(v);
        Check(t.Discard(v,Discard(1,"p5",1))==null,"private wait never speaks for others");Check(t.Discard(v,Discard(2)).key=="ingame_yiman","own yakuman wait");
        v.waitDetails=new[]{new WaitDetail{ronHan=8}};t=Playing(v);Check(t.Discard(v,Discard(1)).key=="ingame_beiman","baiman wait");
        v.waitDetails=new[]{new WaitDetail{ronHan=13}};t=Playing(v);Check(t.Discard(v,Discard(1)).key=="ingame_yiman","counted yakuman wait");
        v.waitDetails=new[]{new WaitDetail{ronHan=7}};t=Playing(v);Check(t.Discard(v,Discard(1))==null,"haneman is below baiman");
        v.audioSeq=10;t.Observe(v,true);Check(t.Discard(v,Discard(9,"s0"))==null,"reconnect skips presented history");
        v.waitDetails=new[]{new WaitDetail{ronHan=8}};v.players[0].river=new[]{new RiverTile{tile="p5"},new RiverTile{tile="p8"}};t=Playing(v);Check(t.Discard(v,Discard(1))==null,"old presentation cannot use latest private waits");
        v=View("play",1);Check(CharacterVoiceTriggers.IsDora(v,"s0"),"red dora");v.indicators=new[]{"z4","z7","p9"};Check(CharacterVoiceTriggers.IsDora(v,"z1")&&CharacterVoiceTriggers.IsDora(v,"z5")&&CharacterVoiceTriggers.IsDora(v,"p1"),"wind dragon and number wrap");Check(!CharacterVoiceTriggers.IsDora(v,"z2"),"non dora");v.n=3;v.indicators=new[]{"m1"};Check(CharacterVoiceTriggers.IsDora(v,"m9"),"sanma 1m indicator");v.indicators=new[]{"m9"};Check(CharacterVoiceTriggers.IsDora(v,"m1"),"sanma 9m indicator");
        v.result=new ResultView{details=new[]{new WinDetail{seat=1,ron=true,score=new ScoreDetail{fanshu=5}}}};Check(CharacterVoiceTriggers.TopKey(v,1)=="game_top_ron","ron top variant");v.result.details[0].score.fanshu=8;Check(CharacterVoiceTriggers.TopKey(v,1)=="game_top_big","baiman top variant");Check(CharacterVoiceTriggers.TopKey(v,0)=="game_top","winner without last hand win uses generic");
        v.result.details[0].score=null;v.result.details[0].ron=false;Check(CharacterVoiceTriggers.TopKey(v,1)=="game_top","null score generic");
        Console.WriteLine("Character voice trigger checks passed: "+checks);
    }
}
