"""Check the actual Android payload for the November 14 visual fixes."""
from pathlib import Path
from zipfile import ZipFile
import hashlib,json,re,sys

ROOT=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(ROOT/'tools/asset-inspector'))
import UnityPy
apk=Path(sys.argv[1]).resolve()
output=Path(sys.argv[2]).resolve()
folder=ROOT/'unity/Assets/Resources/Majsoul/NativeReview/YakumanR39'

def guid(path):
    return re.search(r'(?m)^guid: ([a-f0-9]{32})$',Path(str(path)+'.meta').read_text('utf8'))[1]

with ZipFile(apk)as archive:
    names=set(archive.namelist())
    def payload(path):return archive.read('assets/bin/Data/'+guid(path))
    def material(path):
        rows=[o.read_typetree()for o in UnityPy.load(payload(path)).objects if o.type.name=='Material']
        assert len(rows)==1,path.name
        return rows[0]
    metadata=archive.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
    for value in ['YakumanCinematic','HandTiles','SourceKey','ActualTiles','NativeWinningTileQueue','MatchIntroduction']:
        assert value.encode()in metadata,'Missing compiled runtime '+value
    for value in ['Feedback1114Capture','YakumanCinematicCapture','CharacterHandGalleryCapture']:
        assert value.encode()not in metadata,'Editor fixture leaked '+value
    checked=0
    for path in folder.rglob('*'):
        if not path.is_file()or path.suffix=='.meta':continue
        assert 'assets/bin/Data/'+guid(path)in names,'Missing cinematic asset '+path.name
        if path.suffix=='.json':assert path.read_bytes()in payload(path),'Stale motion/catalog '+path.name
        checked+=1
    prefabs=list(folder.glob('*.prefab'))
    assert len(prefabs)==26
    plans=json.loads((folder/'catalog.json').read_text('utf8'))['plans']
    assert len(plans)==15
    # Check every original custom queue, plus the two previous rendering faults.
    queue_count=standard_count=outline_count=0
    for source in (ROOT/'build/feedback-1114-r39/yakuman/export/YakumanR39').glob('*-source.json'):
        key=source.name.removesuffix('-source.json')
        for i,node in enumerate(json.loads(source.read_text('utf8'))['nodes']):
            for j,entry in enumerate(node.get('materials',[])):
                custom=entry.get('hasSourceQueue')and entry.get('sourceQueue',-1)>=0
                special=entry['shaderName']in ['Particles/Standard Unlit','Catfood/Outline']
                if not custom and not special:continue
                data=material(folder/f'{key}-{i}-{j}.mat')
                if custom:
                    assert data['m_CustomRenderQueue']==entry['sourceQueue'],f'Wrong queue {key}/{i}/{j}'
                    queue_count+=1
                floats=dict(data['m_SavedProperties']['m_Floats'])
                if entry['shaderName']=='Particles/Standard Unlit':
                    original={v['name']:v['value']for v in entry['floats']}
                    assert floats['_SrcBlend']==original['_SrcBlend']and floats['_DstBlend']==original['_DstBlend'],'Opaque native sweep'
                    color=next(v['value']for v in entry['colors']if v['name']=='_Color')
                    assert abs(dict(data['m_SavedProperties']['m_Colors'])['_TintColor']['a']*2-color[3])<1e-5
                    standard_count+=1
                if entry['shaderName']=='Catfood/Outline':
                    assert floats['_ZWrite']==entry['zWrite']==0
                    outline_count+=1
    assert standard_count==6 and outline_count>=1
    audio=[]
    for record in json.loads((ROOT/'build/feedback-1114-r39/yakuman/audio-audit.json').read_text('utf8')):
        path=ROOT/'unity/Assets/Resources'/record['file']
        assert hashlib.sha256(path.read_bytes()).hexdigest()==record['sha256']
        rows=[o.read_typetree()for o in UnityPy.load(payload(path)).objects if o.type.name=='AudioClip']
        assert len(rows)==1 and abs(rows[0]['m_Length']-record['seconds'])<.05
        assert archive.getinfo('assets/bin/Data/'+guid(path)+'.resource').file_size>0
        audio.append(path.name)
    assert len(audio)==16
report=dict(version='0.6.38',versionCode=86,apk=str(apk),bytes=apk.stat().st_size,sha256=hashlib.sha256(apk.read_bytes()).hexdigest(),cinematicAssets=checked,prefabs=len(prefabs),yakumanTypes=len(plans),sourceQueues=queue_count,standardParticleSweeps=standard_count,separateOutlineMaterials=outline_count,audio=audio,androidDeviceTested=False)
output.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n','utf8')
print(json.dumps(report,ensure_ascii=True))
