from pathlib import Path
R=Path(__file__).resolve().parent.parent
p=R/'unity/Assets/Editor/CharacterReviewR1.cs';s=p.read_text('utf8').replace('R1','R2').replace('-r1Quick','-r2Quick').replace('character-review-r1','character-review-r2')
s=s.replace('no table or role-selection changes; no APK;', 'native framing corrections; no gameplay or role-selection changes; no APK;')
s=s.replace('            screenOnly=true;lobby.PreviewHideCharacters();', '            yield return FramingCapture();\n            screenOnly=true;lobby.PreviewHideCharacters();')
point='        void OnDestroy()'
extra='''        IEnumerator FramingCapture(){
            var fixture=JsonUtility.FromJson<RoomView>(File.ReadAllText("../build/unity-smoke/preview-4.json"));fixture.you=0;fixture.phase="play";fixture.audioRoom="framing-r2";fixture.audioSeq=0;fixture.audioEvents=new PublicSound[0];fixture.result=null;fixture.options=new Choice[0];
            string[] keys={"weilai","siqiqiansuili","erjietang","yiji"};int[] skins={406903,403301,400207,400107};
            for(int i=0;i<4;i++){fixture.players[i].character=keys[i];fixture.players[i].skin=skins[i];fixture.players[i].name=CharacterRoster.Find(keys[i]).name;fixture.players[i].takeoverMask=false;fixture.players[i].bot=i!=0;}
            lobby.PreviewHideCharacters();client.DesignPreviewSessionTable(fixture);client.ValidationIntro.Reenter(fixture);client.ValidationIntro.Freeze=true;client.ValidationIntro.FrozenAge=1.1f;
            Chapter("开场构图 · I0 按皮肤与方向读取原版位置、缩放、翻转");yield return Hold(24);yield return Shot("framing-introduction");yield return Hold(36);client.ValidationIntro.Reset();
            var effects=GetComponent<TableEffects>();
            foreach(int seat in new[]{2,3}){effects.Reset();effects.ValidationFreeze=true;effects.ValidationTime=.25f;typeof(TableEffects).GetMethod("BeginCall",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(effects,new object[]{new PublicSound{seat=seat,kind="riichi"}});Chapter("提示器构图 · "+fixture.players[seat].name+" / J0 方向参数");yield return Hold(12);yield return Shot("framing-call-"+seat);yield return Hold(30);}
            effects.Reset();client.BeginLobbyValidation("character-ui-r2");
            screenOnly=true;
            foreach(var role in CharacterRoster.All){lobby.PreviewDorm(role.key,role.defaultSkin,"skins");for(int i=0;i<role.skins.Length;i++){lobby.ReviewSkinIndex(i,false);yield return Hold(1);yield return Shot("skin-audit-"+role.skins[i].id);}}
            screenOnly=false;
        }
'''
s=s.replace(point,extra+point)
(R/'unity/Assets/Editor/CharacterReviewR2.cs').write_text(s,encoding='utf8')
print('Prepared CharacterReviewR2')
