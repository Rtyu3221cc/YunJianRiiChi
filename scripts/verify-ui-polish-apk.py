"""Verify the common wardrobe and sanma fix entered the delivered Android build."""
from pathlib import Path
from zipfile import ZipFile
import sys,json,re,hashlib,subprocess
R=Path(__file__).resolve().parent.parent;apk=Path(sys.argv[1]);out=Path(sys.argv[2]);checks=[]
setup=(R/'unity/Assets/Editor/ProjectSetup.cs').read_text('utf8');version=re.search(r'ReleaseVersion="([^"]+)"',setup)[1];code=int(re.search(r'ReleaseCode=(\d+)',setup)[1])
badging=subprocess.check_output([str(R/'tools/sdk/android-15/aapt2.exe'),'dump','badging',str(apk)],text=True,encoding='utf8',errors='replace')
assert f"versionName='{version}'"in badging and f"versionCode='{code}'"in badging
with ZipFile(apk)as z:
    meta=z.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
    for s in ['DrawWardrobeItems','DrawWardrobePresetList','DrawWardrobePresetName','DrawWardrobePresetUse','VerticalCharacterLabel','ScoreSlot','Physical']:
        assert s.encode()in meta,'Missing shared UI runtime '+s;checks.append(s)
    for s in ['UiPolishR15Review','UiPolishR15Capture','ReviewScoreCards','ReviewScoreArrows','ReviewVerticalNames','ReviewWardrobeUi','Release0645Build','roomPresetRename','roomPresetName']:
        assert s.encode()not in meta,'Old rename control or editor probe included '+s
    checks.append('read-only preset UI and editor probes excluded')
source={}
for name in ['LobbyController.WardrobeShared.cs','LobbyController.Wardrobe.cs','LobbyController.Dorm.cs','LobbyController.RoomCustomization.cs','NativeReviewLayout.cs','ResultPresentation.cs']:
    p=R/'unity/Assets/Scripts'/name;data=p.read_bytes();assert data==(R/'build/phone-test-r24/stage/unity/Assets/Scripts'/name).read_bytes(),'Stale stage '+name;source[name]=hashlib.sha256(data).hexdigest()
report=dict(version=version,versionCode=code,checks=len(checks),passed=checks,sourceSHA256=source,bytes=apk.stat().st_size,sha256=hashlib.sha256(apk.read_bytes()).hexdigest(),androidDeviceTested=False)
out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n','utf8');print('R15 APK PASS',len(checks),'shared UI/runtime checks')
