"""Mux bundled effect sounds at actual recorded Unity cue times."""
from fractions import Fraction
from pathlib import Path
import json,sys
R=Path(__file__).resolve().parent.parent;sys.path.insert(0,str(R/'tools/video-inspector'))
import av,numpy as np
def mux(source_video,recording,target):
    rate=48000;audio=np.zeros((2,round(recording['frames']/recording['fps']*rate)),dtype=np.float32)
    for cue in recording['cues']:
        assert cue['cue'] and '/'not in cue['cue'] and '\\'not in cue['cue']
        pieces=[];resample=av.AudioResampler(format='fltp',layout='stereo',rate=rate)
        clips=[R/'unity/Assets/Resources/Majsoul/Audio'/(cue['cue']+ext)for ext in ['.mp3','.wav','.ogg']]
        source_clip=next((p for p in clips if p.is_file()),None)
        if source_clip is None:raise FileNotFoundError('Missing bundled audio cue '+cue['cue'])
        with av.open(str(source_clip))as source:
            for frame in source.decode(audio=0):pieces.extend(f.to_ndarray()for f in resample.resample(frame))
            pieces.extend(f.to_ndarray()for f in resample.resample(None))
        signal=np.concatenate(pieces,axis=1);start=round(cue['time']*rate);count=min(signal.shape[1],audio.shape[1]-start);audio[:,start:start+count]+=signal[:,:count]*.85
    np.clip(audio,-1,1,out=audio)
    with av.open(str(source_video))as source,av.open(str(target),'w',options={'movflags':'+faststart'})as output:
        video=output.add_stream_from_template(source.streams.video[0]);sound=output.add_stream('aac',rate=rate);sound.layout='stereo';sound.bit_rate=160000;packets=[]
        for packet in source.demux(video=0):
            if packet.dts is not None:packet.stream=video;packets.append(packet)
        for start in range(0,audio.shape[1],1024):
            f=av.AudioFrame.from_ndarray(np.ascontiguousarray(audio[:,start:start+1024]),format='fltp',layout='stereo');f.sample_rate=rate;f.pts=start;f.time_base=Fraction(1,rate);packets.extend(sound.encode(f))
        packets.extend(sound.encode(None));packets.sort(key=lambda p:float(p.dts*p.time_base)if p.dts is not None else 0)
        for packet in packets:output.mux(packet)
if __name__=='__main__':mux(Path(sys.argv[1]),json.loads(Path(sys.argv[2]).read_text('utf8')),Path(sys.argv[3]))
