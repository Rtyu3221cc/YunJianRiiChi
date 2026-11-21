"""Build a browsable gallery from real Unity renders (no generated artwork)."""
from pathlib import Path
import html,json,shutil,zipfile
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parent.parent
S=R/'build/feedback-1114-r39/hand-gallery';O=R/'dist/previews/2026-11-14-all-hands';O.mkdir(parents=True,exist_ok=True)
data=json.loads((S/'gallery.json').read_text('utf8'));groups={}
actions={'Dapai':'打牌','LiQiPai':'横打立直牌','LiZhi':'放立直棒','Fulu':'鸣牌','Zimo':'自摸拍牌'}
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',22);small=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',16)
for e in data['entries']:
    shutil.copyfile(S/e['image'],O/e['image']);groups.setdefault(e['key'],[]).append(e)
cols=4;cw,ch=400,330;sheet=Image.new('RGB',(cols*cw,90+((len(groups)+cols-1)//cols)*ch),'#111c2b');draw=ImageDraw.Draw(sheet);draw.text((20,16),'全部手型 · Unity 实际渲染 · 2026-11-14',font=font,fill='white');draw.text((20,50),'21 套专属手型 × 所有方向和五动作，另含普通手／汪爪／橘猫爪及旧猫爪资源',font=small,fill='#b8cadc')
sections=[]
for index,(key,entries)in enumerate(groups.items()):
    title=entries[0]['characters']or('赤麟（模型预留，未加入角色目录）'if key=='hand_chilin'else key)
    if key=='hand-cat':title+='（旧资源，未开放装备）'
    first=next((e for e in entries if e['action']=='LiZhi'),entries[0]);im=Image.open(S/first['image']).convert('RGBA');bg=Image.new('RGBA',im.size,'#172130');bg.alpha_composite(im);im=bg.convert('RGB');im.thumbnail((386,266));x=index%cols*cw+7;y=90+index//cols*ch;sheet.paste(im,(x+(386-im.width)//2,y));draw.text((x,y+267),title[:20],font=small,fill='white');draw.text((x,y+291),key,font=small,fill='#97aec5')
    links=''.join(f'<a href="{html.escape(e["image"])}" target="_blank"><img loading="lazy" src="{html.escape(e["image"])}"><span>方向 {e["physical"]+1} · {actions[e["action"]]}</span></a>'for e in entries)
    sections.append(f'<section id="{key}"><h2>{html.escape(title)}</h2><p>{key}</p><div class="frames">{links}</div></section>')
sheet.save(O/'all-hands-overview.jpg',quality=94)
nav=''.join(f'<a href="#{key}">{html.escape(es[0]["characters"] or key)}</a>'for key,es in groups.items())
(O/'index.html').write_text('<!doctype html><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>全部手型实际预览</title><style>body{background:#111c2b;color:#e4edf7;font:16px system-ui;margin:24px}a{color:#a8d4ff}nav{display:flex;flex-wrap:wrap;gap:12px}h2{margin-top:40px}.frames{display:grid;grid-template-columns:repeat(auto-fill,minmax(260px,1fr));gap:12px}.frames a{background:#172130;padding:8px;text-decoration:none}.frames img{width:100%;display:block}.frames span{display:block;padding:8px}p{color:#a2b5ca}</style><h1>全部手型实际预览</h1><p>共 26 套资源外观、260 张截图。21 套专属手型共 51 个方向模型、255 个动作画面；5 套普通／旧手型展示装扮预览姿态。点击图片查看原图。其余角色材质保持当前游戏效果；亚丝娜使用已修正的源明暗／法线材质。</p><p>方向编号是 APK 模型编号；单模型手型在牌桌中按座位旋转。赤麟尚未加入角色目录，旧猫爪资源未开放装备。截图是编辑器实际渲染，尚未替代 Android 真机验证。</p><nav>'+nav+'</nav>'+''.join(sections),'utf8')
shutil.copyfile(S/'gallery.json',O/'gallery.json')
with zipfile.ZipFile(O.parent/'2026-11-14-all-hands.zip','w',zipfile.ZIP_DEFLATED)as z:
    for p in O.iterdir():z.write(p,'all-hands/'+p.name)
print(O,len(groups),len(data['entries']))
