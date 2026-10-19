"""Prepare source assets, not an APK. Optional roles live outside Resources."""
from pathlib import Path
import json,runpy,io,re,hashlib,shutil,concurrent.futures,urllib.request,time
from PIL import Image
R=Path(__file__).resolve().parent.parent;B=R/'build/character-pack-experiment';B.mkdir(parents=True,exist_ok=True)
S=runpy.run_path(str(R/'scripts/study-characters-1018.py'));C=R/'unity/Assets/Resources/Majsoul/Characters';D=R/'unity/Assets/Resources/Majsoul/CharacterDirectory';P=R/'unity/Assets/CharacterPacks/Source';D.mkdir(parents=True,exist_ok=True)
cfg=json.loads((R/'build/characters-1018/config.json').read_text('utf8'));V=json.loads((R/'build/reference/public/resversion0.11.252.w.json').read_text('utf8'))['res'];old=json.loads((C/'catalog.json').read_text('utf8'));byid={x['id']:x for x in cfg['item_definition/skin']};records=[]
recommended={200001,200010,200038,200007,200029,200005,200067,200024,200019,200002,200012}
def js(p,value):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
def png(path,obj,logical=False):
 im=obj.read().image
 if logical:
  d=obj.read_typetree();rect=d['m_Rect'];off=d['m_RD']['textureRectOffset'];canvas=Image.new('RGBA',(round(rect['width']),round(rect['height'])));canvas.paste(im,(round(off['x']),round(rect['height']-off['y']-im.height)));im=canvas
 path.parent.mkdir(parents=True,exist_ok=True);im.save(path)
def download(key,dest):
 url='https://game.maj-soul.com/1/'+V[key]['prefix']+'/'+key;dest.parent.mkdir(parents=True,exist_ok=True)
 if dest.exists():data=dest.read_bytes()
 else:
  for attempt in range(4):
   try:data=urllib.request.urlopen(url,timeout=40).read();break
   except Exception:
    if attempt==3:raise
    time.sleep(.5*(attempt+1))
 assert len(data)>100 and not data.startswith(b'<'),key
 if dest.suffix=='.png'and not data.startswith(b'\x89PNG'):
  xor=data[0]^0x89;data=bytes(b^xor for b in data)
 dest.write_bytes(data);return dict(path=str(dest.relative_to(R)),url=url,sha256=hashlib.sha256(data).hexdigest())
roles=[]
for c in cfg['item_definition/character']:
 key=c['sound_folder'];role=dict(id=c['id'],key=key,name=c['name_chs'],cv=c['desc_cv_chs'],height=c['desc_stature_chs'],birth=c['desc_birth_chs'],age=c['desc_age_chs'],blood=c['desc_bloodtype_chs'],hobby=c['desc_hobby_chs'],description=c['desc_chs'],defaultSkin=c['init_skin'],bondSkin=c['full_fetter_skin'],skins=[],emojis=[])
 for sk in cfg['item_definition/skin']:
  if sk['character_id']!=c['id']:continue
  sid=sk['id'];prefix='myassets/spine/'+str(sid)+'/';parts=[('Spine/'+str(sid)+'/'+('main'if p.count('/')==3 else p.split('/')[-2]))for p in sorted(S['paths'])if p.startswith(prefix)and p.endswith('/character.prefab')]
  role['skins'].append(dict(id=sid,key=sk['path'].rsplit('/',1)[-1].lower(),name=sk['name_chs'],description=sk['desc_chs'].replace('\\n','\n'),dynamic=bool(sk['spine_type']),spineWidth=sk['spine_width'],spineHeight=sk['spine_height'],pivotX=sk['pivot_x'],pivotY=sk['pivot_y'],parts=parts,lobbyOffset=sk['lobby_offset'],dormOffset=sk['liaoshe_offset']))
 legacy=next((r for r in old['roles']if r['key']==key),None)
 if legacy:role['skins']=[s for s in role['skins']if s['id']in {s['id']for s in legacy['skins']}]
 role['emojis']=['Emoji/'+key+'/'+Path(k).stem for k in sorted(V)if k.startswith(c['emo']+'/')and re.fullmatch(r'\d+\.png',k.removeprefix(c['emo']+'/'))]
 roles.append(role)
 avatar=D/'Avatars'/(str(c['id'])+'.png');source='myassets/deco/character/'+byid[c['init_skin']]['path'].rsplit('/',1)[-1].lower()+'/bighead/bighead.png'
 if not avatar.exists()and source in S['paths']:
  im=S['obj'](source,'Sprite').read().image;im.thumbnail((160,160));avatar.parent.mkdir(parents=True,exist_ok=True);im.save(avatar)
 S['cache'].clear()
js(D/'catalog.json',dict(roles=roles,ui=old['ui']))
# Reuse GUIDs when moving existing optional assets. Keep source files recoverable.
for role in old['roles']:
 if role['key']in ['yiji','erjietang']:continue
 root=P/role['key']
 for group in ['Voice','Emoji']:
  source=C/group/role['key'];target=root/group/role['key']
  if source.exists():
   assert source.resolve().is_relative_to(C.resolve())and target.resolve().is_relative_to(P.resolve());target.parent.mkdir(parents=True,exist_ok=True);assert not target.exists();shutil.move(str(source),str(target))
   if Path(str(source)+'.meta').exists():shutil.move(str(source)+'.meta',str(target)+'.meta')
 for skin in role['skins']:
  for group in ['Skins','Spine']:
   source=C/group/str(skin['id']);target=root/group/str(skin['id'])
   if source.exists():
    assert source.resolve().is_relative_to(C.resolve())and target.resolve().is_relative_to(P.resolve());target.parent.mkdir(parents=True,exist_ok=True);assert not target.exists();shutil.move(str(source),str(target))
    if Path(str(source)+'.meta').exists():shutil.move(str(source)+'.meta',str(target)+'.meta')
 print('Optional source retained:',role['key'],flush=True)
role=next(r for r in roles if r['key']=='bamuwei');root=P/'bamuwei'
for skin in role['skins']:
 sid=skin['id'];base='myassets/deco/character/'+skin['key']+'/'
 for kind in ['full','bighead','smallhead','half','waitingroom']:
  source=base+kind+'/'+kind+'.png';dest=root/'Skins'/str(sid)/(kind+'.png')
  if source in S['paths']and not dest.exists():png(dest,S['obj'](source,'Sprite'),True)
 if skin['dynamic']:
  for source in sorted(p for p in S['paths']if p.startswith('myassets/spine/'+str(sid)+'/')and p.endswith('/character.prefab')):
   part='main'if source.count('/')==3 else source.split('/')[-2];folder=root/'Spine'/str(sid)/part;folder.mkdir(parents=True,exist_ok=True);f=S['af'](S['paths'][source]);pages=set();seen=set()
   for obj in f.objects.values():
    if obj.type.name!='TextAsset':continue
    name=obj.peek_name();data=S['raw'](obj)
    if name.endswith('.skel'):(folder/'skeleton.bytes').write_bytes(data)
    elif name.endswith('.atlas'):(folder/'atlas.txt').write_bytes(data);pages.update(line.strip()for line in data.decode().splitlines()if line.strip().endswith('.png'))
    elif name=='_info_output':(folder/'info.txt').write_bytes(data)
   for obj in f.objects.values():
    if obj.type.name!='Material':continue
    for entry in obj.read_typetree()['m_SavedProperties']['m_TexEnvs']:
     tex=S['resolve'](obj.assets_file,entry[1]['m_Texture'])
     if tex and tex.type.name=='Texture2D'and tex.peek_name()+'.png'in pages and tex.peek_name()not in seen:png(folder/(tex.peek_name()+'.png'),tex);seen.add(tex.peek_name())
   assert pages=={x+'.png'for x in seen}
 S['cache'].clear();print('Extracted 八木唯',sid,flush=True)
jobs=[(k,root/'Voice'/k.removeprefix('audio/sound/'))for k in V if k.startswith('audio/sound/bamuwei/')and k.endswith('.mp3')]
jobs +=[(k,root/'Emoji'/'bamuwei'/Path(k).name)for k in V if k.startswith('extendRes/emo/e200007/')and re.fullmatch(r'\d+\.png',Path(k).name)]
with concurrent.futures.ThreadPoolExecutor(max_workers=8)as pool:records=list(pool.map(lambda job:download(*job),jobs))
js(B/'official-sources.json',records)
bank=json.loads((C/'framing.json').read_text('utf8'));default=json.loads((R/'build/character-review-r2/official-default.json').read_text('utf8'))['default'];new=[]
for skin in role['skins']:
 item=byid[skin['id']];key=item['illust_data'];data=None
 if key:
  source='lang/base/extendRes/illust_data/'+key+'.json';dest=B/'illust'/(key+'.json')
  if source in V:records.append(download(source,dest));data=json.loads(dest.read_text('utf8'))
 scene=[]
 for kind in ['F0','I0','J0']:
  for index in range(4 if kind!='F0'else 1):
   if not data:continue
   d=default[kind][index];a=data[kind][index];w=d['scale']*data['w'];h=d['scale']*data['h'];sign=-1 if a['reverse']and not item['lock_reverse']else 1;x=d['x']+a['x']*w*a['scale'];y=d['y']+a['y']*h*a['scale']
   if sign<0:x+=2*data['center']*w
   off=dict(x=110,y=-234,scaleX=1.58,scaleY=1.58)if kind=='F0'else d.get('offset',dict(x=0,y=0,scaleX=1,scaleY=1));sx=off['scaleX'];sy=off['scaleY'];x=off['x']+x/sx;y=off['y']+y/sy;w=w/abs(sx)*a['scale'];h=h/abs(sy)*a['scale'];sign*=1 if sx>0 else -1
   scene.append(dict(scene=kind,index=index,rect=[x,y,w,h],flip=sign<0))
 new.append(dict(id=skin['id'],source=key or'legacy crop table',placements=scene,full=[item['full_'+k]for k in ['x','y','width','height']],head=[item['smallhead_'+k]for k in ['x','y','width']],direction=item['direction'],noReverse=bool(item['no_reverse']),lockReverse=bool(item['lock_reverse'])))
bank['skins']=[x for x in bank['skins']if x['id']not in {s['id']for s in new}]+new;js(C/'framing.json',bank);js(B/'official-sources.json',records)
manifest=[]
for role in roles:manifest.append(dict(key=role['key'],name=role['name'],id=role['id'],skins=[s['id']for s in role['skins']],builtin=role['key']in ['yiji','erjietang'],recommended=role['id']in recommended,available=role['key']in ['yiji','erjietang','bamuwei','weilai','siqiqiansuili'],version=1,bytes=0,sha256='',url='',file=''))
js(B/'directory-template.json',dict(schema=1,protocol=1,packs=manifest));print('Prepared',len(roles),'directory entries;',len(jobs),'official supplements;',len(new),'八木唯 framing records',flush=True)
