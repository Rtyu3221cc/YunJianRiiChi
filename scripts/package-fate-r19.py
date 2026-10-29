"""Mux the original effect audio at the recorded trigger times and verify video."""
from fractions import Fraction
from pathlib import Path
import hashlib,json,re,shutil,sys
R=Path(__file__).resolve().parent.parent;B=R/'build/fate-r19';C=B/'capture';O=R/'dist/previews/2026-10-29-fate-r19';O.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(R/'tools/video-inspector'));import av,numpy as np
from PIL import Image,ImageDraw
assert 'FATE R19 STUDY PASS 142' in (B/'study-runtime-queue.log').read_text('utf8',errors='replace')
assert 'FATE R19 MOVIE PASS 5 frames=828' in (B/'movie-runtime-final.log').read_text('utf8',errors='replace')
assert re.search(r'pass\s+185\b',(B/'node-tests.log').read_text('utf8'))
capture=json.loads((C/'capture.json').read_text('utf8'));duration=capture['frames']/capture['fps'];rate=48000
audio=np.zeros((2,round(duration*rate)),dtype=np.float32)
for cue in capture['cues']:
    assert cue['cue']=='ron_25hf'
    parts=[];resample=av.AudioResampler(format='fltp',layout='stereo',rate=rate)
    with av.open(str(R/'unity/Assets/Resources/Majsoul/Audio'/(cue['cue']+'.mp3')))as source:
        for f in source.decode(audio=0):parts.extend(g.to_ndarray()for g in resample.resample(f))
        parts.extend(g.to_ndarray()for g in resample.resample(None))
    signal=np.concatenate(parts,axis=1);start=round(cue['time']*rate);count=min(signal.shape[1],audio.shape[1]-start);audio[:,start:start+count]+=signal[:,:count]*.85
np.clip(audio,-1,1,out=audio)
target=O/'fate-win-r19.mp4'
with av.open(str(C/'fate-r19-silent.mp4'))as source,av.open(str(target),'w',options={'movflags':'+faststart'})as output:
    video=output.add_stream_from_template(source.streams.video[0]);sound=output.add_stream('aac',rate=rate);sound.layout='stereo';sound.bit_rate=160000
    packets=[]
    for packet in source.demux(video=0):
        if packet.dts is not None:packet.stream=video;packets.append(packet)
    for start in range(0,audio.shape[1],1024):
        f=av.AudioFrame.from_ndarray(np.ascontiguousarray(audio[:,start:start+1024]),format='fltp',layout='stereo');f.sample_rate=rate;f.pts=start;f.time_base=Fraction(1,rate);packets.extend(sound.encode(f))
    packets.extend(sound.encode(None));packets.sort(key=lambda p:float(p.dts*p.time_base)if p.dts is not None else 0)
    for packet in packets:output.mux(packet)
qa=B/'video-qa';qa.mkdir(exist_ok=True);wanted=[15,60,112,150,198,246,354,477,570,630,705,795];stills=[];frames=0;last=-1
with av.open(str(target))as movie:
    stream=movie.streams.video[0];assert (stream.width,stream.height)==(1746,810)and stream.average_rate==30
    for f in movie.decode(video=0):
        t=float(f.pts*f.time_base);assert t>last;last=t
        if frames in wanted:
            im=f.to_image();im.save(qa/f'frame-{frames}.png');stills.append((frames,im.copy()))
        frames+=1
assert frames==828
decoded_audio=0;peak=0
with av.open(str(target))as movie:
    for f in movie.decode(audio=0):decoded_audio+=f.samples;peak=max(peak,float(np.max(np.abs(f.to_ndarray()))))
assert decoded_audio>=audio.shape[1] and peak>.05
sheet=Image.new('RGB',(4*480,3*249),'#181b24');draw=ImageDraw.Draw(sheet)
for i,(n,im)in enumerate(stills):im.thumbnail((480,223));x=(i%4)*480;y=(i//4)*249;sheet.paste(im,(x,y+25));draw.text((x+8,y+5),f'{n/30:.2f}s',fill='white')
sheet.save(qa/'contact-sheet.jpg',quality=90);shutil.copy2(qa/'frame-198.png',O/'poster.png')
for name in ['study-checks.txt','movie-checks.txt','capture.json']:shutil.copy2(C/name,O/name)
assets=json.loads((B/'runtime-assets.json').read_text('utf8'))
for row in assets:assert hashlib.sha256((R/row['path']).read_bytes()).hexdigest()==row['sha256']
before=json.loads((B/'catalog-before.json').read_text('utf8'));now=json.loads((R/'unity/Assets/Resources/Majsoul/Wardrobe/catalog.json').read_text('utf8'));assert {i['id']:i for i in before['items']}=={i['id']:i for i in now['items']if i['id']!='ron-fate-hf'}
report=dict(date='2026-10-29',effect='和牌-咒层界・恶念祝祭',sourceId=308041,sourceKey='ron_25hf',luaApk='4.0.16',nativeArt='4.0.51 / 0.16.289',actualUnityRendering=True,androidDeviceTested=False,originalVisualComparison=False,newApk=False,uploaded=False,width=1746,height=810,fps=30,frames=frames,durationSeconds=duration,withSound=True,audio='Original effect audio muxed at the recorded production cue times',allFramesDecoded=True,runtimeChecks=142,movieChecks=5,nodeTests=185,particleLayers=43,meshLayers=114,sourceNodes=213,compiledAnimationChannels=442,sourceClipSeconds=6.483333,sourceCleanupSeconds=6,existingOutfitsPreserved=len(before['items']),assetBytes=sum(a['bytes']for a in assets),sha256=hashlib.sha256(target.read_bytes()).hexdigest(),videoBytes=target.stat().st_size)
(O/'verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n','utf8')
(O/'asset-hashes.json').write_text(json.dumps(assets,indent=1)+'\n','utf8')
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Fate联动和牌特效 · 云间日麻</title><style>body{margin:0;background:#17141c;color:#eee5df;font:16px/1.7 system-ui,"Microsoft YaHei",sans-serif}main{max-width:1500px;margin:auto;padding:24px}video{width:100%;background:#08080b;border-radius:8px}h1{font-size:26px}a{color:#edb887}</style><main><h1>Fate联动 · 咒层界・恶念祝祭</h1><p>云间日麻实际 Unity 画面，27.6 秒：荣和、自摸及演出结束后的役种结算。原始特效音效按实际触发时间合成。</p><video controls playsinline preload="metadata" poster="poster.png" src="fate-win-r19.mp4"></video><p><a href="fate-win-r19.mp4" download>下载视频</a> · <a href="verification.json">检查摘要</a> · <a href="说明.md">实现说明</a></p><p>按解码 Lua 与原生资源参数还原，本轮未做原版录像对照；本轮仅视频，未生成新 APK。</p></main></html>'''
(O/'index.html').write_text(page,'utf8')
doc='''# Fate 联动和牌特效 R19

日期：2026-10-29。用户要求试还原一款 Fate 联动和牌特效并提供视频。本轮新增「咒层界・恶念祝祭」（308041 / ron_25hf），交付实际 Unity 视频，未构建 APK，未上传。

## 来源与移植

从本地原始 Majsoul_4.0.16_MC.apk 继续解码 actionhule、desktopmgr、tools 等 10 份相关 Lua。严格 UTF-8、PKCS7、重新加密字节一致、LuaJIT 语法及格式化 AST 一致检查通过。源包没有此和牌资源组，画面使用已有固定原生清单 4.0.51 / 0.16.289 对应的官方 bundle；声音来自已固定网页清单中该物品的 audio_id。没有下载已发布的云间日麻 APK 来重复验证。

保留 213 个节点、43 层粒子、114 个模型／贴片、原生网格和纹理。通过 Unity Playables 采样原始 fx_skill_079_ron_unity 动画，6.483333 秒、60 FPS、442 组变化通道。原始 Renderer 的特殊绑定在此处是材质属性，显隐由 _Alpha 透明度控制；Renderer.enabled 在该动画中没有变化，未伪造启闭轨迹。

依据 DesktopMgr.setNewHuleEffect 将 root_move 绑定牌位置、root_rot 绑定位置／旋转，root_static 留在原牌桌场景原点。原 Lua 的 -1000 是 Root_Scene 坐标偏移，不能将它误认为独立屏幕背景。结合本项目镜头方向作 180 度世界 Y 轴适配。此款 root_ani、root_rot 为空辅助节点，没有借用其他款的牌面动画。

依据 sargs=3300,1 与 SetPaopaiZtestNew，牌面在 5.3 秒内使用原版 3090 绘制队列并保留深度测试；在同一原单位特效相机内绘制准确的牌面网格，轮询替换模型后重新绑定，结束恢复。ShowHuleEffect 的 2+4 秒销毁上限按原代码保留。

单独实现 YunJian/FateParticle，依据原始 GLES 编译程序核对主贴图、Mask 的 alpha 通道、Distortion 的 green 通道、DMask 的 red 通道、溶解的内外 smoothstep 边缘、ambient 叠加、贴图缩放与滚动、混合及深度状态。以源材质 DEF_* 关键字启用对应分支。已有 R11 等特效着色器不改。

## 检查与范围

142 项实际 Unity 检查通过，包含四座位、模型与粒子数量、动态材质、独立根节点绑定、轮询期间的 3090 队列及到时恢复、六秒销毁、真实公开和牌事件与装扮选择、不重复播放。185 项现有 Node 检查通过。原有 286 个装扮条目逐字段保留。

视频 828 帧、1746×810、30 FPS、27.6 秒，全部逐帧解码且时间戳连续；荣和、自摸以及演出后的役种页检查通过。视频音轨使用原资源音效，并按实际 GameAudio 触发帧合成；不包含现场录音。顶部文字是录制标注。

这是源参数还原的首轮实现，没有原版 Fate 录像逐段对照，也没有 Android 设备验证，不声称像素一致。完整角色源素材与完整角色测试未在精简录制工程中运行；编辑器 Search 初始化索引异常为已知缓存噪声，游戏运行与录制均正常退出。

维护代码与资源保存在 unity/Assets；原始分析与日志在 build/fate-r19。复现顺序：recover-majsoul-input-lua.py --profile fate-r19 → format-recovered-lua.cjs build/fate-r19/lua/decrypted → import-native-effects-r11.py --fate-r19 → register-fate-r19.py → prepare-original-input-stage.py --batch fate-r19 → Unity NativeEffectR11Import.RunFateR19 → FateR19Preview.Study / Movie → retain-fate-r19-import.py → package-fate-r19.py。视频和最终资产保存后清除该批次的临时 Unity Library。
'''
(R/'docs/Fate联动和牌特效-r19-2026-10-29.md').write_text(doc,'utf8');(O/'说明.md').write_text(doc,'utf8')
print('FINAL FATE VIDEO',target,target.stat().st_size,'bytes;',frames,'frames; audio peak',peak)
