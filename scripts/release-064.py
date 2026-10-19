"""Publish the verified 0.6.4 source and artifacts; never re-download uploads."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,re,shutil,subprocess,urllib.error,urllib.parse,urllib.request,zipfile
R=Path(__file__).resolve().parent.parent;B=R/'build/release-064';D=R/'dist';VERSION='0.6.4';TAG='unity-'+VERSION
def module(name,file):
    spec=importlib.util.spec_from_file_location(name,R/'scripts'/file);result=importlib.util.module_from_spec(spec);spec.loader.exec_module(result);return result
prepare=module('prepare064','prepare-github-0545.py');github=module('github064','github-release-0545.py')
APK=D/f'yunjian-riichi-unity-{VERSION}.apk';HANDOFF=R/f'docs/handoff-unity-{VERSION}.md';REPORT=D/f'unity-{VERSION}-verification.json';NOTES=D/f'验证记录-unity-{VERSION}.md';SHA=D/f'SHA256-unity-{VERSION}.txt'
PREVIEWS=[D/'previews/2026-10-18-character-system',D/'previews/2026-10-19-character-system-r2']
def js(path,value):path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n','utf8')
def digest(path):return prepare.sha256(path)
def auth():
    github.authenticate();user=github.api('https://api.github.com/user');repo=github.api(github.BASE)
    assert repo['full_name']=='Rtyu3221cc/YunJianRiiChi'and repo.get('permissions',{}).get('push')
    print('Saved authentication and destination verified:',user['login'],repo['full_name'],flush=True)
def allowed():
    extra=[SHA,REPORT,NOTES,D/HANDOFF.name]
    cache={r['path']:r for r in json.loads((B/'source-preflight.json').read_text('utf8'))}if(B/'source-preflight.json').exists()else{}
    rows=[]
    for path in sorted(set(prepare.source_paths()+[p for folder in PREVIEWS for p in folder.rglob('*')if p.is_file()]+extra)):
        relative=prepare.safe_source(path);assert path.stat().st_size<=prepare.GIT_LIMIT
        known=cache.get(relative)
        # Re-hash every file; reuse only a credential scan for identical bytes.
        rows.append(known if known and known['sha256']==digest(path)else prepare.check_files([path])[0])
    return rows
def node_pass(path):
    text=path.read_text('utf8',errors='replace');assert re.search(r'pass\s+129\b',text)and re.search(r'fail\s+0\b',text)
def preflight():
    rows=prepare.check_files(prepare.source_paths()+[p for folder in PREVIEWS for p in folder.rglob('*')if p.is_file()]);js(B/'source-preflight.json',rows)
    print('Source preflight:',len(rows),'files,',sum(r['bytes']for r in rows),'bytes; credential scan passed; largest',max(r['bytes']for r in rows),'bytes',flush=True)
def assemble():
    node_pass(B/'node-tests.log');assert 'checks passed: 5'in(B/'character-protocol.log').read_text('utf8')
    sig=module('signature064','verify-apk-signature.py').verify_signature(APK)
    verified=(B/'apk-verify.log').read_text('utf8',errors='replace');assert sig['sha256']in verified and '626 original voice clips'in verified
    badging=subprocess.run([str(R/'tools/sdk/android-15/aapt.exe'),'dump','badging',str(APK)],capture_output=True,text=True,encoding='utf8',errors='replace')
    assert badging.returncode==0 and "name='com.yunjian.riichi.unity'"in badging.stdout and "versionCode='52'"in badging.stdout and "versionName='0.6.4'"in badging.stdout
    (B/'apk-badging.txt').write_text(badging.stdout,'utf8')
    stages={}
    for stage,marker in [('table','RIICHI UNITY PLAY PASS'),('lobby','RIICHI LOBBY 1013 PASS'),('room','LOBBY RELEASE ONLY PASS')]:
        log=R/f'build/release-0542/{stage}.log';assert marker in log.read_text('utf8',errors='replace'),stage
        shutil.copyfile(log,B/(stage+'.log'));shutil.copyfile(R/f'build/release-0542/{stage}-passed.txt',B/(stage+'-passed.txt'));stages[stage]=True
    counts={k:int((B/(k+'-unit-passed.txt')).read_text('utf8').split()[0])for k in ['character','callout','legacy']};assert counts==dict(character=375,callout=87,legacy=62)
    assert 'RIICHI UNITY VALIDATION PASS'in(R/'build/unity-validate.log').read_text('utf8',errors='replace')
    shutil.copyfile(R/'build/unity-validate.log',B/'unity-validation-full.log')
    ui=json.loads((B/'character-play.json').read_text('utf8'));assert len(ui['checks'])==7
    assert 'CHARACTER REVIEW R2 COMPLETE errors=0'in(B/'character-play.log').read_text('utf8',errors='replace')
    value=dict(version=VERSION,versionCode=52,date='2026-10-19',nodeTests=129,nodeFailures=0,characterProtocolGroups=5,unityUnitChecks=sum(counts.values()),unitBreakdown=counts,runtimeStages=stages,characterUiChecks=ui['checks'],androidDeviceTested=False,artifactRedownloaded=False,apk=sig)
    js(REPORT,value);SHA.write_text(sig['sha256'].upper()+'  '+APK.name+'\n','utf8')
    text=HANDOFF.read_text('utf8').replace('__APK_BYTES__',str(sig['bytes'])).replace('__APK_SHA256__',sig['sha256']).replace('__UNITY_UNITS__',str(sum(counts.values())));HANDOFF.write_text(text,'utf8');shutil.copyfile(HANDOFF,D/HANDOFF.name)
    NOTES.write_text(f'# Unity {VERSION} 验证记录\n\n本轮 Node 单元129项、角色公共状态集成5组、Unity单元524项全部通过。牌桌、大厅、房间实际Unity运行回归及角色界面7项交互检查通过。\n\nAPK：{sig["bytes"]:,}字节，SHA-256 `{sig["sha256"]}`。ZIP完整性、v1/v2、API23独立v1、原签名证书和16KB对齐通过；双架构、手机房间服务、626段语音、全部离线角色资源、骨骼字节与角色shader均已检查。发布ZIP里的APK另行验签并核对大小/哈希。\n\n真机安装、触摸、声音、扫码与多手机热点联机尚未实测。不重复下载远端发布资产。\n','utf8')
    rows=allowed();js(B/'source-manifest.json',rows)
    source=D/f'yunjian-riichi-source-{VERSION}.zip'
    with zipfile.ZipFile(source,'w',zipfile.ZIP_DEFLATED,compresslevel=6)as archive:
        for row in rows:archive.write(R/row['path'],row['path'])
        archive.writestr('publication-manifest.json',json.dumps(rows,ensure_ascii=False,indent=2))
    release=D/f'yunjian-riichi-unity-release-{VERSION}.zip'
    with zipfile.ZipFile(release,'w',zipfile.ZIP_DEFLATED,compresslevel=1)as archive:
        for p in [APK,SHA,REPORT,NOTES,D/HANDOFF.name]:archive.write(p,p.name)
        for p in PREVIEWS[-1].rglob('*'):
            if p.is_file():archive.write(p,'preview/'+p.relative_to(PREVIEWS[-1]).as_posix())
    with zipfile.ZipFile(source)as archive:assert archive.testzip()is None
    with zipfile.ZipFile(release)as archive:
        assert archive.testzip()is None;data=archive.read(APK.name);assert len(data)==sig['bytes']and hashlib.sha256(data).hexdigest()==sig['sha256']
        folder=B/'zip-copy-check';folder.mkdir(parents=True,exist_ok=True);assert folder.resolve().is_relative_to(B.resolve());inside=folder/APK.name;inside.write_bytes(data)
        assert module('signature064zip','verify-apk-signature.py').verify_signature(inside)['sha256']==sig['sha256']
    assets=[APK,SHA,REPORT,NOTES,source,release,D/HANDOFF.name];assert all(p.stat().st_size<=prepare.RELEASE_LIMIT for p in assets);js(B/'release-assets.json',[dict(file=p.name,bytes=p.stat().st_size,sha256=digest(p))for p in assets])
    print('Local packages, signatures, source scan and release-ZIP APK verified.',flush=True)
def curate():
    assert not prepare.git('status','--porcelain'),'Publication checkout must start clean'
    assert prepare.git('branch','--show-current')=='main'
    assert prepare.git('remote','get-url','origin')=='https://github.com/Rtyu3221cc/YunJianRiiChi.git'
    rows=allowed();js(B/'source-manifest.json',rows)
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
        removed=B/'removed-pathspec.bin';removed.write_bytes(b'\0'.join(b':(literal)'+p.encode('utf8')for p in deletions)+b'\0');prepare.git('rm','--pathspec-from-file='+str(removed),'--pathspec-file-nul')
    staged=set(filter(None,prepare.git('diff','--cached','--name-only','-z',binary=True).decode('utf8').split('\0')))
    assert staged<={r['path']for r in rows}|set(deletions)and not any(Path(p).suffix in ['.apk','.jks','.keystore']for p in staged)
    js(B/'curation.json',dict(allowedFiles=len(rows),stagedFiles=len(staged),deletions=deletions,secretScanPassed=True));print(prepare.git('diff','--cached','--shortstat'),flush=True)
def commit():
    node_pass(B/'publish-node-tests.log');assert 'checks passed: 5'in(B/'publish-character-protocol.log').read_text('utf8')
    prepare.git('-c','user.name=Rtyu3221cc','-c','user.email=55649183+Rtyu3221cc@users.noreply.github.com','commit','-m','Release Unity 0.6.4: add four-character roster, outfits, voices and native framing')
    sha=prepare.git('rev-parse','HEAD');js(B/'source-push.json',dict(version=VERSION,commit=sha));print('Committed vetted source:',sha,flush=True)
def publish():
    auth();sha=json.loads((B/'source-push.json').read_text('utf8'))['commit'];prepare.git('push','origin','main');assert prepare.git('ls-remote','origin','refs/heads/main').split()[0]==sha
    try:release=github.api(github.BASE+'/releases/tags/'+TAG)
    except urllib.error.HTTPError as error:
        if error.code!=404:raise
        body='四雀士角色系统：21套服饰、626段原版语音、71张聊天表情、2套原始动态皮肤。宿舍档案/换装/鉴赏/星标排序，大厅立绘设置与整场牌局固定的随机出战；原版参数构图与非牌桌樱花。\n\nNode129项、角色公共状态5组、Unity单元524项、牌桌/大厅/房间运行回归及7项界面交互通过。versionCode52，保留原包名、证书和双架构；v1/v2、独立v1、16KB对齐和ZIP完整性通过。出战切换面板及牌桌其他布局问题待后续；真机行为未实测。详见交接文档。'
        release=github.api(github.BASE+'/releases','POST',dict(tag_name=TAG,target_commitish=sha,name='云间日麻 Unity 0.6.4 · 四雀士角色系统',body=body,draft=True,prerelease=True))
    assert release['target_commitish']==sha
    existing={a['name']:a for a in release['assets']}
    for row in json.loads((B/'release-assets.json').read_text('utf8')):
        path=D/row['file'];assert path.stat().st_size==row['bytes']and digest(path)==row['sha256'];name='validation-unity-'+VERSION+'.md'if path==NOTES else path.name
        if name in existing:item=existing[name]
        else:
            assert release['draft'];print('Uploading:',name,row['bytes'],'bytes',flush=True)
            def chunks():
                with path.open('rb')as stream:
                    while block:=stream.read(1024*1024):yield block
            headers=dict(github.HEADERS,**{'Content-Type':'application/octet-stream','Content-Length':str(row['bytes'])})
            request=urllib.request.Request(release['upload_url'].split('{')[0]+'?'+urllib.parse.urlencode(dict(name=name)),data=chunks(),method='POST',headers=headers)
            with urllib.request.urlopen(request,timeout=300)as response:item=json.load(response)
        assert item['state']=='uploaded'and item['size']==row['bytes']and item.get('digest')=='sha256:'+row['sha256'],name
    if release['draft']:release=github.api(release['url'],'PATCH',dict(draft=False,prerelease=True,make_latest='false'))
    final=github.api(release['url']);assert not final['draft']and prepare.git('ls-remote','origin','refs/heads/main').split()[0]==sha
    for path in ['README.md','unity/Assets/Scripts/CharacterFraming.cs','unity/Assets/Scripts/CharacterRoster.cs']:
        source=github.api(github.BASE+'/contents/'+path+'?ref='+sha);assert source['sha']==prepare.git('rev-parse','HEAD:'+path)
    js(B/'published.json',dict(url=final['html_url'],tag=TAG,commit=sha,assets=[dict(name=a['name'],bytes=a['size'],digest=a.get('digest'))for a in final['assets']],artifactRedownloaded=False));print('Published and remote contents verified:',final['html_url'],sha,flush=True)
if __name__=='__main__':
    B.mkdir(parents=True,exist_ok=True);parser=argparse.ArgumentParser();parser.add_argument('mode',choices=['auth','preflight','assemble','curate','commit','publish']);globals()[parser.parse_args().mode]()
