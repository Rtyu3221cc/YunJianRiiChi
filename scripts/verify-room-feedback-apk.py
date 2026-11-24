"""Validate the R12 runtime, original UI and Ubel rig in the actual APK payload."""
from pathlib import Path
from zipfile import ZipFile
import sys,json,re,hashlib,subprocess
R=Path(__file__).resolve().parent.parent;sys.path.insert(0,str(R/'tools/asset-inspector'));import UnityPy
A=R/'unity/Assets';apk=Path(sys.argv[1]);out=Path(sys.argv[2]);checks=[]
badging=subprocess.check_output([str(R/"tools/sdk/android-15/aapt2.exe"),"dump","badging",str(apk)],text=True,encoding="utf8",errors="replace")
actual=re.search(r"package: name='([^']+)' versionCode='(\d+)' versionName='([^']+)'",badging);assert actual and actual[1]=="com.yunjian.riichi.unity"
setup=(A/"Editor/ProjectSetup.cs").read_text("utf8");version=re.search(r'ReleaseVersion="([^"]+)"',setup)[1];code=int(re.search(r"ReleaseCode=(\d+)",setup)[1]);assert (actual[3],int(actual[2]))==(version,code)
def guid(p):return re.search(r'(?m)^guid: ([a-f0-9]{32})$',Path(str(p)+'.meta').read_text('utf8'))[1]
with ZipFile(apk)as z:
 names=set(z.namelist());meta=z.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
 for marker in ['DrawRoomCustomization','OpenRoomCustomization','RoomCustomizationLayout','RoleBorder','urgentDisplayQueue','VisualVersion','WarmSelectedIllustrations','homeDisplayedSkin','DrawTitleRect']:
  assert marker.encode()in meta,'Missing R12 runtime '+marker;checks.append(marker)
 for marker in ['RoomFeedbackR12Capture','RoomFeedbackR12Review','Release0643Build','ReviewDisplayOrder','ValidationCaptureRequests','ReviewHudRows']:
  assert marker.encode()not in meta,'Editor fixture leaked '+marker
 def payload(p):return z.read('assets/bin/Data/'+guid(p))
 def text(p):
  rows=[o.read().m_Script for o in UnityPy.load(payload(p)).objects if o.type.name=='TextAsset'];assert len(rows)==1
  return rows[0].decode('utf8')if isinstance(rows[0],bytes)else rows[0]
 for relative in ['RoomCustomization/layout.json','CharacterHands/catalog.json','Characters/native-fonts.json']:
  p=A/'Resources/Majsoul'/relative;assert json.loads(text(p))==json.loads(p.read_text('utf8'));checks.append(relative)
 hand=json.loads(text(A/'Resources/Majsoul/CharacterHands/catalog.json'))['hands'];assert len(hand)==22
 new=next(h for h in hand if h['key']=='hand_youbeier');assert new['sourceItemId']==30990020 and new['characters']==[20000131]and len(new['variants'])==4
 for path in new['variants']:
  assert 'assets/bin/Data/'+guid(A/'Resources'/(path+'.prefab'))in names
  for action in ['Dapai','LiQiPai','LiZhi','Fulu','Zimo']:
   p=A/'Resources'/(path+'-'+action+'.json');data=json.loads(text(p));assert data==json.loads(p.read_text('utf8'))and data['frames']>10
   checks.append(path.rsplit('/',1)[-1]+'/'+action)
 for relative in ['Lobby/Fonts/brush.ttf','Characters/Fonts/character-name-brush.ttf']:
  p=A/'Resources/Majsoul'/relative;fonts=[o.read()for o in UnityPy.load(payload(p)).objects if o.type.name=='Font'];assert len(fonts)==1 and bytes(fonts[0].m_FontData)==p.read_bytes(),'Original role font changed: '+relative;checks.append('current source font '+relative)
 ui=[]
 for p in [*(A/'Resources/Majsoul/RoomCustomization').glob('*.png'),A/'Resources/Majsoul/Profile/TitleIcons/notitle.png',A/'Resources/Majsoul/TableLook/Sprites/notitle.png']:
  objects=[o.read_typetree()for o in UnityPy.load(payload(p)).objects if o.type.name=='Texture2D'];assert len(objects)==1
  o=objects[0];png=p.read_bytes();dims=(int.from_bytes(png[16:20],'big'),int.from_bytes(png[20:24],'big'));assert(o['m_Width'],o['m_Height'])==dims and o['m_MipCount']==1,p.name
  ui.append({'file':str(p.relative_to(A)).replace('\\','/'),'width':dims[0],'height':dims[1]})
 shader=A/'Resources/Majsoul/CharacterHand.shader';entry='assets/bin/Data/'+guid(shader)
 if entry in names:data=payload(shader)
 else:
  base='assets/bin/Data/globalgamemanagers.assets';parts=sorted((n for n in names if re.fullmatch(re.escape(base)+r'\.split\d+',n)),key=lambda n:int(n.rsplit('split',1)[1]));data=b''.join(z.read(n)for n in parts)if parts else z.read(base)
 rows=[o.read_typetree()for o in UnityPy.load(data).objects if o.type.name=='Shader'];row=next(o for o in rows if o['m_ParsedForm']['m_Name']=='YunJian/CharacterHand')
 props={p['m_Name']for p in row['m_ParsedForm']['m_PropInfo']['m_Props']};assert {'_RampMask','_RimMaskMap','_RampDirection','_ViewRampStrength','_ViewRampWidth'}<=props
 assert {9,18}<=set(row['platforms'])and len(row['compressedBlob'])>0;checks.append('Custom2/CustomView compiled for GLES3 and Vulkan')
 rig_guids=set()
 for path in new['variants']:rig_guids.update(re.findall(r'guid: ([a-f0-9]{32})',(A/'Resources'/(path+'.prefab')).read_text('utf8')))
 adapted=0
 for p in (A/'Resources/Majsoul/CharacterHands/Materials').glob('*.mat'):
  if guid(p)not in rig_guids:continue
  mats=[o.read_typetree()for o in UnityPy.load(payload(p)).objects if o.type.name=='Material'];assert len(mats)==1
  floats=dict(mats[0]['m_SavedProperties']['m_Floats'])
  if floats.get('_SourceBRDF',0)==0:continue
  assert floats['_SourceBRDF']in [2,3];checks.append('original Ubel material '+mats[0]['m_Name']);adapted+=1
 assert adapted==5,'Ubel must retain skin plus four authored cloth materials'
report=dict(version=version,versionCode=code,checks=len(checks),passed=checks,originalUiTextures=ui,specialHands=22,UbelDirections=4,UbelClips=20,editorFixturesExcluded=True,androidDeviceTested=False,bytes=apk.stat().st_size,sha256=hashlib.sha256(apk.read_bytes()).hexdigest())
out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf8');print('R12 APK PASS',len(checks),'runtime/hand checks,',len(ui),'original UI textures')
