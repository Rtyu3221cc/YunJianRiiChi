"""Package a few actual Unity stills and a one-second create-room transition."""
from pathlib import Path
import json
import shutil
import sys

ROOT=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(ROOT/'tools/video-inspector'))
import av
from PIL import Image,ImageDraw,ImageFont

SOURCE=ROOT/'build/review-1016b/render'
OUTPUT=ROOT/'dist/preview-20261017'
OUTPUT.mkdir(parents=True,exist_ok=True)
reference=ROOT/'build/review-1016b/reference/user-lantern-reference.png'
if reference.is_file():
    original=Image.open(reference).convert('RGB')
    with Image.open(SOURCE/'home-session-redpoint.png')as home:current=home.crop((1395,0,1502,124)).convert('RGB')
    # Align whole lantern bodies at equal scale; never reposition the badge
    # inside either image. Reference gold body (28,30), 72px; render (25,26), 63px.
    scale=63/72;ox=25-28*scale;oy=26-30*scale
    original=original.transform((107,124),Image.Transform.AFFINE,(1/scale,0,-ox/scale,0,1/scale,-oy/scale),Image.Resampling.BICUBIC)
    comparison=Image.new('RGB',(720,430),'#152034');font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',23)
    for x,image,label in ((20,original,'原版（公告）'),(380,current,'调整后（牌局）')):
        comparison.paste(image.resize((300,348),Image.Resampling.NEAREST),(x,58));ImageDraw.Draw(comparison).text((x,12),label,font=font,fill='#efd0a0')
    comparison.save(SOURCE/'redpoint-comparison.png')
labels={
 'chi-panel-1280':'吃牌候选背景：白线已去除',
 'wait-on-1688':'听牌 ON：开关位于 AI 托管上方，牌面保持手牌上方居中',
 'wait-off-1688':'听牌 OFF：点击收起',
 'home-session-redpoint':'有进行中的牌局时，牌局灯笼显示原版红金感叹号',
 'redpoint-comparison':'红点位置和大小：对齐灯笼后与原版对照',
 'join-scan-button':'扫码与确认：相同大小的黄色按钮',
 'invite-ready-qr':'准备室分享：二维码保持在弹窗以内',
 'invite-table-qr':'牌桌星标：邀请好友二维码入口',
 'invalid-qr-dialog':'错误二维码：中号提示弹窗',
 'scanner':'摄像头／相册扫码界面：未开启提示位于取景框正中',
 'gallery-joined-room':'实际图片识别后加入隔离演示房间',
}
for name in labels:shutil.copyfile(SOURCE/(name+'.png'),OUTPUT/(name+'.png'))
shutil.copyfile(SOURCE/'gallery-flow.txt',OUTPUT/'gallery-flow.txt')
movie=OUTPUT/'create-room-transition.mp4'
frames=sorted((SOURCE/'create-frames').glob('frame-*.png'))
if movie.is_file() and movie.stat().st_mtime>=max(frame.stat().st_mtime for frame in frames):
    decoded=len(frames)
    print('Reusing unchanged transition video; only refreshing stills.')
else:
    container=av.open(str(movie),'w',options={'movflags':'+faststart'})
    stream=container.add_stream('libx264',rate=30);stream.width,stream.height=1688,780;stream.pix_fmt='yuv420p';stream.options={'crf':'18','preset':'fast'}
    for i,path in enumerate(sorted((SOURCE/'create-frames').glob('frame-*.png'))):
     with Image.open(path)as picture:frame=av.VideoFrame.from_image(picture.convert('RGB'))
     frame.pts=i
     for packet in stream.encode(frame):container.mux(packet)
    for packet in stream.encode():container.mux(packet)
    container.close()
    with av.open(str(movie))as check:decoded=sum(1 for _ in check.decode(video=0))
print('Actual Unity transition:',decoded,'frames')
sections=''.join('<figure><a href="'+name+'.png" target="_blank"><img src="'+name+'.png" alt="'+label+'"></a><figcaption>'+label+'</figcaption></figure>'for name,label in labels.items())
music=ROOT/'unity/Assets/Resources/Majsoul/Lobby/Audio/lobby_autumn.mp3'
if music.is_file():
 shutil.copyfile(music,OUTPUT/'lobby_autumn.mp3')
 music_section='<p>大厅背景音乐：金秋送爽。</p><audio controls preload="metadata" src="lobby_autumn.mp3"></audio>'
else:music_section='<p class="pending">BGM 待补齐：现有 APK 未内置“金秋送爽”，等待原曲来源确认。</p>'
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 · 2026-10-17 修改预览</title><style>body{margin:0;background:#121c2d;color:#e8edf5;font:16px/1.8 "Microsoft YaHei",sans-serif}main{max-width:1500px;margin:auto;padding:26px}h1,h2,a{color:#efd0a0}h1{font-size:27px}p{color:#bbc8de}.note,.pending{padding:12px 18px;background:#1e2d45;border-left:3px solid #cfaa6b}.grid{display:grid;grid-template-columns:1fr 1fr;gap:22px}figure{margin:0;background:#1a2a40;border-radius:6px;overflow:hidden}img,video{display:block;width:100%}figcaption{padding:10px 15px}audio{width:100%;max-width:650px}video{max-width:1100px}a:hover{opacity:.9}@media(max-width:850px){.grid{grid-template-columns:1fr}main{padding:16px}}</style><main><h1>云间日麻 · 本地修改预览</h1><p class="note">实际 Unity 渲染，使用隔离演示房间与合成牌面。本轮仅编译和视觉预览，没有运行单元测试、没有生成 APK、没有提交或推送 Git。点击截图可查看原图。</p><h2>创设房间转场</h2><p>大厅元素淡出，创建面板淡入。以下为按动画时钟采样的约 1 秒实际 Unity 画面。</p><video controls playsinline loop muted preload="metadata" src="create-room-transition.mp4"></video><h2>大厅音乐</h2>'''+music_section+'''<h2>关键画面</h2><div class="grid">'''+sections+'''</div><p>相册识别共用路径已在编辑器中实际解码图片并加入隔离房间；Android 系统相册选择与手机摄像头需要后续真机演练。预览图中的二维码属于演示房间。</p></main></html>'''
(OUTPUT/'index.html').write_text(page,'utf8')
(OUTPUT/'preview.json').write_text(json.dumps({'date':'2026-10-17','screenshots':list(labels),'transitionFrames':decoded,'unitTestsRun':False,'apkBuilt':False,'gitCommitted':False,'cameraOnAndroidVerified':False,'musicReady':music.is_file()},ensure_ascii=False,indent=2)+'\n','utf8')
print(OUTPUT/'index.html')
