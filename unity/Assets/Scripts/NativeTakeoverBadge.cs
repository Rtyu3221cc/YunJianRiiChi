using UnityEngine;
namespace YunJian.Riichi {
    public static class NativeTakeoverBadge {
        static Texture2D mask,bolt;
        public static void Draw(Rect portrait){
            if(mask==null)mask=Resources.Load<Texture2D>("Majsoul/Review1017d/disconnect-mask");
            if(bolt==null)bolt=Resources.Load<Texture2D>("Majsoul/Review1017d/disconnect-bolt");
            var old=GUI.color;GUI.color=new Color(old.r,old.g,old.b,old.a*.7f);if(mask!=null)GUI.DrawTexture(portrait,mask);
            GUI.color=new Color(old.r,old.g,old.b,old.a*.8f);
            // UI_DesktopInfo disconnect/node: original 153/202 scale and offset.
            if(bolt!=null)GUI.DrawTexture(new Rect(portrait.x+portrait.width*24.1f/202,portrait.y+portrait.height*36/202,portrait.width*153/202,portrait.height*153/202),bolt);
            GUI.color=old;
        }
    }
}
