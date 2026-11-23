"""Check R8/R9/R10/R11 inputs in the real Android payload, without device claims."""
from pathlib import Path
from zipfile import ZipFile
import sys,json,re,hashlib
R=Path(__file__).resolve().parent.parent;sys.path.insert(0,str(R/'tools/asset-inspector'));import UnityPy
apk=Path(sys.argv[1]).resolve();out=Path(sys.argv[2]).resolve();A=R/'unity/Assets';checks=[]
setup=(A/'Editor/ProjectSetup.cs').read_text('utf8');version=re.search(r'ReleaseVersion="([^"]+)"',setup)[1];code=int(re.search(r'ReleaseCode=(\d+)',setup)[1])
def guid(p):return re.search(r'(?m)^guid: ([a-f0-9]{32})$',Path(str(p)+'.meta').read_text('utf8'))[1]
with ZipFile(apk)as archive:
 names=set(archive.namelist());meta=archive.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
 for marker in ['ToggleLobbyRank','RankAnimationEnd','RankDisplayState','CenterStickInSlot','IsShadow','FitNovemberEnvironment','LatestRiichi','NativeScroll','TitleAchievements','RestoreModelShaders','SourceRenderQueue']:
  assert marker.encode()in meta,'Missing compiled current runtime: '+marker;checks.append(marker)
 for marker in ['NovContentCapture','NovWardrobeValidation','NovCharacterValidation','Release0641Build','RankStickR10Capture','VisualFeedbackR11Review','R11BundleValidation','Release0642Build']:
  assert marker.encode()not in meta,'Editor fixture included: '+marker
 def payload(p):return archive.read('assets/bin/Data/'+guid(p))
 def text(p):
  objects=[o.read().m_Script for o in UnityPy.load(payload(p)).objects if o.type.name=='TextAsset'];assert len(objects)==1
  return objects[0].decode('utf8')if isinstance(objects[0],bytes)else objects[0]
 for rel in ['CharacterDirectory/catalog.json','CharacterDirectory/packs-android.json','WardrobeDirectory/packs-android.json','Characters/framing.json','Characters/voice-directory.json','Settings/music.json','Wardrobe/catalog.json','Actions/playback-contracts.json','Settlement/RankCalc/layout.json','Profile/titles.json','NativeReview/NovR10/render-queues.json']:
  p=A/'Resources/Majsoul'/rel;assert json.loads(text(p))==json.loads(p.read_text('utf8')),'Stale APK JSON '+rel;checks.append(rel)
 directory=json.loads(text(A/'Resources/Majsoul/CharacterDirectory/catalog.json'));roles=[r for r in directory['roles']if r['key']in ['lingxing','fulilian','feilun','xiutaerke','youbeier']];assert len(roles)==5 and sum(len(r['skins'])for r in roles)==14
 for role in roles:
  for skin in role['skins']:
   p=A/'CharacterPacks/Source'/role['key']/'Skins'/str(skin['id'])/'full.png';assert 'assets/bin/Data/'+guid(p)not in names,'Optional illustration entered APK '+str(skin['id'])
 textureChecks=[]
 for p in [*(A/'Resources/Majsoul/Settlement/RankCalc').glob('*.png'),A/'Resources/Majsoul/Characters/UI/skin_using.png']:
  rows=[o.read_typetree()for o in UnityPy.load(payload(p)).objects if o.type.name=='Texture2D'];assert len(rows)==1;data=rows[0];png=p.read_bytes();expected=(int.from_bytes(png[16:20],'big'),int.from_bytes(png[20:24],'big'))
  assert (data['m_Width'],data['m_Height'])==expected and data['m_MipCount']==1,p.name;textureChecks.append(dict(name=p.name,width=expected[0],height=expected[1]))
 # AlwaysIncludedShaders can be serialized into the split shared file instead
 # of their Resources GUID entry. Validate the real compiled object in either
 # layout, including the complete numbered split sequence.
 p=A/'Resources/Majsoul/NativeReview/NovR10/NovEffectParticle.shader'
 shaderEntry='assets/bin/Data/'+guid(p)
 if shaderEntry in names:
  shaderData=archive.read(shaderEntry);shaderParts=[shaderEntry]
 else:
  shaderEntry='assets/bin/Data/globalgamemanagers.assets'
  shaderParts=sorted([n for n in names if re.fullmatch(re.escape(shaderEntry)+r'\.split\d+',n)],key=lambda n:int(n.rsplit('split',1)[1]))
  if shaderParts:
   assert [int(n.rsplit('split',1)[1])for n in shaderParts]==list(range(len(shaderParts))), 'Incomplete Unity shared shader file'
   shaderData=b''.join(archive.read(n)for n in shaderParts)
  else:
   shaderData=archive.read(shaderEntry);shaderParts=[shaderEntry]
 rows=[o.read_typetree()for o in UnityPy.load(shaderData).objects if o.type.name=='Shader']
 rows=[row for row in rows if row['m_ParsedForm']['m_Name']=='YunJian/NovR10EffectParticle'];assert len(rows)==1,'Compiled November shader missing or ambiguous'
 props={p['m_Name']for p in rows[0]['m_ParsedForm']['m_PropInfo']['m_Props']};assert {'_DistortTex','_Distort2Tex','_DistortMaskTex','_Mask2Tex','_DissolveTexUV','_DissolveUvCostom'}<=props
 assert {9,18}<=set(rows[0]['platforms']) and len(rows[0]['compressedBlob'])>0,'Missing GLES3/Vulkan shader programs'
 shaderLocation=dict(parts=shaderParts,platforms=rows[0]['platforms'],compiledBytes=len(rows[0]['compressedBlob']),name=rows[0]['m_ParsedForm']['m_Name'])
 staticEntry='assets/bin/Data/'+guid(A/'Resources/Majsoul/Wardrobe/StaticDecoration.shader')
 staticData=archive.read(staticEntry) if staticEntry in names else shaderData
 staticRows=[o.read_typetree()for o in UnityPy.load(staticData).objects if o.type.name=='Shader']
 staticRows=[row for row in staticRows if row['m_ParsedForm']['m_Name']=='YunJian/StaticDecoration'];assert len(staticRows)==1
 assert {9,18}<=set(staticRows[0]['platforms']) and len(staticRows[0]['compressedBlob'])>0,'Missing current model shader programs'
 checks.append('current compiled StaticDecoration GLES3/Vulkan')
 engine=archive.read('assets/engine.js');assert engine==(R/'app/src/main/assets/engine.js').read_bytes()
 for role in roles:assert role['key'].encode()in engine
report=dict(version=version,versionCode=code,bytes=apk.stat().st_size,sha256=hashlib.sha256(apk.read_bytes()).hexdigest(),currentRuntimeAndCatalogs=checks,textures=textureChecks,newRoles=5,newSkins=14,optionalIllustrationsExcluded=True,novemberPortableShader=True,shaderLocation=shaderLocation,editorFixturesExcluded=True,androidDeviceTested=False)
out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n','utf8');print('R10/R11 APK INPUT VERIFICATION PASS',len(checks),'runtime/catalog checks',len(textureChecks),'native textures')
