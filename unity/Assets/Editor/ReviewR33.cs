using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    // Entry for the 2026-11-07 silent preview recording (see ReviewR33Capture).
    [InitializeOnLoad] public static class ReviewR33 {
        const string Key="ReviewR33";
        static ReviewR33(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<ReviewR33Capture>();}};}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Bootstrap(){
            if(!SessionState.GetBool(Key,false))return;string output=Environment.GetEnvironmentVariable("YUNJIAN_R33_OUT")??Path.GetFullPath("Temp/r33");
            CharacterPackService.RootOverride=output+"/roles";var wardrobes=Environment.GetEnvironmentVariable("YUNJIAN_R33_WARDROBES");WardrobePackService.RootOverride=string.IsNullOrEmpty(wardrobes)?output+"/wardrobes":wardrobes;
            CharacterPackService.RespectPreviewAvailability=false;WardrobePackService.RespectPreviewAvailability=true;CharacterPackService.ResetForReview();WardrobePackService.ResetForReview();
        }
        public static void Run(){PlayerSettings.companyName="YunJianReview";PlayerSettings.productName="ReviewR33";AssetDatabase.Refresh();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
}
