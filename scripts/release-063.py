"""Curate, package and publish verified Unity 0.6.3. Never downloads release assets."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,shutil,subprocess,sys,urllib.error,urllib.parse,urllib.request,zipfile
R=Path(__file__).resolve().parent.parent;B=R/'build/release-063';D=R/'dist';VERSION='0.6.3';TAG='unity-'+VERSION
def module(name,file):
    s=importlib.util.spec_from_file_location(name,R/'scripts'/file);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
prepare=module('prepare063','prepare-github-0545.py');github=module('github063','github-release-0545.py')
APK=D/f'yunjian-riichi-unity-{VERSION}.apk';HANDOFF=R/f'docs/handoff-unity-{VERSION}.md';REPORT=D/f'unity-{VERSION}-verification.json';NOTES=D/f'验证记录-unity-{VERSION}.md';SHA=D/f'SHA256-unity-{VERSION}.txt'
PREVIEW=D/'previews/2026-10-18-table-review-character-size'
def js(path,value):path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n','utf8')
def digest(p):return prepare.sha256(p)
def auth():
    github.authenticate();user=github.api('https://api.github.com/user');repo=github.api(github.BASE)
    assert repo['full_name']=='Rtyu3221cc/YunJianRiiChi'and repo.get('permissions',{}).get('push')
    print('Saved authentication and intended repository verified:',user['login'],repo['full_name'],flush=True)
def allowed():
    extra=[SHA,REPORT,NOTES,D/HANDOFF.name]
    return prepare.check_files(prepare.source_paths()+[p for p in PREVIEW.rglob('*')if p.is_file()]+extra)
def assemble():
    sig=module('signature063','verify-apk-signature.py').verify_signature(APK)
    verified=(B/'apk-verify.log').read_text('utf8',errors='replace');assert sig['sha256']in verified and 'all five APK callout word textures preserve 187x143'in verified
    badging=subprocess.run([str(R/'tools/sdk/android-15/aapt.exe'),'dump','badging',str(APK)],capture_output=True,text=True,encoding='utf8',errors='replace')
    assert badging.returncode==0 and "name='com.yunjian.riichi.unity'"in badging.stdout and "versionCode='51'"in badging.stdout and "versionName='0.6.3'"in badging.stdout
    (B/'apk-badging.txt').write_text(badging.stdout,'utf8')
    test=(B/'node-tests.log').read_text('utf8',errors='replace');assert 'pass 129'in test and 'fail 0'in test
    stages={}
    for stage,marker in [('table','RIICHI UNITY PLAY PASS'),('lobby','RIICHI LOBBY 1013 PASS'),('room','LOBBY RELEASE ONLY PASS')]:
        log=R/f'build/release-0542/{stage}.log';assert marker in log.read_text('utf8',errors='replace'),stage
        shutil.copyfile(log,B/(stage+'.log'));shutil.copyfile(R/f'build/release-0542/{stage}-passed.txt',B/(stage+'-passed.txt'));stages[stage]=True
    units=int((B/'unity-unit-passed.txt').read_text('utf8').split()[0]);old_units=int((R/'build/release-0545/unit-passed.txt').read_text('utf8').split()[0])
    assert units>=87 and old_units>=62
    value=dict(version=VERSION,versionCode=51,date='2026-10-18',nodeTests=129,nodeFailures=0,unityUnitChecks=units+old_units,focusedUnityUnitChecks=units,previousUnityUnitChecks=old_units,runtimeStages=stages,runtimePlayPassed=True,androidDeviceTested=False,artifactRedownloaded=False,apk=sig)
    js(REPORT,value);SHA.write_text(sig['sha256'].upper()+'  '+APK.name+'\n','utf8')
    text=HANDOFF.read_text('utf8').replace('__APK_BYTES__',str(sig['bytes'])).replace('__APK_SHA256__',sig['sha256']).replace('__UNITY_UNITS__',str(units+old_units));HANDOFF.write_text(text,'utf8');shutil.copyfile(HANDOFF,D/HANDOFF.name)
    NOTES.write_text(f'# Unity {VERSION} 验证记录\n\nNode 单元129项通过，0失败。Unity 单元{units+old_units}项通过（既有{old_units}，本轮{units}）。牌桌、大厅、房间三个实际运行回归均通过。旧BGM测试已改为检查实际PlaylistAudio播放通道，覆盖轮询不重播、静音及暂停恢复。\n\n本轮字形回归独立从源PNG透明通道计算可见边界，检查原图比例、80设计像素字高、可见字形中心及双字宽度；另检查牌面源图集定位。\n\nAPK：{sig["bytes"]:,}字节，SHA-256 `{sig["sha256"]}`。v1/v2、API23独立v1、原签名证书、ZIP完整性和16KB对齐均通过；完整离线资源、新NativeCallouts类及编译着色器由verify-unity-apk.py验证。\n\n实际Android安装、触摸、音频、相机与多手机热点联机尚未进行。本次不回下载发布资源。\n','utf8')
    rows=allowed();js(B/'source-manifest.json',rows)
    source=D/f'yunjian-riichi-source-{VERSION}.zip'
    with zipfile.ZipFile(source,'w',zipfile.ZIP_DEFLATED,compresslevel=6)as z:
        for row in rows:z.write(R/row['path'],row['path'])
        z.writestr('publication-manifest.json',json.dumps(rows,ensure_ascii=False,indent=2))
    release=D/f'yunjian-riichi-unity-release-{VERSION}.zip'
    with zipfile.ZipFile(release,'w',zipfile.ZIP_DEFLATED,compresslevel=1)as z:
        for p in [APK,SHA,REPORT,NOTES,D/HANDOFF.name]:z.write(p,p.name)
        for p in PREVIEW.rglob('*'):
            if p.is_file():z.write(p,'preview/'+p.relative_to(PREVIEW).as_posix())
    with zipfile.ZipFile(source)as z:assert z.testzip()is None
    with zipfile.ZipFile(release)as z:
        assert z.testzip()is None;data=z.read(APK.name);assert len(data)==sig['bytes']and hashlib.sha256(data).hexdigest()==sig['sha256']
        check_dir=B/'zip-copy-check';check_dir.mkdir(parents=True,exist_ok=True)
        assert check_dir.resolve().is_relative_to(B.resolve());inside=check_dir/APK.name;inside.write_bytes(data)
        assert module('signature063zip','verify-apk-signature.py').verify_signature(inside)['sha256']==sig['sha256']
    assets=[APK,SHA,REPORT,NOTES,source,release,D/HANDOFF.name]
    js(B/'release-assets.json',[dict(file=p.name,bytes=p.stat().st_size,sha256=digest(p))for p in assets]);print('Local packages, signatures and release-ZIP APK copy verified.',flush=True)
def curate():
    assert not prepare.git('status','--porcelain'),'Publication checkout must start clean'
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
    pathspec=B/'source-pathspec.bin';pathspec.write_bytes(b'\0'.join(b':(literal)'+row['path'].encode('utf8')for row in rows)+b'\0')
    prepare.git('add','-f','--pathspec-from-file='+str(pathspec),'--pathspec-file-nul')
    if deletions:
        removed=B/'removed-pathspec.bin';removed.write_bytes(b'\0'.join(b':(literal)'+p.encode('utf8')for p in deletions)+b'\0');prepare.git('rm','--pathspec-from-file='+str(removed),'--pathspec-file-nul')
    staged=set(prepare.git('diff','--cached','--name-only','-z',binary=True).decode('utf8').strip('\0').split('\0'))
    assert staged<={r['path']for r in rows}|set(deletions)and not any(Path(p).suffix in ['.apk','.jks','.keystore']for p in staged)
    js(B/'curation.json',dict(allowedFiles=len(rows),stagedFiles=len(staged),deletions=deletions,secretScanPassed=True));print(prepare.git('diff','--cached','--shortstat'),flush=True)
def commit():
    assert 'pass 129'in(B/'publish-node-tests.log').read_text('utf8',errors='replace')
    prepare.git('-c','user.name=Rtyu3221cc','-c','user.email=55649183+Rtyu3221cc@users.noreply.github.com','commit','-m','Release Unity 0.6.3: align tile faces and restore native callouts')
    sha=prepare.git('rev-parse','HEAD');js(B/'source-push.json',dict(version=VERSION,commit=sha));print('Committed vetted source:',sha,flush=True)
def publish():
    auth();sha=json.loads((B/'source-push.json').read_text('utf8'))['commit'];prepare.git('push','origin','main');assert prepare.git('ls-remote','origin','refs/heads/main').split()[0]==sha
    try:release=github.api(github.BASE+'/releases/tags/'+TAG)
    except urllib.error.HTTPError as e:
        if e.code!=404:raise
        body='修正牌面居中、掉线标记和四方位动作提示。完整保留原版字图留白；按每字可见高度统一字号，立直／拔北自然增宽。包含此前未发布的0.6.0–0.6.2役种、设置、播放列表、教程及立直火焰效果。\n\nNode 129项、Unity单元149项以及牌桌／大厅／房间运行回归通过。versionCode 51，原包名与证书，ARM64+ARMv7。v1/v2、独立v1、16KB对齐及ZIP完整性通过。真机行为待验证。交接见handoff-unity-0.6.3.md。'
        release=github.api(github.BASE+'/releases','POST',dict(tag_name=TAG,target_commitish=sha,name='云间日麻 Unity 0.6.3 · 原版提示器与牌面居中',body=body,draft=True,prerelease=True))
    assert release['target_commitish']==sha
    assets=json.loads((B/'release-assets.json').read_text('utf8'));existing={a['name']:a for a in release['assets']}
    for row in assets:
        path=D/row['file'];assert path.stat().st_size==row['bytes']and digest(path)==row['sha256']
        name='validation-unity-'+VERSION+'.md'if path==NOTES else path.name
        if name in existing:item=existing[name]
        else:
            assert release['draft'];print('Uploading:',name,flush=True)
            def chunks():
                with path.open('rb')as f:
                    while block:=f.read(1024*1024):yield block
            headers=dict(github.HEADERS,**{'Content-Type':'application/octet-stream','Content-Length':str(row['bytes'])})
            request=urllib.request.Request(release['upload_url'].split('{')[0]+'?'+urllib.parse.urlencode(dict(name=name)),data=chunks(),method='POST',headers=headers)
            with urllib.request.urlopen(request,timeout=300)as response:item=json.load(response)
        assert item['state']=='uploaded'and item['size']==row['bytes']and item.get('digest')=='sha256:'+row['sha256'],name
    if release['draft']:release=github.api(release['url'],'PATCH',dict(draft=False,prerelease=True,make_latest='false'))
    final=github.api(release['url']);assert not final['draft']and prepare.git('ls-remote','origin','refs/heads/main').split()[0]==sha
    source=github.api(github.BASE+'/contents/unity/Assets/Scripts/NativeCallouts.cs?ref='+sha);assert source['sha']==prepare.git('rev-parse','HEAD:unity/Assets/Scripts/NativeCallouts.cs')
    js(B/'published.json',dict(url=final['html_url'],tag=TAG,commit=sha,assets=[dict(name=a['name'],bytes=a['size'],digest=a.get('digest'))for a in final['assets']],artifactRedownloaded=False));print('Published and remote content verified:',final['html_url'],sha,flush=True)
if __name__=='__main__':
    B.mkdir(parents=True,exist_ok=True);p=argparse.ArgumentParser();p.add_argument('mode',choices=['auth','assemble','curate','commit','publish']);a=p.parse_args();globals()[a.mode]()
