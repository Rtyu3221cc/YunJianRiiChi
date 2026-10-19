"""Curate and publish the resource-enabled Unity 0.6.6 release."""
from pathlib import Path
import hashlib, importlib.util, json, re, shutil, subprocess, sys, urllib.error, urllib.parse, urllib.request, zipfile
R=Path(__file__).resolve().parent.parent;B=R/'build/release-066';D=R/'dist';VERSION='0.6.6';TAG='unity-'+VERSION
def module(name,file):
    s=importlib.util.spec_from_file_location(name,R/'scripts'/file);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
prepare=module('prepare066','prepare-github-0545.py');github=module('github066','github-release-0545.py')
APK=D/f'yunjian-riichi-unity-{VERSION}.apk';REPORT=D/f'unity-{VERSION}-verification.json';SHA=D/f'SHA256-unity-{VERSION}.txt';HANDOFF=R/f'docs/handoff-unity-{VERSION}.md'
PREVIEW=D/'previews/2026-10-20-character-resources'
def js(path,value):path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n','utf8')
def digest(path):return prepare.sha256(path)
def node_pass(path):
    text=path.read_text('utf8',errors='replace');assert re.search(r'pass\s+146\b',text)and re.search(r'fail\s+0\b',text),str(path)
def paths():return prepare.source_paths()+[p for p in PREVIEW.rglob('*')if p.is_file()]+[REPORT,SHA,D/HANDOFF.name]
def preflight():
    rows=prepare.check_files(prepare.source_paths());js(B/'source-preflight.json',rows)
    print('SOURCE PREFLIGHT PASS',len(rows),'files',sum(r['bytes']for r in rows),'bytes',flush=True)
def assemble():
    node_pass(B/'node-tests.log')
    units=(R/'build/unity-validate.log').read_text('utf8',errors='replace');assert 'RIICHI UNITY VALIDATION PASS'in units
    assert (R/'build/unity-validate.log').stat().st_mtime>(B/'node-tests.log').stat().st_mtime-30
    assert 'PASS: final APK v1 + v2'in(B/'apk-verify.log').read_text('utf8',errors='replace')
    resource=json.loads((R/'build/character-resources-v1/validation/checks.json').read_text('utf8'))
    assert resource['roles']==13 and resource['skins']==79 and resource['assertions']>1500
    bundles=json.loads((R/'build/character-resources-v1/bundle-verification.json').read_text('utf8'));assert len(bundles['bundleChecks'])==26
    flow=json.loads((R/'build/character-pack-experiment/render/checks.json').read_text('utf8'));assert len(flow['checks'])==37
    assert (R/'build/character-pack-experiment/render/checks.json').stat().st_mtime>(B/'node-tests.log').stat().st_mtime
    assert 'CHARACTER PACK EXPERIMENT PASS 37'in(R/'build/character-resources-v1/flow.log').read_text('utf8',errors='replace')
    sig=module('signature066','verify-apk-signature.py').verify_signature(APK)
    badging=subprocess.run([str(R/'tools/sdk/android-15/aapt.exe'),'dump','badging',str(APK)],capture_output=True,text=True,encoding='utf8',errors='replace')
    assert badging.returncode==0 and "name='com.yunjian.riichi.unity'"in badging.stdout and "versionCode='54'"in badging.stdout and "versionName='0.6.6'"in badging.stdout
    (B/'apk-badging.txt').write_text(badging.stdout,'utf8')
    js(REPORT,dict(version=VERSION,versionCode=54,date='2026-10-20',nodeTests=146,nodeFailures=0,legacyUnityUnitChecks=561,
        resourceChecks=resource,bundleChecks=bundles['bundleChecks'],resourceFlowChecks=flow['checks'],androidDeviceTested=False,artifactRedownloaded=False,apk=sig))
    SHA.write_text(sig['sha256'].upper()+'  '+APK.name+'\n','utf8');shutil.copyfile(HANDOFF,D/HANDOFF.name)
    rows=prepare.check_files(paths());js(B/'source-manifest.json',rows)
    source=D/f'yunjian-riichi-source-{VERSION}.zip'
    with zipfile.ZipFile(source,'w',zipfile.ZIP_DEFLATED,compresslevel=3)as archive:
        for row in rows:archive.write(R/row['path'],row['path'])
        archive.writestr('publication-manifest.json',json.dumps(rows,ensure_ascii=False,indent=2))
    release=D/f'yunjian-riichi-unity-release-{VERSION}.zip'
    with zipfile.ZipFile(release,'w',zipfile.ZIP_DEFLATED,compresslevel=1)as archive:
        for p in [APK,REPORT,SHA,D/HANDOFF.name]:archive.write(p,p.name)
    with zipfile.ZipFile(source)as archive:assert archive.testzip()is None
    with zipfile.ZipFile(release)as archive:
        assert archive.testzip()is None;data=archive.read(APK.name);assert len(data)==sig['bytes']and hashlib.sha256(data).hexdigest()==sig['sha256']
        folder=B/'zip-copy-check';folder.mkdir(parents=True,exist_ok=True);inside=folder/APK.name;inside.write_bytes(data)
        assert module('zip066','verify-apk-signature.py').verify_signature(inside)['sha256']==sig['sha256']
    js(B/'release-assets.json',[dict(file=p.name,bytes=p.stat().st_size,sha256=digest(p))for p in [APK,REPORT,SHA,D/HANDOFF.name,source,release]])
    print('RELEASE ASSEMBLY PASS',sig['bytes'],'bytes',sig['sha256'],flush=True)
def curate():
    assert not prepare.git('status','--porcelain'),'Publication clone must start clean'
    assert prepare.git('branch','--show-current')=='main'
    assert prepare.git('remote','get-url','origin')=='https://github.com/Rtyu3221cc/YunJianRiiChi.git'
    rows=prepare.check_files(paths());js(B/'source-manifest.json',rows)
    for row in rows:
        source=R/row['path'];target=prepare.PUBLISH/row['path'];assert target.resolve().is_relative_to(prepare.PUBLISH.resolve())and not target.is_symlink()
        if not target.is_file()or digest(target)!=row['sha256']:target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target)
    tracked=prepare.git('ls-files','-z',binary=True).decode('utf8').strip('\0').split('\0');deletions=[]
    for path in tracked:
        if not path or path.startswith('docs/')or not any(path.startswith(tree+'/')for tree in prepare.SOURCE_TREES):continue
        if not(R/path).exists():
            target=prepare.PUBLISH/path;assert target.resolve().is_relative_to(prepare.PUBLISH.resolve());deletions.append(path)
    spec=B/'source-pathspec.bin';spec.write_bytes(b'\0'.join(b':(literal)'+row['path'].encode('utf8')for row in rows)+b'\0');prepare.git('add','-f','--pathspec-from-file='+str(spec),'--pathspec-file-nul')
    if deletions:
        spec=B/'removed-pathspec.bin';spec.write_bytes(b'\0'.join(b':(literal)'+p.encode('utf8')for p in deletions)+b'\0');prepare.git('rm','--pathspec-from-file='+str(spec),'--pathspec-file-nul')
    staged=set(filter(None,prepare.git('diff','--cached','--name-only','-z',binary=True).decode('utf8').split('\0')))
    assert staged<={r['path']for r in rows}|set(deletions)and not any(Path(p).suffix in ['.apk','.jks','.keystore']for p in staged)
    js(B/'curation.json',dict(allowedFiles=len(rows),stagedFiles=len(staged),deletions=deletions,secretScanPassed=True));print(prepare.git('diff','--cached','--shortstat'),flush=True)
def commit():
    node_pass(B/'publish-node-tests.log')
    prepare.git('-c','user.name=Rtyu3221cc','-c','user.email=55649183+Rtyu3221cc@users.noreply.github.com','commit','-m','Release Unity 0.6.6: downloadable character resources, TOP10 roster and LAN pack synchronization')
    sha=prepare.git('rev-parse','HEAD');js(B/'source-push.json',dict(version=VERSION,commit=sha));print('SOURCE COMMIT PASS',sha,flush=True)
def publish():
    github.authenticate();repo=github.api(github.BASE);assert repo['full_name']=='Rtyu3221cc/YunJianRiiChi'and repo['permissions']['push']
    sha=json.loads((B/'source-push.json').read_text('utf8'))['commit'];prepare.git('push','origin','main');assert prepare.git('ls-remote','origin','refs/heads/main').split()[0]==sha
    try:release=github.api(github.BASE+'/releases/tags/'+TAG)
    except urllib.error.HTTPError as ex:
        if ex.code!=404:raise
        release=github.api(github.BASE+'/releases','POST',dict(tag_name=TAG,target_commitish=sha,name='云间日麻 Unity 0.6.6 · 角色资源按需下载',draft=True,prerelease=True,
            body='女性TOP10＋汪次郎（11名/71套服饰）资源目录与下载管理。仅一姬、二阶堂美树不可删除并内置；其余支持GitHub下载、安装、删除、进度、失败重试。房间可选择GitHub下载、局域网接收或一姬显示替代，资源准备完成后开局。既有未来与寺崎千穗理资源保留。\n\n角色资源：https://github.com/Rtyu3221cc/YunJianRiiChi/releases/tag/character-resources-v1\n\n单元测试、完整资源包检查、动态立绘与下载/LAN流程验证通过，详情见verification.json。versionCode54、原包名及证书、双架构、v1/v2、API23独立v1与16KB对齐。未进行Android真机安装及多手机热点测试。'))
    assert release['target_commitish']==sha
    existing={a['name']:a for a in github.api(release['url'])['assets']}
    for row in json.loads((B/'release-assets.json').read_text('utf8')):
        p=D/row['file'];assert p.stat().st_size==row['bytes']and digest(p)==row['sha256'];item=existing.get(p.name)
        if not item:
            assert release['draft'];print('Uploading release',p.name,row['bytes'],flush=True)
            def chunks():
                with p.open('rb')as stream:
                    while chunk:=stream.read(1048576):yield chunk
            req=urllib.request.Request(release['upload_url'].split('{')[0]+'?'+urllib.parse.urlencode({'name':p.name}),data=chunks(),method='POST',headers=dict(github.HEADERS,**{'Content-Type':'application/octet-stream','Content-Length':str(row['bytes'])}))
            with urllib.request.urlopen(req,timeout=900)as response:item=json.load(response)
        assert item['state']=='uploaded'and item['size']==row['bytes']and item.get('digest')=='sha256:'+row['sha256'],p.name
    if release['draft']:release=github.api(release['url'],'PATCH',dict(draft=False,prerelease=True,make_latest='false'))
    final=github.api(release['url']);assert not final['draft']and prepare.git('ls-remote','origin','refs/heads/main').split()[0]==sha
    for path in ['README.md','unity/Assets/Scripts/CharacterPackService.cs','unity/Assets/Resources/Majsoul/CharacterDirectory/packs-android.json']:
        remote=github.api(github.BASE+'/contents/'+path+'?ref='+sha);assert remote['sha']==prepare.git('rev-parse','HEAD:'+path)
    js(B/'published.json',dict(url=final['html_url'],commit=sha,assets=final['assets'],artifactRedownloaded=False));print('RELEASE PUBLISH PASS',final['html_url'],sha,flush=True)
if __name__=='__main__':
    B.mkdir(parents=True,exist_ok=True);globals()[sys.argv[1]]()
