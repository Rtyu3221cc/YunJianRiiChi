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
    [Serializable] public sealed class CharacterPackRecord {public string key,name,file,sha256,url,compression;public int id,version;public long bytes;public bool builtin,recommended,available;public int[] skins;}
    [Serializable] public sealed class CharacterPackCatalog {public int schema,protocol;public string platform;public CharacterPackRecord[] packs;}
    // Content identity is independent of the transport URL. Receipts are written last.
    public sealed class CharacterPackService:MonoBehaviour {
        public static CharacterPackService Instance {get;private set;}
        public static string RootOverride;
        public static bool RespectPreviewAvailability;
        public static int Revision {get;private set;}
        public static event Action Changed;
        static CharacterPackCatalog catalog;
        static readonly Dictionary<string,AssetBundle> bundles=new Dictionary<string,AssetBundle>();
        static readonly Dictionary<string,AssetBundleCreateRequest> bundleLoads=new Dictionary<string,AssetBundleCreateRequest>();
        static readonly HashSet<string> ready=new HashSet<string>();
        static readonly Dictionary<string,int> lastFrame=new Dictionary<string,int>();
        static readonly Dictionary<string,long> cachedBytes=new Dictionary<string,long>();
        static readonly Dictionary<string,HashSet<UnityEngine.Object>> cachedAssets=new Dictionary<string,HashSet<UnityEngine.Object>>();
        const long CacheBudget=192L*1024*1024;const int BundleLimit=32;
        static readonly Dictionary<string,int> pins=new Dictionary<string,int>();
        static readonly Dictionary<string,string> owners=new Dictionary<string,string>();
        sealed class InstallMemo {public bool value;public float until;}
        static readonly Dictionary<string,InstallMemo> installedMemo=new Dictionary<string,InstallMemo>();
        static string memoRoot;
        static readonly Dictionary<string,UnityEngine.Object> displayAssets=new Dictionary<string,UnityEngine.Object>();
        readonly Queue<string> displayQueue=new Queue<string>();
        readonly HashSet<string> displayPending=new HashSet<string>();
        readonly Dictionary<string,Type> displayTypes=new Dictionary<string,Type>();
        bool displayLoading;
#if UNITY_EDITOR
        public static int ReviewDiskChecks,ReviewSyncBundleLoads,ReviewAsyncBundleLoads;
#endif
        readonly Queue<CharacterPackRecord> queue=new Queue<CharacterPackRecord>();
        readonly HashSet<string> queued=new HashSet<string>();
        public string DownloadKey,Stage="",Error="";public float Progress;public bool Busy=>DownloadKey!=null;
        public readonly Dictionary<string,string> Results=new Dictionary<string,string>();
        bool cancel,processing;UnityWebRequest request;
        public Func<string,string> OverrideUrl;
        public static string Platform=>Application.platform==RuntimePlatform.Android?"android":"windows";
        public static string Root=>RootOverride??Path.Combine(Application.persistentDataPath,"character-packs-v1");
        public static CharacterPackRecord[] All {get{Ensure();return catalog.packs;}}
        public static CharacterPackRecord Find(string key)=>All.FirstOrDefault(p=>p.key==key);
        public static void Ensure(){if(catalog!=null)return;var asset=Resources.Load<TextAsset>("Majsoul/CharacterDirectory/packs-"+Platform);if(asset==null)throw new InvalidOperationException("角色资源目录尚未生成");catalog=JsonUtility.FromJson<CharacterPackCatalog>(asset.text);if(catalog.schema!=1||catalog.protocol!=1||catalog.platform!=Platform)throw new InvalidDataException("角色资源目录不兼容");}
        public static string HashFile(string path){using(var stream=File.OpenRead(path))using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
        public static string FilePath(CharacterPackRecord pack){if(!System.Text.RegularExpressions.Regex.IsMatch(pack.key,"^[a-z0-9_-]+$"))throw new InvalidDataException("资源标识无效");return Path.Combine(Root,pack.key,pack.sha256+".bundle");}
        public static void RefreshInstalledCache(){installedMemo.Clear();memoRoot=Root;}
        public static bool Installed(string key){
            var p=Find(key);if(p==null)return false;if(p.builtin)return true;if(CharacterRoster.PreviewOnly&&!RespectPreviewAvailability)return p.available;if(!p.available||string.IsNullOrEmpty(p.sha256))return false;
            if(memoRoot!=Root)RefreshInstalledCache();
            bool memoize=Application.isPlaying&&Instance!=null;
            if(memoize&&installedMemo.TryGetValue(key,out var memo)&&memo.until>Time.realtimeSinceStartup)return memo.value;
#if UNITY_EDITOR
            ReviewDiskChecks++;
#endif
            string f=FilePath(p);bool value=File.Exists(f)&&new FileInfo(f).Length==p.bytes&&File.Exists(f+".ok")&&File.ReadAllText(f+".ok")==p.sha256;
            if(memoize)installedMemo[key]=new InstallMemo{value=value,until=Time.realtimeSinceStartup+1};return value;
        }
        public static bool Protected(string key)=>key=="yiji"||key=="erjietang";
        // Verified in this session; verification does not retain every pack in RAM.
        public static bool Ready(string key)=>Installed(key)&&(Protected(key)||ready.Contains(key));
        public static void Touch(string relative){string key=Owner(relative);if(key!=null&&bundles.ContainsKey(key))lastFrame[key]=Time.frameCount;}
        static void ReleaseCached(string key){if(bundles.TryGetValue(key,out var b)&&b!=null)b.Unload(true);bundles.Remove(key);lastFrame.Remove(key);cachedBytes.Remove(key);cachedAssets.Remove(key);foreach(var address in displayAssets.Where(p=>Owner(p.Key)==key).Select(p=>p.Key).ToArray())displayAssets.Remove(address);}
        static void TrimCache(){
            while(bundles.Count>BundleLimit||cachedBytes.Values.Sum()>CacheBudget){
                string active=CharacterRoster.Active.character,dorm=CharacterRoster.Dorm.key;
                string key=lastFrame.Where(p=>p.Value<Time.frameCount&&p.Key!=active&&p.Key!=dorm&&!Pinned(p.Key)).OrderBy(p=>p.Value).Select(p=>p.Key).FirstOrDefault();
                if(key==null)break;ReleaseCached(key);Revision++;
            }
        }
        void LateUpdate(){TrimCache();if(!displayLoading&&displayQueue.Count>0)StartCoroutine(LoadDisplayAsset());}
        static void RejectPack(CharacterPackRecord p,AssetBundle bundle){
            if(bundle!=null)bundle.Unload(true);bundles.Remove(p.key);lastFrame.Remove(p.key);cachedBytes.Remove(p.key);cachedAssets.Remove(p.key);ready.Remove(p.key);
            string receipt=FilePath(p)+".ok";if(File.Exists(receipt))File.Delete(receipt);
            if(Instance!=null)Instance.Results[p.key]="加载失败，可重新下载";Notify();
        }
        // Checking the address does not decode a large portrait on every startup.
        static bool HasPortrait(CharacterPackRecord p,AssetBundle b)=>b!=null&&p.skins.Length>0&&b.Contains("skins/"+p.skins[0]+"/bighead");
        static bool LoadBundle(CharacterPackRecord p,out AssetBundle bundle){
            if(bundles.TryGetValue(p.key,out bundle)&&bundle!=null&&ready.Contains(p.key)){lastFrame[p.key]=Time.frameCount;return true;}
            if(!bundles.TryGetValue(p.key,out bundle)||bundle==null){
#if UNITY_EDITOR
                ReviewSyncBundleLoads++;
#endif
                // A table-entry consumer can arrive while the lobby is already
                // opening this same file. Reuse the request; Unity rejects a
                // second load of an identical bundle as an incompatible pack.
                if(bundleLoads.TryGetValue(p.key,out var loading)){bundle=loading.assetBundle;bundleLoads.Remove(p.key);}else bundle=AssetBundle.LoadFromFile(FilePath(p));}
            if(!HasPortrait(p,bundle)){RejectPack(p,bundle);bundle=null;return false;}
            bundles[p.key]=bundle;lastFrame[p.key]=Time.frameCount;ready.Add(p.key);return true;
        }
        public static bool ContainsSkin(string key,int skin)=>Find(key)?.skins?.Contains(skin)==true;
        public static bool Supports(string key,int skin)=>Installed(key)&&ContainsSkin(key,skin);
        public static string Owner(string relative){if(owners.TryGetValue(relative,out var owner))return owner;var parts=relative.Split('/');if(parts.Length<2)return null;if(parts[0]=="Voice"||parts[0]=="Emoji")owner=parts[1];else if((parts[0]=="Skins"||parts[0]=="Spine")&&int.TryParse(parts[1],out int skin))owner=All.FirstOrDefault(p=>p.skins.Contains(skin))?.key;owners[relative]=owner;return owner;}
        public static T Asset<T>(string relative)where T:UnityEngine.Object {
            string owner=Owner(relative);if(owner==null||Protected(owner))return Resources.Load<T>("Majsoul/Characters/"+relative);
#if UNITY_EDITOR
            if(CharacterRoster.PreviewOnly&&!RespectPreviewAvailability)return UnityEditor.AssetDatabase.LoadAssetAtPath<T>("Assets/CharacterPacks/Source/"+owner+"/"+relative+Extension<T>(relative));
#endif
            if(!Installed(owner))return null;
            if(displayAssets.TryGetValue(relative,out var cached)&&cached!=null){Touch(relative);return cached as T;}
            if(!LoadBundle(Find(owner),out var bundle))return null;
            var asset=bundle.LoadAsset<T>(relative.ToLowerInvariant());
            if(asset!=null){displayAssets[relative]=asset;TrackAsset(owner,asset);TrimCache();}
            return asset;
        }
        static void TrackAsset(string owner,UnityEngine.Object asset){if(!cachedAssets.TryGetValue(owner,out var ids))cachedAssets[owner]=ids=new HashSet<UnityEngine.Object>();if(ids.Add(asset)){long bytes=asset is Texture2D texture?(long)texture.width*texture.height*4:asset is AudioClip clip?(long)clip.samples*clip.channels*4:0;cachedBytes[owner]=(cachedBytes.TryGetValue(owner,out long size)?size:0)+bytes;}}
        // UI requests return immediately. The single queue bounds disk/texture
        // upload work and gives the entrance transition a frame before loading.
        public static T DisplayAsset<T>(string relative)where T:UnityEngine.Object {
            string owner=Owner(relative);if(owner==null||Protected(owner)||Instance==null||!Application.isPlaying||CharacterRoster.PreviewOnly&&!RespectPreviewAvailability)return Asset<T>(relative);
            Touch(relative);if(displayAssets.TryGetValue(relative,out var cached)&&cached!=null)return cached as T;
            if(!Installed(owner))return null;
            if(Instance.displayPending.Add(relative)){Instance.displayTypes[relative]=typeof(T);Instance.displayQueue.Enqueue(relative);}return null;
        }
        public static bool DisplayPending(string relative)=>Instance!=null&&Instance.displayPending.Contains(relative);
        IEnumerator LoadDisplayAsset(){
            displayLoading=true;string relative=displayQueue.Dequeue(),owner=Owner(relative);var p=Find(owner);Pin(owner);var priority=Application.backgroundLoadingPriority;Application.backgroundLoadingPriority=UnityEngine.ThreadPriority.Low;
            try{
                if(!Installed(owner))yield break;
                if(displayAssets.TryGetValue(relative,out var existing)&&existing!=null)yield break;
                if(!bundles.TryGetValue(owner,out var bundle)||bundle==null){
#if UNITY_EDITOR
                    ReviewAsyncBundleLoads++;
#endif
                    if(!bundleLoads.TryGetValue(owner,out var load))bundleLoads[owner]=load=AssetBundle.LoadFromFileAsync(FilePath(p));yield return load;bundle=load.assetBundle;bundleLoads.Remove(owner);
                    if(!HasPortrait(p,bundle)){RejectPack(p,bundle);yield break;}
                    bundles[owner]=bundle;ready.Add(owner);
                }
                lastFrame[owner]=Time.frameCount;
                var request=bundle.LoadAssetAsync(relative.ToLowerInvariant(),displayTypes[relative]);yield return request;
                if(request.asset!=null){displayAssets[relative]=request.asset;TrackAsset(owner,request.asset);}
            }finally{Application.backgroundLoadingPriority=priority;Unpin(owner);displayPending.Remove(relative);displayTypes.Remove(relative);displayLoading=false;}
        }
        static string Extension<T>(string relative){if(typeof(T)==typeof(Texture2D))return ".png";if(typeof(T)==typeof(AudioClip))return ".mp3";if(relative.EndsWith("/skeleton"))return ".bytes";if(relative.EndsWith("/atlas")||relative.EndsWith("/info"))return ".txt";return ".json";}
        public static Texture2D Thumbnail(string key){var p=Find(key);return p==null?null:Resources.Load<Texture2D>("Majsoul/CharacterDirectory/Avatars/"+p.id);}
        public static bool Pinned(string key){lock(pins)return pins.TryGetValue(key,out int n)&&n>0;}
        public static void Pin(string key){lock(pins)pins[key]=pins.TryGetValue(key,out int n)?n+1:1;}
        public static void Unpin(string key){lock(pins)if(pins.TryGetValue(key,out int n))pins[key]=Math.Max(0,n-1);}
        static void Notify(){RefreshInstalledCache();Revision++;CharacterRoster.ReconcileInstalled();Changed?.Invoke();}
        public static bool Delete(string key,out string reason){
            reason="";var p=Find(key);if(p==null||p.builtin||Protected(key)){reason="基础角色不可删除";return false;}if(Pinned(key)){reason="资源正在对局或传输中使用，请稍后删除";return false;}if(Instance!=null&&(Instance.DownloadKey==key||Instance.queued.Contains(key))){reason="请先取消该角色的下载";return false;}
            ready.Remove(key);ReleaseCached(key);var folder=Path.GetDirectoryName(FilePath(p));if(Directory.Exists(folder)){if(!Path.GetFullPath(folder).StartsWith(Path.GetFullPath(Root)+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new InvalidDataException("资源路径越界");Directory.Delete(folder,true);}Instance?.Results.Remove(key);Notify();return true;
        }
        public static void DeleteAll(){foreach(var p in All.Where(p=>!p.builtin))Delete(p.key,out _);}
        void Awake(){if(Instance!=null&&Instance!=this){Destroy(this);return;}Instance=this;Ensure();Directory.CreateDirectory(Root);gameObject.AddComponent<CharacterResourceOverlay>();StartCoroutine(ValidateInstalled());}
        IEnumerator ValidateInstalled(){foreach(var p in All.Where(p=>!p.builtin&&p.available&&Installed(p.key)).ToArray()){string path=FilePath(p);var task=Task.Run(()=>HashFile(path));while(!task.IsCompleted)yield return null;if(!Installed(p.key))continue;if(task.IsFaulted||task.Result!=p.sha256){ready.Remove(p.key);ReleaseCached(p.key);File.Delete(path+".ok");Notify();}else ready.Add(p.key);yield return null;}}
        public void Download(string key,string source=null){var p=Find(key);if(p==null||p.builtin||!p.available||Installed(key)||!queued.Add(key))return;if(source!=null)sourceUrls[key]=source;queue.Enqueue(p);if(!processing)StartCoroutine(ProcessQueue());}
        readonly Dictionary<string,string> sourceUrls=new Dictionary<string,string>();
        public void DownloadGroup(bool recommended){foreach(var p in All.Where(p=>p.available&&!p.builtin&&(!recommended||p.recommended)))Download(p.key);}
        public void Cancel(){queue.Clear();queued.Clear();cancel=true;request?.Abort();}
        public bool Queued(string key)=>queued.Contains(key);
        IEnumerator ProcessQueue(){processing=true;while(queue.Count>0){var p=queue.Dequeue();cancel=false;Error="";DownloadKey=p.key;Progress=0;Stage="准备下载";yield return Guard(DownloadOne(p));Results[p.key]=cancel?"已取消，可继续":string.IsNullOrEmpty(Error)?"已安装":"失败，可重试";queued.Remove(p.key);DownloadKey=null;}if(!cancel&&string.IsNullOrEmpty(Error)&&Stage=="已安装")Stage="下载完成";processing=false;}
        IEnumerator Guard(IEnumerator work){var stack=new Stack<IEnumerator>();stack.Push(work);while(stack.Count>0){object value=null;bool failed=false;try{var top=stack.Peek();if(!top.MoveNext()){stack.Pop();continue;}value=top.Current;if(value is IEnumerator nested){stack.Push(nested);continue;}}catch(Exception ex){Error="资源操作失败："+ex.Message;Stage="操作失败，可重试";request?.Abort();request?.Dispose();request=null;failed=true;}if(failed)yield break;yield return value;}}
        IEnumerator DownloadOne(CharacterPackRecord p){
            Directory.CreateDirectory(Root);string final=FilePath(p),part=final+".part",url=sourceUrls.TryGetValue(p.key,out var source)?source:OverrideUrl?.Invoke(p.key)??p.url;Directory.CreateDirectory(Path.GetDirectoryName(final));sourceUrls.Remove(p.key);
            if(string.IsNullOrEmpty(url)){Error="下载地址不可用";yield break;}
            for(int attempt=0;attempt<2;attempt++){
                long start=File.Exists(part)?new FileInfo(part).Length:0;if(start>p.bytes){File.Delete(part);start=0;}
                long free=long.MaxValue;try{free=new DriveInfo(Path.GetPathRoot(Root)).AvailableFreeSpace;}catch{ }if(free<p.bytes-start+1048576){Error="存储空间不足，请清理资源或使用一姬替代";Stage="空间不足";yield break;}
                if(start<p.bytes){
                    request=UnityWebRequest.Get(url);request.downloadHandler=new DownloadHandlerFile(part,start>0){removeFileOnAbort=false};request.timeout=300;request.redirectLimit=10;if(start>0)request.SetRequestHeader("Range","bytes="+start+"-");Stage="下载中";
                    var op=request.SendWebRequest();while(!op.isDone){Progress=Mathf.Clamp01((start+(long)request.downloadedBytes)/(float)p.bytes);if(cancel)request.Abort();yield return null;}
                    bool success=request.result==UnityWebRequest.Result.Success;long code=request.responseCode;string error=request.error;request.Dispose();request=null;
                    if(cancel){Stage="已取消，可继续下载";yield break;}if(!success){Error="下载失败："+error;Stage="下载失败，可重试";yield break;}if(start>0&&code==200){File.Delete(part);continue;}
                }
                if(!File.Exists(part)||new FileInfo(part).Length!=p.bytes){Error="文件大小不符，请重试";Stage="下载未完成";yield break;}
                Stage="校验资源";Progress=1;var task=Task.Run(()=>HashFile(part));while(!task.IsCompleted)yield return null;if(cancel){Stage="已取消";yield break;}if(task.IsFaulted||task.Result!=p.sha256){File.Delete(part);Error="资源校验失败，未安装";Stage="校验失败";yield break;}
                ready.Remove(p.key);ReleaseCached(p.key);if(File.Exists(final))File.Delete(final);File.Move(part,final);
                Stage="加载资源";var load=AssetBundle.LoadFromFileAsync(final);yield return load;if(load.assetBundle==null){File.Delete(final);Error="资源包与当前客户端不兼容";Stage="加载失败";yield break;}
                if(!HasPortrait(p,load.assetBundle)){load.assetBundle.Unload(true);File.Delete(final);Error="资源包缺少角色头像，未安装";Stage="加载失败";yield break;}
                load.assetBundle.Unload(true);
                File.WriteAllText(final+".ok.tmp",p.sha256);if(File.Exists(final+".ok"))File.Replace(final+".ok.tmp",final+".ok",null);else File.Move(final+".ok.tmp",final+".ok");ready.Add(p.key);Notify();Stage="已安装";yield break;
            }
            Error="下载源不支持续传，请重试";
        }
        public static bool StartupSeen=>File.Exists(Path.Combine(Root,"startup-seen"));
        public static void MarkStartupSeen(){Directory.CreateDirectory(Root);File.WriteAllText(Path.Combine(Root,"startup-seen"),"1");}
        void OnDestroy(){request?.Abort();request?.Dispose();if(Instance==this)Instance=null;}
        public static void ResetForReview(){foreach(var b in bundles.Values)if(b!=null)b.Unload(true);bundles.Clear();bundleLoads.Clear();ready.Clear();lastFrame.Clear();cachedBytes.Clear();cachedAssets.Clear();displayAssets.Clear();pins.Clear();owners.Clear();catalog=null;RefreshInstalledCache();Revision++;}
#if UNITY_EDITOR
        public static int ReviewLoadedBundles=>bundles.Count;
        [Serializable] sealed class EditorBuildPlan {public string output;}
        public static string EditorBundlePath(CharacterPackRecord pack){
            string filename=string.IsNullOrEmpty(pack.file)?pack.key+"-v"+pack.version+"-windows.bundle":pack.file;
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
            var asset=Resources.Load<TextAsset>("Majsoul/CharacterDirectory/build-plan");var plan=asset==null?null:JsonUtility.FromJson<EditorBuildPlan>(asset.text);
            foreach(string folder in new[]{plan?.output,"build/character-resources-all","build/character-resources-v1","build/character-pack-experiment","build/character-packs-cache"}){
                if(string.IsNullOrEmpty(folder))continue;string file=Path.GetFullPath(Path.Combine(root,folder,"windows",filename));
                if(file.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)&&File.Exists(file))return file;
            }
            throw new FileNotFoundException("缺少验证资源包，请先构建或下载编辑器缓存",filename);
        }
#endif
    }
}
