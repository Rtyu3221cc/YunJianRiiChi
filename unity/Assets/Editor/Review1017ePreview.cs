using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace YunJian.Riichi.Editor {
 [InitializeOnLoad] public static class Review1017ePreview {
  const string Key="Riichi.Review1017e";
  static Review1017ePreview(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<Review1017eCapture>();}};}
  public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
 }
 public sealed class Review1017eCapture:MonoBehaviour {
  [Serializable] class Cue {public float at;public string kind,key,page;}
  [Serializable] class Review {public int scoreStartFrame,finalStartFrame,totalFrames;public List<Cue> events=new List<Cue>();}
  readonly Review review=new Review();string output;RiichiClient client;LobbyController lobby;GameAudio audio;SettlementAudio speech;
  static readonly BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
  IEnumerator Start(){var run=Capture();while(true){bool more;try{more=run.MoveNext();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(!more)break;yield return run.Current;}Debug.Log("REVIEW 1017E COMPLETE; one lobby still and audio rehearsal; no new video recording, APK, Git or unit tests");EditorApplication.Exit(0);}
  IEnumerator Size(int w,int h){var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));view.GetType().GetMethod("SetCustomResolution",Private).Invoke(view,new object[]{new Vector2(w,h),"Shortcut spacing and settlement audio preview"});for(int i=0;i<6;i++)yield return null;}
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var pic=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),pic.EncodeToPNG());if(name=="lobby-spacing"){var crop=new Texture2D(108,340,TextureFormat.RGBA32,false);crop.SetPixels(pic.GetPixels(1580,pic.height-340,108,340));crop.Apply();File.WriteAllBytes(Path.Combine(output,"shortcut-spacing.png"),crop.EncodeToPNG());Destroy(crop);}Destroy(pic);}
  void Tick(RoomView v,int serial,string page){int voices=speech.PlayedKeys.Count,effects=audio.PlayedCues.Count;speech.Tick(v,client.ValidationResult,true,audio,true);foreach(string key in speech.PlayedKeys.Skip(voices))review.events.Add(new Cue{at=serial/12f,key=key,kind="voice",page=page});foreach(string key in audio.PlayedCues.Skip(effects))review.events.Add(new Cue{at=serial/12f,key=key,kind="effect",page=page});}
  IEnumerator Capture(){
   output=Path.GetFullPath("../build/review-1017e/render");Directory.CreateDirectory(output);client=GetComponent<RiichiClient>();lobby=client.Lobby;audio=GetComponent<GameAudio>();speech=GetComponent<SettlementAudio>();client.BeginLobbyValidation("visual-only");
   audio.Configure(false,0,true,0);speech.Configure(false,0);yield return Size(1688,780);lobby.DesignPreviewClock=100;lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Home);yield return null;yield return null;yield return Shot("lobby-spacing");
   var view=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/review-1017d/fixtures/double-yakuman.json"));client.DesignPreviewSessionTable(view);var display=client.ValidationResult;display.Observe(view);display.Freeze=true;speech.Reset();speech.PlayedKeys.Clear();audio.PlayedCues.Clear();int serial=0;
   for(int stage=0;stage<3;stage++){
    speech.Reset();string page=stage==0?"details":stage==1?"scores":"final";if(stage==1)review.scoreStartFrame=serial;if(stage==2)review.finalStartFrame=serial;
    float duration=stage==0?SettlementAudio.Sequence(view.result.details[0]).end+3:stage==1?ResultPresentation.ScoreEnd(view.n)+3:2;
    for(int i=0;i<Mathf.CeilToInt(duration*12);i++){display.FrozenAge=i/12f;Tick(view,serial,page);serial++;yield return null;}
    display.FrozenAge=duration;if(stage<2)display.Tick(view,false);
   }
   review.totalFrames=serial;File.WriteAllText(Path.Combine(output,"audio-timeline.json"),JsonUtility.ToJson(review,true));
   int ticks=review.events.Count(c=>c.page=="scores"&&c.key=="score_change"),rank=review.events.Count(c=>c.page=="scores"&&c.key=="rank1"),final=review.events.Count(c=>c.page=="final"),wrong=review.events.Count(c=>c.key=="winlose");
   Debug.Log("SCORE AUDIO: score_change="+ticks+", rank1="+rank+", final cues="+final+", winlose="+wrong);
   if(ticks==0||rank!=1||final!=0||wrong!=0||serial!=Directory.GetFiles("../build/review-1017d/render/double-yakuman","frame-*.png").Length)throw new InvalidOperationException("Audio timeline does not match the requested settlement sequence");
  }
 }
}
