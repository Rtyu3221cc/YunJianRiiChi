"""Prepare and publish the tested wardrobe release; binaries use Release assets."""
from pathlib import Path
import datetime
import hashlib
import importlib.util
import json
import re
import shutil
import sys
import urllib.error
import urllib.parse
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parent.parent
BUILD = ROOT / 'build/release-0613'
DIST = ROOT / 'dist'
VERSION = '0.6.13'
TAG = 'unity-' + VERSION
REPO = 'Rtyu3221cc/YunJianRiiChi'
APK = DIST / f'yunjian-riichi-unity-{VERSION}.apk'
REPORT = DIST / f'unity-{VERSION}-verification.json'
SHA = DIST / f'SHA256-unity-{VERSION}.txt'
HANDOFF = ROOT / f'docs/handoff-unity-{VERSION}.md'
PREVIEWS = ['2026-10-25-wardrobe-r3', '2026-10-25-wardrobe-r5']


def module(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / 'scripts' / filename)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


prepare = module('prepare0613', 'prepare-github-0545.py')
github = module('github0613', 'github-release-0545.py')


def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', 'utf8')


def read_log(relative):
    path = ROOT / relative
    # The final APK verifier may rewrite the identical distribution copy.
    # Anchor evidence to the signed build, not that copy operation's mtime.
    assert path.stat().st_mtime >= (ROOT / 'unity/Builds' / APK.name).stat().st_mtime, 'Stale test evidence: ' + relative
    raw = path.read_bytes()
    return raw.decode('utf-16' if raw[:2] in (b'\xff\xfe', b'\xfe\xff') else 'utf-8-sig')


def source_paths():
    # Retain previously published role inputs. New optional role media already
    # use the independently versioned character-resource releases.
    prefix = 'unity/Assets/CharacterPacks/Source/'
    tracked = prepare.git('ls-files', '-z', binary=True).decode('utf8').split('\0')
    keys = {n[len(prefix):].split('/')[0].removesuffix('.meta') for n in tracked if n.startswith(prefix)}
    result = []
    for path in prepare.source_paths():
        relative = path.relative_to(ROOT).as_posix()
        if relative.startswith(prefix) and relative[len(prefix):].split('/')[0].removesuffix('.meta') not in keys:
            continue
        result.append(path)
    for name in PREVIEWS:
        result.extend(p for p in (DIST / 'previews' / name).rglob('*') if p.is_file())
    return result + [REPORT, SHA, DIST / HANDOFF.name]


def assemble():
    source = (ROOT / 'unity/Assets/Editor/ProjectSetup.cs').read_text('utf8')
    assert f'ReleaseVersion="{VERSION}"' in source and 'ReleaseCode=61;' in source
    node = read_log('build/github-tests-0.6.13.log')
    assert re.search(r'pass\s+165\b', node) and re.search(r'fail\s+0\b', node)
    py = read_log('build/github-python-tests-0.6.13.log')
    assert re.search(r'Ran 8 tests', py) and re.search(r'\bOK\b', py)
    assert 'CHARACTER REGISTRY PASS' in read_log('build/github-registry-0.6.13.log')
    units = read_log('build/unity-validate.log')
    assert 'RIICHI UNITY VALIDATION PASS' in units
    counts = [int(re.search(pattern, units)[1]) for pattern in [
        r'RELEASE 0545 UNIT PASS:\s*(\d+)', r'RELEASE 063 UNIT PASS:\s*(\d+)',
        r'RELEASE 064 CHARACTER UNIT PASS:\s*(\d+)', r'RELEASE 065 ROLE UI UNIT PASS:\s*(\d+)']]
    assert 'SELECTED UNITY REGRESSION STAGES PASSED' in read_log('build/github-unity-regression-0.6.13.log')
    for stage, marker in [('table', 'RIICHI UNITY PLAY PASS'), ('lobby', 'RIICHI LOBBY 1013 PASS'), ('room', 'LOBBY RELEASE ONLY PASS')]:
        assert marker in read_log('build/release-0542/' + stage + '.log'), 'Runtime stage did not pass: ' + stage
    assert '144 source-data fidelity assertions' in read_log('build/github-wardrobe-fidelity-0.6.13.log')
    gameplay = read_log('build/wardrobe-gameplay-audit/passed.txt')
    gameplay_count = int(re.match(r'(\d+) assertions', gameplay)[1])
    orientation_path = ROOT / 'build/wardrobe-orientation-r5-study/render/capture.json'
    assert orientation_path.stat().st_mtime >= (ROOT / 'unity/Builds' / APK.name).stat().st_mtime
    orientation = json.loads(orientation_path.read_text('utf8'))
    assert orientation['checks'] >= 132 and len(orientation['shots']) == 16
    assert 'PASS: final APK v1 + v2' in read_log('build/github-apk-verify-0.6.13.log')
    signature = module('signature0613', 'verify-apk-signature.py').verify_signature(APK)
    assert signature['sha256'] == 'b076818a12efcfc949e81f832d0eff1db827babcb2e9026f365fae9913e13674'
    report = dict(version=VERSION, versionCode=61,
                  date=datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=9))).date().isoformat(),
                  nodeTests=165, pythonInputTests=8, unityUnitChecks=sum(counts),
                  unityUnitBreakdown=dict(zip(['session', 'calloutAtlas', 'character', 'roleUi'], counts)),
                  sourceEffectFidelityChecks=144, wardrobeGameplayChecks=gameplay_count,
                  orientationChecks=orientation['checks'], calloutViewpoints=16,
                  bubbleMotion=orientation['motion'], runtimePlayPassed=True,
                  runtimeStages=['table', 'lobby', 'room'], androidDeviceTested=False,
                  lobbyChecks=int(re.match(r'(\d+)', read_log('build/release-0542/lobby-passed.txt'))[1]),
                  waitingRoomChecks=int(re.match(r'(\d+)', read_log('build/release-0542/room-passed.txt'))[1]),
                  artifactRedownloaded=False, apk=signature,
                  limitations=['Physical Android installation, touch, audio and multi-phone LAN await device testing.',
                               'Wardrobe resources are bundled; multi-presets, preset naming and wardrobe downloads are not implemented.'])
    write_json(REPORT, report)
    SHA.write_text(signature['sha256'].upper() + '  ' + APK.name + '\n', 'utf8')
    shutil.copyfile(HANDOFF, DIST / HANDOFF.name)
    rows = prepare.check_files(source_paths())
    write_json(BUILD / 'source-manifest.json', rows)
    source_zip = DIST / f'yunjian-riichi-source-{VERSION}.zip'
    with zipfile.ZipFile(source_zip, 'w', zipfile.ZIP_DEFLATED, compresslevel=3) as archive:
        for row in rows:
            archive.write(ROOT / row['path'], row['path'])
        archive.writestr('publication-manifest.json', json.dumps(rows, ensure_ascii=False, indent=2))
    release_zip = DIST / f'yunjian-riichi-unity-release-{VERSION}.zip'
    with zipfile.ZipFile(release_zip, 'w', zipfile.ZIP_DEFLATED, compresslevel=1) as archive:
        for path in [APK, REPORT, SHA, DIST / HANDOFF.name]:
            archive.write(path, path.name)
    with zipfile.ZipFile(source_zip) as archive:
        assert archive.testzip() is None
    with zipfile.ZipFile(release_zip) as archive:
        assert archive.testzip() is None
        payload = archive.read(APK.name)
        assert len(payload) == signature['bytes'] and hashlib.sha256(payload).hexdigest() == signature['sha256']
        inside = BUILD / 'zip-copy-check' / APK.name
        inside.parent.mkdir(parents=True, exist_ok=True)
        inside.write_bytes(payload)
        assert module('zipsignature0613', 'verify-apk-signature.py').verify_signature(inside) == signature
    files = [APK, REPORT, SHA, DIST / HANDOFF.name, source_zip, release_zip]
    assert all(p.stat().st_size <= prepare.RELEASE_LIMIT for p in files), 'Release asset exceeds GitHub size limit'
    write_json(BUILD / 'release-assets.json', [dict(path=p.relative_to(ROOT).as_posix(), name=p.name,
               bytes=p.stat().st_size, sha256=prepare.sha256(p)) for p in files])
    print('RELEASE ASSEMBLY PASS', len(rows), 'source files', signature['bytes'], signature['sha256'], flush=True)


def curate():
    assert prepare.git('branch', '--show-current') == 'main'
    assert prepare.git('remote', 'get-url', 'origin') == 'https://github.com/' + REPO + '.git'
    assert not prepare.git('status', '--porcelain'), 'Publication clone must start clean'
    rows = json.loads((BUILD / 'source-manifest.json').read_text('utf8'))
    for row in rows:
        source, target = ROOT / row['path'], prepare.PUBLISH / row['path']
        assert source.stat().st_size == row['bytes'] and prepare.sha256(source) == row['sha256'], row['path']
        assert target.resolve().is_relative_to(prepare.PUBLISH.resolve()) and not target.is_symlink()
        if not target.is_file() or prepare.sha256(target) != row['sha256']:
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source, target)
    spec = BUILD / 'source-pathspec.bin'
    spec.write_bytes(b'\0'.join(b':(literal)' + r['path'].encode('utf8') for r in rows) + b'\0')
    prepare.git('add', '-f', '--pathspec-from-file=' + str(spec), '--pathspec-file-nul')
    staged = set(filter(None, prepare.git('diff', '--cached', '--name-only', '-z', binary=True).decode('utf8').split('\0')))
    assert staged <= {r['path'] for r in rows}
    assert not any(Path(n).suffix.lower() in {'.apk', '.jks', '.keystore', '.log'} for n in staged)
    write_json(BUILD / 'curation.json', dict(sourceFiles=len(rows), stagedFiles=len(staged), credentialScanPassed=True))
    print(prepare.git('diff', '--cached', '--shortstat'), flush=True)


def commit():
    tests = read_log('build/github-publish-tests-0.6.13.log')
    assert re.search(r'pass\s+165\b', tests) and re.search(r'fail\s+0\b', tests)
    prepare.git('-c', 'user.name=Rtyu3221cc', '-c', 'user.email=55649183+Rtyu3221cc@users.noreply.github.com',
                'commit', '-m', 'Release Unity 0.6.13: wardrobe selections, source effects and four-view callout corrections')
    sha = prepare.git('rev-parse', 'HEAD')
    write_json(BUILD / 'source-commit.json', dict(version=VERSION, commit=sha))
    print('SOURCE COMMIT PASS', sha, flush=True)


def publish():
    github.authenticate()
    repository = github.api(github.BASE)
    assert repository['full_name'] == REPO and repository['permissions']['push']
    sha = json.loads((BUILD / 'source-commit.json').read_text('utf8'))['commit']
    assert prepare.git('rev-parse', 'HEAD') == sha and not prepare.git('status', '--porcelain')
    prepare.git('push', 'origin', 'main')
    assert prepare.git('ls-remote', 'origin', 'refs/heads/main').split()[0] == sha
    try:
        release = github.api(github.BASE + '/releases/tags/' + TAG)
    except urllib.error.HTTPError as error:
        if error.code != 404:
            raise
        # Include drafts when resuming an interrupted upload.
        releases = github.api(github.BASE + '/releases?per_page=100')
        release = next((r for r in releases if r['tag_name'] == TAG), None)
        if release is None:
            body = (ROOT / 'docs/release-notes-unity-0.6.13.md').read_text('utf8')
            release = github.api(github.BASE + '/releases', 'POST', dict(tag_name=TAG, target_commitish=sha,
                name='云间日麻 Unity 0.6.13 · 装扮与牌桌特效', draft=True, prerelease=True, body=body))
    assert release['target_commitish'] == sha
    write_json(BUILD / 'release-state.json', dict(id=release['id'], tag=TAG, commit=sha))
    rows = json.loads((BUILD / 'release-assets.json').read_text('utf8'))
    existing = {a['name']: a for a in github.api(release['url'])['assets']}
    for row in rows:
        path = ROOT / row['path']
        assert path.stat().st_size == row['bytes'] and prepare.sha256(path) == row['sha256']
        item = existing.get(row['name'])
        if item is None:
            assert release['draft'], 'Published assets are immutable'
            print('Uploading', row['name'], row['bytes'], flush=True)
            def chunks():
                with path.open('rb') as stream:
                    while block := stream.read(1048576):
                        yield block
            request = urllib.request.Request(release['upload_url'].split('{')[0] + '?' + urllib.parse.urlencode({'name': row['name']}),
                data=chunks(), method='POST', headers=dict(github.HEADERS, **{'Content-Type': 'application/octet-stream', 'Content-Length': str(row['bytes'])}))
            with urllib.request.urlopen(request, timeout=900) as response:
                item = json.load(response)
        assert item['state'] == 'uploaded' and item['size'] == row['bytes'] and item.get('digest') == 'sha256:' + row['sha256'], row['name']
    if release['draft']:
        github.api(release['url'], 'PATCH', dict(draft=False, prerelease=True, make_latest='false'))
    final = github.api(release['url'])
    assert not final['draft'] and prepare.git('ls-remote', 'origin', 'refs/heads/main').split()[0] == sha
    for name in ['README.md', 'unity/Assets/Scripts/WardrobeCatalog.cs', 'unity/Assets/Scripts/NativeParticleEffect.cs', 'unity/Assets/Resources/Majsoul/Wardrobe/catalog.json']:
        remote = github.api(github.BASE + '/contents/' + urllib.parse.quote(name) + '?ref=' + sha)
        assert remote['sha'] == prepare.git('rev-parse', 'HEAD:' + name)
    indexed = {a['name']: a for a in final['assets']}
    for row in rows:
        item = indexed[row['name']]
        assert item['state'] == 'uploaded' and item['size'] == row['bytes'] and item['digest'] == 'sha256:' + row['sha256']
    write_json(BUILD / 'published.json', dict(url=final['html_url'], commit=sha, assets=final['assets'], artifactRedownloaded=False))
    print('RELEASE PUBLISH PASS', final['html_url'], sha, flush=True)


if __name__ == '__main__':
    BUILD.mkdir(parents=True, exist_ok=True)
    assert len(sys.argv) == 2 and sys.argv[1] in {'assemble', 'curate', 'commit', 'publish'}
    globals()[sys.argv[1]]()
