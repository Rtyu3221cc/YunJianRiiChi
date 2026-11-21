"""Package the verified local review movie, original voice cues and audit notes."""
from pathlib import Path
from fractions import Fraction
import hashlib, html, json, shutil, sys
R=Path(__file__).resolve().parent.parent;B=R/'build/review-r25';C=B/'capture'
D=R/'dist/previews/2026-11-03-phone-feedback-r25'
sys.path[:0]=[str(R/'tools/video-inspector'),str(R/'tools/asset-inspector')]
import av
import numpy as np
from PIL import Image,ImageDraw,ImageFont

def read(p):return json.loads(p.read_text('utf8'))
def sha(p):
    h=hashlib.sha256()
    with p.open('rb')as f:
        for chunk in iter(lambda:f.read(1048576),b''):h.update(chunk)
    return h.hexdigest()

report=read(C/'movie-checks.json');voice=read(B/'voice-audit.json');coverage=read(B/'wardrobe-coverage.json')
runtime=read(C/'study-voices-and-baseline.json')
tail=read(C/'tail-checks.json');assert tail['frames']==420
extra=read(C/'extra-checks.json');assert extra['models']==61 and extra['riichiCases']==44 and extra['calloutCases']==16
assert report['frames']>1000 and len(report['chapters'])>=20 and report['cues']
assert not report['apkBuilt']and not report['committed']and not(C/'failed.txt').exists()
assert voice['characters']==122 and voice['clipsDecoded']==10980 and not voice['missing']and not voice['decodeErrors']
assert runtime['characters']==122 and runtime['voiceLookups']==10980
D.mkdir(parents=True,exist_ok=True)
duration=report['frames']/report['fps'];rate=48000
audio=np.zeros((2,round(duration*rate)),dtype=np.float32)
for cue in report['cues']:
    path=Path(cue['source']).resolve();assert path.is_relative_to((R/'unity/Assets').resolve())and path.suffix=='.mp3'
    resample=av.AudioResampler(format='fltp',layout='stereo',rate=rate);pieces=[]
    with av.open(str(path))as stream:
        for frame in stream.decode(audio=0):pieces.extend(f.to_ndarray()for f in resample.resample(frame))
    pieces.extend(f.to_ndarray()for f in resample.resample(None));signal=np.concatenate(pieces,axis=1)
    start=round(cue['time']*rate);count=min(signal.shape[1],audio.shape[1]-start)
    if count>0:audio[:,start:start+count]+=signal[:,:count]*.85
np.clip(audio,-1,1,out=audio)
assert float(np.max(np.abs(audio)))>.01
video=D/'feedback-r25.mp4'
with av.open(str(C/'feedback-r25-joined-silent.mp4'))as source,av.open(str(video),'w',options={'movflags':'+faststart'})as target:
    vs=target.add_stream_from_template(source.streams.video[0]);ss=target.add_stream('aac',rate=rate);ss.layout='stereo';ss.bit_rate=160000;packets=[]
    for packet in source.demux(video=0):
        if packet.dts is not None:packet.stream=vs;packets.append(packet)
    for start in range(0,audio.shape[1],1024):
        frame=av.AudioFrame.from_ndarray(np.ascontiguousarray(audio[:,start:start+1024]),format='fltp',layout='stereo');frame.sample_rate=rate;frame.pts=start;frame.time_base=Fraction(1,rate);packets.extend(ss.encode(frame))
    packets.extend(ss.encode(None));packets.sort(key=lambda p:float(p.dts*p.time_base)if p.dts is not None else 0)
    for packet in packets:target.mux(packet)

# Decode the delivered movie, retain one actual game frame per chapter and make QA sheets.
thumbs=D/'frames';thumbs.mkdir(exist_ok=True)
targets={min(report['frames']-1,round((chapter['at']+.45)*report['fps'])):i for i,chapter in enumerate(report['chapters'])}
saved={};decoded=0
with av.open(str(video))as stream:
    assert len(stream.streams.video)==1 and len(stream.streams.audio)==1
    for index,frame in enumerate(stream.decode(video=0)):
        if index in targets:
            i=targets[index];im=frame.to_image();im.thumbnail((960,540));im.save(thumbs/f'{i+1:02}.jpg',quality=91);saved[i]=im.copy()
        decoded+=1
assert decoded==report['frames'],(decoded,report['frames'])
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',16)
for offset in range(0,len(report['chapters']),12):
    sheet=Image.new('RGB',(1260,4*276),'#111a28');draw=ImageDraw.Draw(sheet)
    for j,i in enumerate(range(offset,min(offset+12,len(report['chapters'])))):
        if i not in saved:continue
        im=saved[i].copy();im.thumbnail((420,236));x=j%3*420;y=j//3*276;sheet.paste(im,(x,y));draw.text((x+6,y+238),f'{i+1:02} {report["chapters"][i]["title"][:24]}',font=font,fill='#edce91')
    sheet.save(B/f'qa-movie-{offset//12+1}.jpg',quality=92)

checks=len(report['checks'])+len(tail['checks'])+len(extra['checks'])
verification=dict(extraChecks=len(extra['checks']),extraModels=61,fourSeatRiichiCases=44,fourSeatCalloutCases=16,editorDiagnosticsRecordedSeparately=len(report.get('editorDiagnostics',[]))+len(extra.get('editorDiagnostics',[])),date='2026-11-03',frames=decoded,fps=report['fps'],seconds=duration,checks=checks,
                  voiceCharacters=122,sourceClipsDecoded=10980,runtimeVoiceLookups=10980,
                  voiceAliases=15,apkBuilt=False,gitCommitted=False,gitPushed=False,
                  originalVoiceCueCount=len(report['cues']),audioMethod='Original character clips mixed at recorded Unity trigger frame times; not system microphone recording',
                  movie=dict(file=video.name,bytes=video.stat().st_size,sha256=sha(video)),
                  androidDeviceTested=False,originalClientPixelParityVerified=False)
(D/'verification.json').write_text(json.dumps(verification,ensure_ascii=False,indent=1)+'\n','utf8')
for src,name in [(B/'voice-audit.json','voice-audit.json'),(B/'wardrobe-coverage.json','wardrobe-coverage.json'),(C/'movie-checks.json','runtime-checks.json'),(C/'extra-checks.json','extra-checks.json'),(C/'tail-checks.json','tail-checks.json')]:shutil.copy2(src,D/name)
missing='\n'.join(f'| {x["sourceId"]} | {x["name"]} | `{x["sourceKey"]}` |'for x in coverage['riichiMissing'])
note=f'''# 实机反馈 R25：源码修正、实际预览与未完工作核对

日期：2026-11-03。本轮只修改源码并制作桌面 Unity 实际运行视频，没有生成 APK、提交或推送 Git。维护工程仍是 `D:/MyWorkSpace/YunJianRiichi_Codex`，正式版本号保持 0.6.19 / 67；现有 0.6.19 APK 不包含本轮 R25 修改。预览使用合成公开局面和隔离资源目录，不读取手机存档。

## 本轮结果

1. 弹窗支持点击外部关闭，按层级先关闭子窗；下载在后台继续。检查覆盖人物／装扮下载、父子资源窗口、设置、音乐、下拉及邀请二维码。开局资源准备可以收起并重新展开，不自动确认或替代资源。
2. 宿舍选择装扮立即调用持久化保存，下载并选用完成后同样自动保存。移除保存按钮；左侧分类视口从 652 增至 816。运行检查包含离开后重新进入，以及清空内存选择缓存后从 PlayerPrefs 重新读取。预览使用独立公司／产品名的偏好设置，避免改变用户实际存档。
3. 全部 122 名角色，各 90 段必需动作／役种／限界语音，共 10,980 段本地源音频逐段解码通过；10,980 次 Windows 实际下载资源包加载也全部通过。一姬、二阶堂美树、藤田佳奈、三上千织、相原舞使用旧文件名 `fan_rich`／`fan_drich`／`fan_tumo`，现在按同一角色兼容查找，不拿其他角色声音替代。
4. 立直棒按实体网格的边界归一化，避免影子和光晕把剑体压小；保留全透明模型的回退，并限制异常小实体造成过度放大。视频展示「胜利誓约」。
5. 装扮尚未全部补齐。固定公开表中的 57 款立直特效，目前接入 11 款，仍缺 46 款；静态定义完成 314 条映射，另有 3 款多层动态桌布未实现。和牌为 59 款可定位原生定义加默认款，过期缺资源定义单列；原生资源组中的别名／新资源不直接算作可选装扮。
6. 本轮额外定向解码并校验 `block_qipai.lua` 和 `actionliqi.lua`。修正「竖式」被误解成固定地面特效的问题：原版 Shu／Heng 都挂到携牌模型，区别在手部动画。旧式特效采用原模型偏移、方向及四秒回收；保留镰刀已确认的固定世界转向及四变体。鸣牌指示按原模型坐标挂载，保留已有可读性材质调整。当前 11 款立直及 4 款正式鸣牌指示在视频中逐项展示；尚未完成原版录像逐像素对照。
7. 二维码窗口标题显示动态房间号，视频示例为「房间号 123456」，不会将所有房间写死成同一个号码。
8. 听牌窗口不再每轮自己出牌时自动打开。本局首次选中打出后可听牌的牌时自动展开一次，其后以手动开合为准，新一局重新计次。
9. 听牌窗口使用手牌布局基准作为锚点；拖动某张牌不会把窗口带走。原版手内换序、拖动阈值及再次点击出牌机制保留。
10. 满贯以下直接报役种，满贯及以上显示角色横幅。标准荣和路径在修正前也能显示横幅；这次修复了空计分对象阻断 `limit` 回退的情况，并在完整播放路径中验证。没有手机上那一局的原始结果，不能认定此前手机缺横幅只有这一个原因。
11. 汪次郎的默认手部改为已有的原始狗爪，显式选择橘猫手仍优先生效；视频展示狗爪动作。
12. 宿舍保存按钮已移除，换装和下载并选用都即时持久化；左侧全部装扮分类共用加高后的视口。人物和装扮下载确认按钮同时保留上一轮 70→105 的 50% 高度调整，显示与点击区域一致。

## 中断是否留下未完成工作

确有未完成的还原工作，但不是已发布文件丢失。R22 完成的重资源拆包、59 款原生和牌和下载流程有对应产物；R23 下载界面修正已进入 0.6.19。历史文档也明确留下了剩余立直特效、动态桌布和部分原生独立资源组。原先「已经使用解码 Lua」主要覆盖输入、和牌／Fate／自摸等流程，不能理解成所有立直及鸣牌分支都已完整迁移。本轮补解码的弃牌控制脚本确认了实际遗漏，并据此修正已接入旧式立直的挂载与生命周期。

现有手部界面按既有范围只显示默认手和橘猫手。角色专属默认手可在运行时解析，未将其余所有原版手部装扮当成已经完成。后续补齐应逐项核对源定义、资源完整性、旧／新 Lua 分支、`root_rot/root_move/root_maque/root_ani`、材质与编译动画，再录制四方向效果，不能只换图标或通用粒子。

## 运行证据与限制

- 本轮视频及补充回归共 {checks} 项检查，另检查 61 款立直棒动画、44 组四座位立直和 16 组四方向鸣牌指示；此前完整 Node 基线 192 项通过。全角色语音的源解码及资源包加载结果分别保存，不混算为真机验证。
- 视频 {duration:.2f} 秒，{decoded} 帧 / {report['fps']} FPS。画面来自实际运行客户端；音轨使用相同角色原始音频，按录制触发帧混合，不是系统麦克风录音。
- 文件：`dist/previews/2026-11-03-phone-feedback-r25/feedback-r25.mp4`；预览页同目录 `index.html`。`verification.json` 记录最终视频大小和 SHA-256。
- 当前目录统计及 46 款缺口：`wardrobe-coverage.json`。全部 122 名角色逐文件摘要、时长及解码幅度：`voice-audit.json`。实际界面回归：`runtime-checks.json`。
- 本轮没有生成 Android 包，未验证手机 GPU／触控／覆盖安装及多人热点。新视觉修正等待用户看视频确认。
- 临时工程只在 `build/review-r25/stage`；角色和装扮测试包复用已验证本地包，原始大包使用硬链接，未重新下载全部客户端。主工程不建立 Library，磁盘终值见最终验证记录。

## 尚未接入的 46 款立直定义

| 原版 ID | 名称 | 资源键 |
|---:|---|---|
{missing}
'''
note_path=R/'docs/实机反馈修正与未完工作核对-r25-2026-11-03.md';note_path.write_text(note,'utf8');(D/'说明.md').write_text(note,'utf8')
links=''.join(f'<button data-time="{c["at"]:.3f}">{i+1:02}　{html.escape(c["title"])}</button>'for i,c in enumerate(report['chapters']))
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 · 实机反馈 R25 预览</title><style>*{box-sizing:border-box}body{margin:0;background:#101725;color:#e8e8ed;font-family:system-ui,"Microsoft YaHei",sans-serif}main{max-width:1480px;margin:auto;padding:24px}h1{font-size:28px}p{color:#bac6da;line-height:1.8}video{width:100%;max-height:78vh;background:#05070d;border:1px solid #3d4b65}nav{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:8px;margin-top:20px}button{border:1px solid #46516a;border-radius:5px;background:#1b2940;color:#e6c28f;text-align:left;padding:12px;font-size:15px;cursor:pointer}a{color:#edc58a}footer{margin-top:22px;line-height:1.9}@media(max-width:800px){main{padding:14px}nav{grid-template-columns:1fr}}</style><main><h1>云间日麻 · 实机反馈 R25 实际运行预览</h1><p>宿舍自动保存、弹窗外部关闭、下载按钮、房间号、听牌窗口、立直棒、满贯横幅及狗爪；逐项展示已接入的立直／鸣牌特效。源码预览，未生成 APK、未提交 Git。</p><video id="movie" controls preload="metadata" src="feedback-r25.mp4"></video><p>角色原始语音按录制触发帧混合；不是麦克风录音。全部 122 名角色已核对。原版装扮尚未齐全：立直特效 11 / 57，另有 3 款动态桌布未接入。</p><nav>__CHAPTERS__</nav><footer><a href="说明.md">12 项修改与未完工作核对</a> · <a href="wardrobe-coverage.json">装扮目录与缺口</a> · <a href="voice-audit.json">全角色语音检查</a> · <a href="runtime-checks.json">运行检查</a> · <a href="verification.json">视频验证</a></footer></main><script>const v=document.querySelector('video');document.querySelectorAll('[data-time]').forEach(b=>b.onclick=()=>{v.currentTime=+b.dataset.time;v.play()});</script></html>'''.replace('__CHAPTERS__',links)
(D/'index.html').write_text(page,'utf8')
print('R25 PREVIEW READY',json.dumps(verification,ensure_ascii=False),flush=True)
