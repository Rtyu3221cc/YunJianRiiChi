"""Verify the phone host and offline assets inside the finished Unity APK."""
import hashlib
import re
from pathlib import Path
from zipfile import ZipFile

root = Path(__file__).resolve().parent.parent
apk = root / 'unity/Builds/yunjian-riichi-unity-0.5.2.apk'
assets = root / 'app/src/main/assets'
with ZipFile(apk) as archive:
    assert archive.testzip() is None
    names = archive.namelist()
    for file in assets.rglob('*'):
        if file.is_file():
            entry = 'assets/' + file.relative_to(assets).as_posix()
            assert archive.read(entry) == file.read_bytes(), 'Missing/stale offline asset: ' + entry
    dex = [archive.read(n) for n in names if n.endswith('.dex')]
    for cls in [b'Lcom/yunjian/riichi/RoomService;', b'Lcom/yunjian/riichi/UnityHostBridge;']:
        assert any(cls in block for block in dex), 'Phone host class missing: ' + repr(cls)
    for abi in ['arm64-v8a','armeabi-v7a']:
        libs = [n for n in names if n.startswith('lib/' + abi + '/') and n.endswith('.so')]
        assert libs, 'Missing ABI: ' + abi
        assert any('il2cpp' in n or 'application' in n for n in libs), 'Missing native managed-code library: ' + abi
    assert any(n.startswith('assets/bin/Data/') for n in names), 'Missing Unity player data'
    for file in (root / 'unity/Assets/Resources/Majsoul/OriginalTile').glob('*.png'):
        guid = re.search(r'guid: (\w+)', Path(str(file) + '.meta').read_text()).group(1)
        assert 'assets/bin/Data/' + guid in names, 'Missing original tile atlas: ' + file.name
    shader_data = b''.join(archive.read(n) for n in names if 'globalgamemanagers.assets' in n)
    for shader in [b'YunJian/OriginalHand', b'YunJian/OriginalTileBody', b'YunJian/TableCloth']:
        assert shader in shader_data, 'Missing compiled shader: ' + repr(shader)
dist = root / 'dist'
output = dist / apk.name
output.write_bytes(apk.read_bytes())
(dist / 'SHA256-unity-0.5.2.txt').write_text(hashlib.sha256(output.read_bytes()).hexdigest().upper() + '  ' + output.name + '\n', encoding='utf8')
print('PASS: APK ZIP integrity, current offline asset bytes, phone host/JNI classes, ARM64 + ARMv7, original tile atlases and compiled shaders.')
print(output.name, output.stat().st_size, hashlib.sha256(output.read_bytes()).hexdigest())
