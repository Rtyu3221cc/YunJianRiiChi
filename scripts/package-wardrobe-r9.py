"""Merge the ring correction and package the seven actual-rendered effects."""
from pathlib import Path
import ast,base64,json,re,shutil,subprocess,sys
from PIL import Image,ImageDraw,ImageFont,ImageOps
R=Path(__file__).resolve().parent.parent;S=R/'build/wardrobe-r9-review/render';P=S.parent/'reactor-patch';B=R/'build/wardrobe-r9-study/reactor-before';O=R/'dist/previews/2026-10-27-wardrobe-r9';O.mkdir(parents=True,exist_ok=True);B.mkdir(exist_ok=True);(B/'frames').mkdir(exist_ok=True)
sys.path.insert(0,str(R/'tools/video-inspector'));import av
if not(B/'capture.json').exists():shutil.copyfile(S/'capture.json',B/'capture.json')
report=json.loads((B/'capture.json').read_text('utf8'));patch=json.loads((P/'capture.json').read_text('utf8'));assert len(patch['shots'])==4 and patch['frames']==480
for old in [s for s in report['shots']if s['key']=='ron-reactor']:
    new=next(s for s in patch['shots']if s['viewer']==old['viewer']);assert new['end']-new['start']==old['end']-old['start']==120
    for i in range(120):
        target=S/'frames'/f"{old['start']+i:05d}.jpg";before=B/'frames'/target.name
        if not before.exists():shutil.copyfile(target,before)
        shutil.copyfile(P/'frames'/f"{new['start']+i:05d}.jpg",target)
    shutil.copyfile(P/(old['key']+'-view-'+str(old['viewer'])+'.png'),S/(old['key']+'-view-'+str(old['viewer'])+'.png'))
report.update(checks=report['checks']+patch['checks'],reactorPatchChecks=patch['checks'],ringGeometryChecks=json.loads((R/'build/wardrobe-r9-study/ring-validation.json').read_text('utf8'))['checks'],ringsPatched=True)
(S/'capture.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),'utf8');FPS=report['fps'];font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',22)
scope=dict(S=S,FPS=FPS,font=font,Image=Image,ImageOps=ImageOps,ImageDraw=ImageDraw)
for n in ast.parse((R/'scripts/package-wardrobe-r6.py').read_text('utf8')).body:
    if isinstance(n,ast.FunctionDef)and n.name=='grid':exec(compile(ast.Module(body=[n],type_ignores=[]),'<grid>','exec'),scope)
grid=scope['grid']

def encode(path,images):
    with av.open(str(path),'w',options={'movflags':'+faststart'})as out:
        stream=None
        for i,image in enumerate(images):
            if stream is None:
                stream=out.add_stream('libx264',rate=FPS);stream.width,stream.height=image.size;stream.pix_fmt='yuv420p';stream.thread_count=2;stream.options={'crf':'20','preset':'fast'}
            frame=av.VideoFrame.from_image(image);frame.pts=i
            for packet in stream.encode(frame):out.mux(packet)
        for packet in stream.encode():out.mux(packet)

groups={}
for shot in report['shots']:groups.setdefault(shot['key'],[]).append(shot)
entries=[]
for key,shots in groups.items():
    shots.sort(key=lambda s:s['viewer']);count=shots[0]['end']-shots[0]['start'];assert [s['viewer']for s in shots]==[0,1,2,3]
    assert all(s['end']-s['start']==count for s in shots)
    encode(O/(key+'.mp4'),(grid(shots,i)for i in range(count)));encode(O/(key+'-close.mp4'),(grid(shots,i,True)for i in range(count)))
    grid(shots,min(15,count-1),True).save(O/(key+'.jpg'),quality=93)
    entries.append(dict(key=key,title=shots[0]['title'],group=shots[0]['group'],full=key+'.mp4',close=key+'-close.mp4',frames=count));print('Packaged',key,flush=True)

for key,title,before_root,group,count in [('callout-sakura','樱花：3 层／8 层对照',R/'build/wardrobe-r8-review/render','callout',78),('ron-reactor','方舟：翻折／平面自转对照',B,'win',120)]:
    original=json.loads((before_root/'capture.json').read_text('utf8'));shots=groups[key]
    def pairs():
        for i in range(count):
            result=Image.new('RGB',(1280,560),'#171820');draw=ImageDraw.Draw(result)
            for shot in shots:
                old=next(s for s in original['shots']if s['key']==key and s['viewer']==shot['viewer']);col,row=shot['viewer']%2,shot['viewer']//2;x,y=col*640,row*280;cx,cy,w,h=shot['detail'];box=tuple(round(v)for v in [cx,cy,cx+w,cy+h])
                for side,(base,first)in enumerate([(before_root,old['start']),(S,shot['start'])]):
                    with Image.open(base/'frames'/f'{first+i:05d}.jpg')as im:picture=ImageOps.pad(im.convert('RGB').crop(box),(320,240),method=Image.Resampling.LANCZOS)
                    result.paste(picture,(x+side*320,y+40));draw.text((x+8+side*320,y+5),'视角 '+str(shot['viewer']+1)+(' · 修改前'if side==0 else' · 修改后'),font=font,fill='#e7c59b'if side==0 else'#bce4bd')
            yield result
    file=key+'-comparison.mp4';encode(O/file,pairs());entries.insert(0,dict(key=key+'-comparison',title=title,group=group,full=file,close=None,frames=count))

for name in ['capture.json','checks.txt']:shutil.copyfile(S/name,O/name)
shutil.copyfile(P/'checks.txt',O/'ring-patch-checks.txt');shutil.copyfile(R/'build/wardrobe-r9-study/ring-validation.json',O/'ring-validation.json')
for path in S.glob('*.png'):shutil.copyfile(path,O/path.name)
(O/'media.json').write_text(json.dumps(entries,ensure_ascii=False,indent=2),'utf8')

def page(single):
    data=[dict(e)for e in entries]
    if single:
        for e in data:
            for mode in ['full','close']:
                if e.get(mode):e[mode]='data:video/mp4;base64,'+base64.b64encode((O/e[mode]).read_bytes()).decode()
    return '''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>0.6.14 · 装扮新增预览</title><style>body{margin:0;background:#181923;color:#e9dfd0;font:16px/1.7 system-ui,sans-serif}main{max-width:1380px;margin:auto;padding:16px}h1{font-size:25px}h2{font-size:20px}.row,#items{display:flex;flex-wrap:wrap;gap:8px;margin:12px 0}button{color:#edcfa0;background:#302b38;border:1px solid #8c7250;border-radius:6px;padding:8px 12px;font:inherit;cursor:pointer}button.active{background:#745e3c;color:white}button:disabled{opacity:.3}#viewport{display:flex;justify-content:center;background:#10121a}#stage{position:relative;overflow:hidden;width:100%}video{display:block;width:100%;height:100%;object-fit:contain;transform-origin:top left}#stage.zoom video{position:absolute;width:200%;height:200%;object-fit:fill}.note{font-size:14px;color:#bfb5c5}input{flex:1;min-width:100px}</style><main><h1>0.6.14 · 樱花补全与新增六种特效</h1><p>鸣牌：樱花补全、冰晶试装、风涡试装。立直：幻影、飞羽。和牌：黑炎、方舟反应堆。可切换四个视角、局部与慢放。</p><p class="note">樱花鸣牌从 3 层补至 8 层，保留原纹理和颜色，按牌河场景调整大小与持续发射。方舟的环面保持放平，内外环反向自转。鸣牌改编款均标注为试装；这些是 Unity 编辑器实际录制，Android 实机效果待测试。</p><nav class="row"><button data-group="callout">鸣牌指示</button><button data-group="riichi">立直特效</button><button data-group="win">和牌特效</button></nav><div id="items"></div><h2 id="title"></h2><div class="row"><button data-mode="full">全桌／对照</button><button data-mode="close">局部</button></div><div class="row"><button data-view="-1">四视角</button><button data-view="0">视角1</button><button data-view="1">视角2</button><button data-view="2">视角3</button><button data-view="3">视角4</button></div><div id="viewport"><div id="stage"><video id="player" muted playsinline loop preload="metadata"></video></div></div><div class="row"><button id="play">播放／暂停</button><button id="replay">重播</button><button id="slow">速度 1×</button><input id="seek" type="range" min="0" max="1000" value="0"><span id="time"></span><button id="full">全屏</button></div><script>const items='''+json.dumps(data,ensure_ascii=False,separators=(',',':'))+''';const p=document.querySelector('#player'),stage=document.querySelector('#stage'),vp=document.querySelector('#viewport');let selected=0,mode='full',view=-1;function fit(){const r=p.videoWidth/p.videoHeight||2,w=Math.min(vp.clientWidth,innerHeight*(document.fullscreenElement?1:.72)*r);stage.style.width=w+'px';stage.style.height=w/r+'px';stage.classList.toggle('zoom',view>=0);p.style.transform=view<0?'none':`translate(${view%2?-50:0}%,${view>=2?-50:0}%)`;document.querySelectorAll('[data-view]').forEach(b=>b.classList.toggle('active',Number(b.dataset.view)===view))}function choose(i,play=false){selected=i;if(!items[i][mode])mode='full';document.querySelector('#title').textContent=items[i].title;p.src=items[i][mode];document.querySelectorAll('#items button').forEach(b=>b.classList.toggle('active',Number(b.dataset.index)===i));document.querySelectorAll('[data-mode]').forEach(b=>{b.disabled=!items[i][b.dataset.mode];b.classList.toggle('active',b.dataset.mode===mode)});if(play)p.play().catch(()=>{});fit()}function group(g,play=false){const list=document.querySelector('#items');list.replaceChildren();document.querySelectorAll('[data-group]').forEach(b=>b.classList.toggle('active',b.dataset.group===g));items.forEach((e,i)=>{if(e.group!==g)return;const b=document.createElement('button');b.textContent=e.title;b.dataset.index=i;b.onclick=()=>choose(i,true);list.append(b)});choose(items.findIndex(e=>e.group===g),play)}document.querySelectorAll('[data-group]').forEach(b=>b.onclick=()=>group(b.dataset.group,true));document.querySelectorAll('[data-mode]').forEach(b=>b.onclick=()=>{mode=b.dataset.mode;choose(selected,true)});document.querySelectorAll('[data-view]').forEach(b=>b.onclick=()=>{view=Number(b.dataset.view);fit()});document.querySelector('#play').onclick=()=>p.paused?p.play():p.pause();document.querySelector('#replay').onclick=()=>{p.currentTime=0;p.play()};document.querySelector('#slow').onclick=()=>{p.playbackRate=p.playbackRate===1?.25:1;document.querySelector('#slow').textContent='速度 '+p.playbackRate+'×'};const seek=document.querySelector('#seek');seek.oninput=()=>{if(p.duration)p.currentTime=seek.value/1000*p.duration};p.ontimeupdate=()=>{if(p.duration){seek.value=p.currentTime/p.duration*1000;document.querySelector('#time').textContent=p.currentTime.toFixed(2)+' / '+p.duration.toFixed(2)+'秒'}};p.onloadedmetadata=fit;window.addEventListener('resize',fit);document.addEventListener('fullscreenchange',fit);document.querySelector('#full').onclick=()=>vp.requestFullscreen?vp.requestFullscreen():p.webkitEnterFullscreen?.();group('callout');</script></main></html>'''

for name,single in [('index.html',False),('手机版新增特效预览.html',True)]:
    text=page(single);(O/name).write_text(text,'utf8');script=re.search(r'<script>(.*?)</script>',text,re.S)[1];temp=R/'build/wardrobe-r9-study'/('phone.js'if single else'index.js');temp.write_text(script,'utf8');node=Path.home()/'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe';subprocess.run([str(node),'--check',str(temp)],check=True,capture_output=True)
videos=[]
for e in entries:
    for mode in ['full','close']:
        if not e.get(mode):continue
        path=O/e[mode]
        with av.open(str(path))as c:count=sum(1 for _ in c.decode(video=0));assert count==e['frames'],path
        videos.append(dict(name=path.name,frames=count,bytes=path.stat().st_size))
assert len(videos)==16 and report['frames']==2760 and len(report['shots'])==28
(O/'validation.json').write_text(json.dumps(dict(videos=videos,capturedFrames=report['frames'],runtimeChecks=report['checks'],ringGeometryChecks=report['ringGeometryChecks'],pageScriptsPassed=True,browserAutomationRun=False),ensure_ascii=False,indent=2),'utf8')
print('R9 MEDIA PASS',len(videos),'videos',flush=True)
