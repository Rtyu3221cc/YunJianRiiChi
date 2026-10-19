from pathlib import Path
import sys,re,json,zipfile
R=Path(__file__).resolve().parent.parent
code=(R/'build/reference/fast-catalog.py').read_text('utf8').split('with zipfile.ZipFile')[0]
exec(code)
pat=re.compile(rb'[a-zA-Z0-9_/.-]+\.(?:mp3|wav|ogg)')
out=R/'build/characters-1018/audio-catalog.jsonl'
with zipfile.ZipFile(R/'dist/Majsoul_4.0.16_MC.apk')as z,out.open('w',encoding='utf8')as f:
 entries=[e for e in z.infolist()if e.filename.endswith('.majset')]
 for i,e in enumerate(entries):
  try:
   ps=scan(z.read(e))
   if ps:f.write(json.dumps(dict(bundle=e.filename,paths=ps))+'\n')
  except Exception:pass
  if i%700==0:f.flush();print(i,flush=True)
print('Audio scan done',flush=True)
