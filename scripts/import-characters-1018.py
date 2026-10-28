"""Import the four reference characters, using only media in the supplied APK."""
import runpy,sys,json,io,hashlib,re
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
S=runpy.run_path(str(Path(__file__).with_name('study-characters-1018.py')))
R=S['R'];paths=S['paths'];af=S['af'];obj=S['obj'];resolve=S['resolve'];raw=S['raw'];z=S['z']
O=R/'unity/Assets/Resources/Majsoul/Characters';O.mkdir(exist_ok=True,parents=True)
study=R/'build/characters-1018';config=json.loads((study/'config.json').read_text('utf8'))
records=[];ui=[]
def write(n,b,o=None):
 p=O/n;p.parent.mkdir(parents=True,exist_ok=True)
 if not p.exists()or p.read_bytes()!=b:p.write_bytes(b)
 r=dict(output=n,bytes=len(b),sha256=hashlib.sha256(b).hexdigest())
 if o:r.update(bundle=S['cab'][o.assets_file.name],pathID=o.path_id,type=o.type.name)
 records.append(r)
def png(n,o,logical=False):
 im=o.read().image
 if logical and o.type.name=='Sprite':
  d=o.read_typetree();rect=d['m_Rect'];rd=d['m_RD'];off=rd['textureRectOffset'];canvas=Image.new('RGBA',(round(rect['width']),round(rect['height'])));canvas.paste(im,(round(off['x']),round(rect['height']-off['y']-im.height)));im=canvas
 b=io.BytesIO();im.save(b,format='PNG');write(n+'.png',b.getvalue(),o);return im.size
needed=set()
for f in study.glob('ui_*.json'):
 for n in json.loads(f.read_text('utf8')):
  for c in n['components']:
   p=c['data'].get('asset_path')
   if p and p.startswith('ui/'):needed.add(Path(p).stem)
# Contract-only backgrounds and name plates belong to the dorm, not table avatars.
needed.update(['sushe_card_married_star_dark','sushe_card_married_outline','sushe_card_married','sushe_card_married_name','sushe_name_married','sushe_card_normal','sushe_card_married_bottom'])
done=set()
for p,bundle in paths.items():
 if not any(k in p for k in ['/liaoshe/','/liaoshe_extra/','/roleset/','/ui/common/main/pic/','/ui/lobby/main/pic/','/ui/necessary/pic/']):continue
 if '/pic/'not in p or any('/'+k+'/'in p for k in ['chs_t','en','jp','kr']):continue
 if bundle in done:continue
 done.add(bundle)
 for o in af(bundle).objects.values():
  name=o.peek_name()
  if o.type.name!='Sprite'or(name not in needed and not any(k in name.lower()for k in ['married','sushe_card','favor','name_'])):continue
  if any(x['key']==name for x in ui):continue
  size=png('UI/'+name,o,True);d=o.read_typetree();border=[d['m_Border'][k]for k in 'xyzw'];ui.append(dict(key=name,width=size[0],height=size[1],border=border))
png('indoor',obj('myassets/scenes/lobby/main/texture/indoor_1.jpg','Texture2D'))
roles=[];ids=[200069,200001,200002,200033]
for id in ids:
 c=next(c for c in config['item_definition/character']if c['id']==id);key=c['sound_folder']
 role=dict(id=id,key=key,name=c['name_chs'],sex=c['sex'],cv=c['desc_cv_chs'],height=c['desc_stature_chs'],birth=c['desc_birth_chs'],age=c['desc_age_chs'],blood=c['desc_bloodtype_chs'],hobby=c['desc_hobby_chs'],description=c['desc_chs'],defaultSkin=c['init_skin'],bondSkin=c['full_fetter_skin'],skins=[],emojis=[])
 for sk in config['item_definition/skin']:
  if sk['character_id']!=id:continue
  skinKey=sk['path'].rsplit('/',1)[-1].lower();base='myassets/deco/character/'+skinKey+'/'
  if base+'full/full.png'not in paths:print('Absent from APK:',sk['id'],skinKey,flush=True);continue
  s=dict(id=sk['id'],key=skinKey,name=sk['name_chs'],description=sk['desc_chs'].replace('\\n','\n'),dynamic=bool(sk['spine_type']),spineWidth=sk['spine_width'],spineHeight=sk['spine_height'],pivotX=sk['pivot_x'],pivotY=sk['pivot_y'],parts=[],lobbyOffset=sk['lobby_offset'],dormOffset=sk['liaoshe_offset'])
  for kind in ['full','smallhead','bighead','half','waitingroom']:
   path=base+kind+'/'+kind+'.png'
   if path in paths:png('Skins/'+str(s['id'])+'/'+kind,obj(path,'Sprite'),True)
  if s['dynamic']:
   sp=[p for p in paths if p.startswith('myassets/spine/'+str(s['id'])+'/')and p.endswith('/character.prefab')]
   for path in sorted(sp):
    part='main'if path.count('/')==3 else path.split('/')[-2];folder='Spine/'+str(s['id'])+'/'+part;f=af(paths[path]);atlasObjs=[]
    for o in f.objects.values():
     if o.type.name=='TextAsset':
      name=o.peek_name();b=raw(o)
      if name.endswith('.skel'):write(folder+'/skeleton.bytes',b,o)
      elif name.endswith('.atlas'):write(folder+'/atlas.txt',b,o);atlasObjs.append(b.decode('utf8'))
      elif name=='_info_output':write(folder+'/info.txt',b,o)
    # Resolve atlas page textures through the original materials.
    pages=set(line.strip()for txt in atlasObjs for line in txt.splitlines()if line.strip().endswith('.png'))
    exported=set()
    for o in f.objects.values():
     if o.type.name!='Material':continue
     for entry in o.read_typetree()['m_SavedProperties']['m_TexEnvs']:
      tex=resolve(o.assets_file,entry[1]['m_Texture'])
      if tex and tex.type.name=='Texture2D'and tex.peek_name()+'.png'in pages and tex.peek_name()not in exported:
       png(folder+'/'+tex.peek_name(),tex);exported.add(tex.peek_name())
    assert pages=={n+'.png'for n in exported},(s['id'],pages,exported)
    s['parts'].append(folder)
  role['skins'].append(s)
  print('Imported',key,s['id'],s['name'],'dynamic='+str(s['dynamic']),flush=True)
 for p in sorted(paths):
  if p.startswith('myassets/deco/character/'+key+'/face/')and p.endswith('.png'):
   name=Path(p).stem;png('Emoji/'+key+'/'+name,obj(p,'Texture2D'));role['emojis'].append('Emoji/'+key+'/'+name)
 roles.append(role)
write('catalog.json',(json.dumps(dict(roles=roles,ui=ui),ensure_ascii=False,indent=2)+'\n').encode())
write('provenance.json',(json.dumps(dict(apk='Majsoul_4.0.16_MC.apk',metadata='Official v0.11.252.w lqc table, intersected with APK assets',records=records),ensure_ascii=False,indent=2)+'\n').encode())
print('Completed',len(ui),'UI sprites,',sum(len(r['skins'])for r in roles),'skins.',flush=True)
