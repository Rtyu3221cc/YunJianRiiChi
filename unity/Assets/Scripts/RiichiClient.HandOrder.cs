using System.Collections.Generic;
using System.Linq;

namespace YunJian.Riichi {
    public partial class RiichiClient {
        readonly ClientHandOrder localHand=new ClientHandOrder();
        int selectedHandId=-1;
        bool UsesLocalHand(RoomView view)=>view==State&&view!=null&&(view.phase=="play"||view.phase=="reaction");
        public IReadOnlyList<ClientHandTile> OrderedHand(RoomView view){
            var canonical=TableRenderer.DisplayHand(view);
            if(!UsesLocalHand(view))return canonical.Select((face,index)=>new ClientHandTile{Id=-index-1,Face=face,Drawn=!string.IsNullOrEmpty(view.drawn)&&index==canonical.Count-1}).ToArray();
            localHand.Reconcile(view.audioRoom+":"+view.matchSeq+":"+view.handSeq+":"+view.you,canonical,view.drawn,AppPreferences.AutoSortHand);return localHand.Tiles;
        }
        public int LocalHandId(int index){var hand=OrderedHand(State);return index>=0&&index<hand.Count?hand[index].Id:-1;}
        public bool LocalHandIsDrawn(int index)=>index>=0&&index<localHand.Tiles.Count&&localHand.Tiles[index].Drawn;
        public int LocalDrawnIndex=>UsesLocalHand(State)?localHand.DrawnIndex:-1;
        public bool LocalDrawGap=>UsesLocalHand(State)?localHand.ShowDrawGap:!string.IsNullOrEmpty(State?.drawn);
        public int PendingHandDiscard=>localHand.PendingDiscard;
        public void RefreshLocalHandSelection()=>RebindSelection();
        void ChooseHand(int index){
            var hand=OrderedHand(State);if(index<0||index>=hand.Count)return;var tile=hand[index];
            SelectedIndex=index;selectedHandId=tile.Id;selectedFace=tile.Face;selectedCopy=hand.Take(index).Count(t=>t.Face==tile.Face);
            SelectedAction=TableRenderer.HandChoice(State,tile.Face,tile.Drawn,RiichiMode)?.id;table.SelectHand(index);
            if(SelectedAction!=null&&(ActiveWaitHint?.waits?.Length??0)>0)waitHoldVisible=true;
        }
        void SubmitHandAction(string action,int physicalId){
            if(Busy||!OptionsReady)return;localHand.MarkDiscard(physicalId);ClearSelection();table.SelectHand(-1,true);table.RefreshSubmittedHand();Act(action);
        }
        void RestoreSubmittedHand(){localHand.CancelDiscard();table?.RefreshSubmittedHand();}
        bool ReorderPressedHand(int index,int destination){
            if(!localHand.Move(index,destination))return false;
            SelectedIndex=localHand.IndexOf(selectedHandId);table.ReflowHandOrder();return true;
        }
#if UNITY_EDITOR
        public int[] ValidationHandIds=>OrderedHand(State).Select(t=>t.Id).ToArray();
        public string[] ValidationHandFaces=>OrderedHand(State).Select(t=>t.Face).ToArray();
        public void ValidationRestoreSubmittedHand()=>RestoreSubmittedHand();
#endif
    }
}
