using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    public static class Release063Validation {
        static readonly List<string> checks=new List<string>();
        static readonly List<string> errors=new List<string>();
        static void Log(string message,string trace,LogType kind){if(kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)errors.Add(message);}
        static void Check(bool value,string text){if(!value)throw new Exception(text);checks.Add(text);}
        static bool Near(float a,float b)=>Mathf.Abs(a-b)<.001f;
        public static void Run(){Application.logMessageReceived+=Log;try{RunChecks();}finally{Application.logMessageReceived-=Log;}}
        static void RunChecks(){
            checks.Clear();errors.Clear();ProjectSetup.Configure();NativeCalloutAssetImport.Configure();
            Check(PlayerSettings.Android.bundleVersionCode>=51,"Native-callout regression requires compatible update code");
            Check(Shader.Find("YunJian/NativeCallout")!=null,"Callout compositor shader available");
            var callout=new NativeCallouts();var widths=new Dictionary<string,float>();
            try{
                foreach(string word in new[]{"shout_chi","shout_peng","shout_gang","shout_lizhi","shout_babei"}){
                    var texture=Resources.Load<Texture2D>("Majsoul/Callouts1018/"+word);
                    Check(texture!=null&&texture.width==187&&texture.height==143,word+" preserves full native Sprite canvas");
                    var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
                    Check(importer.npotScale==TextureImporterNPOTScale.None&&!importer.mipmapEnabled,word+" import preserves dimensions");
                    // Independently derive visible bounds from the original PNG alpha,
                    // rather than trusting the runtime layout metadata under test.
                    var source=new Texture2D(2,2);source.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(texture)));var pixels=source.GetPixels32();
                    int left=source.width,right=-1,bottom=source.height,top=-1;
                    for(int y=0;y<source.height;y++)for(int x=0;x<source.width;x++)if(pixels[y*source.width+x].a>=8){left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
                    var ink=new Rect(left,source.height-1-top,right-left+1,top-bottom+1);UnityEngine.Object.DestroyImmediate(source);
                    foreach(Vector2 center in new[]{Vector2.zero,new Vector2(17,-31),new Vector2(640,360)}){
                        Rect rect=callout.WordRect(word,center);float scale=rect.width/187;
                        Check(Near(rect.width/rect.height,187f/143),word+" keeps native aspect at "+center);
                        Check(Near(ink.height*scale,80),word+" visible character height is 80 at "+center);
                        Check(Vector2.Distance(rect.position+ink.center*scale,center)<.001f,word+" visible lettering is centred at "+center);
                        widths[word]=ink.width*scale;
                    }
                }
                foreach(string word in new[]{"shout_lizhi","shout_babei"})Check(widths[word]>widths["shout_chi"]*1.75f,word+" expands horizontally for two characters");
            }finally{callout.Dispose();}
            // This probe checks the original atlas independently of the saved
            // wardrobe choice and the separately imported wardrobe atlases.
            var original=new OriginalTiles(new WardrobeItem(),new WardrobeItem());
            try{
                // Independent APK atlas landmarks: red/normal suits and honour rows.
                string[] names={"m0","m1","p0","p3","s0","z1","z5","z6","z7"};
                int[] px={3,55,159,315,315,107,315,3,55};int[] py={73,73,145,145,217,361,361,433,433};
                for(int i=0;i<names.Length;i++){
                    var tile=original.CreateTable(null,names[i],Vector3.zero);var face=tile.Find("Original tile").GetComponent<MeshRenderer>().sharedMaterials[1];
                    Check(face.mainTexture==Resources.Load<Texture2D>("Majsoul/OriginalTile/table-face"),names[i]+" uses the complete source atlas");
                    Check(Near(face.mainTextureOffset.x,px[i]/512f)&&Near(face.mainTextureOffset.y,1-py[i]/512f),names[i]+" samples the source (3,3) inset");
                    Check(Near(face.mainTextureScale.x,50f/512)&&Near(face.mainTextureScale.y,70f/512),names[i]+" keeps the 50x70 face window");
                    UnityEngine.Object.DestroyImmediate(tile.gameObject);
                }
            }finally{
                // The production owner disposes during Play mode. These Edit-mode
                // probes own only temporary meshes/materials and require immediate cleanup.
                var owned=new HashSet<UnityEngine.Object>();
                foreach(var field in typeof(OriginalTiles).GetFields(BindingFlags.Instance|BindingFlags.NonPublic)){
                    var value=field.GetValue(original);if(value is Mesh mesh)owned.Add(mesh);if(value is Material material)owned.Add(material);
                    if(value is Dictionary<string,Material> materials)foreach(var m in materials.Values)owned.Add(m);
                }
                foreach(var item in owned)UnityEngine.Object.DestroyImmediate(item);
            }
            Check(errors.Count==0,"No Unity errors during focused units: "+string.Join("; ",errors));
            var path=Path.GetFullPath("../build/release-063");Directory.CreateDirectory(path);File.WriteAllText(Path.Combine(path,"unity-unit-passed.txt"),checks.Count+" focused Unity unit assertions\n"+string.Join("\n",checks));
            Debug.Log("RELEASE 063 UNIT PASS: "+checks.Count);
        }
    }
}
