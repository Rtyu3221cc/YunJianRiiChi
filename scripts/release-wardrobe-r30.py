"""Validate, stage and publish the 0.6.21 wardrobe test release in explicit phases."""
from concurrent.futures import ThreadPoolExecutor
import importlib.util
import json
from pathlib import Path
import re
import shutil
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from zipfile import ZipFile, ZIP_STORED

ROOT = Path(__file__).resolve().parent.parent
WORK = ROOT/'build/wardrobe-r30'
DIST = ROOT/'dist'
VERSION = '0.6.21'
APP_TAG = 'unity-'+VERSION
PACK_TAG = 'wardrobe-resources-v2'
APK = DIST/f'yunjian-riichi-unity-{VERSION}.apk'
VERIFY = DIST/f'unity-{VERSION}-verification.json'
VIDEO = DIST/f'yunjian-riichi-wardrobe-{VERSION}.mp4'
ZIP = DIST/f'yunjian-riichi-unity-release-{VERSION}.zip'
COVERAGE = ROOT/f'docs/wardrobe-coverage-{VERSION}.json'
NOTES = ROOT/f'docs/handoff-unity-{VERSION}.md'


def load_module(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT/'scripts'/filename)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


github = load_module('release_api', 'github-release-0545.py')
source = github.prepare


def read(path):
    return json.loads(path.read_text('utf8'))


def save(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2)+'\n', encoding='utf8')


def pack_rows():
    rows = []
    for platform in ('android', 'windows'):
        directory = ROOT/f'unity/Assets/Resources/Majsoul/WardrobeDirectory/packs-{platform}.json'
        catalog = read(directory)
        assert catalog['platform'] == platform
        for entry in catalog['packs']:
            if not entry['key'].startswith('deco-r30-'):
                continue
            path = WORK/'packs'/platform/entry['file']
            assert entry['url'] == f'https://github.com/{source.REPO}/releases/download/{PACK_TAG}/{path.name}'
            assert path.stat().st_size == entry['bytes'] and source.sha256(path) == entry['sha256']
            rows.append(path)
    assert rows and len({p.name for p in rows}) == len(rows)
    return rows


def bundle_release():
    report = read(VERIFY)
    assert report['version'] == VERSION and report['apk']['sha256'] == source.sha256(APK)
    assert all(report['apk'][key] for key in ('v1','v2','independentV1','alignment16KB'))
    assert VIDEO.stat().st_size > 1024*1024 and NOTES.is_file() and COVERAGE.is_file()
    with ZipFile(ZIP,'w',ZIP_STORED) as out:
        for path in (APK, VERIFY, DIST/f'SHA256-unity-{VERSION}.txt', NOTES, COVERAGE):
            out.write(path, path.name)
    with ZipFile(ZIP) as zipped:
        assert zipped.testzip() is None
        copy = WORK/'zip-check'/APK.name
        copy.parent.mkdir(exist_ok=True)
        with zipped.open(APK.name) as src, copy.open('wb') as dst:
            shutil.copyfileobj(src, dst)
        assert copy.stat().st_size == APK.stat().st_size and source.sha256(copy) == report['apk']['sha256']
    signature = load_module('check_apk', 'verify-apk-signature.py').verify_signature(copy)
    assert signature == report['apk']
    copy.unlink()
    report['releaseZipApkMatches'] = True
    save(WORK/'release-verified.json', report)
    print('LOCAL RELEASE VERIFIED', report['newWardrobeItems'], report['apk']['sha256'], flush=True)


def prepare_source():
    assert source.git('remote','get-url','origin') == f'https://github.com/{source.REPO}.git'
    assert source.git('branch','--show-current') == 'main' and not source.git('status','--porcelain')
    base = source.git('rev-parse','HEAD')
    assert source.git('ls-remote','origin','refs/heads/main').split()[0] == base
    assert read(WORK/'release-verified.json')['version'] == VERSION
    paths = []
    # Runtime and checks from the working checkout, including the improvements since 0.6.19.
    for directory in ('unity/Assets/Scripts','unity/Assets/Editor','unity/Assets/Resources',
                      'unity/Assets/WardrobePacks/Source/Majsoul/NativeReview/WardrobeR30', 'tests'):
        paths.extend(p for p in (ROOT/directory).rglob('*') if p.is_file())
    paths.extend(ROOT/path for path in (
        'README.md','unity/ProjectSettings/ProjectSettings.asset',
        'unity/Assets/Plugins/Android/riichi-host.androidlib/assets/engine.js',
        'app/src/main/assets/engine.js','scripts/import-native-effects-r11.py',
        'scripts/verify-phone-test-r29.py','scripts/release-wardrobe-r30.py'))
    paths.extend((COVERAGE, NOTES))
    physical = ROOT/'unity/Assets/WardrobePacks/Source/Majsoul/NativeReview/WardrobeR30.meta'
    if physical.exists(): paths.append(physical)
    paths = [p for p in paths if not set(p.relative_to(ROOT).parts).intersection(source.EXCLUDED_PARTS)
             and p.suffix.lower() not in source.EXCLUDED_SUFFIXES]
    def differs(path):
        other=source.PUBLISH/path.relative_to(ROOT)
        return not other.is_file() or path.stat().st_size!=other.stat().st_size or source.sha256(path)!=source.sha256(other)
    with ThreadPoolExecutor(max_workers=8) as pool:
        candidates=[p for p, changed in zip(paths,pool.map(differs,paths)) if changed]
    rows=source.check_files(candidates)
    for row in rows:
        dst=source.PUBLISH/row['path']
        assert dst.resolve().is_relative_to(source.PUBLISH.resolve()) and not dst.is_symlink()
        dst.parent.mkdir(parents=True,exist_ok=True)
        shutil.copy2(ROOT/row['path'],dst)
    pathspec=WORK/'publish-paths.bin'
    pathspec.write_bytes(b'\0'.join(b':(literal)'+r['path'].encode('utf8') for r in rows)+b'\0')
    source.git('add','-f','--pathspec-from-file='+str(pathspec),'--pathspec-file-nul')
    staged=set(source.git('diff','--cached','--name-only','-z',binary=True).decode('utf8').rstrip('\0').split('\0'))
    assert staged == {r['path'] for r in rows}
    assert not any(Path(p).suffix.lower() in {'.apk','.majset','.jks','.keystore','.zip','.log'} for p in staged)
    save(WORK/'publish-manifest.json',{'base':base,'files':rows})
    print('SOURCE STAGED',len(rows),sum(r['bytes'] for r in rows),'bytes',flush=True)


def commit_source():
    manifest=read(WORK/'publish-manifest.json')
    assert source.git('rev-parse','HEAD')==manifest['base']
    node=Path.home()/'AppData/Local/Programs/node/node.exe'
    tests=sorted(str(p.relative_to(source.PUBLISH)) for p in (source.PUBLISH/'tests').glob('*.test.cjs'))
    with (WORK/'published-source-tests.log').open('w',encoding='utf8') as out:
        subprocess.run([str(node),'--test','--test-isolation=none',*tests],cwd=source.PUBLISH,stdout=out,stderr=subprocess.STDOUT,check=True)
    log=(WORK/'published-source-tests.log').read_text('utf8')
    assert re.search(r'pass\s+192\b',log) and re.search(r'fail\s+0\b',log)
    source.git('-c','user.name=Rtyu3221cc','-c','user.email=55649183+Rtyu3221cc@users.noreply.github.com',
               'commit','-m','Release Unity 0.6.21 with source-backed wardrobe expansion')
    commit=source.git('rev-parse','HEAD')
    (WORK/'release-commit.txt').write_text(commit+'\n','utf8')
    print('SOURCE COMMIT VERIFIED',commit,flush=True)


def auth():
    github.authenticate()
    assert github.api(github.BASE)['permissions']['push']


def get_draft(tag, title, body, target):
    releases=github.api(github.BASE+'/releases?per_page=100')
    existing=next((r for r in releases if r['tag_name']==tag),None)
    if existing: return existing
    return github.api(github.BASE+'/releases','POST',dict(tag_name=tag,target_commitish=target,name=title,
                     body=body,draft=True,prerelease=True))


def assets(release):
    current={}
    for page in range(1,10):
        batch=github.api(release['url']+f'/assets?per_page=100&page={page}')
        current.update({x['name']:x for x in batch})
        if len(batch)<100: return current
    raise RuntimeError('Unexpected release asset count')


def upload(release, files):
    current=assets(release)
    def one(path):
        size,digest=path.stat().st_size,'sha256:'+source.sha256(path)
        def verify(item):
            return item and item['state']=='uploaded' and item['size']==size and item.get('digest')==digest
        item=current.get(path.name)
        if item:
            assert verify(item),path.name
            return
        assert release['draft'], 'Published releases are immutable'
        for attempt in range(5):
            def blocks():
                with path.open('rb') as f:
                    while data:=f.read(1024*1024):yield data
            url=release['upload_url'].split('{')[0]+'?'+urllib.parse.urlencode({'name':path.name})
            headers=dict(github.HEADERS,**{'Content-Type':'application/octet-stream','Content-Length':str(size)})
            try:
                with urllib.request.urlopen(urllib.request.Request(url,data=blocks(),headers=headers,method='POST'),timeout=300) as response:
                    item=json.load(response)
            except urllib.error.URLError:
                if attempt==4: raise
                time.sleep(2**attempt)
                item=assets(release).get(path.name)
                if item and item['state']!='uploaded':github.api(item['url'],'DELETE');item=None
            if item:
                assert verify(item),path.name
                return
        raise RuntimeError('Upload not confirmed: '+path.name)
    with ThreadPoolExecutor(max_workers=3) as pool:
        for i,_ in enumerate(pool.map(one,files),1):
            if i%20==0 or i==len(files):print('VERIFIED UPLOAD',release['tag_name'],i,'/',len(files),flush=True)


def draft_packs():
    files=pack_rows()
    auth()
    rel=get_draft(PACK_TAG,'云间日麻 · 装扮资源 v2','Unity 0.6.21 新增装扮的安卓和 Windows 分件资源。','main')
    upload(rel,files)
    save(WORK/'resource-draft.json',{'id':rel['id'],'assets':len(files)})


def publish():
    commit=(WORK/'release-commit.txt').read_text('utf8').strip()
    assert source.git('rev-parse','HEAD')==commit and not source.git('status','--porcelain')
    assert read(WORK/'release-verified.json')['apk']['sha256']==source.sha256(APK)
    assert read(WORK/'resource-draft.json')['assets']==len(pack_rows())
    auth()
    resource=github.api(github.BASE+'/releases/'+str(read(WORK/'resource-draft.json')['id']))
    assert resource['tag_name']==PACK_TAG
    upload(resource,pack_rows())
    # The draft can only target an object GitHub already has. Pin it to the
    # tested commit when that commit is present on the remote main branch.
    rel=get_draft(APP_TAG,'云间日麻 Unity '+VERSION+' 装扮补齐测试版',NOTES.read_text('utf8'),'main')
    upload(rel,[APK,ZIP,VIDEO,VERIFY,DIST/f'SHA256-unity-{VERSION}.txt',COVERAGE])
    remote=source.git('ls-remote','origin','refs/heads/main').split()[0]
    if remote!=commit:
        assert remote==read(WORK/'publish-manifest.json')['base']
        source.git('push','origin','main')
    assert source.git('ls-remote','origin','refs/heads/main').split()[0]==commit
    if resource['draft']:resource=github.api(resource['url'],'PATCH',dict(target_commitish=commit,draft=False,prerelease=True,make_latest='false'))
    if rel['draft']:rel=github.api(rel['url'],'PATCH',dict(target_commitish=commit,draft=False,prerelease=True,make_latest='false'))
    assert not resource['draft'] and not rel['draft']
    verified=github.api(github.BASE+'/contents/docs/'+COVERAGE.name+'?ref='+commit)
    assert verified['sha']==source.git('rev-parse','HEAD:docs/'+COVERAGE.name)
    save(WORK/'published.json',dict(commit=commit,release=rel['html_url'],resources=resource['html_url'],artifactRedownloaded=False))
    print('PUBLISHED',rel['html_url'],commit,flush=True)


if __name__=='__main__':
    {'package':bundle_release,'prepare':prepare_source,'commit':commit_source,'draft-packs':draft_packs,'publish':publish}[sys.argv[1]]()
