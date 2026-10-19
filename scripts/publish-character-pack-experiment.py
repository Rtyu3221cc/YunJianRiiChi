"""Publish only the experimental content assets to the previously confirmed repository."""
from pathlib import Path
import json,hashlib,importlib.util,urllib.request,urllib.parse,urllib.error,sys
R=Path(__file__).resolve().parent.parent;B=R/'build/character-pack-experiment';TAG='character-resources-experiment-1';BASE='https://github.com/Rtyu3221cc/YunJianRiiChi/releases/download/'+TAG+'/'
def js(p,x):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def finalize(catalog_only=False):
 assets=[]
 for platform in ['windows','android']:
  catalog=json.loads((B/'directory-template.json').read_text('utf8'));catalog['platform']=platform
  for row in catalog['packs']:
   if row['builtin']or not row['available']:continue
   path=B/platform/(row['key']+'-v1-'+platform+'.bundle');assert path.is_file();row.update(bytes=path.stat().st_size,sha256=sha(path),file=path.name,url=BASE+path.name)
   if not catalog_only:assets.append(dict(path=str(path.relative_to(R)),name=path.name,bytes=path.stat().st_size,sha256=sha(path)))
  filename='catalog-'+platform+('-r2'if catalog_only else'')+'.json'
  for path in [B/filename,R/'unity/Assets/Resources/Majsoul/CharacterDirectory'/('packs-'+platform+'.json')]:js(path,catalog)
  path=B/filename;assets.append(dict(path=str(path.relative_to(R)),name=path.name,bytes=path.stat().st_size,sha256=sha(path)))
 js(B/('publish-assets-r2.json'if catalog_only else'publish-assets.json'),assets);print('Prepared',len(assets),'content-only assets',sum(x['bytes']for x in assets),'bytes')
def publish(catalog_only=False):
 spec=importlib.util.spec_from_file_location('github',R/'scripts/github-release-0545.py');g=importlib.util.module_from_spec(spec);spec.loader.exec_module(g);g.authenticate();repo=g.api(g.BASE);assert repo['full_name']=='Rtyu3221cc/YunJianRiiChi'and repo['permissions']['push'];print('Verified existing resource destination',repo['full_name'],flush=True)
 try:release=g.api(g.BASE+'/releases/tags/'+TAG)
 except urllib.error.HTTPError as ex:
  if ex.code!=404:raise
  release=g.api(g.BASE+'/releases','POST',dict(tag_name=TAG,target_commitish='4be1ea85a7dc1c3e87088a54863f1fbc86e5cd27',name='云间日麻 · 角色资源下载实验 v1',body='独立角色资源实验附件：八木唯9套服饰、174段原版语音和表情；未来与寺崎千穗理迁移为可管理资源包。包括Android和Windows编辑器实验包及各平台大小/哈希目录。基础角色一姬、二阶堂美树继续内置。此Release仅分发实验资源，不是新APK。',draft=True,prerelease=True))
 indexed={x['name']:x for x in release['assets']}
 for row in json.loads((B/('publish-assets-r2.json'if catalog_only else'publish-assets.json')).read_text('utf8')):
  p=R/row['path'];assert p.stat().st_size==row['bytes']and sha(p)==row['sha256']
  if row['name']in indexed:item=indexed[row['name']]
  else:
   assert release['draft']or catalog_only;print('Uploading resource',row['name'],row['bytes'],flush=True)
   def chunks():
    with p.open('rb')as stream:
     while chunk:=stream.read(1024*1024):yield chunk
   req=urllib.request.Request(release['upload_url'].split('{')[0]+'?'+urllib.parse.urlencode({'name':row['name']}),data=chunks(),method='POST',headers=dict(g.HEADERS,**{'Content-Length':str(row['bytes']),'Content-Type':'application/octet-stream'}))
   with urllib.request.urlopen(req,timeout=300)as response:item=json.load(response)
  assert item['state']=='uploaded'and item['size']==row['bytes']and item.get('digest')=='sha256:'+row['sha256']
 if release['draft']:release=g.api(release['url'],'PATCH',dict(draft=False,prerelease=True,make_latest='false'))
 if catalog_only:release=g.api(release['url'],'PATCH',dict(body=release['body']+'\n\n目录以 catalog-*-r2.json 为准，仅列出资源包中实际包含的服饰；原始 bundle 文件保持不变。'))
 js(B/'published.json',dict(url=release['html_url'],assets=g.api(release['url'])['assets'],mainUnchanged=True));print('Resource release published and digests verified',release['html_url'],flush=True)
if __name__=='__main__':{'finalize':finalize,'publish':publish,'finalize-r2':lambda:finalize(True),'publish-r2':lambda:publish(True)}[sys.argv[1]]()
