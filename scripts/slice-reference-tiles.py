from PIL import Image
from pathlib import Path
import json, sys
im=Image.open(sys.argv[1] if len(sys.argv)>1 else 'build/reference/default_hand.png')
rows={'s':0,'m':1,'p':2,'z':3};out=Path('app/src/main/assets/tiles')
out.mkdir(parents=True, exist_ok=True)
if im.size != (800,516): raise ValueError('Expected an 800 x 516 tile atlas')
meta=[]
for suit,row in rows.items():
 for n in (range(1,8) if suit=='z' else range(10)):
  col=n-1 if suit=='z' else n
  rect=(col*80,row*129,(col+1)*80,(row+1)*129)
  im.crop(rect).save(out/f'{suit}{n}.png')
  meta.append({'tile':f'{suit}{n}','rectangle':rect})
Path('app/src/main/assets/tile-assets.json').write_text(json.dumps({'source_apk':'Majsoul_4.0.16_MC.apk','source_texture':'myassets/deco/mjpface/mjpface_default/3d/texture/hand.png','source_size':[800,516],'tile_size':[80,129],'tiles':meta},indent=2),encoding='utf8')
print('Exported',len(meta),'tiles at 80 x 129 pixels')
