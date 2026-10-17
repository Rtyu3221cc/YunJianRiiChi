"""Package only the requested short motion/settlement previews and actual stills."""
from pathlib import Path
from fractions import Fraction
import json,shutil,sys
R=Path(__file__).resolve().parent.parent;S=R/'build/review-1017d/render';O=R/'dist/preview-20261017';O.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(R/'tools/video-inspector'));import av,numpy as np
from PIL import Image
images=['lobby-academy','create-two-minute','create-unlimited','idle-taken','manual-ai','idle-returned','offline-ai','response-before-land','response-after-land','double-yakuman','score-first-rank','final-first-rank','all-ai-terminal']+['qr-compact-'+s for s in ['white','warm','tight','gold']]
for name in images:shutil.copyfile(S/(name+'.png'),O/(name+'.png'))
voice=json.loads((R/'unity/Assets/Resources/Majsoul/SettlementVoice/manifest.json').read_text('utf8'))
voice_paths={x['key']:x['resource']for x in voice['clips']}
def resource(key,kind):
    stem=R/'unity/Assets/Resources'/(voice_paths[key]if kind=='voice'else'Majsoul/Audio/'+key)
    return next(p for p in stem.parent.glob(stem.name+'.*')if p.suffix.lower()in ['.mp3','.wav','.ogg','.aiff'])
def audio(path):
    pieces=[];resampler=av.AudioResampler(format='fltp',layout='stereo',rate=44100)
    with av.open(str(path))as source:
        for frame in source.decode(audio=0):
            for converted in resampler.resample(frame):pieces.append(converted.to_ndarray())
    for converted in resampler.resample(None):pieces.append(converted.to_ndarray())
    return np.concatenate(pieces,axis=1)
timeline=json.loads((S/'audio-timeline.json').read_text('utf8'))['events']
for name,fps in [('riichi-depth',20),('response-order',20),('double-yakuman',12)]:
    frames=sorted((S/name).glob('frame-*.png'));movie=O/(name+'-review.mp4');mix=None
    if name=='double-yakuman':
        mix=np.zeros((2,int(np.ceil(len(frames)/fps*44100))),np.float32)
        for cue in timeline:
            data=audio(resource(cue['key'],cue['kind']));start=round(cue['at']*44100);count=min(data.shape[1],mix.shape[1]-start)
            gain=.8 if cue['kind']=='voice'else.65
            if cue['key']=='hule_fan_out':gain=.35
            if count>0:mix[:,start:start+count]+=data[:,:count]*gain
        peak=np.max(np.abs(mix));mix*=.95/max(1,peak)
    with av.open(str(movie),'w',options={'movflags':'+faststart'})as target:
        with Image.open(frames[0])as picture:w,h=picture.size
        video=target.add_stream('libx264',rate=fps);video.width=w;video.height=h;video.pix_fmt='yuv420p';video.options={'crf':'19','preset':'fast'};packets=[]
        for i,path in enumerate(frames):
            with Image.open(path)as picture:frame=av.VideoFrame.from_image(picture.convert('RGB'))
            frame.pts=i;packets.extend(video.encode(frame))
        packets.extend(video.encode())
        if mix is not None:
            sound=target.add_stream('aac',rate=44100);sound.layout='stereo';sound.bit_rate=160000
            for start in range(0,mix.shape[1],1024):
                frame=av.AudioFrame.from_ndarray(np.ascontiguousarray(mix[:,start:start+1024]),format='fltp',layout='stereo');frame.sample_rate=44100;frame.pts=start;frame.time_base=Fraction(1,44100);packets.extend(sound.encode(frame))
            packets.extend(sound.encode())
        packets.sort(key=lambda p:float(p.dts*p.time_base))
        for packet in packets:target.mux(packet)
    with av.open(str(movie))as check:print(name,len(frames),'frames',round(len(frames)/fps,2),'seconds, audio streams',len(check.streams.audio))
shutil.copyfile(resource('rank1','effect'),O/'rank1.wav')
old=np.array(Image.open(S/'qr-original-source.png').convert('RGB'));new=np.array(Image.open(S/'qr-source-white.png').convert('RGB'));tight=np.array(Image.open(S/'qr-source-tight.png').convert('RGB'))
def margin(image):return int(np.where(image[:,:,0]<80)[1].min())/image.shape[1]*580
stats={'before':margin(old),'safe':margin(new),'tight':margin(tight)};stats.update(safeReduction=1-stats['safe']/stats['before'],tightReduction=1-stats['tight']/stats['before'])
print('Displayed QR margin:',stats)
report=json.loads((R/'build/review-1017d/fixtures/rehearsal-report.json').read_text('utf8'));qr=json.loads((S/'qr-readability.json').read_text('utf8'))
oldpage=O/'previous-1017c.html'
if not oldpage.exists():shutil.copyfile(O/'index.html',oldpage)
def image(name,label):return '<figure><a href="'+name+'.png" target="_blank"><img loading="lazy" src="'+name+'.png"></a><figcaption>'+label+'</figcaption></figure>'
def movie(name,label,sound=False):return '<figure><video controls playsinline '+(''if sound else'muted ')+'preload="metadata" src="'+name+'-review.mp4"></video><figcaption>'+label+'</figcaption></figure>'
sections=movie('riichi-depth','对家横打立直牌：牌本体使用牌桌深度，前排牌河可遮挡')+movie('response-order','黄框／黄三角在落牌交接后出现')
qr_images=image('qr-compact-white','A · 保留至少四模块留白的黑白版（截图识别通过）')+image('qr-compact-warm','B · 收紧留白的暖白深蓝版（截图识别通过）')+image('qr-compact-tight','C · 留白约减 40% 的实验版；约三模块，不建议默认采用')+image('qr-compact-gold','D · 透明底金色实验版（反色识别配置修正后，同一截图已通过解码；手机实扫待验证）')
body='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 · 本轮九项预览</title><style>body{margin:0;background:#121c2d;color:#e8edf5;font:16px/1.8 "Microsoft YaHei",sans-serif}main{max-width:1550px;margin:auto;padding:26px}h1,h2,a{color:#efd0a0}h1{font-size:27px}h2{font-size:21px}p{color:#bbc8de}.note{padding:12px 18px;background:#1e2d45;border-left:3px solid #cfaa6b}.grid{display:grid;grid-template-columns:1fr 1fr;gap:22px}figure{margin:0;background:#1a2a40;border-radius:6px;overflow:hidden}img,video{display:block;width:100%}figcaption{padding:10px 15px}audio{width:100%;max-width:700px}.mainclip{max-width:1280px}details{margin:14px 0}@media(max-width:850px){.grid{grid-template-columns:1fr}main{padding:16px}}</style><main><h1>云间日麻 · 本轮九项预览</h1><p class="note">实际 Unity 截图、原始动画与合成演示牌面；完整 AI 对局使用隔离房间运行。本轮未打包 APK、提交 Git 或运行单元测试。</p><h2>1、3 · 立直遮挡与响应顺序</h2><div class="grid">'''+sections+'''</div><div class="grid">'''+image('response-before-land','落牌前：三角和黄框隐藏')+image('response-after-land','落牌后：三角和黄框出现')+'''</div><h2>2 · 二维码留白与透明金色对比</h2><p>整体仍为 580 × 580。当前码保留四模块后，画面中的留白约减少 '''+str(round(stats['safeReduction']*100))+'''%；C 版约减少 '''+str(round(stats['tightReduction']*100))+'''%，会低于四模块。透明金色版仅作外观对比，未作为默认方案。</p><div class="grid">'''+qr_images+'''</div><p>官方要求至少<a href="https://www.qrcode.com/en/howto/code.html/index.html" target="_blank">四模块留白</a>。截图解码不能代替手机摄像头实扫通过率。</p><h2>4、8 · 二倍役满结算及第一位音效</h2><p>引擎实际计分的“四暗刻单骑”：二倍役满、64000 点。下方短片包含役种、点数、分数结算和终局排名，配原音频；两次第一位出现时各播放一次 rank1。</p><div class="mainclip">'''+movie('double-yakuman','二倍役满结算完整短片（点击播放声音）',True)+'''</div><audio controls preload="metadata" src="rank1.wav"></audio><h2>5 · 两分接管与无限制</h2><p>默认“两分接管”，连续 120 秒没有真人操作才接管；选牌等操作随现有轮询更新活动时间，轮询本身不计入操作。“无限制”关闭闲置接管，玩家断线／离开时仍由 AI 衔接。牌桌按钮统一为“AI接管”。</p><div class="grid">'''+image('create-two-minute','默认：两分接管')+image('create-unlimited','无限制：没有两分钟闲置接管')+'''</div><h2>6 · 原版头像遮罩</h2><div class="grid">'''+image('idle-taken','闲置超时被接管：显示原版暗色遮罩和闪电')+image('manual-ai','主动开启 AI 接管：不显示遮罩')+image('offline-ai','离开／断线：显示遮罩')+image('idle-returned','重新进入：恢复头像及真人操作')+'''</div><h2>7 · 博士帽入口</h2><p>按原版右侧顺序加入博士帽，相机移到下一格；博士帽进入现有规则说明。</p>'''+image('lobby-academy','右侧：设置 → 帮助 → 博士帽 → 相机')+'''<h2>9 · 全 AI 终局</h2><p>修复了无人在线停在单局结算、终局确认后自动重开的问题。三人离线接管场和四人在线全 AI 场均已实跑到终局，确认后保持终局，房主可手动再来一场。</p>'''+image('all-ai-terminal','实际全 AI 对局所得终局状态：等待手动“再来一场”')+'''<p><a href="previous-1017c.html">上一轮预览</a></p></main></html>'''
(O/'index.html').write_text(body,'utf8');(O/'review-1017d.json').write_text(json.dumps({'qrMargins':stats,'qrDecode':qr,'activity':report['activity'],'aiMatches':report['matches'],'rank1Cues':sum(c['key']=='rank1'for c in timeline),'unitTestsRun':False,'apkBuilt':False,'gitCommitted':False},ensure_ascii=False,indent=2)+'\n','utf8')
print(O/'index.html')
