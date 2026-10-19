"""Publish versioned role bundles; verify upload metadata without downloading it."""
from pathlib import Path
import hashlib, importlib.util, json, sys, urllib.error, urllib.parse, urllib.request

R=Path(__file__).resolve().parent.parent
B=R/'build/character-resources-v1'
TAG='character-resources-v1'
REPO='Rtyu3221cc/YunJianRiiChi'

def js(p,x):
    p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n','utf8')

def sha(p):
    h=hashlib.sha256()
    with p.open('rb') as f:
        for block in iter(lambda:f.read(1048576),b''):h.update(block)
    return h.hexdigest()

def module(file):
    spec=importlib.util.spec_from_file_location('github_resources',R/'scripts'/file)
    g=importlib.util.module_from_spec(spec);spec.loader.exec_module(g);return g

def finalize():
    assets=[]
    approved=json.loads((B/'approved-roles.json').read_text('utf8'))
    assert len(approved)==11 and sum(p['skins'] for p in approved)==71
    for platform in ['windows','android']:
        catalog=json.loads((B/'directory-template.json').read_text('utf8'))
        catalog['platform']=platform
        for row in catalog['packs']:
            if not row['available']:continue
            path=B/platform/(row['key']+'-v1-'+platform+'.bundle')
            assert path.is_file()
            digest=sha(path)
            assets.append(dict(path=path.relative_to(R).as_posix(),name=path.name,bytes=path.stat().st_size,sha256=digest))
            if not row['builtin']:
                row.update(file=path.name,bytes=path.stat().st_size,sha256=digest,
                    url='https://github.com/'+REPO+'/releases/download/'+TAG+'/'+path.name)
        assert sum(p['available']and p['recommended'] for p in catalog['packs'])==11
        for path in [B/('catalog-'+platform+'.json'),R/'unity/Assets/Resources/Majsoul/CharacterDirectory'/('packs-'+platform+'.json')]:js(path,catalog)
        path=B/('catalog-'+platform+'.json')
        assets.append(dict(path=path.relative_to(R).as_posix(),name=path.name,bytes=path.stat().st_size,sha256=sha(path)))
    path=B/'approved-roles.json'
    assets.append(dict(path=path.relative_to(R).as_posix(),name=path.name,bytes=path.stat().st_size,sha256=sha(path)))
    js(B/'publish-assets.json',assets)
    print('RESOURCE CATALOG PASS',len(assets),'files',sum(a['bytes'] for a in assets),'bytes',flush=True)

def publish():
    g=module('github-release-0545.py');g.authenticate()
    repo=g.api(g.BASE);assert repo['full_name']==REPO and repo['permissions']['push']
    try:release=g.api(g.BASE+'/releases/tags/'+TAG)
    except urllib.error.HTTPError as ex:
        if ex.code!=404:raise
        release=g.api(g.BASE+'/releases','POST',dict(tag_name=TAG,target_commitish='main',
            name='云间日麻 · 女性TOP10＋汪次郎资源 v1',draft=True,prerelease=True,
            body='女性TOP10＋汪次郎，共11名角色、71套服饰，包括动态立绘、语音和表情。Android和Windows编辑器分别打包。额外保留未来、寺崎千穗理两个已有资源包。目录包含大小、SHA256及固定下载地址；一姬、二阶堂美树仍为不可删除的客户端内置角色，其bundle仅供完整资源归档，不会强制重复下载。搭配Unity 0.6.6或更新兼容客户端使用。'))
    assets={a['name']:a for a in g.api(release['url'])['assets']}
    for row in json.loads((B/'publish-assets.json').read_text('utf8')):
        p=R/row['path'];assert p.stat().st_size==row['bytes']and sha(p)==row['sha256']
        item=assets.get(row['name'])
        if item and item['state']!='uploaded':
            assert release['draft'];g.api(item['url'],'DELETE');item=None
        if not item:
            assert release['draft'],'Published assets are immutable'
            print('Uploading',row['name'],row['bytes'],flush=True)
            def chunks():
                with p.open('rb')as f:
                    while block:=f.read(1048576):yield block
            request=urllib.request.Request(release['upload_url'].split('{')[0]+'?'+urllib.parse.urlencode({'name':row['name']}),
                data=chunks(),method='POST',headers=dict(g.HEADERS,**{'Content-Type':'application/octet-stream','Content-Length':str(row['bytes'])}))
            with urllib.request.urlopen(request,timeout=600)as response:item=json.load(response)
        assert item['state']=='uploaded'and item['size']==row['bytes']and item.get('digest')=='sha256:'+row['sha256'],row['name']
    if release['draft']:release=g.api(release['url'],'PATCH',dict(draft=False,prerelease=True,make_latest='false'))
    final=g.api(release['url']);assert not final['draft']
    js(B/'published.json',dict(url=final['html_url'],assets=final['assets'],artifactRedownloaded=False))
    print('RESOURCE PUBLISH PASS',final['html_url'],len(final['assets']),flush=True)

if __name__=='__main__':{'finalize':finalize,'publish':publish}[sys.argv[1]]()
