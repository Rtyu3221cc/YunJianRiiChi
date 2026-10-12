using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
namespace YunJian.Riichi.Editor {
 public static class ReferenceVideoFrames {
  static string[] files; static int fileIndex, step; static VideoPlayer player; static RenderTexture target; static double requested, deadline; static bool pending; static string folder;
  public static void Run(){
   files=Directory.GetFiles(@"D:\MyWorkSpace\video\1011-1","*.mp4",SearchOption.AllDirectories); folder=Path.GetFullPath("../build/reference/1011/frames");Directory.CreateDirectory(folder);
   EditorApplication.update+=Tick;Next();
  }
  static void Next(){
   if(player!=null)UnityEngine.Object.DestroyImmediate(player.gameObject);if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}
   if(fileIndex>=files.Length){Debug.Log("REFERENCE VIDEO PASS");EditorApplication.Exit(0);return;}
   player=new GameObject("Reference video").AddComponent<VideoPlayer>();player.source=VideoSource.Url;player.url=files[fileIndex];player.playOnAwake=false;player.audioOutputMode=VideoAudioOutputMode.None;player.renderMode=VideoRenderMode.RenderTexture;
   player.errorReceived+=(p,s)=>{Debug.LogError(s);EditorApplication.Exit(1);};player.prepareCompleted+=Prepared;deadline=EditorApplication.timeSinceStartup+40;player.Prepare();
  }
  static void Prepared(VideoPlayer p){
   target=new RenderTexture((int)p.width,(int)p.height,0,RenderTextureFormat.ARGB32);target.Create();p.targetTexture=target;step=0;
   File.WriteAllText(Path.Combine(folder,"video-"+fileIndex+".txt"),files[fileIndex]+"\n"+p.width+"x"+p.height+" fps="+p.frameRate+" duration="+p.length);
   p.seekCompleted+=Seeked;p.Play();p.Pause();Seek();
  }
  static void Seek(){requested=Math.Min(player.length-.04,step*.1);pending=true;deadline=EditorApplication.timeSinceStartup+15;player.time=requested;}
  static void Seeked(VideoPlayer p){pending=false;deadline=EditorApplication.timeSinceStartup+.08;}
  static void Tick(){
   if(EditorApplication.timeSinceStartup<deadline)return;
   if(player==null || !player.isPrepared || pending){Debug.LogError("Reference video timed out: "+files[fileIndex]);EditorApplication.Exit(1);return;}
   var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,"video-"+fileIndex+"-"+step.ToString("D4")+".png"),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;
   step++;if(step*.1<player.length && step<500)Seek();else {Debug.Log("Extracted reference "+files[fileIndex]+" frames="+step);fileIndex++;Next();}
  }
 }
}
