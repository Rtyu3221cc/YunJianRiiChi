"""Read the supplied videos and list the APK assets relevant to this review."""
from pathlib import Path
import json,sys
ROOT=Path(__file__).resolve().parent.parent
OUT=ROOT/'build/review-1017b/reference';OUT.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(ROOT/'tools/video-inspector'))
import av
from PIL import Image,ImageDraw,ImageFont
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',18)
for name in ['役满机会','役满确定']:
    source=Path('D:/MyWorkSpace/video/1013')/(name+'.mp4')
    with av.open(str(source)) as clip:
        stream=clip.streams.video[0];duration=float(stream.duration*stream.time_base)
        print(name,'duration',duration,'size',stream.width,stream.height)
        targets=[duration*i/7 for i in range(8)];frames=[]
        for frame in clip.decode(video=0):
            at=float(frame.time)
            if targets and at>=targets[0]-.04:
                target=targets.pop(0);pic=frame.to_image().convert('RGB');pic.save(OUT/(name+'-'+str(len(frames))+'.png'))
                pic.thumbnail((600,320));frames.append((pic,at))
            if not targets:break
    contact=Image.new('RGB',(1200,4*350),'#152034');draw=ImageDraw.Draw(contact)
    for i,(pic,at) in enumerate(frames):
        x=(i%2)*600;y=(i//2)*350;contact.paste(pic,(x,y+25));draw.text((x+8,y),f'{at:.2f}s',font=font,fill='white')
    contact.save(OUT/(name+'-contact.jpg'))
seen=set();hits=[]
for file in ['catalog.jsonl','fast-catalog.jsonl']:
    for line in (ROOT/'build/reference'/file).read_text('utf8').splitlines():
        row=json.loads(line)
        for path in row.get('paths',[]):
            if any(term in path.lower() for term in ['yiman','yakuman','daisan','guoshi']):
                if (row['bundle'],path) not in seen:
                    seen.add((row['bundle'],path));hits.append({'bundle':row['bundle'],'path':path})
(OUT/'apk-yakuman-assets.json').write_text(json.dumps(hits,ensure_ascii=False,indent=2),'utf8')
print('APK yakuman matches',len(hits))
for row in hits:
    if 'lua' in row['path'] or 'prefab' in row['path'] or 'animation' in row['path']:print(row['path'])
