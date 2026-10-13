"""Re-sign a verified old player with a nondecreasing Android versionCode.

Only the manifest versionCode changes; all player/engine/assets payloads are
checked byte for byte. This permits a same-certificate visual rollback without
requiring the user to uninstall the current trial and erase app data.
"""
import argparse
import hashlib
import importlib
import json
import os
from pathlib import Path
import re
import struct
import subprocess
from zipfile import ZipFile

root = Path(__file__).resolve().parent.parent
parser = argparse.ArgumentParser()
parser.add_argument('--source', type=Path, default=root/'dist/yunjian-riichi-unity-0.5.36.apk')
parser.add_argument('--version-code', type=int, default=38)
args = parser.parse_args()
assert 37 <= args.version_code <= 2147483647
source = args.source.resolve()
verify = importlib.import_module('verify-apk-signature').verify_signature
source_report = verify(source)
assert source_report['sha256'] == 'cee77b64c31bb4d8d7810031ebfdd5d642a2285f1a65298f8d3f5a7a93177d24', 'Unexpected rollback baseline'
work = root/'build/rollback-0537'
work.mkdir(exist_ok=True)
unsigned = work/'rollback-unaligned.apk'
aligned = work/'rollback-aligned.apk'
target = root/f'dist/yunjian-riichi-unity-0.5.36-rollback-v{args.version_code}.apk'

def change_version_code(data, value):
    result = bytearray(data)
    assert struct.unpack_from('<H', data)[0] == 3
    offset = struct.unpack_from('<H', data, 2)[0]
    strings = None
    changed = 0
    while offset < len(data):
        kind, header, size = struct.unpack_from('<HHI', data, offset)
        assert size >= header >= 8 and offset+size <= len(data)
        if kind == 1:
            count, _, flags, start, _ = struct.unpack_from('<5I', data, offset+8)
            strings = []
            for i in range(count):
                p = offset+start+struct.unpack_from('<I', data, offset+header+i*4)[0]
                if flags & 0x100:
                    n = data[p]; p += 2 if n & 0x80 else 1
                    n = data[p]; p += 1
                    if n & 0x80: n = ((n & 0x7f) << 8) | data[p]; p += 1
                    strings.append(data[p:p+n].decode('utf8'))
                else:
                    n = struct.unpack_from('<H', data, p)[0]; p += 2
                    if n & 0x8000: n = ((n & 0x7fff) << 16) | struct.unpack_from('<H', data, p)[0]; p += 2
                    strings.append(data[p:p+n*2].decode('utf-16-le'))
        elif kind == 0x102:
            assert strings is not None
            ext = offset+header
            name = struct.unpack_from('<I', data, ext+4)[0]
            attr_start, attr_size, count = struct.unpack_from('<3H', data, ext+8)
            if strings[name] == 'manifest':
                for i in range(count):
                    p = ext+attr_start+i*attr_size
                    ns, name, _ = struct.unpack_from('<3I', data, p)
                    if strings[name] == 'versionCode':
                        assert strings[ns] == 'http://schemas.android.com/apk/res/android'
                        assert data[p+15] in (0x10, 0x11) and struct.unpack_from('<I', data, p+16)[0] == 37
                        struct.pack_into('<I', result, p+8, 0xffffffff)
                        struct.pack_into('<I', result, p+16, value)
                        changed += 1
        offset += size
    assert changed == 1 and offset == len(data)
    return bytes(result)

def signer_entry(name):
    return bool(re.fullmatch(r'META-INF/(?:MANIFEST\.MF|[^/]+\.(?:SF|RSA|DSA|EC))', name))

with ZipFile(source) as old, ZipFile(unsigned, 'w') as out:
    manifest = change_version_code(old.read('AndroidManifest.xml'), args.version_code)
    for entry in old.infolist():
        if signer_entry(entry.filename): continue
        payload = manifest if entry.filename == 'AndroidManifest.xml' else old.read(entry)
        out.writestr(entry, payload)

sdk = root/'tools/sdk/android-15'
java = Path('D:/Unity/Lib/6000.6.4f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/java.exe')
subprocess.run([str(sdk/'zipalign.exe'), '-f', '-P', '16', '4', str(unsigned), str(aligned)], check=True, capture_output=True)
env = dict(os.environ)
env.setdefault('RIICHI_KEYSTORE_PASSWORD', 'android')
env.setdefault('RIICHI_KEY_PASSWORD', env['RIICHI_KEYSTORE_PASSWORD'])
subprocess.run([str(java), '-jar', str(sdk/'lib/apksigner.jar'), 'sign', '--ks', str(Path(os.environ['USERPROFILE'])/'.android/debug.keystore'),
    '--ks-key-alias', 'androiddebugkey', '--ks-pass', 'env:RIICHI_KEYSTORE_PASSWORD', '--key-pass', 'env:RIICHI_KEY_PASSWORD',
    '--v1-signing-enabled', 'true', '--v2-signing-enabled', 'true', '--v3-signing-enabled', 'false', '--v4-signing-enabled', 'false',
    '--lib-page-alignment', '16384', '--out', str(target), str(aligned)], env=env, check=True, capture_output=True)
report = verify(target)
with ZipFile(source) as old, ZipFile(target) as new:
    expected = {name for name in old.namelist() if not signer_entry(name)}
    assert {name for name in new.namelist() if not signer_entry(name)} == expected
    for name in expected:
        assert new.read(name) == (manifest if name == 'AndroidManifest.xml' else old.read(name)), 'Rollback payload changed: '+name
badging = subprocess.check_output([str(sdk/'aapt.exe'), 'dump', 'badging', str(target)], text=True, encoding='utf8')
package = next(line for line in badging.splitlines() if line.startswith('package:'))
assert "name='com.yunjian.riichi.unity'" in package and f"versionCode='{args.version_code}'" in package and "versionName='0.5.36'" in package
report.update(baselineSHA256=source_report['sha256'], versionCode=args.version_code, versionName='0.5.36', unchangedPlayerPayloads=True)
(target.parent/'rollback-unity-0.5.37.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf8')
(target.parent/'SHA256-rollback-unity-0.5.37.txt').write_text(report['sha256'].upper()+'  '+target.name+'\n', encoding='utf8')
print('PASS: old 0.5.36 player payloads unchanged; Android versionCode '+str(args.version_code)+', same certificate, v1/v2/independent-v1 and 16 KB alignment.')
print(json.dumps(report, indent=2))
