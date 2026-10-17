from pathlib import Path
import json,sys
R=Path(__file__).resolve().parent.parent;O=R/'build/review-1017d/reference';O.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(R/'tools/video-inspector'));import av
from PIL import Image,ImageDraw,ImageFont
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',18)
for name,source in [('riichi','D:/MyWorkSpace/video/1013/对家立直出现的问题.mp4'),('indicator','D:/MyWorkSpace/video/1013/指示器顺序问题.mp4'),('ron','D:/MyWorkSpace/video/自己荣和.mp4')]:
    with av.open(source)as video:
        s=video.streams.video[0];duration=float(s.duration*s.time_base);print(name,'seconds',duration,'size',s.width,s.height)
        targets=[duration*i/11 for i in range(12)];frames=[]
        for frame in video.decode(video=0):
            if targets and frame.time>=targets[0]-.04:
                targets.pop(0);image=frame.to_image().convert('RGB');image.save(O/(name+'-'+str(len(frames))+'.png'));image.thumbnail((600,300));frames.append((image,frame.time))
            if not targets:break
    sheet=Image.new('RGB',(1200,6*330),'#152034');draw=ImageDraw.Draw(sheet)
    for i,(image,at)in enumerate(frames):
        x=(i%2)*600;y=(i//2)*330;sheet.paste(image,(x,y+25));draw.text((x+8,y),f'{at:.3f}s',font=font,fill='white')
    sheet.save(O/(name+'-contact.jpg'))
rows=[json.loads(l)for name in ['catalog.jsonl','fast-catalog.jsonl']for l in(R/'build/reference'/name).read_text('utf8').splitlines()]
paths=sorted(set(p for r in rows for p in r.get('paths',[])))
print('LOBBY BUTTONS',[Path(p).name for p in paths if '/ui/lobby/main/pic/'in p and Path(p).name.startswith('btn')])
print('MASK CANDIDATES',[p for p in paths if '/ui/mj/'in p and any(t in p.lower()for t in ['offline','tuoguan','auto','disconnect','afk','lost','entrust'])])
ui=json.loads((R/'build/reference/tile-study/desktop-ui.json').read_text('utf8'))
for n in ui:
    if any(t in n['path'].lower()for t in ['offline','auto','disconnect','leave','state','afk']):print('HUD NODE',n['path'])
