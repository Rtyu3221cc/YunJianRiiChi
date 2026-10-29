using System;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi {
    public struct HandPointerGeometry {
        public float OriginX,Pitch,BoundaryY;public int Count;
        public int IndexAt(float x)=>OriginalHandInputRules.IndexAt(x,OriginX,Pitch,Count);
    }
    // Source: ViewPlayer_Me._update 1269-1378. Pointer-up does not start a new drag.
    public sealed class HandDiscardGesture {
        public bool Captured {get;private set;}public bool Dragging {get;private set;}
        public Vector2 Start,Point,ScreenSize;public HandPointerGeometry Geometry;
        public Rect DropZone=>Rect.MinMaxRect(0,Geometry.BoundaryY,ScreenSize.x,ScreenSize.y);
        public void Begin(Vector2 point,HandPointerGeometry geometry){Captured=true;Dragging=false;Start=Point=point;Geometry=geometry;ScreenSize=new Vector2(Screen.width,Screen.height);}
        public void Move(Vector2 point,bool held){Point=point;if(held&&OriginalHandInputRules.StartsDrag(point.x-Start.x,point.y-Start.y))Dragging=true;}
        public bool InHand=>OriginalHandInputRules.InHand(Point.y,Geometry.BoundaryY);
        public bool CanDrop=>Dragging&&!InHand;
        public void Reset(){Captured=Dragging=false;}
    }
    public partial class RiichiClient {
        readonly HandDiscardGesture handGesture=new HandDiscardGesture();
        string handPressKey,queuedDragAction,queuedDragKey;
        int handPressId=-1,queuedDragTile=-1;bool clickedChosen,oldWaitVisible,requestIsPoll;float queuedDragUntil;
#if UNITY_EDITOR
        bool validationHandInput;
        public bool ValidationCaptureActions;
        public string ValidationLastAction {get;private set;}
        public int ValidationActionCount {get;private set;}
        public bool ValidationHandDragging=>handGesture.Dragging;
        public Rect ValidationHandDropZone=>handGesture.DropZone;
        public bool ValidationDragQueued=>queuedDragAction!=null;
        public bool ValidationHandPointer(Vector2 screen,bool down,bool held,bool up,float now,bool blocked=false){validationHandInput=true;return UpdateHandPointer(screen,down,held,up,now,blocked);}
        public void ValidationHandBusy(bool busy,bool poll=false){Busy=busy;requestIsPoll=poll;}
        public void ValidationEndHandPointer(){validationHandInput=false;CancelHandDrag();}
#endif
        bool CanPointHand=>active&&State!=null&&(State.phase=="play"||State.phase=="reaction")&&!AutoPlaying&&!showRoom&&page!="settings"&&!lobby.OverlayActive&&!tableEmotes.BlocksTableInput&&!introduction.Active&&!allLast.Active&&!table.ArrangingHand;
        string HandDecisionKey()=>State==null?null:State.audioRoom+":"+State.matchSeq+":"+State.handSeq+":"+State.phase+":"+State.turn+":"+State.you+":"+RiichiMode+":"+string.Join(",",State.hand)+":"+State.drawn+":"+string.Join(",",State.options.Where(o=>o.kind=="discard"||o.kind=="riichi").Select(o=>o.id));
        int PressedHandIndex(){OrderedHand(State);return localHand.IndexOf(handPressId);}
        string PressedHandAction(int index){var hand=OrderedHand(State);return index<0||index>=hand.Count?null:TableRenderer.HandChoice(State,hand[index].Face,hand[index].Drawn,RiichiMode)?.id;}
        void CancelHandDrag(bool restoreWait=true){
            queuedDragAction=queuedDragKey=null;queuedDragTile=-1;
            if(handGesture.Captured){
                ClearSelection();if(restoreWait&&handPressKey==HandDecisionKey())waitHoldVisible=oldWaitVisible;
                table?.SelectHand(-1,true);
            }
            handGesture.Reset();handPressKey=null;handPressId=-1;clickedChosen=false;
        }
        void ObserveHandDrag(){
            if(State!=null)OrderedHand(State);
            if(handGesture.Captured&&handPressKey!=HandDecisionKey()||queuedDragAction!=null&&queuedDragKey!=HandDecisionKey())CancelHandDrag(false);
        }
        bool DragActionValid(string action,string key)=>CanPointHand&&State.phase=="play"&&State.turn==State.you&&key==HandDecisionKey()&&State.options.Any(o=>o.id==action&&o.kind==(RiichiMode?"riichi":"discard"));
        void TickQueuedDrag(float now){
            if(queuedDragAction==null)return;
            if(now>queuedDragUntil||!DragActionValid(queuedDragAction,queuedDragKey)||localHand.IndexOf(queuedDragTile)<0){queuedDragAction=queuedDragKey=null;queuedDragTile=-1;return;}
            if(Busy)return;
            string action=queuedDragAction;int id=queuedDragTile;queuedDragAction=queuedDragKey=null;queuedDragTile=-1;if(OptionsReady)SubmitHandAction(action,id);
        }
        bool UpdateHandPointer(Vector2 point,bool down,bool held,bool up,float now,bool blocked){
            ObserveHandDrag();bool consumed=handGesture.Captured;
            if(blocked||!CanPointHand||handGesture.Captured&&handGesture.ScreenSize!=new Vector2(Screen.width,Screen.height)){CancelHandDrag();return consumed;}
            if(down){queuedDragAction=queuedDragKey=null;queuedDragTile=-1;}
            TickQueuedDrag(now);
            if(down){
                if(handGesture.Captured)CancelHandDrag();if(InUI(point)||Busy&&!requestIsPoll)return false;
                if(!Physics.Raycast(table.HandCamera.ScreenPointToRay(point),out var hit,100,1<<12))return false;
                var target=hit.collider.GetComponentInParent<TileTarget>();if(target==null)return false;
                int id=LocalHandId(target.Index);if(id<0)return false;
                clickedChosen=selectedHandId==id;oldWaitVisible=waitHoldVisible;handPressId=id;handPressKey=HandDecisionKey();
                handGesture.Begin(point,table.HandInputGeometry());ChooseHand(target.Index);return true;
            }
            if(!handGesture.Captured)return false;
            if(!held&&!up){CancelHandDrag();return true;}
            handGesture.Geometry=table.HandInputGeometry();handGesture.Move(point,held&&!up);
            int index=PressedHandIndex();if(index<0){CancelHandDrag(false);return true;}
            string action=PressedHandAction(index);
            if(up){
                bool wasDragging=handGesture.Dragging,discard=(wasDragging?handGesture.CanDrop:clickedChosen)&&!InUI(point)&&DragActionValid(action,handPressKey);
                string key=handPressKey;int id=handPressId;
                if(wasDragging){CancelHandDrag(false);}
                else {handGesture.Reset();handPressKey=null;handPressId=-1;clickedChosen=false;}
                if(!discard)return true;
                if(Busy&&requestIsPoll){queuedDragAction=action;queuedDragKey=key;queuedDragTile=id;queuedDragUntil=now+1.5f;}
                else if(!Busy&&OptionsReady)SubmitHandAction(action,id);
                return true;
            }
            if(handGesture.Dragging&&handGesture.InHand){
                int destination=handGesture.Geometry.IndexAt(point.x);
                if(ReorderPressedHand(index,destination))handGesture.Geometry=table.HandInputGeometry();
            }
            RefreshHandDragVisual();return true;
        }
        public bool IsDraggingHandId(int id)=>handGesture.Dragging&&handPressId==id;
        public void RefreshHandDragVisual(){
            if(handGesture.Dragging&&handPressKey==HandDecisionKey()){int index=PressedHandIndex();if(index>=0){handGesture.Geometry=table.HandInputGeometry();table.DragHandTile(index,handGesture.Point);}}
        }
    }
}
