"""Encode raw RGBA frames from the Unity recorder without a PNG frame cache."""
from pathlib import Path
import argparse,sys
R=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(R/'tools/video-inspector'))
import av,numpy as np
p=argparse.ArgumentParser();p.add_argument('--output',required=True);p.add_argument('--width',type=int,required=True);p.add_argument('--height',type=int,required=True);p.add_argument('--fps',type=int,default=30);a=p.parse_args()
size=a.width*a.height*4;frames=0
with av.open(a.output,'w',options={'movflags':'+faststart'})as container:
    stream=container.add_stream('libx264',rate=a.fps);stream.width=a.width;stream.height=a.height;stream.pix_fmt='yuv420p';stream.options={'crf':'18','preset':'fast','threads':'2'}
    while True:
        raw=sys.stdin.buffer.read(size)
        if not raw:break
        if len(raw)!=size:raise ValueError('Partial Unity frame')
        pixels=np.frombuffer(raw,dtype=np.uint8).reshape(a.height,a.width,4)[::-1].copy()
        frame=av.VideoFrame.from_ndarray(pixels,format='rgba');frame.pts=frames
        for packet in stream.encode(frame):container.mux(packet)
        frames+=1
    for packet in stream.encode():container.mux(packet)
print('ENCODED',frames,'frames',flush=True)
