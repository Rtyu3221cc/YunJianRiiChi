using System.Linq;

namespace YunJian.Riichi {
    // Scripted, purely local table states for the wardrobe preview. They are
    // built from project data only and never reach the room host. `seat` is the
    // player who performs (0 viewer, 1 right, 2 across, 3 left); that seat wears
    // the previewed outfit, and the viewer always keeps theirs for the table.
    public static class DressPreviewStates {
        static readonly string[][] Rivers={
            new[]{"z1","m9","p1","s8","z6","m2"},
            new[]{"z3","p9","s1","m7","z5","p2","s9"},
            new[]{"z4","m1","s2","p8","z7","m3"},
            new[]{"z2","s7","p1","m8","z6","s3","p7"}
        };
        // Own concealed hand before the draw; the drawn tile is appended.
        static readonly string[] Hand={"m2","m3","m4","p3","p4","p5","s5","s6","s7","z5","z5","p7","p8"};
        public const string RiichiDiscard="z1",WinTile="p6",CallTile="z5",DemoCharacter="yiji";
        public const int DemoSkin=400101;
        // Clockwise on screen from the viewer: self, left, across, right.
        public static readonly int[] ActingSeats={0,3,2,1},CallingSeats={3,2,1};
        public static string SeatName(int seat)=>seat==0?"自家":seat==1?"下家":seat==2?"对家":"上家";
        public static RoomView Base(string room,WardrobeLoadout own,string character,int skin,string nickname,int seat=0){
            var v=new RoomView{phase="play",audioRoom=room,pin="",n=4,you=0,turn=0,dealer=0,round=0,rounds=2,honba=0,sticks=0,remaining=46,
                handSeq=1,matchSeq=1,audioSeq=0,host=false,indicators=new[]{"z7"},lobby=new LobbyRoomView{category="friend",red=true}};
            // The other seats are fixed demo players so the preview never needs another pack.
            v.players=Enumerable.Range(0,4).Select(i=>new PlayerView{
                name=i==0?(string.IsNullOrWhiteSpace(nickname)?"我":nickname):"特效演示"+i,character=i==0?character:DemoCharacter,skin=i==0?skin:DemoSkin,
                wardrobe=i==0||i==seat?own:new WardrobeLoadout(),score=25000,wind=i,count=13,online=true,bot=i!=0,
                river=Rivers[i].Select(t=>new RiverTile{tile=t}).ToArray()}).ToArray();
            v.hand=Hand.ToArray();
            return v;
        }
        // The acting seat's turn with a drawn tile in hand.
        static RoomView Drawn(RoomView v,int seat,string tile){
            v.turn=seat;
            if(seat==0){v.hand=Hand.Concat(new[]{tile}).ToArray();v.drawn=tile;v.players[0].count=14;}else v.players[seat].count=14;
            return v;
        }
        public static RoomView BeforeRiichi(string room,WardrobeLoadout own,string character,int skin,string nickname,int seat=0)=>Drawn(Base(room,own,character,skin,nickname,seat),seat,RiichiDiscard);
        // The riichi tile leaves the hand sideways into the river, announced by one public discard event.
        public static RoomView Riichi(string room,WardrobeLoadout own,string character,int skin,string nickname,int seat=0){
            var v=Base(room,own,character,skin,nickname,seat);v.turn=(seat+1)%4;v.players[seat].riichi=1;
            var river=v.players[seat].river.ToList();river.Add(new RiverTile{tile=RiichiDiscard,riichi=true});v.players[seat].river=river.ToArray();
            v.audioSeq=1;v.audioEvents=new[]{new PublicSound{id=1,kind="discard",seat=seat,tile=RiichiDiscard,riichi=true,riverIndex=river.Count}};
            return v;
        }
        // Riichi stick preview: the declaration discard, then the hand that sets the stick down.
        public static RoomView RiichiStick(string room,WardrobeLoadout own,string character,int skin,string nickname,int seat=0){
            var v=Riichi(room,own,character,skin,nickname,seat);var discard=v.audioEvents[0];
            v.audioSeq=2;v.audioEvents=new[]{discard,new PublicSound{id=2,kind="riichi-stick",seat=seat}};v.sticks=1;
            return v;
        }
        public static RoomView BeforeWin(string room,WardrobeLoadout own,string character,int skin,string nickname,int seat=0)=>Drawn(Base(room,own,character,skin,nickname,seat),seat,WinTile);
        // Self-drawn win: m234 p345 s567 p678 z55.
        public static RoomView Tsumo(string room,WardrobeLoadout own,string character,int skin,string nickname,int seat=0){
            var v=Drawn(Base(room,own,character,skin,nickname,seat),seat,WinTile);v.phase="result";
            if(seat!=0)v.hand=Hand.ToArray();
            v.audioSeq=1;v.audioEvents=new[]{new PublicSound{id=1,kind="tsumo",seat=seat,tile=WinTile,from=seat,winSource="tsumo"}};
            var delta=Enumerable.Range(0,4).Select(i=>i==seat?3900:-1300).ToArray();
            v.result=new ResultView{title="自摸",delta=delta,details=new[]{new WinDetail{
                seat=seat,from=seat,winSource="tsumo",ron=false,tile=WinTile,name=v.players[seat].name,hand="m234p345678s567z55",
                tiles=Hand.ToArray(),melds=new string[0],ura=new string[0],limit="",
                yaku=new[]{new YakuDetail{name="门前清自摸和",han=1},new YakuDetail{name="平和",han=1}},
                score=new ScoreDetail{fu=20,fanshu=2,defen=3900,delta=delta}}}};
            return v;
        }
        // Another player discards a tile the viewer may pon; the callout marks it.
        public static RoomView Callable(string room,WardrobeLoadout own,string character,int skin,string nickname,int from=3){
            var v=Base(room,own,character,skin,nickname);v.phase="reaction";v.turn=from;
            var river=v.players[from].river.ToList();river.Add(new RiverTile{tile=CallTile});v.players[from].river=river.ToArray();
            v.pending=new PendingView{from=from,tile=CallTile,kind="discard",waiting=new[]{0}};v.last=new LastDiscard{from=from,tile=CallTile};
            v.options=new[]{new Choice{id="pon:preview",kind="pon",tile=CallTile,meld="z555"},new Choice{id="pass",kind="pass"}};
            return v;
        }
    }
}
