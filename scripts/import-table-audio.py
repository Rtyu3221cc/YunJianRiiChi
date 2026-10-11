"""Reproduce the bundled native table audio; no downloads occur in the game."""
from pathlib import Path
import argparse,sys,json,hashlib,urllib.request,zipfile,warnings
root=Path(__file__).resolve().parent.parent
parser=argparse.ArgumentParser();parser.add_argument('--apk',type=Path,default=root/'dist/Majsoul_4.0.16_MC.apk');args=parser.parse_args()
sys.path.insert(0,str(root/'tools/asset-inspector'));import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
out=root/'unity/Assets/Resources/Majsoul/Audio';manifest=json.loads((out/'provenance.json').read_text(encoding='utf8'))
records=manifest['clips'];bundle=next(r['apkBundle'] for r in records if 'apkBundle' in r)
with zipfile.ZipFile(args.apk) as archive:env=UnityPy.load(archive.read(bundle))
for record in records:
 name=record['resource'].rsplit('/',1)[1]
 if 'apkBundle' in record:
  obj=next(o for o in env.objects if o.type.name=='AudioClip' and o.read().m_Name==name)
  samples=obj.read().samples;assert len(samples)==1
  filename,data=next(iter(samples.items()))
 else:
  filename=name+'.mp3';file=out/filename
  if file.exists() and hashlib.sha256(file.read_bytes()).hexdigest()==record['sha256']:continue
  with urllib.request.urlopen(record['source'],timeout=40) as response:data=response.read(15000000)
 assert hashlib.sha256(data).hexdigest()==record['sha256'], 'Resource version/hash differs: '+name
 (out/filename).write_bytes(data);print('Imported',filename,len(data))
print('Verified five APK table effects and official video-matched game BGM.')
