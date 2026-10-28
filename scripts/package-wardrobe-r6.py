"""Package actual Unity recordings, synchronized viewpoints and native mixed audio."""
from pathlib import Path
import base64,html,io,json,shutil,sys
from PIL import Image,ImageDraw,ImageFont,ImageOps
R=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(R/'tools/video-inspector'))
import av
S=R/'build/wardrobe-r6-review/render';O=R/'dist/previews/2026-10-27-wardrobe-r6';O.mkdir(parents=True,exist_ok=True)
report=json.loads((S/'capture.json').read_text('utf8'));FPS=report['fps'];font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',22)
entries=[];groups={}
for shot in report['shots']:groups.setdefault(shot['key'],[]).append(shot)

def frames(shot):
    for i in range(shot['start'],shot['end']):
        with Image.open(S/'frames'/f'{i:05d}.jpg')as im:yield im.convert('RGB')

def grid(shots,i,close=False):
    cols=3 if len(shots)==3 else 2;rows=(len(shots)+cols-1)//cols;w,h=(600,420)if close else(844,384);header=36
    result=Image.new('RGB',(cols*w,rows*(h+header)),'#151823');draw=ImageDraw.Draw(result)
    for n,shot in enumerate(shots):
        with Image.open(S/'frames'/f"{shot['start']+i:05d}.jpg")as image:
            if close:
                x,y,cw,ch=shot['detail'];image=image.crop((round(x),round(y),round(x+cw),round(y+ch)))
            im=ImageOps.pad(image.convert('RGB'),(w,h),method=Image.Resampling.LANCZOS,color='#151823')
        x=n%cols*w;y=n//cols*(h+header);result.paste(im,(x,y+header));draw.text((x+10,y+3),'玩家视角 '+str(shot['viewer']+1),font=font,fill='#f0d2a2')
    return result

def encode(path,images):
    if '--pages-only'in sys.argv:return
    if '--sakura-only'in sys.argv and not any(path.stem.startswith(k)for k in ['ron-sakura','callout-sakura','wins-all','callouts-all']):return
    if '--final-refine'in sys.argv and not any(path.stem.startswith(k)for k in ['mpzs_shandian','mpzs_22event','effect_zhishi','mpzs_23winter','callout-sakura','callout-bat','callouts-all','bgm-fade']):return
    with av.open(str(path),'w',options={'movflags':'+faststart'})as result:
        stream=None
        for i,image in enumerate(images):
            if stream is None:
                stream=result.add_stream('libx264',rate=FPS);stream.width,stream.height=image.size;stream.pix_fmt='yuv420p';stream.options={'crf':'20','preset':'fast'}
            frame=av.VideoFrame.from_image(image);frame.pts=i
            for packet in stream.encode(frame):result.mux(packet)
        for packet in stream.encode():result.mux(packet)

def mux_audio(movie,wav,target):
    if '--pages-only'in sys.argv or '--sakura-only'in sys.argv:return
    with av.open(str(movie))as video,av.open(str(wav))as audio,av.open(str(target),'w',options={'movflags':'+faststart'})as result:
        visual=result.add_stream_from_template(video.streams.video[0]);sound=result.add_stream('aac',rate=48000);sound.layout='stereo';sound.bit_rate=160000;packets=[]
        for packet in video.demux(video.streams.video[0]):
            if packet.dts is not None:packet.stream=visual;packets.append(packet)
        resampler=av.AudioResampler(format='fltp',layout='stereo',rate=48000)
        for frame in audio.decode(audio=0):
            for converted in resampler.resample(frame):packets.extend(sound.encode(converted))
        for converted in resampler.resample(None):packets.extend(sound.encode(converted))
        packets.extend(sound.encode(None));packets.sort(key=lambda p:float(p.dts*p.time_base))
        for packet in packets:result.mux(packet)

for key,shots in groups.items():
    kind=shots[0]['group'];shots.sort(key=lambda s:s['viewer']);quad=kind in ['win','riichi','callout','north'];count=shots[0]['end']-shots[0]['start'];title=shots[0]['title']
    if quad:
        assert all(s['end']-s['start']==count for s in shots)
        encode(O/(key+'.mp4'),(grid(shots,i)for i in range(count)));encode(O/(key+'-close.mp4'),(grid(shots,i,True)for i in range(count)))
        peak={'win-default':4,'ron-flame':10,'ron-tornado':12,'mpzs_shandian':10,'effect_zhishi':10}.get(key,16)
        grid(shots,min(peak,count-1),kind in ['callout','north']).save(O/(key+'.jpg'),quality=91)
        for shot in shots:encode(O/(key+'-view-'+str(shot['viewer']+1)+'.mp4'),frames(shot))
        full=key+'.mp4';close=key+'-close.mp4'
    else:
        encode(O/(key+'-silent.mp4'if kind=='sound'else key+'.mp4'),frames(shots[0]))
        if kind=='sound':mux_audio(O/(key+'-silent.mp4'),S/shots[0]['audio'],O/(key+'.mp4'))
        with Image.open(S/'frames'/f"{shots[0]['start']+min(29,count-1):05d}.jpg")as image:image.convert('RGB').save(O/(key+'.jpg'),quality=91)
        full=key+'.mp4';close=None
    entries.append(dict(key=key,title=title,group=kind,full=full,close=close,quad=len(shots)==4,views=len(shots),sound=kind=='sound',poster=key+'.jpg'))
    print('Packaged',key,kind,flush=True)

for kind,key,title in [('win','wins-all','五种和牌特效 · 四视角'),('riichi','riichi-all','四种立直特效 · 四视角'),('callout','callouts-all','六种鸣牌指示 · 四视角'),('hand','hands-all','五种手型完整立直＋冰霜分支')]:
    selected=[shots for shots in groups.values()if shots[0]['group']==kind]
    def sequence():
        for shots in selected:
            if kind=='hand':yield from frames(shots[0])
            else:
                for i in range(shots[0]['end']-shots[0]['start']):yield grid(shots,i,kind=='callout')
    encode(O/(key+'.mp4'),sequence());poster=next(e['poster']for e in entries if e['group']==kind);entries.insert(0,dict(key=key,title=title,group=kind,full=key+'.mp4',close=None,quad=kind!='hand',views=4 if kind!='hand'else 1,sound=False,poster=poster))

def reference(key,name,rotate=False):
    path=Path('D:/MyWorkSpace/video/1024')/name
    def images():
        with av.open(str(path))as c:
            next_time=0
            for frame in c.decode(video=0):
                if frame.time is None or frame.time+1e-5<next_time:continue
                im=frame.to_image()
                if rotate:im=im.rotate(90,expand=True)
                im.thumbnail((1280,720));w,h=im.size;im=im.resize((w//2*2,h//2*2));yield im;next_time+=1/FPS
    encode(O/(key+'.mp4'),images())
reference('original-tabs','角色装扮切换.mp4',True)
shutil.copyfile('D:/MyWorkSpace/video/1024/雷击和牌特效以及和牌时bgm渐停效果.mp4',O/'original-lightning-bgm.mp4')
for name in ['capture.json','checks.txt','diagnostics.txt']:shutil.copyfile(S/name,O/name)
for path in S.glob('preview-disabled-*.png'):shutil.copyfile(path,O/path.name)
shutil.copyfile(S/'bgm-fade.wav',O/'bgm-fade.wav')

def uri(file,mime):return 'data:'+mime+';base64,'+base64.b64encode(file.read_bytes()).decode()

def page(single):
    data=[]
    for entry in entries:
        if single and entry['key']in ['wins-all','riichi-all','callouts-all','hands-all']:continue
        e=dict(entry)
        for field in ['full','close']:
            if single and e.get(field):e[field]=uri(O/e[field],'video/mp4')
        data.append(e)
    groups_nav=[('sound','和牌与 BGM'),('hand','完整立直动作'),('win','和牌特效'),('riichi','立直特效'),('callout','鸣牌指示'),('flow','荒牌流局'),('ui','角色／装扮切换'),('north','拔北描边')]
    nav=''.join('<button data-group="'+k+'">'+v+'</button>'for k,v in groups_nav)
    extra=''
    if not single:
        extra='<details><summary>原版参考视频</summary><p>雷击与 BGM 原始录屏：</p><video controls playsinline preload="metadata" src="original-lightning-bgm.mp4"></video><p>角色／装扮切换：</p><video controls playsinline muted preload="metadata" src="original-tabs.mp4"></video></details>'
    return '''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>装扮修正 R6 · 视频预览</title><style>body{margin:0;background:#171820;color:#eadfcf;font:16px/1.7 system-ui,sans-serif}main{max-width:1360px;margin:auto;padding:16px}h1{font-size:25px}h2{font-size:20px}.note{color:#bbb3c0;font-size:14px}.controls,nav,#choices{display:flex;flex-wrap:wrap;gap:8px;margin:12px 0}button{color:#ecd0a1;background:#302b38;border:1px solid #826e52;border-radius:6px;padding:8px 12px;font:inherit;cursor:pointer}button.active{background:#755e3e;color:#fff}button:disabled{opacity:.35;cursor:default}#viewport{display:flex;justify-content:center;align-items:center;background:#101117}#viewport:fullscreen{height:100vh}#stage{position:relative;width:100%;overflow:hidden}#player{width:100%;height:100%;display:block;object-fit:contain;transform-origin:top left}#stage.zoom #player{position:absolute;width:200%;height:200%;object-fit:fill}input{flex:1;min-width:100px}video,img{max-width:100%;display:block}details{margin:18px 0}summary{cursor:pointer;padding:10px;background:#242430;border-radius:6px}a{color:#e7c78e}#status{font-size:14px;color:#c5b8a7}</style><main><h1>装扮动作与特效修正 · R6 视频预览</h1><p>本轮为 Unity 实际运行录制：五种手型的宣言牌与放棒、雷击方向、BGM 渐停与下一局渐入、流局轮询、页签展开收拢、拔北描边，以及各款特效的四个视角。</p><p class="note">本轮仅提供视频预览，没有生成新 APK 或上传 GitHub。和牌／立直／鸣牌三类在宿舍中的独立预览入口已关闭；牌桌展示保留。新增“樱瓣”和“蝙蝠”鸣牌为原粒子试装，已在名称标注。</p><nav>'''+nav+'''</nav><div id="choices"></div><h2 id="title"></h2><p id="status"></p><div class="controls"><button id="whole">全桌</button><button id="close">局部</button><button class="view" data-view="-1">全部视角</button><button class="view" data-view="0">视角 1</button><button class="view" data-view="1">视角 2</button><button class="view" data-view="2">视角 3</button><button class="view" data-view="3">视角 4</button></div><div id="viewport"><div id="stage"><video id="player" playsinline loop preload="metadata"></video></div></div><div class="controls"><button id="play">播放／暂停</button><button id="replay">重播</button><button id="mute">声音：开</button><input id="seek" type="range" min="0" max="1000" value="0"><span id="time">0.0 秒</span><button id="fullscreen">全屏</button></div><details><summary>本轮修正与观察说明</summary><p>猫爪等手型的携牌旋转现在随原动画变化，不再从动作开始就固定为落地角度。完整立直片段由引擎生成的公开宣言、立直与放棒事件驱动。冰霜和蝙蝠按原配置使用常规出牌动作，之后仍有专门的放棒动作。</p><p>雷击已按实际粒子速度验证为向下，保留原始贴图与单端锚点。BGM 从和牌开始用一秒渐停，结算期间保持静音，下一局开始后一秒渐入；声音从 Unity 实际混音输出录制。</p><p>流局片段会在摊牌前和过程中多次重建同一公开状态，检查首次渲染不闪现摊开牌、轮询不重播动作。拔北采用与副露相同的可见描边尺寸。原版鸣牌逻辑仅加载装备的特效，已移除额外黄色三角；普通引导自己的箭头仍会保留。</p></details>'''+extra+'''<script>const entries='''+json.dumps(data,ensure_ascii=False,separators=(',',':'))+''';const player=document.querySelector('#player'),stage=document.querySelector('#stage'),viewport=document.querySelector('#viewport');let selected=entries.findIndex(e=>e.group==='sound'),group='sound',detail=false,view=-1;function fit(){const r=player.videoWidth/player.videoHeight||2.2;const w=Math.min(viewport.clientWidth,innerHeight*(document.fullscreenElement?1:.73)*r);stage.style.width=w+'px';stage.style.height=w/r+'px';stage.classList.toggle('zoom',view>=0);player.style.transform=view<0?'none':`translate(${view%2?-50:0}%,${view>=2?-50:0}%)`;document.querySelectorAll('.view').forEach(b=>{b.disabled=!entries[selected].quad;b.classList.toggle('active',Number(b.dataset.view)===view)})}function choose(i,play=false){selected=i;view=-1;const e=entries[i];document.querySelector('#title').textContent=e.title;document.querySelector('#status').textContent=e.sound?'本段为 Unity 实际录制的 BGM 独立试听，角色语音和动作音效已静音。':e.quad?'同一特效的四个视角，可切换局部和单视角放大。':'实际运行片段，可暂停、拖动和逐段比较。';document.querySelector('#close').disabled=!e.close;detail=detail&&!!e.close;player.src=detail?e.close:e.full;player.muted=!e.sound;document.querySelector('#mute').textContent=player.muted?'声音：关':'声音：开';document.querySelector('#whole').classList.toggle('active',!detail);document.querySelector('#close').classList.toggle('active',detail);document.querySelectorAll('#choices button').forEach(b=>b.classList.toggle('active',Number(b.dataset.index)===i));if(play)player.play();fit()}function tabs(g,play=false){group=g;document.querySelectorAll('nav button').forEach(b=>b.classList.toggle('active',b.dataset.group===g));const choices=document.querySelector('#choices');choices.replaceChildren();entries.forEach((e,i)=>{if(e.group!==g)return;const b=document.createElement('button');b.textContent=e.title;b.dataset.index=i;b.onclick=()=>choose(i,true);choices.append(b)});choose(entries.findIndex(e=>e.group===g),play)}document.querySelectorAll('nav button').forEach(b=>b.onclick=()=>tabs(b.dataset.group,true));document.querySelector('#whole').onclick=()=>{detail=false;choose(selected,true)};document.querySelector('#close').onclick=()=>{detail=true;choose(selected,true)};document.querySelectorAll('.view').forEach(b=>b.onclick=()=>{view=Number(b.dataset.view);fit()});document.querySelector('#play').onclick=()=>player.paused?player.play():player.pause();document.querySelector('#replay').onclick=()=>{player.currentTime=0;player.play()};document.querySelector('#mute').onclick=()=>{player.muted=!player.muted;document.querySelector('#mute').textContent=player.muted?'声音：关':'声音：开'};const seek=document.querySelector('#seek');seek.oninput=()=>{if(player.duration)player.currentTime=seek.value/1000*player.duration};player.ontimeupdate=()=>{if(player.duration){seek.value=player.currentTime/player.duration*1000;document.querySelector('#time').textContent=player.currentTime.toFixed(1)+' / '+player.duration.toFixed(1)+' 秒'}};player.onloadedmetadata=fit;window.addEventListener('resize',fit);document.addEventListener('fullscreenchange',fit);document.querySelector('#fullscreen').onclick=()=>viewport.requestFullscreen?viewport.requestFullscreen():player.webkitEnterFullscreen?.();tabs(group);</script></main></html>'''

(O/'index.html').write_text(page(False),'utf8');(O/'手机版完整视频预览.html').write_text(page(True),'utf8');(O/'media.json').write_text(json.dumps(entries,ensure_ascii=False,indent=2),'utf8')
print('R6 media complete:',len(entries),'entries',O,flush=True)
