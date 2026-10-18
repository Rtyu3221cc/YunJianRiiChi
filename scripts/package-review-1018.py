"""Deliver actual Unity stills and four direction clips for visual review only."""
from pathlib import Path
import hashlib,json,shutil,sys
R=Path(__file__).resolve().parent.parent;S=R/'build/review-1018/render';O=R/'dist/previews/2026-10-18-table-review-character-size';O.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(R/'tools/video-inspector'));import av
from PIL import Image,ImageDraw,ImageFont
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',28);small=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',22)
seats=['自家（下方）','右家（东方向视频）','对家（北方向视频）','左家（西方向视频）'];kinds={'riichi':'立直','chi':'吃','pon':'碰','kan':'杠','nuki':'拔北'}
notes=(S/'render-notes.txt').read_text('utf8');assert notes.strip()=='Actual Unity rendering; synthetic public table fixture; no APK or test runner.',notes
metrics=json.loads((S/'word-metrics.json').read_text('utf8'))['entries']
uniform=[]
for word in ['shout_lizhi','shout_chi','shout_peng','shout_gang','shout_babei']:
    group=[m for m in metrics if m['word']==word and m['screenWidth']==1688 and m['screenHeight']==780]
    assert len(group)==4 and {m['slot']for m in group}=={0,1,2,3},(word,group)
    assert max(m['width']for m in group)-min(m['width']for m in group)<.001,(word,'width',group)
    assert max(m['height']for m in group)-min(m['height']for m in group)<.001,(word,'height',group)
    uniform.append(dict(word=word,width=group[0]['width'],height=group[0]['height'],inkWidth=group[0]['inkWidth'],inkHeight=group[0]['inkHeight'],directions=4))
main_metrics=[m for m in metrics if m['screenWidth']==1688 and m['screenHeight']==780]
assert max(m['inkHeight']for m in main_metrics)-min(m['inkHeight']for m in main_metrics)<.001
chi=next(m for m in uniform if m['word']=='shout_chi')
for double in [m for m in uniform if m['word']in ['shout_lizhi','shout_babei']]:assert double['inkWidth']>chi['inkWidth']*1.75
wide=[m for m in metrics if m['word']=='shout_lizhi'and m['screenWidth']==1280 and m['screenHeight']==720]
assert len(wide)==4 and max(m['width']for m in wide)-min(m['width']for m in wide)<.001
shutil.copyfile(S/'word-metrics.json',O/'word-metrics.json')
(O/'uniform-word-sizes.json').write_text(json.dumps(dict(source='Actual Unity GUI transform after rendering',sameSizeAcrossDirections=True,sameVisibleCharacterHeight=True,twoCharactersExpandHorizontally=True,at1688x780=uniform,at1280x720=wide),ensure_ascii=False,indent=2),'utf8')
manifest=[]
previous=json.loads((O/'artifact-review.json').read_text('utf8')) if (O/'artifact-review.json').is_file() else {}
previous_videos={x['file']:x for x in previous.get('artifacts',[])if 'frames'in x}
motion_time=max(p.stat().st_mtime for p in S.glob('seat-*/frame-*.png'))
for path in S.glob('*.png'):
    dest=O/path.name;shutil.copyfile(path,dest)
    with Image.open(dest)as im:manifest.append(dict(file=dest.name,size=list(im.size)))

def title(im,text):
    out=Image.new('RGB',(im.width,im.height+42),'#152034');out.paste(im,(0,42));ImageDraw.Draw(out).text((14,6),text,font=font,fill='#efd0a0');return out

sheet=Image.new('RGB',(1688,864),'#152034')
for i,seat in enumerate([0,1,2,3]):
    im=Image.open(S/f'riichi-seat-{seat}.png').convert('RGB');im=im.resize((844,390),Image.Resampling.LANCZOS);panel=title(im,seats[seat]+' · 立直');sheet.paste(panel,((i%2)*844,(i//2)*432))
sheet.save(O/'four-directions.png')
# Equal physical screenshot crops at the same twofold enlargement. No fitting
# or independent scaling of each direction's lettering is used in this proof.
all_equal=Image.new('RGB',(1600,1800),'#152034')
for row,(kind,label)in enumerate(kinds.items()):
    panels=Image.new('RGB',(1600,360),'#152034');draw=ImageDraw.Draw(panels)
    for seat in range(4):
        name=f'{kind}-seat-{seat}.png';metric=next(m for m in metrics if m['image']==name)
        cx=round(metric['inkX']+metric['inkWidth']/2);cy=round(metric['inkY']+metric['inkHeight']/2)
        with Image.open(S/name)as im:crop=im.convert('RGB').crop((cx-100,cy-80,cx+100,cy+80)).resize((400,320),Image.Resampling.LANCZOS)
        panels.paste(crop,(seat*400,40));draw.text((seat*400+12,4),['自家','右家','对家','左家'][seat]+' · '+label,font=font,fill='#efd0a0')
    all_equal.paste(panels,(0,row*360))
    if row==0:panels.save(O/'four-directions-equal-size.png')
all_equal.save(O/'all-actions-equal-size.png')
word_strip=Image.new('RGB',(1950,372),'#152034');word_draw=ImageDraw.Draw(word_strip)
for i,(kind,label)in enumerate(kinds.items()):
    metric=next(m for m in metrics if m['image']==f'{kind}-seat-3.png');cx=round(metric['inkX']+metric['inkWidth']/2);cy=round(metric['inkY']+metric['inkHeight']/2)
    with Image.open(S/f'{kind}-seat-3.png')as picture:
        crop=picture.convert('RGB').crop((cx-130,cy-110,cx+130,cy+110)).resize((390,330),Image.Resampling.LANCZOS)
    word_strip.paste(crop,(i*390,42));word_draw.text((i*390+16,5),label+' · 实际渲染',font=font,fill='#efd0a0')
word_strip.save(O/'words-actual.png')
shutil.copyfile(R/'build/review-1018/reference/full-word-sprites.png',O/'apk-word-sprites.png')
after=Image.open(S/'table-1688.png').convert('RGB')
detail=Image.new('RGB',(1580,472),'#152034');draw=ImageDraw.Draw(detail)
draw.text((24,16),'牌面居中 · Unity 实际画面局部放大',font=font,fill='#efd0a0')
draw.text((1160,16),'掉线闪电上移 5px',font=font,fill='#efd0a0')
river=after.crop((700,383,1005,503)).resize((1068,420),Image.Resampling.NEAREST);detail.paste(river,(24,52))
avatar=after.crop((1495,390,1620,540)).resize((325,390),Image.Resampling.NEAREST);detail.paste(avatar,(1190,65));detail.save(O/'tile-and-offline-details.png')

def encode(path,frames,size):
    old=previous_videos.get(path.name)
    if old and path.is_file() and path.stat().st_mtime>=motion_time and hashlib.sha256(path.read_bytes()).hexdigest()==old['sha256']:
        manifest.append(old);print('Reusing previously decoded video:',path.name,flush=True);return
    with av.open(str(path),'w',options={'movflags':'+faststart'})as c:
        stream=c.add_stream('libx264',rate=30);stream.width,stream.height=size;stream.pix_fmt='yuv420p';stream.options={'crf':'18','preset':'fast'}
        count=0
        for i,im in enumerate(frames):
            f=av.VideoFrame.from_image(im.convert('RGB'));f.pts=i
            for p in stream.encode(f):c.mux(p)
            count+=1
        for p in stream.encode():c.mux(p)
    with av.open(str(path))as c:
        decoded=sum(1 for _ in c.decode(video=0));assert decoded==count,(path,decoded,count)
    manifest.append(dict(file=path.name,size=list(size),fps=30,frames=count,decoded=decoded,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    print(path.name,count,'frames, decode complete',flush=True)

for seat in range(4):
    files=sorted((S/f'seat-{seat}').glob('frame-*.png'));assert len(files)==135,(seat,len(files))
    def frames():
        for p in files:
            with Image.open(p)as im:yield im.copy()
    encode(O/f'seat-{seat}.mp4',frames(),(1280,592))

def combined():
    for i in range(135):
        out=Image.new('RGB',(2560,1268),'#152034');draw=ImageDraw.Draw(out)
        action=list(kinds.values())[i//27]
        for seat in range(4):
            x=(seat%2)*1280;y=(seat//2)*634;draw.text((x+18,y+4),seats[seat]+' · '+action,font=font,fill='#efd0a0')
            with Image.open(S/f'seat-{seat}/frame-{i:04d}.png')as im:out.paste(im.convert('RGB'),(x,y+42))
        yield out
encode(O/'four-directions.mp4',combined(),(2560,1268))
source=json.loads((R/'unity/Assets/Resources/Majsoul/Callouts1018/source.json').read_text('utf8'))
for rec in source['assets']:
    assert hashlib.sha256((R/'unity/Assets/Resources/Majsoul/Callouts1018'/rec['output']).read_bytes()).hexdigest()==rec['sha256']
(O/'artifact-review.json').write_text(json.dumps(dict(render='Unity Editor actual frames; deterministic public table fixture',apkBuilt=False,artwork='Supplied Mahjong Soul APK; native four-direction hierarchy and alpha masks',artworkHashesVerified=True,artifacts=manifest),ensure_ascii=False,indent=2),'utf8')
cards=''.join(f'<figure><video controls playsinline muted loop preload="metadata" poster="riichi-seat-{seat}.png" src="seat-{seat}.mp4"></video><figcaption>{label}：立直 → 吃 → 碰 → 杠 → 拔北</figcaption></figure>'for seat,label in enumerate(seats))
stills=''.join('<section><h2>'+label+' · 四个方位</h2><div class="grid">'+''.join(f'<figure><a href="{kind}-seat-{seat}.png" target="_blank"><img src="{kind}-seat-{seat}.png"></a><figcaption>{place} · {label}</figcaption></figure>'for seat,place in enumerate(seats))+'</div></section>'for kind,label in kinds.items())
html='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 · 10月18日牌桌预览</title><style>body{margin:0;background:#101a2a;color:#e8edf5;font:16px/1.8 "Microsoft YaHei",sans-serif}main{max-width:1688px;margin:auto;padding:24px}h1,h2,a{color:#efd0a0}h1{font-size:28px}h2{font-size:22px;margin-top:32px}p{color:#b9c7dc}img,video{display:block;width:100%}figure{margin:0;background:#1b2c43;border-radius:6px;overflow:hidden}figcaption{padding:9px 14px}.grid{display:grid;grid-template-columns:1fr 1fr;gap:18px}.note{padding:12px 18px;background:#1c2b42;border-left:3px solid #cfaa6b}@media(max-width:850px){main{padding:14px}.grid{grid-template-columns:1fr}}</style><main><h1>云间日麻 · 10月18日牌桌修改预览</h1><p class="note">均为修改后 Unity 实际渲染，使用固定演示牌局。视频为正常速度：立直、吃、碰、杠、拔北依次播放。仅效果预览，未生成安装包。</p><h2>四方位动作提示 · 同步视频</h2><video controls playsinline muted loop preload="metadata" poster="four-directions.png" src="four-directions.mp4"></video><h2>牌面居中与掉线闪电</h2><a href="tile-and-offline-details.png" target="_blank"><img src="tile-and-offline-details.png"></a><p>点击图片查看原图。</p><a href="table-1688.png" target="_blank"><img src="table-1688.png"></a><h2>每个方位的独立视频</h2><div class="grid">'''+cards+'''</div>'''+stills+'''<h2>三人场 · 拔北</h2><div class="grid">'''+''.join(f'<figure><a href="sanma-nuki-seat-{seat}.png" target="_blank"><img src="sanma-nuki-seat-{seat}.png"></a><figcaption>{["自家","对家","左家"][seat]}</figcaption></figure>'for seat in range(3))+'''</div><h2>16:9 画面</h2><a href="north-16x9.png" target="_blank"><img src="north-16x9.png"></a></main></html>'''
audit='<h2>全部 37 种牌面 · 四方向展示</h2><a href="all-37-faces.png" target="_blank"><img src="all-37-faces.png"></a>'
for kind,label in [('ron','荣和'),('tsumo','自摸')]:
    audit+='<h2>'+label+' · 原有提示器四方向复核</h2><div class="grid">'+''.join(f'<figure><a href="{kind}-seat-{seat}.png" target="_blank"><img src="{kind}-seat-{seat}.png"></a><figcaption>{seats[seat]} · {label}</figcaption></figure>'for seat in range(4))+'</div>'
html=html.replace('</main>',audit+'</main>')
text_note='<p class="note">按每个字的可见高度统一大小：“立直、拔北”按原比例展开为双字宽度。相同提示在四个方向保持一致。保留原始完整图与透明边距，未拉伸字形。</p><h2>单字与双字 · 同字号实际截图</h2><a href="words-actual.png" target="_blank"><img src="words-actual.png"></a><h2>四方向同字号 · 等倍率实际截图</h2><p>每个局部均取自同分辨率画面的 200×160 像素区域，统一放大两倍。</p><a href="four-directions-equal-size.png" target="_blank"><img src="four-directions-equal-size.png"></a><details><summary>展开五种文字的四方向对照</summary><img src="all-actions-equal-size.png"></details><h2>APK 原始 Sprite（边框表示完整画布）</h2><img src="apk-word-sprites.png">'
html=html.replace('<h2>四方位动作提示',text_note+'<h2>四方位动作提示',1)
(O/'index.html').write_text(html,'utf8')
import re
for file in re.findall(r'(?:src|href)="([^"]+)"',html):assert (O/file).is_file(),file
print('Gallery media links verified; native artwork hashes match.',flush=True)
print('Preview gallery:',O/'index.html',flush=True)
