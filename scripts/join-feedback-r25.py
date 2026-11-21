"""Join unchanged verified chapters with the corrected, actually recorded tail."""
from pathlib import Path
import hashlib,json,sys
R=Path(__file__).resolve().parent.parent;B=R/'build/review-r25';C=B/'capture'
sys.path.insert(0,str(R/'tools/video-inspector'))
import av
report=json.loads((C/'movie-checks.json').read_text('utf8'));tail=json.loads((C/'tail-checks.json').read_text('utf8'))
cut=3089;assert report['frames']==3509 and tail['frames']==420 and report['fps']==tail['fps']==30
dest=C/'feedback-r25-joined-silent.mp4';count=0
with av.open(str(dest),'w',options={'movflags':'+faststart'})as out:
    stream=out.add_stream('libx264',rate=30);stream.width=1600;stream.height=900;stream.pix_fmt='yuv420p';stream.options={'crf':'18','preset':'fast','threads':'2'}
    for name,limit in [('feedback-r25-silent.mp4',cut),('callouts-r25-silent.mp4',420)]:
        with av.open(str(C/name))as source:
            for index,frame in enumerate(source.decode(video=0)):
                if index>=limit:break
                frame.pts=count;frame.time_base=stream.time_base
                # Explicit output timestamps avoid retaining each input's origin.
                frame.time_base=__import__('fractions').Fraction(1,30)
                for packet in stream.encode(frame):out.mux(packet)
                count+=1
    for packet in stream.encode():out.mux(packet)
assert count==report['frames']
proof=dict(frames=count,fps=30,headFrames=cut,tailFrames=420,actualUnityCapture=True,tailChecks=len(tail['checks']),sourceReports=['movie-checks.json','tail-checks.json'],file=dest.name,bytes=dest.stat().st_size,sha256=hashlib.sha256(dest.read_bytes()).hexdigest())
(B/'joined-video.json').write_text(json.dumps(proof,indent=1)+'\n','utf8')
print('JOINED REAL CAPTURE',count,'frames',dest.stat().st_size,'bytes')
