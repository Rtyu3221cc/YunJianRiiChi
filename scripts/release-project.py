"""Publish the release selected by ProjectSetup, with fresh tests and immutable assets."""
from pathlib import Path
import argparse, datetime, hashlib, importlib.util, json, re, shutil, subprocess, time, zipfile
import urllib.error, urllib.parse, urllib.request
R=Path(__file__).resolve().parent.parent;D=R/'dist'
source=(R/'unity/Assets/Editor/ProjectSetup.cs').read_text('utf8')
VERSION=re.search(r'ReleaseVersion\s*=\s*"([0-9.]+)"',source)[1];CODE=int(re.search(r'ReleaseCode\s*=\s*(\d+)',source)[1])
B=R/('build/release-'+VERSION.replace('.',''));TAG='unity-'+VERSION
p=argparse.ArgumentParser(description=__doc__);p.add_argument('mode',choices=['begin','preflight','assemble','curate','commit','publish']);p.add_argument('--content-root',type=Path,default=R/'build/character-resources-all');args=p.parse_args();CONTENT=args.content_root.resolve();assert CONTENT.is_relative_to(R.resolve())
def module(name,file):
 s=importlib.util.spec_from_file_location(name,R/'scripts'/file);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
prepare=module('prepare_current','prepare-github-0545.py');github=module('github_current','github-release-0545.py')
APK=D/f'yunjian-riichi-unity-{VERSION}.apk';REPORT=D/f'unity-{VERSION}-verification.json';SHA=D/f'SHA256-unity-{VERSION}.txt';HANDOFF=R/f'docs/handoff-unity-{VERSION}.md';GUIDE=R/'docs/character-resource-maintenance.md';PREVIEW=D/'previews'/('unity-'+VERSION+'-resources')
def js(file,value):file.parent.mkdir(parents=True,exist_ok=True);file.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n','utf8')
def digest(file):return prepare.sha256(file)
def begin():
 B.mkdir(parents=True,exist_ok=True);assert not(B/'published.json').exists(),'This client version has already been published'
 js(B/'run.json',dict(version=VERSION,versionCode=CODE,started=time.time(),contentRoot=CONTENT.relative_to(R).as_posix(),contentPlanSHA256=digest(CONTENT/'build-plan.json')))
 print('RELEASE RUN STARTED',VERSION,CODE,B,flush=True)
def fresh(file):
 run=json.loads((B/'run.json').read_text('utf8'));assert file.is_file()and file.stat().st_mtime>=run['started']-2,'Missing/stale release evidence: '+str(file)
 return file.read_text('utf8',errors='replace')
def node_pass(file):
 text=fresh(file);a=re.search(r'pass\s+(\d+)\b',text);f=re.search(r'fail\s+(\d+)\b',text);assert a and int(a[1])>=146 and f and int(f[1])==0,'Node suite failed';return int(a[1])
def retained_keys():
 prefix='unity/Assets/CharacterPacks/Source/';result=set()
 for name in prepare.git('ls-files','-z',binary=True).decode('utf8').split('\0'):
  if name.startswith(prefix):
   key=name[len(prefix):].split('/')[0]
   result.add(key.removesuffix('.meta'))
 return result
def paths(extras=True):
 keep=retained_keys();prefix='unity/Assets/CharacterPacks/Source/';result=[]
 for file in prepare.source_paths():
  rel=file.relative_to(R).as_posix()
  if rel.startswith(prefix)and rel[len(prefix):].split('/')[0].removesuffix('.meta')not in keep:continue
  result.append(file)
 if extras:result += [REPORT,SHA,D/HANDOFF.name]+[x for x in PREVIEW.rglob('*')if x.is_file()]
 return result
def preflight():
 rows=prepare.check_files(paths(False));js(B/'source-preflight.json',rows);print('SOURCE PREFLIGHT PASS',len(rows),'files',sum(x['bytes']for x in rows),'bytes',flush=True)
def assemble():
 node=node_pass(B/'node-tests.log');py=fresh(B/'input-unit-tests.log');assert re.search(r'Ran\s+8\s+tests',py)and re.search(r'\bOK\b',py)
 assert 'CHARACTER REGISTRY PASS'in fresh(B/'registry-check.log')
 units=fresh(R/'build/unity-validate.log');assert 'RIICHI UNITY VALIDATION PASS'in units
 unit_counts={k:int(re.search(pattern,units)[1])for k,pattern in [('session',r'RELEASE 0545 UNIT PASS:\s*(\d+)'),('callout',r'RELEASE 063 UNIT PASS:\s*(\d+)'),('character',r'RELEASE 064 CHARACTER UNIT PASS:\s*(\d+)'),('roleUi',r'RELEASE 065 ROLE UI UNIT PASS:\s*(\d+)')]}
 resources=json.loads(fresh(R/'build/character-resources-all/validation/checks.json'))
 roles=json.loads((R/'unity/Assets/Resources/Majsoul/CharacterDirectory/catalog.json').read_text('utf8'))['roles']
 assert resources['roles']==len(roles)and resources['skins']==sum(len(r['skins'])for r in roles)
 bundles=json.loads(fresh(CONTENT/'bundle-verification.json'));assert len(bundles['bundleChecks'])==len(roles)*2
 flow=json.loads(fresh(R/'build/character-pack-experiment/render/checks.json'));assert len(flow['checks'])>=38
 assert 'CHARACTER PACK EXPERIMENT PASS'in fresh(CONTENT/'flow.log')
 assert 'RESOURCE LAYOUT REVIEW PASS'in fresh(R/'build/resource-ui-r3/review.log')
 assert 'PASS: final APK v1 + v2'in fresh(B/'apk-verify.log')
 published=json.loads((CONTENT/'published.json').read_text('utf8'));assert published['allCatalogReferencesVerified']
 sig=module('signature_current','verify-apk-signature.py').verify_signature(APK)
 badging=subprocess.run([str(R/'tools/sdk/android-15/aapt.exe'),'dump','badging',str(APK)],capture_output=True,text=True,encoding='utf8',errors='replace')
 assert badging.returncode==0 and "name='com.yunjian.riichi.unity'"in badging.stdout and f"versionCode='{CODE}'"in badging.stdout and f"versionName='{VERSION}'"in badging.stdout
 (B/'apk-badging.txt').write_text(badging.stdout,'utf8')
 date=datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=9))).date().isoformat()
 value=dict(version=VERSION,versionCode=CODE,date=date,nodeTests=node,inputUnitTests=8,unityUnitChecks=sum(unit_counts.values()),unitBreakdown=unit_counts,resourceChecks=resources,bundleChecks=bundles['bundleChecks'],resourceFlowChecks=flow['checks'],resourceRelease=published['url'],resourceAttachments=len(published['assets']),androidDeviceTested=False,artifactRedownloaded=False,apk=sig)
 js(REPORT,value);SHA.write_text(sig['sha256'].upper()+'  '+APK.name+'\n','utf8');assert HANDOFF.is_file();shutil.copyfile(HANDOFF,D/HANDOFF.name)
 rows=prepare.check_files(paths());js(B/'source-manifest.json',rows)
 source_zip=D/f'yunjian-riichi-source-{VERSION}.zip'
 with zipfile.ZipFile(source_zip,'w',zipfile.ZIP_DEFLATED,compresslevel=3)as archive:
  for row in rows:archive.write(R/row['path'],row['path'])
  archive.writestr('publication-manifest.json',json.dumps(rows,ensure_ascii=False,indent=2))
 release_zip=D/f'yunjian-riichi-unity-release-{VERSION}.zip'
 with zipfile.ZipFile(release_zip,'w',zipfile.ZIP_DEFLATED,compresslevel=1)as archive:
  for file in [APK,REPORT,SHA,D/HANDOFF.name,GUIDE]:archive.write(file,file.name)
 with zipfile.ZipFile(source_zip)as archive:assert archive.testzip()is None
 with zipfile.ZipFile(release_zip)as archive:
  assert archive.testzip()is None;payload=archive.read(APK.name);assert len(payload)==sig['bytes']and hashlib.sha256(payload).hexdigest()==sig['sha256']
  folder=B/'zip-copy-check';folder.mkdir(parents=True,exist_ok=True);inside=folder/APK.name;inside.write_bytes(payload);assert module('zip_current','verify-apk-signature.py').verify_signature(inside)['sha256']==sig['sha256']
 files=[APK,REPORT,SHA,D/HANDOFF.name,GUIDE,source_zip,release_zip]
 js(B/'release-assets.json',[dict(path=f.relative_to(R).as_posix(),name=f.name,bytes=f.stat().st_size,sha256=digest(f))for f in files]);print('RELEASE ASSEMBLY PASS',VERSION,sig['bytes'],sig['sha256'],flush=True)
def curate():
 assert not prepare.git('status','--porcelain'),'Publication checkout must start clean';assert prepare.git('branch','--show-current')=='main';assert prepare.git('remote','get-url','origin')=='https://github.com/Rtyu3221cc/YunJianRiiChi.git'
 rows=prepare.check_files(paths());js(B/'source-manifest.json',rows)
 for row in rows:
  source=R/row['path'];target=prepare.PUBLISH/row['path'];assert target.resolve().is_relative_to(prepare.PUBLISH.resolve())and not target.is_symlink()
  if not target.is_file()or digest(target)!=row['sha256']:target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target)
 deleted=[]
 for name in prepare.git('ls-files','-z',binary=True).decode('utf8').split('\0'):
  if not name or name.startswith('docs/')or not any(name.startswith(t+'/')for t in prepare.SOURCE_TREES):continue
  if not(R/name).exists():assert (prepare.PUBLISH/name).resolve().is_relative_to(prepare.PUBLISH.resolve());deleted.append(name)
 for names,command,suffix in [([r['path']for r in rows],'add','source'),(deleted,'rm','removed')]:
  if not names:continue
  spec=B/(suffix+'-pathspec.bin');spec.write_bytes(b'\0'.join(b':(literal)'+n.encode('utf8')for n in names)+b'\0');prepare.git(command,*(['-f']if command=='add'else[]),'--pathspec-from-file='+str(spec),'--pathspec-file-nul')
 staged=set(filter(None,prepare.git('diff','--cached','--name-only','-z',binary=True).decode('utf8').split('\0')))
 assert staged<={r['path']for r in rows}|set(deleted)and not any(Path(n).suffix in ['.apk','.jks','.keystore']for n in staged)
 js(B/'curation.json',dict(allowedFiles=len(rows),stagedFiles=len(staged),deletions=deleted,secretScanPassed=True,rawSourcePolicy='Previously tracked reference roles retained; new media delivered as versioned bundles and reproducible inputs'))
 print(prepare.git('diff','--cached','--shortstat'),flush=True)
def commit():
 node_pass(B/'publish-node-tests.log');prepare.git('-c','user.name=Rtyu3221cc','-c','user.email=55649183+Rtyu3221cc@users.noreply.github.com','commit','-m',f'Release Unity {VERSION}: full character resources, resource management refinements and maintenance tooling')
 sha=prepare.git('rev-parse','HEAD');js(B/'source-push.json',dict(version=VERSION,commit=sha));print('SOURCE COMMIT PASS',sha,flush=True)
def existing_client_release():
 state=B/'client-release-state.json'
 if state.exists():
  saved=json.loads(state.read_text('utf8'));assert saved['tag']==TAG
  release=github.api(github.BASE+'/releases/'+str(saved['id']));assert release['tag_name']==TAG;return release
 try:return github.api(github.BASE+'/releases/tags/'+TAG)
 except urllib.error.HTTPError as error:
  if error.code!=404:raise
 page=1;found=[]
 while True:
  rows=github.api(github.BASE+'/releases?per_page=100&page='+str(page));found.extend(r for r in rows if r['tag_name']==TAG)
  if len(rows)<100:break
  page+=1
 return min(found,key=lambda r:r['id'])if found else None

def upload_client_file(release,row,file,item):
 def matches(value):return value and value['state']=='uploaded'and value['size']==row['bytes']and value.get('digest')=='sha256:'+row['sha256']
 for attempt in range(5):
  if matches(item):return item
  assert release['draft'],'Published client files are immutable'
  if item:
   try:
    with urllib.request.urlopen(urllib.request.Request(item['url'],method='DELETE',headers=github.HEADERS),timeout=60)as response:assert response.status==204
   except urllib.error.HTTPError as error:
    if error.code!=404:raise
  try:
   print('Uploading release',row['name'],row['bytes'],flush=True)
   def chunks():
    with file.open('rb')as stream:
     while block:=stream.read(1048576):yield block
   request=urllib.request.Request(release['upload_url'].split('{')[0]+'?'+urllib.parse.urlencode({'name':row['name']}),data=chunks(),method='POST',headers=dict(github.HEADERS,**{'Content-Type':'application/octet-stream','Content-Length':str(row['bytes'])}))
   with urllib.request.urlopen(request,timeout=900)as response:item=json.load(response)
   assert matches(item),row['name'];return item
  except Exception as error:
   if attempt==4:raise
   print('Retrying client file',row['name'],type(error).__name__,flush=True);time.sleep(5*(attempt+1))
   item=next((a for a in github.api(release['url'])['assets']if a['name']==row['name']),None)

def publish():
 github.authenticate();repo=github.api(github.BASE);assert repo['full_name']=='Rtyu3221cc/YunJianRiiChi'and repo['permissions']['push'];sha=json.loads((B/'source-push.json').read_text('utf8'))['commit']
 prepare.git('push','origin','main');assert prepare.git('ls-remote','origin','refs/heads/main').split()[0]==sha
 report=json.loads(REPORT.read_text('utf8'));total=report['resourceChecks']
 release=existing_client_release()
 if release is None:
  release=github.api(github.BASE+'/releases','POST',dict(tag_name=TAG,target_commitish=sha,name=f'云间日麻 Unity {VERSION} · 全角色资源',draft=True,prerelease=True,body=f"当前目录开放{total['roles']}名角色、{total['skins']}套服饰、{total['dynamics']}套动态。一姬、二阶堂美树继续内置；其他角色按需下载。包含本轮资源窗口排版、四行列表、定位描边清除与滚动条间距修正，以及全量下载缓存管理。\n\n资源：{report['resourceRelease']}\n\n{report['nodeTests']}项Node单元、8项输入合并单元、{report['unityUnitChecks']}项Unity断言、全部角色加载／渲染及下载和房间流程通过。versionCode{CODE}，原包名及证书、双架构、v1/v2、API23独立v1、16KB对齐。附新角色／新皮肤维护指南。安卓真机安装和多手机热点未实测。"))
 js(B/'client-release-state.json',dict(tag=TAG,id=release['id']))
 assert release['target_commitish']==sha;existing={a['name']:a for a in github.api(release['url'])['assets']}
 for row in json.loads((B/'release-assets.json').read_text('utf8')):
  file=R/row['path'];assert file.stat().st_size==row['bytes']and digest(file)==row['sha256'];item=existing.get(row['name'])
  item=upload_client_file(release,row,file,item)
  assert item['state']=='uploaded'and item['size']==row['bytes']and item.get('digest')=='sha256:'+row['sha256'],row['name']
 if release['draft']:release=github.api(release['url'],'PATCH',dict(draft=False,prerelease=True,make_latest='false'))
 final=github.api(release['url']);assert not final['draft']and prepare.git('ls-remote','origin','refs/heads/main').split()[0]==sha
 for name in ['README.md','docs/character-resource-maintenance.md','unity/Assets/Resources/Majsoul/CharacterDirectory/packs-android.json']:
  remote=github.api(github.BASE+'/contents/'+urllib.parse.quote(name)+'?ref='+sha);assert remote['sha']==prepare.git('rev-parse','HEAD:'+name)
 js(B/'published.json',dict(url=final['html_url'],commit=sha,assets=final['assets'],artifactRedownloaded=False));print('RELEASE PUBLISH PASS',final['html_url'],sha,flush=True)
if __name__=='__main__':B.mkdir(parents=True,exist_ok=True);globals()[args.mode]()
