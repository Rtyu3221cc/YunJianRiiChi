using System;
using System.Linq;
using UnityEngine;
namespace YunJian.Riichi {
 // Original UI_HuleShow/UI_AL positions, sizes and animation parameters from APK.
 public static class NativeAnnouncements {
  [Serializable] class Component {public float alphain_time,alphaout_time;public string asset_path;public Vector scale_in_start;}
  [Serializable] class Vector {public float x,y,z;}
  [Serializable] class Node {public string path;public float[] position,size;public Component[] components;}
  [Serializable] class Layout {public Node[] nodes;}
  static Layout win,al;
  public static Rect WinRect(string kind,int relative,int n,float width,float height,float age){
   if(win==null)win=JsonUtility.FromJson<Layout>(Resources.Load<TextAsset>("Majsoul/Presentation0535/ui_huleshow-layout").text);
   int slot=relative==0?2:relative==n-1?1:relative==1&&n==4?3:0;
   var node=win.nodes.First(x=>x.path=="/UI_HuleShow/container_"+(kind=="ron"?"rong":"zimo")+"/"+slot);
   var basis=win.nodes.First(x=>x.path=="/UI_HuleShow/container_rong/2");var motion=basis.components.First(c=>c.alphain_time>0 && c.scale_in_start!=null && c.scale_in_start.x>1);float t=Mathf.Clamp01(age/motion.alphain_time);float scale=Mathf.Lerp(motion.scale_in_start.x,1,Mathf.SmoothStep(0,1,t));
   var texture=WinTexture(kind);float unit=Mathf.Min(width/1280,height/720),h=basis.size[1]*2/3*unit*scale,w=h*texture.width/texture.height;Vector2 center=new Vector2(width/2+node.position[0]*2/3*unit,height/2-node.position[1]*2/3*unit);center.y=Mathf.Clamp(center.y,h/2+8,height-h/2-8);return new Rect(center.x-w/2,center.y-h/2,w,h);
  }
  public static Texture2D WinTexture(string kind)=>Resources.Load<Texture2D>("Majsoul/Presentation0535/"+(kind=="ron"?"show_rong":"show_zimo"));
  public static Rect LastRect(float width,float height,float age){
   if(al==null)al=JsonUtility.FromJson<Layout>(Resources.Load<TextAsset>("Majsoul/Presentation0535/ui_al-layout").text);
   var node=al.nodes.First(x=>x.path=="/UI_AL/node");var motion=node.components.First(c=>c.alphain_time>0 && c.scale_in_start!=null && c.scale_in_start.x>1);float t=Mathf.Clamp01(age/motion.alphain_time);float scale=Mathf.Lerp(motion.scale_in_start.x,1,t*t*t);
   float unit=Mathf.Min(width/1280,height/720);return new Rect(width/2+node.position[0]*2/3*unit-node.size[0]*scale*unit/3,height/2-node.position[1]*2/3*unit-node.size[1]*scale*unit/3,node.size[0]*scale*unit*2/3,node.size[1]*scale*unit*2/3);
  }
  public static void DrawWin(string kind,int relative,int n,float width,float height,float age){
   var old=GUI.color;GUI.color=new Color(0,0,0,.3f*Mathf.Clamp01(age/.15f));GUI.DrawTexture(new Rect(0,0,width,height),Texture2D.whiteTexture);GUI.color=new Color(1,1,1,Mathf.Clamp01(age/.2f)*Mathf.Clamp01((1.05f-age)/.15f));
   GUI.DrawTexture(WinRect(kind,relative,n,width,height,age),WinTexture(kind),ScaleMode.ScaleToFit);GUI.color=old;
  }
 }
 public sealed class AllLastAnnouncement {
  const float Duration=1.5f;string key,observedRoom,observedPhase;int observedHand;float began;bool running;public bool Freeze;public float FrozenAge;public int Started{get;private set;}
  public float Age=>Freeze?FrozenAge:Time.unscaledTime-began;public bool Active=>running&&Age<Duration;
  public static bool IsLast(RoomView view)=>view!=null && view.round>=view.rounds*view.n-1;
  public void Observe(RoomView view){
   if(view==null || view.phase=="empty" || view.phase=="lobby"){Reset();return;}
   bool fresh=observedRoom==view.audioRoom && observedPhase=="result" && view.phase=="play" && view.handSeq==observedHand+1;observedRoom=view.audioRoom;observedPhase=view.phase;observedHand=view.handSeq;
   string next=view.audioRoom+":"+view.handSeq+":"+view.round+":"+view.honba;if(next==key)return;key=next;running=false;
   long elapsed=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()-view.dealtAt;
   if(view.phase!="play"||!IsLast(view)||view.handSeq<=0||!fresh&&(elapsed<0||elapsed>2500||view.players.Any(p=>p.river.Length>0)))return;
   began=Time.unscaledTime;running=true;Started++;
  }
  public void Reset(){running=false;key=null;observedRoom=null;observedPhase=null;observedHand=0;Freeze=false;}
  public void Draw(float width,float height){if(!Active)return;var color=GUI.color;GUI.color=new Color(1,1,1,Mathf.Clamp01(Age/.25f)*Mathf.Clamp01((Duration-Age)/.15f));GUI.DrawTexture(NativeAnnouncements.LastRect(width,height,Age),Resources.Load<Texture2D>("Majsoul/Presentation0535/al"),ScaleMode.ScaleToFit);GUI.color=color;}
 }
}
