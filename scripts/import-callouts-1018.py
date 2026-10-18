"""Extract all four UI_DesktopInfo shout hierarchies and their alpha masks from the supplied APK."""
from pathlib import Path
import hashlib,io,json,sys,warnings,zipfile
R=Path(__file__).resolve().parent.parent;O=R/'unity/Assets/Resources/Majsoul/Callouts1018';O.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(R/'tools/asset-inspector'));import UnityPy
from UnityPy.export.SpriteHelper import get_image
from PIL import Image
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
rows=[json.loads(l)for f in ['fast-catalog.jsonl','catalog.jsonl']for l in(R/'build/reference'/f).read_text('utf8').splitlines()]
cab=json.loads((R/'build/reference/tile-study/cab-map.json').read_text('utf8'));z=zipfile.ZipFile(R/'dist/Majsoul_4.0.16_MC.apk');cache={};records=[]
def af(b):
    if b not in cache:cache[b]=next(o.assets_file for o in UnityPy.load(z.read(b)).objects)
    return cache[b]
def resolve(f,p):
    if not p['m_PathID']:return None
    if p['m_FileID']:f=af(cab[Path(f.externals[p['m_FileID']-1].path).name])
    return f.objects[p['m_PathID']]
def write(n,b,o=None):
    (O/n).write_bytes(b);rec=dict(output=n,sha256=hashlib.sha256(b).hexdigest())
    if o:rec.update(bundle=cab[o.assets_file.name],pathID=o.path_id)
    records.append(rec)
def vec(d):return [d[k]for k in ('xyzw'if'w'in d else'xyz'if'z'in d else'xy')]
path='myassets/ui/mj/main/prefab_output/ui_desktopinfo.prefab';row=next(r for r in rows if path in r['paths']);f=af(row['bundle']);root=next(o for o in f.objects.values()if o.type.name=='GameObject'and o.peek_name().lower()=='ui_desktopinfo')
nodes=[];needed=set()
def walk(go,path=''):
    gd=go.read_typetree();objs=[resolve(go.assets_file,c['component'])for c in gd['m_Component']];t=next(o for o in objs if o.type.name in ['Transform','RectTransform']);td=t.read_typetree();path+='/'+gd['m_Name']
    if '/container_shout' in path and not any(s in path for s in ['aprilfool','img_content2','img_content3']):
        slot=int(path.split('container_player_')[1][0]);short=path.split('/container_shout',1)[1].lstrip('/');co=[o.read_typetree()for o in objs if o.type.name=='MonoBehaviour'];asset=next((c['asset_path']for c in co if c.get('asset_path')),None)
        node=dict(slot=slot,path=short,position=vec(td['m_AnchoredPosition']),size=vec(td['m_SizeDelta']),pivot=vec(td['m_Pivot']),anchor=vec(td['m_AnchorMin']),scale=vec(td['m_LocalScale']),rotation=vec(td['m_LocalRotation']),image=Path(asset).stem if asset else'',fixMode=next((c['FixMode']for c in co if 'FixMode'in c),0))
        if asset:needed.add(Path(asset).stem)
        nodes.append(node)
    for cp in td['m_Children']:
        tr=resolve(t.assets_file,cp);walk(resolve(tr.assets_file,tr.read_typetree()['m_GameObject']),path)
walk(root)
found=set()
for row in rows:
    if not any('atlas_mj_main_common_'in p for p in row.get('paths',[])):continue
    for obj in af(row['bundle']).objects.values():
        if obj.type.name=='Sprite'and obj.peek_name()in needed and obj.peek_name()not in found:
            b=io.BytesIO();obj.read().image.save(b,format='PNG');write(obj.peek_name()+'.png',b.getvalue(),obj);found.add(obj.peek_name())
assert needed-{'shout_chi','shout_lizhi'}<=found,needed-found
words={'shout_chi','shout_peng','shout_gang','shout_lizhi','shout_babei'};word_info=[]
row=next(r for r in rows if any(p.endswith('atlas_mj_main_chs_0.png')for p in r['paths']))
for obj in af(row['bundle']).objects.values():
    if obj.type.name!='Sprite'or obj.peek_name()not in words:continue
    sprite=obj.read();rd=sprite.m_RD;rect=sprite.m_Rect
    # Sprite.image returns the tight textureRect, discarding transparent padding.
    # Unity UI uses Sprite.rect (187x143) and its trim offsets. Export that entire
    # logical rectangle so the native CImage.FixImage width rule remains correct.
    assert not(rd.settingsRaw&1),'Packed sprites require an atlas transform'
    atlas=get_image(sprite,rd.texture,rd.alphaTexture)
    image=atlas.crop((rect.x,rect.y,rect.x+rect.width,rect.y+rect.height)).transpose(Image.Transpose.FLIP_TOP_BOTTOM)
    assert image.size==(187,143),(sprite.m_Name,image.size)
    b=io.BytesIO();image.save(b,format='PNG');write(sprite.m_Name+'.png',b.getvalue(),obj)
    ink=image.getchannel('A').point(lambda a:255 if a>=8 else 0).getbbox()
    word_info.append(dict(name=sprite.m_Name,size=list(image.size),inkBounds=[ink[0],ink[1],ink[2]-ink[0],ink[3]-ink[1]],characters=2 if sprite.m_Name in ['shout_lizhi','shout_babei']else 1,sourceRect=dict(x=rect.x,y=rect.y,width=rect.width,height=rect.height),tightSize=list(sprite.image.size),trimOffset=[rd.textureRectOffset.x,rd.textureRectOffset.y]))
assert len(word_info)==5
write('word-sprites.json',(json.dumps(dict(sprites=word_info,inkAlphaThreshold=8,fixMode=1,fixModeName='FORCE_KEEP_WIDTH',component='capsui.CImage.FixImage'),indent=2)+'\n').encode())
write('layout.json',(json.dumps(dict(nodes=nodes),indent=2)+'\n').encode(),root)
write('source.json',(json.dumps(dict(apk='Majsoul_4.0.16_MC.apk',prefab=path,assets=records),indent=2)+'\n').encode())
print('Exported',len(nodes),'nodes,',len(found),'backgrounds/masks and 5 full 187x143 word sprites; native FixMode preserved.')
