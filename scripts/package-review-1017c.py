"""Package new Unity stills and add original audio to the existing short clip."""
from pathlib import Path
import json,shutil,sys
R=Path(__file__).resolve().parent.parent;S=R/'build/review-1017c/render';O=R/'dist/preview-20261017';O.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(R/'tools/video-inspector'));import av
names=['wait-down-5','yakuman-ready-sound']+[f'qr-{style}-{width}'for width in [1132,1280,1688]for style in ['white','themed']]
for name in names:shutil.copyfile(S/(name+'.png'),O/(name+'.png'))
shutil.copyfile(R/'build/review-1017b/render/wait-selected-discard.png',O/'wait-before-5.png')
shutil.copyfile('C:/Users/Rtyu3221/AppData/Local/Temp/codex-clipboard-425b611e-1ea1-441b-806d-78693652aba6.png',O/'qr-size-reference.png')
sound=R/'unity/Assets/Resources/Majsoul/Audio/effect_yakumanready.wav';shutil.copyfile(sound,O/sound.name)
movie=O/'yakuman-ready-with-sound.mp4'
with av.open(str(O/'yakuman-certain.mp4'))as video,av.open(str(sound))as audio,av.open(str(movie),'w',options={'movflags':'+faststart'})as result:
    visual=result.add_stream_from_template(video.streams.video[0]);track=result.add_stream('aac',rate=44100);track.layout='stereo';track.bit_rate=128000;packets=[]
    for packet in video.demux(video.streams.video[0]):
        if packet.dts is not None:packet.stream=visual;packets.append(packet)
    resample=av.AudioResampler(format='fltp',layout='stereo',rate=44100)
    for frame in audio.decode(audio=0):
        for converted in resample.resample(frame):packets.extend(track.encode(converted))
    for converted in resample.resample(None):packets.extend(track.encode(converted))
    packets.extend(track.encode(None));packets.sort(key=lambda p:float(p.dts*p.time_base))
    for packet in packets:result.mux(packet)
with av.open(str(movie))as check:
    print('Existing effect clip with native cue:',len(check.streams.video),'video,',len(check.streams.audio),'audio stream')
previous=O/'previous-1017b.html'
if not previous.exists():shutil.copyfile(O/'index.html',previous)
def figure(name,label):return '<figure><a href="'+name+'.png" target="_blank"><img loading="lazy" src="'+name+'.png"></a><figcaption>'+label+'</figcaption></figure>'
qr=figure('qr-white-1132','A · 黑白码：按参考图尺寸，保持正方形')+figure('qr-themed-1132','B · 暖白底＋深蓝码点＋细金框（美化预览）')
wait=figure('wait-before-5','移动前')+figure('wait-down-5','移动后：牌与张数下移 5px，面板位置和样式保持原样')
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 · 音效与二维码预览</title><style>body{margin:0;background:#121c2d;color:#e8edf5;font:16px/1.8 "Microsoft YaHei",sans-serif}main{max-width:1550px;margin:auto;padding:26px}h1,h2,a{color:#efd0a0}h1{font-size:27px}h2{font-size:21px}p{color:#bbc8de}.note{padding:12px 18px;background:#1e2d45;border-left:3px solid #cfaa6b}.grid{display:grid;grid-template-columns:1fr 1fr;gap:22px}figure{margin:0;background:#1a2a40;border-radius:6px;overflow:hidden}img,video{display:block;width:100%}figcaption{padding:10px 15px}audio{width:100%;max-width:700px}video{max-width:1100px}details{margin:14px 0}a:hover{opacity:.9}@media(max-width:850px){.grid{grid-template-columns:1fr}main{padding:16px}}</style><main><h1>云间日麻 · 本次效果预览</h1><p class="note">本轮只编译、截图和试听，没有生成 APK、提交 Git 或运行单元测试。点击效果图查看原图。</p><h2>役满确定：原音效</h2><p>已从雀魂 APK 提取专用音效 effect_yakumanready，时长约 1.33 秒，随“役满确定”播放一次，沿用桌面音效开关及音量。</p><audio controls preload="metadata" src="effect_yakumanready.wav"></audio><p>下面沿用已录制的役满确定短片，配上本次原音效；点击播放即可试听。</p><video controls playsinline loop preload="metadata" poster="yakuman-ready-sound.png" src="yakuman-ready-with-sound.mp4"></video><h2>二维码：尺寸与配色对比</h2><p>按你提供的大小调整为 580 × 580 设计像素。A 保持黑白；B 只修改配色并加外框，暂作美化预览。两版都保持完整方形码点、三个定位图形和四模块以上留白，不覆盖中间内容。</p><div class="grid">'''+qr+'''</div><p>以上两版均已从 Unity 实际截图恢复出相同邀请信息。配色方案仍需手机实扫确认，不把截图解码结果当作真机通过率。</p><details><summary>查看其他尺寸与原参考图</summary><div class="grid">'''+figure('qr-white-1280','A · 1280 × 720')+figure('qr-themed-1280','B · 1280 × 720')+figure('qr-white-1688','A · 1688 × 780')+figure('qr-themed-1688','B · 1688 × 780')+figure('qr-size-reference','你提供的大小参考')+'''</div></details><p>设计依据：<a href="https://www.qrcode.com/en/howto/code.html/index.html" target="_blank">DENSO WAVE 四模块留白说明</a>、<a href="https://www.qrcode.com/en/faq.html" target="_blank">配色与可读性说明</a>。</p><h2>听牌面板内容下移 5px</h2><div class="grid">'''+wait+'''</div><p><a href="previous-1017b.html">查看上一轮完整预览</a></p></main></html>'''
(O/'index.html').write_text(page,'utf8')
(O/'review-1017c.json').write_text(json.dumps({'screenshots':names,'qrSide':580,'waitContentOffset':5,'qrThemeIsPreviewOnly':True,'sound':'effect_yakumanready','newEffectRecording':False,'unitTestsRun':False,'apkBuilt':False,'gitCommitted':False},indent=2)+'\n','utf8')
print(O/'index.html')
