using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace YunJian.Riichi {
 // UI_CutIn original RectTransform hierarchy and AnimationClip curves. Art is
 // extracted from the APK; Ichihime replaces the winner's selectable illustration.
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
  public bool Freeze;public float FrozenAge;
  public float Age=>Freeze?FrozenAge:Time.unscaledTime-began;
  public bool Active=>canvas!=null && Age<Duration;
  public void Play(){Reset();Build();began=Time.unscaledTime;}
  public void Reset(){if(canvas!=null){canvas.SetActive(false);UnityEngine.Object.Destroy(canvas);}if(camera!=null)UnityEngine.Object.Destroy(camera.gameObject);if(image!=null){image.Release();UnityEngine.Object.Destroy(image);}canvas=null;camera=null;image=null;nodes.Clear();curves.Clear();Freeze=false;}
  void Build(){
   canvas=new GameObject("APK UI_CutIn",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
   camera=new GameObject("APK cutin composition camera").AddComponent<Camera>();camera.enabled=false;camera.transform.position=new Vector3(7000,5000,-50);camera.orthographic=true;camera.orthographicSize=540;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<15;camera.nearClipPlane=.1f;camera.farClipPlane=100;
   var c=canvas.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=10;c.sortingOrder=120;canvas.layer=15;
   var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
   nodes[""]=canvas.GetComponent<RectTransform>();
   var layout=JsonUtility.FromJson<Layout>(Resources.Load<TextAsset>("Majsoul/Review1012c/cutin-layout").text);
   foreach(var n in layout.nodes){
    var path=n.path.Substring("/UI_CutIn".Length).TrimStart('/');if(path.Length==0 || path=="curtain")continue;
    var parent=path.Contains("/")?path.Substring(0,path.LastIndexOf('/')):"";
    var go=new GameObject(path.Substring(path.LastIndexOf('/')+1),typeof(RectTransform));go.layer=15;var rt=go.GetComponent<RectTransform>();rt.SetParent(nodes[parent],false);
    rt.anchorMin=new Vector2(n.anchorMin[0],n.anchorMin[1]);rt.anchorMax=new Vector2(n.anchorMax[0],n.anchorMax[1]);rt.pivot=new Vector2(n.pivot[0],n.pivot[1]);rt.sizeDelta=new Vector2(n.size[0],n.size[1]);rt.anchoredPosition=new Vector2(n.position[0],n.position[1]);rt.localRotation=new Quaternion(n.rotation[0],n.rotation[1],n.rotation[2],n.rotation[3]);rt.localScale=new Vector3(n.scale[0],n.scale[1],n.scale[2]);nodes[path]=rt;
    string image=path=="root/backline"?"Majsoul/Review1012c/cutinbg":path=="root/frontline"?"Majsoul/Review1012c/cutin_line":path.EndsWith("/illust")?"Majsoul/Actions/yiji-full":null;
    if(image!=null){var raw=go.AddComponent<RawImage>();raw.texture=Resources.Load<Texture2D>(image);raw.raycastTarget=false;}
    if(path.EndsWith("/mask")){go.AddComponent<Image>().raycastTarget=false;go.AddComponent<Mask>().showMaskGraphic=false;}
    if(path=="root/container_illust"){portrait=go.AddComponent<CanvasGroup>();portrait.blocksRaycasts=false;}
   }
   var clip=JsonUtility.FromJson<Clip>(Resources.Load<TextAsset>("Majsoul/Review1012c/cutin-animation").text);
   foreach(var s in clip.m_ScaleCurves)curves[s.path+":scaleY"]=new AnimationCurve(s.curve.m_Curve.Select(k=>new Keyframe(k.time,k.value.y,k.inSlope.y,k.outSlope.y)).ToArray());
   foreach(var f in clip.m_FloatCurves)curves[f.path+":"+f.attribute]=new AnimationCurve(f.curve.m_Curve.Select(k=>new Keyframe(k.time,k.value,k.inSlope,k.outSlope)).ToArray());
   Sample(0);
  }
  public void Sample(float age){
   if(canvas==null)return;
   foreach(var entry in curves){var split=entry.Key.Split(':');var rt=nodes["root/"+split[0]];float value=entry.Value.Evaluate(age);
    if(split[1]=="scaleY")rt.localScale=new Vector3(rt.localScale.x,value,rt.localScale.z);
    else if(split[1]=="m_AnchoredPosition.x")rt.anchoredPosition=new Vector2(value,rt.anchoredPosition.y);
    else if(split[1]=="m_AnchoredPosition.y")rt.anchoredPosition=new Vector2(rt.anchoredPosition.x,value);
    else if(split[1]=="m_Alpha")portrait.alpha=value;
   }
   canvas.SetActive(age<Duration);
  }
  public void Draw(float width,float height){
   Sample(Age);if(!Active||Event.current.type!=EventType.Repaint)return;
   if(image==null || image.width!=Screen.width || image.height!=Screen.height){if(image!=null){image.Release();UnityEngine.Object.Destroy(image);}image=new RenderTexture(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32){name="Original UI_CutIn composition"};image.Create();camera.targetTexture=image;}
   camera.aspect=Screen.width/(float)Screen.height;Canvas.ForceUpdateCanvases();camera.Render();
   var matrix=GUI.matrix;GUI.matrix=Matrix4x4.identity;GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),image,ScaleMode.StretchToFill,true);GUI.matrix=matrix;
  }
 }
}
