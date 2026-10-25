"""Extract the reference APK's table chat sprites without changing their canvas."""
from pathlib import Path
import hashlib,json,runpy
from PIL import Image
R=Path(__file__).resolve().parent.parent
S=runpy.run_path(str(R/'scripts/study-characters-1018.py'))
out=R/'unity/Assets/Resources/Majsoul/TableEmotes';out.mkdir(parents=True,exist_ok=True)
wanted={'bg_chat_content','bg_func_big','bg_arrow','lifu_arrow','btn_emo'}
seen=set();records=[]
for path,bundle in S['paths'].items():
    if bundle in seen or not any(k in path for k in ['/ui/mj/main/pic/','/ui/common/main/pic/']):continue
    if any('/'+k+'/' in path for k in ['en','jp','chs_t','kr']):continue
    seen.add(bundle)
    for obj in S['af'](bundle).objects.values():
        if obj.type.name!='Sprite' or obj.peek_name() not in wanted:continue
        name=obj.peek_name();tree=obj.read_typetree();image=obj.read().image;rect=tree['m_Rect'];offset=tree['m_RD']['textureRectOffset']
        canvas=Image.new('RGBA',(round(rect['width']),round(rect['height'])))
        canvas.paste(image,(round(offset['x']),round(rect['height']-offset['y']-image.height)))
        file=out/(name+'.png');canvas.save(file)
        records.append(dict(name=name,bundle=bundle,pathID=obj.path_id,size=canvas.size,border=[tree['m_Border'][k] for k in 'xyzw'],sha256=hashlib.sha256(file.read_bytes()).hexdigest()))
        wanted.remove(name)
    if not wanted:break
assert not wanted,wanted
(out/'source.json').write_text(json.dumps(dict(apk='Majsoul_4.0.16_MC.apk',records=records),ensure_ascii=False,indent=2)+'\n','utf8')
print('Imported',len(records),'native table chat sprites')
