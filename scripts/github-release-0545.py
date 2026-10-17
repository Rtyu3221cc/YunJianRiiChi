"""Explicit GitHub Release operations for verified Unity 0.5.45 artifacts.

Modes: auth, draft COMMIT, upload, publish, verify. No artifact download mode.
Only an executed mutation mode writes GitHub. Credentials are read at execution,
never embedded or printed. This file does not commit or push source code.
"""
import argparse
import hashlib
from importlib import import_module
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from zipfile import ZipFile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('prepare0545', ROOT / 'scripts/prepare-github-0545.py')
prepare = importlib.util.module_from_spec(spec)
spec.loader.exec_module(prepare)
REPO = prepare.REPO
BASE = 'https://api.github.com/repos/' + REPO
TAG = 'unity-0.5.45'
STATE = ROOT / 'build/github-release-0545.json'
PACKAGE = ROOT / 'build/release-package-0545.json'
HEADERS = {'User-Agent': 'YunJianRiichi-0545-release', 'Accept': 'application/vnd.github+json',
           'X-GitHub-Api-Version': '2022-11-28'}
ASSETS = [
    ('dist/yunjian-riichi-unity-0.5.45.apk', 'yunjian-riichi-unity-0.5.45.apk'),
    ('dist/SHA256-unity-0.5.45.txt', 'SHA256-unity-0.5.45.txt'),
    ('dist/unity-0.5.45-verification.json', 'unity-0.5.45-verification.json'),
    ('dist/验证记录-unity-0.5.45.md', 'validation-unity-0.5.45.md'),
    ('dist/yunjian-riichi-source-0.5.45.zip', 'yunjian-riichi-source-0.5.45.zip'),
    ('dist/yunjian-riichi-unity-release-0.5.45.zip', 'yunjian-riichi-unity-release-0.5.45.zip'),
    ('dist/handoff-unity-0.5.45.md', 'handoff-unity-0.5.45.md'),
]


def api(url, method='GET', value=None):
    data = None if value is None else json.dumps(value, ensure_ascii=False).encode('utf8')
    with urllib.request.urlopen(urllib.request.Request(url, data=data, method=method, headers=HEADERS), timeout=60) as response:
        return json.load(response)


def authenticate():
    result = subprocess.run([os.environ.get('RIICHI_GIT', 'git'), '-c', 'http.sslBackend=openssl',
                             '-c', 'credential.interactive=false', 'credential', 'fill'],
                            cwd=prepare.PUBLISH, input='protocol=https\nhost=github.com\n\n',
                            text=True, capture_output=True, env=prepare.git_environment())
    values = dict(line.split('=', 1) for line in result.stdout.splitlines() if '=' in line)
    assert result.returncode == 0 and values.get('password'), 'Saved GitHub credential unavailable; execute with access to the existing credential store'
    HEADERS['Authorization'] = 'Bearer ' + values['password']


def local_assets():
    signature, report = prepare.verify_artifacts()
    package = json.loads(PACKAGE.read_text(encoding='utf8'))
    assert package['version'] == prepare.VERSION
    assert package['apk']['sha256'] == signature['sha256'] and package['releaseZipApk']['sha256'] == signature['sha256']
    indexed = {row['file']: row for row in package['assets']}
    rows = []
    for source, name in ASSETS:
        path = ROOT / source
        assert path.is_file() and path.stat().st_size <= prepare.RELEASE_LIMIT
        digest = prepare.sha256(path)
        assert path.name in indexed and indexed[path.name]['bytes'] == path.stat().st_size and indexed[path.name]['sha256'] == digest, 'Repackage changed release asset: ' + path.name
        rows.append({'path': path, 'name': name, 'bytes': path.stat().st_size, 'sha256': digest})
    assert (prepare.DIST / 'handoff-unity-0.5.45.md').read_bytes() == prepare.HANDOFF.read_bytes(), 'Stale handoff Release copy'
    for path in (ROOT / 'README.md', prepare.HANDOFF):
        assert not re.search(r'__(?:RELEASE_COMMIT|APK_BYTES|APK_SHA256|UNITY_CHECKS)__', path.read_text(encoding='utf8')), 'Unfilled publication field in ' + path.name
    return rows


def published_assets(release, rows):
    existing = {item['name']: item for item in release['assets']}
    for row in rows:
        assert row['name'] in existing, 'Release asset absent: ' + row['name']
        item = existing[row['name']]
        assert item['state'] == 'uploaded' and item['size'] == row['bytes'], 'Release size/state mismatch: ' + row['name']
        assert item.get('digest') == 'sha256:' + row['sha256'], 'Release SHA256 mismatch: ' + row['name']
    return existing


def get_release():
    state = json.loads(STATE.read_text(encoding='utf8'))
    release = api(BASE + '/releases/' + str(state['id']))
    assert release['tag_name'] == TAG and release['target_commitish'] == state['commit'], 'Release identity differs from intended commit'
    assert release['prerelease'], 'Expected an explicit trial/prerelease release'
    return state, release


def assert_remote_commit(commit):
    remote = prepare.git('ls-remote', 'origin', 'refs/heads/main').split()
    assert remote and remote[0] == commit, 'Remote main does not equal intended release commit'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=['auth', 'draft', 'upload', 'publish', 'verify'])
    parser.add_argument('commit', nargs='?')
    args = parser.parse_args()
    authenticate()
    if args.mode == 'auth':
        user = api('https://api.github.com/user')
        metadata = api(BASE)
        assert metadata['full_name'] == REPO and metadata.get('permissions', {}).get('push'), 'Repository is not writable'
        print('Saved GitHub authentication and destination verified:', user['login'], REPO)
        try:
            release = api(BASE + '/releases/tags/' + TAG)
            print('Existing target Release:', TAG, 'draft=' + str(release['draft']))
        except urllib.error.HTTPError as error:
            if error.code != 404:
                raise
            print('Target release tag is available:', TAG)
        return
    rows = local_assets()
    if args.mode == 'draft':
        commit = args.commit or ''
        assert re.fullmatch(r'[0-9a-f]{40}', commit), 'Pass a complete commit SHA'
        assert_remote_commit(commit)
        body = (ROOT / 'build/publish-release-body-0545.md').read_text(encoding='utf8')
        try:
            release = api(BASE + '/releases/tags/' + TAG)
        except urllib.error.HTTPError as error:
            if error.code != 404:
                raise
            release = api(BASE + '/releases', 'POST', {'tag_name': TAG, 'target_commitish': commit,
                'name': '云间日麻 Unity 0.5.45 二维码、役满与接管更新', 'body': body, 'draft': True, 'prerelease': True})
        assert release['draft'] and release['target_commitish'] == commit and release['prerelease'], 'Existing target release is not this draft'
        STATE.write_text(json.dumps({'id': release['id'], 'commit': commit, 'tag': TAG}, indent=2) + '\n', encoding='utf8')
        print('Draft ready:', release['id'], TAG, commit)
        return
    state, release = get_release()
    if args.mode == 'upload':
        assert release['draft'], 'Uploads target the draft only'
        existing = {item['name']: item for item in release['assets']}
        upload = release['upload_url'].split('{', 1)[0]
        assert upload.startswith('https://uploads.github.com/repos/' + REPO + '/releases/')
        for row in rows:
            if row['name'] in existing:
                item = existing[row['name']]
                if item['state'] == 'uploaded' and item['size'] == row['bytes'] and item.get('digest') == 'sha256:' + row['sha256']:
                    print('Existing upload verified:', row['name'], flush=True)
                    continue
                assert item['state'] == 'starter' and item['size'] == 0 and not item.get('digest'), 'Conflicting draft asset; do not overwrite: ' + row['name']
                with urllib.request.urlopen(urllib.request.Request(item['url'], method='DELETE', headers=HEADERS), timeout=60) as response:
                    assert response.status == 204
            assert prepare.sha256(row['path']) == row['sha256'], 'Local artifact changed before upload'
            print('Uploading:', row['name'], row['bytes'], 'bytes', flush=True)
            def chunks(path=row['path']):
                with path.open('rb') as source:
                    while block := source.read(1024 * 1024):
                        yield block
            headers = dict(HEADERS, **{'Content-Type': 'application/octet-stream', 'Content-Length': str(row['bytes'])})
            request = urllib.request.Request(upload + '?' + urllib.parse.urlencode({'name': row['name']}),
                                             data=chunks(), method='POST', headers=headers)
            with urllib.request.urlopen(request, timeout=300) as response:
                item = json.load(response)
            assert item['state'] == 'uploaded' and item['size'] == row['bytes'] and item.get('digest') == 'sha256:' + row['sha256'], 'Uploaded digest mismatch'
        print('All draft uploads verified.')
    elif args.mode == 'publish':
        assert release['draft']
        assert_remote_commit(state['commit'])
        published_assets(release, rows)
        release = api(release['url'], 'PATCH', {'draft': False, 'prerelease': True, 'make_latest': 'false'})
        assert not release['draft'] and release['prerelease']
        print('Published:', release['html_url'])
    elif args.mode == 'verify':
        assert not release['draft']
        assert_remote_commit(state['commit'])
        items = published_assets(release, rows)
        print(json.dumps({'url': release['html_url'], 'tag': TAG, 'commit': state['commit'],
                          'assets': [{'name': row['name'], 'bytes': items[row['name']]['size'],
                                      'digest': items[row['name']]['digest'], 'url': items[row['name']]['browser_download_url']} for row in rows]}, ensure_ascii=False, indent=2))


if __name__ == '__main__':
    try:
        main()
    except urllib.error.HTTPError as error:
        print('GitHub API HTTP error:', error.code, file=sys.stderr)
        sys.exit(1)
    except Exception as error:
        print(type(error).__name__ + ': ' + str(error), file=sys.stderr)
        sys.exit(1)
