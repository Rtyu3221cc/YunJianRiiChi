"""Import the original Majsoul score-hub indicator texture and particle evidence.

Requires the user-provided APK, UnityPy under tools/asset-inspector, and the
existing build/reference/fast-catalog.jsonl. Runtime uses only the outputs.
"""
import hashlib,json,sys,urllib.request,warnings,zipfile
from pathlib import Path

ROOT=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(ROOT/'tools/asset-inspector'))
import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1'
warnings.filterwarnings('ignore')
OUT=ROOT/'unity/Assets/Resources/Majsoul/Hud'
CACHE=ROOT/'build/reference/turn-light'
CACHE.mkdir(parents=True,exist_ok=True)
rows=[json.loads(s) for s in (ROOT/'build/reference/fast-catalog.jsonl').read_text(encoding='utf8').splitlines()]
apk=zipfile.ZipFile(ROOT/'dist/Majsoul_4.0.16_MC.apk')

def bundle(suffix):
    row=next(r for r in rows if any(p.endswith(suffix) for p in r['paths']))
    return row,UnityPy.load(apk.read(row['bundle']))

texture_row,env=bundle('/particle_shining.png')
texture=next(o.read() for o in env.objects if o.type.name=='Texture2D' and o.peek_name()=='particle_shining')
texture.image.save(OUT/'turn-light.png')
prefab_row,env=bundle('/scene_mj.prefab')
objects={o.path_id:o for o in env.objects}
data={o.path_id:o.read_typetree() for o in env.objects if o.type.name in ['GameObject','Transform']}
ind=next(t for pid,t in data.items() if objects[pid].type.name=='Transform'
    and data[t['m_GameObject']['m_PathID']]['m_Name']=='ind' and abs(t['m_LocalRotation']['x'])>.7)
layers=[]
for child in ind['m_Children']:
    transform=data[child['m_PathID']]
    go=data[transform['m_GameObject']['m_PathID']]
    components=[objects[c['component']['m_PathID']] for c in go['m_Component']]
    particle=next(o.read_typetree() for o in components if o.type.name=='ParticleSystem')
    renderer=next(o.read_typetree() for o in components if o.type.name=='ParticleSystemRenderer')
    initial=particle['InitialModule'];gradient=particle['ColorModule']['gradient']['maxGradient']
    layers.append({'name':go['m_Name'],'transform':{k:transform[k] for k in ['m_LocalPosition','m_LocalRotation','m_LocalScale']},
        'duration':particle['lengthInSec'],'loop':particle['looping'],'playOnAwake':particle['playOnAwake'],
        'lifetime':initial['startLifetime']['scalar'],'speed':initial['startSpeed']['scalar'],
        'size':initial['startSize']['scalar'],'sizeY':initial['startSizeY']['scalar'],
        'color':initial['startColor']['maxColor'],
        'alphaKeys':[{'time':gradient['atime'+str(i)]/65535,'alpha':gradient['key'+str(i)]['a']} for i in range(gradient['m_NumAlphaKeys'])],
        'emissionRate':particle['EmissionModule']['rateOverTime']['scalar'],
        'burst':{'time':0,'count':particle['EmissionModule']['m_Bursts'][0]['countCurve']['scalar']},
        'renderer':{'mode':renderer['m_RenderMode'],'flip':renderer['m_Flip'],'materials':renderer['m_Materials']}})

material_url='https://game.maj-soul.com/1/v0.10.237.w/scene/Assets/Resource/effect/texture/player_indicator/particle_shining.lmat'
cached=CACHE/'particle_shining.lmat'
if not cached.exists():cached.write_bytes(urllib.request.urlopen(material_url,timeout=30).read())
source={'source':'User-provided Majsoul_4.0.16_MC.apk',
    'prefab':{'path':'myassets/scenes/mj/mjtable/prefab/scene_mj.prefab / man/ind','bundle':prefab_row['bundle']},
    'texture':{'path':'myassets/scenes/mj/mjtable/texture/particle_shining.png','bundle':texture_row['bundle'],
        'width':texture.m_Width,'height':texture.m_Height,'sha256':hashlib.sha256((OUT/'turn-light.png').read_bytes()).hexdigest()},
    'layers':layers,'webMaterial':{'url':material_url,'data':json.loads(cached.read_bytes())},
    'portNotes':'Original particle texture, two start colors, 1-second burst/lifetime, alpha keys and additive blend. The horizontal billboard is represented by a table-plane mesh particle; pose/size are adapted to the existing normalized hub (center 128,216; quad 84x66). Polling preserves the particle cycle while seat and round are unchanged.'}
(OUT/'turn-light-source.json').write_bytes((json.dumps(source,ensure_ascii=False,indent=2)+'\n').encode('utf8'))
print('Imported original indicator:',texture.image.size,'two particle layers, alpha keys',layers[0]['alphaKeys'])
