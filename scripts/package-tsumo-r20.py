"""Deliver a verified before/after hand-size video, no APK."""
from pathlib import Path
import hashlib,json,re,runpy,shutil,sys
R=Path(__file__).resolve().parent.parent;B=R/'build/tsumo-r20';C=B/'capture';O=R/'dist/previews/2026-10-29-tsumo-r20';O.mkdir(parents=True,exist_ok=True)
assert 'TSUMO R20 PASS 32 frames=588' in (B/'runtime-final.log').read_text('utf8',errors='replace')
report=json.loads((C/'capture.json').read_text('utf8'));assert report['frames']==588 and report['checks']==32
runpy.run_path(str(R/'scripts/mux-recorded-cues.py'))['mux'](C/'tsumo-r20-silent.mp4',report,O/'tsumo-hand-r20.mp4')
sys.path.insert(0,str(R/'tools/video-inspector'));import av
from PIL import Image,ImageDraw
qa=B/'video-qa';qa.mkdir(exist_ok=True);stills=[];frames=0;last=-1
with av.open(str(O/'tsumo-hand-r20.mp4'))as movie:
    s=movie.streams.video[0];assert (s.width,s.height)==(1746,810)and s.average_rate==30
    for f in movie.decode(video=0):
        assert float(f.time)>last;last=float(f.time)
        if frames in [30,120,225,270,330,380,440,550]:
            im=f.to_image();im.save(qa/f'frame-{frames}.png');stills.append((frames,im))
        frames+=1
assert frames==588
sheet=Image.new('RGB',(4*440,2*228),'#171a22');draw=ImageDraw.Draw(sheet)
for i,(n,im)in enumerate(stills):im.thumbnail((440,204));x=i%4*440;y=i//4*228;sheet.paste(im,(x,y+24));draw.text((x+5,y+5),f'{n/30:.2f}s',fill='white')
sheet.save(qa/'contact-sheet.jpg',quality=90)
for name in ['before.png','after.png','checks.txt','capture.json']:shutil.copy2(C/name,O/name)
shutil.copy2(C/'after.png',O/'poster.png')
meta=B/'stage/unity/Assets/Editor/TsumoHandR20Preview.cs.meta'
if meta.exists():shutil.copy2(meta,R/'unity/Assets/Editor'/meta.name)
(C/'error.txt').unlink(missing_ok=True)
verification=dict(date='2026-10-29',oldHandScale=.36,newHandScale=.48,linearIncreasePercent=100/3,runtimeChecks=32,frames=588,fps=30,durationSeconds=19.6,width=1746,height=810,allFramesDecoded=True,actualUnityRendering=True,androidDeviceTested=False,newApk=False,originalReference='Existing local self-draw recording; moderate source-scale adjustment, not a pixel-identical reference claim',audio='Bundled Fate effect sound muxed at its recorded production trigger time',videoBytes=(O/'tsumo-hand-r20.mp4').stat().st_size,sha256=hashlib.sha256((O/'tsumo-hand-r20.mp4').read_bytes()).hexdigest())
(O/'verification.json').write_text(json.dumps(verification,ensure_ascii=False,indent=2)+'\n','utf8')
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>自摸手部放大 · 云间日麻</title><style>body{margin:0;background:#171a22;color:#eee5df;font:16px/1.7 system-ui,"Microsoft YaHei",sans-serif}main{max-width:1500px;margin:auto;padding:24px}video{width:100%;background:#080a0e;border-radius:8px}h1{font-size:26px}a{color:#edc887}img{width:49%}</style><main><h1>自摸手部适度放大</h1><p>缩放 0.36 → 0.48，约放大三分之一。19.6 秒：调整前后慢放、调整后的完整自摸及 Fate 特效衔接。牌的尺寸、落点和时序保留。</p><video controls playsinline preload="metadata" poster="poster.png" src="tsumo-hand-r20.mp4"></video><p><a href="tsumo-hand-r20.mp4" download>下载视频</a> · <a href="verification.json">检查摘要</a></p><img src="before.png" alt="调整前"><img src="after.png" alt="调整后"><p>Unity 实际渲染；原音效按实际触发帧合成。本轮未生成 APK。</p></main></html>'''
(O/'index.html').write_text(page,'utf8');print('TSUMO R20 VIDEO READY',O/'tsumo-hand-r20.mp4',verification['videoBytes'],'bytes')
