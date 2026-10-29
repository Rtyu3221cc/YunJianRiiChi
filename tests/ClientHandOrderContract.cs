using System;
using System.Linq;
using System.Collections.Generic;
using YunJian.Riichi;
static class Program {
 static int checks;
 static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
 static string Faces(ClientHandOrder h)=>string.Join(",",h.Tiles.Select(t=>t.Face));
 static void Main(){
  Check(!OriginalHandInputRules.StartsDrag(20,0),"Exactly 20 pixels is not a drag");Check(OriginalHandInputRules.StartsDrag(20.01f,0),"More than 20 pixels is a drag");Check(!OriginalHandInputRules.StartsDrag(12,16),"Diagonal distance exactly 20");Check(OriginalHandInputRules.StartsDrag(13,16),"Diagonal threshold uses squared distance");
  Check(OriginalHandInputRules.Boundary(50,200,0)==150,"Native projected Y formula");Check(OriginalHandInputRules.InHand(149.99f,150),"Below boundary stays in hand");Check(!OriginalHandInputRules.InHand(150,150),"Exact boundary is outside hand");Check(OriginalHandInputRules.IndexAt(-100,10,50,14)==0,"Left clamp");Check(OriginalHandInputRules.IndexAt(10000,10,50,14)==13,"Right clamp");Check(OriginalHandInputRules.IndexAt(110.1f,10,50,14)==2,"Floor-based projected slot");
  var h=new ClientHandOrder();var start=new[]{"m0","m5","m5","p2","z1","m5"};h.Reconcile("room/1",start,"m5",false);int drawn=h.Tiles.Last().Id,red=h.Tiles.First().Id,heldFive=h.Tiles[1].Id;
  Check(h.ShowDrawGap&&h.Tiles.Last().Drawn,"Initial drawn identity and gap");Check(h.Move(5,1),"Drawn copy can move into the hand");Check(h.Tiles[1].Id==drawn&&h.Tiles[1].Drawn,"Drawn identity survives local move");Check(!h.Tiles.First(t=>t.Id==heldFive).Drawn,"Identical held five stays held");Check(h.Tiles.First().Face=="m0"&&h.Tiles.First().Id==red,"Red five stays distinct");Check(!h.ShowDrawGap,"Reordering clears the separated draw slot");
  var local=h.Tiles.Select(t=>t.Id).ToArray();h.Reconcile("room/1",start,"m5",false);Check(local.SequenceEqual(h.Tiles.Select(t=>t.Id)),"Presence poll preserves order and physical IDs");
  h.MarkDiscard(heldFive);h.Reconcile("room/1",start,"m5",false);Check(h.IndexOf(heldFive)>=0,"Poll before acknowledgement retains submitted tile");
  h.Reconcile("room/1",new[]{"m0","m5","p2","z1","m5"},null,false);Check(h.IndexOf(heldFive)<0&&h.IndexOf(drawn)>=0,"Acknowledgement removes the actual selected held copy");Check(h.Tiles.All(t=>!t.Drawn),"Previous drawn tile becomes held");
  h.Reconcile("room/1",new[]{"m0","m5","m5","p2","z1","m5"},"m5",false);int nextDraw=h.Tiles.Last().Id;Check(nextDraw!=drawn&&h.Tiles.Last().Drawn,"A same-face new draw gets a new identity");Check(h.IndexOf(drawn)>=0&&!h.Tiles.First(t=>t.Id==drawn).Drawn,"Old drawn copy does not become the new draw");
  h.MarkDiscard(nextDraw);h.CancelDiscard();h.Reconcile("room/1",new[]{"m0","m5","m5","p2","z1","m5"},"m5",false);Check(h.IndexOf(nextDraw)>=0&&h.PendingDiscard<0,"Rejected action restores its physical tile");
  h.Reconcile("room/1",start,"m5",true);Check(Faces(h)==string.Join(",",start),"Auto sort restores canonical order");Check(!h.Move(0,4),"Auto sort disables manual reorder");h.Reconcile("room/1",start,"m5",false);Check(h.Move(0,4),"Manual setting enables reorder");
  h.Reconcile("room/2",start,"m5",false);Check(Faces(h)==string.Join(",",start),"New hand resets display order");Check(h.Tiles.All(t=>!local.Contains(t.Id)),"New hand does not reuse physical IDs");
  var rng=new Random(180029);for(int round=0;round<40;round++){
   var wire=Enumerable.Range(0,14).Select(i=>new[]{"m0","m5","m5","p1","s9","z7"}[rng.Next(6)]).ToArray();h.Reset();h.Reconcile("property/"+round,wire,wire.Last(),false);var ids=h.Tiles.Select(t=>t.Id).OrderBy(i=>i).ToArray();int originalDraw=h.Tiles.Last().Id;
   for(int move=0;move<12;move++){h.Move(rng.Next(14),rng.Next(14));Check(ids.SequenceEqual(h.Tiles.Select(t=>t.Id).OrderBy(i=>i)),"Move preserves the physical multiset");Check(h.Tiles.Single(t=>t.Drawn).Id==originalDraw,"Move preserves drawn identity");var order=h.Tiles.Select(t=>t.Id).ToArray();h.Reconcile("property/"+round,wire,wire.Last(),false);Check(order.SequenceEqual(h.Tiles.Select(t=>t.Id)),"Poll preserves arbitrary manual permutation");}
  }
  Console.WriteLine("ORIGINAL INPUT PURE PASS "+checks);
 }
}
