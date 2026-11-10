namespace YunJian.Riichi
{
    // Wardrobe table preview. The table shows scripted local states only: the
    // session never becomes active, nothing is sent to the host and polling
    // stays off. Leaving the preview returns to the empty home state.
    public partial class RiichiClient
    {
        public bool DressPreviewing { get; private set; }
        public bool BeginDressPreview()
        {
            if(active||Busy||DressPreviewing)return false;
            CancelHandDrag(false);ClearSelection();localHand.Reset();ResetWaitPress();RiichiMode=false;
            table.Clear();effects.Reset();effects.SuppressShouts=true;DressPreviewing=true;
            return true;
        }
        // A new audioRoom restarts the event cursor; the same room plays only newer events.
        public void ShowDressPreviewState(RoomView view)
        {
            if(!DressPreviewing||view==null)return;
            view.Normalize();State=view;ObserveHandDrag();ClearSelection();
            table.Render(view);effects.Accept(view);
        }
        // The match-entry intro covers loading the previewed pack and the first table render.
        public bool DressIntroActive=>DressPreviewing&&introduction.Active;
        public void PlayDressIntro(RoomView view)
        {
            if(!DressPreviewing||view==null)return;
            view.Normalize();introduction.Reenter(view);
        }
        public void EndDressPreview()
        {
            if(!DressPreviewing)return;
            DressPreviewing=false;effects.SuppressShouts=false;EnterHome();
        }
    }
}
