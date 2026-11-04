#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    public sealed partial class PhoneFeedbackR25Capture {
        bool ExtraMode=>Environment.GetEnvironmentVariable("YUNJIAN_R25_MODE")=="extra";
        IEnumerator ExtraAudit(){
            yield return Size(1280,720);client.BeginLobbyValidation("review-r25-extra");client.SessionPreviewOnly=true;yield return Wait(12);
            foreach(var item in WardrobeCatalog.Items("stick").Where(x=>x.HasModel)){
                var go=new GameObject("Model slot regression "+item.id);go.transform.position=new Vector3(100,100,100);var model=go.AddComponent<WardrobeModelVisual>();model.Initialize(item,.74f);
                foreach(float age in new[]{0f,.33f,1f,3.9f}){model.Sample(age);Check(go.GetComponentsInChildren<MeshRenderer>(true).Any(),"Stick model samples valid materials: "+item.id+" at "+age);}
                Destroy(go);report.models++;yield return null;
            }
            var visual=GetComponent<RiichiDiscardVisual>();
            foreach(var item in WardrobeCatalog.Items("riichiEffect"))for(int seat=0;seat<4;seat++){
                int before=visual.PlayedCount;yield return UseDeclaration(item,seat);yield return new WaitForSecondsRealtime(.2f);
                bool observed=false;
                foreach(float t in new[]{0f,.2f,.6f,1.2f,3f,4.8f,6f}){
                    effects.ValidationTime=t;yield return Wait(3);
                    if(t>=1.2f&&t<=3f&&visual.Latest!=null){observed=true;var tile=table.FindDiscard(seat,"z1",client.State.players[seat].river.Length);Check(tile!=null,"Declared river tile resolves at every seat");Check(Vector3.Distance(visual.Latest.transform.position,SourcePlaybackContracts.LegacyRiichiPoint(tile))<.001f,"Lua legacy offset follows settled tile "+item.id+" seat "+seat);if(item.id=="effect-liqi-23ex-liandao")Check(Quaternion.Angle(visual.Latest.transform.rotation,Quaternion.Euler(0,180,0))<.01f,"Approved scythe world orientation retained at seat "+seat);}
                }
                Check(observed,"Effect is visible during its source lifetime: "+item.id+" seat "+seat);Check(visual.PlayedCount==before+1,"Exactly one declaration effect at seat "+seat+" "+item.id);Check(visual.ActiveCount==0,"Four-second disposal at seat "+seat+" "+item.id);effects.Accept(client.State);yield return Wait(2);Check(visual.PlayedCount==before+1,"Repeated public poll does not replay "+item.id+" seat "+seat);report.riichiCases++;effects.Reset();
            }
            var response=GetComponent<WardrobeResponseIndicator>();
            foreach(var item in WardrobeCatalog.Items("callout"))for(int seat=0;seat<4;seat++){
                var view=Fixture();view.players[0].wardrobe.callout=item.id;client.ShowValidationFixture(view);client.ValidationTableOnly=true;var tile=table.FindDiscard(seat,view.players[seat].river.Last().tile,view.players[seat].river.Length);Check(tile!=null,"Callout inspection target exists");response.InspectionMode=true;response.InspectionTarget=tile;response.InspectionKey=item.id+"/"+seat;response.ValidationFreeze=true;
                foreach(float age in new[]{.1f,.7f,1.8f}){response.ValidationAge=age;yield return Wait(3);Check(response.EffectRoot!=null,"Callout loads for every source direction");Check(Vector3.Distance(response.EffectRoot.position,SourcePlaybackContracts.TileModelOrigin(tile))<.001f,"Callout source-model origin at seat "+seat+" "+item.id);Check(Quaternion.Angle(response.EffectRoot.rotation,tile.rotation*SourcePlaybackContracts.NativeTileMount)<.01f,"Callout native-model frame at seat "+seat+" "+item.id);}
                response.Clear();report.calloutCases++;
            }
            response.InspectionMode=response.ValidationFreeze=false;client.ValidationTableOnly=false;effects.ValidationFreeze=effects.ValidationSequentialClock=false;
            Check(report.models==61&&report.riichiCases==44&&report.calloutCases==16,"All modeled sticks, eleven four-seat riichi cases and four four-seat callouts checked");Debug.Log("R25 EXTRA AUDIT PASS models="+report.models+" riichi="+report.riichiCases+" callouts="+report.calloutCases);
        }
    }
}
#endif
