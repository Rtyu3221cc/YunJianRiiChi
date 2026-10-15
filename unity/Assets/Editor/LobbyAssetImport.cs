using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
namespace YunJian.Riichi.Editor
{
    public sealed class LobbyAssetImport : AssetPostprocessor
    {
        public override int GetPostprocessOrder()=>200;
        void OnPreprocessTexture()
        {
            var t=(TextureImporter)assetImporter;
            if(!assetPath.StartsWith("Assets/Resources/Majsoul/Lobby/")){
                // Adding a postprocessor makes Unity reimport existing assets.
                // Preserve their saved presets instead of letting the legacy
                // folder defaults silently alter the 0.5.39 table resources.
                if(assetPath.StartsWith("Assets/Resources/Majsoul/")&&File.Exists(assetPath+".meta")){
                    string saved=File.ReadAllText(assetPath+".meta");
                    t.mipmapEnabled=Read(saved,"enableMipMap",t.mipmapEnabled?1:0)!=0;
                    t.npotScale=(TextureImporterNPOTScale)Read(saved,"nPOTScale",(int)t.npotScale);
                    t.wrapModeU=(TextureWrapMode)Read(saved,"wrapU",(int)t.wrapModeU);
                    t.wrapModeV=(TextureWrapMode)Read(saved,"wrapV",(int)t.wrapModeV);
                    t.wrapModeW=(TextureWrapMode)Read(saved,"wrapW",(int)t.wrapModeW);
                }
                return;
            }
            t.npotScale=TextureImporterNPOTScale.None;t.wrapMode=TextureWrapMode.Clamp;t.mipmapEnabled=false;t.alphaIsTransparency=true;t.maxTextureSize=4096;t.textureCompression=TextureImporterCompression.Uncompressed;
        }
        static int Read(string yaml,string key,int fallback){var m=Regex.Match(yaml,@"(?m)^\s*"+key+@":\s*(-?\d+)\s*$");return m.Success?int.Parse(m.Groups[1].Value):fallback;}
        void OnPreprocessAudio()
        {
            if(!assetPath.StartsWith("Assets/Resources/Majsoul/Lobby/Audio/"))return;
            var t=(AudioImporter)assetImporter;var s=t.defaultSampleSettings;s.loadType=assetPath.EndsWith("/lobby.wav")?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;t.defaultSampleSettings=s;
        }
    }
}
