"""Import supplied APK settlement sprites and fire textures with original source curves.
The flame spiral and orbiting rubble are adapted in Unity to the supplied video.
"""
import hashlib,json,sys,urllib.request,zipfile,warnings
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent
OUT=ROOT/'unity/Assets/Resources/Majsoul/Results';OUT.mkdir(parents=True,exist_ok=True)
FX=ROOT/'unity/Assets/Resources/Majsoul/Actions';CACHE=ROOT/'build/reference/fire-study';CACHE.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(ROOT/'tools/asset-inspector'))
import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
rows=[json.loads(s) for s in (ROOT/'build/reference/fast-catalog.jsonl').read_text(encoding='utf8').splitlines()];records=[]
def digest(b):return hashlib.sha256(b).hexdigest()
def fetch(path):
 p=CACHE/Path(path).name;url='https://mahjongsoul.game.yo-star.com/v0.10.1.w/scene/'+path
 if not p.exists():p.write_bytes(urllib.request.urlopen(url,timeout=20).read())
 records.append({'source':url,'sha256':digest(p.read_bytes())});return p.read_bytes()
names={'bg_jiesuan','bg_fan','bg_center_fan','bg_yakuman','scorechange_bg','scorechangearrow_0','scorechangearrow_1','bg_point','bg_nickname','bg_rankchange','bg_rankchange_name','fanfu','bg_dora','bg_lidora','bg_type','word_point','word_fan'}
names.update({'word_fu','word_yiman','word_man','word_guan','word_tiao','word_bei','word_yi','word_san','word_si','word_wu','word_liu','word_liang','word_lei','word_ji','ww_minus'})
names.update({prefix+str(i) for prefix in ['h_','ww_'] for i in range(10)})
layers=[];materials={}
with zipfile.ZipFile(ROOT/'dist/Majsoul_4.0.16_MC.apk') as z:
 for row in rows:
  if any('atlas_mj_main_common_' in p or 'atlas_mj_main_chs_0' in p or 'atlas_common_yiman_chs_0' in p or 'atlas_common_yiman_common_0' in p for p in row['paths']):
   env=UnityPy.load(z.read(row['bundle']))
   for o in env.objects:
    if o.type.name=='Sprite' and o.peek_name() in names:
     name=o.peek_name()+'.png';o.read().image.save(OUT/name);records.append({'output':'Results/'+name,'source':'Majsoul_4.0.16_MC.apk','bundle':row['bundle'],'pathID':o.path_id,'sha256':digest((OUT/name).read_bytes())})
 def keys(a):return [{'time':k['key'],'value':k['value']}for k in a]
 def walk(n,pos=(0,0,0)):
  c=n.get('customProps',{});pos=[a+b for a,b in zip(pos,c.get('translate',[0,0,0]))]
  if n['type']=='ShuriKenParticle3D':
   mp=c['material']['path'];stem=Path(mp).stem
   if mp not in materials:
    mat=json.loads(fetch(mp))['props'];tp=mat['textures'][0]['path'];name=Path(tp).name;row=next((r for r in rows if any(p.endswith('/'+name.lower()) for p in r['paths'])),None)
    out=FX/('fire-'+stem+'.png')
    if row:
     path=next(p for p in row['paths'] if p.endswith('/'+name.lower()));e=UnityPy.load(z.read(row['bundle']));o=e.container[path];o.read().image.save(out);records.append({'output':'Actions/'+out.name,'source':'Majsoul_4.0.16_MC.apk','path':path,'bundle':row['bundle'],'pathID':o.path_id,'sha256':digest(out.read_bytes())})
    else:out.write_bytes(fetch(str(Path(mp).parent/tp).replace('\\','/')))
    materials[mp]={'texture':'fire-'+stem,'tint':next(v['value']for v in mat['vectors']if v['name']=='tintColor'),'blendDst':mat['dstBlend']}
   l=dict(name=n['props']['name'],position=[pos[0],pos[1],-pos[2]],rotation=c['rotation'],duration=c['duration'],delay=c['startDelay'],color=c['startColorConstant'],colorMin=c['startColorConstantMin'] if c['startColorType']==2 else c['startColorConstant'],colorMax=c['startColorConstantMax']if c['startColorType']==2 else c['startColorConstant'],size3d=c['startSizeConstantSeparate']if c['threeDStartSize']else [],gravity=c['gravityModifier'],rate=c['emission']['emissionRate'],maxParticles=c['maxParticles'],seed=c['randomSeed'],renderMode=c['renderMode'],stretch=c['stretchedBillboardLengthScale'],rotationMin=c['startRotationConstantMin'] if c['startRotationType']==2 else c['startRotationConstant'],rotationMax=c['startRotationConstantMax']if c['startRotationType']==2 else c['startRotationConstant'],alpha=[],size=[],bursts=[{'time':b['time'],'min':b['min'],'max':b['max']}for b in c['emission']['bursts']],**materials[mp])
   for name in ['life','size','speed']:
    source={'life':'Lifetime','size':'Size','speed':'Speed'}[name];l[name+'Min']=c['start'+source+'ConstantMin']if c['start'+source+'Type']==2 else c['start'+source+'Constant'];l[name+'Max']=c['start'+source+'ConstantMax']if c['start'+source+'Type']==2 else c['start'+source+'Constant']
   co=c.get('colorOverLifetime',{});so=c.get('sizeOverLifetime',{});sh=c.get('shape',{});ts=c.get('textureSheetAnimation',{});vel=c.get('velocityOverLifetime',{}).get('velocity',{})
   if co.get('enable'):l['alpha']=keys(co['color']['gradient']['alphas'])
   if so.get('enable'):l['size']=keys(so['size']['gradient']['sizes'])
   l.update(shape=sh.get('shapeType',-1),radius=sh.get('coneRadius',.03),coneAngle=sh.get('coneAngle',0),tiles=ts.get('tiles',[1,1]),randomFrame=ts.get('enable',False),velocityMin=vel.get('constantMin',[]),velocityMax=vel.get('constantMax',[]))
   layers.append(l)
  for ch in n.get('child',[]):walk(ch,pos)
 walk(json.loads(fetch('effect_hupai_fire.lh')))
exported={Path(t['output']).stem for t in records if t.get('output','').startswith('Results/')}
assert names<=exported, 'Missing APK glyphs: '+str(names-exported)
assert len(layers)==8,len(layers)
(FX/'fire-effect.json').write_bytes((json.dumps({'layers':layers},indent=2)+'\n').encode())
records.append({'output':'Actions/fire-effect.json','sha256':digest((FX/'fire-effect.json').read_bytes()),'adaptation':'Original fire layer curves and APK textures. Unity adds a rising flame spiral and orbiting dark rubble, timed from the supplied winning tile video; this skin is not packaged as a standalone prefab in the supplied APK.'})
(OUT/'source.json').write_bytes((json.dumps(records,ensure_ascii=False,indent=2)+'\n').encode('utf8'))
print('Imported',len(names),'result sprites and',len(layers),'fire layers')
