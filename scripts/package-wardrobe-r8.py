"""Four-seat callout readability comparisons from real Unity frames."""
from pathlib import Path
import ast,base64,json,re,shutil,subprocess,sys
from PIL import Image,ImageDraw,ImageFont,ImageOps
R=Path(__file__).resolve().parent.parent
S=R/'build/wardrobe-r8-review/render';OLD=R/'build/wardrobe-r7-review/render';O=R/'dist/previews/2026-10-27-wardrobe-r8';O.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(R/'tools/video-inspector'));import av
report=json.loads((S/'capture.json').read_text('utf8'));old_report=json.loads((OLD/'capture.json').read_text('utf8'));FPS=report['fps'];font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',22)
scope=dict(S=S,O=O,FPS=FPS,font=font,Image=Image,ImageOps=ImageOps,ImageDraw=ImageDraw,sys=sys,av=av)
for n in ast.parse((R/'scripts/package-wardrobe-r6.py').read_text('utf8')).body:
    if isinstance(n,ast.FunctionDef)and n.name in ['grid','encode']:exec(compile(ast.Module(body=[n],type_ignores=[]),'<media-helpers>','exec'),scope)
grid,encode=(scope[k]for k in ['grid','encode'])
groups={}
for shot in report['shots']:groups.setdefault(shot['key'],[]).append(shot)
entries=[]

def comparison(shots,index=None):
    image=Image.new('RGB',(1280,560),'#171820');draw=ImageDraw.Draw(image)
    for shot in shots:
        col,row=shot['viewer']%2,shot['viewer']//2;x,y=col*640,row*280
        old=next(s for s in old_report['shots']if s['key']==shot['key']and s['viewer']==shot['viewer'])
        x0,y0,w,h=shot['detail'];box=tuple(round(v)for v in [x0,y0,x0+w,y0+h])
        paths=[S/(shot['key']+'-old-'+str(shot['viewer'])+'.png'),S/(shot['key']+'-new-'+str(shot['viewer'])+'.png')]if index is None else [OLD/'frames'/f"{old['start']+index:05d}.jpg",S/'frames'/f"{shot['start']+index:05d}.jpg"]
        for side,path in enumerate(paths):
            with Image.open(path)as im:frame=ImageOps.pad(im.convert('RGB').crop(box),(320,240),method=Image.Resampling.LANCZOS)
            image.paste(frame,(x+side*320,y+40));draw.text((x+8+side*320,y+5),'视角 '+str(shot['viewer']+1)+(' · 上轮'if side==0 else' · 本轮'),font=font,fill='#e6c392'if side==0 else'#bce7bc')
    return image

order=['mpzs_23winter','callout-bat','callout-sakura','mpzs_shandian','mpzs_22event','effect_zhishi']
for key in order:
    shots=sorted(groups[key],key=lambda s:s['viewer']);assert [s['viewer']for s in shots]==[0,1,2,3]
    count=shots[0]['end']-shots[0]['start'];assert count==78 and all(s['end']-s['start']==count for s in shots)
    encode(O/(key+'-comparison.mp4'),(comparison(shots,i)for i in range(count)))
    encode(O/(key+'-full.mp4'),(grid(shots,i)for i in range(count)))
    encode(O/(key+'-close.mp4'),(grid(shots,i,True)for i in range(count)))
    comparison(shots).save(O/(key+'-comparison.jpg'),quality=93)
    entries.append(dict(key=key,title=shots[0]['title'],comparison=key+'-comparison.mp4',full=key+'-full.mp4',close=key+'-close.mp4'))
    print('Packaged',key,flush=True)

for name in ['capture.json','checks.txt']:shutil.copyfile(S/name,O/name)
(O/'media.json').write_text(json.dumps(entries,ensure_ascii=False,indent=2),'utf8')

def page(single):
    data=[dict(e)for e in entries]
    if single:
        for e in data:
            for mode in ['comparison','full','close']:e[mode]='data:video/mp4;base64,'+base64.b64encode((O/e[mode]).read_bytes()).decode()
    return '''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>鸣牌指示 R8 · 牌面可读性</title><style>body{margin:0;background:#181923;color:#e7dfd1;font:16px/1.7 system-ui,sans-serif}main{max-width:1380px;margin:auto;padding:16px}h1{font-size:25px}h2{font-size:20px}.row,#items{display:flex;flex-wrap:wrap;gap:8px;margin:12px 0}button{color:#edcfa0;background:#302c39;border:1px solid #8d7657;border-radius:6px;padding:8px 12px;font:inherit;cursor:pointer}button.active{background:#725b3d;color:white}#viewport{display:flex;justify-content:center;background:#10121a}#stage{position:relative;overflow:hidden;width:100%}video{display:block;width:100%;height:100%;object-fit:contain;transform-origin:top left}#stage.zoom video{position:absolute;width:200%;height:200%;object-fit:fill}.note{font-size:14px;color:#bdb4c5}input{flex:1;min-width:100px}</style><main><h1>鸣牌指示 R8 · 减少遮牌</h1><p>中央发光层经过牌面时柔和减淡，外围保留；普通引导沿用原效果。含六款指示、四视角和同步前后对照。</p><p class="note">前后视频：左侧为 R7 录制，右侧为本轮 Unity 实际录制。每个视角都使用同一张牌河作为目标。樱瓣、蝙蝠仍为试装款。本轮没有 APK 或 GitHub 上传。</p><div id="items"></div><h2 id="title"></h2><div class="row"><button data-mode="comparison">优化前后</button><button data-mode="close">本轮局部</button><button data-mode="full">本轮全桌</button></div><div class="row"><button data-view="-1">四视角</button><button data-view="0">视角1</button><button data-view="1">视角2</button><button data-view="2">视角3</button><button data-view="3">视角4</button></div><div id="viewport"><div id="stage"><video id="player" muted playsinline loop preload="metadata"></video></div></div><div class="row"><button id="play">播放／暂停</button><button id="replay">重播</button><button id="slow">速度 1×</button><input id="seek" type="range" min="0" max="1000" value="0"><span id="time"></span><button id="full">全屏</button></div><p class="note">优化仅作用于鸣牌指示的独立材质。牌字区域采用随透视变化的柔和减淡，并按款式降低叠加亮度；其余指示仍不被邻牌切掉。</p><script>const items='''+json.dumps(data,ensure_ascii=False,separators=(',',':'))+''';const p=document.querySelector('#player'),stage=document.querySelector('#stage'),vp=document.querySelector('#viewport');let selected=0,mode='comparison',view=-1;function fit(){const r=p.videoWidth/p.videoHeight||2;const w=Math.min(vp.clientWidth,innerHeight*(document.fullscreenElement?1:.72)*r);stage.style.width=w+'px';stage.style.height=w/r+'px';stage.classList.toggle('zoom',view>=0);p.style.transform=view<0?'none':`translate(${view%2?-50:0}%,${view>=2?-50:0}%)`;document.querySelectorAll('[data-view]').forEach(b=>b.classList.toggle('active',Number(b.dataset.view)===view))}function choose(i,play=false){selected=i;document.querySelector('#title').textContent=items[i].title;p.src=items[i][mode];document.querySelectorAll('#items button').forEach(b=>b.classList.toggle('active',Number(b.dataset.index)===i));document.querySelectorAll('[data-mode]').forEach(b=>b.classList.toggle('active',b.dataset.mode===mode));if(play)p.play().catch(()=>{});fit()}items.forEach((e,i)=>{const b=document.createElement('button');b.textContent=e.title;b.dataset.index=i;b.onclick=()=>choose(i,true);document.querySelector('#items').append(b)});document.querySelectorAll('[data-mode]').forEach(b=>b.onclick=()=>{mode=b.dataset.mode;choose(selected,true)});document.querySelectorAll('[data-view]').forEach(b=>b.onclick=()=>{view=Number(b.dataset.view);fit()});document.querySelector('#play').onclick=()=>p.paused?p.play():p.pause();document.querySelector('#replay').onclick=()=>{p.currentTime=0;p.play()};document.querySelector('#slow').onclick=()=>{p.playbackRate=p.playbackRate===1?.25:1;document.querySelector('#slow').textContent='速度 '+p.playbackRate+'×'};const seek=document.querySelector('#seek');seek.oninput=()=>{if(p.duration)p.currentTime=seek.value/1000*p.duration};p.ontimeupdate=()=>{if(p.duration){seek.value=p.currentTime/p.duration*1000;document.querySelector('#time').textContent=p.currentTime.toFixed(2)+' / '+p.duration.toFixed(2)+'秒'}};p.onloadedmetadata=fit;window.addEventListener('resize',fit);document.addEventListener('fullscreenchange',fit);document.querySelector('#full').onclick=()=>vp.requestFullscreen?vp.requestFullscreen():p.webkitEnterFullscreen?.();choose(0);</script></main></html>'''

for name,single in [('index.html',False),('手机版鸣牌优化预览.html',True)]:
    content=page(single);(O/name).write_text(content,'utf8');code=re.search(r'<script>(.*?)</script>',content,re.S)[1];temp=R/'build/wardrobe-r8-study'/('phone.js'if single else'index.js');temp.write_text(code,'utf8');node=Path.home()/'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe';subprocess.run([str(node),'--check',str(temp)],check=True,capture_output=True)
    for path in re.findall(r'(?:src|href)="([^"#]+)"',content):assert (O/path).is_file(),path

videos=[]
for path in sorted(O.glob('*.mp4')):
    with av.open(str(path))as container:count=sum(1 for _ in container.decode(video=0));assert count==78,path
    videos.append(dict(name=path.name,frames=count,bytes=path.stat().st_size))
assert len(videos)==18 and report['frames']==1872 and len(report['shots'])==24 and not report['apkBuilt']and not report['githubUploaded']
(O/'validation.json').write_text(json.dumps(dict(runtimeChecks=report['checks'],capturedFrames=report['frames'],videos=videos,pageScriptsPassed=True,localLinksPassed=True,browserAutomationRun=False,apkBuilt=False,githubUploaded=False),ensure_ascii=False,indent=2)+'\n','utf8')
print('R8 media verification PASS',len(videos),'videos',flush=True)
