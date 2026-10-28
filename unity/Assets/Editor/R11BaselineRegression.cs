using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace YunJian.Riichi.Editor {
    [InitializeOnLoad] public static class R11BaselineRegression {
        const string Key="R11BaselineRegression";
        static R11BaselineRegression(){EditorApplication.playModeStateChanged+=state=>{
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Key,false))return;
            SessionState.SetBool(Key,false);
            string source="../build/effects-r11-baseline/";
            typeof(WardrobeCatalog).GetField("data",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,JsonUtility.FromJson<WardrobeData>(File.ReadAllText(source+"unity__Assets__Resources__Majsoul__Wardrobe__catalog.json")));
            var contracts=typeof(SourcePlaybackContracts).GetField("data",BindingFlags.NonPublic|BindingFlags.Static);
            contracts.SetValue(null,JsonUtility.FromJson(File.ReadAllText(source+"unity__Assets__Resources__Majsoul__Actions__playback-contracts.json"),contracts.FieldType));
            Debug.Log("R11 baseline outfit catalog restored in memory only; native source camera count="+UnityEngine.Object.FindObjectsByType<NativeEffectCamera>(FindObjectsSortMode.None).Length);
            UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<RuntimeValidation>();
        };}
        public static void Run(){AssetDatabase.Refresh();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
}
