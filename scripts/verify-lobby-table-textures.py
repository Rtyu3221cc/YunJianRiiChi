"""Compare packaged texture presets with the actual 0.5.39 APK, not just source."""
from pathlib import Path
import sys,re,zipfile,json,warnings
R=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(R/'tools/asset-inspector'));import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
old=zipfile.ZipFile(R/'dist/yunjian-riichi-unity-0.5.39.apk');new=zipfile.ZipFile(sys.argv[1]);verified=[];differences=[]
def settings(archive,entry):
 o=next(o for o in UnityPy.load(archive.read(entry)).objects if o.type.name=='Texture2D');d=o.read_typetree()
 return {k:d[k]for k in ['m_Name','m_Width','m_Height','m_TextureFormat','m_MipCount','m_TextureSettings','m_ColorSpace']if k in d}
for source in (R/'unity/Assets/Resources/Majsoul').rglob('*.png'):
 if 'Lobby'in source.parts:continue
 meta=Path(str(source)+'.meta');guid=re.search(r'guid: (\w+)',meta.read_text()).group(1);entry='assets/bin/Data/'+guid
 if entry not in old.namelist():continue
 assert entry in new.namelist(),'Existing table texture removed: '+str(source)
 a=settings(old,entry);b=settings(new,entry)
 if a!=b:differences.append({'file':source.relative_to(R).as_posix(),'old':a,'new':b})
 else:verified.append(source.relative_to(R).as_posix())
out=R/'build/lobby-reference-1013/table-texture-comparison.json';out.write_text(json.dumps({'identical':verified,'differences':differences},ensure_ascii=False,indent=2),encoding='utf8')
assert not differences,'Packaged table texture settings changed: '+', '.join(d['file']for d in differences)
print('PASS:',len(verified),'existing PNG texture dimensions, formats, mipmaps, wrapping and colour space match the actual 0.5.39 APK')
