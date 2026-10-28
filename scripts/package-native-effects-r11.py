"""Encode the actual editor captures and build a local review gallery."""
from pathlib import Path
from fractions import Fraction
import hashlib,json,math,shutil,sys
import numpy as np
from PIL import Image,ImageDraw,ImageFont,ImageOps

R=Path(__file__).resolve().parent.parent
S=R/'build/effects-r11-review/render'
O=R/'dist/previews/2026-10-27-effects-reference'
G=O/'game';G.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(R/'tools/video-inspector'))
import av
FONT=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',24)
REFERENCE={'ron-flame':'ron-flame','ron-wind':'ron-wind','ron-23ex-heidong':'ron-blackhole','ron-23ex-liandao':'ron-liandao','ron-23ex-shuizhu':'ron-water','effect-liqi-ding':'riichi-ding','effect-liqi-longteng':'riichi-longteng','riichi-feather':'riichi-feather','effect-liqi-23ex-liandao':'riichi-liandao','effect-liqi-23ex-xueqiu':'riichi-xueqiu'}
RATE=48000


def sound(cue,seconds,offset):
    result=np.zeros((2,round(seconds*RATE)),dtype=np.float32)
    file=R/'unity/Assets/Resources/Majsoul/Audio'/(cue+'.mp3')
    if not cue or not file.exists():return result
    parts=[]
    with av.open(str(file))as source:
        sampler=av.AudioResampler(format='fltp',layout='stereo',rate=RATE)
        for frame in source.decode(audio=0):
            for converted in sampler.resample(frame):parts.append(converted.to_ndarray())
        for converted in sampler.resample(None):parts.append(converted.to_ndarray())
    if parts:
        wave=np.concatenate(parts,axis=1);start=max(0,round(offset*RATE));length=min(wave.shape[1],result.shape[1]-start)
        if length>0:result[:,start:start+length]=wave[:,:length]*.75
    return result


def encode(path,frames,fps,wave=None):
    count=0
    with av.open(str(path),'w',options={'movflags':'+faststart'})as output:
        video=None
        audio=output.add_stream('aac',rate=RATE)if wave is not None else None
        if audio is not None:audio.layout='stereo';audio.bit_rate=128000
        for count,image in enumerate(frames,1):
            if video is None:
                video=output.add_stream('libx264',rate=fps);video.width,video.height=image.size;video.pix_fmt='yuv420p';video.thread_count=2;video.options={'crf':'22','preset':'fast'}
            frame=av.VideoFrame.from_image(image);frame.pts=count-1;frame.time_base=Fraction(1,fps);frame.pict_type=av.video.frame.PictureType.NONE
            for packet in video.encode(frame):output.mux(packet)
        for packet in video.encode():output.mux(packet)
        if audio is not None:
            for start in range(0,wave.shape[1],1024):
                chunk=np.ascontiguousarray(wave[:,start:start+1024]);frame=av.AudioFrame.from_ndarray(chunk,format='fltp',layout='stereo');frame.sample_rate=RATE;frame.time_base=Fraction(1,RATE);frame.pts=start
                for packet in audio.encode(frame):output.mux(packet)
            for packet in audio.encode():output.mux(packet)
    return count


def raw(shot,index):return Image.open(S/'frames'/f'{shot["start"]+index:05}.jpg').convert('RGB')


def grid(shots,index,close=False):
    result=Image.new('RGB',(1600,864),(15,17,23));draw=ImageDraw.Draw(result)
    for shot in shots:
        image=raw(shot,index)
        if close and shot.get('detail'):
            x,y,w,h=shot['detail'];image=image.crop((round(x),round(y),round(x+w),round(y+h)))
        image=ImageOps.contain(image,(800,432),Image.Resampling.LANCZOS)
        viewer=shot['viewer'];x=viewer%2*800;y=viewer//2*432;result.paste(image,(x+(800-image.width)//2,y+(432-image.height)//2))
        draw.rectangle((x,y,x+150,y+31),fill=(20,20,25));draw.text((x+8,y+1),'视角 '+str(viewer+1),font=FONT,fill=(245,220,177))
    return result


def validate(path,expected=None):
    with av.open(str(path))as movie:
        count=sum(1 for frame in movie.decode(video=0))
    if expected is not None and count!=expected:raise ValueError((path.name,count,expected))
    return dict(file=path.relative_to(O).as_posix(),frames=count,bytes=path.stat().st_size,sha256=hashlib.sha256(path.read_bytes()).hexdigest())


def compress_originals():
    records=[]
    for path in sorted((O/'original').glob('*.mp4')):
        if path.name.endswith('.new.mp4'):continue
        marker=path.with_suffix('.encoded.json')
        if marker.exists():records.append(json.loads(marker.read_text('utf8')));continue
        original_hash=hashlib.sha256(path.read_bytes()).hexdigest();temporary=path.with_name(path.stem+'.new.mp4')
        def images():
            with av.open(str(path))as movie:
                for frame in movie.decode(video=0):
                    im=frame.to_image();yield im.crop((4,130,1004,670)) if im.size==(1006,670)else im
        count=encode(temporary,images(),30);validate(temporary,count);temporary.replace(path)
        row=validate(path,count);row.update(rawRecordingSHA256=original_hash,audio=False,source='Original Edge game preview',clientVersion='4.0.51')
        marker.write_text(json.dumps(row,ensure_ascii=False,indent=2)+'\n','utf8');records.append(row)
        print('Reference encoded',path.name,path.stat().st_size,flush=True)
    return records


def main():
    report=json.loads((S/'capture.json').read_text('utf8'));fps=report['fps'];groups={}
    for shot in report['shots']:groups.setdefault(shot['key'],[]).append(shot)
    items=[];verified=[];all_wave=[]
    for key,shots in groups.items():
        shots.sort(key=lambda s:s['viewer']);assert [s['viewer']for s in shots]==[0,1,2,3]
        count=shots[0]['end']-shots[0]['start'];assert all(s['end']-s['start']==count for s in shots)
        wave=sound(shots[0]['cue'],count/fps,shots[0]['cueAt']);all_wave.append(wave)
        for close in [False,True]:
            name=key+('-close'if close else'-four')+'.mp4';path=G/name
            if not path.exists():encode(path,(grid(shots,i,close)for i in range(count)),fps,wave)
            verified.append(validate(path,count))
        grid(shots,min(count-1,18),True).save(G/(key+'.jpg'),quality=88)
        reference=REFERENCE.get(key);reference='original/'+reference+'.mp4'if reference and(O/'original'/(reference+'.mp4')).exists()else None
        items.append(dict(key=key,title=shots[0]['title'],category=shots[0]['group'],full='game/'+key+'-four.mp4',close='game/'+key+'-close.mp4',poster='game/'+key+'.jpg',reference=reference))
        print('Game packaged',key,flush=True)
    showcase=O/'云间日麻-全部特效预览.mp4'
    if not showcase.exists():
        def showcase_images():
            for key,shots in groups.items():
                shot=next(s for s in shots if s['viewer']==0)
                for i in range(shot['end']-shot['start']):yield raw(shot,i)
        count=encode(showcase,showcase_images(),fps,np.concatenate(all_wave,axis=1))
    else:count=sum(s['end']-s['start']for s in report['shots']if s['viewer']==0)
    verified.append(validate(showcase,count));originals=compress_originals()
    for file in ['capture.json','checks.txt']:shutil.copyfile(S/file,O/('game-'+file))
    (O/'media.json').write_text(json.dumps(dict(items=items,encoded=verified,originals=originals),ensure_ascii=False,indent=2)+'\n','utf8')
    page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 · 装扮特效预览</title><style>
body{margin:0;background:#171923;color:#eee6d9;font:16px/1.65 system-ui,"Microsoft YaHei",sans-serif}main{max-width:1500px;margin:auto;padding:18px}h1{font-size:26px;margin:4px 0}h2{font-size:20px}.note{color:#c1b9c9;font-size:14px}button,a.button{background:#302c3a;color:#f3d8a7;border:1px solid #826b4b;border-radius:6px;padding:8px 13px;cursor:pointer;font:inherit;text-decoration:none}button.active{background:#725b38;color:#fff}.row,#items{display:flex;gap:8px;flex-wrap:wrap;margin:10px 0}#items button{font-size:14px}video{display:block;width:100%;background:#090b11;max-height:70vh}#columns{display:grid;grid-template-columns:1fr}#columns.compare{grid-template-columns:1fr 1fr;gap:12px}#stage{position:relative;overflow:hidden;aspect-ratio:1600/864}#stage video{height:100%;max-height:none;object-fit:contain;transform-origin:top left}#stage.zoom video{position:absolute;width:200%;height:200%;object-fit:fill}#refbox{display:none}#columns.compare #refbox{display:block}input{accent-color:#d4ad69}p{margin:9px 0}@media(max-width:850px){#columns.compare{grid-template-columns:1fr}main{padding:10px}}
</style><main><h1>云间日麻 · 装扮特效预览</h1><p>13 款和牌、11 款立直。选择效果后可查看四个方向、局部细节，并切换慢放。</p><p class="note">云间画面来自 Unity 编辑器中实际运行的牌桌与演出组件。立直包含宣言出牌、成立后放立直棒，演示压缩了响应等待。本轮未生成 APK，也未进行安卓实机验证。</p><div class="row"><a class="button" href="云间日麻-全部特效预览.mp4">播放全部特效合辑</a><button id="wins">和牌</button><button id="riichi">立直</button></div><div id="items"></div><h2 id="title"></h2><div class="row"><button data-mode="full">全桌</button><button data-mode="close">局部</button><button data-view="-1">四视角</button><button data-view="0">视角1</button><button data-view="1">视角2</button><button data-view="2">视角3</button><button data-view="3">视角4</button><button id="compare">原版参考</button></div><div id="columns"><section><p>云间日麻</p><div id="stage"><video id="game" controls playsinline loop preload="metadata"></video></div></section><section id="refbox"><p>雀魂原版参考（Edge 录制，无录音）</p><video id="reference" controls playsinline loop preload="metadata"></video><p class="note">参考客户端为 WebGL 4.0.51，资源版本 0.16.289。两边的牌局、目标牌位置和摄像机构图不同，可分别拖动进度核对动作与特效；此页不将它们视为逐像素一致。</p></section></div><div class="row"><button id="play">播放／暂停</button><button id="replay">重新播放</button><button id="slow">速度 1×</button><label><input id="mute" type="checkbox"> 静音</label><button id="fullscreen">全屏</button></div><p id="referenceNote" class="note"></p><p class="note">游戏预览的特效音效按演出触发时刻合成，沿用已取得的固定版本音频资源，未混入背景音乐。原版参考仅覆盖本轮实际录到的条目，其余条目依据同版本原生资源恢复。</p><script>const items=ITEMS;const p=document.querySelector('#game'),r=document.querySelector('#reference'),stage=document.querySelector('#stage'),columns=document.querySelector('#columns');let selected=0,mode='full',view=-1,category='winEffect',compare=false,rate=1;p.onloadedmetadata=()=>{p.playbackRate=rate};r.onloadedmetadata=()=>{r.playbackRate=rate};
function fit(){stage.classList.toggle('zoom',view>=0);p.style.transform=view<0?'none':`translate(${view%2?-50:0}%,${view>=2?-50:0}%)`;document.querySelectorAll('[data-view]').forEach(b=>b.classList.toggle('active',Number(b.dataset.view)===view))}
function choose(i,play=false){selected=i;const item=items[i];document.querySelector('#title').textContent=item.title;p.src=item[mode];p.poster=item.poster;r.removeAttribute('src');if(item.reference)r.src=item.reference;document.querySelector('#referenceNote').textContent=item.reference?'此款有本轮原版实播参考。':'此款已恢复同版本原生资源，本轮尚未录到原版实播参考。';columns.classList.toggle('compare',compare&&!!item.reference);document.querySelector('#compare').disabled=!item.reference;document.querySelectorAll('#items button').forEach(b=>b.classList.toggle('active',Number(b.dataset.index)===i));document.querySelectorAll('[data-mode]').forEach(b=>b.classList.toggle('active',b.dataset.mode===mode));if(play)p.play().catch(()=>{});fit()}
function list(){const host=document.querySelector('#items');host.replaceChildren();items.forEach((item,i)=>{if(item.category!==category)return;const b=document.createElement('button');b.textContent=item.title;b.dataset.index=i;b.onclick=()=>choose(i,true);host.append(b)});choose(items.findIndex(i=>i.category===category))}
document.querySelector('#wins').onclick=()=>{category='winEffect';list()};document.querySelector('#riichi').onclick=()=>{category='riichiEffect';list()};document.querySelectorAll('[data-mode]').forEach(b=>b.onclick=()=>{mode=b.dataset.mode;choose(selected,true)});document.querySelectorAll('[data-view]').forEach(b=>b.onclick=()=>{view=Number(b.dataset.view);fit()});document.querySelector('#play').onclick=()=>{if(p.paused){p.play();if(compare&&items[selected].reference)r.play()}else{p.pause();r.pause()}};document.querySelector('#replay').onclick=()=>{p.currentTime=0;p.play();if(compare&&items[selected].reference){r.currentTime=0;r.play()}};document.querySelector('#slow').onclick=()=>{rate=rate===1?.25:1;p.defaultPlaybackRate=rate;r.defaultPlaybackRate=rate;p.playbackRate=rate;r.playbackRate=rate;document.querySelector('#slow').textContent='速度 '+rate+'×'};document.querySelector('#mute').onchange=e=>{p.muted=e.target.checked};document.querySelector('#compare').onclick=()=>{compare=!compare;columns.classList.toggle('compare',compare&&!!items[selected].reference)};document.querySelector('#fullscreen').onclick=()=>stage.requestFullscreen();list();</script></main></html>'''.replace('ITEMS',json.dumps(items,ensure_ascii=False,separators=(',',':')))
    (O/'index.html').write_text(page,'utf8');print('PACKAGE COMPLETE',len(items),'effects;',len(originals),'original recordings',flush=True)


if __name__=='__main__':main()
