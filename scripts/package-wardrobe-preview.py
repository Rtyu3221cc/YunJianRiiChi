"""Package actual Unity captures as a local review gallery and H.264 video."""
from pathlib import Path
import json
import shutil
import sys
from PIL import Image, ImageDraw, ImageFont

R = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(R / 'tools/video-inspector'))
import av

source = R / 'build/wardrobe-preview-1025/render'
out = R / 'dist/previews/2026-10-25-wardrobe'
out.mkdir(parents=True, exist_ok=True)
record = json.loads((source / 'capture.json').read_text('utf8'))
video = out / 'wardrobe-walkthrough.mp4'
with av.open(str(video), 'w', options={'movflags': '+faststart'}) as container:
    stream = container.add_stream('libx264', rate=record['fps'])
    first = Image.open(source / 'frames/00000.jpg')
    stream.width, stream.height = first.size
    stream.pix_fmt = 'yuv420p'
    stream.options = {'crf': '19', 'preset': 'medium'}
    for index in range(record['frames']):
        with Image.open(source / 'frames' / (f'{index:05d}.jpg')) as im:
            frame = av.VideoFrame.from_image(im.convert('RGB'))
            frame.pts = index
            for packet in stream.encode(frame):
                container.mux(packet)
    for packet in stream.encode():
        container.mux(packet)

shots = [
    ('01-wardrobe-cloth', '装扮页面'), ('02-cloth-chigua', '吃瓜桌布预览'),
    ('03-cloth-default', '默认桌布预览'), ('04-tile-back', '新春牌背预览'),
    ('05-tile-face', '猫咪牌面预览'), ('06-head-frame', '猫耳头像框预览'),
    ('07-background', '大厅背景预览'), ('08-equipped', '保存后的使用中状态'),
    ('09-lobby-applied', '应用后的大厅'), ('10-table-applied', '应用后的牌桌')]
for key, _ in shots:
    shutil.copyfile(source / (key + '.png'), out / (key + '.png'))
font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 25)
sheet = Image.new('RGB', (1688, (384 + 52) * 3), '#15151d')
draw = ImageDraw.Draw(sheet)
for index, (key, label) in enumerate([shots[i] for i in [0, 2, 3, 4, 5, 9]]):
    x, y = index % 2 * 844, index // 2 * 436
    draw.text((x + 20, y + 12), label, font=font, fill='#efd2a0')
    with Image.open(source / (key + '.png')) as im:
        im = im.convert('RGB');im.thumbnail((844, 384));sheet.paste(im, (x, y + 52))
sheet.save(out / 'overview.jpg', quality=94)
cards = '\n'.join(f'<figure><a href="{key}.png"><img src="{key}.png" alt="{label}" loading="lazy"></a><figcaption>{label}</figcaption></figure>' for key, label in shots)
html = f'''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 · 装扮预览</title>
<style>body{{margin:0;background:#15131b;color:#ece7df;font:16px/1.7 "Microsoft YaHei",sans-serif}}main{{max-width:1400px;margin:auto;padding:28px}}h1{{font-size:28px;color:#efd2a0}}p{{color:#bab2a8}}video{{width:100%;border:1px solid #665238;border-radius:10px}}section{{display:grid;grid-template-columns:repeat(auto-fit,minmax(440px,1fr));gap:22px}}figure{{margin:0;background:#24212b;border-radius:10px;overflow:hidden}}img{{display:block;width:100%}}figcaption{{padding:12px 18px}}a{{color:inherit}}.tag{{color:#efd2a0;border:1px solid #665238;border-radius:20px;padding:5px 12px;font-size:14px}}</style>
<main><span class="tag">开发预览 · 2026-10-25</span><h1>云间日麻 · 装扮</h1><p>以下画面与视频来自 Unity 编辑器实际渲染。已接入桌布、牌背、牌面、头像框、大厅背景的选择、预览与保存；多方案槽位和重命名未制作。动态特效、资源下载与多人装备同步留待下一阶段。</p><p>本轮未运行正式测试，未生成 APK，未提交或上传 Git。视频中的牌桌使用固定公开展示场景；没有进行手机实测。</p><video controls preload="metadata" poster="01-wardrobe-cloth.png" src="wardrobe-walkthrough.mp4"></video><h1>效果图</h1><section>{cards}</section></main></html>'''
(out / 'index.html').write_text(html, 'utf8')
shutil.copyfile(source / 'capture.json', out / 'capture.json')
print('Preview gallery:', out)
print('Video:', video.stat().st_size, 'bytes;', record['frames'], 'frames;', record['frames'] / record['fps'], 'seconds')
