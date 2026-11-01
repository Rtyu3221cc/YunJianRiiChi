"""Upload immutable wardrobe assets; draft first, publish after APK/runtime gates.

No download mode. GitHub's asset digest/size verifies every uploaded byte stream.
"""
from pathlib import Path
import concurrent.futures,hashlib,importlib.util,json,sys,threading,time,urllib.error,urllib.parse,urllib.request
R=Path(__file__).resolve().parent.parent;B=R/'build/wardrobe-r22';TAG='wardrobe-resources-v1'
def module(name):
 spec=importlib.util.spec_from_file_location('wardrobe_github',R/'scripts'/name);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
g=module('github-release-0545.py');lock=threading.Lock();last_request=0
def sha(path):
 h=hashlib.sha256()
 with path.open('rb')as f:
  for block in iter(lambda:f.read(1048576),b''):h.update(block)
 return h.hexdigest()
def save(path,value):path.write_text(json.dumps(value,ensure_ascii=False,indent=1)+'\n','utf8')
def api(url,method='GET',value=None):
 for attempt in range(7):
  try:return g.api(url,method,value)
  except urllib.error.HTTPError as error:
   body=error.read().decode('utf8','replace')
   if error.code not in [429,500,502,503,504]and not(error.code==403 and('rate limit'in body.lower()or error.headers.get('Retry-After'))):raise
   delay=min(60,int(error.headers.get('Retry-After','0'))or 2**(attempt+1));print('GitHub retry',error.code,'in',delay,'seconds',flush=True);time.sleep(delay)
 raise RuntimeError('GitHub retries exhausted')
def assets(release):
 rows=[];page=1
 while True:
  part=api(release['url']+'/assets?per_page=100&page='+str(page));rows+=part
  if len(part)<100:return {x['name']:x for x in rows}
  page+=1
def finalize():
 rows=[]
 for platform in ['android','windows']:
  p=R/'unity/Assets/Resources/Majsoul/WardrobeDirectory'/('packs-'+platform+'.json');cat=json.loads(p.read_text('utf8'));assert cat['platform']==platform
  for pack in cat['packs']:
   if pack['builtin']:continue
   file=B/'packs'/platform/pack['file'];assert pack['available']and file.stat().st_size==pack['bytes']and sha(file)==pack['sha256']
   rows.append(dict(path=file.relative_to(R).as_posix(),name=file.name,bytes=pack['bytes'],sha256=pack['sha256']))
  rows.append(dict(path=p.relative_to(R).as_posix(),name=p.name,bytes=p.stat().st_size,sha256=sha(p)))
 assert len(rows)==762 and len({x['name']for x in rows})==762
 save(B/'publish-assets.json',rows);print('WARDROBE UPLOAD MANIFEST PASS',len(rows),sum(x['bytes']for x in rows),'bytes',flush=True)
def release():
 try:return api(g.BASE+'/releases/tags/'+TAG)
 except urllib.error.HTTPError as error:
  if error.code!=404:raise
 return api(g.BASE+'/releases','POST',dict(tag_name=TAG,target_commitish='main',name='云间日麻 · 装扮资源 v1',draft=True,prerelease=True,body='适配 Unity 0.6.18 及后续兼容客户端。基础款离线内置，其他装扮支持单件、按种类下载和局域网接收。覆盖 59 款已定位原生和牌特效（客户端另含默认款）、既有立直与鸣牌装扮、静态装扮及音乐。Android 与 Windows 资源分别构建，共享依赖按需下载。文件名含内容指纹，目录提供大小与 SHA-256。未完成原版视频逐像素比对和 Android 真机多手机热点测试。'))
def draft():
 g.authenticate();assert api(g.BASE)['permissions']['push'];rel=release();remote=assets(rel);rows=json.loads((B/'publish-assets.json').read_text('utf8'));completed=0
 def upload(row):
  global last_request
  path=R/row['path'];assert path.stat().st_size==row['bytes']and sha(path)==row['sha256'];item=remote.get(row['name'])
  if item and item['state']!='uploaded':assert rel['draft'];api(item['url'],'DELETE');item=None
  for attempt in range(6):
   if item is not None:break
   assert rel['draft'],'Published resources are immutable'
   with lock:
    delay=max(0,.85-(time.monotonic()-last_request))
    if delay:time.sleep(delay)
    last_request=time.monotonic()
   def chunks():
    with path.open('rb')as f:
     while block:=f.read(1048576):yield block
   request=urllib.request.Request(rel['upload_url'].split('{')[0]+'?'+urllib.parse.urlencode({'name':row['name']}),data=chunks(),method='POST',headers=dict(g.HEADERS,**{'Content-Type':'application/octet-stream','Content-Length':str(row['bytes'])}))
   try:
    with urllib.request.urlopen(request,timeout=300)as response:item=json.load(response)
   except (urllib.error.URLError,TimeoutError,ConnectionError)as error:
    # An interrupted response may still have completed the upload remotely.
    time.sleep(min(60,2**(attempt+1)));item=assets(rel).get(row['name'])
    if item and item['state']!='uploaded':api(item['url'],'DELETE');item=None
    if attempt==5 and item is None:raise RuntimeError('Upload retry exhausted: '+row['name'])from error
  assert item and item['state']=='uploaded'and item['size']==row['bytes']and item.get('digest')=='sha256:'+row['sha256'],row['name']
  return item
 with concurrent.futures.ThreadPoolExecutor(max_workers=2)as pool:
  for item in pool.map(upload,rows):
   completed+=1
   if completed%20==0 or completed==len(rows):print('WARDROBE DRAFT UPLOAD',completed,'/',len(rows),item['name'],flush=True)
 verified=assets(rel)
 for row in rows:
  item=verified[row['name']];assert item['size']==row['bytes']and item['digest']=='sha256:'+row['sha256']and item['state']=='uploaded'
 save(B/'uploaded-draft.json',dict(id=rel['id'],url=rel['html_url'],draft=rel['draft'],assets=list(verified.values()),artifactRedownloaded=False));print('WARDROBE DRAFT VERIFIED',len(rows),flush=True)
def publish():
 verification=json.loads((R/'dist/unity-0.6.18-verification.json').read_text('utf8'));assert verification['version']=='0.6.18'and verification['apk']['bytes']<=300000000
 assert verification['apk']['v1']and verification['apk']['v2']and verification['apk']['independentV1']and verification['apk']['alignment16KB']
 for file,cases in [('checks.json',480),('checks-spot.json',48)]:assert json.loads((B/'capture'/file).read_text('utf8'))['cases']==cases
 assert json.loads((B/'capture/checks-download.json').read_text('utf8'))['checks']
 g.authenticate();rel=release();uploaded=assets(rel)
 for row in json.loads((B/'publish-assets.json').read_text('utf8')):
  item=uploaded[row['name']];assert item['state']=='uploaded'and item['size']==row['bytes']and item['digest']=='sha256:'+row['sha256']
 if rel['draft']:rel=api(rel['url'],'PATCH',dict(draft=False,prerelease=True,make_latest='false',target_commitish=g.prepare.git('rev-parse','HEAD')))
 assert not rel['draft'];save(B/'published.json',dict(url=rel['html_url'],id=rel['id'],assets=list(uploaded.values()),artifactRedownloaded=False));print('WARDROBE RESOURCE PUBLISH PASS',rel['html_url'],len(uploaded),flush=True)
if __name__=='__main__':{'finalize':finalize,'draft':draft,'publish':publish}[sys.argv[1]]()
