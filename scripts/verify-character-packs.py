"""Validate every declared delivery bundle and its address ownership on both platforms."""
from pathlib import Path
import argparse, gc, hashlib, json, sys
R=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(R/'tools/asset-inspector'))
import UnityPy
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--build-root',type=Path,default=R/'build/character-resources-all');p.add_argument('--keys');args=p.parse_args();B=args.build_root.resolve();keys=set(args.keys.split(','))if args.keys else None
directory=json.loads((R/'unity/Assets/Resources/Majsoul/CharacterDirectory/catalog.json').read_text('utf8'));roles={r['key']:r for r in directory['roles']};checks=[]
plan=json.loads((B/'build-plan.json').read_text('utf8'))
for platform in ['android','windows']:
 catalog=json.loads((B/('catalog-'+platform+'.json')).read_text('utf8'))
 for row in catalog['packs']:
  if not row['available']or keys is not None and row['key']not in keys:continue
  filename=row['file']or row['key']+'-v'+str(row['version'])+'-'+platform+'.bundle'
  candidates=[R/folder/platform/filename for folder in [plan['output'],'build/character-resources-all','build/character-resources-v1','build/character-packs-cache']]
  file=next((f for f in candidates if f.is_file()),None)
  if file is None and row['builtin']:
   checks.append(dict(platform=platform,key=row['key'],builtin=True,skins=len(row['skins']),source='APK Resources'));continue
  assert file is not None,filename
  env=UnityPy.load(str(file));bundle=next(o.read_typetree()for o in env.objects if o.type.name=='AssetBundle');names={name for name,_ in bundle['m_Container']};assert len(names)==len(bundle['m_Container'])
  if row.get('compression')=='lz4':assert int(env.file._block_info_flags)&63 in (2,3),'Declared LZ4 bundle uses another block format: '+row['key']
  for skin in roles[row['key']]['skins']:
   for kind in ['full','bighead','smallhead']:assert f'skins/{skin["id"]}/{kind}'in names,(row['key'],skin['id'],kind)
   for part in skin['parts']if skin['dynamic']else[]:assert part.lower()+'/skeleton'in names and part.lower()+'/atlas'in names,(row['key'],part)
  voices=[s for s in names if s.startswith('voice/'+row['key']+'/')];emojis=[s for s in names if s.startswith('emoji/'+row['key']+'/')];assert voices and emojis,row['key']
  assert len(set(roles[row['key']]['emojis']))==len(roles[row['key']]['emojis'])
  assert all(name.lower()in names for name in roles[row['key']]['emojis']),'Declared expression missing from bundle: '+row['key']
  digest=hashlib.sha256(file.read_bytes()).hexdigest()
  if not row['builtin']:assert row['sha256']==digest and row['bytes']==file.stat().st_size
  checks.append(dict(platform=platform,key=row['key'],skins=len(row['skins']),addresses=len(names),voices=len(voices),emojis=len(emojis),bytes=file.stat().st_size,sha256=digest));print('BUNDLE VERIFIED',platform,row['name'],len(names),'assets',flush=True)
  del env,bundle;gc.collect()
expected=2*sum(r['available']and(keys is None or r['key']in keys)for r in catalog['packs']);assert len(checks)==expected
(B/'bundle-verification.json').write_text(json.dumps(dict(bundleChecks=checks,androidDeviceTested=False),ensure_ascii=False,indent=2)+'\n','utf8');print('FULL BUNDLE VERIFICATION PASS',len(checks),flush=True)
