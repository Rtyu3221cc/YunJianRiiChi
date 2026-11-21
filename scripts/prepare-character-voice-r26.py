"""Import all current voice-page metadata and two original playback icons."""
from pathlib import Path
import collections,hashlib,io,json,runpy
R=Path(__file__).resolve().parent.parent;B=R/'build/review-r26';O=R/'unity/Assets/Resources/Majsoul/Characters'
cfg=json.loads((R/'build/characters-1018/config.json').read_text('utf8'))
roles=json.loads((R/'unity/Assets/Resources/Majsoul/CharacterDirectory/catalog.json').read_text('utf8'))['roles']
characters={c['sound_folder']:c for c in cfg['item_definition/character']}
sounds=collections.defaultdict(list)
for row in cfg['voice/sound']:sounds[row['id']].append(row)
pages=[]
for role in roles:
    root=O if role['key']in('yiji','erjietang')else R/'unity/Assets/CharacterPacks/Source'/role['key']
    lines=[]
    for row in sounds[characters[role['key']]['sound']]:
        if row.get('hide')==1:continue
        path=row['path'];source=root/'Voice'/role['key']/(path+'.mp3')
        lines.append(dict(type=row['type'],name=row['name_chs'],text=row.get('words_chs',''),path=path,available=source.is_file()or(root/'Voice'/role['key']/(Path(path).name+'.mp3')).is_file()))
    pages.append(dict(key=role['key'],lines=lines))
payload=dict(schema=1,roles=pages)
(O/'voice-directory.json').write_text(json.dumps(payload,ensure_ascii=False,separators=(',',':'))+'\n','utf8')
S=runpy.run_path(str(R/'scripts/study-characters-1018.py'));needed={'bf_play','bf_pause'};records=[]
bundles={b for p,b in S['paths'].items()if p.startswith('myassets/ui/common/main/pic/')}
for bundle in bundles:
    for obj in S['af'](bundle).objects.values():
        if obj.type.name!='Sprite' or obj.peek_name()not in needed:continue
        name=obj.peek_name();image=obj.read().image;raw=io.BytesIO();image.save(raw,format='PNG');data=raw.getvalue();(O/'UI'/ (name+'.png')).write_bytes(data);needed.remove(name);records.append(dict(name=name,bundle=bundle,pathID=obj.path_id,sha256=hashlib.sha256(data).hexdigest()))
    if not needed:break
assert not needed,needed
report=dict(roles=len(pages),visibleLines=sum(len(p['lines'])for p in pages),availableLines=sum(x['available']for p in pages for x in p['lines']),icons=records,sourceConfig='build/characters-1018/config.json',sourceLua='UI_Visit.Page_Sound',hiddenRowsExcluded=True)
(B/'voice-directory-import.json').write_text(json.dumps(report,ensure_ascii=False,indent=1)+'\n','utf8')
print('VOICE PAGE IMPORT',report['roles'],'roles;',report['visibleLines'],'visible source rows;',report['availableLines'],'audio files;',len(records),'original icons')
