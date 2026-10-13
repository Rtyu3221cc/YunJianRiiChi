"""Package the current Unity release without caches, keys or player state."""
import hashlib
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from importlib import import_module

root = Path(__file__).resolve().parent.parent
dist = root / 'dist'
version = '0.5.38'
apk = dist / f'yunjian-riichi-unity-{version}.apk'
assert apk.exists(), 'Run verify-unity-apk.py first'
signature = import_module('verify-apk-signature').verify_signature(apk)
assert (dist/f'SHA256-unity-{version}.txt').read_text(encoding='utf8').split()[0].lower() == signature['sha256'], 'Final release APK checksum is stale'
compatible_apk = dist / f'yunjian-riichi-unity-{version}-signed.apk'
if compatible_apk.exists():
    assert hashlib.sha256(compatible_apk.read_bytes()).digest() == hashlib.sha256(apk.read_bytes()).digest(), 'Stale compatibility APK alias'
    apk = compatible_apk
apk_checksum = dist / (f'SHA256-unity-{version}.txt' if apk.name == f'yunjian-riichi-unity-{version}.apk' else 'SHA256-' + apk.stem + '.txt')
assert apk_checksum.exists(), 'Missing checksum for the selected APK filename'
assert apk_checksum.read_text(encoding='utf8').split()[0].lower() == signature['sha256'], 'Selected APK checksum is stale'
rollback_apk = dist/'yunjian-riichi-unity-0.5.37-rollback-v39.apk'
rollback_report = import_module('verify-apk-signature').verify_signature(rollback_apk)
assert (dist/'SHA256-rollback-unity-0.5.38.txt').read_text(encoding='utf8').split()[0].lower() == rollback_report['sha256']
for name in ['README.md', 'ARTWORK.md']:
    (dist / name).write_bytes((root / name).read_bytes())
excluded = {'.git', 'Library', 'Temp', 'Obj', 'Logs', 'UserSettings', 'Builds', 'build', '.utmp', 'node_modules', '__pycache__', 'keys'}
source = dist / f'yunjian-riichi-source-{version}.zip'
with ZipFile(source, 'w', ZIP_DEFLATED, compresslevel=9) as archive:
    for folder in ['app', 'scripts', 'tests', 'vendor', 'unity', 'docs']:
        for file in sorted((root / folder).rglob('*')):
            if not file.is_file() or any(p in excluded for p in file.relative_to(root).parts): continue
            if file.suffix in {'.jks', '.log', '.pyc', '.keystore'}: continue
            archive.write(file, 'lan-riichi/' + file.relative_to(root).as_posix())
    for name in ['README.md', 'ARTWORK.md', 'build.ps1', 'desktop.cjs', 'package.json', '.gitignore']:
        archive.write(root / name, 'lan-riichi/' + name)
release = dist / f'yunjian-riichi-unity-release-{version}.zip'
with ZipFile(release, 'w', ZIP_DEFLATED, compresslevel=9) as archive:
    files = [apk, apk_checksum, rollback_apk, dist/'SHA256-rollback-unity-0.5.38.txt', dist/'rollback-unity-0.5.38.json', dist/f'unity-{version}-screenshots.html', dist/f'yunjian-riichi-unity-{version}-screenshots.zip', *(dist / name for name in ['README.md', 'ARTWORK.md', f'验证记录-unity-{version}.txt']), *sorted((dist / 'previews').glob(f'unity-{version}-*'))]
    for file in files:
        assert file.exists(), 'Missing release artifact: ' + str(file)
        archive.write(file, file.relative_to(dist).as_posix())
for package in [source, release]:
    with ZipFile(package) as archive:
        assert archive.testzip() is None
        assert all('Majsoul_4.0.16_MC.apk' not in name and '.desktop-state' not in name and not name.endswith(('.jks','.keystore')) for name in archive.namelist())
        assert all('.utmp' not in Path(name).parts and 'build' not in Path(name).parts for name in archive.namelist())
    print(package.name, package.stat().st_size, hashlib.sha256(package.read_bytes()).hexdigest())
