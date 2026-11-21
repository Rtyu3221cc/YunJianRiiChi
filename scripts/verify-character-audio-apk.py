"""Verify local audio-fix APK identity, signatures and embedded current inputs."""
from pathlib import Path
from zipfile import ZipFile
import importlib.util
import json
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parent.parent
apk = Path(sys.argv[1]).resolve()
source = (ROOT / 'unity/Assets/Editor/ProjectSetup.cs').read_text(encoding='utf-8')
version = re.search(r'ReleaseVersion="([^"]+)"', source)[1]
code = int(re.search(r'ReleaseCode=(\d+)', source)[1])
spec = importlib.util.spec_from_file_location('signing', ROOT / 'scripts/verify-apk-signature.py')
signing = importlib.util.module_from_spec(spec)
spec.loader.exec_module(signing)
signature = signing.verify_signature(apk)
badging = subprocess.check_output([str(ROOT / 'tools/sdk/android-15/aapt2.exe'), 'dump', 'badging', str(apk)], text=True, encoding='utf-8', errors='replace')
for expected in ["name='com.yunjian.riichi.unity'", f"versionCode='{code}'", f"versionName='{version}'", "targetSdkVersion:'32'"]:
    assert expected in badging, expected
assert re.search(r"(?m)^(?:minSdkVersion|sdkVersion):'26'$", badging), 'Minimum Android API must stay 26'
# 0.6.38 embeds all original yakuman scenes instead of downloading them.
assert signature['bytes'] < (500_000_000 if code >= 86 else 300_000_000)


def guid(file):
    return re.search(r'(?m)^guid: ([a-f0-9]{32})$', Path(str(file) + '.meta').read_text(encoding='utf-8'))[1]


checked_audio = []
with ZipFile(apk) as archive:
    assert archive.testzip() is None
    names = set(archive.namelist())
    metadata = archive.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
    for runtime in ['CharacterVoiceTriggers', 'CharacterVoicePlayback', 'CharacterVoiceCue', 'CharacterVoiceCatalog', 'CharacterVoices', 'ContextVoicePlaying', 'ronHan', 'tsumoHan']:
        assert runtime.encode() in metadata, f'Missing compiled audio runtime: {runtime}'
    if code >= 82:
        assert b'DressPreviewAudioPlaying' in metadata, 'Missing preview audio lifecycle fix'
    if code >= 83:
        for runtime in ['CharacterVoiceCaption', 'DrawCharacterSubtitle', 'get_CharacterSubtitleText']:
            assert runtime.encode() in metadata, f'Missing compiled character subtitle runtime: {runtime}'
        assert 'assets/bin/Data/' + guid(ROOT / 'unity/Assets/Resources/Majsoul/Characters/UI/mask_dialogue.png') in names, 'Missing subtitle panel texture'
    if code >= 84:
        for runtime in ['CharacterHands', 'CharacterHandVisual', 'nativeHand']:
            assert runtime.encode() in metadata, f'Missing compiled character hand runtime: {runtime}'
        assert b'yingz' in metadata, 'Missing final projected hand shadow guard'
        hand_root = ROOT / 'unity/Assets/Resources/Majsoul/CharacterHands'
        hand_catalog = json.loads((hand_root / 'catalog.json').read_text(encoding='utf-8'))
        assert len(hand_catalog['hands']) == 21 and sum(len(h['variants']) for h in hand_catalog['hands']) == 51
        hand_resources = [f for f in hand_root.rglob('*') if f.is_file() and f.suffix != '.meta']
        for file in hand_resources:
            assert 'assets/bin/Data/' + guid(file) in names, f'Missing embedded hand asset: {file.name}'
        assert 'assets/bin/Data/' + guid(ROOT / 'unity/Assets/Resources/Majsoul/CharacterHand.shader') in names, 'Missing portable hand shader'
    if code >= 85:
        sys.path.insert(0, str(ROOT / 'tools/asset-inspector'))
        import UnityPy
        shader_data = archive.read('assets/bin/Data/' + guid(ROOT / 'unity/Assets/Resources/Majsoul/CharacterHand.shader'))
        shaders = [o.read_typetree() for o in UnityPy.load(shader_data).objects if o.type.name == 'Shader']
        assert len(shaders) == 1
        properties = {p['m_Name'] for p in shaders[0]['m_ParsedForm']['m_PropInfo']['m_Props']}
        assert {'_SourceBRDF', '_BumpMap', '_ORMMap', '_RampMap', '_MatCapTex', '_HandLightColor'} <= properties, 'Stale hand shader in APK'
        for key in ['89e69b8543b4f672936f', '271bcf6e3f9418ea6eee', 'a33f3f8b8611db7d585f', '0ab55064d9047dc2e594']:
            data = archive.read('assets/bin/Data/' + guid(hand_root / 'Materials' / (key + '.mat')))
            mats = [o.read_typetree() for o in UnityPy.load(data).objects if o.type.name == 'Material']
            assert len(mats) == 1
            props = mats[0]['m_SavedProperties']
            assert dict(props['m_Floats'])['_SourceBRDF'] == 1, 'Missing Asuna source shading'
            assert dict(props['m_Colors'])['_HandLightColor']['r'] > 1.5
            assert all(dict(props['m_TexEnvs'])[key]['m_Texture']['m_PathID'] for key in ['_BumpMap', '_ORMMap', '_RampMap']), 'Missing Asuna shading maps'
        for key in ['58392b026cff376ed6b5', 'c36c7fcc7bd375231d37', 'c35819606159def1921c', '09b593fef228d1786fd2']:
            data = archive.read('assets/bin/Data/' + guid(hand_root / 'Textures' / (key + '.png')))
            textures = [o.read_typetree() for o in UnityPy.load(data).objects if o.type.name == 'Texture2D']
            assert len(textures) == 1 and textures[0]['m_MipCount'] == 1 and textures[0]['m_ColorSpace'] == 0, 'Stale Asuna ramp import'
    for editor in ['CharacterVoiceR34Capture', 'EffectAudioR33Review', 'EffectPreviewR35Capture', 'EffectPreviewAudioCapture', 'CharacterSubtitleCapture', 'CharacterHandCapture', 'CharacterHandImport']:
        assert editor.encode() not in metadata, f'Editor verifier leaked into APK: {editor}'
    for abi in ['arm64-v8a', 'armeabi-v7a']:
        assert f'lib/{abi}/libil2cpp.so' in names
    for file in (ROOT / 'app/src/main/assets').rglob('*'):
        if file.is_file():
            assert archive.read('assets/' + file.relative_to(ROOT / 'app/src/main/assets').as_posix()) == file.read_bytes(), f'Stale host input: {file.name}'
    for relative in ['Majsoul/Wardrobe/catalog.json', 'Majsoul/Characters/voice-directory.json', 'Majsoul/CharacterDirectory/packs-android.json', 'Majsoul/WardrobeDirectory/packs-android.json']:
        file = ROOT / 'unity/Assets/Resources' / relative
        packaged = archive.read('assets/bin/Data/' + guid(file))
        if file.read_bytes() not in packaged:
            # The existing stage uses CRLF for some JSON inputs. Compare the
            # actual packaged TextAsset rather than treating newlines as data.
            sys.path.insert(0, str(ROOT / 'tools/asset-inspector'))
            import UnityPy
            texts = [obj.read().m_Script for obj in UnityPy.load(packaged).objects if obj.type.name == 'TextAsset']
            assert len(texts) == 1 and json.loads(texts[0]) == json.loads(file.read_text(encoding='utf-8-sig')), f'Stale directory: {relative}'
    audit = json.loads((ROOT / 'docs/effect-audio-audit-2026-11-11.json').read_text(encoding='utf-8'))
    for record in audit['effects']:
        if not record.get('repaired'):
            continue
        file = ROOT / 'unity/Assets/Resources/Majsoul/Audio' / (record['cue'] + '.mp3')
        entry = 'assets/bin/Data/' + guid(file)
        assert entry in names and archive.getinfo(entry + '.resource').file_size > 0, f'New effect audio missing: {record["id"]}'
        checked_audio.append(record['id'])
    assert len(checked_audio) == 50
    base_voices = 0
    for file in (ROOT / 'unity/Assets/Resources/Majsoul/Characters/Voice').rglob('*.mp3'):
        entry = 'assets/bin/Data/' + guid(file)
        assert entry in names and archive.getinfo(entry + '.resource').file_size > 0, f'Base voice missing: {file.name}'
        base_voices += 1
report = {
    'localPhoneTestOnly': True,
    'version': version,
    'versionCode': code,
    'apk': signature,
    'embeddedNewEffectAudio': checked_audio,
    'baseVoiceFiles': base_voices,
    'currentHostInputs': True,
    'currentCatalogs': True,
    'characterHandResources': len(hand_resources) if code >= 84 else 0,
    'asunaSourceMaterials': code >= 85,
    'androidDeviceTested': False,
}
output = Path(sys.argv[2]).resolve() if len(sys.argv) > 2 else ROOT / 'build/character-voice-r34/apk-verification.json'
output.parent.mkdir(parents=True, exist_ok=True)
output.write_bytes((json.dumps(report, ensure_ascii=False, indent=2) + '\n').encode('utf-8'))
print(json.dumps(report, ensure_ascii=True))
