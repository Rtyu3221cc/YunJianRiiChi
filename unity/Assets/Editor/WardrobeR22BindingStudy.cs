using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi.Editor {
    // Compile a known public-API material binding to identify the packed index
    // convention before translating the original's sole pointer curve.
    public static class WardrobeR22BindingStudy {
        public static void Probe(){
            string repo=Environment.GetEnvironmentVariable("YUNJIAN_R22_REPO")??Path.GetFullPath(".."),output=repo+"/build/wardrobe-r22/binding-probe";if(File.Exists(output+"/probe.bundle"))return;
            string folder="Assets/WardrobePacks/ImportData/BindingProbe";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var first=new Material(Shader.Find("Unlit/Color")){name="first"};var second=new Material(Shader.Find("Unlit/Color")){name="second"};AssetDatabase.CreateAsset(first,folder+"/first.mat");AssetDatabase.CreateAsset(second,folder+"/second.mat");
            var clip=new AnimationClip{name="known-material-slot-zero"};
            AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("node",typeof(ParticleSystemRenderer),"m_Materials.Array.data[0]"),new[]{new ObjectReferenceKeyframe{time=0,value=first},new ObjectReferenceKeyframe{time=.2f,value=second}});
            AssetDatabase.CreateAsset(clip,folder+"/probe.anim");AssetDatabase.SaveAssets();
            Directory.CreateDirectory(output);
            if(BuildPipeline.BuildAssetBundles(output,new[]{new AssetBundleBuild{assetBundleName="probe.bundle",assetNames=new[]{folder+"/probe.anim"}}},BuildAssetBundleOptions.StrictMode|BuildAssetBundleOptions.ChunkBasedCompression,BuildTarget.StandaloneWindows64)==null)throw new Exception("Binding probe build failed");
            Debug.Log("WARDROBE BINDING PROBE PASS");
        }
    }
}
