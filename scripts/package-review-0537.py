from pathlib import Path
import hashlib
import html
import json
import shutil
from zipfile import ZipFile, ZIP_DEFLATED

root = Path(__file__).resolve().parent.parent
folder = root/'build/unity-smoke'
dist = root/'dist'
previews = dist/'previews'
previews.mkdir(exist_ok=True)
files = []
for source in sorted(folder.glob('review-0537-*.png')):
    target = previews/('unity-0.5.37-'+source.name)
    shutil.copyfile(source, target)
    files.append(target.name)
assert len(files) == 38, f'Expected 13 baseline and 25 trial screenshots, got {len(files)}'
for kind in ('before', 'after'):
    source = folder/f'review-0537-{kind}-geometry.json'
    target = previews/('unity-0.5.37-'+source.name)
    shutil.copyfile(source, target)
    files.append(target.name)
labels = {'tsumo-3-seat-0':'三人桌：自家自摸', 'tsumo-4-seat-1':'四人桌：右侧玩家自摸',
          'tsumo-4-seat-2':'四人桌：对家自摸', 'tsumo-4-seat-3':'四人桌：左侧玩家自摸',
          'meld-1-1688':'一组自家副露，1688×780', 'meld-4-1688':'四组自家副露，1688×780',
          'meld-1-1280':'一组自家副露，1280×720', 'draw-up':'流局听牌摊开', 'draw-down':'流局未听牌扣下'}
pairs = []
for name, label in labels.items():
    pair = [f'unity-0.5.37-review-0537-{kind}-{name}.png' for kind in ('before', 'after')]
    assert all(name in files for name in pair)
    pairs.append('<section><h2>'+html.escape(label)+'</h2><div class="pair">'+''.join(
        f'<figure><a href="previews/{name}"><img loading="lazy" src="previews/{name}"></a><figcaption>{caption}</figcaption></figure>'
        for name, caption in zip(pair, ['0.5.36 原尺寸', '0.5.37 统一尺寸']))+'</div></section>')
other = [name for name in files if name.endswith('.png') and ('after-carried' in name or 'after-kan-stack' in name)]
page = '''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>0.5.37 桌面牌尺寸试验对照</title><style>body{max-width:1600px;margin:24px auto;padding:0 18px;background:#121923;color:#e8edf5;font:17px/1.7 sans-serif}figure{margin:0;padding:10px;background:#1d293a}img{width:100%;height:auto}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px}section{margin:24px 0}h1{color:#f1d298}@media(max-width:700px){.pair{grid-template-columns:1fr}}</style>
<h1>0.5.37 桌面牌尺寸统一试验</h1><p>38张实际Unity渲染截图：13张原尺寸，25张试验版。使用相同合成测试局面，非视频重放、非安卓真机截图。自摸对照暂时隐藏结算面板以显示完整牌桌；游戏内结算流程保持原样。点击图片查看完整尺寸。</p>
<p>桌面牌共用0.35世界宽度，透视产生远近差异。保留区域锚点，组内间距及叠杠高度随真实模型计算；自家二维操作手牌独立展示。回滚APK保留0.5.36程序内容和原证书，versionCode提高到38。</p>'''+''.join(pairs)
page += '<h2>动画搬运及叠杠</h2>'+''.join(f'<figure><a href="previews/{name}"><img loading="lazy" src="previews/{name}"></a><figcaption>{html.escape(name)}</figcaption></figure>' for name in other)+'</html>'
gallery = dist/'unity-0.5.37-screenshots.html'
gallery.write_text(page, encoding='utf8')
manifest = {'actualUnityScreenshots':38, 'baselineCommit':'fe4f470cee984ae0f851e61cb81e1fe3ae2684b9',
            'files':{name:hashlib.sha256((previews/name).read_bytes()).hexdigest() for name in files}}
manifest_file = previews/'unity-0.5.37-screenshot-manifest.json'
manifest_file.write_text(json.dumps(manifest, ensure_ascii=False, indent=2)+'\n', encoding='utf8')
with ZipFile(dist/'yunjian-riichi-unity-0.5.37-screenshots.zip', 'w', ZIP_DEFLATED) as archive:
    archive.write(gallery, gallery.name)
    archive.write(manifest_file, 'previews/'+manifest_file.name)
    for name in files: archive.write(previews/name, 'previews/'+name)
print('PASS: 38 actual screenshots, geometry reports, hashed manifest and offline before/after gallery.')
