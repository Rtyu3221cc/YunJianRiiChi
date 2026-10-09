"""Import original win textures, particle curves and the Ichihime-compatible Zimo clip.

Texture sources prefer the supplied APK. Missing original files use the official
regional CDN. The Unity runtime reads baked local data and needs no network.
"""
import hashlib, importlib.util, json, sys, urllib.parse, urllib.request, warnings, zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent
OUT=ROOT/'unity/Assets/Resources/Majsoul/Actions'
CACHE=ROOT/'build/reference/win-study';CACHE.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(ROOT/'tools/asset-inspector'))
import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
rows=[json.loads(s) for s in (ROOT/'build/reference/fast-catalog.jsonl').read_text(encoding='utf8').splitlines()]
records=[]
def digest(data):return hashlib.sha256(data).hexdigest()
def fetch(path):
    dest=CACHE/Path(path).name
    url='https://mahjongsoul.game.yo-star.com/v0.10.1.w/scene/'+urllib.parse.quote(path)
    if not dest.exists():dest.write_bytes(urllib.request.urlopen(url,timeout=20).read())
    data=dest.read_bytes();records.append({'source':url,'sha256':digest(data)})
    return data
if not (CACHE/'effect_hupai_default.lh').exists():
    try:(CACHE/'effect_hupai_default.lh').write_bytes(urllib.request.urlopen('https://game.maj-soul.com/1/v0.10.1.w/scene/effect_hupai_default.lh',timeout=20).read())
    except OSError:fetch('effect_hupai_default.lh')
source=json.loads((CACHE/'effect_hupai_default.lh').read_bytes())
records.append({'source':'https://game.maj-soul.com/1/v0.10.1.w/scene/effect_hupai_default.lh','sha256':digest((CACHE/'effect_hupai_default.lh').read_bytes())})
materials={};layers=[]
def keys(items):return [{'time':v['key'],'value':v['value']} for v in items]
with zipfile.ZipFile(ROOT/'dist/Majsoul_4.0.16_MC.apk') as archive:
    def walk(node,position=(0,0,0)):
        cp=node.get('customProps',{});translate=cp.get('translate',[0,0,0]);position=[a+b for a,b in zip(position,translate)]
        if node['type']=='ShuriKenParticle3D':
            mp=cp['material']['path'];stem=Path(mp).stem
            if mp not in materials:
                mat=json.loads(fetch(mp))['props'];tp=mat['textures'][0]['path'];texture_name=Path(tp).name
                row=next((r for r in rows if any(p.lower().endswith('/'+texture_name.lower()) for p in r['paths'])),None)
                output=OUT/('win-'+stem+'.png')
                if row:
                    path=next(p for p in row['paths'] if p.lower().endswith('/'+texture_name.lower()))
                    env=UnityPy.load(archive.read(row['bundle']));obj=env.container[path];obj.read().image.save(output)
                    records.append({'output':output.name,'source':'User-provided Majsoul_4.0.16_MC.apk','path':path,'bundle':row['bundle'],'pathID':obj.path_id,'sha256':digest(output.read_bytes())})
                else:
                    raw=fetch(str(Path(mp).parent/tp).replace('\\','/'));output.write_bytes(raw)
                tint=next(v['value'] for v in mat['vectors'] if v['name']=='tintColor')
                assert mat['srcBlend']==770 and mat['dstBlend']==1 and not mat['depthWrite']
                materials[mp]={'texture':'win-'+stem,'tint':tint}
            life=cp['startLifetimeConstant'];size=cp['startSizeConstant'];speed=cp['startSpeedConstant']
            layer=dict(name=node['props']['name'],position=[position[0],position[1],-position[2]],
                rotation=cp['rotation'],duration=cp['duration'],delay=cp['startDelay'],
                lifeMin=cp['startLifetimeConstantMin'] if cp['startLifetimeType']==2 else life,lifeMax=cp['startLifetimeConstantMax'] if cp['startLifetimeType']==2 else life,
                sizeMin=cp['startSizeConstantMin'] if cp['startSizeType']==2 else size,sizeMax=cp['startSizeConstantMax'] if cp['startSizeType']==2 else size,
                speedMin=cp['startSpeedConstantMin'] if cp['startSpeedType']==2 else speed,speedMax=cp['startSpeedConstantMax'] if cp['startSpeedType']==2 else speed,
                rotationMin=cp['startRotationConstantMin'] if cp['startRotationType']==2 else cp['startRotationConstant'],rotationMax=cp['startRotationConstantMax'] if cp['startRotationType']==2 else cp['startRotationConstant'],
                color=cp['startColorConstant'],gravity=cp['gravityModifier'],maxParticles=cp['maxParticles'],seed=cp['randomSeed'],rate=cp['emission']['emissionRate'],
                bursts=[{'time':b['time'],'min':b['min'],'max':b['max']}for b in cp['emission']['bursts']],renderMode=cp['renderMode'],stretch=cp['stretchedBillboardLengthScale'],
                texture=materials[mp]['texture'],tint=materials[mp]['tint'],alpha=[],size=[])
            if cp.get('colorOverLifetime',{}).get('enable'):
                gradient=cp['colorOverLifetime']['color']['gradient'];assert all(x['value']==[1,1,1] for x in gradient['rgbs']);layer['alpha']=keys(gradient['alphas'])
            if cp.get('sizeOverLifetime',{}).get('enable'):layer['size']=keys(cp['sizeOverLifetime']['size']['gradient']['sizes'])
            shape=cp.get('shape',{});layer.update(shape=shape.get('shapeType',-1),radius=shape.get('sphereRadius',.01),coneAngle=shape.get('coneAngle',0))
            sheet=cp.get('textureSheetAnimation',{});layer['tiles']=sheet.get('tiles',[1,1])
            layers.append(layer)
        for child in node.get('child',[]):walk(child,position)
    walk(source)
assert len(layers)==7,len(layers)
(OUT/'win-effect.json').write_bytes((json.dumps({'layers':layers},indent=2)+'\n').encode('utf8'))
spec=importlib.util.spec_from_file_location('hand_baker',ROOT/'scripts/import-hand-animations.py');baker=importlib.util.module_from_spec(spec);spec.loader.exec_module(baker)
def cached_hand(path):
    name=Path(path).name;p=ROOT/'build/reference/hand-study'/name
    if not p.exists():p=CACHE/name
    if not p.exists():return fetch('Assets/Resource/table/hand/'+name)
    return p.read_bytes()
baker.fetch=cached_hand
rig=baker.load_rig();data,payload,land,tp=baker.bake(rig,'Dapai_Ani-Dapai.lani','node_tile')
assert payload==(OUT/'discard-hand-frames.bytes').read_bytes(),'Existing hand self-check failed'
data,payload,land,tp=baker.bake(rig,'Zimo_Anim-Zimo.lani','node_tile');data['landTime']=land
assert land>0 and land<data['duration'],'Zimo settle frame not found'
(OUT/'tsumo-hand.json').write_bytes(json.dumps(data,separators=(',',':')).encode())
(OUT/'tsumo-hand-frames.bytes').write_bytes(payload)
records.append({'source':'https://mahjongsoul.game.yo-star.com/v0.10.1.w/scene/Assets/Resource/table/hand/Zimo_Anim-Zimo.lani','sha256':digest((CACHE/'Zimo_Anim-Zimo.lani').read_bytes()),'duration':data['duration'],'landTime':land,'frames':data['frames']})
for p in [OUT/'win-effect.json',OUT/'tsumo-hand.json',OUT/'tsumo-hand-frames.bytes',*OUT.glob('win-*.png')]:records.append({'output':p.name,'sha256':digest(p.read_bytes())})
records.append({'portNotes':'Original seven-layer default win effect, textures, colors, bursts, lifetimes and alpha/size curves. Native Unity particles and additive shader adapt plane orientation and scale (2 source units per table unit). The result view retains the public board while win calls/effects play. Zimo uses the original curve and existing original hand rig, aligned to the winning tile.'})
(OUT/'win-source.json').write_bytes((json.dumps(records,ensure_ascii=False,indent=2)+'\n').encode('utf8'))
print('Imported',len(layers),'default win particle layers; Zimo',data['duration'],'seconds, settle',land)
