using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi.Editor {
    public static class CharacterResourceValidation {
        [Serializable] sealed class Report {public int assertions,roles,skins,dynamics,voices,emojis;public bool androidDeviceTested;public List<string> checks=new List<string>();}
        static Report report;static string output;
        static readonly List<string> errors=new List<string>();
        static void Log(string value,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(value);}
        static void Check(bool value,string message){if(!value)throw new Exception(message);report.assertions++;report.checks.Add(message);}
        public static void Run(){
            output=Path.GetFullPath("../build/character-resources-all/validation");Directory.CreateDirectory(output);report=new Report();
            errors.Clear();Application.logMessageReceived+=Log;
            try {
                CharacterPackService.RootOverride=Path.Combine(output,"installed-"+Guid.NewGuid().ToString("N"));
                CharacterPackService.RespectPreviewAvailability=true;CharacterPackService.ResetForReview();CharacterRoster.BeginPreview();
                var all=CharacterPackService.All.Where(p=>p.available).ToArray();
                var args=Environment.GetCommandLineArgs();int selected=Array.IndexOf(args,"-validatePackKeys");if(selected>=0){var keys=args[selected+1].Split(',');all=all.Where(p=>keys.Contains(p.key)).ToArray();Check(all.Length==keys.Length,"All requested validation roles exist");}
                Check(CharacterPackService.All.Count(p=>p.builtin)==2&&CharacterPackService.Protected("yiji")&&CharacterPackService.Protected("erjietang"),"Exactly two protected built-in roles");
                int expectedSkins=all.Sum(p=>p.skins.Length),expectedDynamics=all.Sum(p=>CharacterRoster.Find(p.key).skins.Count(s=>s.dynamic));
                foreach(var pack in all){
                    CharacterPackService.ResetForReview();
                    var role=CharacterRoster.Find(pack.key);Check(role!=null,"Metadata exists: "+pack.key);
                    Check(role.skins.Select(s=>s.id).OrderBy(i=>i).SequenceEqual(pack.skins.OrderBy(i=>i)),"Manifest skins match metadata: "+pack.key);
                    string final=null;
                    if(!pack.builtin){
                        string source=CharacterPackService.EditorBundlePath(pack);Check(File.Exists(source),"Bundle exists: "+pack.key);
                        Check(new FileInfo(source).Length==pack.bytes&&CharacterPackService.HashFile(source)==pack.sha256,"Bundle size and SHA256 match: "+pack.key);
                        final=CharacterPackService.FilePath(pack);Directory.CreateDirectory(Path.GetDirectoryName(final));File.Copy(source,final,true);File.WriteAllText(final+".ok",pack.sha256);
                    }
                    Check(CharacterPackService.Installed(pack.key),"Installation accepted: "+pack.key);
                    Check(CharacterPackService.Thumbnail(pack.key)!=null,"Lightweight avatar exists: "+pack.key);
                    Check(role.emojis.Length>0&&role.emojis.Distinct().Count()==role.emojis.Length,"Unique declared expression bank: "+pack.key);
                    foreach(var emoji in role.emojis)Check(CharacterRoster.Texture(emoji)!=null,"Declared expression loads: "+emoji);
                    using(var illustration=new CharacterIllustration())foreach(var skin in role.skins){
                        Check(CharacterPackService.Supports(pack.key,skin.id),"Skin supported: "+skin.id);
                        foreach(string kind in new[]{"full","bighead","smallhead"})Check(CharacterRoster.Texture("Skins/"+skin.id+"/"+kind)!=null,"Texture: "+skin.id+"/"+kind);
                        if(skin.dynamic){
                            Check(skin.parts!=null&&skin.parts.Length>0,"Dynamic parts: "+skin.id);
                            foreach(string part in skin.parts){
                                var bytes=CharacterPackService.Asset<TextAsset>(part+"/skeleton");var atlas=CharacterPackService.Asset<TextAsset>(part+"/atlas");
                                Check(bytes!=null&&bytes.bytes.Length>100&&atlas!=null,"Skeleton and atlas: "+part);
                            }
                            Texture art=null;for(int step=0;step<=30;step++)art=illustration.Art(skin,step/30f);Check(art!=null,"Dynamic runtime renders: "+skin.id);
                            Save(art,skin.id+"-dynamic");report.dynamics++;
                        } else Save(CharacterRoster.Texture("Skins/"+skin.id+"/full"),skin.id+"-still");
                        report.skins++;
                    }
                    if(!pack.builtin){
                        // The provider has loaded this bundle; inspect its complete public address list.
                        var loaded=AssetBundle.GetAllLoadedAssetBundles().Single(b=>b.name==pack.file);
                        var addresses=loaded.GetAllAssetNames();
                        Check(addresses.Distinct().Count()==addresses.Length,"Unique bundle addresses: "+pack.key);
                        foreach(var address in addresses.Where(a=>a.StartsWith("voice/"+pack.key+"/"))){var clip=loaded.LoadAsset<AudioClip>(address);Check(clip!=null&&clip.samples>0&&clip.channels>0,"Voice clip: "+address);report.voices++;}
                        foreach(var address in addresses.Where(a=>a.StartsWith("emoji/"+pack.key+"/"))){Check(loaded.LoadAsset<Texture2D>(address)!=null,"Expression: "+address);report.emojis++;}
                        Check(addresses.Any(a=>a.StartsWith("voice/"+pack.key+"/"))&&addresses.Any(a=>a.StartsWith("emoji/"+pack.key+"/")),"Voice and expression banks: "+pack.key);
                        CharacterPackService.Pin(pack.key);Check(!CharacterPackService.Delete(pack.key,out _),"Pinned deletion rejected: "+pack.key);CharacterPackService.Unpin(pack.key);
                        Check(CharacterPackService.Delete(pack.key,out _)&&!CharacterPackService.Installed(pack.key),"Delete removes installation: "+pack.key);
                    }else{Check(!CharacterPackService.Delete(pack.key,out _),"Base deletion rejected: "+pack.key);foreach(var clip in Resources.LoadAll<AudioClip>("Majsoul/Characters/Voice/"+pack.key)){Check(clip.samples>0&&clip.channels>0,"Base voice clip: "+pack.key+"/"+clip.name);report.voices++;}report.emojis+=role.emojis.Length;}
                    report.roles++;Resources.UnloadUnusedAssets();GC.Collect();
                }
                Check(report.roles==all.Length&&report.skins==expectedSkins&&report.dynamics==expectedDynamics,"All declared characters and outfits were validated");
                Check(errors.Count==0,"No Unity errors during complete resource loading and rendering");
                File.WriteAllText(Path.Combine(output,"checks.json"),JsonUtility.ToJson(report,true));
                Debug.Log("CHARACTER RESOURCE VALIDATION PASS "+report.assertions+" roles="+report.roles+" skins="+report.skins+" dynamics="+report.dynamics);EditorApplication.Exit(0);
            } catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
            finally{Application.logMessageReceived-=Log;CharacterPackService.ResetForReview();CharacterRoster.EndPreview();}
        }
        static void Save(Texture source,string name){
            float scale=Mathf.Min(1,700f/source.height);int width=Mathf.Max(1,Mathf.RoundToInt(source.width*scale)),height=Mathf.Max(1,Mathf.RoundToInt(source.height*scale));
            var previous=RenderTexture.active;var rt=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32);Graphics.Blit(source,rt);RenderTexture.active=rt;
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
            var pixels=texture.GetPixels32();Check(pixels.Count(p=>p.a>32)>pixels.Length*.015f,"Visible illustration pixels: "+name);
            File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);
        }
    }
}
