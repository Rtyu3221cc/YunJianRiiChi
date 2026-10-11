"""Verify the phone host and offline assets inside the finished Unity APK."""
import hashlib
import re
import sys
from pathlib import Path
from zipfile import ZipFile

root = Path(__file__).resolve().parent.parent
apk = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else root / 'unity/Builds/yunjian-riichi-unity-0.5.23.apk'
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
    for file in ['discard-hand.json', 'discard-hand-frames.bytes', 'riichi-hand.json', 'riichi-hand-frames.bytes',
                 'lizhi-hand.json', 'lizhi-hand-frames.bytes', 'fulu-hand.json', 'fulu-hand-frames.bytes',
                 'tsumo-hand.json', 'tsumo-hand-frames.bytes', 'fire-effect.json', '../Results/source.json', '../OriginalTile/outline.json']:
        source = root / 'unity/Assets/Resources/Majsoul/Actions' / file
        guid = re.search(r'guid: (\w+)', Path(str(source) + '.meta').read_text()).group(1)
        assert source.read_bytes() in archive.read('assets/bin/Data/' + guid), 'Missing/stale original hand data: ' + file
    for name in ['show_rong', 'show_zimo', 'fire-fire_bai_add', 'fire-fire_bai_alpha', 'fire-yuanqiu2', 'fire-yuanqiu2_add', 'fire-Particle_Impact01', 'fire-lizi_00032', 'win-yuanqiu2', 'win-shanguang', 'win-bao', 'win-Light2', 'win-thunder', 'win-lizi', 'win-kuosan', 'response-glow', 'hand-skin', 'hand-nails', 'op_peng', 'op_chi', 'op_x', 'shout_peng', 'shout_babei', 'op_babei', 'north-petals', 'yiji-full']:
        source = root / 'unity/Assets/Resources/Majsoul/Actions' / (name+'.png')
        guid = re.search(r'guid: (\w+)', Path(str(source) + '.meta').read_text()).group(1)
        assert 'assets/bin/Data/' + guid in names, 'Missing action texture: ' + name
    for source in (root / 'unity/Assets/Resources/Majsoul/Results').glob('*.png'):
        guid=re.search(r'guid: (\w+)',Path(str(source)+'.meta').read_text(encoding='utf8')).group(1)
        assert 'assets/bin/Data/'+guid in names, 'Missing original settlement art: '+source.name
    for name in ['turn-light.png' , 'turn-light-source.json']:
        source = root / 'unity/Assets/Resources/Majsoul/Hud' / name
        guid = re.search(r'guid: (\w+)', Path(str(source) + '.meta').read_text()).group(1)
        assert 'assets/bin/Data/' + guid in names, 'Missing turn indicator asset: ' + name
        if name.endswith('.json'):
            assert source.read_bytes() in archive.read('assets/bin/Data/' + guid), 'Stale turn indicator source'
    for source in (root / 'unity/Assets/Resources/Majsoul/Indicators').glob('*.png'):
        guid=re.search(r'guid: (\w+)',Path(str(source)+'.meta').read_text()).group(1)
        assert 'assets/bin/Data/'+guid in names, 'Missing native indicator: '+source.name
    source=root/'unity/Assets/Resources/Majsoul/Indicators/source.json'
    guid=re.search(r'guid: (\w+)',Path(str(source)+'.meta').read_text()).group(1)
    assert source.read_bytes() in archive.read('assets/bin/Data/'+guid), 'Stale native indicator source manifest'
    for source in (root / 'unity/Assets/Resources/Majsoul/Settlement').iterdir():
        if source.suffix not in ['.png', '.ttf', '.json']: continue
        guid=re.search(r'guid: (\w+)',Path(str(source)+'.meta').read_text()).group(1)
        assert 'assets/bin/Data/'+guid in names, 'Missing native settlement resource: '+source.name
        if source.suffix=='.json':assert source.read_bytes() in archive.read('assets/bin/Data/'+guid), 'Stale native settlement source manifest'
    for source in (root / 'unity/Assets/Resources/Majsoul/Audio').iterdir():
        if source.suffix not in ['.mp3','.wav','.json']:continue
        guid=re.search(r'guid: (\w+)',Path(str(source)+'.meta').read_text()).group(1)
        entry='assets/bin/Data/'+guid
        assert entry in names, 'Missing native audio clip: '+source.name
        if source.suffix=='.json':assert source.read_bytes() in archive.read(entry), 'Stale audio manifest'
        else:
            assert source.stem.encode() in archive.read(entry), 'Wrong packaged clip: '+source.name
            assert entry+'.resource' in names and archive.getinfo(entry+'.resource').file_size>0, 'Missing audio data: '+source.name
    for folder in ['SettlementVoice']:
        for source in (root/'unity/Assets/Resources/Majsoul'/folder).iterdir():
            if source.suffix not in ['.mp3','.json']:continue
            guid=re.search(r'guid: (\w+)',Path(str(source)+'.meta').read_text()).group(1)
            entry='assets/bin/Data/'+guid
            assert entry in names, 'Missing original settlement clip/config: '+source.name
            if source.suffix=='.json':assert source.read_bytes() in archive.read(entry), 'Stale settlement config: '+source.name
            else:assert entry+'.resource' in names and archive.getinfo(entry+'.resource').file_size>0, 'Missing original voice data: '+source.name
    import json
    tornado=json.loads((root/'unity/Assets/Resources/Majsoul/Actions/fire-effect.json').read_text())
    assert tornado['skinId']==305034 and len(tornado['layers'])==9
    for layer in tornado['layers']:
        source=root/'unity/Assets/Resources/Majsoul/Actions'/(layer['texture']+'.png')
        guid=re.search(r'guid: (\w+)',Path(str(source)+'.meta').read_text()).group(1)
        assert 'assets/bin/Data/'+guid in names, 'Missing fire-tornado texture: '+source.name
    shader_data = b''.join(archive.read(n) for n in names if 'globalgamemanagers.assets' in n)
    for shader in [b'YunJian/OriginalHand', b'YunJian/OriginalTileFace', b'YunJian/OriginalTileBody', b'YunJian/OriginalTileOutline', b'YunJian/ResponseGlow', b'YunJian/TurnLight', b'YunJian/NorthPetal', b'YunJian/WinParticle', b'YunJian/TableCloth']:
        assert shader in shader_data, 'Missing compiled shader: ' + repr(shader)
dist = root / 'dist'
output = dist / apk.name
output.write_bytes(apk.read_bytes())
checksum = 'SHA256-unity-0.5.23.txt' if len(sys.argv) == 1 else 'SHA256-' + output.stem + '.txt'
(dist / checksum).write_text(hashlib.sha256(output.read_bytes()).hexdigest().upper() + '  ' + output.name + '\n', encoding='utf8')
print('PASS: APK ZIP integrity, current offline assets, phone host/JNI, ARM64 + ARMv7, original tile atlases, exact hand animation bytes, action textures, original turn-light assets and compiled shaders.')
print(output.name, output.stat().st_size, hashlib.sha256(output.read_bytes()).hexdigest())
