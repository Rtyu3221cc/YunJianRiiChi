"""Publish a source-aligned scythe review inside the existing local preview set."""
from pathlib import Path
import importlib.util,json,shutil,hashlib
import numpy as np
from PIL import Image,ImageDraw,ImageOps

R=Path(__file__).resolve().parent.parent
spec=importlib.util.spec_from_file_location('effects_pack',R/'scripts/package-native-effects-r11.py');P=importlib.util.module_from_spec(spec);spec.loader.exec_module(P)
O=P.O;S=R/'build/effects-r13-review/render';P.S=S;A=O/'source/r13';A.mkdir(exist_ok=True)
KEY='effect-liqi-23ex-liandao';report=json.loads((S/'capture.json').read_text('utf8'));shots=sorted(report['shots'],key=lambda s:s['viewer']);assert len(shots)==4 and report['fps']==60
for shot in shots:
 x,y,w,h=shot['detail'];cx=x+w*.5;cy=y+h*.6;shot['detail']=[max(0,min(500,cx-550)),max(0,min(104,cy-456)),1100,760]
backup=A/'previous-media.json'
if not backup.exists():shutil.copyfile(O/'media.json',backup)
media=json.loads(backup.read_text('utf8'));verified=[];wave=P.sound(shots[0]['cue'],5,shots[0]['cueAt']);paths={}
for close,kind in [(False,'four'),(True,'detail')]:
 path=O/'game'/(KEY+'-r13-'+kind+'.mp4')
 if not path.exists():P.encode(path,(P.grid(shots,i,close)for i in range(300)),60,wave)
 verified.append(P.validate(path,300));paths[kind]=path.relative_to(O).as_posix();print('R13 encoded',kind,flush=True)
for close,kind in [(False,'single'),(True,'single-detail')]:
 path=O/'game'/(KEY+'-r13-'+kind+'.mp4')
 def frames():
  for i in range(300):
   im=P.raw(shots[0],i)
   if close:im=im.crop((328,66,1160,674)).resize((1040,760),Image.Resampling.LANCZOS)
   yield im
 if not path.exists():P.encode(path,frames(),60,wave)
 verified.append(P.validate(path,300));paths[kind]=path.relative_to(O).as_posix()

ref_file=O/'original/riichi-liandao.mp4';ref_frames={}
with P.av.open(str(ref_file))as movie:
 for i,frame in enumerate(movie.decode(video=0)):
  if 120<=i<=180:ref_frames[i]=frame.to_image()
def comparison(time):
 original=ref_frames[round((time+4+1/30)*30)].crop((265,68,785,448)).resize((800,584),Image.Resampling.LANCZOS)
 game=P.raw(shots[0],min(299,round(time*60))).crop((328,66,1160,674)).resize((800,584),Image.Resampling.LANCZOS)
 image=Image.new('RGB',(1600,640),(20,20,25));d=ImageDraw.Draw(image);image.paste(original,(0,36));image.paste(game,(800,36));d.text((20,2),'雀魂原版',font=P.FONT,fill='white');d.text((820,2),'云间日麻 · R13',font=P.FONT,fill='white');d.text((20,613),f'动作对齐时间 {time:.2f} s · 0.25 倍慢放 · 原版参考无录音',font=P.FONT,fill=(235,210,170));return image
compare=O/'问心之镰-原版与修正R13.mp4'
if not compare.exists():P.encode(compare,(comparison(.45+i/240)for i in range(240)),60)
verified.append(P.validate(compare,240));comparison(.64).save(O/'问心之镰-R13对照.jpg',quality=92)

# Replace only this five-second section in the two previous compilations.
single=[s for s in json.loads((O/'game-capture.json').read_text('utf8'))['shots']if s['viewer']==0]
old4=[next(s for s in json.loads((O/'source/r12/capture.json').read_text('utf8'))['shots']if s['key']==key and s['viewer']==0)for key in ['ron-ko','effect-liqi-23ex-maozhua',KEY,'effect-liqi-23ex-xueqiu']]
for filename,items,fps in [('云间日麻-全部特效预览.mp4',single,20),('云间日麻-四项特效修正R12.mp4',old4,60)]:
 path=O/filename;stamp=A/(filename+'.json');count=sum(round((s['end']-s['start'])/(20 if fps==20 else 60)*fps)for s in items)
 if not stamp.exists():
  temporary=O/(path.stem+'.r13.new.mp4')
  def frames():
   with P.av.open(str(path))as movie:
    stream=iter(movie.decode(video=0))
    for item in items:
     n=item['end']-item['start']
     for i in range(n):
      old=next(stream);yield P.raw(shots[0],i*60//fps)if item['key']==KEY else old.to_image()
  audio=np.concatenate([P.sound(s['cue'],(s['end']-s['start'])/fps,s['cueAt'])for s in items],axis=1)
  P.encode(temporary,frames(),fps,audio);P.validate(temporary,count);temporary.replace(path);stamp.write_text(json.dumps(P.validate(path,count),ensure_ascii=False,indent=2)+'\n','utf8')
 verified.append(P.validate(path,count));print('R13 compilation updated',filename,flush=True)

poster=O/'game'/(KEY+'-r13.jpg');P.grid(shots,round(.64*60),True).save(poster,quality=90)
item=next(i for i in media['items']if i['key']==KEY);obsolete={item['full'],item['close']};item.update(full=paths['four'],close=paths['detail'],poster=poster.relative_to(O).as_posix(),revision=13,fps=60)
replace={v['file']for v in verified};media['encoded']=[v for v in media['encoded']if v['file']not in obsolete|replace]+verified;media['revision']=13
(O/'media.json').write_text(json.dumps(media,ensure_ascii=False,indent=2)+'\n','utf8')
for name in ['index.html','r12.html']:
 path=O/name;page=path.read_text('utf8');a=page.index('const items=')+len('const items=');b=page.index(';const p=',a);entries=json.loads(page[a:b]);entry=next(i for i in entries if i['key']==KEY);entry.update(item);page=page[:a]+json.dumps(entries,ensure_ascii=False,separators=(',',':'))+page[b:]
 note='<p class="note" id="r13-note">问心之镰已更新为 R13 竖直下劈。<a class="button" href="r13.html">打开原版同步对照</a></p>'
 if 'id="r13-note"'not in page:page=page.replace('<div class="row"><a class="button"',note+'<div class="row"><a class="button"',1)
 path.write_text(page,'utf8')

canonical=R/'build/effects-r13-canonical';sheet=Image.new('RGB',(1280,600),(20,20,25));d=ImageDraw.Draw(sheet)
for row,view in enumerate(['top','side']):
 for col,age in enumerate(['0.12','0.16','0.24','0.36']):
  im=Image.open(canonical/f'0-90-0-{view}-{age}.jpg').resize((320,240));x=col*320;y=row*300;sheet.paste(im,(x,y+28));d.text((x+5,y),('俯视'if view=='top'else'侧视')+' '+age+' s',font=P.FONT,fill='white')
sheet.save(O/'问心之镰-R13俯视与侧视.jpg',quality=92)

page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>问心之镰 · R13 原版对照</title><style>
body{margin:0;background:#171923;color:#eee6d9;font:16px/1.6 system-ui,"Microsoft YaHei",sans-serif}main{max-width:1500px;margin:auto;padding:16px}h1{font-size:25px;margin:5px 0}p{margin:7px 0}.note{font-size:14px;color:#bbb2c1}.row{display:flex;gap:8px;flex-wrap:wrap;margin:12px 0}button,a{font:inherit;color:#f3d8a7;background:#302c3a;border:1px solid #826b4b;padding:7px 12px;border-radius:6px;cursor:pointer;text-decoration:none}#panels{display:grid;grid-template-columns:1fr 1fr;gap:12px}video{width:100%;background:#080b10;display:block}#stage{overflow:hidden;aspect-ratio:1040/760;position:relative}#game{height:100%;object-fit:contain}#stage.grid video{position:absolute;width:200%;height:200%;object-fit:fill;transform-origin:top left}#reference{aspect-ratio:1040/760;object-fit:contain}#seek{width:100%}details img{width:100%;max-width:1280px}#note{color:#d9c5a5}@media(max-width:800px){#panels{grid-template-columns:1fr}}
</style><style>#reference{height:100%;aspect-ratio:auto}#refstage{position:relative;overflow:hidden;aspect-ratio:1040/760}#refstage.crop video{position:absolute;width:192.3076923%;height:142.1052632%;object-fit:fill;transform:translate(-26.5%,-12.5925926%);transform-origin:top left}</style><main><h1>问心之镰 · 竖直下劈复核 R13</h1><p>保留原版四个方向的预制体与挥刀动画，修正随手部和横放牌旋转造成的挥动平面错误。</p><p class="note">本轮只更新问心之镰，未生成 APK。两边按下劈动作对齐；牌局和目标牌位置不同。原版参考为本家视角，其他方向用于检查云间日麻的呈现。</p><div class="row"><button id="full">全桌</button><button id="detail">局部</button><button data-view="0">本家视角</button><button data-view="1">视角2</button><button data-view="2">视角3</button><button data-view="3">视角4</button><a href="问心之镰-原版与修正R13.mp4">播放原版对照慢放</a><a href="index.html">全部装扮</a></div><div id="panels"><section><p>云间日麻 · R13</p><div id="stage"><video id="game" playsinline muted preload="auto"></video></div></section><section><p>雀魂原版 · 动作同步参考（无录音）</p><div id="refstage"><video id="reference" playsinline muted preload="auto" src="original/riichi-liandao.mp4"></video></div></section></div><input id="seek" type="range" min="0" max="4.98" step="0.016666667" value="0"><div class="row"><button id="play">播放／暂停</button><button id="replay">重新播放</button><button id="prev">上一帧</button><button id="next">下一帧</button><button id="speed">速度 0.25×</button><button data-time="0.64">扬刀</button><button data-time="0.72">劈落</button><button data-time="0.92">命中后</button></div><p id="note"></p><details><summary>俯视和侧视：核对下劈平面</summary><p class="note">使用同一原生预制体和动画。俯视以牌的长轴朝上，与示意图对齐；侧视可见刀刃从牌面上方向下劈落。</p><img src="问心之镰-R13俯视与侧视.jpg" alt="原生镰刀俯视和侧视关键帧"></details><details><summary>原版与 R13 关键帧</summary><img src="问心之镰-R13对照.jpg" alt="原版与R13动作对齐关键帧"></details><script>
const files=FILES,g=document.querySelector('#game'),r=document.querySelector('#reference'),stage=document.querySelector('#stage'),seek=document.querySelector('#seek');let mode='detail',view=0,rate=.25,pending=.64,playing=false;const offset=4+1/30;
function sync(t){t=Math.max(0,Math.min(4.98,t));pending=t;if(g.readyState)g.currentTime=t;if(r.readyState)r.currentTime=t+offset;seek.value=t;document.querySelector('#note').textContent='动作时间 '+t.toFixed(2)+' 秒'+(view===0?'':' · 原版参考为本家视角');}
function pause(){playing=false;g.pause();r.pause()}
function play(){playing=true;g.playbackRate=rate;r.playbackRate=rate;g.play();r.play()}
function load(){const time=g.currentTime||pending;pause();const single=view===0;stage.classList.toggle('grid',!single);stage.style.aspectRatio=single&&mode==='detail'?'1040 / 760':'1600 / 864';const refstage=document.querySelector('#refstage');refstage.classList.toggle('crop',mode==='detail');refstage.style.aspectRatio=mode==='detail'?'1040 / 760':'1600 / 864';g.style.transform=single?'none':`translate(${view%2?-50:0}%,${view>=2?-50:0}%)`;pending=time;g.src=(single?files[mode==='detail'?'single-detail':'single']:files[mode==='detail'?'detail':'four'])+'?r13=verified';}
g.onloadedmetadata=()=>{g.playbackRate=rate;sync(pending)};r.onloadedmetadata=()=>{r.playbackRate=rate;r.currentTime=pending+offset};g.ontimeupdate=()=>{seek.value=g.currentTime;document.querySelector('#note').textContent='动作时间 '+g.currentTime.toFixed(2)+' 秒'+(view===0?'':' · 原版参考为本家视角');if(playing&&Math.abs(r.currentTime-g.currentTime-offset)>.12)r.currentTime=g.currentTime+offset};g.onended=()=>{sync(0);play()};seek.oninput=()=>{pause();sync(Number(seek.value))};document.querySelector('#play').onclick=()=>playing?pause():play();document.querySelector('#replay').onclick=()=>{sync(0);play()};document.querySelector('#prev').onclick=()=>{pause();sync(g.currentTime-1/60)};document.querySelector('#next').onclick=()=>{pause();sync(g.currentTime+1/60)};document.querySelector('#speed').onclick=()=>{rate=rate===.25?1:.25;g.playbackRate=rate;r.playbackRate=rate;document.querySelector('#speed').textContent='速度 '+rate+'×'};document.querySelector('#full').onclick=()=>{mode='full';load()};document.querySelector('#detail').onclick=()=>{mode='detail';load()};document.querySelectorAll('[data-view]').forEach(b=>b.onclick=()=>{view=Number(b.dataset.view);load()});document.querySelectorAll('[data-time]').forEach(b=>b.onclick=()=>{pause();sync(Number(b.dataset.time))});load();
</script></main></html>'''.replace('FILES',json.dumps(paths,ensure_ascii=False,separators=(',',':')))
(O/'r13.html').write_text(page,'utf8')

for f in ['capture.json','checks.txt']:shutil.copyfile(S/f,A/f)
shutil.copyfile(R/'build/effects-r13-review/blade-geometry.json',A/'blade-geometry.json')
qa=[]
for row in media['encoded']+media['originals']:
 actual=P.validate(O/row['file'],row['frames']);assert actual['sha256']==row['sha256'],row['file'];qa.append(actual)
(A/'final-media-qa.json').write_text(json.dumps(dict(passed=True,videos=len(qa),records=qa),ensure_ascii=False,indent=2)+'\n','utf8')
cleanup=[]
for name in obsolete:
 path=(O/name).resolve();assert path.is_relative_to(O.resolve())
 if path.exists():cleanup.append(dict(file=name,bytes=path.stat().st_size));path.unlink()
(A/'replaced-media-cleanup.json').write_text(json.dumps(cleanup,ensure_ascii=False,indent=2)+'\n','utf8')
print('SCYTHE R13 PACKAGE PASS',len(qa),'videos',flush=True)
