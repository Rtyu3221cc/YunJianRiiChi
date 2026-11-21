"""Measure temporal alignment using rendered scene edges, not edited poses."""
from pathlib import Path
import sys,json
R=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(R/'tools/video-inspector'))
import av,cv2,numpy as np
from PIL import Image,ImageDraw,ImageFont
B=R/'build/yakuman-alignment-r40';C=B/'capture'
cases=json.loads((R/'tests/fixtures/yakuman-video-1114.json').read_text('utf8'))['cases']
source='E:/MyWorkSpace_E/video/1114/codex/役滿特效.mp4'
refs={c['key']:[] for c in cases};case_index=0
for f in av.open(source).decode(video=0):
    t=float(f.time)
    while case_index<len(cases)-1 and t>=cases[case_index]['sourceEnd']:case_index+=1
    c=cases[case_index];rows=refs[c['key']]
    if rows and t-rows[-1][0]<.095:continue
    a=f.to_ndarray(format='rgb24')[:,244:2316]
    rows.append((t,cv2.resize(a,(320,180),interpolation=cv2.INTER_AREA)))
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',16)
results=[];sheets=[]
def feature(im):
    gray=cv2.cvtColor(im,cv2.COLOR_RGB2GRAY).astype(np.float32)/255
    # Avoid the replaced portrait, changing tile faces and source watermark.
    mask=np.ones((180,320),np.float32);mask[25:152,97:212]=0;mask[145:]=0;mask[:25,230:]=0;mask[:3]=0
    gx=cv2.Sobel(gray,cv2.CV_32F,1,0);gy=cv2.Sobel(gray,cv2.CV_32F,0,1)
    out=np.concatenate(((gray-.5)*mask,gx*mask,gy*mask),axis=0).ravel()
    return out/max(np.linalg.norm(out),1e-6)
for c in cases:
    path=C/(c['key']+'.mp4')
    if not path.exists():continue
    try:
        native=[(float(f.time),cv2.resize(f.to_ndarray(format='rgb24'),(320,180),interpolation=cv2.INTER_AREA)) for i,f in enumerate(av.open(str(path)).decode(video=0)) if i%3==0]
    except Exception as e:print(c['key'],str(e));continue
    rt=np.array([v[0]for v in refs[c['key']]]);rf=np.array([feature(v[1])for v in refs[c['key']]])
    nt=np.array([v[0]for v in native]);nf=np.array([feature(v[1])for v in native]);sim=nf@rf.T
    # Fit only motion before the static ending; no retiming in the deliverable.
    use=(nt>=2.55)&(nt<=7.35);best=None
    for offset in np.arange(c['sourceScene']-2.15-.8,c['sourceScene']-2.15+.81,.025):
        j=np.abs(rt[None,:]-(nt[:,None]+offset)).argmin(axis=1)
        good=use&(nt+offset>c['sourceStart'])&(nt+offset<c['sourceEnd'])
        score=float(np.mean(sim[np.where(good)[0],j[good]]))
        if best is None or score>best[0]:best=(score,float(offset))
    score,offset=best
    # Report matching quality at multiple phases for individual human review.
    sheet=Image.new('RGB',(1280,4*210),'#172130');d=ImageDraw.Draw(sheet)
    for row,age in enumerate([2.8,4.3,6.4,8.8]):
        ni=int(np.abs(nt-age).argmin());ri=int(np.abs(rt-(nt[ni]+offset)).argmin())
        d.text((4,row*210+2),f"{c['name']}  native {nt[ni]:.2f}s / source {rt[ri]:.2f}s  offset {offset:.3f}  score {score:.3f}",font=font,fill='white')
        for col,im in enumerate([refs[c['key']][ri][1],native[ni][1]]):sheet.paste(Image.fromarray(im).resize((320,180)),(col*320,row*210+25))
    # compact sheet dimensions; preserve 320x180 panels
    sheet=sheet.crop((0,0,640,840));sheet.save(B/(c['key']+'-phases.jpg'),quality=94)
    results.append(dict(key=c['key'],name=c['name'],offset=offset,sceneAt=offset+2.15,score=score))
    print(c['key'],offset,score,flush=True)
    age=8.8;ni=int(np.abs(nt-age).argmin());ri=int(np.abs(rt-(nt[ni]+offset)).argmin())
    pair=Image.new('RGB',(640,208),'#172130');d=ImageDraw.Draw(pair);d.text((4,1),c['name'],font=font,fill='white');pair.paste(Image.fromarray(refs[c['key']][ri][1]),(0,27));pair.paste(Image.fromarray(native[ni][1]),(320,27));sheets.append(pair)
(B/'alignment.json').write_text(json.dumps(results,ensure_ascii=False,indent=2)+'\n','utf8')
sheet=Image.new('RGB',(1280,((len(sheets)+1)//2)*208),'#172130')
for i,im in enumerate(sheets):sheet.paste(im,((i%2)*640,(i//2)*208))
sheet.save(B/'all-scenes-comparison.jpg',quality=95)
