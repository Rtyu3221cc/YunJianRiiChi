"""Replace the four reviewed effects while retaining the unified reference folder."""
from pathlib import Path
import importlib.util,json,shutil,sys
import numpy as np
from PIL import Image,ImageDraw,ImageOps

R=Path(__file__).resolve().parent.parent
spec=importlib.util.spec_from_file_location('effects_r11_pack',R/'scripts/package-native-effects-r11.py')
P=importlib.util.module_from_spec(spec);spec.loader.exec_module(P)
O=P.O;S=R/'build/effects-r12-review/render';P.S=S
A=O/'source/r12';A.mkdir(exist_ok=True)
report=json.loads((S/'capture.json').read_text('utf8'));fps=report['fps'];groups={}
for shot in report['shots']:
 # Keep the held tile and incoming paw/blade inside the close-up as they move
 # toward the settled discard. A crop centred only on the final tile clips them.
 x,y,w,h=shot['detail'];cx=x+w*.5;cy=y+h*.6
 shot['detail']=[max(0,min(500,cx-550)),max(0,min(104,cy-456)),1100,760]
 groups.setdefault(shot['key'],[]).append(shot)
assert len(groups)==4 and fps==60
previous=A/'previous-media.json'
if not previous.exists():shutil.copyfile(O/'media.json',previous)
media=json.loads(previous.read_text('utf8'));verified=[];waves=[]
for key,shots in groups.items():
 shots.sort(key=lambda s:s['viewer']);count=shots[0]['end']-shots[0]['start']
 assert [s['viewer']for s in shots]==[0,1,2,3]
 wave=P.sound(shots[0]['cue'],count/fps,shots[0]['cueAt']);waves.append(wave)
 item=next(i for i in media['items']if i['key']==key)
 for close,field in [(False,'full'),(True,'close')]:
  path=O/'game'/(key+'-r12-'+('detail'if close else'four')+'.mp4')
  if not path.exists():P.encode(path,(P.grid(shots,i,close)for i in range(count)),fps,wave)
  verified.append(P.validate(path,count));item[field]=path.relative_to(O).as_posix()
 poster=O/'game'/(key+'-r12.jpg');P.grid(shots,round((.35 if key=='ron-ko'else .7)*fps),True).save(poster,quality=90)
 item['poster']=poster.relative_to(O).as_posix();item['revision']=12;item['fps']=60
 if key=='ron-ko':item['reference']='original/ron-ko-iab.mp4'
 if key=='effect-liqi-23ex-maozhua':item['reference']='original/riichi-maozhua-iab.mp4'
 print('R12 packaged',key,flush=True)

short=O/'云间日麻-四项特效修正R12.mp4'
def corrected_images():
 for key,shots in groups.items():
  shot=shots[0]
  for i in range(shot['end']-shot['start']):yield P.raw(shot,i)
short_count=sum(s[0]['end']-s[0]['start']for s in groups.values())
if not short.exists():P.encode(short,corrected_images(),fps,np.concatenate(waves,axis=1))
verified.append(P.validate(short,short_count))

# Replace the same-duration sections in the existing 24-effect compilation.
old=json.loads((O/'game-capture.json').read_text('utf8'));single=[s for s in old['shots']if s['viewer']==0]
all_path=O/'云间日麻-全部特效预览.mp4';all_stamp=A/'compilation.json'
if not all_stamp.exists():
 temporary=O/'全部特效-r12.new.mp4'
 def all_images():
  with P.av.open(str(all_path))as movie:
   frames=iter(movie.decode(video=0))
   for shot in single:
    for i in range(shot['end']-shot['start']):
     previous_frame=next(frames)
     yield P.raw(groups[shot['key']][0],i*fps//old['fps'])if shot['key']in groups else previous_frame.to_image()
 all_count=sum(s['end']-s['start']for s in single)
 all_wave=np.concatenate([P.sound(s['cue'],(s['end']-s['start'])/old['fps'],s['cueAt'])for s in single],axis=1)
 P.encode(temporary,all_images(),old['fps'],all_wave);P.validate(temporary,all_count);temporary.replace(all_path)
 all_stamp.write_text(json.dumps(P.validate(all_path,all_count),ensure_ascii=False,indent=2)+'\n','utf8')
verified.append(P.validate(all_path))

originals=media['originals']
for name in ['ron-ko-iab','riichi-maozhua-iab']:
 path=O/'original'/(name+'.mp4');row=P.validate(path,360);capture=json.loads(path.with_suffix('.capture.json').read_text('utf8'))
 row.update(source=capture['source'],audio=False,clientVersion='4.0.51');originals=[r for r in originals if r['file']!=row['file']]+[row]
 path.with_suffix('.encoded.json').write_text(json.dumps(row,ensure_ascii=False,indent=2)+'\n','utf8')

removed={f'game/{key}-{suffix}.mp4'for key in groups for suffix in ['four','close']}
removed.update(f'game/{key}-r12-close.mp4'for key in groups)
media['encoded']=[row for row in media['encoded']if row['file']not in removed and row['file']!=all_path.name]+verified
media['originals']=originals;media['revision']=12
(O/'media.json').write_text(json.dumps(media,ensure_ascii=False,indent=2)+'\n','utf8')

page=(O/'index.html').read_text('utf8');start=page.index('const items=')+len('const items=');end=page.index(';const p=',start)
page=page[:start]+json.dumps(media['items'],ensure_ascii=False,separators=(',',':'))+page[end:]
page=page.replace('雀魂原版参考（Edge 录制，无录音）','雀魂原版参考（Edge／内置浏览器录制，无录音）')
notice='<p class="note" id="r12-note">R12 已更新 K.O.、猫过留痕、问心之镰、滚雪球效应，修正片段为 60 帧。<a class="button" href="r12.html">只看本轮四项修正</a> <a class="button" href="云间日麻-四项特效修正R12.mp4">播放修正合辑</a></p>'
if 'id="r12-note"'not in page:page=page.replace('<div class="row"><a class="button" href="云间日麻-全部特效预览.mp4">',notice+'<div class="row"><a class="button" href="云间日麻-全部特效预览.mp4">',1)
(O/'index.html').write_text(page,'utf8')
only=[i for i in media['items']if i['key']in groups];start=page.index('const items=')+len('const items=');end=page.index(';const p=',start)
four=page[:start]+json.dumps(only,ensure_ascii=False,separators=(',',':'))+page[end:]
four=four.replace('13 款和牌、11 款立直。','本轮修正 1 款和牌、3 款立直，均有原版参考。').replace('<h1>云间日麻 · 装扮特效预览</h1>','<h1>云间日麻 · 四项特效修正 R12</h1>')
four=four.replace('href="云间日麻-全部特效预览.mp4">播放全部特效合辑','href="云间日麻-四项特效修正R12.mp4">播放四项修正合辑')
(O/'r12.html').write_text(four,'utf8')

proof=Image.new('RGB',(1200,720),(20,20,25));draw=ImageDraw.Draw(proof)
for index,(key,shots)in enumerate(groups.items()):
 shot=shots[0];image=P.raw(shot,round((.35 if key=='ron-ko'else .7)*fps));x,y,w,h=shot['detail'];image=image.crop((round(x),round(y),round(x+w),round(y+h)))
 image=ImageOps.contain(image,(600,324));left=index%2*600;top=index//2*360;proof.paste(image,(left+(600-image.width)//2,top));draw.text((left+12,top+326),shot['title'],font=P.FONT,fill=(245,220,177))
proof.save(O/'四项修正R12.jpg',quality=90)
for name in ['capture.json','checks.txt']:shutil.copyfile(S/name,A/name)
for name in ['current-fizz-main-mask.glsl','shader-evidence.json','shader-and-direction-checks.txt']:shutil.copyfile(R/'build/effects-r12-review'/name,A/name)

# Decode every retained video and refresh its hash before deleting obsolete media.
qa=[]
for row in media['encoded']+media['originals']:
 actual=P.validate(O/row['file'],row['frames']);assert actual['sha256']==row['sha256'],row['file'];qa.append(actual)
(A/'final-media-qa.json').write_text(json.dumps(dict(passed=True,videos=len(qa),records=qa),ensure_ascii=False,indent=2)+'\n','utf8')
cleanup=[]
for name in sorted(removed):
 path=(O/name).resolve();assert path.is_relative_to(O.resolve())
 if path.exists():cleanup.append(dict(file=name,bytes=path.stat().st_size));path.unlink()
(A/'replaced-media-cleanup.json').write_text(json.dumps(cleanup,ensure_ascii=False,indent=2)+'\n','utf8')
print('R12 PACKAGE PASS',len(qa),'videos validated',flush=True)
