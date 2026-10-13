"""Package real Unity renders for the hand-axis and eight-pixel meld correction."""
from pathlib import Path
import sys, shutil, json, zipfile, html
root = Path(__file__).resolve().parent.parent
dist = root/'dist'; previews = dist/'previews'; previews.mkdir(exist_ok=True)
sys.path.insert(0, str(root/'tools/asset-inspector'))
from PIL import Image, ImageDraw, ImageFont
font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 22)
files = []
for source in sorted((root/'build/unity-smoke').glob('review-0533-*.png')):
    if 'diagnostic' in source.name: continue
    target = previews/('unity-0.5.33-'+source.name)
    shutil.copyfile(source, target); files.append(target.name)
assert len(files) == 10
sheet = Image.new('RGB', (1050, 1080), (18, 24, 33)); draw = ImageDraw.Draw(sheet)
for i, (suffix, label) in enumerate([('01-hand-before-left-safe', '0.5.32：安全区域中点使手牌向右偏45像素'), ('02-hand-after-left-safe', '0.5.33：对齐牌桌中轴，手牌大小不变')]):
    image = Image.open(root/f'build/unity-smoke/review-0533-{suffix}.png').convert('RGB')
    ImageDraw.Draw(image).line((image.width/2, 0, image.width/2, image.height), fill=(255,230,40), width=3)
    image.thumbnail((1020, 470)); draw.text((15, i*540+12), label, font=font, fill=(245,216,147)); sheet.paste(image,(15, i*540+53))
sheet.save(previews/'unity-0.5.33-hand-comparison.png'); files.append('unity-0.5.33-hand-comparison.png')
sheet = Image.new('RGB', (760, 660), (18, 24, 33)); draw = ImageDraw.Draw(sheet)
for i, (suffix, label) in enumerate([('05-meld-before-1280', '0.5.32：修改前'), ('06-meld-after-1280', '0.5.33：再下移8像素，保持放大后的尺寸')]):
    image = Image.open(root/f'build/unity-smoke/review-0533-{suffix}.png').crop((780,540,1270,720))
    image = image.resize((735,270)); draw.text((12,i*330+8), label, font=font, fill=(245,216,147))
    # Same crop/scale for both versions: bottom remains visible, no stretching.
    sheet.paste(image, (12,i*330+52))
sheet.save(previews/'unity-0.5.33-meld-comparison.png'); files.append('unity-0.5.33-meld-comparison.png')
figures = ''.join(f'<figure><a href="previews/{n}" target="_blank"><img src="previews/{n}" loading="lazy"></a><figcaption>{html.escape(n)}</figcaption></figure>' for n in files[-2:]+files[:-2])
page = '''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻0.5.33 · 手牌中轴与副露位置</title><style>body{max-width:1200px;margin:24px auto;padding:0 18px;background:#121923;color:#e8edf5;font:17px/1.7 "Microsoft YaHei",sans-serif}h1{color:#f1d298}figure{padding:14px;margin:24px 0;background:#1d293a;border-radius:8px}img{max-width:100%;display:block;margin:auto}figcaption{padding:10px;color:#b9c8dd}a{color:#bbd5ff}</style><h1>云间日麻0.5.33 · 手牌中轴与副露位置</h1><p>自家副露再下移8个屏幕像素，尺寸保持。手牌无碰撞时对齐牌桌中轴；左右安全边距不对称时也保持对齐。对照中的黄线表示牌桌中轴，原始截图不含辅助线。保留实际公开牌遮挡时的避让。</p><p>10张实际Unity截图及2张等比例对照图。截图来自合成测试局面，未做安卓真机或多手机热点联机测试。4:3窄画面保留原有副露上移避让。</p>'''+figures+'</html>'
gallery = dist/'unity-0.5.33-screenshots.html'; gallery.write_text(page, encoding='utf8')
assert all((previews/n).is_file() for n in files)
with zipfile.ZipFile(dist/'yunjian-riichi-unity-0.5.33-screenshots.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    archive.write(gallery, gallery.name)
    for name in files: archive.write(previews/name, 'previews/'+name)
(previews/'unity-0.5.33-screenshot-manifest.json').write_text(json.dumps({'screenshots':10,'comparisonSheets':2,'files':files}, indent=2)+'\n', encoding='utf8')
print('PASS: 10 actual Unity renders, 2 equal-scale comparison sheets, offline screenshot gallery.')
