from pathlib import Path
import re
R=Path(__file__).resolve().parent.parent;O=R/'build/character-review-r2';t=(O/'code-readable.js').read_text('utf8')
sections=[]
for term in ['["_calculateIllust"]=function','["getSkinData"]=function','["charaPart"]=function','["getSkinOffset"]=function']:
 i=t.find(term);end=t.find('},s[',i+len(term));part=t[i:end+1] if end>=0 else t[i:i+7000];sections.append(part);print(part,'\n')
for term in ['EIllustType','skinList','UI_FightBegin']:
 hits=list(re.finditer(re.escape(term),t));print(term,len(hits))
 if term=='EIllustType':
  for m in hits:
   p=t[max(0,m.start()-120):m.end()+230]
   if any(x in p for x in ['="A','="B','="I','="J','sushe','desktopInfo','fightbegin']):print(p)
start=t.find('skinPoses');end=t.find('})(uiscript',start)
part=t[start:end] if end>start else t[start:start+16000];(O/'skin-carousel-source.txt').write_text(part,encoding='utf8')
(O/'framing-methods.txt').write_text('\n\n'.join(sections),encoding='utf8')
for term in ['["EIllustType"]["fightBegin"]','["itemRender"]=function']:
 if term=='["itemRender"]=function':
  i=part.find(term);print('CAROUSEL',part[i:i+4200])
 else:
  i=t.find(term);print('FIGHT',t[i-700:i+400])
