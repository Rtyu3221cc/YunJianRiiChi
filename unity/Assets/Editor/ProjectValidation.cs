using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi.Editor
{
    public static class ProjectValidation
    {
        [MenuItem("云间日麻/验证资源与房间协议")]
        public static void Validate()
        {
            foreach (char suit in "mps") for (int n = 0; n <= 9; n++)
            {
                Require(Resources.Load<Texture2D>("Majsoul/Tiles/" + suit + n) != null, "Missing hand tile " + suit + n);
                Require(Resources.Load<Texture2D>("Majsoul/TableTiles/" + suit + n) != null, "Missing table tile " + suit + n);
            }
            for (int n = 1; n <= 7; n++) Require(Resources.Load<Texture2D>("Majsoul/TableTiles/z" + n) != null, "Missing honor " + n);
            Require(Resources.Load<Texture2D>("Majsoul/Table/cloth") != null, "Missing default cloth");
            Require(Resources.Load<Texture2D>("Majsoul/Table/cloth-chigua") != null, "Missing reference cloth");
            Require(Resources.Load<TextAsset>("Majsoul/OriginalTile/outline") != null, "Missing original outline mesh");
            Require(Resources.Load<TextAsset>("Majsoul/OriginalTile/mesh") != null, "Missing original tile mesh/UV data");
            foreach (var name in new[] { "hand-face", "hand-back", "table-face", "table-back" })
                Require(Resources.Load<Texture2D>("Majsoul/OriginalTile/" + name) != null, "Missing original atlas " + name);
            foreach (var name in new[] { "chang", "ju", "word_left", "number_left", "number_score", "dir", "w_0", "turn-light" })
                Require(Resources.Load<Texture2D>("Majsoul/Hud/" + name) != null, "Missing original HUD atlas " + name);
            foreach (var name in new[] { "btn_leave", "btn_settings", "replay_collect", "left_top", "headframe-default", "ob_ting_off_btn", "ob_ting_on_btn" })
                Require(Resources.Load<Texture2D>("Majsoul/UI/" + name) != null, "Missing HUD skin " + name);
            foreach (var name in new[] { "YunJian/OriginalHand", "YunJian/OriginalTileBody", "YunJian/OriginalTileOutline", "YunJian/ResponseGlow", "YunJian/TurnLight", "YunJian/NorthPetal", "YunJian/WinParticle", "YunJian/TableCloth", "Unlit/Transparent" })
                Require(Shader.Find(name) != null && (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null || Shader.Find(name).isSupported), "Missing/unsupported skin shader " + name);
            Require(Resources.Load<Texture2D>("Majsoul/Table/score-hub") != null, "Missing score hub");
            foreach (var name in new[] { "show_rong","show_zimo","fire-fire_bai_add","fire-fire_bai_alpha","fire-yuanqiu2","win-yuanqiu2","win-shanguang","win-bao","win-Light2","win-thunder","win-lizi","win-kuosan","op_chi","op_peng","op_gang","op_hu","op_zimo","op_lizhi","op_babei","op_liuju","op_x","shout_horizontal","shout_vertical","shout_peng","shout_chi","shout_gang","shout_babei","north-petals","yiji-full","response-arrow","response-glow","hand-skin","hand-nails" })
                Require(Resources.Load<Texture2D>("Majsoul/Actions/"+name)!=null,"Missing action art "+name);
            Require(Resources.Load<TextAsset>("Majsoul/Actions/discard-hand")!=null && Resources.Load<TextAsset>("Majsoul/Actions/discard-hand-frames")!=null,"Missing original discard animation");
            Require(Resources.Load<TextAsset>("Majsoul/Actions/tsumo-hand-frames")!=null && Resources.Load<TextAsset>("Majsoul/Actions/fire-effect")!=null,"Missing original win animation/effect");
            for(int digit=0;digit<10;digit++)foreach(var prefix in new[]{"h_","ww_"})Require(Resources.Load<Texture2D>("Majsoul/Results/"+prefix+digit)!=null,"Missing native result digit "+prefix+digit);
            foreach(var name in new[]{"bg_jiesuan","bg_fan","bg_center_fan","bg_point","scorechange_bg","scorechangearrow_0","word_fan","word_fu","word_point","word_yiman","word_man","word_guan","word_tiao","word_bei","word_yi","word_san","word_si","word_wu","word_liu","word_liang","word_lei","word_ji"})Require(Resources.Load<Texture2D>("Majsoul/Results/"+name)!=null,"Missing settlement art "+name);
            var resultDigit=Resources.Load<Texture2D>("Majsoul/Results/ww_0");
            Require(resultDigit.width==69 && resultDigit.height==108,"Native point digit aspect was altered by import");
            Require(Resources.Load<Font>("Majsoul/Font/game") != null, "Missing Chinese font");
            var manifest = JsonUtility.FromJson<VoiceManifest>(Resources.Load<TextAsset>("Majsoul/voice-manifest").text);
            Require(manifest.provider == "yiji", "Active pack must be the one-role Ichihime pack");
            foreach (var clip in manifest.clips) Require(Resources.Load<AudioClip>(clip.resource) != null, "Missing audio " + clip.resource);
            Require(RiichiClient.ValidEndpoint("http://192.168.43.1:8765"), "LAN endpoint rejected");
            Require(!RiichiClient.ValidEndpoint("https://192.168.43.1:8765") && !RiichiClient.ValidEndpoint("http://8.8.8.8:8765"), "Invalid endpoint accepted");
            var view = JsonUtility.FromJson<RoomReply>("{\"ok\":true,\"state\":{\"phase\":\"play\",\"you\":0,\"n\":3,\"version\":2,\"players\":[{\"name\":\"Host\",\"score\":35000,\"riichi\":1}],\"options\":[{\"id\":\"discard:p1_\",\"kind\":\"discard\",\"tile\":\"p1\"}],\"hand\":[\"p1\"]}}");
            Require(view.ok && view.state.players[0].score == 35000 && view.state.options[0].id == "discard:p1_", "Room protocol JSON mismatch");
            Require(File.Exists("Assets/Plugins/Android/riichi-host.androidlib/libs/riichi-host.jar"), "Missing compiled phone host");
            ProjectSetup.CreateScene();
            Debug.Log("RIICHI UNITY VALIDATION PASS: imported textures, font, single-character audio, protocol, host plug-in and scene.");
        }
        static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    }
}
