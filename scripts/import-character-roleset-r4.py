from pathlib import Path
import runpy,json,io,hashlib
from PIL import Image
R=Path(__file__).resolve().parent.parent;S=runpy.run_path(str(R/'scripts/study-characters-1018.py'));D=R/'unity/Assets/Resources/Majsoul/Characters';O=R/'build/character-review-r4'
rows=json.loads((O/'roleset-layout.json').read_text('utf8'));needed={c['asset_path']for r in rows for c in r['components']if c.get('asset_path','').startswith('ui/')};catalog=json.loads((D/'catalog.json').read_text('utf8'));records=[]
for folder in sorted({p.rsplit('/',1)[0]for p in needed}):
 keys={Path(p).stem for p in needed if p.rsplit('/',1)[0]==folder};seen=set()
 for path,bundle in S['paths'].items():
  if not path.startswith('myassets/'+folder+'/')or not any(t in path for t in ['/common/','/chs/'])or not path.endswith('.png')or bundle in seen:continue
  seen.add(bundle)
  for obj in S['af'](bundle).objects.values():
   if obj.type.name!='Sprite'or obj.peek_name()not in keys:continue
   name=obj.peek_name();data=obj.read_typetree();im=obj.read().image;r=data['m_Rect'];offset=data['m_RD']['textureRectOffset'];canvas=Image.new('RGBA',(round(r['width']),round(r['height'])));canvas.paste(im,(round(offset['x']),round(r['height']-offset['y']-im.height)))
   key='Role/'+name;p=D/'UI'/(key+'.png');p.parent.mkdir(parents=True,exist_ok=True);buf=io.BytesIO();canvas.save(buf,format='PNG');p.write_bytes(buf.getvalue());border=[data['m_Border'][k]for k in 'xyzw'];catalog['ui']=[x for x in catalog['ui']if x['key']!=key]+[dict(key=key,width=canvas.width,height=canvas.height,border=border)]
   records.append(dict(key=key,source=folder+'/'+name,bundle=bundle,pathID=obj.path_id,border=border,sha256=hashlib.sha256(buf.getvalue()).hexdigest()));keys.remove(name)
 if keys:print('Missing sprites',folder,sorted(keys))
(D/'catalog.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n',encoding='utf8');(D/'roleset-source.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf8');print('Imported',len(records),'native roleset resources')
for r in records:
 if r['key'].endswith(('bg','frame','tabs_selected','tabs_unselected','block_base','btn_rand_bg','unfold_base','name_base')):print(r['key'],r['border'])
