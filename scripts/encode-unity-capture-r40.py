"""Encode actual Unity RGB frames streamed over stdin; no generated imagery."""
from pathlib import Path
import sys,argparse
from fractions import Fraction
R=Path(__file__).resolve().parent.parent;sys.path.insert(0,str(R/'tools/video-inspector'))
import av,numpy as np
p=argparse.ArgumentParser();p.add_argument('--output',required=True);p.add_argument('--width',type=int,default=960);p.add_argument('--height',type=int,default=540);p.add_argument('--fps',type=int,default=30);a=p.parse_args()
container=av.open(a.output,'w',options={'movflags':'+faststart'});stream=container.add_stream('libx264',rate=a.fps);stream.width=a.width;stream.height=a.height;stream.pix_fmt='yuv420p';stream.options={'crf':'17','preset':'fast'}
size=a.width*a.height*3;count=0
while True:
 raw=sys.stdin.buffer.read(size)
 if not raw:break
 if len(raw)!=size:raise RuntimeError(f'Partial frame {count}: {len(raw)}/{size}')
 pixels=np.frombuffer(raw,dtype=np.uint8).reshape(a.height,a.width,3)[::-1].copy();frame=av.VideoFrame.from_ndarray(pixels,format='rgb24');frame.pts=count;frame.time_base=Fraction(1,a.fps)
 for packet in stream.encode(frame):container.mux(packet)
 count+=1
for packet in stream.encode():container.mux(packet)
container.close();print('ENCODED',count,'FRAMES',flush=True)
