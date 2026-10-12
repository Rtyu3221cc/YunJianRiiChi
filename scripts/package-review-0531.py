"""Package actual Unity screenshots and same-height reference comparisons for 0.5.31."""
from pathlib import Path
import sys,shutil,json,zipfile,html
R=Path(__file__).resolve().parent.parent;D=R/'dist';P=D/'previews';P.mkdir(exist_ok=True)
sys.path.insert(0,str(R/'tools/asset-inspector'));from PIL import Image,ImageDraw,ImageFont
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',22)
reference=Image.open('D:/MyWorkSpace/video/1012-2/牌桌优化1-原版.jpg').crop((329,54,2430,1240)).resize((1280,720))
before=Image.open(R/'build/unity-smoke/review-0531-meld-before-1280.png');after=Image.open(R/'build/unity-smoke/review-0531-meld-after-1280.png')
sheet=Image.new('RGB',(530,3*235),(18,24,33));draw=ImageDraw.Draw(sheet)
for i,(image,label)in enumerate([(reference,'雀魂原图 · 按相同牌桌高度对齐'),(before,'0.5.30 · 修改前（同一测试局面）'),(after,'0.5.31 · 修改后 · 手牌大小不变')]):
 draw.text((15,i*235+8),label,font=font,fill=(245,216,147));crop=image.crop((770,530,1265,720)).resize((792,304));crop.thumbnail((870,184));sheet.paste(crop,(15,i*235+45))
sheet.save(P/'unity-0.5.31-meld-comparison.png')
title=Image.new('RGB',(600,2*185),(18,24,33));draw=ImageDraw.Draw(title)
for i,(path,label)in enumerate([(R/'dist/previews/unity-0.5.30-review-1012c-04-intro-badges.png','称号修改前'),(R/'build/unity-smoke/review-0531-intro-4.png','称号修改后 · 下移约10像素')]):
 image=Image.open(path);crop=image.crop((1010,340,1580,495));crop.thumbnail((870,140));draw.text((15,i*185+6),label,font=font,fill=(245,216,147));title.paste(crop,(15,i*185+40))
title.save(P/'unity-0.5.31-title-comparison.png')
files=[]
for source in sorted((R/'build/unity-smoke').glob('review-0531-*.png')):
 target=P/('unity-0.5.31-'+source.name);shutil.copyfile(source,target);files.append(target.name)
assert len(files)==10
all_files=['unity-0.5.31-meld-comparison.png','unity-0.5.31-title-comparison.png',*files]
figures=''.join(f'<figure><a href="previews/{name}" target="_blank"><img src="previews/{name}" loading="lazy" alt="{html.escape(name)}"></a><figcaption>{html.escape(name.replace("unity-0.5.31-review-0531-", "").replace("unity-0.5.31-", "").replace(".png", ""))}</figcaption></figure>'for name in all_files)
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻0.5.31 · 副露与称号对照</title><style>body{margin:24px auto;padding:0 20px;max-width:1200px;background:#121923;color:#e8edf5;font:17px/1.7 "Microsoft YaHei",sans-serif}h1{color:#f1d298}figure{margin:28px 0;padding:14px;background:#1d293a;border-radius:10px}img{max-width:100%;display:block;margin:auto}figcaption{padding:12px;color:#b9c8dd}a{color:#bbd5ff}</style><h1>云间日麻0.5.31 · 副露与称号对照</h1><p>副露按雀魂原图的实际牌身轮廓重新校准，1280×720下可见宽度48.36→61.73像素，增加约28%；手牌高度仍为101.6像素。称号下移14个原版坐标单位，在1688×780截图中约为10像素。</p><p>参考图只裁切右下角并按相同牌桌高度对齐；前后测试图来自同一局面。其他截图为实际Unity渲染的合成测试局面，非安卓真机截图。点击图片打开原图。</p>'''+figures+'</html>'
gallery=D/'unity-0.5.31-screenshots.html';gallery.write_text(page,encoding='utf8')
with zipfile.ZipFile(D/'yunjian-riichi-unity-0.5.31-screenshots.zip','w',zipfile.ZIP_DEFLATED)as archive:
 archive.write(gallery,gallery.name)
 for name in all_files:archive.write(P/name,'previews/'+name)
(P/'unity-0.5.31-screenshot-manifest.json').write_text(json.dumps({'actualUnityScreenshots':10,'comparisonSheets':2,'files':all_files},indent=2)+'\n',encoding='utf8')
print('PASS: 10 actual screenshots, 2 comparison sheets and offline gallery/ZIP.')
