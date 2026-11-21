"""Copy the rank badges already extracted from the local 4.0.16 APK into the profile resources.

Source: build/apk-rank-title-study/delivery/images (see its README and source-manifest.json).
Writes byte-identical PNGs plus Unity .meta files that reuse the import settings of the
existing Review1012c/sima_queshi.png. Existing .meta files keep their GUIDs.
"""
from pathlib import Path
import shutil, uuid, hashlib, json
R = Path(__file__).resolve().parent.parent
SRC = R / 'build/apk-rank-title-study/delivery/images'
DST = R / 'unity/Assets/Resources/Majsoul/Profile'
template = (R / 'unity/Assets/Resources/Majsoul/Review1012c/sima_queshi.png.meta').read_text('utf8')
FOLDER = 'fileFormatVersion: 2\nguid: {}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
files = {}
for mode in ('sima', 'sanma'):
    for p in sorted((SRC / 'rank/chs' / mode).glob('*.png')): files['Rank/' + p.name] = p
for name in ('star', 'starbg', 'rank_bg'): files['Rank/' + name + '.png'] = SRC / 'rank/common' / (name + '.png')
for p in sorted((SRC / 'rank/profile').glob('*.png')): files['Rank/' + p.name] = p
for p in sorted((SRC / 'rank/celestial').glob('*.png')): files['Rank/' + p.name] = p
files['Title/title_spilit.png'] = SRC / 'title-ui/common/title_spilit.png'
files['Title/title_get_bg.png'] = SRC / 'title-ui/common/title_get_bg.png'

def meta(path, text):
    m = path.with_name(path.name + '.meta')
    if not m.exists(): m.write_bytes(text.format(uuid.uuid4().hex).encode('utf8') if '{}' in text else text.encode('utf8'))

for folder in (DST, DST / 'Rank', DST / 'Title'):
    folder.mkdir(parents=True, exist_ok=True); meta(folder, FOLDER)
report = []
for rel, src in files.items():
    dst = DST / rel; shutil.copyfile(src, dst)
    m = dst.with_name(dst.name + '.meta')
    if not m.exists(): m.write_bytes(template.replace('guid: dd02e9b390c7c1e4b84919cfb3ed7302', 'guid: ' + uuid.uuid4().hex).encode('utf8'))
    report.append(dict(file=rel, source=str(src.relative_to(R)).replace('\\', '/'), sha256=hashlib.sha256(dst.read_bytes()).hexdigest()))
print(json.dumps(dict(count=len(report)), ensure_ascii=False))
(R / 'build/player-info-r1').mkdir(parents=True, exist_ok=True)
(R / 'build/player-info-r1/rank-art.json').write_text(json.dumps(report, ensure_ascii=False, indent=1) + '\n', 'utf8')
