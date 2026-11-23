using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi.Editor {
 public static class R11BundleValidation {
  [Serializable] class Report {public int checks,packs,sticks,directions,sourceQueues;public bool androidDeviceTested;}
  [Serializable] class SourceBank {public SourceRow[] rows;}
  [Serializable] class SourceRow {public string path;public SourceMaterial[] materials;}
  [Serializable] class SourceMaterial {public int customQueue;}
  public static void Run(){var report=new Report();void Check(bool value,string reason){if(!value)throw new Exception(reason);report.checks++;}
   try{
    string repo=Environment.GetEnvironmentVariable("YUNJIAN_R10_REPO"),output=repo+"/build/visual-feedback-r11/bundle-validation";Directory.CreateDirectory(output);
    CharacterRoster.BeginPreview();WardrobePackService.RootOverride=output+"/installed-"+Guid.NewGuid().ToString("N");WardrobePackService.RespectPreviewAvailability=true;WardrobePackService.ResetForReview();
    var items=WardrobeCatalog.Items("stick").Where(i=>i.HasModel).ToArray();var keys=items.Select(i=>WardrobePackService.ForItem(i.id).key).Concat(new[]{WardrobePackService.ForItem("ron_26fll").key}).SelectMany(WardrobePackService.Closure).Distinct().ToArray();
    var roots=new[]{repo+"/build/wardrobe-r22/packs/windows",repo+"/build/wardrobe-r30/packs/windows",repo+"/build/rank-stick-r10/native-export/packs/windows"};
    foreach(string key in keys){var p=WardrobePackService.Find(key);if(p.builtin)continue;string source=roots.Select(r=>r+"/"+p.file).FirstOrDefault(File.Exists);Check(source!=null,"Existing published cache "+p.file);Check(new FileInfo(source).Length==p.bytes&&CharacterPackService.HashFile(source)==p.sha256,"Published byte contract "+key);string destination=WardrobePackService.FilePath(p);Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(source,destination);File.WriteAllText(destination+".ok",p.sha256);report.packs++;}
    foreach(var item in items){
     Check(WardrobePackService.Installed(item.id),"Dependency closure "+item.id);
     for(int seat=0;seat<4;seat++){
      var go=new GameObject(item.id+" "+seat);go.transform.rotation=Quaternion.Euler(0,seat*90,0);var model=go.AddComponent<WardrobeModelVisual>();model.Initialize(item,.74f);model.Sample(.5f);
      var visible=go.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&!WardrobeModelVisual.IsShadow(r)).ToArray();Check(visible.Length>0,"Body geometry "+item.id+" "+seat);
      foreach(var r in visible)foreach(var m in r.sharedMaterials){Check(m!=null&&m.shader.isSupported&&m.mainTexture!=null,"Real model material "+item.id+" "+r.name);if(m.shader.name=="YunJian/StaticDecoration")Check(m.shader==Resources.Load<Shader>("Majsoul/Wardrobe/StaticDecoration"),"Current app shader "+item.id);}
      if(item.id=="liqi_23wansheng"){var body=visible.First(r=>r.name=="maomaoxingchenbang");Check(Mathf.Max(body.bounds.size.x,body.bounds.size.z)>.6f,"Full wand body size "+seat);Check(go.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="yinzi").All(r=>!r.gameObject.activeInHierarchy),"Shadow excluded "+seat);}
      UnityEngine.Object.DestroyImmediate(go);report.directions++;
     }
     report.sticks++;
    }
    var effectObject=new GameObject("Real bundle flower effect");var effect=effectObject.AddComponent<NativeParticleEffect>();effect.Initialize("NovR10/ron_26fll",71,false,false,new NativeWinContract{newStyle=true,sourceKey="ron_26fll",lifetime=6});effect.Sample(2.5f);
    var root=(Transform)typeof(NativeParticleEffect).GetField("authoredRoot",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(effect);
    var sourceBank=JsonUtility.FromJson<SourceBank>(File.ReadAllText(repo+"/docs/reference/november-2026/flower-render-order-r11.json"));
    foreach(var r in effectObject.GetComponentsInChildren<ParticleSystemRenderer>(true)){
     var parts=new List<string>();var t=r.transform;while(t!=null&&t!=root){parts.Insert(0,t.name);t=t.parent;}var row=sourceBank.rows.Single(s=>s.path==root.name+"/"+string.Join("/",parts));
     Check(r.sharedMaterials.Select((m,i)=>m.renderQueue==(row.materials[i].customQueue>=0?row.materials[i].customQueue:3000)).All(v=>v),"Real flower material queue "+row.path);report.sourceQueues++;
    }
    Check(report.sourceQueues==54,"All original flower layers");UnityEngine.Object.DestroyImmediate(effectObject);
    File.WriteAllText(output+"/checks.json",JsonUtility.ToJson(report,true));Debug.Log("R11 REAL BUNDLES PASS "+report.checks);EditorApplication.Exit(0);
   }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}finally{WardrobePackService.ResetForReview();CharacterRoster.EndPreview();}
  }
 }
}
