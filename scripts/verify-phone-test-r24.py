"""Validate the actual split-resource APK, signatures, source inclusion and size."""
from pathlib import Path
from zipfile import ZipFile
import hashlib,importlib.util,json,re,shutil,subprocess,sys
R=Path(__file__).resolve().parent.parent;B=R/'build/wardrobe-r22';APK=Path(sys.argv[1]).resolve();U=R/'unity'
def module(file):
 spec=importlib.util.spec_from_file_location('verify_r22_signing',R/'scripts'/file);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
signature=module('verify-apk-signature.py').verify_signature(APK)
assert signature['bytes']<=300000000,'Basic APK exceeds 300 MB: '+str(signature['bytes'])
badging=subprocess.run([str(R/'tools/sdk/android-15/aapt2.exe'),'dump','badging',str(APK)],capture_output=True,text=True,encoding='utf8',errors='replace',check=True).stdout
assert "name='com.yunjian.riichi.unity'"in badging and "versionCode='67'"in badging and "versionName='0.6.19'"in badging
layout=json.loads((R/'docs/wardrobe-resource-layout-r22.json').read_text('utf8'))
def guid(p):return re.search(r'(?m)^guid: ([a-f0-9]{32})$',Path(str(p)+'.meta').read_text('utf8'))[1]
resources=0;excluded=0
with ZipFile(APK)as archive:
 assert archive.testzip()is None;names=set(archive.namelist());metadata=archive.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
 for cls in ['WardrobePackService','WardrobeResourceUi','WardrobePackLease','NativeWinPlayback','NativeWinTileModel','NativeWinningTileQueue','ResourceRoomCoordinator','ClientHandOrder','ResourceDownloadUi','ResourceDownloadEstimate','ResourceListLayout','ResourceDownloadBatch']:
  assert cls.encode()in metadata,'Missing compiled runtime: '+cls
 for source in (U/'Assets/Resources').rglob('*'):
  if not source.is_file()or source.suffix in ['.meta','.shader']:continue
  entry='assets/bin/Data/'+guid(source);assert entry in names,'Missing bundled resource: '+source.relative_to(U).as_posix();resources+=1
  if source.suffix in ['.json','.txt','.bytes']:assert source.read_bytes()in archive.read(entry),'Stale source: '+str(source)
 for row in layout['moves']:
  source=U/row['to'];entry='assets/bin/Data/'+guid(source)
  assert entry not in names,'Optional/import-only asset leaked into APK: '+row['to'];excluded+=1
 for platform in ['android','windows']:
  source=U/'Assets/Resources/Majsoul/WardrobeDirectory'/('packs-'+platform+'.json');assert source.read_bytes()in archive.read('assets/bin/Data/'+guid(source))
 for abi in ['arm64-v8a','armeabi-v7a']:assert 'lib/'+abi+'/libil2cpp.so'in names,'Missing architecture '+abi
 for source in (R/'app/src/main/assets').rglob('*'):
  if source.is_file():assert archive.read('assets/'+source.relative_to(R/'app/src/main/assets').as_posix())==source.read_bytes(),'Stale offline engine asset: '+source.name
 dex=b''.join(archive.read(n)for n in names if n.endswith('.dex'))
 for cls in [b'Lcom/yunjian/riichi/RoomService;',b'Lcom/yunjian/riichi/UnityHostBridge;',b'Lcom/yunjian/riichi/QrImagePickerActivity;']:assert cls in dex,'Missing native host class '+repr(cls)
 shader_data=b''.join(archive.read(n)for n in names if 'globalgamemanagers.assets'in n or n=='assets/bin/Data/Resources/unity_builtin_extra')
 for shader in [b'YunJian/WardrobeEffectParticle',b'YunJian/FateParticle',b'YunJian/NativeEffectParticle',b'YunJian/OriginalTileFace',b'YunJian/OriginalTileBody',b'YunJian/OriginalTileOutline']:assert shader in shader_data,'Missing retained shader '+repr(shader)
for file,cases in [('checks.json',480),('checks-spot.json',48)]:assert json.loads((B/'capture'/file).read_text('utf8'))['cases']==cases
movie=json.loads((B/'capture/checks-movie.json').read_text('utf8'));assert movie['frames']==4440 and len(movie['clips'])==60
download=json.loads((B/'capture/checks-download.json').read_text('utf8'));assert len(download['checks'])>=20
ui=json.loads((R/'build/resource-ui-r23/capture/checks.json').read_text('utf8'));assert len(ui['checks'])==62 and not ui['apkBuilt'];assert 'RESOURCE UI R23 PASS 62' in (R/'build/resource-ui-r23/runtime-final.log').read_text('utf8',errors='replace')
report=dict(localPhoneTestOnly=True,resourceUiChecks=len(ui['checks']),version='0.6.19',versionCode=67,apk=signature,baseApkTargetMB=[200,300],bundledResources=resources,excludedHeavyAssets=excluded,winEffects=60,mappedNativeWins=59,fourSeatRonTsumoCases=480,supplementalLuaCases=48,downloadChecks=len(download['checks']),runtimeChecks=len(json.loads((B/'capture/checks.json').read_text('utf8'))['checks']),androidDeviceTested=False,artifactRedownloaded=False)
dest=R/'dist'/APK.name
if dest.resolve()!=APK:shutil.copy2(APK,dest)
(R/'dist/unity-0.6.19-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=1)+'\n','utf8')
(R/'dist/SHA256-unity-0.6.19.txt').write_text(signature['sha256']+'  '+dest.name+'\n','utf8')
print('R24 PHONE TEST APK PASS',signature['bytes'],'bytes;',resources,'bundled resources;',excluded,'heavy/import files excluded;',signature['sha256'])
