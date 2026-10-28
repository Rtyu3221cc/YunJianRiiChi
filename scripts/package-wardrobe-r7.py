"""Three focused R7 corrections, with synchronized original/new comparisons."""
from pathlib import Path
import ast,base64,html,json,shutil,sys
from PIL import Image,ImageDraw,ImageFont,ImageOps
R=Path(__file__).resolve().parent.parent;S=R/'build/wardrobe-r7-review/render';O=R/'dist/previews/2026-10-27-wardrobe-r7';O.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(R/'tools/video-inspector'));import av
report=json.loads((S/'capture.json').read_text('utf8'));FPS=report['fps'];font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',23)
scope=dict(S=S,O=O,FPS=FPS,font=font,Image=Image,ImageOps=ImageOps,ImageDraw=ImageDraw,sys=sys,av=av)
tree=ast.parse((R/'scripts/package-wardrobe-r6.py').read_text('utf8'))
for node in tree.body:
    if isinstance(node,ast.FunctionDef)and node.name in ['frames','grid','encode']:exec(compile(ast.Module(body=[node],type_ignores=[]),'<media-helpers>','exec'),scope)
frames,grid,encode=(scope[k]for k in ['frames','grid','encode'])
groups={}
for shot in report['shots']:groups.setdefault(shot['key'],[]).append(shot)
entries=[]
for key,shots in groups.items():
    shots.sort(key=lambda s:s['viewer']);assert [s['viewer']for s in shots]==[0,1,2,3]
    count=shots[0]['end']-shots[0]['start'];assert all(s['end']-s['start']==count for s in shots)
    encode(O/(key+'.mp4'),(grid(shots,i)for i in range(count)));encode(O/(key+'-close.mp4'),(grid(shots,i,True)for i in range(count)))
    for shot in shots:encode(O/(key+'-view-'+str(shot['viewer']+1)+'.mp4'),frames(shot))
    peak=9 if key=='lightning'else 15;grid(shots,peak,True).save(O/(key+'.jpg'),quality=93)
    entries.append(dict(key=key,title=shots[0]['title'],group=shots[0]['group'],file=key+'.mp4',close=key+'-close.mp4',quad=True))
    print('Packaged',key,flush=True)

def reference(path,start,count):
    images=[];next_time=start
    with av.open(str(path))as c:
        for f in c.decode(video=0):
            if f.time is None or f.time+1e-5<next_time:continue
            images.append(f.to_image().convert('RGB'));next_time+=1/FPS
            if len(images)==count:break
    assert images
    return images+[images[-1]]*(count-len(images))

for key,name,start in [('lightning','雷击和牌特效以及和牌时bgm渐停效果.mp4',4.33),('riichi-blue','苍火立直.mp4',0)]:
    # The winning discard belongs to seat 1; compare its bottom-facing view.
    shot=groups[key][1 if key=='lightning'else 0];count=shot['end']-shot['start'];original=reference(Path('D:/MyWorkSpace/video/1024')/name,start,count)
    def pairs():
        for i,new in enumerate(frames(shot)):
            result=Image.new('RGB',(1688,420),'#171820');draw=ImageDraw.Draw(result)
            for col,im in enumerate([original[i],new]):result.paste(ImageOps.pad(im,(844,384),method=Image.Resampling.LANCZOS),(col*844,36))
            draw.text((14,4),'原版参考',font=font,fill='#efd0a0');draw.text((858,4),'本轮 Unity 实际渲染',font=font,fill='#efd0a0');yield result
    encode(O/(key+'-comparison.mp4'),pairs());entries.insert(0,dict(key=key+'-comparison',title='原版对照 · '+('雷击和牌'if key=='lightning'else'苍火立直'),group='comparison',file=key+'-comparison.mp4',close=None,quad=False))
    print('Aligned comparison',key,flush=True)

for name in ['capture.json','checks.txt','diagnostics.txt']:shutil.copyfile(S/name,O/name)
for path in S.glob('*.png'):shutil.copyfile(path,O/path.name)
shutil.copyfile('D:/MyWorkSpace/video/1024/苍火立直.mp4',O/'original-blue.mp4')
shutil.copyfile('D:/MyWorkSpace/video/1024/雷击和牌特效以及和牌时bgm渐停效果.mp4',O/'original-lightning.mp4')

def uri(name):return 'data:video/mp4;base64,'+base64.b64encode((O/name).read_bytes()).decode()
def page(single):
    data=[]
    for item in entries:
        e=dict(item)
        if single:
            e['file']=uri(e['file'])
            if e.get('close'):e['close']=uri(e['close'])
        data.append(e)
    originals=''if single else '<details><summary>未经裁剪的原始录屏</summary><p>雷击：</p><video controls playsinline src="original-lightning.mp4"></video><p>苍火：</p><video controls playsinline src="original-blue.mp4"></video></details>'
    return '''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>装扮 R7 · 遮挡、雷击与苍火对照</title><style>body{margin:0;background:#171820;color:#ebdfd0;font:16px/1.7 system-ui,sans-serif}main{max-width:1360px;margin:auto;padding:16px}h1{font-size:25px}h2{font-size:20px}p.note{color:#bdb5c5;font-size:14px}.row,nav,#choices{display:flex;gap:8px;flex-wrap:wrap;margin:12px 0}button{color:#edcfa0;background:#302b38;border:1px solid #8c7250;border-radius:6px;padding:8px 12px;font:inherit;cursor:pointer}button.active{background:#745e3c;color:#fff}button:disabled{opacity:.3}#viewport{display:flex;justify-content:center;align-items:center;background:#101116}#stage{position:relative;width:100%;overflow:hidden}#player{display:block;width:100%;height:100%;object-fit:contain;transform-origin:top left}#stage.zoom #player{position:absolute;width:200%;height:200%;object-fit:fill}input{flex:1;min-width:110px}video{max-width:100%}details{margin:18px 0}summary{cursor:pointer;background:#252430;padding:10px;border-radius:6px}</style><main><h1>鸣牌遮挡 · 雷击 · 苍火——R7 效果视频</h1><p>先看“原版对照”，再看各款四视角和局部。所有新画面均为 Unity 实际运行录制；本轮没有 APK 或 GitHub 上传。</p><p class="note">对照按特效出现时刻裁剪。两端场景与摄像机仍有差别，视频用于观察形状、方向和时序，不作为像素完全一致的声明。</p><nav><button data-group="comparison">原版对照</button><button data-group="win">雷击四视角</button><button data-group="riichi">苍火四视角</button><button data-group="callout">鸣牌遮挡修正</button></nav><div id="choices"></div><h2 id="title"></h2><div class="row"><button id="whole">全桌</button><button id="close">局部</button><button class="view" data-view="-1">四宫格</button><button class="view" data-view="0">视角1</button><button class="view" data-view="1">视角2</button><button class="view" data-view="2">视角3</button><button class="view" data-view="3">视角4</button></div><div id="viewport"><div id="stage"><video id="player" playsinline muted loop preload="metadata"></video></div></div><div class="row"><button id="play">播放／暂停</button><button id="replay">重播</button><button id="slow">速度 1×</button><input id="seek" type="range" min="0" max="1000" value="0"><span id="time">0.0秒</span><button id="full">全屏</button></div><details><summary>这轮具体修了什么</summary><p>鸣牌：普通引导保留深度测试；其他指示单独采用覆盖材质，不再被邻牌切掉。每款从四个座位用实际不透明遮挡物验证。</p><p>雷击：原图是一列四行，旧条件只判断列数，因此序列未启用，整张贴图被拉伸。现在逐行播放，并保留原始循环与向下速度；按原固定发射种子生成两束，修正总取最大三束导致的过曝叠加。蓝色外晕仍有差异，尚不能认定完全一致。</p><p>苍火：按原牌模型的前面坐标绑定，修正旋转正负号；绑定点随牌倾斜，并使用实际牌宽缩放。原动画控制器会将时长与触发点除以 speed，已应用人手两倍速等原配置。</p></details>'''+originals+'''<script>const items='''+json.dumps(data,ensure_ascii=False,separators=(',',':'))+''';const p=document.querySelector('#player'),stage=document.querySelector('#stage'),vp=document.querySelector('#viewport');let selected=0,detail=false,view=-1;function fit(){const r=p.videoWidth/p.videoHeight||2;const w=Math.min(vp.clientWidth,innerHeight*(document.fullscreenElement?1:.72)*r);stage.style.width=w+'px';stage.style.height=w/r+'px';stage.classList.toggle('zoom',view>=0);p.style.transform=view<0?'none':`translate(${view%2?-50:0}%,${view>=2?-50:0}%)`;document.querySelectorAll('.view').forEach(b=>{b.disabled=!items[selected].quad;b.classList.toggle('active',Number(b.dataset.view)===view)})}function choose(i,play=false){selected=i;view=-1;const e=items[i];detail=detail&&!!e.close;document.querySelector('#title').textContent=e.title;document.querySelector('#close').disabled=!e.close;p.src=detail?e.close:e.file;document.querySelectorAll('#choices button').forEach(b=>b.classList.toggle('active',Number(b.dataset.index)===i));document.querySelector('#whole').classList.toggle('active',!detail);document.querySelector('#close').classList.toggle('active',detail);if(play)p.play();fit()}function group(g,play=false){document.querySelectorAll('nav button').forEach(b=>b.classList.toggle('active',b.dataset.group===g));const target=document.querySelector('#choices');target.replaceChildren();items.forEach((e,i)=>{if(e.group!==g)return;const b=document.createElement('button');b.textContent=e.title;b.dataset.index=i;b.onclick=()=>choose(i,true);target.append(b)});choose(items.findIndex(e=>e.group===g),play)}document.querySelectorAll('nav button').forEach(b=>b.onclick=()=>group(b.dataset.group,true));document.querySelector('#whole').onclick=()=>{detail=false;choose(selected,true)};document.querySelector('#close').onclick=()=>{detail=true;choose(selected,true)};document.querySelectorAll('.view').forEach(b=>b.onclick=()=>{view=Number(b.dataset.view);fit()});document.querySelector('#play').onclick=()=>p.paused?p.play():p.pause();document.querySelector('#replay').onclick=()=>{p.currentTime=0;p.play()};document.querySelector('#slow').onclick=()=>{p.playbackRate=p.playbackRate===1?.25:1;document.querySelector('#slow').textContent='速度 '+p.playbackRate+'×'};const seek=document.querySelector('#seek');seek.oninput=()=>{if(p.duration)p.currentTime=seek.value/1000*p.duration};p.ontimeupdate=()=>{if(p.duration){seek.value=p.currentTime/p.duration*1000;document.querySelector('#time').textContent=p.currentTime.toFixed(2)+' / '+p.duration.toFixed(2)+'秒'}};p.onloadedmetadata=fit;window.addEventListener('resize',fit);document.addEventListener('fullscreenchange',fit);document.querySelector('#full').onclick=()=>vp.requestFullscreen?vp.requestFullscreen():p.webkitEnterFullscreen?.();group('comparison');</script></main></html>'''
(O/'index.html').write_text(page(False),'utf8');(O/'手机版效果对照.html').write_text(page(True),'utf8');(O/'media.json').write_text(json.dumps(entries,ensure_ascii=False,indent=2),'utf8')
print('R7 media complete',len(entries),'entries',flush=True)
