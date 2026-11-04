using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    public static class BuildWardrobePacks {
        [Serializable] public sealed class Item {public string id,name,category;public bool builtin,hidden;public string[] roots;}
        [Serializable] public sealed class Spec {public int schema;public Item[] items;public string[] core;public WardrobePackDefault[] defaults;}
        [Serializable] public sealed class Move {public string from,to;public bool runtime;}
        [Serializable] public sealed class Asset {public string path,address,pack;}
        [Serializable] public sealed class Plan {public int schema=1;public Spec spec;public Move[] moves;public Asset[] assets;public WardrobePackRecord[] packs;}
        static string Repo=>Environment.GetEnvironmentVariable("YUNJIAN_R22_REPO")??Path.GetFullPath("..");
        static string Root=>Repo+"/build/wardrobe-r22";
        static string PlanPath=>File.Exists(Root+"/pack-plan.json")?Root+"/pack-plan.json":Repo+"/docs/wardrobe-pack-plan-r22.json";
        static string Read(string path)=>File.ReadAllText(path,Encoding.UTF8);
        static void Save(string path,object value){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(value,true)+"\n",new UTF8Encoding(false));}
        static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        static string Address(string path){const string prefix="Assets/Resources/";if(!path.StartsWith(prefix))throw new Exception("Unexpected source "+path);string address=path.Substring(prefix.Length, path.Length-prefix.Length-Path.GetExtension(path).Length);return path.EndsWith(".asset")||path.EndsWith(".mat")&&File.Exists(Path.ChangeExtension(path,".asset"))?address+Path.GetExtension(path):address;}
        static bool RuntimeAsset(string p)=>p.StartsWith("Assets/Resources/")&&!p.EndsWith(".cs")&&!p.EndsWith(".shader");
        static string[] Dependencies(IEnumerable<string> roots)=>AssetDatabase.GetDependencies(roots.ToArray(),true).Where(RuntimeAsset).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        public static void PlanAndMove(){
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var spec=JsonUtility.FromJson<Spec>(Read(Root+"/pack-roots.json"));
            var core=new HashSet<string>(Dependencies(spec.core.Concat(spec.items.Where(x=>x.builtin).SelectMany(x=>x.roots))));
            // Layout and dora are shared gameplay resources, independent of dress-up.
            foreach(string p in AssetDatabase.FindAssets("",new[]{"Assets/Resources/Majsoul/NativeReview"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.Contains("/Sprites/")||p.Contains("/Textures/dora_")||new[]{"dora-geometry.json","north-anchors.json","score-layout.json","rank-animation.json"}.Contains(Path.GetFileName(p))))core.Add(p);
            var owners=new Dictionary<string,SortedSet<string>>();
            foreach(var item in spec.items.Where(x=>!x.builtin))foreach(string p in Dependencies(item.roots)){
                if(core.Contains(p))continue;if(!owners.TryGetValue(p,out var set))owners[p]=set=new SortedSet<string>(StringComparer.Ordinal);set.Add(item.id);
            }
            var packs=new List<WardrobePackRecord>();var assets=new List<Asset>();var moves=new List<Move>();
            foreach(var item in spec.items){
                string key="deco-"+item.id.ToLowerInvariant().Replace('_','-');
                packs.Add(new WardrobePackRecord{key=key,name=item.name,category=item.category,version=1,builtin=item.builtin,hidden=item.hidden,items=new[]{item.id}});
                if(item.builtin)continue;
                // Even aliases have an explicit root, with shared art in dependency packs.
                string marker="Assets/WardrobePacks/Markers/"+item.id+".json";Directory.CreateDirectory(Path.GetDirectoryName(marker));File.WriteAllText(marker,"{\"id\":\""+item.id+"\"}");
                assets.Add(new Asset{path=marker,address="Majsoul/WardrobeMarkers/"+item.id,pack=key});
            }
            foreach(var entry in owners){
                string key=entry.Value.Count==1?"deco-"+entry.Value.First().ToLowerInvariant().Replace('_','-'):"deco-shared-"+Hash(Encoding.UTF8.GetBytes(string.Join("\n",entry.Value))).Substring(0,16);
                if(!packs.Any(p=>p.key==key))packs.Add(new WardrobePackRecord{key=key,name="共享装扮资源",category="shared",version=1,hidden=true,items=entry.Value.ToArray()});
                string destination="Assets/WardrobePacks/Source/"+entry.Key.Substring("Assets/Resources/".Length);
                moves.Add(new Move{from=entry.Key,to=destination,runtime=true});assets.Add(new Asset{path=destination,address=Address(entry.Key),pack=key});
            }
            // Export-only mesh/particle/clip JSON must never enter the APK.
            foreach(string p in Directory.GetFiles("Assets/Resources/Majsoul/NativeReview","*",SearchOption.AllDirectories).Select(p=>p.Replace('\\','/')).Where(p=>!p.EndsWith(".meta")&& !p.EndsWith(".shader"))){
                if(owners.ContainsKey(p)||core.Contains(p))continue;
                bool raw=p.Contains("/Data/")||p.Contains("/Animations/")||p.Contains("/Materials/");
                bool optional=p.Contains("/WinsR22/")||p.Contains("/EffectsR11/")||p.Contains("/StaticR16/");
                if(raw||optional)moves.Add(new Move{from=p,to="Assets/WardrobePacks/ImportData/"+p.Substring("Assets/Resources/".Length)});
            }
            // Legacy geometry is superseded by the imported prefab.
            foreach(var item in spec.items){var original=WardrobeCatalog.Find(item.id);if(original==null||string.IsNullOrEmpty(original.modelPrefab)||string.IsNullOrEmpty(original.model))continue;string p="Assets/Resources/"+original.model+".json";if(File.Exists(p)&&!owners.ContainsKey(p)&&!core.Contains(p))moves.Add(new Move{from=p,to="Assets/WardrobePacks/ImportData/"+p.Substring("Assets/Resources/".Length)});}
            if(moves.Select(x=>x.from).Distinct().Count()!=moves.Count)throw new Exception("Duplicate resource move");
            var plan=new Plan{spec=spec,moves=moves.OrderBy(x=>x.from).ToArray(),assets=assets.OrderBy(x=>x.address).ToArray(),packs=packs.ToArray()};Save(Root+"/pack-plan.json",plan);
            foreach(string folder in plan.moves.Select(m=>Path.GetDirectoryName(m.to)).Distinct())Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            AssetDatabase.StartAssetEditing();try{foreach(var move in plan.moves){string error=AssetDatabase.MoveAsset(move.from,move.to);if(!string.IsNullOrEmpty(error))throw new Exception(error+": "+move.from);}}finally{AssetDatabase.StopAssetEditing();}
            AssetDatabase.SaveAssets();AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("WARDROBE PLAN PASS moves="+moves.Count+" packs="+packs.Count+" assets="+assets.Count);EditorApplication.Exit(0);
        }
        public static void Build(){BuildPlatform(Environment.GetCommandLineArgs().Contains("-packAndroid")?"android":"windows");EditorApplication.Exit(0);}
        public static void BuildBoth(){WardrobeR22BindingStudy.Probe();BuildPlatform("windows");BuildPlatform("android");EditorApplication.Exit(0);}
        public static void BuildWardrobeR30(){
            string work=Repo+"/build/wardrobe-r30";
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var spec=JsonUtility.FromJson<Spec>(Read(work+"/roots.json"));
            var core=new HashSet<string>(spec.core);
            var owners=new Dictionary<string,SortedSet<string>>();
            foreach(var item in spec.items)foreach(string path in Dependencies(item.roots)){
                if(!path.Contains("/WardrobeR30/")||core.Contains(path))continue;
                if(!owners.TryGetValue(path,out var list))owners[path]=list=new SortedSet<string>(StringComparer.Ordinal);
                list.Add(item.id);
            }
            string RootKey(string id)=>"deco-r30-"+id.Replace('_','-');
            var records=spec.items.Select(i=>new WardrobePackRecord{key=RootKey(i.id),items=new[]{i.id},name=i.name,category=i.category,version=1}).ToDictionary(p=>p.key);
            var addresses=new List<WardrobePackAddress>();var moves=new List<Move>();
            foreach(var row in owners.OrderBy(x=>x.Key,StringComparer.Ordinal)){
                string key=row.Value.Count==1?RootKey(row.Value.First()):"deco-r30-shared-"+Hash(Encoding.UTF8.GetBytes(string.Join("\n",row.Value))).Substring(0,16);
                if(!records.ContainsKey(key))records[key]=new WardrobePackRecord{key=key,name="共享装扮资源",category="shared",version=1,hidden=true,items=row.Value.ToArray()};
                string destination="Assets/WardrobePacks/Source/"+row.Key.Substring("Assets/Resources/".Length);
                // The Spine binary and atlas share a basename. Both are serialized
                // prefab references and do not need a public Resources address.
                string address=Address(row.Key);
                if(row.Key.Contains("/WardrobeR30/Spine/"))address+=Path.GetExtension(row.Key);
                addresses.Add(new WardrobePackAddress{path=address,pack=key,source=destination});
                moves.Add(new Move{from=row.Key,to=destination,runtime=true});
            }
            foreach(var item in spec.items){
                var key=RootKey(item.id);
                if(!addresses.Any(a=>a.pack==key))throw new Exception("No independently addressable resource for "+item.id);
            }
            foreach(string directory in moves.Select(m=>Path.GetDirectoryName(m.to)).Distinct())Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();AssetDatabase.StartAssetEditing();
            try{foreach(var move in moves){string error=AssetDatabase.MoveAsset(move.from,move.to);if(error.Length>0)throw new Exception(error);}}
            finally{AssetDatabase.StopAssetEditing();}
            AssetDatabase.SaveAssets();AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            File.WriteAllLines(work+"/runtime-sources.txt",addresses.Select(a=>a.source).Concat(core).Distinct());
            foreach(string platform in new[]{"windows","android"}){
                string output=work+"/packs/"+platform;Directory.CreateDirectory(output);
                var bundles=addresses.GroupBy(a=>a.pack).ToDictionary(g=>g.Key,g=>g.ToArray());
                var builds=bundles.Select(g=>new AssetBundleBuild{assetBundleName=g.Key+".bundle",assetNames=g.Value.Select(a=>a.source).ToArray(),addressableNames=g.Value.Select(a=>a.path.ToLowerInvariant()).ToArray()}).ToArray();
                var target=platform=="android"?BuildTarget.Android:BuildTarget.StandaloneWindows64;
                var manifest=BuildPipeline.BuildAssetBundles(output,builds,BuildAssetBundleOptions.StrictMode|BuildAssetBundleOptions.ChunkBasedCompression,target);
                if(manifest==null)throw new Exception("R30 resource build failed for "+platform);
                string directory="Assets/Resources/Majsoul/WardrobeDirectory/packs-"+platform+".json";
                var baseline=JsonUtility.FromJson<WardrobePackCatalog>(Read(directory));
                var additions=new List<WardrobePackRecord>();
                foreach(var group in bundles){
                    var p=JsonUtility.FromJson<WardrobePackRecord>(JsonUtility.ToJson(records[group.Key]));
                    string name=p.key+".bundle";byte[] data=File.ReadAllBytes(output+"/"+name);
                    p.sha256=Hash(data);p.bytes=data.LongLength;p.available=true;
                    p.file=p.key+"-v1-"+platform+"-"+p.sha256.Substring(0,12)+".bundle";
                    p.url="https://github.com/Rtyu3221cc/YunJianRiiChi/releases/download/wardrobe-resources-v2/"+p.file;
                    p.dependencies=manifest.GetDirectDependencies(name).Select(Path.GetFileNameWithoutExtension).OrderBy(k=>k,StringComparer.Ordinal).ToArray();
                    p.assets=group.Value.Where(a=>!a.source.Contains("/WardrobeR30/Spine/")).Select(a=>a.path).ToArray();
                    File.Copy(output+"/"+name,output+"/"+p.file,true);additions.Add(p);
                }
                baseline.packs=baseline.packs.Concat(additions).ToArray();
                baseline.addresses=baseline.addresses.Concat(addresses.Where(a=>!a.source.Contains("/WardrobeR30/Spine/"))).ToArray();
                baseline.items=baseline.items.Concat(spec.items.Select(i=>new WardrobePackItem{id=i.id,category=i.category,pack=RootKey(i.id)})).ToArray();
                Save(work+"/packs-"+platform+".json",baseline);
                Debug.Log("R30 PACKS "+platform+" "+additions.Count+" "+additions.Sum(p=>p.bytes)+" bytes");
            }
            Debug.Log("R30 PACKS PASS");EditorApplication.Exit(0);
        }
        static void BuildPlatform(string platform){
            var plan=JsonUtility.FromJson<Plan>(Read(PlanPath));AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var target=platform=="android"?BuildTarget.Android:BuildTarget.StandaloneWindows64;
            string output=Root+"/packs/"+platform;Directory.CreateDirectory(output);
            var names=plan.packs.Where(p=>!p.builtin).ToDictionary(p=>p.key,p=>p.key+"-v"+p.version+"-"+platform+".bundle");
            var definitions=names.Select(p=>{var rows=plan.assets.Where(a=>a.pack==p.Key).ToArray();return new AssetBundleBuild{assetBundleName=p.Value,assetNames=rows.Select(a=>a.path).ToArray(),addressableNames=rows.Select(a=>a.address.ToLowerInvariant()).ToArray()};}).ToArray();
            var manifest=BuildPipeline.BuildAssetBundles(output,definitions,BuildAssetBundleOptions.StrictMode|BuildAssetBundleOptions.ChunkBasedCompression,target);
            if(manifest==null)throw new Exception("装扮资源构建失败");
            var reverse=names.ToDictionary(p=>p.Value,p=>p.Key);var records=new List<WardrobePackRecord>();
            foreach(var source in plan.packs){var p=JsonUtility.FromJson<WardrobePackRecord>(JsonUtility.ToJson(source));if(!p.builtin){string filename=names[p.key];byte[] bytes=File.ReadAllBytes(output+"/"+filename);p.sha256=Hash(bytes);p.bytes=bytes.LongLength;p.file=p.key+"-v"+p.version+"-"+platform+"-"+p.sha256.Substring(0,12)+".bundle";File.Copy(output+"/"+filename,output+"/"+p.file,true);p.url="https://github.com/Rtyu3221cc/YunJianRiiChi/releases/download/wardrobe-resources-v1/"+p.file;p.available=true;p.dependencies=manifest.GetDirectDependencies(filename).Select(n=>reverse[n]).OrderBy(k=>k).ToArray();p.assets=plan.assets.Where(a=>a.pack==p.key).Select(a=>a.address).ToArray();}records.Add(p);}
            var catalog=new WardrobePackCatalog{platform=platform,packs=records.ToArray(),items=plan.spec.items.Select(i=>new WardrobePackItem{id=i.id,category=i.category,pack="deco-"+i.id.ToLowerInvariant().Replace('_','-')}).ToArray(),defaults=plan.spec.defaults,addresses=plan.assets.Select(a=>new WardrobePackAddress{path=a.address,pack=a.pack,source=a.path}).ToArray()};
            Save(Root+"/packs-"+platform+".json",catalog);Save("Assets/Resources/Majsoul/WardrobeDirectory/packs-"+platform+".json",catalog);
            Debug.Log("WARDROBE BUILD PASS "+platform+" packs="+definitions.Length+" bytes="+records.Sum(p=>p.bytes));
        }
    }
}
