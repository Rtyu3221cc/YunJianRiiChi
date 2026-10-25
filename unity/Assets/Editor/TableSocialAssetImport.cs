using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi.Editor {
    public sealed class TableSocialAssetImport:AssetPostprocessor {
        public override int GetPostprocessOrder()=>1250;
        static readonly string[] WinFiles={"Assets/Resources/Majsoul/Presentation0535/show_rong.png","Assets/Resources/Majsoul/Presentation0535/show_zimo.png"};
        static bool Applies(string path)=>path.StartsWith("Assets/Resources/Majsoul/TableEmotes/")||System.Array.IndexOf(WinFiles,path)>=0;
        static void Apply(TextureImporter t){t.textureType=TextureImporterType.Default;t.npotScale=TextureImporterNPOTScale.None;t.mipmapEnabled=false;t.wrapMode=TextureWrapMode.Clamp;t.alphaIsTransparency=true;t.textureCompression=TextureImporterCompression.Uncompressed;t.maxTextureSize=1024;}
        void OnPreprocessTexture(){if(Applies(assetPath))Apply((TextureImporter)assetImporter);}
        public static void Configure(){AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);foreach(string file in WinFiles){var t=AssetImporter.GetAtPath(file)as TextureImporter;Apply(t);t.SaveAndReimport();}}
    }
}
