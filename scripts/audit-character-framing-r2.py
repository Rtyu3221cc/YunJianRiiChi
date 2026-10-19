from pathlib import Path
import json,math
R=Path(__file__).resolve().parent.parent;d=json.loads((R/'unity/Assets/Resources/Majsoul/Characters/framing.json').read_text('utf8'))
for s in d['skins']:
 if not s['placements']:print('Legacy',s['id'],s['full'],s['head']);assert s['full'][2]>0 and s['head'][2]>0
 for p in s['placements']:assert len(p['rect'])==4 and all(math.isfinite(v)for v in p['rect'])and p['rect'][2]>0 and p['rect'][3]>0
print('All framing records have native placements or usable legacy crop/head coordinates')
