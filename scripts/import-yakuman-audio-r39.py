from pathlib import Path
import hashlib,json,re,runpy
R=Path(__file__).resolve().parent.parent
S=runpy.run_path(str(R/'scripts/study-characters-1018.py'))
O=R/'unity/Assets/Resources/Majsoul/NativeReview/YakumanR39';O.mkdir(parents=True,exist_ok=True)
audio=R/'unity/Assets/Resources/Majsoul/Audio'
text=''.join(p.read_text('utf8')for p in(R/'build/feedback-1114-r39/lua/audio').glob('*.lua'))
entries=[];audit=[]
definitions=[(35,'天和','tianhu',11),(36,'地和','dihu',12),(37,'大三元','dasanyuan',34),(38,'四暗刻','sianke',33),(39,'字一色','ziyise',32),(40,'绿一色','lvyise',24),(41,'清老头','qinglaotou',22),(42,'国士无双','guoshiwushuang',31),(43,'小四喜','xiaosixi',23),(44,'四杠子','sigangzi',13),(45,'九莲宝灯','jiulianbaodeng',21),(47,'纯正九莲宝灯','chunzhengjiulianbaodeng',14),(48,'四暗刻单骑','siankedanqi',17),(49,'国士无双十三面','guoshishisanmian',15),(50,'大四喜','dasixi',16)]
for _,name,key,_ in definitions+[(-1,'前导','main',0)]:
    cue='yiman_preload'if key=='main'else 'fan_'+key
    path=next(p for p in S['paths']if p.startswith('myassets/scenes/mj/effect_yiman/')and p.endswith('/'+cue+'.mp3'))
    obj=S['obj'](path,'AudioClip');clip=obj.read();samples=clip.samples;assert len(samples)==1
    filename,raw=next(iter(samples.items()));file=cue+Path(filename).suffix;(audio/file).write_bytes(raw)
    audit.append(dict(source=path,file='Majsoul/Audio/'+file,bytes=len(raw),sha256=hashlib.sha256(raw).hexdigest(),seconds=clip.m_Length))
for id,name,key,priority in definitions:
    cue='fan_'+key;match=re.search(r'"scenes/mj/effect_yiman/[^\"]*/'+cue+r'",(\d+),"yiman"',text);assert match,cue
    entries.append(dict(id=id,name=name,key=cue,priority=priority,soundAt=int(match[1])/1000+.15))
(O/'catalog.json').write_text(json.dumps(dict(plans=entries),ensure_ascii=False,indent=1)+'\n','utf8')
(R/'build/feedback-1114-r39/yakuman/audio-audit.json').write_text(json.dumps(audit,ensure_ascii=False,indent=1)+'\n','utf8')
print('IMPORTED',len(audit),'audio clips',len(entries),'source fan definitions')
