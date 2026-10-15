"""Add the original dark popup button without re-extracting existing art."""
from pathlib import Path
import json,hashlib
R=Path(__file__).resolve().parent.parent
scope={'__file__':str(R/'scripts/import-lobby-resources.py')}
exec((R/'scripts/import-lobby-resources.py').read_text('utf8').split('groups=')[0],scope)
out=scope['OUT'];provenance=json.loads((out/'provenance.json').read_text('utf8'))
base=next(r for r in provenance['assets']if r['output']=='Sprites/Necessary/button_light.png')
source=next(o for o in scope['af'](base['bundle']).objects.values()if o.type.name=='Sprite'and o.peek_name()=='button')
scope['png']('Sprites/Necessary/button',source)
record=scope['records'][0];provenance['assets']=[r for r in provenance['assets']if r['output']!=record['output']]+[record]
(out/'provenance.json').write_text(json.dumps(provenance,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
borderPath=out/'borders.json';borders=json.loads(borderPath.read_text('utf8'));key=record['output'][:-4]
borders['borders']=[r for r in borders['borders']if r['key']!=key]+[dict(key=key,values=record['border'])]
borderPath.write_text(json.dumps(borders,indent=2)+'\n',encoding='utf8')
provenance=json.loads((out/'provenance.json').read_text('utf8'))
for r in provenance['derived']:
    if r['output']=='borders.json':r['sha256']=hashlib.sha256(borderPath.read_bytes()).hexdigest()
(out/'provenance.json').write_text(json.dumps(provenance,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print('Imported original dark popup button; native border:',record['border'])
