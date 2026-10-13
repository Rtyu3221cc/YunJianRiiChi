"""Collect actual Unity renders of the opponent hand/meld clearance update."""
from pathlib import Path
import hashlib, html, json, shutil
from zipfile import ZipFile, ZIP_DEFLATED

root = Path(__file__).resolve().parent.parent
dist = root / 'dist'
previews = dist / 'previews'
files, sections = [], []

def copy(source, name):
    assert source.is_file(), source
    target = previews / ('unity-0.5.39-' + name + '.png')
    shutil.copyfile(source, target)
    files.append(target.name)
    return target.name

def figure(name, label):
    return f'<figure><a href="previews/{name}"><img loading="lazy" src="previews/{name}"></a><figcaption>{html.escape(label)}</figcaption></figure>'

for seats in (3, 4):
    before = copy(previews / f'unity-0.5.38-review-0538-all-seats-{seats}.png', f'before-{seats}-seats')
    after = copy(root / f'build/opponent-hand-preview/after-{seats}-seats.png', f'after-{seats}-seats')
    sections.append(f'<h2>{seats}人桌，同一四组副露局面</h2><div class="pair">' + figure(before, '0.5.38：其他玩家手牌与副露重叠') + figure(after, '0.5.39：沿各玩家牌列平移手牌，保留加大的距离') + '</div>')

labels = {'draw-down': '荒牌流局扣牌', 'draw-up': '荒牌流局摊牌', 'standing': '四组副露与站立手牌', 'tsumo-seat': '其他玩家自摸：摊牌与自摸牌'}
for source in sorted((root / 'build/unity-smoke').glob('review-0539-*.png')):
    name = copy(source, source.stem)
    label = next((value for key, value in labels.items() if key in source.stem), source.stem)
    sections.append(figure(name, label + ' · ' + source.stem))
assert len(files) == 17, len(files)
page = '''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>0.5.39手牌避让回归截图</title><style>body{max-width:1600px;margin:24px auto;padding:0 18px;background:#121923;color:#e8edf5;font:17px/1.7 sans-serif}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px}figure{margin:10px 0;padding:10px;background:#1d293a}img{width:100%;height:auto}h1{color:#f1d298}@media(max-width:700px){.pair{grid-template-columns:1fr}}</style><h1>0.5.39 其他玩家手牌避让副露</h1><p>副露贴靠和放大后的尺寸保持；其他玩家的站立手牌、和牌摊牌、流局摊牌沿各自牌列避让副露，自摸牌跟随摊牌位置。17张实际Unity渲染截图：2张0.5.38对照，15张本次结果。均为合成测试局面，非安卓真机截图。点击可查看完整尺寸。</p>''' + ''.join(sections) + '</html>'
gallery = dist / 'unity-0.5.39-screenshots.html'
gallery.write_bytes(page.encode('utf8'))
manifest = previews / 'unity-0.5.39-screenshot-manifest.json'
manifest.write_bytes((json.dumps({'actualUnityScreenshots': 17, 'baselineCommit': 'f1e9f41debdc1bc4c696e569a8b2edd0da2dceda', 'files': {name: hashlib.sha256((previews / name).read_bytes()).hexdigest() for name in files}}, indent=2) + '\n').encode('utf8'))
with ZipFile(dist / 'yunjian-riichi-unity-0.5.39-screenshots.zip', 'w', ZIP_DEFLATED) as archive:
    archive.write(gallery, gallery.name)
    archive.write(manifest, 'previews/' + manifest.name)
    for name in files:
        archive.write(previews / name, 'previews/' + name)
print('PASS: 17 actual Unity screenshots, before/after gallery and checksum manifest.')
