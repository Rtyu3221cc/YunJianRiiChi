"""Package the verified slap/contact correction and its actual game recording."""
from pathlib import Path
import hashlib,json,runpy,shutil,sys
R=Path(__file__).resolve().parent.parent;B=R/'build/tsumo-r21';C=B/'capture';O=R/'dist/previews/2026-10-29-tsumo-r21';O.mkdir(parents=True,exist_ok=True)
assert 'TSUMO R21 PASS 268 frames=498' in (B/'runtime-retry.log').read_text('utf8',errors='replace')
report=json.loads((C/'capture.json').read_text('utf8'));assert len(report['timings'])==14 and report['checks']==268
runpy.run_path(str(R/'scripts/mux-recorded-cues.py'))['mux'](C/'tsumo-r21-silent.mp4',report,O/'tsumo-contact-r21.mp4')
sys.path.insert(0,str(R/'tools/video-inspector'));import av
from PIL import Image,ImageDraw
qa=B/'video-qa';qa.mkdir(exist_ok=True);frames=0;last=-1;stills=[]
with av.open(str(O/'tsumo-contact-r21.mp4'))as movie:
    s=movie.streams.video[0];assert (s.width,s.height)==(1746,810)and s.average_rate==30
    for f in movie.decode(video=0):
        assert float(f.time)>last;last=float(f.time)
        if frames in [35,45,55,75,165,225,335,465]:
            im=f.to_image();im.save(qa/f'frame-{frames}.png');stills.append((frames,im))
        frames+=1
assert frames==498
samples=0
with av.open(str(O/'tsumo-contact-r21.mp4'))as movie:
    for f in movie.decode(audio=0):samples+=f.samples
assert samples>=round(16.6*48000)
sheet=Image.new('RGB',(4*440,2*228),'#171a22');draw=ImageDraw.Draw(sheet)
for i,(n,im)in enumerate(stills):im.thumbnail((440,204));x=i%4*440;y=i//4*228;sheet.paste(im,(x,y+24));draw.text((x+5,y+5),f'{n/30:.2f}s',fill='white')
sheet.save(qa/'contact-sheet.jpg',quality=90)
shutil.copy2(qa/'frame-45.png',O/'poster.png')
for name in ['settled-hand-retracting.png','checks.txt','capture.json']:shutil.copy2(C/name,O/name)
meta=B/'stage/unity/Assets/Editor/TsumoContactR21Preview.cs.meta';shutil.copy2(meta,R/'unity/Assets/Editor'/meta.name)
verification=dict(date='2026-10-29',handScale=.48,scenarioCombinations=14,runtimeAssertions=268,frames=498,fps=30,durationSeconds=16.6,width=1746,height=810,allVideoAndAudioDecoded=True,actualUnityRendering=True,androidDeviceTested=False,newApk=False,timings=report['timings'],audio='Bundled tsumo impact and Fate sounds muxed at actual recorded trigger frames',videoBytes=(O/'tsumo-contact-r21.mp4').stat().st_size,sha256=hashlib.sha256((O/'tsumo-contact-r21.mp4').read_bytes()).hexdigest())
(O/'verification.json').write_text(json.dumps(verification,ensure_ascii=False,indent=2)+'\n','utf8')
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>自摸拍桌时落稳 · 云间日麻</title><style>body{margin:0;background:#171a22;color:#eee5df;font:16px/1.7 system-ui,"Microsoft YaHei",sans-serif}main{max-width:1500px;margin:auto;padding:24px}video{width:100%;background:#080a0e;border-radius:8px}h1{font-size:26px}a{color:#edc887}</style><main><h1>自摸拍桌即落稳，随后只收手</h1><p>保留手部缩放 0.48，移除“停顿后再次下落”。16.6 秒：慢放观察触桌与收手，再以正常速度播放完整自摸和 Fate 特效。</p><video controls playsinline preload="metadata" poster="poster.png" src="tsumo-contact-r21.mp4"></video><p><a href="tsumo-contact-r21.mp4" download>下载视频</a> · <a href="verification.json">检查摘要</a></p><p>实际 Unity 渲染；落牌声和 Fate 原音效按实际触发帧合成。本轮未生成 APK。</p></main></html>'''
(O/'index.html').write_text(page,'utf8')
doc='''# 自摸拍桌与收手分离 R21

日期：2026-10-29。修复 R20 视频中自摸牌先低空停顿、再继续下落的问题。保留已确认的手部缩放 0.48、牌尺寸与桌面落点。本轮是源码／实际视频更新，未构建 APK。

## 原因与修复

此前把整个 Zimo 挂点曲线都当成携牌轨迹，并把桌面牌的接管固定到 1.14 秒、手部寿命固定到 1.16 秒。原始挂点后半段还在回到骨架静止姿态，牌跟着播放就出现贴近桌面后缓慢二次下降。

现在按实际解析后的手型读取原版 Zimo 配置，分别处理拍桌声、物理触桌、手部寿命和释放后的特效。触桌取声效标记之后，挂点首次下降到原始牌厚范围的采样帧，按这一帧校准落点；到点立即切换为真实桌面牌，携牌副本隐藏，后续骨架回收只影响手。女性默认手约 0.5 秒触桌，男性／橘猫手约 0.533 秒，具体浮点值见 verification.json。

女性／橘猫配置声效点 0.45 秒、释放／寿命 0.9 秒；男性声效点 0.52 秒、释放 0.8 秒、寿命 0.833 秒。原版 ViewPlayer_Other._Hule 在释放后再延迟 0.1 秒调用特效，所以提前触桌不会让 Fate 特效提前抢在收手中播放。轮询重绘仍将触桌后的桌面牌保持在同一位置，不再次隐藏／下落；落牌声只播放一次。

修改为 SourcePlaybackContracts.HandAction 和 TableEffects 的自摸路径，普通打牌、立直和其他动作继续沿用自己的时序。旧 landTime 对自摸不再被当成拍桌时刻，不再使用统一 1.14／1.16 秒常数。

## 实际验证

14 组组合：四人桌四个方向分别检查女性默认手、男性默认手、橘猫手；另检查三人桌女性手与橘猫手。268 项运行断言覆盖末段连续下降、触桌零跳变、牌尺寸、触桌后持续静止、轮询、手继续收回、原版声效／释放／寿命及完整自摸 → Fate → 役种页。Unity 退出码 0。

新视频 498 帧、1746×810、30 FPS、16.6 秒，视频及音轨全量解码通过。顶部文字为录制标注；音轨按实际触发帧合成原始落牌声和 Fate 音效。未做 Android 设备验证。

第一次临时工程导入遇到本机内存不足，在测试运行前崩溃；仅关闭该工程及其导入进程，使用 -job-worker-count 2 -assetImportWorkerCount 1 -force-d3d11 复用缓存后完成验证。失败日志保留在 build/tsumo-r21/runtime.log，最终成功日志为 runtime-retry.log；不能将导入崩溃伪称游戏测试通过。

复现：prepare-original-input-stage.py --batch tsumo-r21；设置 YUNJIAN_R21_REPO 为维护目录，Unity 执行 YunJian.Riichi.Editor.TsumoContactR21Preview.Run；随后运行 package-tsumo-r21.py。录制资源保存在 dist/previews/2026-10-29-tsumo-r21/，完成后清理该批次的临时 stage。最新总交接为 docs/handoff-lua-effects-2026-10-29.md。
'''
(R/'docs/自摸落牌时序修正-r21-2026-10-29.md').write_text(doc,'utf8');(O/'说明.md').write_text(doc,'utf8')
print('TSUMO R21 VIDEO READY',O/'tsumo-contact-r21.mp4',verification['videoBytes'],'bytes')
