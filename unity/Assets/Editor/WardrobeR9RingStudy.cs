using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi.Editor {
    public static class WardrobeR9RingStudy {
        [Serializable] public class Sample {public int viewer;public string name;public float age,upDot;}
        [Serializable] public class Report {public int checks;public List<Sample> samples=new List<Sample>();public List<float> spinAngles=new List<float>();}
        public static void Run(){
            var report=new Report();var camera=new GameObject("Ring camera").AddComponent<Camera>();
            for(int viewer=0;viewer<4;viewer++){
                camera.transform.position=Quaternion.Euler(0,viewer*90,0)*new Vector3(0,6,-8);camera.transform.LookAt(Vector3.zero);
                var root=new GameObject("Ring regression");var effect=root.AddComponent<WinTileEffect>();effect.InitializeTable("Majsoul/Actions/ron-reactor-effect",Vector3.zero,Quaternion.identity,0,507);
                var directions=new Dictionary<string,Vector3>();var angles=new Dictionary<string,float>();
                foreach(float age in new[]{.08f,.16f,.32f,.48f,.64f,.8f}){
                    effect.Sample(age);
                    foreach(string name in new[]{"quan di wai","quan di nei"}){
                        var ps=effect.GetComponentsInChildren<ParticleSystem>().First(p=>p.name.StartsWith(name));var mesh=new Mesh();ps.GetComponent<ParticleSystemRenderer>().BakeMesh(mesh,camera,true);var vertices=mesh.vertices;var triangles=mesh.triangles;
                        if(triangles.Length<3)throw new Exception("Ring unexpectedly disappeared: "+name);
                        var normal=Vector3.Cross(vertices[triangles[1]]-vertices[triangles[0]],vertices[triangles[2]]-vertices[triangles[0]]).normalized;float dot=Mathf.Abs(Vector3.Dot(normal,Vector3.up));
                        if(dot<.9999f)throw new Exception("Ring plane tilts during spin: "+name+" age="+age+" normal="+normal);report.checks++;report.samples.Add(new Sample{viewer=viewer,name=name,age=age,upDot=dot});
                        var center=vertices.Aggregate(Vector3.zero,(sum,v)=>sum+v)/vertices.Length;var radial=Vector3.ProjectOnPlane(vertices[0]-center,Vector3.up).normalized;
                        if(age==.08f)directions[name]=radial;if(age==.32f)angles[name]=Vector3.SignedAngle(directions[name],radial,Vector3.up);UnityEngine.Object.DestroyImmediate(mesh);
                    }
                }
                if(angles.Values.Any(a=>Mathf.Abs(a)<30)||angles["quan di wai"]*angles["quan di nei"]>=0)throw new Exception("Rings must spin in opposite directions without freezing");report.checks++;report.spinAngles.AddRange(angles.Values);UnityEngine.Object.DestroyImmediate(root);
            }
            File.WriteAllText("../build/wardrobe-r9-study/ring-validation.json",JsonUtility.ToJson(report,true));UnityEngine.Object.DestroyImmediate(camera.gameObject);Debug.Log("REACTOR RING REGRESSION PASS "+report.checks);EditorApplication.Exit(0);
        }
    }
}
