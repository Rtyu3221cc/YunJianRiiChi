using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.Rendering;
namespace YunJian.Riichi {
 // UI_CutIn original RectTransform hierarchy and AnimationClip curves. Art is
 // extracted from the APK, with the winning seat's current illustration.
 public sealed class WinCutIn {
  [Serializable] class Node {public string path;public float[] position,size,pivot,anchorMin,anchorMax,rotation,scale;}
  [Serializable] class Layout {public Node[] nodes;}
  [Serializable] class V {public float x,y,z;}
  [Serializable] class VK {public float time;public V value,inSlope,outSlope;}
  [Serializable] class FK {public float time,value,inSlope,outSlope;}
  [Serializable] class VC {public VK[] m_Curve;}
  [Serializable] class FC {public FK[] m_Curve;}
  [Serializable] class SC {public string path;public VC curve;}
  [Serializable] class FloatCurve {public string path,attribute;public FC curve;}
  [Serializable] class Clip {public SC[] m_ScaleCurves;public FloatCurve[] m_FloatCurves;}
  readonly Dictionary<string,RectTransform> nodes=new Dictionary<string,RectTransform>();
  readonly Dictionary<string,AnimationCurve> curves=new Dictionary<string,AnimationCurve>();
  GameObject canvas;CanvasGroup portrait;Camera camera;RenderTexture image;float began;
  public const float Duration=1.2f;
  public const int CompositionLayer=30;
  // Use the winner's limit, never points including honba, sticks or dealer bonus.
  public static bool Eligible(WinDetail detail){
   var score=detail?.score;
   if(score!=null&&(score.damanguan>0 || score.fanshu>0 || score.fu>0))return score.damanguan>0 || score.fanshu>=5 || (score.fanshu==4&&score.fu>=40) || (score.fanshu==3&&score.fu>=70);
   var limit=detail?.limit;
   return limit=="满贯"||limit=="跳满"||limit=="倍满"||limit=="三倍满"||limit=="累计役满"||(!string.IsNullOrEmpty(limit)&&limit.Contains("役满"));
  }
  public static bool Eligible(RoomView view,int seat)=>Eligible(System.Array.Find(view?.result?.details??new WinDetail[0],d=>d!=null&&d.seat==seat));
  public bool Freeze;public float FrozenAge;
  public float Age=>Freeze?FrozenAge:Time.unscaledTime-began;
  public bool Active=>canvas!=null && Age<Duration;
  PlayerView player;
  public void Play(PlayerView character=null){Reset();player=character;Build();began=Time.unscaledTime;}
  public void Reset(){
   if(canvas!=null){canvas.SetActive(false);UnityEngine.Object.Destroy(canvas);}
   if(camera!=null){camera.targetTexture=null;UnityEngine.Object.Destroy(camera.gameObject);}
   if(image!=null){image.Release();UnityEngine.Object.Destroy(image);}
   canvas=null;camera=null;image=null;portrait=null;nodes.Clear();curves.Clear();Freeze=false;
  }
  void Build(){
   canvas=new GameObject("APK UI_CutIn",typeof(RectTransform),typeof(Canvas));
   camera=new GameObject("APK cutin composition camera").AddComponent<Camera>();
   camera.enabled=false;
   camera.orthographic=true;
   camera.clearFlags=CameraClearFlags.SolidColor;
   camera.backgroundColor=Color.clear;
   camera.cullingMask=1<<CompositionLayer;
   camera.nearClipPlane=.1f;
   camera.farClipPlane=100;
   camera.allowHDR=false;
   camera.allowMSAA=false;
   camera.transform.position=new Vector3(7000,5000,-50);
   var c=canvas.GetComponent<Canvas>();
   c.renderMode=RenderMode.WorldSpace;
   c.worldCamera=camera;
   canvas.layer=CompositionLayer;
   var root=canvas.GetComponent<RectTransform>();
   root.position=camera.transform.position+Vector3.forward*10;
   nodes[""]=root;
   var layout=JsonUtility.FromJson<Layout>(Resources.Load<TextAsset>("Majsoul/Review1012c/cutin-layout").text);
   foreach(var n in layout.nodes){
    var path=n.path.Substring("/UI_CutIn".Length).TrimStart('/');if(path.Length==0 || path=="curtain")continue;
    var parent=path.Contains("/")?path.Substring(0,path.LastIndexOf('/')):"";
    var go=new GameObject(path.Substring(path.LastIndexOf('/')+1),typeof(RectTransform));go.layer=CompositionLayer;var rt=go.GetComponent<RectTransform>();rt.SetParent(nodes[parent],false);
    rt.anchorMin=new Vector2(n.anchorMin[0],n.anchorMin[1]);rt.anchorMax=new Vector2(n.anchorMax[0],n.anchorMax[1]);rt.pivot=new Vector2(n.pivot[0],n.pivot[1]);rt.sizeDelta=new Vector2(n.size[0],n.size[1]);rt.anchoredPosition=new Vector2(n.position[0],n.position[1]);rt.localRotation=new Quaternion(n.rotation[0],n.rotation[1],n.rotation[2],n.rotation[3]);rt.localScale=new Vector3(n.scale[0],n.scale[1],n.scale[2]);nodes[path]=rt;
    string image=path=="root/backline"?"Majsoul/Review1012c/cutinbg":path=="root/frontline"?"Majsoul/Review1012c/cutin_line":path.EndsWith("/illust")?"character":null;
    if(image!=null){var raw=go.AddComponent<RawImage>();var skin=CharacterRoster.Skin(player?.character,player?.skin??0);raw.texture=path.EndsWith("/illust")?CharacterFraming.SceneTexture(skin.id,"K0"):Resources.Load<Texture2D>(image);raw.raycastTarget=false;if(path.EndsWith("/illust")&&raw.texture!=null){var placed=CharacterFraming.NativeLocal(skin.id,"K0",0,new Rect(rt.anchoredPosition,rt.sizeDelta),out bool flip);rt.anchoredPosition=placed.position;rt.sizeDelta=placed.size;rt.localScale=new Vector3(flip?-1:1,rt.localScale.y,rt.localScale.z);}}
    if(path.EndsWith("/mask")){go.AddComponent<Image>().raycastTarget=false;go.AddComponent<Mask>().showMaskGraphic=false;}
    if(path=="root/container_illust"){portrait=go.AddComponent<CanvasGroup>();portrait.blocksRaycasts=false;}
   }
   var clip=JsonUtility.FromJson<Clip>(Resources.Load<TextAsset>("Majsoul/Review1012c/cutin-animation").text);
   foreach(var s in clip.m_ScaleCurves)curves[s.path+":scaleY"]=new AnimationCurve(s.curve.m_Curve.Select(k=>new Keyframe(k.time,k.value.y,k.inSlope.y,k.outSlope.y)).ToArray());
   foreach(var f in clip.m_FloatCurves)curves[f.path+":"+f.attribute]=new AnimationCurve(f.curve.m_Curve.Select(k=>new Keyframe(k.time,k.value,k.inSlope,k.outSlope)).ToArray());
  }
  void Sample(float age){
   foreach(var entry in curves){var split=entry.Key.Split(':');var rt=nodes["root/"+split[0]];float value=entry.Value.Evaluate(age);
    if(split[1]=="scaleY")rt.localScale=new Vector3(rt.localScale.x,value,rt.localScale.z);
    else if(split[1]=="m_AnchoredPosition.x")rt.anchoredPosition=new Vector2(value,rt.anchoredPosition.y);
    else if(split[1]=="m_AnchoredPosition.y")rt.anchoredPosition=new Vector2(rt.anchoredPosition.x,value);
    else if(split[1]=="m_Alpha")portrait.alpha=value;
   }
  }

  public void Render(){
   if(canvas==null)return;
   float age=Age;
   if(age>=Duration){Reset();return;}
   int width=Screen.width,height=Screen.height;
   if(width<=0||height<=0)return;
   if(image==null||image.width!=width||image.height!=height){
    if(image!=null){camera.targetTexture=null;image.Release();UnityEngine.Object.Destroy(image);}
    // UI.Mask requires stencil even when a platform substitutes the default depth format.
    var descriptor=new RenderTextureDescriptor(width,height,RenderTextureFormat.ARGB32,0);
    descriptor.depthStencilFormat=SystemInfo.IsFormatSupported(GraphicsFormat.D24_UNorm_S8_UInt,GraphicsFormatUsage.Render)
     ?GraphicsFormat.D24_UNorm_S8_UInt:GraphicsFormat.D32_SFloat_S8_UInt;
    image=new RenderTexture(descriptor){name="Original UI_CutIn composition"};
    image.Create();
    camera.targetTexture=image;
    // Match the original CanvasScaler's geometric mean of the width and height scales.
    float scale=Mathf.Sqrt((width/1920f)*(height/1080f));
    var size=new Vector2(width/scale,height/scale);
    nodes[""].sizeDelta=size;
    camera.aspect=width/(float)height;
    camera.orthographicSize=size.y*.5f;
   }
   Sample(age);
   Canvas.ForceUpdateCanvases();
   var previous=RenderTexture.active;
   try{camera.Render();}
   finally{RenderTexture.active=previous;}
  }

  public void Draw(){
   if(!Active||image==null||Event.current.type!=EventType.Repaint)return;
   var matrix=GUI.matrix;
   GUI.matrix=Matrix4x4.identity;
   GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),image,ScaleMode.StretchToFill,true);
   GUI.matrix=matrix;
  }
 }
}
