using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi.Editor
{
    [InitializeOnLoad] public static class LobbyPlayValidation
    {
        const string Key="Riichi.LobbyValidation1013";
        static LobbyPlayValidation(){EditorApplication.playModeStateChanged+=Changed;}
        public static void Run(){ProjectValidation.Validate();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Key,false))return;
            SessionState.SetBool(Key,false);Object.FindFirstObjectByType<RiichiClient>().gameObject.AddComponent<LobbyValidation1013>();
        }
    }
}
