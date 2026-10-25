"""Package actual captures, per-category clips and original-reference comparisons."""
from pathlib import Path
import base64
import html
import io
import json
import shutil
import sys
from PIL import Image, ImageDraw, ImageFont

R = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(R / 'tools/video-inspector'))
import av

source = R / 'build/wardrobe-preview-r2/render'
out = R / 'dist/previews/2026-10-25-wardrobe-r2'
out.mkdir(parents=True, exist_ok=True)
report = json.loads((source / 'capture.json').read_text('utf8'))
fps = report['fps']

def encode_video(path, first, end):
    if '--pages-only' in sys.argv:return
    with av.open(str(path), 'w', options={'movflags': '+faststart'}) as c:
        stream = c.add_stream('libx264', rate=fps)
        with Image.open(source / 'frames' / f'{first:05d}.jpg') as im:
            stream.width, stream.height = im.size
        stream.pix_fmt = 'yuv420p'
        stream.options = {'crf': '18', 'preset': 'medium'}
        for index in range(first, end):
            with Image.open(source / 'frames' / f'{index:05d}.jpg') as im:
                frame = av.VideoFrame.from_image(im.convert('RGB'));frame.pts = index - first
                for packet in stream.encode(frame):c.mux(packet)
        for packet in stream.encode():c.mux(packet)

encode_video(out / 'wardrobe-all-categories.mp4', 0, report['frames'])
print('Full walkthrough encoded', report['frames'] / fps, 'seconds', flush=True)
for section in report['sections']:
    encode_video(out / (section['key'] + '.mp4'), section['start'], section['end'])
    print('Category video:', section['key'], flush=True)
dynamic = [s for s in report['sections'] if s['key'] in ['stick', 'riichiEffect', 'winEffect', 'callout', 'hand']]
encode_video(out / 'dynamic-decorations.mp4', dynamic[0]['start'], dynamic[-1]['end'])
for shot in report['shots']:
    shutil.copyfile(source / (shot['key'] + '.png'), out / (shot['key'] + '.png'))

def native_frame(file, time, target):
    with av.open(str(file)) as c:
        for frame in c.decode(video=0):
            if float(frame.pts * frame.time_base) >= time:
                im = frame.to_image().transpose(Image.Transpose.ROTATE_90)
                if im.width < im.height:im = im.transpose(Image.Transpose.ROTATE_90)
                im.save(target);return

native_frame(Path('D:/MyWorkSpace/video/1024/装扮配置页面.mp4'), 1.4, out / 'reference-config.png')
native_frame(Path('D:/MyWorkSpace/video/1024/桌布预览.mp4'), .85, out / 'reference-cloth.png')
font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 27)
for filename, first, second, title in [
    ('compare-ui.jpg', 'reference-config.png', '01-overview.png', '页签、分类框、高亮与滚动区'),
    ('compare-cloth.jpg', 'reference-cloth.png', 'cloth-0.png', '桌布与牌张预览条')]:
    sheet = Image.new('RGB', (1688, 1644), '#16131d');draw = ImageDraw.Draw(sheet)
    for row, (key, label) in enumerate([(first, '原版参考视频'), (second, '本轮 Unity 实际画面')]):
        y = row * 822;draw.text((22, y + 12), label + ' · ' + title, font=font, fill='#efd2a0')
        with Image.open(out / key) as im:
            im = im.convert('RGB');im.thumbnail((1688, 768));sheet.paste(im, (0, y + 54))
    sheet.save(out / filename, quality=95)

hero_keys = ['01-overview', 'cloth-0', 'hand-0', 'stick-1', 'riichiEffect-0', 'callout-0']
sheet = Image.new('RGB', (1688, 1308), '#16131d');draw = ImageDraw.Draw(sheet)
titles = {s['key']: s['title'] for s in report['shots']}
for index, key in enumerate(hero_keys):
    x, y = index % 2 * 844, index // 2 * 436;draw.text((x + 16, y + 12), titles[key], font=font, fill='#efd2a0')
    with Image.open(out / (key + '.png')) as im:
        im = im.convert('RGB');im.thumbnail((844, 384));sheet.paste(im, (x, y + 52))
sheet.save(out / 'overview.jpg', quality=94)

catalog = json.loads((R / 'unity/Assets/Resources/Majsoul/Wardrobe/catalog.json').read_text('utf8'))
for key in ['music-riichi', 'music-8bit']:
    shutil.copyfile(R / ('unity/Assets/Resources/Majsoul/Wardrobe/Audio/' + key + '.mp3'), out / (key + '.mp3'))

style = '''body{margin:0;background:#15131b;color:#eee8df;font:16px/1.7 "Microsoft YaHei",sans-serif}*{box-sizing:border-box}main{max-width:1600px;margin:auto;padding:clamp(12px,3vw,32px)}h1,h2{color:#efd2a0}h1{font-size:28px}h2{font-size:23px;margin-top:34px}p{color:#c6beb1}video,img{display:block;width:100%;max-width:100%}video{border:1px solid #705637;border-radius:9px;background:#090a0e}nav{display:flex;flex-wrap:wrap;gap:8px;margin:20px 0}nav a,.play{padding:6px 12px;border:1px solid #705637;border-radius:20px;text-decoration:none;color:#efd2a0}section{scroll-margin-top:12px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,520px),1fr));gap:18px}figure{margin:0;background:#24212b;border-radius:8px;overflow:hidden}figcaption{padding:10px 15px}a{color:#efd2a0}audio{width:100%;max-width:600px}details{margin:24px 0}summary{cursor:pointer;color:#efd2a0}button{background:transparent;font:inherit;cursor:pointer}'''
nav = ''.join(f'<a href="#{s["key"]}">{html.escape(s["title"])}</a>' for s in report['sections'])

def jpg_data(path):
    with Image.open(path) as im:
        im = im.convert('RGB');im.thumbnail((1440, 1700));b = io.BytesIO();im.save(b, 'JPEG', quality=91)
        return 'data:image/jpeg;base64,' + base64.b64encode(b.getvalue()).decode()

def make_document(portable=False):
    image_cache = {}
    def image_src(key):
        if not portable:return key
        if key not in image_cache:image_cache[key] = jpg_data(out / key)
        return image_cache[key]
    video_src = 'data:video/mp4;base64,' + base64.b64encode((out / 'wardrobe-all-categories.mp4').read_bytes()).decode() if portable else 'wardrobe-all-categories.mp4'
    cards = []
    for section in report['sections']:
        key = section['key'];shots = [s for s in report['shots'] if s['key'].startswith(key + '-')]
        content = ''
        for shot in shots:
            key_image = shot['key'] + '.png'
            img = f'<img loading="lazy" src="{image_src(key_image)}" alt="{html.escape(shot["title"])}">'
            if not portable:img = f'<a href="{key_image}">{img}</a>'
            content += f'<figure>{img}<figcaption>{html.escape(shot["title"])}</figcaption></figure>'
        clip = f'<button class="play" onclick="playAt({section["start"]/fps})">播放本类预览</button>' if portable else f'<video controls preload="none" poster="{key}-list.png" src="{key}.mp4"></video>'
        cards.append(f'<section id="{key}"><h2>{html.escape(section["title"])}</h2>{clip}<div class="grid">{content}</div></section>')
    compare = ''.join(f'<figure><img loading="lazy" src="{image_src(key)}"><figcaption>{title}</figcaption></figure>' for key, title in [('compare-ui.jpg', '原版与本轮：页签、分类框和选中高亮'), ('compare-cloth.jpg', '原版与本轮：桌布和牌张预览条')])
    tail = ''.join(f'<figure><img loading="lazy" src="{image_src(key+".png")}"><figcaption>{title}</figcaption></figure>' for key, title in [('09-lobby-applied', '秋深背景应用后的大厅'), ('10-table-applied', '头像框贴合修正后的牌桌')])
    audio = '<h2>立直音乐原资源</h2><p>视频为静音录屏，可在这里单独试听原曲。</p>'
    for key, title in [('music-riichi', '原曲'), ('music-8bit', '八位音色')]:
        src = 'data:audio/mpeg;base64,' + base64.b64encode((out / (key + '.mp3')).read_bytes()).decode() if portable else key + '.mp3'
        audio += f'<p>{title}</p><audio controls preload="none" src="{src}"></audio>'
    return f'''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻装扮 · 第二轮预览</title><style>{style}</style><main><h1>云间日麻装扮 · 第二轮预览</h1><p>11 类、22 件样例资源。下面的图片和视频均来自 Unity 编辑器实际渲染。猫爪是原骨骼、原动作和纹理；立直棒是 APK 模型；鸣牌指示保留原粒子模块。</p><p>修正：原版页签重叠、高亮尺寸和对齐、扩大滚动区、分类框拼接缝、原版图标、桌布预览条、背景缩略图比例和头像框内间隙。原版与本轮对照图在页面下方。</p><p>本轮未运行正式测试，未生成 APK，未提交或上传 Git。动态装扮目前为独立预览，牌局触发、联机装备同步和下载留待后续；不是全部装扮资源的完整收录。部分可选源文件未包含在基础 APK 中，使用了工程保存的官方版本清单补齐并记录来源。</p><nav>{nav}</nav><video id="walkthrough" controls preload="metadata" poster="{image_src('01-overview.png')}" src="{video_src}"></video>{''.join(cards)}<h2>应用效果</h2><div class="grid">{tail}</div><h2>原版与本轮对照</h2><div class="grid">{compare}</div>{audio}</main><script>function playAt(t){{var v=document.getElementById('walkthrough');v.currentTime=t;v.play();v.scrollIntoView({{behavior:'smooth'}});}}</script></html>'''

(out / 'index.html').write_text(make_document(), 'utf8')
(out / '手机版完整预览.html').write_text(make_document(True), 'utf8')
shutil.copyfile(source / 'capture.json', out / 'capture.json')
import zipfile
with zipfile.ZipFile(out.parent / '云间日麻-装扮第二轮预览.zip', 'w', zipfile.ZIP_DEFLATED, compresslevel=1) as archive:
    for file in out.iterdir():
        if file.name == '手机版完整预览.html':continue
        if file.is_file():archive.write(file, file.name)
print('Gallery:', out, flush=True)
print('Portable HTML:', (out / '手机版完整预览.html').stat().st_size, 'bytes', flush=True)
