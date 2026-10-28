using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    public static class ScytheR13Study {
        static bool canonical;
        public static void RunCanonical(){canonical=true;Run();}
        [Serializable] class Row {public string mount;public float age;public Vector3 center,normal,min,max;public int vertices;}
        [Serializable] class Report {public List<Row> rows=new List<Row>();}
        public static void Run(){
            string output=Path.GetFullPath("../build/"+(canonical?"effects-r13-canonical":"effects-r13-study"));Directory.CreateDirectory(output);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var tile=GameObject.CreatePrimitive(PrimitiveType.Cube);tile.transform.localScale=new Vector3(.02947767f,.01913448f,.039f);tile.transform.position=new Vector3(0,-.00956724f,0);
            if(canonical)tile.transform.rotation=Quaternion.Euler(0,90,0);
            var cloth=GameObject.CreatePrimitive(PrimitiveType.Plane);cloth.transform.localScale=Vector3.one*.08f;cloth.transform.position=new Vector3(0,-.02f,0);
            var clothMaterial=new Material(Shader.Find("Unlit/Color"));clothMaterial.color=new Color(.055f,.13f,.22f);cloth.GetComponent<Renderer>().sharedMaterial=clothMaterial;
            var tileMaterial=new Material(Shader.Find("Unlit/Color"));tileMaterial.color=new Color(.9f,.88f,.79f);tile.GetComponent<Renderer>().sharedMaterial=tileMaterial;
            var camera=new GameObject("Diagnostic camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=.21f;camera.nearClipPlane=.001f;camera.farClipPlane=3;camera.backgroundColor=new Color(.035f,.07f,.12f);camera.clearFlags=CameraClearFlags.SolidColor;
            var rt=new RenderTexture(640,480,24);camera.targetTexture=rt;var report=new Report();
            var candidates=canonical?new[]{new Vector3(0,90,0)}:new[]{new Vector3(0,90,0),new Vector3(0,-90,0)};
            foreach(var euler in candidates){
                string key=$"{euler.x}-{euler.y}-{euler.z}";var root=new GameObject("Native scythe "+key);root.transform.rotation=Quaternion.Euler(0,90,0)*Quaternion.Euler(euler);
                var effect=root.AddComponent<NativeParticleEffect>();effect.Initialize("EffectsR11/effect_liqi_23ex_liandao",507);
                var blade=root.GetComponentsInChildren<ParticleSystemRenderer>(true).Single(r=>r.name=="liandao_anim");
                foreach(float age in new[]{.08f,.12f,.16f,.2f,.24f,.28f,.32f,.36f,.4f,.44f,.5f,.6f}){
                    effect.Sample(age);var mesh=new Mesh();blade.BakeMesh(mesh,camera,true);var points=mesh.vertices;
                    if(points.Length>=3){var center=points.Aggregate(Vector3.zero,(a,b)=>a+b)/points.Length;var normal=Vector3.Cross(points[1]-points[0],points[2]-points[0]).normalized;report.rows.Add(new Row{mount=key,age=age,center=center,normal=normal,min=new Vector3(points.Min(p=>p.x),points.Min(p=>p.y),points.Min(p=>p.z)),max=new Vector3(points.Max(p=>p.x),points.Max(p=>p.y),points.Max(p=>p.z)),vertices=points.Length});}
                    UnityEngine.Object.DestroyImmediate(mesh);
                    foreach(string view in new[]{"top","side","table"}){
                        camera.transform.position=view=="top"?new Vector3(0,.8f,0):view=="side"?(canonical?new Vector3(0,.06f,-.8f):new Vector3(.8f,.06f,0)):new Vector3(0,.55f,-.75f);
                        camera.transform.rotation=Quaternion.LookRotation(new Vector3(0,.035f,0)-camera.transform.position,view=="top"?(canonical?Vector3.right:Vector3.forward):Vector3.up);
                        camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(640,480,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,640,480),0,0);image.Apply();File.WriteAllBytes(output+"/"+key+"-"+view+"-"+age.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".jpg",image.EncodeToJPG(88));UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=old;
                    }
                }
                UnityEngine.Object.DestroyImmediate(root);
            }
            File.WriteAllText(output+"/blade-geometry.json",JsonUtility.ToJson(report,true));Debug.Log("SCYTHE R13 DIRECTION STUDY COMPLETE "+report.rows.Count);EditorApplication.Exit(0);
        }
    }
}
