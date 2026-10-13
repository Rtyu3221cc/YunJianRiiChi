using System;
using System.Linq;
using UnityEngine;
namespace YunJian.Riichi {
 public static class NativeReviewLayout {
  [Serializable] public class Placement {public string name;public float[] position,size,rotation,scale;public float span;}
  [Serializable] class Placements {public Placement[] anchors,arrows,cards;}
  static Placements north,score;
  public static int Physical(int relative,int n)=>relative==0?0:n==3?(relative==1?2:3):relative;
  public static Vector3 NorthLocal(int physical,int index,float seatAngle){
   if(north==null)north=JsonUtility.FromJson<Placements>(Resources.Load<TextAsset>("Majsoul/NativeReview/north-anchors").text);
   var a=north.anchors[physical];const float units=TableTileGeometry.SourceUnits;
   return Quaternion.Inverse(Quaternion.Euler(0,seatAngle,0))*new Vector3(-a.position[0],0,-a.position[2])*units+new Vector3(-index*a.span*units,.02f,0);
  }
  static Placements Score=>score??(score=JsonUtility.FromJson<Placements>(Resources.Load<TextAsset>("Majsoul/NativeReview/score-layout").text));
  public static Placement Arrow(int from,int to)=>Score.arrows.First(a=>a.name==from.ToString()+to);
  public static int ScoreSlot(int relative,int n)=>n==3&&relative==2?3:relative;
  public static Rect ScoreRect(int relative,int n){var p=Score.cards.First(c=>c.name=="player_"+ScoreSlot(relative,n));return new Rect(640+p.position[0]*2/3,360-p.position[1]*2/3,p.size[0]*2/3,p.size[1]*2/3);}
  public static Vector2 ArrowCenter(Placement p)=>new Vector2(670+p.position[0]*2/3,415.33333f-p.position[1]*2/3);
  [Serializable] class ClipVector {public float x,y,z;}
  [Serializable] class ClipKey {public float time;public ClipVector value,inSlope,outSlope;}
  [Serializable] class Curve {public ClipKey[] m_Curve;}
  [Serializable] class Scale {public Curve curve;public string path;}
  [Serializable] class NativeClip {public Scale[] m_ScaleCurves;}
  static AnimationCurve rankCurve;
  public static float RankScale(float age){
   if(rankCurve==null){var d=JsonUtility.FromJson<NativeClip>(Resources.Load<TextAsset>("Majsoul/NativeReview/rank-animation").text);var c=d.m_ScaleCurves.First().curve.m_Curve;rankCurve=new AnimationCurve(c.Select(k=>new Keyframe(k.time,k.value.x,k.inSlope.x,k.outSlope.x)).ToArray());}
   return rankCurve.Evaluate(Mathf.Max(0,age));
  }
 }
}
