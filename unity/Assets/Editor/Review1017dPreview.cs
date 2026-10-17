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
 [InitializeOnLoad] public static class Review1017dPreview {
  const string Key="Riichi.Review1017d";
  static Review1017dPreview(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<Review1017dCapture>();}};}
  public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
 }
 public sealed class Review1017dCapture:MonoBehaviour {
  [Serializable] class Cue {public float at;public string kind,key;}
  [Serializable] class Cues {public List<Cue> events=new List<Cue>();}
  [Serializable] class QrRead {public string style;public bool decoded;}
  [Serializable] class QrReads {public List<QrRead> cases=new List<QrRead>();}
  readonly Cues cues=new Cues();readonly QrReads qrReads=new QrReads();
  string output;RiichiClient client;LobbyController lobby;TableEffects effects;GameAudio audio;SettlementAudio speech;
  static readonly BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
  IEnumerator Start(){var run=Capture();while(true){bool more;try{more=run.MoveNext();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(!more)break;yield return run.Current;}Debug.Log("REVIEW 1017D PREVIEW COMPLETE; no APK, Git or unit-test runner");EditorApplication.Exit(0);}
  RoomView Load(string name)=>JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/review-1017d/fixtures/"+name+".json"));
  IEnumerator Size(int w,int h){var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));view.GetType().GetMethod("SetCustomResolution",Private).Invoke(view,new object[]{new Vector2(w,h),"Nine-point local preview"});for(int i=0;i<6;i++)yield return null;}
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var pic=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),pic.EncodeToPNG());Destroy(pic);}
  void SoundTick(RoomView view,int frame){int voice=speech.PlayedKeys.Count,sfx=audio.PlayedCues.Count;speech.Tick(view,client.ValidationResult,true,audio,true);foreach(var key in speech.PlayedKeys.Skip(voice))cues.events.Add(new Cue{at=frame/12f,kind="voice",key=key});foreach(var key in audio.PlayedCues.Skip(sfx))cues.events.Add(new Cue{at=frame/12f,kind="effect",key=key});}
  IEnumerator Capture(){
   output=Path.GetFullPath("../build/review-1017d/render");Directory.CreateDirectory(output);client=GetComponent<RiichiClient>();lobby=client.Lobby;effects=GetComponent<TableEffects>();audio=GetComponent<GameAudio>();speech=GetComponent<SettlementAudio>();client.BeginLobbyValidation("visual-only");
   if(Environment.GetEnvironmentVariable("RIICHI_REVIEW1017D_CHROME_ONLY")=="1"){
    yield return Size(1688,780);lobby.DesignPreviewClock=100;lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Home);yield return null;yield return null;yield return Shot("lobby-academy");
    var departed=Load("offline-ai");departed.players[departed.you].bot=true;departed.players[departed.you].takeoverMask=true;client.DesignPreviewSessionTable(departed);yield return null;yield return null;yield return Shot("departed-human-mask");yield break;
   }
   if(Environment.GetEnvironmentVariable("RIICHI_REVIEW1017D_SHADER_PROBE")=="1"){
    yield return Size(1280,592);var probe=Load("double-yakuman");client.DesignPreviewSessionTable(probe);var d=client.ValidationResult;d.Observe(probe);d.Freeze=true;d.FrozenAge=SettlementAudio.Sequence(probe.result.details[0]).titleAt+.6f;yield return null;yield return Shot("fire-probe");
    var fx=typeof(ResultPresentation).GetField("limitFire",Private).GetValue(d);var rt=(RenderTexture)typeof(NativeUiEffect).GetField("image",Private).GetValue(fx);var previous=RenderTexture.active;RenderTexture.active=rt;var copy=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);copy.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);copy.Apply();File.WriteAllBytes(Path.Combine(output,"fire-buffer.png"),copy.EncodeToPNG());Destroy(copy);RenderTexture.active=previous;yield break;
   }
   bool finalPass=Environment.GetEnvironmentVariable("RIICHI_REVIEW1017D_FINAL_PASS")=="1";
   if(!finalPass){
   yield return Size(1688,780);lobby.DesignPreviewClock=100;lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Home);yield return null;yield return null;yield return Shot("lobby-academy");
   lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Create);lobby.ValidationAdvanced(false);yield return null;yield return Shot("create-two-minute");
   typeof(LobbyController).GetProperty("IdleSeconds").SetValue(lobby,0);yield return null;yield return Shot("create-unlimited");typeof(LobbyController).GetProperty("IdleSeconds").SetValue(lobby,120);
   foreach(string name in new[]{"idle-taken","manual-ai","idle-returned","offline-ai"}){client.DesignPreviewSessionTable(Load(name));yield return null;yield return null;yield return Shot(name);}
   yield return Size(1280,592);
   foreach(string scenario in new[]{"riichi-depth","response-order"}){
    var view=Load(scenario);client.DesignPreviewSessionTable(view);yield return null;yield return null;int seat=scenario=="riichi-depth"?2:3,index=scenario=="riichi-depth"?8:7;string tile=scenario=="riichi-depth"?"z4":"s5";
    effects.ValidationPlay(seat,"discard",tile,scenario=="riichi-depth",index);Directory.CreateDirectory(Path.Combine(output,scenario));float handoff=effects.ValidationHandoff,duration=effects.ValidationPlayLength+.15f;
    for(int frame=0;frame<Mathf.CeilToInt(duration*20);frame++){effects.ValidationTime=frame/20f;yield return null;yield return Shot(scenario+"/frame-"+frame.ToString("D3"));}
    Debug.Log("ANIMATION PREVIEW "+scenario+": handoff="+handoff+", carried tile shares table depth");
    if(scenario=="response-order"){
     client.DesignPreviewSessionTable(view);effects.ValidationPlay(seat,"discard",tile,false,index);effects.ValidationTime=.2f;yield return null;yield return Shot("response-before-land");Debug.Log("RESPONSE BEFORE LAND visible="+(GetComponent<TableRenderer>().ResponseTarget!=null));
     effects.ValidationTime=handoff+.02f;yield return null;yield return Shot("response-after-land");Debug.Log("RESPONSE AFTER LAND visible="+(GetComponent<TableRenderer>().ResponseTarget!=null));
    }
   }
   }
   yield return Size(1280,592);var win=Load("double-yakuman");client.DesignPreviewSessionTable(win);var display=client.ValidationResult;display.Observe(win);display.Freeze=true;speech.Reset();audio.PlayedCues.Clear();speech.PlayedKeys.Clear();
   Directory.CreateDirectory(Path.Combine(output,"double-yakuman"));int serial=0;bool still=false;
   for(int stage=0;stage<3;stage++){
    speech.Reset();float duration=stage==0?SettlementAudio.Sequence(win.result.details[0]).end+3:stage==1?ResultPresentation.ScoreEnd(win.n)+3:2;
    for(int i=0;i<Mathf.CeilToInt(duration*12);i++){
     display.FrozenAge=i/12f;SoundTick(win,serial);yield return null;string frame="double-yakuman/frame-"+serial.ToString("D4");yield return Shot(frame);
     if(stage==0&&!still&&display.FrozenAge>=SettlementAudio.Sequence(win.result.details[0]).titleAt+.55f){File.Copy(Path.Combine(output,frame+".png"),Path.Combine(output,"double-yakuman.png"),true);still=true;}
     if(stage==1&&i==Mathf.CeilToInt((ResultPresentation.RankAt(win.n,1)+.1f)*12))File.Copy(Path.Combine(output,frame+".png"),Path.Combine(output,"score-first-rank.png"),true);
     if(stage==2&&i==18)File.Copy(Path.Combine(output,frame+".png"),Path.Combine(output,"final-first-rank.png"),true);
     serial++;
    }
    display.FrozenAge=duration;if(stage<2)display.Tick(win,false);
   }
   File.WriteAllText(Path.Combine(output,"audio-timeline.json"),JsonUtility.ToJson(cues,true));Debug.Log("DOUBLE YAKUMAN VIDEO frames="+serial+", sound cues="+cues.events.Count+", rank1 hits="+cues.events.Count(c=>c.key=="rank1"));
   var final=Load("ai-final-4");client.DesignPreviewSessionTable(final);display=client.ValidationResult;display.Observe(final);display.Freeze=true;
   for(int i=0;i<10&&!display.ShowingFinal;i++){display.FrozenAge=1000;display.Advance(final);}display.FrozenAge=2;yield return null;yield return Shot("all-ai-terminal");
   yield return Size(1132,635);var ready=Load("ready");client.DesignPreviewSessionTable(ready);lobby.DesignPreviewGardenPage(LobbyController.ScreenPage.Room);lobby.DesignPreviewClock=200;yield return null;yield return null;lobby.DesignPreviewInvitation();lobby.DesignPreviewClock=201;yield return null;yield return null;
   var raw=RoomQr.Image(client.InvitationPayload);File.WriteAllBytes(Path.Combine(output,"qr-original-source.png"),raw.EncodeToPNG());Destroy(raw);
   foreach(string style in new[]{"white","warm","tight","gold"}){
    if(style=="gold")lobby.DesignPreviewQrGold();else if(style=="tight")lobby.DesignPreviewQrTight();else lobby.DesignPreviewQrTheme(style=="warm");yield return null;yield return Shot("qr-compact-"+style);var source=lobby.DesignPreviewQrTexture;File.WriteAllBytes(Path.Combine(output,"qr-source-"+style+".png"),source.EncodeToPNG());
    var pic=new Texture2D(2,2);pic.LoadImage(File.ReadAllBytes(Path.Combine(output,"qr-compact-"+style+".png")));bool decoded=RoomQr.Decode(pic.GetPixels32(),pic.width,pic.height)==client.InvitationPayload;Destroy(pic);qrReads.cases.Add(new QrRead{style=style,decoded=decoded});Debug.Log("QR COMPACT "+style+" screenshot decoded="+decoded);
   }
   File.WriteAllText(Path.Combine(output,"qr-readability.json"),JsonUtility.ToJson(qrReads,true));lobby.DesignPreviewQrTheme(false);lobby.DesignPreviewClock=-1;
  }
 }
}
