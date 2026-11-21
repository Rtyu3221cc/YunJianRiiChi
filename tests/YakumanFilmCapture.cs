using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace YunJian.Riichi.Editor {
 [InitializeOnLoad] public static class YakumanFilmCapture {
  const string Key="YakumanFilmCapture";
  static YakumanFilmCapture(){EditorApplication.update+=()=>{if(!SessionState.GetBool(Key,false))return;if(EditorApplication.isPaused)EditorApplication.isPaused=false;if(EditorApplication.isPlaying&&!EditorApplication.isCompiling&&UnityEngine.Object.FindAnyObjectByType<YakumanFilmRunner>()==null)UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<YakumanFilmRunner>();};}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Bootstrap(){if(!SessionState.GetBool(Key,false))return;string repo=Environment.GetEnvironmentVariable("YUNJIAN_1114_REPO");CharacterRoster.PreviewOnly=true;CharacterPackService.RootOverride=repo+"/build/character-voice-r34/roles";CharacterPackService.RespectPreviewAvailability=true;CharacterPackService.ResetForReview();WardrobePackService.RootOverride=repo+"/build/feedback-1114-r39/wardrobes";WardrobePackService.RespectPreviewAvailability=true;WardrobePackService.ResetForReview();}
  public static void Run(){PlayerSettings.companyName="YunJianReview";PlayerSettings.productName="YakumanFilm1114";EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
 }
 public sealed class YakumanFilmRunner:MonoBehaviour {
  [Serializable]public class Case {public string name,key,tile;public string[] tiles,melds;public bool ron;public int from;public float sourceStart,sourceScene,sourceEnd;}
  [Serializable]class Cases {public Case[] cases;}
  const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  string repo,output;int capturedCases;RiichiClient client;TableEffects effects;TableRenderer table;
  System.Diagnostics.Process encoder;Texture2D pixels;RenderTexture full,small;
  IEnumerator Start(){repo=Environment.GetEnvironmentVariable("YUNJIAN_1114_REPO");output=Environment.GetEnvironmentVariable("YUNJIAN_FILM_OUTPUT");if(string.IsNullOrEmpty(output))output=repo+"/build/yakuman-alignment-r40/capture";Directory.CreateDirectory(output);var stack=new Stack<IEnumerator>();stack.Push(Capture());while(stack.Count>0){object next;try{var task=stack.Peek();if(!task.MoveNext()){stack.Pop();continue;}next=task.Current;if(next is IEnumerator child){stack.Push(child);continue;}}catch(Exception e){File.WriteAllText(output+"/failed.txt",e.ToString());Debug.LogException(e);if(encoder!=null&&!encoder.HasExited)encoder.Kill();EditorApplication.Exit(1);yield break;}yield return next;}File.WriteAllText(output+"/complete.txt",capturedCases+" native cases; actual Unity render; table launch included");EditorApplication.Exit(0);}
  IEnumerator Capture(){
   var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.GetType().GetMethod("SetCustomResolution",Private).Invoke(window,new object[]{new Vector2(1280,720),"Yakuman comparison 16:9"});for(int i=0;i<6;i++)yield return null;
   client=GetComponent<RiichiClient>();client.SessionPreviewOnly=true;client.BeginLobbyValidation("film");GetComponent<CharacterPackService>().StopAllCoroutines();GetComponent<ResourceRoomCoordinator>().enabled=false;CharacterRoster.BeginPreview();WardrobeSelection.BeginPreview();AudioListener.volume=0;effects=GetComponent<TableEffects>();table=GetComponent<TableRenderer>();
   yield return WardrobePackService.Preload(new[]{"mjp_rosered"});
   if(!WardrobePackService.Installed("mjp_rosered"))throw new Exception("Reference rose-red back pack missing");
   full=new RenderTexture(1280,720,0);full.Create();small=new RenderTexture(960,540,0);small.Create();pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
   var cases=JsonUtility.FromJson<Cases>(File.ReadAllText(repo+"/tests/fixtures/yakuman-video-1114.json")).cases;
   foreach(var item in cases){
    string filter=Environment.GetEnvironmentVariable("YUNJIAN_FILM_CASE");if(!string.IsNullOrEmpty(filter)&&!filter.Split(',').Contains(item.key))continue;
    var detail=new WinDetail{seat=0,from=item.from,ron=item.ron,winSource=item.ron?"discard":"tsumo",tile=item.tile,tiles=item.tiles,melds=item.melds,yaku=new[]{new YakuDetail{name=item.name,yakuman=1}},limit="役满",score=new ScoreDetail{damanguan=1}};
    var view=DressPreviewStates.Tsumo("film-"+item.key,new WardrobeLoadout{tileBack="mjp_rosered",tileFace="mjpface_default"},"yiji",400101,item.name);view.players[0].melds=item.melds;view.players[0].count=item.tiles.Length+(item.ron?0:1);view.hand=item.tiles.Concat(item.ron?Array.Empty<string>():new[]{item.tile}).ToArray();view.drawn=item.ron?null:item.tile;view.result.details=new[]{detail};view.audioEvents=Array.Empty<PublicSound>();
    if(item.ron){var river=view.players[item.from].river.ToList();river.Add(new RiverTile{tile=item.tile});view.players[item.from].river=river.ToArray();view.last=new LastDiscard{from=item.from,tile=item.tile};}
    effects.Reset();client.ShowValidationFixture(view);effects.ValidationFreeze=true;effects.ValidationTime=0;for(int i=0;i<4;i++)yield return null;
    var cinematic=new YakumanCinematic();cinematic.Play(table,detail,new PublicSound{kind=item.ron?"ron":"tsumo",seat=0,from=item.from,tile=item.tile,winSource=detail.winSource});
    if(cinematic.SourceKey!=item.key)throw new Exception("Wrong scene "+item.key+" -> "+cinematic.SourceKey);
    typeof(TableEffects).GetField("yakumanCinematic",Private).SetValue(effects,cinematic);typeof(TableEffects).GetField("yakumanStartedAt",Private).SetValue(effects,0f);typeof(TableEffects).GetField("winResultReadyAt",Private).SetValue(effects,YakumanCinematic.Duration);
    var info=new System.Diagnostics.ProcessStartInfo{FileName="C:/Users/Rtyu3221/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe",Arguments="-X utf8 \""+repo+"/scripts/encode-unity-capture-r40.py\" --output \""+output+"/"+item.key+".mp4\"",UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};encoder=System.Diagnostics.Process.Start(info);Debug.Log("FILM START "+item.key);
    int frames=Mathf.CeilToInt(YakumanCinematic.Duration*30);File.WriteAllText(output+"/"+item.key+"-flight.txt","");
    for(int frame=0;frame<frames;frame++){
     float age=frame/30f;effects.ValidationTime=age;cinematic.Sample(age);yield return null;yield return new WaitForEndOfFrame();
     ScreenCapture.CaptureScreenshotIntoRenderTexture(full);Graphics.Blit(full,small,new Vector2(1,-1),new Vector2(0,1));var prior=RenderTexture.active;RenderTexture.active=small;pixels.ReadPixels(new Rect(0,0,960,540),0,0);RenderTexture.active=prior;var bytes=pixels.GetRawTextureData<byte>().ToArray();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);
     if(frame==30||frame==45||frame==59){var ghost=(GameObject)typeof(YakumanCinematic).GetField("ghost",Private).GetValue(cinematic);File.AppendAllText(output+"/"+item.key+"-flight.txt",frame+" position="+ghost.transform.position+" viewport="+table.Camera.WorldToViewportPoint(ghost.transform.position)+" renders="+string.Join(";",ghost.GetComponentsInChildren<Renderer>(true).Select(r=>r.name+" active="+r.gameObject.activeInHierarchy+" enabled="+r.enabled+" off="+r.forceRenderingOff+" layer="+r.gameObject.layer))+"\n");}
     if(frame==8||frame==15||frame==25||frame==42){var root=(GameObject)typeof(YakumanCinematic).GetField("burstRoot",Private).GetValue(cinematic);var cam=table.GetComponent<NativeEffectCamera>();var buffer=new ParticleSystem.Particle[4];File.AppendAllText(output+"/"+item.key+"-burst.txt","frame "+frame+" rootActive="+root.activeInHierarchy+" root="+root.transform.position+" effectCamera="+(cam!=null)+"\n"+string.Join("\n",root.GetComponentsInChildren<ParticleSystem>(true).Select(p=>{var r=p.GetComponent<ParticleSystemRenderer>();int n=p.GetParticles(buffer);return "  "+p.name+" active="+p.gameObject.activeInHierarchy+" layer="+p.gameObject.layer+" count="+p.particleCount+" time="+p.time.ToString("F3")+" playing="+p.isPlaying+" paused="+p.isPaused+" emitting="+p.isEmitting+" mode="+r.renderMode+" renderer="+r.enabled+" maxSize="+r.maxParticleSize+" space="+p.main.simulationSpace+" pos="+p.transform.position.ToString("F4")+" mat="+(r.sharedMaterial==null?"null":r.sharedMaterial.shader.name+" q"+r.sharedMaterial.renderQueue+" zt"+r.sharedMaterial.GetFloat("_ZTest"))+(n>0?" first="+buffer[0].position.ToString("F4")+" size="+buffer[0].GetCurrentSize(p).ToString("F4")+" color="+buffer[0].GetCurrentColor(p):"");}))+"\n");}
     if(frame==84){var scene=(GameObject)typeof(YakumanCinematic).GetField("scene",Private).GetValue(cinematic);var cam=(Camera)typeof(YakumanCinematic).GetField("camera",Private).GetValue(cinematic);File.WriteAllLines(output+"/"+item.key+"-portrait.txt",scene.GetComponentsInChildren<UnityEngine.UI.RawImage>(true).Select(r=>{var corners=new Vector3[4];r.rectTransform.GetWorldCorners(corners);return r.name+" tex="+r.texture+" color="+r.color+" active="+r.gameObject.activeInHierarchy+" cull="+r.canvasRenderer.cull+" alpha="+r.canvasRenderer.GetInheritedAlpha()+" rect="+r.rectTransform.rect+" viewport="+string.Join(";",corners.Select(v=>cam.WorldToViewportPoint(v).ToString("F4")));}));}
     if(frame==45||frame==59||frame==210)File.WriteAllBytes(output+"/"+item.key+"-"+frame+".png",pixels.EncodeToPNG());
    }
    encoder.StandardInput.Close();while(!encoder.HasExited)yield return null;if(encoder.ExitCode!=0)throw new Exception(encoder.StandardError.ReadToEnd());File.WriteAllText(output+"/"+item.key+"-encoder.txt",encoder.StandardOutput.ReadToEnd());encoder.Dispose();encoder=null;effects.Reset();capturedCases++;yield return null;Debug.Log("FILM COMPLETE "+item.key);
   }
   small.Release();full.Release();Destroy(small);Destroy(full);Destroy(pixels);
  }
 }
}
