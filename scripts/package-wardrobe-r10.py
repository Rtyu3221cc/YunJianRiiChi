"""Package complete declaration flows; no APK is produced by this script."""
from pathlib import Path
import ast,base64,json,re,shutil,subprocess,sys
from PIL import Image,ImageDraw,ImageFont,ImageOps
R=Path(__file__).resolve().parent.parent;S=R/'build/wardrobe-r10-review/render';O=R/'dist/previews/2026-10-27-wardrobe-r10';O.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(R/'tools/video-inspector'));import av
report=json.loads((S/'capture.json').read_text('utf8'));FPS=report['fps'];font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',22);scope=dict(S=S,FPS=FPS,font=font,Image=Image,ImageOps=ImageOps,ImageDraw=ImageDraw)
for n in ast.parse((R/'scripts/package-wardrobe-r6.py').read_text('utf8')).body:
    if isinstance(n,ast.FunctionDef)and n.name=='grid':exec(compile(ast.Module(body=[n],type_ignores=[]),'<grid>','exec'),scope)
grid=scope['grid']
def encode(path,images):
    with av.open(str(path),'w',options={'movflags':'+faststart'})as out:
        stream=None
        for i,im in enumerate(images):
            if stream is None:stream=out.add_stream('libx264',rate=FPS);stream.width,stream.height=im.size;stream.pix_fmt='yuv420p';stream.thread_count=2;stream.options={'crf':'20','preset':'fast'}
            frame=av.VideoFrame.from_image(im);frame.pts=i
            for packet in stream.encode(frame):out.mux(packet)
        for packet in stream.encode():out.mux(packet)
groups={}
for shot in report['shots']:groups.setdefault(shot['key'],[]).append(shot)
entries=[]
for key,shots in groups.items():
    shots.sort(key=lambda s:s['viewer']);assert [s['viewer']for s in shots]==[0,1,2,3];count=shots[0]['end']-shots[0]['start'];assert count==80 and all(s['end']-s['start']==count for s in shots)
    encode(O/(key+'.mp4'),(grid(shots,i)for i in range(count)));encode(O/(key+'-close.mp4'),(grid(shots,i,True)for i in range(count)));grid(shots,14,True).save(O/(key+'.jpg'),quality=93)
    entries.append(dict(key=key,title=shots[0]['title'],full=key+'.mp4',close=key+'-close.mp4'));print('Packaged',key,flush=True)
for name in ['capture.json','checks.txt']:shutil.copyfile(S/name,O/name)
(O/'media.json').write_text(json.dumps(entries,ensure_ascii=False,indent=2),'utf8')
def page(single):
    data=[dict(e)for e in entries]
    if single:
        for e in data:
            for k in ['full','close']:e[k]='data:video/mp4;base64,'+base64.b64encode((O/e[k]).read_bytes()).decode()
    return '''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>R10 · 方向与完整立直流程核对</title><style>body{margin:0;background:#181923;color:#e9dfd0;font:16px/1.7 system-ui,sans-serif}main{max-width:1380px;margin:auto;padding:16px}h1{font-size:25px}h2{font-size:20px}.row,#items{display:flex;flex-wrap:wrap;gap:8px;margin:12px 0}button{color:#edcfa0;background:#302b38;border:1px solid #8c7250;border-radius:6px;padding:8px 12px;font:inherit;cursor:pointer}button.active{background:#745e3c;color:white}#viewport{display:flex;justify-content:center;background:#10121a}#stage{position:relative;overflow:hidden;width:100%}video{display:block;width:100%;height:100%;object-fit:contain;transform-origin:top left}#stage.zoom video{position:absolute;width:200%;height:200%;object-fit:fill}.note{font-size:14px;color:#c0b6c4}input{flex:1;min-width:100px}</style><main><h1>R10 · 拖影方向与完整立直动作</h1><p>幻影、飞羽及苍火：人手／猫爪，四个观察方向。流程包含宣言出牌与成立后放立直棒；演示压缩了响应等待，并隐藏立直立绘提示以便观察。实际游戏提示保持原样。</p><p class="note">飞羽按现有原网页代码选择 Dapai 普通出牌分支，幻影、苍火选择 LiQiPai 分支。速度、释放点与触发点从原配置生成。挂接方向由原牌模型 UV 与正面推导。此页核对这两项行为，不代表已证明 Android 原版整体画面完全一致。本轮没有生成新 APK。</p><div id="items"></div><h2 id="title"></h2><div class="row"><button data-mode="full">全桌</button><button data-mode="close">局部</button></div><div class="row"><button data-view="-1">四视角</button><button data-view="0">视角1</button><button data-view="1">视角2</button><button data-view="2">视角3</button><button data-view="3">视角4</button></div><div id="viewport"><div id="stage"><video id="player" muted playsinline loop preload="metadata"></video></div></div><div class="row"><button id="play">播放／暂停</button><button id="replay">重播</button><button id="slow">速度 1×</button><input id="seek" type="range" min="0" max="1000" value="0"><span id="time"></span><button id="full">全屏</button></div><script>const items='''+json.dumps(data,ensure_ascii=False,separators=(',',':'))+''';const p=document.querySelector('#player'),stage=document.querySelector('#stage'),vp=document.querySelector('#viewport');let selected=0,mode='full',view=-1;function fit(){const r=p.videoWidth/p.videoHeight||2,w=Math.min(vp.clientWidth,innerHeight*(document.fullscreenElement?1:.72)*r);stage.style.width=w+'px';stage.style.height=w/r+'px';stage.classList.toggle('zoom',view>=0);p.style.transform=view<0?'none':`translate(${view%2?-50:0}%,${view>=2?-50:0}%)`;document.querySelectorAll('[data-view]').forEach(b=>b.classList.toggle('active',Number(b.dataset.view)===view))}function choose(i,play=false){selected=i;document.querySelector('#title').textContent=items[i].title;p.src=items[i][mode];document.querySelectorAll('#items button').forEach(b=>b.classList.toggle('active',Number(b.dataset.index)===i));document.querySelectorAll('[data-mode]').forEach(b=>b.classList.toggle('active',b.dataset.mode===mode));if(play)p.play().catch(()=>{});fit()}items.forEach((e,i)=>{const b=document.createElement('button');b.textContent=e.title;b.dataset.index=i;b.onclick=()=>choose(i,true);document.querySelector('#items').append(b)});document.querySelectorAll('[data-mode]').forEach(b=>b.onclick=()=>{mode=b.dataset.mode;choose(selected,true)});document.querySelectorAll('[data-view]').forEach(b=>b.onclick=()=>{view=Number(b.dataset.view);fit()});document.querySelector('#play').onclick=()=>p.paused?p.play():p.pause();document.querySelector('#replay').onclick=()=>{p.currentTime=0;p.play()};document.querySelector('#slow').onclick=()=>{p.playbackRate=p.playbackRate===1?.25:1;document.querySelector('#slow').textContent='速度 '+p.playbackRate+'×'};const seek=document.querySelector('#seek');seek.oninput=()=>{if(p.duration)p.currentTime=seek.value/1000*p.duration};p.ontimeupdate=()=>{if(p.duration){seek.value=p.currentTime/p.duration*1000;document.querySelector('#time').textContent=p.currentTime.toFixed(2)+' / '+p.duration.toFixed(2)+'秒'}};p.onloadedmetadata=fit;window.addEventListener('resize',fit);document.addEventListener('fullscreenchange',fit);document.querySelector('#full').onclick=()=>vp.requestFullscreen?vp.requestFullscreen():p.webkitEnterFullscreen?.();choose(0);</script></main></html>'''
for name,single in [('index.html',False),('手机版动作核对.html',True)]:
    text=page(single);(O/name).write_text(text,'utf8');code=re.search(r'<script>(.*?)</script>',text,re.S)[1];temp=R/'build/wardrobe-r10-study'/('phone.js'if single else'index.js');temp.write_text(code,'utf8');node=Path.home()/'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe';subprocess.run([str(node),'--check',str(temp)],check=True,capture_output=True)
videos=[]
for path in sorted(O.glob('*.mp4')):
    with av.open(str(path))as c:count=sum(1 for _ in c.decode(video=0));assert count==80,path
    videos.append(dict(name=path.name,bytes=path.stat().st_size,frames=count))
assert len(videos)==12 and report['frames']==1920
(O/'validation.json').write_text(json.dumps(dict(videos=videos,frames=report['frames'],checks=report['checks'],pageScriptsPassed=True,browserAutomationRun=False,apkBuilt=False),ensure_ascii=False,indent=2),'utf8');print('R10 MEDIA PASS',len(videos),'videos')
