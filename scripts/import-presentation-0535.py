"""Export native AL/HuleShow UI parameters and localized victory title from APK."""
from pathlib import Path
import json,sys
R=Path(__file__).resolve().parent.parent
exec((R/'scripts/import-review-1012c.py').read_text(encoding='utf8').split("cut=find(")[0])
OUT=R/'unity/Assets/Resources/Majsoul/Presentation0535';OUT.mkdir(exist_ok=True);records=[]
for name in ['ui_al','ui_huleshow','ui_fightbegin']:
 go=find('myassets/ui/mj/main/prefab_output/'+name+'.prefab');nodes=[]
 for g,t,objs,path in tree(go):
  td=t.read_typetree();node={'path':path,'position':vec(td.get('m_AnchoredPosition',td.get('m_LocalPosition'))),'scale':vec(td['m_LocalScale'])}
  if 'm_SizeDelta' in td:node.update(size=vec(td['m_SizeDelta']),pivot=vec(td['m_Pivot']))
  node['components']=[o.read_typetree() for o in objs if o.type.name=='MonoBehaviour'];nodes.append(node)
 js(name+'-layout.json',{'nodes':nodes},go)
go=find('myassets/scenes/mj/effect_mj/locallize/chs/prefab/winlose_chs.prefab');nodes=[];clips=[]
for g,t,objs,path in tree(go):
 td=t.read_typetree();nodes.append({'path':path,'position':vec(td['m_LocalPosition']),'scale':vec(td['m_LocalScale'])})
 for o in objs:
  if o.type.name=='Animation':
   d=o.read_typetree()
   for p in [d.get('m_Animation',{'m_PathID':0}),*d.get('m_Animations',[])]:
    a=resolve(o.assets_file,p)
    if a and a.type.name=='AnimationClip':clips.append(a.read_typetree())
js('winlose-layout.json',{'nodes':nodes,'clips':clips},go)
needed={'al','show_zimo','show_rong'};found=set()
for row in rows:
 if not any('atlas_mj_main_common_' in p or 'atlas_mj_main_chs_0' in p for p in row['paths']):continue
 for o in af(row['bundle']).objects.values():
  if o.type.name=='Sprite' and o.peek_name() in needed and o.peek_name() not in found:png(o.peek_name(),o);found.add(o.peek_name())
assert needed<=found
row=next(r for r in rows if 'myassets/scenes/mj/effect_mj/locallize/chs/texture/winlose.png' in r['paths'])
texture=next(o for o in af(row['bundle']).objects.values() if o.type.name=='Texture2D' and o.peek_name()=='winlose');png('winlose',texture)
victory=json.loads((R/'unity/Assets/Resources/Majsoul/UiEffects/effect_winlose.json').read_text(encoding='utf8'));victory['layers']=[x for x in victory['layers'] if x['texture']!='native-ui-winlose'];js('victory-particles.json',victory)
js('source.json',{'apk':'Majsoul_4.0.16_MC.apk','assets':records,'limitations':['UI component parameters and serialized artwork are original APK data. Encrypted Lua is not claimed as recovered readable source. Victory letters are rendered separately from APK petals, with shrink timing adapted to the supplied recording.']})
print('Imported AL/HuleShow/FightBegin layouts, AL/ron/tsumo/victory textures. Winlose clips:',[c['m_Name'] for c in clips])
