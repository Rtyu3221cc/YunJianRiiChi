using UnityEditor;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    public sealed class WardrobeAssetImport:AssetPostprocessor {
        void OnPreprocessTexture(){
            if(!assetPath.StartsWith("Assets/Resources/Majsoul/Wardrobe/"))return;
            var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Default;t.npotScale=TextureImporterNPOTScale.None;t.maxTextureSize=2048;t.alphaIsTransparency=true;t.wrapMode=TextureWrapMode.Clamp;t.textureCompression=TextureImporterCompression.Uncompressed;
            t.mipmapEnabled=assetPath.EndsWith("/table.png")||assetPath.EndsWith("/hand.png")||assetPath.Contains("/R16/Cloth/");t.filterMode=t.mipmapEnabled?FilterMode.Trilinear:FilterMode.Bilinear;
        }
        void OnPreprocessAudio(){
            if(!assetPath.StartsWith("Assets/Resources/Majsoul/Wardrobe/R16/Audio/"))return;
            var importer=(AudioImporter)assetImporter;var settings=importer.defaultSampleSettings;
            settings.loadType=AudioClipLoadType.Streaming;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.7f;settings.preloadAudioData=false;importer.defaultSampleSettings=settings;
            importer.loadInBackground=true;
        }
    }
}
