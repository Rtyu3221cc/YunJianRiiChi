"""Resolve LiQiPai timing exactly as the original ModelAnimationController does."""
from pathlib import Path
import hashlib,json
R=Path(__file__).resolve().parent.parent
source=R/'build/wardrobe-r6-study/tables.json';tables=json.loads(source.read_text('utf8'))
catalog=R/'unity/Assets/Resources/Majsoul/Wardrobe/catalog.json';data=json.loads(catalog.read_text('utf8'));records=[]
types={'hand-human':'hand_human','hand-cat':'hand_cat_orange','hand-cat-orange':'hand_cat_orange','hand-dog':'hand_dog','hand-man':'hand_man'}
for item in data['items']:
    if item['category']!='hand':continue
    row=next(r for r in tables['animation/animation']if r['type']==types[item['id']]and r['name']=='LiQiPai');speed=row['speed']
    item.update(riichiSpeed=speed,riichiLifetime=row['lifetime']/speed/1000,riichiCue=row['keypoint'][0][0]/speed/1000)
    records.append(dict(id=item['id'],source=row,resolved={k:item[k]for k in ['riichiSpeed','riichiLifetime','riichiCue']}))
catalog.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n','utf8')
(catalog.parent/'provenance-r7.json').write_text(json.dumps(dict(source=source.relative_to(R).as_posix(),sourceSHA256=hashlib.sha256(source.read_bytes()).hexdigest(),rule='ModelAnimationController.init_data divides lifetime and keypoints by speed',records=records),ensure_ascii=False,indent=2)+'\n','utf8')
print('R7 original timing resolution PASS',len(records),'hand types')
