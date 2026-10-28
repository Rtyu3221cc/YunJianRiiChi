using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace YunJian.Riichi.Editor {
    public static class NativeR11AnimationStudy {
        [Serializable] public class Row {public string bundle,name,output,effect,node;}
        [Serializable] class Rows {public Row[] rows;}
        [Serializable] class Source {public Node[] nodes;}
        [Serializable] class Node {public string name;public int parent;public float[] position,rotation,scale;public string[] animations;}
        static readonly Dictionary<string,AssetBundle> bundles=new Dictionary<string,AssetBundle>();
        public static void Run(){
            AssetDatabase.Refresh();
            string root=Path.GetFullPath("../dist/previews/2026-10-27-effects-reference/source"),folder="Assets/Resources/Majsoul/NativeReview/";
            var rows=JsonUtility.FromJson<Rows>("{\"rows\":"+File.ReadAllText(root+"/animation-sources.json")+"}").rows;
            var report=new List<string>();
            foreach(var row in rows.GroupBy(r=>r.output).Select(g=>g.First())){
                if(!bundles.TryGetValue(row.bundle,out var bundle)){
                    bundle=AssetBundle.LoadFromFile(root+"/bundles/"+row.bundle);
                    if(bundle==null)throw new Exception("Cannot load original animation bundle "+row.bundle);
                    bundles[row.bundle]=bundle;
                }
                var original=bundle.LoadAllAssets<AnimationClip>().FirstOrDefault(c=>c.name==row.name);
                if(original==null)throw new Exception("Missing original animation "+row.name);
                var clip=original;
                string output=folder+row.output;
                var source=JsonUtility.FromJson<Source>(File.ReadAllText(folder+"EffectsR11/"+row.effect+"-source.json"));
                var rootObject=new GameObject("Native clip proof");var nodes=new Transform[source.nodes.Length];
                for(int i=0;i<nodes.Length;i++){
                    var n=source.nodes[i];var t=new GameObject(n.name).transform;t.SetParent(n.parent<0?rootObject.transform:nodes[n.parent],false);
                    t.localPosition=new Vector3(n.position[0],n.position[1],n.position[2]);t.localRotation=new Quaternion(n.rotation[0],n.rotation[1],n.rotation[2],n.rotation[3]);t.localScale=new Vector3(n.scale[0],n.scale[1],n.scale[2]);nodes[i]=t;
                }
                int index=Array.FindIndex(source.nodes,n=>n.name==row.node&&n.animations!=null);
                var before=nodes.Select(t=>t.localToWorldMatrix).ToArray();
                var animator=nodes[index].gameObject.AddComponent<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                var graph=PlayableGraph.Create("Original compiled animation sampling");var playable=AnimationClipPlayable.Create(graph,clip);
                var animationOutput=AnimationPlayableOutput.Create(graph,"Reference",animator);animationOutput.SetSourcePlayable(playable);
                graph.Play();playable.SetTime(Mathf.Min(clip.length*.45f,.7f));graph.Evaluate(0);
                int changed=nodes.Where((t,i)=>t.localToWorldMatrix!=before[i]).Count();
                report.Add(row.effect+" / "+row.name+" length="+clip.length+" changedTransforms="+changed+" bindings="+AnimationUtility.GetCurveBindings(clip).Length);
                if(clip.length<=0)throw new Exception("Imported original clip has no duration "+row.name);
                graph.Destroy();
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
            foreach(var b in bundles.Values)b.Unload(false);bundles.Clear();
            File.WriteAllLines(root+"/animation-import-study.txt",report);Debug.Log("NATIVE R11 ANIMATION STUDY PASS "+report.Count);EditorApplication.Exit(0);
        }
    }
}
