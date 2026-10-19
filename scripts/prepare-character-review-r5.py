"""Keep native artwork; apply the requested local lettering alignment and capture this review."""
from pathlib import Path
from PIL import Image
import hashlib,json,uuid
R=Path(__file__).resolve().parent.parent
D=R/'unity/Assets/Resources/Majsoul/Characters'
p=D/'UI/Role/using_1.png';original=Image.open(p).convert('RGBA');shifted=original.copy()
# The lettering is baked into the APK sprite. Move each ink row, including its
# antialiasing, by two source/design pixels; the lantern silhouette stays fixed.
rows=[]
for y in range(35,128):
 xs=[x for x in range(32,74) if max(original.getpixel((x,y))[:3])<200]
 if not xs:continue
 left=max(32,min(xs)-2);right=min(74,max(xs)+3)
 shifted.paste(original.crop((left,y,right,y+1)),(left+2,y))
 for x in range(left,left+2):shifted.putpixel((x,y),original.getpixel((left-1,y)))
 rows.append([y,left,right])
out=D/'UI/Role/using_1_text_right2.png';shifted.save(out)
meta=out.with_suffix('.png.meta')
if not meta.exists():meta.write_text(p.with_suffix('.png.meta').read_text('utf8').replace('999cf69487eb924469449c6d524b6050',uuid.uuid5(uuid.NAMESPACE_URL,str(out.relative_to(R))).hex),encoding='utf8')
(D/'roleset-refinements-r5.json').write_text(json.dumps(dict(source='UI/Role/using_1.png',sourceSha256=hashlib.sha256(p.read_bytes()).hexdigest(),output='UI/Role/using_1_text_right2.png',outputSha256=hashlib.sha256(out.read_bytes()).hexdigest(),inkOffset=[2,0],rows=rows),indent=2)+'\n',encoding='utf8')
s=(R/'unity/Assets/Editor/CharacterReviewR4.cs').read_text('utf8').replace('R4','R5').replace('character-review-r4','character-review-r5').replace('-r4Quick','-r5Quick').replace('using System.Reflection;\nusing System.Linq;','using System.Reflection;')
start=s.index('        IEnumerator Capture(){');end=s.index('        void OnDestroy()',start)
body='''        string PoolSignature()=>string.Join("|",CharacterRoster.Pool.Select(p=>p.character+":"+p.skin));
        void CheckRandomFilter(bool stars){
            Check(lobby.ValidationTarget("random-character-weilai").width>0&&lobby.ValidationTarget("random-character-siqiqiansuili").width>0,"random filter retains both starred characters");
            Check((lobby.ValidationTarget("random-character-yiji").width==0)==stars&&(lobby.ValidationTarget("random-character-erjietang").width==0)==stars,"random filter "+(stars?"hides":"restores")+" both unstarred characters");
        }
        IEnumerator Capture(){
            CharacterRoster.BeginPreview();client=GetComponent<RiichiClient>();client.BeginLobbyValidation("character-r5-preview");lobby=client.Lobby;AudioListener.pause=true;yield return Size(1750,800);lobby.DesignPreviewClock=clock;CharacterRoster.Use("siqiqiansuili",403301);CharacterRoster.Equip("weilai",406903);CharacterRoster.Star("weilai");CharacterRoster.Star("siqiqiansuili");CharacterRoster.Reorder(new[]{"weilai","siqiqiansuili","erjietang","yiji"});lobby.DesignPreviewGardenEntry();yield return Hold(65);
            Chapter("切换雀士 · 页签相接、拼接缝修正、使用中文字右移 2px");yield return Tap("role");yield return Hold(18);yield return Shot("switch-main");
            Chapter("服饰展开 · 三角与边框相接，缩小底部空白");yield return Tap("battle-character-siqiqiansuili");yield return Hold(12);yield return Shot("switch-skins");
            Chapter("随机配置 · 候选数量数字右移 2px");yield return Tap("role-tab-random");yield return Tap("random-enabled");yield return Tap("random-character-weilai");yield return Tap("random-skin-406901");yield return Tap("random-skin-406903");yield return Tap("random-character-siqiqiansuili");yield return Tap("random-skin-403301");yield return Tap("random-character-yiji");yield return Hold(5);var popup=lobby.ReviewRoleSkinPopover;yield return Drag(ScreenPoint(popup.xMax-35,popup.center.y),ScreenPoint(popup.x+25,popup.center.y),24);yield return Tap("random-skin-400107");yield return Hold(10);yield return Shot("random-all-popup");
            string candidates=PoolSignature();Check(CharacterRoster.Pool.Length==4,"four outfit candidates grouped under three characters");CheckRandomFilter(false);
            Chapter("随机页面星标过滤 · 仅显示星标，已选列表保留");yield return Tap("random-stars");yield return Hold(10);CheckRandomFilter(true);Check(!lobby.ReviewRolePopoverOpen,"changing random star filter closes the outfit popover");Check(PoolSignature()==candidates,"enabling random star filter preserves all candidates");Check(lobby.ValidationTarget("random-pool-yiji").width>0,"unstarred selected candidate stays visible in the random list");yield return Shot("random-favorites");
            yield return Tap("random-character-weilai");yield return Hold(8);Check(lobby.ValidationTarget("random-skin-406901").width>0&&lobby.ValidationTarget("random-skin-406903").width>0,"starred character outfit selection remains available while filtered");yield return Shot("random-favorites-skins");
            Chapter("关闭星标过滤 · 恢复全部四名雀士");yield return Tap("random-stars");yield return Hold(10);CheckRandomFilter(false);Check(!lobby.ReviewRolePopoverOpen&&PoolSignature()==candidates,"disabling random star filter restores roster without changing candidates");yield return Shot("random-all");
            Chapter("没有星标时 · 显示空提示，候选列表仍保留");CharacterRoster.Star("weilai");CharacterRoster.Star("siqiqiansuili");yield return Tap("random-stars");yield return Hold(8);Check(CharacterRoster.Ordered(true).Length==0&&lobby.ValidationTarget("random-character-weilai").width==0&&lobby.ValidationTarget("random-character-yiji").width==0,"empty star filter exposes no roster avatars");Check(PoolSignature()==candidates&&lobby.ValidationTarget("random-pool-yiji").width>0,"empty star filter preserves existing candidates");yield return Shot("random-no-favorites");CharacterRoster.Star("weilai");CharacterRoster.Star("siqiqiansuili");yield return Hold(8);CheckRandomFilter(true);yield return Tap("random-stars");yield return Hold(6);yield return Shot("random-restored");
            screenOnly=true;yield return Size(1280,720);yield return Hold(6);yield return Shot("random-16x9");yield return Tap("role-tab-single");yield return Tap("battle-character-siqiqiansuili");yield return Hold(6);yield return Shot("switch-16x9");
        }
'''
(R/'unity/Assets/Editor/CharacterReviewR5.cs').write_text(s[:start]+body+s[end:],encoding='utf8')
(R/'build/character-review-r5').mkdir(parents=True,exist_ok=True)
print('Prepared R5 native lettering and scoped capture driver')
