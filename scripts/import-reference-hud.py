import sys,json,zipfile,warnings,pathlib
sys.path.insert(0,'tools/asset-inspector');import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
rows=[json.loads(s) for s in open('build/reference/fast-catalog.jsonl',encoding='utf8')];z=zipfile.ZipFile('dist/Majsoul_4.0.16_MC.apk');out=pathlib.Path('unity/Assets/Resources/Majsoul/Hud');out.mkdir(exist_ok=True)
r=next(r for r in rows if any('atlas_mj_main_common_0.png' in p for p in r['paths']));env=UnityPy.load(z.read(r['bundle']))
for o in env.objects:
 if o.type.name=='Sprite' and (o.peek_name() in ['bd'] or o.peek_name().startswith('w_')):
  d=o.read();d.image.save(out/(d.m_Name+'.png'));print(d.m_Name,d.image.size)
cm=json.load(open('build/reference/tile-study/cab-map.json'));env=UnityPy.load(z.read(cm['CAB-2c132555efa8e8aa3234c29660254358']));af=next(o.assets_file for o in env.objects);cache={};records=[]
for o in env.objects:
 if o.type.name!='Material' or o.peek_name() not in ['chang','ju','word_left','number_left','number_score','dir']:continue
 t=o.read_typetree()
 for key,v in t['m_SavedProperties']['m_TexEnvs']:
  if key!='_MainTex':continue
  p=v['m_Texture'];cab=af.externals[p['m_FileID']-1].path.split('/')[-1]
  if cab not in cache:cache[cab]=UnityPy.load(z.read(cm[cab]))
  tex=next(o for o in cache[cab].objects if o.path_id==p['m_PathID']).read();tex.image.save(out/(t['m_Name']+'.png'));records.append({'name':t['m_Name'],'uv':v,'width':tex.m_Width,'height':tex.m_Height});print(t['m_Name'],tex.m_Width,tex.m_Height,v)
(out/'layout-source.json').write_text(json.dumps(records,indent=2),encoding='utf8')
