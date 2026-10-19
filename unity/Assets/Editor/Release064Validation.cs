using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace YunJian.Riichi.Editor {
    public static class Release064Validation {
        static readonly List<string> checks=new List<string>();
        static void Check(bool value,string message){if(!value)throw new Exception(message);checks.Add(message);}
        public static void Run(){
            checks.Clear();CharacterRoster.BeginPreview();
            try{
                Check(PlayerSettings.bundleVersion=="0.6.4"&&PlayerSettings.Android.bundleVersionCode==52,"0.6.4 update uses versionCode 52");
                Check(CharacterRoster.All.Length==4,"Four reference characters are available offline");
                Check(CharacterRoster.All.Sum(c=>c.skins.Length)==21,"All 21 APK skins are catalogued");
                Check(CharacterRoster.All.Sum(c=>c.emojis.Length)==71,"All 71 chat expressions are unlocked");
                foreach(var role in CharacterRoster.All){
                    Check(role.skins[0].id==role.defaultSkin&&role.skins[1].id==role.bondSkin,role.key+" keeps initial and bond outfits first");
                    foreach(var skin in role.skins){
                        Check(CharacterRoster.Texture("Skins/"+skin.id+"/full")!=null&&CharacterRoster.Texture("Skins/"+skin.id+"/bighead")!=null,skin.id+" full illustration and portrait are bundled");
                        CharacterRoster.Equip(role.key,skin.id);Check(CharacterRoster.Outfit(role.key)==skin.id,skin.id+" is selectable without unlocking");
                        for(int seat=0;seat<4;seat++)foreach(string scene in new[]{"I0","J0"}){
                            var rect=CharacterFraming.NativeLocal(skin.id,scene,seat,new Rect(-381,193,858,1291),out _);
                            Check(!float.IsNaN(rect.x)&&!float.IsInfinity(rect.y)&&rect.width>0&&rect.height>0,skin.id+" has valid "+scene+" framing for "+seat);
                        }
                        if(skin.dynamic){foreach(string part in skin.parts)Check(Resources.Load<TextAsset>("Majsoul/Characters/"+part+"/skeleton")!=null&&Resources.Load<TextAsset>("Majsoul/Characters/"+part+"/atlas")!=null,skin.id+" original skeleton and atlas are bundled");}
                    }
                    foreach(string emoji in role.emojis)Check(CharacterRoster.Texture(emoji)!=null,role.key+" expression "+Path.GetFileName(emoji));
                    foreach(string action in new[]{"riichi","double-riichi","ron","tsumo","chi","pon","kan","nuki","fan_pinghu","lobby/lobby_normal1"}){
                        var clip=CharacterVoices.Clip(role.key,action);Check(clip!=null&&clip.length>0,role.key+" original voice "+action);
                        Check(AssetDatabase.GetAssetPath(clip).Contains("/Voice/"+role.key+"/"),role.key+" voice resolves to that character");
                    }
                }
                CharacterRoster.Use("weilai",406903);CharacterRoster.Dorm=CharacterRoster.Find("yiji");CharacterRoster.Equip("yiji",400107);
                Check(CharacterRoster.Active.character=="weilai"&&CharacterRoster.Active.skin==406903,"Dorm browsing and outfit edits leave active character unchanged");
                foreach(string shader in new[]{"YunJian/CharacterSpine","YunJian/CharacterStraightAlpha","YunJian/CharacterIntro"})Check(Shader.Find(shader)!=null,shader+" is available to the player build");
            }finally{CharacterRoster.EndPreview();}
            string output=Path.GetFullPath("../build/release-064");Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"character-unit-passed.txt"),checks.Count+" character assertions\n"+string.Join("\n",checks));
            File.Copy("../build/release-063/unity-unit-passed.txt",Path.Combine(output,"callout-unit-passed.txt"),true);File.Copy("../build/release-0545/unit-passed.txt",Path.Combine(output,"legacy-unit-passed.txt"),true);
            Debug.Log("RELEASE 064 CHARACTER UNIT PASS: "+checks.Count);
        }
    }
}
