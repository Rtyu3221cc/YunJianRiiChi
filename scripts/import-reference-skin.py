"""Import the supplied APK's original HUD sprites and the reference's chigua cloth."""
import sys,json,zipfile,warnings,pathlib,hashlib,urllib.request
sys.path.insert(0,'tools/asset-inspector')
import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1'
warnings.filterwarnings('ignore')
root=pathlib.Path('unity/Assets/Resources/Majsoul')
out=root/'UI';out.mkdir(parents=True,exist_ok=True)
names={'btn_leave','btn_settings','btn_rule','left_top','bg_name','bg_nickname','bg_name_1','bg_head1_mask','bg_action','bg_chipengchoose','btn_skip','btn_emo','botton_zhandou','ob_ting_off_btn','ob_ting_on_btn','button_text_bottom_s','replay_collect','bg_type','bg_point','state_1','state_2','line_zhandou'}
rows=[json.loads(s) for s in open('build/reference/catalog.jsonl',encoding='utf8')]
row=next(r for r in rows if any('atlas_mj_main_common_0.png' in p for p in r.get('paths',[])))
records=[]
with zipfile.ZipFile('dist/Majsoul_4.0.16_MC.apk') as z:
 env=UnityPy.load(z.read(row['bundle']))
 for o in env.objects:
  if o.type.name=='Sprite' and o.peek_name() in names:
   d=o.read();p=out/(d.m_Name+'.png');d.image.save(p)
   records.append({'sprite':d.m_Name,'bundle':row['bundle'],'file':str(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
 target='myassets/deco/head_frame/headframe_default/icon/common/headframe_default.png'
 frame=next(json.loads(s) for s in open('build/reference/fast-catalog.jsonl',encoding='utf8') if target in s)
 env=UnityPy.load(z.read(frame['bundle']))
 p=out/'headframe-default.png';env.container[target].read().image.save(p)
 records.append({'source':target,'bundle':frame['bundle'],'file':str(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
index=json.load(open('build/reference/public/resversion0.11.252.w.json'))['res']
key='scene/Assets/Resource/tablecloth/tablecloth_chigua/Table_Dif.jpg'
url='https://game.maj-soul.com/1/'+index[key]['prefix']+'/'+key
p=root/'Table/cloth-chigua.jpg'
p.write_bytes(urllib.request.urlopen(url,timeout=30).read())
records.append({'source':url,'file':str(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
(root/'reference-skin.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf8')
print('Imported',len(records),'reference skin resources')
