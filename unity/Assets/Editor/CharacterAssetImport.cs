using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi.Editor {
    // Preserve source dimensions and transparent padding, including narrow UI sprites.
    public sealed class CharacterAssetImport:AssetPostprocessor {
        public override int GetPostprocessOrder()=>1200;
        void OnPreprocessTexture(){if(!assetPath.StartsWith("Assets/Resources/Majsoul/Characters/"))return;var i=(TextureImporter)assetImporter;i.textureType=TextureImporterType.Default;i.npotScale=TextureImporterNPOTScale.None;i.mipmapEnabled=false;i.alphaIsTransparency=!assetPath.Contains("/Spine/");i.maxTextureSize=8192;i.textureCompression=TextureImporterCompression.Uncompressed;i.wrapMode=TextureWrapMode.Clamp;}
        public static void Configure(){AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);}
    }
}
