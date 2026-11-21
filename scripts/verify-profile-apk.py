"""Verify native player-info resources in the final Android APK, not just .meta files."""
from pathlib import Path
from zipfile import ZipFile
import hashlib,json,re,sys
ROOT=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(ROOT/'tools/asset-inspector'))
import UnityPy
apk=Path(sys.argv[1]).resolve();output=Path(sys.argv[2]).resolve()
P=ROOT/'unity/Assets/Resources/Majsoul/Profile'
def guid(p):return re.search(r'(?m)^guid: ([a-f0-9]{32})$',Path(str(p)+'.meta').read_text('utf8'))[1]
checks=[]
with ZipFile(apk) as archive:
    metadata=archive.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
    for name in ['ProfilePortraitClips','DrawProfilePortrait','ProfileSourceLabel','ProfileAchievementNumber']:
        assert name.encode() in metadata,'Missing current player-info runtime: '+name
    for name in ['PlayerInfoSourceR7Capture','PlayerInfoSourceR7Review','Release0640Build']:
        assert name.encode() not in metadata,'Editor-only review leaked into APK: '+name
    paths=list((P/'TitleIcons').glob('*.png'))+[P/'Native'/(n+'.png') for n in ['using','illustFrame','illustBg','tabInsideNormal','tabInsideSelected','waitingroom_mask']]
    assert len(paths)==20
    for path in paths:
        payload=archive.read('assets/bin/Data/'+guid(path))
        textures=[o for o in UnityPy.load(payload).objects if o.type.name=='Texture2D'];assert len(textures)==1
        data=textures[0].read_typetree();png=path.read_bytes()
        expected=(int.from_bytes(png[16:20],'big'),int.from_bytes(png[20:24],'big'))
        assert (data['m_Width'],data['m_Height'])==expected,'APK texture aspect changed: '+path.name
        assert data['m_MipCount']==1,'Unexpected APK texture mipmaps: '+path.name
        if path.stem=='waitingroom_mask':assert data['m_IsReadable'],'Portrait alpha silhouette cannot be read on Android'
        checks.append(dict(resource=path.relative_to(P).as_posix(),width=data['m_Width'],height=data['m_Height'],mipCount=data['m_MipCount'],readable=bool(data['m_IsReadable'])))
    font=P/'Fonts/haolong_0.ttf';fonts=[o for o in UnityPy.load(archive.read('assets/bin/Data/'+guid(font))).objects if o.type.name=='Font']
    assert len(fonts)==1 and bytes(fonts[0].read().m_FontData)==font.read_bytes(),'Achievement source font changed in APK'
report=dict(apk=apk.name,bytes=apk.stat().st_size,sha256=hashlib.sha256(apk.read_bytes()).hexdigest(),profileTextures=checks,sourceAchievementFont=True,portraitMaskRuntime=True,editorFixtureExcluded=True,androidDeviceTested=False)
output.parent.mkdir(parents=True,exist_ok=True);output.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n','utf8')
print('PASS: 20 original-size APK textures, no mipmaps, readable native portrait mask, source achievement font, current runtime and no editor fixture.')
