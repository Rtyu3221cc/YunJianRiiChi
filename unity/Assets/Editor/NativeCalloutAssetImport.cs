using UnityEditor;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    // Keep the PNG's original Sprite.rect dimensions. Nearest-POT import would
    // turn 187x143 into 256x128 and invalidate CImage's width-preserving ratio.
    public sealed class NativeCalloutAssetImport:AssetPostprocessor {
        public override int GetPostprocessOrder()=>100;
        static readonly string[] Words={"shout_chi","shout_peng","shout_gang","shout_lizhi","shout_babei"};
        static bool IsWord(string path){foreach(string word in Words)if(path=="Assets/Resources/Majsoul/Callouts1018/"+word+".png")return true;return false;}
        static void Apply(TextureImporter t){t.npotScale=TextureImporterNPOTScale.None;t.mipmapEnabled=false;t.alphaIsTransparency=true;t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.textureCompression=TextureImporterCompression.Uncompressed;t.maxTextureSize=512;}
        void OnPreprocessTexture(){if(IsWord(assetPath))Apply((TextureImporter)assetImporter);}
        public static void Configure(){
            foreach(string word in Words){
                string path="Assets/Resources/Majsoul/Callouts1018/"+word+".png";var t=AssetImporter.GetAtPath(path)as TextureImporter;if(t==null)continue;
                Apply(t);t.SaveAndReimport();var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if(texture.width!=187||texture.height!=143)throw new System.Exception("Native word size changed: "+path+" "+texture.width+"x"+texture.height);
            }
        }
    }
}
