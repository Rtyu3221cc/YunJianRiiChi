"""Acquire only pinned win-effect groups, reusing the existing local cache."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import hashlib,json,runpy,shutil,struct
R=Path(__file__).resolve().parent.parent;B=R/'build/wardrobe-r22';B.mkdir(parents=True,exist_ok=True)
M=json.loads((R/'build/effects-native-manifest.json').read_text('utf8'));T=json.loads((R/'build/wardrobe-r14/current-tables.json').read_text('utf8'));V={v['id']:v for v in T['item_definition/view']}
groups=[g for g in M['groupInfos']if g['groupName'].startswith('deco/effect_hupai/')]
keys=sorted({g['groupName'].split('/')[2]for g in groups});rows=[]
for item in T['item_definition/item']:
    if item['category']!=5 or item['type']!=1:continue
    view=V.get(item['id'],{});names=[view.get('res_name',''),Path(item['icon']).stem]
    found=next((k for k in keys if k in names),None)
    rows.append(dict(id=item['id'],name=item['name_chs'],sourceKey=found,view=view,expired='已过期'in item['name_chs'],status='source-group-found'if found else'no-group-in-pinned-manifest'))
needed={i for g in groups for i in g['bundlesIndex']}
needed.update(a['ownerBundleIndex']for a in M['assetInfos']if a['assetPath'].lower().startswith(('myassets/common3d/','myassets/scenes/mj/effect_mj/main/','myassets/scenes/mj/mjtable/')))
fetch=runpy.run_path(str(R/'scripts/collect-effect-reference.py'))['fetch']
previous=json.loads((R/'dist/previews/2026-10-27-effects-reference/source/bundle-index.json').read_text('utf8'));pinned={x['file']:x['sha256']for x in previous if 'sha256'in x}
def one(i):
    entry=M['bundleInfos'][i];name=entry['name'];path=R/'dist/previews/2026-10-27-effects-reference/source/bundles'/name
    cached=path.exists()
    if not cached and shutil.disk_usage(R).free<15*1024**3:raise RuntimeError('D drive below 15 GiB reserve; stop acquisition')
    row=fetch(dict(url='https://game.maj-soul.com/assetbundles/DXT/'+name))
    if 'error'in row:raise RuntimeError(str(row))
    raw=path.read_bytes();offset=raw.index(b'\0')+1+4;offset=raw.index(b'\0',offset)+1;offset=raw.index(b'\0',offset)+1
    if struct.unpack_from('>q',raw,offset)[0]!=len(raw):raise RuntimeError('Incomplete UnityFS bundle: '+name)
    if name in pinned and row['sha256']!=pinned[name]:raise RuntimeError('Previously pinned bundle changed: '+name)
    # This native manifest reports different compression sizes from DXT for some
    # already verified shared bundles. Keep both figures; validate the file header.
    return dict(row,index=i,reused=cached,manifestReportedBytes=entry['fileSize'],unityFsLengthVerified=True)
with ThreadPoolExecutor(max_workers=3)as pool:bundles=list(pool.map(one,sorted(needed)))
out=dict(clientVersion='4.0.51',resourceVersion='0.16.289',publicTableVersion='0.11.252.w',definitions=rows,nativeKeys=keys,bundles=bundles,totalBytes=sum(x['bytes']for x in bundles),newBytes=sum(x['bytes']for x in bundles if not x['reused']),remainingDiskBytes=shutil.disk_usage(R).free)
(B/'source-inventory.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n','utf8')
print('WIN SOURCE ACQUISITION PASS',len(rows),'definitions;',len(keys),'native keys;',len(bundles),'bundles;',out['newBytes'],'new bytes;',out['remainingDiskBytes'],'bytes free')
