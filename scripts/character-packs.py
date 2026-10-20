"""Finalize, publish and verify versioned character catalogs without re-downloading uploads."""
from pathlib import Path
import argparse, concurrent.futures, hashlib, importlib.util, json, re, time
import urllib.error, urllib.parse, urllib.request

R=Path(__file__).resolve().parent.parent
REPO='Rtyu3221cc/YunJianRiiChi'
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('mode',choices=['finalize','stage','publish','verify','auth'])
parser.add_argument('--build-root',type=Path,default=R/'build/character-resources-all')
parser.add_argument('--workers',type=int,default=2)
args=parser.parse_args();B=args.build_root.resolve()
assert B.is_relative_to(R.resolve())

def js(p,value):
    p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n','utf8')
def sha(p):
    h=hashlib.sha256()
    with p.open('rb')as f:
        for block in iter(lambda:f.read(1048576),b''):h.update(block)
    return h.hexdigest()
def github():
    s=importlib.util.spec_from_file_location('pack_github',R/'scripts/github-release-0545.py');g=importlib.util.module_from_spec(s);s.loader.exec_module(g);g.authenticate()
    repo=g.api(g.BASE);assert repo['full_name']==REPO and repo['permissions']['push'];return g
def assets(g,release):
    result=[];page=1
    while True:
        rows=g.api(release['assets_url']+'?per_page=100&page='+str(page));result.extend(rows)
        if len(rows)<100:return result
        page+=1
def finalize():
    plan=json.loads((B/'build-plan.json').read_text('utf8'));index={p['key']:p for p in plan['packs']};upload=[]
    roles=json.loads((R/'unity/Assets/Resources/Majsoul/CharacterDirectory/catalog.json').read_text('utf8'))['roles']
    for platform in ['windows','android']:
        previous_file=B/'previous-catalogs'/(platform+'.json')
        if not previous_file.exists():
            previous_file.parent.mkdir(parents=True,exist_ok=True)
            previous_file.write_bytes((R/'unity/Assets/Resources/Majsoul/CharacterDirectory'/('packs-'+platform+'.json')).read_bytes())
        previous={p['key']:p for p in json.loads(previous_file.read_text('utf8'))['packs']}
        catalog=json.loads((B/'directory-template.json').read_text('utf8'));catalog['platform']=platform
        for row in catalog['packs']:
            entry=index[row['key']]
            if not row['available']:continue
            if entry['build']:
                p=B/platform/(row['key']+'-v'+str(entry['version'])+'-'+platform+'.bundle');assert p.is_file(),p
                digest=sha(p);upload.append(dict(path=p.relative_to(R).as_posix(),name=p.name,bytes=p.stat().st_size,sha256=digest))
                if not row['builtin']:row.update(file=p.name,bytes=p.stat().st_size,sha256=digest,compression='lz4',url='https://github.com/'+REPO+'/releases/download/'+plan['tag']+'/'+p.name)
            else:
                old=previous[row['key']];assert old['version']==row['version']and old['skins']==row['skins']and old['available']
                for field in ['file','bytes','sha256','url','compression']:
                    if field in old:row[field]=old[field]
        for p in [B/('catalog-'+platform+'.json'),R/'unity/Assets/Resources/Majsoul/CharacterDirectory'/('packs-'+platform+'.json')]:js(p,catalog)
        p=B/('catalog-'+platform+'.json');upload.append(dict(path=p.relative_to(R).as_posix(),name=p.name,bytes=p.stat().st_size,sha256=sha(p)))
    client_version=re.search(r'ReleaseVersion\s*=\s*"([0-9.]+)"',(R/'unity/Assets/Editor/ProjectSetup.cs').read_text('utf8'))[1]
    js(B/'catalog-summary.json',dict(tag=plan['tag'],clientVersion=client_version,roles=len(roles),skins=sum(len(r['skins'])for r in roles),dynamics=sum(s['dynamic']for r in roles for s in r['skins']),newOrUpdatedPacks=sum(p['build']for p in plan['packs']),protected=['yiji','erjietang']))
    p=B/'catalog-summary.json';upload.append(dict(path=p.relative_to(R).as_posix(),name=p.name,bytes=p.stat().st_size,sha256=sha(p)))
    js(B/'publish-assets.json',upload);print('ALL PACK CATALOG PASS',len(upload),'new files',sum(p['bytes']for p in upload),'bytes',flush=True)
def delete_draft(g,release,item):
    assert release['draft'],'Published resource files are immutable; use a new version/tag'
    for attempt in range(5):
        try:
            with urllib.request.urlopen(urllib.request.Request(item['url'],method='DELETE',headers=g.HEADERS),timeout=60)as response:assert response.status==204
            return
        except urllib.error.HTTPError as error:
            if error.code==404:return
            if error.code<500 or attempt==4:raise
            time.sleep(min(5*(attempt+1),20))
def same(item,row):return item and item['state']=='uploaded'and item['size']==row['bytes']and item.get('digest')=='sha256:'+row['sha256']
def find_release(g,tag):
    state=B/'release-state.json'
    if state.exists():
        saved=json.loads(state.read_text('utf8'))
        if saved['tag']==tag:
            release=g.api(g.BASE+'/releases/'+str(saved['id']));assert release['tag_name']==tag;return release
    try:return g.api(g.BASE+'/releases/tags/'+tag)
    except urllib.error.HTTPError as error:
        if error.code!=404:raise
    page=1;found=[]
    while True:
        rows=g.api(g.BASE+'/releases?per_page=100&page='+str(page));found.extend(r for r in rows if r['tag_name']==tag)
        if len(rows)<100:break
        page+=1
    if found:
        release=min(found,key=lambda r:r['id']);js(state,dict(tag=tag,id=release['id']));return release
    return None

def publish(stage_only=False):
    g=github();plan=json.loads((B/'build-plan.json').read_text('utf8'));summary=json.loads((B/'catalog-summary.json').read_text('utf8'))
    release=find_release(g,plan['tag'])
    if release is None:
        release=g.api(g.BASE+'/releases','POST',dict(tag_name=plan['tag'],target_commitish='main',name='云间日麻 · 全角色资源 '+plan['tag'],draft=True,prerelease=True,
            body=f"当前固定资源目录包含{summary['roles']}名角色、{summary['skins']}套服饰（{summary['dynamics']}套动态）。本版本补充其他角色及更新皮肤；旧资源版本与下载地址保留。Android与Windows编辑器分别打包，新包采用LZ4分块。完整目录给出每个角色的实际版本、大小、SHA-256和固定URL，可同时引用旧版本包。一姬、二阶堂美树继续内置且不可删除。配合Unity {summary['clientVersion']}或后续兼容客户端使用。"))
    js(B/'release-state.json',dict(tag=plan['tag'],id=release['id']))
    known={a['name']:a for a in assets(g,release)}
    rows=json.loads((B/'publish-assets.json').read_text('utf8'))
    def upload(row):
        p=R/row['path'];assert p.stat().st_size==row['bytes']and sha(p)==row['sha256'];item=known.get(row['name'])
        if same(item,row):return row['name']
        if item:delete_draft(g,release,item)
        assert release['draft'],'Published resource files are immutable'
        for attempt in range(5):
            try:
                print('Uploading resource',row['name'],row['bytes'],flush=True)
                def chunks():
                    with p.open('rb')as stream:
                        while block:=stream.read(1048576):yield block
                request=urllib.request.Request(release['upload_url'].split('{')[0]+'?'+urllib.parse.urlencode({'name':row['name']}),data=chunks(),method='POST',headers=dict(g.HEADERS,**{'Content-Type':'application/octet-stream','Content-Length':str(row['bytes'])}))
                with urllib.request.urlopen(request,timeout=600)as response:item=json.load(response)
                assert same(item,row),row['name'];return row['name']
            except Exception as error:
                print('Retrying resource',row['name'],'after',type(error).__name__,flush=True)
                if attempt==4:raise
                time.sleep(5*(attempt+1));item=next((a for a in assets(g,release)if a['name']==row['name']),None)
                if same(item,row):return row['name']
                if item:delete_draft(g,release,item)
    failures=[]
    with concurrent.futures.ThreadPoolExecutor(max_workers=args.workers)as pool:
        futures={pool.submit(upload,row):row['name']for row in rows}
        for future in concurrent.futures.as_completed(futures):
            try:print('Resource verified',future.result(),flush=True)
            except Exception as error:
                failures.append(futures[future]);print('Resource failed',futures[future],type(error).__name__,flush=True)
    assert not failures,'Retry unfinished resource files: '+', '.join(failures)
    remote={a['name']:a for a in assets(g,release)};assert all(same(remote.get(r['name']),r)for r in rows)
    if stage_only:
        js(B/'staged.json',dict(url=release['html_url'],draft=release['draft'],verifiedAssets=len(remote)))
        print('CHARACTER RESOURCE STAGING PASS',len(remote),'attachments; release remains in its existing visibility state',flush=True)
        return
    if release['draft']:release=g.api(release['url'],'PATCH',dict(draft=False,prerelease=True,make_latest='false'))
    verify(g,release)
def verify(g=None,release=None):
    g=g or github();plan=json.loads((B/'build-plan.json').read_text('utf8'));release=release or find_release(g,plan['tag']);assert release is not None and not release['draft']
    listed=assets(g,release);remote={a['name']:a for a in listed};rows=json.loads((B/'publish-assets.json').read_text('utf8'));assert all(same(remote.get(r['name']),r)for r in rows)
    # Existing catalogs may reference immutable attachments from earlier releases.
    by_tag={plan['tag']:remote}
    for platform in ['windows','android']:
        for row in json.loads((B/('catalog-'+platform+'.json')).read_text('utf8'))['packs']:
            if not row['available']or row['builtin']:continue
            tag=urllib.parse.urlparse(row['url']).path.split('/')[-2]
            if tag not in by_tag:by_tag[tag]={a['name']:a for a in assets(g,g.api(g.BASE+'/releases/tags/'+tag))}
            assert same(by_tag[tag].get(row['file']),row),(platform,row['key'])
    js(B/'published.json',dict(url=release['html_url'],assets=listed,allCatalogReferencesVerified=True,artifactRedownloaded=False))
    print('ALL CHARACTER RESOURCE PUBLISH PASS',release['html_url'],len(listed),'new attachments',flush=True)
if __name__=='__main__':
    if args.mode=='auth':g=github();print('Destination verified',REPO)
    elif args.mode=='stage':publish(stage_only=True)
    else:globals()[args.mode]()
