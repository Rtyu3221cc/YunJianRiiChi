"""Build dist/previews/2026-11-15-player-info: reference video frames beside actual Unity renders.

Inputs: the user's reference recordings (E:/MyWorkSpace_E/video/1114/claude), the editor
review screenshots (build/player-info-r1/review2) and the R41 yakuman checks.
"""
from pathlib import Path
import sys, json, hashlib, html
R = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(R / 'tools/video-inspector'))
import av
from PIL import Image, ImageDraw, ImageFont
VIDEO = R.parent / 'video/1114/claude'
REVIEW = R / 'build/player-info-r1/review2'
OUT = R / 'dist/previews/2026-11-15-player-info'; OUT.mkdir(parents=True, exist_ok=True)
font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 26)

def frames(name, times):
    clip = av.open(str(VIDEO / name)); want = sorted(times); got = {}; i = 0
    for f in clip.decode(video=0):
        if i >= len(want): break
        if f.time >= want[i]: got[want[i]] = f.to_image().rotate(90, expand=True).convert('RGB'); i += 1
    return got

def pair(left, right, title_left, title_right, out, height=720):
    l = left.resize((round(left.width * height / left.height), height)); r = right.resize((round(right.width * height / right.height), height))
    sheet = Image.new('RGB', (l.width + r.width + 12, height + 44), '#101820'); d = ImageDraw.Draw(sheet)
    sheet.paste(l, (0, 44)); sheet.paste(r, (l.width + 12, 44))
    d.text((12, 6), title_left, font=font, fill='#e8c58a'); d.text((l.width + 24, 6), title_right, font=font, fill='#8fd3ff')
    sheet.save(OUT / out, quality=90); return out

info = frames('點擊進入玩家信息.mp4', [1.2, 2.2, 9.2, 12.2, 13.2, 20.2])
rank = frames('段位算分方法.mp4', [2.6, 8.6])
shot = lambda n: Image.open(REVIEW / (n + '.png')).convert('RGB')
pairs = [
    ('玩家信息页（四麻·段位场）', pair(info[1.2], shot('02-info-four'), '参考视频 1.2s', 'Unity 实际渲染（示例档案）', 'cmp-info-four.jpg')),
    ('三麻（累计役满示例）', pair(info[13.2], shot('03-info-three'), '参考视频 13.2s', 'Unity 实际渲染', 'cmp-info-three.jpg')),
    ('无记录', pair(info[12.2], shot('01-info-empty'), '参考视频 12.2s', 'Unity 新档案', 'cmp-info-empty.jpg')),
    ('称号簿', pair(info[2.2], shot('06-title-book'), '参考视频 2.2s', 'Unity（自定称号）', 'cmp-title-book.jpg')),
    ('更改昵称', pair(info[9.2], shot('08-rename'), '参考视频 9.2s', 'Unity', 'cmp-rename.jpg')),
    ('四麻段位说明', pair(rank[2.6], shot('05-rank-table-four'), '参考视频《段位算分方法》2.6s', 'Unity', 'cmp-rank-four.jpg')),
    ('三麻段位说明', pair(rank[8.6], shot('04-rank-table-three'), '参考视频《段位算分方法》8.6s', 'Unity', 'cmp-rank-three.jpg')),
]
singles = [('终局页段位变化', '12-final-rank'), ('大厅名牌（真实段位与称号）', '10-lobby-badge-sample'), ('风格属性说明', '07-style-help'), ('友人场无记录', '09-friend-tab-empty'), ('2400×1080 宽屏', '11-info-wide')]
for _, n in singles: shot(n).save(OUT / (n + '.jpg'), quality=90)
yakuman = [('飞牌爆点与桌面水波圈（国士无双）', '国士无双-桌面水波圈.jpg'), ('划光时序（纯正九莲宝灯）', '纯正九莲宝灯-划光时序.jpg'), ('划光时序（清老头）', '清老头-划光时序.jpg')]
for _, n in yakuman: (OUT / n).write_bytes((R / 'build/yakuman-alignment-r41' / n).read_bytes())

css = 'body{background:#172130;color:#eef;font:16px system-ui;margin:24px;max-width:1500px}img{max-width:100%;border-radius:6px}h2{color:#e8c58a;margin-top:36px}.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(440px,1fr));gap:14px}a{color:#adceff;text-decoration:none}span{display:block;padding:6px 0}p{color:#c8cbe0}'
parts = ['<!doctype html><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>玩家信息与段位预览</title><style>' + css + '</style>',
         '<h1>玩家信息页与段位算分 · 0.6.39 预览</h1><p>右侧均为 Unity 编辑器实际渲染（示例档案注入，不写入真实存档）；左侧为用户提供的参考录像帧。人物、称号与示例数据不同，未宣称逐像素一致。Android 真机尚未验证。</p>',
         '<h2>与参考对照</h2>']
for title, file in pairs: parts.append(f'<h3>{html.escape(title)}</h3><a href="{file}"><img loading="lazy" src="{file}"></a>')
parts.append('<h2>其他画面</h2><div class="grid">')
for title, n in singles: parts.append(f'<a href="{n}.jpg"><img loading="lazy" src="{n}.jpg"><span>{html.escape(title)}</span></a>')
parts.append('</div><h2>同版本包含的役满特效修正（R41）</h2><div class="grid">')
for title, n in yakuman: parts.append(f'<a href="{n}"><img loading="lazy" src="{n}"><span>{html.escape(title)}</span></a>')
parts.append('</div><p>段位规则：段位分 = 顺位分 + (终局点数 − 起始点数) ÷ 1000；顺位分默认四麻半庄 +80/+40/0、东风 +40/+20/0，三麻半庄 +100/0、东风 +50/0，末位按段位表减分；跌破 0 降一级，初心不降。</p>')
(OUT / 'index.html').write_text(''.join(parts), 'utf8')
files = sorted(p for p in OUT.iterdir() if p.is_file() and p.name != 'manifest.json')
manifest = [dict(file=p.name, bytes=p.stat().st_size, sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in files]
(OUT / 'manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=1) + '\n', 'utf8')
print(json.dumps(dict(files=len(files), bytes=sum(m['bytes'] for m in manifest))))
