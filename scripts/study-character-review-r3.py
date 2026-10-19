from pathlib import Path
import runpy,json,io,hashlib
R=Path(__file__).resolve().parent.parent;S=runpy.run_path(str(R/'scripts/study-characters-1018.py'));O=R/'build/character-review-r3';O.mkdir(parents=True,exist_ok=True)
p='myassets/ui/lobby/extend/pri_screen/prefab_output/ui_priscreen.prefab';nodes=list(S['tree'](S['obj'](p,'GameObject')));(O/'ui_priscreen.json').write_text(json.dumps(nodes,ensure_ascii=False,indent=2),encoding='utf8')
for n in nodes:
 for c in n['components']:
  d=c['data']
  if d.get('asset_path')or d.get('m_Text'):print(n['path'],d.get('asset_path'),repr(d.get('m_Text')),n['transform'].get('m_SizeDelta'),n['transform'].get('m_AnchoredPosition'))
for p,b in S['paths'].items():
 if '/pri_screen/pic/common/atlas'in p and p.endswith('.png'):
  print('SPRITES',[o.peek_name()for o in S['af'](b).objects.values()if o.type.name=='Sprite']);break
target=R/'unity/Assets/Resources/Majsoul/Characters';records=[];seen=set();catalog=json.loads((target/'catalog.json').read_text('utf8'))
for p,b in S['paths'].items():
 if '/pri_screen/pic/'not in p or not p.endswith('.png')or b in seen or '/chs_t/'in p or'/jp/'in p or'/en/'in p or'/kr/'in p:continue
 seen.add(b)
 for o in S['af'](b).objects.values():
  if o.type.name!='Sprite':continue
  name=o.peek_name()
  if name not in ['btn_hide','btn_reset','btn_save','btn_save_gray','btn_close','MAX','MIN','slide_scale_bottom','slide_scale_handle']:continue
  im=o.read().image;d=o.read_typetree();rect=d['m_Rect'];off=d['m_RD']['textureRectOffset'];canvas=__import__('PIL.Image',fromlist=['Image']).new('RGBA',(round(rect['width']),round(rect['height'])));canvas.paste(im,(round(off['x']),round(rect['height']-off['y']-im.height)))
  buffer=io.BytesIO();canvas.save(buffer,format='PNG');destination=target/'UI/Photo'/(name+'.png');destination.parent.mkdir(parents=True,exist_ok=True);destination.write_bytes(buffer.getvalue());records.append(dict(output='UI/Photo/'+name+'.png',bundle=b,pathID=o.path_id,sha256=hashlib.sha256(buffer.getvalue()).hexdigest()))
  key='Photo/'+name;catalog['ui']=[r for r in catalog['ui']if r['key']!=key]+[dict(key=key,width=canvas.width,height=canvas.height,border=[d['m_Border'][k]for k in 'xyzw'])]
(target/'catalog.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
(target/'photo-source.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf8');print('Imported original photo controls',len(records))
