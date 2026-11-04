using System;
using UnityEditor;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    public static class PhoneFeedbackR29Build {
        public static void Run() {
            try {
                ProjectSetup.Configure();
                // The legacy aggregate requires optional character media in
                // Resources. This stage deliberately excludes downloadable packs;
                // R25/R26 already exercised those through the actual pack loader.
                Release0545Validation.Run();
                Release063Validation.Run();
                Release065Validation.Run();
                ProjectSetup.BuildAndroid();
                Debug.Log("PHONE FEEDBACK R29 ANDROID BUILD PASS " + ProjectSetup.ReleaseVersion);
                EditorApplication.Exit(0);
            } catch(Exception error) {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }
    }
}
