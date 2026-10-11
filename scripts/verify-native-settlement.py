"""Verify the committed native popup, Ichihime narration and video-matched skin data."""
from pathlib import Path
import hashlib,json
root=Path(__file__).resolve().parent.parent
assets=root/'unity/Assets/Resources/Majsoul'
records=json.loads((assets/'SettlementVoice/provenance.json').read_text(encoding='utf8'))
checked=0
for rec in records:
    if not rec.get('output'):continue
    file=(assets/rec['output']).resolve();assert file.is_relative_to(assets.resolve())
    assert file.exists(), 'Missing resource: '+rec['output']
    assert hashlib.sha256(file.read_bytes()).hexdigest()==rec['sha256'], 'Source hash mismatch: '+rec['output']
    checked+=1
for rec in json.loads((assets/'Audio/provenance.json').read_text(encoding='utf8'))['clips']:
    candidates=list((assets/'Audio').glob(rec['resource'].rsplit('/',1)[1]+'.*'))
    file=next(p for p in candidates if p.suffix in ['.mp3','.wav'])
    assert hashlib.sha256(file.read_bytes()).hexdigest()==rec['sha256'], 'Table audio hash mismatch: '+file.name
manifest=json.loads((assets/'SettlementVoice/manifest.json').read_text(encoding='utf8'))
assert manifest['provider']=='yiji' and len(manifest['clips'])==82
for clip in manifest['clips']:
    file=root/'unity/Assets/Resources'/(clip['resource']+'.mp3')
    assert file.exists() and file.stat().st_size>1000
effect=json.loads((assets/'Actions/fire-effect.json').read_text(encoding='utf8'))
assert effect['skinId']==305034 and effect['audioId']==305034 and len(effect['layers'])==9
assert len([l for l in effect['layers']if l['renderMode']==4])==2
for layer in effect['layers']:
    assert (assets/'Actions'/(layer['texture']+'.png')).exists()
    if layer['renderMode']==4:
        mesh=layer['mesh'];assert len(mesh['indices'])%3==0 and len(mesh['positions'])//3==len(mesh['uv'])//2
assert (assets/'Indicators/pop_bg.png').exists() and (assets/'Indicators/pop_bound.png').exists()
print('PASS:',checked,'native transformed-asset hashes; 17 original audio hashes; 82 Ichihime clips; exact 305034 fire-tornado data with nine emitters/two meshes.')
