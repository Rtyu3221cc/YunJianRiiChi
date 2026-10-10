using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace YunJian.Riichi.Editor
{
    [InitializeOnLoad] public static class ProjectSetup
    {
        static ProjectSetup() { EditorApplication.delayCall += EnsureScene; }
        static void EnsureScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists("Assets/Scenes/Table.unity")) return;
            CreateScene();
        }
        [MenuItem("云间日麻/创建或打开牌桌场景")]
        public static void CreateScene()
        {
            if (File.Exists("Assets/Scenes/Table.unity")) { EditorSceneManager.OpenScene("Assets/Scenes/Table.unity"); return; }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("RiichiClient").AddComponent<RiichiClient>();
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Table.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Table.unity", true) };
            Configure();
        }
        [MenuItem("云间日麻/配置安卓构建")]
        public static void Configure()
        {
            PlayerSettings.companyName = "YunJian";
            PlayerSettings.productName = "云间日麻 3D";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.yunjian.riichi.unity");
            PlayerSettings.bundleVersion = "0.5.11";
            PlayerSettings.Android.bundleVersionCode = 12;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            // Local, offline distribution: matches the existing phone-hosted service.
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            // Runtime-generated materials still need their shaders included in a Player build.
            var graphicsAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (graphicsAssets.Length == 0) throw new System.Exception("Unity 尚未生成 GraphicsSettings，请完成首次导入后重新配置。");
            var graphics = new SerializedObject(graphicsAssets[0]);
            var shaders = graphics.FindProperty("m_AlwaysIncludedShaders");
            if (shaders == null) throw new System.Exception("无法读取 Always Included Shaders，请检查编辑器版本。");
            foreach (var name in new[] { "Standard", "Unlit/Texture", "Unlit/Transparent", "Unlit/Color", "YunJian/OriginalHand", "YunJian/OriginalTileBody", "YunJian/OriginalTileOutline", "YunJian/ResponseGlow", "YunJian/TurnLight", "YunJian/NorthPetal", "YunJian/WinParticle", "YunJian/TableCloth" })
            {
                var shader = Shader.Find(name); if (shader == null) continue;
                bool found = false; for (int i = 0; i < shaders.arraySize; i++) if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader) found = true;
                if (!found) { int index = shaders.arraySize; shaders.InsertArrayElementAtIndex(index); shaders.GetArrayElementAtIndex(index).objectReferenceValue = shader; }
            }
            graphics.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }
        [MenuItem("云间日麻/构建安卓 APK")]
        public static void BuildAndroid()
        {
            if (!File.Exists("Assets/Scenes/Table.unity")) CreateScene();
            Configure();
            Directory.CreateDirectory("Builds");
            var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Builds/yunjian-riichi-unity-0.5.11.apk", BuildTarget.Android, BuildOptions.None);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Android build failed");
        }
    }
    public class ResourceImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Majsoul/")) return;
            var t = (TextureImporter)assetImporter;
            t.mipmapEnabled = true;
            t.textureCompression = TextureImporterCompression.Uncompressed;
            t.maxTextureSize = 2048;
            t.alphaIsTransparency = true;
            if (assetPath == "Assets/Resources/Majsoul/Hud/turn-light.png")
            {
                t.mipmapEnabled = false; t.npotScale = TextureImporterNPOTScale.None;
                t.wrapMode = TextureWrapMode.Clamp;
            }
            if(assetPath.StartsWith("Assets/Resources/Majsoul/Actions/"))t.npotScale=TextureImporterNPOTScale.None;
            if(assetPath.StartsWith("Assets/Resources/Majsoul/Results/")){t.npotScale=TextureImporterNPOTScale.None;t.wrapMode=TextureWrapMode.Clamp;}
            if(assetPath=="Assets/Resources/Majsoul/Actions/north-petals.png")
            { t.wrapMode=TextureWrapMode.Clamp; t.mipmapEnabled=false; }
        }
    }
    // Unity otherwise chooses compileSdk from targetSdk. Use the installed modern
    // compiler platform while retaining the offline app's target behavior.
    public class AndroidCompilePlatform : UnityEditor.Android.IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 100;
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var root = Directory.GetParent(path).FullName;
            foreach (var folder in new[] { "unityLibrary", "launcher" })
            {
                var file = Path.Combine(root, folder, "build.gradle");
                if (!File.Exists(file)) continue;
                var text = File.ReadAllText(file);
                text = System.Text.RegularExpressions.Regex.Replace(text, @"compileSdk(?:Version)?\s+(?:=\s*)?\d+", "compileSdk 36");
                File.WriteAllText(file, text);
            }
        }
    }
}
