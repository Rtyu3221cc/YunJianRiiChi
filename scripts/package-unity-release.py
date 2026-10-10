"""Package the current Unity release without caches, keys or player state."""
import hashlib
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED

root = Path(__file__).resolve().parent.parent
dist = root / 'dist'
version = '0.5.13'
apk = dist / f'yunjian-riichi-unity-{version}.apk'
assert apk.exists(), 'Run verify-unity-apk.py first'
for name in ['README.md', 'ARTWORK.md']:
    (dist / name).write_bytes((root / name).read_bytes())
excluded = {'.git', 'Library', 'Temp', 'Obj', 'Logs', 'UserSettings', 'Builds', 'node_modules', '__pycache__', 'keys'}
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
    files = [apk, *(dist / name for name in ['README.md', 'ARTWORK.md', f'SHA256-unity-{version}.txt', f'验证记录-unity-{version}.txt']), *sorted((dist / 'previews').glob(f'unity-{version}-*'))]
    for file in files:
        assert file.exists(), 'Missing release artifact: ' + str(file)
        archive.write(file, file.relative_to(dist).as_posix())
for package in [source, release]:
    with ZipFile(package) as archive:
        assert archive.testzip() is None
        assert all('Majsoul_4.0.16_MC.apk' not in name and '.desktop-state' not in name and not name.endswith(('.jks','.keystore')) for name in archive.namelist())
    print(package.name, package.stat().st_size, hashlib.sha256(package.read_bytes()).hexdigest())
