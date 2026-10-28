using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    public static class ScytheR13Validation {
        [Serializable] class Row {public int view;public float age,upDot,sideDot;public Vector3 center,normal;}
        [Serializable] class Report {public int checks;public List<Row> rows=new List<Row>();}
        static Row Blade(NativeParticleEffect effect,Camera camera,int view,float age){
            effect.Sample(age);var renderer=effect.GetComponentsInChildren<ParticleSystemRenderer>(true).Single(r=>r.name=="liandao_anim");var mesh=new Mesh();renderer.BakeMesh(mesh,camera,true);
            var p=mesh.vertices;var tris=mesh.triangles;if(tris.Length<3)throw new Exception("Missing native scythe blade");
            var normal=Vector3.Cross(p[tris[1]]-p[tris[0]],p[tris[2]]-p[tris[0]]).normalized;
            var result=new Row{view=view,age=age,center=p.Aggregate(Vector3.zero,(a,b)=>a+b)/p.Length,normal=normal,upDot=Mathf.Abs(Vector3.Dot(normal,Vector3.up)),sideDot=Mathf.Abs(Vector3.Dot(normal,Vector3.right))};UnityEngine.Object.DestroyImmediate(mesh);return result;
        }
        public static void Run(){
            var config=JsonUtility.FromJson<WinTileEffect.Config>(Resources.Load<TextAsset>("Majsoul/Actions/native-effect_liqi_23ex_liandao").text);var camera=new GameObject("Native blade geometry camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,.5f,-.7f);camera.transform.LookAt(Vector3.zero);var report=new Report();
            foreach(int view in Enumerable.Range(0,4)){
                float angle=view==0?0:view==1?90:view==2?180:-90;int sourceView=((int)Mathf.Round(Mathf.Repeat(-angle,360)/90))%4;
                var root=new GameObject("Scythe view "+view);root.transform.rotation=Quaternion.Euler(config.nativeWorldEuler[0],config.nativeWorldEuler[1],config.nativeWorldEuler[2]);var effect=root.AddComponent<NativeParticleEffect>();effect.Initialize(config.nativeVariants[sourceView],507);
                foreach(float age in new[]{.08f,.12f,.16f,.2f,.24f,.28f,.32f,.36f,.4f,.5f})report.rows.Add(Blade(effect,camera,view,age));
                UnityEngine.Object.DestroyImmediate(root);
            }
            Directory.CreateDirectory("../build/effects-r13-review");File.WriteAllText("../build/effects-r13-review/blade-geometry.json",JsonUtility.ToJson(report,true));
            foreach(int view in Enumerable.Range(0,4)){
                var rows=report.rows.Where(r=>r.view==view).ToArray();
                float angle=view==0?0:view==1?90:view==2?180:-90;var tileShort=Quaternion.Euler(0,angle+90,0)*Vector3.right;
                if(rows.Any(r=>r.upDot>.5f||Mathf.Abs(Vector3.Dot(r.normal,tileShort))<.85f))throw new Exception("Scythe does not stay in the vertical plane through the tile's long axis: view "+view);report.checks++;
                var high=rows.First(r=>Mathf.Abs(r.age-.12f)<.001f);var low=rows.First(r=>Mathf.Abs(r.age-.32f)<.001f);
                if(high.center.y-low.center.y<.05f)throw new Exception("Blade must fall toward the tile face: view "+view+" high="+high.center+" low="+low.center);report.checks++;
            }
            var oldRoot=new GameObject("Rejected R12 plane");oldRoot.transform.rotation=Quaternion.Euler(0,90,0)*Quaternion.Euler(90,90,0);var old=oldRoot.AddComponent<NativeParticleEffect>();old.Initialize(config.nativeVariants[0],507);var failed=Blade(old,camera,0,.24f);if(failed.upDot<.99f)throw new Exception("Regression control does not detect R12's horizontal blade plane");report.checks++;UnityEngine.Object.DestroyImmediate(oldRoot);
            var liveRoot=new GameObject("Production scythe attachment");var live=liveRoot.AddComponent<WinTileEffect>();live.InitializeTable("Majsoul/Actions/native-effect_liqi_23ex_liandao",Vector3.zero,Quaternion.identity,0,507);
            foreach(var pose in new[]{Quaternion.identity,Quaternion.Euler(20,90,30),Quaternion.Euler(-60,180,45),Quaternion.Euler(0,270,0)}){if(!live.HasFixedWorldRotation||Quaternion.Angle(live.TableRotation(pose),Quaternion.Euler(0,180,0))>.001f)throw new Exception("Held tile rotation leaked into the source scythe world plane");report.checks++;}UnityEngine.Object.DestroyImmediate(liveRoot);
            File.WriteAllText("../build/effects-r13-review/blade-geometry.json",JsonUtility.ToJson(report,true));Debug.Log("SCYTHE R13 GEOMETRY PASS "+report.checks);EditorApplication.Exit(0);
        }
    }
}
