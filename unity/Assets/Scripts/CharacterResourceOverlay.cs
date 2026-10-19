using UnityEngine;
namespace YunJian.Riichi {
    public sealed class CharacterResourceOverlay:MonoBehaviour {
        void OnGUI(){GUI.depth=-100;CharacterResourceUi.Current?.DrawOverlay();}
    }
}
