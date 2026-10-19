"""Package actual Unity R2 renders; retain R1 as an independent prior review."""
from pathlib import Path
import runpy,json,shutil,html,re
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parent.parent;S=R/'build/character-review-r2/render';O=R/'dist/previews/2026-10-19-character-system-r2';O.mkdir(exist_ok=True,parents=True)
# Reuse the established encoder/gallery machinery with the new recording only.
source=(R/'scripts/package-character-review-r1.py').read_text('utf8').replace('character-review-r1','character-review-r2').replace('2026-10-18-character-system-r1','2026-10-19-character-system-r2').replace('walkthrough-r1','walkthrough-r2').replace('修正 R1','修正 R2')
source=source.replace('按本轮七张标注图修正。','按最新标注图继续修正，并接入原版立绘构图参数。').replace('半透明面板、灯笼、原版字体与标题底框','选中灯笼原版发光、左右花饰、移除契约提示牌、恢复名称与 CV 字体').replace('标题、页签、头像外发光与契约背景','角色页签置于装扮前方；保留头像外发光与契约背景').replace('初始、契约及其余服饰保持固定顺序','固定顺序、原版卡片构图、角色名字体与下移后的按钮').replace('保留原有出战切换面板与牌桌，本轮没有继续调整这两部分。','本轮修正了开场和立直提示器的角色构图；出战切换面板和牌桌其他问题仍按原计划留待后续。')
source=source.replace("('16:9 布局'", "('原版立绘构图',[('framing-introduction','I0：按皮肤和方位读取构图；旧皮肤使用裁切与头像中心'),('framing-call-2','北向提示器：旧皮肤采用头像中心对齐'),('framing-call-3','西向提示器：恢复完整立绘，使用 J0 方位参数')]),\n ('16:9 布局'")
exec(compile(source,str(R/'scripts/package-character-review-r1.py'),'exec'),{'__file__':str(R/'scripts/package-character-review-r1.py'),'__name__':'__main__'})
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',24);small=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',19)
audit=sorted(S.glob('skin-audit-*.png'));columns=4;w=400;h=440;canvas=Image.new('RGB',(columns*w,((len(audit)+columns-1)//columns)*h),(12,19,32));draw=ImageDraw.Draw(canvas)
for i,p in enumerate(audit):
 im=Image.open(p);scale=im.height/1080;vw=im.width/scale
 box=(int((vw-800)*scale),int(100*scale),int((vw-300)*scale),int(630*scale));crop=im.crop(box);crop.thumbnail((w-10,h-55));x=i%columns*w+(w-crop.width)//2;y=i//columns*h;canvas.paste(crop,(x,y));draw.text((i%columns*w+18,y+h-45),p.stem.replace('skin-audit-','服饰 '),font=font,fill=(227,200,150))
canvas.save(O/'skin-cards-audit.jpg',quality=95)
# Before/after comparison uses the original Unity preview, without red annotations.
before=R/'build/characters-1018/render';comparisons=[]
for old,new,label in [('match-introduction','framing-introduction','开局介绍'),('call-seat-2','framing-call-2','北向立直'),('call-seat-3','framing-call-3','西向立直')]:
 if not(before/(old+'.png')).exists():continue
 c=Image.new('RGB',(1600,415),(12,19,32));d=ImageDraw.Draw(c)
 for j,p in enumerate([before/(old+'.png'),S/(new+'.png')]):
  im=Image.open(p).convert('RGB');im.thumbnail((800,365));c.paste(im,(j*800,(365-im.height)//2+45));d.text((j*800+20,10),label+(' · 修改前'if j==0 else' · 修改后'),font=font,fill=(227,200,150))
 filename='compare-'+new+'.jpg';c.save(O/filename,quality=95);comparisons.append((filename,label))
extra='<h2>立绘构图前后对照</h2><p>同一套源图片，读取统一的场景规则和皮肤配置。原版配置会针对每套皮肤保存美术调整；旧皮肤则使用裁切框与头像中心计算。这里没有按这四名角色另写位置特例。</p>'+''.join('<figure class="strip"><a target="_blank" href="'+p+'"><img src="'+p+'"></a><figcaption>'+n+'</figcaption></figure>'for p,n in comparisons)+'<h2>21 套服饰卡片抽查</h2><figure><a target="_blank" href="skin-cards-audit.jpg"><img src="skin-cards-audit.jpg"></a><figcaption>点击放大；来自实际 Unity 服饰页截图。</figcaption></figure>'
p=O/'index.html';t=p.read_text('utf8').replace('<h2>页面转场逐帧查看</h2>',extra+'<h2>页面转场逐帧查看</h2>');p.write_text(t,encoding='utf8')
print('R2 packaged',O,flush=True)
