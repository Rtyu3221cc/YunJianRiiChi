"""Package verified Unity screenshots for the November 14 feedback."""
from pathlib import Path
import html,json,shutil
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parent.parent;B=R/'build/feedback-1114-r39';O=R/'dist/previews/2026-11-14-effects-and-portraits';O.mkdir(parents=True,exist_ok=True)
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',26);small=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',18)
def pair(name,images,boxes,labels):
    out=Image.new('RGB',(1200,80+len(boxes)*370),'#172130');d=ImageDraw.Draw(out);d.text((20,10),name,font=font,fill='white');d.text((20,48),'修改前',font=small,fill='#bacbdd');d.text((620,48),'修改后',font=small,fill='#bacbdd')
    for row,(box,label)in enumerate(zip(boxes,labels)):
        for col,im in enumerate(images):
            shot=im.crop(box).convert('RGB');shot.thumbnail((580,330));out.paste(shot,(col*600+10+(580-shot.width)//2,80+row*370));d.text((col*600+20,80+row*370+335),label,font=small,fill='white')
    return out
pair('立直 · 虚影祝祷 · 同一时点', [Image.open(B/m/'fate-0-1.1.png')for m in ['before','after']],[(550,250,1050,620)],['独立牌身／牌面，去掉遮挡牌面的 Always 描边']).save(O/'fate-before-after.jpg',quality=95)
pair('开局立绘 · 同一分辨率／同一原图', [Image.open(B/m/'intro-2760x1256.png')for m in ['before','after']],[(330,290,790,720),(2000,180,2460,610),(1210,760,1670,1190)],['福姬 · 缘结祈岁','小鸟游雏田 · 无拘乐趣','Saber · 契约']).save(O/'portraits-before-after.jpg',quality=95)
for mode in ['before','after']:
    for p in(B/mode).glob('*.png'):
        if p.name.startswith(('fate-','intro-')):shutil.copyfile(p,O/(mode+'-'+p.name))
report=json.loads((B/'yakuman-review/report.json').read_text('utf8'));cases=report['cases'];sheet=Image.new('RGB',(1800,80+((len(cases)+2)//3)*310),'#172130');draw=ImageDraw.Draw(sheet);draw.text((20,15),'役满演出 · APK 原始场景／实时结算手牌 · Unity 实际截图',font=font,fill='white');links=[]
for i,case in enumerate(cases):
    p=B/'yakuman-review'/(case['screenshots'][-1]+'.png');shutil.copyfile(p,O/p.name);im=Image.open(p);im.thumbnail((586,264));x=i%3*600+7;y=80+i//3*310;sheet.paste(im,(x,y));draw.text((x,y+267),case['key']+' / '+str(case['tiles'])+' 张',font=small,fill='white');links.append('<a href="'+p.name+'"><img loading="lazy" src="'+p.name+'"><span>'+html.escape(case['key'])+' · '+str(case['tiles'])+' 张</span></a>')
sheet.save(O/'yakuman-overview.jpg',quality=94)
shutil.copyfile(B/'yakuman-review/alternate-actual-hand.png',O/'alternate-actual-hand.png')
(O/'index.html').write_text('<!doctype html><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>1114 修复预览</title><style>body{background:#172130;color:white;font:16px system-ui;margin:24px}img{max-width:100%}.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(420px,1fr));gap:12px}a{color:#adceff;text-decoration:none}span{display:block;padding:10px}</style><h1>1114 修复预览</h1><p>全部是 Unity 实际渲染。Android 真机尚未验证。</p><h2>虚影祝祷</h2><img src="fate-before-after.jpg"><h2>开局立绘</h2><img src="portraits-before-after.jpg"><p>同一原图，修正合成降清晰度；未用图片插值冒充高清素材。</p><h2>役满场景与手牌</h2><p>15 种役满场景、含杠后的 18 张版本，共 23 组。以下为结算数据驱动的开发样例。</p><div class="grid">'+''.join(links)+'</div><h2>另一副实际手牌（含赤五）</h2><img src="alternate-actual-hand.png">','utf8')
print(O,len(cases))
