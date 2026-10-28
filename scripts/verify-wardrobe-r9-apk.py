"""Verify the distributed test APK includes the current effects and renderer."""
from pathlib import Path
from zipfile import ZipFile
import importlib.util,hashlib,json,re,subprocess
R=Path(__file__).resolve().parent.parent;apk=R/'dist/yunjian-riichi-unity-0.6.14.apk'
spec=importlib.util.spec_from_file_location('apk_signature',R/'scripts/verify-apk-signature.py');module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);signature=module.verify_signature(apk)
badging=subprocess.run([str(R/'tools/sdk/android-15/aapt2.exe'),'dump','badging',str(apk)],capture_output=True,text=True,encoding='utf8',errors='replace',check=True).stdout
assert "package: name='com.yunjian.riichi.unity'"in badging and "versionCode='62'"in badging and "versionName='0.6.14'"in badging
catalog=json.loads((R/'unity/Assets/Resources/Majsoul/Wardrobe/catalog.json').read_text('utf8'));keys=['callout-sakura','callout-frost','callout-vortex','riichi-afterimage','riichi-feather','ron-blackfire','ron-reactor'];counts={}
with ZipFile(apk)as archive:
    metadata=archive.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
    for symbol in [b'CalloutReadability',b'calloutFaceOpacity',b'calloutIntensity']:assert symbol in metadata,'Missing current renderer code: '+repr(symbol)
    def entry(path):
        guid=re.search(r'guid: (\w+)',Path(str(path)+'.meta').read_text('utf8'))[1];name='assets/bin/Data/'+guid;assert name in archive.namelist(),'Missing resource '+str(path);return name
    for key in keys:
        item=next(i for i in catalog['items']if i['id']==key);path=R/'unity/Assets/Resources'/(item['effect']+'.json');assert path.read_bytes()in archive.read(entry(path)),'Stale effect '+key
        graph=json.loads(path.read_text('utf8'));counts[key]=len(graph['layers'])
        for layer in graph['layers']:
            texture=R/'unity/Assets/Resources'/('Majsoul/'+layer['texture']if '/'in layer['texture']else'Majsoul/Actions/'+layer['texture']);entry(texture.with_suffix('.png'))
        if item.get('cue'):entry(R/'unity/Assets/Resources/Majsoul/Audio'/(item['cue']+'.mp3'))
    assert counts['callout-sakura']==8
built=R/'unity/Builds'/apk.name;assert hashlib.sha256(built.read_bytes()).hexdigest()==signature['sha256'],'Distribution copy differs from signed build'
node=(R/'build/wardrobe-r9-study/node-tests.log').read_text('utf8');assert re.search(r'pass\s+167\b',node)and re.search(r'fail\s+0\b',node)
source=json.loads((R/'build/wardrobe-r9-study/source-validation.json').read_text('utf8'));readable=json.loads((R/'build/wardrobe-r9-study/readability.json').read_text('utf8'));rings=json.loads((R/'build/wardrobe-r9-study/ring-validation.json').read_text('utf8'));patch=json.loads((R/'build/wardrobe-r9-review/reactor-patch/capture.json').read_text('utf8'))
report=dict(version='0.6.14',versionCode=62,package='com.yunjian.riichi.unity',apk=signature,apkBuilt=True,githubUploaded=False,androidDeviceTested=False,nodeTests=167,sourceChecks=source['checks'],calloutReadabilityChecks=readable['checks'],ringGeometryChecks=rings['checks'],ringPatchRuntimeChecks=patch['checks'],bundledLayerCounts=counts,limitations=['Android installation, input, sound and LAN behavior await physical-device testing.','Sakura, frost and vortex river indicators are explicitly labelled adaptations.'])
(R/'dist/unity-0.6.14-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n','utf8');(R/'dist/SHA256-unity-0.6.14.txt').write_text(signature['sha256'].upper()+'  '+apk.name+'\n','utf8');(R/'build/wardrobe-r9-study/apk-badging.txt').write_text(badging,'utf8')
print('R9 APK PASS',signature['bytes'],signature['sha256'],counts)
