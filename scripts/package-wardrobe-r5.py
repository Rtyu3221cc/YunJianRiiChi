"""Synchronize four actual Unity viewpoints, with close-ups for river indicators."""
from pathlib import Path
import base64,html,io,json,shutil,sys
from PIL import Image,ImageDraw,ImageFont,ImageOps
R=Path(__file__).resolve().parent.parent;sys.path.insert(0,str(R/'tools/video-inspector'));import av
source=R/'build/wardrobe-preview-r5/render';out=R/'dist/previews/2026-10-25-wardrobe-r5';out.mkdir(parents=True,exist_ok=True)
report=json.loads((source/'capture.json').read_text('utf8'));fps=report['fps'];font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',23)
groups={}
for shot in report['shots']:groups.setdefault(shot['effect'],[]).append(shot)
for shots in groups.values():shots.sort(key=lambda s:s['viewer']);assert [s['viewer'] for s in shots]==[0,1,2,3]
def grid(shots,index,detail=False):
    w,h,header=(640,480,32) if detail else (844,384,38);sheet=Image.new('RGB',(w*2,(h+header)*2),'#17151f');draw=ImageDraw.Draw(sheet)
    for n,shot in enumerate(shots):
        with Image.open(source/'frames'/f"{shot['start']+index:05d}.jpg") as im:
            if detail:
                x,y,cw,ch=shot['detail'];im=im.crop((round(x),round(y),round(x+cw),round(y+ch)))
            image=ImageOps.pad(im.convert('RGB'),(w,h),method=Image.Resampling.LANCZOS,color='#17151f')
        x=n%2*w;y=n//2*(h+header);sheet.paste(image,(x,y+header));draw.text((x+12,y+3),'玩家视角 '+str(shot['viewer']+1),font=font,fill='#f0d1a0')
    return sheet
def encode(file,images):
    if '--pages-only' in sys.argv:return
    with av.open(str(file),'w',options={'movflags':'+faststart'}) as c:
        stream=None
        for i,image in enumerate(images):
            if stream is None:stream=c.add_stream('libx264',rate=fps);stream.width,stream.height=image.size;stream.pix_fmt='yuv420p';stream.options={'crf':'19','preset':'medium'}
            frame=av.VideoFrame.from_image(image);frame.pts=i
            for packet in stream.encode(frame):c.mux(packet)
        for packet in stream.encode():c.mux(packet)
def individual(shot):
    for n in range(shot['start'],shot['end']):
        with Image.open(source/'frames'/f'{n:05d}.jpg') as image:yield image.convert('RGB')
entries=[]
for key,shots in groups.items():
    frames=shots[0]['end']-shots[0]['start'];assert all(s['end']-s['start']==frames for s in shots)
    callout=key in ['mpzs_22event','mpzs_shandian','effect_zhishi','mpzs_23winter']
    encode(out/(key+'-four-views.mp4'),(grid(shots,i) for i in range(frames)))
    if callout:encode(out/(key+'-close-up.mp4'),(grid(shots,i,True) for i in range(frames)))
    for shot in shots:
        encode(out/(key+'-view-'+str(shot['viewer']+1)+'.mp4'),individual(shot))
        shutil.copyfile(source/(key+'-view-'+str(shot['viewer'])+'.png'),out/(key+'-view-'+str(shot['viewer']+1)+'.png'))
    peak=min(24,frames-1);grid(shots,peak).save(out/(key+'-four-views.jpg'),quality=93)
    if callout:grid(shots,peak,True).save(out/(key+'-close-up.jpg'),quality=94)
    entries.append(dict(key=key,title=shots[0]['title'],full=key+'-four-views.mp4',detail=key+'-close-up.mp4' if callout else None,views=[key+'-view-'+str(i)+'.mp4' for i in range(1,5)]))
    print('Packaged',key,'four synchronized viewpoints',flush=True)
encode(out/'all-effects-four-views.mp4',(grid(shots,i) for shots in groups.values() for i in range(shots[0]['end']-shots[0]['start'])))
encode(out/'callouts-four-views-close-up.mp4',(grid(groups[e['key']],i,True) for e in entries if e['detail'] for i in range(groups[e['key']][0]['end']-groups[e['key']][0]['start'])))
for file in ['capture.json','checks.txt','motion-study.txt']:shutil.copyfile(source/file,out/file)
def data_uri(file,mime):return 'data:'+mime+';base64,'+base64.b64encode(file.read_bytes()).decode()
def image_uri(file,single):
    if not single:return file.name
    with Image.open(file) as im:
        im=im.convert('RGB');im.thumbnail((1280,1024));b=io.BytesIO();im.save(b,format='JPEG',quality=86)
    return 'data:image/jpeg;base64,'+base64.b64encode(b.getvalue()).decode()
def page(single):
    sources=[]
    for e in entries:
        item=dict(e);item['full']=data_uri(out/e['full'],'video/mp4') if single else e['full'];item['detail']=data_uri(out/e['detail'],'video/mp4') if single and e['detail'] else e['detail'];sources.append(item)
    buttons=''.join('<button class="effect" data-index="'+str(i)+'">'+html.escape(e['title'])+'</button>' for i,e in enumerate(entries))
    stills=''.join('<details><summary>'+html.escape(e['title'])+' · 四视角截图</summary><img loading="lazy" src="'+image_uri(out/(e['key']+('-close-up.jpg' if e['detail'] else '-four-views.jpg')),single)+'"></details>' for e in entries)
    files=''
    if not single:
        files='<details><summary>原分辨率单视角视频</summary>'
        for e in entries:files+='<p>'+html.escape(e['title'])+'： '+' / '.join('<a href="'+name+'">视角 '+str(i+1)+'</a>' for i,name in enumerate(e['views']))+'</p>'
        files+='</details>'
    return '''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>装扮四视角预览 · 气泡方向修正</title><style>body{margin:0;background:#191720;color:#f0e2cb;font:16px/1.65 system-ui,sans-serif}main{max-width:1280px;margin:auto;padding:16px}h1{font-size:25px}h2{font-size:20px}nav,.controls{display:flex;gap:8px;flex-wrap:wrap;margin:12px 0}button{font:inherit;color:#ebd0a1;background:#302a38;border:1px solid #8b704c;border-radius:6px;padding:8px 11px;cursor:pointer}button.active{background:#755c39;color:white}button:disabled{opacity:.4;cursor:default}#viewport{display:flex;align-items:center;justify-content:center;width:100%}#viewport:fullscreen{background:#111018}#stage{position:relative;width:100%;aspect-ratio:1.25;background:#111018;overflow:hidden;border-radius:8px}#player{display:block;width:100%;height:100%;object-fit:contain;transform-origin:top left}#stage.zoom{aspect-ratio:1.25}#stage.zoom #player{position:absolute;width:200%;height:200%;object-fit:fill}img{display:block;width:100%;border-radius:8px}summary{cursor:pointer;padding:10px;border:1px solid #6f5b42;border-radius:6px}details{margin:14px 0}.note{font-size:14px;color:#bfb4c7}a{color:#efca86}input[type=range]{flex:1;min-width:130px}#time{min-width:90px}</style><main><h1>气泡方向修正 · 四个玩家视角</h1><p>四种鸣牌指示：咕噜咕噜（气泡）、雷驰电掣、普通引导（新增）、众星捧月（新增）。另附烈焰、爆炎龙卷和冰霜的四视角。</p><p class="note">同一事件从四个玩家位置观察，四宫格同步播放。可切换局部放大或单个视角；画面来自 Unity 编辑器实际运行。本轮没有生成新 APK。</p><nav>'''+buttons+'''</nav><h2 id="title"></h2><div class="controls"><button id="whole">全桌</button><button id="detail">局部放大</button><button class="view active" data-view="-1">四宫格</button><button class="view" data-view="0">视角 1</button><button class="view" data-view="1">视角 2</button><button class="view" data-view="2">视角 3</button><button class="view" data-view="3">视角 4</button></div><div id="viewport"><div id="stage"><video id="player" muted playsinline loop preload="metadata"></video></div></div><div class="controls"><button id="play">播放 / 暂停</button><button id="replay">重播</button><input id="seek" type="range" min="0" max="1000" value="0"><span id="time">0.0 / 3.2 秒</span><button id="fullscreen">全屏</button></div>'''+stills+files+'''<script>const sources='''+json.dumps(sources,ensure_ascii=False,separators=(',',':'))+''';const p=document.querySelector('#player'),stage=document.querySelector('#stage'),viewport=document.querySelector('#viewport'),seek=document.querySelector('#seek');let selected=0,detail=true,view=-1;function zoom(){stage.classList.toggle('zoom',view>=0);const ratio=detail&&sources[selected].detail?1.25:2;stage.style.aspectRatio=ratio;const isFull=document.fullscreenElement===viewport;const width=Math.min(isFull?innerWidth:viewport.clientWidth,innerHeight*(isFull?1:.70)*ratio);stage.style.width=width+'px';stage.style.height=width/ratio+'px';p.style.transform=view<0?'none':`translate(${view%2?-50:0}%,${view>=2?-50:0}%)`;document.querySelectorAll('.view').forEach(b=>b.classList.toggle('active',Number(b.dataset.view)===view))}function setSource(reset=false){const e=sources[selected];document.querySelector('#title').textContent=e.title;document.querySelector('#detail').disabled=!e.detail;const useDetail=detail&&e.detail;document.querySelector('#whole').classList.toggle('active',!useDetail);document.querySelector('#detail').classList.toggle('active',!!useDetail);const age=reset?0:p.currentTime;p.src=useDetail?e.detail:e.full;zoom();p.addEventListener('loadedmetadata',()=>{p.currentTime=Math.min(age,Math.max(0,p.duration-.05))},{once:true});document.querySelectorAll('.effect').forEach((b,i)=>b.classList.toggle('active',i===selected))}document.querySelectorAll('.effect').forEach(b=>b.onclick=()=>{selected=Number(b.dataset.index);view=-1;setSource(true);p.play()});document.querySelector('#whole').onclick=()=>{detail=false;setSource();p.play()};document.querySelector('#detail').onclick=()=>{detail=true;setSource();p.play()};document.querySelectorAll('.view').forEach(b=>b.onclick=()=>{view=Number(b.dataset.view);zoom()});document.querySelector('#play').onclick=()=>p.paused?p.play():p.pause();document.querySelector('#replay').onclick=()=>{p.currentTime=0;p.play()};p.ontimeupdate=()=>{if(p.duration){seek.value=p.currentTime/p.duration*1000;document.querySelector('#time').textContent=p.currentTime.toFixed(1)+' / '+p.duration.toFixed(1)+' 秒'}};seek.oninput=()=>{if(p.duration)p.currentTime=seek.value/1000*p.duration};document.querySelector('#fullscreen').onclick=()=>viewport.requestFullscreen?viewport.requestFullscreen():p.webkitEnterFullscreen?.();window.addEventListener('resize',zoom);document.addEventListener('fullscreenchange',zoom);setSource(true);</script></main></html>'''
(out/'index.html').write_text(page(False),'utf8');(out/'手机版四视角预览.html').write_text(page(True),'utf8')
print('Complete:',len(groups),'effects;',len(report['shots']),'views;',report['checks'],'runtime assertions',flush=True)
