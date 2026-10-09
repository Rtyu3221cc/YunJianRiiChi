"""Fetch the original Majsoul meld impact textures.

scene/effect_mingpai_default.lh wraps effect_chipeng, whose 'ci' particle uses
effect_fulu/bang and whose 'smoke' particle uses effect_fulu/smoke. Both materials
are additive (srcBlend 770 / dstBlend 1, depthWrite off), so the impact is a bright
flash plus smoke. Only the textures are needed here; the particle systems are
rebuilt with Unity components.
"""
import hashlib, json, urllib.parse, urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT/'unity/Assets/Resources/Majsoul/Actions'
CACHE = ROOT/'build/reference/hand-study'
PUBLIC = ROOT/'build/reference/public'
CACHE.mkdir(parents=True, exist_ok=True); PUBLIC.mkdir(parents=True, exist_ok=True)

INDEX_PATH = PUBLIC/'resversion0.11.252.w.json'
if not INDEX_PATH.exists() or INDEX_PATH.stat().st_size == 0:
    INDEX_PATH.write_bytes(urllib.request.urlopen('https://game.maj-soul.com/1/resversion0.11.252.w.json', timeout=120).read())
INDEX = json.loads(INDEX_PATH.read_text(encoding='utf8'))['res']

ASSETS = [
    ('scene/Assets/Resource/effect/texture/effect_fulu/bang.png', 'meld-bang.png'),
    ('scene/Assets/Resource/effect/texture/effect_fulu/smoke.png', 'meld-smoke.png'),
]
records = []
for path, name in ASSETS:
    dest = CACHE/Path(path).name
    if dest.exists() and dest.stat().st_size > 0:
        data = dest.read_bytes(); source = 'cached:'+dest.name
    else:
        last = None
        for version in list(dict.fromkeys([INDEX[path]['prefix'], 'v0.10.1.w'])):
            url = 'https://game.maj-soul.com/1/'+version+'/'+urllib.parse.quote(path)
            try:
                data = urllib.request.urlopen(url, timeout=60).read(); dest.write_bytes(data); source = url; break
            except urllib.error.HTTPError as error:
                last = error
                if error.code != 404: raise
        else:
            raise RuntimeError('Missing source: '+path) from last
    (OUT/name).write_bytes(data)
    records.append({'file': name, 'source': source, 'sha256': hashlib.sha256(data).hexdigest()})
    print('%-16s %6d B  %s' % (name, len(data), source))
(OUT/'meld-effect-provenance.json').write_text(json.dumps(records, ensure_ascii=False, indent=2), encoding='utf8')
print('wrote meld-effect-provenance.json')
