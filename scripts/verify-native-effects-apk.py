"""Verify the signed phone-test APK contains the approved native effect revision."""
from collections import Counter
from pathlib import Path
from zipfile import ZipFile
import datetime,hashlib,json,re,runpy,subprocess,sys

R=Path(__file__).resolve().parent.parent
setup=(R/'unity/Assets/Editor/ProjectSetup.cs').read_text('utf8')
version=re.search(r'ReleaseVersion\s*=\s*"([^"]+)"',setup)[1]
code=int(re.search(r'ReleaseCode\s*=\s*(\d+)',setup)[1])
B=R/'build'/('release-'+version.replace('.',''));B.mkdir(exist_ok=True)
built=R/'unity/Builds'/('yunjian-riichi-unity-'+version+'.apk')

def source_stamp():
    rows=[(p.relative_to(R).as_posix(),p.stat().st_size,p.stat().st_mtime_ns)for folder in ['unity/Assets','app/src/main/assets']for p in (R/folder).rglob('*')if p.is_file()]
    return hashlib.sha256(json.dumps(sorted(rows),separators=(',',':')).encode()).hexdigest()

# Resume only the native section when the complete gate already passed against
# the identical APK and source tree. Never substitute a different build's log.
cache=B/'base-verification.json'
if '--resume-native'in sys.argv:
    saved=json.loads(cache.read_text('utf8'));apk=R/'dist'/built.name;signature=saved['signature']
    assert saved['sourceStamp']==source_stamp(),'Source changed since the complete package verification'
    assert all(hashlib.sha256(p.read_bytes()).hexdigest()==signature['sha256']for p in [apk,built]),'APK changed since the complete package verification'
    evidence=R/saved['evidence'];assert hashlib.sha256(evidence.read_bytes()).hexdigest()==saved['evidenceSHA256']
    print('PASS: reuse complete package gate for the unchanged signed APK and source tree.',flush=True)
else:
    previous_args=sys.argv[:]
    try:
        sys.argv=[str(R/'scripts/verify-unity-apk.py'),str(built)]
        base=runpy.run_path(str(R/'scripts/verify-unity-apk.py'),run_name='__main__')
    finally:sys.argv=previous_args
    apk=base['output'];signature=base['signature']
badging=subprocess.run([str(R/'tools/sdk/android-15/aapt2.exe'),'dump','badging',str(apk)],capture_output=True,text=True,encoding='utf8',errors='replace',check=True).stdout
for expected in ["package: name='com.yunjian.riichi.unity'",f"versionCode='{code}'",f"versionName='{version}'","minSdkVersion:'26'","targetSdkVersion:'32'"]:
    assert expected in badging,'Unexpected Android package metadata: '+expected
(B/'apk-badging.txt').write_text(badging,'utf8')
sys.path.insert(0,str(R/'tools/asset-inspector'));import UnityPy
N=R/'unity/Assets/Resources/Majsoul/NativeReview'
manifest=json.loads((N/'EffectsR11/manifest.json').read_text('utf8'))
catalog=json.loads((R/'unity/Assets/Resources/Majsoul/Wardrobe/catalog.json').read_text('utf8'))
records=[];prefabs=layers=0
with ZipFile(apk)as archive:
    names=set(archive.namelist());metadata=archive.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
    for symbol in [b'NativeParticleEffect',b'NativeEffectMotion',b'NativeEffectCamera',b'TableRotation',b'HasFixedWorldRotation',b'nativeWorldEuler',b'nativeTileEuler']:
        assert symbol in metadata,'Missing approved native playback code: '+repr(symbol)
    shader_data=b''.join(archive.read(name)for name in sorted(names)if 'globalgamemanagers.assets'in name)
    assert b'YunJian/NativeEffectParticle'in shader_data,'Native effect shader was stripped from the Android build'
    def entry(path):
        guid=re.search(r'guid: (\w+)',Path(str(path)+'.meta').read_text('utf8'))[1]
        target='assets/bin/Data/'+guid;assert target in names,'Missing native resource: '+path.name;return target
    for effect in manifest['effects']:
        item=next(i for i in catalog['items']if i['id']==effect['id'])
        wrapper=R/'unity/Assets/Resources'/(item['effect']+'.json')
        assert wrapper.read_bytes()in archive.read(entry(wrapper)),'Stale effect binding '+effect['id']
        config=json.loads(wrapper.read_text('utf8'))
        for native in config.get('nativeVariants',[config['nativeEffect']]):
            source=json.loads((N/(native+'-source.json')).read_text('utf8'))
            expected=Counter(n['name']for n in source['nodes']if n.get('particle'))
            data=archive.read(entry(N/(native+'.prefab')));env=UnityPy.load(data)
            particles=[obj for obj in env.objects if obj.type.name=='ParticleSystem']
            actual=Counter(obj.assets_file.objects[obj.read_typetree()['m_GameObject']['m_PathID']].peek_name()for obj in particles)
            assert actual==expected,'Missing or stale particle hierarchy: '+native
            renderers=[obj for obj in env.objects if obj.type.name=='ParticleSystemRenderer'];assert len(renderers)==len(particles)
            for renderer in renderers:
                materials=renderer.read_typetree()['m_Materials'];assert materials and all(p['m_PathID']for p in materials),'Broken native material reference: '+native
            static=sum(obj.type.name=='MeshRenderer'for obj in env.objects)
            assert static==sum(bool(n.get('staticMesh'))for n in source['nodes']),'Missing native animated mesh: '+native
            motion=N/(native+'-motion.json')
            if motion.exists():
                assert motion.read_bytes()in archive.read(entry(motion)),'Stale compiled-animation sampling: '+native
                assert any(obj.type.name=='MonoBehaviour'for obj in env.objects),'Native motion component missing: '+native
            prefabs+=1;layers+=len(particles)
            records.append(dict(effect=effect['id'],native=native,particleLayers=len(particles),staticMeshes=static,motion=motion.exists(),prefabSHA256=hashlib.sha256(data).hexdigest()))
        if item.get('cue'):entry(R/'unity/Assets/Resources/Majsoul/Audio'/(item['cue']+'.mp3'))
    scythe=json.loads((R/'unity/Assets/Resources/Majsoul/Actions/native-effect_liqi_23ex_liandao.json').read_text('utf8'))
    assert scythe['nativeWorldEuler']==[0,180,0] and 'nativeTileEuler'not in scythe,'Scythe did not retain the approved R13 vertical chopping frame'
    assert len(manifest['effects'])==24 and prefabs==27 and layers==312
assert hashlib.sha256(built.read_bytes()).hexdigest()==signature['sha256'],'Signed build and distribution differ'
tests=(B/'node-tests.log').read_text('utf8');node_match=re.search(r'pass\s+(\d+)\b',tests)
node_count=int(node_match[1])if node_match else 0
assert node_count==({'0.6.15':173,'0.6.16':181}[version])and re.search(r'fail\s+0\b',tests)
geometry=json.loads((R/'dist/previews/2026-10-27-effects-reference/source/r13/blade-geometry.json').read_text('utf8'));assert geometry['checks']==13
regression=(R/'build/effects-r13-regression.log').read_text('utf8',errors='replace');assert 'REVIEW PASS 620 checks'in regression
report=dict(version=version,versionCode=code,package='com.yunjian.riichi.unity',date=datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=9))).date().isoformat(),apk=signature,apkBuilt=True,githubUploaded=False,androidDeviceTested=False,nodeTests=node_count,nativeEffects=24,nativePrefabs=prefabs,nativeParticleLayers=layers,scytheGeometryChecks=13,allEffectsPreviewChecks=620,effects=records,limitations=['Physical Android installation, input, audio, GPU rendering, performance and LAN await the user device test.','The existing North-tile gap assertion remains reproducible with baseline outfits; this task did not change that layout.','The 620 all-effects and 13 scythe checks are prior R13 evidence, not a fresh full-game runtime run.'])
if version=='0.6.16':
    unit_log=(B/'unity-unit.log').read_text('utf8',errors='replace')
    assert 'RIICHI UNITY VALIDATION PASS'in unit_log
    unit_counts=[int(re.search(pattern,unit_log)[1])for pattern in [r'RELEASE 0545 UNIT PASS:\s*(\d+)',r'RELEASE 063 UNIT PASS:\s*(\d+)',r'RELEASE 064 CHARACTER UNIT PASS:\s*(\d+)',r'RELEASE 065 ROLE UI UNIT PASS:\s*(\d+)']]
    assert unit_counts==[62,87,385,37]
    python_tests=0
    for filename,expected in [('python-character-tests.log',8),('python-effect-tests.log',3)]:
        log=(B/filename).read_text('utf8',errors='replace');assert re.search(r'Ran '+str(expected)+r' tests',log)and re.search(r'\bOK\b',log);python_tests+=expected
    runtime_log=(B/'wardrobe-runtime.log').read_text('utf8',errors='replace');assert 'WARDROBE R15 SCREENSHOTS PASS 58'in runtime_log
    wardrobe=catalog['items'];hands=[i for i in wardrobe if i['category']=='hand'and not i.get('hiddenFromSelection')]
    assert {i['id']for i in hands}=={'hand-human','hand-cat-orange'}
    with ZipFile(apk)as archive:
        for name in ['Majsoul/Characters/catalog.json','Majsoul/CharacterDirectory/catalog.json']:
            source=R/'unity/Assets/Resources'/name;assert source.read_bytes()in archive.read(entry(source)),'Stale character sex metadata: '+name
        for symbol in [b'ResolveHand',b'ObserveWaitState',b'Avatar']:
            assert symbol in metadata,'Missing current release behavior: '+repr(symbol)
    report.update(unityUnitChecks=sum(unit_counts),unityUnitBreakdown=dict(zip(['session','calloutAtlas','character','roleUi'],unit_counts)),pythonTests=python_tests,wardrobeRuntimeChecks=58,handChoices=2,backgroundTakeoverGraceSeconds=120)
(R/f'dist/unity-{version}-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n','utf8')
(R/f'dist/SHA256-unity-{version}.txt').write_text(signature['sha256'].upper()+'  '+apk.name+'\n','utf8')
print('NATIVE EFFECT APK PASS',version,'effects=24 prefabs=27 particleLayers=312',signature['bytes'],signature['sha256'],flush=True)
