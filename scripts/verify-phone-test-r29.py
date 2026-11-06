"""Verify the current feedback APK from its actual packaged content."""
from pathlib import Path
from zipfile import ZipFile
import hashlib, importlib.util, json, re, shutil, subprocess, sys

R = Path(__file__).resolve().parent.parent
R31 = '--wardrobe-r31' in sys.argv[2:]
R30 = R31 or '--wardrobe-r30' in sys.argv[2:]
B = R / ('build/phone-test-r32' if R31 else 'build/wardrobe-r30' if R30 else 'build/phone-test-r29')
U = R / 'unity'
APK = Path(sys.argv[1]).resolve()
VERSION, CODE = ('0.6.22', 70) if R31 else ('0.6.21', 69) if R30 else ('0.6.20', 68)

spec = importlib.util.spec_from_file_location('signature_r29', R/'scripts/verify-apk-signature.py')
signing = importlib.util.module_from_spec(spec)
spec.loader.exec_module(signing)
signature = signing.verify_signature(APK)
assert signature['bytes'] <= 300000000, 'Unexpected heavy assets in base APK'
badging = subprocess.run([str(R/'tools/sdk/android-15/aapt2.exe'), 'dump', 'badging', str(APK)], capture_output=True, text=True, encoding='utf8', errors='replace', check=True).stdout
(B/'apk-badging.txt').write_text(badging, 'utf8')
for text in ["name='com.yunjian.riichi.unity'", f"versionCode='{CODE}'", f"versionName='{VERSION}'"]:
    assert text in badging, text
assert re.search(r"(?m)^(?:minSdkVersion|sdkVersion):'26'$", badging) and "targetSdkVersion:'32'" in badging

def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

def guid(p):
    return re.search(r'(?m)^guid: ([a-f0-9]{32})$', Path(str(p)+'.meta').read_text('utf8'))[1]

layout = json.loads((R/'docs/wardrobe-resource-layout-r22.json').read_text('utf8'))
resources = excluded = 0
with ZipFile(APK) as archive:
    names = set(archive.namelist())
    metadata = archive.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
    for cls in ['WardrobePackService', 'WardrobeResourceUi', 'NativeWinPlayback', 'NativeWinningTileQueue', 'ResourceRoomCoordinator', 'ClientHandOrder', 'ResourceDownloadUi', 'CharacterVoiceCatalog', 'CharacterVoiceLine', 'CharacterVoicePage', 'CharacterVoices', 'WardrobeResponseIndicator', 'NativePanel', 'WinCutIn']:
        assert cls.encode() in metadata, 'Missing runtime class: '+cls
    for method in ['DrawCharacterVoicePage', 'EquipDressItem', 'DismissOutside', 'BindCallableTileRoot']:
        assert method.encode() in metadata, 'Missing current feature: '+method
    if R30:
        for symbol in [b'NativeSpineEffect', b'TryGetItemDownloadProgress', b'BindLegacyRiichiSeat']:
            assert symbol in metadata, 'Missing wardrobe update: '+repr(symbol)
        android = json.loads((U/'Assets/Resources/Majsoul/WardrobeDirectory/packs-android.json').read_text('utf8'))
        assert any(row['id']=='ron_26shuiyi' for row in android['items'])
        for row in android['addresses']:
            if row['pack'].startswith('deco-r30-'):
                assert 'assets/bin/Data/'+guid(U/row['source']) not in names, row['source']
    if R31:
        for symbol in [b'RestoreSourceMaterials', b'RestoreCompressedSkin', b'ConfigureRiichi', b'tilePackPins', b'paintFootprints']:
            assert symbol in metadata, 'Missing effect correction: '+repr(symbol)
    for cls in ['PhoneFeedbackR25Capture', 'PhoneFeedbackR29Build', 'ReviewPreviousCalloutRendering']:
        assert cls.encode() not in metadata, 'Editor-only recorder leaked into Android: '+cls
    for source in (U/'Assets/Resources').rglob('*'):
        if not source.is_file() or source.suffix in ['.meta', '.shader']:
            continue
        entry = 'assets/bin/Data/'+guid(source)
        assert entry in names, 'Missing base resource: '+str(source.relative_to(U))
        resources += 1
        if source.suffix in ['.json', '.txt', '.bytes']:
            assert source.read_bytes() in archive.read(entry), 'Stale text resource: '+str(source)
    for row in layout['moves']:
        source = U/row['to']
        assert 'assets/bin/Data/'+guid(source) not in names, 'Downloadable/import asset leaked into APK: '+row['to']
        excluded += 1
    for abi in ['arm64-v8a', 'armeabi-v7a']:
        assert 'lib/'+abi+'/libil2cpp.so' in names, 'Missing native ABI: '+abi
    for source in (R/'app/src/main/assets').rglob('*'):
        if source.is_file():
            assert archive.read('assets/'+source.relative_to(R/'app/src/main/assets').as_posix()) == source.read_bytes(), 'Stale offline engine: '+source.name
    dex = b''.join(archive.read(n) for n in names if n.endswith('.dex'))
    for cls in [b'Lcom/yunjian/riichi/RoomService;', b'Lcom/yunjian/riichi/UnityHostBridge;', b'Lcom/yunjian/riichi/QrImagePickerActivity;']:
        assert cls in dex, 'Missing phone host class '+repr(cls)
    shaders = b''.join(archive.read(n) for n in names if 'globalgamemanagers.assets' in n or n=='assets/bin/Data/Resources/unity_builtin_extra')
    for name in [b'YunJian/WardrobeEffectParticle', b'YunJian/FateParticle', b'YunJian/NativeEffectParticle', b'YunJian/OriginalTileFace', b'YunJian/OriginalTileBody', b'YunJian/OriginalTileOutline', b'YunJian/WardrobeParticle']:
        assert name in shaders, 'Missing native shader: '+repr(name)

voice = json.loads((U/'Assets/Resources/Majsoul/Characters/voice-directory.json').read_text('utf8'))
assert len(voice['roles']) == 122 and sum(len(r['lines']) for r in voice['roles']) == 16226
assert all(line['available'] for row in voice['roles'] for line in row['lines'])
runtime = json.loads((R/'build/review-r28/capture/final-checks.json').read_text('utf8'))
assert runtime['frames']==1800 and runtime['calloutCases']==20 and len(runtime['checks'])==131
voice_runtime = json.loads((R/'build/review-r26/capture/movie-checks.json').read_text('utf8'))
assert voice_runtime['characters']==122 and voice_runtime['voiceLookups']==16226
node = (B/'node-tests.log').read_text('utf8', errors='replace')
assert re.search(r'pass\s+192\b', node) and re.search(r'fail\s+0\b', node)
build = (B/'android-build.log').read_text('utf8', errors='replace')
assert f'PHONE FEEDBACK R29 ANDROID BUILD PASS {VERSION}' in build
assert all(marker in build for marker in ['RELEASE 0545 UNIT PASS: 62', 'RELEASE 063 UNIT PASS: 87', 'RELEASE 065 ROLE UI UNIT PASS:'])

stage = R/'build/phone-test-r24/stage/unity'
source_rows = []
for folder in ['Assets/Scripts', 'Assets/Editor']:
    for source in (U/folder).rglob('*.cs'):
        target = stage/source.relative_to(U)
        assert target.is_file() and sha(target)==sha(source), 'Stale compiled input: '+str(source)
        source_rows.append({'path':source.relative_to(R).as_posix(), 'sha256':sha(source)})
(B/'source-stage-match.json').write_text(json.dumps(source_rows, indent=2), 'utf8')

dest = R/'dist'/APK.name
if dest.resolve()!=APK:
    shutil.copy2(APK, dest)
assert sha(dest)==signature['sha256']
report = dict(localPhoneTestOnly=True, version=VERSION, versionCode=CODE, apk=signature, bundledResources=resources, excludedHeavyAssets=excluded, nodeTests=192, compiledInputFiles=len(source_rows), voiceDirectoryRoles=122, visibleVoiceRows=16226, priorVoicePageRuntimeLookups=16226, priorR28RuntimeChecks=131, priorR28CalloutClips=20, androidDeviceTested=False, artifactRedownloaded=False, githubReleaseCreated=False, committed=False)
if R31:
    preview = json.loads((R/'build/wardrobe-r31/capture/checks.json').read_text('utf8'))
    assert len(preview['chapters']) == 10 and len(preview['checks']) == 166
    assert 'Hand texture survives loading all 46 riichi packs' in preview['checks']
    report.update(unityUnitAssertions=186, wardrobeRuntimeChecks=len(preview['checks']),
                  correctedEffects=len(preview['chapters']), wardrobeDownload=json.loads((B/'storage.json').read_text('utf8')))
elif R30:
    preview = json.loads((B/'capture/checks.json').read_text('utf8'))
    registered = json.loads((B/'registered.json').read_text('utf8'))['items']
    assert len(preview['chapters']) == len(registered)
    report.update(newWardrobeItems=len(registered), unityUnitAssertions=186, wardrobeRuntimeChecks=len(preview['checks']), wardrobePreviewFrames=preview['frames'])
(R/f'dist/unity-{VERSION}-verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n', 'utf8')
(R/f'dist/SHA256-unity-{VERSION}.txt').write_text(signature['sha256']+'  '+dest.name+'\n', 'utf8')
progress = json.loads((B/'progress.json').read_text('utf8')) if (B/'progress.json').exists() else {}
progress.update(status='verified-local-phone-test-ready', apk=signature, githubReleaseCreated=False, committed=False)
(B/'progress.json').write_text(json.dumps(progress, ensure_ascii=False, indent=2)+'\n', 'utf8')
print('R29 PHONE TEST APK PASS', signature['bytes'], 'bytes;', resources, 'base resources;', excluded, 'heavy/import resources excluded;', signature['sha256'], flush=True)
