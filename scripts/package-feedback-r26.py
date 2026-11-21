"""Package R26 actual UI/particle video with real UI-triggered voice cues."""
from pathlib import Path
from fractions import Fraction
import hashlib,html,json,shutil,sys
R=Path(__file__).resolve().parent.parent;B=R/'build/review-r26';C=B/'capture';D=R/'dist/previews/2026-11-03-directions-voice-r26'
sys.path[:0]=[str(R/'tools/video-inspector'),str(R/'tools/asset-inspector')]
import av,numpy as np
from PIL import Image,ImageDraw,ImageFont
def read(p):return json.loads(p.read_text('utf8'))
report=read(C/'movie-checks.json');four=read(C/'four-checks.json');directory=read(B/'voice-directory-import.json')
assert not(C/'failed.txt').exists()and report['characters']==122 and report['voiceLookups']==16226
assert four['riichiCases']==44 and four['calloutCases']==16 and not report['apkBuilt']and not report['committed']
D.mkdir(parents=True,exist_ok=True);duration=report['frames']/report['fps'];rate=48000;audio=np.zeros((2,round(duration*rate)),np.float32);used=[]
for cue in report['cues']:
    if cue.get('duration',-1)>=0 and cue['duration']<.04:continue
    path=Path(cue['source']).resolve();assert path.is_relative_to((R/'unity/Assets').resolve())and path.is_file()
    res=av.AudioResampler(format='fltp',layout='stereo',rate=rate);parts=[]
    with av.open(str(path))as source:
        for frame in source.decode(audio=0):parts.extend(f.to_ndarray()for f in res.resample(frame))
    parts.extend(f.to_ndarray()for f in res.resample(None));signal=np.concatenate(parts,axis=1);limit=round(cue['duration']*rate)if cue.get('duration',-1)>=0 else signal.shape[1];start=round(cue['time']*rate);count=min(signal.shape[1],limit,audio.shape[1]-start)
    if count>0:audio[:,start:start+count]+=signal[:,:count]*.85;used.append(cue)
np.clip(audio,-1,1,out=audio);assert used and float(np.abs(audio).max())>.01
video=D/'feedback-r26.mp4'
with av.open(str(C/'feedback-r26-silent.mp4'))as source,av.open(str(video),'w',options={'movflags':'+faststart'})as target:
    vs=target.add_stream_from_template(source.streams.video[0]);ss=target.add_stream('aac',rate=rate);ss.layout='stereo';ss.bit_rate=160000;packets=[]
    for p in source.demux(video=0):
        if p.dts is not None:p.stream=vs;packets.append(p)
    for start in range(0,audio.shape[1],1024):
        f=av.AudioFrame.from_ndarray(np.ascontiguousarray(audio[:,start:start+1024]),format='fltp',layout='stereo');f.sample_rate=rate;f.pts=start;f.time_base=Fraction(1,rate);packets.extend(ss.encode(f))
    packets.extend(ss.encode(None));packets.sort(key=lambda p:float(p.dts*p.time_base)if p.dts is not None else 0)
    for p in packets:target.mux(p)
targets={min(report['frames']-1,round((x['at']+.6)*30)):i for i,x in enumerate(report['chapters'])};frames=D/'frames';frames.mkdir(exist_ok=True);saved={};count=0
with av.open(str(video))as source:
    assert len(source.streams.video)==len(source.streams.audio)==1
    for n,f in enumerate(source.decode(video=0)):
        if n in targets:
            i=targets[n];im=f.to_image();im.thumbnail((960,540));im.save(frames/f'{i+1:02}.jpg',quality=91);saved[i]=im.copy()
        count+=1
assert count==report['frames']
audio_samples=0;peak=0
with av.open(str(video))as source:
    for f in source.decode(audio=0):audio_samples+=f.samples;peak=max(peak,float(np.abs(f.to_ndarray()).max()))
assert peak>.01 and abs(audio_samples/rate-duration)<.1
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',16)
for offset in range(0,len(report['chapters']),12):
    sheet=Image.new('RGB',(1260,1104),'#101a28');draw=ImageDraw.Draw(sheet)
    for j,i in enumerate(range(offset,min(offset+12,len(report['chapters'])))):
        if i not in saved:continue
        im=saved[i].copy();im.thumbnail((420,236));x=j%3*420;y=j//3*276;sheet.paste(im,(x,y));draw.text((x+5,y+238),report['chapters'][i]['title'][:25],font=font,fill='#eaca92')
    sheet.save(B/f'qa-final-{offset//12+1}.jpg',quality=92)
for src,name in [(C/'movie-checks.json','runtime-checks.json'),(C/'four-checks.json','four-seat-checks.json'),(C/'direction-study.json','direction-study.json'),(B/'voice-directory-import.json','voice-directory.json')]:shutil.copy2(src,D/name)
verification=dict(date='2026-11-03',frames=count,fps=30,seconds=duration,voicePages=122,voiceRows=16226,riichiFourSeatCases=44,calloutFourSeatCases=16,checks=len(report['checks'])+len(four['checks']),actualUiVoiceCues=len(used),audioPeak=peak,audioDecoded=True,buttonHeight=84,promptFontSize=36,apkBuilt=False,gitCommitted=False,gitPushed=False,originalClientVideoParityVerified=False,movie=dict(file=video.name,bytes=video.stat().st_size,sha256=hashlib.sha256(video.read_bytes()).hexdigest()),audioMethod='Actual CharacterVoices playback callbacks from voice-page controls; original clips mixed at recorded start/stop frames')
(D/'verification.json').write_text(json.dumps(verification,ensure_ascii=False,indent=1)+'\n','utf8')
note=f'''# R26：方向复核、下载弹窗和角色语音分页

日期：2026-11-03。仅源码与实际 Unity 预览，未生成 APK、未提交或推送 Git；现有 0.6.19 APK 不包含 R25／R26 修改。R25 预览和证据保留。

## 1. 立直特效方向

11 款已接入特效按原配置 `LiQiPai`（横式）／`Dapai`（竖式）以及独立方向配置分别复核。本轮对横式默认挂载增加坐标衔接的半圈补偿，覆盖幻影、火焰、叮～和苍火；竖式、猫过留痕／滚雪球的独立配置、问心之镰固定世界方向及四变体继续保留。视频提供幻影、火焰和叮～的上一轮／本轮实际播放对照，另逐项显示其余已接入款。

依据是已解码 `Block_QiPai` 的挂载、手部类型、根旋转和生命周期，以及当前牌面坐标／原始预制件。源粒子速度、贴图 UV 和编译动画未重写。方向结论属于脚本与资源框架校准，并通过四座位实绘检查；尚未取得原版客户端对应款式的实际播放录像，不能将其写成原版录像逐帧核验通过。`direction-study.json` 保留前后粒子中心和挂载分类数据，供继续比对。

## 2. 雷驰电掣

原始预制件还带有独立展示用的根姿态，叠加牌面挂载后使底板竖起。对鸣牌指示的运行挂载将根制作姿态归零，底板回到牌底。以 `DesktopMgr.ShowChiPengEffect` 指向最后一张可鸣弃牌的关系为基础，分别核对四方向；没有通过移动整张牌桌或修改全局粒子方向来掩盖问题。视频包含雷驰电掣前后对照及其他 3 款指示。

## 3. 下载按钮

人物和装扮单件确认弹窗的 3 个按钮，高度由 105 改成 84（缩小 20%），字体统一为 36。显示和点击范围一致。其他资源工具栏和删除弹窗不套用这组值。视频使用真实缺失角色资源的界面并从本机 HTTP 下载后选用，避免把已安装的 0.0 MB 提示当作未下载演示。

## 4. 角色语音界面与上轮遗漏

上轮确实只展示了语音播放，语音页签仍是占位，没有完成角色语音分页。这次接通“人物档案→语音”，使用原版列表背景、播放／停止图标、名称及台词；支持触摸／滚轮滚动，切换条目、再次点击停止，离开分页或切换角色时停止。打开语音分页临时降低背景音乐，不改用户保存的音量值。

元数据取自固定配置 `voice/sound`，按原版 `UI_Visit.Page_Sound` 排除 hide=1 条目，覆盖 122 个角色、16,226 条可见定义。原始 `lobby/` 前缀与当前包内平铺文件名采用同角色回退解析，没有替换成其他角色声音。运行检查逐条通过资源包加载；视频单独展示三上千织和一姬的真实界面操作，并用实际播放／停止回调记录音轨，未用独立背景音替代按钮功能。

之前 10,980 段检查针对每角色 90 段必需动作／役种／限界语音，并不等同于语音分页完成；本轮补上更广的完整页面目录和交互。音轨由同一原始音频按界面触发帧混合，并按停止事件截断；不是系统麦克风录音。

## 仍未完成的旧工作

- 固定公开配置中的 46 条未接入立直定义（包含过期项）和 3 款多层动态桌布仍未补齐；本轮没有把数量问题写成完成。
- 原生清单中的别名／新资源组仍需与公开定义逐项核对，不能直接当作新的可选装扮。
- 所有特效的原版实机录像逐帧视觉对照尚未完成；本轮方向提供实际对照视频待审核。
- 此前手机上某局没有满贯横幅的唯一原因仍未确定；已验证标准路径与空计分对象回退，不扩大结论。
- 上一轮语音分页没有完成，也没有录制该分页；本轮已补上实际操作。其他 R25 修正保留，没有重新打包进旧 APK。

## 验证和产物

视频 {duration:.2f} 秒，{count} 帧 / 30 FPS；本轮共 {verification['checks']} 项界面／补充检查，44 组立直四座位、16 组鸣牌四方向，122 页／16,226 条语音定义运行加载。手机 GPU、触控与原版录像像素一致性仍未实测。

- 预览：`dist/previews/2026-11-03-directions-voice-r26/index.html`。
- 视频：同目录 `feedback-r26.mp4`；大小和 SHA-256 见 `verification.json`。
- 界面与加载检查：`runtime-checks.json`；四向检查：`four-seat-checks.json`；方向数据：`direction-study.json`。
- 语音源配置与图标记录：`voice-directory.json`。解码后的 `ui_visit.lua` 保留在本地 `build/review-r26/lua`，没有加入发布目录。
- 复用精简工程与资源硬链接；C／D 盘终值另记，不建立主工程 Library。
'''
(R/'docs/方向复核与角色语音分页-r26-2026-11-03.md').write_text(note,'utf8');(D/'说明.md').write_text(note,'utf8')
buttons=''.join(f'<button data-time="{c["at"]:.3f}">{i+1:02}　{html.escape(c["title"])}</button>'for i,c in enumerate(report['chapters']))
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 R26 · 方向复核与角色语音分页</title><style>*{box-sizing:border-box}body{margin:0;background:#111927;color:#eee;font-family:system-ui,"Microsoft YaHei",sans-serif}main{max-width:1480px;padding:24px;margin:auto}p{line-height:1.8;color:#b9c5d8}video{width:100%;max-height:78vh;background:#06090e}nav{display:grid;grid-template-columns:1fr 1fr;gap:8px;margin-top:18px}button{padding:12px;background:#1d2b42;color:#ebc88d;border:1px solid #44536f;border-radius:5px;text-align:left;cursor:pointer;font-size:15px}a{color:#efca8e}footer{margin-top:22px}@media(max-width:750px){main{padding:12px}nav{grid-template-columns:1fr}}</style><main><h1>云间日麻 R26 · 本轮实际运行预览</h1><p>84 高／36 号字下载按钮；真实人物语音分页与播放／停止；立直方向前后对照和逐款复核；雷驰电掣牌底修正。未生成 APK、未提交代码。</p><video id="v" src="feedback-r26.mp4" controls preload="metadata"></video><p>方向结论基于脚本、源预制件和四向检查，尚未标记为原版录像逐帧一致。音轨使用语音页真实按钮触发的原始音频，按停止事件截断。</p><nav>__BUTTONS__</nav><footer><a href="说明.md">本轮修改及上轮未完成项</a> · <a href="verification.json">验证记录</a> · <a href="runtime-checks.json">真实界面检查</a> · <a href="four-seat-checks.json">四座位检查</a> · <a href="direction-study.json">方向数据</a></footer></main><script>const v=document.querySelector('video');document.querySelectorAll('[data-time]').forEach(b=>b.onclick=()=>{v.currentTime=+b.dataset.time;v.play()});</script></html>'''.replace('__BUTTONS__',buttons)
(D/'index.html').write_text(page,'utf8');print('R26 PREVIEW READY',json.dumps(verification,ensure_ascii=False))
