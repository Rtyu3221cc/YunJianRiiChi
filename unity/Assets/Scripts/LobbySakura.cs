using UnityEngine;
namespace YunJian.Riichi {
    // Original APK yinghua / yinghua2 atlases. A screen-space projection keeps
    // the same light snowfall across every non-table page and aspect ratio.
    public sealed class LobbySakura {
        Texture2D petals;
        static float Noise(int i,float salt)=>Mathf.Repeat(Mathf.Sin(i*127.1f+salt*311.7f)*43758.5453f,1);
        public void Draw(Rect screen,float time){
            if(Event.current.type!=EventType.Repaint)return;if(petals==null)petals=CharacterRoster.Texture("Effects/petals");if(petals==null)return;var matrix=GUI.matrix;var color=GUI.color;
            for(int i=0;i<32;i++){float n=Noise(i,1),speed=Mathf.Lerp(55,125,Noise(i,2)),period=(screen.height+180)/speed,t=Mathf.Repeat(time+Noise(i,3)*period,period);float y=t*speed-90,x=screen.x+Mathf.Repeat(n*screen.width+Mathf.Sin(t*.8f+i)*40+t*Mathf.Lerp(-13,30,Noise(i,4)),screen.width);float size=Mathf.Lerp(14,30,Noise(i,5)),w=size*Mathf.Lerp(.35f,1,Mathf.Abs(Mathf.Sin(time*1.3f+i))),alpha=Mathf.Lerp(.45f,.84f,Noise(i,6));int frame=Mathf.FloorToInt(Mathf.Repeat(time*.7f+Noise(i,7),1)*20);GUI.color=new Color(color.r,color.g,color.b,color.a*alpha);GUIUtility.RotateAroundPivot(25*Mathf.Sin(time+i)+Noise(i,8)*360,new Vector2(x,y));GUI.DrawTextureWithTexCoords(new Rect(x-w/2,y-size/2,w,size),petals,new Rect(frame%4/4f,1-(frame/4+1)/5f,.25f,.2f));GUI.matrix=matrix;}
            GUI.color=color;
        }
    }
}
