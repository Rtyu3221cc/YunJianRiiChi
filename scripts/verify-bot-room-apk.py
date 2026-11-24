"""R13/R14 actual APK check. Behaviour is covered separately by engine/UI tests."""
from pathlib import Path
from zipfile import ZipFile
import sys,json,re,hashlib,subprocess
R=Path(__file__).resolve().parent.parent;A=R/'unity/Assets';sys.path.insert(0,str(R/'tools/asset-inspector'));import UnityPy
apk=Path(sys.argv[1]);out=Path(sys.argv[2]);checks=[]
def guid(p):return re.search(r'(?m)^guid: ([a-f0-9]{32})$',Path(str(p)+'.meta').read_text('utf8'))[1]
setup=(A/'Editor/ProjectSetup.cs').read_text('utf8');version=re.search(r'ReleaseVersion="([^"]+)"',setup)[1];code=int(re.search(r'ReleaseCode=(\d+)',setup)[1])
badging=subprocess.check_output([str(R/'tools/sdk/android-15/aapt2.exe'),'dump','badging',str(apk)],text=True,encoding='utf8',errors='replace')
for s in ["name='com.yunjian.riichi.unity'",f"versionCode='{code}'",f"versionName='{version}'"]:assert s in badging
with ZipFile(apk)as z:
    meta=z.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
    for s in ['BotLevel','BotDifficultyName','aiLevel','CommandBusy','FlushCharacterSelection','CharacterPackFingerprint','SynchronizeRoomVisuals','StartupRoomSkins','WarmRoomInterface']:
        assert s.encode()in meta,'Missing runtime '+s;checks.append('compiled '+s)
    for s in ['BotDifficultyR13Capture','BotDifficultyR13Review','RoomCacheR14Capture','RoomCacheR14Review','ReviewFullHashChecks','ReviewVerificationCacheHits','Release0644Build']:
        assert s.encode()not in meta,'Editor fixture leaked '+s
    checks.append('editor review fixtures excluded')
    for s in ['engine.js','ai.js','client.js']:
        source=(R/'app/src/main/assets'/s).read_bytes();plugin=A/'Plugins/Android/riichi-host.androidlib/assets'/s
        assert source==plugin.read_bytes()==z.read('assets/'+s),'Stale Android host '+s
        checks.append('exact current '+s)
    p=A/'Resources/Majsoul/Lobby/Sprites/Create/room_create_computer_text.png'
    rows=[o.read_typetree()for o in UnityPy.load(z.read('assets/bin/Data/'+guid(p))).objects if o.type.name=='Texture2D'];assert len(rows)==1
    t=rows[0];assert(t['m_Width'],t['m_Height'],t['m_MipCount'])==(185,63,1);checks.append('native 185x63 computer label, no mipmaps')
report=dict(version=version,versionCode=code,checks=len(checks),passed=checks,editorFixturesExcluded=True,androidDeviceTested=False,bytes=apk.stat().st_size,sha256=hashlib.sha256(apk.read_bytes()).hexdigest())
out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n','utf8');print('R13/R14 APK PASS',len(checks),'checks')
