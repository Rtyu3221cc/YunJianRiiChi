"""Extract only native lobby/room resources from the supplied Mahjong Soul APK.
No image is generated or taken from the reference video. Lua is retained as
encrypted evidence; the room transport is the project's offline LAN adapter.
"""
from pathlib import Path
import sys,json,zipfile,hashlib,io,warnings
R=Path(__file__).resolve().parent.parent
OUT=R/'unity/Assets/Resources/Majsoul/Lobby';OUT.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(R/'tools/asset-inspector'));import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
rows=[json.loads(l)for f in ['catalog.jsonl','fast-catalog.jsonl']for l in(R/'build/reference'/f).read_text(encoding='utf8').splitlines()]
cab=json.loads((R/'build/reference/tile-study/cab-map.json').read_text(encoding='utf8'))
z=zipfile.ZipFile(R/'dist/Majsoul_4.0.16_MC.apk');cache={};records=[];done=set()
def af(b):
 if b not in cache:cache[b]=next(o.assets_file for o in UnityPy.load(z.read(b)).objects)
 return cache[b]
def resolve(f,p):
 if not p or not p.get('m_PathID'):return None
 if p['m_FileID']:f=af(cab[Path(f.externals[p['m_FileID']-1].path).name])
 return f.objects[p['m_PathID']]
def write(n,data,o):
 (OUT/n).parent.mkdir(parents=True,exist_ok=True);(OUT/n).write_bytes(data)
 record=dict(output=n,sha256=hashlib.sha256(data).hexdigest(),bundle=cab[o.assets_file.name],pathID=o.path_id,type=o.type.name)
 if o.type.name=='Sprite':record['border']=[o.read_typetree()['m_Border'][k]for k in 'xyzw']
 records.append(record)
def js(n,d,o):write(n,(json.dumps(d,ensure_ascii=False,indent=2,default=str)+'\n').encode('utf8'),o)
def png(n,o):
 image=o.read().image;layout=None
 if n in ['Sprites/Main/frame_gold','Sprites/Main/character_bg']:
  # These tight sprites have different trimmed rectangles. Unity restores
  # their transparent offsets inside m_Rect when both use the same UI bounds.
  # Preserve that canvas instead of stretching the cropped pixels to the frame.
  from PIL import Image
  d=o.read_typetree();rect=d['m_Rect'];rd=d['m_RD'];offset=rd['textureRectOffset'];packed=rd['textureRect']
  canvas=Image.new('RGBA',(round(rect['width']),round(rect['height'])))
  origin=(round(offset['x']),round(rect['height']-offset['y']-packed['height']))
  canvas.paste(image,origin);image=canvas
  layout=dict(canvas=[rect['width'],rect['height']],origin=origin,textureRect=packed,textureRectOffset=offset)
 b=io.BytesIO();image.save(b,format='PNG');write(n+'.png',b.getvalue(),o)
 if layout:records[-1]['spriteLayout']=layout
def obj(path,typ=None):
 row=next(r for r in rows if path in r['paths']);f=af(row['bundle'])
 return next(o for o in f.objects.values()if (not typ or o.type.name==typ)and o.peek_name().lower()==Path(path).stem.lower())
groups={'/ui/lobby/main/pic/':'Main','/ui/lobby/extend/friend_room/pic/':'Room','/ui/lobby/extend/create_room/pic/':'Create','/ui/lobby/extend/number_input/pic/':'Number','/ui/common/main/pic/':'Common','/ui/necessary/pic/':'Necessary','/ui/common/extend/rules_room/pic/':'Rules','/extendres/rules_room/chs/':'RulePages'}
needed={
 'Main':set('btn_yibanchang btn_dajiangsai btn_yourenchang bg_yourenchang bg_back0 bg_2 bg_6 selfinfo title_bg character_bg img_return1_bg img_return1 img_return2 frame_gold btn_set btn_priscreen btn_roleset btn_rule_desc room_rule_btn btn_yi btn_close tips ready num num_pressed d_time btn0 btn1 btn2 btn4'.split()),
 'Room':set('roomowner room_player_card room_invite_btn room_info_line1 room_info_line2 room_info_bg room_info_advance_bg room_info_small_bg room_number_bg room_share_btn room_start_btn room_gray_btn room_start_btn_text room_prepare_btn room_prepare_text room_cancel_btn room_cancel_btn_text'.split()),
 'Create':set('room_create_match_text room_create_match_icon room_create_jushu_text room_create_jushu_icon room_create_time_text room_create_time_icon room_create_advance_text room_create_advance_icon room_rule_option_select room_rule_option_unselect room_rule_advance_line room_rule_advance_number_bg room_create_start_point_text room_create_point_text room_create_jifei_text room_create_hongbaopai_text room_create_guyi_text room_create_shiduan_text room_create_fanfu_text room_create_tip_text'.split()),
 'Common':set('bg_textinput pop_bound namebg what inputbox sanma_fish'.split()),
 'Necessary':set('pop_bg btn_close match_scroll_bg match_scroll_btn button_light bg frame decorate_left decorate_right bbg star starbg'.split()),
 'Number':{'back'},
 'Rules':set('room_rule_tab_small_select room_rule_tab_small_unselect room_rule_bg room_rule_bg_small room_rule_tab_select room_rule_tab_unselect'.split()),
 'RulePages':{'siren','sanren'}
}
for row in rows:
 group=next((g for p in row['paths']for k,g in groups.items()if k in p and not any('/'+lang+'/'in p for lang in ['chs_t','en','jp','kr'])),None)
 if not group or row['bundle']in done:continue
 done.add(row['bundle'])
 for o in af(row['bundle']).objects.values():
  if o.type.name=='Sprite'and o.peek_name()in needed[group]:png('Sprites/'+group+'/'+o.peek_name(),o)
for path,name in [('myassets/scenes/lobby/main/texture/yard.jpg','yard'),('myassets/scenes/lobby/main/texture/indoor.jpg','indoor'),('myassets/deco/character/yiji/full/full.png','yiji-full')]:
 png(name,obj(path,'Texture2D'))
for name in ['w_createroom','w_joinroom']:
 png('Sprites/Main/'+name,obj('myassets/ui/lobby/main/pic_scattered/chs_t/'+name+'.png','Texture2D'))
# Exact fonts referenced by the source Text components.
def subtree(go):
 yield go
 for cp in go.read_typetree()['m_Component']:
  o=resolve(go.assets_file,cp['component'])
  if o.type.name not in ['Transform','RectTransform']:continue
  for ptr in o.read_typetree()['m_Children']:
   tr=resolve(o.assets_file,ptr);yield from subtree(resolve(tr.assets_file,tr.read_typetree()['m_GameObject']))
for name,path,node in [('body','myassets/ui/lobby/extend/friend_room/prefab_output/ui_friendroom.prefab','label_name')]:
 root=obj(path,'GameObject');f=root.assets_file
 go=next(o for o in subtree(root)if o.peek_name()==node)
 found=False
 for cp in go.read_typetree()['m_Component']:
  o=resolve(f,cp['component'])
  if o.type.name!='MonoBehaviour':continue
  d=o.read_typetree()
  if 'm_FontData'in d:
    ft=resolve(o.assets_file,d['m_FontData']['m_Font']);fd=ft.read_typetree();data=bytes(fd['m_FontData']);body_hash=hashlib.sha256(data).hexdigest();assert body_hash==hashlib.sha256((R/'unity/Assets/Resources/Majsoul/Font/game.ttf').read_bytes()).hexdigest();found=True;break
 assert found,'Source font not found: '+name
fontrow=next(r for r in rows if 'myassets/fonts/chs/chs_fengyucumaokai.ttf'in r['paths'])
font=next(o for o in af(fontrow['bundle']).objects.values()if o.type.name=='Font'and o.peek_name()=='chs_fengyucumaokai')
write('Fonts/brush.ttf',bytes(font.read_typetree()['m_FontData']),font)
fontrow=next(r for r in rows if 'myassets/fonts/chs_t/fengyucumaokai.ttf'in r['paths'])
font=next(o for o in af(fontrow['bundle']).objects.values()if o.type.name=='Font'and o.peek_name()=='fengyucumaokai')
write('Fonts/room-footer.ttf',bytes(font.read_typetree()['m_FontData']),font)
# Typography and interaction settings are read from the original create-room
# components, not inferred from their labels in a screenshot.
root=obj('myassets/ui/lobby/main/prefab_output/ui_creatroom.prefab','GameObject')
number_text=None;toggle=None;number_button=None
for go in subtree(root):
 for cp in go.read_typetree()['m_Component']:
  component=resolve(go.assets_file,cp['component'])
  if component.type.name!='MonoBehaviour':continue
  d=component.read_typetree();script=resolve(component.assets_file,d['m_Script']);kind=script.read_typetree()['m_ClassName']
  if go.peek_name()=='enter_txt'and kind=='Text'and number_text is None:number_text=(component,d)
  if kind=='CToggle'and toggle is None:toggle=d
  if kind=='CButton'and number_button is None:number_button=d
number_font=resolve(number_text[0].assets_file,number_text[1]['m_FontData']['m_Font']);write('Fonts/advanced.ttf',bytes(number_font.read_typetree()['m_FontData']),number_font)
room_root=obj('myassets/ui/lobby/extend/friend_room/prefab_output/ui_friendroom.prefab','GameObject')
room_font=None
for go in subtree(room_root):
 for cp in go.read_typetree()['m_Component']:
  component=resolve(go.assets_file,cp['component'])
  if component.type.name!='MonoBehaviour':continue
  d=component.read_typetree()
  if 'm_FontData' not in d:continue
  candidate=resolve(component.assets_file,d['m_FontData']['m_Font'])
  if candidate.peek_name()=='chs_hyyk':room_font=candidate;break
 if room_font is not None:break
assert room_font is not None,'Native room caption font missing'
write('Fonts/room-caption.ttf',bytes(room_font.read_typetree()['m_FontData']),room_font)
advanced_style=dict(rowHeight=112,numberWidth=248.36,numberHeight=83.93,numberFontSize=number_text[1]['m_FontData']['m_FontSize'],descriptionFontSize=30,optionWidth=253.6,optionHeight=91.9,optionFontSize=36,clickScale=toggle['click_bigger'],buttonClickScale=number_button.get('click_bigger',1.1),helpSize=39.6,buttonFade=.1)
js('advanced-style.json',advanced_style,root)
for path in ['myassets/ui/lobby/main/anim/lobby_title_in.anim','myassets/ui/lobby/main/anim/lobby_title_out.anim','myassets/ui/lobby/main/anim/friend_room_in.anim','myassets/ui/lobby/main/anim/friend_room_out.anim','myassets/ui/common/main/anim/ui_pop.anim','myassets/ui/common/main/anim/ui_popout.anim']:
 o=obj(path,'AnimationClip');d=o.read_typetree();js('Evidence/'+o.peek_name()+'.json',d,o)
for name,path in [('flyin','myassets/ui/lobby/main/audio/flyin.mp3'),('normalflyin','myassets/ui/lobby/main/audio/normalflyin.mp3'),('btn_click','myassets/ui/mj/main/audio/btn_click.mp3'),('btn_cancel','myassets/ui/mj/main/audio/btn_cancel.mp3'),('btn_appear','myassets/ui/mj/main/audio/btn_appear.mp3')]:
 o=obj(path,'AudioClip');filename,data=next(iter(o.read().samples.items()));write('Audio/'+name+Path(filename).suffix,data,o)
# Find global sounds by actual APK catalog path, never guess a remote resource.
for row in rows:
 for p in row['paths']:
  if p.endswith(('/lobby.mp3','/button.mp3','/button_click.mp3','/pop.mp3','/back.mp3','/click.mp3'))and '/audio/'in p:
   o=obj(p,'AudioClip');filename,data=next(iter(o.read().samples.items()));write('Audio/'+o.peek_name()+Path(filename).suffix,data,o)
for name in ['ui_lobby','ui_creatroom','ui_friendroom']:
 path='luabyte/lua/ui/'+name+'.lua.bytes'
 try:
  row=next(r for r in rows if path in r['paths']);f=af(row['bundle']);o=next(o for o in f.objects.values()if o.type.name=='TextAsset');d=o.read();s=d.m_Script;write('Evidence/'+name+'.lua.bytes',s.encode('utf8','surrogateescape')if isinstance(s,str)else bytes(s),o)
 except StopIteration:pass
# Decode the original streamed cubic coefficients into 60 Hz samples. This
# keeps overshoot, delays and durations rather than substituting an ease curve.
import struct,math
animations=[]
for file in (OUT/'Evidence').glob('*.json'):
 d=json.loads(file.read_text(encoding='utf8'));clip=d['m_MuscleClip'];c=clip['m_Clip']['data'];stream=c['m_StreamedClip'];raw=struct.pack('<'+'I'*len(stream['data']),*stream['data']);pos=0;keys={}
 while pos<len(raw):
  time,count=struct.unpack_from('<fI',raw,pos);pos+=8
  for _ in range(count):
   index,a,b,cc,v=struct.unpack_from('<Iffff',raw,pos);pos+=20
   if math.isfinite(time)and time>=0:keys.setdefault(index,[]).append((time,a,b,cc,v))
 duration=clip['m_StopTime'];tracks=[];index=0
 for binding in d['m_ClipBindingConstant']['genericBindings']:
  count=3 if binding['typeID']==4 and binding['attribute']in [1,3] else 4 if binding['typeID']==4 and binding['attribute']==2 else 1
  for part in range(count):
   ks=keys.get(index,[]);values=[]
   for frame in range(round(duration*60)+1):
    time=min(duration,frame/60)
    if ks:
     k=next((k for k in reversed(ks)if k[0]<=time),ks[0]);dt=max(0,time-k[0]);value=((k[1]*dt+k[2])*dt+k[3])*dt+k[4]
    else:
     constant=index-stream['curveCount']-c['m_DenseClip']['m_CurveCount'];value=c['m_ConstantClip']['data'][constant]
    values.append(round(value,6))
   tracks.append(dict(path=binding['path'],attribute=binding['attribute'],part=part,values=values));index+=1
 animations.append(dict(name=d['m_Name'].lower(),duration=duration,tracks=tracks))
(OUT/'motion.json').write_text(json.dumps(dict(animations=animations),indent=2)+'\n',encoding='utf8')
(OUT/'borders.json').write_text(json.dumps(dict(borders=[dict(key=r['output'][:-4],values=r['border'])for r in records if 'border'in r]),indent=2)+'\n',encoding='utf8')
(OUT/'provenance.json').write_text(json.dumps(dict(apk='Majsoul_4.0.16_MC.apk',assets=records,reused=[dict(resource='Majsoul/Font/game',sha256=body_hash,source='Exact Font referenced by UI_FriendRoom/label_name')],derived=[dict(output='motion.json',source='Evidence AnimationClip streamed cubic coefficients',sha256=hashlib.sha256((OUT/'motion.json').read_bytes()).hexdigest()),dict(output='borders.json',source='Native Sprite.m_Border',sha256=hashlib.sha256((OUT/'borders.json').read_bytes()).hexdigest())],limitations=['Encrypted Lua is evidence, not recovered source. Native sprites, fonts, audio and AnimationClip data are extracted exactly. Offline flow is implemented by the project.']),ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print('PASS',len(records),'APK lobby resources')
