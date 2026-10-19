using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace YunJian.Riichi.Editor {
    public static class Release065Validation {
        static readonly List<string> checks=new List<string>();
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add(message);}
        static string Pool()=>string.Join("|",CharacterRoster.Pool.Select(p=>p.character+":"+p.skin));
        public static void Run(){
            checks.Clear();CharacterRoster.BeginPreview();
            try{
                Check(PlayerSettings.bundleVersion=="0.6.6"&&PlayerSettings.Android.bundleVersionCode==54,"Release metadata is 0.6.6 / 54");
                CharacterRoster.Use("weilai",406903);CharacterRoster.TogglePool("weilai",406901);CharacterRoster.TogglePool("weilai",406903);CharacterRoster.TogglePool("siqiqiansuili",403301);CharacterRoster.TogglePool("yiji",400107);string candidates=Pool();
                Check(!CharacterRoster.Random&&CharacterRoster.Pool.Length==4,"Candidates can be retained while random mode is disabled");
                foreach(bool enabled in new[]{true,false,true}){CharacterRoster.Random=enabled;Check(Pool()==candidates,"Switching mode preserves candidate outfits: "+enabled);var request=new RequestBody();CharacterRoster.FillRequest(request);Check(request.randomCharacters==enabled&&request.characterPool.Length==4,"Public request follows the mode switch without losing candidates: "+enabled);}
                CharacterRoster.Star("weilai");CharacterRoster.Star("siqiqiansuili");CharacterRoster.Reorder(new[]{"yiji","erjietang","siqiqiansuili","weilai"});
                for(int i=0;i<3;i++){
                    var all=CharacterRoster.Ordered().Where(c=>new[]{"yiji","erjietang","weilai","siqiqiansuili"}.Contains(c.key)).ToArray();var starred=CharacterRoster.Ordered(true);
                    Check(all.Length==4&&starred.Length==2,"Star filtering returns only the two starred characters");
                    Check(starred.All(r=>CharacterRoster.Favorite(r.key))&&!starred.Any(r=>r.key=="yiji"),"Unstarred candidate is excluded only from the filtered roster");
                    Check(Pool()==candidates&&CharacterRoster.InPool("yiji",400107),"Star filtering keeps unstarred candidates in the selected pool");
                    Check(CharacterRoster.Active.character=="weilai"&&CharacterRoster.Active.skin==406903,"Filtering and mode changes preserve the specified character");
                }
                CharacterRoster.Star("weilai");CharacterRoster.Star("siqiqiansuili");Check(CharacterRoster.Ordered(true).Length==0&&Pool()==candidates,"No favorites produces an empty filtered roster without deleting candidates");
                CharacterRoster.ClearPool();Check(CharacterRoster.Random&&CharacterRoster.Pool.Length==0,"Clearing the pool leaves random mode enabled");var emptyRequest=new RequestBody();CharacterRoster.FillRequest(emptyRequest);Check(!emptyRequest.randomCharacters&&emptyRequest.character=="weilai"&&emptyRequest.skin==406903,"Empty random pool falls back to the specified character");
                CharacterRoster.TogglePool("weilai",406901);Check(CharacterRoster.InPool("weilai",406901),"A candidate can be added after clearing");CharacterRoster.TogglePool("weilai",406901);Check(CharacterRoster.Pool.Length==0,"Toggling the candidate again removes only that outfit");
                var texture=new Texture2D(2,2);try{
                    texture.LoadImage(File.ReadAllBytes("Assets/Resources/Majsoul/Characters/UI/Role/btn_rand_slider.png"));var pixels=texture.GetPixels32();int minX=texture.width,minY=texture.height,maxX=0,maxY=0;
                    for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>230){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x+1);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y+1);}
                    var method=typeof(LobbyController).GetMethod("RoleRandomKnob",BindingFlags.NonPublic|BindingFlags.Static);Check(method!=null,"Runtime uses one native toggle-knob geometry function");var track=new Rect(310,176,140,40);
                    foreach(bool enabled in new[]{false,true}){var knob=(Rect)method.Invoke(null,new object[]{track,enabled});float left=knob.x+minX*knob.width/texture.width,right=knob.x+maxX*knob.width/texture.width,top=knob.y+(texture.height-maxY)*knob.height/texture.height,bottom=knob.y+(texture.height-minY)*knob.height/texture.height;Check(Math.Abs(top-track.yMin)<.2f&&Math.Abs(bottom-track.yMax)<.2f,"Opaque knob matches track height after excluding native shadow padding: "+enabled);Check(enabled?Math.Abs(right-track.xMax)<.2f:Math.Abs(left-track.xMin)<1,"Opaque knob closes the outer track end: "+enabled);}
                }finally{UnityEngine.Object.DestroyImmediate(texture);}
                foreach(string sprite in new[]{"Role/frame","Role/bg","Role/tabs_selected","Role/tabs_unselected","Role/unfold_base","Role/using_1_text_right2","Role/btn_rand_slider"})Check(CharacterRoster.Texture("UI/"+sprite)!=null,"Native roleset sprite is bundled: "+sprite);
            }finally{CharacterRoster.EndPreview();}
            var folder=Path.GetFullPath("../build/release-065");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"role-ui-unit-passed.txt"),checks.Count+" role UI assertions\n"+string.Join("\n",checks));Debug.Log("RELEASE 065 ROLE UI UNIT PASS: "+checks.Count);
        }
    }
}
