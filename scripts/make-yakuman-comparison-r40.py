"""Join the reference and actual Unity recordings without altering either pose."""
from pathlib import Path
from fractions import Fraction
import sys,json,hashlib,html
R=Path(__file__).resolve().parent.parent;sys.path.insert(0,str(R/'tools/video-inspector'))
import av,cv2,numpy as np
from PIL import Image,ImageDraw,ImageFont
B=R/'build/yakuman-alignment-r40';O=R/'dist/previews/2026-11-14-yakuman-comparison';O.mkdir(parents=True,exist_ok=True)
cases=json.loads((R/'tests/fixtures/yakuman-video-1114.json').read_text('utf8'))['cases'];alignment={r['key']:r for r in json.loads((B/'alignment.json').read_text('utf8'))}
source_path='E:/MyWorkSpace_E/video/1114/codex/役滿特效.mp4'
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',26);small=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',19)
video=O/'役满原版与修正对照-含完整飞牌.mp4'
out=av.open(str(video),'w',options={'movflags':'+faststart'});stream=out.add_stream('libx264',rate=30);stream.width=1920;stream.height=640;stream.pix_fmt='yuv420p';stream.options={'crf':'18','preset':'fast','threads':'6'}
frame_index=0;chapters=[];cards=[]
def phase(age):
    if age<.15:return '桌面准备'
    if age<1.55:return '牌桌抖动、翻起'
    if age<2.15:return '向上加速飞出、拖尾'
    if age<5.7:return '天空飞牌'
    if age<8:return '实时手牌展开'
    return '役满文字与收尾'
for case_index,c in enumerate(cases):
    key=c['key'];offset=alignment[key]['offset'];native=av.open(str(B/'capture'/(key+'.mp4')));native.streams.video[0].thread_type='AUTO'
    ref=av.open(source_path);rs=ref.streams.video[0];rs.thread_type='AUTO';begin=max(c['sourceStart'],offset);end=c['sourceEnd']-.04
    ref.seek(max(0,int(begin/rs.time_base)),stream=rs);decoder=ref.decode(video=0);current=next(decoder);following=next(decoder,None)
    start=frame_index/30;chapter=dict(name=c['name'],key=key,start=start,sourceOffset=offset,sourceBounds=[c['sourceStart'],c['sourceEnd']],frames=0,stillFrames=0);chapters.append(chapter)
    header=Image.new('RGB',(1920,640),'#111b2b');d=ImageDraw.Draw(header);d.text((18,8),f"{case_index+1:02d}/15  {c['name']}    原版参考",font=font,fill='white');d.text((978,8),'修正后 · Unity 实际渲染',font=font,fill='#9ee8ca')
    base=np.array(header);cached_ref=None;cached_time=None
    for ni,nf in enumerate(native.decode(video=0)):
        age=ni/30;wanted=offset+age;target=min(max(wanted,c['sourceStart']),end)
        while following is not None and float(following.time)<=target+.009:
            current=following;following=next(decoder,None)
        if cached_time!=current.pts:
            cached_ref=cv2.resize(current.to_ndarray(format='rgb24')[:,244:2316],(960,540),interpolation=cv2.INTER_AREA);cached_time=current.pts
        a=base.copy();a[56:596,:960]=cached_ref;a[56:596,960:]=nf.to_ndarray(format='rgb24');a[:,958:962]=[67,85,105]
        image=Image.fromarray(a);d=ImageDraw.Draw(image)
        state='原片片段已结束（定格）'if wanted>end else '原片片段起点（定格）'if wanted<c['sourceStart'] else f'原片 {float(current.time):06.2f} 秒'
        chapter['stillFrames']+=wanted>end or wanted<c['sourceStart']
        d.text((18,606),state,font=small,fill='#c9d5e5');d.text((978,606),f"{age:05.2f}s  {phase(age)}   |  静音画面对照",font=small,fill='#c9d5e5')
        vf=av.VideoFrame.from_ndarray(np.asarray(image),format='rgb24');vf.pts=frame_index;vf.time_base=Fraction(1,30)
        for packet in stream.encode(vf):out.mux(packet)
        if ni in [59,114,225]:
            label={59:'桌面飞起',114:'天空飞牌',225:'牌列'}[ni];image.save(O/(key+'-'+label+'.jpg'),quality=95)
            if ni==225:cards.append(image.resize((960,320)))
        frame_index+=1;chapter['frames']+=1
    native.close();ref.close();chapter['end']=frame_index/30
    print('COMPOSED',c['name'],chapter['frames'],flush=True)
for packet in stream.encode():out.mux(packet)
out.close()
sheet=Image.new('RGB',(1920,((len(cards)+1)//2)*320),'#111b2b')
for i,im in enumerate(cards):sheet.paste(im,((i%2)*960,(i//2)*320))
sheet.save(O/'15项牌列对照总览.jpg',quality=94)
(O/'chapters.json').write_text(json.dumps(chapters,ensure_ascii=False,indent=2)+'\n','utf8')
links=''.join(f'<button onclick="v.currentTime={c["start"]};v.play()">{i+1:02d} {html.escape(c["name"])}</button>'for i,c in enumerate(chapters))
page=f'''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><title>役满逐项对照</title><style>body{{background:#111b2b;color:#e7effb;font:17px "Microsoft YaHei";margin:28px}}video{{width:100%;max-height:75vh}}button{{padding:10px;margin:5px;border:1px solid #60728d;background:#23334b;color:white;cursor:pointer}}a{{color:#9ee8ca}}</style><h1>役满原版与修正对照</h1><p>左：用户提供的原录像；右：修正后的 Unity 编辑器实际逐帧录制。15 项均包含桌面抖动、翻起、向上飞出、天空追随和实际手牌展开。两侧均为 16:9，同速播放，只调整时间起点。原片截断处明确标记定格。</p><video id="v" controls src="{video.name}"></video><div>{links}</div><p>本视频为静音画面对照。右侧使用一姬和当前牌桌，手牌按原片重建，所有牌列仍由结算数据动态绑定。原片角色、牌桌、粒子随机和录像压缩不同。尚未 Android 真机验证，本次未构建新 APK。</p><p><a href="{video.name}" download>下载完整 MP4</a> · <a href="15项牌列对照总览.jpg">查看全部牌列</a></p></html>'''
(O/'index.html').write_text(page,'utf8')
# Decode the finished artifact completely, checking timestamp continuity.
check=av.open(str(video));vs=check.streams.video[0];vs.thread_type='AUTO';count=0;last=-1
for f in check.decode(video=0):
    assert f.pts>last;last=f.pts;assert f.width==1920 and f.height==640;count+=1
assert count==frame_index==15*305
report=dict(video=video.name,bytes=video.stat().st_size,sha256=hashlib.sha256(video.read_bytes()).hexdigest(),frames=count,fps=30,duration=count/30,size=[1920,640],cases=len(chapters),audio='silent visual comparison',androidDeviceTested=False,chapters=chapters)
(O/'verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n','utf8');print('VERIFIED',count,'frames',video,flush=True)
