#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace YunJian.Riichi
{
    public static class NativeFirePreview
    {
        public static IEnumerator Run(RiichiClient client,string folder)
        {
            var native=NativePresentationValidation.Run(client,folder);while(native.MoveNext())yield return native.Current;
            var view=JsonUtility.FromJson<RoomView>(File.ReadAllText(Path.Combine(folder,"win-4-tsumo-0-0.json")));view.audioRoom="native-fire-preview";var events=view.audioEvents;int seq=view.audioSeq;view.audioEvents=new PublicSound[0];view.audioSeq=seq-1;client.ShowValidationFixture(view);yield return null;
            var fx=client.GetComponent<TableEffects>();fx.ValidationFreeze=true;fx.ValidationTime=.25f;fx.Accept(view);view.audioEvents=events;view.audioSeq=seq;fx.Accept(view);yield return null;
            fx.ValidationTime=1.55f;yield return null;yield return null;var effect=fx.ValidationWin;if(effect==null)throw new Exception("Native fire preview has no effect");
            foreach(float age in new[]{.12f,.45f,.9f,1.5f}){
                fx.ValidationTime=effect.StartedAt+age;yield return null;yield return null;
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"native-0522-fire-"+age.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".png"));yield return new WaitForSecondsRealtime(.15f);
                Debug.Log("NATIVE FIRE SAMPLE "+age+" scale "+effect.transform.localScale+" mesh count "+effect.MeshLayerCount+" particles "+effect.ParticleCount);
            }
            Debug.Log("RIICHI NATIVE TORNADO PREVIEW PASS");
        }
    }
}
#endif
