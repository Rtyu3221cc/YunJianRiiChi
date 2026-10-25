using UnityEditor;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    public sealed class WardrobeAssetImport:AssetPostprocessor {
        void OnPreprocessTexture(){
            if(!assetPath.StartsWith("Assets/Resources/Majsoul/Wardrobe/"))return;
            var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Default;t.npotScale=TextureImporterNPOTScale.None;t.maxTextureSize=2048;t.alphaIsTransparency=true;t.wrapMode=TextureWrapMode.Clamp;t.textureCompression=TextureImporterCompression.Uncompressed;
            t.mipmapEnabled=assetPath.EndsWith("/table.png")||assetPath.EndsWith("/hand.png");t.filterMode=t.mipmapEnabled?FilterMode.Trilinear:FilterMode.Bilinear;
        }
    }
}
