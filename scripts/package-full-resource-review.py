"""Package actual full-roster Unity captures and per-outfit render evidence."""
from pathlib import Path
import html,json,re,shutil
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parent.parent
version=re.search(r'ReleaseVersion\s*=\s*"([0-9.]+)"',(R/'unity/Assets/Editor/ProjectSetup.cs').read_text('utf8'))[1]
D=R/'dist/previews'/('unity-'+version+'-resources');D.mkdir(parents=True,exist_ok=True)
V=R/'build/character-resources-all/validation';F=R/'build/character-pack-experiment/render';U=R/'build/resource-ui-r3/render'
report=json.loads((V/'checks.json').read_text('utf8'));flow=json.loads((F/'checks.json').read_text('utf8'))
assert len(flow['checks'])>=38 and len(json.loads((U/'checks.json').read_text('utf8'))['checks'])==15
roles=json.loads((R/'unity/Assets/Resources/Majsoul/CharacterDirectory/catalog.json').read_text('utf8'))['roles']
dynamic=[(r,s)for r in roles for s in r['skins']if s['dynamic']]
assert report['roles']==len(roles) and report['skins']==sum(len(r['skins'])for r in roles) and report['dynamics']==len(dynamic)
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',17)
images=[]
for start in range(0,len(dynamic),24):
 rows=dynamic[start:start+24];sheet=Image.new('RGB',(1200,((len(rows)+5)//6)*264),'#172038');draw=ImageDraw.Draw(sheet)
 for index,(role,skin)in enumerate(rows):
  src=V/(str(skin['id'])+'-dynamic.png');im=Image.open(src).convert('RGBA');im.thumbnail((190,222))
  x=index%6*200+(200-im.width)//2;y=index//6*264+4;sheet.paste(im,(x,y),im)
  draw.text((index%6*200+7,index//6*264+230),role['name']+' '+str(skin['id']),font=font,fill='#edd29b')
 name='dynamic-sheet-'+str(start//24+1)+'.jpg';sheet.save(D/name,quality=90);images.append((name,'动态服饰运行时渲染 '+str(start+1)+'–'+str(start+len(rows))))
shots=[(U,'settings-highlight','设置资源页：完整四行与定位描边'),(U,'standalone-highlight','独立资源窗口：滚动条与描边间距'),(F,'all-installed','全部角色安装完成'),(F,'sort-all-start','全部角色排序：顶部'),(F,'sort-all-end','全部角色排序：末页')]
for folder,name,label in shots:
 src=folder/(name+'.png');assert src.is_file(),src;shutil.copyfile(src,D/src.name);images.insert(0,(src.name,label))
body=''.join('<figure><a href="'+name+'"><img loading="lazy" src="'+name+'"></a><figcaption>'+html.escape(label)+'</figcaption></figure>'for name,label in images)
page='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>云间日麻 · 全角色资源验证</title><style>body{margin:0;background:#101726;color:#eee;font:16px system-ui}main{max-width:1280px;margin:auto;padding:24px}p{line-height:1.8;color:#b5c6df}figure{margin:28px 0;background:#19253b;border:1px solid #3b4961}img{display:block;width:100%}figcaption{padding:14px;color:#edcd94}a{color:#edcd94}</style><main><h1>云间日麻 Unity __VERSION__ · 全角色资源</h1><p>当前目录__ROLES__名角色、__SKINS__套服饰、__DYNAMICS__套动态；以下为实际Unity运行截图和逐动态服饰的渲染联系表。两名基础角色始终内置，其他角色按需下载。测试使用隔离存储，不修改玩家个人安装状态。Android真机触摸、声音和多手机热点仍待实测。</p>__BODY__</main></html>'''.replace('__VERSION__',version).replace('__ROLES__',str(report['roles'])).replace('__SKINS__',str(report['skins'])).replace('__DYNAMICS__',str(report['dynamics'])).replace('__BODY__',body)
(D/'index.html').write_text(page,'utf8');shutil.copyfile(V/'checks.json',D/'render-checks.json');shutil.copyfile(F/'checks.json',D/'flow-checks.json')
assert all((D/link).is_file()for link in re.findall(r'(?:href|src)="([^"#]+)"',page))
print('FULL RESOURCE PREVIEW PASS',len(images),'images',D)
