using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace YunJian.Riichi.Editor {
 public static class BuildNovemberCharacters {
  [Serializable] class Plan {public string output;public Row[] packs;}
  [Serializable] class Row {public string key;public int version;public bool build;}
  public static void Run(){
   try{
    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    string repo=Environment.GetEnvironmentVariable("YUNJIAN_R10_REPO");
    var plan=JsonUtility.FromJson<Plan>(File.ReadAllText("Assets/Resources/Majsoul/CharacterDirectory/build-plan.json"));
    foreach(string platform in new[]{"windows","android"}){
     var definitions=plan.packs.Where(p=>p.build).Select(p=>{
      string root="Assets/CharacterPacks/Source/"+p.key+"/";
      var files=Directory.GetFiles(root,"*",SearchOption.AllDirectories).Where(f=>new[]{".png",".mp3",".bytes",".txt",".json"}.Contains(Path.GetExtension(f))).Select(f=>f.Replace('\\','/')).OrderBy(f=>f,StringComparer.Ordinal).ToArray();
      var addresses=files.Select(f=>f.Substring(root.Length, f.Length-root.Length-Path.GetExtension(f).Length).ToLowerInvariant()).ToArray();
      if(files.Length==0||addresses.Distinct().Count()!=addresses.Length)throw new Exception("Missing or ambiguous character addresses: "+p.key);
      return new AssetBundleBuild{assetBundleName=p.key+"-v"+p.version+"-"+platform+".bundle",assetNames=files,addressableNames=addresses};
     }).ToArray();
     string output=Path.Combine(repo,plan.output,platform);Directory.CreateDirectory(output);
     var result=BuildPipeline.BuildAssetBundles(output,definitions,BuildAssetBundleOptions.StrictMode|BuildAssetBundleOptions.ChunkBasedCompression,platform=="android"?BuildTarget.Android:BuildTarget.StandaloneWindows64);
     if(result==null)throw new Exception("New character bundle build failed: "+platform);
     Debug.Log("NOVEMBER CHARACTER BUILD PASS "+platform+" packs="+definitions.Length);
    }
    EditorApplication.Exit(0);
   }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
  }
 }
}
