#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace YunJian.Riichi.Editor {
    public sealed partial class PhoneFeedbackR25Capture {
        bool R28Mode=>(Environment.GetEnvironmentVariable("YUNJIAN_R25_MODE")??"").StartsWith("r28");
        [Serializable] class R28Part {public string name,mode;public int count;public Vector3 center,localCenter,size,average;public Vector2 screen;}
        [Serializable] class R28Case {public string id,target,variant;public int seat;public Vector3 face,origin,up;public Vector2 faceScreen;public List<R28Part> parts=new List<R28Part>();}
        [Serializable] class R28Cases {public List<R28Case> cases=new List<R28Case>();}
        IEnumerator R28Setup(string id,int seat,bool crowded=true,bool depth=false){
            var marker=GetComponent<WardrobeResponseIndicator>();marker.Clear();var v=Fixture();v.phase="reaction";v.turn=seat;
            if(crowded){var river=v.players[seat].river.Where(x=>!x.called).ToArray();river[river.Length-2].tile="s4";river[river.Length-1].tile="p3";v.players[seat].river=river;}
            else v.players[seat].river=(id=="mpzs_shandian"?new[]{"z1","z5","z7","p1"}:new[]{"s9","z1"}).Select(x=>new RiverTile{tile=x}).ToArray();
            var tile=v.players[seat].river.Last().tile;v.pending=new PendingView{from=seat,tile=tile,kind="discard",waiting=new[]{0}};v.last=new LastDiscard{from=seat,tile=tile};v.options=new[]{new Choice{id="pon:test",kind="pon",tile=tile}};v.players[0].wardrobe.callout=id;client.ShowValidationFixture(v);client.ValidationTableOnly=true;
            marker.InspectionMode=seat==0;marker.InspectionTarget=seat==0?table.FindDiscard(0,tile,v.players[seat].river.Length):null;marker.InspectionKey="r28:"+id;marker.ReviewPreviousRootPose=false;marker.ReviewFaceAnchor=marker.ValidationRawOverlay=false;marker.ReviewSourceDepth=depth;marker.ValidationFreeze=true;marker.ValidationAge=0;yield return Wait(5);
            Check(marker.Target!=null&&marker.Target.name.EndsWith(" "+tile),"Exact pending target "+id+" seat "+seat);
        }
        R28Case R28Measure(string id,int seat,string variant){
            var m=GetComponent<WardrobeResponseIndicator>();var box=m.Target.GetComponent<BoxCollider>();var face=m.Target.TransformPoint(box.center+Vector3.up*box.size.y*.5f);var row=new R28Case{id=id,seat=seat,variant=variant,target=m.Target.name,origin=m.EffectRoot.position,face=face,faceScreen=table.Camera.WorldToScreenPoint(face),up=m.EffectRoot.forward};
            foreach(var ps in m.EffectRoot.GetComponentsInChildren<ParticleSystem>()){
                var renderer=ps.GetComponent<ParticleSystemRenderer>();var data=new ParticleSystem.Particle[ps.main.maxParticles];int n=ps.GetParticles(data);var avg=Vector3.zero;for(int i=0;i<n;i++)avg+=ps.main.simulationSpace==ParticleSystemSimulationSpace.World?data[i].position:ps.transform.TransformPoint(data[i].position);if(n>0)avg/=n;
                row.parts.Add(new R28Part{name=ps.name,mode=renderer.renderMode.ToString(),count=n,center=renderer.bounds.center,localCenter=m.Target.InverseTransformPoint(renderer.bounds.center),size=renderer.bounds.size,average=avg,screen=table.Camera.WorldToScreenPoint(renderer.bounds.center)});
            }return row;
        }
        [Serializable] class R28Clip {public string id,variant;public int seat,start,end;public Vector2 center;public float width;}
        [Serializable] class R28Clips {public List<R28Clip> clips=new List<R28Clip>();}
        readonly R28Clips r28clips=new R28Clips();
        IEnumerator R28ClipMovie(string id,int seat,bool previous,bool crowded,int frames){
            var m=GetComponent<WardrobeResponseIndicator>();m.ReviewMountVariant=0;m.ReviewPreviousCalloutRendering=previous;yield return R28Setup(id,seat,crowded);var box=m.Target.GetComponent<BoxCollider>();var face=m.Target.TransformPoint(box.center+Vector3.up*box.size.y*.5f);var a=table.Camera.WorldToScreenPoint(face-m.Target.right*box.size.x*.5f);var b=table.Camera.WorldToScreenPoint(face+m.Target.right*box.size.x*.5f);var clip=new R28Clip{id=id,seat=seat,variant=previous?"before":crowded?"crowded":"reference",start=report.frames,center=table.Camera.WorldToScreenPoint(face),width=Vector2.Distance(a,b)};
            ChapterAt((previous?"上轮画面 · ":"按原版录像调整 · ")+WardrobeCatalog.Find(id).name+" · 座位 "+seat);
            for(int f=0;f<frames;f++){m.ValidationAge=f/30f;yield return Frame();}clip.end=report.frames;r28clips.clips.Add(clip);yield return Shot(id+"-"+clip.variant+"-seat"+seat);report.calloutCases++;
        }
        IEnumerator R28Final(){
            yield return Size(1600,900);BeginRecording();var m=GetComponent<WardrobeResponseIndicator>();var rows=new R28Cases();
            foreach(var reference in new[]{new{id="mpzs_shandian",seat=3},new{id="mpzs_22event",seat=2}})foreach(bool previous in new[]{true,false}){yield return R28ClipMovie(reference.id,reference.seat,previous,false,90);rows.cases.Add(R28Measure(reference.id,reference.seat,previous?"before":"reference"));}
            foreach(var item in WardrobeCatalog.Items("callout"))for(int seat=0;seat<4;seat++){
                yield return R28ClipMovie(item.id,seat,false,true,90);var row=R28Measure(item.id,seat,"crowded");rows.cases.Add(row);
                foreach(var renderer in m.EffectRoot.GetComponentsInChildren<ParticleSystemRenderer>())foreach(var material in renderer.sharedMaterials)Check(Mathf.Abs(material.GetFloat("_ZTest")-4)<.01f,"Tile depth occludes "+item.id+" / "+renderer.name+" seat "+seat);
                if(item.id=="mpzs_shandian"||item.id=="mpzs_22event"){
                    var basePart=row.parts.First(x=>x.name=="di");Check(basePart.center.y<row.face.y&&basePart.center.y>m.Target.position.y,"Base remains below the face and above the cloth "+item.id+" seat "+seat);
                    string rising=item.id=="mpzs_22event"?"paopao":"lizi";var floatPart=row.parts.First(x=>x.name==rising);Check(floatPart.average.y>row.face.y,"Source particles rise above the callable tile "+item.id+" seat "+seat);
                }
                if(item.id=="effect_zhishi")Check(row.parts.First(x=>x.name=="arrow").center.y>row.face.y,"Default guide arrow remains above the tile seat "+seat);
                yield return R28Occlusion(item.id,seat);
            }
            m.Clear();m.InspectionMode=m.ValidationFreeze=m.ReviewPreviousCalloutRendering=false;var done=Fixture();done.phase="play";client.ShowValidationFixture(done);yield return Wait(4);Check(m.EffectRoot==null,"Ending the response removes the equipped marker");
            File.WriteAllText(output+"/final-geometry.json",JsonUtility.ToJson(rows,true));File.WriteAllText(output+"/clips.json",JsonUtility.ToJson(r28clips,true));pipe.Close();encoder.WaitForExit();Check(encoder.ExitCode==0,"R28 four-style reference capture encoded");encoder.Dispose();encoder=null;pipe=null;Time.captureFramerate=0;client.ValidationTableOnly=false;
        }
        IEnumerator R28Occlusion(string id,int seat){
            var m=GetComponent<WardrobeResponseIndicator>();var particles=m.EffectRoot.GetComponentsInChildren<ParticleSystemRenderer>();var image=new RenderTexture(1600,900,24);var camera=table.Camera;var previous=camera.targetTexture;camera.targetTexture=image;
            Action<string> save=name=>{camera.Render();var old=RenderTexture.active;RenderTexture.active=image;var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(output+"/"+name+".png",texture.EncodeToPNG());Destroy(texture);RenderTexture.active=old;};
            var targetBox=m.Target.GetComponent<BoxCollider>();float faceY=m.Target.TransformPoint(targetBox.center+Vector3.up*targetBox.size.y*.5f).y;foreach(var renderer in particles)renderer.enabled=renderer.bounds.center.y<faceY;
            save("occlusion-"+id+"-seat"+seat+"-on");foreach(var renderer in particles)renderer.enabled=false;save("occlusion-"+id+"-seat"+seat+"-off");foreach(var renderer in particles)renderer.enabled=true;camera.targetTexture=previous;image.Release();Destroy(image);
            var state=client.State;var neighbour=table.FindDiscard(seat,"s4",state.players[seat].river.Length-1);var box=neighbour.GetComponent<BoxCollider>();var face=neighbour.TransformPoint(box.center+Vector3.up*box.size.y*.5f);var screen=table.Camera.WorldToScreenPoint(face);File.AppendAllText(output+"/occlusion-points.jsonl",JsonUtility.ToJson(new R28Clip{id=id,seat=seat,center=screen,width=Vector2.Distance(table.Camera.WorldToScreenPoint(face-neighbour.right*box.size.x*.25f),table.Camera.WorldToScreenPoint(face+neighbour.right*box.size.x*.25f))})+"\n");
            yield return null;
        }
        IEnumerator R28Run(){
            if(Environment.GetEnvironmentVariable("YUNJIAN_R25_MODE")=="r28-final"){yield return R28Final();yield break;}
            yield return Size(1600,900);var marker=GetComponent<WardrobeResponseIndicator>();var rows=new R28Cases();
            foreach(var item in WardrobeCatalog.Items("callout"))foreach(int mount in new[]{2,1,3}){
                marker.ReviewMountVariant=mount==3?2:mount;int seat=item.id=="mpzs_22event"?2:item.id=="mpzs_shandian"?3:1;yield return R28Setup(item.id,seat,false,mount==3);marker.ValidationAge=1.2f;yield return Wait(5);rows.cases.Add(R28Measure(item.id,seat,mount==2?"native":mount==3?"depth":"front"));yield return Shot(item.id+"-"+(mount==2?"native":mount==3?"depth":"front"));
            }
            File.WriteAllText(output+"/mount-probe.json",JsonUtility.ToJson(rows,true));marker.Clear();marker.InspectionMode=marker.ValidationFreeze=false;marker.ReviewMountVariant=0;client.ValidationTableOnly=false;
        }
    }
}
#endif
