from pathlib import Path
import runpy,json
R=Path(__file__).resolve().parent.parent;S=runpy.run_path(str(R/'scripts/study-characters-1018.py'));out=R/'build/character-review-r2'
for name in ['ui_fightbegin','ui_desktopinfos','ui_desktopinfo']:
 ps=[p for p in S['paths']if p.endswith('/'+name+'.prefab')]
 for p in ps:
  nodes=list(S['tree'](S['obj'](p,'GameObject')));(out/(name+'.json')).write_text(json.dumps(nodes,ensure_ascii=False,indent=2),encoding='utf8')
  for n in nodes:
   if n['path'].endswith('/body')or n['path'].endswith('/father/illust'):
    print(n['path'],n['transform'],[(c['kind'],{k:v for k,v in c['data'].items()if k in ['asset_path','fix_mode','fixMode']})for c in n['components']])
t=(out/'code-readable.js').read_text('utf8');i=t.find('skinPoses');part=t[i-20000:i];(out/'carousel-helper-source.txt').write_text(part,encoding='utf8')
for term in ['EIllustType','setIllustType','setSkin','charaPart','getSkinData']:
 start=0
 while (j:=part.find(term,start))>=0:
  print(term,part[max(0,j-120):j+230]);start=j+len(term)
