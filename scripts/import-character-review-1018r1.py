"""Native character UI refinements, source motion and non-table cherry petals."""
from pathlib import Path
import runpy,json,io,hashlib,struct,math,zlib
from PIL import Image,ImageFilter,ImageChops
R=Path(__file__).resolve().parent.parent;S=runpy.run_path(str(R/'scripts/study-characters-1018.py'));af=S['af'];obj=S['obj'];resolve=S['resolve'];paths=S['paths']
O=R/'unity/Assets/Resources/Majsoul/Characters';E=R/'build/character-review-r1';E.mkdir(exist_ok=True,parents=True)
catalog=json.loads((O/'catalog.json').read_text('utf8'));records=[]
def png(key,o,logical=True):
 im=o.read().image
 if logical and o.type.name=='Sprite':
  d=o.read_typetree();rd=d['m_RD'];r=d['m_Rect'];off=rd['textureRectOffset'];canvas=Image.new('RGBA',(round(r['width']),round(r['height'])));canvas.paste(im,(round(off['x']),round(r['height']-off['y']-im.height)));im=canvas
 b=io.BytesIO();im.save(b,format='PNG');p=O/(key+'.png');p.parent.mkdir(exist_ok=True,parents=True);p.write_bytes(b.getvalue());records.append(dict(output=key+'.png',bundle=S['cab'][o.assets_file.name],pathID=o.path_id,sha256=hashlib.sha256(b.getvalue()).hexdigest()))
 if key.startswith('UI/'):
  d=o.read_typetree();r=dict(key=key[3:],width=im.width,height=im.height,border=[d['m_Border'][x]for x in 'xyzw']);catalog['ui']=[x for x in catalog['ui']if x['key']!=r['key']]+[r]
 return im
seen=set()
for p,b in paths.items():
 if b in seen or not any(x in p for x in ['/liaoshe/','/liaoshe_extra/','/ui/common/main/pic/','/ui/lobby/main/pic/']):continue
 if '/pic/'not in p or any('/'+x+'/'in p for x in ['en','jp','kr','chs_t']):continue
 seen.add(b)
 for o in af(b).objects.values():
  if o.type.name=='Sprite'and(o.peek_name().startswith('btn_anim_2')or o.peek_name()in ['bf_chooesd','bf_chosen','sushe_favor_bottom','bf_title_bg','button_gray','name_qy']):png('UI/'+o.peek_name(),o)
# The glow follows the native mask's silhouette, including the protruding nameplate.
mask=Image.open(O/'UI/sushe_click_effect.png').getchannel('A');pad=8;m=Image.new('L',(mask.width+pad*2,mask.height+pad*2));m.paste(mask,(pad,pad));edge=ImageChops.subtract(m.filter(ImageFilter.MaxFilter(5)),m);glow=ImageChops.subtract(m.filter(ImageFilter.GaussianBlur(2.8)),m);a=ImageChops.lighter(edge,glow.point(lambda x:min(255,x*3)));im=Image.new('RGBA',m.size,(0,244,255));im.putalpha(a);im.save(O/'UI/dorm-contour-glow.png')
catalog['ui']=[x for x in catalog['ui']if x['key']!='dorm-contour-glow']+[dict(key='dorm-contour-glow',width=im.width,height=im.height,border=[0,0,0,0])]
records.append(dict(output='UI/dorm-contour-glow.png',source='APK sushe_click_effect alpha silhouette',method='outer 2px contour and soft outward glow',sha256=hashlib.sha256((O/'UI/dorm-contour-glow.png').read_bytes()).hexdigest()))
for source,name in [('myassets/necessary/texture/yinghua.png','petals'),('myassets/necessary/texture/yinghua2.png','petals-soft')]:png('Effects/'+name,obj(source,'Texture2D'),False)
motion=[]
for name in ['ui_liaosheselect_in','ui_liaosheselect_out']:
 path='myassets/ui/lobby/extend/liaoshe/anim/'+name+'.anim';o=obj(path,'AnimationClip');d=o.read_typetree();(E/(name+'.json')).write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf8');mc=d['m_MuscleClip'];c=mc['m_Clip']['data'];s=c['m_StreamedClip'];raw=struct.pack('<'+'I'*len(s['data']),*s['data']);pos=0;keys={}
 while pos<len(raw):
  time,n=struct.unpack_from('<fI',raw,pos);pos+=8
  for _ in range(n):
   idx,a,b,cc,v=struct.unpack_from('<Iffff',raw,pos);pos+=20
   if math.isfinite(time)and time>=0:keys.setdefault(idx,[]).append((time,a,b,cc,v))
 tracks=[];idx=0;duration=mc['m_StopTime']
 for binding in d['m_ClipBindingConstant']['genericBindings']:
  count=3 if binding['typeID']==4 and binding['attribute']in [1,3]else 4 if binding['typeID']==4 and binding['attribute']==2 else 1
  for part in range(count):
   values=[];ks=keys.get(idx,[])
   for frame in range(round(duration*60)+1):
    t=min(duration,frame/60)
    if ks:
     k=next((k for k in reversed(ks)if k[0]<=t),ks[0]);dt=max(0,t-k[0]);v=((k[1]*dt+k[2])*dt+k[3])*dt+k[4]
    else:
     const=idx-s['curveCount']-c['m_DenseClip']['m_CurveCount'];v=c['m_ConstantClip']['data'][const]if const>=0 else 0
    values.append(round(v,6))
   tracks.append(dict(path=binding['path'],attribute=binding['attribute'],part=part,values=values));idx+=1
 motion.append(dict(name=name,duration=duration,tracks=tracks))
for name in ['scene_lobby','yinghua','yinghua3']:
 path=next(p for p in paths if p.endswith('/'+name+'.prefab')and('/scenes/lobby/'in p or'/scenes/entrance/'in p));o=obj(path,'GameObject');nodes=list(S['tree'](o));(E/(name+'-nodes.json')).write_text(json.dumps(nodes,ensure_ascii=False,indent=2),encoding='utf8')
(O/'review-motion.json').write_text(json.dumps(dict(animations=motion),indent=2),encoding='utf8');(O/'catalog.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n',encoding='utf8');(O/'review-r1-source.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf8')
print('Motion',[(m['name'],m['duration'],[(t['path'],t['attribute'],t['part'],t['values'][0],t['values'][-1])for t in m['tracks']])for m in motion]);print('Imported plaque sprites',[x['key']for x in catalog['ui']if x['key'].startswith('btn_anim_2')])
