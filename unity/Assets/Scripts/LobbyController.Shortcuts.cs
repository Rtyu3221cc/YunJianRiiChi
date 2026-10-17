using UnityEngine;

namespace YunJian.Riichi {
    public sealed partial class LobbyController {
        readonly Texture2D[] shortcutArt=new Texture2D[4];
        static readonly string[] shortcutPaths={"Lobby/Sprites/Main/btn_set","Lobby/Sprites/Main/btn_help","Review1017d/academy","Lobby/Sprites/Main/btn_priscreen"};
        static readonly string[] shortcutIds={"settings","garden-help","academy","capture"};
        // Visible bounds from the APK textures; transparent padding differs.
        static readonly Vector4[] shortcutInk={new Vector4(6f/76,5f/76,64f/76,63f/76),new Vector4(2f/85,2f/85,82f/85,82f/85),new Vector4(2f/85,2f/85,82f/85,82f/85),new Vector4(2f/85,2f/85,83f/85,83f/85)};
        void DrawGardenShortcuts(){
            const float top=34,diameter=74,gap=38;
            for(int i=0;i<4;i++){
                if(shortcutArt[i]==null)shortcutArt[i]=Resources.Load<Texture2D>("Majsoul/"+shortcutPaths[i]);
                var texture=shortcutArt[i];var ink=shortcutInk[i];float h=diameter/ink.w,w=h*texture.width/texture.height,y=top+i*(diameter+gap);
                var r=new Rect(width-78-(ink.x+ink.z/2)*w,y-ink.y*h,w,h);
                GUI.DrawTexture(ControlRect(r,shortcutIds[i],1.1f),texture,ScaleMode.ScaleToFit);
                if(!Hit(new Rect(width-127,y-12,98,98),shortcutIds[i]))continue;
                Pulse(shortcutIds[i]);
                if(i==0)client.LobbyOpenSettings();
                else if(i==1)OpenGardenHelp();
                else if(i==2)OpenRules();
                else {string file=System.IO.Path.Combine(Application.persistentDataPath,"yunjian-lobby-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".png");ScreenCapture.CaptureScreenshot(file);help="大厅截图已保存。";OpenModal("notice");}
            }
        }
    }
}
