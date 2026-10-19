from pathlib import Path
import runpy,json,hashlib
R=Path(__file__).resolve().parent.parent;S=runpy.run_path(str(R/'scripts/study-characters-1018.py'));O=R/'unity/Assets/Resources/Majsoul/Characters';result={};records=[]
groups={
 'ui_liaoshemain':{'name':'/illust/container_name/label_name_short','cv':'/illust/container_name/label_CV_short'},
 'ui_visit':{'profile-label':'/container_text/shenggao','profile-body':'/shenggao/height','profile-heading':'/content/intro/node'},
 'ui_liaoshechangeskin':{'skin-title':'/container_info/skin_name','skin-body':'/skin_desc_root/Viewport/Content/text'},
 'ui_dormsortcharacter':{'sort':'/favor_title/text','sort-help':'/desc'}
}
for prefab,fields in groups.items():
 p='myassets/ui/lobby/extend/liaoshe/prefab_output/'+prefab+'.prefab';root=S['obj'](p,'GameObject');f=root.assets_file;nodes=list(S['tree'](root))
 for key,suffix in fields.items():
  node=next(n for n in nodes if n['path'].endswith(suffix));d=next(c['data']for c in node['components']if 'm_FontData'in c['data']);font=S['resolve'](f,d['m_FontData']['m_Font']);fd=font.read_typetree();name=font.peek_name();b=bytes(fd['m_FontData']);dest=O/'Fonts'/(name+'.ttf');dest.parent.mkdir(exist_ok=True,parents=True);dest.write_bytes(b);result[key]=dict(resource='Majsoul/Characters/Fonts/'+name,size=d['m_FontData']['m_FontSize'],name=name);records.append(dict(field=key,prefab=p,node=node['path'],font=name,sha256=hashlib.sha256(b).hexdigest(),bytes=len(b)))
(O/'native-fonts.json').write_text(json.dumps(dict(fonts=[dict(key=k,**v)for k,v in result.items()]),ensure_ascii=False,indent=2),encoding='utf8');(R/'build/character-review-r1/font-source.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf8');print(result)
