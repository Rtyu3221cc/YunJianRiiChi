from pathlib import Path
import json,math,runpy
R=Path(__file__).resolve().parent.parent;O=R/'build/character-review-r1';O.mkdir(parents=True,exist_ok=True)
S=R/'build/characters-1018'
def rects(nodes,width=2374):
 states={};result={}
 for n in nodes:
  p=n['path'];t=n['transform'];parent=p.rsplit('/',1)[0]
  if parent not in states:
   state=dict(px=width/2,py=540,w=width,h=1080,sx=1,sy=1,pvx=.5,pvy=.5)
  else:
   par=states[parent];a=t.get('m_AnchorMin',dict(x=.5,y=.5));b=t.get('m_AnchorMax',a);pv=t.get('m_Pivot',dict(x=.5,y=.5));d=t.get('m_SizeDelta',dict(x=0,y=0));pos=t.get('m_AnchoredPosition',t.get('m_LocalPosition',dict(x=0,y=0)));sc=t['m_LocalScale']
   w=d['x']+(b['x']-a['x'])*par['w'];h=d['y']+(b['y']-a['y'])*par['h']
   x=pos['x']+par['w']*(a['x']+(b['x']-a['x'])*pv['x']-par['pvx']);y=pos['y']+par['h']*(a['y']+(b['y']-a['y'])*pv['y']-par['pvy'])
   state=dict(px=par['px']+x*par['sx'],py=par['py']+y*par['sy'],w=w,h=h,sx=par['sx']*sc['x'],sy=par['sy']*sc['y'],pvx=pv['x'],pvy=pv['y'])
  states[p]=state;q=state;x0=q['px']-q['pvx']*q['w']*q['sx'];y0=q['py']-q['pvy']*q['h']*q['sy'];x1=x0+q['w']*q['sx'];y1=y0+q['h']*q['sy']
  result[p]=[round(min(x0,x1),3),round(1080-max(y0,y1),3),round(abs(x1-x0),3),round(abs(y1-y0),3)]
 return result
out={};brief=[]
for name in ['ui_liaoshemain','ui_liaosheselect','ui_visit','ui_liaoshechangeskin','ui_dormsortcharacter']:
 nodes=json.loads((S/(name+'.json')).read_text('utf8'));a=rects(nodes);b=rects(nodes,2474);out[name]=[];brief.append('\n'+name)
 for n in nodes:
  p=n['path'];components=[]
  for c in n['components']:
   d=c['data'];font=d.get('m_FontData',{});entry=dict(kind=c['kind'])
   for k in ['asset_path','m_Text','m_Color','click_bigger','alpha_start','alpha_end','anims','m_Sprite','m_Interactable']:
    if k in d:entry[k]=d[k]
   if font:entry['font']=font
   if len(entry)>1:components.append(entry)
  r=dict(path=p,rect=a[p],widthSlope=[round((b[p][i]-a[p][i])/100,3)for i in range(4)],active=n['active'],components=components)
  out[name].append(r)
  # A compact native layout inventory: no giant prefab dump.
  if p.count('/')<=3 or components and any(c.get('asset_path')or 'm_Text'in c for c in components)and not any(x in p for x in ['page_deco','gift','reward','templete (','slot1','slot2','slot3','sound/','btn_emo','label_emo','icon_emo']):
   info=[]
   for c in components:
    if c.get('asset_path'):info.append(c['asset_path'])
    if 'm_Text'in c:info.append('TEXT size='+str(c.get('font',{}).get('m_FontSize'))+' '+repr(c['m_Text']))
   brief.append(p+' '+str(a[p])+' '+str(info))
(O/'native-layout-full.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8');(O/'native-layout.txt').write_text('\n'.join(brief),encoding='utf8')
print('Wrote native layout report',len(brief),'lines')
