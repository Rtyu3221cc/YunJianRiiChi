"""Import the original sakura flutter atlas from the user's reference APK.

Uses the existing local catalog and UnityPy in tools/asset-inspector.
The North placement burst is an adaptation, not the lobby's looping emitter.
"""
import hashlib, json, sys, warnings, zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / 'tools/asset-inspector'))
import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION = '2022.3.62f2c1'
warnings.filterwarnings('ignore')
OUT = ROOT / 'unity/Assets/Resources/Majsoul/Actions'
source = 'myassets/necessary/texture/yinghua2.png'
rows = [json.loads(s) for s in (ROOT / 'build/reference/fast-catalog.jsonl').read_text(encoding='utf8').splitlines()]
row = next(r for r in rows if source in r['paths'])
with zipfile.ZipFile(ROOT / 'dist/Majsoul_4.0.16_MC.apk') as archive:
    env = UnityPy.load(archive.read(row['bundle']))
    obj = env.container[source]
    texture = obj.read()
    texture.image.save(OUT / 'north-petals.png')
record = {
    'source': 'User-provided Majsoul_4.0.16_MC.apk', 'path': source,
    'bundle': row['bundle'], 'pathID': obj.path_id,
    'width': texture.m_Width, 'height': texture.m_Height, 'frames': [4, 4],
    'sha256': hashlib.sha256((OUT / 'north-petals.png').read_bytes()).hexdigest(),
    'adaptation': 'Original RGBA flutter atlas. North landing uses a nine-petal, non-looping 1.1-1.5 second burst, upward drift, alpha fade and camera-facing billboards. Placement targets the event-specific extracted North tile. Cut-in reuses yiji-full and shout_babei; hand reuses the baked Fulu clip with its settle point aligned to North.'
}
(OUT / 'north-effect-source.json').write_bytes((json.dumps(record, ensure_ascii=False, indent=2) + '\n').encode('utf8'))
print('Imported sakura atlas:', texture.m_Width, texture.m_Height, record['sha256'])
