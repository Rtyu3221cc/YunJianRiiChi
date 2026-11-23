using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi.Editor {
 public static class NovWardrobeValidation {
  [Serializable] class Report {public int checks,packs,assets,renderers;public bool androidDeviceTested;}
  public static void Run(){
   var report=new Report();void Check(bool ok,string value){if(!ok)throw new Exception(value);report.checks++;}
   try{
    string repo=Environment.GetEnvironmentVariable("YUNJIAN_R10_REPO");string output=repo+"/build/rank-stick-r10/wardrobe-validation";Directory.CreateDirectory(output);
    WardrobePackService.RootOverride=output+"/installed-"+Guid.NewGuid().ToString("N");WardrobePackService.RespectPreviewAvailability=true;WardrobePackService.ResetForReview();CharacterRoster.BeginPreview();
    var packs=WardrobePackService.All.Where(p=>p.key.StartsWith("deco-nov26-")).ToArray();Check(packs.Length==9,"All nine new wardrobe packs");
    foreach(var p in packs){string source=repo+"/build/rank-stick-r10/native-export/packs/windows/"+p.file;Check(File.Exists(source)&&new FileInfo(source).Length==p.bytes&&CharacterPackService.HashFile(source)==p.sha256,"Real bundle byte contract "+p.key);string target=WardrobePackService.FilePath(p);Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(source,target);File.WriteAllText(target+".ok",p.sha256);report.packs++;}
    foreach(var address in WardrobePackService.Directory.addresses.Where(a=>a.pack.StartsWith("deco-nov26-"))){
     var asset=WardrobePackService.Asset<UnityEngine.Object>(address.path);Check(asset!=null,"Bundle address resolves "+address.path);report.assets++;
     if(asset is GameObject prefab)foreach(var r in prefab.GetComponentsInChildren<Renderer>(true)){Check(r.sharedMaterials.All(m=>m!=null&&m.shader!=null&&m.shader.isSupported&&m.mainTexture!=null),"Serialized materials and textures "+address.path+"/"+r.name);report.renderers++;}
     if(asset is AudioClip clip)Check(clip.samples>0&&clip.channels>0,"New source audio decodes "+address.path);
    }
    foreach(var item in WardrobePackService.Directory.items.Where(i=>i.pack.StartsWith("deco-nov26-")))Check(WardrobePackService.Installed(item.id),"Full dependency closure installed "+item.id);
    File.WriteAllText(output+"/checks.json",JsonUtility.ToJson(report,true));Debug.Log("NOV WARDROBE VALIDATION PASS "+report.checks+" assets="+report.assets);EditorApplication.Exit(0);
   }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}finally{WardrobePackService.ResetForReview();CharacterRoster.EndPreview();}
  }
 }
}
