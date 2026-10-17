"""Publish the gold-default correction. No tests, recordings or artifact downloads."""
import argparse
import importlib.util
import json
from pathlib import Path
import shutil
import urllib.error
import urllib.parse
import urllib.request

ROOT = Path(__file__).resolve().parent.parent


def module(name, file):
    spec = importlib.util.spec_from_file_location(name, ROOT / 'scripts' / file)
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


prepare = module('curation', 'prepare-github-0545.py')
github = module('github', 'github-release-0545.py')
VERSION = '0.5.46'
TAG = 'unity-' + VERSION
DIST = ROOT / 'dist'
BUILD = ROOT / 'build/release-0546'
APK = DIST / ('yunjian-riichi-unity-' + VERSION + '.apk')
HANDOFF = ROOT / ('docs/handoff-unity-' + VERSION + '.md')
CHECKSUM = DIST / ('SHA256-unity-' + VERSION + '.txt')
RECORD = DIST / ('unity-' + VERSION + '-build.json')


def assemble():
    assert APK.is_file() and APK.stat().st_size > 1000000
    signature_log = (BUILD / 'android-build.log').read_text('utf8')
    assert 'PASS: v1 and v2 signatures' in signature_log, 'Build signing did not complete'
    digest = prepare.sha256(APK)
    CHECKSUM.write_text(digest.upper() + '  ' + APK.name + '\n', 'utf8')
    text = HANDOFF.read_text('utf8').replace('__APK_BYTES__', str(APK.stat().st_size)).replace('__APK_SHA256__', digest)
    HANDOFF.write_text(text, 'utf8')
    shutil.copyfile(HANDOFF, DIST / HANDOFF.name)
    record = {'version': VERSION, 'versionCode': 47, 'date': '2026-10-17', 'baseRelease': '0.5.45',
              'change': 'Default invitation QR uses transparent background and gold modules in both room and table entry points.',
              'testsRun': False, 'testsSkippedByExplicitUserRequest': True, 'androidDeviceTested': False,
              'signingHandledByBuild': True, 'apk': {'bytes': APK.stat().st_size, 'sha256': digest}, 'artifactRedownloaded': False}
    RECORD.write_text(json.dumps(record, ensure_ascii=False, indent=2) + '\n', 'utf8')
    print(json.dumps(record, ensure_ascii=False), flush=True)


def push_source():
    assert not prepare.git('status', '--porcelain'), 'Publication clone is dirty'
    prepare.git('fetch', 'origin', 'main')
    prepare.git('merge', '--ff-only', 'origin/main')
    changed = ['README.md', 'unity/Assets/Scripts/LobbyController.Invites.cs',
               'unity/Assets/Editor/ProjectSetup.cs', 'unity/ProjectSettings/ProjectSettings.asset',
               'unity/Assets/Plugins/Android/riichi-host.androidlib/libs/riichi-host.jar',
               'scripts/verify-unity.ps1', 'scripts/verify-unity-apk.py', 'scripts/publish-gold-0546.py']
    rows = prepare.check_files([ROOT / path for path in changed] + [HANDOFF, CHECKSUM, RECORD])
    for row in rows:
        source = ROOT / row['path']
        target = prepare.PUBLISH / row['path']
        assert target.resolve().is_relative_to(prepare.PUBLISH.resolve()) and not target.is_symlink()
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)
        assert prepare.sha256(target) == row['sha256']
    pathspec = BUILD / 'source-pathspec.bin'
    pathspec.write_bytes(b'\0'.join(b':(literal)' + row['path'].encode('utf8') for row in rows) + b'\0')
    prepare.git('add', '-f', '--pathspec-from-file=' + str(pathspec), '--pathspec-file-nul')
    staged = set(prepare.git('diff', '--cached', '--name-only', '-z', binary=True).decode('utf8').strip('\0').split('\0'))
    assert staged <= {row['path'] for row in rows}
    assert 'unity/Assets/Scripts/LobbyController.Invites.cs' in staged
    assert not any(Path(p).suffix in {'.apk', '.jks', '.keystore'} for p in staged)
    print(prepare.git('diff', '--cached', '--shortstat'), flush=True)
    prepare.git('-c', 'user.name=Rtyu3221cc', '-c', 'user.email=55649183+Rtyu3221cc@users.noreply.github.com',
                'commit', '-m', 'Default room invitation QR to transparent gold; release Unity 0.5.46 (tests skipped by request)')
    commit = prepare.git('rev-parse', 'HEAD')
    prepare.git('push', 'origin', 'main')
    assert prepare.git('ls-remote', 'origin', 'refs/heads/main').split()[0] == commit
    (BUILD / 'source-push.json').write_text(json.dumps({'version': VERSION, 'commit': commit, 'sourceFiles': len(rows)}, indent=2) + '\n', 'utf8')
    print('SOURCE PUSH VERIFIED: ' + commit, flush=True)


def release():
    github.authenticate()
    commit = json.loads((BUILD / 'source-push.json').read_text('utf8'))['commit']
    assert prepare.git('ls-remote', 'origin', 'refs/heads/main').split()[0] == commit
    body = '修正默认邀请二维码：准备房间分享／邀请及牌桌星形入口均使用透明底金色二维码，保留原尺寸与反色识别。versionCode 47，沿用原证书。\n\n按用户要求，本次未运行测试；只完成编译和签名。源码已推送 main，最新交接见 handoff-unity-0.5.46.md。没有回下载复核，手机实扫未验证。'
    try:
        value = github.api(github.BASE + '/releases/tags/' + TAG)
    except urllib.error.HTTPError as error:
        if error.code != 404:
            raise
        value = github.api(github.BASE + '/releases', 'POST', {'tag_name': TAG, 'target_commitish': commit,
                           'name': '云间日麻 Unity 0.5.46 透明底金色二维码', 'body': body, 'draft': True, 'prerelease': True})
    assert value['target_commitish'] == commit
    files = [APK, CHECKSUM, RECORD, DIST / HANDOFF.name]
    existing = {item['name']: item for item in value['assets']}
    for path in files:
        size, digest = path.stat().st_size, 'sha256:' + prepare.sha256(path)
        if path.name in existing:
            item = existing[path.name]
        else:
            assert value['draft']
            print('Uploading: ' + path.name, flush=True)
            def chunks():
                with path.open('rb') as stream:
                    while block := stream.read(1024 * 1024):
                        yield block
            headers = dict(github.HEADERS, **{'Content-Type': 'application/octet-stream', 'Content-Length': str(size)})
            request = urllib.request.Request(value['upload_url'].split('{')[0] + '?' + urllib.parse.urlencode({'name': path.name}), data=chunks(), method='POST', headers=headers)
            with urllib.request.urlopen(request, timeout=300) as response:
                item = json.load(response)
        assert item['state'] == 'uploaded' and item['size'] == size and item.get('digest') == digest
    if value['draft']:
        value = github.api(value['url'], 'PATCH', {'draft': False, 'prerelease': True, 'make_latest': 'false'})
    assert not value['draft']
    (BUILD / 'release-published.json').write_text(json.dumps({'url': value['html_url'], 'commit': commit, 'tag': TAG, 'testsRun': False, 'artifactRedownloaded': False}, indent=2) + '\n', 'utf8')
    print('PUBLISHED: ' + value['html_url'], flush=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=['assemble', 'push', 'release'])
    mode = parser.parse_args().mode
    {'assemble': assemble, 'push': push_source, 'release': release}[mode]()
