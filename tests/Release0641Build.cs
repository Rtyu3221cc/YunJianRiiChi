using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi.Editor {
    public static class Release0641Build {
        [Serializable] sealed class TextureRecord {public string asset;public int width,height;public bool mipmap,readable;}
        [Serializable] sealed class ImportReport {public string version;public int versionCode;public List<TextureRecord> textures=new List<TextureRecord>();}
        public static void Run() {
            try {
                string output=Environment.GetEnvironmentVariable("YUNJIAN_RELEASE_REVIEW");
                if(string.IsNullOrEmpty(output))throw new Exception("YUNJIAN_RELEASE_REVIEW is required");
                Directory.CreateDirectory(output);
                var report=new ImportReport{version=ProjectSetup.ReleaseVersion,versionCode=ProjectSetup.ReleaseCode};
                var paths=new List<string>(Directory.GetFiles("Assets/Resources/Majsoul/Profile/TitleIcons","*.png"));
                foreach(string name in new[]{"using","illustFrame","illustBg","tabInsideNormal","tabInsideSelected","waitingroom_mask"})
                    paths.Add("Assets/Resources/Majsoul/Profile/Native/"+name+".png");
                foreach(string path in paths) {
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                    var image=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                    byte[] bytes=File.ReadAllBytes(path);int Be32(int i)=>(bytes[i]<<24)|(bytes[i+1]<<16)|(bytes[i+2]<<8)|bytes[i+3];
                    if(image==null||image.width!=Be32(16)||image.height!=Be32(20)||importer.mipmapEnabled||importer.npotScale!=TextureImporterNPOTScale.None)
                        throw new Exception("Profile import changed original aspect or mipmaps: "+path);
                    if(path.EndsWith("waitingroom_mask.png")&&(!image.isReadable||image.GetPixels32()[0].a!=0))
                        throw new Exception("Native portrait mask must be readable with transparent corners");
                    report.textures.Add(new TextureRecord{asset=path,width=image.width,height=image.height,mipmap=importer.mipmapEnabled,readable=image.isReadable});
                }
                AssetDatabase.SaveAssets();
                File.WriteAllText(Path.Combine(output,"profile-imports.json"),JsonUtility.ToJson(report,true));
                Debug.Log("PROFILE RELEASE IMPORTS VERIFIED "+report.textures.Count);
                ProjectSetup.BuildAndroid();
                EditorApplication.Exit(0);
            } catch(Exception e) {Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
