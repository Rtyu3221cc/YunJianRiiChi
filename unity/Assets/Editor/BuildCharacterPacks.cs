using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi.Editor {
    public static class BuildCharacterPacks {
        [Serializable] sealed class DirectoryData {public CharacterDefinition[] roles;}
        public static void Run(){
            ProjectSetup.Configure();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);string platform=Environment.GetCommandLineArgs().Contains("-packAndroid")?"android":"windows";bool all=Environment.GetCommandLineArgs().Contains("-allCharacterPacks");var target=platform=="android"?BuildTarget.Android:BuildTarget.StandaloneWindows64;string output=Path.GetFullPath("../build/"+(all?"character-resources-v1":"character-pack-experiment")+"/"+platform);Directory.CreateDirectory(output);
            var keys=all?new[]{"yiji","erjietang","xiangyuanwu","bamuwei","kawei","wangcilang","xiaoniaoyouchutian","beijianshahezi","huiyeji","fuji","qingluan","weilai","siqiqiansuili"}:new[]{"bamuwei","weilai","siqiqiansuili"};
            var directory=JsonUtility.FromJson<DirectoryData>(File.ReadAllText("Assets/Resources/Majsoul/CharacterDirectory/catalog.json"));
            var definitions=keys.Select(key=>{bool builtin=key=="yiji"||key=="erjietang";string root=builtin?"Assets/Resources/Majsoul/Characters/":"Assets/CharacterPacks/Source/"+key+"/";var role=directory.roles.First(r=>r.key==key);var assets=Directory.GetFiles(root,"*",SearchOption.AllDirectories).Where(p=>new[]{".png",".mp3",".bytes",".txt",".json"}.Contains(Path.GetExtension(p))).Select(p=>p.Replace('\\','/')).Where(p=>!builtin||p.StartsWith(root+"Voice/"+key+"/")||p.StartsWith(root+"Emoji/"+key+"/")||role.skins.Any(s=>p.StartsWith(root+"Skins/"+s.id+"/")||p.StartsWith(root+"Spine/"+s.id+"/"))).OrderBy(p=>p,StringComparer.Ordinal).ToArray();if(assets.Length==0)throw new Exception("角色源文件缺失: "+key);return new AssetBundleBuild{assetBundleName=key+"-v1-"+platform+".bundle",assetNames=assets,addressableNames=assets.Select(p=>p.Substring(root.Length).Substring(0,p.Substring(root.Length).LastIndexOf('.')).ToLowerInvariant()).ToArray()};}).ToArray();
            if(BuildPipeline.BuildAssetBundles(output,definitions,BuildAssetBundleOptions.StrictMode,target)==null)throw new Exception("角色资源包构建失败");Debug.Log("CHARACTER PACK BUILD PASS "+platform+" "+definitions.Length);
        }
    }
}
