using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace YunJian.Riichi {
    [Serializable] public sealed class WardrobePackRecord {
        public string key,name,category,file,sha256,url;public int version;public long bytes;public bool builtin,available,hidden;
        public string[] items=new string[0],assets=new string[0],dependencies=new string[0];
    }
    [Serializable] public sealed class WardrobePackAddress {public string path,pack,source;}
    [Serializable] public sealed class WardrobePackItem {public string id,pack,category;}
    [Serializable] public sealed class WardrobePackDefault {public string category,id;}
    [Serializable] public sealed class WardrobePackCatalog {
        public int schema=1,protocol=1;public string platform;public WardrobePackRecord[] packs=new WardrobePackRecord[0];
        public WardrobePackAddress[] addresses=new WardrobePackAddress[0];public WardrobePackItem[] items=new WardrobePackItem[0];public WardrobePackDefault[] defaults=new WardrobePackDefault[0];
    }
    // The same receipt-last, resumable installation contract as character packs.
    // Heavy optional art is addressed through this service instead of Resources.
    public sealed class WardrobePackService:MonoBehaviour {
        public static WardrobePackService Instance {get;private set;}
        public static string RootOverride;public static bool RespectPreviewAvailability;public static int Revision {get;private set;}
        public static event Action Changed;
        public static readonly HashSet<string> RemoteFallbackIds=new HashSet<string>();
        static WardrobePackCatalog catalog;static readonly Dictionary<string,WardrobePackAddress> addresses=new Dictionary<string,WardrobePackAddress>(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<string,WardrobePackRecord> packIndex=new Dictionary<string,WardrobePackRecord>();static readonly Dictionary<string,WardrobePackItem> itemIndex=new Dictionary<string,WardrobePackItem>();static readonly Dictionary<string,string[]> closures=new Dictionary<string,string[]>();
        static readonly Dictionary<string,AssetBundle> bundles=new Dictionary<string,AssetBundle>();static readonly Dictionary<string,UnityEngine.Object> loaded=new Dictionary<string,UnityEngine.Object>(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<string,AssetBundleCreateRequest> bundleLoads=new Dictionary<string,AssetBundleCreateRequest>();
        static readonly Dictionary<string,(bool value,float until)> installedMemo=new Dictionary<string,(bool,float)>();static string memoRoot;
        static readonly Dictionary<string,int> touched=new Dictionary<string,int>(),pins=new Dictionary<string,int>();static readonly Dictionary<string,long> memory=new Dictionary<string,long>();
        static readonly HashSet<string> verified=new HashSet<string>();static readonly object pinGate=new object();
        const long CacheBudget=160L*1024*1024;const int BundleLimit=24;
        readonly Queue<string> queue=new Queue<string>();readonly HashSet<string> queued=new HashSet<string>();readonly Dictionary<string,string> sources=new Dictionary<string,string>();
        public readonly Dictionary<string,string> Results=new Dictionary<string,string>();public string DownloadKey,Stage="",Error="";public float Progress;public bool Busy=>DownloadKey!=null;
        public Func<string,string> OverrideUrl;bool cancel,processing;UnityWebRequest request;
        public static string Platform=>Application.platform==RuntimePlatform.Android?"android":"windows";
        public static string Root=>RootOverride??Path.Combine(Application.persistentDataPath,"wardrobe-packs-v1");
        public static WardrobePackCatalog Directory {get{Ensure();return catalog;}}
        public static WardrobePackRecord[] All=>Directory.packs;
        public static void Ensure(){
            if(catalog!=null)return;var data=Resources.Load<TextAsset>("Majsoul/WardrobeDirectory/packs-"+Platform);
            catalog=data==null?new WardrobePackCatalog{platform=Platform}:JsonUtility.FromJson<WardrobePackCatalog>(data.text);
            if(catalog.schema!=1||catalog.protocol!=1||catalog.platform!=Platform)throw new InvalidDataException("装扮资源目录不兼容");
            addresses.Clear();foreach(var row in catalog.addresses){if(addresses.ContainsKey(row.path))throw new InvalidDataException("装扮资源地址重复");addresses[row.path]=row;}
            packIndex.Clear();itemIndex.Clear();closures.Clear();foreach(var p in catalog.packs){ValidateKey(p.key);if(packIndex.ContainsKey(p.key))throw new InvalidDataException("装扮资源标识重复");packIndex[p.key]=p;if(!p.builtin&&p.available&&(!Hash(p.sha256)||p.bytes<=0))throw new InvalidDataException("装扮资源指纹无效");}foreach(var item in catalog.items){if(itemIndex.ContainsKey(item.id)||!packIndex.ContainsKey(item.pack))throw new InvalidDataException("装扮资源映射无效");itemIndex[item.id]=item;}
        }
        static void ValidateKey(string key){if(!System.Text.RegularExpressions.Regex.IsMatch(key??"","^deco-[a-z0-9_-]+$"))throw new InvalidDataException("装扮资源标识无效");}
        static bool Hash(string value)=>System.Text.RegularExpressions.Regex.IsMatch(value??"","^[a-f0-9]{64}$");
        public static WardrobePackRecord Find(string key){Ensure();return key!=null&&packIndex.TryGetValue(key,out var p)?p:null;}
        public static WardrobePackRecord ForItem(string id){Ensure();return id!=null&&itemIndex.TryGetValue(id,out var row)?Find(row.pack):null;}
        public static WardrobeItem Fallback(string category){var id=Directory.defaults.FirstOrDefault(x=>x.category==category)?.id;return WardrobeCatalog.Find(id)??WardrobeCatalog.Items(category).FirstOrDefault(x=>ForItem(x.id)?.builtin!=false);}
        public static bool Managed(string path){Ensure();return addresses.ContainsKey(path);}
        public static WardrobePackRecord ForAddress(string path){Ensure();return path!=null&&addresses.TryGetValue(path,out var row)?Find(row.pack):null;}
        public static bool AssetAvailable(string path){var p=ForAddress(path);return p==null||InstalledPack(p.key);}
        public static void Touch(string path){var p=ForAddress(path);if(p!=null&&bundles.ContainsKey(p.key))touched[p.key]=Time.frameCount;}
        public static string FilePath(WardrobePackRecord p){ValidateKey(p.key);if(!Hash(p.sha256))throw new InvalidDataException("装扮资源指纹无效");return Path.Combine(Root,p.key,p.sha256+".bundle");}
        public static string HashFile(string path){using(var s=File.OpenRead(path))using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(s)).Replace("-","").ToLowerInvariant();}
        static long FreeBytes(){
            try{
#if UNITY_ANDROID && !UNITY_EDITOR
                using(var stat=new AndroidJavaObject("android.os.StatFs",Root))return stat.Call<long>("getAvailableBytes");
#else
                return new DriveInfo(Path.GetPathRoot(Root)).AvailableFreeSpace;
#endif
            }catch(Exception){return long.MaxValue;}
        }
        public static string[] Closure(string key){Ensure();if(closures.TryGetValue(key,out var cached))return cached;var result=new List<string>();var visiting=new HashSet<string>();var done=new HashSet<string>();Action<string> visit=null;visit=k=>{if(done.Contains(k))return;if(!visiting.Add(k))throw new InvalidDataException("装扮依赖循环");var p=Find(k)??throw new InvalidDataException("缺少装扮依赖");foreach(string d in p.dependencies)visit(d);visiting.Remove(k);done.Add(k);result.Add(k);};visit(key);return closures[key]=result.ToArray();}
        static bool Preview=>CharacterRoster.PreviewOnly&&!RespectPreviewAvailability;
        public static bool RawInstalled(WardrobePackRecord p){
            if(p==null)return false;if(p.builtin)return true;if(Preview)return p.available;if(!p.available||!Hash(p.sha256))return false;
            if(memoRoot!=Root){installedMemo.Clear();memoRoot=Root;}if(Application.isPlaying&&installedMemo.TryGetValue(p.key,out var memo)&&memo.until>Time.realtimeSinceStartup)return memo.value;
            bool value;try{string file=FilePath(p);value=File.Exists(file)&&new FileInfo(file).Length==p.bytes&&File.Exists(file+".ok")&&File.ReadAllText(file+".ok")==p.sha256;}catch(IOException){value=false;}
            if(Application.isPlaying)installedMemo[p.key]=(value,Time.realtimeSinceStartup+1);return value;
        }
        public static bool InstalledPack(string key){var p=Find(key);return p!=null&&Closure(key).All(k=>RawInstalled(Find(k)));}
        public static bool Installed(string id){var p=ForItem(id);return p==null?WardrobeCatalog.Find(id)!=null:InstalledPack(p.key);}
        public static bool Available(string id){var p=ForItem(id);return p==null?WardrobeCatalog.Find(id)!=null:Closure(p.key).All(k=>Find(k).builtin||Find(k).available);}
        public static bool Builtin(string id)=>ForItem(id)?.builtin??true;
        public static long DownloadBytes(IEnumerable<string> ids)=>ids.Select(ForItem).Where(p=>p!=null).SelectMany(p=>Closure(p.key)).Distinct().Select(Find).Where(p=>!p.builtin&&!RawInstalled(p)).Sum(p=>p.bytes);
        public static bool Pinned(string key){lock(pinGate)return pins.TryGetValue(key,out int n)&&n>0;}
        public static void Pin(string key){lock(pinGate)pins[key]=pins.TryGetValue(key,out int n)?n+1:1;}
        public static void Unpin(string key){lock(pinGate)if(pins.TryGetValue(key,out int n))pins[key]=Math.Max(0,n-1);}
        public static string[] PinItem(string id){var p=ForItem(id);var keys=p==null?new string[0]:Closure(p.key).Where(k=>!Find(k).builtin).ToArray();foreach(string key in keys)Pin(key);return keys;}
        public static void Lease(string path,GameObject owner){Ensure();if(!addresses.TryGetValue(path,out var row)||Find(row.pack)?.builtin!=false)return;var lease=owner.GetComponent<WardrobePackLease>();if(lease==null)lease=owner.AddComponent<WardrobePackLease>();lease.Add(Closure(row.pack));}
        readonly Dictionary<int,string[]> timedPins=new Dictionary<int,string[]>();int holdSerial;
        public static void HoldAsset(string path,float seconds){var p=ForAddress(path);if(p==null||p.builtin||Instance==null)return;Instance.StartCoroutine(Instance.Hold(Closure(p.key).Where(k=>!Find(k).builtin).ToArray(),seconds));}
        IEnumerator Hold(string[] keys,float seconds){int id=++holdSerial;timedPins[id]=keys;foreach(string key in keys)Pin(key);yield return new WaitForSecondsRealtime(Mathf.Max(.1f,seconds));if(timedPins.Remove(id))foreach(string key in keys)Unpin(key);}
        static void Release(string key){
            // A cached prefab retains pointers into its dependency bundles.
            // Evict those dependents too, so reloading never returns a prefab
            // whose textures were destroyed during an earlier cache trim.
            foreach(string parent in bundles.Keys.Where(k=>k!=key&&Closure(k).Contains(key)).ToArray())Release(parent);
            if(bundles.TryGetValue(key,out var b)&&b!=null)b.Unload(true);bundles.Remove(key);touched.Remove(key);memory.Remove(key);foreach(string path in loaded.Keys.Where(p=>addresses.TryGetValue(p,out var row)&&row.pack==key).ToArray())loaded.Remove(path);
        }
        static void Trim(){
            var local=new HashSet<string>(WardrobeCatalog.Data.items.Select(i=>i.category).Distinct().Select(c=>ForItem(WardrobeSelection.Get(c)?.id)?.key).Where(k=>k!=null).SelectMany(Closure));
            while(bundles.Count>BundleLimit||memory.Values.Sum()>CacheBudget){string key=touched.Where(x=>x.Value<Time.frameCount&&!Pinned(x.Key)&&!local.Contains(x.Key)).OrderBy(x=>x.Value).Select(x=>x.Key).FirstOrDefault();if(key==null)break;Release(key);}
        }
        static void Track(string key,UnityEngine.Object asset){
            var set=new HashSet<UnityEngine.Object>();Action<UnityEngine.Object> add=o=>{if(o!=null)set.Add(o);};add(asset);
            if(asset is GameObject go)foreach(var r in go.GetComponentsInChildren<Renderer>(true)){foreach(var m in r.sharedMaterials){if(m==null)continue;add(m);foreach(string name in m.GetTexturePropertyNames())add(m.GetTexture(name));}if(r is SkinnedMeshRenderer skin)add(skin.sharedMesh);else{var filter=r.GetComponent<MeshFilter>();if(filter!=null)add(filter.sharedMesh);}}
            long bytes=set.Sum(o=>Math.Max(0,UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(o)));memory[key]=(memory.TryGetValue(key,out long before)?before:0)+bytes;
        }
        static bool ValidContent(WardrobePackRecord p,AssetBundle bundle)=>bundle!=null&&p.assets.Length>0&&p.assets.All(a=>bundle.Contains(a.ToLowerInvariant()));
        static AssetBundle Load(string key){
            if(bundles.TryGetValue(key,out var existing)&&existing!=null){touched[key]=Time.frameCount;return existing;}
            var p=Find(key);if(!RawInstalled(p))return null;foreach(string d in p.dependencies)if(!Find(d).builtin&&Load(d)==null)return null;
            var b=bundleLoads.TryGetValue(key,out var loading)?loading.assetBundle:AssetBundle.LoadFromFile(FilePath(p));bundleLoads.Remove(key);if(!ValidContent(p,b)){b?.Unload(true);File.Delete(FilePath(p)+".ok");installedMemo.Clear();return null;}bundles[key]=b;touched[key]=Time.frameCount;return b;
        }
        public static T Asset<T>(string path)where T:UnityEngine.Object {
            Ensure();if(!addresses.TryGetValue(path,out var row)||Find(row.pack).builtin)return Resources.Load<T>(path);
#if UNITY_EDITOR
            if(Preview)return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(row.source);
#endif
            if(!InstalledPack(row.pack))return null;
            if(loaded.TryGetValue(path,out var cached)&&cached!=null){touched[row.pack]=Time.frameCount;return cached as T;}
            var b=Load(row.pack);if(b==null)return null;var asset=b.LoadAsset<T>(path.ToLowerInvariant());if(asset!=null){loaded[path]=asset;Track(row.pack,asset);}return asset;
        }
        public static IEnumerator Preload(IEnumerable<string> ids){
            var keys=ids.Select(ForItem).Where(p=>p!=null).SelectMany(p=>Closure(p.key)).Distinct().Where(k=>!Find(k).builtin).ToArray();foreach(string key in keys)Pin(key);
            // Dependencies remain pinned until the entire graph is loaded. A
            // large texture pack must not be evicted between it and its prefab.
            try{foreach(string key in keys){
                var p=Find(key);if(!RawInstalled(p)||bundles.ContainsKey(key))continue;
                if(!bundleLoads.TryGetValue(key,out var op))bundleLoads[key]=op=AssetBundle.LoadFromFileAsync(FilePath(p));yield return op;bundleLoads.Remove(key);if(!ValidContent(p,op.assetBundle)){op.assetBundle?.Unload(true);File.Delete(FilePath(p)+".ok");installedMemo.Clear();continue;}bundles[key]=op.assetBundle;touched[key]=Time.frameCount;
                foreach(string address in p.assets){var load=op.assetBundle.LoadAssetAsync(address.ToLowerInvariant());yield return load;if(load.asset!=null){loaded[address]=load.asset;Track(key,load.asset);}}yield return null;
            }}finally{foreach(string key in keys)Unpin(key);}
        }
        static void Notify(){installedMemo.Clear();Revision++;WardrobeSelection.ReconcileAvailability();Changed?.Invoke();}
        public bool Queued(string key)=>queued.Contains(key);
        public bool ItemQueued(string id){var p=ForItem(id);return p!=null&&Closure(p.key).Any(k=>k==DownloadKey||queued.Contains(k));}
        public void DownloadItem(string id,string source=null){var p=ForItem(id);if(p!=null)Download(p.key,source);}
        public void Download(string key,string source=null){var p=Find(key);if(p==null||p.builtin||!p.available)return;if(source!=null)sources[key]=source;foreach(string k in Closure(key)){var d=Find(k);if(d.builtin||RawInstalled(d)||!d.available||!queued.Add(k))continue;queue.Enqueue(k);}if(!processing&&queue.Count>0)StartCoroutine(Process());}
        public void DownloadCategory(string category){foreach(var row in Directory.items.Where(x=>category==null||x.category==category))DownloadItem(row.id);}
        public void Cancel(){queue.Clear();queued.Clear();sources.Clear();cancel=true;request?.Abort();}
        IEnumerator Process(){processing=true;while(queue.Count>0){string key=queue.Dequeue();cancel=false;Error="";DownloadKey=key;Progress=0;Stage="准备下载";yield return Guard(DownloadOne(Find(key)));Results[key]=cancel?"已取消，可继续":string.IsNullOrEmpty(Error)?"已安装":"失败，可重试";queued.Remove(key);DownloadKey=null;}processing=false;}
        IEnumerator Guard(IEnumerator task){var work=new Stack<IEnumerator>();work.Push(task);while(work.Count>0){object next=null;bool failed=false;try{var top=work.Peek();if(!top.MoveNext()){work.Pop();continue;}next=top.Current;if(next is IEnumerator child){work.Push(child);continue;}}catch(Exception e){Error="装扮资源操作失败："+e.Message;Stage="失败，可重试";request?.Abort();request?.Dispose();request=null;failed=true;}if(failed)yield break;yield return next;}}
        IEnumerator DownloadOne(WardrobePackRecord p){
            string final=FilePath(p),part=final+".part",url=sources.TryGetValue(p.key,out var source)?source:OverrideUrl?.Invoke(p.key)??p.url;sources.Remove(p.key);System.IO.Directory.CreateDirectory(Path.GetDirectoryName(final));
            if(RawInstalled(p)){Stage="已安装";Progress=1;yield break;}
            if(p.dependencies.Any(d=>!RawInstalled(Find(d)))){Error="依赖资源未安装，请重试下载";yield break;}
            if(string.IsNullOrEmpty(url)){Error="下载地址不可用";yield break;}
            for(int attempt=0;attempt<2;attempt++){
                if(!File.Exists(part)&&File.Exists(final)&&!File.Exists(final+".ok"))File.Move(final,part);
                long start=File.Exists(part)?new FileInfo(part).Length:0;if(start>p.bytes){File.Delete(part);start=0;}
                if(FreeBytes()<p.bytes-start+1048576){Error="存储空间不足，请清理装扮资源";yield break;}
                if(start<p.bytes){request=UnityWebRequest.Get(url);request.downloadHandler=new DownloadHandlerFile(part,start>0){removeFileOnAbort=false};request.timeout=300;request.redirectLimit=10;if(start>0)request.SetRequestHeader("Range","bytes="+start+"-");Stage="下载中";var op=request.SendWebRequest();while(!op.isDone){Progress=Mathf.Clamp01((start+(long)request.downloadedBytes)/(float)p.bytes);if(cancel)request.Abort();yield return null;}bool ok=request.result==UnityWebRequest.Result.Success;long code=request.responseCode;string error=request.error;request.Dispose();request=null;if(cancel)yield break;if(!ok){Error="下载失败："+error;yield break;}if(start>0&&code==200){File.Delete(part);continue;}}
                if(!File.Exists(part)||new FileInfo(part).Length!=p.bytes){Error="资源大小不符，请重试";yield break;}Stage="校验资源";Progress=1;var hash=Task.Run(()=>HashFile(part));while(!hash.IsCompleted)yield return null;if(cancel)yield break;if(hash.IsFaulted||hash.Result!=p.sha256){File.Delete(part);Error="资源校验失败，未安装";yield break;}
                if(Pinned(p.key)){Error="装扮正在使用，请稍后安装";yield break;}Release(p.key);
                Stage="验证资源包";var load=AssetBundle.LoadFromFileAsync(part);yield return load;if(!ValidContent(p,load.assetBundle)){load.assetBundle?.Unload(true);File.Delete(part);Error="资源包与当前客户端不兼容";yield break;}load.assetBundle.Unload(true);if(cancel)yield break;
                if(File.Exists(final))File.Delete(final);File.Move(part,final);File.WriteAllText(final+".ok.tmp",p.sha256);if(File.Exists(final+".ok"))File.Replace(final+".ok.tmp",final+".ok",null);else File.Move(final+".ok.tmp",final+".ok");verified.Add(p.key);Notify();Stage="已安装";yield break;
            }
            Error="下载源不支持续传，请重试";
        }
        public static bool DeleteItem(string id,out string reason){var p=ForItem(id);if(p==null){reason="基础装扮不可删除";return false;}return DeletePack(p.key,out reason);}
        public static bool DeletePack(string key,out string reason){
            reason="";var p=Find(key);if(p==null||p.builtin){reason="基础装扮不可删除";return false;}if(Pinned(key)){reason="装扮正在对局、预览或传输中使用";return false;}if(Instance!=null&&(Instance.DownloadKey==key||Instance.queued.Contains(key))){reason="请先取消该装扮的下载";return false;}
            if(All.Any(x=>x.key!=key&&!x.builtin&&RawInstalled(x)&&x.dependencies.Contains(key))){reason="其他已安装装扮仍依赖此资源";return false;}
            Release(key);verified.Remove(key);string dir=Path.GetDirectoryName(FilePath(p));if(!Path.GetFullPath(dir).StartsWith(Path.GetFullPath(Root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("装扮资源路径越界");if(System.IO.Directory.Exists(dir))System.IO.Directory.Delete(dir,true);Instance?.Results.Remove(key);Notify();return true;
        }
        public static void DeleteAll(){foreach(var p in All.Where(x=>!x.builtin&&!x.hidden))DeletePack(p.key,out _);bool removed;do{removed=false;foreach(var p in All.Where(x=>!x.builtin&&x.hidden&&RawInstalled(x)))if(DeletePack(p.key,out _))removed=true;}while(removed);}
        void Awake(){if(Instance!=null&&Instance!=this){Destroy(this);return;}Instance=this;Ensure();System.IO.Directory.CreateDirectory(Root);StartCoroutine(ValidateInstalled());}
        IEnumerator ValidateInstalled(){foreach(var p in All.Where(x=>!x.builtin&&RawInstalled(x)).ToArray()){var hash=Task.Run(()=>HashFile(FilePath(p)));while(!hash.IsCompleted)yield return null;if(hash.IsFaulted||hash.Result!=p.sha256){if(!Pinned(p.key))Release(p.key);File.Delete(FilePath(p)+".ok");Notify();}else verified.Add(p.key);yield return null;}}
        void LateUpdate(){Trim();}
        void OnDestroy(){request?.Abort();request?.Dispose();foreach(var keys in timedPins.Values)foreach(string key in keys)Unpin(key);timedPins.Clear();if(Instance==this)Instance=null;}
#if UNITY_EDITOR
        public static void ResetForReview(){foreach(string key in bundles.Keys.ToArray())Release(key);loaded.Clear();bundleLoads.Clear();installedMemo.Clear();verified.Clear();addresses.Clear();packIndex.Clear();itemIndex.Clear();closures.Clear();pins.Clear();catalog=null;RemoteFallbackIds.Clear();Revision++;}
        public static int ReviewLoadedBundles=>bundles.Count;
#endif
    }
    public sealed class WardrobePackLease:MonoBehaviour {
        readonly HashSet<string> keys=new HashSet<string>();
        public void Add(IEnumerable<string> values){foreach(string key in values)if(!WardrobePackService.Find(key).builtin&&keys.Add(key))WardrobePackService.Pin(key);}
        void OnDestroy(){foreach(string key in keys)WardrobePackService.Unpin(key);}
    }
}
