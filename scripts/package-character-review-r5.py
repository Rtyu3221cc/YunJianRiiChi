"""Screenshot-only R5 review, using actual Unity captures and labelled detail crops."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,shutil,re
R=Path(__file__).resolve().parent.parent;S=R/'build/character-review-r5/render-quick';O=R/'dist/previews/2026-10-19-character-system-r5';O.mkdir(parents=True,exist_ok=True)
groups=[('切换雀士',[('switch-main','页签相接；使用中文字右移 2px；左侧、底部拼接缝修正'),('switch-skins','展开框三角与边框连接；收紧头像下方空白')]),('随机雀士 · 星标过滤核对',[('random-all','全部：四名雀士；三名雀士共四套服饰已选'),('random-favorites','仅星标：显示两名星标；左侧候选保持不变'),('random-favorites-skins','过滤后仍可展开和配置星标雀士的服饰'),('random-no-favorites','没有星标：显示空提示；左侧候选继续保留')]),('16:9 布局',[('switch-16x9','切换雀士 · 16:9'),('random-16x9','随机配置 · 16:9')])]
for _,shots in groups:
 for key,_ in shots:
  im=Image.open(S/(key+'.png'));im.verify();shutil.copyfile(S/(key+'.png'),O/(key+'.png'))
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',24)
before=Image.open(R/'dist/previews/2026-10-19-character-system-r4/switch-skins.png').convert('RGB');after=Image.open(S/'switch-skins.png').convert('RGB')
# Identical unmodified screen coordinates, magnified equally for comparison.
region=(700,322,1070,475);detail=Image.new('RGB',(1480,366),(15,22,36));draw=ImageDraw.Draw(detail)
for i,(im,label) in enumerate([(before,'修改前'),(after,'修改后')]):
 detail.paste(im.crop(region).resize((740,306)),(i*740,60));draw.text((i*740+20,16),label,font=font,fill=(237,208,158))
detail.save(O/'popover-comparison.png')
groups.insert(1,('展开框局部对比',[('popover-comparison','同一区域等比例放大：三角连接与底部间距')]))
def seam_score(im):
 differences=[]
 for y in list(range(150,285))+list(range(600,670)):
  middle=im.getpixel((379,y));a=im.getpixel((378,y));b=im.getpixel((380,y));differences.append(max(abs(middle[c]-(a[c]+b[c])/2) for c in range(3)))
 return dict(samples=len(differences),exposedPixels=sum(d>20 for d in differences),maxNeighborDifference=max(differences))
movie=json.loads((S/'movie.json').read_text('utf8'));assert 'CHARACTER REVIEW R5 COMPLETE errors=0' in (R/'build/character-review-r5/quick.log').read_text('utf8',errors='replace')
parts=[]
for title,shots in groups:
 parts.append('<h2>'+title+'</h2><div class="grid">')
 for key,caption in shots:parts.append(f'<figure><a href="{key}.png" target="_blank"><img src="{key}.png" loading="lazy"></a><figcaption>{caption}</figcaption></figure>')
 parts.append('</div>')
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 · 雀士弹窗修正 R5</title><style>*{box-sizing:border-box}body{margin:0;background:#0c1320;color:#e6ebf3;font-family:system-ui,"Microsoft YaHei",sans-serif}main{max-width:1550px;padding:30px 24px 60px;margin:auto}h1{font-size:29px}h2{font-size:22px;margin-top:30px}p,footer{line-height:1.8;color:#b5c2d5}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:18px}figure{margin:0;border:1px solid #35465f;border-radius:8px;overflow:hidden;background:#152136}img{display:block;width:100%}figcaption{padding:12px 16px;line-height:1.6;color:#e7c88f}footer{margin-top:32px;border-top:1px solid #35465f;padding-top:18px;font-size:13px}@media(max-width:850px){main{padding:18px 12px}.grid{grid-template-columns:1fr}}</style><main><h1>云间日麻 · 雀士弹窗修正 R5</h1><p>页签相接、展开框三角连接、收紧底部空白、修正拼接缝；使用中文字和数量数字各右移 2px。点击图片可查看原尺寸。</p>__BODY__<footer>全部效果图来自当前 Unity 工程实际渲染。随机页星标过滤已核对：全部／仅星标／无星标、服饰展开、候选保留和弹层关闭。核对通过编辑器验证入口驱动界面选择，滚动沿用共用运行时手势路由；没有进行手机真机触摸验证。<br>本轮只交付效果图，未编译 APK、未上传 Git。</footer></main></html>'''
(O/'index.html').write_text(page.replace('__BODY__',''.join(parts)),encoding='utf8')
links=re.findall(r'(?:src|href)="([^"#]+)"',page.replace('__BODY__',''.join(parts)));assert all((O/link).is_file()for link in links)
report=dict(images=sum(len(shots)for _,shots in groups),links=len(links),checks=movie['checks'],unityErrors=0,seamBefore=seam_score(before),seamAfter=seam_score(after),apkBuilt=False,gitUploaded=False)
assert report['seamAfter']['exposedPixels']==0
(R/'build/character-review-r5/artifact-checks.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps({k:v for k,v in report.items()if k!='checks'},ensure_ascii=False));print('Filter/capture checks:',len(movie['checks']))
