"""Actual Unity footage next to the user's original reference recordings."""
from pathlib import Path
import base64,html,io,json,shutil,sys
from PIL import Image,ImageDraw,ImageFont,ImageOps
R=Path(__file__).resolve().parent.parent;sys.path.insert(0,str(R/'tools/video-inspector'));import av
source=R/'build/wardrobe-preview-r4/render';out=R/'dist/previews/2026-10-25-wardrobe-r4';out.mkdir(parents=True,exist_ok=True)
report=json.loads((source/'capture.json').read_text('utf8'));fps=report['fps'];font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',25)
def encode(path,images):
    with av.open(str(path),'w',options={'movflags':'+faststart'}) as c:
        s=c.add_stream('libx264',rate=fps);s.width=1688;s.height=768;s.pix_fmt='yuv420p';s.options={'crf':'19','preset':'medium'}
        for i,image in enumerate(images):
            frame=av.VideoFrame.from_image(image.convert('RGB'));frame.pts=i
            for p in s.encode(frame):c.mux(p)
        for p in s.encode():c.mux(p)
def frames(start,end):
    for i in range(start,end):
        with Image.open(source/'frames'/f'{i:05d}.jpg') as image:yield image.copy()
if '--pages-only' not in sys.argv:
    encode(out/'effects-comparison-demo.mp4',frames(0,report['frames']))
    for ch in report['chapters']:encode(out/(ch['name']+'.mp4'),frames(ch['start'],ch['end']))
refs=[('callout-position','鸣牌特效位置.mp4','mpzs_22event-seat-2',2.6),('lightning','雷驰电掣.mp4','mpzs_shandian-seat-1',4.2),('flame','烈焰和牌特效.mp4','ron-flame',.65),('ice','冰霜立直特效.mp4','riichi-ice',.6),('tornado','荣牌特效.mp4','ron-tornado',1.7)]
for key,name,chapter,at in refs:
    native=Path('D:/MyWorkSpace/video/1024')/name
    if not native.exists():native=Path('D:/MyWorkSpace/video')/name
    def native_frames():
        index=0
        with av.open(str(native)) as c:
            for f in c.decode(video=0):
                t=float(f.pts*f.time_base)
                if t+1e-4<index/fps:continue
                image=f.to_image()
                if image.height>image.width:image=image.transpose(Image.Transpose.ROTATE_90)
                image=ImageOps.pad(image,(1688,768),color='black');index+=1
                yield image
    if '--pages-only' not in sys.argv:encode(out/('reference-'+key+'.mp4'),native_frames())
    with av.open(str(out/('reference-'+key+'.mp4'))) as c:
        for f in c.decode(video=0):
            if float(f.pts*f.time_base)>=at:native_image=f.to_image();break
    ch=next(c for c in report['chapters'] if c['name']==chapter);frame=ch['start']+min(10,ch['end']-ch['start']-1)
    with Image.open(source/'frames'/f'{frame:05d}.jpg') as im:current=im.copy();current.save(out/(chapter+'.png'))
    sheet=Image.new('RGB',(1688,1640),'#17151f');draw=ImageDraw.Draw(sheet)
    for row,(image,title) in enumerate([(native_image,'原版参考视频 · '+name),(current,'本轮 Unity 实际画面')]):y=row*820;draw.text((20,y+8),title,font=font,fill='#f0d1a0');sheet.paste(image,(0,y+48))
    sheet.save(out/('compare-'+key+'.jpg'),quality=92)
for name in ['default-stick','stick-list','stick-preview']:
    shutil.copyfile(source/(name+'.png'),out/(name+'.png'))
shutil.copyfile(source/'capture.json',out/'capture.json');shutil.copyfile(source/'checks.txt',out/'checks.txt')
def uri(file,kind):return 'data:'+kind+';base64,'+base64.b64encode(file.read_bytes()).decode()
def picture(file,single):
    if not single:return file.name
    with Image.open(file) as image:
        image=image.convert('RGB');image.thumbnail((1280,1280));b=io.BytesIO();image.save(b,format='JPEG',quality=86)
    return 'data:image/jpeg;base64,'+base64.b64encode(b.getvalue()).decode()
def page(single):
    def video(name):return uri(out/name,'video/mp4') if single else name
    nav=''.join('<button data-time="'+str(ch['start']/fps)+'">'+html.escape(ch['title'])+'</button>' for ch in report['chapters'])
    comparisons=''
    for key,name,chapter,at in refs:
        comparisons+='<details><summary>'+html.escape(name[:-4])+' · 原版与本轮对照</summary><div class="pair"><figure><video controls playsinline preload="none" src="'+video('reference-'+key+'.mp4')+'"></video><figcaption>原版参考</figcaption></figure><figure><video controls playsinline preload="none" src="'+video(chapter+'.mp4')+'"></video><figcaption>本轮 Unity 运行</figcaption></figure></div><img loading="lazy" src="'+picture(out/('compare-'+key+'.jpg'),single)+'"></details>'
    pictures=''.join('<figure><img src="'+picture(out/(name+'.png'),single)+'"><figcaption>'+title+'</figcaption></figure>' for name,title in [('default-stick','牌桌使用普通立直棒'),('stick-list','普通立直棒恢复为默认选项'),('stick-preview','普通立直棒模型预览')])
    install='APK 单独提供，请安装 0.6.12 测试包。' if single else '<a href="../../yunjian-riichi-unity-0.6.12.apk">下载 0.6.12 APK</a>'
    return '''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 0.6.12 · 特效原版对照</title><style>body{margin:0;background:#191720;color:#f0e1c6;font:16px/1.65 system-ui,sans-serif}main{max-width:1280px;margin:auto;padding:16px}h1{font-size:25px}video,img{display:block;width:100%;border-radius:8px;background:#252431}nav{display:flex;flex-wrap:wrap;gap:8px;margin:15px 0}button,summary{cursor:pointer;background:#302a38;color:#efd09b;border:1px solid #846c48;border-radius:6px;padding:10px;font:inherit}details{margin:16px 0}figure{margin:12px 0}figcaption{color:#c9ac82;padding:5px 0}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px}a{color:#efca83}.note{color:#bdb3c8;font-size:14px}@media(max-width:700px){.pair{grid-template-columns:1fr}}</style><main><h1>0.6.12 · 特效修正与原版对照</h1><p>鸣牌指示改为牌河中的可响应弃牌；烈焰与爆炎龙卷保留向上的火柱、旋涡、原始网格和曲线；冰霜使用竖式动作与固定桌面冰环；普通立直棒恢复为默认选项。</p><p>'''+install+'''</p><video id="player" controls playsinline preload="metadata" src="'''+video('effects-comparison-demo.mp4')+'''"></video><nav>'''+nav+'''</nav><p class="note">本轮画面来自 Unity 编辑器实际运行。参考录屏来自你提供的视频；对照保留各自的视角和牌桌布局。视频无录音，APK 内包含原始特效音效。</p>'''+comparisons+'''<details><summary>普通立直棒</summary>'''+pictures+'''</details><script>const player=document.querySelector('#player');document.querySelectorAll('[data-time]').forEach(b=>b.onclick=()=>{const seek=()=>{player.currentTime=Number(b.dataset.time);player.play()};if(player.readyState)seek();else player.addEventListener('loadedmetadata',seek,{once:true})})</script></main></html>'''
(out/'index.html').write_text(page(False),'utf8');(out/'手机版原版对照.html').write_text(page(True),'utf8')
print('Packaged',report['frames']/fps,'seconds;',len(report['chapters']),'chapters; 5 native reference videos',flush=True)
