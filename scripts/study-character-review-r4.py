from pathlib import Path
import json,sys,ast,runpy
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parent.parent;O=R/'build/character-review-r4';O.mkdir(exist_ok=True,parents=True)
sys.path.insert(0,str(R/'tools/video-inspector'));import av
videos=Path('D:/MyWorkSpace/video/1018/角色');font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',22)
for name in ['大厅切换出战角色.mp4','大厅切换出战角色及皮肤.mp4','大厅切换出战角色星标过滤.mp4','大厅随机出战角色功能.mp4','切换角色界面出现和消失动画.mp4']:
 p=videos/name
 if not p.exists():print('Missing',name);continue
 with av.open(str(p))as c:
  stream=c.streams.video[0];duration=float(stream.duration*stream.time_base);times=[duration*f for f in [.12,.3,.5,.7,.88,.97]];images=[]
  for sec in times:
   c.seek(int(sec/stream.time_base),stream=stream);frame=next(c.decode(video=0));im=frame.to_image().convert('RGB');im=im.transpose(Image.Transpose.ROTATE_90)if im.height>im.width else im;im.thumbnail((640,300));images.append(im)
  if name=='大厅随机出战角色功能.mp4':
   for sec in [5.8,9.7,13.5]:
    c.seek(int(sec/stream.time_base),stream=stream);im=next(c.decode(video=0)).to_image();im=im.transpose(Image.Transpose.ROTATE_90)if im.height>im.width else im;im.save(O/('reference-random-'+str(sec)+'.png'))
 canvas=Image.new('RGB',(1280,3*335),(13,19,31));d=ImageDraw.Draw(canvas)
 for i,im in enumerate(images):x=i%2*640;y=i//2*335;canvas.paste(im,(x,y+30));d.text((x+10,y+4),f'{name} · {times[i]:.1f}s',font=font,fill=(235,207,153))
 canvas.save(O/(p.stem+'.jpg'),quality=94);print(name,'duration',round(duration,2))
# Reuse only the layout calculator; avoid rerunning the earlier import script.
module=ast.parse((R/'scripts/study-character-review-1018r1.py').read_text('utf8'));node=next(n for n in module.body if isinstance(n,ast.FunctionDef)and n.name=='rects');ns={};exec(compile(ast.Module(body=[node],type_ignores=[]),'native-rects','exec'),ns)
nodes=json.loads((R/'build/characters-1018/ui_roleset.json').read_text('utf8'));rects=ns['rects'](nodes,1920);rows=[]
for n in nodes:
 cs=[]
 for c in n['components']:
  d=c['data'];entry={k:d[k]for k in ['asset_path','m_Text','FixMode','m_Type','m_PreserveAspect','m_Interactable']if k in d};entry['kind']=c['kind']
  if d.get('m_FontData'):entry['fontSize']=d['m_FontData']['m_FontSize']
  if len(entry)>1:cs.append(entry)
 rows.append(dict(path=n['path'],active=n['active'],rect=rects[n['path']],scale=n['transform']['m_LocalScale'],components=cs))
(O/'roleset-layout.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf8')
text='\n'.join(r['path']+' '+str(r['rect'])+' '+str(r['active'])+' '+str(r['components'])for r in rows if r['components']or r['path'].count('/')<3)
(O/'roleset-layout.txt').write_text(text,encoding='utf8');print('Native nodes',len(rows))
