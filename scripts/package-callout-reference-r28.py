"""Package real Unity recordings alongside the user's original reference clips."""
from pathlib import Path
import hashlib, html, json, shutil, sys

R = Path(__file__).resolve().parent.parent
sys.path[:0] = [str(R / 'tools/video-inspector'), str(R / 'tools/asset-inspector')]
import av
import numpy as np
from PIL import Image, ImageDraw, ImageFont

P = R / 'build/review-r28/capture'
O = R / 'dist/previews/2026-11-03-callout-reference-r28'
O.mkdir(parents=True, exist_ok=True)
FONT = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 26)
SMALL = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 20)
CLIPS = json.loads((P / 'clips.json').read_text('utf8'))['clips']
NAMES = {'mpzs_shandian': '雷驰电掣', 'mpzs_22event': '咕噜咕噜', 'effect_zhishi': '普通引导', 'mpzs_23winter': '众星捧月'}
MOVIE = P / 'feedback-r28-silent.mp4'

def digest(p):
    h = hashlib.sha256()
    with p.open('rb') as f:
        for data in iter(lambda: f.read(1024 * 1024), b''):
            h.update(data)
    return h.hexdigest()

def pick(key, variant='crowded', seat=1):
    return next(c for c in CLIPS if c['id'] == key and c['variant'] == variant and c['seat'] == seat)

def crop_at(image, center, width, size):
    x, y = center
    height = width * size[1] / size[0]
    return image.crop((round(x-width/2), round(y-height/2), round(x+width/2), round(y+height/2))).resize(size, Image.Resampling.LANCZOS)

def load_crop(path, start, duration, center, width, size, rotate=False):
    images = []
    with av.open(str(path)) as container:
        stream = container.streams.video[0]
        container.seek(max(0, int(start / stream.time_base)), stream=stream, backward=True)
        target = start
        for frame in container.decode(video=0):
            at = float(frame.time or 0)
            if at + .0001 < target:
                continue
            image = frame.to_image()
            if rotate and image.height > image.width:
                image = image.transpose(Image.Transpose.ROTATE_90)
            while at + .0001 >= target and len(images) < round(duration * 30):
                images.append(crop_at(image, center, width, size))
                target = start + len(images) / 30
            if len(images) >= round(duration * 30):
                break
    assert images, path
    while len(images) < round(duration * 30):
        images.append(images[-1].copy())
    return images

def unity_crop(c, size, width=280):
    return load_crop(MOVIE, c['start']/30, (c['end']-c['start'])/30,
                     (c['center']['x'], 900-c['center']['y']), width, size)

chapters = []
count = 0
stills = {}

def pair(title, left_title, right_title, left, right, image_name):
    global count
    chapters.append({'title': title, 'at': count/30})
    for i in range(90):
        image = Image.new('RGB', (1280, 720), '#111722')
        draw = ImageDraw.Draw(image)
        draw.text((24, 8), title, font=FONT, fill='#ebd2a7')
        draw.text((20, 48), left_title, font=SMALL, fill='white')
        draw.text((660, 48), right_title, font=SMALL, fill='white')
        image.paste(left[min(i, len(left)-1)], (0, 82))
        image.paste(right[min(i, len(right)-1)], (640, 82))
        draw.text((20, 664), '各自的镜头与粒子阶段；局部放大观察位置、方向和遮挡。无录音。', font=SMALL, fill='#bfc7d6')
        if i == 45:
            image.save(O / image_name, quality=94)
            stills[image_name] = title
        count += 1
        yield image

def frames():
    global count
    for key, filename, seat, start, center in [
        ('mpzs_shandian', '雷驰电掣.mp4', 3, 1.5, (963, 499)),
        ('mpzs_22event', '鸣牌特效位置.mp4', 2, 1.65, (1275, 270)),
    ]:
        reference = load_crop(Path('D:/MyWorkSpace/video/1024') / filename, start, 3, center, 420, (640, 560), True)
        current = unity_crop(pick(key, 'reference', seat), (640, 560))
        yield from pair(NAMES[key]+' · 原版录像 / 本轮实际运行', '原版 · '+filename, '本轮 · Unity 实际运行', reference, current, 'reference-'+key+'.jpg')
    prior = R / 'dist/previews/2026-11-03-callout-target-r27/callout-target-r27.mp4'
    c = pick('mpzs_shandian')
    before = load_crop(prior, 80/30, 3, (c['center']['x'], 900-c['center']['y']), 280, (640, 560))
    after = unity_crop(c, (640, 560))
    yield from pair('三筒紧邻四索 · 上轮 / 本轮', '上轮 R27 · 底光抬高且强制覆盖', '本轮 R28 · 底光在下，邻牌正常遮挡', before, after, 'before-after-lightning.jpg')
    before = unity_crop(pick('mpzs_22event', 'before', 2), (640, 560))
    after = unity_crop(pick('mpzs_22event', 'reference', 2), (640, 560))
    yield from pair('咕噜咕噜 · 调整前 / 本轮', '调整前 · 强制覆盖渲染', '本轮 · 保留向上漂浮，正常深度遮挡', before, after, 'before-after-bubble.jpg')
    for key, title in NAMES.items():
        chapters.append({'title': title+' · 四个座位', 'at': count/30})
        views = [unity_crop(pick(key, seat=seat), (640, 300)) for seat in range(4)]
        for i in range(90):
            image = Image.new('RGB', (1280, 720), '#111722')
            draw = ImageDraw.Draw(image)
            draw.text((20, 6), title+' · 四个座位，待鸣三筒／邻牌四索', font=FONT, fill='#ebd2a7')
            for seat in range(4):
                x, y = seat%2*640, 48+seat//2*324
                image.paste(views[seat][i], (x,y+24))
                draw.text((x+12,y), '座位 '+str(seat)+('（编辑器观察自己的公开弃牌）' if seat==0 else ''), font=SMALL, fill='white')
            draw.text((20, 696), '座位 0 仅为方向检查；实战不允许鸣自己的弃牌。', font=SMALL, fill='#bfc7d6')
            if i==45:
                image.save(O / ('four-'+key+'.jpg'), quality=94)
                stills['four-'+key+'.jpg'] = title+' · 四个座位'
            count += 1
            yield image

with av.open(str(O / 'callout-reference-r28.mp4'), 'w', options={'movflags': '+faststart'}) as container:
    stream = container.add_stream('libx264', rate=30)
    stream.width, stream.height, stream.pix_fmt = 1280, 720, 'yuv420p'
    stream.options = {'crf': '18', 'preset': 'fast', 'threads': '2'}
    for i, image in enumerate(frames()):
        frame = av.VideoFrame.from_image(image)
        frame.pts = i
        for packet in stream.encode(frame):
            container.mux(packet)
    for packet in stream.encode():
        container.mux(packet)

shutil.copyfile(MOVIE, O / 'all-four-full.mp4')
shutil.copyfile(P / 'final-checks.json', O / 'runtime-checks.json')
shutil.copyfile(P / 'final-geometry.json', O / 'geometry.json')

pixel_rows = []
for line in (P / 'occlusion-points.jsonl').read_text('utf8').splitlines():
    row = json.loads(line)
    stem = 'occlusion-'+row['id']+'-seat'+str(row['seat'])
    on = np.array(Image.open(P / (stem+'-on.png')), dtype=np.int16)
    off = np.array(Image.open(P / (stem+'-off.png')), dtype=np.int16)
    x, y = row['center']['x'], 900-row['center']['y']
    h = max(3, row['width']/2)
    box = (round(x-h), round(y-h*.5), round(x+h), round(y+h*.5))
    left, top, right, bottom = box
    diff = np.abs(on[top:bottom,left:right]-off[top:bottom,left:right])
    entry = {'id': row['id'], 'seat': row['seat'], 'box': box, 'maxDelta': int(diff.max()), 'meanDelta': float(diff.mean()), 'changedPixelsOver2': int(np.any(diff>2,axis=2).sum())}
    if row['id'] != 'mpzs_23winter':
        assert entry['maxDelta'] <= 2, entry
    pixel_rows.append(entry)
pixel_report = {'method': 'Same-frame below-face-centered particle renderers on/off, interior of neighbouring s4. Winter contains 3D rings extending above the face; its values are diagnostic, not a zero-difference assertion.', 'planarAssertions': 12, 'rows': pixel_rows}
(O / 'neighbour-checks.json').write_text(json.dumps(pixel_report, ensure_ascii=False, indent=2), 'utf8')

notes = '''# 鸣牌指示原版录像对照 R28

日期：2026-11-03。只改本地源码、输出实际预览，未生成 APK、未提交或推送 Git。

本轮重新查看 D:/MyWorkSpace/video/1024/雷驰电掣.mp4 和 鸣牌特效位置.mp4。原版底部光效在牌面下方，气泡向上漂浮。R27 为使底板投影靠近牌面而反转雷电局部高度轴，造成火花方向也反转；本轮恢复粒子的正确高度方向。底光仍以待鸣弃牌模型原点挂载，不以屏幕位置挪动。

主要遮挡错误来自鸣牌粒子被强制设成 ZTest Always。底光可以透过更近的四索，视觉上像是在指邻牌。本轮使用正常 LEqual 深度测试，保留既有强度和牌面减淡参数。雷驰电掣、咕噜咕噜、众星捧月使用该修正；普通引导原有 LEqual 保持，仅复核预览。四款均保留原始网格、粒子、贴图、UV 动画和播放曲线，没有重画特效或更改全局坐标。

解码脚本 DesktopMgr.ShowChiPengEffect (build/fate-r19/lua/decrypted/readable/desktopmgr.lua:865) 将 effect_pai_canchi 放在 lastqipai.model 下，普通弃牌局部位置和旋转归零。这里只据原版录像和该挂载关系检查当前适配；没有声称全客户端逐像素一致。录像镜头、背景和随机粒子阶段不同。没有使用这两段录像推断众星捧月的完整原版外观。

24 秒对照视频：0–3 秒雷电原版／当前；3–6 秒气泡原版／当前；6–9 秒三筒四索上轮／当前；9–12 秒气泡前后；12–24 秒四款各自四个座位。另附 60 秒实际 Unity 完整画面。均为无录音预览。

验证：20 段实际录制片段；四款×四座位检查实际待鸣牌、原始上升方向、所有材质深度状态和响应结束清理。原版两款及普通引导共 12 个邻牌字面内部同帧像素检查，底部粒子无超过 2 灰阶的变化。众星捧月包含抬起的三维光环，其在牌上方的部分允许正常可见，数据保留在 neighbour-checks.json，不伪称整款完全不影响邻牌像素。座位 0 只是编辑器观察公开弃牌，不代表允许鸣自己的牌。
'''
(O / '说明.md').write_text(notes, 'utf8')
(R / 'docs/鸣牌指示录像对照优化-r28-2026-11-03.md').write_text(notes, 'utf8')

buttons = ''.join('<button data-time="'+str(c['at'])+'">'+html.escape(c['title'])+'</button>' for c in chapters)
pictures = ''.join('<details><summary>'+html.escape(title)+'</summary><img loading="lazy" src="'+name+'"></details>' for name,title in stills.items())
page = '''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 · 鸣牌录像对照 R28</title><style>*{box-sizing:border-box}body{margin:0;background:#111722;color:#f3e4ce;font:16px/1.7 system-ui,"Microsoft YaHei",sans-serif}main{max-width:1360px;margin:auto;padding:20px}h1{font-size:25px}p{color:#c0c9d7}video,img{display:block;width:100%;background:#080b12}video{max-height:78vh}nav{display:grid;grid-template-columns:1fr 1fr;gap:8px;margin:16px 0}button,summary{cursor:pointer;padding:10px;background:#202c41;border:1px solid #59617a;border-radius:5px;color:#efd0a0;font:inherit;text-align:left}details{margin:14px 0}a{color:#e5be7b}@media(max-width:700px){main{padding:10px}nav{grid-template-columns:1fr}}</style><main><h1>鸣牌指示 · 原版录像对照 R28</h1><p>按你提供的两段录像优化：底光留在牌下方，麻将正常遮挡光效，气泡与火花保持向上。保留牌面减淡和原始粒子。未生成 APK，未提交代码。</p><video id="v" controls playsinline preload="metadata" poster="reference-mpzs_shandian.jpg" src="callout-reference-r28.mp4"></video><nav>__BUTTONS__</nav><p>前 6 秒为原版／当前，其后有前后对比和四款四座位预览。镜头和随机粒子阶段不同；无录音。</p>__PICTURES__<details><summary>60 秒 Unity 实际完整画面</summary><video controls playsinline preload="none" src="all-four-full.mp4"></video></details><p><a href="说明.md">修改与验证说明</a> · <a href="runtime-checks.json">运行检查</a> · <a href="neighbour-checks.json">邻牌像素检查</a> · <a href="verification.json">文件验证</a></p></main><script>const v=document.querySelector('#v');document.querySelectorAll('[data-time]').forEach(b=>b.onclick=()=>{v.currentTime=+b.dataset.time;v.play()});</script></html>'''
(O / 'index.html').write_text(page.replace('__BUTTONS__',buttons).replace('__PICTURES__',pictures), 'utf8')
verification = {'date': '2026-11-03', 'frames': count, 'fps': 30, 'seconds': count/30, 'chapters': chapters, 'apkBuilt': False, 'committed': False, 'references': [{'file': str(p), 'sha256': digest(p)} for p in [Path('D:/MyWorkSpace/video/1024/雷驰电掣.mp4'), Path('D:/MyWorkSpace/video/1024/鸣牌特效位置.mp4')]], 'files': [{'name': p.name, 'bytes': p.stat().st_size, 'sha256': digest(p)} for p in [O/'callout-reference-r28.mp4',O/'all-four-full.mp4']], 'source': [{'path': str(p.relative_to(R)), 'sha256': digest(p)} for p in [R/'unity/Assets/Scripts/SourcePlaybackContracts.cs',R/'unity/Assets/Scripts/WardrobeResponseIndicator.cs']]}
(O/'verification.json').write_text(json.dumps(verification, ensure_ascii=False, indent=2), 'utf8')
assert count == 720
print('Packaged', count, 'frames;', len(CLIPS), 'actual Unity clips;', len(pixel_rows), 'pixel diagnostics', flush=True)
