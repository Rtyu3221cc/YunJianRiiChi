"""Verify the phone host and offline assets inside the finished Unity APK."""
import hashlib
import re
import sys
from pathlib import Path
from zipfile import ZipFile
from importlib import import_module

root = Path(__file__).resolve().parent.parent
apk = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else root / 'unity/Builds/yunjian-riichi-unity-0.6.4.apk'
assets = root / 'app/src/main/assets'
signature = import_module('verify-apk-signature').verify_signature(apk)
with ZipFile(apk) as archive:
    assert archive.testzip() is None
    names = archive.namelist()
    metadata=archive.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
    for runtime_class in (b'NativeSettingsUi',b'NativeYakuUi',b'PlaylistAudio',b'AppPreferences',b'RiichiDiscardVisual',b'NativeCallouts',b'WordRect',b'CharacterRoster',b'CharacterIllustration',b'CharacterFraming',b'CharacterVoices',b'LobbyIllustrationSettings',b'LobbySakura'):
        assert runtime_class in metadata,'Missing compiled release code: '+repr(runtime_class)
    assert 'META-INF/MANIFEST.MF' in names, 'Missing v1/JAR signature manifest; run scripts/sign-unity-apk.ps1'
    for source in (root/'unity/Assets/Resources/Majsoul/Actions').glob('riichi-fire-*'):
        if source.suffix not in ('.png','.json'):continue
        guid=re.search(r'guid: (\w+)',Path(str(source)+'.meta').read_text(encoding='utf8')).group(1)
        entry='assets/bin/Data/'+guid
        assert entry in names,'Missing original riichi fire resource: '+source.name
        if source.suffix=='.json':assert source.read_bytes() in archive.read(entry),'Stale riichi fire data: '+source.name
    signature_files = [name for name in names if re.fullmatch(r'META-INF/[^/]+\.SF', name)]
    assert any(any(name[:-3] + suffix in names for suffix in ['.RSA', '.DSA', '.EC']) for name in signature_files), 'Missing v1 signature file or certificate'
    for file in assets.rglob('*'):
        if file.is_file():
            entry = 'assets/' + file.relative_to(assets).as_posix()
            assert archive.read(entry) == file.read_bytes(), 'Missing/stale offline asset: ' + entry
    dex = [archive.read(n) for n in names if n.endswith('.dex')]
    for cls in [b'Lcom/yunjian/riichi/RoomService;', b'Lcom/yunjian/riichi/UnityHostBridge;', b'Lcom/yunjian/riichi/QrImagePickerActivity;']:
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
    for source in (root / 'unity/Assets/Resources/Majsoul/Settlement').rglob('*'):
        if source.suffix not in ['.png', '.ttf', '.json']: continue
        guid=re.search(r'guid: (\w+)',Path(str(source)+'.meta').read_text()).group(1)
        assert 'assets/bin/Data/'+guid in names, 'Missing native settlement resource: '+source.name
        if source.suffix=='.json':assert source.read_bytes() in archive.read('assets/bin/Data/'+guid), 'Stale native settlement source manifest'
    metadata=archive.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
    for cls in [b'NativeUiChrome',b'LobbyController',b'LanRoomDiscovery',b'SavedSession',b'RoomQr',b'RoomQrScanner',b'OnImagePicked',b'BarcodeReaderGeneric',b'YakumanAnnouncement',b'NativeTakeoverBadge']:
        assert cls in metadata,'Missing session/UI runtime class: '+repr(cls)
    for name in ['ob_ting_off_btn','ob_ting_on_btn','button_text_bottom_s']:
        source=root/'unity/Assets/Resources/Majsoul/UI'/(name+'.png')
        guid=re.search(r'guid: (\w+)',Path(str(source)+'.meta').read_text()).group(1)
        assert 'assets/bin/Data/'+guid in names,'Missing AI takeover HUD sprite: '+name
    assert any(b'YUNJIAN-ROOM/1 ' in block for block in dex),'Phone-host room discovery protocol absent from DEX'
    for source in (root / 'unity/Assets/Resources/Majsoul/Audio').iterdir():
        if source.suffix not in ['.mp3','.wav','.json']:continue
        guid=re.search(r'guid: (\w+)',Path(str(source)+'.meta').read_text()).group(1)
        entry='assets/bin/Data/'+guid
        assert entry in names, 'Missing native audio clip: '+source.name
        if source.suffix=='.json':assert source.read_bytes() in archive.read(entry), 'Stale audio manifest'
        else:
            assert source.stem.encode() in archive.read(entry), 'Wrong packaged clip: '+source.name
            assert entry+'.resource' in names and archive.getinfo(entry+'.resource').file_size>0, 'Missing audio data: '+source.name
    for folder in ['SettlementVoice','Lobby/Audio']:
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
    for source in (root/'unity/Assets/Resources/Majsoul/TablePolish').iterdir():
        if source.suffix not in ['.png','.json','.ttf']:continue
        guid=re.search(r'guid: (\w+)',Path(str(source)+'.meta').read_text()).group(1);entry='assets/bin/Data/'+guid
        assert entry in names,'Missing table polish asset: '+source.name
        if source.suffix=='.json':assert source.read_bytes() in archive.read(entry),'Stale table polish provenance'
    for source in (root/'unity/Assets/Resources/Majsoul/TableLook').rglob('*'):
        if source.suffix not in ['.png','.json','.prefab']:continue
        guid=re.search(r'guid: (\w+)',Path(str(source)+'.meta').read_text()).group(1);entry='assets/bin/Data/'+guid
        if source.suffix=='.prefab' or source.parent.name=='Data':continue  # prefab and its modules are serialized into the scene data
        assert entry in names,'Missing table look asset: '+source.name
        if source.suffix=='.json':assert source.read_bytes() in archive.read(entry),'Stale table look data: '+source.name
    for source in (root/'unity/Assets/Resources/Majsoul/Review1012c').rglob('*'):
        if source.suffix not in ['.png','.json']:continue
        guid=re.search(r'guid: (\w+)',Path(str(source)+'.meta').read_text()).group(1);entry='assets/bin/Data/'+guid
        assert entry in names,'Missing cut-in/FightBegin review resource: '+source.name
        if source.suffix=='.json':assert source.read_bytes() in archive.read(entry),'Stale cut-in animation/layout/provenance: '+source.name
    for source in (root/'unity/Assets/Resources/Majsoul/Review1017d').rglob('*'):
        if source.suffix not in ['.png','.json']:continue
        guid=re.search(r'guid: (\w+)',Path(str(source)+'.meta').read_text()).group(1);entry='assets/bin/Data/'+guid
        assert entry in names,'Missing academy/takeover resource: '+source.name
        if source.suffix=='.json':assert source.read_bytes() in archive.read(entry),'Stale academy/takeover provenance: '+source.name
    sys.path.insert(0,str(root/'tools/asset-inspector'))
    import UnityPy
    word_texture_sizes={}
    for source in (root/'unity/Assets/Resources/Majsoul/Callouts1018').glob('*'):
        if source.suffix not in ('.png','.json'):continue
        guid=re.search(r'guid: (\w+)',Path(str(source)+'.meta').read_text(encoding='utf8')).group(1)
        entry='assets/bin/Data/'+guid
        assert entry in names,'Missing four-direction callout asset: '+source.name
        if source.suffix=='.json':assert source.read_bytes() in archive.read(entry),'Stale callout layout or character bounds: '+source.name
        if source.stem in ['shout_chi','shout_peng','shout_gang','shout_lizhi','shout_babei']:
            env=UnityPy.load(archive.read(entry));texture=next(o.read()for o in env.objects if o.type.name=='Texture2D')
            word_texture_sizes[source.stem]=[texture.m_Width,texture.m_Height]
            assert word_texture_sizes[source.stem]==[187,143],'Android import altered the full word canvas: '+source.name
    assert len(word_texture_sizes)==5
    print('PASS: all five APK callout word textures preserve 187x143 original canvases.')
    shader_data = b''.join(archive.read(n) for n in names if 'globalgamemanagers.assets' in n)
    assert b'YunJian/NativeCallout' in shader_data,'Missing compiled four-direction callout shader'
    character_root=root/'unity/Assets/Resources/Majsoul/Characters'
    character_counts={suffix:0 for suffix in ['.png','.mp3','.bytes','.json','.ttf','.txt']}
    for source in character_root.rglob('*'):
        if source.suffix not in character_counts:continue
        guid=re.search(r'guid: (\w+)',Path(str(source)+'.meta').read_text(encoding='utf8')).group(1)
        entry='assets/bin/Data/'+guid
        assert entry in names,'Missing offline character resource: '+source.relative_to(character_root).as_posix()
        character_counts[source.suffix]+=1
        if source.suffix in ['.json','.bytes','.txt']:assert source.read_bytes() in archive.read(entry),'Stale character metadata or skeleton: '+source.name
        if source.suffix=='.mp3':assert entry+'.resource' in names and archive.getinfo(entry+'.resource').file_size>0,'Missing original character audio data: '+source.name
    assert character_counts['.mp3']==626,'Incomplete four-character voice pack'
    for shader in [b'YunJian/CharacterSpine',b'YunJian/CharacterStraightAlpha',b'YunJian/CharacterIntro']:
        assert shader in shader_data,'Missing character shader in player build: '+repr(shader)
    print('PASS: complete four-character offline resources, 626 original voice clips, native skeleton bytes and player shaders.',character_counts)
    for source in (root/'unity/Assets/Resources/Majsoul/Settings').rglob('*'):
        if source.suffix not in ('.png','.json','.ttf','.mp3'):continue
        guid=re.search(r'guid: (\w+)',Path(str(source)+'.meta').read_text(encoding='utf8')).group(1)
        entry='assets/bin/Data/'+guid
        assert entry in names,'Missing 0.6.3 settings/yaku/music resource: '+source.name
        if source.suffix=='.json':assert source.read_bytes() in archive.read(entry),'Stale 0.6.3 settings data: '+source.name
        if source.suffix=='.mp3':assert entry+'.resource' in names and archive.getinfo(entry+'.resource').file_size>0,'Missing 0.6.3 music data: '+source.name
    assert b'UI/Default' in archive.read('assets/bin/Data/Resources/unity_builtin_extra'), 'Missing default UI shader for cut-in masking/composition'
    for source in (root/'unity/Assets/Resources/Majsoul/NativeReview').rglob('*'):
        if source.suffix not in ['.png','.json','.bytes']:continue
        guid=re.search(r'guid: (\w+)',Path(str(source)+'.meta').read_text()).group(1);entry='assets/bin/Data/'+guid
        assert entry in names,'Missing APK-imported review resource: '+source.name
        if source.suffix in ['.json','.bytes']:assert source.read_bytes() in archive.read(entry),'Stale native review data: '+source.name
    for shader in [b'YunJian/OriginalHand', b'YunJian/OriginalTileFace', b'YunJian/OriginalTileBody', b'YunJian/OriginalTileOutline', b'YunJian/ResponseGlow', b'YunJian/TurnLight', b'YunJian/NorthPetal', b'YunJian/WinParticle', b'YunJian/TableCloth', b'YunJian/TileShadow']:
        assert shader in shader_data, 'Missing compiled shader: ' + repr(shader)
    for source in (root/'unity/Assets/Resources/Majsoul/Presentation0535').glob('*'):
        if source.suffix not in ('.png','.json'):continue
        guid=re.search(r'guid: (\w+)',Path(str(source)+'.meta').read_text(encoding='utf8')).group(1);entry='assets/bin/Data/'+guid
        assert entry in names,'Missing 0.5.39 APK presentation resource: '+source.name
        if source.suffix=='.json':assert source.read_bytes() in archive.read(entry),'Stale 0.5.39 layout/provenance'
dist = root / 'dist'
output = dist / apk.name
output.write_bytes(apk.read_bytes())
checksum = 'SHA256-unity-0.6.4.txt' if len(sys.argv) == 1 else 'SHA256-' + output.stem + '.txt'
(dist / checksum).write_text(hashlib.sha256(output.read_bytes()).hexdigest().upper() + '  ' + output.name + '\n', encoding='utf8')
print('PASS: APK ZIP integrity, current offline assets, phone host/JNI, ARM64 + ARMv7, original tile atlases, exact hand animation bytes, action textures, original turn-light assets and compiled shaders.')
print(output.name, output.stat().st_size, hashlib.sha256(output.read_bytes()).hexdigest())
assert hashlib.sha256(output.read_bytes()).hexdigest() == signature['sha256'], 'Distributed APK differs from signature-verified file'
print('PASS: final APK v1 + v2, independent v1, original certificate and 16 KB alignment.')
