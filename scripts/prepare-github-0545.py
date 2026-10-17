"""Curate the 0.5.45 publication clone; never commit, push or call GitHub.

Run without --apply for a preflight/manifest only. --apply copies the allowlist;
--stage also stages exactly that list. Historical publication files are retained.
"""
import argparse
import hashlib
from importlib import import_module
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parent.parent
PUBLISH = ROOT / '.publish/YunJianRiiChi'
VERSION = '0.5.45'
CODE = 46
REPO = 'Rtyu3221cc/YunJianRiiChi'
BUILD = ROOT / 'build'
DIST = ROOT / 'dist'
APK = DIST / f'yunjian-riichi-unity-{VERSION}.apk'
CHECKSUM = DIST / f'SHA256-unity-{VERSION}.txt'
VERIFICATION = DIST / f'unity-{VERSION}-verification.json'
NOTES = DIST / f'验证记录-unity-{VERSION}.md'
HANDOFF = ROOT / f'docs/handoff-unity-{VERSION}.md'
PREVIEW = DIST / 'previews/unity-0.5.45'
COMMIT_MESSAGE = 'Add room QR joining, yakuman cues and idle takeover; fix table and settlement presentation; release Unity 0.5.45'
GIT_LIMIT = 95 * 1024 * 1024
RELEASE_LIMIT = 2 * 1024 * 1024 * 1024
TOP_FILES = ('unity/.gitignore', '.gitignore', 'ARTWORK.md', 'README.md', 'build.ps1', 'desktop.cjs', 'package.json')
SOURCE_TREES = ('app/src/main', 'scripts', 'tests', 'vendor', 'unity/Assets',
                'unity/Android', 'unity/Packages', 'unity/ProjectSettings', 'docs')
EXCLUDED_PARTS = {'.git', '.publish', 'tools', 'Library', 'Temp', 'Obj', 'Logs',
                  'UserSettings', 'Builds', 'build', '.utmp', 'node_modules', '__pycache__', 'keys',
                  '.cache', 'Artifacts', 'TestResults', '__GeneratedPreview', '__PlayValidation',
                  '__Temp', '__Preview', '__Validation'}
EXCLUDED_SUFFIXES = {'.jks', '.keystore', '.log', '.pyc', '.p12', '.pfx', '.pem', '.key'}
SECRET_PATTERNS = [
    re.compile(rb'-----BEGIN (?:RSA |EC |DSA |OPENSSH )?PRIVATE KEY-----'),
    re.compile(rb'\bgh[pousr]_[A-Za-z0-9]{30,}\b'),
    re.compile(rb'\bgithub_pat_[A-Za-z0-9_]{40,}\b'),
    re.compile(rb'\bAKIA[A-Z0-9]{16}\b'),
    re.compile(rb'https?://[^\s/:]{1,80}:[^\s/@]{3,160}@github\.com', re.I),
]


def sha256(path):
    digest = hashlib.sha256()
    with Path(path).open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(block)
    return digest.hexdigest()


def git_environment():
    env = dict(os.environ, GIT_TERMINAL_PROMPT='0', GCM_INTERACTIVE='Never')
    bundled = Path.home() / '.cache/codex-runtimes/codex-primary-runtime/dependencies/native/git/mingw64/bin'
    if bundled.is_dir():
        env['GIT_EXEC_PATH'] = str(bundled)
    return env


def git(*args, cwd=PUBLISH, binary=False):
    command = [os.environ.get('RIICHI_GIT', 'git'), '-c', 'http.sslBackend=openssl', *args]
    completed = subprocess.run(command, cwd=cwd, env=git_environment(), capture_output=True)
    if completed.returncode:
        # Git publication operations never receive secrets as arguments.
        raise RuntimeError('Git operation failed: ' + ' '.join(args[:2]) + '\n' + completed.stderr.decode('utf8', errors='replace')[:1500])
    return completed.stdout if binary else completed.stdout.decode('utf8', errors='replace').strip()


def safe_source(path):
    path = Path(path)
    relative = path.relative_to(ROOT)
    assert path.is_file() and not path.is_symlink(), 'Only regular source files: ' + str(relative)
    assert path.resolve().is_relative_to(ROOT.resolve()), 'Source escapes workspace: ' + str(relative)
    assert not set(relative.parts).intersection(EXCLUDED_PARTS), 'Excluded directory: ' + str(relative)
    assert path.suffix.lower() not in EXCLUDED_SUFFIXES, 'Private/generated file: ' + str(relative)
    assert not (path.name == '.env' or path.name.startswith('.env.') or path.name.lower() in {'credentials.json', 'secrets.json', 'local.properties'}), 'Local/private configuration: ' + str(relative)
    assert not path.name.startswith('.desktop-state'), 'Runtime state cannot be published'
    assert path.name != 'Majsoul_4.0.16_MC.apk' and path.suffix.lower() != '.apk', 'APKs use Release assets only'
    assert not re.match(r'(?:room-state-|host-state-|saved-session)', path.name, re.I), 'Runtime room state: ' + str(relative)
    return relative.as_posix()


def check_files(paths, limit=GIT_LIMIT):
    rows = []
    for path in sorted(set(map(Path, paths))):
        relative = safe_source(path)
        size = path.stat().st_size
        assert size <= limit, f'File exceeds publication limit ({limit} bytes): {relative}'
        # Scan each allowed file, including any embedded plain-text credential.
        payload = path.read_bytes()
        if any(pattern.search(payload) for pattern in SECRET_PATTERNS):
            raise RuntimeError('Credential-like content found; inspect locally without printing it: ' + relative)
        rows.append({'path': relative, 'bytes': size, 'sha256': hashlib.sha256(payload).hexdigest()})
    return rows


def source_paths():
    paths = [ROOT / item for item in TOP_FILES]
    for directory in SOURCE_TREES:
        base = ROOT / directory
        assert base.is_dir(), 'Missing source tree: ' + directory
        for file in base.rglob('*'):
            if not file.is_file():
                continue
            relative = file.relative_to(ROOT)
            if set(relative.parts).intersection(EXCLUDED_PARTS) or file.suffix.lower() in EXCLUDED_SUFFIXES:
                continue
            if file.name.startswith('.desktop-state') or re.match(r'(?:room-state-|host-state-|saved-session)', file.name, re.I):
                continue
            paths.append(file)
    for name in ('package-lock.json',):
        if (ROOT / name).is_file():
            paths.append(ROOT / name)
    return paths


def preview_paths():
    assert (PREVIEW / 'index.html').is_file(), 'Missing complete preview index'
    paths = [p for p in PREVIEW.rglob('*') if p.is_file()]
    assert any(p.suffix.lower() == '.png' for p in paths), 'No preview PNGs'
    assert len([p for p in paths if p.suffix.lower() == '.mp4']) == 4, 'Preview must contain exactly four MP4s'
    allowed = {'.png', '.jpg', '.jpeg', '.webp', '.mp4', '.html', '.css', '.js', '.json', '.txt', '.md', '.svg'}
    assert all(p.suffix.lower() in allowed for p in paths), 'Unexpected file in preview folder'
    return paths


def verify_artifacts():
    for required in (APK, CHECKSUM, VERIFICATION, NOTES, HANDOFF, PREVIEW / 'index.html'):
        assert required.is_file(), 'Release artifact not produced yet: ' + required.relative_to(ROOT).as_posix()
    sys.path.insert(0, str(ROOT / 'scripts'))
    signature = import_module('verify-apk-signature').verify_signature(APK)
    words = CHECKSUM.read_text(encoding='utf-8-sig').strip().split()
    assert len(words) == 2 and words[0].lower() == signature['sha256'] and words[1] == APK.name, 'Canonical APK checksum is stale or names another APK'
    report = json.loads(VERIFICATION.read_text(encoding='utf8'))
    assert report.get('version') == VERSION and int(report.get('versionCode', 0)) == CODE, 'Verification version mismatch'
    assert int(report.get('nodeTests', 0)) >= 129, 'Expected at least 118 passing Node checks in verification'
    assert int(report.get('unityUnitChecks', 0)) >= 62 and int(report.get('unityChecks', 0)) > 0, 'Unity verification count missing'
    assert report.get('runtimePlayPassed') is True, 'Complete runtime Play regression has not passed'
    declared = report.get('apk', {})
    assert int(declared.get('bytes', -1)) == signature['bytes'] and declared.get('sha256', '').lower() == signature['sha256'], 'Verification APK size/hash mismatch'
    assert NOTES.is_file() and HANDOFF.is_file(), 'Missing verification notes or handoff'
    # A commit is not knowable until publication; root fills that final field.
    for file in (ROOT / 'README.md', NOTES, HANDOFF):
        text = file.read_text(encoding='utf8')
        for token in ('__APK_BYTES__', '__APK_SHA256__', '__UNITY_CHECKS__'):
            assert token not in text, 'Unfilled release field in ' + file.relative_to(ROOT).as_posix()
    preview_paths()
    return signature, report


def publication_rows():
    return check_files(source_paths() + [CHECKSUM, VERIFICATION, NOTES] + preview_paths())


def release_body(signature, report):
    return f"""云间日麻 Unity {VERSION}，versionCode {CODE}。包含本轮大厅、二维码邀请/相机与相册扫码、役满提示与原音效、整庄模式、听牌交互、两分接管/离线遮罩、立直遮挡和响应时序、全 AI 终局与结算声音修正。

- 源码（Unity、Android 房主服务、引擎、资源、测试、脚本及文档）已提交到 main，同时提供源码 ZIP。
- {report['nodeTests']} 项 Node 单元测试、{report['unityUnitChecks']} 项 Unity 单元断言，以及自摸/结算、大厅、房间和完整 Play 回归通过。
- 结算数字变化时滴声，第一位出现仅响一次，终局无音效。
- APK {signature['bytes']} 字节；SHA256 `{signature['sha256']}`。
- 原包名及签名证书；ARM64/ARMv7；v1/v2、API23 独立 v1、ZIP 完整性和 16KB 对齐通过。
- 新对话请先阅读附件 `handoff-unity-{VERSION}.md`。预览沿用已生成画面，本轮未重新录制。
- Android 真机安装、触摸、摄像头/相册、声音、后台服务及多手机局域网尚未实测。
- 发布后仅核对 GitHub 提交和上传文件元数据，不回下载复核。
"""


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply', action='store_true', help='Copy the validated allowlist to the publication clone')
    parser.add_argument('--stage', action='store_true', help='Stage exactly the allowlist; requires --apply')
    parser.add_argument('--refresh', action='store_true', help='Permit dirty paths only if all are in the previous prepared allowlist')
    args = parser.parse_args()
    assert not args.stage or args.apply, '--stage requires --apply'
    assert (PUBLISH / '.git').exists(), 'Publication clone missing'
    assert git('branch', '--show-current') == 'main', 'Publication clone must be on main'
    assert git('remote', 'get-url', 'origin').rstrip('/') == 'https://github.com/' + REPO + '.git', 'Unexpected publication destination'
    signature, report = verify_artifacts()
    rows = publication_rows()
    manifest = {'version': VERSION, 'versionCode': CODE, 'destination': str(PUBLISH),
                'baseCommit': git('rev-parse', 'HEAD'), 'apk': signature, 'files': rows,
                'totalBytes': sum(row['bytes'] for row in rows), 'commitMessage': COMMIT_MESSAGE}
    manifest_path = BUILD / 'publish-manifest-0545.json'
    if args.apply:
        dirty = git('status', '--porcelain', '-z', binary=True)
        if dirty:
            assert args.refresh and manifest_path.is_file(), 'Publication clone is dirty; inspect before --apply'
            previous = json.loads(manifest_path.read_text(encoding='utf8'))
            permitted = {row['path'] for row in previous['files']}
            dirty_paths = []
            for entry in dirty.decode('utf8').split('\0'):
                if not entry:
                    continue
                assert entry[:2].strip() not in ('R', 'C'), 'Rename/copy in publication clone requires manual inspection'
                dirty_paths.append(entry[3:])
            assert set(dirty_paths) <= permitted, 'Dirty paths outside the previous allowlist; inspect manually'
        for row in rows:
            source = ROOT / row['path']
            target = PUBLISH / row['path']
            assert target.resolve().is_relative_to(PUBLISH.resolve()), 'Publication path escapes clone'
            assert not target.is_symlink(), 'Refuse to overwrite a publication symlink'
            assert source.stat().st_size == row['bytes'] and sha256(source) == row['sha256'], 'Source changed since preflight: ' + row['path']
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source, target)
            assert sha256(target) == row['sha256'], 'Copy verification failed: ' + row['path']
        if args.stage:
            pathspec = BUILD / 'publish-pathspec-0545.bin'
            pathspec.write_bytes(b'\0'.join(b':(literal)' + row['path'].encode('utf8') for row in rows) + b'\0')
            git('add', '-f', '--pathspec-from-file=' + str(pathspec), '--pathspec-file-nul')
            staged = git('diff', '--cached', '--name-only', '-z', binary=True).decode('utf8').split('\0')
            assert {name for name in staged if name} <= {row['path'] for row in rows}, 'Unexpected pre-existing staged file'
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf8')
    (BUILD / 'publish-commit-message-0545.txt').write_text(COMMIT_MESSAGE + '\n', encoding='utf8')
    (BUILD / 'publish-release-body-0545.md').write_text(release_body(signature, report), encoding='utf8')
    print(json.dumps({'mode': 'copied-and-staged' if args.stage else 'copied' if args.apply else 'preflight-only',
                      'files': len(rows), 'bytes': manifest['totalBytes'], 'baseCommit': manifest['baseCommit'],
                      'manifest': str(manifest_path), 'commitMessage': COMMIT_MESSAGE}, ensure_ascii=False, indent=2))
    if args.apply:
        print(git('diff', '--cached' if args.stage else '--stat', *(['--stat'] if args.stage else [])))


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        print(type(error).__name__ + ': ' + str(error), file=sys.stderr)
        sys.exit(1)
