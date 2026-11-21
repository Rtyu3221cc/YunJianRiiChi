using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace YunJian.Riichi.Editor {
 [InitializeOnLoad] public static class CharacterHandGallery {
  const string Key="CharacterHandGallery";
  static CharacterHandGallery(){EditorApplication.update+=()=>{if(EditorApplication.isPlaying&&!EditorApplication.isCompiling&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<CharacterHandGalleryCapture>();}};}
  public static void Run(){PlayerSettings.companyName="YunJianReview";PlayerSettings.productName="HandGallery1114";EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
 }
 public sealed class CharacterHandGalleryCapture:MonoBehaviour {
  [Serializable] class Entry {public string key,characters,action,image;public int physical;public bool currentRoster;}
  [Serializable] class Report {public List<Entry> entries=new List<Entry>();}
  string output;Report report=new Report();
  IEnumerator Start(){
   output=Environment.GetEnvironmentVariable("YUNJIAN_HAND_GALLERY") ;Directory.CreateDirectory(output);GetComponent<RiichiClient>().BeginLobbyValidation("hand-gallery");GetComponent<CharacterPackService>().StopAllCoroutines();GetComponent<ResourceRoomCoordinator>().enabled=false;CharacterRoster.PreviewOnly=true;AudioListener.volume=0;
   var iterator=Capture();while(true){object next;try{if(!iterator.MoveNext())break;next=iterator.Current;}catch(Exception e){File.WriteAllText(output+"/failed.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
   File.WriteAllText(output+"/gallery.json",JsonUtility.ToJson(report,true));Debug.Log("HAND GALLERY COMPLETE "+report.entries.Count);EditorApplication.Exit(0);
  }
  void Save(Texture image,string path){var prior=RenderTexture.active;RenderTexture.active=(RenderTexture)image;var t=new Texture2D(image.width,image.height,TextureFormat.RGBA32,false);t.ReadPixels(new Rect(0,0,image.width,image.height),0,0);t.Apply();File.WriteAllBytes(output+"/"+path,t.EncodeToPNG());Destroy(t);RenderTexture.active=prior;}
  void FitVisible(Camera camera,RenderTexture image){
   camera.orthographicSize*=2;
   var scratch=new Texture2D(image.width,image.height,TextureFormat.RGBA32,false);var prior=RenderTexture.active;
   for(int attempt=0;attempt<4;attempt++){
    camera.Render();RenderTexture.active=image;scratch.ReadPixels(new Rect(0,0,image.width,image.height),0,0);var pixels=scratch.GetPixels32();var bg=pixels[0];int left=image.width,right=0,bottom=image.height,top=0;
    for(int y=0;y<image.height;y++)for(int x=0;x<image.width;x++){var p=pixels[y*image.width+x];if(Math.Abs(p.r-bg.r)+Math.Abs(p.g-bg.g)+Math.Abs(p.b-bg.b)<15)continue;left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
    if(left>=right)throw new Exception("Empty hand render");
    if(left<3||bottom<3||right>image.width-4||top>image.height-4){camera.orthographicSize*=2;continue;}
    float unit=2*camera.orthographicSize/image.height;
    camera.transform.position+=camera.transform.right*((left+right-image.width)*.5f*unit)+camera.transform.up*((bottom+top-image.height)*.5f*unit);
    camera.orthographicSize*=Mathf.Max((right-left)/(image.width*.82f),(top-bottom)/(image.height*.82f));break;
   }
   RenderTexture.active=prior;Destroy(scratch);camera.Render();
  }
  IEnumerator Capture(){
   var camera=new GameObject("Hand gallery camera").AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.nearClipPlane=.001f;camera.farClipPlane=200;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.09f,.13f,.19f,1);camera.cullingMask=1<<27;camera.allowMSAA=true;
   var image=new RenderTexture(1000,800,24,RenderTextureFormat.ARGB32){antiAliasing=4};image.Create();camera.targetTexture=image;
   foreach(var hand in CharacterHands.All){var roles=CharacterRoster.All.Where(r=>hand.characters.Contains(r.id)).ToArray();
    for(int physical=0;physical<hand.variants.Length;physical++)foreach(string action in new[]{"Dapai","LiQiPai","LiZhi","Fulu","Zimo"}){
     var clip=CharacterHands.Clip(new WardrobeItem{nativeHand=hand.key},physical,action);var go=new GameObject("Hand gallery "+hand.key);go.layer=27;go.transform.position=new Vector3(600,600,600);var rig=go.AddComponent<CharacterHandVisual>();rig.Initialize(clip);rig.SampleFrame((clip.frames-1)*.45f);
     var bounds=new Bounds();bool first=true;var baked=new Mesh();foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled)){skin.BakeMesh(baked,false);foreach(var p in baked.vertices){var world=skin.transform.TransformPoint(p);if(first){bounds=new Bounds(world,Vector3.zero);first=false;}else bounds.Encapsulate(world);}}Destroy(baked);if(first)throw new Exception("No geometry "+hand.key);
     float size=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);camera.orthographicSize=size*.53f;camera.transform.position=bounds.center+new Vector3(size*.15f,size*.8f,-size*1.6f);camera.transform.LookAt(bounds.center);FitVisible(camera,image);string file=hand.key+"-"+physical+"-"+action+".png";Save(image,file);
     report.entries.Add(new Entry{key=hand.key,characters=string.Join("、",roles.Select(r=>r.name)),currentRoster=roles.Length>0,physical=physical,action=action,image=file});go.SetActive(false);Destroy(go);yield return null;
    }
   }
   camera.targetTexture=null;image.Release();Destroy(image);Destroy(camera.gameObject);
   foreach(string key in new[]{"hand-human","hand-man","hand-dog","hand-cat-orange","hand-cat"}){
    var item=WardrobeCatalog.Find(key);using(var preview=new WardrobeMotionPreview()){preview.Tick(item,.7f);var previewCamera=(Camera)typeof(WardrobeMotionPreview).GetField("camera",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(preview);FitVisible(previewCamera,(RenderTexture)preview.Image);string file=key+"-0-LiZhi.png";Save(preview.Image,file);report.entries.Add(new Entry{key=key,characters=item.name,currentRoster=key!="hand-cat",physical=0,action="LiZhi",image=file});}yield return null;
   }
  }
 }
}
