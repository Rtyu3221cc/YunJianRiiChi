using System;
using System.Collections.Generic;
using System.Linq;

namespace YunJian.Riichi {
    // Rules recovered from ViewPlayer_Me._getMouseInfo / _update, not R17 estimates.
    public static class OriginalHandInputRules {
        public const float DragDistanceSquared=400;
        public const float SelectedLift=.8f;
        public static bool StartsDrag(float dx,float dy)=>dx*dx+dy*dy>DragDistanceSquared;
        public static bool InHand(float pointerY,float handBoundaryY)=>pointerY<handBoundaryY;
        public static float Boundary(float originY,float upperY,float lowerY)=>upperY-originY+lowerY;
        public static int IndexAt(float pointerX,float originX,float pitch,int count){if(count<=0)return -1;return Math.Max(0,Math.Min(count-1,(int)Math.Floor((pointerX-originX)/pitch)));}
    }
    public sealed class ClientHandTile {
        public int Id {get;internal set;}
        public string Face {get;internal set;}
        public bool Drawn {get;internal set;}
    }
    // Local display order never changes the authoritative room hand or action IDs.
    public sealed class ClientHandOrder {
        readonly List<ClientHandTile> tiles=new List<ClientHandTile>();
        string context,physicalKey;int nextId,pending=-1;bool autoSort,suppressDrawGap;
        public IReadOnlyList<ClientHandTile> Tiles=>tiles;
        public int PendingDiscard=>pending;
        public bool ShowDrawGap=>!suppressDrawGap&&tiles.Count>0&&tiles[tiles.Count-1].Drawn;
        public int DrawnIndex=>tiles.FindIndex(t=>t.Drawn);
        public int IndexOf(int id)=>tiles.FindIndex(t=>t.Id==id);
        public void Reset(){tiles.Clear();context=physicalKey=null;pending=-1;suppressDrawGap=false;}
        public void MarkDiscard(int id){pending=IndexOf(id)>=0?id:-1;}
        public void CancelDiscard(){pending=-1;}
        public bool Move(int from,int to){
            if(autoSort||from<0||from>=tiles.Count||to<0||to>=tiles.Count||from==to)return false;
            var tile=tiles[from];tiles.RemoveAt(from);tiles.Insert(to,tile);suppressDrawGap=true;return true;
        }
        public void Reconcile(string roomHand,IReadOnlyList<string> canonical,string drawn,bool sort){
            string key=string.Join(",",canonical)+"|"+drawn;
            bool fresh=context!=roomHand,changed=fresh||physicalKey!=key;
            if(!changed&&autoSort==sort)return;
            var previousDrawn=tiles.FirstOrDefault(t=>t.Drawn);var added=new List<ClientHandTile>();
            if(fresh){tiles.Clear();pending=-1;previousDrawn=null;suppressDrawGap=false;}
            else if(changed&&pending>=0){tiles.RemoveAll(t=>t.Id==pending);pending=-1;}
            var required=canonical.GroupBy(s=>s).ToDictionary(g=>g.Key,g=>g.Count());
            foreach(var face in tiles.Select(t=>t.Face).Distinct().ToArray()){
                int excess=tiles.Count(t=>t.Face==face)-(required.TryGetValue(face,out int need)?need:0);
                while(excess-->0){int at=tiles.FindLastIndex(t=>t.Face==face&&!t.Drawn);if(at<0)at=tiles.FindLastIndex(t=>t.Face==face);tiles.RemoveAt(at);}
            }
            foreach(var face in canonical){
                int have=tiles.Count(t=>t.Face==face);
                if(have>=required[face])continue;
                var tile=new ClientHandTile{Id=++nextId,Face=face};tiles.Add(tile);added.Add(tile);
            }
            foreach(var tile in tiles)tile.Drawn=false;
            ClientHandTile newDrawn=null;
            if(!string.IsNullOrEmpty(drawn)){
                newDrawn=added.LastOrDefault(t=>t.Face==drawn);
                if(newDrawn==null&&previousDrawn!=null&&previousDrawn.Face==drawn&&tiles.Contains(previousDrawn))newDrawn=previousDrawn;
                if(newDrawn==null)newDrawn=tiles.LastOrDefault(t=>t.Face==drawn);
                if(newDrawn!=null)newDrawn.Drawn=true;
            }
            if(newDrawn!=previousDrawn&&newDrawn!=null){tiles.Remove(newDrawn);tiles.Add(newDrawn);suppressDrawGap=false;}
            if(fresh||sort){
                var available=tiles.ToList();tiles.Clear();
                for(int i=0;i<canonical.Count;i++){
                    bool isDrawn=!string.IsNullOrEmpty(drawn)&&i==canonical.Count-1;
                    var tile=available.First(t=>t.Face==canonical[i]&&t.Drawn==isDrawn);available.Remove(tile);tiles.Add(tile);
                }
                suppressDrawGap=false;
            }
            context=roomHand;physicalKey=key;autoSort=sort;
        }
    }
}
