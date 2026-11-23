from pathlib import Path
from fractions import Fraction
import sys,json,hashlib,shutil
R=Path(__file__).resolve().parents[1];B=R/'build/visual-feedback-r11';W=B
sys.path.insert(0,str(R/'tools/video-inspector'))
import av
from PIL import Image,ImageDraw,ImageFont,ImageOps
D=R/'dist/previews/2026-11-23-visual-feedback-r11';D.mkdir(parents=True,exist_ok=True)
font=lambda n:ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',n)
def frames(folder,start,count):
    c=av.open(str(B/folder/'feedback-raw.mp4'));s=c.streams.video[0]
    c.seek(int(Fraction(start,20)/s.time_base),stream=s)
    emitted=0
    for f in c.decode(s):
        index=round(float(f.pts*s.time_base)*20)
        if index<start:continue
        if index>=start+count:break
        emitted+=1;yield ImageOps.flip(f.to_image()).convert('RGB')
    c.close();assert emitted==count,(folder,start,count,emitted)
class Video:
    def __init__(self,name,size):
        self.path=D/name;self.c=av.open(str(self.path),'w',options={'movflags':'+faststart'});self.s=self.c.add_stream('libx264',rate=20)
        self.s.width,self.s.height=size;self.s.pix_fmt='yuv420p';self.s.options={'crf':'18','preset':'fast'};self.n=0;self.size=size
    def put(self,im,repeat=1):
        for _ in range(repeat):
            f=av.VideoFrame.from_image(im);f.pts=self.n;f.time_base=Fraction(1,20);self.n+=1
            for p in self.s.encode(f):self.c.mux(p)
    def finish(self):
        for p in self.s.encode():self.c.mux(p)
        self.c.close()
        c=av.open(str(self.path));count=0
        for f in c.decode(video=0):
            assert (f.width,f.height)==self.size
            if count==0:f.to_image().save(self.path.with_suffix('.jpg'),quality=92)
            count+=1
        c.close();assert count==self.n
        return dict(file=self.path.name,frames=count,fps=20,seconds=count/20,width=self.size[0],height=self.size[1],bytes=self.path.stat().st_size,sha256=hashlib.sha256(self.path.read_bytes()).hexdigest(),allFramesDecoded=True)
def base(title,size=(1920,1080)):
    im=Image.new('RGB',size,(10,19,31));d=ImageDraw.Draw(im);d.text((28,18),title,font=font(36),fill=(237,196,132));return im,d
def label(d,text,xy,color=(213,221,232)):d.text(xy,text,font=font(28),fill=color)
def fit(im,size):return im.resize(size,Image.Resampling.LANCZOS)
reports=[]
v=Video('立直棒六款-颜色与尺寸修正对比.mp4',(1920,1080))
names=['乘风破浪','冰糖葫芦（咸）','夹心猫团子','波子汽水','柴犬团子','喵星棒']
for k,name in enumerate(names):
    for old,new in zip(frames('before-complete',k*40,40),frames('final-review-2',k*40,40)):
        im,d=base('立直棒 · '+name+f'   {k+1} / 6')
        label(d,'修正前',(32,85));label(d,'修正后',(992,85))
        im.paste(fit(old,(880,495)),(24,130));im.paste(fit(new,(880,495)),(984,130))
        for x,f in [(210,old),(1170,new)]:im.paste(fit(f.crop((655,245,942,453)),(552,400)),(x,650))
        v.put(im,2)
reports.append(v.finish())
v=Video('动态皮肤四套-原版构图修正对比.mp4',(1920,660))
for k,name in enumerate(['芙莉莲','菲伦','休塔尔克','尤贝尔']):
    for old,new in zip(frames('before-home',k*60,60),frames('final-review-2',240+k*60,60)):
        im,d=base(name+' · 于遥远之地 · 大厅实际渲染',(1920,660));label(d,'修正前',(24,83));label(d,'修正后 · 原版 v2 画布与锚点',(984,83))
        im.paste(fit(old,(928,522)),(24,128));im.paste(fit(new,(928,522)),(984,128));v.put(im,2)
reports.append(v.finish())
v=Video('段位结算-进度与辉光对齐修正.mp4',(1920,1080))
for old,new in zip(frames('before-complete',480,100),frames('final-review-2',480,100)):
    im,d=base('段位结算 · 进度 1% → 99% · 演示数据')
    label(d,'修正前',(24,80));label(d,'修正后 · 恢复原图透明画布偏移',(984,80))
    im.paste(fit(old,(880,495)),(24,125));im.paste(fit(new,(880,495)),(984,125))
    for x,f in [(335,old),(1295,new)]:im.paste(fit(f.crop((943,234,1322,628)),(413,430)),(x,635))
    v.put(im,2)
reports.append(v.finish())
v=Video('变出花田的魔法-四方向同帧对照.mp4',(1920,1240))
names=['自家','下家','对家','上家']
streams=[frames('final-review-2',580+i*120,120)for i in range(4)]
for k,group in enumerate(zip(*streams)):
    im,d=base('变出花田的魔法 · 原版材质顺序修复 · 四方向相同时刻',(1920,1240))
    label(d,f'特效时间 {k/20:.2f} 秒 · Unity 实际渲染 · 保留原版场景遮罩与淡入淡出',(28,75))
    for i,f in enumerate(group):
        x=24+(i%2)*960;y=130+(i//2)*545
        label(d,names[i],(x,y));im.paste(fit(f,(928,522)),(x,y+34))
    v.put(im,2)
reports.append(v.finish())
for f in ['dynamic-comparison.jpg','stick-comparison.jpg','rank-comparison.jpg']:shutil.copyfile(W/f,D/f)
(D/'verification.json').write_text(json.dumps(reports,ensure_ascii=False,indent=2)+'\n','utf8')
print(json.dumps(reports,ensure_ascii=False,indent=2))
