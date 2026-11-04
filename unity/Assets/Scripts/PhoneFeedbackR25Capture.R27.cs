#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace YunJian.Riichi.Editor {
    public sealed partial class PhoneFeedbackR25Capture {
        bool R27Mode=>(Environment.GetEnvironmentVariable("YUNJIAN_R25_MODE")??"").StartsWith("r27");
        [Serializable] class R27Case {public string name,target;public int mount;public bool face,depth;public Vector3 origin,faceCenter,baseCenter;public Vector2 faceScreen,baseScreen;}
        [Serializable] class R27Cases {public List<R27Case> cases=new List<R27Case>();}
        IEnumerator R27Final(){
            yield return Size(1600,900);BeginRecording();var marker=GetComponent<WardrobeResponseIndicator>();var cases=new R27Cases();
            foreach(var test in new[]{new{seat=1,old=true},new{seat=1,old=false},new{seat=0,old=false},new{seat=2,old=false},new{seat=3,old=false}}){
                marker.Clear();var view=Fixture();view.phase="reaction";view.turn=test.seat;var river=view.players[test.seat].river.Where(x=>!x.called).ToList();river[river.Count-2].tile="s4";river[river.Count-1].tile="p3";view.players[test.seat].river=river.ToArray();view.pending=new PendingView{from=test.seat,tile="p3",kind="discard",waiting=new[]{0}};view.last=new LastDiscard{from=test.seat,tile="p3"};view.options=new[]{new Choice{id="pon:p333-",kind="pon",tile="p3"}};view.players[0].wardrobe.callout="mpzs_shandian";client.ShowValidationFixture(view);client.ValidationTableOnly=true;
                marker.InspectionMode=test.seat==0;marker.InspectionTarget=test.seat==0?table.FindDiscard(0,"p3",river.Count):null;marker.InspectionKey="r27-self";marker.ReviewMountVariant=test.old?2:0;marker.ReviewFaceAnchor=marker.ReviewSourceDepth=false;marker.ValidationRawOverlay=false;marker.ValidationFreeze=true;marker.ValidationAge=0;yield return Wait(5);Check(marker.Target!=null&&marker.Target.name.EndsWith(" p3"),"Pending three-dot is the exact marker target, seat "+test.seat);ChapterAt(test.old?"修正前：待鸣三筒，但底板投影偏到四索":"修正后：光效对准三筒 · 座位 "+test.seat);
                for(int f=0;f<(test.seat==1?80:60);f++){marker.ValidationAge=f/30f;yield return Frame();}var box=marker.Target.GetComponent<BoxCollider>();var face=marker.Target.TransformPoint(box.center+Vector3.up*box.size.y*.5f);var di=marker.EffectRoot.GetComponentsInChildren<ParticleSystem>().First(x=>x.name=="di").GetComponent<ParticleSystemRenderer>();var fs=(Vector2)table.Camera.WorldToScreenPoint(face);var bs=(Vector2)table.Camera.WorldToScreenPoint(di.bounds.center);float error=Vector2.Distance(fs,bs);Check(test.old||error<5,"Final projected base is within five pixels of the three-dot face, seat "+test.seat+" error="+error);cases.cases.Add(new R27Case{name=(test.old?"before":"after")+test.seat,target=marker.Target.name,faceCenter=face,baseCenter=di.bounds.center,faceScreen=fs,baseScreen=bs});yield return Shot((test.old?"before":"after")+test.seat);report.calloutCases++;
            }
            marker.Clear();marker.InspectionMode=false;marker.ReviewMountVariant=0;var invalid=Fixture();invalid.phase="reaction";invalid.pending=new PendingView{from=1,tile="p5",kind="discard",waiting=new[]{0}};invalid.options=new[]{new Choice{id="pon:p555-",kind="pon",tile="p5"}};client.ShowValidationFixture(invalid);yield return Wait(4);Check(table.ResponseTarget==null,"An inconsistent pending tile does not mark a different latest river tile");marker.ValidationFreeze=false;client.ValidationTableOnly=false;File.WriteAllText(output+"/final-projection.json",JsonUtility.ToJson(cases,true));ChapterAt("已按最终画面落点复核，未生成 APK、未提交代码");yield return Hold(45);pipe.Close();encoder.WaitForExit();Check(encoder.ExitCode==0,"R27 projected-target review video encoded");encoder.Dispose();encoder=null;pipe=null;Time.captureFramerate=0;
        }
        IEnumerator R27Run(){
            if(Environment.GetEnvironmentVariable("YUNJIAN_R25_MODE")=="r27-final"){yield return R27Final();yield break;}
            yield return Size(1600,900);var marker=GetComponent<WardrobeResponseIndicator>();var rows=new R27Cases();
            foreach(var variant in new[]{new{n="A-current",m=0,f=false,d=false},new{n="B-depth",m=0,f=false,d=true},new{n="C-front-frame",m=1,f=false,d=false},new{n="D-front-depth",m=1,f=false,d=true},new{n="E-face-anchor",m=0,f=true,d=true},new{n="F-front-face-depth",m=1,f=true,d=true}}){
                var view=Fixture();view.phase="reaction";view.turn=1;view.pending=new PendingView{from=1,tile="p3",kind="discard",waiting=new[]{0}};view.last=new LastDiscard{from=1,tile="p3"};view.options=new[]{new Choice{id="pon:p333-",kind="pon",tile="p3"}};view.players[0].wardrobe.callout="mpzs_shandian";client.ShowValidationFixture(view);client.ValidationTableOnly=true;marker.ReviewPreviousRootPose=false;marker.ReviewMountVariant=variant.m;marker.ReviewFaceAnchor=variant.f;marker.ReviewSourceDepth=variant.d;marker.ValidationRawOverlay=variant.d;marker.ValidationFreeze=true;marker.ValidationAge=.75f;yield return Wait(8);Check(marker.Target!=null&&marker.Target.name.EndsWith(" p3"),"Only the actual three-dot is the target");
                var box=marker.Target.GetComponent<BoxCollider>();var face=marker.Target.TransformPoint(box.center+Vector3.up*box.size.y*.5f);var di=marker.EffectRoot.GetComponentsInChildren<ParticleSystem>().First(x=>x.name=="di").GetComponent<ParticleSystemRenderer>();var row=new R27Case{name=variant.n,target=marker.Target.name,mount=variant.m,face=variant.f,depth=variant.d,origin=marker.EffectRoot.position,faceCenter=face,baseCenter=di.bounds.center,faceScreen=table.Camera.WorldToScreenPoint(face),baseScreen=table.Camera.WorldToScreenPoint(di.bounds.center)};rows.cases.Add(row);yield return Shot(variant.n);marker.Clear();
            }
            marker.ReviewMountVariant=0;marker.ReviewFaceAnchor=marker.ReviewSourceDepth=marker.ValidationRawOverlay=marker.ValidationFreeze=false;client.ValidationTableOnly=false;File.WriteAllText(output+"/projection-study.json",JsonUtility.ToJson(rows,true));
        }
    }
}
#endif
