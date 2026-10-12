"""Bundle real Unity screenshots and the two APK-exported presentation sounds."""
from pathlib import Path
import sys,shutil,json,zipfile,html
R=Path(__file__).resolve().parent.parent;D=R/'dist';P=D/'previews';P.mkdir(exist_ok=True)
sys.path.insert(0,str(R/'tools/asset-inspector'));from PIL import Image,ImageDraw,ImageFont
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',20)
files=[]
for source in sorted((R/'build/unity-smoke').glob('review-0532-*.png')):
 target=P/('unity-0.5.32-'+source.name);shutil.copyfile(source,target);files.append(target.name)
assert len(files)==11
for name in ['cutin','winlose']:
 target=P/f'unity-0.5.32-{name}.wav';shutil.copyfile(R/f'unity/Assets/Resources/Majsoul/Audio/{name}.wav',target);files.append(target.name)
sheet=Image.new('RGB',(690,480),(18,24,33));draw=ImageDraw.Draw(sheet)
for i,(suffix,label)in enumerate([('before-1280','0.5.31 副露位置'),('after-1280','0.5.32 下移7像素 · 尺寸不变')]):
 image=Image.open(R/f'build/unity-smoke/review-0532-04-{suffix}.png');crop=image.crop((800,550,1270,720));crop.thumbnail((650,190));draw.text((15,i*240+8),label,font=font,fill=(245,216,147));sheet.paste(crop,(15,i*240+43))
sheet.save(P/'unity-0.5.32-meld-comparison.png');files.append('unity-0.5.32-meld-comparison.png')
figures=''.join(f'<figure><a href="previews/{n}" target="_blank"><img src="previews/{n}" loading="lazy"></a><figcaption>{html.escape(n.replace("unity-0.5.32-review-0532-", "").replace(".png", ""))}</figcaption></figure>'for n in files if n.endswith('.png'))
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻0.5.32 · 和牌交接与音效</title><style>body{max-width:1200px;margin:24px auto;padding:0 18px;background:#121923;color:#e8edf5;font:17px/1.7 "Microsoft YaHei",sans-serif}h1,h2{color:#f1d298}figure{padding:14px;margin:24px 0;background:#1d293a;border-radius:8px}img{max-width:100%;display:block;margin:auto}figcaption{padding:10px;color:#b9c8dd}audio{width:100%;max-width:650px}a{color:#bbd5ff}</style><h1>云间日麻0.5.32 · 和牌交接与原版音效</h1><p>站立手牌先隐藏，再显示倒牌；两种音效直接从提供的雀魂APK导出，游戏内使用同一资源。自家副露下移7像素，保留原大小。截图为实际Unity渲染的合成局面，非安卓真机截图。</p><h2>和牌横幅原版音效 cutin</h2><audio controls src="previews/unity-0.5.32-cutin.wav"></audio><h2>开局名牌原版音效 winlose</h2><audio controls src="previews/unity-0.5.32-winlose.wav"></audio>'''+figures+'</html>'
gallery=D/'unity-0.5.32-screenshots.html';gallery.write_text(page,encoding='utf8')
with zipfile.ZipFile(D/'yunjian-riichi-unity-0.5.32-screenshots.zip','w',zipfile.ZIP_DEFLATED)as archive:
 archive.write(gallery,gallery.name)
 for n in files:archive.write(P/n,'previews/'+n)
(P/'unity-0.5.32-screenshot-manifest.json').write_text(json.dumps({'screenshots':11,'comparisonSheets':1,'apkSounds':2,'files':files},indent=2)+'\n',encoding='utf8')
print('PASS: 11 actual screenshots, meld comparison, both APK sound previews and offline gallery.')
