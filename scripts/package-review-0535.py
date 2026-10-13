"""Bundle real render screenshots, AI timing evidence and the analysis-only item."""
from pathlib import Path
import json,shutil,sys,zipfile,html
R=Path(__file__).resolve().parent.parent;D=R/'dist';P=D/'previews';P.mkdir(exist_ok=True)
sys.path.insert(0,str(R/'tools/asset-inspector'));from PIL import Image,ImageDraw,ImageFont
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',20);files=[]
for source in sorted((R/'build/unity-smoke').glob('review-0535-*.png')):
 target=P/('unity-0.5.35-'+source.name);shutil.copyfile(source,target);files.append(target.name)
assert len(files)==25
timing=json.loads((R/'build/ai-timing-0535.json').read_text(encoding='utf8'));im=Image.new('RGB',(1020,640),(18,25,35));draw=ImageDraw.Draw(im);draw.text((20,14),'第4项 · AI随机等待实测（测试记录，非游戏内提示）',font=font,fill='#f1d298')
for i,row in enumerate(timing['rows']):
 y=70+i*60;draw.text((20,y),row['action'],font=font,fill='white');draw.rectangle((230,y+4,230+row['plannedMs']*.7,y+27),fill='#5eadbb');draw.text((800,y),f"{row['plannedMs']} / {row['elapsedMs']} ms",font=font,fill='white')
draw.text((20,584),'前值为随机等待，后值包含调度和计算；20ms轮询的合成对局。',font=font,fill='#bac9dc');im.save(P/'unity-0.5.35-ai-timing.png');files.append('unity-0.5.35-ai-timing.png')
shutil.copyfile(R/'build/ai-timing-0535.json',P/'unity-0.5.35-ai-timing.json')
labels={'01':'1 · 放大指示器和长按候选听牌','02':'2 · 自摸站立牌／推倒连续显示','03':'3 · 胜负文字由大缩小','04':'4 · AI随机等待实测','05':'5 · APK荣和／自摸提示','06':'6 · 北牌紧邻摆放','07':'7 · 无等待响应文字','08':'8 · 流局摊开与扣牌的位置','09':'9 · All Last','11':'11 · 第一、第二张北牌按动画落地'}
figures=''
for n in files:
 key=n.split('review-0535-')[-1][:2] if 'review-' in n else '04';figures+=f'<figure><img src="previews/{n}" loading="lazy"><figcaption>{html.escape(labels[key])} · {html.escape(n)}</figcaption></figure>'
analysis=(R/'docs/牌桌尺寸计算分析-0.5.35.md').read_text(encoding='utf8')
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>0.5.35逐项确认</title><style>body{max-width:1250px;margin:24px auto;padding:0 18px;background:#121923;color:#e8edf5;font:17px/1.7 "Microsoft YaHei",sans-serif}h1,h2{color:#f1d298}figure{padding:14px;margin:24px 0;background:#1d293a;border-radius:8px}img{max-width:100%;display:block;margin:auto}figcaption{padding:10px;color:#b9c8dd}pre{white-space:pre-wrap;overflow-wrap:anywhere;font:16px/1.8 "Microsoft YaHei",sans-serif}</style><h1>0.5.35 · 逐项截图确认</h1><p>25张实际Unity截图与1张AI时间记录图，合成测试局面，非安卓真机截图。第10项只分析，没有调整全局牌尺寸。</p>'''+figures+'<h2>10 · 尺寸计算分析</h2><pre>'+html.escape(analysis)+'</pre></html>'
gallery=D/'unity-0.5.35-screenshots.html';gallery.write_text(page,encoding='utf8')
with zipfile.ZipFile(D/'yunjian-riichi-unity-0.5.35-screenshots.zip','w',zipfile.ZIP_DEFLATED) as z:
 z.write(gallery,gallery.name)
 for n in [*files,'unity-0.5.35-ai-timing.json']:assert(P/n).is_file();z.write(P/n,'previews/'+n)
(P/'unity-0.5.35-screenshot-manifest.json').write_text(json.dumps({'actualUnityScreenshots':25,'timingFigures':1,'files':files},indent=2)+'\n',encoding='utf8')
print('PASS: 25 real screenshots, AI timing figure/data, embedded item-10 analysis and offline gallery links.')
