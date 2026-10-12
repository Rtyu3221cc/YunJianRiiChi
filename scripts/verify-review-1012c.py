"""Verify newly recovered cut-in/FightBegin assets against their APK provenance."""
from pathlib import Path
import hashlib,json
root=Path(__file__).resolve().parent.parent
assets=root/'unity/Assets/Resources/Majsoul/Review1012c'
source=json.loads((assets/'source.json').read_text(encoding='utf8'))
for row in source['assets']:
 file=assets/row['output'];assert file.is_file() and hashlib.sha256(file.read_bytes()).hexdigest()==row['sha256'],row['output']
 assert row.get('bundle') and row.get('pathID'),row['output']
layout=json.loads((assets/'cutin-layout.json').read_text(encoding='utf8'))
assert len(layout['nodes'])==10
clip=json.loads((assets/'cutin-animation.json').read_text(encoding='utf8'))
assert {c['path']for c in clip['m_ScaleCurves']}=={'backline','frontline'}
assert abs(max(c['curve']['m_Curve'][-1]['time']for c in clip['m_ScaleCurves'])-1.2)<.0001
assert len(clip['m_FloatCurves'])==6
print('PASS:',len(source['assets']),'APK texture/layout/animation hashes; native cut-in 1.2-second curves; Queshi rank art.')
