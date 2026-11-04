using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    [InitializeOnLoad] public static class PhoneFeedbackR25Review {
        const string Key="PhoneFeedbackR25";
        static PhoneFeedbackR25Review(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);UnityEngine.Object.FindAnyObjectByType<RiichiClient>().gameObject.AddComponent<PhoneFeedbackR25Capture>();}};}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Bootstrap(){if(!SessionState.GetBool(Key,false))return;string repo=SessionState.GetString(Key+"Repo","");string batch=(Environment.GetEnvironmentVariable("YUNJIAN_R25_MODE")??"").StartsWith("r26")?"review-r26":"review-r25";CharacterPackService.RootOverride=repo+"/build/"+batch+"/clients/roles";WardrobePackService.RootOverride=repo+"/build/"+(Environment.GetEnvironmentVariable("YUNJIAN_R25_MODE")=="r30"?"wardrobe-r30":batch)+"/clients/wardrobes";CharacterPackService.RespectPreviewAvailability=WardrobePackService.RespectPreviewAvailability=true;CharacterPackService.ResetForReview();WardrobePackService.ResetForReview();}
        public static void Run(){string repo=Environment.GetEnvironmentVariable("YUNJIAN_R25_REPO")??Path.GetFullPath("..");SessionState.SetString(Key+"Repo",repo);PlayerSettings.companyName="YunJianReview";PlayerSettings.productName=(Environment.GetEnvironmentVariable("YUNJIAN_R25_MODE")??"").StartsWith("r26")?"PhoneFeedbackR26":"PhoneFeedbackR25";AssetDatabase.Refresh();EditorSceneManager.OpenScene("Assets/Scenes/Table.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    }
}
