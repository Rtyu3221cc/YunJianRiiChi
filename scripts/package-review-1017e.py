"""Reuse the approved frames; update settlement sound and the toolbar still."""
from pathlib import Path
from fractions import Fraction
import json,shutil,sys
R=Path(__file__).resolve().parent.parent;S=R/'build/review-1017e/render';O=R/'dist/preview-20261017'
sys.path.insert(0,str(R/'tools/video-inspector'));import av,numpy as np
from PIL import Image
for name in ['lobby-spacing','shortcut-spacing']:shutil.copyfile(S/(name+'.png'),O/(name+'.png'))
review=json.loads((S/'audio-timeline.json').read_text('utf8'));voices=json.loads((R/'unity/Assets/Resources/Majsoul/SettlementVoice/manifest.json').read_text('utf8'));voice_paths={v['key']:v['resource']for v in voices['clips']}
def wave(cue):
    stem=R/'unity/Assets/Resources'/(voice_paths[cue['key']]if cue['kind']=='voice'else'Majsoul/Audio/'+cue['key']);file=next(p for p in stem.parent.glob(stem.name+'.*')if p.suffix.lower()in ['.wav','.mp3','.ogg'])
    parts=[];resample=av.AudioResampler(format='fltp',layout='stereo',rate=44100)
    with av.open(str(file))as source:
        for frame in source.decode(audio=0):
            for converted in resample.resample(frame):parts.append(converted.to_ndarray())
    for converted in resample.resample(None):parts.append(converted.to_ndarray())
    return np.concatenate(parts,axis=1)
mix=np.zeros((2,round(review['totalFrames']/12*44100)),np.float32)
for cue in review['events']:
    data=wave(cue);at=round(cue['at']*44100);count=min(data.shape[1],mix.shape[1]-at);gain=.8 if cue['kind']=='voice'else.65
    if cue['key']=='hule_fan_out':gain=.35
    if count>0:mix[:,at:at+count]+=data[:,:count]*gain
mix*=.95/max(1,np.max(np.abs(mix)))
def audio_packets(target,data):
    stream=target.add_stream('aac',rate=44100);stream.layout='stereo';stream.bit_rate=160000;packets=[]
    for at in range(0,data.shape[1],1024):
        frame=av.AudioFrame.from_ndarray(np.ascontiguousarray(data[:,at:at+1024]),format='fltp',layout='stereo');frame.sample_rate=44100;frame.pts=at;frame.time_base=Fraction(1,44100);packets.extend(stream.encode(frame))
    packets.extend(stream.encode());return packets
def mux(target,packets):
    packets.sort(key=lambda p:float(p.dts*p.time_base))
    for packet in packets:target.mux(packet)
with av.open(str(O/'double-yakuman-review.mp4'))as source,av.open(str(O/'double-yakuman-audio-fixed.mp4'),'w',options={'movflags':'+faststart'})as target:
    video=target.add_stream_from_template(source.streams.video[0]);packets=[]
    for packet in source.demux(source.streams.video[0]):
        if packet.dts is not None:packet.stream=video;packets.append(packet)
    packets.extend(audio_packets(target,mix));mux(target,packets)
frames=sorted((R/'build/review-1017d/render/double-yakuman').glob('frame-*.png'))[review['scoreStartFrame']:]
data=mix[:,round(review['scoreStartFrame']/12*44100):]
with av.open(str(O/'score-audio-review.mp4'),'w',options={'movflags':'+faststart'})as target:
    with Image.open(frames[0])as pic:w,h=pic.size
    video=target.add_stream('libx264',rate=12);video.width=w;video.height=h;video.pix_fmt='yuv420p';video.options={'crf':'19','preset':'fast'};packets=[]
    for i,file in enumerate(frames):
        with Image.open(file)as pic:frame=av.VideoFrame.from_image(pic.convert('RGB'))
        frame.pts=i;packets.extend(video.encode(frame))
    packets.extend(video.encode());packets.extend(audio_packets(target,data));mux(target,packets)
final_peak=float(np.max(np.abs(mix[:,round(review['finalStartFrame']/12*44100):])))
if final_peak!=0:raise RuntimeError('Final page audio is not silent')
with av.open(str(O/'score-audio-review.mp4'))as check:print('Score preview:',round(check.duration/av.time_base,2),'seconds;',len(check.streams.audio),'audio stream; final peak',final_peak)
old=O/'previous-1017d.html'
if not old.exists():shutil.copyfile(O/'index.html',old)
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 · 间距与结算音效修正</title><style>body{margin:0;background:#121c2d;color:#e8edf5;font:16px/1.8 "Microsoft YaHei",sans-serif}main{max-width:1400px;margin:auto;padding:26px}h1,h2,a{color:#efd0a0}h1{font-size:27px}h2{font-size:21px}p{color:#bbc8de}.note{padding:12px 18px;background:#1e2d45;border-left:3px solid #cfaa6b}figure{margin:0;background:#1a2a40;border-radius:6px;overflow:hidden}img,video{display:block;width:100%}figcaption{padding:10px 15px}.toolbar{display:flex;align-items:flex-start;gap:22px}.crop{flex:0 0 130px}.full{flex:1}details{margin:24px 0}@media(max-width:700px){main{padding:16px}.toolbar{display:block}.crop{width:130px;margin:0 auto 20px}}</style><main><h1>云间日麻 · 本次修正预览</h1><p class="note">只更新预览；未打包 APK、提交 Git 或运行单元测试。结算画面沿用上一轮，更新实际代码生成的音效时序，没有重新录制整段。</p><h2>四个按钮统一间距</h2><p>按可见圆形边缘等距排列，消除素材透明边距和导入尺寸造成的偏差；设置、帮助、博士帽、相机保持同列。</p><div class="toolbar"><figure class="crop"><a href="shortcut-spacing.png" target="_blank"><img src="shortcut-spacing.png"></a><figcaption>等距效果</figcaption></figure><figure class="full"><a href="lobby-spacing.png" target="_blank"><img src="lobby-spacing.png"></a><figcaption>大厅整体效果</figcaption></figure></div><h2>本局积分结算声音</h2><p>数字开始变化时播放原版“嘀嘀嘀”；新排名随后依次出现，第一位指示出现时只“当”一次；末尾终局页无音效。</p><figure><video controls playsinline preload="metadata" src="score-audio-review.mp4"></video><figcaption>约 9 秒：数字变动 → 新排名 → 第一位“当”一次 → 终局无音效</figcaption></figure><details><summary>完整二倍役满结算（同步更新声音）</summary><video controls playsinline preload="metadata" src="double-yakuman-audio-fixed.mp4"></video></details><p><a href="previous-1017d.html">上一轮全部预览</a></p></main></html>'''
(O/'index.html').write_text(page,'utf8');(O/'review-1017e.json').write_text(json.dumps({'scoreTicks':sum(c['key']=='score_change'for c in review['events']),'scoreRank1':sum(c['key']=='rank1'and c['page']=='scores'for c in review['events']),'finalCues':sum(c['page']=='final'for c in review['events']),'finalAudioPeak':final_peak,'newVideoRecording':False,'unitTestsRun':False,'apkBuilt':False,'gitCommitted':False},indent=2)+'\n','utf8')
print(O/'index.html')
