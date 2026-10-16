using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;

namespace YunJian.Riichi.Editor
{
    // Visual rehearsal against a disposable host; no unit-test framework or APK.
    [InitializeOnLoad] public static class AutoSettlementPreview
    {
        const string Key="Riichi.AutoSettlementPreview";
        static AutoSettlementPreview(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        static void Changed(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindFirstObjectByType<RiichiClient>().gameObject.AddComponent<AutoSettlementCapture>();}}
    }
    public sealed class AutoSettlementCapture:MonoBehaviour
    {
        [Serializable] public class Frame {public string file,phase,stage;public float time,winEffectAge,winEffectDuration;public int handSeq,winParticles,spawnedWinEffects;public bool winEffects,pendingEffects,cutIn,carriedVisible,targetVisible;public Vector3 carriedPosition,targetPosition;}
        [Serializable] public class Frames {public string scenario;public List<Frame> frames=new List<Frame>();}
        RiichiClient client;TableEffects effects;string output;bool failed;
        static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        IEnumerator Command(RequestBody request){var routine=(IEnumerator)typeof(RiichiClient).GetMethod("Send",Private).Invoke(client,new object[]{request,null,false});yield return client.StartCoroutine(routine);}
        IEnumerator Start()
        {
            output=Path.GetFullPath(Environment.GetEnvironmentVariable("RIICHI_AUTO_PREVIEW_OUTPUT")??"../build/ai-settlement-preview/render-final");Directory.CreateDirectory(output);client=GetComponent<RiichiClient>();effects=GetComponent<TableEffects>();
            var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",Private).Invoke(window,new object[]{new Vector2(1280,720),"Auto settlement visual review"});window.Repaint();for(int i=0;i<8;i++)yield return null;
            foreach(string scenario in (Environment.GetEnvironmentVariable("RIICHI_AUTO_PREVIEW_SCENARIOS")??"sanma-tsumo,four-double-ron,final-four-tsumo,four-draw").Split(',')){
                client.BeginLobbyValidation("ai-settlement-preview-only");
                typeof(RiichiClient).GetField("nextPoll",Private).SetValue(client,float.PositiveInfinity);
                using(var seed=UnityWebRequest.Get("http://127.0.0.1:8765/preview/seed?scenario="+scenario)){yield return seed.SendWebRequest();if(seed.result!=UnityWebRequest.Result.Success){Debug.LogError(seed.error);failed=true;break;}}
                yield return Command(new RequestBody{op="recoverHost",host="ai-settlement-preview-only"});
                yield return Command(new RequestBody{op="autoPlay",enabled=true});
                if(client.State?.phase!="play"||!client.AutoPlaying){Debug.LogError("PREVIEW could not enable auto play");failed=true;break;}
                typeof(RiichiClient).GetField("nextPoll",Private).SetValue(client,Time.unscaledTime+.1f);
                var frames=new Frames{scenario=scenario};string dir=Path.Combine(output,scenario);Directory.CreateDirectory(dir);
                int hand=client.State.handSeq,index=0;float began=Time.realtimeSinceStartup,continued=-1;string previous=null;float stageAt=0;var stills=new HashSet<string>();
                while(Time.realtimeSinceStartup-began<85){
                    yield return new WaitForEndOfFrame();
                    float now=Time.realtimeSinceStartup-began;var state=client.State;
                    string stage=state.phase=="result"?(effects.ShowWinPresentation?"win":client.ValidationResult.ShowingFinal?"final":client.ValidationResult.ShowingDraw?"draw":client.ValidationResult.ShowingScores?"scores":"details-"+client.ValidationResult.WinnerIndex):state.handSeq>hand?"next-hand":"playing";
                    if(stage!=previous){previous=stage;stageAt=now;Debug.Log("AUTO PREVIEW "+scenario+" "+stage+" at="+now.ToString("F2")+" hand="+state.handSeq);}
                    string name="frame-"+(index++).ToString("D4")+".png";
                    var texture=ScreenCapture.CaptureScreenshotAsTexture();var png=texture.EncodeToPNG();Destroy(texture);File.WriteAllBytes(Path.Combine(dir,name),png);
                    var burst=effects.ValidationWin;
                    var carried=effects.ValidationAnimatedTile;var target=effects.ValidationTarget;
                    frames.frames.Add(new Frame{file=name,time=now,phase=state.phase,stage=stage,handSeq=state.handSeq,winEffects=effects.ShowWinPresentation,pendingEffects=effects.HasPendingActions,cutIn=effects.CutIn.Active,winParticles=burst?.ParticleCount??0,winEffectAge=burst==null?-1:Time.unscaledTime-burst.StartedAt,winEffectDuration=burst?.PlaybackDuration??0,spawnedWinEffects=effects.SpawnedWinEffects,carriedVisible=carried!=null&&carried.gameObject.activeInHierarchy,targetVisible=target!=null&&target.gameObject.activeInHierarchy,carriedPosition=carried==null?Vector3.zero:carried.position,targetPosition=target==null?Vector3.zero:target.position});
                    if(now-stageAt>=.85f&&stills.Add(stage))File.WriteAllBytes(Path.Combine(output,scenario+"-"+stage+".png"),png);
                    if(stage=="next-hand"&&continued<0)continued=now;
                    if(continued>=0&&now-continued>=6)break;
                    yield return new WaitForSecondsRealtime(.075f);
                }
                File.WriteAllText(Path.Combine(dir,"frames.json"),JsonUtility.ToJson(frames,true));
                if(continued<0){Debug.LogError("AUTO PREVIEW did not continue: "+scenario);failed=true;break;}
                Debug.Log("AUTO PREVIEW scenario complete: "+scenario+" frames="+index);
            }
            using(var stop=UnityWebRequest.Get("http://127.0.0.1:8765/preview/stop"))yield return stop.SendWebRequest();
            Debug.Log(failed?"AUTO SETTLEMENT PREVIEW INCOMPLETE":"AUTO SETTLEMENT PREVIEW COMPLETE");EditorApplication.Exit(failed?1:0);
        }
    }
}
