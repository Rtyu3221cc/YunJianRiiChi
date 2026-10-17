using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    // Deterministic unit checks against the production QR and presentation code.
    public static class Release0545Validation {
        static int checks;
        static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
        public static void Run(){
            checks=0;
            string payload=RoomQr.EncodeInvite("479009",new[]{"http://192.168.31.131:8765","http://10.10.10.1:8765"});
            Check(RoomQr.TryParse(payload,out var invitation)&&invitation.pin=="479009"&&invitation.endpoints.Length==2,"Valid room invitation rejected");
            foreach(string invalid in new[]{"", "https://example.com/479009",payload.Replace("479009","47900"),payload+"&v=1",payload+"#bad",RoomQr.EncodeInvite("479009",new string[0]).Replace("hosts=","hosts=http%3A%2F%2F8.8.8.8%3A8765")})
                Check(!RoomQr.TryParse(invalid,out _),"Invalid invitation accepted: "+invalid);
            foreach(bool gold in new[]{false,true}){
                var texture=gold?RoomQr.Image(payload,384,new Color32(232,175,113,255),new Color32(20,34,55,0),true):RoomQr.Image(payload,384,null,null,true);
                var source=texture.GetPixels32();int side=texture.width;
                foreach(int target in new[]{256,384,580}){
                    var pixels=new Color32[target*target];
                    for(int y=0;y<target;y++)for(int x=0;x<target;x++){
                        var c=source[(y*side/target)*side+x*side/target];
                        // Composite transparent modules over a dark, gently varying panel.
                        if(c.a==0)c=new Color32((byte)(20+x*8/target),(byte)(34+y*8/target),55,255);
                        pixels[y*target+x]=c;
                    }
                    Check(RoomQr.Decode(pixels,target,target)==payload,(gold?"Gold/inverted":"White")+" QR failed at "+target);
                    Array.Reverse(pixels);
                    Check(RoomQr.Decode(pixels,target,target)==payload,"Rotated QR failed at "+target);
                }
                UnityEngine.Object.DestroyImmediate(texture);
            }
            Check(RoomQr.Decode(Enumerable.Repeat(new Color32(22,34,55,255),256*256).ToArray(),256,256)==null,"Blank image decoded as QR");
            foreach(int n in new[]{3,4})foreach(bool final in new[]{false,true}){
                var v=new RoomView{phase="result",audioRoom="release-unit-"+n+final,n=n,you=0,players=Enumerable.Range(0,n).Select(i=>new PlayerView()).ToArray(),result=new ResultView{title="倒计时回归",matchOver=final,delta=new int[n],details=new[]{new WinDetail{score=new ScoreDetail{defen=64000,damanguan=2},yaku=new[]{new YakuDetail{name="四暗刻单骑",yakuman=2}}}}}};
                var d=new ResultPresentation();d.Observe(v);d.Freeze=true;
                float end=SettlementAudio.Sequence(v.result.details[0]).end;
                foreach(float age in new[]{end-.01f,end,end+.99f,end+1.99f,end+2.99f}){d.FrozenAge=age;d.Tick(v,false);Check(!d.ShowingScores,"Yaku confirmation advanced before three seconds");d.Observe(v);}
                d.FrozenAge=end+3.01f;d.Tick(v,false);Check(d.ShowingScores,"Yaku confirmation did not advance at three seconds");
                d.FrozenAge=ResultPresentation.ScoreEnd(n)+2.99f;Check(d.Tick(v,false)==ResultPresentation.Action.None,"Score confirmation advanced early");
                d.FrozenAge=ResultPresentation.ScoreEnd(n)+3.01f;var action=d.Tick(v,false);
                Check(final?d.ShowingFinal&&action==ResultPresentation.Action.None:action==ResultPresentation.Action.Ready,"Score confirmation failed");
                if(final){d.FrozenAge=8.99f;Check(d.Tick(v,false)==ResultPresentation.Action.None,"Final confirmation advanced early");d.FrozenAge=9;Check(d.Tick(v,false)==ResultPresentation.Action.Ready,"Final confirmation failed");}
                d.ClearEffects();
            }
            foreach(string resource in new[]{"Majsoul/Audio/effect_yakumanready","Majsoul/Audio/rank1","Majsoul/Audio/score_change","Majsoul/Lobby/Audio/lobby_autumn"})
                Check(Resources.Load<AudioClip>(resource)!=null,"Missing release audio: "+resource);
            foreach(string shader in new[]{"Majsoul/UiParticleBuffer","Majsoul/UiParticleComposite"})Check(Resources.Load<Shader>(shader)!=null,"Missing release particle shader: "+shader);
            string output=Path.GetFullPath("../build/release-0545");Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"unit-passed.txt"),checks+" Unity unit assertions: QR, inverted QR, invitation parsing, countdowns and release resources.");
            Debug.Log("RELEASE 0545 UNIT PASS: "+checks);
        }
    }
}
