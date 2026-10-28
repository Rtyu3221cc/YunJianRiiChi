"""Curate and publish Unity 0.6.16; no remote artifact re-downloads."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import hashlib, importlib.util, json, re, shutil, sys, urllib.error, urllib.parse, urllib.request

R=Path(__file__).resolve().parent.parent
B=R/'build/release-0616'
D=R/'dist'
VERSION='0.6.16'
REPO='Rtyu3221cc/YunJianRiiChi'
TAG='unity-'+VERSION

def module(name,file):
    spec=importlib.util.spec_from_file_location(name,R/'scripts'/file)
    value=importlib.util.module_from_spec(spec);spec.loader.exec_module(value);return value
prepare=module('prepare0616','prepare-github-0545.py')
github=module('github0616','github-release-0545.py')
def save(path,value):
    path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n','utf8')
def log(name):
    path=B/name
    assert path.stat().st_mtime>=(R/'unity/Assets/Editor/ProjectSetup.cs').stat().st_mtime,'Stale release check: '+name
    return path.read_text('utf8',errors='replace')

def source_paths():
    prefix='unity/Assets/CharacterPacks/Source/'
    tracked=prepare.git('ls-files','-z',binary=True).decode('utf8').split('\0')
    role_keys={n[len(prefix):].split('/')[0].removesuffix('.meta')for n in tracked if n.startswith(prefix)}
    paths=[]
    for file in prepare.source_paths():
        name=file.relative_to(R).as_posix()
        if name.startswith(prefix)and name[len(prefix):].split('/')[0].removesuffix('.meta')not in role_keys:continue
        paths.append(file)
    # Keep the new review stills, not recordings, reference bundles or build data.
    paths.extend(p for p in (D/'previews/2026-10-28-wardrobe-r15').rglob('*')if p.is_file())
    paths.extend([D/f'unity-{VERSION}-verification.json',D/f'SHA256-unity-{VERSION}.txt',D/f'handoff-unity-{VERSION}.md',D/f'unpreviewed-effects-{VERSION}.json'])
    return paths

def preflight():
    assert prepare.git('remote','get-url','origin')=='https://github.com/'+REPO+'.git'
    assert prepare.git('branch','--show-current')=='main'
    assert not prepare.git('status','--porcelain'),'Publication clone must be clean before curation'
    assert prepare.git('ls-remote','origin','refs/heads/main').split()[0]==prepare.git('rev-parse','HEAD'),'Remote main changed; inspect before curation'
    # Do not require APK-dependent dist files at this read-only inventory stage.
    paths=[p for p in source_paths()if p.exists()]
    rows=prepare.check_files(paths)
    save(B/'source-preflight.json',dict(baseCommit=prepare.git('rev-parse','HEAD'),files=rows,totalBytes=sum(r['bytes']for r in rows)))
    print('SOURCE PREFLIGHT PASS',len(rows),'files',sum(r['bytes']for r in rows),'bytes',flush=True)

def assemble():
    apk=D/f'yunjian-riichi-unity-{VERSION}.apk'
    report_path=D/f'unity-{VERSION}-verification.json'
    report=json.loads(report_path.read_text('utf8'));signature=report['apk']
    assert report['version']==VERSION and report['versionCode']==64
    assert report['nodeTests']==181 and report['pythonTests']==11 and report['unityUnitChecks']==571 and report['wardrobeRuntimeChecks']==58
    assert re.search(r'pass\s+181\b',log('node-tests.log'))and re.search(r'fail\s+0\b',log('node-tests.log'))
    assert 'RIICHI UNITY VALIDATION PASS'in log('unity-unit.log')and 'WARDROBE R15 SCREENSHOTS PASS 58'in log('wardrobe-runtime.log')
    for file,count in [('python-character-tests.log',8),('python-effect-tests.log',3)]:assert f'Ran {count} tests'in log(file)and re.search(r'\bOK\b',log(file))
    assert 'CHARACTER REGISTRY PASS 122 roles 486 skins'in log('registry.log')
    assert 'NATIVE EFFECT APK PASS'in log('apk-verification.log')
    assert apk.stat().st_size==signature['bytes']and prepare.sha256(apk)==signature['sha256']
    handoff=R/f'docs/handoff-unity-{VERSION}.md'
    assert not re.search(r'__(?:APK_BYTES|APK_SHA256|RELEASE_COMMIT)__',handoff.read_text('utf8'))
    shutil.copyfile(handoff,D/handoff.name)
    shutil.copyfile(B/'unpreviewed-candidates.json',D/f'unpreviewed-effects-{VERSION}.json')
    rows=prepare.check_files(source_paths());save(B/'source-manifest.json',rows)
    source_zip=D/f'yunjian-riichi-source-{VERSION}.zip'
    with ZipFile(source_zip,'w',ZIP_DEFLATED,compresslevel=3)as z:
        for row in rows:z.write(R/row['path'],row['path'])
        z.writestr('publication-manifest.json',json.dumps(rows,ensure_ascii=False,indent=2))
    release_zip=D/f'yunjian-riichi-unity-release-{VERSION}.zip'
    release_files=[apk,report_path,D/f'SHA256-unity-{VERSION}.txt',D/handoff.name,D/f'unpreviewed-effects-{VERSION}.json']
    with ZipFile(release_zip,'w',ZIP_DEFLATED,compresslevel=1)as z:
        for path in release_files:z.write(path,path.name)
    with ZipFile(source_zip)as z:assert z.testzip()is None
    with ZipFile(release_zip)as z:
        assert z.testzip()is None;payload=z.read(apk.name)
        assert len(payload)==signature['bytes']and hashlib.sha256(payload).hexdigest()==signature['sha256']
        copy=B/'zip-copy-check'/apk.name;copy.parent.mkdir(parents=True,exist_ok=True);copy.write_bytes(payload)
    assert module('signature0616','verify-apk-signature.py').verify_signature(copy)==signature
    copy.unlink();copy.parent.rmdir()
    files=release_files+[source_zip,release_zip]
    assert all(p.stat().st_size<=prepare.RELEASE_LIMIT for p in files)
    save(B/'release-assets.json',[dict(path=p.relative_to(R).as_posix(),name=p.name,bytes=p.stat().st_size,sha256=prepare.sha256(p))for p in files])
    print('RELEASE ASSEMBLY PASS',len(rows),'source files',signature['bytes'],signature['sha256'],flush=True)

def curate():
    assert not prepare.git('status','--porcelain')
    previous=json.loads((B/'source-preflight.json').read_text('utf8'))
    assert prepare.git('rev-parse','HEAD')==previous['baseCommit']
    assert prepare.git('ls-remote','origin','refs/heads/main').split()[0]==previous['baseCommit']
    rows=json.loads((B/'source-manifest.json').read_text('utf8'))
    for row in rows:
        source,target=R/row['path'],prepare.PUBLISH/row['path']
        assert source.stat().st_size==row['bytes']and prepare.sha256(source)==row['sha256']
        assert target.resolve().is_relative_to(prepare.PUBLISH.resolve())and not target.is_symlink()
        if not target.is_file()or prepare.sha256(target)!=row['sha256']:
            target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target)
    spec=B/'source-pathspec.bin';spec.write_bytes(b'\0'.join(b':(literal)'+r['path'].encode('utf8')for r in rows)+b'\0')
    prepare.git('add','-f','--pathspec-from-file='+str(spec),'--pathspec-file-nul')
    staged=set(filter(None,prepare.git('diff','--cached','--name-only','-z',binary=True).decode('utf8').split('\0')))
    assert staged<={r['path']for r in rows}
    assert not any(Path(n).suffix.lower()in {'.apk','.jks','.keystore','.log'}for n in staged)
    save(B/'curation.json',dict(sourceFiles=len(rows),stagedFiles=len(staged),credentialScanPassed=True))
    print(prepare.git('diff','--cached','--shortstat'),flush=True)

def commit():
    test_log=log('publication-node-tests.log')
    assert re.search(r'pass\s+181\b',test_log)and re.search(r'fail\s+0\b',test_log)
    prepare.git('-c','user.name=Rtyu3221cc','-c','user.email=55649183+Rtyu3221cc@users.noreply.github.com','commit','-m','Release Unity 0.6.16: native outfit effects, settlement frames, wait inspection and background grace')
    sha=prepare.git('rev-parse','HEAD');save(B/'source-commit.json',dict(version=VERSION,commit=sha));print('SOURCE COMMIT PASS',sha,flush=True)

def publish():
    github.authenticate();repo=github.api(github.BASE)
    assert repo['full_name']==REPO and repo['permissions']['push']
    sha=json.loads((B/'source-commit.json').read_text('utf8'))['commit']
    assert prepare.git('rev-parse','HEAD')==sha and not prepare.git('status','--porcelain')
    prepare.git('push','origin','main')
    assert prepare.git('ls-remote','origin','refs/heads/main').split()[0]==sha
    try:release=github.api(github.BASE+'/releases/tags/'+TAG)
    except urllib.error.HTTPError as e:
        if e.code!=404:raise
        release=next((r for r in github.api(github.BASE+'/releases?per_page=100')if r['tag_name']==TAG),None)
        if release is None:
            release=github.api(github.BASE+'/releases','POST',dict(tag_name=TAG,target_commitish=sha,name='云间日麻 Unity 0.6.16 · 装扮与结算修正',draft=True,prerelease=True,body=(R/f'docs/release-notes-unity-{VERSION}.md').read_text('utf8')))
    assert release['target_commitish']==sha
    save(B/'release-state.json',dict(id=release['id'],tag=TAG,commit=sha))
    existing={a['name']:a for a in github.api(release['url'])['assets']}
    rows=json.loads((B/'release-assets.json').read_text('utf8'))
    for row in rows:
        path=R/row['path'];assert path.stat().st_size==row['bytes']and prepare.sha256(path)==row['sha256']
        item=existing.get(row['name'])
        if item is None:
            assert release['draft'],'Published assets are immutable'
            print('Uploading',row['name'],row['bytes'],flush=True)
            def chunks():
                with path.open('rb')as stream:
                    while block:=stream.read(1048576):yield block
            req=urllib.request.Request(release['upload_url'].split('{')[0]+'?'+urllib.parse.urlencode({'name':row['name']}),data=chunks(),method='POST',headers=dict(github.HEADERS,**{'Content-Type':'application/octet-stream','Content-Length':str(row['bytes'])}))
            with urllib.request.urlopen(req,timeout=900)as response:item=json.load(response)
        assert item['state']=='uploaded'and item['size']==row['bytes']and item.get('digest')=='sha256:'+row['sha256'],row['name']
    if release['draft']:github.api(release['url'],'PATCH',dict(draft=False,prerelease=True,make_latest='false'))
    final=github.api(release['url']);assert not final['draft']and prepare.git('ls-remote','origin','refs/heads/main').split()[0]==sha
    indexed={a['name']:a for a in final['assets']}
    for row in rows:
        item=indexed[row['name']];assert item['state']=='uploaded'and item['size']==row['bytes']and item['digest']=='sha256:'+row['sha256']
    for name in ['README.md','unity/Assets/Scripts/RiichiClient.cs','app/src/main/assets/engine.js',f'docs/handoff-unity-{VERSION}.md']:
        remote=github.api(github.BASE+'/contents/'+urllib.parse.quote(name)+'?ref='+sha)
        assert remote['sha']==prepare.git('rev-parse','HEAD:'+name)
    save(B/'published.json',dict(url=final['html_url'],commit=sha,assets=final['assets'],artifactRedownloaded=False))
    print('RELEASE PUBLISH PASS',final['html_url'],sha,flush=True)

if __name__=='__main__':
    assert len(sys.argv)==2 and sys.argv[1]in {'preflight','assemble','curate','commit','publish'}
    B.mkdir(parents=True,exist_ok=True);globals()[sys.argv[1]]()
